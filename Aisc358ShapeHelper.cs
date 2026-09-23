using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace SpectrumComparison
{
    public class WShapeProperties
    {
        public string Name = "";
        public double d;
        public double bf;
        public double tw;
        public double tf;
        public double Zx;
        public double Ag;

        public double Weight
        {
            get
            {
                try
                {
                    var parts = Name.ToUpper().Split('X');
                    if (parts.Length > 1 && double.TryParse(parts[1], out double w))
                        return w;
                }
                catch { }
                return 999;
            }
        }
    }

    public static class Aisc358ShapeHelper
    {
        private static List<WShapeProperties>? _cache;
        private static readonly string CsvFileName = "aisc-w-shapes-cache-358-v2.csv";

        public static List<WShapeProperties> LoadWShapes()
        {
            if (_cache != null) return _cache;

            var shapes = new List<WShapeProperties>();

            try
            {
                string? csvPath = FindFile(CsvFileName);
                if (csvPath != null)
                {
                    LoadFromCsv(csvPath, shapes);
                }
                else
                {
                    string? xlsxPath = FindFile("aisc-shapes-database-v160-2.xlsx");
                    if (xlsxPath == null)
                    {
                        _cache = shapes;
                        return shapes;
                    }
                    LoadFromXlsx(xlsxPath, shapes);
                    SaveToCsv(Path.Combine(
                        Path.GetDirectoryName(xlsxPath) ?? AppDomain.CurrentDomain.BaseDirectory,
                        CsvFileName), shapes);
                }
            }
            catch { }

            _cache = shapes;
            return shapes;
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

        private static void LoadFromCsv(string csvPath, List<WShapeProperties> shapes)
        {
            foreach (var line in File.ReadLines(csvPath).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var f = line.Split(',');
                if (f.Length < 6) continue;
                var s = new WShapeProperties
                {
                    Name = f[0].Trim(),
                    d = D(f[1]), bf = D(f[2]), tw = D(f[3]), tf = D(f[4]), Zx = D(f[5]),
                    Ag = f.Length >= 7 ? D(f[6]) : 0
                };
                if (s.d > 0 && s.Zx > 0)
                    shapes.Add(s);
            }
        }

        private static void SaveToCsv(string csvPath, List<WShapeProperties> shapes)
        {
            if (shapes.Count == 0) return;
            using var sw = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8);
            sw.WriteLine("Name,d,bf,tw,tf,Zx,Ag");
            foreach (var s in shapes)
                sw.WriteLine($"{s.Name},{G(s.d)},{G(s.bf)},{G(s.tw)},{G(s.tf)},{G(s.Zx)},{G(s.Ag)}");
        }

        private static void LoadFromXlsx(string xlsxPath, List<WShapeProperties> shapes)
        {
            using var workbook = new XLWorkbook(xlsxPath);
            var ws = workbook.Worksheet("Database v16.0") ?? workbook.Worksheet(1);
            int rowCount = ws.RowCount();

            for (int row = 1; row <= rowCount; row++)
            {
                string type = ws.Cell(row, 1).GetString().Trim();
                if (type != "W") continue;

                string name = ws.Cell(row, 3).GetString().Trim();
                if (string.IsNullOrEmpty(name)) continue;

                var s = new WShapeProperties
                {
                    Name = name,
                    d = GetCellDouble(ws.Cell(row, 7)),
                    bf = GetCellDouble(ws.Cell(row, 12)),
                    tw = GetCellDouble(ws.Cell(row, 17)),
                    tf = GetCellDouble(ws.Cell(row, 20)),
                    Zx = GetCellDouble(ws.Cell(row, 40)),
                    Ag = GetCellDouble(ws.Cell(row, 6)),
                };

                if (s.d > 0 && s.Zx > 0)
                    shapes.Add(s);
            }
        }

        private static double D(string s) => double.TryParse(s, out double v) ? v : 0;
        private static string G(double v) => v.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

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
    }
}
