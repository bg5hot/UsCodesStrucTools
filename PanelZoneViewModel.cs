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
    public class PanelZoneViewModel : INotifyPropertyChanged
    {
        private readonly List<ShapeData> _allShapes = new();

        private class ShapeData
        {
            public string Name = "";
            public double d, bf, tw, tf, Zx, Ag;
        }

        private int _systemIndex;
        private int _methodIndex;
        private int _columnSourceIndex;
        private int _selectedColumnShapeIndex = -1;
        private int _beamSourceIndex;
        private int _selectedBeamLeftShapeIndex = -1;

        // Column
        private double _dc, _bcf, _tcf, _tcw, _fyc = 50, _agCol;

        // Beam Left
        private double _dbLeft, _tfbLeft, _zxLeft;
        private double _fybLeft = 50, _ryLeft = 1.1, _cprLeft = 1.15;
        private bool _autoRyLeft = true;
        private double _mprLeft;

        // Beam Right
        private bool _hasRightBeam = true;
        private bool _sameBeamRight = true;
        private double _dbRight, _tfbRight, _zxRight;
        private double _fybRight = 50, _ryRight = 1.1, _cprRight = 1.15;
        private bool _autoRyRight = true;
        private double _mprRight;

        // Joint
        private double _hc, _pr;

        // Results
        private string _processText = "";
        private bool _hasResult;
        private string _demandText = "";
        private string _capacityText = "";
        private string _strengthRatioText = "";
        private string _thicknessText = "";
        private string _doublerText = "";
        private string _plugWeldText = "";
        private string _statusText = "";
        private Brush _statusForeground = Brushes.Green;

        public ObservableCollection<string> SystemOptions { get; } = new();
        public ObservableCollection<string> MethodOptions { get; } = new() { "LRFD", "ASD" };
        public ObservableCollection<string> SourceOptions { get; } = new() { "AISC Database", "Direct Input" };
        public ObservableCollection<string> ColumnShapeOptions { get; } = new();
        public ObservableCollection<string> BeamShapeOptions { get; } = new();

        public ICommand CalculateCommand { get; }

        public PanelZoneViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            foreach (var s in PanelZoneCalculations.GetSystemOptions())
                SystemOptions.Add(s);
            LoadShapes();
        }

        // ── System & Method ──

        public int SystemIndex
        {
            get => _systemIndex;
            set { if (_systemIndex == value) return; _systemIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(UseCapacityDesign)); OnPropertyChanged(nameof(ShowCapacityInputs)); OnPropertyChanged(nameof(ShowUserMoment)); UpdateMprLeft(); UpdateMprRight(); }
        }

        public int MethodIndex
        {
            get => _methodIndex;
            set { if (_methodIndex == value) return; _methodIndex = value; OnPropertyChanged(); }
        }

        // ── Column ──

        public int ColumnSourceIndex
        {
            get => _columnSourceIndex;
            set
            {
                if (_columnSourceIndex == value) return;
                _columnSourceIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsColumnDatabase));
                OnPropertyChanged(nameof(IsColumnDirect));
                if (value == 0 && _selectedColumnShapeIndex >= 0) FillFromColumnShape();
            }
        }

        public int SelectedColumnShapeIndex
        {
            get => _selectedColumnShapeIndex;
            set { if (_selectedColumnShapeIndex == value) return; _selectedColumnShapeIndex = value; OnPropertyChanged(); if (value >= 0) FillFromColumnShape(); }
        }

        public double Dc { get => _dc; set { _dc = value; OnPropertyChanged(); } }
        public double Bcf { get => _bcf; set { _bcf = value; OnPropertyChanged(); } }
        public double Tcf { get => _tcf; set { _tcf = value; OnPropertyChanged(); } }
        public double Tcw { get => _tcw; set { _tcw = value; OnPropertyChanged(); } }
        public double Fyc { get => _fyc; set { _fyc = value; OnPropertyChanged(); } }
        public double AgCol { get => _agCol; set { _agCol = value; OnPropertyChanged(); } }

        public bool IsColumnDatabase => ColumnSourceIndex == 0;
        public bool IsColumnDirect => ColumnSourceIndex == 1;

        // ── Beam Left ──

        public int BeamSourceIndex
        {
            get => _beamSourceIndex;
            set
            {
                if (_beamSourceIndex == value) return;
                _beamSourceIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBeamDatabase));
                OnPropertyChanged(nameof(IsBeamDirect));
                if (value == 0 && _selectedBeamLeftShapeIndex >= 0) FillFromBeamLeftShape();
            }
        }

        public int SelectedBeamLeftShapeIndex
        {
            get => _selectedBeamLeftShapeIndex;
            set { if (_selectedBeamLeftShapeIndex == value) return; _selectedBeamLeftShapeIndex = value; OnPropertyChanged(); if (value >= 0) FillFromBeamLeftShape(); }
        }

        public double DbLeft { get => _dbLeft; set { _dbLeft = value; OnPropertyChanged(); } }
        public double TfbLeft { get => _tfbLeft; set { _tfbLeft = value; OnPropertyChanged(); } }
        public double ZxLeft { get => _zxLeft; set { _zxLeft = value; OnPropertyChanged(); UpdateMprLeft(); } }
        public double FybLeft { get => _fybLeft; set { _fybLeft = value; OnPropertyChanged(); if (AutoRyLeft) UpdateRyLeft(); UpdateMprLeft(); } }
        public double RyLeft { get => _ryLeft; set { _ryLeft = value; OnPropertyChanged(); UpdateMprLeft(); } }
        public double CprLeft { get => _cprLeft; set { _cprLeft = value; OnPropertyChanged(); UpdateMprLeft(); } }
        public bool AutoRyLeft { get => _autoRyLeft; set { if (_autoRyLeft == value) return; _autoRyLeft = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsRyLeftReadOnly)); if (value) UpdateRyLeft(); } }
        public double MprLeft { get => _mprLeft; set { _mprLeft = value; OnPropertyChanged(); } }

        public bool IsBeamDatabase => BeamSourceIndex == 0;
        public bool IsBeamDirect => BeamSourceIndex == 1;
        public bool IsRyLeftReadOnly => AutoRyLeft;

        // ── Beam Right ──

        public bool HasRightBeam
        {
            get => _hasRightBeam;
            set { if (_hasRightBeam == value) return; _hasRightBeam = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowBeamRightCard)); }
        }

        public bool SameBeamRight
        {
            get => _sameBeamRight;
            set { if (_sameBeamRight == value) return; _sameBeamRight = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowBeamRightInputs)); if (value) CopyLeftToRight(); }
        }

        public double DbRight { get => _dbRight; set { _dbRight = value; OnPropertyChanged(); } }
        public double TfbRight { get => _tfbRight; set { _tfbRight = value; OnPropertyChanged(); } }
        public double ZxRight { get => _zxRight; set { _zxRight = value; OnPropertyChanged(); UpdateMprRight(); } }
        public double FybRight { get => _fybRight; set { _fybRight = value; OnPropertyChanged(); if (AutoRyRight) UpdateRyRight(); UpdateMprRight(); } }
        public double RyRight { get => _ryRight; set { _ryRight = value; OnPropertyChanged(); UpdateMprRight(); } }
        public double CprRight { get => _cprRight; set { _cprRight = value; OnPropertyChanged(); UpdateMprRight(); } }
        public bool AutoRyRight { get => _autoRyRight; set { if (_autoRyRight == value) return; _autoRyRight = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsRyRightReadOnly)); if (value) UpdateRyRight(); } }
        public double MprRight { get => _mprRight; set { _mprRight = value; OnPropertyChanged(); } }

        public bool IsRyRightReadOnly => AutoRyRight;

        // ── Joint ──

        public double Hc { get => _hc; set { _hc = value; OnPropertyChanged(); } }
        public double Pr { get => _pr; set { _pr = value; OnPropertyChanged(); } }

        // ── Computed Visibility ──

        public bool UseCapacityDesign => SystemIndex != 2;
        public bool ShowCapacityInputs => UseCapacityDesign;
        public bool ShowUserMoment => !UseCapacityDesign;
        public bool ShowBeamRightCard => HasRightBeam;
        public bool ShowBeamRightInputs => HasRightBeam && !SameBeamRight;

        // ── Results ──

        public string ProcessText { get => _processText; set { _processText = value; OnPropertyChanged(); } }
        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }
        public string DemandText { get => _demandText; set { _demandText = value; OnPropertyChanged(); } }
        public string CapacityText { get => _capacityText; set { _capacityText = value; OnPropertyChanged(); } }
        public string StrengthRatioText { get => _strengthRatioText; set { _strengthRatioText = value; OnPropertyChanged(); } }
        public string ThicknessText { get => _thicknessText; set { _thicknessText = value; OnPropertyChanged(); } }
        public string DoublerText { get => _doublerText; set { _doublerText = value; OnPropertyChanged(); } }
        public string PlugWeldText { get => _plugWeldText; set { _plugWeldText = value; OnPropertyChanged(); } }
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
        public Brush StatusForeground { get => _statusForeground; set { _statusForeground = value; OnPropertyChanged(); } }

        // ── Private Methods ──

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
            ColumnShapeOptions.Clear();
            BeamShapeOptions.Clear();
            foreach (var s in _allShapes)
            {
                ColumnShapeOptions.Add(s.Name);
                BeamShapeOptions.Add(s.Name);
            }
            if (ColumnShapeOptions.Count > 0) SelectedColumnShapeIndex = 0;
            if (BeamShapeOptions.Count > 0) SelectedBeamLeftShapeIndex = 0;
        }

        private void FillFromColumnShape()
        {
            if (SelectedColumnShapeIndex < 0 || SelectedColumnShapeIndex >= _allShapes.Count) return;
            var s = _allShapes[SelectedColumnShapeIndex];
            Dc = Math.Round(s.d, 3);
            Bcf = Math.Round(s.bf, 3);
            Tcf = Math.Round(s.tf, 4);
            Tcw = Math.Round(s.tw, 4);
            AgCol = Math.Round(s.Ag, 2);
        }

        private void FillFromBeamLeftShape()
        {
            if (SelectedBeamLeftShapeIndex < 0 || SelectedBeamLeftShapeIndex >= _allShapes.Count) return;
            var s = _allShapes[SelectedBeamLeftShapeIndex];
            DbLeft = Math.Round(s.d, 3);
            TfbLeft = Math.Round(s.tf, 4);
            ZxLeft = Math.Round(s.Zx, 3);
            if (SameBeamRight) CopyLeftToRight();
        }

        private void UpdateRyLeft()
        {
            if (!AutoRyLeft) return;
            RyLeft = PanelZoneCalculations.GetDefaultRy(FybLeft);
        }

        private void UpdateRyRight()
        {
            if (!AutoRyRight) return;
            RyRight = PanelZoneCalculations.GetDefaultRy(FybRight);
        }

        private void UpdateMprLeft()
        {
            if (!UseCapacityDesign) return;
            if (ZxLeft > 0 && FybLeft > 0 && RyLeft > 0 && CprLeft > 0)
                MprLeft = Math.Round(CprLeft * RyLeft * FybLeft * ZxLeft, 1);
        }

        private void UpdateMprRight()
        {
            if (!UseCapacityDesign) return;
            if (ZxRight > 0 && FybRight > 0 && RyRight > 0 && CprRight > 0)
                MprRight = Math.Round(CprRight * RyRight * FybRight * ZxRight, 1);
        }

        private void CopyLeftToRight()
        {
            DbRight = DbLeft;
            TfbRight = TfbLeft;
            ZxRight = ZxLeft;
            FybRight = FybLeft;
            RyRight = RyLeft;
            CprRight = CprLeft;
            MprRight = MprLeft;
        }

        private void Calculate()
        {
            try
            {
                var system = (PanelZoneCalculations.SeismicSystem)SystemIndex;
                var method = (PanelZoneCalculations.DesignMethod)MethodIndex;
                bool useCapacity = UseCapacityDesign;

                var input = new PanelZoneCalculations.InputParameters
                {
                    System = system,
                    Method = method,

                    Dc = Dc, Bcf = Bcf, Tcf = Tcf, Tcw = Tcw,
                    Fyc = Fyc, AgCol = AgCol, Pr = Pr,

                    DbLeft = DbLeft, TfbLeft = TfbLeft,
                    UseCapacityLeft = useCapacity,
                    ZxLeft = ZxLeft, FybLeft = FybLeft,
                    RyLeft = RyLeft, CprLeft = CprLeft,
                    MfLeft = MprLeft,

                    HasRightBeam = HasRightBeam,
                    DbRight = DbRight, TfbRight = TfbRight,
                    UseCapacityRight = useCapacity,
                    ZxRight = ZxRight, FybRight = FybRight,
                    RyRight = RyRight, CprRight = CprRight,
                    MfRight = MprRight,

                    Hc = Hc
                };

                var result = PanelZoneCalculations.Calculate(input);

                ProcessText = string.Join("\n", result.Process);
                HasResult = result.IsValid;

                if (result.IsValid)
                {
                    DemandText = $"{result.Ru:F2} kips";
                    CapacityText = $"{result.DesignCapacity:F2} kips";
                    StrengthRatioText = $"{result.StrengthRatio:F3}";

                    if (result.ThicknessChecked)
                        ThicknessText = $"tmin = {result.Tmin:F4} in | tcw = {Tcw:F4} in";
                    else
                        ThicknessText = "N/A (OMF)";

                    if (result.NeedsDoubler)
                    {
                        DoublerText = $"tdp = {result.RecommendedTdp:F4} in";
                        PlugWeldText = result.NeedsPlugWelds ? "需要塞焊" : "不需要";
                    }
                    else
                    {
                        DoublerText = "无需补强板";
                        PlugWeldText = "—";
                    }

                    bool allPass = result.StrengthPass && result.ThicknessPass;
                    StatusText = allPass
                        ? "PASS — 节点剪切区验算通过"
                        : $"FAIL — {(result.StrengthPass ? "" : "强度不足")}{(!result.ThicknessPass && !result.StrengthPass ? "，" : "")}{(!result.ThicknessPass ? "腹板过薄" : "")}";
                    StatusForeground = allPass ? Brushes.Green : Brushes.Red;
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
