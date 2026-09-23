using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    /// <summary>
    /// KBB (Kaiser Bolted Bracket) Connection Design Verification
    /// AISC 358-16 Chapter 9, Section 9.9 - Design Procedure (18 steps)
    /// </summary>
    public static class KbbCalculations
    {
        // ====================== CONSTANTS ======================
        public const double PHI_D = 1.00;   // Ductile limit states
        public const double PHI_N = 0.90;   // Nonductile limit states
        public const double E = 29000.0;    // Modulus of elasticity (ksi)
        public const double DEFAULT_FEXX = 70.0; // E70 weld electrode (ksi)

        // ====================== BRACKET DATA (TABLES 9.1, 9.2, 9.3) ======================

        public class BracketData
        {
            public string Series = "";   // "W" or "B"
            public double Lbb;           // Bracket length (in)
            public double Hbb;           // Bracket height (in)
            public double Bbb;           // Bracket width (in)
            public int Ncb;              // Number of column bolts per bracket
            public double G;             // Column bolt gage (in)
            public double DbCol;         // Column bolt diameter (in)
            public double DbBeam;        // Beam bolt diameter (B-series only)
            public int[] NbbOptions = Array.Empty<int>(); // Valid beam bolt counts (B-series)
        }

        public class WSeriesProps
        {
            public double De;            // Edge distance (in)
            public double Pb;            // Bolt pitch (in), 0 for 2-bolt brackets
            public double Ts;            // Continuity plate thickness multiplier
            public double Rv;            // Vertical restraint parameter, 0 for N/A
            public double Rh;            // Horizontal restraint parameter, 0 for N/A
            public double W;             // Fillet weld leg size (in)
        }

        public class BSeriesProps
        {
            public double De;            // Edge distance (in)
            public double Pb;            // Bolt pitch (in)
            public double Ts;            // Continuity plate thickness multiplier
            public double Rv;            // Vertical restraint parameter
        }

        // Table 9.1 - KBB Proportions
        public static readonly Dictionary<string, BracketData> Brackets = new()
        {
            { "W3.0", new BracketData { Series = "W", Lbb = 16.0, Hbb = 5.5, Bbb = 9.0,  Ncb = 2, G = 5.5, DbCol = 1.375 } },
            { "W3.1", new BracketData { Series = "W", Lbb = 16.0, Hbb = 5.5, Bbb = 9.0,  Ncb = 2, G = 5.5, DbCol = 1.5 } },
            { "W2.0", new BracketData { Series = "W", Lbb = 16.0, Hbb = 8.75, Bbb = 9.5, Ncb = 4, G = 6.0, DbCol = 1.375 } },
            { "W2.1", new BracketData { Series = "W", Lbb = 18.0, Hbb = 8.75, Bbb = 9.5, Ncb = 4, G = 6.5, DbCol = 1.5 } },
            { "W1.0", new BracketData { Series = "W", Lbb = 25.5, Hbb = 12.0, Bbb = 9.5, Ncb = 6, G = 6.5, DbCol = 1.5 } },
            { "B2.1", new BracketData { Series = "B", Lbb = 18.0, Hbb = 8.75, Bbb = 10.0, Ncb = 4, G = 6.5, DbCol = 1.5, DbBeam = 1.125, NbbOptions = new[] { 8, 10 } } },
            { "B1.0", new BracketData { Series = "B", Lbb = 25.5, Hbb = 12.0, Bbb = 10.0, Ncb = 6, G = 6.5, DbCol = 1.5, DbBeam = 1.125, NbbOptions = new[] { 12 } } },
        };

        // Table 9.2 - W-Series Design Proportions
        public static readonly Dictionary<string, WSeriesProps> WSeriesPropsTable = new()
        {
            { "W3.0", new WSeriesProps { De = 2.5,  Pb = 0,    Ts = 1.0, Rv = 0,    Rh = 28.0, W = 0.500 } },
            { "W3.1", new WSeriesProps { De = 2.5,  Pb = 0,    Ts = 1.0, Rv = 0,    Rh = 28.0, W = 0.625 } },
            { "W2.0", new WSeriesProps { De = 2.25, Pb = 3.5,  Ts = 2.0, Rv = 12.0, Rh = 28.0, W = 0.750 } },
            { "W2.1", new WSeriesProps { De = 2.25, Pb = 3.5,  Ts = 2.0, Rv = 16.0, Rh = 38.0, W = 0.875 } },
            { "W1.0", new WSeriesProps { De = 2.0,  Pb = 3.5,  Ts = 2.0, Rv = 28.0, Rh = 0,    W = 0.875 } },
        };

        // Table 9.3 - B-Series Design Proportions
        public static readonly Dictionary<string, BSeriesProps> BSeriesPropsTable = new()
        {
            { "B2.1", new BSeriesProps { De = 2.0, Pb = 3.5, Ts = 2.0, Rv = 16.0 } },
            { "B1.0", new BSeriesProps { De = 2.0, Pb = 3.5, Ts = 2.0, Rv = 28.0 } },
        };

        // Column bolt properties (F3125 A490 or A354 Grade BD)
        public static readonly Dictionary<double, BoltProps> ColBoltProps = new()
        {
            { 1.375, new BoltProps { Fnt = 113.0, Fnv = 68.0, Ab = Math.PI * 1.375 * 1.375 / 4.0 } },
            { 1.5,   new BoltProps { Fnt = 113.0, Fnv = 68.0, Ab = Math.PI * 1.5 * 1.5 / 4.0 } },
        };

        // Beam bolt properties (F3125 A490, threads excluded from shear plane)
        public static readonly Dictionary<double, BoltProps> BeamBoltProps = new()
        {
            { 1.125, new BoltProps { Fnt = 113.0, Fnv = 84.0, Ab = Math.PI * 1.125 * 1.125 / 4.0 } },
        };

        // Yield line mechanism parameter Y_m per Step 9
        public static readonly Dictionary<string, double> YmValues = new()
        {
            { "W3.0", 5.9 }, { "W3.1", 5.9 },
            { "W2.0", 6.5 }, { "W2.1", 6.5 }, { "B2.1", 6.5 },
            { "W1.0", 7.5 }, { "B1.0", 7.5 },
        };

        // Tributary length p per bolt per Step 8
        public static readonly Dictionary<string, double> PValues = new()
        {
            { "W1.0", 3.5 }, { "B1.0", 3.5 },
            // All others default to 5.0
        };

        public class BoltProps
        {
            public double Fnt { get; set; }  // Nominal tensile strength (ksi)
            public double Fnv { get; set; }  // Nominal shear strength (ksi)
            public double Ab  { get; set; }  // Nominal unthreaded bolt area (in^2)
        }

        // ====================== DATA CLASSES ======================

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
            public double ColRt = 1.2;

            // Bracket selection
            public string BracketModel = "W2.1";

            // B-series: beam bolt count
            public int Nbb = 8;

            // Weld electrode (W-series)
            public double FEXX = 70.0;  // E70 (ksi)

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
            public bool BoltTensionPassed { get; set; }
            public bool CfWidthPassed { get; set; }
            public bool CfThicknessPryingPassed { get; set; }
            public bool ContinuityNoPlatesPassed { get; set; }
            public bool ContinuityPlatesPassed { get; set; }
            public bool BeamFlangeWidthPassed { get; set; }
            public bool BeamBoltShearPassed { get; set; }
            public bool BlockShearPassed { get; set; }
            public bool FilletWeldPassed { get; set; }
            public bool BeamShearPassed { get; set; }
            public bool PanelZonePassed { get; set; }

            // Key results
            public double Cpr { get; set; }
            public double Mpr { get; set; }    // kip-in
            public double Mf  { get; set; }    // kip-in
            public double Vh  { get; set; }    // kips
            public double DEff { get; set; }   // in
            public double Rut { get; set; }    // kips/bolt

            // Utilization ratios
            public double BoltTensionRatio { get; set; }
            public double CfWidthRatio { get; set; }
            public double CfThicknessRatio { get; set; }
            public double ContinuityRatio { get; set; }
            public double BeamBoltShearRatio { get; set; }
            public double BlockShearRatio { get; set; }
            public double FilletWeldRatio { get; set; }
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
                // Validate bracket model
                if (!Brackets.ContainsKey(input.BracketModel))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Invalid bracket model: {input.BracketModel}. Valid: W3.0, W3.1, W2.0, W2.1, W1.0, B2.1, B1.0";
                    p.Add($"ERROR: {result.ErrorMessage}");
                    return result;
                }

                var bk = Brackets[input.BracketModel];
                string series = bk.Series;
                double Lbb = bk.Lbb;

                // --- Input Summary ---
                p.Add("================================================================================");
                p.Add("  KBB CONNECTION DESIGN VERIFICATION (AISC 358-16 CHAPTER 9)");
                p.Add("================================================================================");
                p.Add("");
                p.Add("--- INPUT PARAMETERS ---");
                p.Add($"BEAM: {input.BeamDesignation} | d={input.BeamD:F2} bf={input.BeamBf:F2} tf={input.BeamTf:F3} tw={input.BeamTw:F3} Zx={input.BeamZx:F1}");
                p.Add($"      Fy={input.BeamFy} Fu={input.BeamFu} Ry={input.BeamRy} Rt={input.BeamRt}");
                p.Add($"COLUMN: {input.ColDesignation} | d={input.ColD:F2} bf={input.ColBf:F2} tf={input.ColTf:F3} tw={input.ColTw:F3}");
                p.Add($"        Fy={input.ColFy} Fu={input.ColFu} Ry={input.ColRy} Rt={input.ColRt}");
                p.Add($"BRACKET: {input.BracketModel} ({(series == "W" ? "Welded" : "Bolted")}-series)");
                p.Add($"  Lbb={bk.Lbb:F1} hbb={bk.Hbb:F2} bbb={bk.Bbb:F1}");
                p.Add($"  ncb={bk.Ncb} g={bk.G:F1} db_col={bk.DbCol:F3}");
                if (series == "B")
                {
                    p.Add($"  nbb={input.Nbb} db_beam={bk.DbBeam:F3}");
                }
                if (series == "W")
                {
                    var wp = WSeriesPropsTable[input.BracketModel];
                    p.Add($"  w={wp.W:F3} de={wp.De:F2}");
                }
                p.Add($"SPAN: L={input.Span:F0} in ({input.Span / 12:F1} ft) | {input.SystemType}");
                p.Add($"LOADS: D={input.LoadD} L={input.LoadL} S={input.LoadS} | Vu={input.Vu:F2}");
                p.Add("");

                // Derived
                double gravity = 1.2 * input.LoadD + input.F1 * input.LoadL + 0.2 * input.LoadS;

                // ===== STEP 0: PREQUALIFICATION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  PREQUALIFICATION LIMITS (SECTION 9.3)");
                p.Add("--------------------------------------------------------------------------------");

                bool prequal = true;

                // Beam depth <= W33
                p.Add($"  Beam depth: d = {input.BeamD:F1} in <= 33 in (W33 max): " + (input.BeamD <= 33 ? "OK" : "FAIL"));
                if (input.BeamD > 33) prequal = false;

                // Beam weight <= 130 plf
                double beamWeight = ParseWeight(input.BeamDesignation);
                p.Add($"  Beam weight: {beamWeight:F0} plf <= 130 plf: " + (beamWeight <= 130 ? "OK" : "FAIL"));
                if (beamWeight > 130) prequal = false;

                // Flange thickness <= 1.0 in
                p.Add($"  Flange thickness: tf = {input.BeamTf:F3} in <= 1.0 in: " + (input.BeamTf <= 1.0 ? "OK" : "FAIL"));
                if (input.BeamTf > 1.0) prequal = false;

                // Beam flange width >= 6 in (W-series) or >= 10 in (B-series)
                double bfMin = series == "W" ? 6.0 : 10.0;
                p.Add($"  Beam flange width: bf = {input.BeamBf:F2} in >= {bfMin:F0} in ({series}-series): " + (input.BeamBf >= bfMin ? "OK" : "FAIL"));
                if (input.BeamBf < bfMin) prequal = false;

                // Span/depth ratio >= 9
                double spanDepth = input.Span / input.BeamD;
                p.Add($"  Span/depth L/d = {spanDepth:F1} >= 9 ({input.SystemType}): " + (spanDepth >= 9 ? "OK" : "FAIL"));
                if (spanDepth < 9) prequal = false;

                // Column flange width >= 12 in
                p.Add($"  Column flange width: bf_c = {input.ColBf:F2} in >= 12 in: " + (input.ColBf >= 12.0 ? "OK" : "FAIL"));
                if (input.ColBf < 12.0) prequal = false;

                // Column depth: W14 max (no slab), W36 max (with slab)
                p.Add($"  Column depth: d_c = {input.ColD:F1} in <= 14 in (no slab) or <= 36 in (with slab): " +
                    (input.ColD > 36 ? "FAIL" : input.ColD > 14 ? "OK (requires concrete structural slab)" : "OK"));
                if (input.ColD > 36) prequal = false;

                // Lateral bracing and protected zone
                p.Add($"  Lateral bracing: d to 1.5d from bracket end");
                p.Add($"    = {input.BeamD:F1} to {1.5 * input.BeamD:F1} in from bracket end ({Lbb:F1} in from column face)");
                p.Add($"    = {Lbb + input.BeamD:F1} to {Lbb + 1.5 * input.BeamD:F1} in from column face");
                p.Add($"  Protected zone: column face to {Lbb:F1} + {input.BeamD:F1} = {Lbb + input.BeamD:F1} in from column face");

                result.PrequalificationPassed = prequal;
                if (!prequal) allPassed = false;
                p.Add("");

                // ===== STEP 1: M_pr =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 1: PROBABLE MAXIMUM MOMENT M_pr (SECTION 2.4.3)");
                p.Add("--------------------------------------------------------------------------------");

                double Cpr = Math.Min((input.BeamFy + input.BeamFu) / (2 * input.BeamFy), 1.2);
                double Mpr = Cpr * input.BeamRy * input.BeamFy * input.BeamZx;

                p.Add($"  C_pr = min((Fy+Fu)/(2*Fy), 1.2) = min(({input.BeamFy}+{input.BeamFu})/(2*{input.BeamFy}), 1.2) = {Cpr:F3}");
                p.Add($"  M_pr = C_pr * Ry * Fy * Zx");
                p.Add($"  M_pr = {Cpr:F3} * {input.BeamRy} * {input.BeamFy} * {input.BeamZx:F1}");
                p.Add($"  M_pr = {Mpr:F0} kip-in ({Mpr / 12:F1} kip-ft)");
                p.Add("");

                result.Cpr = Cpr;
                result.Mpr = Mpr;

                // ===== STEPS 2-3: BRACKET SELECTION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEPS 2-3: BRACKET SELECTION (TABLES 9.1-9.3)");
                p.Add("--------------------------------------------------------------------------------");

                double Sh = Lbb;
                p.Add($"  Bracket: {input.BracketModel}");
                p.Add($"  S_h = L_bb = {Sh:F1} in (plastic hinge at bracket end)");

                // d_eff: centroidal distance between bolt groups in upper/lower brackets
                double de, pbVal = 0;
                int ncb = bk.Ncb;

                if (series == "W")
                {
                    var wprops = WSeriesPropsTable[input.BracketModel];
                    de = wprops.De;
                    pbVal = wprops.Pb;
                }
                else
                {
                    var bprops = BSeriesPropsTable[input.BracketModel];
                    de = bprops.De;
                    pbVal = bprops.Pb;
                }

                double boltOffset;
                if (ncb == 2)
                {
                    // Single row at distance de from bracket far edge
                    boltOffset = bk.Hbb - de;
                }
                else if (ncb == 4)
                {
                    // Two rows: centroid at de + pb/2 from far edge
                    boltOffset = bk.Hbb - (de + pbVal / 2.0);
                }
                else // ncb == 6
                {
                    // Three rows: centroid at de + pb from far edge
                    boltOffset = bk.Hbb - (de + pbVal);
                }

                double dEff = input.BeamD + 2 * boltOffset;
                p.Add($"  d_eff = d + 2 * bolt_offset = {input.BeamD:F2} + 2 * {boltOffset:F2} = {dEff:F2} in");
                p.Add("");

                result.DEff = dEff;

                // ===== STEP 4: V_h =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 4: SHEAR FORCE AT PLASTIC HINGE");
                p.Add("--------------------------------------------------------------------------------");

                double Lh = input.Span - input.ColD - 2 * Sh;
                double Vh = 2 * Mpr / Lh + gravity / 2;

                p.Add($"  S_h = L_bb = {Sh:F1} in");
                p.Add($"  L_h = L - d_c - 2*S_h = {input.Span:F0} - {input.ColD:F2} - 2*{Sh:F1} = {Lh:F1} in");
                p.Add($"  Gravity load (1.2D + {input.F1}L + 0.2S) = {gravity:F2} kips");
                p.Add($"  V_h = 2*M_pr/L_h + gravity/2");
                p.Add($"  V_h = 2*{Mpr:F0}/{Lh:F1} + {gravity:F2}/2 = {Vh:F1} kips");
                p.Add("");

                result.Vh = Vh;

                // ===== STEP 5: M_f =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 5: MOMENT AT COLUMN FACE M_f (EQ. 9.9-1)");
                p.Add("--------------------------------------------------------------------------------");

                double Mf = Mpr + Vh * Sh;

                p.Add($"  M_f = M_pr + V_h * S_h  (Eq. 9.9-1)");
                p.Add($"  M_f = {Mpr:F0} + {Vh:F1} * {Sh:F1}");
                p.Add($"  M_f = {Mf:F0} kip-in ({Mf / 12:F1} kip-ft)");
                p.Add("");

                result.Mf = Mf;

                // ===== STEP 6: COLUMN BOLT TENSILE STRENGTH =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 6: COLUMN BOLT TENSILE STRENGTH (EQ. 9.9-2, 9.9-3)");
                p.Add("--------------------------------------------------------------------------------");

                double dbCol = bk.DbCol;
                if (!ColBoltProps.ContainsKey(dbCol))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"No bolt properties for column bolt diameter {dbCol}";
                    p.Add($"ERROR: {result.ErrorMessage}");
                    return result;
                }

                var colBolt = ColBoltProps[dbCol];
                double AbCol = colBolt.Ab;
                double Fnt = colBolt.Fnt;

                // Eq. 9.9-3: r_ut = M_f / (d_eff * n_cb)
                double rut = Mf / (dEff * ncb);

                // Eq. 9.9-2: r_ut <= phi_n * Fnt * Ab
                double boltCap = PHI_N * Fnt * AbCol;

                p.Add($"  d_eff = {dEff:F2} in, n_cb = {ncb}");
                p.Add($"  r_ut = M_f / (d_eff * n_cb) = {Mf:F0} / ({dEff:F2} * {ncb})");
                p.Add($"  r_ut = {rut:F1} kips/bolt  (Eq. 9.9-3)");
                p.Add($"  phi_n * F_nt * A_b = {PHI_N} * {Fnt} * {AbCol:F3} = {boltCap:F1} kips/bolt  (Eq. 9.9-2)");
                p.Add($"  Bolt: {dbCol:F3}-in dia F3125 A490 (or A354 Grade BD)");

                bool boltTensionOK = rut <= boltCap;
                double boltTensionRatio = rut / boltCap;
                p.Add($"  {(boltTensionOK ? "OK" : "FAIL")} (Utilization: {boltTensionRatio:F3})");
                result.BoltTensionPassed = boltTensionOK;
                result.BoltTensionRatio = boltTensionRatio;
                result.Rut = rut;
                if (!boltTensionOK) allPassed = false;
                p.Add("");

                // ===== STEP 7: MINIMUM COLUMN FLANGE WIDTH =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 7: MINIMUM COLUMN FLANGE WIDTH (EQ. 9.9-4)");
                p.Add("--------------------------------------------------------------------------------");

                double RyC = input.ColRy;
                double RtC = input.ColRt;
                double FyfC = input.ColFy;
                double FufC = input.ColFu;
                double bcf = input.ColBf;

                // Eq. 9.9-4: b_cf >= 2*(db + 1/8) / (1 - Ry*Fy/(Rt*Fu))
                double denom7 = 1.0 - RyC * FyfC / (RtC * FufC);
                double bcfReq;
                if (denom7 <= 0)
                {
                    p.Add($"  WARNING: Denominator (1 - Ry*Fy/(Rt*Fu)) = {denom7:F4} <= 0");
                    p.Add($"  Ry*Fy/(Rt*Fu) = {RyC}*{FyfC}/({RtC}*{FufC}) = {RyC * FyfC / (RtC * FufC):F3}");
                    bcfReq = 999.0;
                }
                else
                {
                    bcfReq = 2.0 * (dbCol + 0.125) / denom7;
                }

                p.Add($"  b_cf >= 2*(d_b + 1/8) / (1 - Ry*Fy_f/(Rt*Fu_f))");
                p.Add($"  b_cf >= 2*({dbCol:F3} + 0.125) / (1 - {RyC}*{FyfC}/({RtC}*{FufC}))");
                p.Add($"  b_cf >= {bcfReq:F2} in");
                p.Add($"  Provided b_cf = {bcf:F2} in");

                bool cfWidthOK = bcf >= bcfReq;
                p.Add($"  {(cfWidthOK ? "OK" : "FAIL")}");
                result.CfWidthPassed = cfWidthOK;
                result.CfWidthRatio = bcfReq > 0 ? bcf / bcfReq : 0;
                if (!cfWidthOK) allPassed = false;
                p.Add("");

                // ===== STEP 8: MINIMUM COLUMN FLANGE THICKNESS (PRYING) =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 8: MIN COLUMN FLANGE THICKNESS - NO PRYING (EQ. 9.9-5, 9.9-6)");
                p.Add("--------------------------------------------------------------------------------");

                double g = bk.G;
                double k1 = input.ColTw; // Approximate k1
                double tcw = input.ColTw;
                double FyC = input.ColFy;
                double tcf = input.ColTf;

                // Eq. 9.9-6: b' = 0.5*(g - k1 - 0.5*t_cw - d_b)
                double bPrime = 0.5 * (g - k1 - 0.5 * tcw - dbCol);

                // p value
                double pVal = PValues.ContainsKey(input.BracketModel) ? PValues[input.BracketModel] : 5.0;

                // Eq. 9.9-5: t_cf >= sqrt(4.44 * r_ut * b' / (phi_d * p * Fy))
                double tcfReq = Math.Sqrt(4.44 * rut * bPrime / (PHI_D * pVal * FyC));

                p.Add($"  b' = 0.5*(g - k1 - 0.5*t_cw - d_b)  (Eq. 9.9-6)");
                p.Add($"  b' = 0.5*({g:F1} - {k1:F3} - 0.5*{tcw:F3} - {dbCol:F3}) = {bPrime:F3} in");
                p.Add($"  p = {pVal:F1} in (tributary length per bolt)");
                p.Add($"  t_cf >= sqrt(4.44*r_ut*b' / (phi_d*p*Fy))  (Eq. 9.9-5)");
                p.Add($"  t_cf >= sqrt(4.44*{rut:F1}*{bPrime:F3} / ({PHI_D}*{pVal:F1}*{FyC}))");
                p.Add($"  t_cf >= {tcfReq:F3} in");
                p.Add($"  Provided t_cf = {tcf:F3} in");

                bool cfThickOK = tcf >= tcfReq;
                if (!cfThickOK)
                    p.Add($"  FAIL - Select column with thicker flange or include prying per AISC Manual Part 9");
                else
                    p.Add($"  OK (prying action eliminated)");
                result.CfThicknessPryingPassed = cfThickOK;
                result.CfThicknessRatio = tcfReq / tcf;
                if (!cfThickOK) allPassed = false;
                p.Add("");

                // ===== STEP 9: ELIMINATE CONTINUITY PLATES =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 9: COLUMN FLANGE THICKNESS TO ELIMINATE CONTINUITY PLATES (EQ. 9.9-7)");
                p.Add("--------------------------------------------------------------------------------");

                double Ym = YmValues[input.BracketModel];

                // Eq. 9.9-7: t_cf >= sqrt(M_f / (phi_d * Fy_f * d_eff * Y_m))
                double tcfReq9 = Math.Sqrt(Mf / (PHI_D * input.ColFy * dEff * Ym));

                p.Add($"  Y_m = {Ym} (for {input.BracketModel})");
                p.Add($"  t_cf >= sqrt(M_f / (phi_d * Fy_f * d_eff * Y_m))");
                p.Add($"  t_cf >= sqrt({Mf:F0} / ({PHI_D} * {input.ColFy} * {dEff:F2} * {Ym}))");
                p.Add($"  t_cf >= {tcfReq9:F3} in");
                p.Add($"  Provided t_cf = {tcf:F3} in");

                bool continuityNoPlatesOK = tcf >= tcfReq9;
                if (!continuityNoPlatesOK)
                    p.Add($"  Continuity plates ARE REQUIRED (see Step 10)");
                else
                    p.Add($"  OK (continuity plates not required by this check)");
                result.ContinuityNoPlatesPassed = continuityNoPlatesOK;
                result.ContinuityRatio = tcfReq9 / tcf;
                p.Add("");

                // ===== STEP 10: CONTINUITY PLATES =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 10: CONTINUITY PLATE REQUIREMENTS");
                p.Add("--------------------------------------------------------------------------------");

                bool isW14OrLess = input.ColD <= 14.0;

                if (isW14OrLess)
                {
                    if (continuityNoPlatesOK)
                    {
                        p.Add($"  Column depth = {input.ColD:F1} in <= W14");
                        p.Add($"  Eq. 9.9-7 satisfied => continuity plates NOT REQUIRED");
                        result.ContinuityPlatesPassed = true;
                    }
                    else
                    {
                        p.Add($"  Column depth = {input.ColD:F1} in <= W14");
                        p.Add($"  Eq. 9.9-7 NOT satisfied => continuity plates REQUIRED");
                        double tsMin = Math.Max(input.ColTw, 0.5 * input.BeamTf);
                        p.Add($"  Minimum continuity plate thickness: {tsMin:F3} in");
                        p.Add($"  Continuity plate design per AISC 341 Seismic Provisions");
                        result.ContinuityPlatesPassed = false;
                        allPassed = false;
                    }
                }
                else
                {
                    p.Add($"  Column depth = {input.ColD:F1} in > W14");
                    p.Add($"  Continuity plates SHALL be provided per Section 9.9 Step 10");
                    double tsMin = Math.Max(input.ColTw, 0.5 * input.BeamTf);
                    p.Add($"  Minimum continuity plate thickness: {tsMin:F3} in");
                    p.Add($"  Continuity plate design per AISC 341 Seismic Provisions");
                    result.ContinuityPlatesPassed = true; // Plates provided, check passes
                }
                p.Add("");

                // ===== STEP 11: BEAM FLANGE WIDTH (B-SERIES ONLY) =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 11: BEAM FLANGE WIDTH CHECK - B-SERIES (EQ. 9.9-8)");
                p.Add("--------------------------------------------------------------------------------");

                if (series == "W")
                {
                    p.Add("  W-series: bracket welded to beam flange, this step is N/A");
                    p.Add("  Proceed to Step 14.");
                    result.BeamFlangeWidthPassed = true;
                }
                else
                {
                    double dbBeam = bk.DbBeam;
                    double RyB = input.BeamRy;
                    double RtB = input.BeamRt;
                    double FyfB = input.BeamFy;
                    double FufB = input.BeamFu;
                    double bbf = input.BeamBf;

                    // Eq. 9.9-8: b_bf >= 2*(db + 1/32) / (1 - Ry*Fy/(Rt*Fu))
                    double denom11 = 1.0 - RyB * FyfB / (RtB * FufB);
                    double bbfReq;
                    if (denom11 <= 0)
                    {
                        bbfReq = 999.0;
                    }
                    else
                    {
                        bbfReq = 2.0 * (dbBeam + 1.0 / 32.0) / denom11;
                    }

                    p.Add($"  b_bf >= 2*(d_b + 1/32) / (1 - Ry*Fy_f/(Rt*Fu_f))  (Eq. 9.9-8)");
                    p.Add($"  b_bf >= 2*({dbBeam:F3} + {1.0 / 32.0:F4}) / (1 - {RyB}*{FyfB}/({RtB}*{FufB}))");
                    p.Add($"  b_bf >= {bbfReq:F2} in");
                    p.Add($"  Provided b_bf = {bbf:F2} in");

                    bool beamBfOK = bbf >= bbfReq;
                    p.Add($"  {(beamBfOK ? "OK" : "FAIL")}");
                    result.BeamFlangeWidthPassed = beamBfOK;
                    if (!beamBfOK) allPassed = false;
                }
                p.Add("");

                // ===== STEP 12: BEAM BOLT SHEAR (B-SERIES ONLY) =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 12: BEAM BOLT SHEAR STRENGTH - B-SERIES (EQ. 9.9-9)");
                p.Add("--------------------------------------------------------------------------------");

                if (series == "W")
                {
                    p.Add("  W-series: no beam bolts, this step is N/A");
                    result.BeamBoltShearPassed = true;
                }
                else
                {
                    double dbBeam = bk.DbBeam;
                    int nbb = input.Nbb;
                    if (!BeamBoltProps.ContainsKey(dbBeam))
                    {
                        p.Add($"  ERROR: No beam bolt properties for diameter {dbBeam}");
                        result.BeamBoltShearPassed = false;
                        allPassed = false;
                    }
                    else
                    {
                        var beamBolt = BeamBoltProps[dbBeam];
                        double AbBeam = beamBolt.Ab;
                        double FnvBeam = beamBolt.Fnv;

                        // Eq. 9.9-9: M_f / (phi_n * Fnv * Ab * d_eff * nbb) < 1.0
                        double ratio12 = Mf / (PHI_N * FnvBeam * AbBeam * dEff * nbb);

                        p.Add($"  M_f / (phi_n * F_nv * A_b * d_eff * n_bb) < 1.0  (Eq. 9.9-9)");
                        p.Add($"  {Mf:F0} / ({PHI_N} * {FnvBeam} * {AbBeam:F3} * {dEff:F2} * {nbb})");
                        p.Add($"  = {ratio12:F3}");

                        bool beamBoltShearOK = ratio12 < 1.0;
                        p.Add($"  {(beamBoltShearOK ? "OK" : "FAIL")} (Utilization: {ratio12:F3})");
                        result.BeamBoltShearPassed = beamBoltShearOK;
                        result.BeamBoltShearRatio = ratio12;
                        if (!beamBoltShearOK) allPassed = false;
                    }
                }
                p.Add("");

                // ===== STEP 13: BLOCK SHEAR (B-SERIES ONLY) =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 13: BEAM FLANGE BLOCK SHEAR - B-SERIES (EQ. 9.9-10)");
                p.Add("--------------------------------------------------------------------------------");

                if (series == "W")
                {
                    p.Add("  W-series: no beam bolts, this step is N/A");
                    result.BlockShearPassed = true;
                }
                else
                {
                    double dbBeam = bk.DbBeam;
                    int nbb = input.Nbb;
                    double dh = dbBeam + 0.0625; // standard hole

                    // Beam flange force
                    double Ff = Mf / dEff;

                    // Block shear per AISC 360 Chapter J
                    double tf = input.BeamTf;
                    double bfBeam = input.BeamBf;

                    // Simplified: symmetrical pattern, each side has nbb/4 bolts per line
                    int boltsPerSide = Math.Max(nbb / 4, 1);

                    double Lgv = boltsPerSide * 3.0; // approximate spacing
                    double Agv = 2 * tf * Lgv;
                    double Anv = 2 * tf * (Lgv - boltsPerSide * dh);
                    double Ant = tf * (bfBeam - 2 * dh);

                    double Ubs = 1.0;
                    double Rn1 = 0.6 * input.BeamFu * Anv + Ubs * input.BeamFu * Ant;
                    double Rn2 = 0.6 * input.BeamFy * Agv + Ubs * input.BeamFu * Ant;
                    double Rn = Math.Min(Rn1, Rn2);
                    double phiRn = PHI_N * Rn;

                    p.Add($"  Beam flange force: F_f = M_f / d_eff = {Mf:F0} / {dEff:F2} = {Ff:F1} kips");
                    p.Add($"  phi_n * R_n = {PHI_N} * {Rn:F1} = {phiRn:F1} kips");
                    p.Add($"  (Simplified block shear - verify with actual bolt layout)");

                    bool blockShearOK = Ff <= phiRn;
                    double blockShearRatio = phiRn > 0 ? Ff / phiRn : 0;
                    p.Add($"  {(blockShearOK ? "OK" : "FAIL")} (Utilization: {blockShearRatio:F3})");
                    result.BlockShearPassed = blockShearOK;
                    result.BlockShearRatio = blockShearRatio;
                    if (!blockShearOK) allPassed = false;
                }
                p.Add("");

                // ===== STEP 14: FILLET WELD (W-SERIES ONLY) =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 14: FILLET WELD ATTACHMENT - W-SERIES (EQ. 9.9-11)");
                p.Add("--------------------------------------------------------------------------------");

                if (series == "B")
                {
                    p.Add("  B-series: bracket bolted to beam flange, this step is N/A");
                    result.FilletWeldPassed = true;
                }
                else
                {
                    var wp = WSeriesPropsTable[input.BracketModel];
                    double w = wp.W;
                    double bbb = bk.Bbb;
                    double bbf = input.BeamBf;

                    // Eq. 9.9-12: l_w = 2*(L_bb - 2.5 - l)
                    // where l = 0 if bf >= bbb, l = 5 if bf < bbb
                    double lVal;
                    if (bbf >= bbb)
                    {
                        lVal = 0.0;
                        p.Add($"  b_bf ({bbf:F2}) >= b_bb ({bbb:F1}) => l = 0");
                    }
                    else
                    {
                        lVal = 5.0;
                        p.Add($"  b_bf ({bbf:F2}) < b_bb ({bbb:F1}) => l = 5 in");
                    }

                    double lw = 2.0 * (Lbb - 2.5 - lVal);
                    p.Add($"  l_w = 2*(L_bb - 2.5 - l) = 2*({Lbb:F1} - 2.5 - {lVal:F1}) = {lw:F2} in  (Eq. 9.9-12)");

                    // Eq. 9.9-11: M_f / (phi_n * F_w * d_eff * l_w * 0.707*w) < 1.0
                    double Fw = 0.60 * input.FEXX;
                    double denom14 = PHI_N * Fw * dEff * lw * 0.707 * w;
                    double ratio14 = Mf / denom14;

                    p.Add($"  F_w = 0.60 * F_EXX = 0.60 * {input.FEXX} = {Fw:F1} ksi");
                    p.Add($"  M_f / (phi_n * F_w * d_eff * l_w * 0.707*w) < 1.0  (Eq. 9.9-11)");
                    p.Add($"  {Mf:F0} / ({PHI_N} * {Fw:F1} * {dEff:F2} * {lw:F2} * 0.707*{w:F3})");
                    p.Add($"  = {ratio14:F3}");

                    bool weldOK = ratio14 < 1.0;
                    p.Add($"  {(weldOK ? "OK" : "FAIL")} (Utilization: {ratio14:F3})");
                    result.FilletWeldPassed = weldOK;
                    result.FilletWeldRatio = ratio14;
                    if (!weldOK) allPassed = false;
                }
                p.Add("");

                // ===== STEP 15: REQUIRED SHEAR =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 15: REQUIRED SHEAR STRENGTH (EQ. 9.9-13)");
                p.Add("--------------------------------------------------------------------------------");

                double Lh15 = input.Span - input.ColD - 2 * Sh;
                double Vu15 = 2 * Mpr / Lh15 + gravity / 2;

                // Beam shear capacity per AISC 360 Chapter G
                double Vn = 0.6 * input.BeamFy * input.BeamD * input.BeamTw;
                double phiVn = 1.0 * Vn; // phi_v = 1.0 for rolled W-shapes

                p.Add($"  L_h = {Lh15:F1} in");
                p.Add($"  V_u = 2*M_pr/L_h + V_gravity/2  (Eq. 9.9-13)");
                p.Add($"  V_u = 2*{Mpr:F0}/{Lh15:F1} + {gravity:F2}/2 = {Vu15:F1} kips");

                double VuDesign = Math.Max(Vu15, input.Vu);
                if (input.Vu > 0)
                    p.Add($"  V_u,user = {input.Vu:F1} kips => V_u = max({Vu15:F1}, {input.Vu:F1}) = {VuDesign:F1} kips");

                p.Add($"  Beam shear capacity: V_n = 0.6*Fy*d*tw = {Vn:F1} kips");
                p.Add($"  phi*V_n = {phiVn:F1} kips");

                bool beamShearOK = VuDesign <= phiVn;
                double beamShearRatio = phiVn > 0 ? VuDesign / phiVn : 0;
                p.Add($"  {(beamShearOK ? "OK" : "FAIL")} (Utilization: {beamShearRatio:F3})");
                result.BeamShearPassed = beamShearOK;
                result.BeamShearRatio = beamShearRatio;
                if (!beamShearOK) allPassed = false;
                p.Add("");

                // ===== STEP 16: WEB CONNECTION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 16: BEAM WEB-TO-COLUMN CONNECTION (SECTION 9.7)");
                p.Add("--------------------------------------------------------------------------------");

                double Lh16 = input.Span - input.ColD - 2 * Sh;
                double Vu16 = 2 * Mpr / Lh16 + gravity / 2;
                double VuDesign16 = Math.Max(Vu16, input.Vu);

                p.Add($"  Required shear: V_u = {VuDesign16:F1} kips");
                p.Add($"  Single-plate shear connection per Section 9.7:");
                p.Add($"    - Connected to column flange via two-sided fillet, PJP, or CJP weld");
                p.Add($"    - Pretensioned high-strength bolts (beam web to shear tab)");
                p.Add($"    - Design per AISC 360 for V_u = {VuDesign16:F1} kips");
                p.Add("");

                // ===== STEP 17: PANEL ZONE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 17: COLUMN PANEL ZONE (SECTION 9.4)");
                p.Add("--------------------------------------------------------------------------------");

                double Fyc = input.ColFy;
                double dc = input.ColD;
                double twc = input.ColTw;

                // Panel zone demand: flange force based on M_f and d_eff
                double Vpz = Mf / dEff;

                // Panel zone capacity per AISC 360 J10.6 (phi=1.0 per AISC 341)
                double phiPz = 1.0;
                double VnBasic = 0.6 * Fyc * dc * twc;

                // With column flange contribution
                double bcfPz = input.ColBf;
                double tcfPz = input.ColTf;
                double contrib = 3.0 * bcfPz * tcfPz * tcfPz / (dEff * dc * twc);
                double VnFull = VnBasic * (1.0 + contrib);

                p.Add($"  Panel zone demand: V_pz = M_f / d_eff = {Mf:F0} / {dEff:F2} = {Vpz:F1} kips");
                p.Add($"  Use d_eff (not beam d) per Section 9.9 Step 17");
                p.Add($"  phi = {phiPz} (per AISC 341)");
                p.Add($"  Vn (basic) = 0.6*Fyc*dc*twc = 0.6*{Fyc}*{dc:F2}*{twc:F3} = {VnBasic:F1} kips");
                p.Add($"  Flange contribution factor: {contrib:F3}");
                p.Add($"  Vn (full) = {VnFull:F1} kips");

                bool panelZoneOK = Vpz <= phiPz * VnFull;
                double panelZoneRatio = phiPz * VnFull > 0 ? Vpz / (phiPz * VnFull) : 0;
                if (!panelZoneOK)
                    p.Add($"  FAIL - Consider web doubler plates (Utilization: {panelZoneRatio:F3})");
                else
                    p.Add($"  OK (Utilization: {panelZoneRatio:F3})");
                result.PanelZonePassed = panelZoneOK;
                result.PanelZoneRatio = panelZoneRatio;
                if (!panelZoneOK) allPassed = false;
                p.Add("");

                // ===== SUMMARY =====
                p.Add("================================================================================");
                p.Add("  DESIGN VERIFICATION SUMMARY");
                p.Add("================================================================================");

                p.Add($"Prequalification:              {(result.PrequalificationPassed ? "PASS" : "FAIL")}");
                p.Add($"Bolt tension (Step 6):         {(result.BoltTensionPassed ? "PASS" : "FAIL")}");
                p.Add($"Column flange width (Step 7):  {(result.CfWidthPassed ? "PASS" : "FAIL")}");
                p.Add($"CF thickness/prying (Step 8):  {(result.CfThicknessPryingPassed ? "PASS" : "FAIL")}");
                p.Add($"Continuity plates (Steps 9-10):{(result.ContinuityPlatesPassed ? "PASS" : "FAIL")}");
                if (series == "B")
                {
                    p.Add($"Beam flange width (Step 11):   {(result.BeamFlangeWidthPassed ? "PASS" : "FAIL")}");
                    p.Add($"Beam bolt shear (Step 12):     {(result.BeamBoltShearPassed ? "PASS" : "FAIL")}");
                    p.Add($"Block shear (Step 13):         {(result.BlockShearPassed ? "PASS" : "FAIL")}");
                }
                if (series == "W")
                {
                    p.Add($"Fillet weld (Step 14):         {(result.FilletWeldPassed ? "PASS" : "FAIL")}");
                }
                p.Add($"Beam shear (Step 15):          {(result.BeamShearPassed ? "PASS" : "FAIL")}");
                p.Add($"Panel zone (Step 17):          {(result.PanelZonePassed ? "PASS" : "FAIL")}");
                p.Add("");
                p.Add($"KEY: M_pr={Mpr:F0} kip-in | M_f={Mf:F0} kip-in | d_eff={dEff:F2} in");
                p.Add($"     r_ut={rut:F1} kips/bolt | Bracket={input.BracketModel}");
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
