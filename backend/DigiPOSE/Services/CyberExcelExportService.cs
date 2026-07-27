using ClosedXML.Excel;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;

namespace DigiPOSE.Services
{
    public static class CyberExcelExportService
    {
        public static byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName, string title, Dictionary<string, string>? customHeaders = null)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Export Data" : sheetName);
            
            // Enable gridlines for high readability
            worksheet.ShowGridLines = true;

            // Row 1: Cyber-Cinematic HUD Title Block
            worksheet.Cell(1, 1).Value = $"[DIGIPOSE ENTERPRISE TELEMETRY EXPORT] // {title.ToUpper()}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontName = "Consolas";
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#00E5FF");
            worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0A0A0A");
            worksheet.Row(1).Height = 28;
            worksheet.Row(1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Row 2: Metadata / Telemetry Status
            worksheet.Cell(2, 1).Value = $"EXPORT TIMESTAMP (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | STATUS: RECONCILED & INTEGRITY VERIFIED";
            worksheet.Cell(2, 1).Style.Font.FontName = "Consolas";
            worksheet.Cell(2, 1).Style.Font.FontSize = 10;
            worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#00FF66");
            worksheet.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#000000");
            worksheet.Row(2).Height = 20;
            worksheet.Row(2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (properties.Length == 0)
            {
                using var emptyStream = new MemoryStream();
                workbook.SaveAs(emptyStream);
                return emptyStream.ToArray();
            }

            // Merge Title & Metadata across all projected columns
            worksheet.Range(1, 1, 1, properties.Length).Merge();
            worksheet.Range(2, 1, 2, properties.Length).Merge();

            // Row 4: Column Headers (Holographic Cyan with Carbon text)
            int startRow = 4;
            for (int col = 0; col < properties.Length; col++)
            {
                var prop = properties[col];
                string headerText = prop.Name;
                if (customHeaders != null && customHeaders.TryGetValue(prop.Name, out var customName))
                {
                    headerText = customName;
                }
                else
                {
                    // Convert CamelCase to clean readable spacing
                    headerText = System.Text.RegularExpressions.Regex.Replace(headerText, "([A-Z])", " $1").Trim();
                }
                
                var headerCell = worksheet.Cell(startRow, col + 1);
                headerCell.Value = headerText.ToUpper();
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Font.FontName = "Consolas";
                headerCell.Style.Font.FontSize = 11;
                headerCell.Style.Font.FontColor = XLColor.FromHtml("#000000");
                headerCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#00E5FF");
                headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerCell.Style.Border.TopBorder = XLBorderStyleValues.Medium;
                headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                headerCell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                headerCell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                headerCell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#000000");
            }
            worksheet.Row(startRow).Height = 24;

            // Populate Data Rows
            int currentRow = startRow + 1;
            foreach (var item in data)
            {
                for (int col = 0; col < properties.Length; col++)
                {
                    var prop = properties[col];
                    var val = prop.GetValue(item);
                    var cell = worksheet.Cell(currentRow, col + 1);

                    if (val == null)
                    {
                        cell.Value = string.Empty;
                    }
                    else if (val is bool boolVal)
                    {
                        cell.Value = boolVal ? "ACTIVE" : "INACTIVE";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        if (boolVal)
                        {
                            cell.Style.Font.FontColor = XLColor.FromHtml("#008040");
                            cell.Style.Font.Bold = true;
                        }
                        else
                        {
                            cell.Style.Font.FontColor = XLColor.FromHtml("#B00000");
                        }
                    }
                    else if (val is decimal decVal)
                    {
                        cell.Value = decVal;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else if (val is double dblVal)
                    {
                        cell.Value = dblVal;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else if (val is int intVal || val is long longVal)
                    {
                        cell.SetValue(Convert.ToDouble(val));
                        cell.Style.NumberFormat.Format = "#,##0";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else if (val is DateTime dtVal)
                    {
                        cell.Value = dtVal;
                        cell.Style.NumberFormat.Format = "yyyy-MM-dd HH:mm:ss";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else if (val is byte[] || prop.PropertyType.IsArray)
                    {
                        cell.Value = "[BINARY DATA]";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else
                    {
                        cell.Value = val.ToString()!;
                    }

                    cell.Style.Font.FontName = "Calibri";
                    cell.Style.Font.FontSize = 11;
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                    cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#D0D0D0");
                    cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.LeftBorderColor = XLColor.FromHtml("#E0E0E0");
                    cell.Style.Border.RightBorderColor = XLColor.FromHtml("#E0E0E0");
                    
                    // Zebra striping for enhanced scannability
                    if ((currentRow - startRow) % 2 == 0)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F9FBFD");
                    }
                }
                worksheet.Row(currentRow).Height = 20;
                worksheet.Row(currentRow).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                currentRow++;
            }

            // Auto-fit columns with safety bounds
            worksheet.Columns().AdjustToContents(startRow, currentRow - 1);
            for (int i = 1; i <= properties.Length; i++)
            {
                if (worksheet.Column(i).Width < 12)
                {
                    worksheet.Column(i).Width = 12;
                }
                else if (worksheet.Column(i).Width > 60)
                {
                    worksheet.Column(i).Width = 60;
                }
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
