using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class DoubleteeViewModel : INotifyPropertyChanged
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
        private double _beamD = 21.0;
        private double _beamBf = 6.5;
        private double _beamTf = 0.45;
        private double _beamTw = 0.35;
        private double _beamZx = 95.0;

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
        private double _beamRt = 1.1;

        // Column material
        private double _colFy = 50.0;
        private double _colFu = 65.0;
        private double _colRy = 1.1;

        // Bolt configuration
        private int _nTbIndex = 0;
        private int _boltTypeIndex = 0;
        private double _s1 = 3.0;
        private double _sVb = 3.0;
        private double _gVb = 3.5;
        private double _gTb = 5.5;

        // T-Stub geometry
        private double _tSt = 0.0;
        private double _tFt = 0.0;
        private double _bFt = 0.0;

        // Design parameters
        private double _span = 300.0;
        private int _systemTypeIndex = 0;
        private bool _hasSlab = false;

        // Story / column axial
        private double _storyAbove = 156.0;
        private double _storyBelow = 156.0;
        private double _pu = 0.0;
        private double _asCol = 0.0;

        // Loads
        private double _loadD = 0.0;
        private double _loadL = 0.0;
        private double _loadS = 0.0;
        private double _f1 = 0.5;

        // Results
        private string _processText = "";
        private bool _hasResult;
        private bool _overallPassed;
        private string _overallStatus = "";
        private string _overallStatusColor = "Green";

        // Individual check results
        private bool _prequalificationPassed;
        private bool _boltDiameterPassed;
        private bool _beamShearPassed;
        private bool _columnBeamPassed;
        private bool _stiffnessPassed;
        private bool _shearBoltsPassed;
        private bool _tStemPassed;
        private bool _tFlangePassed;
        private bool _gageRatioPassed;
        private bool _bearingPassed;
        private bool _blockShearPassed;
        private bool _columnFlangePassed;
        private bool _columnWebPassed;
        private bool _panelZonePassed;
        private bool _continuityPlatesPassed;

        // Key results
        private double _mpr;
        private double _mf;
        private double _fpr;
        private double _ff;
        private double _vh;

        // Ratios
        private double _beamShearRatio;
        private double _shearBoltsRatio;
        private double _tStemRatio;
        private double _tFlangeRatio;
        private double _bearingRatio;
        private double _blockShearRatio;
        private double _panelZoneRatio;
        private double _stiffnessRatio;
        private double _columnFlangeRatio;
        private double _columnWebRatio;
        private double _columnBeamRatio;

        #endregion

        #region Constructor

        public DoubleteeViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            NTBOptions = new List<int> { 4, 8 };
            BoltTypeOptions = new List<string> { "A325", "A490" };
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

        // Bolt configuration
        public List<int> NTBOptions { get; }
        public int NTbIndex
        {
            get => _nTbIndex;
            set { _nTbIndex = value; OnPropertyChanged(); }
        }
        public List<string> BoltTypeOptions { get; }
        public int BoltTypeIndex
        {
            get => _boltTypeIndex;
            set { _boltTypeIndex = value; OnPropertyChanged(); }
        }
        public double S1
        {
            get => _s1;
            set { _s1 = value; OnPropertyChanged(); }
        }
        public double SVb
        {
            get => _sVb;
            set { _sVb = value; OnPropertyChanged(); }
        }
        public double GVb
        {
            get => _gVb;
            set { _gVb = value; OnPropertyChanged(); }
        }
        public double GTb
        {
            get => _gTb;
            set { _gTb = value; OnPropertyChanged(); }
        }

        // T-Stub geometry
        public double TSt
        {
            get => _tSt;
            set { _tSt = value; OnPropertyChanged(); }
        }
        public double TFt
        {
            get => _tFt;
            set { _tFt = value; OnPropertyChanged(); }
        }
        public double BFt
        {
            get => _bFt;
            set { _bFt = value; OnPropertyChanged(); }
        }

        // Design parameters
        public double Span
        {
            get => _span;
            set { _span = value; OnPropertyChanged(); }
        }
        public List<string> SystemTypeOptions { get; }
        public int SystemTypeIndex
        {
            get => _systemTypeIndex;
            set { _systemTypeIndex = value; OnPropertyChanged(); }
        }
        public bool HasSlab
        {
            get => _hasSlab;
            set { _hasSlab = value; OnPropertyChanged(); }
        }

        // Story / column axial
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
        public double AsCol
        {
            get => _asCol;
            set { _asCol = value; OnPropertyChanged(); }
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

        public bool BoltDiameterPassed
        {
            get => _boltDiameterPassed;
            set { _boltDiameterPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BoltDiameterStatus)); }
        }
        public string BoltDiameterStatus => BoltDiameterPassed ? "PASS" : "FAIL";

        public bool BeamShearPassed
        {
            get => _beamShearPassed;
            set { _beamShearPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BeamShearStatus)); }
        }
        public string BeamShearStatus => BeamShearPassed ? "PASS" : "FAIL";

        public bool ColumnBeamPassed
        {
            get => _columnBeamPassed;
            set { _columnBeamPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColumnBeamStatus)); }
        }
        public string ColumnBeamStatus => ColumnBeamPassed ? "PASS" : "FAIL";

        public bool StiffnessPassed
        {
            get => _stiffnessPassed;
            set { _stiffnessPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(StiffnessStatus)); }
        }
        public string StiffnessStatus => StiffnessPassed ? "PASS" : "FAIL";

        public bool ShearBoltsPassed
        {
            get => _shearBoltsPassed;
            set { _shearBoltsPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShearBoltsStatus)); }
        }
        public string ShearBoltsStatus => ShearBoltsPassed ? "PASS" : "FAIL";

        public bool TStemPassed
        {
            get => _tStemPassed;
            set { _tStemPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(TStemStatus)); }
        }
        public string TStemStatus => TStemPassed ? "PASS" : "FAIL";

        public bool TFlangePassed
        {
            get => _tFlangePassed;
            set { _tFlangePassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(TFlangeStatus)); }
        }
        public string TFlangeStatus => TFlangePassed ? "PASS" : "FAIL";

        public bool GageRatioPassed
        {
            get => _gageRatioPassed;
            set { _gageRatioPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(GageRatioStatus)); }
        }
        public string GageRatioStatus => GageRatioPassed ? "PASS" : "FAIL";

        public bool BearingPassed
        {
            get => _bearingPassed;
            set { _bearingPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BearingStatus)); }
        }
        public string BearingStatus => BearingPassed ? "PASS" : "FAIL";

        public bool BlockShearPassed
        {
            get => _blockShearPassed;
            set { _blockShearPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BlockShearStatus)); }
        }
        public string BlockShearStatus => BlockShearPassed ? "PASS" : "FAIL";

        public bool ColumnFlangePassed
        {
            get => _columnFlangePassed;
            set { _columnFlangePassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColumnFlangeStatus)); }
        }
        public string ColumnFlangeStatus => ColumnFlangePassed ? "PASS" : "FAIL";

        public bool ColumnWebPassed
        {
            get => _columnWebPassed;
            set { _columnWebPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColumnWebStatus)); }
        }
        public string ColumnWebStatus => ColumnWebPassed ? "PASS" : "FAIL";

        public bool PanelZonePassed
        {
            get => _panelZonePassed;
            set { _panelZonePassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(PanelZoneStatus)); }
        }
        public string PanelZoneStatus => PanelZonePassed ? "PASS" : "FAIL";

        public bool ContinuityPlatesPassed
        {
            get => _continuityPlatesPassed;
            set { _continuityPlatesPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ContinuityPlatesStatus)); }
        }
        public string ContinuityPlatesStatus => ContinuityPlatesPassed ? "PASS" : "FAIL";

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
        public double Fpr
        {
            get => _fpr;
            set { _fpr = value; OnPropertyChanged(); }
        }
        public double Ff
        {
            get => _ff;
            set { _ff = value; OnPropertyChanged(); }
        }
        public double Vh
        {
            get => _vh;
            set { _vh = value; OnPropertyChanged(); }
        }

        // Ratios
        public double BeamShearRatio
        {
            get => _beamShearRatio;
            set { _beamShearRatio = value; OnPropertyChanged(); }
        }
        public double ShearBoltsRatio
        {
            get => _shearBoltsRatio;
            set { _shearBoltsRatio = value; OnPropertyChanged(); }
        }
        public double TStemRatio
        {
            get => _tStemRatio;
            set { _tStemRatio = value; OnPropertyChanged(); }
        }
        public double TFlangeRatio
        {
            get => _tFlangeRatio;
            set { _tFlangeRatio = value; OnPropertyChanged(); }
        }
        public double BearingRatio
        {
            get => _bearingRatio;
            set { _bearingRatio = value; OnPropertyChanged(); }
        }
        public double BlockShearRatio
        {
            get => _blockShearRatio;
            set { _blockShearRatio = value; OnPropertyChanged(); }
        }
        public double PanelZoneRatio
        {
            get => _panelZoneRatio;
            set { _panelZoneRatio = value; OnPropertyChanged(); }
        }
        public double StiffnessRatio
        {
            get => _stiffnessRatio;
            set { _stiffnessRatio = value; OnPropertyChanged(); }
        }
        public double ColumnFlangeRatio
        {
            get => _columnFlangeRatio;
            set { _columnFlangeRatio = value; OnPropertyChanged(); }
        }
        public double ColumnWebRatio
        {
            get => _columnWebRatio;
            set { _columnWebRatio = value; OnPropertyChanged(); }
        }
        public double ColumnBeamRatio
        {
            get => _columnBeamRatio;
            set { _columnBeamRatio = value; OnPropertyChanged(); }
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

        private void Calculate()
        {
            try
            {
                var input = new DoubleteeCalculations.InputParameters
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

                    // Bolt configuration
                    NTb = NTBOptions[_nTbIndex],
                    BoltType = BoltTypeOptions[_boltTypeIndex],
                    S1 = _s1,
                    SVb = _sVb,
                    GVb = _gVb,
                    GTb = _gTb,

                    // T-Stub geometry (0 = auto)
                    TSt = _tSt,
                    TFt = _tFt,
                    BFt = _bFt,

                    // Design parameters
                    Span = _span,
                    SystemType = SystemTypeOptions[_systemTypeIndex],
                    HasSlab = _hasSlab,
                    StoryAbove = _storyAbove,
                    StoryBelow = _storyBelow,
                    Pu = _pu,
                    AsCol = _asCol,

                    // Loads
                    LoadD = _loadD,
                    LoadL = _loadL,
                    LoadS = _loadS,
                    F1 = _f1,
                };

                var result = DoubleteeCalculations.Calculate(input);

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
                BoltDiameterPassed = result.BoltDiameterPassed;
                BeamShearPassed = result.BeamShearPassed;
                ColumnBeamPassed = result.ColumnBeamPassed;
                StiffnessPassed = result.StiffnessPassed;
                ShearBoltsPassed = result.ShearBoltsPassed;
                TStemPassed = result.TStemPassed;
                TFlangePassed = result.TFlangePassed;
                GageRatioPassed = result.GageRatioPassed;
                BearingPassed = result.BearingPassed;
                BlockShearPassed = result.BlockShearPassed;
                ColumnFlangePassed = result.ColumnFlangePassed;
                ColumnWebPassed = result.ColumnWebPassed;
                PanelZonePassed = result.PanelZonePassed;
                ContinuityPlatesPassed = result.ContinuityPlatesPassed;

                // Key results
                Mpr = result.Mpr;
                Mf = result.Mf;
                Fpr = result.Fpr;
                Ff = result.Ff;
                Vh = result.Vh;

                // Ratios
                BeamShearRatio = result.BeamShearRatio;
                ShearBoltsRatio = result.ShearBoltsRatio;
                TStemRatio = result.TStemRatio;
                TFlangeRatio = result.TFlangeRatio;
                BearingRatio = result.BearingRatio;
                BlockShearRatio = result.BlockShearRatio;
                PanelZoneRatio = result.PanelZoneRatio;
                StiffnessRatio = result.StiffnessRatio;
                ColumnFlangeRatio = result.ColumnFlangeRatio;
                ColumnWebRatio = result.ColumnWebRatio;
                ColumnBeamRatio = result.ColumnBeamRatio;
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
