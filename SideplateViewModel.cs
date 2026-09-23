using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class SideplateViewModel : INotifyPropertyChanged
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
        private double _beamD = 36.0;
        private double _beamBf = 12.0;
        private double _beamTf = 0.94;
        private double _beamTw = 0.625;
        private double _beamZx = 580.0;

        // Column properties
        private string _colDesignation = "";
        private double _colD = 17.2;
        private double _colBf = 16.0;
        private double _colTf = 1.68;
        private double _colTw = 0.96;
        private double _colZx = 600.0;

        // Beam material
        private double _beamFy = 50.0;
        private double _beamFu = 65.0;
        private double _beamRy = 1.1;

        // Column material
        private double _colFy = 50.0;
        private double _colFu = 65.0;
        private double _colRy = 1.1;

        // Connection type
        private int _connectionTypeIndex = 0;

        // Design parameters
        private double _span = 360.0;
        private int _systemTypeIndex = 0;
        private double _extensionA = 0.0;
        private double _storyHeight = 168.0;
        private double _colD2 = 0.0;

        // Loads
        private double _loadD = 0.0;
        private double _loadL = 0.0;
        private double _loadS = 0.0;
        private double _f1 = 0.5;

        // Column axial load
        private double _puCol = 0.0;
        private double _asCol = 0.0;

        // Results
        private string _processText = "";
        private bool _hasResult;
        private bool _overallPassed;
        private string _overallStatus = "";
        private string _overallStatusColor = "Green";

        // Individual check results
        private bool _prequalificationPassed;
        private bool _beamPrequalificationPassed;
        private bool _columnPrequalificationPassed;
        private bool _extensionRangePassed;
        private bool _lhRatioPassed;
        private bool _scwbRatioPassed;
        private bool _beamShearPassed;
        private bool _panelZonePassed;

        // Key results
        private double _cpr;
        private double _mpr;
        private double _mf;
        private double _mcl;
        private double _vh;
        private double _sh;
        private double _lh;
        private double _extensionAResult;

        // Utilization ratios
        private double _beamShearRatio;
        private double _panelZoneRatio;
        private double _geometricCompatRatio;

        #endregion

        #region Constructor

        public SideplateViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            ConnectionTypeOptions = new List<string> { "welded", "bolted" };
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

        // Connection type
        public int ConnectionTypeIndex
        {
            get => _connectionTypeIndex;
            set { _connectionTypeIndex = value; OnPropertyChanged(); }
        }
        public List<string> ConnectionTypeOptions { get; }

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
        public double ExtensionA
        {
            get => _extensionA;
            set { _extensionA = value; OnPropertyChanged(); }
        }
        public double StoryHeight
        {
            get => _storyHeight;
            set { _storyHeight = value; OnPropertyChanged(); }
        }
        public double ColD2
        {
            get => _colD2;
            set { _colD2 = value; OnPropertyChanged(); }
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

        // Column axial load
        public double PuCol
        {
            get => _puCol;
            set { _puCol = value; OnPropertyChanged(); }
        }
        public double AsCol
        {
            get => _asCol;
            set { _asCol = value; OnPropertyChanged(); }
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

        public bool BeamPrequalificationPassed
        {
            get => _beamPrequalificationPassed;
            set { _beamPrequalificationPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(BeamPrequalificationStatus)); }
        }
        public string BeamPrequalificationStatus => BeamPrequalificationPassed ? "PASS" : "FAIL";

        public bool ColumnPrequalificationPassed
        {
            get => _columnPrequalificationPassed;
            set { _columnPrequalificationPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColumnPrequalificationStatus)); }
        }
        public string ColumnPrequalificationStatus => ColumnPrequalificationPassed ? "PASS" : "FAIL";

        public bool ExtensionRangePassed
        {
            get => _extensionRangePassed;
            set { _extensionRangePassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ExtensionRangeStatus)); }
        }
        public string ExtensionRangeStatus => ExtensionRangePassed ? "PASS" : "FAIL";

        public bool LhRatioPassed
        {
            get => _lhRatioPassed;
            set { _lhRatioPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(LhRatioStatus)); }
        }
        public string LhRatioStatus => LhRatioPassed ? "PASS" : "FAIL";

        public bool ScwbRatioPassed
        {
            get => _scwbRatioPassed;
            set { _scwbRatioPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScwbRatioStatus)); }
        }
        public string ScwbRatioStatus => ScwbRatioPassed ? "PASS" : "FAIL";

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
        public double Cpr
        {
            get => _cpr;
            set { _cpr = value; OnPropertyChanged(); }
        }
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
        public double Mcl
        {
            get => _mcl;
            set { _mcl = value; OnPropertyChanged(); }
        }
        public double Vh
        {
            get => _vh;
            set { _vh = value; OnPropertyChanged(); }
        }
        public double Sh
        {
            get => _sh;
            set { _sh = value; OnPropertyChanged(); }
        }
        public double Lh
        {
            get => _lh;
            set { _lh = value; OnPropertyChanged(); }
        }
        public double ExtensionAResult
        {
            get => _extensionAResult;
            set { _extensionAResult = value; OnPropertyChanged(); }
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
        public double GeometricCompatRatio
        {
            get => _geometricCompatRatio;
            set { _geometricCompatRatio = value; OnPropertyChanged(); }
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
                var input = new SideplateCalculations.InputParameters
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

                    // Connection
                    ConnectionType = ConnectionTypeOptions[_connectionTypeIndex],

                    // Design
                    Span = _span,
                    SystemType = SystemTypeOptions[_systemTypeIndex],
                    ExtensionA = _extensionA,
                    StoryHeight = _storyHeight,
                    ColD2 = _colD2,

                    // Loads
                    LoadD = _loadD,
                    LoadL = _loadL,
                    LoadS = _loadS,
                    F1 = _f1,

                    // Column axial
                    PuCol = _puCol,
                    AsCol = _asCol,
                };

                var result = SideplateCalculations.Calculate(input);

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
                BeamPrequalificationPassed = result.BeamPrequalificationPassed;
                ColumnPrequalificationPassed = result.ColumnPrequalificationPassed;
                ExtensionRangePassed = result.ExtensionRangePassed;
                LhRatioPassed = result.LhRatioPassed;
                ScwbRatioPassed = result.ScwbRatioPassed;
                BeamShearPassed = result.BeamShearPassed;
                PanelZonePassed = result.PanelZonePassed;

                // Key results
                Cpr = result.Cpr;
                Mpr = result.Mpr;
                Mf = result.Mf;
                Mcl = result.Mcl;
                Vh = result.Vh;
                Sh = result.Sh;
                Lh = result.Lh;
                ExtensionAResult = result.ExtensionA;

                // Utilization ratios
                BeamShearRatio = result.BeamShearRatio;
                PanelZoneRatio = result.PanelZoneRatio;
                GeometricCompatRatio = result.GeometricCompatRatio;
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
