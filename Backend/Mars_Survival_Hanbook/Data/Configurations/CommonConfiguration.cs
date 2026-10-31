using Mars_Survival_Hanbook.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Mars_Survival_Hanbook.Data.Configurations;

internal static class CommonConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes().ToList())
        {
            var entity = builder.Entity(entityType.ClrType);
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                entity.HasKey(nameof(Entity.Id));
                entity.Property(nameof(Entity.Id)).ValueGeneratedNever();
                entity.Property(nameof(Entity.RowVersion)).IsRowVersion();
                entity.Property(nameof(Entity.CreatedAt))
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
                entity.HasIndex(nameof(Entity.ArchivedAt));
            }

            foreach (var property in entityType.GetProperties().ToList())
            {
                var enumType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (!enumType.IsEnum)
                    continue;

                entity.Property(property.Name).HasConversion<string>().HasMaxLength(48);
                var values = string.Join(", ", Enum.GetNames(enumType).Select(x => $"N'{x}'"));
                entity.ToTable(table => table.HasCheckConstraint(
                    $"CK_{entityType.GetTableName()}_{property.Name}",
                    $"[{property.Name}] IN ({values})"));
            }
        }

        // Archive is explicit: no global filter silently removes sources, citations or bookmarks.
        // Public queries must select Published articles and exclude ArchivedAt records as appropriate.
    }
}

