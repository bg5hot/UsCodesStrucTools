using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class RbsViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        private List<WShapeProperties> _beamShapes = new();
        private List<WShapeProperties> _colShapes = new();
        private List<string> _beamShapeOptions = new();
        private List<string> _colShapeOptions = new();
        private int _selectedBeamIndex = 0;
        private int _selectedColIndex = 0;

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

        // RBS geometry
        private double _rbsA = 5.0;
        private double _rbsB = 20.0;
        private double _rbsC = 1.5;

        // Design parameters
        private double _span = 360.0;
        private int _systemTypeIndex = 0;

        // Loads
        private double _loadD = 0;
        private double _loadL = 0;
        private double _loadS = 0;
        private double _loadF1 = 0.5;

        // Beam material
        private double _beamFy = 50.0;
        private double _beamFu = 65.0;
        private double _beamRy = 1.1;

        // Column material
        private double _colFy = 50.0;
        private double _colRy = 1.1;

        // Results
        private string _processText = "";
        private bool _hasResult;
        private bool _overallPassed;
        private double _flexuralRatio;
        private double _shearRatio;
        private bool _flexuralOK;
        private bool _shearOK;
        private string _flexuralStatus = "";
        private string _shearStatus = "";

        #endregion

        #region Constructor

        public RbsViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            SystemTypeOptions = new List<string> { "SMF", "IMF" };
            LoadShapes();
        }

        #endregion

        #region Properties

        public List<string> BeamShapeOptions
        {
            get => _beamShapeOptions;
            set { _beamShapeOptions = value; OnPropertyChanged(); }
        }

        public List<string> ColShapeOptions
        {
            get => _colShapeOptions;
            set { _colShapeOptions = value; OnPropertyChanged(); }
        }

        public int SelectedBeamIndex
        {
            get => _selectedBeamIndex;
            set
            {
                _selectedBeamIndex = value;
                OnPropertyChanged();
                AutoFillBeam();
            }
        }

        public int SelectedColIndex
        {
            get => _selectedColIndex;
            set
            {
                _selectedColIndex = value;
                OnPropertyChanged();
                AutoFillCol();
            }
        }

        // Beam section dimensions (editable)
        public string BeamName { get => _beamName; set { _beamName = value; OnPropertyChanged(); } }
        public double BeamD { get => _beamD; set { _beamD = value; OnPropertyChanged(); } }
        public double BeamBf { get => _beamBf; set { _beamBf = value; OnPropertyChanged(); } }
        public double BeamTf { get => _beamTf; set { _beamTf = value; OnPropertyChanged(); } }
        public double BeamTw { get => _beamTw; set { _beamTw = value; OnPropertyChanged(); } }
        public double BeamZx { get => _beamZx; set { _beamZx = value; OnPropertyChanged(); } }

        // Column section dimensions (editable)
        public string ColName { get => _colName; set { _colName = value; OnPropertyChanged(); } }
        public double ColD { get => _colD; set { _colD = value; OnPropertyChanged(); } }
        public double ColBf { get => _colBf; set { _colBf = value; OnPropertyChanged(); } }
        public double ColTf { get => _colTf; set { _colTf = value; OnPropertyChanged(); } }
        public double ColTw { get => _colTw; set { _colTw = value; OnPropertyChanged(); } }
        public double ColZx { get => _colZx; set { _colZx = value; OnPropertyChanged(); } }

        public List<string> SystemTypeOptions { get; }

        public int SystemTypeIndex
        {
            get => _systemTypeIndex;
            set { _systemTypeIndex = value; OnPropertyChanged(); }
        }

        public double RbsA { get => _rbsA; set { _rbsA = value; OnPropertyChanged(); } }
        public double RbsB { get => _rbsB; set { _rbsB = value; OnPropertyChanged(); } }
        public double RbsC { get => _rbsC; set { _rbsC = value; OnPropertyChanged(); } }

        public double Span { get => _span; set { _span = value; OnPropertyChanged(); } }

        public double LoadD { get => _loadD; set { _loadD = value; OnPropertyChanged(); } }
        public double LoadL { get => _loadL; set { _loadL = value; OnPropertyChanged(); } }
        public double LoadS { get => _loadS; set { _loadS = value; OnPropertyChanged(); } }
        public double LoadF1 { get => _loadF1; set { _loadF1 = value; OnPropertyChanged(); } }

        public double BeamFy { get => _beamFy; set { _beamFy = value; OnPropertyChanged(); } }
        public double BeamFu { get => _beamFu; set { _beamFu = value; OnPropertyChanged(); } }
        public double BeamRy { get => _beamRy; set { _beamRy = value; OnPropertyChanged(); } }

        public double ColFy { get => _colFy; set { _colFy = value; OnPropertyChanged(); } }
        public double ColRy { get => _colRy; set { _colRy = value; OnPropertyChanged(); } }

        public string ProcessText { get => _processText; set { _processText = value; OnPropertyChanged(); } }
        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }
        public bool OverallPassed { get => _overallPassed; set { _overallPassed = value; OnPropertyChanged(); } }
        public double FlexuralRatio { get => _flexuralRatio; set { _flexuralRatio = value; OnPropertyChanged(); } }
        public double ShearRatio { get => _shearRatio; set { _shearRatio = value; OnPropertyChanged(); } }
        public bool FlexuralOK { get => _flexuralOK; set { _flexuralOK = value; OnPropertyChanged(); } }
        public bool ShearOK { get => _shearOK; set { _shearOK = value; OnPropertyChanged(); } }
        public string FlexuralStatus { get => _flexuralStatus; set { _flexuralStatus = value; OnPropertyChanged(); } }
        public string ShearStatus { get => _shearStatus; set { _shearStatus = value; OnPropertyChanged(); } }

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

                SelectedBeamIndex = 0;
                SelectedColIndex = 0;
            }
            catch (Exception)
            {
                BeamShapeOptions = new List<string> { "(Database file not found)" };
                ColShapeOptions = new List<string> { "(Database file not found)" };
            }
        }

        private void AutoFillBeam()
        {
            int idx = _selectedBeamIndex - 1;
            if (idx >= 0 && idx < _beamShapes.Count)
            {
                var s = _beamShapes[idx];
                BeamName = s.Name; BeamD = s.d; BeamBf = s.bf;
                BeamTf = s.tf; BeamTw = s.tw; BeamZx = s.Zx;
            }
        }

        private void AutoFillCol()
        {
            int idx = _selectedColIndex - 1;
            if (idx >= 0 && idx < _colShapes.Count)
            {
                var s = _colShapes[idx];
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

                var input = new RbsCalculations.InputParameters
                {
                    BeamD = _beamD, BeamBf = _beamBf, BeamTf = _beamTf,
                    BeamTw = _beamTw, BeamZx = _beamZx, BeamName = _beamName,
                    BeamFy = _beamFy, BeamFu = _beamFu, BeamRy = _beamRy,
                    ColD = _colD, ColBf = _colBf, ColTf = _colTf,
                    ColTw = _colTw, ColZx = _colZx, ColName = _colName,
                    ColFy = _colFy, ColRy = _colRy,
                    RbsA = _rbsA, RbsB = _rbsB, RbsC = _rbsC,
                    Span = _span,
                    SystemType = SystemTypeOptions[_systemTypeIndex],
                    LoadD = _loadD, LoadL = _loadL, LoadS = _loadS, LoadF1 = _loadF1
                };

                var result = RbsCalculations.Calculate(input);

                if (!result.IsValid)
                {
                    ProcessText = result.ErrorMessage;
                    return;
                }

                ProcessText = string.Join(Environment.NewLine, result.Process);
                OverallPassed = result.OverallPassed;
                FlexuralRatio = result.FlexuralRatio;
                ShearRatio = result.ShearRatio;
                FlexuralOK = result.FlexuralOK;
                ShearOK = result.ShearOK;
                FlexuralStatus = result.FlexuralStatus;
                ShearStatus = result.ShearStatus;
                HasResult = true;
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
