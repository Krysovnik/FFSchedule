using DocumentFormat.OpenXml.Features;
using FFSchedule.Container;
using FFSchedule.Models;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FFSchedule.Services
{
    public class MapDataService
    {
        public GeoJsonLoadResult ParseGeoJson(string geojsonPath)
        {
            if (string.IsNullOrEmpty(geojsonPath))
                throw new ArgumentException("Путь к файлу не может быть пустым", nameof(geojsonPath));

            if (!File.Exists(geojsonPath))
                throw new FileNotFoundException($"Файл GeoJSON не найден: {geojsonPath}");

            var result = new GeoJsonLoadResult();
            string geojson = File.ReadAllText(geojsonPath);

            var reader = new GeoJsonReader();
            var fc = reader.Read<NetTopologySuite.Features.FeatureCollection>(geojson);

            foreach (var f in fc)
            {
                var geom = f.Geometry;

                if (geom is Polygon || geom is MultiPolygon)
                {
                    foreach (var polygon in ConvertGeometry(geom))
                    {
                        var spatialObj = new SpatialObject
                        {
                            Geometry = polygon
                        };

                        foreach (var attributeName in f.Attributes.GetNames())
                        {
                            spatialObj.Attributes[attributeName] = f.Attributes[attributeName];
                        }
                        result.Polygons.Add(spatialObj);
                    }
                }
                else if (geom is Point point)
                {
                    var station = new FireStation
                    {
                        Name = f.Attributes.Exists("name") ? f.Attributes["name"]?.ToString() : "Не указано",
                        Address = f.Attributes.Exists("address") ? f.Attributes["address"]?.ToString() : "Не указано",
                        District = f.Attributes.Exists("district") ? f.Attributes["district"]?.ToString() : "Не указано",
                        Type = f.Attributes.Exists("type") ? f.Attributes["type"]?.ToString() : "Не указано",
                        Phone = f.Attributes.Exists("phone") ? f.Attributes["phone"]?.ToString() : "Не указано",
                        Longitude = point.X,
                        Latitude = point.Y
                    };

                    result.FireStations.Add(station);
                }
            }
            return result;
        }

        private List<Polygon> ConvertGeometry(Geometry geom)
        {
            var result = new List<Polygon>();
            if (geom is Polygon poly) result.Add(poly);
            else if (geom is MultiPolygon multi) result.AddRange(multi.Geometries.Cast<Polygon>());
            return result;
        }
    }
}
