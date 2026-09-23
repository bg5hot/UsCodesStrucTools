using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class SlottedwebViewModel : INotifyPropertyChanged
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
        private double _beamD = 24.1;
        private double _beamBf = 9.07;
        private double _beamTf = 0.875;
        private double _beamTw = 0.515;
        private double _beamZx = 289.0;
        private double _beamT = 0.0;

        // Column properties
        private string _colDesignation = "";
        private double _colD = 17.0;
        private double _colBf = 16.0;
        private double _colTf = 1.72;
        private double _colTw = 1.03;
        private double _colZx = 683.0;

        // Beam material
        private double _beamFy = 50.0;
        private double _beamFu = 65.0;
        private double _beamRy = 1.1;
        private double _beamRt = 1.1;

        // Column material
        private double _colFy = 50.0;

        // Shear plate
        private double _lpOverride = 0.0;
        private double _plateFy = 50.0;

        // Design parameters
        private double _span = 360.0;
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
        private bool _shearPlatePassed;
        private bool _beamShearPassed;
        private bool _continuityPlatesPassed;
        private bool _panelZonePassed;
        private bool _columnBeamRatioPassed;

        // Key results
        private double _mpr;
        private double _mf;
        private double _vbeam;
        private double _ls;
        private double _lp;
        private double _hPlate;
        private double _tp;
        private double _mweld;
        private double _vweld;
        private double _ex;
        private double _lb;

        // Utilization ratios
        private double _beamShearRatio;
        private double _panelZoneRatio;
        private double _columnBeamRatio;

        #endregion

        #region Constructor

        public SlottedwebViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
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
        public double BeamT
        {
            get => _beamT;
            set { _beamT = value; OnPropertyChanged(); }
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

        // Shear plate
        public double LpOverride
        {
            get => _lpOverride;
            set { _lpOverride = value; OnPropertyChanged(); }
        }
        public double PlateFy
        {
            get => _plateFy;
            set { _plateFy = value; OnPropertyChanged(); }
        }

        // Design parameters
        public double Span
        {
            get => _span;
            set { _span = value; OnPropertyChanged(); }
        }
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

        public bool ShearPlatePassed
        {
            get => _shearPlatePassed;
            set { _shearPlatePassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShearPlateStatus)); }
        }
        public string ShearPlateStatus => ShearPlatePassed ? "PASS" : "FAIL";

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

        public bool ColumnBeamRatioPassed
        {
            get => _columnBeamRatioPassed;
            set { _columnBeamRatioPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColumnBeamRatioStatus)); }
        }
        public string ColumnBeamRatioStatus => ColumnBeamRatioPassed ? "PASS" : "FAIL";

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
        public double Vbeam
        {
            get => _vbeam;
            set { _vbeam = value; OnPropertyChanged(); }
        }
        public double Ls
        {
            get => _ls;
            set { _ls = value; OnPropertyChanged(); }
        }
        public double Lp
        {
            get => _lp;
            set { _lp = value; OnPropertyChanged(); }
        }
        public double HPlate
        {
            get => _hPlate;
            set { _hPlate = value; OnPropertyChanged(); }
        }
        public double Tp
        {
            get => _tp;
            set { _tp = value; OnPropertyChanged(); }
        }
        public double Mweld
        {
            get => _mweld;
            set { _mweld = value; OnPropertyChanged(); }
        }
        public double Vweld
        {
            get => _vweld;
            set { _vweld = value; OnPropertyChanged(); }
        }
        public double Ex
        {
            get => _ex;
            set { _ex = value; OnPropertyChanged(); }
        }
        public double Lb
        {
            get => _lb;
            set { _lb = value; OnPropertyChanged(); }
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
            // Default T to d - 2*tf (conservative approximation)
            BeamT = 0.0;
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
                var input = new SlottedwebCalculations.InputParameters
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
                    BeamT = _beamT,

                    // Column
                    ColDesignation = _colDesignation,
                    ColD = _colD,
                    ColBf = _colBf,
                    ColTf = _colTf,
                    ColTw = _colTw,
                    ColZx = _colZx,
                    ColFy = _colFy,

                    // Shear plate
                    LpOverride = _lpOverride,
                    PlateFy = _plateFy,

                    // Design parameters
                    Span = _span,
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

                var result = SlottedwebCalculations.Calculate(input);

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
                ShearPlatePassed = result.ShearPlatePassed;
                BeamShearPassed = result.BeamShearPassed;
                ContinuityPlatesPassed = result.ContinuityPlatesPassed;
                PanelZonePassed = result.PanelZonePassed;
                ColumnBeamRatioPassed = result.ColumnBeamRatioPassed;

                // Key results
                Mpr = result.Mpr;
                Mf = result.Mf;
                Vbeam = result.Vbeam;
                Ls = result.Ls;
                Lp = result.Lp;
                HPlate = result.H;
                Tp = result.Tp;
                Mweld = result.Mweld;
                Vweld = result.Vweld;
                Ex = result.Ex;
                Lb = result.Lb;

                // Utilization ratios
                BeamShearRatio = result.BeamShearRatio;
                PanelZoneRatio = result.PanelZoneRatio;
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
