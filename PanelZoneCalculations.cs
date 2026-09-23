using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class PanelZoneCalculations
    {
        public enum SeismicSystem { SMF, IMF, OMF }
        public enum DesignMethod { LRFD, ASD }

        public class InputParameters
        {
            public SeismicSystem System { get; set; }
            public DesignMethod Method { get; set; }

            // Column
            public double Dc { get; set; }
            public double Bcf { get; set; }
            public double Tcf { get; set; }
            public double Tcw { get; set; }
            public double Fyc { get; set; }
            public double AgCol { get; set; }
            public double Pr { get; set; } // required axial compressive strength (kips, positive = compression)

            // Beam Left
            public double DbLeft { get; set; }
            public double TfbLeft { get; set; }
            public bool UseCapacityLeft { get; set; } = true;
            public double ZxLeft { get; set; }
            public double FybLeft { get; set; }
            public double RyLeft { get; set; }
            public double CprLeft { get; set; }
            public double MfLeft { get; set; }

            // Beam Right
            public bool HasRightBeam { get; set; } = true;
            public double DbRight { get; set; }
            public double TfbRight { get; set; }
            public bool UseCapacityRight { get; set; } = true;
            public double ZxRight { get; set; }
            public double FybRight { get; set; }
            public double RyRight { get; set; }
            public double CprRight { get; set; }
            public double MfRight { get; set; }

            // Joint
            public double Hc { get; set; }
        }

        public class DesignResult
        {
            public bool IsValid { get; set; }
            public List<string> Process { get; set; } = new();

            // Step 1
            public double MfLeftResult { get; set; }
            public double MfRightResult { get; set; }
            public double PfLeft { get; set; }
            public double PfRight { get; set; }
            public double PfTotal { get; set; }
            public double Vc { get; set; }
            public double Ru { get; set; }

            // Step 2
            public double Py { get; set; }
            public double AxialRatio { get; set; }
            public double Rn { get; set; }
            public double DesignCapacity { get; set; }
            public double StrengthRatio { get; set; }
            public bool StrengthPass { get; set; }

            // Step 3
            public double Wz { get; set; }
            public double Dz { get; set; }
            public double DbAvg { get; set; }
            public double Tmin { get; set; }
            public bool ThicknessPass { get; set; }
            public bool ThicknessChecked { get; set; }

            // Step 4
            public bool NeedsDoubler { get; set; }
            public double TdpStrength { get; set; }
            public double TdpBuckling { get; set; }
            public double TdpRequired { get; set; }
            public double RecommendedTdp { get; set; }
            public bool NeedsPlugWelds { get; set; }
        }

        private static readonly double[] StandardPlateThicknesses =
            { 0.1875, 0.25, 0.3125, 0.375, 0.4375, 0.5, 0.5625, 0.625, 0.75, 0.875, 1.0 };

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var p = result.Process;

            // ── Input validation ──
            if (input.Dc <= 0) { p.Add("错误：柱截面深度 dc 必须大于 0"); return result; }
            if (input.Bcf <= 0) { p.Add("错误：柱翼缘宽度 bcf 必须大于 0"); return result; }
            if (input.Tcf <= 0) { p.Add("错误：柱翼缘厚度 tcf 必须大于 0"); return result; }
            if (input.Tcw <= 0) { p.Add("错误：柱腹板厚度 tcw 必须大于 0"); return result; }
            if (input.Fyc <= 0) { p.Add("错误：柱屈服强度 Fyc 必须大于 0"); return result; }
            if (input.AgCol <= 0) { p.Add("错误：柱截面积 Ag 必须大于 0"); return result; }
            if (input.Hc <= 0) { p.Add("错误：层高 Hc 必须大于 0"); return result; }

            if (input.DbLeft <= 0) { p.Add("错误：左梁截面深度 db 必须大于 0"); return result; }
            if (input.TfbLeft <= 0) { p.Add("错误：左梁翼缘厚度 tfb 必须大于 0"); return result; }
            if (input.DbLeft <= input.TfbLeft) { p.Add("错误：左梁 db 必须 > tfb"); return result; }

            if (input.UseCapacityLeft)
            {
                if (input.ZxLeft <= 0) { p.Add("错误：左梁塑性模量 Zx 必须大于 0"); return result; }
                if (input.FybLeft <= 0) { p.Add("错误：左梁屈服强度 Fyb 必须大于 0"); return result; }
                if (input.RyLeft <= 0) { p.Add("错误：左梁 Ry 必须大于 0"); return result; }
                if (input.CprLeft <= 0) { p.Add("错误：左梁 Cpr 必须大于 0"); return result; }
            }
            else
            {
                if (input.MfLeft < 0) { p.Add("错误：左梁弯矩 Mf 不能为负"); return result; }
            }

            if (input.HasRightBeam)
            {
                if (input.DbRight <= 0) { p.Add("错误：右梁截面深度 db 必须大于 0"); return result; }
                if (input.TfbRight <= 0) { p.Add("错误：右梁翼缘厚度 tfb 必须大于 0"); return result; }
                if (input.DbRight <= input.TfbRight) { p.Add("错误：右梁 db 必须 > tfb"); return result; }

                if (input.UseCapacityRight)
                {
                    if (input.ZxRight <= 0) { p.Add("错误：右梁塑性模量 Zx 必须大于 0"); return result; }
                    if (input.FybRight <= 0) { p.Add("错误：右梁屈服强度 Fyb 必须大于 0"); return result; }
                    if (input.RyRight <= 0) { p.Add("错误：右梁 Ry 必须大于 0"); return result; }
                    if (input.CprRight <= 0) { p.Add("错误：右梁 Cpr 必须大于 0"); return result; }
                }
                else
                {
                    if (input.MfRight < 0) { p.Add("错误：右梁弯矩 Mf 不能为负"); return result; }
                }
            }

            result.IsValid = true;
            const double phi = 0.90;
            const double omega = 1.67;

            bool useCapacity = input.System != SeismicSystem.OMF;
            bool checkMinThickness = input.System != SeismicSystem.OMF;

            // ── Header ──
            p.Add("═══════════════════════════════════════════════════════");
            p.Add("  梁柱节点剪切区（Panel Zone）强度验算");
            p.Add("  AISC 341-16 §E3.6e + AISC 360-16 §J10.6");
            p.Add("═══════════════════════════════════════════════════════");
            p.Add("");
            p.Add("【基本参数】");
            p.Add($"  设计方法: {(input.Method == DesignMethod.LRFD ? "LRFD" : "ASD")}");
            p.Add($"  抗震体系: {GetSystemDisplayName(input.System)}");
            p.Add($"  柱截面: dc = {input.Dc:F2} in, bcf = {input.Bcf:F2} in");
            p.Add($"          tcf = {input.Tcf:F3} in, tcw = {input.Tcw:F3} in");
            p.Add($"  柱材料: Fyc = {input.Fyc:F1} ksi, Ag = {input.AgCol:F2} in²");
            p.Add($"  柱轴力: Pr = {input.Pr:F1} kips");
            p.Add($"  层高: Hc = {input.Hc:F1} in ({input.Hc / 12.0:F1} ft)");
            p.Add("");

            // ═══════════════════════════════════════════════════════
            // Step 1: Shear Demand
            // ═══════════════════════════════════════════════════════
            p.Add("───────────────────────────────────────────────────────");
            p.Add("【第一步】剪切区剪力需求计算");
            p.Add("───────────────────────────────────────────────────────");

            // Left beam
            if (input.UseCapacityLeft)
            {
                result.MfLeftResult = input.CprLeft * input.RyLeft * input.FybLeft * input.ZxLeft;
                p.Add($"  ★ 左梁 — 基于能力设计法 (Capacity Design)");
                p.Add($"  Mpr = Cpr × Ry × Fyb × Zx");
                p.Add($"      = {input.CprLeft:F2} × {input.RyLeft:F2} × {input.FybLeft:F1} × {input.ZxLeft:F2}");
                p.Add($"      = {result.MfLeftResult:F1} kip-in ({result.MfLeftResult / 12.0:F1} kip-ft)");
            }
            else
            {
                result.MfLeftResult = input.MfLeft;
                p.Add($"  左梁 — 用户输入设计弯矩");
                p.Add($"  Mf(左) = {input.MfLeft:F1} kip-in ({input.MfLeft / 12.0:F1} kip-ft)");
            }

            double leverLeft = input.DbLeft - input.TfbLeft;
            result.PfLeft = result.MfLeftResult / leverLeft;
            p.Add($"  Pf(左) = Mf / (db - tfb) = {result.MfLeftResult:F1} / {leverLeft:F3}");
            p.Add($"        = {result.PfLeft:F2} kips");
            p.Add("");

            // Right beam
            if (input.HasRightBeam)
            {
                if (input.UseCapacityRight)
                {
                    result.MfRightResult = input.CprRight * input.RyRight * input.FybRight * input.ZxRight;
                    p.Add($"  ★ 右梁 — 基于能力设计法 (Capacity Design)");
                    p.Add($"  Mpr = Cpr × Ry × Fyb × Zx");
                    p.Add($"      = {input.CprRight:F2} × {input.RyRight:F2} × {input.FybRight:F1} × {input.ZxRight:F2}");
                    p.Add($"      = {result.MfRightResult:F1} kip-in ({result.MfRightResult / 12.0:F1} kip-ft)");
                }
                else
                {
                    result.MfRightResult = input.MfRight;
                    p.Add($"  右梁 — 用户输入设计弯矩");
                    p.Add($"  Mf(右) = {input.MfRight:F1} kip-in ({input.MfRight / 12.0:F1} kip-ft)");
                }

                double leverRight = input.DbRight - input.TfbRight;
                result.PfRight = result.MfRightResult / leverRight;
                p.Add($"  Pf(右) = Mf / (db - tfb) = {result.MfRightResult:F1} / {leverRight:F3}");
                p.Add($"        = {result.PfRight:F2} kips");
                p.Add("");
            }
            else
            {
                result.MfRightResult = 0;
                result.PfRight = 0;
                p.Add("  (无边梁 — Exterior Joint)");
                p.Add("");
            }

            // Totals
            result.PfTotal = result.PfLeft + result.PfRight;
            double sumMf = result.MfLeftResult + result.MfRightResult;
            result.Vc = sumMf / input.Hc;
            result.Ru = result.PfTotal - result.Vc;

            p.Add($"  Σ Pf = {result.PfLeft:F2} + {result.PfRight:F2} = {result.PfTotal:F2} kips");
            p.Add($"  Vc = Σ Mf / Hc = {sumMf:F1} / {input.Hc:F1} = {result.Vc:F2} kips");
            p.Add($"  Ru = Σ Pf - Vc = {result.PfTotal:F2} - {result.Vc:F2}");
            p.Add($"    = {result.Ru:F2} kips");
            p.Add("");

            // ═══════════════════════════════════════════════════════
            // Step 2: Shear Capacity (AISC 360-16 §J10.6)
            // ═══════════════════════════════════════════════════════
            p.Add("───────────────────────────────────────────────────────");
            p.Add("【第二步】剪切区抗剪承载力 (AISC 360-16 §J10.6)");
            p.Add("───────────────────────────────────────────────────────");

            // Average beam depth
            result.DbAvg = input.HasRightBeam
                ? (input.DbLeft + input.DbRight) / 2.0
                : input.DbLeft;

            // Py and axial ratio
            result.Py = input.Fyc * input.AgCol;
            result.AxialRatio = Math.Abs(input.Pr) / result.Py;

            p.Add($"  考虑剪切区变形对框架稳定性的影响 → 使用 Eq. J10-11/J10-12");
            p.Add($"  平均梁深: db = {result.DbAvg:F2} in");
            p.Add($"  Py = Fyc × Ag = {input.Fyc:F1} × {input.AgCol:F2} = {result.Py:F1} kips");
            p.Add($"  Pr/Py = {Math.Abs(input.Pr):F1} / {result.Py:F1} = {result.AxialRatio:F3}");
            p.Add("");

            // Bracket term
            double bracket = 1.0 + (3.0 * input.Bcf * Math.Pow(input.Tcf, 2))
                / (result.DbAvg * input.Dc * input.Tcw);
            double reduction;

            if (result.AxialRatio <= 0.4)
            {
                reduction = 1.0;
                result.Rn = 0.6 * input.Fyc * input.Dc * input.Tcw * bracket;
                p.Add($"  Pr/Py ≤ 0.4 → Eq. J10-11 (无轴力折减):");
            }
            else
            {
                reduction = 1.4 - result.AxialRatio;
                if (reduction <= 0)
                {
                    p.Add("  ⚠ 警告：Pr/Py ≥ 1.4，折减系数 ≤ 0，柱轴向承载力不足！");
                    result.Rn = 0;
                }
                else
                {
                    result.Rn = 0.6 * input.Fyc * input.Dc * input.Tcw * bracket * reduction;
                }
                p.Add($"  Pr/Py > 0.4 → Eq. J10-12 (轴力折减系数 = {reduction:F4}):");
            }

            p.Add($"  Rn = 0.6 × Fyc × dc × tcw × [1 + 3×bcf×tcf²/(db×dc×tcw)]");
            if (result.AxialRatio > 0.4)
                p.Add($"     × (1.4 - Pr/Py)");
            p.Add($"     = 0.6 × {input.Fyc:F1} × {input.Dc:F2} × {input.Tcw:F3} × {bracket:F4}");
            if (result.AxialRatio > 0.4)
                p.Add($"     × {reduction:F4}");
            p.Add($"     = {result.Rn:F2} kips");
            p.Add("");

            // Design capacity
            if (input.Method == DesignMethod.LRFD)
            {
                result.DesignCapacity = phi * result.Rn;
                p.Add($"  φRn = {phi:F2} × {result.Rn:F2} = {result.DesignCapacity:F2} kips");
            }
            else
            {
                result.DesignCapacity = result.Rn / omega;
                p.Add($"  Rn/Ω = {result.Rn:F2} / {omega:F2} = {result.DesignCapacity:F2} kips");
            }

            result.StrengthRatio = result.Ru / result.DesignCapacity;
            result.StrengthPass = result.StrengthRatio <= 1.0;
            p.Add("");
            p.Add($"  Ru / 设计承载力 = {result.Ru:F2} / {result.DesignCapacity:F2} = {result.StrengthRatio:F3}");
            p.Add($"  → {(result.StrengthPass ? "✓ 通过 (PASS)" : "✗ 不通过 (FAIL) — 强度不足")}");
            p.Add("");

            // ═══════════════════════════════════════════════════════
            // Step 3: Minimum Thickness (AISC 341-16 §E3.6e(3))
            // ═══════════════════════════════════════════════════════
            if (checkMinThickness)
            {
                result.ThicknessChecked = true;
                p.Add("───────────────────────────────────────────────────────");
                p.Add("【第三步】最小腹板厚度校核 (AISC 341-16 §E3.6e(3))");
                p.Add("───────────────────────────────────────────────────────");

                result.Wz = input.Dc - 2 * input.Tcf;
                double tfbAvg = input.HasRightBeam
                    ? (input.TfbLeft + input.TfbRight) / 2.0
                    : input.TfbLeft;
                result.Dz = result.DbAvg - 2 * tfbAvg;
                result.Tmin = (result.Dz + result.Wz) / 90.0;

                p.Add($"  wz = dc - 2×tcf = {input.Dc:F2} - 2×{input.Tcf:F3} = {result.Wz:F3} in");
                p.Add($"  dz = db - 2×tfb = {result.DbAvg:F2} - 2×{tfbAvg:F3} = {result.Dz:F3} in");
                p.Add($"  tmin = (dz + wz) / 90 = ({result.Dz:F3} + {result.Wz:F3}) / 90");
                p.Add($"       = {result.Tmin:F4} in");
                p.Add($"  tcw = {input.Tcw:F4} in");
                p.Add($"  tcw ≥ tmin ?  {input.Tcw:F4} ≥ {result.Tmin:F4}");

                result.ThicknessPass = input.Tcw >= result.Tmin;
                p.Add($"  → {(result.ThicknessPass ? "✓ 通过 (PASS)" : "✗ 不通过 (FAIL) — 腹板过薄，可能局部屈曲")}");
                p.Add("");
            }
            else
            {
                result.ThicknessChecked = false;
                result.ThicknessPass = true;
            }

            // ═══════════════════════════════════════════════════════
            // Step 4: Doubler Plate Design
            // ═══════════════════════════════════════════════════════
            p.Add("───────────────────────────────────────────────────────");
            p.Add("【第四步】补强板设计 (Doubler Plate)");
            p.Add("───────────────────────────────────────────────────────");

            // Strength deficiency
            if (result.StrengthPass)
            {
                result.TdpStrength = 0;
                p.Add("  强度校核通过，无需因强度补强。");
            }
            else
            {
                double demandFactor = input.Method == DesignMethod.LRFD ? 1.0 / phi : omega;
                // Required Rn ≥ Ru × demandFactor
                // 0.6*Fyc*[dc*(tcw+tdp) + 3*bcf*tcf²/db] * reduction ≥ Ru × demandFactor
                // dc*(tcw+tdp) ≥ Ru*demandFactor/(0.6*Fyc*reduction) - 3*bcf*tcf²/db
                double tTotalRequired = result.Ru * demandFactor / (0.6 * input.Fyc * reduction)
                    - 3.0 * input.Bcf * Math.Pow(input.Tcf, 2) / (result.DbAvg);
                tTotalRequired /= input.Dc;
                result.TdpStrength = Math.Max(0, tTotalRequired - input.Tcw);

                p.Add($"  强度不足，需补强:");
                p.Add($"  所需总腹板厚度 t_total:");
                p.Add($"    = Ru×{demandFactor:F2} / (0.6×Fyc×reduction×dc) - 3×bcf×tcf²/(db×dc)");
                p.Add($"    = {result.Ru:F2}×{demandFactor:F2} / (0.6×{input.Fyc:F1}×{reduction:F4}×{input.Dc:F2})");
                p.Add($"      - 3×{input.Bcf:F2}×{input.Tcf:F3}²/({result.DbAvg:F2}×{input.Dc:F2})");
                p.Add($"    = {tTotalRequired:F4} in");
                p.Add($"  tdp(强度) = t_total - tcw = {tTotalRequired:F4} - {input.Tcw:F4}");
                p.Add($"            = {result.TdpStrength:F4} in");
            }
            p.Add("");

            // Buckling deficiency
            if (checkMinThickness && result.ThicknessPass)
            {
                result.TdpBuckling = 0;
                p.Add("  防屈曲校核通过，无需因屈曲补强。");
            }
            else if (!checkMinThickness)
            {
                result.TdpBuckling = 0;
                p.Add("  OMF 体系无需进行防屈曲厚度校核。");
            }
            else
            {
                result.TdpBuckling = result.Tmin - input.Tcw;
                p.Add($"  腹板过薄，需补强:");
                p.Add($"  tdp(屈曲) = tmin - tcw = {result.Tmin:F4} - {input.Tcw:F4}");
                p.Add($"            = {result.TdpBuckling:F4} in");
            }
            p.Add("");

            // Final determination
            result.TdpRequired = Math.Max(result.TdpStrength, result.TdpBuckling);
            result.NeedsDoubler = result.TdpRequired > 0.0001;

            if (!result.NeedsDoubler)
            {
                result.RecommendedTdp = 0;
                p.Add("  ══ 结论：无需补强板 (No Doubler Plate Required) ══");
            }
            else
            {
                // Round up to standard plate thickness
                result.RecommendedTdp = result.TdpRequired;
                foreach (var t in StandardPlateThicknesses)
                {
                    if (t >= result.TdpRequired - 0.0001)
                    {
                        result.RecommendedTdp = t;
                        break;
                    }
                }

                p.Add($"  ══ 结论：需要补强板 ══");
                p.Add($"  所需最小厚度: tdp = max(tdp强度, tdp屈曲)");
                p.Add($"                      = max({result.TdpStrength:F4}, {result.TdpBuckling:F4})");
                p.Add($"                      = {result.TdpRequired:F4} in");
                p.Add($"  推荐标准板厚: {result.RecommendedTdp:F4} in");

                if (result.RecommendedTdp > 1.0)
                    p.Add("  ⚠ 所需厚度超过常规标准板厚 (1 in)，请联系工程师确认方案");

                if (result.TdpRequired <= 0.5)
                    p.Add("  建议：单侧补强板即可 (Single-sided doubler)");
                else
                    p.Add($"  建议：考虑双侧补强板 (各 {(result.TdpRequired / 2.0):F4} in)");

                // Plug weld check
                if (checkMinThickness && input.Tcw < result.Tmin
                    && result.TdpRequired < result.Tmin)
                {
                    result.NeedsPlugWelds = true;
                    p.Add($"  ⚠ 塞焊警告 (Plug Weld Required):");
                    p.Add($"    柱腹板 tcw = {input.Tcw:F4} in < tmin = {result.Tmin:F4} in");
                    p.Add($"    补强板 tdp = {result.TdpRequired:F4} in < tmin = {result.Tmin:F4} in");
                    p.Add($"    两板各自均不满足防屈曲限值，需设置塞焊使板件协同工作！");
                }
                else
                {
                    result.NeedsPlugWelds = false;
                    p.Add("  塞焊：不需要 (板件厚度满足防屈曲限值)");
                }
            }

            // ═══════════════════════════════════════════════════════
            // Summary
            // ═══════════════════════════════════════════════════════
            p.Add("");
            p.Add("═══════════════════════════════════════════════════════");
            p.Add("【设计总结】");
            p.Add("═══════════════════════════════════════════════════════");
            p.Add($"  抗震体系: {GetSystemDisplayName(input.System)}");
            p.Add($"  剪力需求: Ru = {result.Ru:F2} kips");
            p.Add($"  设计承载力: {result.DesignCapacity:F2} kips");
            p.Add($"  强度比: Ru/(φRn) = {result.StrengthRatio:F3}");
            p.Add($"    → {(result.StrengthPass ? "PASS" : "FAIL")}");
            if (checkMinThickness)
            {
                p.Add($"  最小厚度: tmin = {result.Tmin:F4} in");
                p.Add($"  实际腹板: tcw = {input.Tcw:F4} in");
                p.Add($"    → {(result.ThicknessPass ? "PASS" : "FAIL")}");
            }
            if (result.NeedsDoubler)
            {
                p.Add($"  补强板: tdp = {result.RecommendedTdp:F4} in (推荐标准板厚)");
                if (result.NeedsPlugWelds)
                    p.Add("  塞焊: 需要 (Plug Welds Required)");
            }
            else
            {
                p.Add("  补强板: 无需 (No Doubler Plate Required)");
            }
            p.Add("═══════════════════════════════════════════════════════");

            return result;
        }

        public static string GetSystemDisplayName(SeismicSystem system) => system switch
        {
            SeismicSystem.SMF => "SMF (特殊抗弯框架)",
            SeismicSystem.IMF => "IMF (中等抗弯框架)",
            SeismicSystem.OMF => "OMF (普通抗弯框架)",
            _ => system.ToString()
        };

        public static string GetSystemRef(SeismicSystem system) => system switch
        {
            SeismicSystem.SMF => "AISC 341-16 §E3.6e",
            SeismicSystem.IMF => "AISC 341-16 §E2.6e",
            SeismicSystem.OMF => "AISC 341-16 §E1.6",
            _ => ""
        };

        public static double GetDefaultRy(double fy) => fy <= 36 ? 1.50 : 1.10;

        public static double GetDefaultCpr() => 1.15;

        public static string[] GetSystemOptions() =>
            new[] { "SMF (特殊抗弯框架)", "IMF (中等抗弯框架)", "OMF (普通抗弯框架)" };
    }
}
