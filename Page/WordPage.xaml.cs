using FFSchedule.Class;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FFSchedule.Page
{
    /// <summary>
    /// Логика взаимодействия для WordPage.xaml
    /// </summary>
    public partial class WordPage : System.Windows.Controls.Page
    {
        private readonly MainWindow _mainWindow;
        public WordPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }
        private void GenerateWordTable_Click(object sender, RoutedEventArgs e)
        {
            var settlements = _mainWindow._dbcontext.Settlements
                .Include(s => s.Vc)
                .Include(s => s.Tol)
                .Include(s => s.SettlementMainDepartaments)
                .ThenInclude(smd => smd.Dpt)
                .Include(s => s.SettlementDepartamentDistances)
                .ThenInclude(sdd => sdd.Dpt)
                .Include(s => s.EquipmentTypeQuantities)
                .ThenInclude(etq => etq.Dpt)
                .ToList();

            var exporter = new WordTableExporter();
            string templatePath = @"FFS/template.docx";
            string outputPath = @"FFS/Schedule.docx";
            exporter.ExportSettlementsOnly(settlements, templatePath, outputPath);
            MessageBox.Show("Word документ создан!");
        }

        private void AddDepartment_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.StartAddDepartmentMode();
        }

        private void DeleteDepartment_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Выберите ПЧ на карте для удаления");
            _mainWindow.StartDeleteDepartmentMode();
        }
    }
}
