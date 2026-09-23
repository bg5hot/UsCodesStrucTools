using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class SstViewModel : INotifyPropertyChanged
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

        // Yield-Link geometry
        private double _tStem = 0.75;
        private double _bColSide = 0.0;
        private double _bBmSide = 0.0;
        private double _bYield = 0.0;
        private double _lColSide = 0.0;
        private double _lBmSide = 0.0;
        private double _lYLink = 0.0;

        // Connection type
        private int _linkTypeIndex = 0;
        private double _aDist = 3.0;

        // Yield-Link material
        private double _linkFy = 50.0;
        private double _linkFu = 65.0;

        // Weld electrode
        private double _fexx = 70.0;

        // Design parameters
        private double _span = 300.0;
        private int _systemTypeIndex = 0;
        private double _storyAbove = 156.0;
        private double _storyBelow = 156.0;

        // Loads
        private double _loadD = 0.0;
        private double _loadL = 0.0;
        private double _loadS = 0.0;
        private double _f1 = 0.5;
        private double _vu = 0.0;
        private double _mu = 3500.0;
        private double _puSp = 0.0;

        // Results
        private string _processText = "";
        private bool _hasResult;
        private bool _overallPassed;
        private string _overallStatus = "";
        private string _overallStatusColor = "Green";

        // Individual check results
        private bool _prequalificationPassed;
        private bool _flangeConnectionPassed;
        private bool _bucklingRestraintPassed;
        private bool _stiffnessPassed;
        private bool _beamShearPassed;
        private bool _columnBeamPassed;
        private bool _shearPlatePassed;
        private bool _panelZonePassed;
        private bool _columnWebPassed;
        private bool _columnFlangePassed;
        private bool _continuityPlatesPassed;

        // Key results
        private double _mpr;
        private double _prLink;
        private double _vuCalc;
        private double _kEff;
        private double _thetaY;

        // Utilization ratios
        private double _beamShearRatio;
        private double _panelZoneRatio;
        private double _stiffnessRatio;

        #endregion

        #region Constructor

        public SstViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            LinkTypeOptions = new List<string> { "tstub", "endplate" };
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

        // Yield-Link geometry
        public double TStem
        {
            get => _tStem;
            set { _tStem = value; OnPropertyChanged(); }
        }
        public double BColSide
        {
            get => _bColSide;
            set { _bColSide = value; OnPropertyChanged(); }
        }
        public double BBmSide
        {
            get => _bBmSide;
            set { _bBmSide = value; OnPropertyChanged(); }
        }
        public double BYield
        {
            get => _bYield;
            set { _bYield = value; OnPropertyChanged(); }
        }
        public double LColSide
        {
            get => _lColSide;
            set { _lColSide = value; OnPropertyChanged(); }
        }
        public double LBmSide
        {
            get => _lBmSide;
            set { _lBmSide = value; OnPropertyChanged(); }
        }
        public double LYLink
        {
            get => _lYLink;
            set { _lYLink = value; OnPropertyChanged(); }
        }

        // Connection type
        public int LinkTypeIndex
        {
            get => _linkTypeIndex;
            set { _linkTypeIndex = value; OnPropertyChanged(); }
        }
        public List<string> LinkTypeOptions { get; }
        public double ADist
        {
            get => _aDist;
            set { _aDist = value; OnPropertyChanged(); }
        }

        // Yield-Link material
        public double LinkFy
        {
            get => _linkFy;
            set { _linkFy = value; OnPropertyChanged(); }
        }
        public double LinkFu
        {
            get => _linkFu;
            set { _linkFu = value; OnPropertyChanged(); }
        }

        // Weld electrode
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
        public double Mu
        {
            get => _mu;
            set { _mu = value; OnPropertyChanged(); }
        }
        public double PuSp
        {
            get => _puSp;
            set { _puSp = value; OnPropertyChanged(); }
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

        public bool FlangeConnectionPassed
        {
            get => _flangeConnectionPassed;
            set { _flangeConnectionPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(FlangeConnectionStatus)); }
        }
        public string FlangeConnectionStatus => FlangeConnectionPassed ? "PASS" : "FAIL";

        public bool BucklingRestraintPassed
        {
            get => _bucklingRestraintPassed;
            set { _bucklingRestraintPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BucklingRestraintStatus)); }
        }
        public string BucklingRestraintStatus => BucklingRestraintPassed ? "PASS" : "FAIL";

        public bool StiffnessPassed
        {
            get => _stiffnessPassed;
            set { _stiffnessPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(StiffnessStatus)); }
        }
        public string StiffnessStatus => StiffnessPassed ? "PASS" : "FAIL";

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

        public bool ShearPlatePassed
        {
            get => _shearPlatePassed;
            set { _shearPlatePassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShearPlateStatus)); }
        }
        public string ShearPlateStatus => ShearPlatePassed ? "PASS" : "FAIL";

        public bool PanelZonePassed
        {
            get => _panelZonePassed;
            set { _panelZonePassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(PanelZoneStatus)); }
        }
        public string PanelZoneStatus => PanelZonePassed ? "PASS" : "FAIL";

        public bool ColumnWebPassed
        {
            get => _columnWebPassed;
            set { _columnWebPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColumnWebStatus)); }
        }
        public string ColumnWebStatus => ColumnWebPassed ? "PASS" : "FAIL";

        public bool ColumnFlangePassed
        {
            get => _columnFlangePassed;
            set { _columnFlangePassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColumnFlangeStatus)); }
        }
        public string ColumnFlangeStatus => ColumnFlangePassed ? "PASS" : "FAIL";

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
        public double PrLink
        {
            get => _prLink;
            set { _prLink = value; OnPropertyChanged(); }
        }
        public double VuCalc
        {
            get => _vuCalc;
            set { _vuCalc = value; OnPropertyChanged(); }
        }
        public double KEff
        {
            get => _kEff;
            set { _kEff = value; OnPropertyChanged(); }
        }
        public double ThetaY
        {
            get => _thetaY;
            set { _thetaY = value; OnPropertyChanged(); }
        }

        // Utilization ratios
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
        public double StiffnessRatio
        {
            get => _stiffnessRatio;
            set { _stiffnessRatio = value; OnPropertyChanged(); }
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
                var input = new SstCalculations.InputParameters
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

                    // Yield-Link geometry
                    TStem = _tStem,
                    BColSide = _bColSide,
                    BBmSide = _bBmSide,
                    BYield = _bYield,
                    LColSide = _lColSide,
                    LBmSide = _lBmSide,
                    LYLink = _lYLink,

                    // Connection type
                    LinkType = LinkTypeOptions[_linkTypeIndex],
                    ADist = _aDist,

                    // Yield-Link material
                    LinkFy = _linkFy,
                    LinkFu = _linkFu,

                    // Weld electrode
                    FEXX = _fexx,

                    // Design
                    Span = _span,
                    SystemType = SystemTypeOptions[_systemTypeIndex],
                    StoryAbove = _storyAbove,
                    StoryBelow = _storyBelow,

                    // Loads
                    LoadD = _loadD,
                    LoadL = _loadL,
                    LoadS = _loadS,
                    F1 = _f1,
                    Vu = _vu,
                    Mu = _mu,
                    PuSp = _puSp,
                };

                var result = SstCalculations.Calculate(input);

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
                FlangeConnectionPassed = result.FlangeConnectionPassed;
                BucklingRestraintPassed = result.BucklingRestraintPassed;
                StiffnessPassed = result.StiffnessPassed;
                BeamShearPassed = result.BeamShearPassed;
                ColumnBeamPassed = result.ColumnBeamPassed;
                ShearPlatePassed = result.ShearPlatePassed;
                PanelZonePassed = result.PanelZonePassed;
                ColumnWebPassed = result.ColumnWebPassed;
                ColumnFlangePassed = result.ColumnFlangePassed;
                ContinuityPlatesPassed = result.ContinuityPlatesPassed;

                // Key results
                Mpr = result.Mpr;
                PrLink = result.PrLink;
                VuCalc = result.VuCalc;
                KEff = result.KEff;
                ThetaY = result.ThetaY;

                // Utilization ratios
                BeamShearRatio = result.BeamShearRatio;
                PanelZoneRatio = result.PanelZoneRatio;
                StiffnessRatio = result.StiffnessRatio;
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
