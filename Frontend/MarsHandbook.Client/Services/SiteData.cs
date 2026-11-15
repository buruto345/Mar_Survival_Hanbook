using MarsHandbook.Client.Models;
namespace MarsHandbook.Client.Services;

/// <summary>Curated targets (positions approximate, east-positive longitude). Source: public NASA mission pages.</summary>
public static class SiteData
{
    public static readonly Poi[] All =
    {
        new("jezero","Jezero Crater · Perseverance",18.4447,77.4508,"landing","Mars 2020","Ancient river delta; Perseverance landing site (2021)."),
        new("jezero-delta","Jezero delta front",18.45,77.35,"geology","Mars 2020","Layered delta deposits: prime target for past-life biosignatures."),
        new("jezero-seitah","Séítah ridge",18.42,77.37,"geology","Mars 2020","Olivine-rich igneous terrain."),
        new("gale","Gale Crater · Curiosity",-4.5895,137.4417,"landing","MSL","Bradbury Landing (2012); 5 km tall Mount Sharp at the centre."),
        new("gale-yk","Yellowknife Bay",-4.70,137.39,"ice","MSL","Evidence of an ancient lake and hydrated minerals."),
        new("gale-sharp","Mount Sharp foothills",-5.05,137.85,"geology","MSL","Climbing layers record Mars' drying climate."),
        new("olympus","Olympus Mons",18.65,-133.8,"hazard","MGS/MOLA","Tallest volcano known (~22 km); thin air, steep flanks."),
        new("valles","Valles Marineris · Candor",-6.5,-75.0,"hazard","MRO","Canyon system ~4,000 km long; deep cliffs and dust."),
        new("phoenix","Phoenix · Vastitas Borealis",68.22,-125.7,"ice","Phoenix","Digging confirmed shallow water ice (2008)."),
        new("insight","InSight · Elysium Planitia",4.50,135.62,"landing","InSight","Seismology; flat, safe terrain."),
        new("viking1","Viking 1 · Chryse Planitia",22.27,-47.95,"landing","Viking","First successful Mars lander (1976)."),
        new("gusev","Gusev · Spirit",-14.57,175.47,"geology","MER","Spirit rover explored volcanic plains and Columbia Hills."),
    };

    public static string KindName(string k) => k switch { "landing" => "Landing site", "ice" => "Water / ice", "geology" => "Geology", "hazard" => "Hazard", _ => k };
}
