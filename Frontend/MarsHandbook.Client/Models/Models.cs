using System.Text.Json;
namespace MarsHandbook.Client.Models;

public class RoutePoint { public double Lat { get; set; } public double Lng { get; set; } }

public record Poi(string Id, string Name, double Lat, double Lng, string Kind, string Mission, string Note);

public class Assumptions
{
    public double WalkSpeedKmh { get; set; } = 2.0;
    public double SuitHours { get; set; } = 8;
    public double ReservePct { get; set; } = 30;
    public double ScienceStopMin { get; set; } = 20;
    public int ScienceStops { get; set; } = 3;
    public double O2KgPerHour { get; set; } = 0.1;
    public bool OutAndBack { get; set; } = true;
}

public enum Verdict { None, Go, Caution, NoGo }

public record RouteStats(double DistanceKm, double WalkHours, double TotalHours, double O2Kg, double ReachKm,
                         double UsableHours, double MaxFromBaseKm, Verdict Verdict, string Message);

public record NearbyPoi(Poi Poi, double DistKm);

public record ApiItem(string Title, string Summary, string? Tag);
