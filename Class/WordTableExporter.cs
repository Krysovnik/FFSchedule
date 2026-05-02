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
        };

        public void ExportSettlementsOnly(List<Settlement> settlements, string templatePath, string outputPath)
        {
            File.Copy(templatePath, outputPath, true);

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

                //сортировка по времени
                var orderedDepartments = s.EquipmentTypeQuantities?
                    .Where(x => x.Dpt != null && x.EtqTime != null)
                    .Select(x => new
                    {
                        Dpt = x.Dpt,
                        TimeMinutes = int.TryParse(x.EtqTime.Replace(" мин", ""), out var t)
                            ? t
                            : int.MaxValue
                    })
                    .GroupBy(x => x.Dpt.DptId)
                    .Select(g => new
                    {
                        Dpt = g.First().Dpt,
                        TimeMinutes = g.Min(x => x.TimeMinutes)
                    })
                    .OrderBy(x => x.TimeMinutes)
                    .ToList();

                //общее
                var result = new List<(string text, string time)>();

                var usedDpts = new HashSet<int>();
                bool ladderAdded = false;

                foreach (var d in orderedDepartments)
                {
                    var dept = d.Dpt;
                    if (usedDpts.Contains(dept.DptId))
                        continue;

                    usedDpts.Add(dept.DptId);

                    string name = !string.IsNullOrWhiteSpace(dept.DptShort)
                        ? dept.DptShort
                        : dept.DptName;

                    int firetrucks = dept.DptFiretrucks ?? 0;

                    //АЦ — 1 от ПЧ
                    if (firetrucks > 0)
                    {
                        result.Add(($"1 АЦ {name}", $"{d.TimeMinutes} мин"));
                    }

                    //АЛ - только 1 на пожар
                    if (!ladderAdded && dept.DptHasLadder == 1)
                    {
                        result.Add(($"1 АЛ {name}", $"{d.TimeMinutes} мин"));
                        ladderAdded = true;
                    }
                }

                //добавление АЛ
                if (!ladderAdded)
                {
                    var ladder = orderedDepartments
                        .FirstOrDefault(x => x.Dpt.DptHasLadder == 1);

                    if (ladder != null)
                    {
                        string name = !string.IsNullOrWhiteSpace(ladder.Dpt.DptShort)
                            ? ladder.Dpt.DptShort
                            : ladder.Dpt.DptName;

                        result.Add(($"1 АЛ {name}", $"{ladder.TimeMinutes} мин"));
                    }
                }

                //ранги одной таблицы
                foreach (var kvp in RankCounts)
                {
                    string rankName = kvp.Key;
                    int count = kvp.Value;

                    var forcesCell = table.Descendants<TableCell>()
                        .FirstOrDefault(c => c.InnerText.Contains($"{{InvolvedForcesRank{rankName}}}"));

                    var timeCell = table.Descendants<TableCell>()
                        .FirstOrDefault(c => c.InnerText.Contains($"{{TimeRank{rankName}}}"));

                    if (forcesCell == null || timeCell == null)
                        continue;

                    var forcesParagraph = forcesCell.Elements<Paragraph>().FirstOrDefault();
                    var timeParagraph = timeCell.Elements<Paragraph>().FirstOrDefault();

                    forcesParagraph?.RemoveAllChildren<Run>();
                    timeParagraph?.RemoveAllChildren<Run>();

                    var slice = result.Take(count).ToList();

                    foreach (var item in slice)
                    {
                        forcesParagraph?.Append(new Run(new Text(item.text)));
                        timeParagraph?.Append(new Run(new Text(item.time)));

                        forcesParagraph?.Append(new Run(new Break()));
                        timeParagraph?.Append(new Run(new Break()));
                    }
                }

                //населенный пункт
                if (cellSettlement != null)
                {
                    var paragraph = cellSettlement.Elements<Paragraph>().FirstOrDefault();
                    string fullName = $"{s.Tol?.TolShortName} {s.SeName} {s.Vc?.VcName} с/с.";

                    paragraph?.RemoveAllChildren<Run>();
                    paragraph?.Append(new Run(new Text(fullName)));
                }

                
                //подразделения пожарной охраны
                if (depCell != null)
                {
                    var paragraph = depCell.Elements<Paragraph>().FirstOrDefault();
                    paragraph?.RemoveAllChildren<Run>();

                    var departments = s.SettlementMainDepartaments?
                        .Where(x => x.Dpt != null)
                        .Select(x => x.Dpt.DptName)
                        .ToList();

                    string depText = (departments != null && departments.Count > 0)
                        ? string.Join(", ", departments)
                        : "—";

                    paragraph?.Append(new Run(new Text(depText)));
                }

                body.Append(table);
                body.Append(new Paragraph(new Run(new Break() { Type = BreakValues.Page })));
            }

            doc.MainDocumentPart.Document.Save();
        }
    }
}