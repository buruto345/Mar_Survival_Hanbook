namespace Mars_Survival_Hanbook.Services.Discovery;

internal sealed record DiscoverCategoryImage(string FileName, string Credit)
{
    private static readonly Dictionary<string, DiscoverCategoryImage> Images = new()
    {
        ["exploration-history"] = new("ff57c9be65ed921d0fe0d2f3b5dc55c24cac5cfe831681872b4a58d37283ffdc.jpg", "NASA/JPL"),
        ["structure-and-terrain"] = new("bc6e83ebc4cb79dc79045d577af5d98ead5124efa59b6229be640ed20ec40269.jpg", "NASA/JPL/ASU"),
        ["atmosphere-temperature-gravity"] = new("076d0be8f7cf603f05e8b7281709400d9d39f21c6f615e8e385b8dfffc9bafdd.jpg", "NASA/JPL/USGS"),
        ["water-and-life"] = new("083eddb4f087ee30b6ad698dd1b05b0ee89672ebfaf77d777e99f4bb0f1e4bf0.jpg", "NASA/JPL-Caltech/MSSS/JHU-APL"),
        ["missions-and-rovers"] = new("2b4d0f9d7c35be3a4aa20be42d1dcd472d5c2d0107b9f0d7ef2ffde909e57dfe.jpg", "NASA/JPL-Caltech")
    };

    internal static DiscoverCategoryImage? Find(string slug) => Images.GetValueOrDefault(slug);

    internal static object Category(Guid id, string slug, string name, string? description, int publishedArticleCount)
    {
        var image = Find(slug);
        return new { id, slug, name, description, publishedArticleCount,
            imageUrl = image is null ? null : $"/api/discover-mars/categories/{slug}/image",
            imageCredit = image?.Credit };
    }
}
