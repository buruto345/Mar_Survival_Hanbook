using MarsHandbook.Client.Models;
namespace MarsHandbook.Client.Services;

public static class MarsMath
{
    public const double RadiusKm = 3389.5;
    static double Rad(double d) => d * Math.PI / 180;

    public static double Haversine(double la1, double lo1, double la2, double lo2)
    {
        var dLa = Rad(la2 - la1); var dLo = Rad(lo2 - lo1);
        var a = Math.Sin(dLa / 2) * Math.Sin(dLa / 2) + Math.Cos(Rad(la1)) * Math.Cos(Rad(la2)) * Math.Sin(dLo / 2) * Math.Sin(dLo / 2);
        return 2 * RadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    public static double PathLength(IReadOnlyList<RoutePoint> p)
    {
        double s = 0;
        for (int i = 1; i < p.Count; i++) s += Haversine(p[i - 1].Lat, p[i - 1].Lng, p[i].Lat, p[i].Lng);
        return s;
    }

    // Distance from point to polyline, approximated locally (equirectangular) – fine for EVA-scale (<50 km).
    public static double DistanceToPath(double lat, double lng, IReadOnlyList<RoutePoint> p)
    {
        if (p.Count == 0) return double.MaxValue;
        if (p.Count == 1) return Haversine(lat, lng, p[0].Lat, p[0].Lng);
        double best = double.MaxValue, k = Math.Cos(Rad(lat));
        for (int i = 1; i < p.Count; i++)
        {
            double ax = p[i - 1].Lng * k, ay = p[i - 1].Lat, bx = p[i].Lng * k, by = p[i].Lat, px = lng * k, py = lat;
            double dx = bx - ax, dy = by - ay, len2 = dx * dx + dy * dy;
            double t = len2 == 0 ? 0 : Math.Clamp(((px - ax) * dx + (py - ay) * dy) / len2, 0, 1);
            double qy = ay + t * dy, qx = (ax + t * dx) / (k == 0 ? 1 : k);
            best = Math.Min(best, Haversine(lat, lng, qy, qx));
        }
        return best;
    }

    public static double Reach(Assumptions a)
    {
        var usable = a.SuitHours * (1 - a.ReservePct / 100.0);
        var walking = Math.Max(0, usable - a.ScienceStops * a.ScienceStopMin / 60.0);
        var km = walking * a.WalkSpeedKmh;
        return a.OutAndBack ? km / 2 : km;
    }

    public static RouteStats Evaluate(IReadOnlyList<RoutePoint> pts, Assumptions a)
    {
        var reach = Reach(a);
        var usable = a.SuitHours * (1 - a.ReservePct / 100.0);
        if (pts.Count < 2)
            return new(0, 0, 0, 0, reach, usable, 0, Verdict.None, "Click the map to place the habitat (H), then add waypoints.");

        double dist = PathLength(pts);
        if (a.OutAndBack) dist *= 2;
        double walk = dist / a.WalkSpeedKmh, total = walk + a.ScienceStops * a.ScienceStopMin / 60.0;
        double maxFrom = pts.Max(p => Haversine(pts[0].Lat, pts[0].Lng, p.Lat, p.Lng));
        double o2 = total * a.O2KgPerHour;
        Verdict v; string msg;
        if (total > a.SuitHours) { v = Verdict.NoGo; msg = $"Exceeds suit life by {total - a.SuitHours:0.0} h. Shorten the route."; }
        else if (total > usable) { v = Verdict.Caution; msg = $"Inside suit life but eats the {a.ReservePct:0}% safety reserve."; }
        else { v = Verdict.Go; msg = $"Fits with {usable - total:0.0} h of margin before the reserve."; }
        return new(dist, walk, total, o2, reach, usable, maxFrom, v, msg);
    }
}
