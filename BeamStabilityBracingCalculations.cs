using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class BeamStabilityBracingCalculations
    {
        public enum SeismicSystem { SMF, IMF, SCBF, EBF, BRBF }
        public enum MemberRole { Beam, Link }
        public enum DesignMethod { LRFD, ASD }
        public enum BraceType { LateralNodal, LateralRelative, Torsional }
        public enum DuctilityLevel { ModeratelyDuctile, HighlyDuctile }

        public class InputParameters
        {
            public SeismicSystem System { get; set; }
            public MemberRole Member { get; set; }
            public DesignMethod Method { get; set; }

            public double Zx { get; set; }
            public double RyRadius { get; set; }
            public double Ho { get; set; }
            public double Iy { get; set; }
            public double Tw { get; set; }

            public double Fy { get; set; }
            public double E { get; set; }
            public double Ry { get; set; }

            public double Lb { get; set; }

            public BraceType Brace { get; set; }
            public bool AtPlasticHinge { get; set; }

            public double Mr { get; set; }
            public bool UseSeismicMr { get; set; } = true;

            public double Cd { get; set; } = 1.0;

            public double IyEff { get; set; }
            public double Cb { get; set; } = 1.0;
            public double LSpan { get; set; }
            public int NBraces { get; set; } = 1;
            public double Tst { get; set; }
            public double Bs { get; set; }
            public bool FullDepthStiffener { get; set; }
        }

        public class DesignResult
        {
            public bool IsValid { get; set; }
            public DuctilityLevel Ductility { get; set; }
            public string DuctilityRef { get; set; } = "";
            public double LbMax { get; set; }
            public bool LbCheckPass { get; set; }
            public double RequiredStrength { get; set; }
            public double RequiredStiffness { get; set; }
            public string StrengthLabel { get; set; } = "";
            public string StiffnessLabel { get; set; } = "";
            public string StrengthUnit { get; set; } = "";
            public string StiffnessUnit { get; set; } = "";
            public List<string> Process { get; set; } = new();
        }

        private static DuctilityLevel GetDuctilityLevel(SeismicSystem system, MemberRole member)
        {
            return (system, member) switch
            {
                (SeismicSystem.SMF, MemberRole.Beam) => DuctilityLevel.HighlyDuctile,
                (SeismicSystem.IMF, MemberRole.Beam) => DuctilityLevel.ModeratelyDuctile,
                (SeismicSystem.SCBF, MemberRole.Beam) => DuctilityLevel.ModeratelyDuctile,
                (SeismicSystem.EBF, MemberRole.Beam) => DuctilityLevel.ModeratelyDuctile,
                (SeismicSystem.EBF, MemberRole.Link) => DuctilityLevel.HighlyDuctile,
                (SeismicSystem.BRBF, MemberRole.Beam) => DuctilityLevel.ModeratelyDuctile,
                _ => DuctilityLevel.ModeratelyDuctile
            };
        }

        private static string GetDuctilityRef(SeismicSystem system)
        {
            return system switch
            {
                SeismicSystem.SMF => "AISC 341-16 §E3",
                SeismicSystem.IMF => "AISC 341-16 §E2",
                SeismicSystem.SCBF => "AISC 341-16 §F2",
                SeismicSystem.EBF => "AISC 341-16 §F3",
                SeismicSystem.BRBF => "AISC 341-16 §F4",
                _ => ""
            };
        }

        private static double GetAlphaS(DesignMethod method) =>
            method == DesignMethod.LRFD ? 1.0 : 1.5;

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var p = result.Process;

            if (input.Zx <= 0) { p.Add("错误：塑性截面模量 Zx 必须大于 0"); return result; }
            if (input.RyRadius <= 0) { p.Add("错误：回转半径 ry 必须大于 0"); return result; }
            if (input.Ho <= 0) { p.Add("错误：翼缘形心间距 ho 必须大于 0"); return result; }
            if (input.Fy <= 0) { p.Add("错误：屈服强度 Fy 必须大于 0"); return result; }
            if (input.E <= 0) { p.Add("错误：弹性模量 E 必须大于 0"); return result; }
            if (input.Ry <= 0) { p.Add("错误：Ry 必须大于 0"); return result; }
            if (input.Lb <= 0) { p.Add("错误：无支撑长度 Lb 必须大于 0"); return result; }

            if (!input.AtPlasticHinge && input.UseSeismicMr && input.Cd <= 0)
            { p.Add("错误：曲率系数 Cd 必须大于 0"); return result; }

            if (!input.AtPlasticHinge && !input.UseSeismicMr && input.Mr <= 0)
            { p.Add("错误：所需弯矩 Mr 必须大于 0"); return result; }

            if (input.Brace == BraceType.Torsional)
            {
                if (input.IyEff <= 0) { p.Add("错误：抗扭计算需要有效弱轴惯性矩 Iy,eff > 0"); return result; }
                if (input.Cb <= 0) { p.Add("错误：弯矩梯度系数 Cb 必须大于 0"); return result; }
                if (input.LSpan <= 0) { p.Add("错误：抗扭计算需要梁跨度 L > 0"); return result; }
                if (input.NBraces <= 0) { p.Add("错误：支撑数量 n 必须大于 0"); return result; }
                if (!input.FullDepthStiffener)
                {
                    if (input.Tst <= 0) { p.Add("错误：加劲肋厚度 tst 必须大于 0"); return result; }
                    if (input.Bs <= 0) { p.Add("错误：加劲肋宽度 bs 必须大于 0"); return result; }
                }
            }

            result.IsValid = true;
            double alphaS = GetAlphaS(input.Method);

            result.Ductility = GetDuctilityLevel(input.System, input.Member);
            result.DuctilityRef = GetDuctilityRef(input.System);
            string ductilityStr = result.Ductility == DuctilityLevel.HighlyDuctile
                ? "高延性 (Highly Ductile)" : "中等延性 (Moderately Ductile)";

            p.Add("═══════════════════════════════════════════════════════");
            p.Add("  梁的侧向稳定支撑验算");
            p.Add("  AISC 341-16 §D1.2 + AISC 360-16 Appendix 6");
            p.Add("═══════════════════════════════════════════════════════");
            p.Add("");
            p.Add("【基本参数】");
            p.Add($"  设计方法: {(input.Method == DesignMethod.LRFD ? "LRFD" : "ASD")} (αs = {alphaS:F2})");
            p.Add($"  抗震体系: {GetSystemDisplayName(input.System)}");
            p.Add($"  构件类型: {GetMemberDisplayName(input.Member)}");
            p.Add($"  参考条文: {result.DuctilityRef}");
            p.Add($"  延性等级: {ductilityStr}");
            p.Add($"  截面参数: Zx = {input.Zx:F2} in³, ry = {input.RyRadius:F3} in, ho = {input.Ho:F3} in");
            p.Add($"  材料参数: Fy = {input.Fy:F1} ksi, E = {input.E:F0} ksi, Ry = {input.Ry:F2}");
            p.Add($"  实际无支撑长度: Lb = {input.Lb:F2} in ({input.Lb / 12.0:F2} ft)");
            p.Add("");

            // D1.2a.1(a) design requirement note
            p.Add("  注：§D1.2a.1(a) 要求梁的两个翼缘均应侧向支撑，");
            p.Add("      或采用点扭转支撑约束截面扭转。");
            p.Add("");

            // ── Step 1: Lb,max ──
            p.Add("───────────────────────────────────────────────────────");
            p.Add("【第一步】最大无支撑长度校核 (AISC 341-16 §D1.2)");
            p.Add("───────────────────────────────────────────────────────");

            double LbMax;
            if (result.Ductility == DuctilityLevel.ModeratelyDuctile)
            {
                LbMax = 0.19 * input.RyRadius * input.E / (input.Ry * input.Fy);
                p.Add("  中等延性构件 (§D1.2a.1):");
                p.Add("  Lb,max = 0.19 × ry × E / (Ry × Fy)          (Eq. D1-2)");
                p.Add($"        = 0.19 × {input.RyRadius:F3} × {input.E:F0} / ({input.Ry:F2} × {input.Fy:F1})");
            }
            else
            {
                LbMax = 0.095 * input.RyRadius * input.E / (input.Ry * input.Fy);
                p.Add("  高延性构件 (§D1.2b):");
                p.Add("  Lb,max = 0.095 × ry × E / (Ry × Fy)");
                p.Add($"        = 0.095 × {input.RyRadius:F3} × {input.E:F0} / ({input.Ry:F2} × {input.Fy:F1})");
            }

            p.Add($"        = {LbMax:F2} in ({LbMax / 12.0:F2} ft)");
            result.LbMax = LbMax;
            result.LbCheckPass = input.Lb <= LbMax;
            p.Add($"  实际 Lb = {input.Lb:F2} in ({input.Lb / 12.0:F2} ft)");
            p.Add($"  Lb ≤ Lb,max ?  {input.Lb:F2} ≤ {LbMax:F2}");
            p.Add($"  → {(result.LbCheckPass ? "✓ 通过 (PASS)" : "✗ 不通过 (FAIL) — 需增加侧向支撑点")}");
            p.Add("");

            // ── Step 2: Bracing demand ──
            p.Add("───────────────────────────────────────────────────────");
            p.Add("【第二步】支撑强度与刚度需求计算");
            p.Add("───────────────────────────────────────────────────────");

            double MrSeismic = input.Ry * input.Fy * input.Zx / alphaS;
            double Mr;

            if (input.AtPlasticHinge)
            {
                Mr = MrSeismic;
                p.Add("  ★ 塑性铰区域 — AISC 341-16 §D1.2c");
                p.Add($"  Mr = Ry × Fy × Zx / αs");
                p.Add($"     = {input.Ry:F2} × {input.Fy:F1} × {input.Zx:F2} / {alphaS:F2}");
                p.Add($"     = {Mr:F2} kip-in ({Mr / 12.0:F2} kip-ft)  (Eq. D1-6)");
                p.Add($"  Cd = 1.0 (§D1.2c 规定)");
                p.Add("");
            }
            else
            {
                if (input.UseSeismicMr)
                {
                    Mr = MrSeismic;
                    p.Add("  常规区域 — AISC 341-16 §D1.2a + Appendix 6");
                    p.Add("  按 §D1.2a.1(b) 取 Mr = Ry × Fy × Zx / αs  (Eq. D1-1)");
                    p.Add($"     = {input.Ry:F2} × {input.Fy:F1} × {input.Zx:F2} / {alphaS:F2}");
                    p.Add($"     = {Mr:F2} kip-in ({Mr / 12.0:F2} kip-ft)");
                }
                else
                {
                    Mr = input.Mr;
                    p.Add("  常规区域 — AISC 360-16 Appendix 6");
                    p.Add($"  用户输入 Mr = {Mr:F2} kip-in ({Mr / 12.0:F2} kip-ft)");
                }
                p.Add($"  Cd = {input.Cd:F2}");
                p.Add("");
            }

            if (input.Brace == BraceType.LateralNodal || input.Brace == BraceType.LateralRelative)
                CalculateLateralBracing(input, result, Mr, p);
            else
                CalculateTorsionalBracing(input, result, Mr, p);

            // ── Summary ──
            p.Add("");
            p.Add("═══════════════════════════════════════════════════════");
            p.Add("【设计总结】");
            p.Add("═══════════════════════════════════════════════════════");
            p.Add($"  延性等级: {ductilityStr}");
            p.Add($"  无支撑长度校核: Lb = {input.Lb:F2} in vs Lb,max = {LbMax:F2} in");
            p.Add($"    → {(result.LbCheckPass ? "PASS" : "FAIL")}");

            if (input.Brace == BraceType.Torsional)
            {
                p.Add($"  支撑类型: 抗扭支撑 (Torsional Bracing)");
                p.Add($"  所需弯矩强度: Mbr = {result.RequiredStrength:F2} kip-in");
                p.Add($"  所需扭转刚度: βbr = {result.RequiredStiffness:E4} kip-in/rad");
            }
            else
            {
                string braceLabel = input.Brace == BraceType.LateralNodal
                    ? "节点侧向支撑 (Nodal Lateral)" : "相对侧向支撑 (Relative Lateral)";
                p.Add($"  支撑类型: {braceLabel}");
                p.Add($"  所需轴向强度: {result.RequiredStrength:F2} kips");
                p.Add($"  所需弹簧刚度: βbr = {result.RequiredStiffness:F2} kips/in");
            }

            p.Add(input.AtPlasticHinge
                ? "  ⚠ 塑性铰区域 — 高压迫要求 (§D1.2c)"
                : "  常规区域 — 附录 6 / §D1.2a 要求");
            p.Add("═══════════════════════════════════════════════════════");

            return result;
        }

        private static void CalculateLateralBracing(InputParameters input, DesignResult result,
            double Mr, List<string> p)
        {
            const double phi = 0.75;
            const double omega = 2.00;
            double factor = input.Method == DesignMethod.LRFD ? 1.0 / phi : omega;
            string factorLabel = input.Method == DesignMethod.LRFD ? "1/φ" : "Ω";
            double Cd = input.AtPlasticHinge ? 1.0 : input.Cd;

            if (input.AtPlasticHinge)
            {
                // §D1.2c — strength (Eq. D1-4)
                double Pr = 0.06 * Mr / input.Ho;
                result.RequiredStrength = Pr;
                result.StrengthLabel = input.Brace == BraceType.LateralNodal
                    ? "Pr (节点侧向)" : "Vr (相对侧向)";
                result.StrengthUnit = "kips";

                p.Add($"  ▸ 支撑强度需求 (§D1.2c, Eq. D1-4):");
                p.Add($"  Pr = 0.06 × (Ry×Fy×Zx/αs) / ho");
                p.Add($"     = 0.06 × {Mr:F2} / {input.Ho:F3}");
                p.Add($"     = {Pr:F2} kips");
                p.Add("");
            }
            else
            {
                if (input.Brace == BraceType.LateralNodal)
                {
                    // App 6 §6.3.1b — Eq. A-6-7
                    double Pbr = 0.02 * Mr * Cd / input.Ho;
                    result.RequiredStrength = Pbr;
                    result.StrengthLabel = "Pbr (节点侧向)";
                    result.StrengthUnit = "kips";

                    p.Add("  ▸ 支撑强度需求 (App 6 §6.3.1b, Eq. A-6-7):");
                    p.Add($"  Pbr = 0.02 × Mr × Cd / ho");
                    p.Add($"      = 0.02 × {Mr:F2} × {Cd:F2} / {input.Ho:F3}");
                    p.Add($"      = {Pbr:F2} kips");
                }
                else
                {
                    // App 6 §6.3.1a — Eq. A-6-5
                    double Vbr = 0.01 * Mr * Cd / input.Ho;
                    result.RequiredStrength = Vbr;
                    result.StrengthLabel = "Vbr (相对侧向)";
                    result.StrengthUnit = "kips";

                    p.Add("  ▸ 支撑强度需求 (App 6 §6.3.1a, Eq. A-6-5):");
                    p.Add($"  Vbr = 0.01 × Mr × Cd / ho");
                    p.Add($"      = 0.01 × {Mr:F2} × {Cd:F2} / {input.Ho:F3}");
                    p.Add($"      = {Vbr:F2} kips");
                }
                p.Add("");
            }

            // Stiffness — same formula for plastic hinge and normal
            // Nodal:  βbr = factor × 10 × Mr × Cd / (Lb × ho)  (A-6-8)
            // Relative: βbr = factor × 4 × Mr × Cd / (Lb × ho)  (A-6-6)
            double coeff = input.Brace == BraceType.LateralNodal ? 10.0 : 4.0;
            string eqRef = input.Brace == BraceType.LateralNodal ? "A-6-8" : "A-6-6";
            string sectionRef = input.Brace == BraceType.LateralNodal ? "§6.3.1b" : "§6.3.1a";

            double betaBr = factor * coeff * Mr * Cd / (input.Lb * input.Ho);
            result.RequiredStiffness = betaBr;
            result.StiffnessLabel = "βbr";
            result.StiffnessUnit = "kips/in";

            p.Add($"  ▸ 支撑刚度需求 (App 6 {sectionRef}, Eq. {eqRef}):");
            p.Add($"  βbr = {factorLabel} × {coeff:G} × Mr × Cd / (Lb × ho)");
            p.Add($"      = {factorLabel} × {coeff:G} × {Mr:F2} × {Cd:F2} / ({input.Lb:F2} × {input.Ho:F3})");
            p.Add($"      = {factorLabel} × {coeff * Mr * Cd / (input.Lb * input.Ho):F2}");
            p.Add($"      = {betaBr:F2} kips/in");
            p.Add($"  (φ = {phi}, Ω = {omega})");
        }

        private static void CalculateTorsionalBracing(InputParameters input, DesignResult result,
            double Mr, List<string> p)
        {
            const double phi = 0.75;
            const double omega = 3.00;
            double factor = input.Method == DesignMethod.LRFD ? 1.0 / phi : omega;
            string factorLabel = input.Method == DesignMethod.LRFD ? "1/φ" : "Ω";
            double alphaS = GetAlphaS(input.Method);

            // Strength
            double Mbr;
            if (input.AtPlasticHinge)
            {
                // §D1.2c Eq. D1-5
                Mbr = 0.06 * input.Ry * input.Fy * input.Zx / alphaS;
                p.Add("  ▸ 抗扭支撑强度需求 (§D1.2c, Eq. D1-5):");
                p.Add($"  Mbr = 0.06 × Ry × Fy × Zx / αs");
                p.Add($"      = 0.06 × {input.Ry:F2} × {input.Fy:F1} × {input.Zx:F2} / {alphaS:F2}");
            }
            else
            {
                // App 6 Eq. A-6-9
                Mbr = 0.02 * Mr;
                p.Add("  ▸ 抗扭支撑强度需求 (App 6 §6.3.2a, Eq. A-6-9):");
                p.Add($"  Mbr = 0.02 × Mr = 0.02 × {Mr:F2}");
            }

            result.RequiredStrength = Mbr;
            result.StrengthLabel = "Mbr (抗扭弯矩)";
            result.StrengthUnit = "kip-in";
            p.Add($"        = {Mbr:F2} kip-in");
            p.Add("");

            // Stiffness — β_T per Eq. A-6-11
            double betaT = factor * 2.4 * input.LSpan
                / (input.NBraces * input.E * input.IyEff)
                * Math.Pow(Mr / input.Cb, 2);

            p.Add("  ▸ 抗扭刚度需求 (Eq. A-6-10 ~ A-6-11):");
            p.Add($"  βT = {factorLabel} × 2.4 × L / (n × E × Iy,eff) × (Mr/Cb)²");
            p.Add($"     = {factorLabel} × 2.4 × {input.LSpan:F2} / ({input.NBraces} × {input.E:F0} × {input.IyEff:F3}) × ({Mr:F2}/{input.Cb:F2})²");
            p.Add($"     = {betaT:E4}");

            // β_sec per Eq. A-6-12
            double betaSec;
            if (input.FullDepthStiffener)
            {
                betaSec = double.PositiveInfinity;
                p.Add("  βsec = ∞ (全高加劲肋/隔板，βbr = βT)");
            }
            else
            {
                betaSec = 3.3 * input.E / input.Ho
                    * (1.5 * input.Ho * Math.Pow(input.Tw, 3) / 12.0
                       + input.Tst * Math.Pow(input.Bs, 3) / 12.0);
                p.Add($"  βsec = 3.3×E/ho × (1.5×ho×tw³/12 + tst×bs³/12)   (Eq. A-6-12)");
                p.Add($"       = {betaSec:E4}");
            }

            double betaBr;
            if (double.IsInfinity(betaSec))
            {
                betaBr = betaT;
            }
            else if (betaT >= betaSec)
            {
                p.Add("  ⚠ 警告: βT ≥ βsec，腹板畸变刚度不足，支撑截面刚度无法满足！");
                betaBr = double.PositiveInfinity;
            }
            else
            {
                betaBr = betaT / (1.0 - betaT / betaSec);
            }

            result.RequiredStiffness = betaBr;
            result.StiffnessLabel = "βbr";
            result.StiffnessUnit = "kip-in/rad";
            p.Add($"  βbr = βT / (1 - βT/βsec) = {betaBr:E4} kip-in/rad");
            p.Add($"  (φ = {phi}, Ω = {omega})");
        }

        public static string GetSystemDisplayName(SeismicSystem system) => system switch
        {
            SeismicSystem.SMF => "SMF (特殊抗弯框架)",
            SeismicSystem.IMF => "IMF (中等抗弯框架)",
            SeismicSystem.SCBF => "SCBF (特殊中心支撑框架)",
            SeismicSystem.EBF => "EBF (偏心支撑框架)",
            SeismicSystem.BRBF => "BRBF (屈曲约束支撑框架)",
            _ => system.ToString()
        };

        public static string GetMemberDisplayName(MemberRole member) => member switch
        {
            MemberRole.Beam => "梁 (Beam)",
            MemberRole.Link => "连梁 (Link)",
            _ => member.ToString()
        };

        public static double GetDefaultRy(double fy) => fy <= 36 ? 1.50 : 1.10;

        public static bool SystemHasLink(SeismicSystem system) => system == SeismicSystem.EBF;

        public static string[] GetSystemOptions() =>
            new[] { "SMF (特殊抗弯框架)", "IMF (中等抗弯框架)", "SCBF (特殊中心支撑框架)",
                    "EBF (偏心支撑框架)", "BRBF (屈曲约束支撑框架)" };

        public static string[] GetMemberOptions(bool hasLink) =>
            hasLink ? new[] { "梁 (Beam)", "连梁 (Link)" } : new[] { "梁 (Beam)" };
    }
}
