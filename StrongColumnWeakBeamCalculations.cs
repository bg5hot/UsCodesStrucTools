using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class StrongColumnWeakBeamCalculations
    {
        #region Enums

        public enum DesignMethod { LRFD, ASD }

        #endregion

        #region Input / Result

        public class InputParameters
        {
            public DesignMethod Method = DesignMethod.LRFD;

            // Column above joint
            public double ZcAbove = 0;     // plastic section modulus (in³)
            public double AgAbove = 0;     // gross area (in²)
            public double FycAbove = 50;   // specified minimum yield stress (ksi)
            public double PrAbove = 0;     // required axial compressive strength (kips)

            // Column below joint
            public double ZcBelow = 0;
            public double AgBelow = 0;
            public double FycBelow = 50;
            public double PrBelow = 0;
            public bool SameColumnBelow = true;

            // Beam left side
            public double MprLeft = 0;     // maximum probable moment at plastic hinge (kip-in)
            public double MvLeft = 0;      // additional moment due to shear amplification (kip-in)

            // Beam right side
            public double MprRight = 0;
            public double MvRight = 0;
            public bool SameBeamRight = true;
        }

        public class CheckResult
        {
            public bool IsValid;
            public string ErrorMessage = "";

            public double AlphaS;
            public double MpcAbove;
            public double MpcBelow;
            public double SigmaMpc;
            public double MpbLeft;
            public double MpbRight;
            public double SigmaMpb;
            public double Ratio;
            public bool Pass;

            // Exception check values
            public double PcAbove;
            public double PcBelow;
            public double PrPcRatioAbove;
            public double PrPcRatioBelow;
            public bool ExemptAbove;
            public bool ExemptBelow;

            public List<string> Process = new();
        }

        #endregion

        #region Main Calculate

        public static CheckResult Calculate(InputParameters input)
        {
            var result = new CheckResult();
            var p = result.Process;

            double alphaS = input.Method == DesignMethod.LRFD ? 1.0 : 1.5;
            result.AlphaS = alphaS;
            string methodName = input.Method == DesignMethod.LRFD ? "LRFD" : "ASD";

            // Resolve effective values
            double ZcBelow = input.SameColumnBelow ? input.ZcAbove : input.ZcBelow;
            double AgBelow = input.SameColumnBelow ? input.AgAbove : input.AgBelow;
            double FycBelow = input.SameColumnBelow ? input.FycAbove : input.FycBelow;
            double PrBelow = input.SameColumnBelow ? input.PrAbove : input.PrBelow;

            double MprRight = input.SameBeamRight ? input.MprLeft : input.MprRight;
            double MvRight = input.SameBeamRight ? input.MvLeft : input.MvRight;

            // Fix 1: allow roof joint (no column above) or base joint (no column below)
            bool hasColumnAbove = input.ZcAbove > 0 && input.AgAbove > 0;
            bool hasColumnBelow = !input.SameColumnBelow
                ? ZcBelow > 0 && AgBelow > 0
                : hasColumnAbove;

            if (!hasColumnAbove && !hasColumnBelow)
            {
                result.IsValid = false;
                result.ErrorMessage = "至少需要一侧（上或下）的柱 Zc 和 Ag 大于 0。";
                return result;
            }
            if (hasColumnAbove && input.FycAbove <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "上柱 Fyc 必须大于 0。";
                return result;
            }
            if (hasColumnBelow && !input.SameColumnBelow && FycBelow <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "下柱 Fyc 必须大于 0。";
                return result;
            }
            if (input.MprLeft < 0 || MprRight < 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Mpr 不能为负值。";
                return result;
            }
            if (input.MprLeft <= 0 && MprRight <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "至少一侧梁的 Mpr 必须大于 0。";
                return result;
            }

            // Fix 2: clamp Pr to non-negative (spec defines Pr as compressive strength)
            double PrAboveClean = Math.Max(0, input.PrAbove);
            double PrBelowClean = Math.Max(0, PrBelow);

            // Header
            p.Add("═══════════════════════════════════════════════════");
            p.Add("  AISC 341-16 §E3.4a SMF 强柱弱梁校核");
            p.Add("  Strong Column-Weak Beam Check");
            p.Add("  Special Moment Frames (SMF)");
            p.Add("═══════════════════════════════════════════════════");
            p.Add("");

            // Step 1: Design method
            p.Add("── 1. 设计方法 ──");
            p.Add($"   设计方法: {methodName}");
            p.Add($"   αs = {alphaS:F1}");
            p.Add("");

            // Step 2: Column parameters
            p.Add("── 2. 柱参数 (Column Parameters) ──");
            if (hasColumnAbove)
            {
                p.Add("   上柱 (Column Above):");
                p.Add($"     Zc  = {input.ZcAbove:F2} in³  (塑性截面模量)");
                p.Add($"     Ag  = {input.AgAbove:F2} in²  (毛截面面积)");
                p.Add($"     Fyc = {input.FycAbove:F1} ksi  (屈服强度)");
                p.Add($"     Pr  = {input.PrAbove:F1} kips  (所需轴压强度)");
                if (input.PrAbove < 0)
                    p.Add($"     Pr < 0 (受拉), 计算取 Pr = {PrAboveClean:F1} kips");
            }
            else
            {
                p.Add("   上柱: 无 (如顶层屋面节点)");
            }
            p.Add("");

            if (input.SameColumnBelow && hasColumnAbove)
            {
                p.Add("   下柱 (Column Below): 同上柱");
            }
            else if (hasColumnBelow)
            {
                p.Add("   下柱 (Column Below):");
                p.Add($"     Zc  = {ZcBelow:F2} in³");
                p.Add($"     Ag  = {AgBelow:F2} in²");
                p.Add($"     Fyc = {FycBelow:F1} ksi");
                p.Add($"     Pr  = {PrBelow:F1} kips");
                if (PrBelow < 0)
                    p.Add($"     Pr < 0 (受拉), 计算取 Pr = {PrBelowClean:F1} kips");
            }
            else
            {
                p.Add("   下柱: 无");
            }
            p.Add("");

            // Step 3: Beam parameters
            p.Add("── 3. 梁参数 (Beam Parameters) ──");
            p.Add("   Mpr = 塑性铰位置最大可能弯矩 (per AISC 358)");
            p.Add("   Mv  = 塑性铰到柱中心线剪力放大附加弯矩");
            p.Add("");

            p.Add("   左梁 (Beam Left):");
            p.Add($"     Mpr = {input.MprLeft:F1} kip-in");
            p.Add($"     Mv  = {input.MvLeft:F1} kip-in");
            p.Add("");

            if (input.SameBeamRight)
            {
                p.Add("   右梁 (Beam Right): 同左梁");
            }
            else
            {
                p.Add("   右梁 (Beam Right):");
                p.Add($"     Mpr = {MprRight:F1} kip-in");
                p.Add($"     Mv  = {MvRight:F1} kip-in");
            }
            p.Add("");

            // Step 4: Calculate ΣM*pc (Eq. E3-2)
            p.Add("── 4. 柱名义抗弯强度 ΣM*pc ──");
            p.Add("   §E3.4a Eq. E3-2:");
            p.Add("   ΣM*pc = Σ Zc × max(0, Fyc − αs × Pr / Ag)");
            p.Add("");

            double MpcAbove = 0;
            double MpcBelow = 0;

            if (hasColumnAbove)
            {
                // Fix 3: clamp residual stress to non-negative
                double termAbove = Math.Max(0, input.FycAbove - alphaS * PrAboveClean / input.AgAbove);
                MpcAbove = input.ZcAbove * termAbove;

                p.Add("   上柱:");
                p.Add($"     M*pc = Zc × max(0, Fyc − αs × Pr / Ag)");
                p.Add($"          = {input.ZcAbove:F2} × max(0, {input.FycAbove:F1} − {alphaS:F1} × {PrAboveClean:F1} / {input.AgAbove:F2})");
                p.Add($"          = {input.ZcAbove:F2} × max(0, {input.FycAbove - alphaS * PrAboveClean / input.AgAbove:F2})");
                p.Add($"          = {input.ZcAbove:F2} × {termAbove:F2}");
                p.Add($"          = {MpcAbove:F1} kip-in");
                if (termAbove <= 0)
                    p.Add("     ⚠ 上柱受压达到或超过屈服强度，剩余抗弯能力记为 0");
                p.Add("");
            }
            else
            {
                p.Add("   上柱: 无 → M*pc = 0");
                p.Add("");
            }

            if (hasColumnBelow)
            {
                double termBelow = Math.Max(0, FycBelow - alphaS * PrBelowClean / AgBelow);
                MpcBelow = ZcBelow * termBelow;

                p.Add("   下柱:");
                p.Add($"     M*pc = {ZcBelow:F2} × max(0, {FycBelow:F1} − {alphaS:F1} × {PrBelowClean:F1} / {AgBelow:F2})");
                p.Add($"          = {ZcBelow:F2} × max(0, {FycBelow - alphaS * PrBelowClean / AgBelow:F2})");
                p.Add($"          = {ZcBelow:F2} × {termBelow:F2}");
                p.Add($"          = {MpcBelow:F1} kip-in");
                if (termBelow <= 0)
                    p.Add("     ⚠ 下柱受压达到或超过屈服强度，剩余抗弯能力记为 0");
                p.Add("");
            }
            else
            {
                p.Add("   下柱: 无 → M*pc = 0");
                p.Add("");
            }

            result.MpcAbove = MpcAbove;
            result.MpcBelow = MpcBelow;
            result.SigmaMpc = MpcAbove + MpcBelow;

            p.Add($"   ΣM*pc = {MpcAbove:F1} + {MpcBelow:F1} = {result.SigmaMpc:F1} kip-in");
            p.Add("");

            // Step 5: Calculate ΣM*pb (Eq. E3-3)
            p.Add("── 5. 梁预期抗弯强度 ΣM*pb ──");
            p.Add("   §E3.4a Eq. E3-3:");
            p.Add("   ΣM*pb = Σ (Mpr + αs × Mv)");
            p.Add("");

            double MpbLeft = input.MprLeft + alphaS * input.MvLeft;
            double MpbRight = MprRight + alphaS * MvRight;
            result.MpbLeft = MpbLeft;
            result.MpbRight = MpbRight;
            result.SigmaMpb = MpbLeft + MpbRight;

            p.Add("   左梁:");
            p.Add($"     M*pb = Mpr + αs × Mv");
            p.Add($"          = {input.MprLeft:F1} + {alphaS:F1} × {input.MvLeft:F1}");
            p.Add($"          = {input.MprLeft:F1} + {alphaS * input.MvLeft:F1}");
            p.Add($"          = {MpbLeft:F1} kip-in");
            p.Add("");

            p.Add("   右梁:");
            p.Add($"     M*pb = {MprRight:F1} + {alphaS:F1} × {MvRight:F1}");
            p.Add($"          = {MprRight:F1} + {alphaS * MvRight:F1}");
            p.Add($"          = {MpbRight:F1} kip-in");
            p.Add("");

            p.Add($"   ΣM*pb = {MpbLeft:F1} + {MpbRight:F1} = {result.SigmaMpb:F1} kip-in");
            p.Add("");

            // Early validation for ratio calculation
            if (result.SigmaMpb <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "ΣM*pb ≤ 0，请检查梁 Mpr 和 Mv 输入。";
                return result;
            }
            if (result.SigmaMpc <= 0)
            {
                p.Add($"   ⚠ ΣM*pc = {result.SigmaMpc:F1} ≤ 0，柱轴力过大或无有效柱。");
                result.Ratio = 0;
                result.Pass = false;
                p.Add($"   比值 = 0.000 ≤ 1.0 → ✗ 不满足 (FAIL)");
                p.Add("");
                result.IsValid = true;
                return result;
            }

            // Step 6: Ratio check
            p.Add("── 6. 强柱弱梁比值校核 (Moment Ratio Check) ──");
            p.Add("   §E3.4a Eq. E3-1:");
            p.Add("   ΣM*pc / ΣM*pb > 1.0");
            p.Add("");

            result.Ratio = result.SigmaMpc / result.SigmaMpb;
            result.Pass = result.Ratio > 1.0;

            p.Add($"   ΣM*pc / ΣM*pb = {result.SigmaMpc:F1} / {result.SigmaMpb:F1}");
            p.Add($"                   = {result.Ratio:F3}");
            p.Add("");

            if (result.Pass)
                p.Add($"   校核: {result.Ratio:F3} > 1.0 → ✓ 通过 (PASS)");
            else
                p.Add($"   校核: {result.Ratio:F3} ≤ 1.0 → ✗ 不满足 (FAIL)");
            p.Add("");

            // Step 7: Exception check
            p.Add("── 7. 例外情况检查 (Exception Check) ──");
            p.Add("   以下条件满足时，Eq. E3-1 不适用:");
            p.Add("");

            result.ExemptAbove = true;
            if (hasColumnAbove)
            {
                result.PcAbove = input.FycAbove * input.AgAbove / alphaS;
                result.PrPcRatioAbove = PrAboveClean / result.PcAbove;
                result.ExemptAbove = result.PrPcRatioAbove < 0.3;

                p.Add("   上柱 (Eq. E3-5):");
                p.Add($"     Pc = Fyc × Ag / αs = {input.FycAbove:F1} × {input.AgAbove:F2} / {alphaS:F1}");
                p.Add($"        = {result.PcAbove:F1} kips");
                p.Add($"     Pr / Pc = {PrAboveClean:F1} / {result.PcAbove:F1} = {result.PrPcRatioAbove:F3}");
                p.Add($"     Pr/Pc < 0.3? → {(result.ExemptAbove ? "是 (Yes)" : "否 (No)")}");
                p.Add("");
            }

            result.ExemptBelow = true;
            if (hasColumnBelow)
            {
                result.PcBelow = FycBelow * AgBelow / alphaS;
                result.PrPcRatioBelow = PrBelowClean / result.PcBelow;
                result.ExemptBelow = result.PrPcRatioBelow < 0.3;

                p.Add("   下柱 (Eq. E3-5):");
                p.Add($"     Pc = Fyc × Ag / αs = {FycBelow:F1} × {AgBelow:F2} / {alphaS:F1}");
                p.Add($"        = {result.PcBelow:F1} kips");
                p.Add($"     Pr / Pc = {PrBelowClean:F1} / {result.PcBelow:F1} = {result.PrPcRatioBelow:F3}");
                p.Add($"     Pr/Pc < 0.3? → {(result.ExemptBelow ? "是 (Yes)" : "否 (No)")}");
                p.Add("");
            }

            p.Add("   例外条件 (a): 柱 Pr/Pc < 0.3（非超强荷载组合），且满足:");
            p.Add("     (1) 单层建筑或多层建筑顶层; 或");
            p.Add("     (2) 豁免柱总抗剪强度 < 框架柱总抗剪强度的 20%，");
            p.Add("         且每个柱列线上豁免柱抗剪强度 < 该柱列总抗剪强度的 33%");
            p.Add("");
            p.Add("   例外条件 (b): 该层抗剪强度/需求比较上一层大 50%");
            p.Add("");

            if (!hasColumnAbove)
            {
                p.Add("   ⓘ 顶层（屋面）节点，符合例外条件 (a)(1) 的豁免要求。");
            }
            else if (result.ExemptAbove && result.ExemptBelow)
            {
                p.Add("   ⓘ 上柱和下柱均满足 Pr/Pc < 0.3，可考虑例外条件 (a)。");
            }
            else if (result.ExemptAbove || result.ExemptBelow)
            {
                p.Add("   ⓘ 仅部分柱满足 Pr/Pc < 0.3，请结合工程条件判断是否适用例外。");
            }
            else
            {
                p.Add("   所有柱 Pr/Pc ≥ 0.3，不适用例外条件 (a)。");
            }
            p.Add("");

            // Summary
            p.Add("── 8. 校核总结 ──");
            p.Add("");

            p.Add($"   ΣM*pc = {result.SigmaMpc:F1} kip-in");
            p.Add($"   ΣM*pb = {result.SigmaMpb:F1} kip-in");
            p.Add($"   比值  = {result.Ratio:F3}");
            p.Add("");

            if (result.Pass)
            {
                p.Add("   结论: ✓ 满足 §E3.4a 强柱弱梁要求");
            }
            else
            {
                p.Add("   结论: ✗ 不满足 §E3.4a 强柱弱梁要求");
                p.Add("");
                p.Add("   建议:");
                p.Add("   - 增大柱截面（增大 Zc）");
                p.Add("   - 降低柱轴压比（减小 Pr）");
                p.Add("   - 减小梁截面（降低 Mpr）");
                p.Add("   - 确认是否符合 §E3.4a 例外条件");
            }

            result.IsValid = true;
            return result;
        }

        #endregion
    }
}
