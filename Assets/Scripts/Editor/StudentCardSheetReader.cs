using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

public static class StudentCardSheetReader
{
    public static List<Dictionary<string, string>> Read(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension == ".csv")
        {
            return ReadCsv(path);
        }

        if (extension == ".xlsx")
        {
            return ReadFirstSheetXlsx(path);
        }

        throw new InvalidOperationException("Unsupported card table file type: " + extension);
    }

    private static List<Dictionary<string, string>> ReadCsv(string path)
    {
        var text = Encoding.UTF8.GetString(ReadSharedFileBytes(path));
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        if (lines.Count == 0)
        {
            return new List<Dictionary<string, string>>();
        }

        var separator = DetectSeparator(lines[0]);
        var headers = ParseDelimitedLine(lines[0], separator);
        var rows = new List<Dictionary<string, string>>();
        for (var i = 1; i < lines.Count; i++)
        {
            var values = ParseDelimitedLine(lines[i], separator);
            rows.Add(ToDictionary(headers, values));
        }

        return rows;
    }

    private static char DetectSeparator(string headerLine)
    {
        var semicolonCount = headerLine.Count(c => c == ';');
        var commaCount = headerLine.Count(c => c == ',');
        return semicolonCount >= commaCount ? ';' : ',';
    }

    private static List<string> ParseDelimitedLine(string line, char separator)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == separator && !inQuotes)
            {
                values.Add(current.ToString());
                current.Length = 0;
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString());
        return values;
    }

    private static List<Dictionary<string, string>> ReadFirstSheetXlsx(string path)
    {
        using (var stream = new MemoryStream(ReadSharedFileBytes(path)))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            var sharedStrings = ReadSharedStrings(archive);
            var sheetEntry = FindFirstWorksheetEntry(archive);
            if (sheetEntry == null)
            {
                throw new InvalidOperationException("Could not find first worksheet in xlsx.");
            }

            var rows = ReadSheetRows(sheetEntry, sharedStrings);
            if (rows.Count == 0)
            {
                return new List<Dictionary<string, string>>();
            }

            var headers = rows[0];
            return rows.Skip(1).Select(values => ToDictionary(headers, values)).ToList();
        }
    }

    private static byte[] ReadSharedFileBytes(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }

    private static ZipArchiveEntry FindFirstWorksheetEntry(ZipArchive archive)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml");
        var relationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
        if (workbookEntry != null && relationshipsEntry != null)
        {
            var workbook = LoadXml(workbookEntry);
            var workbookNamespaceManager = CreateNamespaceManager(workbook);
            var firstSheet = workbook.SelectSingleNode("//x:sheets/x:sheet", workbookNamespaceManager);
            var relationId = firstSheet != null && firstSheet.Attributes != null
                ? firstSheet.Attributes["id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"]?.Value
                : null;

            if (!string.IsNullOrWhiteSpace(relationId))
            {
                var relationships = LoadXml(relationshipsEntry);
                var relationshipsNamespaceManager = new XmlNamespaceManager(relationships.NameTable);
                relationshipsNamespaceManager.AddNamespace("r", "http://schemas.openxmlformats.org/package/2006/relationships");
                var relationship = relationships.SelectSingleNode("//r:Relationship[@Id='" + relationId + "']", relationshipsNamespaceManager);
                var target = relationship != null && relationship.Attributes != null ? relationship.Attributes["Target"]?.Value : null;
                var entryPath = ToWorkbookEntryPath(target);
                var entry = !string.IsNullOrWhiteSpace(entryPath) ? archive.GetEntry(entryPath) : null;
                if (entry != null)
                {
                    return entry;
                }
            }
        }

        return archive.Entries
            .Where(entry => entry.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase)
                && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string ToWorkbookEntryPath(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return "";
        }

        var normalized = target.Replace('\\', '/').TrimStart('/');
        return normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ? normalized : "xl/" + normalized;
    }

    private static Dictionary<string, string> ToDictionary(List<string> headers, List<string> values)
    {
        var dict = new Dictionary<string, string>();
        for (var c = 0; c < headers.Count; c++)
        {
            var header = headers[c];
            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            dict[header.Trim()] = c < values.Count ? values[c].Trim() : "";
        }

        return dict;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        var result = new List<string>();
        if (entry == null)
        {
            return result;
        }

        var doc = LoadXml(entry);
        var namespaceManager = CreateNamespaceManager(doc);
        foreach (XmlNode node in doc.SelectNodes("//x:si", namespaceManager))
        {
            var texts = node.SelectNodes(".//x:t", namespaceManager);
            result.Add(string.Concat(texts.Cast<XmlNode>().Select(n => n.InnerText)));
        }

        return result;
    }

    private static List<List<string>> ReadSheetRows(ZipArchiveEntry entry, List<string> sharedStrings)
    {
        var doc = LoadXml(entry);
        var namespaceManager = CreateNamespaceManager(doc);
        var output = new List<List<string>>();

        foreach (XmlNode rowNode in doc.SelectNodes("//x:sheetData/x:row", namespaceManager))
        {
            var row = new List<string>();
            foreach (XmlNode cellNode in rowNode.SelectNodes("x:c", namespaceManager))
            {
                var cellReference = cellNode.Attributes["r"] != null ? cellNode.Attributes["r"].Value : "";
                var columnIndex = GetColumnIndex(cellReference);
                while (row.Count <= columnIndex)
                {
                    row.Add("");
                }

                row[columnIndex] = ReadCellValue(cellNode, sharedStrings, namespaceManager);
            }

            output.Add(row);
        }

        return output;
    }

    private static string ReadCellValue(XmlNode cellNode, List<string> sharedStrings, XmlNamespaceManager namespaceManager)
    {
        var type = cellNode.Attributes["t"] != null ? cellNode.Attributes["t"].Value : "";
        if (type == "inlineStr")
        {
            var inlineText = cellNode.SelectSingleNode(".//x:t", namespaceManager);
            return inlineText != null ? inlineText.InnerText : "";
        }

        var valueNode = cellNode.SelectSingleNode("x:v", namespaceManager);
        if (valueNode == null)
        {
            return "";
        }

        if (type == "s" && int.TryParse(valueNode.InnerText, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedIndex];
        }

        return valueNode.InnerText;
    }

    private static XmlDocument LoadXml(ZipArchiveEntry entry)
    {
        var doc = new XmlDocument();
        using (var stream = entry.Open())
        {
            doc.Load(stream);
        }
        return doc;
    }

    private static XmlNamespaceManager CreateNamespaceManager(XmlDocument doc)
    {
        var namespaceManager = new XmlNamespaceManager(doc.NameTable);
        namespaceManager.AddNamespace("x", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
        return namespaceManager;
    }

    private static int GetColumnIndex(string cellReference)
    {
        var letters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        if (string.IsNullOrEmpty(letters))
        {
            return 0;
        }

        var index = 0;
        foreach (var letter in letters.ToUpperInvariant())
        {
            index = index * 26 + (letter - 'A' + 1);
        }
        return index - 1;
    }
}
