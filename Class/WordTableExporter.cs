using System;
using System.Collections.Generic;
using System.Text;
using FFSchedule.Models;
using System.IO;
//using Xceed.Document.NET;
//using Xceed.Words.NET;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace FFSchedule.Class
{
    public class WordTableExporter
    {
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

                var cell = table.Descendants<TableCell>().FirstOrDefault(c => c.InnerText.Contains("{{SettlementName}}"));

                if (cell != null)
                {
                    var paragraph = cell.Elements<Paragraph>().FirstOrDefault();
                    string fullName = $"{s.Tol?.TolShortName} {s.SeName} {s.Vc?.VcName} с/с.";
                    paragraph.RemoveAllChildren<Run>();

                    paragraph.Append(new Run(new Text(fullName)));
                }

                body.Append(table);

                body.Append(new Paragraph(new Run(new Break() { Type = BreakValues.Page })));
            }

            doc.MainDocumentPart.Document.Save();
        }
    }
}
