using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class RbsCalculations
    {
        // Constants
        private const double PHI_D = 1.0;   // Resistance factor for ductile limit states
        private const double PHI_V = 0.9;   // Resistance factor for shear (LRFD)
        private const double E = 29000.0;   // Modulus of elasticity (ksi)

        public class InputParameters
        {
            // Beam section properties
            public double BeamD = 0;         // Beam depth (in)
            public double BeamBf = 0;        // Beam flange width (in)
            public double BeamTf = 0;        // Beam flange thickness (in)
            public double BeamTw = 0;        // Beam web thickness (in)
            public double BeamZx = 0;        // Beam plastic section modulus (in^3)
            public string BeamName = "";     // Beam designation

            // Beam material properties
            public double BeamFy = 50.0;     // Beam yield stress (ksi)
            public double BeamFu = 65.0;     // Beam tensile strength (ksi)
            public double BeamRy = 1.1;      // Beam material overstrength factor

            // Column section properties
            public double ColD = 0;          // Column depth (in)
            public double ColBf = 0;         // Column flange width (in)
            public double ColTf = 0;         // Column flange thickness (in)
            public double ColTw = 0;         // Column web thickness (in)
            public double ColZx = 0;         // Column plastic section modulus (in^3)
            public string ColName = "";      // Column designation

            // Column material properties
            public double ColFy = 50.0;      // Column yield stress (ksi)
            public double ColRy = 1.1;       // Column material overstrength factor

            // RBS geometry
            public double RbsA = 0;          // Distance from column face to start of cut (in)
            public double RbsB = 0;          // Length of cut (in)
            public double RbsC = 0;          // Depth of cut at center (in)

            // Design parameters
            public double Span = 360.0;      // Beam span center-to-center (in)
            public string SystemType = "SMF"; // "SMF" or "IMF"

            // Loads (kips)
            public double LoadD = 0;         // Dead load
            public double LoadL = 0;         // Live load
            public double LoadS = 0;         // Snow load
            public double LoadF1 = 0.5;      // Live load factor (not less than 0.5)
        }

        public class DesignResult
        {
            public bool IsValid = false;
            public string ErrorMessage = "";
            public List<string> Process = new();

            // Overall status
            public bool OverallPassed = false;

            // Flexural check
            public double FlexuralRatio = 0;
            public bool FlexuralOK = false;
            public string FlexuralStatus = "";

            // Shear check
            public double ShearRatio = 0;
            public bool ShearOK = false;
            public string ShearStatus = "";
        }

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var p = input;

            // ========== Input Validation ==========
            if (p.BeamD <= 0 || p.BeamBf <= 0 || p.BeamTf <= 0 || p.BeamTw <= 0 || p.BeamZx <= 0)
            {
                result.ErrorMessage = "Invalid beam section properties. Please select a valid beam section.";
                return result;
            }
            if (p.ColD <= 0 || p.ColBf <= 0 || p.ColTf <= 0 || p.ColTw <= 0 || p.ColZx <= 0)
            {
                result.ErrorMessage = "Invalid column section properties. Please select a valid column section.";
                return result;
            }
            if (p.RbsA <= 0 || p.RbsB <= 0 || p.RbsC <= 0)
            {
                result.ErrorMessage = "RBS geometry parameters (a, b, c) must be positive values.";
                return result;
            }
            if (p.Span <= 0)
            {
                result.ErrorMessage = "Beam span must be a positive value.";
                return result;
            }
            if (p.SystemType != "SMF" && p.SystemType != "IMF")
            {
                result.ErrorMessage = "System type must be SMF or IMF.";
                return result;
            }
            if (p.BeamFy <= 0 || p.BeamFu <= 0 || p.ColFy <= 0)
            {
                result.ErrorMessage = "Material properties (Fy, Fu) must be positive values.";
                return result;
            }
            if (p.BeamRy <= 0 || p.ColRy <= 0)
            {
                result.ErrorMessage = "Material overstrength factors (Ry) must be positive values.";
                return result;
            }

            result.IsValid = true;

            // ========== Header ==========
            result.Process.Add("================================================================================");
            result.Process.Add("  RBS MOMENT CONNECTION DESIGN VERIFICATION (AISC 358-16 Chapter 5)");
            result.Process.Add("================================================================================");
            result.Process.Add("");

            // ========== Input Parameters Summary ==========
            result.Process.Add("================================================================================");
            result.Process.Add("  INPUT PARAMETERS");
            result.Process.Add("================================================================================");
            result.Process.Add("");
            result.Process.Add("BEAM SECTION:");
            result.Process.Add($"  Designation: {p.BeamName}");
            result.Process.Add($"  d = {p.BeamD:F3} in,  bf = {p.BeamBf:F3} in");
            result.Process.Add($"  tw = {p.BeamTw:F3} in,  tf = {p.BeamTf:F3} in");
            result.Process.Add($"  Zx = {p.BeamZx:F1} in³");
            result.Process.Add($"  Fy = {p.BeamFy:F1} ksi,  Fu = {p.BeamFu:F1} ksi,  Ry = {p.BeamRy:F2}");
            result.Process.Add("");
            result.Process.Add("COLUMN SECTION:");
            result.Process.Add($"  Designation: {p.ColName}");
            result.Process.Add($"  dc = {p.ColD:F3} in,  bcf = {p.ColBf:F3} in");
            result.Process.Add($"  tcw = {p.ColTw:F3} in,  tcf = {p.ColTf:F3} in");
            result.Process.Add($"  Zcx = {p.ColZx:F1} in³");
            result.Process.Add($"  Fyc = {p.ColFy:F1} ksi,  Ryc = {p.ColRy:F2}");
            result.Process.Add("");
            result.Process.Add("RBS GEOMETRY:");
            result.Process.Add($"  a = {p.RbsA:F3} in  (distance to start of cut)");
            result.Process.Add($"  b = {p.RbsB:F3} in  (length of cut)");
            result.Process.Add($"  c = {p.RbsC:F3} in  (depth of cut at center)");
            result.Process.Add("");
            result.Process.Add("DESIGN PARAMETERS:");
            result.Process.Add($"  Span L = {p.Span:F1} in ({p.Span / 12:F2} ft)");
            result.Process.Add($"  System type: {p.SystemType}");
            result.Process.Add("");
            result.Process.Add("LOADS:");
            result.Process.Add($"  D = {p.LoadD:F2} kips,  L = {p.LoadL:F2} kips,  S = {p.LoadS:F2} kips");
            result.Process.Add($"  f1 = {p.LoadF1:F2}");
            double gravCombo = 1.2 * p.LoadD + p.LoadF1 * p.LoadL + 0.2 * p.LoadS;
            result.Process.Add($"  Gravity combination (1.2D + {p.LoadF1:F1}L + 0.2S) = {gravCombo:F2} kips");
            result.Process.Add("");

            // Track geometry check
            bool geometryPassed = true;

            // ========== STEP 1: RBS Geometry Limits ==========
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("  STEP 1: RBS GEOMETRY LIMITS CHECK");
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("");

            // Eq. 5.8-1: 0.5*bf <= a <= 0.75*bf
            double aMin = 0.5 * p.BeamBf;
            double aMax = 0.75 * p.BeamBf;
            result.Process.Add("AISC 358-16 Equation 5.8-1:  0.5*bf <= a <= 0.75*bf");
            result.Process.Add($"  Required: {aMin:F3} <= a <= {aMax:F3} in");
            result.Process.Add($"  Provided: a = {p.RbsA:F3} in");
            bool aOK = p.RbsA >= aMin && p.RbsA <= aMax;
            if (aOK)
                result.Process.Add("  [OK] a is within acceptable range");
            else
            {
                result.Process.Add("  [FAIL] a is NOT within acceptable range");
                geometryPassed = false;
            }
            result.Process.Add("");

            // Eq. 5.8-2: 0.65*d <= b <= 0.85*d
            double bMin = 0.65 * p.BeamD;
            double bMax = 0.85 * p.BeamD;
            result.Process.Add("AISC 358-16 Equation 5.8-2:  0.65*d <= b <= 0.85*d");
            result.Process.Add($"  Required: {bMin:F3} <= b <= {bMax:F3} in");
            result.Process.Add($"  Provided: b = {p.RbsB:F3} in");
            bool bOK = p.RbsB >= bMin && p.RbsB <= bMax;
            if (bOK)
                result.Process.Add("  [OK] b is within acceptable range");
            else
            {
                result.Process.Add("  [FAIL] b is NOT within acceptable range");
                geometryPassed = false;
            }
            result.Process.Add("");

            // Eq. 5.8-3: 0.1*bf <= c <= 0.25*bf
            double cMin = 0.1 * p.BeamBf;
            double cMax = 0.25 * p.BeamBf;
            result.Process.Add("AISC 358-16 Equation 5.8-3:  0.1*bf <= c <= 0.25*bf");
            result.Process.Add($"  Required: {cMin:F3} <= c <= {cMax:F3} in");
            result.Process.Add($"  Provided: c = {p.RbsC:F3} in");
            bool cOK = p.RbsC >= cMin && p.RbsC <= cMax;
            if (cOK)
                result.Process.Add("  [OK] c is within acceptable range");
            else
            {
                result.Process.Add("  [FAIL] c is NOT within acceptable range");
                geometryPassed = false;
            }
            result.Process.Add("");

            double reductionPct = (p.RbsC / p.BeamBf) * 100;
            result.Process.Add($"Flange reduction: {reductionPct:F1}%  (affects elastic drift calculation)");
            result.Process.Add("");

            // ========== STEP 2: Z_RBS ==========
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("  STEP 2: PLASTIC SECTION MODULUS AT RBS");
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("");

            double Z_RBS = p.BeamZx - 2 * p.RbsC * p.BeamTf * (p.BeamD - p.BeamTf);
            double zReduction = p.BeamZx - Z_RBS;
            double zReductionPct = (zReduction / p.BeamZx) * 100;

            result.Process.Add("AISC 358-16 Equation 5.8-4:");
            result.Process.Add("  Z_RBS = Zx - 2*c*tf*(d - tf)");
            result.Process.Add($"  Z_RBS = {p.BeamZx:F1} - 2*{p.RbsC:F3}*{p.BeamTf:F3}*({p.BeamD:F3} - {p.BeamTf:F3})");
            result.Process.Add($"  Z_RBS = {p.BeamZx:F1} - {2 * p.RbsC * p.BeamTf * (p.BeamD - p.BeamTf):F1}");
            result.Process.Add($"  Z_RBS = {Z_RBS:F1} in³");
            result.Process.Add($"  Reduction in Z: {zReduction:F1} in³ ({zReductionPct:F1}%)");
            result.Process.Add("");

            // ========== STEP 3: M_pr ==========
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("  STEP 3: PROBABLE MAXIMUM MOMENT AT RBS");
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("");

            // C_pr per Eq. 2.4-2
            double C_pr = Math.Min((p.BeamFy + p.BeamFu) / (2 * p.BeamFy), 1.2);
            result.Process.Add("C_pr per AISC 358-16 Equation 2.4-2:");
            result.Process.Add("  C_pr = (Fy + Fu) / (2*Fy)  <= 1.2");
            result.Process.Add($"  C_pr = ({p.BeamFy:F1} + {p.BeamFu:F1}) / (2*{p.BeamFy:F1})");
            result.Process.Add($"  C_pr = {C_pr:F3} (limited to 1.2)");
            result.Process.Add("");

            double M_pr = C_pr * p.BeamRy * p.BeamFy * Z_RBS;
            result.Process.Add("AISC 358-16 Equation 5.8-5:");
            result.Process.Add("  M_pr = C_pr * Ry * Fy * Z_RBS");
            result.Process.Add($"  M_pr = {C_pr:F3} * {p.BeamRy:F2} * {p.BeamFy:F1} * {Z_RBS:F1}");
            result.Process.Add($"  M_pr = {M_pr:F0} kip-in  ({M_pr / 12:F1} kip-ft)");
            result.Process.Add("");

            // ========== STEP 4: V_RBS ==========
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("  STEP 4: SHEAR FORCE AT RBS");
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("");

            double V_gravity = gravCombo / 2.0;
            double Lh = p.Span - p.ColD - 2 * (p.RbsA + p.RbsB / 2.0);
            double V_RBS = 2 * M_pr / Lh + V_gravity;

            result.Process.Add("Distance between plastic hinge locations:");
            result.Process.Add("  Lh = Span - dc - 2*(a + b/2)");
            result.Process.Add($"  Lh = {p.Span:F1} - {p.ColD:F3} - 2*({p.RbsA:F3} + {p.RbsB:F3}/2)");
            result.Process.Add($"  Lh = {Lh:F2} in  ({Lh / 12:F2} ft)");
            result.Process.Add("");
            result.Process.Add("Shear at RBS from free-body diagram:");
            result.Process.Add("  V_RBS = 2*M_pr/Lh + V_gravity");
            result.Process.Add($"  V_gravity = (1.2D + {p.LoadF1:F1}L + 0.2S) / 2 = {gravCombo:F2} / 2 = {V_gravity:F2} kips");
            result.Process.Add($"  V_RBS = 2*{M_pr:F0}/{Lh:F2} + {V_gravity:F2}");
            result.Process.Add($"  V_RBS = {2 * M_pr / Lh:F2} + {V_gravity:F2}");
            result.Process.Add($"  V_RBS = {V_RBS:F2} kips");
            result.Process.Add($"  Gravity shear: {V_gravity:F2} kips");
            result.Process.Add($"  Seismic shear: {2 * M_pr / Lh:F2} kips");
            result.Process.Add("");

            // ========== STEP 5: M_f ==========
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("  STEP 5: PROBABLE MAXIMUM MOMENT AT COLUMN FACE");
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("");

            double S_h = p.RbsA + p.RbsB / 2.0;
            double M_f = M_pr + V_RBS * S_h;

            result.Process.Add("Distance from column face to plastic hinge:");
            result.Process.Add("  S_h = a + b/2");
            result.Process.Add($"  S_h = {p.RbsA:F3} + {p.RbsB:F3}/2");
            result.Process.Add($"  S_h = {S_h:F2} in");
            result.Process.Add("");
            result.Process.Add("AISC 358-16 Equation 5.8-6:");
            result.Process.Add("  M_f = M_pr + V_RBS * S_h");
            result.Process.Add($"  M_f = {M_pr:F0} + {V_RBS:F2} * {S_h:F2}");
            result.Process.Add($"  M_f = {M_f:F0} kip-in  ({M_f / 12:F1} kip-ft)");
            result.Process.Add("");

            // ========== STEP 6: M_pe ==========
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("  STEP 6: PLASTIC MOMENT OF BEAM");
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("");

            double M_pe = p.BeamRy * p.BeamFy * p.BeamZx;

            result.Process.Add("AISC 358-16 Equation 5.8-7:");
            result.Process.Add("  M_pe = Ry * Fy * Zx");
            result.Process.Add($"  M_pe = {p.BeamRy:F2} * {p.BeamFy:F1} * {p.BeamZx:F1}");
            result.Process.Add($"  M_pe = {M_pe:F0} kip-in  ({M_pe / 12:F1} kip-ft)");
            result.Process.Add("");

            // ========== STEP 7: Flexural Strength Check ==========
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("  STEP 7: FLEXURAL STRENGTH CHECK AT COLUMN FACE");
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("");

            double phi_Mpe = PHI_D * M_pe;
            double flexRatio = M_f / phi_Mpe;
            bool flexOK = M_f <= phi_Mpe;

            result.Process.Add("AISC 358-16 Equation 5.8-8:");
            result.Process.Add("  M_f <= phi_d * M_pe");
            result.Process.Add($"  M_f   = {M_f:F0} kip-in  ({M_f / 12:F1} kip-ft)");
            result.Process.Add($"  phi_d * M_pe = {PHI_D:F1} * {M_pe:F0} = {phi_Mpe:F0} kip-in");
            result.Process.Add($"  Utilization ratio: {flexRatio:F3}");
            result.Process.Add("");

            if (flexOK)
                result.Process.Add("  [OK] Flexural strength is adequate");
            else
            {
                result.Process.Add("  [FAIL] Flexural strength is NOT adequate");
                result.Process.Add($"  Required:  {M_f:F0} kip-in");
                result.Process.Add($"  Available: {phi_Mpe:F0} kip-in");
            }
            result.Process.Add("");

            // ========== STEP 8: Shear Strength Check ==========
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("  STEP 8: SHEAR STRENGTH CHECK");
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("");

            double V_u = 2 * M_pr / Lh + V_gravity;
            double dw = p.BeamD - 2 * p.BeamTf;
            double V_n = 0.6 * p.BeamFy * dw * p.BeamTw;
            double phi_Vn = PHI_V * V_n;
            double shearRatio = V_u / phi_Vn;
            bool shearOK = V_u <= phi_Vn;

            result.Process.Add("AISC 358-16 Equation 5.8-9:");
            result.Process.Add("  V_u = 2*M_pr/Lh + V_gravity");
            result.Process.Add($"  V_u = 2*{M_pr:F0}/{Lh:F2} + {V_gravity:F2}");
            result.Process.Add($"  V_u = {V_u:F2} kips");
            result.Process.Add("");
            result.Process.Add("Shear strength per AISC 360 Chapter G:");
            result.Process.Add("  V_n = 0.6*Fy*dw*tw");
            result.Process.Add($"  dw = d - 2*tf = {p.BeamD:F3} - 2*{p.BeamTf:F3} = {dw:F3} in");
            result.Process.Add($"  V_n = 0.6*{p.BeamFy:F1}*{dw:F3}*{p.BeamTw:F3}");
            result.Process.Add($"  V_n = {V_n:F2} kips");
            result.Process.Add($"  phi_v * V_n = {PHI_V:F1} * {V_n:F2} = {phi_Vn:F2} kips");
            result.Process.Add($"  Utilization ratio: {shearRatio:F3}");
            result.Process.Add("");

            if (shearOK)
                result.Process.Add("  [OK] Shear strength is adequate");
            else
            {
                result.Process.Add("  [FAIL] Shear strength is NOT adequate");
                result.Process.Add($"  Required:  {V_u:F2} kips");
                result.Process.Add($"  Available: {phi_Vn:F2} kips");
            }
            result.Process.Add("");

            // ========== STEP 9: Web Connection Requirements ==========
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("  STEP 9: BEAM WEB-TO-COLUMN CONNECTION (Section 5.6)");
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("");

            result.Process.Add($"System type: {p.SystemType}");
            result.Process.Add("");

            if (p.SystemType == "SMF")
            {
                result.Process.Add("For SMF systems (Section 5.6.2.a):");
                result.Process.Add("  - Beam web shall be connected using CJP groove weld");
                result.Process.Add("  - Single-plate shear connection shall extend between weld access holes");
                result.Process.Add("  - Minimum plate thickness: 3/8 in. (10 mm)");
                result.Process.Add("  - Weld tabs are not required at ends of CJP groove weld");
            }
            else
            {
                result.Process.Add("For IMF systems (Section 5.6.2.b):");
                result.Process.Add("  - Beam web shall be connected using CJP groove weld (same as SMF)");
                result.Process.Add("  Exception: Bolted single-plate shear connection is permitted");
                result.Process.Add("    - Connection shall be slip-critical");
                result.Process.Add("    - Design based on shear yielding and shear rupture");
                result.Process.Add("    - Plate welded to column flange (CJP or double fillet welds)");
            }
            result.Process.Add("");
            result.Process.Add($"Required shear strength for web connection: V_u = {V_u:F2} kips");
            result.Process.Add("  Design web connection for this shear force per AISC 360");
            result.Process.Add("");

            // ========== STEP 10: Continuity Plate Requirements ==========
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("  STEP 10: CONTINUITY PLATE REQUIREMENTS (Chapter 2)");
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("");

            result.Process.Add("Continuity plates shall be provided per Chapter 2 of AISC 358-16");
            result.Process.Add("  when required based on column flange thickness and force transfer.");
            result.Process.Add("");
            result.Process.Add("Refer to AISC 358-16 Chapter 2 for detailed continuity plate requirements:");
            result.Process.Add("  - Panel zone strength");
            result.Process.Add("  - Column flange bending");
            result.Process.Add("  - Force transfer through column flange");
            result.Process.Add("");

            // ========== STEP 11: Column-Beam Relationship ==========
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("  STEP 11: COLUMN-BEAM RELATIONSHIP LIMITATIONS (Section 5.4)");
            result.Process.Add("--------------------------------------------------------------------------------");
            result.Process.Add("");

            result.Process.Add($"System type: {p.SystemType}");
            result.Process.Add("");

            double Sh_total = p.RbsA + p.RbsB / 2.0 + p.ColD / 2.0;
            double M_uv = V_RBS * Sh_total;
            double M_pb_star = M_pr + M_uv;

            result.Process.Add("Column-beam moment ratio check (Section 5.4):");
            result.Process.Add($"  M_pr = {M_pr:F0} kip-in");
            result.Process.Add($"  Distance a + b/2 + dc/2 = {p.RbsA:F3} + {p.RbsB:F3}/2 + {p.ColD:F3}/2 = {Sh_total:F2} in");
            result.Process.Add("  M_uv = V_RBS * (a + b/2 + dc/2)");
            result.Process.Add($"  M_uv = {V_RBS:F2} * {Sh_total:F2} = {M_uv:F0} kip-in");
            result.Process.Add($"  M_pb* = M_pr + M_uv = {M_pr:F0} + {M_uv:F0} = {M_pb_star:F0} kip-in");
            result.Process.Add("");

            if (p.SystemType == "SMF")
            {
                result.Process.Add("For SMF systems:");
                result.Process.Add("  Sum M_nc >= Sum M_pb*  (AISC Seismic Provisions)");
                result.Process.Add($"  Sum M_pb* for both sides of column = 2 * {M_pb_star:F0} = {2 * M_pb_star:F0} kip-in");
                result.Process.Add("");

                // Calculate column capacity ratio
                double colCapacity = p.ColZx * p.ColFy;
                double ratioSCWB = colCapacity / M_pb_star;

                result.Process.Add("  Strong-column / weak-beam ratio:");
                result.Process.Add("    Ratio = Zcx * Fyc / M_pb*");
                result.Process.Add($"    Ratio = {p.ColZx:F1} * {p.ColFy:F1} / {M_pb_star:F0}");
                result.Process.Add($"    Ratio = {ratioSCWB:F3}");
                result.Process.Add("    (Ratio >= 1.0 indicates column is stronger than beam)");
            }
            else
            {
                result.Process.Add("For IMF systems:");
                result.Process.Add("  Column-beam moment ratio shall conform to AISC Seismic Provisions");
            }
            result.Process.Add("");

            // Check column depth limitation
            if (p.ColD <= 36)
                result.Process.Add($"  [OK] Column depth ({p.ColD:F1} in) is within W36 limitation");
            else
                result.Process.Add($"  [FAIL] Column depth ({p.ColD:F1} in) exceeds W36 limitation");
            result.Process.Add("");

            // ========== Summary ==========
            result.Process.Add("================================================================================");
            result.Process.Add("  DESIGN VERIFICATION SUMMARY");
            result.Process.Add("================================================================================");
            result.Process.Add("");
            result.Process.Add("CHECKS SUMMARY:");
            result.Process.Add("");
            result.Process.Add("RBS Geometry Limits:");
            result.Process.Add($"  Equation 5.8-1 (a limits):  {(aOK ? "[PASS]" : "[FAIL]")}");
            result.Process.Add($"  Equation 5.8-2 (b limits):  {(bOK ? "[PASS]" : "[FAIL]")}");
            result.Process.Add($"  Equation 5.8-3 (c limits):  {(cOK ? "[PASS]" : "[FAIL]")}");
            result.Process.Add("");
            result.Process.Add("Strength Checks:");
            result.Process.Add($"  Flexural strength at column face (Eq. 5.8-8):  {(flexOK ? "[PASS]" : "[FAIL]")}");
            result.Process.Add($"    Utilization: {flexRatio:F3}");
            result.Process.Add($"  Shear strength:                                 {(shearOK ? "[PASS]" : "[FAIL]")}");
            result.Process.Add($"    Utilization: {shearRatio:F3}");
            result.Process.Add("");
            result.Process.Add("KEY RESULTS:");
            result.Process.Add($"  Z_RBS = {Z_RBS:F1} in³");
            result.Process.Add($"  M_pr  = {M_pr:F0} kip-in  ({M_pr / 12:F1} kip-ft)");
            result.Process.Add($"  M_f   = {M_f:F0} kip-in  ({M_f / 12:F1} kip-ft)");
            result.Process.Add($"  V_u   = {V_u:F2} kips");
            result.Process.Add("");

            // Set result properties
            bool allPassed = geometryPassed && flexOK && shearOK;

            result.OverallPassed = allPassed;
            result.FlexuralRatio = flexRatio;
            result.FlexuralOK = flexOK;
            result.FlexuralStatus = flexOK ? "PASS" : "FAIL";
            result.ShearRatio = shearRatio;
            result.ShearOK = shearOK;
            result.ShearStatus = shearOK ? "PASS" : "FAIL";

            result.Process.Add("================================================================================");
            if (allPassed)
                result.Process.Add("  ALL CHECKS PASSED - RBS CONNECTION DESIGN IS ADEQUATE");
            else
                result.Process.Add("  SOME CHECKS FAILED - REVIEW AND ADJUST DESIGN");
            result.Process.Add("================================================================================");

            return result;
        }
    }
}
