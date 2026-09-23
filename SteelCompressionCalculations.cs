using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class SteelCompressionCalculations
    {
        public enum SectionShapeType { IShape, HSS }
        public enum DesignMethod { LRFD, ASD }
        public enum ElementClass { Nonslender, Slender }

        public class SectionProperties
        {
            public string Name = "";
            public SectionShapeType ShapeType;

            public double d, bf, tw, tf;
            public double Ht, B_hss, tdes;

            public double A;
            public double Ix, Iy;
            public double Sx, Sy;
            public double Zx, Zy;
            public double rx, ry;
            public double J;

            public double Cw;
            public double rts;
            public double ho;

            public double h;
            public double b_flat;
        }

        public class InputParameters
        {
            public SectionProperties Section = new();
            public double Fy = 50;
            public double E = 29000;
            public double Pu = 200;
            public int MethodIndex = 0; // 0=LRFD, 1=ASD
            public bool IsRolled = true;

            // Effective length parameters
            public double Kx = 1.0;
            public double Ky = 1.0;
            public double Lx = 15.0; // ft
            public double Ly = 15.0; // ft
            public double Lz = 15.0; // ft (torsional, for I-shapes only)
        }

        public class DesignResult
        {
            public bool IsValid = false;
            public string ErrorMessage = "";
            public List<string> Process = new();

            public double Fcr = 0;
            public double Ae = 0;
            public double Pn = 0;
            public double DesignStrength = 0;
            public double Ratio = 0;
            public bool IsOK;
            public string ControllingMode = "";
            public string Summary = "";
        }

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var sec = input.Section;
            var p = result.Process;
            double G = 11200.0; // AISC 360-16 prescribed shear modulus for steel, ksi

            // Input validation
            if (sec.A <= 0)
            {
                result.ErrorMessage = "Invalid section: gross area A must be positive.";
                return result;
            }
            if (input.Fy <= 0 || input.E <= 0)
            {
                result.ErrorMessage = "Fy and E must be positive.";
                return result;
            }
            if (input.Pu < 0)
            {
                result.ErrorMessage = "Required compressive force must be non-negative.";
                return result;
            }
            if (input.Lx <= 0 || input.Ly <= 0)
            {
                result.ErrorMessage = "Unbraced lengths Lx and Ly must be positive.";
                return result;
            }

            double Fy = input.Fy;
            double E = input.E;

            // Header
            p.Add("============================================");
            p.Add("  AISC 360-16 Compression Member Design");
            p.Add("  Chapter E: Design of Members for Compression");
            p.Add("============================================");
            p.Add("");

            // Input summary
            p.Add("-- Input Parameters --");
            p.Add($"  Section: {sec.Name}");
            p.Add($"  Type: {(sec.ShapeType == SectionShapeType.IShape ? "I-Shape (W/WT)" : "HSS (Rectangular/Square)")}");
            p.Add($"  Fy = {Fy:F1} ksi,  E = {E:F0} ksi,  G = {G:F0} ksi");
            p.Add($"  Method: {(input.MethodIndex == 0 ? "LRFD" : "ASD")}");
            string pLabel = input.MethodIndex == 0 ? "Pu" : "Pa";
            p.Add($"  {pLabel} = {input.Pu:F2} kips");
            p.Add($"  Kx = {input.Kx:F2},  Lx = {input.Lx:F1} ft");
            p.Add($"  Ky = {input.Ky:F2},  Ly = {input.Ly:F1} ft");
            if (sec.ShapeType == SectionShapeType.IShape)
                p.Add($"  Lz (torsional) = {input.Lz:F1} ft");
            p.Add($"  Section type: {(input.IsRolled ? "Rolled (from database)" : "Custom (built-up)")}");
            p.Add("");

            // Section properties
            p.Add("-- Section Properties --");
            p.Add($"  Ag = {sec.A:F3} in²");
            p.Add($"  Ix = {sec.Ix:F2} in⁴,  Iy = {sec.Iy:F2} in⁴");
            p.Add($"  rx = {sec.rx:F3} in,    ry = {sec.ry:F3} in");
            p.Add($"  J  = {sec.J:F4} in⁴");
            if (sec.ShapeType == SectionShapeType.IShape)
            {
                p.Add($"  Cw = {sec.Cw:F1} in⁶,  ho = {sec.ho:F3} in");
                p.Add($"  d = {sec.d:F3}, bf = {sec.bf:F3}, tw = {sec.tw:F3}, tf = {sec.tf:F3}");
                p.Add($"  h = {sec.h:F3} in");
            }
            else
            {
                p.Add($"  Ht = {sec.Ht:F3} in,  B = {sec.B_hss:F3} in,  tdes = {sec.tdes:F4} in");
                p.Add($"  h = {sec.h:F3} in,  b_flat = {sec.b_flat:F3} in");
            }
            p.Add("");

            // Step 1: Section classification (Table B4.1a for compression)
            p.Add("============================================");
            p.Add("-- Step 1: Section Classification (Table B4.1a) --");
            p.Add("============================================");

            bool hasSlender = false;
            double lr_f = 0, lr_w = 0, lr_hss = 0;

            if (sec.ShapeType == SectionShapeType.IShape)
            {
                // Flange (Case 1 for rolled, Case 2 for welded/built-up)
                double flangeLambda = sec.bf / (2 * sec.tf);
                if (input.IsRolled)
                {
                    lr_f = 0.56 * Math.Sqrt(E / Fy); // Case 1
                }
                else
                {
                    double kc = Math.Max(0.35, Math.Min(0.76, 4.0 / Math.Sqrt(sec.h / sec.tw)));
                    lr_f = 0.64 * Math.Sqrt(kc * E / Fy); // Case 2
                }

                ElementClass flangeClass = ClassifyElement(flangeLambda, lr_f);
                hasSlender = flangeClass == ElementClass.Slender;

                p.Add($"  Flange: λ = bf/(2tf) = {flangeLambda:F3}");
                p.Add($"    λr = {lr_f:F3} ({(input.IsRolled ? "Case 1, rolled" : "Case 2, welded")})");
                p.Add($"    Flange classification: {flangeClass}");

                // Web (Case 5): λ = h/tw
                double webLambda = sec.h / sec.tw;
                lr_w = 1.49 * Math.Sqrt(E / Fy);
                ElementClass webClass = ClassifyElement(webLambda, lr_w);
                if (webClass == ElementClass.Slender) hasSlender = true;

                p.Add($"  Web: λ = h/tw = {webLambda:F3}");
                p.Add($"    λr = 1.49*sqrt(E/Fy) = {lr_w:F3}");
                p.Add($"    Web classification: {webClass}");
            }
            else // HSS
            {
                // All walls (Case 6): λr = 1.40*sqrt(E/Fy)
                lr_hss = 1.40 * Math.Sqrt(E / Fy);

                double flangeLambda = sec.b_flat / sec.tdes;
                ElementClass flangeClass = ClassifyElement(flangeLambda, lr_hss);
                hasSlender = flangeClass == ElementClass.Slender;

                p.Add($"  Flange wall: λ = b/tdes = {flangeLambda:F3}");
                p.Add($"    λr = 1.40*sqrt(E/Fy) = {lr_hss:F3} (Case 6)");
                p.Add($"    Flange wall classification: {flangeClass}");

                double webLambda = sec.h / sec.tdes;
                ElementClass webClass = ClassifyElement(webLambda, lr_hss);
                if (webClass == ElementClass.Slender) hasSlender = true;

                p.Add($"  Web wall: λ = h/tdes = {webLambda:F3}");
                p.Add($"    Web wall classification: {webClass}");
            }

            string sectionClass = hasSlender ? "Slender element section" : "Nonslender element section";
            p.Add($"  Overall: {sectionClass}");
            p.Add("");

            // Step 2: Effective length and slenderness ratio
            p.Add("============================================");
            p.Add("-- Step 2: Slenderness Ratio (KL/r) --");
            p.Add("============================================");

            double Lcx = input.Kx * input.Lx * 12.0; // in
            double Lcy = input.Ky * input.Ly * 12.0; // in
            double KLr_x = Lcx / sec.rx;
            double KLr_y = Lcy / sec.ry;

            p.Add($"  Lcx = Kx*Lx = {input.Kx:F2}*{input.Lx:F1}*12 = {Lcx:F1} in");
            p.Add($"  Lcy = Ky*Ly = {input.Ky:F2}*{input.Ly:F1}*12 = {Lcy:F1} in");
            p.Add($"  (KL/r)x = {KLr_x:F2}");
            p.Add($"  (KL/r)y = {KLr_y:F2}");

            double KLr_governing = Math.Max(KLr_x, KLr_y);
            string governingAxis = KLr_x >= KLr_y ? "x-axis" : "y-axis";
            p.Add($"  Governing: (KL/r) = max({KLr_x:F2}, {KLr_y:F2}) = {KLr_governing:F2} ({governingAxis})");
            p.Add("");

            // Step 3: Flexural buckling (E3)
            p.Add("============================================");
            p.Add("-- Step 3: Flexural Buckling (Chapter E3) --");
            p.Add("============================================");

            // E3-4: Elastic buckling stress
            double Fex = Math.PI * Math.PI * E / (KLr_x * KLr_x);
            double Fey = Math.PI * Math.PI * E / (KLr_y * KLr_y);
            double Fe_flex = Math.Min(Fex, Fey);

            p.Add($"  [E3-4] Elastic buckling stress:");
            p.Add($"    Fex = π²E/(KL/r)x² = π²*{E:F0}/{KLr_x:F2}² = {Fex:F2} ksi");
            p.Add($"    Fey = π²E/(KL/r)y² = π²*{E:F0}/{KLr_y:F2}² = {Fey:F2} ksi");
            p.Add($"    Fe (flexural) = min(Fex, Fey) = {Fe_flex:F2} ksi");
            p.Add("");

            // Step 4: Torsional buckling (E4) - for I-shapes only
            double Fe_torsional = double.MaxValue;
            if (sec.ShapeType == SectionShapeType.IShape)
            {
                p.Add("============================================");
                p.Add("-- Step 4: Torsional Buckling (Chapter E4) --");
                p.Add("============================================");

                double Lcz = input.Lz * 12.0;
                double Cw_eff = sec.Iy * sec.ho * sec.ho / 4.0;

                p.Add($"  [E4-2] Doubly-symmetric I-shape about shear center:");
                p.Add($"    Lcz = {Lcz:F1} in ({input.Lz:F1} ft)");
                p.Add($"    Cw = Iy*ho²/4 = {sec.Iy:F2}*{sec.ho:F3}²/4 = {Cw_eff:F1} in⁶");

                if (Lcz < 0.001)
                {
                    Fe_torsional = double.MaxValue;
                    p.Add($"    Lcz ≈ 0 => Torsional buckling does not apply");
                }
                else
                {
                    // E4-2: Fez for doubly-symmetric members
                    double Fez = (Math.PI * Math.PI * E * Cw_eff / (Lcz * Lcz) + G * sec.J) / (sec.Ix + sec.Iy);
                    Fe_torsional = Fez;

                    p.Add($"    Fez = (π²E*Cw/Lcz² + G*J)/(Ix+Iy)");
                    p.Add($"         = (π²*{E:F0}*{Cw_eff:F1}/{Lcz:F1}² + {G:F0}*{sec.J:F4})/({sec.Ix:F2}+{sec.Iy:F2})");
                    p.Add($"         = {Fez:F2} ksi");
                }
                p.Add("");

                if (Fe_torsional >= Fe_flex)
                    p.Add($"  Fez = {Fe_torsional:F2} >= Fe_flexural = {Fe_flex:F2} => Torsional does not govern");
                else
                    p.Add($"  Fez = {Fe_torsional:F2} < Fe_flexural = {Fe_flex:F2} => Torsional buckling governs");
                p.Add("");
            }

            // Governing Fe
            double Fe = Math.Min(Fe_flex, Fe_torsional);
            string bucklingMode = Fe <= Fe_flex ? "Torsional (E4)" : $"Flexural ({governingAxis}, E3)";
            if (sec.ShapeType == SectionShapeType.HSS)
                bucklingMode = $"Flexural ({governingAxis}, E3)";

            p.Add($"  Governing Fe = min(Fe_flexural, Fe_torsional) = {Fe:F2} ksi");
            p.Add($"  Buckling mode: {bucklingMode}");
            p.Add("");

            // Step 5: Critical stress Fcr (E3-2 / E3-3)
            p.Add("============================================");
            p.Add("-- Step 5: Critical Stress Fcr --");
            p.Add("============================================");

            double Fcr;
            if (Fe > 0.44 * Fy)
            {
                Fcr = Math.Pow(0.658, Fy / Fe) * Fy;
                p.Add($"  Fe = {Fe:F2} > 0.44*Fy = {0.44 * Fy:F2} => Inelastic buckling (Eq. E3-2)");
                p.Add($"  Fcr = 0.658^(Fy/Fe)*Fy = 0.658^({Fy:F1}/{Fe:F2})*{Fy:F1}");
                p.Add($"       = {Fcr:F2} ksi");
            }
            else
            {
                Fcr = 0.877 * Fe;
                p.Add($"  Fe = {Fe:F2} <= 0.44*Fy = {0.44 * Fy:F2} => Elastic buckling (Eq. E3-3)");
                p.Add($"  Fcr = 0.877*Fe = 0.877*{Fe:F2} = {Fcr:F2} ksi");
            }

            result.Fcr = Fcr;
            result.ControllingMode = bucklingMode;
            p.Add("");

            // Step 6: Effective area (E7) for slender elements
            p.Add("============================================");
            p.Add("-- Step 6: Effective Area Ae (Chapter E7) --");
            p.Add("============================================");

            double Ae = sec.A;

            if (hasSlender)
            {
                p.Add("  Section contains slender elements => Evaluate E7 effective area");
                p.Add("");

                double areaReduction = 0;
                double limitMultiplier = Math.Sqrt(Fy / Fcr);

                if (sec.ShapeType == SectionShapeType.IShape)
                {
                    // Check flange
                    double flangeLambda = sec.bf / (2 * sec.tf);
                    double flangeLimit = lr_f * limitMultiplier;

                    if (flangeLambda > flangeLimit)
                    {
                        double c1 = 0.22, c2 = 1.49;
                        double Fel = Math.Pow(c2 * lr_f / flangeLambda, 2) * Fy;
                        double sqrtRatio = Math.Sqrt(Fel / Fcr);
                        double rho = Math.Max(0, Math.Min(1.0, (1.0 - c1 * sqrtRatio) * sqrtRatio));

                        double be = rho * (sec.bf / 2.0);
                        double reduction = 4 * (sec.bf / 2.0 - be) * sec.tf;
                        areaReduction += reduction;

                        p.Add($"  Flange (unstiffened, c1={c1}, c2={c2}):");
                        p.Add($"    λ = {flangeLambda:F3} > λr*√(Fy/Fcr) = {lr_f:F3}*{limitMultiplier:F3} = {flangeLimit:F3} => REDUCE");
                        p.Add($"    Fel = (c2*λr/λ)²*Fy = ({c2}*{lr_f:F3}/{flangeLambda:F3})²*{Fy:F1} = {Fel:F2} ksi  (Eq. E7-5)");
                        p.Add($"    ρ = (1-c1*√(Fel/Fcr))*√(Fel/Fcr) = {rho:F4}  (Eq. E7-3)");
                        p.Add($"    be = ρ*(bf/2) = {be:F3} in (effective half-flange)");
                        p.Add($"    Area reduction = 4*(bf/2-be)*tf = {reduction:F3} in²");
                    }
                    else
                    {
                        p.Add($"  Flange: λ = {flangeLambda:F3} <= λr*√(Fy/Fcr) = {flangeLimit:F3} => FULLY EFFECTIVE (Eq. E7-2)");
                    }

                    // Check web
                    double webLambda = sec.h / sec.tw;
                    double webLimit = lr_w * limitMultiplier;

                    if (webLambda > webLimit)
                    {
                        double c1 = 0.18, c2 = 1.31;
                        double Fel = Math.Pow(c2 * lr_w / webLambda, 2) * Fy;
                        double sqrtRatio = Math.Sqrt(Fel / Fcr);
                        double rho = Math.Max(0, Math.Min(1.0, (1.0 - c1 * sqrtRatio) * sqrtRatio));

                        double he = rho * sec.h;
                        double reduction = (sec.h - he) * sec.tw;
                        areaReduction += reduction;

                        p.Add($"  Web (stiffened, c1={c1}, c2={c2}):");
                        p.Add($"    λ = {webLambda:F3} > λr*√(Fy/Fcr) = {webLimit:F3} => REDUCE");
                        p.Add($"    Fel = {Fel:F2} ksi");
                        p.Add($"    ρ = {rho:F4}");
                        p.Add($"    he = ρ*h = {he:F3} in");
                        p.Add($"    Area reduction = (h-he)*tw = {reduction:F3} in²");
                    }
                    else
                    {
                        p.Add($"  Web: λ = {webLambda:F3} <= λr*√(Fy/Fcr) = {webLimit:F3} => FULLY EFFECTIVE (Eq. E7-2)");
                    }
                }
                else // HSS
                {
                    double c1 = 0.20, c2 = 1.38;
                    double hssLimit = lr_hss * limitMultiplier;

                    // Check flange wall
                    double flangeLambda = sec.b_flat / sec.tdes;

                    if (flangeLambda > hssLimit)
                    {
                        double Fel = Math.Pow(c2 * lr_hss / flangeLambda, 2) * Fy;
                        double sqrtRatio = Math.Sqrt(Fel / Fcr);
                        double rho = Math.Max(0, Math.Min(1.0, (1.0 - c1 * sqrtRatio) * sqrtRatio));
                        double be_eff = rho * sec.b_flat;
                        double reduction = 2 * (sec.b_flat - be_eff) * sec.tdes;
                        areaReduction += reduction;

                        p.Add($"  Flange walls (stiffened HSS, c1={c1}, c2={c2}):");
                        p.Add($"    λ = {flangeLambda:F3} > λr*√(Fy/Fcr) = {hssLimit:F3} => REDUCE");
                        p.Add($"    Fel = {Fel:F2} ksi");
                        p.Add($"    ρ = {rho:F4}");
                        p.Add($"    be = ρ*b_flat = {be_eff:F3} in (each wall)");
                        p.Add($"    Area reduction = 2*(b_flat-be)*tdes = {reduction:F3} in²");
                    }
                    else
                    {
                        p.Add($"  Flange walls: λ = {flangeLambda:F3} <= λr*√(Fy/Fcr) = {hssLimit:F3} => FULLY EFFECTIVE (Eq. E7-2)");
                    }

                    // Check web wall
                    double webLambda = sec.h / sec.tdes;
                    if (webLambda > hssLimit)
                    {
                        double Fel = Math.Pow(c2 * lr_hss / webLambda, 2) * Fy;
                        double sqrtRatio = Math.Sqrt(Fel / Fcr);
                        double rho = Math.Max(0, Math.Min(1.0, (1.0 - c1 * sqrtRatio) * sqrtRatio));
                        double he = rho * sec.h;
                        double reduction = 2 * (sec.h - he) * sec.tdes;
                        areaReduction += reduction;

                        p.Add($"  Web walls (stiffened HSS, c1={c1}, c2={c2}):");
                        p.Add($"    λ = {webLambda:F3} > λr*√(Fy/Fcr) = {hssLimit:F3} => REDUCE");
                        p.Add($"    Fel = {Fel:F2} ksi");
                        p.Add($"    ρ = {rho:F4}");
                        p.Add($"    he = ρ*h = {he:F3} in (each wall)");
                        p.Add($"    Area reduction = 2*(h-he)*tdes = {reduction:F3} in²");
                    }
                    else
                    {
                        p.Add($"  Web walls: λ = {webLambda:F3} <= λr*√(Fy/Fcr) = {hssLimit:F3} => FULLY EFFECTIVE (Eq. E7-2)");
                    }
                }

                Ae = sec.A - areaReduction;
                p.Add($"");
                p.Add($"  Ag = {sec.A:F3} in²");
                p.Add($"  Total area reduction = {areaReduction:F3} in²");
                p.Add($"  Ae = Ag - reduction = {Ae:F3} in²");
            }
            else
            {
                p.Add($"  All elements nonslender => Ae = Ag = {Ae:F3} in²");
            }

            result.Ae = Ae;
            p.Add("");

            // Step 7: Nominal strength
            p.Add("============================================");
            p.Add("-- Step 7: Nominal Strength Pn (E3-1 / E7) --");
            p.Add("============================================");

            double Pn = Fcr * Ae;
            result.Pn = Pn;

            p.Add($"  Pn = Fcr × Ae = {Fcr:F2} × {Ae:F3} = {Pn:F2} kips  (Eq. E3-1)");
            p.Add("");

            // Step 8: Design strength
            p.Add("============================================");
            p.Add("-- Step 8: Design Strength --");
            p.Add("============================================");

            double phi_c = 0.90;
            double omega_c = 1.67;
            double designStrength;

            if (input.MethodIndex == 0) // LRFD
            {
                designStrength = phi_c * Pn;
                p.Add($"  LRFD: φc×Pn = {phi_c} × {Pn:F2} = {designStrength:F2} kips");
            }
            else
            {
                designStrength = Pn / omega_c;
                p.Add($"  ASD: Pn/Ωc = {Pn:F2} / {omega_c} = {designStrength:F2} kips");
            }

            result.DesignStrength = designStrength;
            p.Add("");

            // Step 9: Design check
            p.Add("============================================");
            p.Add("-- Step 9: Design Check --");
            p.Add("============================================");

            result.Ratio = designStrength > 0 ? input.Pu / designStrength : 0;
            result.IsOK = result.Ratio <= 1.0;

            string compareOp = result.IsOK ? "<=" : ">";
            string passFail = result.IsOK ? "PASS" : "FAIL";

            p.Add($"  Required: {pLabel} = {input.Pu:F2} kips");
            p.Add($"  Available: {designStrength:F2} kips");
            p.Add($"  Ratio = {pLabel}/Available = {input.Pu:F2}/{designStrength:F2} = {result.Ratio:F4}");
            p.Add($"  {result.Ratio:F4} {compareOp} 1.0  [{passFail}]");
            p.Add("");

            // Summary
            result.Summary = $"Compression: {result.Ratio:F4} [{passFail}] | Fcr={Fcr:F1} ksi | {result.ControllingMode}" +
                (hasSlender ? $" | Ae={Ae:F2}/{sec.A:F2} in²" : "");

            p.Add("============================================");
            p.Add("-- FINAL RESULT --");
            p.Add("============================================");
            p.Add($"  Buckling mode: {result.ControllingMode}");
            p.Add($"  Fcr = {Fcr:F2} ksi");
            p.Add($"  Pn = {Pn:F2} kips");
            p.Add($"  Compression Ratio = {result.Ratio:F4}  [{passFail}]");

            result.IsValid = true;
            return result;
        }

        private static ElementClass ClassifyElement(double lambda, double lambda_r)
        {
            if (lambda <= lambda_r) return ElementClass.Nonslender;
            return ElementClass.Slender;
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
