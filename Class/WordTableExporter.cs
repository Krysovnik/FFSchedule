using System;
using System.Collections.Generic;
using System.Text;
using FFSchedule.Models;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;

namespace FFSchedule.Class
{
    public class WordTableExporter
    {
        private readonly Dictionary<string, int> RankCounts = new()
        {
            { "1", 3 },
            { "1bis", 5 },
            { "2", 7 },
            { "3", 9 },
            { "4", 11 },
            { "5", 13 }
        };//ранги в шаблоне

        public void ExportSettlementsOnly(List<Settlement> settlements, string templatePath, string outputPath)
        {
            System.IO.File.Copy(templatePath, outputPath, true);

            using var doc = WordprocessingDocument.Open(outputPath, true);
            var body = doc.MainDocumentPart.Document.Body;

            var templateTable = body.Elements<Table>().FirstOrDefault();
            if (templateTable == null) return;

            var templateTableClone = (Table)templateTable.CloneNode(true);
            body.RemoveAllChildren<Table>();

            foreach (var s in settlements)
            {
                var table = (Table)templateTableClone.CloneNode(true);

                var cellSettlement = table.Descendants<TableCell>()
                    .FirstOrDefault(c => c.InnerText.Contains("{{SettlementName}}"));
                var depCell = table.Descendants<TableCell>()
                    .FirstOrDefault(c => c.InnerText.Contains("{{Department}}"));

                var equipments = s.EquipmentTypeQuantities?
                    .Where(x => x.Dpt != null && x.EtqTime != null && x.EtqQuantity != null && x.EtId != null)
                    .Select(x => new {
                        Dpt = x.Dpt,
                        TimeMinutes = int.TryParse(x.EtqTime.Replace(" мин", ""), out var t) ? t : int.MaxValue,
                        EtId = x.EtId.Value,
                        Quantity = x.EtqQuantity.Value
                    })
                    .OrderBy(x => x.TimeMinutes)
                    .ToList();

                
                List<string> GenerateRankLines(int rankCount)
                {
                    var lines = new List<string>();
                    var usedDpts = new HashSet<int>();

                    foreach (var eq in equipments)
                    {
                        if (usedDpts.Contains(eq.Dpt.DptId)) continue;
                        usedDpts.Add(eq.Dpt.DptId);

                        string equipmentType = eq.EtId switch
                        {
                            1 => "АЛ",
                            2 => "АЦ",
                            _ => "АЦ"
                        };

                        string name = !string.IsNullOrWhiteSpace(eq.Dpt.DptShort) ? eq.Dpt.DptShort : eq.Dpt.DptName;
                        lines.Add($"{eq.Quantity} {equipmentType} {name} ({eq.TimeMinutes} мин)");

                        if (lines.Count >= rankCount) break;
                    }

                    return lines;
                }

                // Заполнение ячеек рангов 
                foreach (var kvp in RankCounts)
                {
                    string rankName = kvp.Key;
                    int count = kvp.Value;

                    var forcesCell = table.Descendants<TableCell>()
                        .FirstOrDefault(c => c.InnerText.Contains($"{{InvolvedForcesRank{rankName}}}"));
                    var timeCell = table.Descendants<TableCell>()
                        .FirstOrDefault(c => c.InnerText.Contains($"{{TimeRank{rankName}}}"));

                    if (forcesCell != null && timeCell != null)
                    {
                        var forcesParagraph = forcesCell.Elements<Paragraph>().FirstOrDefault();
                        var timeParagraph = timeCell.Elements<Paragraph>().FirstOrDefault();

                        forcesParagraph.RemoveAllChildren<Run>();
                        timeParagraph.RemoveAllChildren<Run>();

                        var lines = GenerateRankLines(count);

                        foreach (var line in lines)
                        {
                            //"1 АЦ псч-7 (10 мин)"
                            var splitIndex = line.LastIndexOf('(');
                            string equipmentText = splitIndex > 0 ? line.Substring(0, splitIndex).Trim() : line;
                            string timeText = splitIndex > 0 ? line.Substring(splitIndex + 1).Replace(")", "").Trim() : "";

                            forcesParagraph.Append(new Run(new Text(equipmentText)));
                            timeParagraph.Append(new Run(new Text(timeText)));

                            forcesParagraph.Append(new Run(new Break()));
                            timeParagraph.Append(new Run(new Break()));
                        }
                    }
                }

                // населенный пункт
                if (cellSettlement != null)
                {
                    var paragraph = cellSettlement.Elements<Paragraph>().FirstOrDefault();
                    string fullName = $"{s.Tol?.TolShortName} {s.SeName} {s.Vc?.VcName} с/с.";
                    paragraph.RemoveAllChildren<Run>();
                    paragraph.Append(new Run(new Text(fullName)));
                }

                // подразделение пожарной охраны
                if (depCell != null)
                {
                    var paragraph = depCell.Elements<Paragraph>().FirstOrDefault();
                    paragraph.RemoveAllChildren<Run>();

                    var departments = s.SettlementMainDepartaments?.Where(x => x.Dpt != null)
                        .Select(x => x.Dpt.DptName)
                        .ToList();

                    string depText = (departments != null && departments.Count > 0) ? string.Join(", ", departments) : "—";
                    paragraph.Append(new Run(new Text(depText)));
                }

                body.Append(table);
                body.Append(new Paragraph(new Run(new Break() { Type = BreakValues.Page })));
            }

            doc.MainDocumentPart.Document.Save();
        }
    }
}