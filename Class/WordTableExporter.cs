using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FFSchedule.Models;

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

            // -------------------- helper: стиль текста --------------------
            Run CreateRun(string text, bool preserveLineBreaks = false)
            {
                var run = new Run(
                    new RunProperties(
                        new RunFonts
                        {
                            Ascii = "Times New Roman",
                            HighAnsi = "Times New Roman"
                        },
                        new FontSize { Val = "18" } // 9pt
                    )
                );

                if (!preserveLineBreaks)
                {
                    run.Append(new Text(text ?? ""));
                }
                else
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        run.Append(new Text(""));
                    }
                    else
                    {
                        var parts = text.Split('\n');
                        for (int i = 0; i < parts.Length; i++)
                        {
                            run.Append(new Text(parts[i]));
                            if (i < parts.Length - 1)
                                run.Append(new Break());
                        }
                    }
                }

                return run;
            }

            foreach (var s in settlements)
            {
                var table = (Table)templateTableClone.CloneNode(true);

                // -------------------- helper replace --------------------
                void ReplaceCell(string placeholder, string value, bool preserveBreaks = false)
                {
                    var cell = table.Descendants<TableCell>()
                        .FirstOrDefault(c => c.InnerText.Contains(placeholder));

                    if (cell == null) return;

                    var paragraph = cell.Elements<Paragraph>().FirstOrDefault();
                    if (paragraph == null) return;

                    paragraph.RemoveAllChildren<Run>();
                    paragraph.Append(CreateRun(value, preserveBreaks));
                }

                // -------------------- OPTKP --------------------
                string involvedForcesRescue = "";
                string rescueTypes = "";
                string rescueTotal = "";
                string timeRescue = "240 мин";

                if (s.Optkp == 1)
                {
                    involvedForcesRescue =
                        "4 АЦ опткп-1\n2 АНР опткп-1\n2 ПНС опткп-1\n2 АР опткп-1";

                    rescueTypes = "АЦ-4, АНР-2, ПНС-2, АР-2";
                    rescueTotal = "10";
                }
                else if (s.Optkp == 2)
                {
                    involvedForcesRescue =
                        "5 АЦ опткп-2\n1 АНР опткп-2\n1 АЛ опткп-2";

                    rescueTypes = "АЦ-5, АНР-1, АЛ-1";
                    rescueTotal = "7";
                }

                ReplaceCell("{{InvolvedForcesRescue}}", involvedForcesRescue, true);
                ReplaceCell("{{TimeRescue}}", timeRescue);
                ReplaceCell("{{RescueTypes}}", rescueTypes);
                ReplaceCell("{{RescueTotal}}", rescueTotal);

                // -------------------- сортировка подразделений --------------------
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

                var result = new List<(string text, string time)>();
                var usedDpts = new HashSet<int>();
                bool ladderAdded = false;

                foreach (var d in orderedDepartments)
                {
                    var dept = d.Dpt;
                    if (!usedDpts.Add(dept.DptId))
                        continue;

                    string name = !string.IsNullOrWhiteSpace(dept.DptShort)
                        ? dept.DptShort
                        : dept.DptName;

                    if ((dept.DptFiretrucks ?? 0) > 0)
                    {
                        result.Add(($"1 АЦ {name}", $"{d.TimeMinutes} мин"));
                    }

                    if (!ladderAdded && dept.DptHasLadder == 1)
                    {
                        result.Add(($"1 АЛ {name}", $"{d.TimeMinutes} мин"));
                        ladderAdded = true;
                    }
                }

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

                // -------------------- ранги --------------------
                foreach (var kvp in RankCounts)
                {
                    var forcesCell = table.Descendants<TableCell>()
                        .FirstOrDefault(c => c.InnerText.Contains($"{{InvolvedForcesRank{kvp.Key}}}"));

                    var timeCell = table.Descendants<TableCell>()
                        .FirstOrDefault(c => c.InnerText.Contains($"{{TimeRank{kvp.Key}}}"));

                    if (forcesCell == null || timeCell == null)
                        continue;

                    var forcesParagraph = forcesCell.Elements<Paragraph>().FirstOrDefault();
                    var timeParagraph = timeCell.Elements<Paragraph>().FirstOrDefault();

                    forcesParagraph?.RemoveAllChildren<Run>();
                    timeParagraph?.RemoveAllChildren<Run>();

                    var slice = result.Take(kvp.Value).ToList();

                    foreach (var item in slice)
                    {
                        forcesParagraph?.Append(CreateRun(item.text));
                        timeParagraph?.Append(CreateRun(item.time));

                        forcesParagraph?.Append(new Run(new Break()));
                        timeParagraph?.Append(new Run(new Break()));
                    }
                }

                // -------------------- населённый пункт --------------------
                var settlementCell = table.Descendants<TableCell>()
                    .FirstOrDefault(c => c.InnerText.Contains("{{SettlementName}}"));

                if (settlementCell != null)
                {
                    var p = settlementCell.Elements<Paragraph>().FirstOrDefault();

                    string fullName =
                        $"{s.Tol?.TolShortName} {s.SeName} {s.Vc?.VcName} с/с.";

                    p?.RemoveAllChildren<Run>();
                    p?.Append(CreateRun(fullName));
                }

                // -------------------- подразделения --------------------
                var depCell = table.Descendants<TableCell>()
                    .FirstOrDefault(c => c.InnerText.Contains("{{Department}}"));

                if (depCell != null)
                {
                    var p = depCell.Elements<Paragraph>().FirstOrDefault();

                    var departments = s.SettlementMainDepartaments?
                        .Where(x => x.Dpt != null)
                        .Select(x => x.Dpt.DptName)
                        .ToList();

                    string depText = (departments != null && departments.Count > 0)
                        ? string.Join(", ", departments)
                        : "—";

                    p?.RemoveAllChildren<Run>();
                    p?.Append(CreateRun(depText));
                }

                body.Append(table);
                body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
            }

            doc.MainDocumentPart.Document.Save();
        }
    }
}