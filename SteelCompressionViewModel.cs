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
    public class SteelCompressionViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        private int _sectionTypeIndex = 0;
        private int _sectionSourceIndex = 0;
        private List<string> _shapeOptions = new();
        private int _selectedShapeIndex = -1;

        private double _customD = 12.0;
        private double _customBf = 8.0;
        private double _customTw = 0.375;
        private double _customTf = 0.5;

        private double _customHt = 12.0;
        private double _customB = 8.0;
        private double _customT = 0.375;

        private double _fy = 50.0;
        private double _e = 29000.0;
        private int _methodIndex = 0;
        private double _pu = 200.0;

        private double _kx = 1.0;
        private double _ky = 1.0;
        private double _lx = 15.0;
        private double _ly = 15.0;
        private double _lz = 15.0;

        private string _processText = "";
        private string _resultText = "";
        private double _ratio;
        private bool _isOK;
        private bool _hasResult;
        private string _compressionStatus = "";

        #endregion

        #region Shape Database

        private class ShapeData
        {
            public string Name = "";
            public SteelCompressionCalculations.SectionShapeType ShapeType;
            public SteelCompressionCalculations.SectionProperties Properties = new();
        }

        private readonly List<ShapeData> _allShapes = new();
        private readonly List<ShapeData> _filteredShapes = new();

        #endregion

        #region Constructor

        public SteelCompressionViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            SectionTypeOptions = new List<string> { "I-Shape (W/WT)", "HSS (Rectangular/Square)" };
            SectionSourceOptions = new List<string> { "AISC Database", "Custom" };
            MethodOptions = new List<string> { "LRFD", "ASD" };
            LoadShapeDatabase();
        }

        #endregion

        #region Properties

        public List<string> SectionTypeOptions { get; }
        public List<string> SectionSourceOptions { get; }
        public List<string> MethodOptions { get; }

        public int SectionTypeIndex
        {
            get => _sectionTypeIndex;
            set
            {
                _sectionTypeIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsIShape));
                OnPropertyChanged(nameof(IsHSS));
                OnPropertyChanged(nameof(ShowIShapeCustom));
                OnPropertyChanged(nameof(ShowHSSCustom));
                OnPropertyChanged(nameof(ShowTorsionalLength));
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
                OnPropertyChanged(nameof(ShowHSSCustom));
            }
        }

        public bool IsIShape => _sectionTypeIndex == 0;
        public bool IsHSS => _sectionTypeIndex == 1;
        public bool IsDatabase => _sectionSourceIndex == 0;
        public bool IsCustom => _sectionSourceIndex == 1;
        public bool ShowIShapeCustom => IsIShape && IsCustom;
        public bool ShowHSSCustom => IsHSS && IsCustom;
        public bool ShowTorsionalLength => IsIShape;

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

        // Custom HSS
        public double CustomHt { get => _customHt; set { _customHt = value; OnPropertyChanged(); } }
        public double CustomB { get => _customB; set { _customB = value; OnPropertyChanged(); } }
        public double CustomT { get => _customT; set { _customT = value; OnPropertyChanged(); } }

        // Material
        public double Fy { get => _fy; set { _fy = value; OnPropertyChanged(); } }
        public double E { get => _e; set { _e = value; OnPropertyChanged(); } }

        // Design parameters
        public int MethodIndex { get => _methodIndex; set { _methodIndex = value; OnPropertyChanged(); } }
        public double Pu { get => _pu; set { _pu = value; OnPropertyChanged(); } }

        // Effective length parameters
        public double Kx { get => _kx; set { _kx = value; OnPropertyChanged(); } }
        public double Ky { get => _ky; set { _ky = value; OnPropertyChanged(); } }
        public double Lx { get => _lx; set { _lx = value; OnPropertyChanged(); } }
        public double Ly { get => _ly; set { _ly = value; OnPropertyChanged(); } }
        public double Lz { get => _lz; set { _lz = value; OnPropertyChanged(); } }

        // Results
        public string ProcessText { get => _processText; set { _processText = value; OnPropertyChanged(); } }
        public string ResultText { get => _resultText; set { _resultText = value; OnPropertyChanged(); } }
        public double Ratio { get => _ratio; set { _ratio = value; OnPropertyChanged(); } }
        public bool IsOK { get => _isOK; set { _isOK = value; OnPropertyChanged(); } }
        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }
        public string CompressionStatus { get => _compressionStatus; set { _compressionStatus = value; OnPropertyChanged(); } }

        #endregion

        #region Commands

        public ICommand CalculateCommand { get; }

        #endregion

        #region Methods

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
                    SaveToCsv(Path.Combine(
                        Path.GetDirectoryName(xlsxPath) ?? AppDomain.CurrentDomain.BaseDirectory,
                        CsvFileName));
                }
            }
            catch (Exception)
            {
                ShapeOptions = new List<string> { "(Error loading database)" };
                return;
            }

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
                if (f.Length < 24) continue;

                string type = f[0].Trim();
                var props = new SteelCompressionCalculations.SectionProperties
                {
                    ShapeType = type == "W"
                        ? SteelCompressionCalculations.SectionShapeType.IShape
                        : SteelCompressionCalculations.SectionShapeType.HSS,
                    Name = f[1].Trim(),
                    A = D(f[2]),
                    d = D(f[3]), bf = D(f[4]), tw = D(f[5]), tf = D(f[6]), h = D(f[7]),
                    Ht = D(f[8]), B_hss = D(f[9]), tdes = D(f[10]), b_flat = D(f[11]),
                    Ix = D(f[12]), Iy = D(f[13]),
                    Sx = D(f[14]), Sy = D(f[15]),
                    Zx = D(f[16]), Zy = D(f[17]),
                    rx = D(f[18]), ry = D(f[19]),
                    J = D(f[20]),
                    Cw = D(f[21]), rts = D(f[22]), ho = D(f[23])
                };

                if (props.A <= 0) continue;

                _allShapes.Add(new ShapeData
                {
                    Name = props.Name,
                    ShapeType = props.ShapeType,
                    Properties = props
                });
            }
        }

        private void SaveToCsv(string csvPath)
        {
            if (_allShapes.Count == 0) return;
            using var sw = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8);
            sw.WriteLine("Type,Name,A,d,bf,tw,tf,h,Ht,B,tdes,b_flat,Ix,Iy,Sx,Sy,Zx,Zy,rx,ry,J,Cw,rts,ho");
            foreach (var s in _allShapes)
            {
                var p = s.Properties;
                sw.WriteLine(string.Join(",",
                    s.ShapeType == SteelCompressionCalculations.SectionShapeType.IShape ? "W" : "HSS",
                    p.Name, F(p.A),
                    F(p.d), F(p.bf), F(p.tw), F(p.tf), F(p.h),
                    F(p.Ht), F(p.B_hss), F(p.tdes), F(p.b_flat),
                    F(p.Ix), F(p.Iy),
                    F(p.Sx), F(p.Sy),
                    F(p.Zx), F(p.Zy),
                    F(p.rx), F(p.ry),
                    F(p.J),
                    F(p.Cw), F(p.rts), F(p.ho)));
            }
        }

        private static double D(string s) => double.TryParse(s, out double v) ? v : 0;
        private static string F(double v) => v.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

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

                var props = new SteelCompressionCalculations.SectionProperties
                {
                    Name = name,
                    ShapeType = type == "W"
                        ? SteelCompressionCalculations.SectionShapeType.IShape
                        : SteelCompressionCalculations.SectionShapeType.HSS
                };

                props.A = GetCellDouble(ws.Cell(row, 6));

                if (type == "W")
                {
                    props.d = GetCellDouble(ws.Cell(row, 7));
                    props.bf = GetCellDouble(ws.Cell(row, 12));
                    props.tw = GetCellDouble(ws.Cell(row, 17));
                    props.tf = GetCellDouble(ws.Cell(row, 20));
                    props.h = GetCellDouble(ws.Cell(row, 10));
                    props.Ix = GetCellDouble(ws.Cell(row, 39));
                    props.Zx = GetCellDouble(ws.Cell(row, 40));
                    props.Sx = GetCellDouble(ws.Cell(row, 41));
                    props.rx = GetCellDouble(ws.Cell(row, 42));
                    props.Iy = GetCellDouble(ws.Cell(row, 43));
                    props.Zy = GetCellDouble(ws.Cell(row, 44));
                    props.Sy = GetCellDouble(ws.Cell(row, 45));
                    props.ry = GetCellDouble(ws.Cell(row, 46));
                    props.J = GetCellDouble(ws.Cell(row, 50));
                    props.Cw = GetCellDouble(ws.Cell(row, 51));
                    props.rts = GetCellDouble(ws.Cell(row, 75));
                    props.ho = GetCellDouble(ws.Cell(row, 76));

                    if (props.h <= 0) props.h = props.d - 2 * props.tf;
                    if (props.ho <= 0) props.ho = props.d - props.tf;
                }
                else
                {
                    props.Ht = GetCellDouble(ws.Cell(row, 9));
                    props.B_hss = GetCellDouble(ws.Cell(row, 14));
                    props.tdes = GetCellDouble(ws.Cell(row, 24));

                    if (props.Ht <= 0 || props.B_hss <= 0) continue;

                    props.h = GetCellDouble(ws.Cell(row, 10));
                    props.b_flat = GetCellDouble(ws.Cell(row, 15));
                    props.Ix = GetCellDouble(ws.Cell(row, 39));
                    props.Zx = GetCellDouble(ws.Cell(row, 40));
                    props.Sx = GetCellDouble(ws.Cell(row, 41));
                    props.rx = GetCellDouble(ws.Cell(row, 42));
                    props.Iy = GetCellDouble(ws.Cell(row, 43));
                    props.Zy = GetCellDouble(ws.Cell(row, 44));
                    props.Sy = GetCellDouble(ws.Cell(row, 45));
                    props.ry = GetCellDouble(ws.Cell(row, 46));
                    props.J = GetCellDouble(ws.Cell(row, 50));

                    if (props.h <= 0) props.h = props.Ht - 3 * props.tdes;
                    if (props.b_flat <= 0) props.b_flat = props.B_hss - 3 * props.tdes;
                }

                if (props.A <= 0) continue;

                _allShapes.Add(new ShapeData
                {
                    Name = name,
                    ShapeType = props.ShapeType,
                    Properties = props
                });
            }
        }

        private void UpdateShapeOptions()
        {
            var targetType = _sectionTypeIndex == 0
                ? SteelCompressionCalculations.SectionShapeType.IShape
                : SteelCompressionCalculations.SectionShapeType.HSS;

            _filteredShapes.Clear();
            _filteredShapes.AddRange(_allShapes.Where(s => s.ShapeType == targetType));

            ShapeOptions = new List<string> { "-- Select --" }
                .Concat(_filteredShapes.Select(s => s.Name))
                .ToList();
            SelectedShapeIndex = 0;
        }

        private SteelCompressionCalculations.SectionProperties? GetSelectedSection()
        {
            if (IsDatabase)
            {
                int idx = _selectedShapeIndex - 1;
                if (idx < 0 || idx >= _filteredShapes.Count) return null;
                return _filteredShapes[idx].Properties;
            }

            if (IsIShape)
            {
                if (_customD <= 0 || _customBf <= 0 || _customTw <= 0 || _customTf <= 0)
                    return null;
                return SteelCompressionCalculations.ComputeCustomIShape(_customD, _customBf, _customTw, _customTf);
            }
            else
            {
                if (_customHt <= 0 || _customB <= 0 || _customT <= 0)
                    return null;
                return SteelCompressionCalculations.ComputeCustomHSS(_customHt, _customB, _customT);
            }
        }

        private void Calculate()
        {
            try
            {
                var section = GetSelectedSection();
                if (section == null)
                {
                    ProcessText = "Please select or define a valid section.";
                    return;
                }

                var input = new SteelCompressionCalculations.InputParameters
                {
                    Section = section,
                    Fy = _fy,
                    E = _e,
                    Pu = _pu,
                    MethodIndex = _methodIndex,
                    IsRolled = IsDatabase,
                    Kx = _kx,
                    Ky = _ky,
                    Lx = _lx,
                    Ly = _ly,
                    Lz = _lz
                };

                var result = SteelCompressionCalculations.Calculate(input);

                if (!result.IsValid)
                {
                    ProcessText = result.ErrorMessage;
                    return;
                }

                ProcessText = string.Join(Environment.NewLine, result.Process);
                Ratio = result.Ratio;
                IsOK = result.IsOK;
                CompressionStatus = result.IsOK ? "PASS" : "FAIL";
                ResultText = result.Summary;
                HasResult = true;
            }
            catch (Exception ex)
            {
                ProcessText = $"Calculation error: {ex.Message}";
            }
        }

        private static double GetCellDouble(IXLCell cell)
        {
            if (cell.IsEmpty()) return 0;
            try
            {
                return cell.GetDouble();
            }
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
