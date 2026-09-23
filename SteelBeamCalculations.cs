using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class SteelBeamCalculations
    {
        public enum SectionShapeType { IShape, HSS }
        public enum DesignMethod { LRFD, ASD }
        public enum ElementClass { Compact, Noncompact, Slender }

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
            public double E = 29000;    // ksi
            public double Mu = 0;       // kip-ft
            public double Vu = 0;       // kips
            public double Lb = 0;       // ft
            public double Cb = 1.0;
            public DesignMethod Method = DesignMethod.LRFD;
            public bool IsRolled = true; // true for database shapes, false for custom
        }

        public class DesignResult
        {
            public bool IsValid = false;
            public string ErrorMessage = "";
            public List<string> Process = new();

            public double Mn_flexure = 0;          // kip-ft (nominal)
            public double DesignFlexStrength = 0;   // φMn or Mn/Ω (kip-ft)
            public double FlexuralRatio = 0;
            public bool FlexuralOK;

            public double Vn = 0;                   // kips (nominal)
            public double DesignShearStrength = 0;   // φVn or Vn/Ω (kips)
            public double ShearRatio = 0;
            public bool ShearOK;

            public string Summary = "";
        }

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var sec = input.Section;

            // Input validation
            if (sec.A <= 0 || sec.Zx <= 0 || sec.Sx <= 0)
            {
                result.ErrorMessage = "Invalid section properties: A, Zx, and Sx must be positive.";
                return result;
            }
            if (sec.ShapeType == SectionShapeType.IShape && (sec.d <= 0 || sec.tw <= 0 || sec.ry <= 0))
            {
                result.ErrorMessage = "Invalid I-shape dimensions: d, tw, and ry must be positive.";
                return result;
            }
            if (sec.ShapeType == SectionShapeType.HSS && (sec.Ht <= 0 || sec.B_hss <= 0 || sec.tdes <= 0))
            {
                result.ErrorMessage = "Invalid HSS dimensions: Ht, B, and tdes must be positive.";
                return result;
            }
            if (input.Fy <= 0 || input.E <= 0)
            {
                result.ErrorMessage = "Fy and E must be positive.";
                return result;
            }

            var Fy = input.Fy;
            var E = input.E;
            var p = result.Process;

            double Lb_in = input.Lb * 12.0;

            // Header
            p.Add("============================================");
            p.Add("  AISC 360-16 Steel Member Flexure & Shear");
            p.Add("============================================");
            p.Add("");

            // Input summary
            p.Add("-- Input Parameters --");
            p.Add($"  Section: {sec.Name}");
            p.Add($"  Type: {(sec.ShapeType == SectionShapeType.IShape ? "I-Shape (W)" : "Rectangular HSS")}");
            p.Add($"  Fy = {Fy} ksi,  E = {E} ksi");
            p.Add($"  Method: {input.Method}");
            string mLabel = input.Method == DesignMethod.LRFD ? "Mu" : "Ma";
            string vLabel = input.Method == DesignMethod.LRFD ? "Vu" : "Va";
            p.Add($"  {mLabel} = {input.Mu:F2} kip-ft,  {vLabel} = {input.Vu:F2} kips");
            p.Add($"  Lb = {input.Lb:F2} ft ({Lb_in:F2} in),  Cb = {input.Cb:F3}");
            p.Add("");

            // Section properties
            p.Add("-- Section Properties --");
            p.Add($"  A  = {sec.A:F3} in²");
            p.Add($"  Ix = {sec.Ix:F2} in⁴,  Iy = {sec.Iy:F2} in⁴");
            p.Add($"  Sx = {sec.Sx:F3} in³,  Sy = {sec.Sy:F3} in³");
            p.Add($"  Zx = {sec.Zx:F3} in³,  Zy = {sec.Zy:F3} in³");
            p.Add($"  rx = {sec.rx:F3} in,    ry = {sec.ry:F3} in");
            p.Add($"  J  = {sec.J:F4} in⁴");

            if (sec.ShapeType == SectionShapeType.IShape)
            {
                p.Add($"  Cw = {sec.Cw:F1} in⁶");
                p.Add($"  rts = {sec.rts:F4} in,  ho = {sec.ho:F3} in");
                p.Add($"  d = {sec.d:F3}, bf = {sec.bf:F3}, tw = {sec.tw:F3}, tf = {sec.tf:F3}");
                p.Add($"  h = {sec.h:F3} in,  bf/(2tf) = {sec.bf / (2 * sec.tf):F3},  h/tw = {sec.h / sec.tw:F3}");
            }
            else
            {
                p.Add($"  Ht = {sec.Ht:F3} in,  B = {sec.B_hss:F3} in,  tdes = {sec.tdes:F4} in");
                p.Add($"  h = {sec.h:F3} in (clear web),  b = {sec.b_flat:F3} in (clear flange)");
                p.Add($"  h/tdes = {sec.h / sec.tdes:F3},  b/tdes = {sec.b_flat / sec.tdes:F3}");
            }
            p.Add("");

            // Step 1: Classification
            p.Add("============================================");
            p.Add("-- Step 1: Section Classification (Table B4.1b) --");
            p.Add("============================================");

            double Mp = 0, phi_b = 0.90, omega_b = 1.67;
            double phi_v = 0.90, omega_v = 1.67;

            if (sec.ShapeType == SectionShapeType.IShape)
                CalculateIShape(input, sec, Fy, E, Lb_in, p, result, ref Mp, ref phi_v, ref omega_v);
            else
                CalculateHSS(input, sec, Fy, E, Lb_in, p, result, ref Mp, ref phi_v, ref omega_v);

            // Step 4: Design check
            p.Add("");
            p.Add("============================================");
            p.Add("-- Step 4: Design Strength & Check --");
            p.Add("============================================");

            if (input.Method == DesignMethod.LRFD)
            {
                result.DesignFlexStrength = phi_b * result.Mn_flexure;
                result.DesignShearStrength = phi_v * result.Vn;
                p.Add($"  LRFD:");
                p.Add($"    phi_b * Mn = {phi_b} x {result.Mn_flexure:F2} = {result.DesignFlexStrength:F2} kip-ft");
                p.Add($"    phi_v * Vn = {phi_v} x {result.Vn:F2} = {result.DesignShearStrength:F2} kips");
            }
            else
            {
                result.DesignFlexStrength = result.Mn_flexure / omega_b;
                result.DesignShearStrength = result.Vn / omega_v;
                p.Add($"  ASD:");
                p.Add($"    Mn / Omega_b = {result.Mn_flexure:F2} / {omega_b} = {result.DesignFlexStrength:F2} kip-ft");
                p.Add($"    Vn / Omega_v = {result.Vn:F2} / {omega_v} = {result.DesignShearStrength:F2} kips");
            }

            if (result.DesignFlexStrength > 0)
                result.FlexuralRatio = input.Mu / result.DesignFlexStrength;
            if (result.DesignShearStrength > 0)
                result.ShearRatio = input.Vu / result.DesignShearStrength;

            result.FlexuralOK = result.FlexuralRatio <= 1.0;
            result.ShearOK = result.ShearRatio <= 1.0;

            p.Add("");
            p.Add($"  Flexure: {mLabel} / Design = {input.Mu:F2} / {result.DesignFlexStrength:F2} = {result.FlexuralRatio:F4}");
            p.Add($"  Shear:   {vLabel} / Design = {input.Vu:F2} / {result.DesignShearStrength:F2} = {result.ShearRatio:F4}");
            p.Add("");

            string flexStatus = result.FlexuralOK ? "PASS" : "FAIL";
            string shearStatus = result.ShearOK ? "PASS" : "FAIL";
            p.Add($"  ** Flexure Ratio = {result.FlexuralRatio:F4}  [{flexStatus}]");
            p.Add($"  ** Shear Ratio   = {result.ShearRatio:F4}  [{shearStatus}]");

            result.Summary = $"Flexure: {result.FlexuralRatio:F4} [{flexStatus}]  |  Shear: {result.ShearRatio:F4} [{shearStatus}]";
            result.IsValid = true;
            return result;
        }

        private static void CalculateIShape(InputParameters input, SectionProperties sec,
            double Fy, double E, double Lb_in, List<string> p, DesignResult result,
            ref double Mp, ref double phi_v, ref double omega_v)
        {
            // Flange classification (Case 10)
            double flangeLambda = sec.bf / (2 * sec.tf);
            double lp_f = 0.38 * Math.Sqrt(E / Fy);
            double lr_f = 1.0 * Math.Sqrt(E / Fy);
            ElementClass flangeClass = ClassifyElement(flangeLambda, lp_f, lr_f);

            p.Add($"  Flange: lambda = bf/(2tf) = {flangeLambda:F3}");
            p.Add($"    lambda_p = {lp_f:F3},  lambda_r = {lr_f:F3}");
            p.Add($"    Flange classification: {flangeClass}");

            // Web classification (Case 15)
            double webLambda = sec.h / sec.tw;
            double lp_w = 3.76 * Math.Sqrt(E / Fy);
            double lr_w = 5.70 * Math.Sqrt(E / Fy);
            ElementClass webClass = ClassifyElement(webLambda, lp_w, lr_w);

            p.Add($"  Web: lambda = h/tw = {webLambda:F3}");
            p.Add($"    lambda_p = {lp_w:F3},  lambda_r = {lr_w:F3}");
            p.Add($"    Web classification: {webClass}");
            p.Add("");

            Mp = Fy * sec.Zx; // kip-in

            // Shear phi/omega (G2.1(a): phi=1.00 only for rolled I-shapes)
            double htLim224 = 2.24 * Math.Sqrt(E / Fy);
            if (webLambda <= htLim224 && input.IsRolled)
            {
                phi_v = 1.00;
                omega_v = 1.50;
            }
            else
            {
                phi_v = 0.90;
                omega_v = 1.67;
            }

            // Step 2: Flexural strength
            p.Add("-- Step 2: Flexural Strength --");

            double Mn_min = double.MaxValue;
            string ctrlLS = "";

            if (webClass == ElementClass.Compact && flangeClass == ElementClass.Compact)
            {
                p.Add("  Chapter F2: Doubly Symmetric Compact I-Shaped Members");
                p.Add("");

                // F2.1 Yielding
                double Mn_y = Mp;
                p.Add($"  [F2.1] Yielding (Eq. F2-1):");
                p.Add($"    Mn = Mp = Fy * Zx = {Fy} * {sec.Zx:F3} = {Mn_y:F1} kip-in ({Mn_y / 12:F2} kip-ft)");
                UpdateMin(ref Mn_min, Mn_y, "Yielding (F2-1)", ref ctrlLS);

                // F2.2 LTB
                double Lp = 1.76 * sec.ry * Math.Sqrt(E / Fy);
                double c = 1.0;
                double Lr = ComputeLr_IShape(sec, E, Fy, c);

                p.Add($"  [F2.2] Lateral-Torsional Buckling:");
                p.Add($"    Lp = 1.76*ry*sqrt(E/Fy) = {Lp:F2} in ({Lp / 12:F2} ft)  (Eq. F2-5)");
                p.Add($"    Lr = {Lr:F2} in ({Lr / 12:F2} ft)  (Eq. F2-6)");
                p.Add($"    Lb = {Lb_in:F2} in ({input.Lb:F2} ft)");

                if (Lb_in <= Lp)
                {
                    p.Add($"    Lb <= Lp => LTB does not govern");
                }
                else if (Lb_in <= Lr)
                {
                    double Mn_ltb = input.Cb * (Mp - (Mp - 0.7 * Fy * sec.Sx) * ((Lb_in - Lp) / (Lr - Lp)));
                    Mn_ltb = Math.Min(Mn_ltb, Mp);
                    p.Add($"    Lp < Lb <= Lr => Inelastic LTB (Eq. F2-2)");
                    p.Add($"    Mn = Cb*[Mp - (Mp-0.7*Fy*Sx)*(Lb-Lp)/(Lr-Lp)]");
                    p.Add($"       = {input.Cb:F3}*[{Mp:F1} - ({Mp:F1}-{0.7 * Fy * sec.Sx:F1})*({Lb_in - Lp:F2}/{Lr - Lp:F2})]");
                    p.Add($"       = {Mn_ltb:F1} kip-in ({Mn_ltb / 12:F2} kip-ft)");
                    UpdateMin(ref Mn_min, Mn_ltb, "LTB - Inelastic (F2-2)", ref ctrlLS);
                }
                else
                {
                    double Fcr = ComputeFcr_IShape(sec, E, Fy, input.Cb, Lb_in, c);
                    double Mn_ltb = Math.Min(Fcr * sec.Sx, Mp);
                    p.Add($"    Lb > Lr => Elastic LTB (Eq. F2-3, F2-4)");
                    p.Add($"    Fcr = {Fcr:F3} ksi");
                    p.Add($"    Mn = min(Fcr*Sx, Mp) = min({Fcr * sec.Sx:F1}, {Mp:F1}) = {Mn_ltb:F1} kip-in ({Mn_ltb / 12:F2} kip-ft)");
                    UpdateMin(ref Mn_min, Mn_ltb, "LTB - Elastic (F2-3)", ref ctrlLS);
                }
            }
            else if (webClass == ElementClass.Compact)
            {
                // F3: Compact web, noncompact/slender flange
                p.Add("  Chapter F3: Compact Web, Noncompact/Slender Flange I-Shaped Members");
                p.Add("");

                // F3.1 LTB (same as F2.2)
                double Lp = 1.76 * sec.ry * Math.Sqrt(E / Fy);
                double c = 1.0;
                double Lr = ComputeLr_IShape(sec, E, Fy, c);

                p.Add($"  [F3.1] Lateral-Torsional Buckling:");
                p.Add($"    Lp = {Lp:F2} in, Lr = {Lr:F2} in, Lb = {Lb_in:F2} in");

                if (Lb_in <= Lp)
                {
                    p.Add($"    Lb <= Lp => LTB does not govern");
                }
                else if (Lb_in <= Lr)
                {
                    double Mn_ltb = input.Cb * (Mp - (Mp - 0.7 * Fy * sec.Sx) * ((Lb_in - Lp) / (Lr - Lp)));
                    Mn_ltb = Math.Min(Mn_ltb, Mp);
                    p.Add($"    Mn(LTB) = {Mn_ltb:F1} kip-in (Eq. F2-2)");
                    UpdateMin(ref Mn_min, Mn_ltb, "LTB (F2-2)", ref ctrlLS);
                }
                else
                {
                    double Fcr = ComputeFcr_IShape(sec, E, Fy, input.Cb, Lb_in, c);
                    double Mn_ltb = Math.Min(Fcr * sec.Sx, Mp);
                    p.Add($"    Mn(LTB) = {Mn_ltb:F1} kip-in (Eq. F2-3)");
                    UpdateMin(ref Mn_min, Mn_ltb, "LTB (F2-3)", ref ctrlLS);
                }

                // F3.2 FLB
                p.Add($"  [F3.2] Compression Flange Local Buckling:");
                if (flangeClass == ElementClass.Noncompact)
                {
                    double Mn_flb = Mp - (Mp - 0.7 * Fy * sec.Sx) * ((flangeLambda - lp_f) / (lr_f - lp_f));
                    p.Add($"    Noncompact flange (Eq. F3-1):");
                    p.Add($"    Mn = {Mn_flb:F1} kip-in ({Mn_flb / 12:F2} kip-ft)");
                    UpdateMin(ref Mn_min, Mn_flb, "FLB - Noncompact (F3-1)", ref ctrlLS);
                }
                else
                {
                    double kc = Math.Max(0.35, Math.Min(0.76, 4.0 / Math.Sqrt(webLambda)));
                    double Mn_flb = 0.69 * E * kc * sec.Sx / (flangeLambda * flangeLambda);
                    p.Add($"    Slender flange (Eq. F3-2):");
                    p.Add($"    kc = {kc:F4}, Mn = {Mn_flb:F1} kip-in ({Mn_flb / 12:F2} kip-ft)");
                    UpdateMin(ref Mn_min, Mn_flb, "FLB - Slender (F3-2)", ref ctrlLS);
                }
            }
            else
            {
                p.Add("  Chapter F4/F5: Noncompact/Slender Web I-Shaped Members");
                p.Add("  WARNING: Full F4/F5 calculation not implemented in this version.");
                p.Add("  Using conservative simplified approach: Mn = Fy * Sx (<= Mp)");
                p.Add("  Note: For plate girders with slender webs, Rpg reduction (F5-1) may apply.");
                p.Add("  Consider using a compact web section for reliable results.");
                double Mn_simple = Fy * sec.Sx;
                UpdateMin(ref Mn_min, Mn_simple, "Simplified (Fy*Sx)", ref ctrlLS);
            }

            result.Mn_flexure = Mn_min / 12.0;
            p.Add("");
            p.Add($"  Controlling limit state: {ctrlLS}");
            p.Add($"  Mn = {Mn_min:F1} kip-in = {Mn_min / 12:F2} kip-ft");

            // Step 3: Shear
            p.Add("");
            p.Add("-- Step 3: Shear Strength (Chapter G2) --");

            double Aw = sec.d * sec.tw;
            double Cv1;
            double htw = sec.h / sec.tw;

            if (htw <= htLim224 && input.IsRolled)
            {
                Cv1 = 1.0;
                p.Add($"  G2.1(a): Rolled I-shape, h/tw = {htw:F3} <= 2.24*sqrt(E/Fy) = {htLim224:F3}");
                p.Add($"    phi_v = 1.00, Cv1 = 1.0");
            }
            else if (htw <= htLim224 && !input.IsRolled)
            {
                Cv1 = 1.0;
                p.Add($"  G2.1(b): Built-up I-shape, h/tw = {htw:F3} <= 2.24*sqrt(E/Fy) = {htLim224:F3}");
                p.Add($"    phi_v = 0.90 (custom section treated as built-up), Cv1 = 1.0");
            }
            else
            {
                double kv = 5.34;
                double htLim_kv = 1.10 * Math.Sqrt(kv * E / Fy);
                p.Add($"  G2.1(b): h/tw = {htw:F3} > 2.24*sqrt(E/Fy) = {htLim224:F3}");
                p.Add($"    phi_v = 0.90, kv = {kv}");

                if (htw <= htLim_kv)
                {
                    Cv1 = 1.0;
                    p.Add($"    h/tw <= 1.10*sqrt(kv*E/Fy) = {htLim_kv:F3}");
                    p.Add($"    Cv1 = 1.0 (Eq. G2-3)");
                }
                else
                {
                    Cv1 = Math.Min(1.10 * Math.Sqrt(kv * E / Fy) / htw, 1.0);
                    p.Add($"    h/tw > 1.10*sqrt(kv*E/Fy) = {htLim_kv:F3}");
                    p.Add($"    Cv1 = {Cv1:F4} (Eq. G2-4)");
                }
            }

            double Vn = 0.6 * Fy * Aw * Cv1;
            result.Vn = Vn;

            p.Add($"    Aw = d * tw = {sec.d:F3} * {sec.tw:F3} = {Aw:F3} in²");
            p.Add($"    Vn = 0.6*Fy*Aw*Cv1 = 0.6*{Fy}*{Aw:F3}*{Cv1:F4} = {Vn:F2} kips (Eq. G2-1)");
        }

        private static void CalculateHSS(InputParameters input, SectionProperties sec,
            double Fy, double E, double Lb_in, List<string> p, DesignResult result,
            ref double Mp, ref double phi_v, ref double omega_v)
        {
            phi_v = 0.90;
            omega_v = 1.67;

            // Flange classification (Case 17)
            double flangeLambda = sec.b_flat / sec.tdes;
            double lp_f = 1.12 * Math.Sqrt(E / Fy);
            double lr_f = 1.40 * Math.Sqrt(E / Fy);
            ElementClass flangeClass = ClassifyElement(flangeLambda, lp_f, lr_f);

            p.Add($"  Flange: lambda = b/tdes = {flangeLambda:F3}");
            p.Add($"    lambda_p = {lp_f:F3},  lambda_r = {lr_f:F3}");
            p.Add($"    Flange classification: {flangeClass}");

            // Web classification (Case 19)
            double webLambda = sec.h / sec.tdes;
            double lp_w = 2.42 * Math.Sqrt(E / Fy);
            double lr_w = 5.70 * Math.Sqrt(E / Fy);
            ElementClass webClass = ClassifyElement(webLambda, lp_w, lr_w);

            p.Add($"  Web: lambda = h/tdes = {webLambda:F3}");
            p.Add($"    lambda_p = {lp_w:F3},  lambda_r = {lr_w:F3}");
            p.Add($"    Web classification: {webClass}");
            p.Add("");

            Mp = Fy * sec.Zx;

            // Step 2: Flexural - Chapter F7
            p.Add("-- Step 2: Flexural Strength (Chapter F7) --");
            p.Add("");

            double Mn_min = double.MaxValue;
            string ctrlLS = "";

            // F7.1 Yielding
            double Mn_y = Mp;
            p.Add($"  [F7.1] Yielding (Eq. F7-1):");
            p.Add($"    Mn = Mp = Fy * Zx = {Fy} * {sec.Zx:F3} = {Mn_y:F1} kip-in ({Mn_y / 12:F2} kip-ft)");
            UpdateMin(ref Mn_min, Mn_y, "Yielding (F7-1)", ref ctrlLS);

            // F7.2 FLB
            p.Add($"  [F7.2] Flange Local Buckling:");
            if (flangeClass == ElementClass.Compact)
            {
                p.Add($"    Compact flange => FLB does not govern");
            }
            else if (flangeClass == ElementClass.Noncompact)
            {
                double Mn_flb = Mp - (Mp - Fy * sec.Sx) * (3.57 * flangeLambda * Math.Sqrt(Fy / E) - 4.0);
                Mn_flb = Math.Min(Mn_flb, Mp);
                p.Add($"    Noncompact flange (Eq. F7-2):");
                p.Add($"    Mn = {Mn_flb:F1} kip-in ({Mn_flb / 12:F2} kip-ft)");
                UpdateMin(ref Mn_min, Mn_flb, "FLB - Noncompact (F7-2)", ref ctrlLS);
            }
            else
            {
                double be = 1.92 * sec.tdes * Math.Sqrt(E / Fy) * (1.0 - 0.38 / flangeLambda * Math.Sqrt(E / Fy));
                be = Math.Min(be, sec.b_flat);

                // Compute effective section modulus with neutral axis shift
                // When compression flange effective width reduces, NA shifts toward tension flange
                double t = sec.tdes;
                double H = sec.Ht;
                double B = sec.B_hss;
                double b_eff = be;
                double b_reduced = sec.b_flat - be;

                // Full section properties for reference
                double h_int = H - 2 * t;
                double b_int = B - 2 * t;

                // Effective section: compression flange width reduced from b_flat to be
                // We compute from scratch using effective compression flange width
                // Compression flange (top): width = be, at y = H - t/2
                // Tension flange (bottom): width = b_flat (= B - 3t), at y = t/2
                // Webs: 2 webs of thickness t, height h_int
                // Note: we use corner radii approximation consistent with b_flat = B - 3t

                // Area of each element
                double A_cf = b_eff * t;
                double A_tf = sec.b_flat * t;
                double A_web = 2 * h_int * t;
                double A_eff = A_cf + A_tf + A_web;

                // Centroid from bottom (y_bar)
                double y_cf = H - t / 2.0;
                double y_tf = t / 2.0;
                double y_web = H / 2.0;
                double y_bar = (A_cf * y_cf + A_tf * y_tf + A_web * y_web) / A_eff;

                // Moment of inertia about effective centroid
                double Ix_cf = b_eff * t * t * t / 12 + A_cf * (y_cf - y_bar) * (y_cf - y_bar);
                double Ix_tf = sec.b_flat * t * t * t / 12 + A_tf * (y_tf - y_bar) * (y_tf - y_bar);
                double Ix_web = 2 * (t * h_int * h_int * h_int / 12 + h_int * t * (y_web - y_bar) * (y_web - y_bar));
                double Ix_eff = Ix_cf + Ix_tf + Ix_web;

                // Se = Ix_eff / max(y_bar, H - y_bar)
                double y_max = Math.Max(y_bar, H - y_bar);
                double Se = Ix_eff / y_max;

                double Mn_flb = Fy * Se;
                p.Add($"    Slender flange (Eq. F7-3, F7-4):");
                p.Add($"    be = 1.92*t*sqrt(E/Fy)*(1-0.38/(b/t)*sqrt(E/Fy)) = {be:F3} in");
                p.Add($"    Effective centroid: y_bar = {y_bar:F3} in (NA shift = {y_bar - H / 2.0:F3} in)");
                p.Add($"    Ix_eff = {Ix_eff:F2} in⁴");
                p.Add($"    y_max = max(y_bar, H-y_bar) = {y_max:F3} in");
                p.Add($"    Se = Ix_eff / y_max = {Se:F3} in³");
                p.Add($"    Mn = Fy*Se = {Mn_flb:F1} kip-in ({Mn_flb / 12:F2} kip-ft)");
                UpdateMin(ref Mn_min, Mn_flb, "FLB - Slender (F7-3)", ref ctrlLS);
            }

            // F7.3 WLB
            p.Add($"  [F7.3] Web Local Buckling:");
            if (webClass == ElementClass.Compact)
            {
                p.Add($"    Compact web => WLB does not govern");
            }
            else if (webClass == ElementClass.Noncompact)
            {
                double Mn_wlb = Mp - (Mp - Fy * sec.Sx) * (0.305 * webLambda * Math.Sqrt(Fy / E) - 0.738);
                Mn_wlb = Math.Min(Mn_wlb, Mp);
                p.Add($"    Noncompact web (Eq. F7-6):");
                p.Add($"    Mn = {Mn_wlb:F1} kip-in ({Mn_wlb / 12:F2} kip-ft)");
                UpdateMin(ref Mn_min, Mn_wlb, "WLB - Noncompact (F7-6)", ref ctrlLS);
            }
            else
            {
                p.Add($"    Slender web => See Chapter F5 (not fully implemented)");
            }

            // F7.4 LTB
            p.Add($"  [F7.4] Lateral-Torsional Buckling:");
            bool isSquare = Math.Abs(sec.Ht - sec.B_hss) < 0.01;

            if (isSquare)
            {
                p.Add($"    Square section => LTB does not govern");
            }
            else if (Mp > 0)
            {
                double Lp_hss = 0.13 * E * sec.ry * Math.Sqrt(J_nonzero(sec.J) * sec.A) / Mp;
                double Lr_hss = 2 * E * sec.ry * Math.Sqrt(J_nonzero(sec.J) * sec.A) / (0.7 * Fy * sec.Sx);

                p.Add($"    Lp = 0.13*E*ry*sqrt(J*A)/Mp = {Lp_hss:F2} in ({Lp_hss / 12:F2} ft)  (Eq. F7-12)");
                p.Add($"    Lr = 2*E*ry*sqrt(J*A)/(0.7*Fy*Sx) = {Lr_hss:F2} in ({Lr_hss / 12:F2} ft)  (Eq. F7-13)");
                p.Add($"    Lb = {Lb_in:F2} in ({input.Lb:F2} ft)");

                if (Lb_in <= Lp_hss)
                {
                    p.Add($"    Lb <= Lp => LTB does not govern");
                }
                else if (Lb_in <= Lr_hss)
                {
                    double Mn_ltb = input.Cb * (Mp - (Mp - 0.7 * Fy * sec.Sx) * ((Lb_in - Lp_hss) / (Lr_hss - Lp_hss)));
                    Mn_ltb = Math.Min(Mn_ltb, Mp);
                    p.Add($"    Lp < Lb <= Lr => Inelastic LTB (Eq. F7-10)");
                    p.Add($"    Mn = {Mn_ltb:F1} kip-in ({Mn_ltb / 12:F2} kip-ft)");
                    UpdateMin(ref Mn_min, Mn_ltb, "LTB - Inelastic (F7-10)", ref ctrlLS);
                }
                else
                {
                    double sqrtJA = Math.Sqrt(J_nonzero(sec.J) * sec.A);
                    double Mn_ltb = 2 * E * input.Cb * sqrtJA / (Lb_in / sec.ry);
                    Mn_ltb = Math.Min(Mn_ltb, Mp);
                    p.Add($"    Lb > Lr => Elastic LTB (Eq. F7-11)");
                    p.Add($"    Mn = 2*E*Cb*sqrt(J*A)/(Lb/ry) = {Mn_ltb:F1} kip-in ({Mn_ltb / 12:F2} kip-ft)");
                    UpdateMin(ref Mn_min, Mn_ltb, "LTB - Elastic (F7-11)", ref ctrlLS);
                }
            }
            else
            {
                p.Add($"    Cannot compute LTB (Mp = 0)");
            }

            result.Mn_flexure = Mn_min / 12.0;
            p.Add("");
            p.Add($"  Controlling limit state: {ctrlLS}");
            p.Add($"  Mn = {Mn_min:F1} kip-in = {Mn_min / 12:F2} kip-ft");

            // Step 3: Shear - Chapter G4
            p.Add("");
            p.Add("-- Step 3: Shear Strength (Chapter G4) --");

            double h_shear = sec.h;
            double Aw_hss = 2 * h_shear * sec.tdes;
            double ht_ratio = h_shear / sec.tdes;
            double kv = 5.0;

            p.Add($"    h = {h_shear:F3} in,  tdes = {sec.tdes:F4} in,  h/t = {ht_ratio:F3}");
            p.Add($"    kv = {kv},  Aw = 2*h*t = {Aw_hss:F3} in²");

            double Cv2;
            double lim1 = 1.10 * Math.Sqrt(kv * E / Fy);
            double lim2 = 1.37 * Math.Sqrt(kv * E / Fy);

            if (ht_ratio <= lim1)
            {
                Cv2 = 1.0;
                p.Add($"    h/t <= 1.10*sqrt(kv*E/Fy) = {lim1:F3} => Cv2 = 1.0 (Eq. G2-9)");
            }
            else if (ht_ratio <= lim2)
            {
                Cv2 = 1.10 * Math.Sqrt(kv * E / Fy) / ht_ratio;
                p.Add($"    1.10*sqrt(kv*E/Fy) < h/t <= 1.37*sqrt(kv*E/Fy)");
                p.Add($"    Cv2 = {Cv2:F4} (Eq. G2-10)");
            }
            else
            {
                Cv2 = 1.51 * kv * E / (ht_ratio * ht_ratio * Fy);
                p.Add($"    h/t > 1.37*sqrt(kv*E/Fy) = {lim2:F3}");
                p.Add($"    Cv2 = 1.51*kv*E/((h/t)^2*Fy) = {Cv2:F4} (Eq. G2-11)");
            }

            double Vn = 0.6 * Fy * Aw_hss * Cv2;
            result.Vn = Vn;

            p.Add($"    Vn = 0.6*Fy*Aw*Cv2 = 0.6*{Fy}*{Aw_hss:F3}*{Cv2:F4} = {Vn:F2} kips (Eq. G4-1)");
        }

        #region Helper Methods

        private static ElementClass ClassifyElement(double lambda, double lambda_p, double lambda_r)
        {
            if (lambda <= lambda_p) return ElementClass.Compact;
            if (lambda <= lambda_r) return ElementClass.Noncompact;
            return ElementClass.Slender;
        }

        private static double ComputeLr_IShape(SectionProperties sec, double E, double Fy, double c)
        {
            double term1 = (sec.J * c) / (sec.Sx * sec.ho);
            double term2 = Math.Sqrt(term1 * term1 + 6.76 * Math.Pow(0.7 * Fy / E, 2));
            return 1.95 * sec.rts * (E / (0.7 * Fy)) * Math.Sqrt(term1 + term2);
        }

        private static double ComputeFcr_IShape(SectionProperties sec, double E, double Fy,
            double Cb, double Lb_in, double c)
        {
            double ratio = Lb_in / sec.rts;
            return (Cb * Math.PI * Math.PI * E) / (ratio * ratio) *
                Math.Sqrt(1 + 0.078 * (sec.J * c) / (sec.Sx * sec.ho) * (ratio * ratio));
        }

        private static double J_nonzero(double J) => Math.Max(J, 1e-10);

        private static void UpdateMin(ref double current, double candidate, string name, ref string ctrlLS)
        {
            if (candidate < current)
            {
                current = candidate;
                ctrlLS = name;
            }
        }

        #endregion

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
