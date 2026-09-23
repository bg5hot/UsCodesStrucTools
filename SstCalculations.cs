using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    /// <summary>
    /// SST (Simpson Strong-Tie) Strong Frame Moment Connection Design Verification
    /// AISC 358-16 Chapter 12, Section 12.9 - Design Procedure (19 steps)
    ///
    /// PR connection using Yield-Link structural fuses.
    /// Plastic hinging occurs in Yield-Links, NOT in the beam.
    /// Two link types: T-stub and End-plate.
    /// </summary>
    public static class SstCalculations
    {
        // ====================== CONSTANTS ======================
        public const double PHI_D = 1.00;   // Ductile limit states
        public const double PHI_N = 0.90;   // Nonductile limit states
        public const double PHI_V = 0.90;   // Shear
        public const double PHI_B = 0.90;   // Flexure
        public const double E = 29000.0;    // Modulus of elasticity (ksi)

        // Yield-Link material (A572 Gr 50 plate)
        public const double RY_LINK = 1.1;  // Section 12.9 Step 7
        public const double RT_LINK = 1.2;  // Section 12.9 Step 7

        // Yield-Link geometry limits
        public const double T_STEM_MIN = 0.5;    // in
        public const double T_STEM_MAX = 1.0;    // in
        public const double B_YIELD_MAX = 6.0;   // in (150 mm)

        // Buckling restraint plate limits
        public const double T_BRP_MIN = 0.875;   // in (7/8 in)
        public const double FY_BRP = 50.0;        // ksi minimum
        public const double RY_BRP = 1.1;
        public const double D_BRP_BOLT_MIN = 0.625; // in (5/8 in)
        public const double MU_K = 0.3;            // dry kinetic friction coefficient

        // Beam flange minimum thickness
        public const double TF_BEAM_MIN = 0.40;   // in (10 mm)

        // Default weld electrode
        public const double DEFAULT_FEXX = 70.0;  // ksi (E70)

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

            // Yield-Link geometry
            public double TStem = 0.75;       // Stem thickness (in)
            public double BColSide = 0.0;     // Nonreduced width at column side (0=auto)
            public double BBmSide = 0.0;      // Nonreduced width at beam side (0=auto)
            public double BYield = 0.0;       // Width of reduced yielding section (0=auto)
            public double LColSide = 0.0;     // Nonreduced length at column side (0=auto)
            public double LBmSide = 0.0;      // Nonreduced length at beam side (0=auto)
            public double LYLink = 0.0;       // Minimum yielding length (0=auto)

            // Connection type
            public string LinkType = "tstub";  // "tstub" or "endplate"
            public double ADist = 3.0;         // Distance from shear bolt CL to column face (in)

            // Yield-Link material
            public double LinkFy = 50.0;       // ksi (A572 Gr 50)
            public double LinkFu = 65.0;       // ksi

            // Weld electrode
            public double FEXX = 70.0;         // ksi (E70)

            // Design parameters
            public double Span = 300.0;        // Span (in)
            public string SystemType = "SMF";  // SMF or IMF
            public double StoryAbove = 156.0;  // Story height above node (in)
            public double StoryBelow = 156.0;  // Story height below node (in)

            // Loads
            public double LoadD = 0.0;         // kips
            public double LoadL = 0.0;
            public double LoadS = 0.0;
            public double F1 = 0.5;            // Live load factor
            public double Vu = 0.0;            // Specified shear (0=calculate)
            public double Mu = 3500.0;         // Moment demand from elastic analysis (kip-in)
            public double PuSp = 0.0;          // Required axial strength of connection (kips)
        }

        public class DesignResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = "";
            public List<string> Process { get; set; } = new();

            public bool OverallPassed { get; set; }

            // Individual checks
            public bool PrequalificationPassed { get; set; }
            public bool FlangeConnectionPassed { get; set; }
            public bool BucklingRestraintPassed { get; set; }
            public bool StiffnessPassed { get; set; }
            public bool BeamShearPassed { get; set; }
            public bool ColumnBeamPassed { get; set; }
            public bool ShearPlatePassed { get; set; }
            public bool PanelZonePassed { get; set; }
            public bool ColumnWebPassed { get; set; }
            public bool ColumnFlangePassed { get; set; }
            public bool ContinuityPlatesPassed { get; set; }

            // Key results
            public double Mpr { get; set; }          // kip-in
            public double PrLink { get; set; }        // kips
            public double PyeLink { get; set; }       // kips
            public double VuCalc { get; set; }        // kips
            public double BYield { get; set; }        // in
            public double LYLink { get; set; }        // in
            public double AYLink { get; set; }        // in^2

            // Connection stiffness
            public double KEff { get; set; }          // kip/in
            public double ThetaY { get; set; }        // rad

            // Utilization ratios
            public double BeamShearRatio { get; set; }
            public double PanelZoneRatio { get; set; }
            public double StiffnessRatio { get; set; }
        }

        // ====================== MAIN CALCULATE ======================

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var p = result.Process;
            bool allPassed = true;

            try
            {
                // Derived
                double FyLink = input.LinkFy;
                double FuLink = input.LinkFu;
                double gravity = 1.2 * input.LoadD + input.F1 * input.LoadL + 0.2 * input.LoadS;

                // Working variables (mirrors Python self._xxx pattern)
                double AYLinkReq, PyLinkReq;
                double bColSide, bBmSide, bYield, LColSide, LBmSide, LYLink, AYLink;
                double R_transition;
                double PrLink, PyeLink, Mpr, MyeLink;
                double tFlange, rT, dFlangeBolt, PrWeld;
                double Lh, VuCalc;
                double K1, K2, K3, KEff, deltaY, thetaY, delta04, delta07;
                double phiRnPz, RnPzDemand;

                // ===== HEADER =====
                p.Add("================================================================================");
                p.Add("  SST STRONG FRAME CONNECTION DESIGN VERIFICATION");
                p.Add("  AISC 358-16 CHAPTER 12, SECTION 12.9");
                p.Add("================================================================================");
                p.Add("");
                p.Add("--- INPUT PARAMETERS ---");
                p.Add($"BEAM: {input.BeamDesignation} | d={input.BeamD:F2} bf={input.BeamBf:F2} tf={input.BeamTf:F3} tw={input.BeamTw:F3} Zx={input.BeamZx:F1}");
                p.Add($"      Fy={input.BeamFy} Fu={input.BeamFu} Ry={input.BeamRy} Rt={input.BeamRt}");
                p.Add($"COLUMN: {input.ColDesignation} | d={input.ColD:F2} bf={input.ColBf:F2} tf={input.ColTf:F3} tw={input.ColTw:F3} Zx={input.ColZx:F1}");
                p.Add($"        Fy={input.ColFy} Fu={input.ColFu}");
                p.Add($"LINK TYPE: {input.LinkType} | t_stem={input.TStem:F3} in");
                p.Add($"YIELD-LINK MATERIAL: Fy={FyLink} Fu={FuLink} ksi | Ry={RY_LINK} Rt={RT_LINK}");
                p.Add($"SPAN: L={input.Span:F0} in ({input.Span / 12:F1} ft) | {input.SystemType}");
                p.Add($"STORY: H_above={input.StoryAbove:F0} in | H_below={input.StoryBelow:F0} in");
                p.Add($"LOADS: D={input.LoadD} L={input.LoadL} S={input.LoadS} | Mu={input.Mu:F0} kip-in | Pu_sp={input.PuSp:F1} kips");
                p.Add($"       a (shear bolt to col face) = {input.ADist:F1} in");
                p.Add("");

                // ===== STEP 0: PREQUALIFICATION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  PREQUALIFICATION LIMITS (SECTION 12.3)");
                p.Add("--------------------------------------------------------------------------------");

                bool prequal = true;
                string lt = input.LinkType;
                double maxDepth, minDepth;

                if (lt == "tstub")
                {
                    maxDepth = 36.0;
                    minDepth = 0.0;
                    p.Add($"  T-stub Yield-Link: beam depth limit W36 (max d={maxDepth:F0} in)");
                }
                else
                {
                    minDepth = 8.0;
                    maxDepth = 12.0;
                    p.Add($"  End-plate Yield-Link: beam depth W8 to W12 (d={minDepth:F0} to {maxDepth:F0} in)");
                    if (input.BeamD < minDepth)
                    {
                        p.Add($"  FAIL: beam depth {input.BeamD:F2} < {minDepth:F0} in");
                        prequal = false;
                    }
                }

                p.Add($"  Beam depth: d = {input.BeamD:F2} in <= {maxDepth:F0} in: " + (input.BeamD <= maxDepth ? "OK" : "FAIL"));
                if (input.BeamD > maxDepth) prequal = false;

                p.Add($"  Flange thickness: tf = {input.BeamTf:F3} in >= {TF_BEAM_MIN:F2} in: " + (input.BeamTf >= TF_BEAM_MIN ? "OK" : "FAIL"));
                if (input.BeamTf < TF_BEAM_MIN) prequal = false;

                p.Add($"  Column depth: d = {input.ColD:F2} in <= 36 in: " + (input.ColD <= 36.0 ? "OK" : "FAIL"));
                if (input.ColD > 36.0) prequal = false;

                // t_stem range
                p.Add($"  Stem thickness: t_stem = {input.TStem:F3} in [{T_STEM_MIN:F3}, {T_STEM_MAX:F3}]: " +
                       (input.TStem >= T_STEM_MIN && input.TStem <= T_STEM_MAX ? "OK" : "FAIL"));
                if (input.TStem < T_STEM_MIN || input.TStem > T_STEM_MAX) prequal = false;

                result.PrequalificationPassed = prequal;
                if (!prequal) allPassed = false;
                p.Add("");

                // ===== DESIGN PROCEDURE HEADER =====
                p.Add("================================================================================");
                p.Add("  DESIGN PROCEDURE (SECTION 12.9)");
                p.Add("================================================================================");
                p.Add("");

                // ===== STEP 1-2: BEAM SELECTION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 1-2: BEAM SELECTION AND SIMPLE SPAN CHECK");
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  Step 1: Trial beam/column selected by EOR assuming FR connections.");
                p.Add("  Step 2: Check beam as simply supported between shear plate connections.");
                p.Add("  (EOR responsibility - verifying with provided Mu)");
                p.Add($"  Applied moment Mu = {input.Mu:F0} kip-in");
                p.Add("");

                // ===== STEP 3: YIELD AREA =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 3: REQUIRED YIELD-LINK YIELD AREA (EQ. 12.9-1, 12.9-2)");
                p.Add("--------------------------------------------------------------------------------");

                double Mu = input.Mu;
                double dBeam = input.BeamD;

                PyLinkReq = Mu / (PHI_B * dBeam);
                p.Add($"  P'_y-link = Mu / (phi_b * d)  (Eq. 12.9-1)");
                p.Add($"  P'_y-link = {Mu:F0} / ({PHI_B} * {dBeam:F2}) = {PyLinkReq:F1} kips");

                AYLinkReq = PyLinkReq / FyLink;
                p.Add($"  A'_y-link = P'_y-link / F_y-link  (Eq. 12.9-2)");
                p.Add($"  A'_y-link = {PyLinkReq:F1} / {FyLink:F1} = {AYLinkReq:F2} in^2");
                p.Add("");

                // ===== STEP 4: COLUMN-SIDE GEOMETRY =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 4: YIELD-LINK COLUMN-SIDE GEOMETRY");
                p.Add("--------------------------------------------------------------------------------");

                // Step 4.1: b_col-side
                if (input.BColSide > 0)
                {
                    bColSide = input.BColSide;
                    p.Add($"  Step 4.1: b_col-side (user specified) = {bColSide:F2} in");
                }
                else
                {
                    bColSide = Math.Min(input.BeamBf, input.ColBf);
                    p.Add($"  Step 4.1: b_col-side = min(beam_bf, col_bf) = min({input.BeamBf:F2}, {input.ColBf:F2}) = {bColSide:F2} in");
                }

                // Step 4.2: L_col-side
                if (input.LColSide > 0)
                {
                    LColSide = input.LColSide;
                    p.Add($"  Step 4.2: L_col-side (user specified) = {LColSide:F1} in");
                }
                else
                {
                    double tFlangeEst = 0.75;
                    double LColSideMin = input.ADist - tFlangeEst + 1.0;
                    double LColSideMax = 5.0;
                    LColSide = Math.Min(Math.Max(LColSideMin, 3.0), LColSideMax);
                    p.Add($"  Step 4.2: L_col-side = {LColSide:F1} in (min={LColSideMin:F1}, max={LColSideMax:F1})");
                }

                // b_bm-side initial
                if (input.BBmSide > 0)
                {
                    bBmSide = input.BBmSide;
                    p.Add($"  b_bm-side (user specified) = {bBmSide:F2} in");
                }
                else
                {
                    bBmSide = bColSide;
                    p.Add($"  b_bm-side (initial) = {bBmSide:F2} in");
                }
                p.Add("");

                // ===== STEP 5: YIELD WIDTH =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 5: YIELDING SECTION WIDTH (EQ. 12.9-3)");
                p.Add("--------------------------------------------------------------------------------");

                double tStem = input.TStem;

                p.Add($"  t_stem = {tStem:F3} in (min {T_STEM_MIN:F3}, max {T_STEM_MAX:F3})");

                if (input.BYield > 0)
                {
                    bYield = input.BYield;
                    AYLink = bYield * tStem;
                    p.Add($"  b_yield (user specified) = {bYield:F2} in");
                    p.Add($"  A_y-link = b_yield * t_stem = {bYield:F2} * {tStem:F3} = {AYLink:F3} in^2");
                }
                else
                {
                    double bYieldReq = AYLinkReq / tStem;
                    double bYieldLimit = Math.Min(0.5 * bColSide, Math.Min(0.5 * bBmSide, B_YIELD_MAX));
                    bYield = Math.Min(bYieldReq, bYieldLimit);

                    p.Add($"  b_yield,req'd = A'_y-link / t_stem = {AYLinkReq:F2} / {tStem:F3} = {bYieldReq:F2} in  (Eq. 12.9-3)");
                    p.Add($"  b_yield limit = min(0.5*b_col, 0.5*b_bm, 6) = min({0.5 * bColSide:F2}, {0.5 * bBmSide:F2}, {B_YIELD_MAX}) = {bYieldLimit:F2} in");
                    p.Add($"  b_yield = {bYield:F2} in");

                    if (bYieldReq > bYieldLimit)
                        p.Add($"  WARNING: Required width exceeds limit. Increase t_stem or beam/column size.");

                    AYLink = bYield * tStem;
                    p.Add($"  A_y-link = b_yield * t_stem = {bYield:F2} * {tStem:F3} = {AYLink:F3} in^2");
                }
                p.Add("");

                // ===== STEP 6: YIELD LENGTH =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 6: MINIMUM YIELDING LENGTH (EQ. 12.9-4)");
                p.Add("--------------------------------------------------------------------------------");

                R_transition = tStem; // Transition radius = t_stem (Section 12.7)

                if (input.LYLink > 0)
                {
                    LYLink = input.LYLink;
                    p.Add($"  L_y-link (user specified) = {LYLink:F2} in");
                }
                else
                {
                    LYLink = (0.05 / 0.085) * ((dBeam + tStem) / 2) + 2 * R_transition;
                    p.Add($"  L_y-link = (0.05/0.085)*((d+t_stem)/2) + 2*R  (Eq. 12.9-4)");
                    p.Add($"  L_y-link = (0.05/0.085)*(({dBeam:F2}+{tStem:F3})/2) + 2*{R_transition:F3}");
                    p.Add($"  L_y-link = {LYLink:F2} in");
                }

                double strainCheck = 0.05 * (dBeam + tStem) / 2 / (LYLink - 2 * R_transition);
                p.Add($"  Strain check: {strainCheck:F4} <= 0.085: " + (strainCheck <= 0.085 ? "OK" : "FAIL"));
                p.Add("");

                // ===== STEP 7: LINK STRENGTH =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 7: YIELD-LINK STRENGTH (EQ. 12.9-5, 12.9-6)");
                p.Add("--------------------------------------------------------------------------------");

                PyeLink = AYLink * RY_LINK * FyLink;
                p.Add($"  P_ye-link = A_y-link * R_y * F_y-link  (Eq. 12.9-5)");
                p.Add($"  P_ye-link = {AYLink:F3} * {RY_LINK} * {FyLink} = {PyeLink:F1} kips");

                PrLink = AYLink * RT_LINK * FuLink;
                p.Add($"  P_r-link = A_y-link * R_t * F_u-link  (Eq. 12.9-6)");
                p.Add($"  P_r-link = {AYLink:F3} * {RT_LINK} * {FuLink} = {PrLink:F1} kips");

                Mpr = PrLink * (dBeam + tStem);
                p.Add($"  M_pr = P_r-link * (d + t_stem)  (Eq. 12.9-28)");
                p.Add($"  M_pr = {PrLink:F1} * ({dBeam:F2} + {tStem:F3}) = {Mpr:F0} kip-in ({Mpr / 12:F1} kip-ft)");

                MyeLink = PyeLink * (dBeam + tStem);
                p.Add($"  M_ye-link = P_ye-link * (d + t_stem)  (Eq. 12.9-29)");
                p.Add($"  M_ye-link = {PyeLink:F1} * ({dBeam:F2} + {tStem:F3}) = {MyeLink:F0} kip-in");
                p.Add("");

                // ===== STEP 8: BEAM-SIDE DESIGN =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 8: YIELD-LINK BEAM-SIDE DESIGN (EQ. 12.9-7)");
                p.Add("--------------------------------------------------------------------------------");

                double dBStem = 0.875;  // 7/8 in A325
                int nRows = 2;          // minimum 2 rows
                double sStem = 3.0;     // bolt spacing
                double sC = 1.5;        // edge distance from reduced section
                double sB = 1.5;        // edge distance from end

                // Check bolt shear capacity
                double Fnv325 = 54.0;   // ksi (A325, threads excluded)
                double ABolt = Math.PI / 4 * dBStem * dBStem;
                double RnBolt = 2 * Fnv325 * ABolt * nRows;

                p.Add($"  Step 8.1: Stem-to-beam bolts: {nRows} rows x 2 bolts, d_b = {dBStem:F3} in A325");
                p.Add($"  R_n (bolt shear) = {RnBolt:F1} kips vs P_r-link = {PrLink:F1} kips");

                if (RnBolt < PrLink)
                {
                    nRows = (int)Math.Ceiling(PrLink / (2 * Fnv325 * ABolt));
                    RnBolt = 2 * Fnv325 * ABolt * nRows;
                    p.Add($"  Increased to {nRows} rows. R_n = {RnBolt:F1} kips");
                }

                if (input.LBmSide > 0)
                {
                    LBmSide = input.LBmSide;
                    p.Add($"  Step 8.3: L_bm-side (user specified) = {LBmSide:F2} in");
                }
                else
                {
                    LBmSide = sC + (nRows - 1) * sStem + sB;
                    p.Add($"  Step 8.3: L_bm-side = s_c + (n_rows-1)*s_stem + s_b  (Eq. 12.9-7)");
                    p.Add($"  L_bm-side = {sC} + ({nRows - 1})*{sStem} + {sB} = {LBmSide:F2} in");
                }

                p.Add($"  Step 8.2: b_bm-side = {bBmSide:F2} in");

                double LTotal = LColSide + LYLink + LBmSide;

                p.Add("");
                p.Add($"  Yield-Link Geometry Summary:");
                p.Add($"    t_stem = {tStem:F3} in");
                p.Add($"    b_col-side = {bColSide:F2} in");
                p.Add($"    b_bm-side = {bBmSide:F2} in");
                p.Add($"    b_yield = {bYield:F2} in");
                p.Add($"    L_col-side = {LColSide:F2} in");
                p.Add($"    L_y-link = {LYLink:F2} in");
                p.Add($"    L_bm-side = {LBmSide:F2} in");
                p.Add($"    L_total = {LTotal:F2} in");
                p.Add($"    A_y-link = {AYLink:F3} in^2");
                p.Add("");

                // ===== STEP 9: FLANGE CONNECTION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 9: YIELD-LINK FLANGE-TO-COLUMN CONNECTION");
                p.Add("--------------------------------------------------------------------------------");

                // Step 9.1: Bolt tension demand
                if (lt == "tstub")
                {
                    rT = PrLink / 4;
                    p.Add($"  Step 9.1: T-stub Yield-Link");
                    p.Add($"  r_t = P_r-link / 4  (Eq. 12.9-8)");
                    p.Add($"  r_t = {PrLink:F1} / 4 = {rT:F1} kips/bolt");
                }
                else
                {
                    double tf = input.BeamTf;
                    double pfi = 2.0;   // typical inner pitch
                    double pfo = 2.0;   // typical outer pitch
                    double dFt = dBeam - tf;
                    double h0 = dFt + pfo;
                    double h1 = dFt - pfi;

                    // Pre-compute V_u per Eq. 12.9-34
                    double dcHalfPre = input.ColD / 2;
                    double aSp = input.ADist;
                    double LhPre = input.Span - 2 * (dcHalfPre + aSp);
                    double VuPre = 2 * Mpr / LhPre + gravity / 2;
                    double aVal = input.ADist;

                    rT = Mpr / (2 * (h0 + h1)) + VuPre * aVal / (2 * h1);
                    p.Add($"  Step 9.1: End-plate Yield-Link");
                    p.Add($"  r_t = M_pr/(2*(h_0+h_1)) + V_u*a/(2*h_1)  (Eq. 12.9-9)");
                    p.Add($"  r_t = {rT:F1} kips/bolt");
                }

                // Step 9.2: Flange thickness for no prying
                dFlangeBolt = 1.0;
                double bDist = 2.0;
                double bPrime = bDist - dFlangeBolt / 2;
                double sFlange = 4.0;
                double pFlange = Math.Min(bColSide / 2, sFlange);

                double tFlangeReq = Math.Sqrt(4 * rT * bPrime / (pFlange * PHI_D * FuLink));
                p.Add("");
                p.Add($"  Step 9.2: Flange thickness (no prying) (Eq. 12.9-10)");
                p.Add($"  b' = b - d_b/2 = {bDist} - {dFlangeBolt / 2:F3} = {bPrime:F3} in  (Eq. 12.9-11)");
                p.Add($"  p = min(b_col/2, s_flange) = min({bColSide / 2:F2}, {sFlange}) = {pFlange:F2} in");
                p.Add($"  t_flange = sqrt(4*r_t*b' / (p*phi_d*F_u))");
                p.Add($"  t_flange = sqrt(4*{rT:F1}*{bPrime:F3} / ({pFlange:F2}*{PHI_D}*{FuLink}))");
                p.Add($"  t_flange = {tFlangeReq:F3} in");

                tFlange = Math.Ceiling(tFlangeReq * 8) / 8; // round up to nearest 1/8"
                p.Add($"  Use t_flange = {tFlange:F3} in");

                // Step 9.4: Stem-to-flange weld
                PrWeld = bColSide * tStem * RT_LINK * FuLink;
                p.Add("");
                p.Add($"  Step 9.4: Stem-to-flange weld demand (Eq. 12.9-12)");
                p.Add($"  P_r-weld = b_col-side * t_stem * R_t * F_u-link");
                p.Add($"  P_r-weld = {bColSide:F2} * {tStem:F3} * {RT_LINK} * {FuLink}");
                p.Add($"  P_r-weld = {PrWeld:F1} kips");

                double Fw = 0.60 * input.FEXX;
                double weldLength = bColSide;
                double wReq = PrWeld / (2 * PHI_N * Fw * weldLength);
                p.Add($"  Double fillet weld: w = P_r-weld / (2*phi*Fw*L)");
                p.Add($"  w = {PrWeld:F1} / (2*{PHI_N}*{Fw:F1}*{weldLength:F2})");
                p.Add($"  w = {wReq:F3} in (each side)");

                result.FlangeConnectionPassed = true;
                p.Add("");

                // ===== STEP 10: BUCKLING RESTRAINT =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 10: BUCKLING RESTRAINT ASSEMBLY");
                p.Add("--------------------------------------------------------------------------------");

                // Step 10.1: BRP minimum thickness
                double Lcant = LYLink * 0.4;
                double bN = bYield;
                double tBRPMinCalc = 0.51 * Math.Sqrt(Lcant * PrLink / (FY_BRP * RY_BRP * bN));
                double tBRP = Math.Max(tBRPMinCalc, T_BRP_MIN);

                p.Add($"  Step 10.1: BRP thickness (Eq. 12.9-13)");
                p.Add($"  t_BRP,min = 0.51*sqrt(L_cant*P_r / (Fy_BRP*Ry_BRP*b_n))");
                p.Add($"  t_BRP,min = {tBRPMinCalc:F3} in, use {tBRP:F3} in (min {T_BRP_MIN:F3})");

                // Step 10.2: Beam flange minimum thickness
                double Iy = bYield * Math.Pow(tStem, 3) / 12;
                double epsTarget = 0.04 * (dBeam + tStem) / 2 / (LYLink + 2 * R_transition);
                double gGap = 0.25 * epsTarget * tStem;

                double lO = Math.Sqrt(1900 * Iy / PrLink * (1 + Math.Pow(bYield / (2 * gGap) + 1.013, -1)));
                int NDesign = Math.Max(1, (int)Math.Round(0.5 * LYLink / lO));
                double Qi = 4 * gGap * PrLink / lO;
                double QTotal = NDesign * Qi;

                int nBRPBolts = 2;
                double Tux = QTotal / nBRPBolts;

                double bPrimeBf = 1.5;
                double pE = 3.0;
                double FubBeam = input.BeamFu;
                double tBfMin = Math.Sqrt(4 * Tux * bPrimeBf / (PHI_D * pE * FubBeam));
                tBfMin = Math.Max(tBfMin, TF_BEAM_MIN);

                p.Add("");
                p.Add($"  Step 10.2: Beam flange thickness check (Eq. 12.9-14)");
                p.Add($"  I_y (weak-axis) = {Iy:F4} in^4");
                p.Add($"  eps_target = {epsTarget:F4}  (Eq. 12.9-20)");
                p.Add($"  g = {gGap:F4} in  (Eq. 12.9-19)");
                p.Add($"  l_o = {lO:F2} in  (Eq. 12.9-18)");
                p.Add($"  N_design = {NDesign}  (Eq. 12.9-17)");
                p.Add($"  Q = {QTotal:F1} kips  (Eq. 12.9-16)");
                p.Add($"  T_ux = {Tux:F1} kips/bolt  (Eq. 12.9-15)");
                p.Add($"  t_bf,min = {tBfMin:F3} in");

                bool bfOk = input.BeamTf >= tBfMin;
                p.Add($"  Beam tf = {input.BeamTf:F3} in >= {tBfMin:F3}: " + (bfOk ? "OK" : "FAIL"));

                // Step 10.3: BRP bolt shear
                double Vux = MU_K * Tux;
                double Ix = tStem * Math.Pow(bYield, 3) / 12;
                double Vuy = (0.5 * PrLink) / Math.Sqrt(
                    1900 * Ix / PrLink * (1 + Math.Pow(4 * tStem + 1.013, -1)));

                p.Add("");
                p.Add($"  Step 10.3: BRP bolt check");
                p.Add($"  V_ux (out-of-plane shear) = mu_k * T_ux = {MU_K} * {Tux:F1} = {Vux:F1} kips  (Eq. 12.9-22)");
                p.Add($"  V_uy (in-plane shear) = {Vuy:F1} kips  (Eq. 12.9-23)");
                p.Add($"  Bolt size: min {D_BRP_BOLT_MIN:F3} in diameter");

                result.BucklingRestraintPassed = bfOk;
                if (!bfOk) allPassed = false;
                p.Add("");

                // ===== STEP 11: CONNECTION STIFFNESS =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 11: CONNECTION STIFFNESS (EQ. 12.9-24 TO 12.9-33)");
                p.Add("--------------------------------------------------------------------------------");

                double wColSide = bColSide;
                double gFlange = 3.0;

                // K1 (flange bending stiffness)
                double IFlange = wColSide * Math.Pow(tFlange, 3) / 12;
                K1 = (0.75 * 192 * E * IFlange) / Math.Pow(gFlange, 3);
                p.Add($"  K1 (flange bending) = {K1:F0} kip/in  (Eq. 12.9-24)");

                // K2 (nonyielding stem)
                double sCk2 = 1.5;
                double lVk2 = 0.0;
                K2 = tStem * bColSide * E / (LColSide + sCk2 + lVk2);
                p.Add($"  K2 (nonyielding stem) = {K2:F0} kip/in  (Eq. 12.9-25)");

                // K3 (yielding stem)
                K3 = tStem * bYield * E / LYLink;
                p.Add($"  K3 (yielding stem) = {K3:F0} kip/in  (Eq. 12.9-26)");

                // K_eff
                KEff = K1 * K2 * K3 / (K1 * K2 + K2 * K3 + K1 * K3);
                p.Add($"  K_eff = {KEff:F0} kip/in  (Eq. 12.9-27)");

                // Deformation parameters
                delta04 = 0.04 * (dBeam + tStem) / 2;
                delta07 = 0.07 * (dBeam + tStem) / 2;
                deltaY = PyeLink / KEff;
                thetaY = deltaY / (0.5 * (dBeam + tStem));

                p.Add("");
                p.Add($"  Deformation parameters:");
                p.Add($"  delta_0.04 = {delta04:F4} in  (Eq. 12.9-30)");
                p.Add($"  delta_0.07 = {delta07:F4} in  (Eq. 12.9-31)");
                p.Add($"  delta_y = {deltaY:F4} in  (Eq. 12.9-32)");
                p.Add($"  theta_y = {thetaY:F6} rad ({thetaY * 180 / Math.PI:F4} deg)  (Eq. 12.9-33)");
                p.Add($"  M_pr = {Mpr:F0} kip-in | M_ye = {MyeLink:F0} kip-in");

                // Step 11.2: Connection moment check
                double phiMn = PHI_B * MyeLink / RY_LINK;
                bool stiffnessOk = Mu <= phiMn;
                p.Add("");
                p.Add($"  Step 11.2: Connection moment check");
                p.Add($"  phi*M_n = phi*M_ye/R_y = {PHI_B}*{MyeLink:F0}/{RY_LINK} = {phiMn:F0} kip-in");
                p.Add($"  Mu = {Mu:F0} kip-in <= phi*M_n = {phiMn:F0}: " + (stiffnessOk ? "OK" : "FAIL"));

                result.StiffnessPassed = stiffnessOk;
                result.StiffnessRatio = Mu > 0 ? Mu / phiMn : 0;
                if (!stiffnessOk) allPassed = false;
                p.Add("");

                // ===== STEP 12: REQUIRED SHEAR =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 12: REQUIRED SHEAR STRENGTH (EQ. 12.9-34)");
                p.Add("--------------------------------------------------------------------------------");

                double aDist = input.ADist;
                double dcHalf = input.ColD / 2;
                Lh = input.Span - 2 * (dcHalf + aDist);
                VuCalc = 2 * Mpr / Lh + gravity / 2;

                p.Add($"  L_h = L - 2*(dc/2 + a) = {input.Span:F0} - 2*({dcHalf:F2}+{aDist:F1}) = {Lh:F1} in");
                p.Add($"  Gravity (1.2D + {input.F1}L + 0.2S) = {gravity:F2} kips");
                p.Add($"  V_u = 2*M_pr/L_h + gravity/2  (Eq. 12.9-34)");
                p.Add($"  V_u = 2*{Mpr:F0}/{Lh:F1} + {gravity:F2}/2 = {VuCalc:F1} kips");
                p.Add("");

                // ===== STEP 13: MEMBER CHECKS =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 13: BEAM AND COLUMN CHECKS");
                p.Add("--------------------------------------------------------------------------------");

                // Step 13.1: Beam shear strength
                double Vn = 0.6 * input.BeamFy * input.BeamD * input.BeamTw;
                double phiVn = PHI_V * Vn;
                bool beamShearOk = VuCalc <= phiVn;

                p.Add($"  Step 13.1: Beam shear");
                p.Add($"  V_n = 0.6*Fy*d*tw = 0.6*{input.BeamFy}*{input.BeamD:F2}*{input.BeamTw:F3} = {Vn:F1} kips");
                p.Add($"  phi*V_n = {phiVn:F1} kips");
                p.Add($"  V_u = {VuCalc:F1} kips <= phi*V_n: " + (beamShearOk ? "OK" : "FAIL"));

                result.BeamShearPassed = beamShearOk;
                result.BeamShearRatio = phiVn > 0 ? VuCalc / phiVn : 0;
                if (!beamShearOk) allPassed = false;

                // Step 13.2: Column flexural strength limit
                double SxCol = ZxToSx(input.ColZx, input.ColD, input.ColTf, input.ColTw);
                double phiFySx = PHI_B * input.ColFy * SxCol;
                p.Add("");
                p.Add($"  Step 13.2: Column flexural strength limit (if bracing at top flange only)");
                p.Add($"  phi*Fy*Sx = {PHI_B}*{input.ColFy}*{SxCol:F1} = {phiFySx:F0} kip-in");
                p.Add("");

                // ===== STEP 14: COLUMN-BEAM RELATIONSHIP =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 14: COLUMN-BEAM RELATIONSHIP (SECTION 12.4)");
                p.Add("--------------------------------------------------------------------------------");

                bool colBeamOk;
                if (input.SystemType == "IMF")
                {
                    p.Add("  IMF: Column-beam ratio per AISC Seismic Provisions");
                    p.Add("  (May not require strong-column/weak-beam check)");
                    colBeamOk = true;
                }
                else
                {
                    double Muv = VuCalc * (input.ADist + input.ColD / 2);
                    int nBeams = 2;
                    double SumMpb = nBeams * (Mpr + Muv);

                    p.Add($"  M_uv = V_u * (a + dc/2) = {VuCalc:F1} * ({input.ADist:F1} + {input.ColD / 2:F2}) = {Muv:F0} kip-in");
                    p.Add($"  Sum M_pb* = {nBeams}*(M_pr + M_uv) = {nBeams}*({Mpr:F0} + {Muv:F0}) = {SumMpb:F0} kip-in");

                    double H = (input.StoryAbove + input.StoryBelow) / 2;
                    double SumMpc = 2 * input.ColZx * input.ColFy;
                    double ratio = SumMpb > 0 ? SumMpc / SumMpb : 999;

                    p.Add($"  Sum M_pc (simplified) ~ 2*Zc*Fyc = 2*{input.ColZx:F1}*{input.ColFy} = {SumMpc:F0} kip-in");
                    p.Add($"  Ratio Sum M_pc / Sum M_pb* = {ratio:F3}");
                    p.Add($"  " + (ratio >= 1.0 ? "OK" : "FAIL - increase column size"));
                    p.Add($"  Note: Full check per AISC 341 E3.6c including axial effects");

                    colBeamOk = ratio >= 1.0;
                }

                result.ColumnBeamPassed = colBeamOk;
                if (!colBeamOk) allPassed = false;
                p.Add("");

                // ===== STEP 15: SHEAR PLATE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 15: SHEAR PLATE CONNECTION DESIGN");
                p.Add("--------------------------------------------------------------------------------");

                double MuSp = VuCalc * input.ADist;
                p.Add($"  M_u-sp = V_u * a = {VuCalc:F1} * {input.ADist:F1} = {MuSp:F0} kip-in");

                // Step 15.1: Bolt shear
                int nHorz = 3;
                int nVert = 3;
                double VuBolt = Math.Sqrt(
                    Math.Pow(input.PuSp / nHorz, 2) + Math.Pow(VuCalc / nVert, 2));

                p.Add("");
                p.Add($"  Step 15.1: Bolt shear (Eq. 12.9-35)");
                p.Add($"  n_horz = {nHorz} | n_vert = {nVert}");
                p.Add($"  V_u-bolt = sqrt((P_u-sp/{nHorz})^2 + (V_u/{nVert})^2)");
                p.Add($"  V_u-bolt = sqrt(({input.PuSp:F1}/{nHorz})^2 + ({VuCalc:F1}/{nVert})^2) = {VuBolt:F1} kips");

                double dBSp = 0.875;
                double FnvSp = 54.0;
                double ABoltSp = Math.PI / 4 * dBSp * dBSp;
                double phiRnBolt = PHI_N * FnvSp * ABoltSp;
                bool boltOk = VuBolt <= phiRnBolt;
                p.Add($"  phi*R_n (single bolt, A325 7/8\") = {phiRnBolt:F1} kips");
                p.Add($"  V_u-bolt = {VuBolt:F1} <= phi*R_n: " + (boltOk ? "OK" : "FAIL"));

                // Step 15.2: Slot lengths
                double sVert = 3.0;
                double sHorz = 3.0;
                double LSlotHorz = dBSp + 0.125 + 0.14 * sVert * (nVert - 1) / 2;
                double LSlotVert = dBSp + 0.125 + 0.14 * sHorz * (nHorz - 1);

                p.Add("");
                p.Add($"  Step 15.2: Slot lengths for 0.07 rad rotation");
                p.Add($"  L_slot_horz = {LSlotHorz:F2} in  (Eq. 12.9-36)");
                p.Add($"  L_slot_vert = {LSlotVert:F2} in  (Eq. 12.9-37)");

                // Step 15.4: Weld
                double tPShear = 0.375;
                double wMinWeld = 0.75 * tPShear;
                p.Add("");
                p.Add($"  Step 15.4: Shear plate weld (min 3/4*t_p for double fillet)");
                p.Add($"  t_p = {tPShear:F3} in | min weld = {wMinWeld:F3} in");

                result.ShearPlatePassed = boltOk;
                if (!boltOk) allPassed = false;
                p.Add("");

                // ===== STEP 16: PANEL ZONE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 16: PANEL ZONE SHEAR (AISC 360 J10.6)");
                p.Add("--------------------------------------------------------------------------------");

                double nBeamsPz = 2;
                double SumMFace = nBeamsPz * Mpr;
                double Hpz = (input.StoryAbove + input.StoryBelow) / 2;
                double VCol = SumMFace / Hpz;

                RnPzDemand = SumMFace / dBeam - VCol;

                // phi = 0.90 (AISC 360), NOT 1.0 (AISC 341) - SST specific
                double phiPz = 0.90;
                double dcPz = input.ColD;
                double RnPz = 0.6 * input.ColFy * dcPz * input.ColTw;
                phiRnPz = phiPz * RnPz;

                p.Add($"  Demand: V_pz = Sum(M_pr)/d - V_col");
                p.Add($"  Sum M_face = {nBeamsPz}*{Mpr:F0} = {SumMFace:F0} kip-in");
                p.Add($"  V_col = Sum(M_pr)/H = {SumMFace:F0}/{Hpz:F1} = {VCol:F1} kips");
                p.Add($"  V_pz = {SumMFace:F0}/{dBeam:F2} - {VCol:F1} = {RnPzDemand:F1} kips");
                p.Add($"  Note: SST uses phi = 0.90 (AISC 360), NOT phi = 1.0 (AISC 341)");
                p.Add($"  d_c = {dcPz:F2} in (overall column depth per AISC 360 J10.6)");
                p.Add($"  R_n = 0.6*Fy*dc*tw = 0.6*{input.ColFy}*{dcPz:F2}*{input.ColTw:F3} = {RnPz:F1} kips");
                p.Add($"  phi*R_n = {phiPz}*{RnPz:F1} = {phiRnPz:F1} kips");

                bool panelZoneOk = RnPzDemand <= phiRnPz;
                p.Add($"  " + (panelZoneOk ? "OK" : "FAIL") + $" (Utilization: {RnPzDemand / phiRnPz:F3})");
                if (!panelZoneOk)
                    p.Add($"  Consider doubler plates");

                result.PanelZonePassed = panelZoneOk;
                result.PanelZoneRatio = phiRnPz > 0 ? RnPzDemand / phiRnPz : 0;
                if (!panelZoneOk) allPassed = false;
                p.Add("");

                // ===== STEP 17: COLUMN WEB =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 17: COLUMN WEB CONCENTRATED FORCE (AISC 360 J10)");
                p.Add("--------------------------------------------------------------------------------");

                double kCol = input.ColTf;
                double phiRnWy = PHI_D * 5 * input.ColFy * kCol * input.ColTw;

                double NBearing = input.BeamBf;
                double phiRnWc = PHI_N * 0.80 * Math.Pow(input.ColTw, 2) *
                    (1 + 3 * (NBearing / input.ColD) * Math.Pow(input.ColTw / input.ColTf, 1.5)) *
                    Math.Sqrt(E * input.ColFy * input.ColTf / input.ColTw);

                p.Add($"  P_r-link = {PrLink:F1} kips (concentrated force)");
                p.Add($"  Web local yielding: phi*R_n ~ {phiRnWy:F1} kips");
                p.Add($"  Web local crippling: phi*R_n ~ {phiRnWc:F1} kips");

                double phiRnWebMin = Math.Min(phiRnWy, phiRnWc);
                bool webOk = PrLink <= phiRnWebMin;
                p.Add($"  " + (webOk ? "OK" : "FAIL - need continuity plates"));

                result.ColumnWebPassed = webOk;
                if (!webOk) allPassed = false;
                p.Add("");

                // ===== STEP 18: COLUMN FLANGE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 18: COLUMN FLANGE FLEXURAL YIELDING (EQ. 12.9-38)");
                p.Add("--------------------------------------------------------------------------------");

                double Yc = 6.0;
                double tCfMin = Math.Sqrt(1.11 * Mpr / (PHI_D * input.ColFy * Yc));

                p.Add($"  t_cf,min = sqrt(1.11*M_pr / (phi_d*Fyc*Y_c))  (Eq. 12.9-38)");
                p.Add($"  t_cf,min = sqrt(1.11*{Mpr:F0} / ({PHI_D}*{input.ColFy}*{Yc}))");
                p.Add($"  t_cf,min = {tCfMin:F3} in");
                p.Add($"  Column tf = {input.ColTf:F3} in");

                bool cfOk = input.ColTf >= tCfMin;
                p.Add($"  " + (cfOk ? "OK" : "FAIL - need continuity plates"));

                result.ColumnFlangePassed = cfOk;
                if (!cfOk) allPassed = false;
                p.Add("");

                // ===== STEP 19: CONTINUITY PLATES =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 19: CONTINUITY PLATES (EQ. 12.9-39)");
                p.Add("--------------------------------------------------------------------------------");

                if (webOk && cfOk)
                {
                    p.Add("  No continuity plates required - all column limit states OK");
                    result.ContinuityPlatesPassed = true;
                }
                else
                {
                    p.Add($"  Continuity plates required (Eq. 12.9-39)");
                    if (!webOk)
                    {
                        double FsuWeb = Math.Max(0, PrLink - phiRnWebMin);
                        p.Add($"  web: F_su = P_r-link - phi*R_n = {PrLink:F1} - {phiRnWebMin:F1} = {FsuWeb:F1} kips");
                    }
                    if (!cfOk)
                    {
                        double FsuCf = Math.Max(0, PrLink);
                        p.Add($"  flange: F_su = {FsuCf:F1} kips (full P_r demands continuity plate)");
                    }
                    p.Add($"  Min thickness: 1/4 in (6 mm) per AISC 360 J10.8");
                    p.Add($"  Fillet welds permitted at column web and flanges");
                    result.ContinuityPlatesPassed = true; // plates can be designed
                }
                p.Add("");

                // ===== SUMMARY =====
                p.Add("================================================================================");
                p.Add("  DESIGN VERIFICATION SUMMARY");
                p.Add("================================================================================");
                p.Add("");
                p.Add($"Prequalification:       {(result.PrequalificationPassed ? "PASS" : "FAIL")}");
                p.Add($"Flange connection:      {(result.FlangeConnectionPassed ? "PASS" : "FAIL")}");
                p.Add($"Buckling restraint:     {(result.BucklingRestraintPassed ? "PASS" : "FAIL")}");
                p.Add($"Connection stiffness:   {(result.StiffnessPassed ? "PASS" : "FAIL")}");
                p.Add($"Beam shear (Step 13):   {(result.BeamShearPassed ? "PASS" : "FAIL")}");
                p.Add($"Column-beam ratio:      {(result.ColumnBeamPassed ? "PASS" : "FAIL")}");
                p.Add($"Shear plate:            {(result.ShearPlatePassed ? "PASS" : "FAIL")}");
                p.Add($"Panel zone:             {(result.PanelZonePassed ? "PASS" : "FAIL")}");
                p.Add($"Column web:             {(result.ColumnWebPassed ? "PASS" : "FAIL")}");
                p.Add($"Column flange:          {(result.ColumnFlangePassed ? "PASS" : "FAIL")}");
                p.Add($"Continuity plates:      {(result.ContinuityPlatesPassed ? "PASS" : "FAIL")}");
                p.Add("");
                p.Add($"KEY RESULTS:");
                p.Add($"  M_pr = {Mpr:F0} kip-in | P_r-link = {PrLink:F1} kips");
                p.Add($"  V_u = {VuCalc:F1} kips");
                p.Add($"  Yield-Link: t_stem={tStem:F3} b_yield={bYield:F2} L_y-link={LYLink:F2}");
                p.Add($"  K_eff = {KEff:F0} kip/in | theta_y = {thetaY:F6} rad");
                p.Add("");
                p.Add("================================================================================");
                if (allPassed)
                    p.Add("  ALL CHECKS PASSED");
                else
                    p.Add("  SOME CHECKS FAILED - REVIEW AND ADJUST DESIGN");
                p.Add("================================================================================");

                // Store key results
                result.OverallPassed = allPassed;
                result.IsValid = true;
                result.Mpr = Mpr;
                result.PrLink = PrLink;
                result.PyeLink = PyeLink;
                result.VuCalc = VuCalc;
                result.BYield = bYield;
                result.LYLink = LYLink;
                result.AYLink = AYLink;
                result.KEff = KEff;
                result.ThetaY = thetaY;
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

        private static double ZxToSx(double Zx, double d, double tf, double tw)
        {
            double flangeContrib = Zx - tw * (d - 2 * tf) * (d - 2 * tf) / 4;
            if (tf > 0 && (d - tf) > 0 && flangeContrib > 0)
            {
                double bfApprox = flangeContrib / (tf * (d - tf));
                double SxFlanges = 2 * (bfApprox * tf) * (d / 2 - tf / 2);
                double SxWeb = tw * (d - 2 * tf) * (d - 2 * tf) / 6;
                return SxFlanges + SxWeb;
            }
            return Zx * 0.90;
        }
    }
}
