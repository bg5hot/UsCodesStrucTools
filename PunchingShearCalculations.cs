using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class PunchingShearCalculations
    {
        public enum ColumnLocation
        {
            Interior,
            Edge,
            Corner
        }

        public enum ShearReinforcementType
        {
            None,
            Stirrups,
            HeadedStuds
        }

        public class InputParameters
        {
            public double H { get; set; }
            public double Cover { get; set; }
            public double BarDiameter { get; set; }
            public double C1 { get; set; }
            public double C2 { get; set; }
            public ColumnLocation Location { get; set; }
            public double Vu { get; set; }
            public double Msc { get; set; }
            public double Fc { get; set; }
            public double Lambda { get; set; } = 1.0;
            public double Fyt { get; set; } = 60000;
            public ShearReinforcementType ReinforcementType { get; set; } = ShearReinforcementType.None;
            public double StudArea { get; set; }
            public int StudsPerPerimeter { get; set; }
            public double StudSpacing { get; set; }
        }

        public class CriticalSectionProperties
        {
            public double D { get; set; }
            public double B1 { get; set; }
            public double B2 { get; set; }
            public double Bo { get; set; }
            public double Ac { get; set; }
            public double Jc { get; set; }
            public double CAB { get; set; }
            public double CCD { get; set; }
            public double GammaV { get; set; }
            public double GammaF { get; set; }
        }

        public class DesignResult
        {
            public bool IsSectionAdequate { get; set; }
            public bool NeedsReinforcement { get; set; }
            public double Vu_demand { get; set; }
            public double Vc_concrete { get; set; }
            public double PhiVc { get; set; }
            public double Vs_required { get; set; }
            public double Vs_provided { get; set; }
            public double PhiVn { get; set; }
            public double Vmax { get; set; }
            public double LambdaS { get; set; }
            public CriticalSectionProperties? Section { get; set; }
            public List<string> DesignProcess { get; set; } = new List<string>();
            public string? ResultSummary { get; set; }
            public string? ReinforcementResult { get; set; }
        }

        public static readonly Dictionary<string, double> BarAreas = new Dictionary<string, double>
        {
            ["#3"] = 0.11,
            ["#4"] = 0.20,
            ["#5"] = 0.31,
            ["#6"] = 0.44,
            ["#7"] = 0.60,
            ["#8"] = 0.79,
            ["#9"] = 1.00,
            ["#10"] = 1.27,
            ["#11"] = 1.56
        };

        public static readonly Dictionary<string, double> BarDiameters = new Dictionary<string, double>
        {
            ["#3"] = 0.375,
            ["#4"] = 0.500,
            ["#5"] = 0.625,
            ["#6"] = 0.750,
            ["#7"] = 0.875,
            ["#8"] = 1.000,
            ["#9"] = 1.128,
            ["#10"] = 1.270,
            ["#11"] = 1.410
        };

        public static double GetBarArea(string barSize)
        {
            return BarAreas.GetValueOrDefault(barSize, 0.20);
        }

        public static double GetBarDiameter(string barSize)
        {
            return BarDiameters.GetValueOrDefault(barSize, 0.5);
        }

        public static CriticalSectionProperties CalculateCriticalSection(InputParameters input)
        {
            var section = new CriticalSectionProperties();
            
            section.D = input.H - input.Cover - input.BarDiameter; // 偏保守，直接扣除钢筋直径
            double d = section.D;

            switch (input.Location)
            {
                case ColumnLocation.Interior:
                    section.B1 = input.C1 + d;
                    section.B2 = input.C2 + d;
                    section.Bo = 2 * section.B1 + 2 * section.B2;
                    section.CAB = section.B1 / 2.0;
                    section.CCD = section.B1 / 2.0;
                    section.Jc = CalculateJcInterior(input.C1, input.C2, d);
                    break;

                case ColumnLocation.Edge:
                    section.B1 = input.C1 + d / 2.0;
                    section.B2 = input.C2 + d;
                    section.Bo = 2 * section.B1 + section.B2;
                    var (cabEdge, ccdEdge) = CalculateCentroidEdge(section.B1, section.B2, d);
                    section.CAB = cabEdge;
                    section.CCD = ccdEdge;
                    section.Jc = CalculateJcEdge(input.C1, input.C2, d, section.CAB, section.CCD);
                    break;

                case ColumnLocation.Corner:
                    section.B1 = input.C1 + d / 2.0;
                    section.B2 = input.C2 + d / 2.0;
                    section.Bo = section.B1 + section.B2;
                    var (cabCorner, ccdCorner) = CalculateCentroidCorner(section.B1, section.B2, d);
                    section.CAB = cabCorner;
                    section.CCD = ccdCorner;
                    section.Jc = CalculateJcCorner(input.C1, input.C2, d, section.CAB, section.CCD);
                    break;
            }

            section.Ac = section.Bo * d;

            double sqrtRatio = Math.Sqrt(section.B1 / section.B2);
            section.GammaF = 1.0 / (1.0 + (2.0 / 3.0) * sqrtRatio);
            section.GammaV = 1.0 - section.GammaF;

            return section;
        }

        private static double CalculateJcInterior(double c1, double c2, double d)
        {
            double b1 = c1 + d;
            double b2 = c2 + d;
            
            double term1 = d * Math.Pow(b1, 3) / 6.0;
            double term2 = b1 * Math.Pow(d, 3) / 6.0;
            double term3 = d * b2 * Math.Pow(b1, 2) / 2.0;
            
            return term1 + term2 + term3;
        }

        private static (double cAB, double cCD) CalculateCentroidEdge(double b1, double b2, double d)
        {
            double areaSides = 2 * b1 * d;
            double areaFront = b2 * d;
            double totalArea = areaSides + areaFront;

            double momentSides = areaSides * (b1 / 2.0);
            //double momentFront = areaFront * b1;

            double cAB = momentSides / totalArea;
            double cCD = b1 - cAB;

            return (cAB, cCD);
        }

        private static double CalculateJcEdge(double c1, double c2, double d, double cAB, double cCD)
        {
            //double b1 = c1 + d / 2.0;
            //double b2 = c2 + d;

            //double I_sides = 2 * (d * Math.Pow(b1, 3) / 12.0 + b1 * d * Math.Pow(cCD - b1 / 2.0, 2));
            //double I_torsion = b1 * Math.Pow(d, 3) / 6.0;
            //double I_front = b2 * d * Math.Pow(cAB, 2);

            //ADPT
            double I_torsion = (c1 + d / 2.0) * Math.Pow(d, 3) / 6.0;
            double I_front = d * (c2 + d) * Math.Pow(cAB, 2);
            double I_sides = 2 * d * (Math.Pow(cAB, 3) + Math.Pow(cCD, 3)) / 3.0;

            return I_sides + I_torsion + I_front;
        }

        private static (double cAB, double cCD) CalculateCentroidCorner(double b1, double b2, double d)
        {
            // b1 = c1 + d/2
            // b2 = c2 + d/2
            // double area1 = b1 * d;
            // double area2 = b2 * d;
            // double totalArea = area1 + area2;

            //double moment1 = area1 * (b1 / 2.0);
            // double moment2 = area2 * b1;

            double cAB = b1 * b1 / 2.0 / (b1 + b2);
            double cCD = b1 - cAB;

            return (cAB, cCD);
        }

        private static double CalculateJcCorner(double c1, double c2, double d, double cAB, double cCD)
        {
            //double b1 = c1 + d / 2.0;
            //double b2 = c2 + d / 2.0;

            double J1 = (c1 + d / 2.0) * Math.Pow(d, 3) / 12.0;
            double J2 = d * (Math.Pow(cAB, 3) + Math.Pow(cCD, 3)) / 3.0;
            double J3 = d * (c2 + d / 2.0) * Math.Pow(cAB, 2);

            return J1 + J2 + J3;    
        }

        public static double CalculateSizeEffectFactor(double d)
        {
            double lambdaS = Math.Sqrt(2.0 / (1.0 + d / 10.0));
            return Math.Min(lambdaS, 1.0);
        }

        public static double CalculateVc(double fc, double lambda, double lambdaS, double beta, double alphaS, double bo, double d)
        {
            double sqrtFc = Math.Min(Math.Sqrt(fc), 100);

            double vc1 = 4 * lambdaS * lambda * sqrtFc;

            double vc2 = (2 + 4 / beta) * lambdaS * lambda * sqrtFc;

            double vc3 = (2 + alphaS * d / bo) * lambdaS * lambda * sqrtFc;

            return Math.Min(vc1, Math.Min(vc2, vc3));
        }

        public static double CalculateVcWithReinforcement(double fc, double lambda, double lambdaS, double beta, double alphaS, double bo, double d, ShearReinforcementType type)
        {
            double sqrtFc = Math.Min(Math.Sqrt(fc), 100);
            
            if (type == ShearReinforcementType.Stirrups)
            {
                return 2 * lambdaS * lambda * sqrtFc;
            }
            else if (type == ShearReinforcementType.HeadedStuds)
            {
                double vc1 = 3 * lambdaS * lambda * sqrtFc;
                double vc2 = (2 + 4 / beta) * lambdaS * lambda * sqrtFc;
                double vc3 = (2 + alphaS * d / bo) * lambdaS * lambda * sqrtFc;
                return Math.Min(vc1, Math.Min(vc2, vc3));
            }
            return 0;
        }

        public static double CalculateVuDemand(double vu, double msc, double gammaV, double cAB, double Jc, double Ac)
        {
            double vuStress = vu / Ac;
            double momentStress = gammaV * msc * cAB / Jc;
            return vuStress + momentStress;
        }

        public static double CalculateVmax(double fc, ShearReinforcementType type)
        {
            double sqrtFc = Math.Min(Math.Sqrt(fc), 100);
            if (type == ShearReinforcementType.HeadedStuds)
            {
                return 8 * sqrtFc;
            }
            else
            {
                return 6 * sqrtFc;
            }
        }

        public static double CalculateVs(double av, double fyt, double bo, double s)
        {
            return av * fyt / (bo * s);
        }

        public static DesignResult PerformDesign(InputParameters input)
        {
            var result = new DesignResult();
            var process = new List<string>();
            double phi = 0.75;

            process.Add("=== ACI 318-19 双向板抗冲切验算 ===");
            process.Add("");
            process.Add("【输入参数】");
            process.Add($"楼板厚度 h = {input.H:F2} in");
            process.Add($"保护层厚度 = {input.Cover:F2} in");
            process.Add($"钢筋直径 = {input.BarDiameter:F3} in");
            process.Add($"柱尺寸 c1 × c2 = {input.C1:F2} × {input.C2:F2} in");
            process.Add($"柱位置 = {input.Location}");
            process.Add($"设计剪力 Vu = {input.Vu / 1000:F2} kips");
            process.Add($"不平衡弯矩 Msc = {input.Msc / 12000:F2} kip-ft");
            process.Add($"混凝土强度 f'c = {input.Fc:F0} psi");
            process.Add($"轻质混凝土系数 λ = {input.Lambda:F2}");
            process.Add($"抗剪钢筋类型 = {input.ReinforcementType}");
            process.Add("");

            var section = CalculateCriticalSection(input);
            result.Section = section;

            process.Add("【步骤1: 计算临界截面属性】");
            process.Add($"有效高度 d = {section.D:F2} in");
            process.Add($"临界截面尺寸 b1 × b2 = {section.B1:F2} × {section.B2:F2} in");
            process.Add($"临界周长 bo = {section.Bo:F2} in");
            process.Add($"临界面积 Ac = {section.Ac:F2} in²");
            process.Add($"极惯性矩 Jc = {section.Jc:F0} in⁴");
            process.Add($"形心位置 cAB = {section.CAB:F2} in, cCD = {section.CCD:F2} in");
            process.Add($"γf = {section.GammaF:F4}, γv = {section.GammaV:F4}");
            process.Add("");

            double vuDemand = CalculateVuDemand(input.Vu, input.Msc, section.GammaV, section.CAB, section.Jc, section.Ac);
            result.Vu_demand = vuDemand;

            process.Add("【步骤2: 计算设计冲切剪应力需求】");
            process.Add($"vu = Vu/Ac + γv·Msc·cAB/Jc");
            process.Add($"vu = {input.Vu / 1000:F2}×1000/{section.Ac:F2} + {section.GammaV:F4}×{input.Msc / 12000:F2}×12000×{section.CAB:F2}/{section.Jc:F0}");
            process.Add($"vu = {vuDemand:F1} psi");
            process.Add("");

            double lambdaS = CalculateSizeEffectFactor(section.D);
            result.LambdaS = lambdaS;

            process.Add("【步骤3: 计算尺寸效应系数 λs】");
            process.Add($"λs = √(2/(1 + d/10)) = √(2/(1 + {section.D:F2}/10))");
            process.Add($"λs = {lambdaS:F4}");
            process.Add("");

            double beta = Math.Max(input.C1 / input.C2, input.C2 / input.C1);
            double alphaS = input.Location switch
            {
                ColumnLocation.Interior => 40,
                ColumnLocation.Edge => 30,
                ColumnLocation.Corner => 20,
                _ => 40
            };

            process.Add("【步骤4: 计算混凝土抗冲切承载力 vc】");
            process.Add($"柱截面长宽比 β = {beta:F2}");
            process.Add($"截面位置系数 αs = {alphaS}");
            process.Add("");

            double sqrtFc = Math.Min(Math.Sqrt(input.Fc), 100);
            double vc1 = 4 * lambdaS * input.Lambda * sqrtFc;
            double vc2 = (2 + 4 / beta) * lambdaS * input.Lambda * sqrtFc;
            double vc3 = (2 + alphaS * section.D / section.Bo) * lambdaS * input.Lambda * sqrtFc;

            process.Add($"公式 vc = 4·λs·λ·√f'c = {vc1:F1} psi");
            process.Add($"公式 = (2 + 4/β)·λs·λ·√f'c = {vc2:F1} psi");
            process.Add($"公式 = (2 + αs·d/bo)·λs·λ·√f'c = {vc3:F1} psi");

            double vc;
            if (input.ReinforcementType == ShearReinforcementType.None)
            {
                vc = Math.Min(vc1, Math.Min(vc2, vc3));
                process.Add($"取最小值 vc = {vc:F1} psi");
            }
            else
            {
                vc = CalculateVcWithReinforcement(input.Fc, input.Lambda, lambdaS, beta, alphaS, section.Bo, section.D, input.ReinforcementType);
                if (input.ReinforcementType == ShearReinforcementType.Stirrups)
                {
                    process.Add($"配置箍筋后 vc = 2·λs·λ·√f'c = {vc:F1} psi");
                }
                else
                {
                    double vcStud1 = 3 * lambdaS * input.Lambda * sqrtFc;
                    double vcStud2 = (2 + 4 / beta) * lambdaS * input.Lambda * sqrtFc;
                    double vcStud3 = (2 + alphaS * section.D / section.Bo) * lambdaS * input.Lambda * sqrtFc;
                    process.Add($"配置抗剪栓钉后 vc 取以下最小值:");
                    process.Add($"  (b) 3·λs·λ·√f'c = {vcStud1:F1} psi");
                    process.Add($"  (c) (2 + 4/β)·λs·λ·√f'c = {vcStud2:F1} psi");
                    process.Add($"  (d) (2 + αs·d/bo)·λs·λ·√f'c = {vcStud3:F1} psi");
                    process.Add($"取最小值 vc = {vc:F1} psi");
                }
            }

            result.Vc_concrete = vc;
            result.PhiVc = phi * vc;

            process.Add("");
            process.Add($"设计承载力 φvc = {phi} × {vc:F1} = {phi * vc:F1} psi");
            process.Add("");

            double vMax = CalculateVmax(input.Fc, input.ReinforcementType);
            result.Vmax = vMax;

            process.Add("【步骤5: 截面尺寸验算】");
            if (input.ReinforcementType == ShearReinforcementType.HeadedStuds)
            {
                process.Add($"采用抗剪螺柱，最大允许应力 = φ·8·√f'c = {phi}×8×{sqrtFc:F1} = {phi * vMax:F1} psi");
            }
            else if (input.ReinforcementType == ShearReinforcementType.Stirrups)
            {
                process.Add($"采用箍筋，最大允许应力 = φ·6·√f'c = {phi}×6×{sqrtFc:F1} = {phi * vMax:F1} psi");
            }
            else
            {
                process.Add($"无抗剪钢筋，无需验算最大应力上限");
            }

            if (vuDemand > phi * vMax)
            {
                process.Add($"");
                process.Add($"✗ vu = {vuDemand:F1} psi > φ·vmax = {phi * vMax:F1} psi");
                process.Add($"截面尺寸不足！需要增加板厚或柱尺寸");
                result.IsSectionAdequate = false;
                result.NeedsReinforcement = true;
                result.ResultSummary = "截面尺寸不足，需要增加板厚或柱尺寸";
                result.DesignProcess = process;
                return result;
            }
            else
            {
                process.Add($"✓ vu = {vuDemand:F1} psi ≤ φ·vmax = {phi * vMax:F1} psi");
                result.IsSectionAdequate = true;
            }
            process.Add("");

            process.Add("【步骤6: 抗冲切验算】");
            if (vuDemand <= phi * vc)
            {
                process.Add($"vu = {vuDemand:F1} psi ≤ φvc = {phi * vc:F1} psi");
                process.Add($"✓ 混凝土自身抗冲切能力足够，无需配置抗冲切钢筋");
                result.NeedsReinforcement = false;
                result.ResultSummary = "满足要求，无需配置抗冲切钢筋";
            }
            else
            {
                process.Add($"vu = {vuDemand:F1} psi > φvc = {phi * vc:F1} psi");
                process.Add($"需要配置抗冲切钢筋");
                result.NeedsReinforcement = true;

                if (input.ReinforcementType == ShearReinforcementType.None)
                {
                    process.Add($"");
                    process.Add($"当前未配置抗剪钢筋，建议配置抗剪螺柱或箍筋");
                    result.ResultSummary = "需要配置抗冲切钢筋";
                }
                else
                {
                    process.Add("");
                    process.Add("【步骤7: 构造要求与尺寸效应豁免检查】");
                    
                    bool detailCheckPassed = true;
                    double av = input.StudArea * input.StudsPerPerimeter;
                    double avPerS = av / input.StudSpacing;
                    double avMinPerS = 2 * sqrtFc * section.Bo / input.Fyt;
                    
                    bool isLambdaSWaived = (avPerS >= avMinPerS);

                    if (input.ReinforcementType == ShearReinforcementType.Stirrups)
                    {
                        process.Add("箍筋构造要求 (ACI 318-19 22.6.7.1):");
                        process.Add($"  d = {section.D:F2} in ≥ 6 in? {(section.D >= 6 ? "✓" : "✗")}");
                        
                        double stirrupDb = Math.Sqrt(input.StudArea * 4 / Math.PI);
                        double sixteenDb = 16 * stirrupDb;
                        process.Add($"  d = {section.D:F2} in ≥ 16×db = {sixteenDb:F2} in? {(section.D >= sixteenDb ? "✓" : "✗")}");
                        
                        if (section.D < 6 || section.D < sixteenDb)
                        {
                            process.Add("✗ 箍筋构造要求不满足，建议改用抗剪栓钉或增加板厚");
                            detailCheckPassed = false;
                        }
                        process.Add("");
                        
                        process.Add($"箍筋配筋率检查 (用于判断是否可豁免尺寸效应):");
                        process.Add($"  实际 Av/s = {avPerS:F4} in²/in");
                        process.Add($"  下限 2√f'c·bo/fyt = {avMinPerS:F4} in²/in");
                        if (isLambdaSWaived)
                        {
                            process.Add($"  ✓ Av/s ≥ 下限，可触发尺寸效应豁免");
                        }
                        else
                        {
                            process.Add($"  ○ Av/s < 下限，无法触发尺寸效应豁免（箍筋不强制要求最小配筋率）");
                        }
                        process.Add("");
                    }
                    else if (input.ReinforcementType == ShearReinforcementType.HeadedStuds)
                    {
                        process.Add($"栓钉强制最小配筋率检查 (ACI 318-19 22.6.8.3):");
                        process.Add($"  实际 Av/s = {avPerS:F4} in²/in");
                        process.Add($"  下限 2√f'c·bo/fyt = {avMinPerS:F4} in²/in");
                        
                        if (isLambdaSWaived)
                        {
                            process.Add($"  ✓ Av/s 满足下限要求");
                        }
                        else
                        {
                            process.Add($"  ✗ 错误：抗剪栓钉必须满足最小配筋率下限！");
                            detailCheckPassed = false;
                        }
                        process.Add("");
                    }

                    double finalLambdaS = lambdaS;
                    if (isLambdaSWaived && lambdaS < 1.0)
                    {
                        finalLambdaS = 1.0;
                        process.Add($"✓ 满足 ACI 318-19 (22.6.6.2) 充足配筋条件，尺寸效应系数 λs 恢复为 1.0！");
                        process.Add("");
                    }
                    
                    double finalVc = CalculateVcWithReinforcement(input.Fc, input.Lambda, finalLambdaS, beta, alphaS, section.Bo, section.D, input.ReinforcementType);
                    double phiFinalVc = phi * finalVc;
                    
                    process.Add($"配筋后的混凝土贡献 (λs={finalLambdaS:F4}):");
                    if (input.ReinforcementType == ShearReinforcementType.Stirrups)
                    {
                        process.Add($"  vc = 2·λs·λ·√f'c = {finalVc:F1} psi");
                    }
                    else
                    {
                        double vcStud1 = 3 * finalLambdaS * input.Lambda * sqrtFc;
                        double vcStud2 = (2 + 4 / beta) * finalLambdaS * input.Lambda * sqrtFc;
                        double vcStud3 = (2 + alphaS * section.D / section.Bo) * finalLambdaS * input.Lambda * sqrtFc;
                        process.Add($"  vc 取以下最小值:");
                        process.Add($"    (b) 3·λs·λ·√f'c = {vcStud1:F1} psi");
                        process.Add($"    (c) (2 + 4/β)·λs·λ·√f'c = {vcStud2:F1} psi");
                        process.Add($"    (d) (2 + αs·d/bo)·λs·λ·√f'c = {vcStud3:F1} psi");
                        process.Add($"  vc = {finalVc:F1} psi");
                    }
                    process.Add($"  φvc = {phiFinalVc:F1} psi");
                    process.Add("");

                    process.Add("【步骤8: 计算与验算抗剪钢筋】");
                    
                    double vsRequired = Math.Max((vuDemand / phi) - finalVc, 0);
                    result.Vs_required = vsRequired;
                    process.Add($"所需钢筋应力 vs_req = vu/φ - vc = {vuDemand:F1}/{phi} - {finalVc:F1} = {vsRequired:F1} psi");

                    double sMax;
                    if (input.ReinforcementType == ShearReinforcementType.Stirrups)
                    {
                        sMax = 0.5 * section.D;
                        process.Add($"箍筋最大间距检查 (ACI 318-19 Table 8.7.6.3):");
                        process.Add($"  s_max = d/2 = {sMax:F2} in");
                    }
                    else
                    {
                        double phi6SqrtFc = phi * 6 * sqrtFc;
                        if (vuDemand <= phi6SqrtFc)
                        {
                            sMax = 0.75 * section.D;
                            process.Add($"栓钉最大间距检查 (ACI 318-19 Table 8.7.7.1.2):");
                            process.Add($"  vu = {vuDemand:F1} psi ≤ φ·6√f'c = {phi6SqrtFc:F1} psi");
                            process.Add($"  s_max = 3d/4 = {sMax:F2} in");
                        }
                        else
                        {
                            sMax = 0.5 * section.D;
                            process.Add($"栓钉最大间距检查 (ACI 318-19 Table 8.7.7.1.2):");
                            process.Add($"  vu = {vuDemand:F1} psi > φ·6√f'c = {phi6SqrtFc:F1} psi");
                            process.Add($"  s_max = d/2 = {sMax:F2} in");
                        }
                    }
                    
                    bool spacingOK = input.StudSpacing <= sMax;
                    if (spacingOK)
                    {
                        process.Add($"  ✓ s = {input.StudSpacing:F2} in ≤ s_max = {sMax:F2} in");
                    }
                    else
                    {
                        process.Add($"  ✗ s = {input.StudSpacing:F2} in > s_max = {sMax:F2} in，间距过大");
                        detailCheckPassed = false;
                    }
                    process.Add("");

                    process.Add($"抗剪钢筋配置:");
                    process.Add($"  单根面积 = {input.StudArea:F3} in²");
                    process.Add($"  每周根数 = {input.StudsPerPerimeter}");
                    process.Add($"  总面积 Av = {av:F3} in²");
                    process.Add($"  间距 s = {input.StudSpacing:F2} in");

                    double vsProvided = CalculateVs(av, input.Fyt, section.Bo, input.StudSpacing);
                    result.Vs_provided = vsProvided;

                    process.Add($"");
                    process.Add($"提供钢筋应力 vs_prov = Av·fyt/(bo·s) = {av:F3}×{input.Fyt:F0}/({section.Bo:F2}×{input.StudSpacing:F2})");
                    process.Add($"vs_prov = {vsProvided:F1} psi");

                    double vn = finalVc + vsProvided;
                    double phiVn = phi * vn;
                    result.PhiVn = phiVn;

                    process.Add($"");
                    process.Add($"总名义承载力 vn = vc + vs = {finalVc:F1} + {vsProvided:F1} = {vn:F1} psi");
                    process.Add($"设计承载力 φvn = {phi} × {vn:F1} = {phiVn:F1} psi");
                    process.Add($"");

                    if (!detailCheckPassed)
                    {
                        process.Add($"✗ 构造要求不满足，请调整配筋配置");
                        result.ResultSummary = "构造要求不满足，请调整配筋";
                    }
                    else if (phiVn >= vuDemand)
                    {
                        process.Add($"✓ φvn = {phiVn:F1} psi ≥ vu = {vuDemand:F1} psi");
                        process.Add($"抗冲切验算通过！");
                        result.ResultSummary = $"满足要求，配置抗冲切钢筋有效";
                        result.ReinforcementResult = $"Av = {av:F3} in², s = {input.StudSpacing:F1} in";
                    }
                    else
                    {
                        process.Add($"✗ φvn = {phiVn:F1} psi < vu = {vuDemand:F1} psi");
                        process.Add($"承载力不足，需要增加抗剪钢筋面积或减小间距");
                        result.ResultSummary = "抗剪钢筋配置不足，需要增加";
                    }
                }
            }

            result.DesignProcess = process;
            return result;
        }

        public static double CalculateRequiredStudSpacing(double vsRequired, double av, double fyt, double bo)
        {
            if (vsRequired <= 0) return double.MaxValue;
            return av * fyt / (bo * vsRequired);
        }

        public static (double avRequired, double sRequired, double sMax) CalculateRequiredReinforcement(
            double vuDemand, double vc, double phi, double bo, double d, double fyt, double sqrtFc,
            ShearReinforcementType type, double studArea, int studsPerPerimeter)
        {
            double vsRequired = vuDemand / phi - vc;
            
            if (vsRequired <= 0)
            {
                return (0, double.MaxValue, 0);
            }

            double av = studArea * studsPerPerimeter;
            double sRequired = av * fyt / (bo * vsRequired);

            double sMax;
            if (type == ShearReinforcementType.Stirrups)
            {
                sMax = 0.5 * d;
            }
            else if (type == ShearReinforcementType.HeadedStuds)
            {
                double phi6SqrtFc = phi * 6 * sqrtFc;
                if (vuDemand <= phi6SqrtFc)
                {
                    sMax = 0.75 * d;
                }
                else
                {
                    sMax = 0.5 * d;
                }
            }
            else
            {
                sMax = double.MaxValue;
            }

            return (av, sRequired, sMax);
        }
    }
}
