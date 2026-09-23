using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class PunchingShearViewModel : INotifyPropertyChanged
    {
        private double _h = 8;
        private double _cover = 0.75;
        private string _selectedBarSize = "#5";
        private double _c1 = 18;
        private double _c2 = 18;
        private string _columnLocation = "中柱";
        private double _vu = 80;
        private double _msc = 120;
        private double _fc = 4000;
        private double _lambda = 1.0;
        private double _fyt = 60000;
        private double _studFyt = 51000;
        private string _reinforcementType = "无抗剪钢筋";
        private string _studSize = "#4";
        private int _studsPerPerimeter = 8;
        private double _studSpacing = 3;

        private string _resultSummary = "";
        private string _reinforcementResult = "";
        private string _designProcess = "";
        private string _sectionProperties = "";
        private string _demandVsCapacity = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public PunchingShearViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region 属性

        public double H
        {
            get => _h;
            set { _h = value; OnPropertyChanged(); }
        }

        public double Cover
        {
            get => _cover;
            set { _cover = value; OnPropertyChanged(); }
        }

        public string SelectedBarSize
        {
            get => _selectedBarSize;
            set { _selectedBarSize = value; OnPropertyChanged(); }
        }

        public double C1
        {
            get => _c1;
            set { _c1 = value; OnPropertyChanged(); }
        }

        public double C2
        {
            get => _c2;
            set { _c2 = value; OnPropertyChanged(); }
        }

        public string ColumnLocation
        {
            get => _columnLocation;
            set { _columnLocation = value; OnPropertyChanged(); }
        }

        public double Vu
        {
            get => _vu;
            set { _vu = value; OnPropertyChanged(); }
        }

        public double Msc
        {
            get => _msc;
            set { _msc = value; OnPropertyChanged(); }
        }

        public double Fc
        {
            get => _fc;
            set { _fc = value; OnPropertyChanged(); }
        }

        public double Lambda
        {
            get => _lambda;
            set { _lambda = value; OnPropertyChanged(); }
        }

        public double Fyt
        {
            get => _fyt;
            set { _fyt = value; OnPropertyChanged(); }
        }

        public double StudFyt
        {
            get => _studFyt;
            set { _studFyt = value; OnPropertyChanged(); }
        }

        public string ReinforcementType
        {
            get => _reinforcementType;
            set 
            { 
                _reinforcementType = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowReinforcementInputs));
            }
        }

        public string StudSize
        {
            get => _studSize;
            set { _studSize = value; OnPropertyChanged(); }
        }

        public int StudsPerPerimeter
        {
            get => _studsPerPerimeter;
            set { _studsPerPerimeter = value; OnPropertyChanged(); }
        }

        public double StudSpacing
        {
            get => _studSpacing;
            set { _studSpacing = value; OnPropertyChanged(); }
        }

        public bool ShowReinforcementInputs => ReinforcementType != "无抗剪钢筋";

        public string ResultSummary
        {
            get => _resultSummary;
            set { _resultSummary = value; OnPropertyChanged(); }
        }

        public string ReinforcementResult
        {
            get => _reinforcementResult;
            set { _reinforcementResult = value; OnPropertyChanged(); }
        }

        public string DesignProcess
        {
            get => _designProcess;
            set { _designProcess = value; OnPropertyChanged(); }
        }

        public string SectionProperties
        {
            get => _sectionProperties;
            set { _sectionProperties = value; OnPropertyChanged(); }
        }

        public string DemandVsCapacity
        {
            get => _demandVsCapacity;
            set { _demandVsCapacity = value; OnPropertyChanged(); }
        }

        #endregion

        #region 选项列表

        public List<string> BarSizeOptions { get; } = new()
        {
            "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10", "#11"
        };

        public List<string> ColumnLocationOptions { get; } = new()
        {
            "中柱", "边柱", "角柱"
        };

        public List<string> ReinforcementTypeOptions { get; } = new()
        {
            "无抗剪钢筋", "抗剪栓钉", "箍筋"
        };

        public List<string> StudSizeOptions { get; } = new()
        {
            "#3", "#4", "#5", "#6", "#7", "#8"
        };

        public List<int> StudsPerPerimeterOptions { get; } = new()
        {
            2, 4, 6, 8, 10, 12, 14, 16
        };

        #endregion

        public ICommand CalculateCommand { get; }

        private void Calculate()
        {
            try
            {
                var input = new PunchingShearCalculations.InputParameters
                {
                    H = H,
                    Cover = Cover,
                    BarDiameter = PunchingShearCalculations.GetBarDiameter(SelectedBarSize),
                    C1 = C1,
                    C2 = C2,
                    Location = ColumnLocation switch
                    {
                        "中柱" => PunchingShearCalculations.ColumnLocation.Interior,
                        "边柱" => PunchingShearCalculations.ColumnLocation.Edge,
                        "角柱" => PunchingShearCalculations.ColumnLocation.Corner,
                        _ => PunchingShearCalculations.ColumnLocation.Interior
                    },
                    Vu = Vu * 1000,
                    Msc = Msc * 12000,
                    Fc = Fc,
                    Lambda = Lambda,
                    Fyt = ReinforcementType == "抗剪栓钉" ? StudFyt : Fyt,
                    ReinforcementType = ReinforcementType switch
                    {
                        "抗剪栓钉" => PunchingShearCalculations.ShearReinforcementType.HeadedStuds,
                        "箍筋" => PunchingShearCalculations.ShearReinforcementType.Stirrups,
                        _ => PunchingShearCalculations.ShearReinforcementType.None
                    },
                    StudArea = PunchingShearCalculations.GetBarArea(StudSize),
                    StudsPerPerimeter = StudsPerPerimeter,
                    StudSpacing = StudSpacing
                };

                var result = PunchingShearCalculations.PerformDesign(input);

                ResultSummary = result.ResultSummary ?? "";
                ReinforcementResult = result.ReinforcementResult ?? "";
                DesignProcess = string.Join("\n", result.DesignProcess);

                var section = result.Section;
                if (section != null)
                {
                    SectionProperties = $"d = {section.D:F2} in\n" +
                                       $"b₁ = {section.B1:F2} in, b₂ = {section.B2:F2} in\n" +
                                       $"bₒ = {section.Bo:F2} in\n" +
                                       $"Aᶜ = {section.Ac:F2} in²\n" +
                                       $"Jᶜ = {section.Jc:F0} in⁴\n" +
                                       $"γᵥ = {section.GammaV:F4}";
                }
                else
                {
                    SectionProperties = "临界截面属性计算失败";
                }

                DemandVsCapacity = $"vᵤ = {result.Vu_demand:F1} psi\n" +
                                  $"φvᶜ = {result.PhiVc:F1} psi\n" +
                                  $"λₛ = {result.LambdaS:F4}";

                if (result.NeedsReinforcement && result.Vs_provided > 0)
                {
                    DemandVsCapacity += $"\nvₛ = {result.Vs_provided:F1} psi\nφvₙ = {result.PhiVn:F1} psi";
                }
            }
            catch (Exception ex)
            {
                DesignProcess = $"计算错误: {ex.Message}\n\n{ex.StackTrace}";
            }
        }
    }
}
