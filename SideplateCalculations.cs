using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    /// <summary>
    /// SidePlate Moment Connection Design Verification
    /// AISC 358-16 Chapter 11 - Prequalified Connections for Seismic Applications
    /// Implements Steps 1-5 and 8 (EOR's responsibilities)
    /// Steps 6-7 (detailed component design) are performed by SidePlate Systems Inc.
    /// </summary>
    public static class SideplateCalculations
    {
        // ====================== CONSTANTS ======================
        public const double PHI_D = 1.00;   // Ductile limit states (AISC 358 Section 2.4.1)
        public const double PHI_V = 0.90;   // Shear (AISC 360 G2.1(a))
        public const double E = 29000.0;    // Modulus of elasticity (ksi)

        // Default material properties
        public const double DEFAULT_FY_BEAM = 50.0;    // ksi (A992)
        public const double DEFAULT_FU_BEAM = 65.0;    // ksi (A992)
        public const double DEFAULT_FY_COLUMN = 50.0;  // ksi (A992)
        public const double DEFAULT_FU_COLUMN = 65.0;  // ksi (A992)

        // SidePlate connection plate material (Section 11.3.3(1))
        public const double FY_PLATE = 50.0;  // ksi (ASTM A572 Gr 50)

        // Prequalification limits (Section 11.3)
        public const int MAX_BEAM_DEPTH_WELDED = 40;       // W40 max for field-welded (Section 11.3.1(2))
        public const int MAX_BEAM_DEPTH_BOLTED = 44;       // W44 max for field-bolted (Section 11.3.1(2))
        public const int MAX_BEAM_WEIGHT_WELDED = 302;     // lb/ft (Section 11.3.1(4))
        public const int MAX_BEAM_WEIGHT_BOLTED = 400;     // lb/ft (Section 11.3.1(4))
        public const double MAX_BEAM_FLANGE_AREA_BOLTED = 36.0; // in^2 (Section 11.3.1(4))
        public const double MAX_BEAM_TF = 2.5;             // in (Section 11.3.1(1))
        public const int MAX_HSS_DEPTH_SMF = 14;           // HSS14 max for SMF (Section 11.3.1(3a))
        public const int MAX_HSS_DEPTH_IMF = 16;           // HSS16 max for IMF (Section 11.3.1(3b))
        public const int MAX_COLUMN_DEPTH = 44;            // W44 max (Section 11.3.2(4))
        public const double MAX_BOX_WIDTH = 33.0;          // in (Section 11.3.2(4))
        public const double MAX_BOLT_DIAMETER = 1.375;     // 1-3/8 in (Section 11.6.3(5))

        // Side plate extension limits (Section 11.3.3(2))
        public const double MIN_EXTENSION_RATIO = 0.65;   // Both types
        public const double MAX_EXTENSION_WELDED = 1.0;   // d for field-welded
        public const double MAX_EXTENSION_BOLTED = 1.7;   // 1.7d for field-bolted

        // L_h/d limits (Section 11.3.1(5))
        public const double LH_D_MIN_WELDED_RECT = 6.0;  // Rectangular cover plates, SMF
        public const double LH_D_MIN_WELDED_U = 4.5;     // U-shaped cover plates, SMF, field-welded
        public const double LH_D_MIN_BOLTED_U = 4.0;     // U-shaped cover plates, SMF, field-bolted
        public const double LH_D_MIN_IMF = 3.0;           // IMF

        // Plastic hinge distance from end of side plate extension (Section 11.3.1(5))
        public const double HINGE_RATIO_WELDED = 0.333;   // d/3
        public const double HINGE_RATIO_BOLTED = 0.165;   // d/6

        // Strong-column weak-beam ratio (Section 11.7 Step 1)
        public const double SCWB_PRELIM_RATIO = 1.7;

        // ====================== DATA CLASSES ======================

        public class InputParameters
        {
            // Beam properties
            public string BeamDesignation = "";
            public double BeamD  = 36.0;    // Depth (in)
            public double BeamBf = 12.0;    // Flange width (in)
            public double BeamTf = 0.94;    // Flange thickness (in)
            public double BeamTw = 0.625;   // Web thickness (in)
            public double BeamZx = 580.0;   // Plastic section modulus (in^3)
            public double BeamFy = 50.0;    // ksi (A992)
            public double BeamFu = 65.0;    // ksi (A992)
            public double BeamRy = 1.1;

            // Column properties
            public string ColDesignation = "";
            public double ColD  = 17.2;     // Depth (in)
            public double ColBf = 16.0;     // Flange width (in)
            public double ColTf = 1.68;     // Flange thickness (in)
            public double ColTw = 0.96;     // Web thickness (in)
            public double ColZx = 600.0;    // Plastic section modulus (in^3)
            public double ColFy = 50.0;     // ksi (A992)
            public double ColFu = 65.0;     // ksi (A992)
            public double ColRy = 1.1;

            // Connection type: "welded" or "bolted"
            public string ConnectionType = "welded";

            // Design parameters
            public double Span = 360.0;         // Span between column centerlines (in)
            public string SystemType = "SMF";   // SMF or IMF
            public double ExtensionA = 0.0;     // Side plate extension (in), 0 = auto (0.77d)
            public double StoryHeight = 168.0;  // Story height (in), default 14 ft
            public double ColD2 = 0.0;          // Opposite column depth (in), 0 = same as column

            // Gravity loads
            public double LoadD = 0.0;  // Total dead load on span (kips)
            public double LoadL = 0.0;  // Total live load on span (kips)
            public double LoadS = 0.0;  // Total snow load on span (kips)
            public double F1   = 0.5;   // Live load factor

            // Column axial load (for SCWB reduction)
            public double PuCol = 0.0;  // Column axial load P_uc (kips)
            public double AsCol = 0.0;  // Column gross area A_s (in^2), 0 = auto
        }

        public class DesignResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = "";
            public List<string> Process { get; set; } = new();

            public bool OverallPassed { get; set; }

            // Individual checks
            public bool PrequalificationPassed { get; set; }
            public bool BeamPrequalificationPassed { get; set; }
            public bool ColumnPrequalificationPassed { get; set; }
            public bool ExtensionRangePassed { get; set; }
            public bool LhRatioPassed { get; set; }
            public bool ScwbRatioPassed { get; set; }
            public bool BeamShearPassed { get; set; }
            public bool PanelZonePassed { get; set; }

            // Key results
            public double Cpr { get; set; }
            public double Mpr { get; set; }        // kip-in
            public double Mf  { get; set; }        // kip-in (moment at column face)
            public double Mcl { get; set; }        // kip-in (moment at column CL)
            public double Vh  { get; set; }        // kips (shear at hinge)
            public double Sh  { get; set; }        // in (distance from column CL to hinge)
            public double Lh  { get; set; }        // in (hinge-to-hinge span)
            public double ExtensionA { get; set; } // in (side plate extension)
            public double HingeDist { get; set; }  // in (hinge distance from plate end)
            public double ScwbRatio { get; set; }

            // Utilization ratios
            public double BeamShearRatio { get; set; }
            public double PanelZoneRatio { get; set; }
            public double GeometricCompatRatio { get; set; }
        }

        // ====================== MAIN CALCULATE ======================

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var p = result.Process;
            bool allPassed = true;

            try
            {
                // --- Determine connection type ---
                bool isWelded = input.ConnectionType.ToLower().Contains("weld");
                bool isBolted = input.ConnectionType.ToLower().Contains("bolt");

                if (!isWelded && !isBolted)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Invalid connection type: {input.ConnectionType}. Use 'welded' or 'bolted'.";
                    p.Add($"ERROR: {result.ErrorMessage}");
                    return result;
                }

                string connLabel = isWelded ? "Field-welded" : "Field-bolted";

                // --- Derived properties ---
                double hingeRatio = isWelded ? HINGE_RATIO_WELDED : HINGE_RATIO_BOLTED;
                string hingeLabel = isWelded ? "d/3" : "d/6";
                double hingeDist = hingeRatio * input.BeamD;

                // Side plate extension (default 0.77d per Step 2 commentary)
                double extensionA = input.ExtensionA > 0 ? input.ExtensionA : 0.77 * input.BeamD;

                // Second column depth
                double colD2 = input.ColD2 > 0 ? input.ColD2 : input.ColD;

                // Column gross area
                double asCol = input.AsCol > 0 ? input.AsCol
                    : 2 * input.ColBf * input.ColTf + (input.ColD - 2 * input.ColTf) * input.ColTw;

                // S_h: distance from column centerline to plastic hinge
                double Sh = input.ColD / 2.0 + extensionA + hingeDist;

                // L_h: hinge-to-hinge span (Eq. 11.3-1a or 11.3-1b)
                double Lh = input.Span - 0.5 * input.ColD - 0.5 * colD2 - 2 * hingeDist - 2 * extensionA;

                // C_pr per Section 2.4.3 (Eq. 2.4-2)
                double Cpr = Math.Min((input.BeamFy + input.BeamFu) / (2 * input.BeamFy), 1.2);

                // M_pr (Eq. 2.4-1)
                double Mpr = Cpr * input.BeamRy * input.BeamFy * input.BeamZx;

                // V_h (Eq. 11.4-3)
                double gravityCombination = 1.2 * input.LoadD + input.F1 * input.LoadL + 0.2 * input.LoadS;
                double Vgravity = gravityCombination / 2.0;
                double Vh = Lh > 0 ? 2 * Mpr / Lh + Vgravity : 0;

                // Beam derived
                double beamWeight = ParseWeight(input.BeamDesignation);
                double beamNominalDepth = ParseNominalDepth(input.BeamDesignation);
                double beamFlangeArea = 2 * input.BeamBf * input.BeamTf;
                double colNominalDepth = ParseNominalDepth(input.ColDesignation);

                // Store key results
                result.Cpr = Cpr;
                result.Mpr = Mpr;
                result.Vh = Vh;
                result.Sh = Sh;
                result.Lh = Lh;
                result.ExtensionA = extensionA;
                result.HingeDist = hingeDist;

                // ==================== HEADER ====================
                p.Add("================================================================================");
                p.Add("  AISC 358-16 CHAPTER 11: SIDEPLATE MOMENT CONNECTION");
                p.Add("================================================================================");
                p.Add($"  Beam: {input.BeamDesignation}    Column: {input.ColDesignation}");
                p.Add($"  Connection: {connLabel}    System: {input.SystemType}");
                p.Add($"  Span: {input.Span:F1} in ({input.Span / 12:F1} ft)");
                p.Add($"  Story height H = {input.StoryHeight:F1} in ({input.StoryHeight / 12:F1} ft)");
                p.Add($"  Side plate extension A = {extensionA:F2} in ({extensionA / input.BeamD:F3}d)");
                p.Add("");

                // ==================== STEP 1: PRELIMINARY CHECKS ====================
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 1: GEOMETRIC COMPATIBILITY & PRELIMINARY CHECK");
                p.Add("--------------------------------------------------------------------------------");

                // Geometric compatibility (Eqs. 11.4-1a or 11.4-1b)
                double requiredBcf;
                string eqNum;
                if (isWelded)
                {
                    // Eq. 11.4-1a: b_bf + 1.1*t_bf + 0.5 <= b_cf
                    requiredBcf = input.BeamBf + 1.1 * input.BeamTf + 0.5;
                    eqNum = "11.4-1a";
                }
                else
                {
                    // Eq. 11.4-1b: b_bf + 1.0 <= b_cf
                    requiredBcf = input.BeamBf + 1.0;
                    eqNum = "11.4-1b";
                }

                bool geomOK = input.ColBf >= requiredBcf;
                p.Add($"  Beam: {input.BeamDesignation}  (d={input.BeamD:F2}, bf={input.BeamBf:F3}, tf={input.BeamTf:F3})");
                p.Add($"  Column: {input.ColDesignation}  (d={input.ColD:F2}, bf={input.ColBf:F3})");
                p.Add($"  Connection: {connLabel}");
                p.Add($"  System: {input.SystemType}");
                p.Add($"  Span L = {input.Span:F1} in");
                p.Add($"  Story height H = {input.StoryHeight:F1} in");
                p.Add($"  Side plate extension A = {extensionA:F2} in ({extensionA / input.BeamD:F3}d)");
                p.Add("");
                p.Add($"  Eq. {eqNum}: Geometric compatibility");
                p.Add($"    Required b_cf >= {requiredBcf:F3} in");
                p.Add($"    Available b_cf = {input.ColBf:F3} in");
                p.Add($"    {(geomOK ? "PASS" : "FAIL")}: Geometric compatibility (Eq. {eqNum})");
                if (!geomOK) allPassed = false;
                result.GeometricCompatRatio = requiredBcf / input.ColBf;

                // Side plate extension range check (Section 11.3.3(2))
                double minExt = MIN_EXTENSION_RATIO * input.BeamD;
                double maxExt = isWelded ? MAX_EXTENSION_WELDED * input.BeamD : MAX_EXTENSION_BOLTED * input.BeamD;
                double maxRatio = isWelded ? MAX_EXTENSION_WELDED : MAX_EXTENSION_BOLTED;
                bool extOK = minExt <= extensionA && extensionA <= maxExt;

                p.Add($"");
                p.Add($"  Side plate extension range: {minExt:F2} to {maxExt:F2} in");
                p.Add($"    {(extOK ? "PASS" : "FAIL")}: Side plate extension = {extensionA:F2} in ({extensionA / input.BeamD:F3}d) in range [{MIN_EXTENSION_RATIO}d, {maxRatio}d]");
                if (!extOK) allPassed = false;
                result.ExtensionRangePassed = extOK;

                // Preliminary column-beam moment ratio for SMF (Eq. 11.7-1)
                bool prelimScwbOK = true;
                if (input.SystemType == "SMF")
                {
                    double colCap = 2 * input.ColFy * input.ColZx;
                    double beamCap = input.BeamFy * input.BeamZx;
                    double ratio = beamCap > 0 ? colCap / beamCap : double.PositiveInfinity;

                    p.Add($"");
                    p.Add($"  Eq. 11.7-1: Preliminary column-beam moment ratio (SMF)");
                    p.Add($"    Sum(F_yc * Z_c) = 2 * {input.ColFy} * {input.ColZx:F1} = {colCap:F0} kip-in");
                    p.Add($"    Sum(F_yb * Z_b) = {input.BeamFy} * {input.BeamZx:F1} = {beamCap:F0} kip-in");
                    p.Add($"    Ratio = {colCap:F0} / {beamCap:F0} = {ratio:F3}");
                    p.Add($"    Required > {SCWB_PRELIM_RATIO}");
                    prelimScwbOK = ratio > SCWB_PRELIM_RATIO;
                    p.Add($"    {(prelimScwbOK ? "PASS" : "FAIL")}: Preliminary SCWB ratio = {ratio:F3} > {SCWB_PRELIM_RATIO}");
                    if (!prelimScwbOK) allPassed = false;
                }
                p.Add("");

                // ==================== STEP 2: FRAME MODELING ====================
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 2: FRAME MODELING PARAMETERS");
                p.Add("--------------------------------------------------------------------------------");

                p.Add($"  Per Section 11.7 Step 2:");
                p.Add($"    - Use 100% rigid offset in panel zone");
                p.Add($"    - Increase beam I, S, Z by ~3x for distance ~0.77d = {0.77 * input.BeamD:F2} in");
                p.Add($"    - Beyond column face (approx. side plate extension)");
                p.Add($"  Note: Side plate extension A = {extensionA:F2} in = {extensionA / input.BeamD:F3}d");

                if (beamWeight > 200 && isBolted)
                {
                    p.Add($"");
                    p.Add($"  WARNING: Heavy beam ({beamWeight:F0} plf) may require extension up to 1.7d = {1.7 * input.BeamD:F2} in");
                }
                p.Add("");

                // ==================== STEP 3: BEAM PREQUALIFICATION ====================
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 3: BEAM PREQUALIFICATION LIMITS (Section 11.3.1)");
                p.Add("--------------------------------------------------------------------------------");

                bool beamPassed = true;

                // Beam depth limit
                int maxDepth = isWelded ? MAX_BEAM_DEPTH_WELDED : MAX_BEAM_DEPTH_BOLTED;
                string depthLabel = isWelded ? "W40" : "W44";
                bool depthOK = beamNominalDepth <= maxDepth;
                p.Add($"  {(depthOK ? "PASS" : "FAIL")}: Beam depth = {beamNominalDepth:F0} <= {depthLabel} ({maxDepth})");
                if (!depthOK) beamPassed = false;

                // Beam weight limit
                int maxWeight = isWelded ? MAX_BEAM_WEIGHT_WELDED : MAX_BEAM_WEIGHT_BOLTED;
                bool weightOK = beamWeight <= maxWeight;
                p.Add($"  {(weightOK ? "PASS" : "FAIL")}: Beam weight = {beamWeight:F0} plf <= {maxWeight} plf");
                if (!weightOK) beamPassed = false;

                // Beam flange thickness limit (Section 11.3.1(1))
                bool tfOK = input.BeamTf <= MAX_BEAM_TF;
                p.Add($"  {(tfOK ? "PASS" : "FAIL")}: Beam flange thickness = {input.BeamTf:F3} in <= {MAX_BEAM_TF} in");
                if (!tfOK) beamPassed = false;

                // Beam flange area (field-bolted only, Section 11.3.1(4))
                if (isBolted)
                {
                    bool faOK = beamFlangeArea <= MAX_BEAM_FLANGE_AREA_BOLTED;
                    p.Add($"  {(faOK ? "PASS" : "FAIL")}: Beam flange area = {beamFlangeArea:F2} in^2 <= {MAX_BEAM_FLANGE_AREA_BOLTED} in^2");
                    if (!faOK) beamPassed = false;
                }

                // L_h/d ratio (Section 11.3.1(5))
                bool lhdOK = true;
                if (Lh > 0)
                {
                    double lhd = Lh / input.BeamD;
                    double minLhd;
                    string lhdLabel;
                    if (input.SystemType == "SMF")
                    {
                        if (isWelded)
                        {
                            minLhd = LH_D_MIN_WELDED_U;  // U-shaped is typical
                            lhdLabel = "4.5";
                        }
                        else
                        {
                            minLhd = LH_D_MIN_BOLTED_U;
                            lhdLabel = "4.0";
                        }
                    }
                    else  // IMF
                    {
                        minLhd = LH_D_MIN_IMF;
                        lhdLabel = "3.0";
                    }

                    lhdOK = lhd >= minLhd;
                    string coverNote = input.SystemType == "SMF" ? " (Assuming U-shaped cover plates)" : "";
                    p.Add($"  {(lhdOK ? "PASS" : "FAIL")}: L_h/d = {lhd:F2} >= {lhdLabel} ({input.SystemType}{coverNote})");
                    if (!lhdOK) beamPassed = false;
                }
                else
                {
                    p.Add($"    L_h = {Lh:F2} in (negative - CHECK GEOMETRY)");
                    p.Add($"  FAIL: L_h = {Lh:F2} in must be positive");
                    beamPassed = false;
                }

                result.BeamPrequalificationPassed = beamPassed;
                if (!beamPassed) allPassed = false;
                result.LhRatioPassed = lhdOK;
                p.Add("");

                // ==================== STEP 4: COLUMN PREQUALIFICATION ====================
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 4: COLUMN PREQUALIFICATION LIMITS (Section 11.3.2)");
                p.Add("--------------------------------------------------------------------------------");

                bool colDepthOK = colNominalDepth <= MAX_COLUMN_DEPTH;
                p.Add($"  {(colDepthOK ? "PASS" : "FAIL")}: Column depth = {colNominalDepth:F0} <= W44 ({MAX_COLUMN_DEPTH})");
                p.Add($"    No column weight limit per Section 11.3.2(5)");

                result.ColumnPrequalificationPassed = colDepthOK;
                if (!colDepthOK) allPassed = false;
                p.Add("");

                // ==================== STEP 5: DESIGN FORCES ====================
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 5: DESIGN FORCES (Section 11.7)");
                p.Add("--------------------------------------------------------------------------------");

                p.Add($"  C_pr = min((Fy+Fu)/(2*Fy), 1.2)");
                p.Add($"       = min(({input.BeamFy}+{input.BeamFu})/(2*input.BeamFy), 1.2)");
                p.Add($"       = min({(input.BeamFy + input.BeamFu) / (2 * input.BeamFy):F3}, 1.2) = {Cpr:F3}");
                p.Add($"  Note: SidePlate proprietary C_pr ranges from 1.15 to 1.35 (per User Note, Section 11.7)");

                p.Add($"");
                p.Add($"  M_pr = C_pr * Ry * Fy * Zx  (Eq. 2.4-1)");
                p.Add($"       = {Cpr:F3} * {input.BeamRy} * {input.BeamFy} * {input.BeamZx:F1}");
                p.Add($"       = {Mpr:F0} kip-in");

                p.Add($"");
                p.Add($"  Plastic hinge location:");
                p.Add($"    Hinge ratio = {hingeLabel} = {hingeDist:F2} in from end of side plate");
                p.Add($"    Side plate extension A = {extensionA:F2} in");
                p.Add($"    S_h = dc/2 + A + {hingeLabel} = {input.ColD / 2:F2} + {extensionA:F2} + {hingeDist:F2}");
                p.Add($"        = {Sh:F2} in (from column CL)");

                string lhEqNum = isWelded ? "11.3-1a" : "11.3-1b";
                p.Add($"");
                p.Add($"  L_h = L - dc1/2 - dc2/2 - 2*{hingeLabel} - 2*A  (Eq. {lhEqNum})");
                p.Add($"      = {input.Span} - {input.ColD / 2:F2} - {colD2 / 2:F2} - 2*{hingeDist:F2} - 2*{extensionA:F2}");
                p.Add($"      = {Lh:F2} in");

                p.Add($"");
                p.Add($"  V_h = 2*M_pr/L_h + V_gravity  (Eq. 11.4-3)");
                p.Add($"    V_gravity = (1.2D + f1*L + 0.2S) / 2 = {gravityCombination:F2} / 2 = {Vgravity:F2} kips");
                if (Lh > 0)
                {
                    p.Add($"    V_h = 2*{Mpr:F0}/{Lh:F2} + {Vgravity:F2}");
                }
                else
                {
                    p.Add($"    ERROR: L_h <= 0");
                }
                p.Add($"    V_h = {Vh:F2} kips");

                // M_f at column face
                double Mf = Mpr + Vh * (Sh - input.ColD / 2.0);
                result.Mf = Mf;
                p.Add($"");
                p.Add($"  M_f at column face = M_pr + V_h * (S_h - dc/2)");
                p.Add($"                     = {Mpr:F0} + {Vh:F2} * {Sh - input.ColD / 2.0:F2}");
                p.Add($"                     = {Mf:F0} kip-in");

                // Moment at column CL (for SCWB)
                double Mcl = Mpr + Vh * Sh;
                result.Mcl = Mcl;
                p.Add($"");
                p.Add($"  M at column CL = M_pr + V_h * S_h");
                p.Add($"                 = {Mpr:F0} + {Vh:F2} * {Sh:F2}");
                p.Add($"                 = {Mcl:F0} kip-in");
                p.Add("");

                // ==================== STEP 6: COLUMN-BEAM MOMENT RATIO ====================
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 6: COLUMN-BEAM MOMENT RATIO (Section 11.4)");
                p.Add("--------------------------------------------------------------------------------");

                // Sum M_pb* (Eq. 11.4-2)
                double Mv = Vh * Sh;
                double MpbStarSingle = 1.1 * input.BeamRy * input.BeamFy * input.BeamZx + Mv;
                double SumMpb = 2 * MpbStarSingle;  // Assume two-sided (conservative)

                p.Add($"  Eq. 11.4-2: Sum M_pb* (one beam, projected to column CL)");
                p.Add($"    1.1*Ry*Fy*Zb = 1.1 * {input.BeamRy} * {input.BeamFy} * {input.BeamZx:F1} = {1.1 * input.BeamRy * input.BeamFy * input.BeamZx:F0} kip-in");
                p.Add($"    M_v = V_h * S_h = {Vh:F2} * {Sh:F2} = {Mv:F0} kip-in");
                p.Add($"    M_pb* (single beam) = {1.1 * input.BeamRy * input.BeamFy * input.BeamZx:F0} + {Mv:F0} = {MpbStarSingle:F0} kip-in");

                double SumMpbOne = MpbStarSingle;
                p.Add($"");
                p.Add($"    Sum M_pb* (one-sided) = {SumMpbOne:F0} kip-in");
                p.Add($"    Sum M_pb* (two-sided) = 2 x {MpbStarSingle:F0} = {SumMpb:F0} kip-in");

                // Sum M_pc* (Eq. 11.4-4 for uniaxial, wide-flange column)
                // Z_ec per Eq. 11.4-5: Z_ec = Z_c * H / H_h
                double H = input.StoryHeight;
                double Hh = H - input.ColD / 2.0;
                double Zec;
                if (Hh <= 0)
                {
                    p.Add($"");
                    p.Add($"  WARNING: H_h = {Hh:F2} in <= 0 (story height too small)");
                    Zec = input.ColZx;
                }
                else
                {
                    Zec = input.ColZx * H / Hh;
                }

                // Reduction for axial load
                double axialReduction = asCol > 0 ? input.PuCol / asCol : 0;

                double MpcStarSingle = Zec * (input.ColFy - axialReduction);
                if (MpcStarSingle < 0) MpcStarSingle = 0;

                double SumMpc = 2 * MpcStarSingle;  // Column above + below

                p.Add($"");
                p.Add($"  Eq. 11.4-4 & 11.4-5: Sum M_pc* (uniaxial, wide-flange column)");
                p.Add($"    H = {H:F1} in");
                p.Add($"    H_h = H - dc/2 = {H:F1} - {input.ColD / 2:F2} = {Hh:F2} in");
                p.Add($"    Z_ec = Z_c * H / H_h = {input.ColZx:F1} * {H:F1} / {Hh:F2} = {Zec:F1} in^3");
                if (axialReduction > 0)
                {
                    p.Add($"    P_uc/A_s = {input.PuCol:F1}/{asCol:F1} = {axialReduction:F2} ksi");
                }
                p.Add($"    M_pc* (single face) = Z_ec * (F_yc - P_uc/A_s)");
                p.Add($"                        = {Zec:F1} * ({input.ColFy:F1} - {axialReduction:F2})");
                p.Add($"                        = {MpcStarSingle:F0} kip-in");
                p.Add($"    Sum M_pc* = 2 x {MpcStarSingle:F0} = {SumMpc:F0} kip-in");

                // Check ratio (one-sided)
                double ratioOne = SumMpbOne > 0 ? SumMpc / SumMpbOne : double.PositiveInfinity;
                p.Add($"");
                p.Add($"  Column-beam moment ratio (one-sided):");
                p.Add($"    Sum M_pc* / Sum M_pb* = {SumMpc:F0} / {SumMpbOne:F0} = {ratioOne:F3}");
                p.Add($"    Required >= 1.0 per AISC 341 E3.4a ({input.SystemType})");

                bool scwbOK = ratioOne >= 1.0;
                p.Add($"    {(scwbOK ? "PASS" : "FAIL")}: SCWB ratio (one-sided) = {ratioOne:F3} >= 1.0 ({input.SystemType})");
                if (!scwbOK) allPassed = false;

                result.ScwbRatioPassed = scwbOK;
                result.ScwbRatio = ratioOne;

                // Also show two-sided for reference
                if (SumMpb > 0)
                {
                    double ratioTwo = SumMpc / SumMpb;
                    p.Add($"");
                    p.Add($"  Reference (two-sided): {ratioTwo:F3}");
                }
                p.Add("");

                // ==================== STEP 7: BEAM SHEAR ====================
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 7: BEAM SHEAR STRENGTH (AISC 360 G2.1)");
                p.Add("--------------------------------------------------------------------------------");

                // Shear area for rolled W-shapes
                double Aw = input.BeamD * input.BeamTw;

                // Nominal shear strength (AISC 360 G2.1(a) for rolled shapes)
                double Vn = 0.6 * input.BeamFy * Aw;
                double phiVn = PHI_V * Vn;

                double Vu = Vh;

                p.Add($"  V_u = V_h = {Vu:F2} kips");
                p.Add($"  A_w = d * tw = {input.BeamD:F2} * {input.BeamTw:F3} = {Aw:F3} in^2");
                p.Add($"  V_n = 0.6 * Fy * Aw = 0.6 * {input.BeamFy} * {Aw:F3} = {Vn:F1} kips");
                p.Add($"  phi*V_n = {PHI_V} * {Vn:F1} = {phiVn:F1} kips");

                bool beamShearOK = Vu <= phiVn;
                double beamShearRatio = phiVn > 0 ? Vu / phiVn : double.PositiveInfinity;
                p.Add($"  V_u / phi*V_n = {Vu:F2} / {phiVn:F1} = {beamShearRatio:F3}");
                p.Add($"  {(beamShearOK ? "PASS" : "FAIL")}: Beam shear: V_u = {Vu:F2} <= phi*V_n = {phiVn:F1} kips");

                result.BeamShearPassed = beamShearOK;
                result.BeamShearRatio = beamShearRatio;
                if (!beamShearOK) allPassed = false;
                p.Add("");

                // ==================== STEP 8: PANEL ZONE ====================
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 8: PANEL ZONE CHECK (Section 11.4(2), AISC 360 J10.6)");
                p.Add("--------------------------------------------------------------------------------");

                // Panel zone shear demand
                double dbEff = input.BeamD;  // approximate
                double Vpz = dbEff > 0 ? Mcl / dbEff : 0;

                // Panel zone capacity with side plates as doublers
                double dc = input.ColD;
                double twc = input.ColTw;
                double bfc = input.ColBf;
                double tfc = input.ColTf;
                double dSp = input.BeamD;  // approximate side plate depth

                double bfTf2 = bfc * tfc * tfc;
                double denom = dSp * dc * twc;
                double factor = denom > 0 ? 3 * bfTf2 / denom : 0;
                double RnBase = 0.6 * input.ColFy * dc * twc * (1 + factor);

                p.Add($"  Panel zone shear demand (at column CL):");
                p.Add($"    V_pz = M_cl / d = {Mcl:F0} / {dbEff:F2} = {Vpz:F1} kips");
                p.Add($"");
                p.Add($"  Column web panel zone capacity (AISC 360 Eq. J10-11):");
                p.Add($"    d_c = {dc:F2} in, t_wc = {twc:F3} in");
                p.Add($"    b_fc = {bfc:F3} in, t_fc = {tfc:F3} in");
                p.Add($"    d_sp (approx) = {dSp:F2} in");
                p.Add($"    R_n = 0.6*Fy*dc*tw*(1 + 3*bf*tf^2/(d_sp*dc*tw))");
                p.Add($"         = 0.6*{input.ColFy}*{dc:F2}*{twc:F3}*(1 + {factor:F4})");
                p.Add($"         = {RnBase:F1} kips");

                p.Add($"");
                p.Add($"  Note: Side plates {{A}} significantly strengthen panel zone (min 3 panel zones)");
                p.Add($"        Final panel zone design by SidePlate Systems Inc. (Steps 6-7)");

                // Preliminary information only; side plates act as doubler plates
                if (Vpz > RnBase)
                {
                    p.Add($"");
                    p.Add($"  [INFO] Bare column web insufficient (V_pz = {Vpz:F1} > R_n = {RnBase:F1} kips)");
                    p.Add($"         Side plates {{A}} will act as doubler plates to provide remaining capacity.");
                }
                else
                {
                    p.Add($"");
                    p.Add($"  [INFO] Bare column web is adequate on its own (V_pz = {Vpz:F1} <= R_n = {RnBase:F1} kips)");
                }

                // Panel zone capacity guaranteed by SidePlate proprietary design
                p.Add($"  PASS: Panel zone capacity (Guaranteed by SidePlate proprietary design in Steps 6-7)");

                result.PanelZonePassed = true;
                result.PanelZoneRatio = RnBase > 0 ? Vpz / RnBase : 0;
                p.Add("");

                // ==================== M_group CALCULATION ====================
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  M_group CALCULATION (Eq. 11.7-2, Reference)");
                p.Add("--------------------------------------------------------------------------------");

                double xFace = extensionA + hingeDist;  // from hinge to column face
                double MgroupFace = Mpr + Vh * xFace;

                double xCl = Sh;  // from hinge to column CL
                double MgroupCl = Mpr + Vh * xCl;

                p.Add($"  Eq. 11.7-2: M_group = M_pr + V_u * x");
                p.Add($"  Eq. 11.7-3: M_pr = C_pr * Ry * Fy * Zx = {Mpr:F0} kip-in");
                p.Add($"  Eq. 11.7-4: V_u = 2*M_pr/L_h + V_gravity = {Vh:F2} kips");
                p.Add($"");
                p.Add($"  At column face (x = {xFace:F2} in):");
                p.Add($"    M_group = {Mpr:F0} + {Vh:F2} * {xFace:F2} = {MgroupFace:F0} kip-in");
                p.Add($"  At column CL (x = {xCl:F2} in):");
                p.Add($"    M_group = {Mpr:F0} + {Vh:F2} * {xCl:F2} = {MgroupCl:F0} kip-in");
                p.Add("");

                // ==================== STEPS 6-7 NOTE ====================
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEPS 6-7: CONNECTION COMPONENT DESIGN (By SidePlate Systems Inc.)");
                p.Add("--------------------------------------------------------------------------------");

                p.Add($"  Per Section 11.7, Steps 6 and 7 are performed by SidePlate Systems Inc.");
                p.Add($"  The proprietary design includes:");
                p.Add($"    - Side plate {{A}} thickness (Eq. C-11.7-1)");
                p.Add($"    - Cover plate {{B}} thickness (Eq. C-11.7-3)");
                p.Add($"    - VSE thickness (Eq. C-11.7-4)");
                p.Add($"    - HSP thickness (Eq. C-11.7-5)");
                p.Add($"    - Weld group sizing (ultimate strength approach)");
                p.Add($"    - Bolt group design (field-bolted only)");
                p.Add($"");
                p.Add($"  Engineer of record submits (Step 5):");
                p.Add($"    - V_gravity = {gravityCombination:F2} kips");
                p.Add($"    - M_pr = {Mpr:F0} kip-in");
                p.Add($"    - V_h = {Vh:F2} kips");
                p.Add($"    - Beam/column sizes and material grades");
                p.Add($"    - Story height H = {input.StoryHeight:F1} in");
                p.Add($"");
                p.Add($"  EOR reviews SidePlate calculations per Step 8.");
                p.Add("");

                // ==================== SUMMARY ====================
                p.Add("================================================================================");
                p.Add("  DESIGN VERIFICATION SUMMARY");
                p.Add("================================================================================");

                // Combined prequalification
                bool prequal = geomOK && extOK && beamPassed && colDepthOK;
                if (input.SystemType == "SMF")
                    prequal = prequal && prelimScwbOK;
                result.PrequalificationPassed = prequal;

                p.Add($"Geometric compatibility:  {(geomOK ? "PASS" : "FAIL")}");
                p.Add($"Extension range:          {(extOK ? "PASS" : "FAIL")}");
                p.Add($"Beam prequalification:    {(beamPassed ? "PASS" : "FAIL")}");
                p.Add($"Column prequalification:  {(colDepthOK ? "PASS" : "FAIL")}");
                if (input.SystemType == "SMF")
                    p.Add($"Preliminary SCWB:         {(prelimScwbOK ? "PASS" : "FAIL")}");
                p.Add($"SCWB ratio (detailed):    {(scwbOK ? "PASS" : "FAIL")}");
                p.Add($"Beam shear:               {(beamShearOK ? "PASS" : "FAIL")}");
                p.Add($"Panel zone:               PASS (SidePlate proprietary)");
                p.Add("");
                p.Add($"KEY: M_pr={Mpr:F0} kip-in | M_f={Mf:F0} kip-in | M_cl={Mcl:F0} kip-in | V_h={Vh:F2} kips");
                p.Add($"     S_h={Sh:F2} in | L_h={Lh:F2} in | A={extensionA:F2} in | C_pr={Cpr:F3}");
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
            return 999;  // Unknown, will trigger check
        }

        private static double ParseNominalDepth(string designation)
        {
            try
            {
                var parts = designation.ToUpper().Split('X');
                if (parts.Length > 0)
                {
                    string depthStr = parts[0].Replace("W", "").Trim();
                    if (double.TryParse(depthStr, out double d))
                        return d;
                }
            }
            catch { }
            return 999;  // Unknown
        }
    }
}
