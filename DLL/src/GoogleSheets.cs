using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace iandouglas736
{
    /// <summary>
    /// Helpers for reading public Google Sheets as nested dictionaries.
    /// No authentication is required; the sheet must be publicly viewable.
    /// </summary>
    public static class GoogleSheets
    {
        /// <summary>
        /// Reads a public Google Sheet and returns a dictionary keyed by the first column.
        /// 
        /// Parameters:
        ///   googleSheetUrl  - The browser URL of the sheet, e.g.
        ///                     https://docs.google.com/spreadsheets/d/XXXX/edit?usp=sharing
        ///   sheetName       - The sheet/tab name to read. Defaults to "Sheet1".
        ///   duplicateMode   - How to handle repeated keys. Defaults to LastEntryWins.
        ///   timeoutSeconds  - HTTP timeout. Defaults to 30 seconds.
        /// 
        /// Result:
        ///   Dictionary<string, object> where each key is a value from the first column.
        ///   In LastEntryWins mode, the value is a Dictionary<string, object>.
        ///   In BuildList mode, the value is List<Dictionary<string, object>>.
        /// 
        /// Empty rows are skipped. Empty cells become null. Header row is required.
        /// </summary>
        public static Dictionary<string, object> ReadFile(
            string googleSheetUrl,
            string sheetName = "Sheet1",
            SheetDuplicateKeyMode duplicateMode = SheetDuplicateKeyMode.LastEntryWins,
            int timeoutSeconds = 30)
        {
            if (string.IsNullOrWhiteSpace(googleSheetUrl))
                throw new ArgumentException("Google Sheet URL is required.", nameof(googleSheetUrl));

            string csvUrl = BuildCsvExportUrl(googleSheetUrl, sheetName);
            string csvText = DownloadCsv(csvUrl, timeoutSeconds);

            if (string.IsNullOrWhiteSpace(csvText))
                return new Dictionary<string, object>();

            return ParseCsv(csvText, duplicateMode);
        }

        /// <summary>
        /// Async version of ReadFile. Useful if called from non-Streamer.bot contexts.
        /// </summary>
        public static async Task<Dictionary<string, object>> ReadFileAsync(
            string googleSheetUrl,
            string sheetName = "Sheet1",
            SheetDuplicateKeyMode duplicateMode = SheetDuplicateKeyMode.LastEntryWins,
            int timeoutSeconds = 30)
        {
            if (string.IsNullOrWhiteSpace(googleSheetUrl))
                throw new ArgumentException("Google Sheet URL is required.", nameof(googleSheetUrl));

            string csvUrl = BuildCsvExportUrl(googleSheetUrl, sheetName);
            string csvText = await DownloadCsvAsync(csvUrl, timeoutSeconds);

            if (string.IsNullOrWhiteSpace(csvText))
                return new Dictionary<string, object>();

            return ParseCsv(csvText, duplicateMode);
        }

        /// <summary>
        /// Converts a Google Sheets browser URL into the gviz CSV export URL.
        /// </summary>
        public static string BuildCsvExportUrl(string googleSheetUrl, string sheetName = "Sheet1")
        {
            string docId = ExtractDocumentId(googleSheetUrl);
            if (string.IsNullOrEmpty(docId))
                throw new ArgumentException("Could not extract a Google Sheet document ID from the URL.", nameof(googleSheetUrl));

            return $"https://docs.google.com/spreadsheets/d/{docId}/gviz/tq?tqx=out:csv&sheet={Uri.EscapeDataString(sheetName ?? "Sheet1")}";
        }

        private static string ExtractDocumentId(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            // Match /d/<id>/ or /d/<id>
            var match = Regex.Match(url, @"/d/([a-zA-Z0-9\-_]+)");
            if (match.Success)
                return match.Groups[1].Value;

            return null;
        }

        private static string DownloadCsv(string csvUrl, int timeoutSeconds)
        {
            using (var client = new WebClient { Encoding = System.Text.Encoding.UTF8 })
            {
                client.Encoding = System.Text.Encoding.UTF8;
                return client.DownloadString(csvUrl);
            }
        }

        private static async Task<string> DownloadCsvAsync(string csvUrl, int timeoutSeconds)
        {
            using (var client = new WebClient { Encoding = System.Text.Encoding.UTF8 })
            {
                return await client.DownloadStringTaskAsync(csvUrl);
            }
        }

        private static Dictionary<string, object> ParseCsv(string csvText, SheetDuplicateKeyMode duplicateMode)
        {
            var result = new Dictionary<string, object>();
            List<string[]> rows = SplitCsv(csvText);

            if (rows.Count == 0)
                return result;

            string[] headers = rows[0];
            if (headers.Length == 0)
                return result;

            for (int i = 1; i < rows.Count; i++)
            {
                string[] cells = rows[i];
                if (cells.Length == 0)
                    continue;

                string key = cells[0]?.Trim();
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                var rowDict = new Dictionary<string, object>();
                for (int col = 0; col < headers.Length; col++)
                {
                    string columnName = headers[col]?.Trim();
                    if (string.IsNullOrWhiteSpace(columnName))
                        continue;

                    string rawValue = col < cells.Length ? cells[col] : null;
                    rowDict[columnName] = InferValue(rawValue);
                }

                if (duplicateMode == SheetDuplicateKeyMode.BuildList)
                {
                    if (result.ContainsKey(key))
                    {
                        if (result[key] is List<Dictionary<string, object>> list)
                        {
                            list.Add(rowDict);
                        }
                        else if (result[key] is Dictionary<string, object> firstRow)
                        {
                            result[key] = new List<Dictionary<string, object>> { firstRow, rowDict };
                        }
                    }
                    else
                    {
                        // Store the first row directly. If a duplicate appears later, we upgrade to a list.
                        result[key] = rowDict;
                    }
                }
                else
                {
                    result[key] = rowDict;
                }
            }

            return result;
        }

        private static List<string[]> SplitCsv(string csvText)
        {
            var rows = new List<string[]>();
            using (var reader = new StringReader(csvText))
            {
                string line;
                var currentFields = new List<string>();
                var currentField = new System.Text.StringBuilder();
                bool insideQuotes = false;

                while ((line = reader.ReadLine()) != null)
                {
                    for (int i = 0; i < line.Length; i++)
                    {
                        char c = line[i];

                        if (c == '"')
                        {
                            if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                            {
                                // Escaped quote
                                currentField.Append('"');
                                i++;
                            }
                            else
                            {
                                insideQuotes = !insideQuotes;
                            }
                        }
                        else if (c == ',' && !insideQuotes)
                        {
                            currentFields.Add(currentField.ToString());
                            currentField.Clear();
                        }
                        else
                        {
                            currentField.Append(c);
                        }
                    }

                    if (insideQuotes)
                    {
                        // Multi-line field: keep reading into the same field
                        currentField.AppendLine();
                    }
                    else
                    {
                        currentFields.Add(currentField.ToString());
                        rows.Add(currentFields.ToArray());
                        currentFields.Clear();
                        currentField.Clear();
                    }
                }

                // Handle file ending while still inside a quoted field
                if (currentField.Length > 0 || currentFields.Count > 0)
                {
                    currentFields.Add(currentField.ToString());
                    rows.Add(currentFields.ToArray());
                }
            }

            return rows;
        }

        private static object InferValue(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return null;

            string value = rawValue.Trim();

            if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;

            if (long.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out long longResult))
                return longResult;

            if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double doubleResult))
                return doubleResult;

            return value;
        }
    }
}
