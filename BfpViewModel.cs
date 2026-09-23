using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class BfpViewModel : INotifyPropertyChanged
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

        // Beam properties (auto-filled from section or manual)
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

        // Plate geometry
        private double _bp = 9.0;
        private double _tp = 0.5;
        private double _db = 1.0;
        private int _boltGradeIndex = 0;
        private int _nBolts = 6;
        private double _s1 = 3.0;
        private double _s = 3.0;
        private double _edEdge = 2.0;

        // Plate material
        private double _plateFy = 50.0;
        private double _plateFu = 65.0;

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
        private bool _boltDiameterCheckPassed;
        private bool _boltCountCheckPassed;
        private bool _plateYieldingPassed;
        private bool _plateRupturePassed;
        private bool _blockShearPassed;
        private bool _compressionBucklingPassed;
        private bool _beamShearPassed;
        private bool _continuityPlatesPassed;
        private bool _panelZonePassed;

        // Key results
        private double _mpr;
        private double _mf;
        private double _fpr;
        private double _rn;

        // Utilization ratios
        private double _boltCountRatio;
        private double _plateYieldingRatio;
        private double _plateRuptureRatio;
        private double _blockShearRatio;
        private double _compressionBucklingRatio;
        private double _beamShearRatio;
        private double _panelZoneRatio;

        #endregion

        #region Constructor

        public BfpViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            BoltGradeOptions = new List<string> { "A490", "F2280" };
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

        // Plate geometry
        public double Bp
        {
            get => _bp;
            set { _bp = value; OnPropertyChanged(); }
        }
        public double Tp
        {
            get => _tp;
            set { _tp = value; OnPropertyChanged(); }
        }
        public double Db
        {
            get => _db;
            set { _db = value; OnPropertyChanged(); }
        }
        public int BoltGradeIndex
        {
            get => _boltGradeIndex;
            set { _boltGradeIndex = value; OnPropertyChanged(); }
        }
        public List<string> BoltGradeOptions { get; }
        public int NBolts
        {
            get => _nBolts;
            set { _nBolts = value; OnPropertyChanged(); }
        }
        public double S1
        {
            get => _s1;
            set { _s1 = value; OnPropertyChanged(); }
        }
        public double S
        {
            get => _s;
            set { _s = value; OnPropertyChanged(); }
        }
        public double EdEdge
        {
            get => _edEdge;
            set { _edEdge = value; OnPropertyChanged(); }
        }

        // Plate material
        public double PlateFy
        {
            get => _plateFy;
            set { _plateFy = value; OnPropertyChanged(); }
        }
        public double PlateFu
        {
            get => _plateFu;
            set { _plateFu = value; OnPropertyChanged(); }
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

        public bool BoltDiameterCheckPassed
        {
            get => _boltDiameterCheckPassed;
            set { _boltDiameterCheckPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BoltDiameterStatus)); }
        }
        public string BoltDiameterStatus => BoltDiameterCheckPassed ? "PASS" : "FAIL";

        public bool BoltCountCheckPassed
        {
            get => _boltCountCheckPassed;
            set { _boltCountCheckPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BoltCountStatus)); }
        }
        public string BoltCountStatus => BoltCountCheckPassed ? "PASS" : "FAIL";

        public bool PlateYieldingPassed
        {
            get => _plateYieldingPassed;
            set { _plateYieldingPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlateYieldingStatus)); }
        }
        public string PlateYieldingStatus => PlateYieldingPassed ? "PASS" : "FAIL";

        public bool PlateRupturePassed
        {
            get => _plateRupturePassed;
            set { _plateRupturePassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlateRuptureStatus)); }
        }
        public string PlateRuptureStatus => PlateRupturePassed ? "PASS" : "FAIL";

        public bool BlockShearPassed
        {
            get => _blockShearPassed;
            set { _blockShearPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BlockShearStatus)); }
        }
        public string BlockShearStatus => BlockShearPassed ? "PASS" : "FAIL";

        public bool CompressionBucklingPassed
        {
            get => _compressionBucklingPassed;
            set { _compressionBucklingPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(CompressionBucklingStatus)); }
        }
        public string CompressionBucklingStatus => CompressionBucklingPassed ? "PASS" : "FAIL";

        public bool BeamShearPassed
        {
            get => _beamShearPassed;
            set { _beamShearPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BeamShearStatus)); }
        }
        public string BeamShearStatus => BeamShearPassed ? "PASS" : "FAIL";

        public bool ContinuityPlatesPassed
        {
            get => _continuityPlatesPassed;
            set { _continuityPlatesPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ContinuityPlatesStatus)); }
        }
        public string ContinuityPlatesStatus => ContinuityPlatesPassed ? "PASS" : "FAIL";

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
        public double Fpr
        {
            get => _fpr;
            set { _fpr = value; OnPropertyChanged(); }
        }
        public double Rn
        {
            get => _rn;
            set { _rn = value; OnPropertyChanged(); }
        }

        // Utilization ratios
        public double BoltCountRatio
        {
            get => _boltCountRatio;
            set { _boltCountRatio = value; OnPropertyChanged(); }
        }
        public double PlateYieldingRatio
        {
            get => _plateYieldingRatio;
            set { _plateYieldingRatio = value; OnPropertyChanged(); }
        }
        public double PlateRuptureRatio
        {
            get => _plateRuptureRatio;
            set { _plateRuptureRatio = value; OnPropertyChanged(); }
        }
        public double BlockShearRatio
        {
            get => _blockShearRatio;
            set { _blockShearRatio = value; OnPropertyChanged(); }
        }
        public double CompressionBucklingRatio
        {
            get => _compressionBucklingRatio;
            set { _compressionBucklingRatio = value; OnPropertyChanged(); }
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
            Bp = Math.Round(shape.bf + 2.0, 2);  // Default plate width
            Tp = Math.Round(Math.Max(0.5, shape.tf * 0.75), 3); // Default plate thickness
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
                var input = new BfpCalculations.InputParameters
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

                    // Plate
                    Bp = _bp,
                    Tp = _tp,
                    Db = _db,
                    BoltGrade = BoltGradeOptions[_boltGradeIndex],
                    NBolts = _nBolts,
                    S1 = _s1,
                    S = _s,
                    EdEdge = _edEdge,

                    // Plate material
                    PlateFy = _plateFy,
                    PlateFu = _plateFu,

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

                var result = BfpCalculations.Calculate(input);

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
                BoltDiameterCheckPassed = result.BoltDiameterCheckPassed;
                BoltCountCheckPassed = result.BoltCountCheckPassed;
                PlateYieldingPassed = result.PlateYieldingPassed;
                PlateRupturePassed = result.PlateRupturePassed;
                BlockShearPassed = result.BlockShearPassed;
                CompressionBucklingPassed = result.CompressionBucklingPassed;
                BeamShearPassed = result.BeamShearPassed;
                ContinuityPlatesPassed = result.ContinuityPlatesPassed;
                PanelZonePassed = result.PanelZonePassed;

                // Key results
                Mpr = result.Mpr;
                Mf = result.Mf;
                Fpr = result.Fpr;
                Rn = result.Rn;

                // Utilization ratios
                BoltCountRatio = result.BoltCountRatio;
                PlateYieldingRatio = result.PlateYieldingRatio;
                PlateRuptureRatio = result.PlateRuptureRatio;
                BlockShearRatio = result.BlockShearRatio;
                CompressionBucklingRatio = result.CompressionBucklingRatio;
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
