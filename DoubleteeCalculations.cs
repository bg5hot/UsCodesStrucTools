using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    /// <summary>
    /// Double-Tee Connection Design Verification
    /// AISC 358-16 Chapter 13, Section 13.6 - Design Procedure (23 steps)
    /// </summary>
    public static class DoubleteeCalculations
    {
        // ====================== CONSTANTS ======================
        public const double PHI_D = 1.00;   // Ductile limit states
        public const double PHI_N = 0.90;   // Nonductile limit states
        public const double PHI_V = 0.90;   // Shear

        public const double E = 29000.0;    // Modulus of elasticity (ksi)
        public const double G_MOD = 11200.0; // Shear modulus (ksi)

        // T-stub material defaults (A992 / A913 Gr 50)
        public const double DEFAULT_FY_T = 50.0;
        public const double DEFAULT_FU_T = 65.0;
        public const double RY_T = 1.1;
        public const double RT_T = 1.1;

        // Beam prequalification limits (Section 13.3.1)
        public const double BEAM_MAX_DEPTH = 24.0;
        public const double BEAM_MAX_WEIGHT = 55.0;
        public const double BEAM_MAX_TF = 0.625;
        public const double BEAM_MIN_SPAN_DEPTH = 9.0;

        // Column prequalification limits (Section 13.3.2)
        public const double COL_MAX_DEPTH_SLAB = 36.0;
        public const double COL_MAX_DEPTH_NO_SLAB = 14.0;

        // Bolt properties
        public static readonly Dictionary<string, double> FNT_MAP = new()
        {
            { "A325", 90.0 },
            { "A490", 113.0 },
        };
        public static readonly Dictionary<string, double> FNV_MAP = new()
        {
            { "A325", 54.0 },
            { "A490", 68.0 },
        };

        // Slip coefficient
        public const double MU_CLASS_A = 0.30;
        public const double DELTA_SLIP = 0.0076; // in (Eq. 13.6-39)

        // Standard bolt diameters (in)
        public static readonly double[] STANDARD_BOLT_DIAMETERS =
            { 0.625, 0.75, 0.875, 1.0, 1.125, 1.25, 1.375, 1.5 };

        // ====================== DATA CLASSES ======================

        public class InputParameters
        {
            // Beam properties
            public string BeamDesignation = "";
            public double BeamD = 21.0;
            public double BeamBf = 6.5;
            public double BeamTf = 0.45;
            public double BeamTw = 0.35;
            public double BeamZx = 95.0;
            public double BeamFy = 50.0;
            public double BeamFu = 65.0;
            public double BeamRy = 1.1;
            public double BeamRt = 1.1;

            // Column properties
            public string ColDesignation = "";
            public double ColD = 14.0;
            public double ColBf = 15.0;
            public double ColTf = 1.0;
            public double ColTw = 0.5;
            public double ColZx = 400.0;
            public double ColFy = 50.0;
            public double ColFu = 65.0;
            public double ColRy = 1.1;

            // Design parameters
            public double Span = 300.0;
            public string SystemType = "SMF";
            public int NTb = 4;           // number of tension bolts (4 or 8)
            public double S1 = 3.0;
            public double SVb = 3.0;      // shear bolt spacing (in)
            public double GVb = 3.5;      // shear bolt gage in T-stem (in)
            public double GTb = 5.5;      // tension bolt gage in T-flange (in)
            public string BoltType = "A325";

            // T-Stub geometry (user inputs, 0 = auto-calculate)
            public double TSt = 0.0;      // T-stem thickness (in), 0=auto
            public double TFt = 0.0;      // T-flange thickness (in), 0=auto
            public double BFt = 0.0;      // T-flange width (in), 0=auto

            // Story / slab / column axial
            public bool HasSlab = false;
            public double StoryAbove = 156.0;
            public double StoryBelow = 156.0;
            public double Pu = 0.0;
            public double AsCol = 0.0;

            // Loads
            public double LoadD = 0.0;
            public double LoadL = 0.0;
            public double LoadS = 0.0;
            public double F1 = 0.5;
        }

        public class DesignResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = "";
            public List<string> Process { get; set; } = new();

            public bool OverallPassed { get; set; }

            // Individual checks
            public bool PrequalificationPassed { get; set; }
            public bool BoltDiameterPassed { get; set; }
            public bool BeamShearPassed { get; set; }
            public bool ColumnBeamPassed { get; set; }
            public bool StiffnessPassed { get; set; }
            public bool ShearBoltsPassed { get; set; }
            public bool TStemPassed { get; set; }
            public bool TFlangePassed { get; set; }
            public bool GageRatioPassed { get; set; }
            public bool BearingPassed { get; set; }
            public bool BlockShearPassed { get; set; }
            public bool ColumnFlangePassed { get; set; }
            public bool ColumnWebPassed { get; set; }
            public bool PanelZonePassed { get; set; }
            public bool ContinuityPlatesPassed { get; set; }

            // Key results
            public double Cpr { get; set; }
            public double Mpr { get; set; }
            public double Vh { get; set; }
            public double Sh { get; set; }
            public double Mf { get; set; }
            public double Fpr { get; set; }
            public double Ff { get; set; }
            public double Dvb { get; set; }
            public double Dtb { get; set; }
            public int NVb { get; set; }
            public double TSt { get; set; }
            public double TFt { get; set; }
            public double BFt { get; set; }
            public double Lh { get; set; }

            // Ratios
            public double BeamShearRatio { get; set; }
            public double ShearBoltsRatio { get; set; }
            public double TStemRatio { get; set; }
            public double TFlangeRatio { get; set; }
            public double BearingRatio { get; set; }
            public double BlockShearRatio { get; set; }
            public double PanelZoneRatio { get; set; }
            public double StiffnessRatio { get; set; }
            public double ColumnFlangeRatio { get; set; }
            public double ColumnWebRatio { get; set; }
            public double ColumnBeamRatio { get; set; }
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
                p.Add("  DOUBLE-TEE CONNECTION DESIGN VERIFICATION (AISC 358-16 CHAPTER 13)");
                p.Add("================================================================================");
                p.Add("");
                p.Add("--- INPUT PARAMETERS ---");
                p.Add($"BEAM: {input.BeamDesignation} | d={input.BeamD:F2} bf={input.BeamBf:F2} " +
                      $"tf={input.BeamTf:F3} tw={input.BeamTw:F3} Zx={input.BeamZx:F1}");
                p.Add($"      Fy={input.BeamFy} Fu={input.BeamFu} Ry={input.BeamRy} Rt={input.BeamRt}");
                p.Add($"COLUMN: {input.ColDesignation} | d={input.ColD:F2} bf={input.ColBf:F2} " +
                      $"tf={input.ColTf:F3} tw={input.ColTw:F3} Zx={input.ColZx:F1}");
                p.Add($"        Fy={input.ColFy} Fu={input.ColFu}");
                p.Add($"TENSION BOLTS: {input.NTb} | BOLT TYPE: {input.BoltType}");
                p.Add($"GEOMETRY: S1={input.S1} s_vb={input.SVb} " +
                      $"g_vb={input.GVb} g_tb={input.GTb}");
                p.Add($"SPAN: L={input.Span:F0} in ({input.Span / 12:F1} ft) | {input.SystemType}");
                string slabStr = input.HasSlab ? "with slab" : "no slab";
                p.Add($"STORY: H_above={input.StoryAbove:F0} in | H_below={input.StoryBelow:F0} in | " +
                      $"Pu={input.Pu:F0} kips | {slabStr}");
                double gravity = 1.2 * input.LoadD + input.F1 * input.LoadL + 0.2 * input.LoadS;
                p.Add($"LOADS: D={input.LoadD} L={input.LoadL} S={input.LoadS} | " +
                      $"Gravity={gravity:F2} kips");
                p.Add("");

                // Validate bolt type
                if (!FNT_MAP.ContainsKey(input.BoltType))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Invalid bolt type: {input.BoltType}. Use A325 or A490.";
                    p.Add($"ERROR: {result.ErrorMessage}");
                    return result;
                }

                double Fnt = FNT_MAP[input.BoltType];
                double Fnv = FNV_MAP[input.BoltType];

                // Local variables that accumulate through steps
                double M_pr = 0, V_h = 0, S_h = 0, M_f = 0, F_pr = 0, F_f = 0;
                int n_vb = 0;
                double d_vb = 0, d_tb = 0;
                double t_st = 0, t_ft = 0, b_ft = 0;
                double W_T = 0, W_Whit = 0;
                double phi_rnv = 0;
                double r_bolt_shear = 0, r_beam_bearing = 0;
                double a_save = 0, b_save = 0, a_prime_save = 0, b_prime_save = 0;
                double p_save = 0, phi_rnt_save = 0;
                double L_h = 0;

                // ===== STEP 0: PREQUALIFICATION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  PREQUALIFICATION LIMITS (SECTION 13.3)");
                p.Add("--------------------------------------------------------------------------------");

                bool prequal = true;
                double beamWeight = ParseWeight(input.BeamDesignation);

                // Beam depth <= W24
                p.Add($"  Beam depth: d = {input.BeamD:F2} in <= {BEAM_MAX_DEPTH:F0}: " +
                      (input.BeamD <= BEAM_MAX_DEPTH ? "OK" : "FAIL"));
                if (input.BeamD > BEAM_MAX_DEPTH) prequal = false;

                // Beam weight <= 55 plf
                p.Add($"  Beam weight: {beamWeight:F0} plf <= {BEAM_MAX_WEIGHT:F0}: " +
                      (beamWeight <= BEAM_MAX_WEIGHT ? "OK" : "FAIL"));
                if (beamWeight > BEAM_MAX_WEIGHT) prequal = false;

                // Beam flange thickness <= 5/8 in
                p.Add($"  Beam tf: {input.BeamTf:F3} in <= {BEAM_MAX_TF:F3}: " +
                      (input.BeamTf <= BEAM_MAX_TF ? "OK" : "FAIL"));
                if (input.BeamTf > BEAM_MAX_TF) prequal = false;

                // Span/depth >= 9
                double sd = input.Span / input.BeamD;
                p.Add($"  Span/depth: L/d = {sd:F1} >= {BEAM_MIN_SPAN_DEPTH:F0}: " +
                      (sd >= BEAM_MIN_SPAN_DEPTH ? "OK" : "FAIL"));
                if (sd < BEAM_MIN_SPAN_DEPTH) prequal = false;

                // Column depth
                double colMax = input.HasSlab ? COL_MAX_DEPTH_SLAB : COL_MAX_DEPTH_NO_SLAB;
                p.Add($"  Column depth: d = {input.ColD:F2} in <= {colMax:F0} ({slabStr}): " +
                      (input.ColD <= colMax ? "OK" : "FAIL"));
                if (input.ColD > colMax) prequal = false;

                result.PrequalificationPassed = prequal;
                if (!prequal) allPassed = false;
                p.Add("");

                // ===== STEP 1: M_pr =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 1: PROBABLE MAXIMUM MOMENT (EQ. 13.6-1)");
                p.Add("--------------------------------------------------------------------------------");

                double Cpr = Math.Min((input.BeamFy + input.BeamFu) / (2 * input.BeamFy), 1.2);
                M_pr = Cpr * input.BeamRy * input.BeamFy * input.BeamZx;

                p.Add($"  C_pr = min((Fy+Fu)/(2*Fy), 1.2)");
                p.Add($"  C_pr = min(({input.BeamFy}+{input.BeamFu})/(2*{input.BeamFy}), 1.2) = {Cpr:F3}");
                p.Add($"  M_pr = C_pr * Ry * Fy * Zx  (Eq. 13.6-1)");
                p.Add($"  M_pr = {Cpr:F3} * {input.BeamRy} * {input.BeamFy} * {input.BeamZx:F1}");
                p.Add($"  M_pr = {M_pr:F0} kip-in ({M_pr / 12:F1} kip-ft)");
                p.Add("");

                result.Cpr = Cpr;
                result.Mpr = M_pr;

                // ===== STEP 2: SHEAR BOLT DIAMETER =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 2: MAXIMUM SHEAR BOLT DIAMETER (EQ. 13.6-3)");
                p.Add("--------------------------------------------------------------------------------");

                // Eq. 13.6-3
                double dvbMax = (input.BeamZx / (2 * input.BeamTf * (input.BeamD - input.BeamTf))) *
                                (1 - input.BeamRy * input.BeamFy / (input.BeamRt * input.BeamFu)) - 0.125;

                p.Add($"  d_vb <= (Zx/(2*tf*(d-tf)))*(1 - Ry*Fy/(Rt*Fu)) - 1/8");
                p.Add($"  d_vb <= ({input.BeamZx:F1}/(2*{input.BeamTf:F3}*{input.BeamD - input.BeamTf:F3}))" +
                      $"*(1 - {input.BeamRy}*{input.BeamFy}/({input.BeamRt}*{input.BeamFu})) - 0.125");
                p.Add($"  d_vb <= {dvbMax:F3} in");

                // Select largest bolt diameter that fits
                d_vb = 0.625; // minimum fallback
                for (int i = STANDARD_BOLT_DIAMETERS.Length - 1; i >= 0; i--)
                {
                    if (STANDARD_BOLT_DIAMETERS[i] <= dvbMax)
                    {
                        d_vb = STANDARD_BOLT_DIAMETERS[i];
                        break;
                    }
                }

                bool boltDiaOK = d_vb <= dvbMax;
                p.Add($"  Use d_vb = {d_vb:F3} in: " +
                      (boltDiaOK ? "OK" : "WARNING - may need smaller bolt"));

                result.BoltDiameterPassed = boltDiaOK;
                if (!boltDiaOK) allPassed = false;
                p.Add("");

                // ===== STEP 3: BOLT STRENGTH PER BOLT =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 3: DESIGN SHEAR STRENGTH PER BOLT (EQ. 13.6-4)");
                p.Add("--------------------------------------------------------------------------------");

                double A_vb = Math.PI / 4 * d_vb * d_vb;
                double t_st_est = 0.5; // initial estimate

                // Eq. 13.6-4: three-term minimum
                r_bolt_shear = PHI_N * Fnv * A_vb;
                r_beam_bearing = PHI_D * 2.4 * d_vb * input.BeamTf * input.BeamFu;
                double r_stem_bearing_est = PHI_D * 2.4 * d_vb * t_st_est * DEFAULT_FU_T;

                phi_rnv = Math.Min(r_bolt_shear, Math.Min(r_beam_bearing, r_stem_bearing_est));

                p.Add($"  phi*r_nv = min(bolt shear, beam bearing, T-stem bearing)");
                p.Add($"    Bolt shear: {PHI_N}*{Fnv}*{A_vb:F3} = {r_bolt_shear:F1} kips");
                p.Add($"    Beam bearing: {PHI_D}*2.4*{d_vb:F3}*{input.BeamTf:F3}" +
                      $"*{input.BeamFu} = {r_beam_bearing:F1} kips");
                p.Add($"    T-stem bearing (est): {r_stem_bearing_est:F1} kips");
                p.Add($"  phi*r_nv = {phi_rnv:F1} kips/bolt");
                p.Add("");

                // ===== STEP 4: NUMBER OF SHEAR BOLTS =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 4: NUMBER OF SHEAR BOLTS (EQ. 13.6-5)");
                p.Add("--------------------------------------------------------------------------------");

                double nVbCalc = 1.25 * M_pr / (input.BeamD * phi_rnv);
                n_vb = (int)Math.Ceiling(nVbCalc);
                if (n_vb % 2 != 0) n_vb += 1;

                p.Add($"  n_vb >= 1.25*M_pr / (d * phi*r_nv)  (Eq. 13.6-5)");
                p.Add($"  n_vb >= 1.25*{M_pr:F0} / ({input.BeamD:F2} * {phi_rnv:F1})");
                p.Add($"  n_vb >= {nVbCalc:F1}, use n_vb = {n_vb} (even integer)");
                p.Add("");

                result.NVb = n_vb;

                // ===== STEP 5: PLASTIC HINGE LOCATION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 5: PLASTIC HINGE LOCATION (EQ. 13.6-6, 13.6-7)");
                p.Add("--------------------------------------------------------------------------------");

                double L_vb_step5 = input.SVb * (n_vb / 2.0 - 1);
                S_h = input.S1 + L_vb_step5;

                p.Add($"  L_vb = s_vb*(n_vb/2 - 1) = {input.SVb}*({n_vb}/2 - 1)" +
                      $" = {L_vb_step5:F2} in  (Eq. 13.6-7)");
                p.Add($"  S_h = S1 + L_vb = {input.S1} + {L_vb_step5:F2}" +
                      $" = {S_h:F2} in  (Eq. 13.6-6)");
                p.Add("");

                result.Sh = S_h;

                // ===== STEP 6: SHEAR AT PLASTIC HINGE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 6: SHEAR AT PLASTIC HINGE");
                p.Add("--------------------------------------------------------------------------------");

                double dc = input.ColD;
                L_h = input.Span - 2 * (dc / 2 + S_h);
                V_h = 2 * M_pr / L_h + gravity / 2;

                p.Add($"  L_h = L - 2*(dc/2 + S_h) = {input.Span:F0} - " +
                      $"2*({dc / 2:F2}+{S_h:F2}) = {L_h:F1} in");
                p.Add($"  V_h = 2*M_pr/L_h + gravity/2");
                p.Add($"  V_h = 2*{M_pr:F0}/{L_h:F1} + {gravity:F2}/2" +
                      $" = {V_h:F1} kips");
                p.Add("");

                result.Vh = V_h;
                result.Lh = L_h;

                // ===== BEAM SHEAR STRENGTH =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  BEAM SHEAR STRENGTH CHECK (AISC 360 G2.1)");
                p.Add("--------------------------------------------------------------------------------");

                double Aw = input.BeamD * input.BeamTw;
                double Cv = 1.0;
                double V_n = 0.6 * input.BeamFy * Aw * Cv;
                double phi_Vn = PHI_V * V_n;
                bool beamShearOK = V_h <= phi_Vn;

                p.Add($"  V_n = 0.6*Fy*d*tw = 0.6*{input.BeamFy}*{input.BeamD:F2}*{input.BeamTw:F3}" +
                      $" = {V_n:F1} kips");
                p.Add($"  phi*V_n = {phi_Vn:F1} kips >= V_h = {V_h:F1}: " +
                      (beamShearOK ? "OK" : "FAIL"));

                result.BeamShearPassed = beamShearOK;
                result.BeamShearRatio = V_h / phi_Vn;
                if (!beamShearOK) allPassed = false;
                p.Add("");

                // ===== STEP 7: MOMENT AT COLUMN FACE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 7: MOMENT AT COLUMN FACE (EQ. 13.6-10)");
                p.Add("--------------------------------------------------------------------------------");

                M_f = M_pr + V_h * S_h;

                p.Add($"  M_f = M_pr + V_h * S_h  (Eq. 13.6-10)");
                p.Add($"  M_f = {M_pr:F0} + {V_h:F1} * {S_h:F2}" +
                      $" = {M_f:F0} kip-in ({M_f / 12:F1} kip-ft)");
                p.Add("");

                result.Mf = M_f;

                // ===== STEP 7a: COLUMN-BEAM RELATIONSHIP =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  COLUMN-BEAM RELATIONSHIP (AISC 341 E3.6c)");
                p.Add("--------------------------------------------------------------------------------");

                bool colBeamOK = true;
                double colBeamRatio = 999;

                if (input.SystemType == "IMF")
                {
                    p.Add("  IMF: Column-beam ratio per AISC Seismic Provisions");
                    p.Add("  (Strong-column/weak-beam may not be required for IMF)");
                    colBeamOK = true;
                }
                else
                {
                    double M_uv = V_h * (input.ColD / 2);
                    double M_pb_star = M_pr + M_uv;
                    int n_beams = 2;
                    double Sum_Mpb = n_beams * M_pb_star;

                    double AsCol = input.AsCol;
                    if (AsCol <= 0)
                        AsCol = input.ColBf * input.ColTf * 2 + (input.ColD - 2 * input.ColTf) * input.ColTw;

                    double denom = AsCol * input.ColFy;
                    double M_pc;
                    if (denom > 0 && input.Pu > 0)
                        M_pc = input.ColZx * input.ColFy * Math.Max(0, 1 - input.Pu / denom);
                    else
                        M_pc = input.ColZx * input.ColFy;

                    double Sum_Mpc = 2 * M_pc;

                    colBeamRatio = Sum_Mpb > 0 ? Sum_Mpc / Sum_Mpb : 999;
                    colBeamOK = colBeamRatio >= 1.0;

                    p.Add($"  Strong-Column / Weak-Beam (SMF):");
                    p.Add($"  M_uv = V_h * (d_c/2) = {V_h:F1} * {input.ColD / 2:F2} = {M_uv:F0} kip-in");
                    p.Add($"  M_pb* = M_pr + M_uv = {M_pr:F0} + {M_uv:F0} = {M_pb_star:F0} kip-in");
                    p.Add($"  Sum M_pb* = {n_beams} * {M_pb_star:F0} = {Sum_Mpb:F0} kip-in");
                    p.Add($"  M_pc = Zx_c * Fy_c * (1 - Pu/(As*Fy))");
                    p.Add($"  M_pc = {input.ColZx:F1} * {input.ColFy} * (1 - {input.Pu:F0}/{denom:F0})" +
                          $" = {M_pc:F0} kip-in");
                    p.Add($"  Sum M_pc = 2 * {M_pc:F0} = {Sum_Mpc:F0} kip-in");
                    p.Add($"  Ratio = {Sum_Mpc:F0} / {Sum_Mpb:F0} = {colBeamRatio:F3}");
                    if (!colBeamOK)
                        p.Add($"  FAIL - Increase column size");
                    else
                        p.Add($"  OK");
                    p.Add($"  Note: Simplified (same column above/below, {n_beams} beams)");
                }

                result.ColumnBeamPassed = colBeamOK;
                result.ColumnBeamRatio = colBeamRatio;
                if (!colBeamOK) allPassed = false;
                p.Add("");

                // ===== STEP 8: T-STUB FORCE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 8: PROBABLE T-STUB FORCE (EQ. 13.6-11)");
                p.Add("--------------------------------------------------------------------------------");

                F_pr = M_f / (1.05 * input.BeamD);

                p.Add($"  F_pr = M_f / (1.05*d)  (Eq. 13.6-11)");
                p.Add($"  F_pr = {M_f:F0} / (1.05*{input.BeamD:F2})" +
                      $" = {F_pr:F1} kips");
                p.Add("");

                result.Fpr = F_pr;

                // ===== STEP 9: T-STEM SIZE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 9: T-STEM SIZE (EQ. 13.6-12 TO 13.6-15)");
                p.Add("--------------------------------------------------------------------------------");

                // Eq. 13.6-12: Whitmore width
                double L_vb_step9 = input.SVb * (n_vb / 2.0 - 1);
                W_Whit = 2 * L_vb_step9 * Math.Tan(30.0 * Math.PI / 180.0) + input.GVb;
                p.Add($"  W_Whit = 2*L_vb*tan30 + g_vb  (Eq. 13.6-12)");
                p.Add($"  W_Whit = 2*{L_vb_step9:F2}*0.577 + {input.GVb} = {W_Whit:F2} in");

                // W_T: T-stub width parallel to column flange (estimate)
                W_T = Math.Min(input.BeamBf, input.ColBf);
                double W_eff = Math.Min(W_T, W_Whit);

                // Eq. 13.6-13: Stem thickness for yielding
                double t_st_yield = F_pr / (W_eff * PHI_D * DEFAULT_FY_T);

                // Eq. 13.6-14: Stem thickness for fracture
                double d_vht = d_vb + 1.0 / 16.0;
                double t_st_fracture = F_pr / (PHI_N * DEFAULT_FU_T *
                    (W_eff - 2 * (d_vht + 1.0 / 16.0)));

                // Eq. 13.6-15: Compression buckling
                double t_ft_est = 1.0; // estimate
                double t_st_buckling = (input.S1 - t_ft_est) / 9.60;

                double t_st_req = Math.Max(t_st_yield, Math.Max(t_st_fracture, t_st_buckling));
                // Round up to nearest 1/8 in, minimum 1/4 in
                t_st = Math.Max(0.25, Math.Ceiling(t_st_req * 8) / 8);

                // If user specified a T-stem thickness, use it
                if (input.TSt > 0) t_st = input.TSt;

                p.Add($"  W_T (estimate) = {W_T:F2} in");
                p.Add($"  W_eff = min(W_T, W_Whit) = {W_eff:F2} in");
                p.Add($"  t_st (yielding) = {t_st_yield:F3} in  (Eq. 13.6-13)");
                p.Add($"  t_st (fracture) = {t_st_fracture:F3} in  (Eq. 13.6-14)");
                p.Add($"  t_st (buckling) = {t_st_buckling:F3} in  (Eq. 13.6-15)");
                p.Add($"  t_st,req = {t_st_req:F3} in, use t_st = {t_st:F3} in");

                // Recompute phi_rnv with actual t_st for T-stem bearing
                double r_stem_bearing = PHI_D * 2.4 * d_vb * t_st * DEFAULT_FU_T;
                phi_rnv = Math.Min(r_bolt_shear, Math.Min(r_beam_bearing, r_stem_bearing));
                p.Add($"  Updated phi*r_nv = {phi_rnv:F1} kips/bolt");
                p.Add("");

                result.TSt = t_st;

                // ===== STEP 10: TENSION BOLT SIZE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 10: TENSION BOLT SIZE (EQ. 13.6-16)");
                p.Add("--------------------------------------------------------------------------------");

                double dtbReq = Math.Sqrt(4 * F_pr / (input.NTb * PHI_N * Math.PI * Fnt));
                d_tb = 0.875; // default 7/8 in
                for (int i = 0; i < STANDARD_BOLT_DIAMETERS.Length; i++)
                {
                    if (STANDARD_BOLT_DIAMETERS[i] >= dtbReq)
                    {
                        d_tb = STANDARD_BOLT_DIAMETERS[i];
                        break;
                    }
                }

                p.Add($"  d_tb >= sqrt(4*F_pr / (n_tb*phi_n*pi*Fnt))  (Eq. 13.6-16)");
                p.Add($"  d_tb >= sqrt(4*{F_pr:F1} / ({input.NTb}*{PHI_N}*pi*{Fnt}))");
                p.Add($"  d_tb >= {dtbReq:F3} in, use d_tb = {d_tb:F3} in");
                p.Add("");

                result.Dtb = d_tb;

                // ===== STEP 11: T-FLANGE CONFIGURATION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 11: T-FLANGE CONFIGURATION (EQ. 13.6-17 TO 13.6-27)");
                p.Add("--------------------------------------------------------------------------------");

                // b = distance between effective T-stem and bolt line (Eq. 13.6-53)
                double k1 = 0.75;
                double t_st_eff = k1 + t_st / 2.0; // Eq. 13.6-54
                double b_val = 0.5 * (input.GTb - t_st_eff); // Eq. 13.6-53

                // a = 1.5 * d_tb (initial), limited to 1.25*b (Eq. 13.6-18)
                double a_val = 1.5 * d_tb;
                double a_limit = 1.25 * b_val;

                // b_ft (Eq. 13.6-17)
                double b_ft_req = input.GTb + 2 * a_val;

                // p = tributary width per bolt (Eq. 13.6-22)
                double p_val = 2 * W_T / input.NTb;

                // a', b' (Eqs. 13.6-23, 13.6-24)
                double a_prime = a_val + 0.5 * d_tb;
                double b_prime = b_val - 0.5 * d_tb;

                // Bolt design strength (Eq. 13.6-19)
                double A_tb = Math.PI / 4 * d_tb * d_tb;
                double phi_rnt = PHI_N * A_tb * Fnt;

                // T_req (Eq. 13.6-20)
                double T_req = F_pr / input.NTb;

                // t_ft from mixed-mode (Eq. 13.6-21)
                double radical_21 = T_req * (a_prime + b_prime) - phi_rnt * a_prime;
                double t_ft_21;
                if (radical_21 > 0)
                {
                    t_ft_21 = 2 * Math.Sqrt(radical_21 / (PHI_D * DEFAULT_FY_T * p_val));
                }
                else
                {
                    double dtht_21 = d_tb + 1.0 / 16.0;
                    double delta_21 = 1 - dtht_21 / p_val;
                    t_ft_21 = 2 * Math.Sqrt(
                        phi_rnt * a_prime * b_prime /
                        (PHI_D * DEFAULT_FY_T * p_val * (a_prime + delta_21 * (a_prime + b_prime))));
                }

                // No-prying thickness (Eq. 13.6-27)
                double t_ft_crit = Math.Sqrt(4 * phi_rnt * b_prime / (PHI_D * DEFAULT_FY_T * p_val));

                t_ft = Math.Max(t_ft_21, t_ft_crit);
                t_ft = Math.Max(t_ft, Math.Ceiling(t_ft * 8) / 8); // round up to 1/8

                b_ft = Math.Max(b_ft_req, input.GTb + 2 * 1.5 * d_tb);
                b_ft = Math.Ceiling(b_ft * 4) / 4; // round up to 1/4 in

                // If user specified values, use them
                if (input.TFt > 0) t_ft = input.TFt;
                if (input.BFt > 0) b_ft = input.BFt;

                // Save for later steps
                a_save = a_val;
                b_save = b_val;
                a_prime_save = a_prime;
                b_prime_save = b_prime;
                p_save = p_val;
                phi_rnt_save = phi_rnt;

                p.Add($"  b = (g_tb - t_st_eff)/2 = ({input.GTb} - {t_st_eff:F3})/2" +
                      $" = {b_val:F3} in  (Eq. 13.6-53)");
                p.Add($"  a = 1.5*d_tb = {a_val:F3} in (limit 1.25*b = {a_limit:F3})");
                p.Add($"  p = 2*W_T/n_tb = {p_val:F2} in  (Eq. 13.6-22)");
                p.Add($"  phi*r_nt = {phi_rnt:F1} kips/bolt  (Eq. 13.6-19)");
                p.Add($"  T_req = {T_req:F1} kips/bolt  (Eq. 13.6-20)");
                p.Add($"  t_ft (mixed-mode) = {t_ft_21:F3} in  (Eq. 13.6-21)");
                p.Add($"  t_ft (no prying) = {t_ft_crit:F3} in  (Eq. 13.6-27)");
                p.Add($"  Use t_ft = {t_ft:F3} in");
                p.Add($"  b_ft = {b_ft:F2} in");

                // Section 13.5.4(5): g_tb/t_ft <= 7.0
                double gageRatio = input.GTb / t_ft;
                bool gageOK = gageRatio <= 7.0;
                p.Add($"  g_tb/t_ft = {input.GTb}/{t_ft:F3} = {gageRatio:F2} " +
                      $"<= 7.0: " + (gageOK ? "OK" : "FAIL") + "  (Section 13.5.4(5))");

                result.GageRatioPassed = gageOK;
                if (!gageOK) allPassed = false;
                p.Add("");

                // ===== STEP 12: SELECT T-STUB FROM W-SHAPE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 12: SELECT T-STUB FROM W-SHAPE");
                p.Add("--------------------------------------------------------------------------------");
                p.Add($"  Required: t_st >= {t_st:F3} in, t_ft >= {t_ft:F3} in, b_ft >= {b_ft:F2} in");
                double minDepth = input.S1 + input.SVb * (n_vb / 2.0 - 1);
                p.Add($"  Minimum depth >= S1 + L_vb = {input.S1:F1} + " +
                      $"{input.SVb * (n_vb / 2.0 - 1):F1} = {minDepth:F1} in");
                p.Add($"  Note: Select a W-shape with tw >= {t_st:F3}, " +
                      $"tf >= {t_ft:F3}, bf >= {b_ft:F2}");
                p.Add($"  T-stub cut from W-shape (ASTM A992 or A913 Gr 50)");
                p.Add("");

                // ===== STEP 13: FR CONNECTION STIFFNESS CHECK =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 13: FR CONNECTION STIFFNESS CHECK (EQ. 13.6-28)");
                p.Add("--------------------------------------------------------------------------------");

                // Approximate I_beam from Zx
                double I_beam = input.BeamZx * (input.BeamD / 2) * 0.9;
                double L_o = input.Span;

                // Eq. 13.6-28: K_i >= 18*E*I_beam / L_o
                double K_req = 18 * E * I_beam / L_o;

                // Eq. 13.6-32: K_flange
                double I_ft = p_save * Math.Pow(t_ft, 3) / 12; // Eq. 13.6-36
                double beta_a = 1 + 12 * E * I_ft / (G_MOD * p_save * t_ft * Math.Pow(a_prime_save, 2));
                double beta_b = 1 + 12 * E * I_ft / (G_MOD * p_save * t_ft * Math.Pow(b_prime_save, 2));

                double denomK = Math.Pow(b_prime_save, 3) * beta_b *
                    (4 * a_prime_save * beta_a + 3 * b_prime_save * beta_b);
                double K_flange;
                if (Math.Abs(denomK) > 1e-10)
                {
                    K_flange = (12 * input.NTb * E * I_ft *
                        (a_prime_save * beta_a + 3 * b_prime_save * beta_b)) / denomK;
                }
                else
                {
                    K_flange = 1e10; // very stiff fallback
                }

                // Eq. 13.6-33: K_stem
                double b_fb = input.BeamBf;
                double ratio_W_bf = W_T - b_fb;
                double K_stem;
                if (Math.Abs(ratio_W_bf) > 1e-4 && W_T > 0)
                {
                    double logTerm = Math.Log(b_fb / W_T);
                    double stemDenom = input.S1 * (ratio_W_bf + b_fb * logTerm);
                    K_stem = Math.Abs(stemDenom) > 1e-10
                        ? t_st * E * Math.Pow(ratio_W_bf, 2) / stemDenom
                        : t_st * W_T * E / input.S1;
                }
                else
                {
                    K_stem = t_st * W_T * E / input.S1;
                }

                // Eq. 13.6-35: P_slip
                double A_vb_slip = Math.PI / 4 * d_vb * d_vb;
                double alpha = input.BoltType == "A325" ? 1.0 : 0.88;
                double P_slip = n_vb * alpha * (0.70 * Fnt * A_vb_slip) * MU_CLASS_A;

                // Eq. 13.6-34: K_slip
                double K_slip = P_slip / DELTA_SLIP;

                // Eq. 13.6-30: K_ten
                double K_ten = 1.0 / (1.0 / K_flange + 1.0 / K_stem + 1.0 / K_slip);
                // Eq. 13.6-31: K_comp
                double K_comp = 1.0 / (1.0 / K_stem + 1.0 / K_slip);
                // Eq. 13.6-29: K_i
                double K_i = Math.Pow(input.BeamD, 2) * K_ten * K_comp / (K_ten + K_comp);

                bool stiffOK = K_i >= K_req;
                double stiffRatio = K_req > 0 ? K_i / K_req : 999;

                p.Add($"  K_req = 18*E*I_beam/L = 18*{E:F0}*{I_beam:F0}/{L_o:F0}" +
                      $" = {K_req:F0} kip-in/rad  (Eq. 13.6-28)");
                p.Add($"  K_flange = {K_flange:F0} kip/in  (Eq. 13.6-32)");
                p.Add($"  K_stem = {K_stem:F0} kip/in  (Eq. 13.6-33)");
                p.Add($"  K_slip = {K_slip:F0} kip/in  (Eq. 13.6-34)");
                p.Add($"  K_ten = {K_ten:F0} kip/in  (Eq. 13.6-30)");
                p.Add($"  K_comp = {K_comp:F0} kip/in  (Eq. 13.6-31)");
                p.Add($"  K_i = {K_i:F0} kip-in/rad  (Eq. 13.6-29)");
                p.Add($"  K_i/K_req = {stiffRatio:F3} >= 1.0: " +
                      (stiffOK ? "OK (FR)" : "FAIL (PR - not prequalified)"));

                result.StiffnessPassed = stiffOK;
                result.StiffnessRatio = stiffRatio;
                if (!stiffOK) allPassed = false;
                p.Add("");

                // ===== STEP 14: ACTUAL FLANGE FORCE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 14: ACTUAL FLANGE FORCE (EQ. 13.6-40)");
                p.Add("--------------------------------------------------------------------------------");

                F_f = M_f / (input.BeamD + t_st);

                p.Add($"  F_f = M_f / (d + t_st)  (Eq. 13.6-40)");
                p.Add($"  F_f = {M_f:F0} / ({input.BeamD:F2} + {t_st:F3})" +
                      $" = {F_f:F1} kips");
                p.Add("");

                result.Ff = F_f;

                // ===== STEP 15: BACK-CHECK SHEAR BOLTS =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 15: BACK-CHECK SHEAR BOLTS (EQ. 13.6-41)");
                p.Add("--------------------------------------------------------------------------------");

                double phi_Rn_shear = n_vb * phi_rnv;
                bool shearBoltsOK = phi_Rn_shear >= F_f;

                p.Add($"  phi*R_n = {n_vb} * {phi_rnv:F1} = {phi_Rn_shear:F1} kips");
                p.Add($"  F_f = {F_f:F1} kips");
                p.Add($"  " + (shearBoltsOK ? "OK" : "FAIL") +
                      $" (Utilization: {F_f / phi_Rn_shear:F3})");

                result.ShearBoltsPassed = shearBoltsOK;
                result.ShearBoltsRatio = F_f / phi_Rn_shear;
                if (!shearBoltsOK) allPassed = false;
                p.Add("");

                // ===== STEP 16: BACK-CHECK T-STEM =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 16: BACK-CHECK T-STEM (EQ. 13.6-42 TO 13.6-45)");
                p.Add("--------------------------------------------------------------------------------");

                W_eff = Math.Min(W_T, W_Whit);
                d_vht = d_vb + 1.0 / 16.0;

                // Gross section yielding (Eq. 13.6-42)
                double phi_Rn_yield = PHI_D * DEFAULT_FY_T * W_eff * t_st;

                // Net section fracture (Eq. 13.6-43)
                double phi_Rn_fracture = PHI_N * DEFAULT_FU_T * (W_eff - 2 * (d_vht + 1.0 / 16.0)) * t_st;

                // Flexural buckling (Eq. 13.6-44)
                double KLr = 2.60 * (input.S1 - t_ft) / t_st;
                double phi_Rn_buckling;

                if (KLr <= 25)
                {
                    phi_Rn_buckling = PHI_D * DEFAULT_FY_T * W_eff * t_st;
                }
                else
                {
                    double Fe = Math.PI * Math.PI * E / (KLr * KLr);
                    double Fcr;
                    if (Fe >= 0.44 * DEFAULT_FY_T)
                        Fcr = Math.Pow(0.658, DEFAULT_FY_T / Fe) * DEFAULT_FY_T;
                    else
                        Fcr = 0.877 * Fe;
                    phi_Rn_buckling = PHI_N * Fcr * W_eff * t_st;
                }

                double phi_Rn_stem_min = Math.Min(phi_Rn_yield, Math.Min(phi_Rn_fracture, phi_Rn_buckling));
                bool tStemOK = phi_Rn_stem_min >= F_f;

                p.Add($"  W_eff = {W_eff:F2} in");
                p.Add($"  Yielding: phi*R_n = {phi_Rn_yield:F1} kips  (Eq. 13.6-42)");
                p.Add($"  Fracture: phi*R_n = {phi_Rn_fracture:F1} kips  (Eq. 13.6-43)");
                p.Add($"  KL/r = {KLr:F1}  (Eq. 13.6-44)");
                p.Add($"  Buckling: phi*R_n = {phi_Rn_buckling:F1} kips");
                p.Add($"  Governing: phi*R_n = {phi_Rn_stem_min:F1} kips >= F_f = {F_f:F1}: " +
                      (tStemOK ? "OK" : "FAIL"));

                result.TStemPassed = tStemOK;
                result.TStemRatio = F_f / phi_Rn_stem_min;
                if (!tStemOK) allPassed = false;
                p.Add("");

                // ===== STEP 17: BACK-CHECK T-FLANGE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 17: BACK-CHECK T-FLANGE (EQ. 13.6-46 TO 13.6-54)");
                p.Add("--------------------------------------------------------------------------------");

                // Recompute a, b from actual b_ft per Eqs. 13.6-50 to 13.6-54
                double k1_17 = 0.75;
                double t_st_eff_17 = k1_17 + t_st / 2.0;
                double b_17 = 0.5 * (input.GTb - t_st_eff_17); // Eq. 13.6-53
                double a_17 = 0.5 * (b_ft - input.GTb);        // Eq. 13.6-51
                a_17 = Math.Min(a_17, 1.25 * b_17);            // Eq. 13.6-52
                double a_prime_17 = a_17 + 0.5 * d_tb;          // Eq. 13.6-50
                double b_prime_17 = b_17 - 0.5 * d_tb;

                double dtht_17 = d_tb + 1.0 / 16.0;
                double delta_17 = 1 - dtht_17 / p_save;

                // Eq. 13.6-47: Plastic flange mechanism
                double phi_T1 = ((1 + delta_17) / (4 * b_prime_17)) * p_save *
                    PHI_D * DEFAULT_FY_T * t_ft * t_ft;

                // Eq. 13.6-48: Mixed-mode failure
                double phi_T2 = (phi_rnt_save * a_prime_17 / (a_prime_17 + b_prime_17)) +
                    p_save * PHI_D * DEFAULT_FY_T * t_ft * t_ft /
                    (4 * (a_prime_17 + b_prime_17));

                // Eq. 13.6-49: Bolt fracture without prying
                double phi_T3 = phi_rnt_save;

                double phi_T_min = Math.Min(phi_T1, Math.Min(phi_T2, phi_T3));
                double phi_Rn_tflange = input.NTb * phi_T_min;
                bool tFlangeOK = phi_Rn_tflange >= F_f;

                p.Add($"  a = {a_17:F3} in | b = {b_17:F3} in | a' = {a_prime_17:F3} | b' = {b_prime_17:F3}");
                p.Add($"  phi*T1 (plastic mechanism) = {phi_T1:F1} kips/bolt  (Eq. 13.6-47)");
                p.Add($"  phi*T2 (mixed-mode) = {phi_T2:F1} kips/bolt  (Eq. 13.6-48)");
                p.Add($"  phi*T3 (bolt fracture) = {phi_T3:F1} kips/bolt  (Eq. 13.6-49)");
                p.Add($"  Governing: phi*T = {phi_T_min:F1} kips/bolt");
                p.Add($"  phi*R_n = {input.NTb} * {phi_T_min:F1} = {phi_Rn_tflange:F1} kips");
                p.Add($"  phi*R_n = {phi_Rn_tflange:F1} >= F_f = {F_f:F1}: " +
                      (tFlangeOK ? "OK" : "FAIL"));

                result.TFlangePassed = tFlangeOK;
                result.TFlangeRatio = F_f / phi_Rn_tflange;
                if (!tFlangeOK) allPassed = false;
                p.Add("");

                // ===== STEP 18: BEARING AND TEAR-OUT =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 18: BEARING AND TEAR-OUT (AISC 360 CH. J)");
                p.Add("--------------------------------------------------------------------------------");

                // Beam flange bearing (ductile, phi_d = 1.0)
                double r_bf = PHI_D * 2.4 * d_vb * input.BeamTf * input.BeamFu;
                double r_bf_total = n_vb * r_bf;

                // T-stem bearing
                double r_ts = PHI_D * 2.4 * d_vb * t_st * DEFAULT_FU_T;
                double r_ts_total = n_vb * r_ts;

                // Tear-out check assumes minimum edge distance = 1.5*db
                double Lc_min = 1.5 * d_vb;
                double r_to_bf = PHI_D * 1.2 * Lc_min * input.BeamTf * input.BeamFu;
                double r_to_ts = PHI_D * 1.2 * Lc_min * t_st * DEFAULT_FU_T;

                double r_min_per_bolt = Math.Min(r_bf, Math.Min(r_ts, Math.Min(r_to_bf, r_to_ts)));
                double phi_Rn_bearing = n_vb * r_min_per_bolt;
                bool bearingOK = phi_Rn_bearing >= F_f;

                p.Add($"  Beam flange bearing: {r_bf:F1} kips/bolt");
                p.Add($"  T-stem bearing: {r_ts:F1} kips/bolt");
                p.Add($"  Tear-out (Lc={Lc_min:F2}): beam={r_to_bf:F1}, T-stem={r_to_ts:F1} kips/bolt");
                p.Add($"  Governing: {r_min_per_bolt:F1} kips/bolt");
                p.Add($"  phi*R_n = {n_vb}*{r_min_per_bolt:F1} = {phi_Rn_bearing:F1} kips");
                p.Add($"  F_f = {F_f:F1} kips: " + (bearingOK ? "OK" : "FAIL"));

                result.BearingPassed = bearingOK;
                result.BearingRatio = F_f / phi_Rn_bearing;
                if (!bearingOK) allPassed = false;
                p.Add("");

                // ===== STEP 19: BLOCK SHEAR =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 19: BLOCK SHEAR (AISC 360 CH. J)");
                p.Add("--------------------------------------------------------------------------------");

                d_vht = d_vb + 1.0 / 16.0;
                int n_rows = n_vb / 2;

                // Gross and net shear areas (T-stem)
                double L_gv = (n_rows - 1) * input.SVb;
                double Agv = 2 * L_gv * t_st;
                double Anv = Agv - 2 * (n_rows - 1) * (d_vht + 1.0 / 16.0) * t_st;

                // Gross and net tension area (T-stem)
                double Agt = input.GVb * t_st;
                double Ant = (input.GVb - (d_vht + 1.0 / 16.0)) * t_st;

                double Ubs = 0.5;
                double phi_Rn_bs1 = PHI_D * (0.6 * DEFAULT_FU_T * Anv + Ubs * DEFAULT_FU_T * Ant);
                double phi_Rn_bs2 = PHI_D * (0.6 * DEFAULT_FU_T * Anv + DEFAULT_FY_T * Agv);
                double phi_Rn_bs = Math.Min(phi_Rn_bs1, phi_Rn_bs2);

                bool blockShearOK = phi_Rn_bs >= F_f;
                p.Add($"  T-stem block shear:");
                p.Add($"    Agv = {Agv:F2} in^2, Anv = {Anv:F2} in^2");
                p.Add($"    Agt = {Agt:F2} in^2, Ant = {Ant:F2} in^2");
                p.Add($"    phi*R_n = {phi_Rn_bs:F1} kips >= F_f = {F_f:F1}: " +
                      (blockShearOK ? "OK" : "FAIL"));
                p.Add($"  (Alternate mechanism per Fig. 13.7 need not be checked)");

                result.BlockShearPassed = blockShearOK;
                result.BlockShearRatio = F_f / phi_Rn_bs;
                if (!blockShearOK) allPassed = false;
                p.Add("");

                // ===== STEP 20: SHEAR CONNECTION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 20: SHEAR CONNECTION TO WEB");
                p.Add("--------------------------------------------------------------------------------");
                p.Add($"  V_u = V_h = {V_h:F1} kips");
                p.Add("  Design single-plate shear connection per AISC 360");
                p.Add("  Note: Extended shear tab likely needed due to large setback");
                p.Add("  L_sc must fit between T-stub flanges");
                p.Add("  [EOR responsibility - not automatically checked]");
                p.Add("");

                // ===== STEP 21: COLUMN FLANGE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 21: COLUMN FLANGE FLEXURAL YIELDING (EQ. 13.6-55)");
                p.Add("--------------------------------------------------------------------------------");

                // Column flange yield line parameters
                double g_ic = input.GTb;
                double a_c = (input.ColBf - g_ic) / 2; // Eq. 13.6-57
                double b_c = g_ic / 2;                   // Eq. 13.6-58

                // s (Eq. 13.6-60)
                double s_val = Math.Sqrt(input.ColBf * g_ic) / 2;

                // p_s with continuity plate (Eq. 13.6-59)
                double t_cp = input.BeamTf;
                double p_s = Math.Min((g_ic - t_cp) / 2, s_val);

                // Y_C (Eq. 13.6-56)
                double Y_C;
                if (b_c > 0 && (s_val + p_s) > 0)
                {
                    Y_C = (2 / b_c) * (s_val + p_s +
                        (a_c * b_c + b_c * b_c) / s_val +
                        (a_c * b_c + b_c * b_c) / p_s);
                }
                else
                {
                    Y_C = 6.0; // fallback
                }

                // Eq. 13.6-55
                double phi_Rn_cf = PHI_D * input.ColFy * Y_C * input.ColTf * input.ColTf;
                bool colFlangeOK = phi_Rn_cf >= F_f;

                // Alternative: Eq. 13.6-61
                double t_fc_req = Math.Sqrt(1.11 * F_f / (PHI_D * input.ColFy * Y_C));

                p.Add($"  a_c = {a_c:F2} in | b_c = {b_c:F2} in | s = {s_val:F2} in");
                p.Add($"  p_s = {p_s:F2} in | Y_C = {Y_C:F2}");
                p.Add($"  phi*R_n = phi_d*Fyc*Y_C*t_fc = {phi_Rn_cf:F1} kips  (Eq. 13.6-55)");
                p.Add($"  t_fc,req = {t_fc_req:F3} in  (Eq. 13.6-61)");
                p.Add($"  Column tf = {input.ColTf:F3} in: " +
                      (input.ColTf >= t_fc_req ? "OK" : "FAIL - need continuity plates"));

                result.ColumnFlangePassed = colFlangeOK;
                result.ColumnFlangeRatio = F_f / phi_Rn_cf;
                if (!colFlangeOK) allPassed = false;
                p.Add("");

                // ===== STEP 22: COLUMN WEB AND PANEL ZONE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 22: COLUMN WEB AND PANEL ZONE CHECKS");
                p.Add("--------------------------------------------------------------------------------");

                // Web local yielding (AISC 360 J10.2)
                double k_col = input.ColTf;
                double phi_Rn_wy = PHI_D * 5 * input.ColFy * k_col * input.ColTw;

                // Web local crippling (AISC 360 J10.3, Eq J10-4)
                double N_bearing = input.BeamBf;
                double phi_Rn_wc = PHI_N * 0.80 * input.ColTw * input.ColTw * (
                    1 + 3 * (N_bearing / input.ColD) * Math.Pow(input.ColTw / input.ColTf, 1.5)
                ) * Math.Sqrt(E * input.ColFy * input.ColTf / input.ColTw);

                double phi_Rn_web = Math.Min(phi_Rn_wy, phi_Rn_wc);
                bool webOK = phi_Rn_web >= F_f;

                p.Add($"  --- Web Local Checks ---");
                p.Add($"  F_f = {F_f:F1} kips (concentrated force)");
                p.Add($"  Web local yielding: phi*R_n = {phi_Rn_wy:F1} kips");
                p.Add($"  Web local crippling: phi*R_n = {phi_Rn_wc:F1} kips");
                p.Add($"  Web governing: phi*R_n = {phi_Rn_web:F1} kips: " +
                      (webOK ? "OK" : "FAIL - need continuity plates/doublers"));

                result.ColumnWebPassed = webOK;
                result.ColumnWebRatio = F_f / phi_Rn_web;
                if (!webOK) allPassed = false;

                // Panel zone shear (AISC 341 D1.2c + AISC 360 J10.6)
                double Sum_Mface = 2 * M_f;
                double H_story = (input.StoryAbove + input.StoryBelow) / 2;
                double V_col = H_story > 0 ? Sum_Mface / H_story : 0;
                double V_pz = Sum_Mface / input.BeamD - V_col;

                double phi_pz = 1.0;
                double phi_Rn_pz = phi_pz * 0.6 * input.ColFy * input.ColD * input.ColTw;

                bool pzOK = phi_Rn_pz >= V_pz;

                p.Add($"");
                p.Add($"  --- Panel Zone Shear (AISC 341 D1.2c + AISC 360 J10.6) ---");
                p.Add($"  Sum M_face = 2*M_f = 2*{M_f:F0} = {Sum_Mface:F0} kip-in");
                p.Add($"  V_col = Sum M_face / H = {Sum_Mface:F0} / {H_story:F0} = {V_col:F1} kips");
                p.Add($"  V_pz = {Sum_Mface:F0}/{input.BeamD:F2} - {V_col:F1} = {V_pz:F1} kips");
                p.Add($"  phi*R_n = {phi_pz}*0.6*{input.ColFy}*{input.ColD:F2}*{input.ColTw:F3}" +
                      $" = {phi_Rn_pz:F1} kips");
                p.Add($"  Panel zone: phi*R_n = {phi_Rn_pz:F1} >= V_pz = {V_pz:F1}: " +
                      (pzOK ? "OK" : "FAIL - need doubler plates"));

                result.PanelZonePassed = pzOK;
                result.PanelZoneRatio = V_pz / phi_Rn_pz;
                if (!pzOK) allPassed = false;
                p.Add("");

                // ===== STEP 23: CONTINUITY PLATES =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 23: CONTINUITY PLATES (SECTION 13.5.2)");
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  Continuity plates required at all column locations");
                p.Add($"  Min Thickness = beam tf = {input.BeamTf:F3} in");
                p.Add("  Extend to column flange edge less 1/4 in");
                p.Add("  Weld per AISC Seismic Provisions");
                result.ContinuityPlatesPassed = true;
                p.Add("");

                // ===== SUMMARY =====
                p.Add("================================================================================");
                p.Add("  DESIGN VERIFICATION SUMMARY");
                p.Add("================================================================================");

                p.Add($"Prequalification:           {(result.PrequalificationPassed ? "PASS" : "FAIL")}");
                p.Add($"Bolt diameter (Step 2):     {(result.BoltDiameterPassed ? "PASS" : "FAIL")}");
                p.Add($"Beam shear (Step 6a):       {(result.BeamShearPassed ? "PASS" : "FAIL")}");
                p.Add($"Column-beam ratio (Step 7a):{(result.ColumnBeamPassed ? "PASS" : "FAIL")}");
                p.Add($"FR Stiffness (Step 13):     {(result.StiffnessPassed ? "PASS" : "FAIL")}");
                p.Add($"Shear bolts (Step 15):      {(result.ShearBoltsPassed ? "PASS" : "FAIL")}");
                p.Add($"T-stem (Step 16):           {(result.TStemPassed ? "PASS" : "FAIL")}");
                p.Add($"T-flange (Step 17):         {(result.TFlangePassed ? "PASS" : "FAIL")}");
                p.Add($"Gage ratio g_tb/t_ft (Step 11): {(result.GageRatioPassed ? "PASS" : "FAIL")}");
                p.Add($"Bearing/tearout (Step 18):  {(result.BearingPassed ? "PASS" : "FAIL")}");
                p.Add($"Block shear (Step 19):      {(result.BlockShearPassed ? "PASS" : "FAIL")}");
                p.Add($"Column flange (Step 21):    {(result.ColumnFlangePassed ? "PASS" : "FAIL")}");
                p.Add($"Column web (Step 22):       {(result.ColumnWebPassed ? "PASS" : "FAIL")}");
                p.Add($"Panel zone (Step 22):       {(result.PanelZonePassed ? "PASS" : "FAIL")}");
                p.Add($"Continuity plates (Step 23): PASS");
                p.Add("");

                p.Add($"KEY RESULTS:");
                p.Add($"  M_pr = {M_pr:F0} kip-in | M_f = {M_f:F0} kip-in");
                p.Add($"  F_pr = {F_pr:F1} kips | F_f = {F_f:F1} kips");
                p.Add($"  V_h = {V_h:F1} kips");
                p.Add($"  Shear bolts: n_vb = {n_vb}, d_vb = {d_vb:F3} in");
                p.Add($"  Tension bolts: n_tb = {input.NTb}, d_tb = {d_tb:F3} in");
                p.Add($"  T-stub: t_st = {t_st:F3}, t_ft = {t_ft:F3}, b_ft = {b_ft:F2}");
                p.Add($"  S_h = {S_h:F2} in | L_h = {L_h:F1} in");
                p.Add("");

                p.Add("================================================================================");
                if (allPassed)
                    p.Add("  ALL CHECKS PASSED");
                else
                    p.Add("  SOME CHECKS FAILED - REVIEW AND ADJUST DESIGN");
                p.Add("================================================================================");

                // Store remaining results
                result.Dvb = d_vb;
                result.BFt = b_ft;
                result.TFt = t_ft;

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

        private static double ParseWeight(string designation)
        {
            try
            {
                var parts = designation.ToUpper().Split('X');
                if (parts.Length > 1 && double.TryParse(parts[1], out double w))
                    return w;
            }
            catch { }
            return 999;
        }
    }
}
