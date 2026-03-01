using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FFSchedule.Services
{
    public record NominatimResult(
    [property: JsonPropertyName("display_name")] string DisplayName,
    double Lat,
    double Lon,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("class")] string Class,
    [property: JsonPropertyName("importance")] double Importance,
    [property: JsonPropertyName("extratags")] Dictionary<string, string>? Extratags)
    {
        public string ShortDisplayName
        {
            get
            {
                var parts = DisplayName?.Split(',') ?? Array.Empty<string>();
                return parts.Length > 2 ? string.Join(",", parts.Take(2)).Trim() : DisplayName ?? "";
            }
        }
    }
}
