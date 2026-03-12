using System;
using System.Collections.Generic;
using System.Text;
using FFSchedule.Models;
using System.IO;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace FFSchedule.Class
{
    public class WordTableGenerator
    {
        public void AddEmployeeTableToWord(string wordFilePath, List<Employee> employees)
        {
            if (!File.Exists(wordFilePath))
                throw new FileNotFoundException($"Файл Word не найден: {wordFilePath}");

            try
            {
                // Открываем существующий .docx файл
                using (var document = DocX.Load(wordFilePath))
                {
                    var paragraph = document.InsertParagraph("\nСписок сотрудников:", false);
                    paragraph.FontSize(14).Bold().SpacingAfter(10);

                    var table = document.InsertTable(employees.Count + 1, 5);
                    table.Design = TableDesign.TableGrid; 

                    table.Rows[0].Cells[0].Paragraphs[0].Append("ID").Bold().FontSize(12);
                    table.Rows[0].Cells[1].Paragraphs[0].Append("Логин").Bold().FontSize(12);
                    table.Rows[0].Cells[2].Paragraphs[0].Append("ФИО").Bold().FontSize(12);
                    table.Rows[0].Cells[3].Paragraphs[0].Append("Пароль").Bold().FontSize(12);
                    table.Rows[0].Cells[4].Paragraphs[0].Append("Роль ID").Bold().FontSize(12);

                    for (int i = 0; i < employees.Count; i++)
                    {
                        var emp = employees[i];
                        table.Rows[i + 1].Cells[0].Paragraphs[0].Append(emp.EmId.ToString());
                        table.Rows[i + 1].Cells[1].Paragraphs[0].Append(emp.EmLogin ?? "");
                        table.Rows[i + 1].Cells[2].Paragraphs[0].Append(emp.EmFio ?? "");
                        table.Rows[i + 1].Cells[3].Paragraphs[0].Append(emp.EmPassword ?? "");
                        table.Rows[i + 1].Cells[4].Paragraphs[0].Append(emp.RoId?.ToString() ?? "");
                    }

                    table.AutoFit = AutoFit.Window;

                    document.Save();
                }

                Console.WriteLine("Таблица успешно добавлена в Word-документ.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                throw;
            }
        }
    }
}
