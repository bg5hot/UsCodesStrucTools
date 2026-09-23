using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ClosedXML.Excel;

namespace SpectrumComparison
{
    public class DuctilityClassificationViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        private int _systemIndex;
        private int _memberIndex;
        private int _sectionTypeIndex;
        private int _sectionSourceIndex;
        private List<string> _shapeOptions = new();
        private int _selectedShapeIndex = -1;

        // Custom I-shape
        private double _customD = 12.0;
        private double _customBf = 8.0;
        private double _customTw = 0.375;
        private double _customTf = 0.5;

        // Custom HSS Rect
        private double _customHt = 12.0;
        private double _customB = 8.0;
        private double _customT = 0.375;

        // Custom HSS Round
        private double _customD_hss = 12.75;
        private double _customT_round = 0.375;

        // Material
        private double _fy = 50.0;
        private double _e = 29000.0;
        private double _ry = 1.1;
        private bool _autoRy = true;

        // Axial force
        private int _methodIndex;
        private double _pu = 0;
        private double _pa = 0;
        private double _ag = 10.0;

        // Results
        private string _processText = "";
        private bool _hasResult;
        private string _flangeStatus = "";
        private string _webStatus = "";
        private string _overallStatus = "";
        private bool _flangeOK;
        private bool _webOK;
        private bool _sectionOK;
        private double _flangeRatio;
        private double _webRatio;
        private double _flangeLimit;
        private double _webLimit;

        #endregion

        #region Shape Database

        private class ShapeData
        {
            public string Name = "";
            public string Type = ""; // "W", "HSS"
            public double d, bf, tw, tf;
            public double Ht, B_hss, tdes;
            public double Ag;
        }

        private readonly List<ShapeData> _allShapes = new();
        private readonly List<ShapeData> _filteredShapes = new();

        #endregion

        #region Constructor

        public DuctilityClassificationViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);

            SystemOptions = Enum.GetValues(typeof(DuctilityClassificationCalculations.SeismicSystem))
                .Cast<DuctilityClassificationCalculations.SeismicSystem>()
                .Select(s => DuctilityClassificationCalculations.GetSystemDisplayName(s))
                .ToList();

            SectionTypeOptions = new List<string>
            {
                "I-Shape (W)",
                "HSS Rectangular / Square",
                "HSS Round"
            };

            SectionSourceOptions = new List<string> { "AISC Database", "Custom" };
            MethodOptions = new List<string> { "LRFD", "ASD" };

            _systemIndex = 2; // SMF default
            UpdateMemberOptions();
            LoadShapeDatabase();
        }

        #endregion

        #region Properties

        public List<string> SystemOptions { get; }
        public List<string> SectionTypeOptions { get; }
        public List<string> SectionSourceOptions { get; }
        public List<string> MethodOptions { get; }

        private List<string> _memberOptions = new();
        public List<string> MemberOptions
        {
            get => _memberOptions;
            set { _memberOptions = value; OnPropertyChanged(); }
        }

        public int SystemIndex
        {
            get => _systemIndex;
            set
            {
                _systemIndex = value;
                OnPropertyChanged();
                UpdateMemberOptions();
            }
        }

        public int MemberIndex
        {
            get => _memberIndex;
            set { _memberIndex = value; OnPropertyChanged(); }
        }

        public int SectionTypeIndex
        {
            get => _sectionTypeIndex;
            set
            {
                _sectionTypeIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsIShape));
                OnPropertyChanged(nameof(IsHSSRect));
                OnPropertyChanged(nameof(IsHSSRound));
                OnPropertyChanged(nameof(ShowIShapeCustom));
                OnPropertyChanged(nameof(ShowHSSRectCustom));
                OnPropertyChanged(nameof(ShowHSSRoundCustom));
                UpdateShapeOptions();
            }
        }

        public int SectionSourceIndex
        {
            get => _sectionSourceIndex;
            set
            {
                _sectionSourceIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDatabase));
                OnPropertyChanged(nameof(IsCustom));
                OnPropertyChanged(nameof(ShowIShapeCustom));
                OnPropertyChanged(nameof(ShowHSSRectCustom));
                OnPropertyChanged(nameof(ShowHSSRoundCustom));
            }
        }

        public bool IsIShape => _sectionTypeIndex == 0;
        public bool IsHSSRect => _sectionTypeIndex == 1;
        public bool IsHSSRound => _sectionTypeIndex == 2;
        public bool IsDatabase => _sectionSourceIndex == 0;
        public bool IsCustom => _sectionSourceIndex == 1;
        public bool ShowIShapeCustom => IsIShape && IsCustom;
        public bool ShowHSSRectCustom => IsHSSRect && IsCustom;
        public bool ShowHSSRoundCustom => IsHSSRound && IsCustom;

        public List<string> ShapeOptions
        {
            get => _shapeOptions;
            set { _shapeOptions = value; OnPropertyChanged(); }
        }

        public int SelectedShapeIndex
        {
            get => _selectedShapeIndex;
            set { _selectedShapeIndex = value; OnPropertyChanged(); }
        }

        // Custom I-shape
        public double CustomD { get => _customD; set { _customD = value; OnPropertyChanged(); } }
        public double CustomBf { get => _customBf; set { _customBf = value; OnPropertyChanged(); } }
        public double CustomTw { get => _customTw; set { _customTw = value; OnPropertyChanged(); } }
        public double CustomTf { get => _customTf; set { _customTf = value; OnPropertyChanged(); } }

        // Custom HSS Rect
        public double CustomHt { get => _customHt; set { _customHt = value; OnPropertyChanged(); } }
        public double CustomB { get => _customB; set { _customB = value; OnPropertyChanged(); } }
        public double CustomT { get => _customT; set { _customT = value; OnPropertyChanged(); } }

        // Custom HSS Round
        public double CustomD_Hss { get => _customD_hss; set { _customD_hss = value; OnPropertyChanged(); } }
        public double CustomT_Round { get => _customT_round; set { _customT_round = value; OnPropertyChanged(); } }

        // Material
        public double Fy
        {
            get => _fy;
            set
            {
                _fy = value;
                OnPropertyChanged();
                if (_autoRy) Ry = DuctilityClassificationCalculations.GetDefaultRy(value);
            }
        }
        public double E { get => _e; set { _e = value; OnPropertyChanged(); } }
        public double Ry { get => _ry; set { _ry = value; OnPropertyChanged(); } }
        public bool AutoRy
        {
            get => _autoRy;
            set { _autoRy = value; OnPropertyChanged(); }
        }

        // Axial force
        public int MethodIndex { get => _methodIndex; set { _methodIndex = value; OnPropertyChanged(); } }
        public double Pu { get => _pu; set { _pu = value; OnPropertyChanged(); } }
        public double Pa { get => _pa; set { _pa = value; OnPropertyChanged(); } }
        public double Ag { get => _ag; set { _ag = value; OnPropertyChanged(); } }

        // Results
        public string ProcessText { get => _processText; set { _processText = value; OnPropertyChanged(); } }
        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }
        public string FlangeStatus { get => _flangeStatus; set { _flangeStatus = value; OnPropertyChanged(); } }
        public string WebStatus { get => _webStatus; set { _webStatus = value; OnPropertyChanged(); } }
        public string OverallStatus { get => _overallStatus; set { _overallStatus = value; OnPropertyChanged(); } }
        public bool FlangeOK { get => _flangeOK; set { _flangeOK = value; OnPropertyChanged(); } }
        public bool WebOK { get => _webOK; set { _webOK = value; OnPropertyChanged(); } }
        public bool SectionOK { get => _sectionOK; set { _sectionOK = value; OnPropertyChanged(); } }
        public double FlangeRatio { get => _flangeRatio; set { _flangeRatio = value; OnPropertyChanged(); } }
        public double WebRatio { get => _webRatio; set { _webRatio = value; OnPropertyChanged(); } }
        public double FlangeLimit { get => _flangeLimit; set { _flangeLimit = value; OnPropertyChanged(); } }
        public double WebLimit { get => _webLimit; set { _webLimit = value; OnPropertyChanged(); } }

        #endregion

        #region Commands

        public ICommand CalculateCommand { get; }

        #endregion

        #region Methods

        private void UpdateMemberOptions()
        {
            var system = (DuctilityClassificationCalculations.SeismicSystem)_systemIndex;
            var members = DuctilityClassificationCalculations.GetAvailableMembers(system);

            MemberOptions = members
                .Select(m => DuctilityClassificationCalculations.GetMemberDisplayName(m))
                .ToList();
            _memberIndex = 0;
            OnPropertyChanged(nameof(MemberIndex));
        }

        private static readonly string CsvFileName = "aisc-shapes-cache.csv";

        private void LoadShapeDatabase()
        {
            try
            {
                string? csvPath = FindFile(CsvFileName);
                if (csvPath != null)
                {
                    LoadFromCsv(csvPath);
                }
                else
                {
                    string? xlsxPath = FindFile("aisc-shapes-database-v160-2.xlsx");
                    if (xlsxPath == null)
                    {
                        ShapeOptions = new List<string> { "(Database file not found)" };
                        return;
                    }
                    LoadFromXlsx(xlsxPath);
                }
            }
            catch { }

            UpdateShapeOptions();
        }

        private static string? FindFile(string fileName)
        {
            string[] paths =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                Path.Combine(Directory.GetCurrentDirectory(), fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", fileName),
            };
            return paths.FirstOrDefault(File.Exists);
        }

        private void LoadFromCsv(string csvPath)
        {
            foreach (var line in File.ReadLines(csvPath).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var f = line.Split(',');
                if (f.Length < 12) continue;

                string type = f[0].Trim();
                if (type != "W" && type != "HSS") continue;

                var s = new ShapeData
                {
                    Type = type,
                    Name = f[1].Trim(),
                    Ag = D(f[2]),
                    d = D(f[3]), bf = D(f[4]), tw = D(f[5]), tf = D(f[6]),
                    Ht = D(f[8]), B_hss = D(f[9]), tdes = D(f[10])
                };

                if (s.Ag <= 0) continue;
                _allShapes.Add(s);
            }
        }

        private void LoadFromXlsx(string xlsxPath)
        {
            using var workbook = new XLWorkbook(xlsxPath);
            var ws = workbook.Worksheet("Database v16.0") ?? workbook.Worksheet(1);
            int rowCount = ws.RowCount();

            for (int row = 1; row <= rowCount; row++)
            {
                string type = ws.Cell(row, 1).GetString().Trim();
                if (type != "W" && type != "HSS") continue;

                string name = ws.Cell(row, 3).GetString().Trim();
                if (string.IsNullOrEmpty(name)) continue;

                var s = new ShapeData
                {
                    Type = type,
                    Name = name,
                    Ag = GetCellDouble(ws.Cell(row, 6))
                };

                if (type == "W")
                {
                    s.d = GetCellDouble(ws.Cell(row, 7));
                    s.bf = GetCellDouble(ws.Cell(row, 12));
                    s.tw = GetCellDouble(ws.Cell(row, 17));
                    s.tf = GetCellDouble(ws.Cell(row, 20));
                }
                else // HSS
                {
                    s.Ht = GetCellDouble(ws.Cell(row, 9));
                    s.B_hss = GetCellDouble(ws.Cell(row, 14));
                    s.tdes = GetCellDouble(ws.Cell(row, 24));
                }

                if (s.Ag <= 0) continue;
                _allShapes.Add(s);
            }
        }

        private void UpdateShapeOptions()
        {
            string targetType = _sectionTypeIndex == 0 ? "W" : "HSS";

            _filteredShapes.Clear();
            _filteredShapes.AddRange(_allShapes.Where(s => s.Type == targetType));

            ShapeOptions = new List<string> { "-- Select --" }
                .Concat(_filteredShapes.Select(s => s.Name))
                .ToList();
            SelectedShapeIndex = 0;
        }

        private ShapeData? GetSelectedShape()
        {
            int idx = _selectedShapeIndex - 1;
            if (idx < 0 || idx >= _filteredShapes.Count) return null;
            return _filteredShapes[idx];
        }

        private void Calculate()
        {
            try
            {
                var system = (DuctilityClassificationCalculations.SeismicSystem)_systemIndex;
                var members = DuctilityClassificationCalculations.GetAvailableMembers(system);
                if (_memberIndex < 0 || _memberIndex >= members.Count)
                {
                    ProcessText = "Please select a valid member type.";
                    return;
                }
                var member = members[_memberIndex];

                var sectionType = _sectionTypeIndex switch
                {
                    0 => DuctilityClassificationCalculations.SectionCategory.IShape,
                    1 => DuctilityClassificationCalculations.SectionCategory.HSS_Rect,
                    2 => DuctilityClassificationCalculations.SectionCategory.HSS_Round,
                    _ => DuctilityClassificationCalculations.SectionCategory.IShape
                };

                var input = new DuctilityClassificationCalculations.InputParameters
                {
                    System = system,
                    Member = member,
                    SectionType = sectionType,
                    Fy = _fy,
                    E = _e,
                    Ry = _ry,
                    Method = _methodIndex == 0
                        ? DuctilityClassificationCalculations.DesignMethod.LRFD
                        : DuctilityClassificationCalculations.DesignMethod.ASD,
                    Pu = _pu,
                    Pa = _pa,
                    Ag = _ag
                };

                // Fill section dimensions
                if (IsDatabase)
                {
                    var shape = GetSelectedShape();
                    if (shape == null)
                    {
                        ProcessText = "Please select a section from the database.";
                        return;
                    }

                    if (shape.Type == "W")
                    {
                        input.d = shape.d;
                        input.bf = shape.bf;
                        input.tw = shape.tw;
                        input.tf = shape.tf;
                        input.Ag = shape.Ag > 0 ? shape.Ag : _ag;
                    }
                    else // HSS
                    {
                        if (_sectionTypeIndex == 2) // Round
                        {
                            input.D_hss = shape.Ht; // use Ht as OD
                            input.tdes = shape.tdes;
                        }
                        else // Rect
                        {
                            input.Ht = shape.Ht;
                            input.B_hss = shape.B_hss;
                            input.tdes = shape.tdes;
                        }
                        input.Ag = shape.Ag > 0 ? shape.Ag : _ag;
                    }
                }
                else
                {
                    // Custom dimensions
                    if (IsIShape)
                    {
                        input.d = _customD;
                        input.bf = _customBf;
                        input.tw = _customTw;
                        input.tf = _customTf;
                        input.Ag = 2 * _customBf * _customTf + (_customD - 2 * _customTf) * _customTw;
                    }
                    else if (IsHSSRect)
                    {
                        input.Ht = _customHt;
                        input.B_hss = _customB;
                        input.tdes = _customT;
                    }
                    else // Round
                    {
                        input.D_hss = _customD_hss;
                        input.tdes = _customT_round;
                    }
                }

                var result = DuctilityClassificationCalculations.Calculate(input);

                if (!result.IsValid)
                {
                    ProcessText = result.ErrorMessage;
                    return;
                }

                ProcessText = string.Join(Environment.NewLine, result.Process);
                HasResult = true;

                FlangeRatio = result.FlangeRatioActual;
                FlangeLimit = result.FlangeRatioLimit;
                WebRatio = result.WebRatioActual;
                WebLimit = result.WebRatioLimit;

                FlangeOK = result.FlangeOK;
                WebOK = result.WebOK;
                SectionOK = result.SectionOK;

                FlangeStatus = result.FlangeOK ? "PASS" : "FAIL";
                WebStatus = result.WebOK ? "PASS" : "FAIL";
                OverallStatus = result.SectionOK ? "SEISMICALLY COMPACT" : "NOT COMPACT";
            }
            catch (Exception ex)
            {
                ProcessText = $"Error: {ex.Message}";
            }
        }

        private static double D(string s) => double.TryParse(s, out double v) ? v : 0;

        private static double GetCellDouble(IXLCell cell)
        {
            if (cell.IsEmpty()) return 0;
            try { return cell.GetDouble(); }
            catch
            {
                string s = cell.GetString().Trim();
                if (string.IsNullOrEmpty(s) || s == "-" || s == "N/A") return 0;
                return double.TryParse(s, out double val) ? val : 0;
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
