using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    /// <summary>
    /// ConXL (ConXtech) Moment Connection Design Verification
    /// AISC 358-16 Chapter 10, Section 10.8 - Design Procedure (11 steps)
    ///
    /// ConXL connections use high-strength field-bolted collar assemblies to connect
    /// wide-flange beams to concrete-filled 16-in. square HSS or built-up box columns.
    /// </summary>
    public static class ConxlCalculations
    {
        // ====================== CONSTANTS ======================

        // Resistance factors (AISC 358-16 Section 2.4.1)
        public const double PHI_D = 1.00;   // Ductile limit states
        public const double PHI_N = 0.90;   // Nonductile limit states
        public const double PHI_V = 1.00;   // Shear (rolled W-shapes, AISC 360 G2.1(a))

        // ConXL proprietary constants
        public const double T_COLLAR = 7.125;   // in - distance column face to collar outside face (Fig. 10.10)
        public const double DCOL = 16.0;        // in - column outside dimension (always 16-in square)
        public const int    N_CF = 8;           // collar bolts per collar flange (fixed)
        public const double DB_COLLAR = 1.25;   // in - collar bolt diameter (1-1/4 in ASTM A574)
        public const double TB = 102.0;         // kips - minimum bolt pretension (same as 1-1/4" A490)
        public const double D_LEG_CC = 3.5;     // in - effective depth of collar corner leg (Eq. 10.8-19)

        // Steel properties
        public const double E = 29000.0;    // ksi

        // Concrete limits
        public const double MIN_FC = 3.0;               // ksi (3000 psi minimum per Section 10.3.2(6))
        public const double MIN_CONCRETE_WEIGHT = 110.0; // pcf minimum per Section 10.3.2(6)

        // Default weld electrode
        public const double DEFAULT_FEXX = 70.0; // ksi (E70)

        // Prequalified beam depth groups with required weld lengths
        public static readonly Dictionary<string, BeamGroupData> BeamGroups = new()
        {
            { "W30", new BeamGroupData { LwCWX = 54.0, LwCC = 72.0 } },
            { "W27", new BeamGroupData { LwCWX = 48.0, LwCC = 66.0 } },
            { "W24", new BeamGroupData { LwCWX = 42.0, LwCC = 60.0 } },
            { "W21", new BeamGroupData { LwCWX = 36.0, LwCC = 54.0 } },
            { "W18", new BeamGroupData { LwCWX = 30.0, LwCC = 48.0 } },
        };

        // Slip-critical bolt parameters (Class B per Commentary C-10.8)
        public const double MU_CLASS_B = 0.50;   // Class B: machined surfaces
        public const double DU = 1.13;            // Uniformity factor
        public const double PHI_SC = 0.85;        // Slip-critical phi (oversized holes)
        public const int N_BOLTS_TOTAL = 16;      // 8 per collar flange x 2 (top + bottom)

        // ====================== DATA CLASSES ======================

        public class BeamGroupData
        {
            public double LwCWX { get; set; }  // Beam web-to-CWX weld length (in)
            public double LwCC  { get; set; }   // Collar corner-to-column weld length (in)
        }

        public class InputParameters
        {
            // Beam properties (W-shape, selected from AISC database)
            public string BeamDesignation = "";
            public double BeamD  = 24.0;    // Depth (in)
            public double BeamBf = 9.0;     // Flange width (in)
            public double BeamTf = 0.5;     // Flange thickness (in)
            public double BeamTw = 0.3;     // Web thickness (in)
            public double BeamZx = 200.0;   // Plastic section modulus (in^3)
            public double BeamFy = 50.0;    // ksi (A992)
            public double BeamFu = 65.0;    // ksi (A992)
            public double BeamRy = 1.1;
            public double BeamRt = 1.2;

            // Column properties (16-in square concrete-filled box column)
            public double TCol = 0.5;            // Column wall thickness (in), min 0.375
            public double ColFy = 50.0;          // ksi (A500 Gr C / A572 Gr 50)
            public double ColFu = 62.0;          // ksi (A500 Gr C)
            public double Fc = 4.0;              // Concrete f'c (ksi)
            public double ConcreteWeight = 145.0; // Concrete unit weight (pcf), min 110
            public double TLegCC = 0.75;         // Effective collar corner leg thickness (in)

            // RBS geometry (optional)
            public bool   UseRbs = false;
            public double RbsA = 0.0;   // Distance from collar face to start of cut (in)
            public double RbsB = 0.0;   // Length of cut (in)
            public double RbsC = 0.0;   // Depth of cut at center (in)

            // Design parameters
            public double Span = 300.0;          // Span (in)
            public string SystemType = "SMF";    // SMF or IMF
            public double StoryAbove = 156.0;    // Story height above node (in), default 13 ft
            public double StoryBelow = 156.0;    // Story height below node (in), default 13 ft
            public double Pu = 0.0;              // Axial load on column (kips)
            public double FEXX = DEFAULT_FEXX;   // Weld electrode strength (ksi)

            // Gravity loads
            public double LoadD = 0.0;   // kips
            public double LoadL = 0.0;
            public double LoadS = 0.0;
            public double F1   = 0.5;     // Live load factor
            public double Vu   = 0.0;     // Specified shear (kips)
        }

        public class DesignResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = "";
            public List<string> Process { get; set; } = new();

            public bool OverallPassed { get; set; }

            // Individual checks
            public bool PrequalificationPassed { get; set; }
            public bool ColumnBeamPassed { get; set; }
            public bool BoltTensionPassed { get; set; }
            public bool BoltShearPassed { get; set; }
            public bool BeamShearPassed { get; set; }
            public bool CwxWeldPassed { get; set; }
            public bool CcWeldPassed { get; set; }
            public bool PanelZonePassed { get; set; }

            // Key results
            public double Cpr { get; set; }
            public double Ze  { get; set; }
            public double Mpr { get; set; }       // kip-in
            public double Vh  { get; set; }        // kips
            public double Sh  { get; set; }        // in - column CL to plastic hinge
            public double Sf  { get; set; }        // in - column face to plastic hinge
            public double SBolts { get; set; }     // in - hinge to bolt centroid
            public double MBolts { get; set; }     // kip-in
            public double Rut { get; set; }        // kips - bolt tension demand
            public double PzDemand { get; set; }   // kips - panel zone demand
            public double PzCapacity { get; set; } // kips - panel zone capacity

            // Utilization ratios
            public double BoltTensionRatio { get; set; }
            public double BoltShearRatio { get; set; }
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
                // Derived column properties
                double dInner = DCOL - 2 * input.TCol;
                double As = 4 * DCOL * input.TCol - 4 * input.TCol * input.TCol;
                double Ac = dInner * dInner;
                double Zc = (Math.Pow(DCOL, 3) - Math.Pow(dInner, 3)) / 4.0;

                // Gravity combination
                double gravity = 1.2 * input.LoadD + input.F1 * input.LoadL + 0.2 * input.LoadS;

                // Determine beam depth group
                string depthGroup = GetDepthGroup(input.BeamDesignation);

                // --- Input Summary ---
                p.Add("================================================================================");
                p.Add("  ConXL CONNECTION DESIGN VERIFICATION (AISC 358-16 CHAPTER 10)");
                p.Add("================================================================================");
                p.Add("");
                p.Add("--- INPUT PARAMETERS ---");
                p.Add($"BEAM: {input.BeamDesignation} | d={input.BeamD:F2} bf={input.BeamBf:F2} tf={input.BeamTf:F3} tw={input.BeamTw:F3} Zx={input.BeamZx:F1}");
                p.Add($"      Fy={input.BeamFy} Fu={input.BeamFu} Ry={input.BeamRy} Rt={input.BeamRt}");
                if (input.UseRbs)
                    p.Add($"      RBS: a={input.RbsA:F2} b={input.RbsB:F2} c={input.RbsC:F2}");
                p.Add($"COLUMN: {DCOL:F0}-in. square HSS/box | t_col={input.TCol:F3} Fy={input.ColFy} Fu={input.ColFu}");
                p.Add($"        Concrete: f'c={input.Fc:F1} ksi | As={As:F2} in^2 | Ac={Ac:F1} in^2");
                p.Add($"        Zc={Zc:F1} in^3 | t_leg_CC={input.TLegCC:F3}");
                p.Add($"COLLAR: t_collar={T_COLLAR:F3} | n_cf={N_CF} | d_b={DB_COLLAR:F3} (ASTM A574)");
                p.Add($"        T_b={TB:F0} kips | d_leg_CC={D_LEG_CC:F1}");
                p.Add($"SPAN: L={input.Span:F0} in ({input.Span / 12:F1} ft) | {input.SystemType}");
                p.Add($"STORY: H_above={input.StoryAbove:F0} in | H_below={input.StoryBelow:F0} in");
                p.Add($"       Pu={input.Pu:F1} kips");
                p.Add($"LOADS: D={input.LoadD} L={input.LoadL} S={input.LoadS} | Vu={input.Vu:F2}");
                p.Add("");

                // ===== STEP 0: PREQUALIFICATION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  PREQUALIFICATION LIMITS (SECTION 10.3)");
                p.Add("--------------------------------------------------------------------------------");

                bool prequal = true;

                // Beam depth must be in prequalified list
                p.Add($"  Beam depth group: {depthGroup} | Must be W30, W27, W24, W21, or W18: " +
                       (BeamGroups.ContainsKey(depthGroup) ? "OK" : "FAIL"));
                if (!BeamGroups.ContainsKey(depthGroup)) prequal = false;

                // Beam flange thickness <= 1.0 in
                p.Add($"  Flange thickness: tf = {input.BeamTf:F3} in <= 1.0 in: " +
                       (input.BeamTf <= 1.0 ? "OK" : "FAIL"));
                if (input.BeamTf > 1.0) prequal = false;

                // Beam flange width <= 12 in
                p.Add($"  Flange width: bf = {input.BeamBf:F2} in <= 12 in: " +
                       (input.BeamBf <= 12.0 ? "OK" : "FAIL"));
                if (input.BeamBf > 12.0) prequal = false;

                // Span/depth ratio
                double spanDepth = input.Span / input.BeamD;
                double sdMin = input.SystemType == "SMF" ? 7.0 : 5.0;
                p.Add($"  Span/depth L/d = {spanDepth:F1} >= {sdMin:F0} ({input.SystemType}): " +
                       (spanDepth >= sdMin ? "OK" : "FAIL"));
                if (spanDepth < sdMin) prequal = false;

                // Column: 16-in square
                p.Add($"  Column dimension: {DCOL:F1} in (must be 16 in): OK (fixed)");

                // Column wall thickness >= 3/8 in
                p.Add($"  Column wall thickness: t_col = {input.TCol:F3} in >= 0.375 in: " +
                       (input.TCol >= 0.375 ? "OK" : "FAIL"));
                if (input.TCol < 0.375) prequal = false;

                // Concrete strength >= 3000 psi
                p.Add($"  Concrete f'c = {input.Fc:F1} ksi >= {MIN_FC:F1} ksi: " +
                       (input.Fc >= MIN_FC ? "OK" : "FAIL"));
                if (input.Fc < MIN_FC) prequal = false;

                // Concrete unit weight >= 110 pcf
                p.Add($"  Concrete unit weight = {input.ConcreteWeight:F0} pcf >= {MIN_CONCRETE_WEIGHT:F0} pcf: " +
                       (input.ConcreteWeight >= MIN_CONCRETE_WEIGHT ? "OK" : "FAIL"));
                if (input.ConcreteWeight < MIN_CONCRETE_WEIGHT) prequal = false;

                // RBS geometry limits (if applicable)
                if (input.UseRbs)
                {
                    double aMin = 0.5 * input.BeamBf;
                    double aMax = 0.75 * input.BeamBf;
                    double bMin = 0.65 * input.BeamD;
                    double bMax = 0.85 * input.BeamD;
                    double cMin = 0.1 * input.BeamBf;
                    double cMax = 0.25 * input.BeamBf;

                    p.Add($"  RBS a = {input.RbsA:F2} in ({aMin:F2} to {aMax:F2}): " +
                           (aMin <= input.RbsA && input.RbsA <= aMax ? "OK" : "WARNING - outside recommended range"));
                    p.Add($"  RBS b = {input.RbsB:F2} in ({bMin:F2} to {bMax:F2}): " +
                           (bMin <= input.RbsB && input.RbsB <= bMax ? "OK" : "WARNING - outside recommended range"));
                    p.Add($"  RBS c = {input.RbsC:F2} in ({cMin:F2} to {cMax:F2}): " +
                           (cMin <= input.RbsC && input.RbsC <= cMax ? "OK" : "WARNING - outside recommended range"));
                }

                // Protected zone
                double pz;
                if (input.UseRbs)
                {
                    pz = T_COLLAR + input.RbsA + input.RbsB;
                    p.Add($"  Protected zone: column face to {pz:F1} in (end of RBS)");
                }
                else
                {
                    pz = T_COLLAR + input.BeamD;
                    p.Add($"  Protected zone: column face to {pz:F1} in (collar face + d)");
                }

                result.PrequalificationPassed = prequal;
                if (!prequal) allPassed = false;
                p.Add("");

                // ===== DESIGN PROCEDURE =====
                p.Add("================================================================================");
                p.Add("  DESIGN PROCEDURE (SECTION 10.8)");
                p.Add("================================================================================");
                p.Add("");

                // ===== STEP 1: M_pr =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 1: PROBABLE MAXIMUM MOMENT M_pr (SECTION 2.4.3)");
                p.Add("--------------------------------------------------------------------------------");

                double Cpr, Ze;
                if (input.UseRbs)
                {
                    // RBS: C_pr per Eq. 2.4-2
                    Cpr = Math.Min((input.BeamFy + input.BeamFu) / (2 * input.BeamFy), 1.2);
                    p.Add($"  RBS beam: C_pr = min((Fy+Fu)/(2*Fy), 1.2)");
                    p.Add($"  C_pr = min(({input.BeamFy}+{input.BeamFu})/(2*{input.BeamFy}), 1.2) = {Cpr:F3}");

                    // Z_e = Z_RBS = Zx - 2*c*tf*(d-tf)
                    Ze = input.BeamZx - 2 * input.RbsC * input.BeamTf * (input.BeamD - input.BeamTf);
                    p.Add($"  Z_e = Z_RBS = Zx - 2*c*tf*(d-tf)");
                    p.Add($"  Z_e = {input.BeamZx:F1} - 2*{input.RbsC:F2}*{input.BeamTf:F3}*({input.BeamD:F2}-{input.BeamTf:F3})");
                    p.Add($"  Z_e = {Ze:F1} in^3");
                }
                else
                {
                    // Non-RBS: C_pr = 1.1 (ConXL specific!)
                    Cpr = 1.1;
                    p.Add($"  Non-RBS beam: C_pr = 1.1 (ConXL specific, NOT Eq. 2.4-2)");
                    Ze = input.BeamZx;
                    p.Add($"  Z_e = Zx = {input.BeamZx:F1} in^3");
                }

                double Mpr = Cpr * input.BeamRy * input.BeamFy * Ze;

                p.Add($"  M_pr = C_pr * Ry * Fy * Ze");
                p.Add($"  M_pr = {Cpr:F3} * {input.BeamRy} * {input.BeamFy} * {Ze:F1}");
                p.Add($"  M_pr = {Mpr:F0} kip-in ({Mpr / 12:F1} kip-ft)");

                // Compute distances
                double dcHalf = DCOL / 2.0;
                double sH, sF, sBolts;

                if (input.UseRbs)
                {
                    sF = T_COLLAR + input.RbsA + input.RbsB / 2.0;              // Eq. 10.8-13
                    sH = dcHalf + T_COLLAR + input.RbsA + input.RbsB / 2.0;     // Eq. 10.8-15
                    sBolts = T_COLLAR / 2.0 + input.RbsA + input.RbsB / 2.0;    // Eq. 10.8-5
                }
                else
                {
                    sF = T_COLLAR + input.BeamD / 2.0;                           // Eq. 10.8-14
                    sH = dcHalf + T_COLLAR + input.BeamD / 2.0;                  // Eq. 10.8-16
                    sBolts = T_COLLAR / 2.0 + input.BeamD / 2.0;                 // Eq. 10.8-6
                }

                p.Add($"  s_h = {sH:F2} in (column CL to plastic hinge)");
                p.Add($"  s_f = {sF:F2} in (column face to plastic hinge)");
                p.Add($"  s_bolts = {sBolts:F2} in (hinge to bolt centroid)");
                p.Add("");

                result.Cpr = Cpr;
                result.Ze = Ze;
                result.Mpr = Mpr;
                result.Sh = sH;
                result.Sf = sF;
                result.SBolts = sBolts;

                // ===== STEP 2: V_h =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 2: SHEAR FORCE AT PLASTIC HINGE (EQ. 10.8-1)");
                p.Add("--------------------------------------------------------------------------------");

                double Lh = input.Span - 2 * sH;
                double Vh = 2 * Mpr / Lh + gravity / 2.0;

                p.Add($"  L_h = L - 2*s_h = {input.Span:F0} - 2*{sH:F2} = {Lh:F1} in");
                p.Add($"  Gravity (1.2D + {input.F1}L + 0.2S) = {gravity:F2} kips");
                p.Add($"  V_h = 2*M_pr/L_h + gravity/2  (Eq. 10.8-1)");
                p.Add($"  V_h = 2*{Mpr:F0}/{Lh:F1} + {gravity:F2}/2 = {Vh:F1} kips");
                p.Add("");

                result.Vh = Vh;

                // ===== STEP 3: COLUMN-BEAM MOMENT RATIO =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 3: COLUMN-BEAM MOMENT RATIO (EQ. 10.8-2, 10.8-3)");
                p.Add("--------------------------------------------------------------------------------");

                if (input.SystemType == "IMF")
                {
                    p.Add("  IMF: Column-beam ratio per AISC Seismic Provisions");
                    p.Add("  (May not require strong-column/weak-beam check)");
                    result.ColumnBeamPassed = true;
                }
                else
                {
                    // SMF biaxial strong-column/weak-beam check
                    double Mv = Vh * sH;
                    int nBeams = 2;
                    double SumMpb = nBeams * (Mpr + Mv);

                    p.Add($"  SMF biaxial strong-column/weak-beam check:");
                    p.Add($"  M_v = V_h * s_h = {Vh:F1} * {sH:F2} = {Mv:F0} kip-in");
                    p.Add($"  Sum M_pb* = {nBeams}*(M_pr + M_v) = {nBeams}*({Mpr:F0} + {Mv:F0})");
                    p.Add($"            = {SumMpb:F0} kip-in (about one axis)");

                    // Eq. 10.8-3: M_pc*
                    double denom = As * input.ColFy + 0.85 * Ac * input.Fc;
                    double MpcStar;
                    if (denom == 0)
                        MpcStar = 0;
                    else
                        MpcStar = Math.Max(0, 0.67 * Zc * input.ColFy * (1 - input.Pu / denom));

                    p.Add($"");
                    p.Add($"  Eq. 10.8-3: M_pc* = 0.67*Zc*Fy*(1 - Pu/(As*Fy + 0.85*Ac*f'c))");
                    p.Add($"  Zc = {Zc:F1} in^3 | Fy = {input.ColFy} ksi");
                    p.Add($"  As = {As:F2} in^2 | Ac = {Ac:F1} in^2 | f'c = {input.Fc} ksi");
                    p.Add($"  Pu = {input.Pu:F1} kips");
                    p.Add($"  M_pc* = 0.67*{Zc:F1}*{input.ColFy}*(1 - {input.Pu:F1}/({As * input.ColFy:F1} + {0.85 * Ac * input.Fc:F1}))");
                    p.Add($"  M_pc* = {MpcStar:F0} kip-in");

                    // Eq. 10.8-2
                    double Hu = input.StoryAbove;
                    double Hl = input.StoryBelow;
                    double dBeam = input.BeamD;
                    double SumMpc = 2 * MpcStar + SumMpb / (Hu + Hl) * dBeam;

                    p.Add($"");
                    p.Add($"  Eq. 10.8-2: Sum M_pc* = M_pcu* + M_pcl* + Sum M_pb*/(Hu+Hl)*d");
                    p.Add($"  Hu = {Hu:F0} in | Hl = {Hl:F0} in | d (beam) = {dBeam:F1} in");
                    p.Add($"  Sum M_pc* = 2*{MpcStar:F0} + {SumMpb:F0}/({Hu:F0}+{Hl:F0})*{dBeam:F1}");
                    p.Add($"  Sum M_pc* = {SumMpc:F0} kip-in");

                    double ratio = SumMpb > 0 ? SumMpc / SumMpb : 999;
                    bool colBeamOK = ratio >= 1.0;
                    p.Add($"");
                    p.Add($"  Ratio Sum M_pc* / Sum M_pb* = {ratio:F3}");
                    if (!colBeamOK)
                        p.Add($"  FAIL - Consider RBS cutouts or heavier column");
                    else
                        p.Add($"  OK");
                    p.Add($"  Note: Simplified (same column above/below, {nBeams} beams about one axis)");
                    p.Add($"  For final design, check both axes per AISC 341 E3.6c.");

                    result.ColumnBeamPassed = colBeamOK;
                    if (!colBeamOK) allPassed = false;
                }
                p.Add("");

                // ===== STEP 4: M_bolts =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 4: MOMENT AT COLLAR BOLTS (EQ. 10.8-4)");
                p.Add("--------------------------------------------------------------------------------");

                double MBolts = Mpr + Vh * sBolts;

                if (input.UseRbs)
                    p.Add($"  s_bolts = t_collar/2 + a + b/2 = {T_COLLAR:F3}/2 + {input.RbsA:F2} + {input.RbsB:F2}/2");
                else
                    p.Add($"  s_bolts = t_collar/2 + d/2 = {T_COLLAR:F3}/2 + {input.BeamD:F2}/2");
                p.Add($"  s_bolts = {sBolts:F2} in");
                p.Add($"  M_bolts = M_pr + V_h * s_bolts  (Eq. 10.8-4)");
                p.Add($"  M_bolts = {Mpr:F0} + {Vh:F1} * {sBolts:F2}");
                p.Add($"  M_bolts = {MBolts:F0} kip-in ({MBolts / 12:F1} kip-ft)");
                p.Add("");

                result.MBolts = MBolts;

                // ===== STEP 5: COLLAR BOLT TENSION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 5: COLLAR BOLT TENSILE STRENGTH (EQ. 10.8-7, 10.8-8)");
                p.Add("--------------------------------------------------------------------------------");

                // Eq. 10.8-8: r_ut = M_bolts / (n_cf * d * sin45)
                double sin45 = Math.Sin(Math.PI / 4.0);
                double rUt = MBolts / (N_CF * input.BeamD * sin45);
                double rUtCheck = 0.177 * MBolts / input.BeamD;

                p.Add($"  r_ut = M_bolts / (n_cf * d * sin45)  (Eq. 10.8-8)");
                p.Add($"  r_ut = {MBolts:F0} / ({N_CF} * {input.BeamD:F2} * sin45)");
                p.Add($"  r_ut = {rUt:F1} kips (= 0.177*{MBolts:F0}/{input.BeamD:F2} = {rUtCheck:F1})");

                // Eq. 10.8-7: r_ut / (phi_d * R_pt) = r_ut / 102 <= 1.0
                double boltTensionRatio = rUt / TB;
                p.Add($"");
                p.Add($"  r_ut / (phi_d * R_pt) = {rUt:F1} / {TB:F0} = {boltTensionRatio:F3}  (Eq. 10.8-7)");
                p.Add($"  phi_d = {PHI_D} | R_pt = {TB:F0} kips (min bolt pretension)");
                p.Add($"  Bolt: {DB_COLLAR:F3}-in dia ASTM A574 (pretensioned as A490)");

                bool boltTensionOK = boltTensionRatio <= 1.0;
                p.Add($"  {(boltTensionOK ? "OK" : "FAIL")} (Utilization: {boltTensionRatio:F3})");

                result.BoltTensionPassed = boltTensionOK;
                result.Rut = rUt;
                result.BoltTensionRatio = boltTensionRatio;
                if (!boltTensionOK) allPassed = false;
                p.Add("");

                // ===== STEP 6: SLIP-CRITICAL BOLT SHEAR =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 6: COLLAR BOLT SLIP-CRITICAL SHEAR CHECK");
                p.Add("--------------------------------------------------------------------------------");

                double VBolts = Vh; // Simplified

                double RnPerBolt = MU_CLASS_B * DU * TB;
                double phiRnPerBolt = PHI_SC * RnPerBolt;
                double RnTotal = N_BOLTS_TOTAL * phiRnPerBolt;

                p.Add($"  V_bolts ~ V_h = {VBolts:F1} kips (simplified, gravity on short segment neglected)");
                p.Add($"  Slip-critical resistance (Class B, oversized holes, per Commentary C-10.8):");
                p.Add($"    R_n/bolt = mu * D_u * T_b = {MU_CLASS_B} * {DU} * {TB:F0} = {RnPerBolt:F1} kips");
                p.Add($"    phi*R_n/bolt = {PHI_SC} * {RnPerBolt:F1} = {phiRnPerBolt:F1} kips/bolt");
                p.Add($"    phi*R_n(total) = {N_BOLTS_TOTAL} bolts * {phiRnPerBolt:F1} = {RnTotal:F1} kips");
                p.Add($"    Note: 16 bolts = 8 (top CFT) + 8 (bottom CFB) per Commentary");

                bool boltShearOK = VBolts <= RnTotal;
                double boltShearRatio = RnTotal > 0 ? VBolts / RnTotal : 999;
                p.Add($"  {(boltShearOK ? "OK" : "FAIL")} (Utilization: {boltShearRatio:F3})");

                result.BoltShearPassed = boltShearOK;
                result.BoltShearRatio = boltShearRatio;
                if (!boltShearOK) allPassed = false;
                p.Add("");

                // ===== STEP 7: BEAM SHEAR =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 7: BEAM SHEAR STRENGTH CHECK");
                p.Add("--------------------------------------------------------------------------------");

                double Vcf = Vh;
                double Vu = Math.Max(Vcf, input.Vu);
                double Vn = 0.6 * input.BeamFy * input.BeamD * input.BeamTw;
                double phiVn = PHI_V * Vn;

                p.Add($"  V_cf ~ V_h = {Vcf:F1} kips");
                p.Add($"  V_u = max(V_cf, Vu_user) = max({Vcf:F1}, {input.Vu:F2}) = {Vu:F1} kips");
                p.Add($"  V_n = 0.6*Fy*d*tw = 0.6*{input.BeamFy}*{input.BeamD:F2}*{input.BeamTw:F3} = {Vn:F1} kips");
                p.Add($"  phi*V_n = {phiVn:F1} kips");

                bool beamShearOK = Vu <= phiVn;
                double beamShearRatio = phiVn > 0 ? Vu / phiVn : 999;
                p.Add($"  {(beamShearOK ? "OK" : "FAIL")} (Utilization: {beamShearRatio:F3})");

                result.BeamShearPassed = beamShearOK;
                result.BeamShearRatio = beamShearRatio;
                if (!beamShearOK) allPassed = false;
                p.Add("");

                // ===== STEP 8: CWX FILLET WELD =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 8: BEAM WEB-TO-CWX FILLET WELD (EQ. 10.8-9)");
                p.Add("--------------------------------------------------------------------------------");

                if (!BeamGroups.ContainsKey(depthGroup))
                {
                    p.Add($"  ERROR: Beam group '{depthGroup}' not in prequalified list");
                    result.CwxWeldPassed = false;
                    allPassed = false;
                }
                else
                {
                    double lwCWX = BeamGroups[depthGroup].LwCWX;
                    double Fw = 0.60 * input.FEXX;

                    // Eq. 10.8-9: tf_CWX >= sqrt(2) * V_cf / (phi_n * Fw * lw_CWX)
                    double tfReqCWX = Math.Sqrt(2) * Vcf / (PHI_N * Fw * lwCWX);

                    p.Add($"  Beam group: {depthGroup} | l_w^CWX = {lwCWX:F0} in");
                    p.Add($"  F_w = 0.60 * F_EXX = 0.60 * {input.FEXX} = {Fw:F1} ksi");
                    p.Add($"  V_cf = {Vcf:F1} kips");
                    p.Add($"  t_f^CWX >= sqrt(2)*V_cf / (phi_n*Fw*l_w^CWX)  (Eq. 10.8-9)");
                    p.Add($"  t_f^CWX >= {Math.Sqrt(2):F3}*{Vcf:F1} / ({PHI_N}*{Fw:F1}*{lwCWX:F0})");
                    p.Add($"  t_f^CWX >= {tfReqCWX:F4} in");

                    // Minimum practical weld size
                    double wMin;
                    if (input.BeamTw <= 0.25)
                        wMin = 0.125;
                    else if (input.BeamTw <= 0.375)
                        wMin = 0.15625; // 5/32
                    else
                        wMin = 0.1875; // 3/16

                    p.Add($"  Recommended fillet weld size: {Math.Max(tfReqCWX, wMin):F3} in (each side)");
                    p.Add($"  Note: Two-sided fillet weld to CWX per Section 10.5.2");

                    // The check passes if a reasonable weld can be made
                    double maxWeld = 0.5 * input.BeamTw;
                    bool cwxOK = tfReqCWX <= maxWeld;
                    if (cwxOK)
                        p.Add($"  OK (required {tfReqCWX:F3} <= max practical {maxWeld:F3} in)");
                    else
                        p.Add($"  FAIL (required {tfReqCWX:F3} > max practical {maxWeld:F3} in)");

                    result.CwxWeldPassed = cwxOK;
                    if (!cwxOK) allPassed = false;
                }
                p.Add("");

                // ===== STEP 9: COLLAR CORNER WELD =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 9: COLLAR CORNER-TO-COLUMN FILLET WELD (EQ. 10.8-10)");
                p.Add("--------------------------------------------------------------------------------");

                if (!BeamGroups.ContainsKey(depthGroup))
                {
                    p.Add($"  ERROR: Beam group '{depthGroup}' not in prequalified list");
                    result.CcWeldPassed = false;
                    allPassed = false;
                }
                else
                {
                    double lwCC = BeamGroups[depthGroup].LwCC;
                    double FwCC = 0.60 * input.FEXX;
                    double Vf = Vh;

                    // Eq. 10.8-10: tf_CC >= sqrt(2) * V_f / (phi_n * Fw * lw_CC)
                    double tfReqCC = Math.Sqrt(2) * Vf / (PHI_N * FwCC * lwCC);

                    p.Add($"  Beam group: {depthGroup} | l_w^CC = {lwCC:F0} in");
                    p.Add($"  F_w = 0.60 * F_EXX = 0.60 * {input.FEXX} = {FwCC:F1} ksi");
                    p.Add($"  V_f ~ V_h = {Vf:F1} kips");
                    p.Add($"  t_f^CC >= sqrt(2)*V_f / (phi_n*Fw*l_w^CC)  (Eq. 10.8-10)");
                    p.Add($"  t_f^CC >= {Math.Sqrt(2):F3}*{Vf:F1} / ({PHI_N}*{FwCC:F1}*{lwCC:F0})");
                    p.Add($"  t_f^CC >= {tfReqCC:F4} in");
                    p.Add($"  Note: Flare bevel groove weld with 3/8-in fillet reinforcing per Section 10.4(4)");

                    result.CcWeldPassed = true; // Prescriptive detail
                }
                p.Add("");

                // ===== STEP 10: PANEL ZONE DEMAND =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 10: PANEL ZONE DEMAND (EQ. 10.8-11)");
                p.Add("--------------------------------------------------------------------------------");

                double Hu10 = input.StoryAbove;
                double Hl10 = input.StoryBelow;
                double H10 = (Hu10 + Hl10) / 2.0; // Eq. 10.8-17

                // V_col = Sum(M_pr + V_h * s_h) / H  (Eq. 10.8-12)
                int nBeams10 = 2;
                double SumMpSh = nBeams10 * (Mpr + Vh * sH);
                double Vcol = SumMpSh / H10;

                // R_n^pz = Sum(M_pr + V_h * s_f) / d - V_col  (Eq. 10.8-11)
                double SumMpSf = nBeams10 * (Mpr + Vh * sF);
                double RnPz = SumMpSf / input.BeamD - Vcol;

                p.Add($"  H = (Hu + Hl)/2 = ({Hu10:F0} + {Hl10:F0})/2 = {H10:F1} in  (Eq. 10.8-17)");
                p.Add($"  Sum(M_pr + V_h*s_h) = {nBeams10}*({Mpr:F0} + {Vh:F1}*{sH:F2})");
                p.Add($"                       = {SumMpSh:F0} kip-in");
                p.Add($"  V_col = Sum(M_pr + V_h*s_h)/H = {SumMpSh:F0}/{H10:F1} = {Vcol:F1} kips  (Eq. 10.8-12)");
                p.Add($"");
                p.Add($"  Sum(M_pr + V_h*s_f) = {nBeams10}*({Mpr:F0} + {Vh:F1}*{sF:F2})");
                p.Add($"                       = {SumMpSf:F0} kip-in");
                p.Add($"  R_n^pz = Sum(M_pr + V_h*s_f)/d - V_col  (Eq. 10.8-11)");
                p.Add($"  R_n^pz = {SumMpSf:F0}/{input.BeamD:F2} - {Vcol:F1} = {RnPz:F1} kips");

                result.PzDemand = RnPz;
                p.Add("");

                // ===== STEP 11: PANEL ZONE CAPACITY =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 11: PANEL ZONE CAPACITY (EQ. 10.8-18, 10.8-19)");
                p.Add("--------------------------------------------------------------------------------");

                // Eq. 10.8-19: A_pz = 2*dc*tcol + 4*(d_leg_CC * t_leg_CC)
                double Apz = 2 * DCOL * input.TCol + 4 * (D_LEG_CC * input.TLegCC);

                // Eq. 10.8-18: phi*R_n^pz = phi_d * 0.6 * Fy * A_pz
                double phiRnPz = PHI_D * 0.6 * input.ColFy * Apz;

                p.Add($"  A_pz = 2*dc*tcol + 4*(d_leg_CC * t_leg_CC)  (Eq. 10.8-19)");
                p.Add($"  A_pz = 2*{DCOL:F1}*{input.TCol:F3} + 4*({D_LEG_CC:F1}*{input.TLegCC:F3})");
                p.Add($"  A_pz = {2 * DCOL * input.TCol:F2} + {4 * D_LEG_CC * input.TLegCC:F2} = {Apz:F2} in^2");
                p.Add($"");
                p.Add($"  phi*R_n^pz = phi_d * 0.6 * Fy * A_pz  (Eq. 10.8-18)");
                p.Add($"  phi*R_n^pz = {PHI_D} * 0.6 * {input.ColFy} * {Apz:F2}");
                p.Add($"  phi*R_n^pz = {phiRnPz:F1} kips");
                p.Add($"");
                p.Add($"  Demand R_n^pz = {RnPz:F1} kips");

                bool panelZoneOK = RnPz <= phiRnPz;
                double panelZoneRatio = phiRnPz > 0 ? RnPz / phiRnPz : 999;
                if (!panelZoneOK)
                    p.Add($"  FAIL - Increase column wall thickness or reduce beam (Utilization: {panelZoneRatio:F3})");
                else
                    p.Add($"  OK (Utilization: {panelZoneRatio:F3})");

                result.PanelZonePassed = panelZoneOK;
                result.PzCapacity = phiRnPz;
                result.PanelZoneRatio = panelZoneRatio;
                if (!panelZoneOK) allPassed = false;
                p.Add("");

                // ===== SUMMARY =====
                p.Add("================================================================================");
                p.Add("  DESIGN VERIFICATION SUMMARY");
                p.Add("================================================================================");

                p.Add($"Prequalification:      {(result.PrequalificationPassed ? "PASS" : "FAIL")}");
                p.Add($"Column-beam ratio:     {(result.ColumnBeamPassed ? "PASS" : "FAIL")}");
                p.Add($"Bolt tension (Step 5): {(result.BoltTensionPassed ? "PASS" : "FAIL")}");
                p.Add($"Bolt shear/slip (Step 6): {(result.BoltShearPassed ? "PASS" : "FAIL")}");
                p.Add($"Beam shear (Step 7):   {(result.BeamShearPassed ? "PASS" : "FAIL")}");
                p.Add($"CWX weld (Step 8):     {(result.CwxWeldPassed ? "PASS" : "FAIL")}");
                p.Add($"CC weld (Step 9):      {(result.CcWeldPassed ? "PASS" : "FAIL")}");
                p.Add($"Panel zone (Steps 10-11): {(result.PanelZonePassed ? "PASS" : "FAIL")}");
                p.Add("");

                string rbsStr = input.UseRbs ? " (RBS)" : " (non-RBS)";
                p.Add($"KEY: M_pr={Mpr:F0} kip-in | V_h={Vh:F1} kips | C_pr={Cpr:F3}{rbsStr}");
                p.Add($"     M_bolts={MBolts:F0} kip-in | r_ut={rUt:F1} kips");
                p.Add($"     s_h={sH:F2} | s_f={sF:F2} | s_bolts={sBolts:F2}");
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
                result.Process.Add($"ERROR: {ex.Message}");
            }

            return result;
        }

        // ====================== HELPER METHODS ======================

        /// <summary>
        /// Extract nominal beam depth group (e.g., "W24" from "W24X68")
        /// </summary>
        private static string GetDepthGroup(string designation)
        {
            if (string.IsNullOrEmpty(designation)) return "";
            string name = designation.ToUpper().Replace(" ", "");
            foreach (var group in BeamGroups.Keys)
            {
                if (name.StartsWith(group))
                    return group;
            }
            return "";
        }
    }
}
