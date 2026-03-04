using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.UI.Wpf;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;

namespace FFSchedule.Services
{
    public class SearchService
    {
        private readonly HttpClient _httpClient;
        private readonly MapControl _mapControl;
        private const string SEARCH_PIN_LAYER = "SearchPin";

        public SearchService(HttpClient httpClient, MapControl mapControl)
        {
            _httpClient = httpClient;
            _mapControl = mapControl;
        }

        #region Публичные методы

        /// Выполняет поиск по запросу и возвращает отсортированный список результатов.
        public async Task<List<NominatimResult>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<NominatimResult>();

            query = $"{query}, Новосибирск";

            var url = $"https://nominatim.openstreetmap.org/search" +
                      $"?q={Uri.EscapeDataString(query)}" +
                      $"&format=jsonv2" +
                      $"&addressdetails=1" +
                      $"&extratags=1" +
                      $"&countrycodes=RU" +
                      $"&bounded=1" +
                      $"&limit=5";

            var results = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(url);
            return results?.OrderByDescending(r => r.Importance).ToList() ?? new List<NominatimResult>();
        }

        /// Размещает маркер на карте по координатам результата.
        public void PutSearchPin(NominatimResult result)
        {
            var merc = Mapsui.Projections.SphericalMercator.FromLonLat(result.Lon, result.Lat);

            // Удаляем старый маркер, если он есть
            var old = _mapControl.Map?.Layers.FirstOrDefault(l => l.Name == SEARCH_PIN_LAYER);
            if (old != null) _mapControl.Map.Layers.Remove(old);

            var pin = new GeometryFeature
            {
                Geometry = new Point(merc.x, merc.y),
                ["label"] = result.DisplayName,
                ["shortLabel"] = result.ShortDisplayName
            };

            // Стиль маркера
            pin.Styles.Add(new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                Fill = new Brush(Color.FromArgb(255, 255, 50, 50)),
                Outline = new Pen(new Color(255, 255, 255, 255), 3.0f) { PenStyle = PenStyle.Solid },
                SymbolScale = 0.35f,
                MinVisible = 1,
                MaxVisible = 500
            });

            // Стиль подписи
            pin.Styles.Add(new LabelStyle
            {
                Text = result.ShortDisplayName,
                Font = new Font { Size = 12, Bold = true, FontFamily = "Arial" },
                ForeColor = new Color(0, 0, 0),
                BackColor = new Brush(new Color(255, 255, 255, 220)),
                Halo = new Pen(new Color(255, 255, 255, 200), 1.5f),
                Offset = new Offset(0.0, -18),
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                LineHeight = 1.2,
                MaxVisible = 80
            });

            _mapControl.Map?.Layers.Add(new MemoryLayer
            {
                Name = SEARCH_PIN_LAYER,
                Features = new[] { pin },
                Style = null
            });
        }

        /// Перемещает карту к координатам результата и ставит маркер.
        public void FlyToResult(NominatimResult result)
        {
            var merc = Mapsui.Projections.SphericalMercator.FromLonLat(result.Lon, result.Lat);
            _mapControl.Map?.Navigator?.CenterOn(merc.x, merc.y);
            _mapControl.Map?.Navigator?.ZoomToLevel(15);
            PutSearchPin(result);
        }

        #endregion
    }
}
