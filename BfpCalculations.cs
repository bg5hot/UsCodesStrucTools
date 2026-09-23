using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    /// <summary>
    /// BFP (Bolted Flange Plate) Connection Design Verification
    /// AISC 358-16 Chapter 7, Section 7.6 - Design Procedure (17 steps)
    /// </summary>
    public static class BfpCalculations
    {
        // ====================== CONSTANTS ======================
        public const double PHI_D = 1.00;   // Ductile limit states
        public const double PHI_N = 0.90;   // Nonductile limit states
        public const double E = 29000.0;    // Modulus of elasticity (ksi)

        // Bolt grades prequalified for BFP (Section 7.5)
        public static readonly Dictionary<string, BoltGradeData> BoltGrades = new()
        {
            { "A490",  new BoltGradeData { Fnt = 113.0, Fnv = 68.0, Fu = 150.0 } },
            { "F2280", new BoltGradeData { Fnt = 113.0, Fnv = 68.0, Fu = 150.0 } },
        };

        // Nominal unthreaded bolt areas (in^2)
        public static readonly Dictionary<double, double> BoltAreas = new()
        {
            { 0.625,  0.307 },
            { 0.75,   0.442 },
            { 0.875,  0.601 },
            { 1.0,    0.785 },
            { 1.125,  0.994 },
        };

        // ====================== DATA CLASSES ======================

        public class BoltGradeData
        {
            public double Fnt { get; set; }  // Nominal tensile strength (ksi)
            public double Fnv { get; set; }  // Nominal shear strength (ksi)
            public double Fu  { get; set; }  // Ultimate tensile strength (ksi)
        }

        public class InputParameters
        {
            // Beam properties
            public string BeamDesignation = "";
            public double BeamD  = 24.0;   // Depth (in)
            public double BeamBf = 9.0;    // Flange width (in)
            public double BeamTf = 0.5;    // Flange thickness (in)
            public double BeamTw = 0.3;    // Web thickness (in)
            public double BeamZx = 200.0;  // Plastic section modulus (in^3)
            public double BeamFy = 50.0;   // ksi (A992)
            public double BeamFu = 65.0;   // ksi (A992)
            public double BeamRy = 1.1;
            public double BeamRt = 1.2;

            // Column properties
            public string ColDesignation = "";
            public double ColD  = 14.0;
            public double ColBf = 15.0;
            public double ColTf = 1.0;
            public double ColTw = 0.5;
            public double ColZx = 400.0;
            public double ColFy = 50.0;
            public double ColFu = 65.0;
            public double ColRy = 1.1;

            // Flange plate geometry
            public double Bp = 9.0;       // Plate width (in)
            public double Tp = 0.5;       // Plate thickness (in)
            public double Db = 1.0;       // Bolt diameter (in)
            public string BoltGrade = "A490";
            public int    NBolts = 6;      // Must be even
            public double S1 = 3.0;       // Distance from column face to first bolt row (in)
            public double S  = 3.0;       // Bolt row spacing (in)
            public double EdEdge = 2.0;   // Edge distance at beam end (in)

            // Plate material
            public double PlateFy = 50.0;  // ksi (A572 Gr 50)
            public double PlateFu = 65.0;  // ksi (A572 Gr 50)

            // Design parameters
            public double Span = 300.0;    // Span (in)
            public string SystemType = "SMF"; // SMF or IMF

            // Gravity loads
            public double LoadD = 0.0;     // kips
            public double LoadL = 0.0;
            public double LoadS = 0.0;
            public double F1   = 0.5;      // Live load factor
            public double Vu   = 0.0;      // Specified shear (kips)
        }

        public class DesignResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = "";
            public List<string> Process { get; set; } = new();

            public bool OverallPassed { get; set; }

            // Individual checks
            public bool PrequalificationPassed { get; set; }
            public bool BoltDiameterCheckPassed { get; set; }
            public bool BoltCountCheckPassed { get; set; }
            public bool PlateYieldingPassed { get; set; }
            public bool PlateRupturePassed { get; set; }
            public bool BlockShearPassed { get; set; }
            public bool CompressionBucklingPassed { get; set; }
            public bool BeamShearPassed { get; set; }
            public bool WebConnectionPassed { get; set; }
            public bool ContinuityPlatesPassed { get; set; }
            public bool PanelZonePassed { get; set; }

            // Key results
            public double Cpr { get; set; }
            public double Mpr { get; set; }   // kip-in
            public double Mf  { get; set; }   // kip-in
            public double Fpr { get; set; }   // kips
            public double Rn  { get; set; }   // kips/bolt
            public double Vh  { get; set; }   // kips
            public double Sh  { get; set; }   // in

            // Utilization ratios
            public double BoltCountRatio { get; set; }
            public double PlateYieldingRatio { get; set; }
            public double PlateRuptureRatio { get; set; }
            public double BlockShearRatio { get; set; }
            public double CompressionBucklingRatio { get; set; }
            public double BeamShearRatio { get; set; }
            public double PanelZoneRatio { get; set; }
        }

        // ====================== MAIN CALCULATE ======================

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var p = result.Process;
            bool allPassed = true;

            try
            {
                // --- Input Summary ---
                p.Add("================================================================================");
                p.Add("  BFP CONNECTION DESIGN VERIFICATION (AISC 358-16 SECTION 7.6)");
                p.Add("================================================================================");
                p.Add("");
                p.Add("--- INPUT PARAMETERS ---");
                p.Add($"BEAM: {input.BeamDesignation} | d={input.BeamD:F2} bf={input.BeamBf:F2} tf={input.BeamTf:F3} tw={input.BeamTw:F3} Zx={input.BeamZx:F1}");
                p.Add($"      Fy={input.BeamFy} Fu={input.BeamFu} Ry={input.BeamRy} Rt={input.BeamRt}");
                p.Add($"COLUMN: {input.ColDesignation} | d={input.ColD:F2} bf={input.ColBf:F2} tf={input.ColTf:F3} tw={input.ColTw:F3}");
                p.Add($"        Fy={input.ColFy} Fu={input.ColFu}");
                p.Add($"PLATE: bp={input.Bp:F2} tp={input.Tp:F3} | Fy={input.PlateFy} Fu={input.PlateFu} ksi");
                p.Add($"       db={input.Db:F3} ({input.BoltGrade}) n={input.NBolts} S1={input.S1:F2} s={input.S:F2}");
                p.Add($"SPAN: L={input.Span:F0} in ({input.Span / 12:F1} ft) | {input.SystemType}");
                p.Add($"LOADS: D={input.LoadD} L={input.LoadL} S={input.LoadS} f1={input.F1} | Vu={input.Vu:F2}");
                p.Add("");

                // Get bolt properties
                if (!BoltGrades.ContainsKey(input.BoltGrade))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Invalid bolt grade: {input.BoltGrade}. Only A490/F2280 are prequalified.";
                    p.Add($"ERROR: {result.ErrorMessage}");
                    return result;
                }

                var grade = BoltGrades[input.BoltGrade];
                double Ab = GetBoltArea(input.Db);

                // Derived properties
                double dw = input.BeamD - 2 * input.BeamTf; // beam web depth
                double gravity = 1.2 * input.LoadD + input.F1 * input.LoadL + 0.2 * input.LoadS;

                // ===== STEP 0: PREQUALIFICATION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  PREQUALIFICATION LIMITS (SECTION 7.3)");
                p.Add("--------------------------------------------------------------------------------");

                bool prequal = true;
                string systemType = input.SystemType;

                // Beam depth <= 36
                p.Add($"  Beam depth: d = {input.BeamD:F1} in <= 36 in (W36 max): " + (input.BeamD <= 36 ? "OK" : "FAIL"));
                if (input.BeamD > 36) prequal = false;

                // Beam weight <= 150 plf (approximate from designation)
                double beamWeight = ParseWeight(input.BeamDesignation);
                p.Add($"  Beam weight: {beamWeight:F0} plf <= 150 plf: " + (beamWeight <= 150 ? "OK" : "FAIL"));
                if (beamWeight > 150) prequal = false;

                // Flange thickness <= 1.0
                p.Add($"  Flange thickness: tf = {input.BeamTf:F3} in <= 1.0 in: " + (input.BeamTf <= 1.0 ? "OK" : "FAIL"));
                if (input.BeamTf > 1.0) prequal = false;

                // Span/depth ratio
                double spanDepth = input.Span / input.BeamD;
                double sdMin = systemType == "SMF" ? 9.0 : 7.0;
                p.Add($"  Span/depth L/d = {spanDepth:F1} >= {sdMin:F0} ({systemType}): " + (spanDepth >= sdMin ? "OK" : "FAIL"));
                if (spanDepth < sdMin) prequal = false;

                // Bolt diameter max 1.125"
                p.Add($"  Bolt diameter: {input.Db:F3} in <= 1.125 in: " + (input.Db <= 1.125 ? "OK" : "FAIL"));
                if (input.Db > 1.125) prequal = false;

                // Bolt grade A490/F2280
                p.Add($"  Bolt grade: {input.BoltGrade} (A490 or F2280 required): OK");

                // Even number of bolts
                p.Add($"  Bolt count: {input.NBolts} (must be even): " + (input.NBolts % 2 == 0 ? "OK" : "FAIL"));
                if (input.NBolts % 2 != 0) prequal = false;

                // Plate Fy <= 55 ksi
                p.Add($"  Plate Fy = {input.PlateFy} ksi <= 55 ksi (A36 or A572 Gr 50): " + (input.PlateFy <= 55 ? "OK" : "FAIL"));
                if (input.PlateFy > 55) prequal = false;

                result.PrequalificationPassed = prequal;
                if (!prequal) allPassed = false;
                p.Add("");

                // ===== STEP 1: M_pr =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 1: PROBABLE MAXIMUM MOMENT M_pr (Eq. 2.4-1)");
                p.Add("--------------------------------------------------------------------------------");

                double Cpr = Math.Min((input.BeamFy + input.BeamFu) / (2 * input.BeamFy), 1.2);
                double Mpr = Cpr * input.BeamRy * input.BeamFy * input.BeamZx;

                p.Add($"  C_pr = min((Fy+Fu)/(2*Fy), 1.2) = {Cpr:F3}");
                p.Add($"  M_pr = C_pr * Ry * Fy * Zx = {Cpr:F3} * {input.BeamRy} * {input.BeamFy} * {input.BeamZx:F1}");
                p.Add($"  M_pr = {Mpr:F0} kip-in ({Mpr / 12:F1} kip-ft)");
                p.Add("");

                result.Cpr = Cpr;
                result.Mpr = Mpr;

                // ===== STEP 2: MAX BOLT DIAMETER =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 2: MAX BOLT DIAMETER TO PREVENT TENSILE RUPTURE (Eq. 7.6-2)");
                p.Add("--------------------------------------------------------------------------------");

                double dbMax = (input.BeamBf / 2) * (1 - input.BeamRy * input.BeamFy / (input.BeamRt * input.BeamFu)) - 0.125;
                bool boltDiaOK = input.Db <= dbMax;

                p.Add($"  d_b,max = (bf/2)*(1 - Ry*Fy/(Rt*Fu)) - 1/8");
                p.Add($"  d_b,max = ({input.BeamBf:F2}/2)*(1 - {input.BeamRy}*{input.BeamFy}/({input.BeamRt}*{input.BeamFu})) - 0.125");
                p.Add($"  d_b,max = {dbMax:F3} in");
                p.Add($"  Selected d_b = {input.Db:F3} in");
                p.Add($"  {(boltDiaOK ? "OK" : "FAIL")}");

                result.BoltDiameterCheckPassed = boltDiaOK;
                if (!boltDiaOK) allPassed = false;
                p.Add("");

                // ===== STEP 3: r_n =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 3: CONTROLLING NOMINAL SHEAR STRENGTH PER BOLT (Eq. 7.6-3)");
                p.Add("--------------------------------------------------------------------------------");

                double Fnv = grade.Fnv;
                double Fub = input.BeamFu;
                double Fup = input.PlateFu;

                double r1 = 1.0 * Fnv * Ab;
                double r2 = 2.4 * Fub * input.Db * input.BeamTf;
                double r3 = 2.4 * Fup * input.Db * input.Tp;
                double rn = Math.Min(r1, Math.Min(r2, r3));

                p.Add($"  r_n = min(1.0*Fnv*Ab, 2.4*Fub*db*tf, 2.4*Fup*db*tp)");
                p.Add($"  r_1 = 1.0 * {Fnv} * {Ab:F3} = {r1:F1} kips (bolt shear)");
                p.Add($"  r_2 = 2.4 * {Fub} * {input.Db:F3} * {input.BeamTf:F3} = {r2:F1} kips (beam bearing)");
                p.Add($"  r_3 = 2.4 * {Fup} * {input.Db:F3} * {input.Tp:F3} = {r3:F1} kips (plate bearing)");
                p.Add($"  r_n = {rn:F1} kips/bolt (controlling)");
                p.Add("");

                result.Rn = rn;

                // ===== STEP 4: TRIAL BOLTS =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 4: TRIAL NUMBER OF BOLTS (Eq. 7.6-4)");
                p.Add("--------------------------------------------------------------------------------");

                double nReqRaw = 1.25 * Mpr / (PHI_N * rn * (input.BeamD + input.Tp));
                int nReq = (int)Math.Ceiling(nReqRaw);
                if (nReq % 2 != 0) nReq += 1; // round to even

                p.Add($"  n >= 1.25*M_pr / (phi_n * r_n * (d + tp))");
                p.Add($"  n >= 1.25*{Mpr:F0} / ({PHI_N} * {rn:F1} * ({input.BeamD:F2} + {input.Tp:F3}))");
                p.Add($"  n >= {nReq} (rounded to even)");
                p.Add($"  Selected n = {input.NBolts}");

                if (input.NBolts < nReq)
                    p.Add($"  WARNING: More bolts may be needed");
                p.Add("");

                // ===== STEP 5: S_h =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 5: PLASTIC HINGE LOCATION S_h (Eq. 7.6-5)");
                p.Add("--------------------------------------------------------------------------------");

                double Sh = input.S1 + input.S * (input.NBolts / 2.0 - 1);

                p.Add($"  S_h = S_1 + s*(n/2 - 1)");
                p.Add($"  S_h = {input.S1:F2} + {input.S:F2}*({input.NBolts}/2 - 1)");
                p.Add($"  S_h = {Sh:F2} in");
                p.Add("");

                result.Sh = Sh;

                // ===== STEP 6: V_h =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 6: SHEAR FORCE AT PLASTIC HINGE");
                p.Add("--------------------------------------------------------------------------------");

                double Lh = input.Span - input.ColD - 2 * Sh;
                double Vh = 2 * Mpr / Lh + gravity / 2;

                p.Add($"  L_h = L - d_c - 2*S_h = {input.Span:F0} - {input.ColD:F2} - 2*{Sh:F2} = {Lh:F1} in");
                p.Add($"  V_h = 2*M_pr/L_h + V_gravity/2 = {Vh:F2} kips");
                p.Add("");

                result.Vh = Vh;

                // ===== STEP 7: M_f =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 7: MOMENT AT COLUMN FACE M_f (Eq. 7.6-6)");
                p.Add("--------------------------------------------------------------------------------");

                double Mf = Mpr + Vh * Sh;

                p.Add($"  M_f = M_pr + V_h * S_h");
                p.Add($"  M_f = {Mpr:F0} + {Vh:F2} * {Sh:F2}");
                p.Add($"  M_f = {Mf:F0} kip-in ({Mf / 12:F1} kip-ft)");
                p.Add("");

                result.Mf = Mf;

                // ===== STEP 8: F_pr =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 8: FLANGE PLATE FORCE F_pr (Eq. 7.6-7)");
                p.Add("--------------------------------------------------------------------------------");

                // NOTE: d + t_p, NOT d - t_f
                double Fpr = Mf / (input.BeamD + input.Tp);

                p.Add($"  F_pr = M_f / (d + t_p)   [NOTE: uses d + t_p, NOT d - t_f]");
                p.Add($"  F_pr = {Mf:F0} / ({input.BeamD:F2} + {input.Tp:F3})");
                p.Add($"  F_pr = {Fpr:F1} kips");
                p.Add("");

                result.Fpr = Fpr;

                // ===== STEP 9: CONFIRM BOLTS =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 9: CONFIRM NUMBER OF BOLTS (Eq. 7.6-8)");
                p.Add("--------------------------------------------------------------------------------");

                double nConfirm = Fpr / (PHI_N * rn);
                int nConfirmCeil = (int)Math.Ceiling(nConfirm);
                bool boltCountOK = input.NBolts >= nConfirm;

                p.Add($"  n >= F_pr / (phi_n * r_n)");
                p.Add($"  n >= {Fpr:F1} / ({PHI_N} * {rn:F1}) = {nConfirm:F1}");
                p.Add($"  n >= {nConfirmCeil} (minimum)");
                p.Add($"  Selected n = {input.NBolts}: {(boltCountOK ? "OK" : "FAIL")}");

                result.BoltCountCheckPassed = boltCountOK;
                result.BoltCountRatio = nConfirm / input.NBolts;
                if (!boltCountOK) allPassed = false;
                p.Add("");

                // ===== STEP 10: PLATE YIELDING =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 10: FLANGE PLATE TENSION YIELDING (Eq. 7.6-9)");
                p.Add("--------------------------------------------------------------------------------");

                double tpReq = Fpr / (PHI_D * input.PlateFy * input.Bp);
                bool plateYieldingOK = input.Tp >= tpReq;

                p.Add($"  t_p >= F_pr / (phi_d * Fy * b_fp)");
                p.Add($"  t_p >= {Fpr:F1} / ({PHI_D} * {input.PlateFy} * {input.Bp:F2})");
                p.Add($"  t_p >= {tpReq:F3} in");
                p.Add($"  Selected t_p = {input.Tp:F3} in");
                p.Add($"  {(plateYieldingOK ? "OK" : "FAIL")}");

                result.PlateYieldingPassed = plateYieldingOK;
                result.PlateYieldingRatio = tpReq / input.Tp;
                if (!plateYieldingOK) allPassed = false;
                p.Add("");

                // ===== STEP 11: PLATE RUPTURE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 11: FLANGE PLATE TENSILE RUPTURE (Eq. 7.6-10)");
                p.Add("--------------------------------------------------------------------------------");

                double dh = input.Db + 0.0625; // standard hole
                double An = input.Tp * (input.Bp - 2 * dh);
                double RnRupture = PHI_N * input.PlateFu * An;
                bool plateRuptureOK = Fpr <= RnRupture;

                p.Add($"  Net area An = tp*(bp - 2*dh) = {input.Tp:F3}*({input.Bp:F2} - 2*{dh:F4}) = {An:F2} in^2");
                p.Add($"  phi_n * Fu * An = {PHI_N} * {input.PlateFu} * {An:F2} = {RnRupture:F1} kips");
                p.Add($"  Demand F_pr = {Fpr:F1} kips");
                p.Add($"  {(plateRuptureOK ? "OK" : "FAIL")} (Utilization: {Fpr / RnRupture:F3})");

                result.PlateRupturePassed = plateRuptureOK;
                result.PlateRuptureRatio = Fpr / RnRupture;
                if (!plateRuptureOK) allPassed = false;
                p.Add("");

                // ===== STEP 12: BLOCK SHEAR =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 12: BEAM FLANGE BLOCK SHEAR RUPTURE (Eq. 7.6-11)");
                p.Add("--------------------------------------------------------------------------------");

                int n = input.NBolts;
                double Lgv = input.EdEdge + (n / 2.0 - 1) * input.S;
                double Agv = 2 * input.BeamTf * Lgv;
                double Anv = 2 * input.BeamTf * (Lgv - (n / 2.0 - 0.5) * dh);
                double Ant = input.BeamTf * (input.BeamBf - 2 * dh);

                double Ubs = 1.0;
                double RnBs1 = 0.6 * input.BeamFu * Anv + Ubs * input.BeamFu * Ant;
                double RnBs2 = 0.6 * input.BeamFy * Agv + Ubs * input.BeamFu * Ant;
                double RnBs = PHI_N * Math.Min(RnBs1, RnBs2);
                bool blockShearOK = Fpr <= RnBs;

                p.Add($"  Block shear through beam flange:");
                p.Add($"  Agv = {Agv:F2} in^2, Anv = {Anv:F2} in^2, Ant = {Ant:F2} in^2");
                p.Add($"  R_n = phi_n * min({RnBs1:F1}, {RnBs2:F1}) = {RnBs:F1} kips");
                p.Add($"  Demand F_pr = {Fpr:F1} kips");
                p.Add($"  {(blockShearOK ? "OK" : "FAIL")} (Utilization: {Fpr / RnBs:F3})");

                result.BlockShearPassed = blockShearOK;
                result.BlockShearRatio = Fpr / RnBs;
                if (!blockShearOK) allPassed = false;
                p.Add("");

                // ===== STEP 13: COMPRESSION BUCKLING =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 13: COMPRESSION PLATE BUCKLING (Eq. 7.6-12)");
                p.Add("--------------------------------------------------------------------------------");

                double KL = 0.65 * input.S1;
                double r = input.Tp / Math.Sqrt(12);
                double slenderness = KL / r;
                double Fe = Math.PI * Math.PI * E / (slenderness * slenderness);
                double Fcr;
                if (Fe >= input.PlateFy)
                    Fcr = Math.Pow(0.658, input.PlateFy / Fe) * input.PlateFy;
                else
                    Fcr = 0.877 * Fe;

                double grossArea = input.Bp * input.Tp;
                double RnBuck = PHI_N * Fcr * grossArea;
                bool compBuckOK = Fpr <= RnBuck;

                p.Add($"  KL = 0.65 * S1 = 0.65 * {input.S1:F2} = {KL:F2} in (per Commentary)");
                p.Add($"  r = tp/sqrt(12) = {r:F3} in");
                p.Add($"  KL/r = {slenderness:F1}");
                p.Add($"  Fe = pi^2*E/(KL/r)^2 = {Fe:F1} ksi");
                p.Add($"  Fcr = {Fcr:F1} ksi");
                p.Add($"  R_n = phi_n * Fcr * Ag = {PHI_N} * {Fcr:F1} * {grossArea:F2} = {RnBuck:F1} kips");
                p.Add($"  Demand F_pr = {Fpr:F1} kips");
                p.Add($"  {(compBuckOK ? "OK" : "FAIL")} (Utilization: {Fpr / RnBuck:F3})");

                result.CompressionBucklingPassed = compBuckOK;
                result.CompressionBucklingRatio = Fpr / RnBuck;
                if (!compBuckOK) allPassed = false;
                p.Add("");

                // ===== STEP 14: SHEAR STRENGTH =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 14: REQUIRED SHEAR STRENGTH (Eq. 7.6-13)");
                p.Add("--------------------------------------------------------------------------------");

                double Lh2 = input.Span - input.ColD - 2 * Sh;
                double Vu_calc = 2 * Mpr / Lh2 + gravity / 2;
                double Vn = PHI_N * 0.6 * input.BeamFy * input.BeamTw * dw;
                bool beamShearOK = Vu_calc <= Vn;

                p.Add($"  V_u = 2*M_pr/L_h + V_gravity = {Vu_calc:F1} kips");
                p.Add($"  Beam shear capacity = phi*0.6*Fy*tw*dw = {Vn:F1} kips");
                p.Add($"  {(beamShearOK ? "OK" : "FAIL")} (Utilization: {Vu_calc / Vn:F3})");

                result.BeamShearPassed = beamShearOK;
                result.BeamShearRatio = Vu_calc / Vn;
                if (!beamShearOK) allPassed = false;
                p.Add("");

                // ===== STEP 15: WEB CONNECTION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 15: SINGLE-PLATE SHEAR CONNECTION");
                p.Add("--------------------------------------------------------------------------------");

                double Vu_web = input.Vu > 0 ? input.Vu : Vh;
                double VnWeb = 0.6 * input.BeamFy * input.BeamTw * dw;

                p.Add($"  Required shear V_u = {Vu_web:F1} kips");
                p.Add($"  Beam web shear capacity (unreduced): 0.6*Fy*tw*dw = {VnWeb:F1} kips");
                if (VnWeb > 0)
                    p.Add($"  Utilization: {Vu_web / VnWeb:F3}");
                p.Add($"  Design single-plate shear connection per AISC 360 for V_u");
                p.Add($"  Weld to column: CJP, two-sided PJP, or two-sided fillet");
                p.Add($"  Beam web: bolts in short-slotted holes");
                p.Add($"  Note: Plate material {input.PlateFy} ksi (verify with actual design)");

                result.WebConnectionPassed = true;
                p.Add("");

                // ===== STEP 16: CONTINUITY PLATES =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 16: CONTINUITY PLATE CHECK (CHAPTER 2)");
                p.Add("--------------------------------------------------------------------------------");

                double tcfReq = Math.Sqrt(Fpr / (PHI_D * input.ColFy * input.ColBf));
                bool needContinuity = input.ColTf < tcfReq;

                p.Add($"  Column flange tcf = {input.ColTf:F3} in, required ~ {tcfReq:F3} in (simplified)");
                if (needContinuity)
                {
                    double tsMin = Math.Max(input.ColTw, 0.5 * input.BeamTf);
                    p.Add($"  Continuity plates RECOMMENDED (ts >= {tsMin:F3} in)");
                }
                else
                {
                    p.Add($"  Continuity plates may not be required");
                }

                result.ContinuityPlatesPassed = !needContinuity;
                if (needContinuity) allPassed = false;
                p.Add("");

                // ===== STEP 17: PANEL ZONE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 17: COLUMN PANEL ZONE (SECTION 7.4)");
                p.Add("--------------------------------------------------------------------------------");

                double Vpz = Fpr;
                double VnPz = 0.6 * input.ColFy * input.ColD * input.ColTw; // phi=1.0
                bool panelZoneOK = Vpz <= VnPz;

                p.Add($"  Panel zone demand V_pz = F_pr = {Vpz:F1} kips");
                p.Add($"  Capacity Vn = 0.6*Fyc*dc*twc = 0.6*{input.ColFy}*{input.ColD:F2}*{input.ColTw:F3} = {VnPz:F1} kips");

                if (!panelZoneOK)
                    p.Add($"  FAIL - Consider web doubler plates (Utilization: {Vpz / VnPz:F3})");
                else
                    p.Add($"  OK (Utilization: {Vpz / VnPz:F3})");

                result.PanelZonePassed = panelZoneOK;
                result.PanelZoneRatio = Vpz / VnPz;
                if (!panelZoneOK) allPassed = false;
                p.Add("");

                // ===== SUMMARY =====
                p.Add("================================================================================");
                p.Add("  DESIGN VERIFICATION SUMMARY");
                p.Add("================================================================================");

                p.Add($"Prequalification:      {(result.PrequalificationPassed ? "PASS" : "FAIL")}");
                p.Add($"Bolt diameter check:   {(result.BoltDiameterCheckPassed ? "PASS" : "FAIL")}");
                p.Add($"Bolt count:            {(result.BoltCountCheckPassed ? "PASS" : "FAIL")}");
                p.Add($"Plate yielding:        {(result.PlateYieldingPassed ? "PASS" : "FAIL")}");
                p.Add($"Plate rupture:         {(result.PlateRupturePassed ? "PASS" : "FAIL")}");
                p.Add($"Block shear:           {(result.BlockShearPassed ? "PASS" : "FAIL")}");
                p.Add($"Compression buckling:  {(result.CompressionBucklingPassed ? "PASS" : "FAIL")}");
                p.Add($"Beam shear:            {(result.BeamShearPassed ? "PASS" : "FAIL")}");
                p.Add($"Continuity plates:     {(result.ContinuityPlatesPassed ? "PASS" : "FAIL")}");
                p.Add($"Panel zone:            {(result.PanelZonePassed ? "PASS" : "FAIL")}");
                p.Add("");
                p.Add($"KEY: M_pr={Mpr:F0} kip-in | M_f={Mf:F0} kip-in | F_pr={Fpr:F1} kips | r_n={rn:F1} kips/bolt");
                p.Add("");

                p.Add("================================================================================");
                if (allPassed)
                    p.Add("  ALL CHECKS PASSED");
                else
                    p.Add("  SOME CHECKS FAILED - REVIEW AND ADJUST DESIGN");
                p.Add("================================================================================");

                result.OverallPassed = allPassed;
                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Calculation error: {ex.Message}";
                p.Add($"ERROR: {ex.Message}");
            }

            return result;
        }

        // ====================== HELPER METHODS ======================

        private static double GetBoltArea(double db)
        {
            if (BoltAreas.TryGetValue(db, out double area))
                return area;
            return Math.PI * db * db / 4.0;
        }

        private static double ParseWeight(string designation)
        {
            try
            {
                var parts = designation.ToUpper().Split('X');
                if (parts.Length > 1 && double.TryParse(parts[1], out double w))
                    return w;
            }
            catch { }
            return 999; // Unknown, will trigger check
        }
    }
}
