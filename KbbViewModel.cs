using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class KbbViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        // Beam section selection
        private List<string> _beamShapeOptions = new();
        private int _selectedBeamShapeIndex = -1;
        private List<WShapeProperties> _beamShapes = new();

        // Column section selection
        private List<string> _colShapeOptions = new();
        private int _selectedColShapeIndex = -1;
        private List<WShapeProperties> _colShapes = new();

        // Beam properties
        private string _beamDesignation = "";
        private double _beamD = 24.0;
        private double _beamBf = 9.0;
        private double _beamTf = 0.5;
        private double _beamTw = 0.3;
        private double _beamZx = 200.0;

        // Column properties
        private string _colDesignation = "";
        private double _colD = 14.0;
        private double _colBf = 15.0;
        private double _colTf = 1.0;
        private double _colTw = 0.5;
        private double _colZx = 400.0;

        // Beam material
        private double _beamFy = 50.0;
        private double _beamFu = 65.0;
        private double _beamRy = 1.1;
        private double _beamRt = 1.2;

        // Column material
        private double _colFy = 50.0;
        private double _colFu = 65.0;
        private double _colRy = 1.1;
        private double _colRt = 1.2;

        // Bracket selection
        private int _bracketSeriesIndex = 0; // 0=W, 1=B
        private int _bracketModelIndex = 0;

        // B-series: beam bolt count
        private int _nbb = 8;

        // Weld electrode (W-series)
        private double _fexx = 70.0;

        // Design parameters
        private double _span = 300.0;
        private int _systemTypeIndex = 0;

        // Loads
        private double _loadD = 0.0;
        private double _loadL = 0.0;
        private double _loadS = 0.0;
        private double _f1 = 0.5;
        private double _vu = 0.0;

        // Results
        private string _processText = "";
        private bool _hasResult;
        private bool _overallPassed;
        private string _overallStatus = "";
        private string _overallStatusColor = "Green";

        // Individual check results
        private bool _prequalificationPassed;
        private bool _boltTensionPassed;
        private bool _cfWidthPassed;
        private bool _cfThicknessPryingPassed;
        private bool _continuityNoPlatesPassed;
        private bool _continuityPlatesPassed;
        private bool _beamFlangeWidthPassed;
        private bool _beamBoltShearPassed;
        private bool _blockShearPassed;
        private bool _filletWeldPassed;
        private bool _beamShearPassed;
        private bool _panelZonePassed;

        // Key results
        private double _mpr;
        private double _mf;
        private double _vh;
        private double _dEff;
        private double _rut;

        // Utilization ratios
        private double _boltTensionRatio;
        private double _cfThicknessRatio;
        private double _beamBoltShearRatio;
        private double _blockShearRatio;
        private double _filletWeldRatio;
        private double _beamShearRatio;
        private double _panelZoneRatio;

        #endregion

        #region Constructor

        public KbbViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            BracketSeriesOptions = new List<string> { "W", "B" };
            SystemTypeOptions = new List<string> { "SMF", "IMF" };
            LoadShapeOptions();
            UpdateBracketModelOptions();
        }

        #endregion

        #region Properties

        // Section selection
        public List<string> BeamShapeOptions
        {
            get => _beamShapeOptions;
            set { _beamShapeOptions = value; OnPropertyChanged(); }
        }
        public int SelectedBeamShapeIndex
        {
            get => _selectedBeamShapeIndex;
            set
            {
                _selectedBeamShapeIndex = value;
                OnPropertyChanged();
                ApplyBeamSection();
            }
        }
        public List<string> ColShapeOptions
        {
            get => _colShapeOptions;
            set { _colShapeOptions = value; OnPropertyChanged(); }
        }
        public int SelectedColShapeIndex
        {
            get => _selectedColShapeIndex;
            set
            {
                _selectedColShapeIndex = value;
                OnPropertyChanged();
                ApplyColSection();
            }
        }

        // Beam properties
        public string BeamDesignation
        {
            get => _beamDesignation;
            set { _beamDesignation = value; OnPropertyChanged(); }
        }
        public double BeamD
        {
            get => _beamD;
            set { _beamD = value; OnPropertyChanged(); }
        }
        public double BeamBf
        {
            get => _beamBf;
            set { _beamBf = value; OnPropertyChanged(); }
        }
        public double BeamTf
        {
            get => _beamTf;
            set { _beamTf = value; OnPropertyChanged(); }
        }
        public double BeamTw
        {
            get => _beamTw;
            set { _beamTw = value; OnPropertyChanged(); }
        }
        public double BeamZx
        {
            get => _beamZx;
            set { _beamZx = value; OnPropertyChanged(); }
        }

        // Column properties
        public string ColDesignation
        {
            get => _colDesignation;
            set { _colDesignation = value; OnPropertyChanged(); }
        }
        public double ColD
        {
            get => _colD;
            set { _colD = value; OnPropertyChanged(); }
        }
        public double ColBf
        {
            get => _colBf;
            set { _colBf = value; OnPropertyChanged(); }
        }
        public double ColTf
        {
            get => _colTf;
            set { _colTf = value; OnPropertyChanged(); }
        }
        public double ColTw
        {
            get => _colTw;
            set { _colTw = value; OnPropertyChanged(); }
        }
        public double ColZx
        {
            get => _colZx;
            set { _colZx = value; OnPropertyChanged(); }
        }

        // Beam material
        public double BeamFy
        {
            get => _beamFy;
            set { _beamFy = value; OnPropertyChanged(); }
        }
        public double BeamFu
        {
            get => _beamFu;
            set { _beamFu = value; OnPropertyChanged(); }
        }
        public double BeamRy
        {
            get => _beamRy;
            set { _beamRy = value; OnPropertyChanged(); }
        }
        public double BeamRt
        {
            get => _beamRt;
            set { _beamRt = value; OnPropertyChanged(); }
        }

        // Column material
        public double ColFy
        {
            get => _colFy;
            set { _colFy = value; OnPropertyChanged(); }
        }
        public double ColFu
        {
            get => _colFu;
            set { _colFu = value; OnPropertyChanged(); }
        }
        public double ColRy
        {
            get => _colRy;
            set { _colRy = value; OnPropertyChanged(); }
        }
        public double ColRt
        {
            get => _colRt;
            set { _colRt = value; OnPropertyChanged(); }
        }

        // Bracket selection
        public List<string> BracketSeriesOptions { get; }
        public int BracketSeriesIndex
        {
            get => _bracketSeriesIndex;
            set
            {
                _bracketSeriesIndex = value;
                OnPropertyChanged();
                UpdateBracketModelOptions();
            }
        }
        public List<string> BracketModelOptions { get; private set; } = new();
        public int BracketModelIndex
        {
            get => _bracketModelIndex;
            set
            {
                _bracketModelIndex = value;
                OnPropertyChanged();
                UpdateNbbDefault();
            }
        }

        // B-series: beam bolt count
        public int Nbb
        {
            get => _nbb;
            set { _nbb = value; OnPropertyChanged(); }
        }
        public bool IsBSeries => BracketSeriesIndex == 1;
        public bool IsWSeries => BracketSeriesIndex == 0;

        // Weld electrode (W-series)
        public double FEXX
        {
            get => _fexx;
            set { _fexx = value; OnPropertyChanged(); }
        }

        // Design parameters
        public double Span
        {
            get => _span;
            set { _span = value; OnPropertyChanged(); }
        }
        public int SystemTypeIndex
        {
            get => _systemTypeIndex;
            set { _systemTypeIndex = value; OnPropertyChanged(); }
        }
        public List<string> SystemTypeOptions { get; }

        // Loads
        public double LoadD
        {
            get => _loadD;
            set { _loadD = value; OnPropertyChanged(); }
        }
        public double LoadL
        {
            get => _loadL;
            set { _loadL = value; OnPropertyChanged(); }
        }
        public double LoadS
        {
            get => _loadS;
            set { _loadS = value; OnPropertyChanged(); }
        }
        public double F1
        {
            get => _f1;
            set { _f1 = value; OnPropertyChanged(); }
        }
        public double Vu
        {
            get => _vu;
            set { _vu = value; OnPropertyChanged(); }
        }

        // Results
        public string ProcessText
        {
            get => _processText;
            set { _processText = value; OnPropertyChanged(); }
        }
        public bool HasResult
        {
            get => _hasResult;
            set { _hasResult = value; OnPropertyChanged(); }
        }
        public bool OverallPassed
        {
            get => _overallPassed;
            set { _overallPassed = value; OnPropertyChanged(); }
        }
        public string OverallStatus
        {
            get => _overallStatus;
            set { _overallStatus = value; OnPropertyChanged(); }
        }
        public string OverallStatusColor
        {
            get => _overallStatusColor;
            set { _overallStatusColor = value; OnPropertyChanged(); }
        }

        // Individual check results
        public bool PrequalificationPassed
        {
            get => _prequalificationPassed;
            set { _prequalificationPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(PrequalificationStatus)); }
        }
        public string PrequalificationStatus => PrequalificationPassed ? "PASS" : "FAIL";

        public bool BoltTensionPassed
        {
            get => _boltTensionPassed;
            set { _boltTensionPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BoltTensionStatus)); }
        }
        public string BoltTensionStatus => BoltTensionPassed ? "PASS" : "FAIL";

        public bool CfWidthPassed
        {
            get => _cfWidthPassed;
            set { _cfWidthPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(CfWidthStatus)); }
        }
        public string CfWidthStatus => CfWidthPassed ? "PASS" : "FAIL";

        public bool CfThicknessPryingPassed
        {
            get => _cfThicknessPryingPassed;
            set { _cfThicknessPryingPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(CfThicknessPryingStatus)); }
        }
        public string CfThicknessPryingStatus => CfThicknessPryingPassed ? "PASS" : "FAIL";

        public bool ContinuityNoPlatesPassed
        {
            get => _continuityNoPlatesPassed;
            set { _continuityNoPlatesPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ContinuityNoPlatesStatus)); }
        }
        public string ContinuityNoPlatesStatus => ContinuityNoPlatesPassed ? "PASS" : "FAIL";

        public bool ContinuityPlatesPassed
        {
            get => _continuityPlatesPassed;
            set { _continuityPlatesPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ContinuityPlatesStatus)); }
        }
        public string ContinuityPlatesStatus => ContinuityPlatesPassed ? "PASS" : "FAIL";

        public bool BeamFlangeWidthPassed
        {
            get => _beamFlangeWidthPassed;
            set { _beamFlangeWidthPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BeamFlangeWidthStatus)); }
        }
        public string BeamFlangeWidthStatus => BeamFlangeWidthPassed ? "PASS" : "FAIL";

        public bool BeamBoltShearPassed
        {
            get => _beamBoltShearPassed;
            set { _beamBoltShearPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BeamBoltShearStatus)); }
        }
        public string BeamBoltShearStatus => BeamBoltShearPassed ? "PASS" : "FAIL";

        public bool BlockShearPassed
        {
            get => _blockShearPassed;
            set { _blockShearPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BlockShearStatus)); }
        }
        public string BlockShearStatus => BlockShearPassed ? "PASS" : "FAIL";

        public bool FilletWeldPassed
        {
            get => _filletWeldPassed;
            set { _filletWeldPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilletWeldStatus)); }
        }
        public string FilletWeldStatus => FilletWeldPassed ? "PASS" : "FAIL";

        public bool BeamShearPassed
        {
            get => _beamShearPassed;
            set { _beamShearPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BeamShearStatus)); }
        }
        public string BeamShearStatus => BeamShearPassed ? "PASS" : "FAIL";

        public bool PanelZonePassed
        {
            get => _panelZonePassed;
            set { _panelZonePassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(PanelZoneStatus)); }
        }
        public string PanelZoneStatus => PanelZonePassed ? "PASS" : "FAIL";

        // Key results
        public double Mpr
        {
            get => _mpr;
            set { _mpr = value; OnPropertyChanged(); }
        }
        public double Mf
        {
            get => _mf;
            set { _mf = value; OnPropertyChanged(); }
        }
        public double Vh
        {
            get => _vh;
            set { _vh = value; OnPropertyChanged(); }
        }
        public double DEff
        {
            get => _dEff;
            set { _dEff = value; OnPropertyChanged(); }
        }
        public double Rut
        {
            get => _rut;
            set { _rut = value; OnPropertyChanged(); }
        }

        // Utilization ratios
        public double BoltTensionRatio
        {
            get => _boltTensionRatio;
            set { _boltTensionRatio = value; OnPropertyChanged(); }
        }
        public double CfThicknessRatio
        {
            get => _cfThicknessRatio;
            set { _cfThicknessRatio = value; OnPropertyChanged(); }
        }
        public double BeamBoltShearRatio
        {
            get => _beamBoltShearRatio;
            set { _beamBoltShearRatio = value; OnPropertyChanged(); }
        }
        public double BlockShearRatio
        {
            get => _blockShearRatio;
            set { _blockShearRatio = value; OnPropertyChanged(); }
        }
        public double FilletWeldRatio
        {
            get => _filletWeldRatio;
            set { _filletWeldRatio = value; OnPropertyChanged(); }
        }
        public double BeamShearRatio
        {
            get => _beamShearRatio;
            set { _beamShearRatio = value; OnPropertyChanged(); }
        }
        public double PanelZoneRatio
        {
            get => _panelZoneRatio;
            set { _panelZoneRatio = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand CalculateCommand { get; }

        #endregion

        #region Methods

        private void LoadShapeOptions()
        {
            var shapes = Aisc358ShapeHelper.LoadWShapes();
            _beamShapes = shapes;
            _colShapes = shapes;

            BeamShapeOptions = new List<string> { "-- Select --" }
                .Concat(shapes.Select(s => s.Name))
                .ToList();
            ColShapeOptions = new List<string> { "-- Select --" }
                .Concat(shapes.Select(s => s.Name))
                .ToList();

            SelectedBeamShapeIndex = 0;
            SelectedColShapeIndex = 0;
        }

        private void ApplyBeamSection()
        {
            int idx = _selectedBeamShapeIndex - 1;
            if (idx < 0 || idx >= _beamShapes.Count) return;

            var shape = _beamShapes[idx];
            BeamDesignation = shape.Name;
            BeamD = shape.d;
            BeamBf = shape.bf;
            BeamTf = shape.tf;
            BeamTw = shape.tw;
            BeamZx = shape.Zx;
        }

        private void ApplyColSection()
        {
            int idx = _selectedColShapeIndex - 1;
            if (idx < 0 || idx >= _colShapes.Count) return;

            var shape = _colShapes[idx];
            ColDesignation = shape.Name;
            ColD = shape.d;
            ColBf = shape.bf;
            ColTf = shape.tf;
            ColTw = shape.tw;
            ColZx = shape.Zx;
        }

        private void UpdateBracketModelOptions()
        {
            string series = BracketSeriesOptions[_bracketSeriesIndex];
            var models = KbbCalculations.Brackets
                .Where(kvp => kvp.Value.Series == series)
                .Select(kvp => kvp.Key)
                .ToList();

            BracketModelOptions = models;
            OnPropertyChanged(nameof(BracketModelOptions));
            BracketModelIndex = 0;
            OnPropertyChanged(nameof(IsBSeries));
            OnPropertyChanged(nameof(IsWSeries));
        }

        private void UpdateNbbDefault()
        {
            if (_bracketModelIndex < 0 || _bracketModelIndex >= BracketModelOptions.Count) return;

            string model = BracketModelOptions[_bracketModelIndex];
            var bk = KbbCalculations.Brackets[model];
            if (bk.Series == "B" && bk.NbbOptions.Length > 0)
            {
                Nbb = bk.NbbOptions[0];
            }
        }

        private string GetSelectedBracketModel()
        {
            if (_bracketModelIndex >= 0 && _bracketModelIndex < BracketModelOptions.Count)
                return BracketModelOptions[_bracketModelIndex];
            return "W2.1";
        }

        private void Calculate()
        {
            try
            {
                var input = new KbbCalculations.InputParameters
                {
                    // Beam
                    BeamDesignation = _beamDesignation,
                    BeamD = _beamD,
                    BeamBf = _beamBf,
                    BeamTf = _beamTf,
                    BeamTw = _beamTw,
                    BeamZx = _beamZx,
                    BeamFy = _beamFy,
                    BeamFu = _beamFu,
                    BeamRy = _beamRy,
                    BeamRt = _beamRt,

                    // Column
                    ColDesignation = _colDesignation,
                    ColD = _colD,
                    ColBf = _colBf,
                    ColTf = _colTf,
                    ColTw = _colTw,
                    ColZx = _colZx,
                    ColFy = _colFy,
                    ColFu = _colFu,
                    ColRy = _colRy,
                    ColRt = _colRt,

                    // Bracket
                    BracketModel = GetSelectedBracketModel(),
                    Nbb = _nbb,
                    FEXX = _fexx,

                    // Design
                    Span = _span,
                    SystemType = SystemTypeOptions[_systemTypeIndex],

                    // Loads
                    LoadD = _loadD,
                    LoadL = _loadL,
                    LoadS = _loadS,
                    F1 = _f1,
                    Vu = _vu,
                };

                var result = KbbCalculations.Calculate(input);

                if (!result.IsValid)
                {
                    ProcessText = $"Error: {result.ErrorMessage}";
                    return;
                }

                ProcessText = string.Join(Environment.NewLine, result.Process);
                HasResult = true;
                OverallPassed = result.OverallPassed;
                OverallStatus = result.OverallPassed ? "ALL CHECKS PASSED" : "SOME CHECKS FAILED";
                OverallStatusColor = result.OverallPassed ? "Green" : "Red";

                // Individual checks
                PrequalificationPassed = result.PrequalificationPassed;
                BoltTensionPassed = result.BoltTensionPassed;
                CfWidthPassed = result.CfWidthPassed;
                CfThicknessPryingPassed = result.CfThicknessPryingPassed;
                ContinuityNoPlatesPassed = result.ContinuityNoPlatesPassed;
                ContinuityPlatesPassed = result.ContinuityPlatesPassed;
                BeamFlangeWidthPassed = result.BeamFlangeWidthPassed;
                BeamBoltShearPassed = result.BeamBoltShearPassed;
                BlockShearPassed = result.BlockShearPassed;
                FilletWeldPassed = result.FilletWeldPassed;
                BeamShearPassed = result.BeamShearPassed;
                PanelZonePassed = result.PanelZonePassed;

                // Key results
                Mpr = result.Mpr;
                Mf = result.Mf;
                Vh = result.Vh;
                DEff = result.DEff;
                Rut = result.Rut;

                // Utilization ratios
                BoltTensionRatio = result.BoltTensionRatio;
                CfThicknessRatio = result.CfThicknessRatio;
                BeamBoltShearRatio = result.BeamBoltShearRatio;
                BlockShearRatio = result.BlockShearRatio;
                FilletWeldRatio = result.FilletWeldRatio;
                BeamShearRatio = result.BeamShearRatio;
                PanelZoneRatio = result.PanelZoneRatio;
            }
            catch (Exception ex)
            {
                ProcessText = $"Calculation error: {ex.Message}";
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
