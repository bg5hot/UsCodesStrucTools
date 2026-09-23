using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class EndplateViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        // Beam section
        private List<string> _beamShapeOptions = new();
        private int _selectedBeamShapeIndex = -1;
        private double _beamD;
        private double _beamBf;
        private double _beamTf;
        private double _beamTw;
        private double _beamZx;

        // Column section
        private List<string> _colShapeOptions = new();
        private int _selectedColShapeIndex = -1;
        private double _colD;
        private double _colBf;
        private double _colTf;
        private double _colTw;
        private double _colZx;

        // Connection type
        private int _connectionTypeIndex = 0;

        // Bolt parameters
        private int _boltGradeIndex = 0;
        private double _boltDb = 1.0;

        // Plate geometry
        private double _plateBp = 8.0;
        private double _plateTp = 0.75;
        private double _gage = 4.0;
        private double _pfo = 1.25;
        private double _pfi = 1.5;
        private double _pb = 3.5;

        // Stiffener
        private double _stiffenerTs = 0.5;
        private double _stiffenerLst = 5.0;

        // Plate material
        private double _plateFy = 50.0;
        private double _plateFu = 65.0;
        private double _stiffenerFy = 50.0;

        // Design parameters
        private double _span = 360.0;
        private int _systemTypeIndex = 0;

        // Material
        private double _beamFy = 50.0;
        private double _beamFu = 65.0;
        private double _beamRy = 1.1;
        private double _colFy = 50.0;
        private double _colFu = 65.0;
        private double _colRy = 1.1;

        // Loads
        private double _loadD = 0;
        private double _loadL = 0;
        private double _loadS = 0;
        private double _f1 = 0.5;
        private double _vu = 0;

        // Results
        private string _processText = "";
        private bool _hasResult;
        private bool _overallPassed;
        private bool _boltDiameterOK;
        private bool _plateThicknessOK;
        private double _mf;
        private double _ffu;
        private double _dbReq;
        private double _tpReq;
        private string _statusText = "";
        private string _boltStatusText = "";
        private string _plateStatusText = "";

        #endregion

        #region Shape Database Cache

        private List<WShapeProperties>? _allShapes;

        #endregion

        #region Constructor

        public EndplateViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            ConnectionTypeOptions = new List<string> { "4E", "4ES", "8ES" };
            BoltGradeOptions = new List<string> { "A325", "A490", "F1852" };
            SystemTypeOptions = new List<string> { "SMF", "IMF" };

            LoadShapeDatabase();
        }

        #endregion

        #region Options

        public List<string> ConnectionTypeOptions { get; }
        public List<string> BoltGradeOptions { get; }
        public List<string> SystemTypeOptions { get; }

        #endregion

        #region Properties - Beam

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
                ApplyBeamShape(value);
            }
        }

        public double BeamD { get => _beamD; set { _beamD = value; OnPropertyChanged(); } }
        public double BeamBf { get => _beamBf; set { _beamBf = value; OnPropertyChanged(); } }
        public double BeamTf { get => _beamTf; set { _beamTf = value; OnPropertyChanged(); } }
        public double BeamTw { get => _beamTw; set { _beamTw = value; OnPropertyChanged(); } }
        public double BeamZx { get => _beamZx; set { _beamZx = value; OnPropertyChanged(); } }

        #endregion

        #region Properties - Column

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
                ApplyColShape(value);
            }
        }

        public double ColD { get => _colD; set { _colD = value; OnPropertyChanged(); } }
        public double ColBf { get => _colBf; set { _colBf = value; OnPropertyChanged(); } }
        public double ColTf { get => _colTf; set { _colTf = value; OnPropertyChanged(); } }
        public double ColTw { get => _colTw; set { _colTw = value; OnPropertyChanged(); } }
        public double ColZx { get => _colZx; set { _colZx = value; OnPropertyChanged(); } }

        #endregion

        #region Properties - Connection / Bolt

        public int ConnectionTypeIndex
        {
            get => _connectionTypeIndex;
            set
            {
                _connectionTypeIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowStiffener));
                OnPropertyChanged(nameof(ShowPb));
                OnPropertyChanged(nameof(Is4E));
            }
        }

        public string ConnectionType => ConnectionTypeOptions[_connectionTypeIndex];
        public bool ShowStiffener => ConnectionType == "4ES" || ConnectionType == "8ES";
        public bool ShowPb => ConnectionType == "8ES";
        public bool Is4E => ConnectionType == "4E";

        public int BoltGradeIndex
        {
            get => _boltGradeIndex;
            set { _boltGradeIndex = value; OnPropertyChanged(); }
        }

        public double BoltDb
        {
            get => _boltDb;
            set { _boltDb = value; OnPropertyChanged(); }
        }

        #endregion

        #region Properties - Plate Geometry

        public double PlateBp { get => _plateBp; set { _plateBp = value; OnPropertyChanged(); } }
        public double PlateTp { get => _plateTp; set { _plateTp = value; OnPropertyChanged(); } }
        public double Gage { get => _gage; set { _gage = value; OnPropertyChanged(); } }
        public double Pfo { get => _pfo; set { _pfo = value; OnPropertyChanged(); } }
        public double Pfi { get => _pfi; set { _pfi = value; OnPropertyChanged(); } }
        public double Pb { get => _pb; set { _pb = value; OnPropertyChanged(); } }

        #endregion

        #region Properties - Stiffener

        public double StiffenerTs { get => _stiffenerTs; set { _stiffenerTs = value; OnPropertyChanged(); } }
        public double StiffenerLst { get => _stiffenerLst; set { _stiffenerLst = value; OnPropertyChanged(); } }
        public double StiffenerFy { get => _stiffenerFy; set { _stiffenerFy = value; OnPropertyChanged(); } }

        #endregion

        #region Properties - Design Parameters

        public double Span { get => _span; set { _span = value; OnPropertyChanged(); } }

        public int SystemTypeIndex
        {
            get => _systemTypeIndex;
            set { _systemTypeIndex = value; OnPropertyChanged(); }
        }

        #endregion

        #region Properties - Material

        public double BeamFy { get => _beamFy; set { _beamFy = value; OnPropertyChanged(); } }
        public double BeamFu { get => _beamFu; set { _beamFu = value; OnPropertyChanged(); } }
        public double BeamRy { get => _beamRy; set { _beamRy = value; OnPropertyChanged(); } }
        public double ColFy { get => _colFy; set { _colFy = value; OnPropertyChanged(); } }
        public double ColFu { get => _colFu; set { _colFu = value; OnPropertyChanged(); } }
        public double ColRy { get => _colRy; set { _colRy = value; OnPropertyChanged(); } }
        public double PlateFy { get => _plateFy; set { _plateFy = value; OnPropertyChanged(); } }
        public double PlateFu { get => _plateFu; set { _plateFu = value; OnPropertyChanged(); } }

        #endregion

        #region Properties - Loads

        public double LoadD { get => _loadD; set { _loadD = value; OnPropertyChanged(); } }
        public double LoadL { get => _loadL; set { _loadL = value; OnPropertyChanged(); } }
        public double LoadS { get => _loadS; set { _loadS = value; OnPropertyChanged(); } }
        public double F1 { get => _f1; set { _f1 = value; OnPropertyChanged(); } }
        public double Vu { get => _vu; set { _vu = value; OnPropertyChanged(); } }

        #endregion

        #region Properties - Results

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

        public bool BoltDiameterOK
        {
            get => _boltDiameterOK;
            set { _boltDiameterOK = value; OnPropertyChanged(); }
        }

        public bool PlateThicknessOK
        {
            get => _plateThicknessOK;
            set { _plateThicknessOK = value; OnPropertyChanged(); }
        }

        public double Mf { get => _mf; set { _mf = value; OnPropertyChanged(); } }
        public double Ffu { get => _ffu; set { _ffu = value; OnPropertyChanged(); } }
        public double DbReq { get => _dbReq; set { _dbReq = value; OnPropertyChanged(); } }
        public double TpReq { get => _tpReq; set { _tpReq = value; OnPropertyChanged(); } }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public string BoltStatusText
        {
            get => _boltStatusText;
            set { _boltStatusText = value; OnPropertyChanged(); }
        }

        public string PlateStatusText
        {
            get => _plateStatusText;
            set { _plateStatusText = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand CalculateCommand { get; }

        #endregion

        #region Private Methods

        private void LoadShapeDatabase()
        {
            try
            {
                _allShapes = Aisc358ShapeHelper.LoadWShapes();
                var names = _allShapes.Select(s => s.Name).ToList();

                BeamShapeOptions = new List<string> { "-- Select --" }.Concat(names).ToList();
                ColShapeOptions = new List<string> { "-- Select --" }.Concat(names).ToList();
            }
            catch
            {
                BeamShapeOptions = new List<string> { "(Database file not found)" };
                ColShapeOptions = new List<string> { "(Database file not found)" };
            }

            SelectedBeamShapeIndex = 0;
            SelectedColShapeIndex = 0;
        }

        private void ApplyBeamShape(int index)
        {
            if (_allShapes == null || index <= 0 || index > _allShapes.Count) return;
            var shape = _allShapes[index - 1];
            BeamD = shape.d;
            BeamBf = shape.bf;
            BeamTf = shape.tf;
            BeamTw = shape.tw;
            BeamZx = shape.Zx;
            PlateBp = shape.bf; // default plate width = beam flange width
        }

        private void ApplyColShape(int index)
        {
            if (_allShapes == null || index <= 0 || index > _allShapes.Count) return;
            var shape = _allShapes[index - 1];
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
                if (_beamD <= 0 || _beamBf <= 0 || _beamTf <= 0 || _beamTw <= 0 || _beamZx <= 0)
                {
                    ProcessText = "Please select a valid beam section.";
                    return;
                }
                if (_colD <= 0 || _colBf <= 0 || _colTf <= 0 || _colTw <= 0)
                {
                    ProcessText = "Please select a valid column section.";
                    return;
                }

                var input = new EndplateCalculations.InputParameters
                {
                    BeamD = _beamD,
                    BeamBf = _beamBf,
                    BeamTf = _beamTf,
                    BeamTw = _beamTw,
                    BeamZx = _beamZx,
                    BeamFy = _beamFy,
                    BeamFu = _beamFu,
                    BeamRy = _beamRy,

                    ColD = _colD,
                    ColBf = _colBf,
                    ColTf = _colTf,
                    ColTw = _colTw,
                    ColZx = _colZx,
                    ColFy = _colFy,
                    ColFu = _colFu,
                    ColRy = _colRy,

                    ConnectionType = ConnectionType,
                    BoltDb = _boltDb,
                    BoltGrade = BoltGradeOptions[_boltGradeIndex],

                    PlateBp = _plateBp,
                    PlateTp = _plateTp,
                    G = _gage,
                    Pfo = _pfo,
                    Pfi = _pfi,
                    Pb = _pb,

                    StiffenerTs = _stiffenerTs,
                    StiffenerLst = _stiffenerLst,
                    StiffenerFy = _stiffenerFy,

                    PlateFy = _plateFy,
                    PlateFu = _plateFu,

                    Span = _span,
                    SystemType = SystemTypeOptions[_systemTypeIndex],

                    LoadD = _loadD,
                    LoadL = _loadL,
                    LoadS = _loadS,
                    F1 = _f1,
                    Vu = _vu,
                };

                var result = EndplateCalculations.Calculate(input);

                if (!result.IsValid)
                {
                    ProcessText = result.ErrorMessage;
                    HasResult = false;
                    return;
                }

                ProcessText = string.Join(Environment.NewLine, result.Process);
                HasResult = true;
                OverallPassed = result.OverallPassed;
                BoltDiameterOK = result.BoltDiameterOK;
                PlateThicknessOK = result.PlateThicknessOK;
                Mf = result.Mf;
                Ffu = result.Ffu;
                DbReq = result.DbReq;
                TpReq = result.TpReq;

                StatusText = result.OverallPassed ? "ALL CHECKS PASSED" : "SOME CHECKS FAILED";
                BoltStatusText = result.BoltDiameterOK ? "PASS" : "FAIL";
                PlateStatusText = result.PlateThicknessOK ? "PASS" : "FAIL";
            }
            catch (Exception ex)
            {
                ProcessText = $"Calculation error: {ex.Message}";
                HasResult = false;
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
