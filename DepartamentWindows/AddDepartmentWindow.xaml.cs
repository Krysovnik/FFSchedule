using FFSchedule.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FFSchedule.DepartamentWindows.JsonModels;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FFSchedule.DepartamentWindows
{
    /// <summary>
    /// Логика взаимодействия для AddDepartmentWindow.xaml
    /// </summary>
    public partial class AddDepartmentWindow : Window
    {
        private readonly FfsContext _db;

        private readonly double _latitude;
        private readonly double _longitude;

        public AddDepartmentWindow(FfsContext db, double latitude, double longitude)
        {
            InitializeComponent();

            _db = db;

            _latitude = latitude;
            _longitude = longitude;

            TbCoordinates.Text = $"{latitude:F6}, {longitude:F6}";

            LoadData();
        }

        private void LoadData()
        {
            CbDepartmentType.ItemsSource = _db.DepartmentTypes.ToList();

            CbDepartmentType.DisplayMemberPath = "DtName";

            CbDepartmentType.SelectedValuePath = "DtId";

            CbFireBrigade.ItemsSource = _db.FireBrigades.ToList();

            CbFireBrigade.DisplayMemberPath = "FbName";

            CbFireBrigade.SelectedValuePath = "FbId";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbName.Text))
            {
                MessageBox.Show("Введите название");
                return;
            }

            int newId = _db.Departments.Select(d => (int?)d.DptId).Max() ?? 0;
            newId++;

            var department = new Department
            {
                DptId = newId,
                DptName = TbName.Text,
                DptShort = TbShort.Text,
                DptAddress = TbAddress.Text,
                DptPhoneNum = TbPhone.Text,
                DtId = (int?)CbDepartmentType.SelectedValue,
                FbId = (int?)CbFireBrigade.SelectedValue,
                DptFiretrucks = int.TryParse(TbFiretrucks.Text, out int trucks) ? trucks : 0,
                DptHasLadder = ChkLadder.IsChecked == true ? 1 : 0
            };

            _db.Departments.Add(department);
            _db.SaveChanges();

            AddToGeoJson(department);

            MessageBox.Show("Пожарная часть добавлена");

            DialogResult = true;
            Close();
        }

        private void AddToGeoJson(Department dept)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MapVector", "FireStationPoints.geojson");

            if (!File.Exists(path)) throw new FileNotFoundException("GeoJSON не найден", path);

            string json = File.ReadAllText(path);

            var geo = JsonSerializer.Deserialize<GeoJson>(json);

            if (geo?.features == null) geo.features = new List<Feature>();

            geo.features.Add(new Feature
            {
                type = "Feature",
                properties = new Dictionary<string, object>
                {
                    { "fid", dept.DptId },
                    { "name", dept.DptName },
                    { "address", dept.DptAddress },
                    { "district", "" },
                    { "type", "пч" },
                    { "phone", dept.DptPhoneNum }
                },

                geometry = new Geometry
                {
                    type = "Point",
                    coordinates = new[] { _longitude, _latitude }
                }
            });

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            File.WriteAllText(path, JsonSerializer.Serialize(geo, options));
        }
    }
}
