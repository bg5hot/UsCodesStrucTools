using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class SteelTensionCalculations
    {
        public enum SectionShapeType { IShape, HSS }
        public enum DesignMethod { LRFD, ASD }

        public enum ConnectionCase
        {
            AllElementsConnected,       // Case 1: U = 1.0
            IShapeFlangeBolted,         // Case 7: W flange, >=3 fasteners (auto bf/d)
            IShapeWebBolted,            // Case 7c: W web, >=4 fasteners, U=0.70
            HSSSingleGusset,            // Case 6a: Rect HSS single gusset, U=1-xbar/l
            HSSTwoSideGusset,           // Case 6b: Rect HSS two side gussets, U=1-xbar/l
            UserDefined                 // User inputs U directly
        }

        public class SectionProperties
        {
            public string Name = "";
            public SectionShapeType ShapeType;

            // I-shape dimensions (in)
            public double d, bf, tw, tf;
            // HSS dimensions (in)
            public double Ht, B_hss, tdes;

            // Common properties
            public double A;       // in²
            public double Ix, Iy;  // in⁴
            public double Sx, Sy;  // in³
            public double Zx, Zy;  // in³
            public double rx, ry;  // in
            public double J;       // in⁴

            // I-shape specific
            public double Cw;      // in⁶
            public double rts;     // in
            public double ho;      // in

            // Derived
            public double h;       // clear distance between flanges (in)
            public double b_flat;  // flat width of HSS wall (in)
        }

        public class InputParameters
        {
            public SectionProperties Section = new();
            public double Fy = 50;      // ksi
            public double Fu = 65;      // ksi
            public double Tu = 100;     // kips (required tensile force)
            public double MemberLength = 0; // ft (for slenderness check)
            public DesignMethod Method = DesignMethod.LRFD;

            // Connection parameters
            public ConnectionCase ConnectionType = ConnectionCase.AllElementsConnected;
            public double ConnectionLength = 0;  // l (in) - for cases needing xbar/l
            public double Xbar = 0;              // connection eccentricity x̄ (in) - for Table D3.1 Case 2
            public double UserU = 1.0;           // user-defined shear lag factor

            // Net area parameters
            public bool UseAutoNetArea = true;    // auto-calculate from bolt holes
            public double An = 0;                 // user-input net area (if not auto)
            public double BoltHoleDiameter = 0;   // bolt shank diameter (in)
            public int NumberOfHoles = 0;         // number of bolt holes
        }

        public class DesignResult
        {
            public bool IsValid = false;
            public string ErrorMessage = "";
            public List<string> Process = new();

            public double Pn_yield = 0;            // kips (nominal yielding strength)
            public double Pn_rupture = 0;           // kips (nominal rupture strength)
            public double DesignStrength = 0;       // φPn or Pn/Ω (kips)
            public double Ratio = 0;
            public bool IsOK;
            public double SlendernessRatio = 0;
            public bool SlendernessOK;

            public string YieldStatus = "";
            public string RuptureStatus = "";
            public string ControllingLimit = "";
            public string Summary = "";
        }

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var sec = input.Section;

            // Input validation
            if (sec.A <= 0)
            {
                result.ErrorMessage = "Invalid section: gross area A must be positive.";
                return result;
            }
            if (input.Fy <= 0 || input.Fu <= 0)
            {
                result.ErrorMessage = "Fy and Fu must be positive.";
                return result;
            }
            if (input.Tu < 0)
            {
                result.ErrorMessage = "Required tensile force Tu must be non-negative.";
                return result;
            }

            var p = result.Process;
            double Ag = sec.A;

            // Header
            p.Add("============================================");
            p.Add("  AISC 360-16 Tension Member Design");
            p.Add("  Chapter D: Design of Members for Tension");
            p.Add("============================================");
            p.Add("");

            // Input summary
            p.Add("-- Input Parameters --");
            p.Add($"  Section: {sec.Name}");
            p.Add($"  Type: {(sec.ShapeType == SectionShapeType.IShape ? "I-Shape (W/WT)" : "HSS (Rectangular/Square)")}");
            p.Add($"  Fy = {input.Fy:F1} ksi,  Fu = {input.Fu:F1} ksi");
            p.Add($"  Method: {input.Method}");
            string tLabel = input.Method == DesignMethod.LRFD ? "Tu" : "Ta";
            p.Add($"  {tLabel} = {input.Tu:F2} kips");
            p.Add($"  Member Length = {input.MemberLength:F1} ft");
            p.Add("");

            // Section properties
            p.Add("-- Section Properties --");
            p.Add($"  Ag = {Ag:F3} in²");
            p.Add($"  Ix = {sec.Ix:F2} in⁴,  Iy = {sec.Iy:F2} in⁴");
            p.Add($"  rx = {sec.rx:F3} in,    ry = {sec.ry:F3} in");

            if (sec.ShapeType == SectionShapeType.IShape)
            {
                p.Add($"  d = {sec.d:F3}, bf = {sec.bf:F3}, tw = {sec.tw:F3}, tf = {sec.tf:F3}");
            }
            else
            {
                p.Add($"  Ht = {sec.Ht:F3} in,  B = {sec.B_hss:F3} in,  tdes = {sec.tdes:F4} in");
            }
            p.Add("");

            // Step 1: Net area
            p.Add("============================================");
            p.Add("-- Step 1: Net Area (An) [Section B4.3] --");
            p.Add("============================================");

            double An;
            if (input.UseAutoNetArea && input.NumberOfHoles > 0 && input.BoltHoleDiameter > 0)
            {
                double t_connected;
                if (sec.ShapeType == SectionShapeType.IShape)
                    t_connected = (input.ConnectionType == ConnectionCase.IShapeWebBolted)
                        ? sec.tw : sec.tf;
                else
                    t_connected = sec.tdes;

                // AISC 360-16 B4.3b: add 1/16 in. to nominal hole diameter for net area
                // Standard hole diameter = bolt diameter + 1/16 in.
                // Design hole width = standard hole + 1/16 = bolt diameter + 1/8 in.
                double effectiveHoleDia = input.BoltHoleDiameter + 2.0 / 16.0;
                double holeArea = input.NumberOfHoles * effectiveHoleDia * t_connected;
                An = Ag - holeArea;

                p.Add($"  Auto-calculated from bolt holes:");
                p.Add($"    Bolt diameter = {input.BoltHoleDiameter:F3} in");
                p.Add($"    Standard hole dia. = bolt dia. + 1/16 = {input.BoltHoleDiameter + 1.0 / 16.0:F3} in");
                p.Add($"    Design hole width = std. hole + 1/16 (B4.3b) = {effectiveHoleDia:F3} in");
                p.Add($"    Number of holes = {input.NumberOfHoles}");
                p.Add($"    Connected element thickness t = {t_connected:F4} in");
                p.Add($"    Hole area = {input.NumberOfHoles} × {effectiveHoleDia:F3} × {t_connected:F4} = {holeArea:F3} in²");
                p.Add($"    An = Ag - hole area = {Ag:F3} - {holeArea:F3} = {An:F3} in²");
                p.Add($"    (Note: Simplified single-row deduction; for staggered or");
                p.Add($"     multi-row holes, use manual An input)");

                if (An <= 0)
                {
                    result.ErrorMessage = "Net area An is non-positive. Check bolt hole parameters.";
                    return result;
                }
            }
            else
            {
                An = input.An > 0 ? input.An : Ag;
                if (input.An > 0)
                    p.Add($"  User-input An = {An:F3} in²");
                else
                    p.Add($"  No bolt holes specified, An = Ag = {An:F3} in²");
            }
            p.Add("");

            // Step 2: Shear lag factor U
            p.Add("============================================");
            p.Add("-- Step 2: Shear Lag Factor (U) [Table D3.1] --");
            p.Add("============================================");

            double U;
            string caseDesc;

            switch (input.ConnectionType)
            {
                case ConnectionCase.AllElementsConnected:
                    U = 1.0;
                    caseDesc = "Case 1: All tension elements connected by fasteners or welds";
                    p.Add($"  {caseDesc}");
                    p.Add($"  U = 1.0");
                    break;

                case ConnectionCase.IShapeFlangeBolted:
                    {
                        // Case 7: W-shape, flange connected with >= 3 fasteners per line
                        double bf_d_ratio = sec.bf / sec.d;
                        double U_case7;

                        p.Add($"  Case 7: W-shape, flange connected with >= 3 fasteners per line");
                        if (bf_d_ratio >= 2.0 / 3.0)
                        {
                            U_case7 = 0.90;
                            p.Add($"  bf/d = {sec.bf:F3}/{sec.d:F3} = {bf_d_ratio:F3} >= 2/3");
                            p.Add($"  U(Case 7) = 0.90");
                        }
                        else
                        {
                            U_case7 = 0.85;
                            p.Add($"  bf/d = {sec.bf:F3}/{sec.d:F3} = {bf_d_ratio:F3} < 2/3");
                            p.Add($"  U(Case 7) = 0.85");
                        }

                        U = U_case7;

                        // Case 2: U = 1 - x̄/l (when connection eccentricity and length are provided)
                        if (input.Xbar > 0 && input.ConnectionLength > 0)
                        {
                            double U_case2 = 1.0 - input.Xbar / input.ConnectionLength;
                            U_case2 = Math.Max(0, U_case2);

                            p.Add($"");
                            p.Add($"  Case 2: U = 1 - x̄/l  (Table D3.1)");
                            p.Add($"    x̄ = {input.Xbar:F3} in,  l = {input.ConnectionLength:F3} in");
                            p.Add($"    U(Case 2) = 1 - {input.Xbar:F3}/{input.ConnectionLength:F3} = {U_case2:F4}");

                            if (U_case2 > U_case7)
                            {
                                U = U_case2;
                                p.Add($"    Case 2 controls: {U_case2:F4} > {U_case7:F4} (Case 7)");
                            }
                            else
                            {
                                p.Add($"    Case 7 controls: {U_case7:F4} >= {U_case2:F4} (Case 2)");
                            }
                        }
                    }
                    break;

                case ConnectionCase.IShapeWebBolted:
                    U = 0.70;
                    caseDesc = "Case 7: W-shape, web connected with >= 4 fasteners per line";
                    p.Add($"  {caseDesc}");
                    p.Add($"  U = 0.70");
                    break;

                case ConnectionCase.HSSSingleGusset:
                    {
                        caseDesc = "Case 6: Rectangular HSS, single concentric gusset plate";
                        double H_conn = sec.Ht;
                        double B_conn = sec.B_hss;
                        double l = input.ConnectionLength;

                        p.Add($"  {caseDesc}");

                        // AISC Table D3.1 Case 6 requires l >= H
                        if (l <= 0)
                        {
                            result.ErrorMessage = "Connection length 'l' must be > 0 for HSS gusset connection (Table D3.1 Case 6).";
                            return result;
                        }
                        if (l < H_conn)
                        {
                            result.ErrorMessage = $"Connection length l ({l:F2} in) < H ({H_conn:F2} in). Table D3.1 Case 6 requires l >= H.";
                            return result;
                        }

                        double xbar = (B_conn * B_conn + 2 * B_conn * H_conn) / (4 * (B_conn + H_conn));
                        U = Math.Min(1.0, 1.0 - xbar / l);

                        p.Add($"  x̄ = (B² + 2BH) / (4(B+H))");
                        p.Add($"    = ({B_conn:F3}² + 2×{B_conn:F3}×{H_conn:F3}) / (4×({B_conn:F3}+{H_conn:F3}))");
                        p.Add($"    = {xbar:F3} in");
                        p.Add($"  l = {l:F3} in >= H = {H_conn:F3} in  [OK]");
                        p.Add($"  U = 1 - x̄/l = 1 - {xbar:F3}/{l:F3} = {U:F4}");
                    }
                    break;

                case ConnectionCase.HSSTwoSideGusset:
                    {
                        caseDesc = "Case 6: Rectangular HSS, two side gusset plates";
                        double H_conn = sec.Ht;
                        double B_conn = sec.B_hss;
                        double l = input.ConnectionLength;

                        p.Add($"  {caseDesc}");

                        if (l <= 0)
                        {
                            result.ErrorMessage = "Connection length 'l' must be > 0 for HSS gusset connection (Table D3.1 Case 6).";
                            return result;
                        }
                        if (l < H_conn)
                        {
                            result.ErrorMessage = $"Connection length l ({l:F2} in) < H ({H_conn:F2} in). Table D3.1 Case 6 requires l >= H.";
                            return result;
                        }

                        double xbar = B_conn * B_conn / (4 * (B_conn + H_conn));
                        U = Math.Min(1.0, 1.0 - xbar / l);

                        p.Add($"  x̄ = B² / (4(B+H))");
                        p.Add($"    = {B_conn:F3}² / (4×({B_conn:F3}+{H_conn:F3}))");
                        p.Add($"    = {xbar:F3} in");
                        p.Add($"  l = {l:F3} in >= H = {H_conn:F3} in  [OK]");
                        p.Add($"  U = 1 - x̄/l = 1 - {xbar:F3}/{l:F3} = {U:F4}");
                    }
                    break;

                case ConnectionCase.UserDefined:
                    U = input.UserU;
                    caseDesc = "User-defined shear lag factor";
                    p.Add($"  {caseDesc}");
                    p.Add($"  U = {U:F4}");
                    break;

                default:
                    U = 1.0;
                    p.Add($"  Default: U = 1.0");
                    break;
            }

            // D3: For open cross sections, U need not be less than A_connected / A_gross
            if (sec.ShapeType == SectionShapeType.IShape &&
                input.ConnectionType != ConnectionCase.AllElementsConnected &&
                input.ConnectionType != ConnectionCase.UserDefined)
            {
                double A_connected;
                if (input.ConnectionType == ConnectionCase.IShapeWebBolted)
                    A_connected = sec.h * sec.tw;
                else // IShapeFlangeBolted
                    A_connected = 2 * sec.bf * sec.tf;

                double U_min = A_connected / Ag;
                double U_before = U;
                U = Math.Max(U, U_min);

                p.Add($"");
                p.Add($"  [D3] Open section lower bound check:");
                p.Add($"    A_connected / Ag = {A_connected:F3} / {Ag:F3} = {U_min:F4}");
                if (U_before < U_min)
                    p.Add($"    U increased: {U_before:F4} -> {U:F4} (lower bound controls)");
                else
                    p.Add($"    U = {U_before:F4} >= {U_min:F4}  [OK]");
            }

            U = Math.Max(0, Math.Min(1.0, U));
            p.Add($"  U = {U:F4}");
            p.Add("");

            // Step 3: Effective net area
            p.Add("============================================");
            p.Add("-- Step 3: Effective Net Area (Ae) [Section D3] --");
            p.Add("============================================");

            double Ae = U * An;
            p.Add($"  Ae = U × An = {U:F4} × {An:F3} = {Ae:F3} in²  (Eq. D3-1)");
            p.Add("");

            // Step 4: Nominal strengths
            p.Add("============================================");
            p.Add("-- Step 4: Tensile Strength --");
            p.Add("============================================");

            // D2(a): Yielding on gross section
            double Pn_yield = input.Fy * Ag;
            p.Add($"  [D2(a)] Tensile Yielding on Gross Section (Eq. D2-1):");
            p.Add($"    Pn = Fy × Ag = {input.Fy:F1} × {Ag:F3} = {Pn_yield:F2} kips");
            p.Add("");

            // D2(b): Rupture on effective net section
            double Pn_rupture = input.Fu * Ae;
            p.Add($"  [D2(b)] Tensile Rupture on Net Section (Eq. D2-2):");
            p.Add($"    Pn = Fu × Ae = {input.Fu:F1} × {Ae:F3} = {Pn_rupture:F2} kips");
            p.Add("");

            result.Pn_yield = Pn_yield;
            result.Pn_rupture = Pn_rupture;

            // Step 5: Design strength
            p.Add("============================================");
            p.Add("-- Step 5: Design Strength --");
            p.Add("============================================");

            double phi_t_yield = 0.90, omega_t_yield = 1.67;
            double phi_t_rupture = 0.75, omega_t_rupture = 2.00;

            double designYield, designRupture;

            if (input.Method == DesignMethod.LRFD)
            {
                designYield = phi_t_yield * Pn_yield;
                designRupture = phi_t_rupture * Pn_rupture;
                p.Add($"  LRFD:");
                p.Add($"    Yielding:   φt×Pn = {phi_t_yield} × {Pn_yield:F2} = {designYield:F2} kips");
                p.Add($"    Rupture:    φt×Pn = {phi_t_rupture} × {Pn_rupture:F2} = {designRupture:F2} kips");
            }
            else
            {
                designYield = Pn_yield / omega_t_yield;
                designRupture = Pn_rupture / omega_t_rupture;
                p.Add($"  ASD:");
                p.Add($"    Yielding:   Pn/Ωt = {Pn_yield:F2} / {omega_t_yield} = {designYield:F2} kips");
                p.Add($"    Rupture:    Pn/Ωt = {Pn_rupture:F2} / {omega_t_rupture} = {designRupture:F2} kips");
            }

            double designStrength = Math.Min(designYield, designRupture);
            result.DesignStrength = designStrength;

            p.Add($"");
            p.Add($"  Controlling limit state:");

            if (designYield <= designRupture)
            {
                result.ControllingLimit = "Yielding (D2-1)";
                p.Add($"    Yielding governs: {designYield:F2} kips <= {designRupture:F2} kips (rupture)");
            }
            else
            {
                result.ControllingLimit = "Rupture (D2-2)";
                p.Add($"    Rupture governs: {designRupture:F2} kips < {designYield:F2} kips (yielding)");
            }

            result.YieldStatus = designYield >= input.Tu ? "PASS" : "FAIL";
            result.RuptureStatus = designRupture >= input.Tu ? "PASS" : "FAIL";

            p.Add($"");
            p.Add($"  Available tensile strength = {designStrength:F2} kips");
            p.Add("");

            // Step 6: Design check
            p.Add("============================================");
            p.Add("-- Step 6: Design Check --");
            p.Add("============================================");

            result.Ratio = designStrength > 0 ? input.Tu / designStrength : 0;
            result.IsOK = result.Ratio <= 1.0;

            p.Add($"  Required: {tLabel} = {input.Tu:F2} kips");
            p.Add($"  Available: {designStrength:F2} kips");
            p.Add($"  Ratio = {tLabel} / Available = {input.Tu:F2} / {designStrength:F2} = {result.Ratio:F4}");
            string compareOp = result.IsOK ? "<=" : ">";
            p.Add($"  Result: {result.Ratio:F4} {compareOp} 1.0  [{(result.IsOK ? "PASS" : "FAIL")}]");
            p.Add("");

            // Step 7: Slenderness check (recommended)
            if (input.MemberLength > 0)
            {
                p.Add("============================================");
                p.Add("-- Step 7: Slenderness Check (Section D1) --");
                p.Add("============================================");

                double L = input.MemberLength * 12.0; // ft to in
                double r_min = Math.Min(sec.rx, sec.ry);
                result.SlendernessRatio = L / r_min;
                result.SlendernessOK = result.SlendernessRatio <= 300;

                p.Add($"  L = {input.MemberLength:F1} ft = {L:F1} in");
                p.Add($"  r_min = min(rx, ry) = min({sec.rx:F3}, {sec.ry:F3}) = {r_min:F3} in");
                p.Add($"  L/r = {L:F1} / {r_min:F3} = {result.SlendernessRatio:F1}");
                p.Add($"  Recommended limit: L/r <= 300");

                if (result.SlendernessOK)
                    p.Add($"  L/r = {result.SlendernessRatio:F1} <= 300  [OK]");
                else
                    p.Add($"  L/r = {result.SlendernessRatio:F1} > 300  [EXCEEDS RECOMMENDED LIMIT]");
                p.Add("");
            }

            // Summary
            string overallStatus = result.IsOK ? "PASS" : "FAIL";
            string slendernote = result.SlendernessRatio > 300 ? $" | Slenderness: {result.SlendernessRatio:F0}/300 [{(result.SlendernessOK ? "OK" : "EXCEEDS")}]" : "";
            result.Summary = $"Tension: {result.Ratio:F4} [{overallStatus}] | Yielding: {result.YieldStatus} | Rupture: {result.RuptureStatus}{slendernote}";

            p.Add("============================================");
            p.Add("-- FINAL RESULT --");
            p.Add("============================================");
            p.Add($"  Controlling limit state: {result.ControllingLimit}");
            p.Add($"  Tension Ratio = {result.Ratio:F4}  [{overallStatus}]");

            result.IsValid = true;
            return result;
        }

        #region Custom Section Property Computation

        public static SectionProperties ComputeCustomIShape(double d, double bf, double tw, double tf)
        {
            double h = d - 2 * tf;
            double ho = d - tf;

            double A = 2 * bf * tf + h * tw;
            double Ix = 2 * (bf * tf * tf * tf / 12 + bf * tf * Math.Pow((d - tf) / 2, 2)) + tw * h * h * h / 12;
            double Sx = Ix / (d / 2);
            double Zx = bf * tf * (d - tf) + tw * h * h / 4;

            double Iy = 2 * tf * bf * bf * bf / 12 + h * tw * tw * tw / 12;
            double Sy = Iy / (bf / 2);
            double Zy = bf * bf * tf / 2 + h * tw * tw / 4;

            double rx = Math.Sqrt(Ix / A);
            double ry = Math.Sqrt(Iy / A);
            double J = (2 * bf * tf * tf * tf + h * tw * tw * tw) / 3;
            double Cw = tf * Math.Pow(d - tf, 2) * bf * bf * bf / 24;
            double rts = Math.Sqrt(Math.Sqrt(Iy * Cw) / Sx);

            return new SectionProperties
            {
                Name = $"Custom I ({d:F1}x{bf:F1}x{tw:F2}x{tf:F2})",
                ShapeType = SectionShapeType.IShape,
                d = d, bf = bf, tw = tw, tf = tf,
                h = h, ho = ho,
                A = A, Ix = Ix, Iy = Iy, Sx = Sx, Sy = Sy,
                Zx = Zx, Zy = Zy, rx = rx, ry = ry,
                J = J, Cw = Cw, rts = rts
            };
        }

        public static SectionProperties ComputeCustomHSS(double Ht, double B, double tdes)
        {
            double t = tdes;
            double h = Ht - 3 * t;
            double b_flat = B - 3 * t;

            double A = 2 * t * (Ht + B - 2 * t);
            double Ix = (B * Ht * Ht * Ht - (B - 2 * t) * Math.Pow(Ht - 2 * t, 3)) / 12;
            double Iy = (Ht * B * B * B - (Ht - 2 * t) * Math.Pow(B - 2 * t, 3)) / 12;
            double Sx = Ix / (Ht / 2);
            double Sy = Iy / (B / 2);
            double Zx = (B * Ht * Ht - (B - 2 * t) * Math.Pow(Ht - 2 * t, 2)) / 4;
            double Zy = (Ht * B * B - (Ht - 2 * t) * Math.Pow(B - 2 * t, 2)) / 4;
            double rx = Math.Sqrt(Ix / A);
            double ry = Math.Sqrt(Iy / A);
            double J = 2 * t * (B - t) * (B - t) * (Ht - t) * (Ht - t) / (B + Ht - 2 * t);

            return new SectionProperties
            {
                Name = $"Custom HSS ({Ht:F1}x{B:F1}x{tdes:F3})",
                ShapeType = SectionShapeType.HSS,
                Ht = Ht, B_hss = B, tdes = tdes,
                h = h, b_flat = b_flat,
                A = A, Ix = Ix, Iy = Iy, Sx = Sx, Sy = Sy,
                Zx = Zx, Zy = Zy, rx = rx, ry = ry, J = J
            };
        }

        #endregion
    }
}
