using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class WufwViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        // Shape selection
        private List<string> _beamShapeOptions = new();
        private int _selectedBeamShapeIndex = -1;
        private List<string> _colShapeOptions = new();
        private int _selectedColShapeIndex = -1;

        // Design parameters
        private double _span = 300.0;
        private int _systemTypeIndex = 0;

        // Loads
        private double _loadD = 0;
        private double _loadL = 0;
        private double _loadS = 0;
        private double _loadF1 = 0.5;
        private double _loadVu = 0;

        // Material: Beam
        private double _beamFy = 50.0;
        private double _beamFu = 65.0;
        private double _beamRy = 1.1;

        // Material: Column
        private double _colFy = 50.0;
        private double _colRy = 1.1;

        // Results
        private string _processText = "";
        private bool _hasResult;
        private bool _overallPassed;
        private string _overallStatus = "";
        private double _mpr;
        private double _vh;
        private double _shearRatio;
        private double _columnBeamRatio;
        private double _panelZoneRatio;

        private bool _prequalificationPassed;
        private bool _columnBeamPassed;
        private bool _beamShearPassed;
        private bool _continuityPlatesPassed;
        private bool _panelZonePassed;
        private bool _connectionDetailsPassed;

        // Status strings for XAML binding
        private string _prequalificationStatus = "";
        private string _columnBeamStatus = "";
        private string _beamShearStatus = "";
        private string _continuityPlatesStatus = "";
        private string _panelZoneStatus = "";

        // Cached shape data
        private List<WShapeProperties> _beamShapes = new();
        private List<WShapeProperties> _colShapes = new();

        // Custom beam section fields
        private string _beamName = "";
        private double _beamD = 24.0;
        private double _beamBf = 9.0;
        private double _beamTf = 0.5;
        private double _beamTw = 0.3;
        private double _beamZx = 200.0;

        // Custom column section fields
        private string _colName = "";
        private double _colD = 14.0;
        private double _colBf = 15.0;
        private double _colTf = 1.0;
        private double _colTw = 0.5;
        private double _colZx = 400.0;

        #endregion

        #region Constructor

        public WufwViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            SystemTypeOptions = new List<string> { "SMF", "IMF" };
            LoadShapes();
        }

        #endregion

        #region Properties

        public List<string> SystemTypeOptions { get; }

        // Beam shape
        public List<string> BeamShapeOptions
        {
            get => _beamShapeOptions;
            set { _beamShapeOptions = value; OnPropertyChanged(); }
        }

        public int SelectedBeamShapeIndex
        {
            get => _selectedBeamShapeIndex;
            set { _selectedBeamShapeIndex = value; OnPropertyChanged(); AutoFillBeam(); }
        }

        // Beam section dimensions (editable)
        public string BeamName { get => _beamName; set { _beamName = value; OnPropertyChanged(); } }
        public double BeamD { get => _beamD; set { _beamD = value; OnPropertyChanged(); } }
        public double BeamBf { get => _beamBf; set { _beamBf = value; OnPropertyChanged(); } }
        public double BeamTf { get => _beamTf; set { _beamTf = value; OnPropertyChanged(); } }
        public double BeamTw { get => _beamTw; set { _beamTw = value; OnPropertyChanged(); } }
        public double BeamZx { get => _beamZx; set { _beamZx = value; OnPropertyChanged(); } }

        // Column shape
        public List<string> ColShapeOptions
        {
            get => _colShapeOptions;
            set { _colShapeOptions = value; OnPropertyChanged(); }
        }

        public int SelectedColShapeIndex
        {
            get => _selectedColShapeIndex;
            set { _selectedColShapeIndex = value; OnPropertyChanged(); AutoFillCol(); }
        }

        // Column section dimensions (editable)
        public string ColName { get => _colName; set { _colName = value; OnPropertyChanged(); } }
        public double ColD { get => _colD; set { _colD = value; OnPropertyChanged(); } }
        public double ColBf { get => _colBf; set { _colBf = value; OnPropertyChanged(); } }
        public double ColTf { get => _colTf; set { _colTf = value; OnPropertyChanged(); } }
        public double ColTw { get => _colTw; set { _colTw = value; OnPropertyChanged(); } }
        public double ColZx { get => _colZx; set { _colZx = value; OnPropertyChanged(); } }

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

        public double LoadF1
        {
            get => _loadF1;
            set { _loadF1 = value; OnPropertyChanged(); }
        }

        public double LoadVu
        {
            get => _loadVu;
            set { _loadVu = value; OnPropertyChanged(); }
        }

        // Material: Beam
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

        // Material: Column
        public double ColFy
        {
            get => _colFy;
            set { _colFy = value; OnPropertyChanged(); }
        }

        public double ColRy
        {
            get => _colRy;
            set { _colRy = value; OnPropertyChanged(); }
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

        public double ShearRatio
        {
            get => _shearRatio;
            set { _shearRatio = value; OnPropertyChanged(); }
        }

        public double ColumnBeamRatio
        {
            get => _columnBeamRatio;
            set { _columnBeamRatio = value; OnPropertyChanged(); }
        }

        public double PanelZoneRatio
        {
            get => _panelZoneRatio;
            set { _panelZoneRatio = value; OnPropertyChanged(); }
        }

        public bool PrequalificationPassed
        {
            get => _prequalificationPassed;
            set { _prequalificationPassed = value; OnPropertyChanged(); }
        }

        public bool ColumnBeamPassed
        {
            get => _columnBeamPassed;
            set { _columnBeamPassed = value; OnPropertyChanged(); }
        }

        public bool BeamShearPassed
        {
            get => _beamShearPassed;
            set { _beamShearPassed = value; OnPropertyChanged(); }
        }

        public bool ContinuityPlatesPassed
        {
            get => _continuityPlatesPassed;
            set { _continuityPlatesPassed = value; OnPropertyChanged(); }
        }

        public bool PanelZonePassed
        {
            get => _panelZonePassed;
            set { _panelZonePassed = value; OnPropertyChanged(); }
        }

        public bool ConnectionDetailsPassed
        {
            get => _connectionDetailsPassed;
            set { _connectionDetailsPassed = value; OnPropertyChanged(); }
        }

        public string PrequalificationStatus
        {
            get => _prequalificationStatus;
            set { _prequalificationStatus = value; OnPropertyChanged(); }
        }

        public string ColumnBeamStatus
        {
            get => _columnBeamStatus;
            set { _columnBeamStatus = value; OnPropertyChanged(); }
        }

        public string BeamShearStatus
        {
            get => _beamShearStatus;
            set { _beamShearStatus = value; OnPropertyChanged(); }
        }

        public string ContinuityPlatesStatus
        {
            get => _continuityPlatesStatus;
            set { _continuityPlatesStatus = value; OnPropertyChanged(); }
        }

        public string PanelZoneStatus
        {
            get => _panelZoneStatus;
            set { _panelZoneStatus = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand CalculateCommand { get; }

        #endregion

        #region Methods

        private void LoadShapes()
        {
            try
            {
                var shapes = Aisc358ShapeHelper.LoadWShapes();
                _beamShapes = shapes;
                _colShapes = shapes;

                BeamShapeOptions = new List<string> { "-- Custom --" }
                    .Concat(shapes.Select(s => s.Name))
                    .ToList();
                ColShapeOptions = new List<string> { "-- Custom --" }
                    .Concat(shapes.Select(s => s.Name))
                    .ToList();

                SelectedBeamShapeIndex = 0;
                SelectedColShapeIndex = 0;
            }
            catch
            {
                BeamShapeOptions = new List<string> { "(Database file not found)" };
                ColShapeOptions = new List<string> { "(Database file not found)" };
            }
        }

        private WShapeProperties? GetBeamShape()
        {
            int idx = _selectedBeamShapeIndex - 1;
            if (idx >= 0 && idx < _beamShapes.Count) return _beamShapes[idx];
            return null;
        }

        private WShapeProperties? GetColShape()
        {
            int idx = _selectedColShapeIndex - 1;
            if (idx >= 0 && idx < _colShapes.Count) return _colShapes[idx];
            return null;
        }

        private void AutoFillBeam()
        {
            var s = GetBeamShape();
            if (s != null)
            {
                BeamName = s.Name; BeamD = s.d; BeamBf = s.bf;
                BeamTf = s.tf; BeamTw = s.tw; BeamZx = s.Zx;
            }
        }

        private void AutoFillCol()
        {
            var s = GetColShape();
            if (s != null)
            {
                ColName = s.Name; ColD = s.d; ColBf = s.bf;
                ColTf = s.tf; ColTw = s.tw; ColZx = s.Zx;
            }
        }

        private void Calculate()
        {
            try
            {
                if (_beamD <= 0 || _beamBf <= 0 || _beamTf <= 0 || _beamTw <= 0 || _beamZx <= 0)
                {
                    ProcessText = "Please select or enter valid beam section properties.";
                    return;
                }
                if (_colD <= 0 || _colBf <= 0 || _colTf <= 0 || _colTw <= 0 || _colZx <= 0)
                {
                    ProcessText = "Please select or enter valid column section properties.";
                    return;
                }

                var input = new WufwCalculations.InputParameters
                {
                    BeamD = _beamD,
                    BeamBf = _beamBf,
                    BeamTf = _beamTf,
                    BeamTw = _beamTw,
                    BeamZx = _beamZx,
                    BeamFy = _beamFy,
                    BeamFu = _beamFu,
                    BeamRy = _beamRy,
                    BeamName = _beamName,

                    ColD = _colD,
                    ColBf = _colBf,
                    ColTf = _colTf,
                    ColTw = _colTw,
                    ColZx = _colZx,
                    ColFy = _colFy,
                    ColRy = _colRy,
                    ColName = _colName,

                    Span = _span,
                    SystemType = SystemTypeOptions[_systemTypeIndex],

                    D = _loadD,
                    L = _loadL,
                    S = _loadS,
                    f1 = _loadF1,
                    Vu = _loadVu
                };

                var result = WufwCalculations.Calculate(input);

                if (!result.IsValid)
                {
                    ProcessText = result.ErrorMessage;
                    return;
                }

                ProcessText = string.Join(Environment.NewLine, result.Process);
                HasResult = true;
                OverallPassed = result.OverallPassed;
                OverallStatus = result.OverallPassed ? "ALL CHECKS PASSED" : "SOME CHECKS FAILED";
                Mpr = result.Mpr;
                Vh = result.Vh;
                ShearRatio = result.ShearRatio;
                ColumnBeamRatio = result.ColumnBeamRatio;
                PanelZoneRatio = result.PanelZoneRatio;

                PrequalificationPassed = result.PrequalificationPassed;
                ColumnBeamPassed = result.ColumnBeamPassed;
                BeamShearPassed = result.BeamShearPassed;
                ContinuityPlatesPassed = result.ContinuityPlatesPassed;
                PanelZonePassed = result.PanelZonePassed;
                ConnectionDetailsPassed = result.ConnectionDetailsPassed;

                PrequalificationStatus = result.PrequalificationPassed ? "PASS" : "FAIL";
                ColumnBeamStatus = result.ColumnBeamPassed ? "PASS" : "FAIL";
                BeamShearStatus = result.BeamShearPassed ? "PASS" : "FAIL";
                ContinuityPlatesStatus = result.ContinuityPlatesPassed ? "PASS" : "FAIL";
                PanelZoneStatus = result.PanelZonePassed ? "PASS" : "FAIL";
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
