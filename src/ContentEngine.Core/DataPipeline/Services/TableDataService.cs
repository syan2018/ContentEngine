using Microsoft.AspNetCore.Components.Forms;
using ContentEngine.Core.DataPipeline.Models;
using LiteDB;
using OfficeOpenXml;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using FieldType = ContentEngine.Core.DataPipeline.Models.FieldType;

namespace ContentEngine.Core.DataPipeline.Services;

/// <summary>
/// 表格数据读取服务实现
/// </summary>
public class TableDataService : ITableDataService
{
    public TableDataService()
    {
        // 设置EPPlus许可证上下文（非商业用途）
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<TableImportData> ReadExcelAsync(IBrowserFile file, int sheetIndex = 0, CancellationToken cancellationToken = default)
    {
        using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024); // 50MB限制
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        using var package = new ExcelPackage(memoryStream);

        if (package.Workbook.Worksheets.Count == 0)
        {
            throw new InvalidOperationException("Excel文件中没有工作表");
        }

        if (sheetIndex >= package.Workbook.Worksheets.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(sheetIndex), "工作表索引超出范围");
        }

        var worksheet = package.Workbook.Worksheets[sheetIndex];
        var result = new TableImportData
        {
            FileName = file.Name,
            SheetName = worksheet.Name
        };

        if (worksheet.Dimension == null)
        {
            return result; // 空工作表
        }

        var startRow = worksheet.Dimension.Start.Row;
        var endRow = worksheet.Dimension.End.Row;
        var startCol = worksheet.Dimension.Start.Column;
        var endCol = worksheet.Dimension.End.Column;

        // 读取表头（第一行）
        for (int col = startCol; col <= endCol; col++)
        {
            var headerValue = worksheet.Cells[startRow, col].Text?.Trim() ?? $"列{col}";
            result.Headers.Add(headerValue);
        }

        // 读取数据行
        for (int row = startRow + 1; row <= endRow; row++)
        {
            var rowData = new List<string>();
            for (int col = startCol; col <= endCol; col++)
            {
                var cellValue = worksheet.Cells[row, col].Text?.Trim() ?? string.Empty;
                rowData.Add(cellValue);
            }
            result.Rows.Add(rowData);
        }

        return result;
    }

    public async Task<TableImportData> ReadCsvAsync(IBrowserFile file, string encoding = "UTF-8", char delimiter = ',', CancellationToken cancellationToken = default)
    {
        using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024); // 50MB限制
        using var reader = new StreamReader(stream, Encoding.GetEncoding(encoding));

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var csv = new CsvReader(reader, config);

        var result = new TableImportData
        {
            FileName = file.Name,
            SheetName = "Sheet1"
        };

        // 读取表头
        await csv.ReadAsync();
        csv.ReadHeader();
        result.Headers = csv.HeaderRecord?.ToList() ?? new List<string>();

        // 读取数据行
        while (await csv.ReadAsync())
        {
            var rowData = new List<string>();
            for (int i = 0; i < result.Headers.Count; i++)
            {
                var value = csv.GetField(i)?.Trim() ?? string.Empty;
                rowData.Add(value);
            }
            result.Rows.Add(rowData);
        }

        return result;
    }

    public async Task<List<string>> GetExcelSheetNamesAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        using var package = new ExcelPackage(memoryStream);

        return package.Workbook.Worksheets.Select(ws => ws.Name).ToList();
    }

    public bool IsTableFileSupported(string fileName, string mimeType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        return extension switch
        {
            ".xlsx" or ".xls" => true,
            ".csv" => true,
            _ => false
        };
    }

    public async Task<List<BsonDocument>> ConvertTableDataToBsonAsync(TableImportData tableData, SchemaDefinition schema, TableImportConfig config)
    {
        var result = new List<BsonDocument>();
        
        var startIndex = Math.Max(config.StartRowIndex, 0);
        var endIndex = config.EndRowIndex == -1 ? tableData.Rows.Count - 1 : Math.Min(config.EndRowIndex, tableData.Rows.Count - 1);

        for (int i = startIndex; i <= endIndex; i++)
        {
            if (i >= tableData.Rows.Count) break;

            var row = tableData.Rows[i];
            
            // 检查是否为空行
            if (config.IgnoreEmptyRows && row.All(cell => string.IsNullOrWhiteSpace(cell)))
            {
                continue;
            }

            var bsonDoc = new BsonDocument();
            
            foreach (var mapping in config.FieldMappings.Where(m => m.IsMapped))
            {
                if (mapping.ColumnIndex >= 0 && mapping.ColumnIndex < row.Count)
                {
                    var cellValue = row[mapping.ColumnIndex];
                    var field = schema.Fields.FirstOrDefault(f => f.Name == mapping.SchemaFieldName);
                    
                    if (field != null)
                    {
                        var convertedValue = ConvertCellValue(cellValue, field.Type);
                        if (convertedValue != null)
                        {
                            bsonDoc[mapping.SchemaFieldName] = new BsonValue(convertedValue);
                        }
                    }
                }
            }

            if (bsonDoc.Count > 0) // 只添加有数据的文档
            {
                result.Add(bsonDoc);
            }
        }

        return result;
    }

    public List<FieldMapping> SuggestFieldMappings(List<string> tableHeaders, SchemaDefinition schema)
    {
        var mappings = new List<FieldMapping>();

        foreach (var field in schema.Fields)
        {
            var mapping = new FieldMapping
            {
                SchemaFieldName = field.Name
            };

            // 尝试找到最匹配的列
            var bestMatch = FindBestColumnMatch(field.Name, tableHeaders);
            if (bestMatch.HasValue)
            {
                mapping.TableColumnName = tableHeaders[bestMatch.Value];
                mapping.ColumnIndex = bestMatch.Value;
                mapping.IsMapped = true;
            }

            mappings.Add(mapping);
        }

        return mappings;
    }

    private int? FindBestColumnMatch(string fieldName, List<string> tableHeaders)
    {
        var fieldNameLower = fieldName.ToLowerInvariant();
        
        // 完全匹配
        for (int i = 0; i < tableHeaders.Count; i++)
        {
            if (string.Equals(tableHeaders[i], fieldName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        // 包含匹配
        for (int i = 0; i < tableHeaders.Count; i++)
        {
            var headerLower = tableHeaders[i].ToLowerInvariant();
            if (headerLower.Contains(fieldNameLower) || fieldNameLower.Contains(headerLower))
            {
                return i;
            }
        }

        // 相似度匹配（简单的字符串相似度）
        var bestScore = 0.0;
        int? bestIndex = null;
        
        for (int i = 0; i < tableHeaders.Count; i++)
        {
            var similarity = CalculateStringSimilarity(fieldNameLower, tableHeaders[i].ToLowerInvariant());
            if (similarity > bestScore && similarity > 0.6) // 相似度阈值
            {
                bestScore = similarity;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private double CalculateStringSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        var longer = s1.Length > s2.Length ? s1 : s2;
        var shorter = s1.Length > s2.Length ? s2 : s1;

        if (longer.Length == 0)
            return 1.0;

        return (longer.Length - ComputeLevenshteinDistance(longer, shorter)) / (double)longer.Length;
    }

    private int ComputeLevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    private object? ConvertCellValue(string cellValue, FieldType fieldType)
    {
        if (string.IsNullOrWhiteSpace(cellValue))
            return null;

        try
        {
            return fieldType switch
            {
                FieldType.Text => cellValue,
                FieldType.Number => double.TryParse(cellValue, out var d) ? d : null,
                FieldType.Boolean => ParseBoolean(cellValue),
                FieldType.Date => DateTime.TryParse(cellValue, out var dt) ? dt : null,
                FieldType.Reference => cellValue,
                _ => cellValue
            };
        }
        catch
        {
            return null;
        }
    }

    private bool? ParseBoolean(string value)
    {
        var lowerValue = value.ToLowerInvariant().Trim();
        
        return lowerValue switch
        {
            "true" or "1" or "yes" or "是" or "真" or "✓" => true,
            "false" or "0" or "no" or "否" or "假" or "✗" => false,
            _ => bool.TryParse(value, out var result) ? result : null
        };
    }
} 