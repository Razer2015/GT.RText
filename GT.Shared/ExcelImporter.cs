using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GT.Shared
{
    public class ExcelImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<ImportedEntry> ImportedEntries { get; set; } = new List<ImportedEntry>();
    }

    public class ImportedEntry
    {
        public int RecNo { get; set; }
        public string Label { get; set; }
        public string String { get; set; }
    }

    public static class ExcelImporter
    {
        /// <summary>
        /// Imports data from a CSV file with format: RecNo, Label, String
        /// </summary>
        public static ExcelImportResult ImportFromCsv(string filePath)
        {
            var result = new ExcelImportResult();
            
            try
            {
                if (!File.Exists(filePath))
                {
                    result.Message = "Arquivo não encontrado.";
                    return result;
                }

                var lines = File.ReadAllLines(filePath);
                
                if (lines.Length < 2)
                {
                    result.Message = "The file must contain at least one header line and one data line.";
                    return result;
                }

                // Check header (first line)
                var headerLine = lines[0].ToLower();
                if (!headerLine.Contains("recno") || !headerLine.Contains("label") || !headerLine.Contains("string"))
                {
                    result.Message = "The file must have columns in order: RecNo, Label, String.\n" +
                                   $"Header found: '{lines[0]}'";
                    return result;
                }

                // Process data lines (skip header)
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    
                    // Pular linhas vazias
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var parts = ParseCsvLine(line);
                    
                    if (parts.Length < 3)
                    {
                        Console.WriteLine($"Linha {i + 1}: Formato inválido '{line}', ignorando linha.");
                        continue;
                    }

                    // Clean and process fields
                    var recNoText = parts[0].Trim().Trim('"'); // Remove extra quotes from Excel
                    var label = parts[1].Trim().Trim('"');     // Remove extra quotes from Excel
                    var stringValue = parts[2].Trim().Trim('"'); // Remove extra quotes from Excel

                    // If there are still quotes in the middle of text, restore them correctly
                    label = label.Replace("\"\"", "\"");
                    stringValue = stringValue.Replace("\"\"", "\"");

                    if (int.TryParse(recNoText, out int recNo))
                    {
                        result.ImportedEntries.Add(new ImportedEntry
                        {
                            RecNo = recNo,
                            Label = label,
                            String = stringValue
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Linha {i + 1}: RecNo inválido '{recNoText}', ignorando linha.");
                    }
                }

                if (result.ImportedEntries.Count == 0)
                {
                    result.Message = "Nenhum registro válido foi encontrado no arquivo.";
                    return result;
                }

                result.Success = true;
                result.Message = $"Importados {result.ImportedEntries.Count} registros com sucesso.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Erro ao importar arquivo: {ex.Message}";
            }
            
            return result;
        }

        /// <summary>
        /// Parses a CSV line considering quotes and commas inside fields
        /// Compatible with Excel format that adds quotes to all fields
        /// </summary>
        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = "";
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Two consecutive quotes = one literal quote
                        current += '"';
                        i++; // Skip next quote
                    }
                    else
                    {
                        // Toggle quote state
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // Comma outside quotes = field separator
                    result.Add(current.Trim('"')); // Remove aspas extras do Excel
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            
            // Add last field, removing extra quotes
            result.Add(current.Trim('"'));
            
            return result.ToArray();
        }

        /// <summary>
        /// Creates a sample CSV file for the user
        /// </summary>
        public static void CreateSampleCsv(string filePath)
        {
            var sampleContent = @"RecNo,Label,String
1,the exact same label must be here,This is an example string for entry 1.
2,the exact same label must be here,This is an example string for entry 2.";

            File.WriteAllText(filePath, sampleContent);
        }

        /// <summary>
        /// Exports category entries to CSV format
        /// </summary>
        public static bool ExportToCsv(string filePath, List<ImportedEntry> entries)
        {
            try
            {
                var csvContent = "RecNo,Label,String\n";
                
                foreach (var entry in entries)
                {
                    // Escape quotes in the data by doubling them
                    var label = entry.Label.Replace("\"", "\"\"");
                    var stringValue = entry.String.Replace("\"", "\"\"");
                    
                    // Add quotes around fields that contain commas, quotes, or newlines
                    if (label.Contains(",") || label.Contains("\"") || label.Contains("\n"))
                        label = $"\"{label}\"";
                    
                    if (stringValue.Contains(",") || stringValue.Contains("\"") || stringValue.Contains("\n"))
                        stringValue = $"\"{stringValue}\"";
                    
                    csvContent += $"{entry.RecNo},{label},{stringValue}\n";
                }
                
                File.WriteAllText(filePath, csvContent);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
