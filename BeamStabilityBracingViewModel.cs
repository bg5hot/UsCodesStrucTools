using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace SpectrumComparison
{
    public class BeamStabilityBracingViewModel : INotifyPropertyChanged
    {
        private readonly List<ShapeData> _allShapes = new();

        private class ShapeData
        {
            public string Name = "";
            public double d, bf, tw, tf, Zx, Ag;

            public double Ho => d - tf;
            public double IyApprox => 2.0 * tf * Math.Pow(bf, 3) / 12.0
                                      + Math.Max(0, d - 2 * tf) * Math.Pow(tw, 3) / 12.0;
            public double RyApprox => Ag > 0 ? Math.Sqrt(IyApprox / Ag) : 0;
        }

        private int _systemIndex;
        private int _memberIndex;
        private int _methodIndex;
        private int _sectionSourceIndex;
        private int _shapeIndex = -1;

        private double _zx, _ryRadius, _ho, _iy, _tw;
        private double _fy = 50, _e = 29000, _ry = 1.1;
        private bool _autoRy = true;

        private double _lb;

        private int _braceTypeIndex;
        private bool _atPlasticHinge;

        private bool _useSeismicMr = true;
        private double _mr;
        private double _cd = 1.0;

        private double _iyEff, _cb = 1.0, _lSpan;
        private int _nBraces = 1;
        private double _tst, _bs;
        private bool _fullDepthStiffener;

        private string _processText = "";
        private bool _hasResult;
        private string _lbMaxText = "";
        private string _strengthText = "";
        private string _stiffnessText = "";
        private string _ductilityText = "";
        private string _braceDescText = "";
        private string _statusText = "";
        private Brush _statusForeground = Brushes.Green;

        public ObservableCollection<string> SystemOptions { get; } = new();
        public ObservableCollection<string> MemberOptions { get; } = new();
        public ObservableCollection<string> MethodOptions { get; } = new() { "LRFD", "ASD" };
        public ObservableCollection<string> SectionSourceOptions { get; } = new() { "AISC Database", "Direct Input" };
        public ObservableCollection<string> ShapeOptions { get; } = new();
        public ObservableCollection<string> BraceTypeOptions { get; } = new()
        {
            "Nodal Lateral (侧向节点)",
            "Relative Lateral (侧向相对)",
            "Torsional (抗扭)"
        };

        public ICommand CalculateCommand { get; }

        public BeamStabilityBracingViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            foreach (var s in BeamStabilityBracingCalculations.GetSystemOptions())
                SystemOptions.Add(s);
            UpdateMemberOptions();
            LoadShapes();
        }

        public int SystemIndex
        {
            get => _systemIndex;
            set { if (_systemIndex == value) return; _systemIndex = value; OnPropertyChanged(); UpdateMemberOptions(); UpdateRy(); }
        }

        public int MemberIndex
        {
            get => _memberIndex;
            set { if (_memberIndex == value) return; _memberIndex = value; OnPropertyChanged(); }
        }

        public int MethodIndex
        {
            get => _methodIndex;
            set { if (_methodIndex == value) return; _methodIndex = value; OnPropertyChanged(); UpdateMr(); }
        }

        public int SectionSourceIndex
        {
            get => _sectionSourceIndex;
            set
            {
                if (_sectionSourceIndex == value) return;
                _sectionSourceIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowDatabaseControls));
                if (value == 0 && _shapeIndex >= 0) FillFromShape();
            }
        }

        public int ShapeIndex
        {
            get => _shapeIndex;
            set { if (_shapeIndex == value) return; _shapeIndex = value; OnPropertyChanged(); if (value >= 0) FillFromShape(); }
        }

        public int BraceTypeIndex
        {
            get => _braceTypeIndex;
            set { if (_braceTypeIndex == value) return; _braceTypeIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowTorsionalParams)); }
        }

        public bool AtPlasticHinge
        {
            get => _atPlasticHinge;
            set { if (_atPlasticHinge == value) return; _atPlasticHinge = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowMrControls)); OnPropertyChanged(nameof(ShowCdControl)); UpdateMr(); }
        }

        public bool UseSeismicMr
        {
            get => _useSeismicMr;
            set { if (_useSeismicMr == value) return; _useSeismicMr = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsMrReadOnly)); UpdateMr(); }
        }

        public bool FullDepthStiffener
        {
            get => _fullDepthStiffener;
            set { if (_fullDepthStiffener == value) return; _fullDepthStiffener = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowStiffenerDimensions)); }
        }

        public bool AutoRy
        {
            get => _autoRy;
            set { if (_autoRy == value) return; _autoRy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsRyReadOnly)); if (value) UpdateRy(); }
        }

        public double Zx { get => _zx; set { _zx = value; OnPropertyChanged(); UpdateMr(); } }
        public double RyRadius { get => _ryRadius; set { _ryRadius = value; OnPropertyChanged(); } }
        public double Ho { get => _ho; set { _ho = value; OnPropertyChanged(); } }
        public double Iy { get => _iy; set { _iy = value; OnPropertyChanged(); } }
        public double Tw { get => _tw; set { _tw = value; OnPropertyChanged(); } }

        public double Fy { get => _fy; set { _fy = value; OnPropertyChanged(); if (AutoRy) UpdateRy(); UpdateMr(); } }
        public double E { get => _e; set { _e = value; OnPropertyChanged(); } }
        public double Ry { get => _ry; set { _ry = value; OnPropertyChanged(); UpdateMr(); } }

        public double Lb { get => _lb; set { _lb = value; OnPropertyChanged(); } }

        public double Mr { get => _mr; set { _mr = value; OnPropertyChanged(); } }
        public double Cd { get => _cd; set { _cd = value; OnPropertyChanged(); } }

        public double IyEff { get => _iyEff; set { _iyEff = value; OnPropertyChanged(); } }
        public double Cb { get => _cb; set { _cb = value; OnPropertyChanged(); } }
        public double LSpan { get => _lSpan; set { _lSpan = value; OnPropertyChanged(); } }
        public int NBraces { get => _nBraces; set { _nBraces = value; OnPropertyChanged(); } }
        public double Tst { get => _tst; set { _tst = value; OnPropertyChanged(); } }
        public double Bs { get => _bs; set { _bs = value; OnPropertyChanged(); } }

        public bool ShowDatabaseControls => SectionSourceIndex == 0;
        public bool ShowTorsionalParams => BraceTypeIndex == 2;
        public bool ShowMrControls => !AtPlasticHinge;
        public bool ShowCdControl => !AtPlasticHinge;
        public bool IsMrReadOnly => UseSeismicMr;
        public bool IsRyReadOnly => AutoRy;
        public bool ShowStiffenerDimensions => !FullDepthStiffener;

        public string ProcessText { get => _processText; set { _processText = value; OnPropertyChanged(); } }
        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }
        public string LbMaxText { get => _lbMaxText; set { _lbMaxText = value; OnPropertyChanged(); } }
        public string StrengthText { get => _strengthText; set { _strengthText = value; OnPropertyChanged(); } }
        public string StiffnessText { get => _stiffnessText; set { _stiffnessText = value; OnPropertyChanged(); } }
        public string DuctilityText { get => _ductilityText; set { _ductilityText = value; OnPropertyChanged(); } }
        public string BraceDescText { get => _braceDescText; set { _braceDescText = value; OnPropertyChanged(); } }
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
        public Brush StatusForeground { get => _statusForeground; set { _statusForeground = value; OnPropertyChanged(); } }

        private void LoadShapes()
        {
            try
            {
                var shapes = Aisc358ShapeHelper.LoadWShapes();
                _allShapes.Clear();
                foreach (var s in shapes)
                    _allShapes.Add(new ShapeData { Name = s.Name, d = s.d, bf = s.bf, tw = s.tw, tf = s.tf, Zx = s.Zx, Ag = s.Ag });
                UpdateShapeOptions();
            }
            catch { }
        }

        private void UpdateShapeOptions()
        {
            ShapeOptions.Clear();
            foreach (var s in _allShapes)
                ShapeOptions.Add(s.Name);
            if (ShapeOptions.Count > 0) ShapeIndex = 0;
        }

        private void FillFromShape()
        {
            if (ShapeIndex < 0 || ShapeIndex >= _allShapes.Count) return;
            var s = _allShapes[ShapeIndex];
            Zx = Math.Round(s.Zx, 3);
            RyRadius = Math.Round(s.RyApprox, 4);
            Ho = Math.Round(s.Ho, 3);
            Iy = Math.Round(s.IyApprox, 3);
            Tw = Math.Round(s.tw, 4);
            IyEff = Math.Round(s.IyApprox, 3);
        }

        private void UpdateMemberOptions()
        {
            MemberOptions.Clear();
            var system = (BeamStabilityBracingCalculations.SeismicSystem)SystemIndex;
            bool hasLink = BeamStabilityBracingCalculations.SystemHasLink(system);
            foreach (var m in BeamStabilityBracingCalculations.GetMemberOptions(hasLink))
                MemberOptions.Add(m);
            MemberIndex = 0;
            OnPropertyChanged(nameof(MemberOptions));
        }

        private void UpdateRy()
        {
            if (!AutoRy) return;
            Ry = BeamStabilityBracingCalculations.GetDefaultRy(Fy);
        }

        private void UpdateMr()
        {
            if (AtPlasticHinge || UseSeismicMr)
            {
                double alphaS = MethodIndex == 0 ? 1.0 : 1.5;
                Mr = Math.Round(Ry * Fy * Zx / alphaS, 2);
            }
        }

        private void Calculate()
        {
            try
            {
                var system = (BeamStabilityBracingCalculations.SeismicSystem)SystemIndex;
                var member = (BeamStabilityBracingCalculations.MemberRole)MemberIndex;
                var method = (BeamStabilityBracingCalculations.DesignMethod)MethodIndex;
                var braceType = (BeamStabilityBracingCalculations.BraceType)BraceTypeIndex;

                var input = new BeamStabilityBracingCalculations.InputParameters
                {
                    System = system,
                    Member = member,
                    Method = method,
                    Brace = braceType,
                    Zx = Zx, RyRadius = RyRadius, Ho = Ho, Iy = Iy, Tw = Tw,
                    Fy = Fy, E = E, Ry = Ry, Lb = Lb,
                    AtPlasticHinge = AtPlasticHinge,
                    UseSeismicMr = UseSeismicMr, Mr = Mr, Cd = Cd,
                    IyEff = IyEff, Cb = Cb, LSpan = LSpan, NBraces = NBraces,
                    Tst = Tst, Bs = Bs, FullDepthStiffener = FullDepthStiffener
                };

                var result = BeamStabilityBracingCalculations.Calculate(input);

                ProcessText = string.Join("\n", result.Process);
                HasResult = result.IsValid;

                if (result.IsValid)
                {
                    DuctilityText = result.Ductility == BeamStabilityBracingCalculations.DuctilityLevel.HighlyDuctile
                        ? "Highly Ductile (高延性)" : "Moderately Ductile (中等延性)";
                    LbMaxText = $"{result.LbMax:F2} in ({result.LbMax / 12.0:F2} ft)";

                    if (braceType == BeamStabilityBracingCalculations.BraceType.Torsional)
                    {
                        BraceDescText = "Torsional (抗扭)";
                        StrengthText = $"{result.RequiredStrength:F2} kip-in";
                        StiffnessText = double.IsInfinity(result.RequiredStiffness)
                            ? "N/A (βT≥βsec)" : $"{result.RequiredStiffness:E4} kip-in/rad";
                    }
                    else
                    {
                        BraceDescText = braceType == BeamStabilityBracingCalculations.BraceType.LateralNodal
                            ? "Nodal Lateral (节点侧向)" : "Relative Lateral (相对侧向)";
                        StrengthText = $"{result.RequiredStrength:F2} kips";
                        StiffnessText = $"{result.RequiredStiffness:F2} kips/in";
                    }

                    StatusText = result.LbCheckPass
                        ? $"PASS — Lb = {Lb:F1} in ≤ Lb,max = {result.LbMax:F1} in"
                        : $"FAIL — Lb = {Lb:F1} in > Lb,max = {result.LbMax:F1} in";
                    StatusForeground = result.LbCheckPass ? Brushes.Green : Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                ProcessText = $"Error: {ex.Message}";
                HasResult = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
