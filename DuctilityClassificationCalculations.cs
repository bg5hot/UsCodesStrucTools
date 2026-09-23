using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class DuctilityClassificationCalculations
    {
        #region Enums

        public enum SeismicSystem
        {
            // Chapter E - Moment Frame Systems
            OMF,    // E1 - Ordinary Moment Frames
            IMF,    // E2 - Intermediate Moment Frames
            SMF,    // E3 - Special Moment Frames
            STMF,   // E4 - Special Truss Moment Frames
            OCCS,   // E5 - Ordinary Cantilever Column Systems
            SCCS,   // E6 - Special Cantilever Column Systems
            // Chapter F - Braced Frame & Shear Wall Systems
            OCBF,   // F1 - Ordinary Concentrically Braced Frames
            SCBF,   // F2 - Special Concentrically Braced Frames
            EBF,    // F3 - Eccentrically Braced Frames
            BRBF,   // F4 - Buckling-Restrained Braced Frames
            SPSW    // F5 - Special Plate Shear Walls
        }

        public enum MemberType
        {
            Beam,
            Column,
            Brace,
            Link,       // EBF link beam
            HBE,        // Horizontal boundary element (SPSW)
            VBE,        // Vertical boundary element (SPSW)
            Chord,      // STMF chord
            Diagonal,   // STMF diagonal / general diagonal
            Strut       // MT-SCBF strut
        }

        public enum SectionCategory
        {
            IShape,             // Rolled or built-up I-shaped
            HSS_Rect,           // Rectangular HSS
            HSS_Round,          // Round HSS
            BoxBuiltUp,         // Built-up box
            DoubleAngle,        // Double angle
            SingleAngle,        // Single angle
            Tee,                // Tee section
            HPile,              // H-Pile section
            Channel             // Channel
        }

        public enum DesignMethod { LRFD, ASD }

        public enum DuctilityLevel
        {
            None,               // No AISC 341 requirement (OMF beams/columns)
            ModeratelyDuctile,
            HighlyDuctile
        }

        #endregion

        #region Input / Result

        public class InputParameters
        {
            public SeismicSystem System;
            public MemberType Member;
            public SectionCategory SectionType;

            // Section dimensions (in)
            public double d;     // overall depth
            public double bf;    // flange width
            public double tw;    // web thickness
            public double tf;    // flange thickness
            public double Ht;    // HSS overall height
            public double B_hss; // HSS overall width
            public double tdes;  // HSS design wall thickness
            public double D_hss; // Round HSS outside diameter

            // Material
            public double Fy = 50;    // ksi
            public double E = 29000;  // ksi
            public double Ry = 1.0;   // ratio of expected to specified yield stress

            // Axial force for Ca calculation
            public DesignMethod Method = DesignMethod.LRFD;
            public double Pu = 0;     // LRFD required axial strength (kips)
            public double Pa = 0;     // ASD required axial strength (kips)
            public double Ag = 0;     // gross area (in²)
            public double phi_c = 0.9; // LRFD resistance factor for compression
            public double Omega_c = 1.67; // ASD safety factor for compression
        }

        public class CheckResult
        {
            public bool IsValid;
            public string ErrorMessage = "";

            public DuctilityLevel RequiredLevel;
            public string RequiredLevelDescription = "";

            // Flange check
            public double FlangeRatioActual;
            public double FlangeRatioLimit;
            public string FlangeFormula = "";
            public bool FlangeOK;
            public string FlangeElementDescription = "";

            // Web check
            public double WebRatioActual;
            public double WebRatioLimit;
            public string WebFormula = "";
            public bool WebOK;
            public string WebElementDescription = "";

            // Overall
            public bool SectionOK;

            public List<string> Process = new();
        }

        #endregion

        #region Ductility Mapping

        public static DuctilityLevel GetRequiredDuctility(SeismicSystem system, MemberType member)
        {
            return (system, member) switch
            {
                // E1 - OMF: No specific ductility requirements beyond Specification
                (SeismicSystem.OMF, _) => DuctilityLevel.None,

                // E2 - IMF: §E2.5a "moderately ductile members"
                (SeismicSystem.IMF, MemberType.Beam) => DuctilityLevel.ModeratelyDuctile,
                (SeismicSystem.IMF, MemberType.Column) => DuctilityLevel.ModeratelyDuctile,

                // E3 - SMF
                (SeismicSystem.SMF, MemberType.Beam) => DuctilityLevel.HighlyDuctile,
                (SeismicSystem.SMF, MemberType.Column) => DuctilityLevel.HighlyDuctile,

                // E4 - STMF
                (SeismicSystem.STMF, MemberType.Chord) => DuctilityLevel.HighlyDuctile,
                (SeismicSystem.STMF, MemberType.Diagonal) => DuctilityLevel.HighlyDuctile,

                // E5 - OCCS
                (SeismicSystem.OCCS, _) => DuctilityLevel.None,

                // E6 - SCCS
                (SeismicSystem.SCCS, MemberType.Column) => DuctilityLevel.HighlyDuctile,

                // F1 - OCBF
                (SeismicSystem.OCBF, MemberType.Brace) => DuctilityLevel.ModeratelyDuctile,

                // F2 - SCBF: §F2.5a "Columns, beams and braces - highly ductile; Struts - moderately ductile"
                (SeismicSystem.SCBF, MemberType.Brace) => DuctilityLevel.HighlyDuctile,
                (SeismicSystem.SCBF, MemberType.Column) => DuctilityLevel.HighlyDuctile,
                (SeismicSystem.SCBF, MemberType.Beam) => DuctilityLevel.HighlyDuctile,
                (SeismicSystem.SCBF, MemberType.Strut) => DuctilityLevel.ModeratelyDuctile,

                // F3 - EBF
                (SeismicSystem.EBF, MemberType.Link) => DuctilityLevel.HighlyDuctile,
                (SeismicSystem.EBF, MemberType.Brace) => DuctilityLevel.ModeratelyDuctile,
                (SeismicSystem.EBF, MemberType.Column) => DuctilityLevel.HighlyDuctile,
                (SeismicSystem.EBF, MemberType.Beam) => DuctilityLevel.ModeratelyDuctile,

                // F4 - BRBF: §F4.5a "Beams and columns - moderately ductile members"
                (SeismicSystem.BRBF, MemberType.Brace) => DuctilityLevel.ModeratelyDuctile,
                (SeismicSystem.BRBF, MemberType.Column) => DuctilityLevel.ModeratelyDuctile,
                (SeismicSystem.BRBF, MemberType.Beam) => DuctilityLevel.ModeratelyDuctile,

                // F5 - SPSW
                (SeismicSystem.SPSW, MemberType.HBE) => DuctilityLevel.HighlyDuctile,
                (SeismicSystem.SPSW, MemberType.VBE) => DuctilityLevel.HighlyDuctile,

                _ => DuctilityLevel.None
            };
        }

        public static string GetDuctilityReference(SeismicSystem system, MemberType member)
        {
            return (system, member) switch
            {
                (SeismicSystem.OMF, _) => "AISC 341 §E1: No width-to-thickness limitations beyond AISC 360 Specification",
                (SeismicSystem.OCCS, _) => "AISC 341 §E5: No specific ductility requirements",
                (SeismicSystem.IMF, MemberType.Beam) => "AISC 341 §E2.5a: Beams - Moderately Ductile (§D1.1)",
                (SeismicSystem.IMF, MemberType.Column) => "AISC 341 §E2.5a: Columns - Moderately Ductile (§D1.1)",
                (SeismicSystem.SMF, MemberType.Beam) => "AISC 341 §E3.5a: Beams - Highly Ductile (§D1.1)",
                (SeismicSystem.SMF, MemberType.Column) => "AISC 341 §E3.5a: Columns - Highly Ductile (§D1.1)",
                (SeismicSystem.STMF, MemberType.Chord) => "AISC 341 §E4.5a: Chord members - Highly Ductile (§D1.1)",
                (SeismicSystem.STMF, MemberType.Diagonal) => "AISC 341 §E4.5a: Diagonal web members - Highly Ductile (§D1.1)",
                (SeismicSystem.SCCS, MemberType.Column) => "AISC 341 §E6.5a: Columns - Highly Ductile (§D1.1)",
                (SeismicSystem.OCBF, MemberType.Brace) => "AISC 341 §F1.5a: Braces - Moderately Ductile (§D1.1)",
                (SeismicSystem.SCBF, MemberType.Brace) => "AISC 341 §F2.5a: Braces - Highly Ductile (§D1.1)",
                (SeismicSystem.SCBF, MemberType.Column) => "AISC 341 §F2.5a: Columns - Highly Ductile (§D1.1)",
                (SeismicSystem.SCBF, MemberType.Beam) => "AISC 341 §F2.5a: Beams - Highly Ductile (§D1.1)",
                (SeismicSystem.EBF, MemberType.Link) => "AISC 341 §F3.5a: Links - Highly Ductile (§D1.1)",
                (SeismicSystem.EBF, MemberType.Brace) => "AISC 341 §F3.5a: Braces - Moderately Ductile (§D1.1)",
                (SeismicSystem.EBF, MemberType.Column) => "AISC 341 §F3.5a: Columns - Highly Ductile (§D1.1)",
                (SeismicSystem.EBF, MemberType.Beam) => "AISC 341 §F3.5a: Beams (outside link) - Moderately Ductile (§D1.1)",
                (SeismicSystem.BRBF, MemberType.Brace) => "AISC 341 §F4.5a: Braces - Moderately Ductile (§D1.1)",
                (SeismicSystem.BRBF, MemberType.Column) => "AISC 341 §F4.5a: Columns - Moderately Ductile (§D1.1)",
                (SeismicSystem.BRBF, MemberType.Beam) => "AISC 341 §F4.5a: Beams - Moderately Ductile (§D1.1)",
                (SeismicSystem.SPSW, MemberType.HBE) => "AISC 341 §F5.5a: HBE - Highly Ductile (§D1.1)",
                (SeismicSystem.SPSW, MemberType.VBE) => "AISC 341 §F5.5a: VBE - Highly Ductile (§D1.1)",
                _ => ""
            };
        }

        #endregion

        #region Ry Default Values

        public static double GetDefaultRy(double Fy)
        {
            if (Fy <= 36) return 1.50;   // A36
            if (Fy <= 50) return 1.10;   // A572 Gr. 50, A992
            if (Fy <= 60) return 1.10;   // A572 Gr. 60
            if (Fy <= 65) return 1.10;   // A572 Gr. 65
            return 1.10;
        }

        #endregion

        #region Main Calculate

        public static CheckResult Calculate(InputParameters input)
        {
            var result = new CheckResult();

            // Determine ductility level
            result.RequiredLevel = GetRequiredDuctility(input.System, input.Member);
            result.RequiredLevelDescription = GetDuctilityReference(input.System, input.Member);

            var p = result.Process;
            string systemName = GetSystemDisplayName(input.System);
            string memberName = GetMemberDisplayName(input.Member);
            string sectionTypeName = GetSectionCategoryDisplayName(input.SectionType);

            p.Add("═══════════════════════════════════════════════════");
            p.Add("  AISC 341-16 构件延性分类与板件宽厚比校核");
            p.Add("  Ductility Classification & Width-to-Thickness Check");
            p.Add("═══════════════════════════════════════════════════");
            p.Add("");

            // Step 1: System & Member info
            p.Add("── 1. 系统与构件信息 ──");
            p.Add($"   抗震体系: {systemName}");
            p.Add($"   构件类型: {memberName}");
            p.Add($"   截面类型: {sectionTypeName}");
            p.Add("");

            // Step 2: Ductility level
            p.Add("── 2. 延性等级判定 ──");
            if (result.RequiredLevel == DuctilityLevel.None)
            {
                p.Add($"   延性等级: 无特殊要求 (No specific requirement)");
                p.Add($"   {result.RequiredLevelDescription}");
                p.Add("");
                p.Add("   该构件仅需满足 AISC 360 Specification 的宽厚比限制。");
                p.Add("   无需进行 AISC 341 抗震紧凑截面校核。");
                result.IsValid = true;
                result.SectionOK = true;
                result.FlangeOK = true;
                result.WebOK = true;
                return result;
            }

            string ductilityName = result.RequiredLevel == DuctilityLevel.HighlyDuctile
                ? "高延性构件 (Highly Ductile Member)"
                : "中等延性构件 (Moderately Ductile Member)";
            p.Add($"   延性等级: {ductilityName}");
            p.Add($"   依据: {result.RequiredLevelDescription}");
            p.Add("");

            // Step 3: Material parameters
            p.Add("── 3. 材料参数 ──");
            p.Add($"   Fy = {input.Fy:F1} ksi");
            p.Add($"   E  = {input.E:F0} ksi");
            p.Add($"   Ry = {input.Ry:F2}");
            p.Add($"   Ry·Fy = {input.Ry * input.Fy:F1} ksi");
            p.Add("");

            double sqrtE_RyFy = Math.Sqrt(input.E / (input.Ry * input.Fy));
            p.Add($"   √(E/(Ry·Fy)) = √({input.E:F0}/{input.Ry * input.Fy:F1}) = {sqrtE_RyFy:F3}");
            p.Add("");

            // Step 4: Section dimensions
            p.Add("── 4. 截面尺寸 ──");
            double h, bf_eff, b_flat, D_out;

            switch (input.SectionType)
            {
                case SectionCategory.IShape:
                case SectionCategory.HPile:
                    h = input.d - 2 * input.tf;
                    bf_eff = input.bf / 2.0;
                    p.Add($"   d  = {input.d:F3} in");
                    p.Add($"   bf = {input.bf:F3} in");
                    p.Add($"   tw = {input.tw:F3} in");
                    p.Add($"   tf = {input.tf:F3} in");
                    p.Add($"   h  = d - 2·tf = {h:F3} in (腹板净高)");
                    p.Add($"   b  = bf/2 = {bf_eff:F3} in (翼缘外伸宽度)");
                    p.Add("");
                    break;

                case SectionCategory.Channel:
                    h = input.d - 2 * input.tf;
                    p.Add($"   d  = {input.d:F3} in");
                    p.Add($"   bf = {input.bf:F3} in");
                    p.Add($"   tw = {input.tw:F3} in");
                    p.Add($"   tf = {input.tf:F3} in");
                    p.Add($"   h  = d - 2·tf = {h:F3} in (腹板净高)");
                    p.Add($"   b  = bf = {input.bf:F3} in (翼缘全宽)");
                    p.Add("");
                    break;

                case SectionCategory.Tee:
                    p.Add($"   d  = {input.d:F3} in (T形截面总高/腹板长)");
                    p.Add($"   bf = {input.bf:F3} in");
                    p.Add($"   tw = {input.tw:F3} in (腹板厚度)");
                    p.Add($"   tf = {input.tf:F3} in (翼缘厚度)");
                    p.Add($"   b  = bf/2 = {input.bf / 2.0:F3} in (翼缘外伸宽度)");
                    p.Add("");
                    break;

                case SectionCategory.SingleAngle:
                case SectionCategory.DoubleAngle:
                    p.Add($"   d  = {input.d:F3} in (竖向肢长)");
                    p.Add($"   bf = {input.bf:F3} in (水平肢长)");
                    p.Add($"   t  = {input.tf:F3} in (肢厚度)");
                    p.Add("");
                    break;

                case SectionCategory.HSS_Rect:
                    h = input.Ht - 3 * input.tdes;
                    b_flat = input.B_hss - 3 * input.tdes;
                    p.Add($"   Ht   = {input.Ht:F3} in");
                    p.Add($"   B    = {input.B_hss:F3} in");
                    p.Add($"   tdes = {input.tdes:F3} in");
                    p.Add($"   h (净高) = Ht - 3·tdes = {h:F3} in");
                    p.Add($"   b (净宽) = B - 3·tdes = {b_flat:F3} in");
                    p.Add("");
                    break;

                case SectionCategory.BoxBuiltUp:
                    h = input.d - 2 * input.tf;
                    b_flat = input.bf - 2 * input.tw;
                    p.Add($"   d  = {input.d:F3} in (箱体总高)");
                    p.Add($"   bf = {input.bf:F3} in (箱体总宽)");
                    p.Add($"   tw = {input.tw:F3} in (腹板厚度)");
                    p.Add($"   tf = {input.tf:F3} in (翼缘厚度)");
                    p.Add($"   h (腹板净高) = d - 2·tf = {h:F3} in");
                    p.Add($"   b (翼缘净宽) = bf - 2·tw = {b_flat:F3} in");
                    p.Add("");
                    break;

                case SectionCategory.HSS_Round:
                    D_out = input.D_hss;
                    p.Add($"   D  = {D_out:F3} in (外径)");
                    p.Add($"   t  = {input.tdes:F3} in");
                    p.Add("");
                    break;

                default:
                    h = input.d - 2 * input.tf;
                    bf_eff = input.bf / 2.0;
                    p.Add($"   d  = {input.d:F3} in");
                    p.Add($"   bf = {input.bf:F3} in");
                    p.Add($"   tw = {input.tw:F3} in");
                    p.Add($"   tf = {input.tf:F3} in");
                    p.Add("");
                    break;
            }

            // Step 5: Compute Ca for webs in flexure/combined
            double Ca = 0;
            bool needsCa = false;

            if (input.SectionType == SectionCategory.IShape ||
                input.SectionType == SectionCategory.Channel ||
                input.SectionType == SectionCategory.BoxBuiltUp)
            {
                // Web check for beams/columns in flexure or combined axial and flexure
                if (input.Member == MemberType.Beam || input.Member == MemberType.Column ||
                    input.Member == MemberType.Link || input.Member == MemberType.HBE ||
                    input.Member == MemberType.VBE || input.Member == MemberType.Chord ||
                    input.Member == MemberType.Diagonal || input.Member == MemberType.Strut)
                {
                    needsCa = true;
                }
            }

            if (needsCa)
            {
                p.Add("── 5. 轴力比 Ca 计算 ──");
                double Py = input.Ry * input.Fy * input.Ag;

                if (input.Method == DesignMethod.LRFD)
                {
                    Ca = input.Pu / (input.phi_c * Py);
                    p.Add($"   LRFD: Ca = Pu / (φc·Py)");
                    p.Add($"   Py = Ry·Fy·Ag = {input.Ry:F2}×{input.Fy:F1}×{input.Ag:F2} = {Py:F1} kips");
                    p.Add($"   Ca = {input.Pu:F1} / ({input.phi_c:F2}×{Py:F1}) = {Ca:F4}");
                }
                else
                {
                    Ca = input.Omega_c * input.Pa / Py;
                    p.Add($"   ASD: Ca = Ωc·Pa / Py");
                    p.Add($"   Py = Ry·Fy·Ag = {input.Ry:F2}×{input.Fy:F1}×{input.Ag:F2} = {Py:F1} kips");
                    p.Add($"   Ca = {input.Omega_c:F2}×{input.Pa:F1} / {Py:F1} = {Ca:F4}");
                }

                // Clamp Ca to non-negative: tension reduces web compression demand
                if (Ca < 0)
                {
                    p.Add($"   Ca < 0 (受拉), 取 Ca = 0");
                    Ca = 0;
                }
                p.Add("");
            }

            // Step 6: Flange check (unstiffened element)
            p.Add(needsCa ? "── 6. 翼缘宽厚比校核 (Unstiffened Element) ──"
                          : "── 5. 翼缘宽厚比校核 (Unstiffened Element) ──");
            p.Add("   Table D1.1 - Flanges of rolled or built-up I-shaped sections");
            p.Add("");

            CheckFlange(input, result, sqrtE_RyFy, p);

            // Step 7: Web check (stiffened element)
            int stepNum = needsCa ? 7 : 6;
            p.Add($"── {stepNum}. 腹板宽厚比校核 (Stiffened Element) ──");
            p.Add("   Table D1.1 - Webs in flexure or combined axial and flexure");
            p.Add("");

            CheckWeb(input, result, sqrtE_RyFy, Ca, p);

            // Step 8: Summary
            int summaryStep = needsCa ? 8 : 7;
            p.Add($"── {summaryStep}. 校核总结 ──");
            p.Add("");

            result.SectionOK = result.FlangeOK && result.WebOK;
            result.IsValid = true;

            string flangeStatus = result.FlangeOK ? "✓ 通过 (PASS)" : "✗ 不满足 (FAIL)";
            string webStatus = result.WebOK ? "✓ 通过 (PASS)" : "✗ 不满足 (FAIL)";
            string overallStatus = result.SectionOK ? "✓ 满足抗震紧凑截面要求" : "✗ 不满足抗震紧凑截面要求";

            p.Add($"   翼缘: {flangeStatus}");
            p.Add($"     实际 = {result.FlangeRatioActual:F3}, 限值 = {result.FlangeRatioLimit:F3}");
            p.Add($"   腹板: {webStatus}");
            p.Add($"     实际 = {result.WebRatioActual:F3}, 限值 = {result.WebRatioLimit:F3}");
            p.Add("");
            p.Add($"   结论: {overallStatus}");

            if (!result.SectionOK)
            {
                p.Add("");
                p.Add("   建议: 选择更厚实截面或降低构件轴力比。");
            }

            return result;
        }

        #endregion

        #region Flange Check

        private static void CheckFlange(InputParameters input, CheckResult result, double sqrtE_RyFy, List<string> p)
        {
            bool isHighlyDuctile = result.RequiredLevel == DuctilityLevel.HighlyDuctile;
            double lambda_hd, lambda_md, actualRatio;

            switch (input.SectionType)
            {
                case SectionCategory.IShape:
                case SectionCategory.Tee:
                    // Flanges of I-shaped sections and tees: b = bf/2
                    actualRatio = (input.bf / 2.0) / input.tf;
                    lambda_hd = 0.32 * sqrtE_RyFy;
                    lambda_md = 0.40 * sqrtE_RyFy;
                    result.FlangeElementDescription = "Flanges of I-shaped / tee sections";
                    result.FlangeFormula = isHighlyDuctile ? "λhd = 0.32√(E/(Ry·Fy))" : "λmd = 0.40√(E/(Ry·Fy))";

                    p.Add($"   翼缘类型: 翼缘外伸 (Flange outstanding, b=bf/2)");
                    p.Add($"   宽厚比 = b/t = (bf/2)/tf");
                    p.Add($"          = ({input.bf:F3}/2) / {input.tf:F3}");
                    p.Add($"          = {actualRatio:F3}");
                    p.Add($"   λhd = 0.32 × {sqrtE_RyFy:F3} = {lambda_hd:F3}");
                    p.Add($"   λmd = 0.40 × {sqrtE_RyFy:F3} = {lambda_md:F3}");
                    break;

                case SectionCategory.Channel:
                case SectionCategory.DoubleAngle:
                case SectionCategory.SingleAngle:
                    // Channels: b = bf (full flange width, projects from one side of web)
                    // Angles: b = bf (full leg width)
                    actualRatio = input.bf / input.tf;
                    lambda_hd = 0.32 * sqrtE_RyFy;
                    lambda_md = 0.40 * sqrtE_RyFy;
                    result.FlangeElementDescription = "Flanges of channels / legs of angles";
                    result.FlangeFormula = isHighlyDuctile ? "λhd = 0.32√(E/(Ry·Fy))" : "λmd = 0.40√(E/(Ry·Fy))";

                    p.Add($"   翼缘类型: 槽钢翼缘/角钢肢 (Full width, b=bf)");
                    p.Add($"   宽厚比 = b/t = bf/tf");
                    p.Add($"          = {input.bf:F3} / {input.tf:F3}");
                    p.Add($"          = {actualRatio:F3}");
                    p.Add($"   λhd = 0.32 × {sqrtE_RyFy:F3} = {lambda_hd:F3}");
                    p.Add($"   λmd = 0.40 × {sqrtE_RyFy:F3} = {lambda_md:F3}");
                    break;

                case SectionCategory.HPile:
                    // H-Pile flanges: no highly ductile limit
                    actualRatio = (input.bf / 2.0) / input.tf;
                    lambda_hd = double.MaxValue; // not applicable
                    lambda_md = 0.48 * sqrtE_RyFy;
                    result.FlangeElementDescription = "Flanges of H-Pile sections per §D4";
                    result.FlangeFormula = "λmd = 0.48√(E/(Ry·Fy)) (λhd not applicable)";

                    p.Add($"   翼缘类型: H-Pile 翼缘 (§D4)");
                    p.Add($"   宽厚比 = b/t = (bf/2)/tf = {actualRatio:F3}");
                    p.Add($"   λhd = Not Applicable (H-Pile 仅中等延性)");
                    p.Add($"   λmd = 0.48 × {sqrtE_RyFy:F3} = {lambda_md:F3}");
                    break;

                case SectionCategory.HSS_Rect:
                    {
                        // Table D1.1: rectangular HSS walls
                        // As diagonal braces: 0.65/0.76
                        // As beams/columns (flanges in uniform compression): 0.65/1.18
                        double h_hss_f = input.Ht - 3 * input.tdes;
                        double b_hss = input.B_hss - 3 * input.tdes;
                        actualRatio = b_hss / input.tdes;
                        lambda_hd = 0.65 * sqrtE_RyFy;

                        if (input.Member == MemberType.Brace || input.Member == MemberType.Diagonal)
                        {
                            lambda_md = 0.76 * sqrtE_RyFy;
                            result.FlangeElementDescription = "Walls of rectangular HSS used as diagonal braces";
                            result.FlangeFormula = isHighlyDuctile ? "λhd = 0.65√(E/(Ry·Fy))" : "λmd = 0.76√(E/(Ry·Fy))";
                            p.Add($"   翼缘类型: 矩形HSS壁板 (用作斜撑)");
                            p.Add($"   λmd = 0.76 × {sqrtE_RyFy:F3} = {lambda_md:F3}");
                        }
                        else
                        {
                            lambda_md = 1.18 * sqrtE_RyFy;
                            result.FlangeElementDescription = "Walls of rectangular HSS (flange in uniform compression)";
                            result.FlangeFormula = isHighlyDuctile ? "λhd = 0.65√(E/(Ry·Fy))" : "λmd = 1.18√(E/(Ry·Fy))";
                            p.Add($"   翼缘类型: 矩形HSS壁板 (均匀受压)");
                            p.Add($"   λmd = 1.18 × {sqrtE_RyFy:F3} = {lambda_md:F3}");
                        }

                        p.Add($"   宽厚比 = b/t = {b_hss:F3}/{input.tdes:F3} = {actualRatio:F3}");
                        p.Add($"   λhd = 0.65 × {sqrtE_RyFy:F3} = {lambda_hd:F3}");
                        break;
                    }

                case SectionCategory.HSS_Round:
                    // Walls of round HSS
                    actualRatio = input.D_hss / input.tdes;
                    lambda_hd = 0.053 * input.E / (input.Ry * input.Fy);
                    lambda_md = 0.062 * input.E / (input.Ry * input.Fy);
                    result.FlangeElementDescription = "Walls of round HSS (D/t)";
                    result.FlangeFormula = isHighlyDuctile ? "λhd = 0.053E/(Ry·Fy)" : "λmd = 0.062E/(Ry·Fy)";

                    p.Add($"   圆管径厚比 = D/t = {input.D_hss:F3}/{input.tdes:F3} = {actualRatio:F3}");
                    p.Add($"   λhd = 0.053 × {input.E:F0} / {input.Ry * input.Fy:F1} = {lambda_hd:F3}");
                    p.Add($"   λmd = 0.062 × {input.E:F0} / {input.Ry * input.Fy:F1} = {lambda_md:F3}");
                    break;

                case SectionCategory.BoxBuiltUp:
                    {
                        // Table D1.1: built-up box shapes (welded plates, separate flange/web thickness)
                        // As diagonal braces: 0.65/0.76 (side plates/walls)
                        // As link beams: 0.65/0.76 (flanges of built-up box used as link beams)
                        // As beams/columns (flanges in uniform compression): 0.65/1.18
                        double b_box = input.bf - 2 * input.tw;
                        actualRatio = b_box / input.tf;
                        lambda_hd = 0.65 * sqrtE_RyFy;

                        if (input.Member == MemberType.Brace || input.Member == MemberType.Diagonal
                            || input.Member == MemberType.Link)
                        {
                            lambda_md = 0.76 * sqrtE_RyFy;
                            result.FlangeElementDescription = input.Member == MemberType.Link
                                ? "Flanges of built-up box shapes used as link beams"
                                : "Side plates of built-up box shapes used as diagonal braces";
                            result.FlangeFormula = isHighlyDuctile ? "λhd = 0.65√(E/(Ry·Fy))" : "λmd = 0.76√(E/(Ry·Fy))";
                            p.Add($"   翼缘类型: 焊接箱形截面 (用作{(input.Member == MemberType.Link ? "连梁" : "斜撑")})");
                            p.Add($"   λmd = 0.76 × {sqrtE_RyFy:F3} = {lambda_md:F3}");
                        }
                        else
                        {
                            lambda_md = 1.18 * sqrtE_RyFy;
                            result.FlangeElementDescription = "Flanges of built-up box shapes (flanges in uniform compression)";
                            result.FlangeFormula = isHighlyDuctile ? "λhd = 0.65√(E/(Ry·Fy))" : "λmd = 1.18√(E/(Ry·Fy))";
                            p.Add($"   翼缘类型: 焊接箱形截面翼缘 (均匀受压)");
                            p.Add($"   λmd = 1.18 × {sqrtE_RyFy:F3} = {lambda_md:F3}");
                        }

                        p.Add($"   宽厚比 = b/tf = {b_box:F3}/{input.tf:F3} = {actualRatio:F3}");
                        p.Add($"   λhd = 0.65 × {sqrtE_RyFy:F3} = {lambda_hd:F3}");
                        break;
                    }

                default:
                    actualRatio = (input.bf / 2.0) / input.tf;
                    lambda_hd = 0.32 * sqrtE_RyFy;
                    lambda_md = 0.40 * sqrtE_RyFy;
                    result.FlangeFormula = isHighlyDuctile ? "λhd = 0.32√(E/(Ry·Fy))" : "λmd = 0.40√(E/(Ry·Fy))";
                    break;
            }

            double limit = isHighlyDuctile ? lambda_hd : lambda_md;

            result.FlangeRatioActual = actualRatio;
            result.FlangeRatioLimit = limit;

            // For HPile with highly ductile, there's no flange limit defined
            if (input.SectionType == SectionCategory.HPile && isHighlyDuctile)
            {
                result.FlangeOK = false;
                p.Add($"   ⚠ H-Pile 截面不适用于高延性构件要求");
            }
            else
            {
                result.FlangeOK = actualRatio <= limit;
            }

            p.Add($"   适用限值: {result.FlangeFormula} = {limit:F3}");
            p.Add($"   校核: {actualRatio:F3} {(result.FlangeOK ? "≤" : ">")} {limit:F3} → {(result.FlangeOK ? "✓ 通过" : "✗ 不满足")}");
            p.Add("");
        }

        #endregion

        #region Web Check

        private static void CheckWeb(InputParameters input, CheckResult result, double sqrtE_RyFy, double Ca, List<string> p)
        {
            bool isHighlyDuctile = result.RequiredLevel == DuctilityLevel.HighlyDuctile;
            double actualRatio, limit;
            double RyFy = input.Ry * input.Fy;

            // Angles have no stiffened (web) element in Table D1.1 — all legs are unstiffened
            if (input.SectionType == SectionCategory.SingleAngle ||
                input.SectionType == SectionCategory.DoubleAngle)
            {
                result.WebOK = true;
                result.WebRatioActual = 0;
                result.WebRatioLimit = 0;
                p.Add("   截面为角钢，无腹板（加劲件），本项免检。");
                p.Add("");
                return;
            }

            switch (input.SectionType)
            {
                case SectionCategory.IShape:
                case SectionCategory.Channel:
                    // Check if brace (diagonal) or beam/column
                    if (input.Member == MemberType.Brace || input.Member == MemberType.Diagonal)
                    {
                        // Webs used as diagonal braces: h/tw limit
                        double h_web = input.d - 2 * input.tf;
                        actualRatio = h_web / input.tw;

                        // Both HD and MD: 1.57√(E/(RyFy))
                        limit = 1.57 * sqrtE_RyFy;
                        result.WebFormula = "λhd = λmd = 1.57√(E/(Ry·Fy))";
                        result.WebElementDescription = "Webs of I-shaped sections used as diagonal braces";

                        p.Add($"   腹板类型: 工字形截面腹板用作斜撑");
                        p.Add($"   宽厚比 = h/tw = {h_web:F3}/{input.tw:F3} = {actualRatio:F3}");
                        p.Add($"   λhd = λmd = 1.57 × {sqrtE_RyFy:F3} = {limit:F3}");
                    }
                    else
                    {
                        // Webs in flexure or combined axial and flexure (beam, column, link)
                        double h_web = input.d - 2 * input.tf;
                        actualRatio = h_web / input.tw;

                        double lambdaMin = 1.57 * sqrtE_RyFy;

                        // Check for SMF beam special case
                        bool isSmfBeam = input.System == SeismicSystem.SMF && input.Member == MemberType.Beam;
                        bool isImfBeam = input.System == SeismicSystem.IMF && input.Member == MemberType.Beam;

                        double calculatedLimit;
                        string formulaDetail;

                        if (isHighlyDuctile)
                        {
                            if (isSmfBeam && Ca <= 0.114)
                            {
                                // SMF beam special: max 2.57√(E/RyFy) per footnote [b]
                                calculatedLimit = Math.Min(
                                    2.57 * sqrtE_RyFy * (1 - 1.04 * Ca),
                                    2.57 * sqrtE_RyFy);
                                formulaDetail = "SMF梁: min[2.57√(E/(Ry·Fy)), 2.57√(E/(Ry·Fy))·(1-1.04Ca)]";
                            }
                            else if (Ca <= 0.114)
                            {
                                calculatedLimit = 2.57 * sqrtE_RyFy * (1 - 1.04 * Ca);
                                formulaDetail = "Ca≤0.114: λhd = 2.57√(E/(Ry·Fy))·(1-1.04Ca)";
                            }
                            else
                            {
                                calculatedLimit = 0.88 * sqrtE_RyFy * (2.68 - Ca);
                                formulaDetail = "Ca>0.114: λhd = 0.88√(E/(Ry·Fy))·(2.68-Ca)";
                            }

                            limit = Math.Max(calculatedLimit, lambdaMin);
                            result.WebFormula = $"λhd ≥ 1.57√(E/(Ry·Fy))";
                        }
                        else
                        {
                            if (isImfBeam && Ca <= 0.114)
                            {
                                // IMF beam special: max 3.96√(E/RyFy) per footnote [b]
                                calculatedLimit = Math.Min(
                                    3.96 * sqrtE_RyFy * (1 - 3.04 * Ca),
                                    3.96 * sqrtE_RyFy);
                                formulaDetail = "IMF梁: min[3.96√(E/(Ry·Fy)), 3.96√(E/(Ry·Fy))·(1-3.04Ca)]";
                            }
                            else if (Ca <= 0.114)
                            {
                                calculatedLimit = 3.96 * sqrtE_RyFy * (1 - 3.04 * Ca);
                                formulaDetail = "Ca≤0.114: λmd = 3.96√(E/(Ry·Fy))·(1-3.04Ca)";
                            }
                            else
                            {
                                calculatedLimit = 1.29 * sqrtE_RyFy * (2.12 - Ca);
                                formulaDetail = "Ca>0.114: λmd = 1.29√(E/(Ry·Fy))·(2.12-Ca)";
                            }

                            limit = Math.Max(calculatedLimit, lambdaMin);
                            result.WebFormula = $"λmd ≥ 1.57√(E/(Ry·Fy))";
                        }

                        result.WebElementDescription = "Webs in flexure or combined axial and flexure";

                        p.Add($"   腹板类型: 工字形截面腹板 (受弯或压弯)");
                        p.Add($"   宽厚比 = h/tw = {h_web:F3}/{input.tw:F3} = {actualRatio:F3}");
                        p.Add($"   Ca = {Ca:F4}");
                        p.Add($"   计算公式: {formulaDetail}");
                        p.Add($"   计算限值 = {calculatedLimit:F3}");
                        p.Add($"   最小限值 = 1.57 × {sqrtE_RyFy:F3} = {lambdaMin:F3}");
                        p.Add($"   取用限值 = max({calculatedLimit:F3}, {lambdaMin:F3}) = {limit:F3}");
                    }
                    break;

                case SectionCategory.HSS_Rect:
                    if (input.Member == MemberType.Brace || input.Member == MemberType.Diagonal)
                    {
                        // Table D1.1: Walls of rectangular HSS used as diagonal braces
                        double h_hss_br = input.Ht - 3 * input.tdes;
                        actualRatio = h_hss_br / input.tdes;

                        limit = isHighlyDuctile ? 0.65 * sqrtE_RyFy : 0.76 * sqrtE_RyFy;
                        result.WebFormula = isHighlyDuctile ? "λhd = 0.65√(E/(Ry·Fy))" : "λmd = 0.76√(E/(Ry·Fy))";
                        result.WebElementDescription = "Walls of rectangular HSS used as diagonal braces";

                        p.Add($"   腹板类型: 矩形HSS壁板 (用作斜撑)");
                        p.Add($"   宽厚比 = h/t = {h_hss_br:F3}/{input.tdes:F3} = {actualRatio:F3}");
                        p.Add($"   λhd = 0.65 × {sqrtE_RyFy:F3} = {0.65 * sqrtE_RyFy:F3}");
                        p.Add($"   λmd = 0.76 × {sqrtE_RyFy:F3} = {0.76 * sqrtE_RyFy:F3}");
                    }
                    else
                    {
                        // Table D1.1: Rectangular HSS beams/columns use same limits for both b/t and h/t
                        // "Where used in beams or columns as flanges in uniform compression: Walls of rectangular HSS"
                        // Both flange and web (h/t) use λhd=0.65, λmd=1.18
                        double h_hss_bc = input.Ht - 3 * input.tdes;
                        actualRatio = h_hss_bc / input.tdes;

                        limit = isHighlyDuctile ? 0.65 * sqrtE_RyFy : 1.18 * sqrtE_RyFy;
                        result.WebFormula = isHighlyDuctile ? "λhd = 0.65√(E/(Ry·Fy))" : "λmd = 1.18√(E/(Ry·Fy))";
                        result.WebElementDescription = "Walls of rectangular HSS in uniform compression (beams/columns)";

                        p.Add($"   腹板类型: 矩形HSS壁板 (梁柱均匀受压)");
                        p.Add($"   宽厚比 = h/t = {h_hss_bc:F3}/{input.tdes:F3} = {actualRatio:F3}");
                        p.Add($"   λhd = 0.65 × {sqrtE_RyFy:F3} = {0.65 * sqrtE_RyFy:F3}");
                        p.Add($"   λmd = 1.18 × {sqrtE_RyFy:F3} = {1.18 * sqrtE_RyFy:F3}");
                    }
                    break;

                case SectionCategory.BoxBuiltUp:
                    if (input.Member == MemberType.Brace || input.Member == MemberType.Diagonal)
                    {
                        // Table D1.1: side plates/walls of built-up box used as diagonal braces
                        double h_box_br = input.d - 2 * input.tf;
                        actualRatio = h_box_br / input.tw;

                        limit = isHighlyDuctile ? 0.65 * sqrtE_RyFy : 0.76 * sqrtE_RyFy;
                        result.WebFormula = isHighlyDuctile ? "λhd = 0.65√(E/(Ry·Fy))" : "λmd = 0.76√(E/(Ry·Fy))";
                        result.WebElementDescription = "Walls of built-up box shapes used as diagonal braces";

                        p.Add($"   腹板类型: 焊接箱形截面壁板 (用作斜撑)");
                        p.Add($"   宽厚比 = h/tw = {h_box_br:F3}/{input.tw:F3} = {actualRatio:F3}");
                    }
                    else if (input.Member == MemberType.Link)
                    {
                        // Webs of built-up box sections used as EBF links
                        double h_box = input.d - 2 * input.tf;
                        actualRatio = h_box / input.tw;

                        limit = isHighlyDuctile ? 0.67 * sqrtE_RyFy : 1.75 * sqrtE_RyFy;
                        result.WebFormula = isHighlyDuctile ? "λhd = 0.67√(E/(Ry·Fy))" : "λmd = 1.75√(E/(Ry·Fy))";
                        result.WebElementDescription = "Webs of built-up box sections used as EBF links";

                        p.Add($"   腹板类型: 焊接箱形截面腹板 (EBF连梁)");
                        p.Add($"   宽厚比 = h/tw = {h_box:F3}/{input.tw:F3} = {actualRatio:F3}");
                    }
                    else
                    {
                        // Table D1.1: Webs of built-up box sections in flexure or combined
                        // Uses Ca-dependent formulas (same as I-shape webs)
                        double h_box_bc = input.d - 2 * input.tf;
                        actualRatio = h_box_bc / input.tw;

                        double lambdaMin = 1.57 * sqrtE_RyFy;
                        double calculatedLimit;

                        if (isHighlyDuctile)
                        {
                            if (Ca <= 0.114)
                                calculatedLimit = 2.57 * sqrtE_RyFy * (1 - 1.04 * Ca);
                            else
                                calculatedLimit = 0.88 * sqrtE_RyFy * (2.68 - Ca);
                        }
                        else
                        {
                            if (Ca <= 0.114)
                                calculatedLimit = 3.96 * sqrtE_RyFy * (1 - 3.04 * Ca);
                            else
                                calculatedLimit = 1.29 * sqrtE_RyFy * (2.12 - Ca);
                        }

                        limit = Math.Max(calculatedLimit, lambdaMin);
                        result.WebFormula = "Web Ca-dependent formula ≥ 1.57√(E/(Ry·Fy))";
                        result.WebElementDescription = "Webs of built-up box sections in flexure or combined";

                        p.Add($"   腹板类型: 焊接箱形截面腹板 (受弯或压弯)");
                        p.Add($"   宽厚比 = h/tw = {h_box_bc:F3}/{input.tw:F3} = {actualRatio:F3}");
                        p.Add($"   Ca = {Ca:F4}");
                        p.Add($"   计算限值 = {calculatedLimit:F3}");
                        p.Add($"   最小限值 = {lambdaMin:F3}");
                        p.Add($"   取用限值 = max({calculatedLimit:F3}, {lambdaMin:F3}) = {limit:F3}");
                    }

                    p.Add($"   λhd = {(isHighlyDuctile ? "0.65" : "—")} × {sqrtE_RyFy:F3}");
                    p.Add($"   λmd = {(isHighlyDuctile ? "—" : "0.76")} × {sqrtE_RyFy:F3}");
                    break;

                case SectionCategory.HSS_Round:
                    // Round HSS uses D/t for the web check too (same as flange)
                    actualRatio = input.D_hss / input.tdes;
                    limit = isHighlyDuctile ? 0.053 * input.E / RyFy : 0.062 * input.E / RyFy;
                    result.WebFormula = isHighlyDuctile ? "λhd = 0.053E/(Ry·Fy)" : "λmd = 0.062E/(Ry·Fy)";
                    result.WebElementDescription = "Walls of round HSS (D/t)";

                    p.Add($"   圆管径厚比 = D/t = {actualRatio:F3}");
                    p.Add($"   限值 = {limit:F3}");
                    break;

                case SectionCategory.HPile:
                    // Webs of H-Pile sections
                    double h_hp = input.d - 2 * input.tf;
                    actualRatio = h_hp / input.tw;

                    // HD: not applicable; MD: 1.57√(E/RyFy)
                    if (isHighlyDuctile)
                    {
                        limit = 1.57 * sqrtE_RyFy; // Still apply MD limit as fallback
                        result.WebFormula = "H-Pile λhd not applicable, using λmd = 1.57√(E/(Ry·Fy))";
                    }
                    else
                    {
                        limit = 1.57 * sqrtE_RyFy;
                        result.WebFormula = "λmd = 1.57√(E/(Ry·Fy))";
                    }
                    result.WebElementDescription = "Webs of H-Pile sections";

                    p.Add($"   腹板类型: H-Pile 截面腹板");
                    p.Add($"   宽厚比 = h/tw = {h_hp:F3}/{input.tw:F3} = {actualRatio:F3}");
                    p.Add($"   λmd = 1.57 × {sqrtE_RyFy:F3} = {limit:F3}");
                    break;

                case SectionCategory.Tee:
                    // Stems of tees: d/t where t is the stem (web) thickness
                    actualRatio = input.d / input.tw;
                    limit = isHighlyDuctile ? 0.32 * sqrtE_RyFy : 0.40 * sqrtE_RyFy;
                    result.WebFormula = isHighlyDuctile ? "λhd = 0.32√(E/(Ry·Fy))" : "λmd = 0.40√(E/(Ry·Fy))";
                    result.WebElementDescription = "Stems of tees";

                    p.Add($"   腹板类型: T形截面腹板");
                    p.Add($"   宽厚比 = d/t = {actualRatio:F3}");
                    p.Add($"   限值 = {limit:F3}");
                    break;

                default:
                    double h_def = input.d - 2 * input.tf;
                    actualRatio = h_def / input.tw;
                    limit = 1.57 * sqrtE_RyFy;
                    result.WebFormula = "λ = 1.57√(E/(Ry·Fy))";
                    break;
            }

            result.WebRatioActual = actualRatio;
            result.WebRatioLimit = limit;
            result.WebOK = actualRatio <= limit;

            p.Add($"   适用限值: {result.WebFormula} = {limit:F3}");
            p.Add($"   校核: {actualRatio:F3} {(result.WebOK ? "≤" : ">")} {limit:F3} → {(result.WebOK ? "✓ 通过" : "✗ 不满足")}");
            p.Add("");
        }

        #endregion

        #region Display Helpers

        public static string GetSystemDisplayName(SeismicSystem s) => s switch
        {
            SeismicSystem.OMF => "OMF - Ordinary Moment Frames (普通抗弯框架)",
            SeismicSystem.IMF => "IMF - Intermediate Moment Frames (中等抗弯框架)",
            SeismicSystem.SMF => "SMF - Special Moment Frames (特殊抗弯框架)",
            SeismicSystem.STMF => "STMF - Special Truss Moment Frames (特殊桁架抗弯框架)",
            SeismicSystem.OCCS => "OCCS - Ordinary Cantilever Column Systems (普通悬臂柱体系)",
            SeismicSystem.SCCS => "SCCS - Special Cantilever Column Systems (特殊悬臂柱体系)",
            SeismicSystem.OCBF => "OCBF - Ordinary Concentrically Braced Frames (普通中心支撑框架)",
            SeismicSystem.SCBF => "SCBF - Special Concentrically Braced Frames (特殊中心支撑框架)",
            SeismicSystem.EBF => "EBF - Eccentrically Braced Frames (偏心支撑框架)",
            SeismicSystem.BRBF => "BRBF - Buckling-Restrained Braced Frames (屈曲约束支撑框架)",
            SeismicSystem.SPSW => "SPSW - Special Plate Shear Walls (特殊钢板剪力墙)",
            _ => s.ToString()
        };

        public static string GetMemberDisplayName(MemberType m) => m switch
        {
            MemberType.Beam => "Beam (梁)",
            MemberType.Column => "Column (柱)",
            MemberType.Brace => "Brace (支撑)",
            MemberType.Link => "Link (连梁/耗能段)",
            MemberType.HBE => "HBE (水平边界构件)",
            MemberType.VBE => "VBE (竖向边界构件)",
            MemberType.Chord => "Chord (弦杆)",
            MemberType.Diagonal => "Diagonal (斜杆)",
            MemberType.Strut => "Strut (腹杆)",
            _ => m.ToString()
        };

        public static string GetSectionCategoryDisplayName(SectionCategory sc) => sc switch
        {
            SectionCategory.IShape => "I-Shape (工字形)",
            SectionCategory.HSS_Rect => "HSS Rectangular (矩形管)",
            SectionCategory.HSS_Round => "HSS Round (圆管)",
            SectionCategory.BoxBuiltUp => "Built-up Box (焊接箱形)",
            SectionCategory.DoubleAngle => "Double Angle (双角钢)",
            SectionCategory.SingleAngle => "Single Angle (单角钢)",
            SectionCategory.Tee => "Tee (T形)",
            SectionCategory.HPile => "H-Pile (H型桩)",
            SectionCategory.Channel => "Channel (槽钢)",
            _ => sc.ToString()
        };

        public static List<MemberType> GetAvailableMembers(SeismicSystem system)
        {
            return system switch
            {
                SeismicSystem.OMF => new() { MemberType.Beam, MemberType.Column },
                SeismicSystem.IMF => new() { MemberType.Beam, MemberType.Column },
                SeismicSystem.SMF => new() { MemberType.Beam, MemberType.Column },
                SeismicSystem.STMF => new() { MemberType.Chord, MemberType.Diagonal },
                SeismicSystem.OCCS => new() { MemberType.Column },
                SeismicSystem.SCCS => new() { MemberType.Column },
                SeismicSystem.OCBF => new() { MemberType.Brace },
                SeismicSystem.SCBF => new() { MemberType.Brace, MemberType.Column, MemberType.Beam, MemberType.Strut },
                SeismicSystem.EBF => new() { MemberType.Link, MemberType.Brace, MemberType.Column, MemberType.Beam },
                SeismicSystem.BRBF => new() { MemberType.Brace, MemberType.Column, MemberType.Beam },
                SeismicSystem.SPSW => new() { MemberType.HBE, MemberType.VBE },
                _ => new() { MemberType.Beam, MemberType.Column, MemberType.Brace }
            };
        }

        #endregion
    }
}
