using Mars_Survival_Hanbook.Contracts;

namespace Mars_Survival_Hanbook.Services.Discovery;

internal sealed record DiscoverCategoryImage(string NasaId, string Title, string FileName, string Credit)
{
    // Public illustrations curated independently of the editorial article snapshots.
    private static readonly Dictionary<string, (string ArticleSlug, DiscoverCategoryImage[] Images)> Catalog = new()
    {
        ["exploration-history"] = ("mariner-4-and-viking-1-en",
        [
            new("PIA02980", "Atlantis Region on Mars - Mariner 4", "ff57c9be65ed921d0fe0d2f3b5dc55c24cac5cfe831681872b4a58d37283ffdc.jpg", "NASA/JPL"),
            new("PIA01685", "Mariner 4 Meets Mars Global Surveyor -- Mariner Crater 1965 and 1999", "3cfb0675b088778c32137ee778ac72bbcf91953db62a8c0f0281ca1b61f72fe5.jpg", "NASA/JPL/MSSS"),
        ]),
        ["structure-and-terrain"] = ("mars-structure-and-surface-en",
        [
            new("PIA15443", "Olympus Rupes", "bc6e83ebc4cb79dc79045d577af5d98ead5124efa59b6229be640ed20ec40269.jpg", "NASA/JPL/ASU"),
            new("PIA00407", "Global Color Views of Mars", "076d0be8f7cf603f05e8b7281709400d9d39f21c6f615e8e385b8dfffc9bafdd.jpg", "NASA/JPL/USGS"),
            new("PIA02005", "Tharsis Volcanoes and Valles Marineris, Mars", "4750beb797c7eb71f6098e90cb9c38470b0f4e6727d4f4158af98c1136c982be.jpg", "NASA/JPL/MSSS"),
            new("PIA01167", "Layers within the Valles Marineris: Clues to the Ancient Crust of Mars", "fceef156e7ce73e6f71b85aa5c20f13a835666fc3584425b7a616699cce36422.jpg", "NASA/JPL/Malin Space Science Systems"),
        ]),
        ["atmosphere-temperature-gravity"] = ("mars-atmosphere-en",
        [
            new("PIA00407", "Global Color Views of Mars", "076d0be8f7cf603f05e8b7281709400d9d39f21c6f615e8e385b8dfffc9bafdd.jpg", "NASA/JPL/USGS"),
        ]),
        ["water-and-life"] = ("mars-water-and-jezero-en",
        [
            new("PIA23239", "Jezero Crater, Mars 2020's Landing Site", "083eddb4f087ee30b6ad698dd1b05b0ee89672ebfaf77d777e99f4bb0f1e4bf0.jpg", "NASA/JPL-Caltech/MSSS/JHU-APL"),
            new("PIA24096", "Jezero Crater as Seen by ESA's Mars Express Orbiter", "d4d3c5ec6879bb50e7882719a9c910db8dfaa00181ffe6a9999696f6c14e1193.jpg", "ESA/DLR/FU-Berlin"),
        ]),
        ["missions-and-rovers"] = ("mars-rovers-and-orbiter-en",
        [
            new("PIA24487", "Perseverance Is Roving on Mars", "2b4d0f9d7c35be3a4aa20be42d1dcd472d5c2d0107b9f0d7ef2ffde909e57dfe.jpg", "NASA/JPL-Caltech"),
            new("PIA19808", "Looking Up at Mars Rover Curiosity in Buckskin Selfie", "cdbf121382a2a77777564aaaf2db11d81303c14cd29f56465ef38517e1500412.jpg", "NASA/JPL-Caltech/MSSS"),
        ]),

    };

    internal static DiscoverCategoryImage? Find(string slug, string? nasaId)
    {
        if (!Catalog.TryGetValue(slug, out var entry)) return null;
        return nasaId is null ? entry.Images[0] : entry.Images.FirstOrDefault(image => image.NasaId == nasaId);
    }

    private static DiscoverImageResponse[] ImageLinks(string category, DiscoverCategoryImage[] images) =>
        images.Select(image => new DiscoverImageResponse(image.NasaId, image.Title, image.Credit,
             $"/api/discover-mars/categories/{category}/images/{image.NasaId}")).ToArray();

    internal static DiscoverImageResponse[]? ForArticle(string articleSlug)
    {
        foreach (var (category, entry) in Catalog)
            if (entry.ArticleSlug == articleSlug) return ImageLinks(category, entry.Images);
        return null;
    }

    internal static DiscoverCategoryResponse Category(Guid id, string slug, string name, string? description, int publishedArticleCount)
    {
        var cover = Find(slug, null);
        return new DiscoverCategoryResponse(id, slug, name, description, publishedArticleCount,
            cover is null ? null : $"/api/discover-mars/categories/{slug}/image",
            cover?.Credit,
            Catalog.TryGetValue(slug, out var entry) ? ImageLinks(slug, entry.Images) : []);
    }
}
