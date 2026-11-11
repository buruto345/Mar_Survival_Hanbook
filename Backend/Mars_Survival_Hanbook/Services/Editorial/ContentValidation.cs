using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services.Sources;

namespace Mars_Survival_Hanbook.Services.Editorial;

public static class ContentValidation
{
    public static void Validate(ProvenancedRequest r)
    {
        Validator.ValidateObject(r, new ValidationContext(r), true);
        if (r.SourceRevisionId == Guid.Empty) throw ApiException.Invalid("A sourceRevisionId is required.");
        switch (r)
        {
            case FactRequest f:
                Range(f.Value, f.UpperValue, f.MeasurementKind);
                break;
            case MeasurementRequest m:
                Range(m.Value, m.UpperValue, m.MeasurementKind);
                if (m.ObservedAt == default || m.ObservedAt.Year >= 3000 || m.ObservationEndAt < m.ObservedAt)
                    throw ApiException.Invalid("The observation interval is invalid.");
                break;
            case MissionRequest m when m.EndDate < m.LaunchDate || m.EndDate?.Year >= 3000 || m.LaunchDate?.Year >= 3000:
                throw ApiException.Invalid("Supply valid mission dates.");
            case LocationRequest l when l.Latitude is < -90 or > 90 || l.Longitude is < -180 or > 180:
                throw ApiException.Invalid("Coordinates are outside the valid range.");
            case TimelineEventRequest e when e.EventDate == default || e.EndDate < e.EventDate:
                throw ApiException.Invalid("The event interval is invalid.");
            case MediaRequest m:
                _ = SourceNormalization.CanonicalUrl(m.Url); _ = SourceNormalization.CanonicalUrl(m.OriginalUrl);
                if (m.Width <= 0 || m.Height <= 0 || m.FileSizeBytes <= 0) throw ApiException.Invalid("Media dimensions and file size must be positive.");
                break;
            case MapLayerRequest m:
                _ = SourceNormalization.CanonicalUrl(m.ServiceUrl);
                _ = SourceNormalization.CanonicalUrl(System.Text.RegularExpressions.Regex.Replace(
                    m.ResourceUrlTemplate, @"\{[A-Za-z]+\}", "0"));
                using (var d = JsonDocument.Parse(m.BoundingBoxJson))
                    if (d.RootElement.ValueKind != JsonValueKind.Array || d.RootElement.GetArrayLength() != 4)
                        throw ApiException.Invalid("BoundingBoxJson must contain four coordinates.");
                break;
        }
    }
    public static void Range(decimal value, decimal? upper, MeasurementKind kind)
    {
        if (upper < value || (kind == MeasurementKind.Range && upper is null))
            throw ApiException.Invalid("The value range is invalid.");
    }
    public static decimal ConvertUnit(decimal value, string from, string to)
    {
        if (from == to) return value;
        return (from, to) switch {
            ("hPa", "Pa") => value * 100, ("Pa", "hPa") => value / 100,
            ("mb" or "mbar", "Pa") => value * 100,
            ("°C", "K") => value + 273.15m, ("K", "°C") => value - 273.15m,
            ("km", "m") => value * 1000, ("m", "km") => value / 1000,
            _ => throw ApiException.Invalid("This unit conversion is unsupported.")
        };
    }
    public static decimal[]? Bbox(string? input)
    {
        if (input is null) return null;
        var parts = input.Split(',');
        if (parts.Length != 4 || parts.Any(x => !decimal.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
            throw ApiException.Invalid("Specify bbox as west,south,east,north.");
        var v = parts.Select(x => decimal.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        if (v[0] < -180 || v[2] > 180 || v[1] < -90 || v[3] > 90 || v[0] > v[2] || v[1] > v[3])
            throw ApiException.Invalid("The bounding box is outside the supported range or crosses the antimeridian.");
        return v;
    }
}
