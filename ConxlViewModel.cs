using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class ConxlViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        // Beam section selection
        private List<string> _beamShapeOptions = new();
        private int _selectedBeamShapeIndex = -1;
        private List<WShapeProperties> _beamShapes = new();

        // Beam properties (auto-filled from section or manual)
        private string _beamDesignation = "";
        private double _beamD = 24.0;
        private double _beamBf = 9.0;
        private double _beamTf = 0.5;
        private double _beamTw = 0.3;
        private double _beamZx = 200.0;

        // Beam material
        private double _beamFy = 50.0;
        private double _beamFu = 65.0;
        private double _beamRy = 1.1;
        private double _beamRt = 1.2;

        // Column box properties (NOT a W-shape, manual input)
        private double _tCol = 0.5;
        private double _colFy = 50.0;
        private double _colFu = 62.0;
        private double _fc = 4.0;
        private double _concreteWeight = 145.0;
        private double _tLegCC = 0.75;

        // RBS geometry
        private bool _useRbs = false;
        private double _rbsA = 5.0;
        private double _rbsB = 18.0;
        private double _rbsC = 1.5;

        // Design parameters
        private double _span = 300.0;
        private int _systemTypeIndex = 0;
        private double _storyAbove = 156.0;
        private double _storyBelow = 156.0;
        private double _pu = 0.0;
        private double _fexx = 70.0;

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
        private bool _columnBeamPassed;
        private bool _boltTensionPassed;
        private bool _boltShearPassed;
        private bool _beamShearPassed;
        private bool _cwxWeldPassed;
        private bool _ccWeldPassed;
        private bool _panelZonePassed;

        // Key results
        private double _cpr;
        private double _ze;
        private double _mpr;
        private double _vh;
        private double _mBolts;
        private double _rut;

        // Utilization ratios
        private double _boltTensionRatio;
        private double _boltShearRatio;
        private double _beamShearRatio;
        private double _panelZoneRatio;

        #endregion

        #region Constructor

        public ConxlViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            SystemTypeOptions = new List<string> { "SMF", "IMF" };
            LoadShapeOptions();
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

        // Column box properties
        public double TCol
        {
            get => _tCol;
            set { _tCol = value; OnPropertyChanged(); }
        }
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
        public double Fc
        {
            get => _fc;
            set { _fc = value; OnPropertyChanged(); }
        }
        public double ConcreteWeight
        {
            get => _concreteWeight;
            set { _concreteWeight = value; OnPropertyChanged(); }
        }
        public double TLegCC
        {
            get => _tLegCC;
            set { _tLegCC = value; OnPropertyChanged(); }
        }

        // RBS geometry
        public bool UseRbs
        {
            get => _useRbs;
            set { _useRbs = value; OnPropertyChanged(); }
        }
        public double RbsA
        {
            get => _rbsA;
            set { _rbsA = value; OnPropertyChanged(); }
        }
        public double RbsB
        {
            get => _rbsB;
            set { _rbsB = value; OnPropertyChanged(); }
        }
        public double RbsC
        {
            get => _rbsC;
            set { _rbsC = value; OnPropertyChanged(); }
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
        public double StoryAbove
        {
            get => _storyAbove;
            set { _storyAbove = value; OnPropertyChanged(); }
        }
        public double StoryBelow
        {
            get => _storyBelow;
            set { _storyBelow = value; OnPropertyChanged(); }
        }
        public double Pu
        {
            get => _pu;
            set { _pu = value; OnPropertyChanged(); }
        }
        public double FEXX
        {
            get => _fexx;
            set { _fexx = value; OnPropertyChanged(); }
        }

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

        public bool ColumnBeamPassed
        {
            get => _columnBeamPassed;
            set { _columnBeamPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColumnBeamStatus)); }
        }
        public string ColumnBeamStatus => ColumnBeamPassed ? "PASS" : "FAIL";

        public bool BoltTensionPassed
        {
            get => _boltTensionPassed;
            set { _boltTensionPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BoltTensionStatus)); }
        }
        public string BoltTensionStatus => BoltTensionPassed ? "PASS" : "FAIL";

        public bool BoltShearPassed
        {
            get => _boltShearPassed;
            set { _boltShearPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BoltShearStatus)); }
        }
        public string BoltShearStatus => BoltShearPassed ? "PASS" : "FAIL";

        public bool BeamShearPassed
        {
            get => _beamShearPassed;
            set { _beamShearPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BeamShearStatus)); }
        }
        public string BeamShearStatus => BeamShearPassed ? "PASS" : "FAIL";

        public bool CwxWeldPassed
        {
            get => _cwxWeldPassed;
            set { _cwxWeldPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(CwxWeldStatus)); }
        }
        public string CwxWeldStatus => CwxWeldPassed ? "PASS" : "FAIL";

        public bool CcWeldPassed
        {
            get => _ccWeldPassed;
            set { _ccWeldPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(CcWeldStatus)); }
        }
        public string CcWeldStatus => CcWeldPassed ? "PASS" : "FAIL";

        public bool PanelZonePassed
        {
            get => _panelZonePassed;
            set { _panelZonePassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(PanelZoneStatus)); }
        }
        public string PanelZoneStatus => PanelZonePassed ? "PASS" : "FAIL";

        // Key results
        public double Cpr
        {
            get => _cpr;
            set { _cpr = value; OnPropertyChanged(); }
        }
        public double Ze
        {
            get => _ze;
            set { _ze = value; OnPropertyChanged(); }
        }
        public double Mpr
        {
            get => _mpr;
            set { _mpr = value; OnPropertyChanged(); }
        }
        public double Vh
        {
            get => _vh;
            set { _vh = value; OnPropertyChanged(); }
        }
        public double MBolts
        {
            get => _mBolts;
            set { _mBolts = value; OnPropertyChanged(); }
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
        public double BoltShearRatio
        {
            get => _boltShearRatio;
            set { _boltShearRatio = value; OnPropertyChanged(); }
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

            BeamShapeOptions = new List<string> { "-- Select --" }
                .Concat(shapes.Select(s => s.Name))
                .ToList();

            SelectedBeamShapeIndex = 0;
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

        private void Calculate()
        {
            try
            {
                var input = new ConxlCalculations.InputParameters
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

                    // Column box
                    TCol = _tCol,
                    ColFy = _colFy,
                    ColFu = _colFu,
                    Fc = _fc,
                    ConcreteWeight = _concreteWeight,
                    TLegCC = _tLegCC,

                    // RBS
                    UseRbs = _useRbs,
                    RbsA = _rbsA,
                    RbsB = _rbsB,
                    RbsC = _rbsC,

                    // Design
                    Span = _span,
                    SystemType = SystemTypeOptions[_systemTypeIndex],
                    StoryAbove = _storyAbove,
                    StoryBelow = _storyBelow,
                    Pu = _pu,
                    FEXX = _fexx,

                    // Loads
                    LoadD = _loadD,
                    LoadL = _loadL,
                    LoadS = _loadS,
                    F1 = _f1,
                    Vu = _vu,
                };

                var result = ConxlCalculations.Calculate(input);

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
                ColumnBeamPassed = result.ColumnBeamPassed;
                BoltTensionPassed = result.BoltTensionPassed;
                BoltShearPassed = result.BoltShearPassed;
                BeamShearPassed = result.BeamShearPassed;
                CwxWeldPassed = result.CwxWeldPassed;
                CcWeldPassed = result.CcWeldPassed;
                PanelZonePassed = result.PanelZonePassed;

                // Key results
                Cpr = result.Cpr;
                Ze = result.Ze;
                Mpr = result.Mpr;
                Vh = result.Vh;
                MBolts = result.MBolts;
                Rut = result.Rut;

                // Utilization ratios
                BoltTensionRatio = result.BoltTensionRatio;
                BoltShearRatio = result.BoltShearRatio;
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
