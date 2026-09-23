using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class StrongColumnWeakBeamViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        private int _methodIndex;
        private bool _hasResult;
        private string _processText = "";

        // Column source
        private int _columnSourceIndex = 1; // Direct Input as default
        private List<string> _columnShapeOptions = new();
        private int _selectedColumnShapeIndex;

        // Column above
        private double _zcAbove = 200;
        private double _agAbove = 30;
        private double _fycAbove = 50;
        private double _prAbove = 200;

        // Column below
        private double _zcBelow = 200;
        private double _agBelow = 30;
        private double _fycBelow = 50;
        private double _prBelow = 200;
        private bool _sameColumnBelow = true;

        // Beam source
        private int _beamSourceIndex = 1; // Direct Input as default
        private List<string> _beamShapeOptions = new();
        private int _selectedBeamShapeIndex;

        // Beam Mpr calculation parameters
        private double _beamFyb = 50;
        private double _beamRy = 1.1;
        private double _beamCpr = 1.0;
        private bool _beamAutoRy = true;

        // Beam left
        private double _mprLeft = 15000;
        private double _mvLeft = 500;

        // Beam right
        private double _mprRight = 15000;
        private double _mvRight = 500;
        private bool _sameBeamRight = true;

        // Results
        private double _ratio;
        private bool _pass;
        private string _statusText = "";
        private double _sigmaMpc;
        private double _sigmaMpb;

        #endregion

        #region Shape Database

        private readonly List<WShapeProperties> _allShapes = new();

        #endregion

        #region Constructor

        public StrongColumnWeakBeamViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            MethodOptions = new List<string> { "LRFD", "ASD" };
            SourceOptions = new List<string> { "AISC Database", "Direct Input" };

            LoadShapes();
        }

        #endregion

        #region Properties

        public List<string> MethodOptions { get; }
        public List<string> SourceOptions { get; }

        public int MethodIndex
        {
            get => _methodIndex;
            set { _methodIndex = value; OnPropertyChanged(); }
        }

        // Column source
        public int ColumnSourceIndex
        {
            get => _columnSourceIndex;
            set
            {
                _columnSourceIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsColumnDatabase));
                OnPropertyChanged(nameof(IsColumnDirect));
                if (value == 0) OnColumnShapeChanged();
            }
        }
        public bool IsColumnDatabase => _columnSourceIndex == 0;
        public bool IsColumnDirect => _columnSourceIndex == 1;

        public List<string> ColumnShapeOptions
        {
            get => _columnShapeOptions;
            set { _columnShapeOptions = value; OnPropertyChanged(); }
        }

        public int SelectedColumnShapeIndex
        {
            get => _selectedColumnShapeIndex;
            set
            {
                _selectedColumnShapeIndex = value;
                OnPropertyChanged();
                OnColumnShapeChanged();
            }
        }

        // Column above
        public double ZcAbove { get => _zcAbove; set { _zcAbove = value; OnPropertyChanged(); } }
        public double AgAbove { get => _agAbove; set { _agAbove = value; OnPropertyChanged(); } }
        public double FycAbove { get => _fycAbove; set { _fycAbove = value; OnPropertyChanged(); } }
        public double PrAbove { get => _prAbove; set { _prAbove = value; OnPropertyChanged(); } }

        // Column below
        public double ZcBelow { get => _zcBelow; set { _zcBelow = value; OnPropertyChanged(); } }
        public double AgBelow { get => _agBelow; set { _agBelow = value; OnPropertyChanged(); } }
        public double FycBelow { get => _fycBelow; set { _fycBelow = value; OnPropertyChanged(); } }
        public double PrBelow { get => _prBelow; set { _prBelow = value; OnPropertyChanged(); } }
        public bool SameColumnBelow
        {
            get => _sameColumnBelow;
            set { _sameColumnBelow = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowColumnBelow)); }
        }
        public bool ShowColumnBelow => !_sameColumnBelow;

        // Beam source
        public int BeamSourceIndex
        {
            get => _beamSourceIndex;
            set
            {
                _beamSourceIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBeamDatabase));
                OnPropertyChanged(nameof(IsBeamDirect));
                if (value == 0) UpdateBeamMpr();
            }
        }
        public bool IsBeamDatabase => _beamSourceIndex == 0;
        public bool IsBeamDirect => _beamSourceIndex == 1;

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
                OnBeamShapeChanged();
            }
        }

        // Beam Mpr calculation
        public double BeamFyb
        {
            get => _beamFyb;
            set
            {
                _beamFyb = value;
                OnPropertyChanged();
                if (_beamAutoRy) BeamRy = GetDefaultRy(value);
                UpdateBeamMpr();
            }
        }
        public double BeamRy
        {
            get => _beamRy;
            set { _beamRy = value; OnPropertyChanged(); UpdateBeamMpr(); }
        }
        public double BeamCpr
        {
            get => _beamCpr;
            set { _beamCpr = value; OnPropertyChanged(); UpdateBeamMpr(); }
        }
        public bool BeamAutoRy
        {
            get => _beamAutoRy;
            set { _beamAutoRy = value; OnPropertyChanged(); }
        }

        // Beam left
        public double MprLeft { get => _mprLeft; set { _mprLeft = value; OnPropertyChanged(); } }
        public double MvLeft { get => _mvLeft; set { _mvLeft = value; OnPropertyChanged(); } }

        // Beam right
        public double MprRight { get => _mprRight; set { _mprRight = value; OnPropertyChanged(); } }
        public double MvRight { get => _mvRight; set { _mvRight = value; OnPropertyChanged(); } }
        public bool SameBeamRight
        {
            get => _sameBeamRight;
            set { _sameBeamRight = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowBeamRight)); }
        }
        public bool ShowBeamRight => !_sameBeamRight;

        // Results
        public string ProcessText { get => _processText; set { _processText = value; OnPropertyChanged(); } }
        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }
        public double Ratio { get => _ratio; set { _ratio = value; OnPropertyChanged(); } }
        public bool Pass { get => _pass; set { _pass = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusForeground)); } }
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
        public double SigmaMpc { get => _sigmaMpc; set { _sigmaMpc = value; OnPropertyChanged(); } }
        public double SigmaMpb { get => _sigmaMpb; set { _sigmaMpb = value; OnPropertyChanged(); } }
        public string StatusForeground => _pass ? "#FF107C10" : "#FFD13438";

        #endregion

        #region Commands

        public ICommand CalculateCommand { get; }

        #endregion

        #region Shape Loading

        private void LoadShapes()
        {
            var shapes = Aisc358ShapeHelper.LoadWShapes();
            _allShapes.AddRange(shapes);

            var options = new List<string> { "-- Select --" };
            options.AddRange(_allShapes.ConvertAll(s => s.Name));

            ColumnShapeOptions = new List<string>(options);
            BeamShapeOptions = new List<string>(options);
            SelectedColumnShapeIndex = 0;
            SelectedBeamShapeIndex = 0;

            if (_allShapes.Count == 0)
            {
                ColumnSourceIndex = 1;
                BeamSourceIndex = 1;
            }
        }

        private void OnColumnShapeChanged()
        {
            if (!IsColumnDatabase) return;
            int idx = _selectedColumnShapeIndex - 1;
            if (idx < 0 || idx >= _allShapes.Count) return;

            var shape = _allShapes[idx];
            ZcAbove = shape.Zx;
            AgAbove = shape.Ag;
        }

        private void OnBeamShapeChanged()
        {
            if (!IsBeamDatabase) return;
            UpdateBeamMpr();
        }

        private void UpdateBeamMpr()
        {
            if (!IsBeamDatabase) return;
            int idx = _selectedBeamShapeIndex - 1;
            if (idx < 0 || idx >= _allShapes.Count) return;

            var shape = _allShapes[idx];
            double mpr = _beamCpr * _beamRy * _beamFyb * shape.Zx;
            MprLeft = mpr;
        }

        private static double GetDefaultRy(double fy)
        {
            if (fy <= 36) return 1.50;
            if (fy <= 65) return 1.10;
            return 1.10;
        }

        #endregion

        #region Methods

        private void Calculate()
        {
            try
            {
                var input = new StrongColumnWeakBeamCalculations.InputParameters
                {
                    Method = _methodIndex == 0
                        ? StrongColumnWeakBeamCalculations.DesignMethod.LRFD
                        : StrongColumnWeakBeamCalculations.DesignMethod.ASD,
                    ZcAbove = _zcAbove,
                    AgAbove = _agAbove,
                    FycAbove = _fycAbove,
                    PrAbove = _prAbove,
                    ZcBelow = _zcBelow,
                    AgBelow = _agBelow,
                    FycBelow = _fycBelow,
                    PrBelow = _prBelow,
                    SameColumnBelow = _sameColumnBelow,
                    MprLeft = _mprLeft,
                    MvLeft = _mvLeft,
                    MprRight = _mprRight,
                    MvRight = _mvRight,
                    SameBeamRight = _sameBeamRight
                };

                var result = StrongColumnWeakBeamCalculations.Calculate(input);

                if (!result.IsValid)
                {
                    ProcessText = result.ErrorMessage;
                    HasResult = false;
                    return;
                }

                ProcessText = string.Join(Environment.NewLine, result.Process);
                HasResult = true;
                Ratio = result.Ratio;
                Pass = result.Pass;
                SigmaMpc = result.SigmaMpc;
                SigmaMpb = result.SigmaMpb;
                StatusText = result.Pass ? "PASS - 强柱弱梁满足" : "FAIL - 强柱弱梁不满足";
            }
            catch (Exception ex)
            {
                ProcessText = $"Error: {ex.Message}";
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
