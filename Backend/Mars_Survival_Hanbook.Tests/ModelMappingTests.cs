using System.Text.Json;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public class ModelMappingTests
{
    private static MarsDbContext CreateContext() => new(
        new DbContextOptionsBuilder<MarsDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelMetadataOnly;Integrated Security=true")
            .Options);

    [Fact]
    public void EveryConcreteModelIsMappedWithoutShadowPropertiesOrCascadeDeletes()
    {
        using var context = CreateContext();
        var modelTypes = typeof(Article).Assembly.GetTypes()
            .Where(x => x.Namespace == typeof(Article).Namespace && x.IsClass && !x.IsAbstract)
            .OrderBy(x => x.Name).ToArray();
        var mappedTypes = context.Model.GetEntityTypes().OrderBy(x => x.ClrType.Name).ToArray();
        Assert.Equal(modelTypes, mappedTypes.Select(x => x.ClrType));
        foreach (var entity in mappedTypes)
        {
            Assert.NotNull(entity.FindPrimaryKey());
            Assert.DoesNotContain(entity.GetProperties(), x => x.IsShadowProperty());
            Assert.All(entity.GetForeignKeys(), x => Assert.Equal(DeleteBehavior.NoAction, x.DeleteBehavior));
        }
    }

    [Fact]
    public void ScientificValuesPreservePrecisionAndRequiredProvenance()
    {
        using var context = CreateContext();
        foreach (var type in new[] { typeof(ScientificFact), typeof(Measurement) })
        {
            var entity = context.Model.FindEntityType(type)!;
            foreach (var name in new[] { "Value", "OriginalValue", "UpperValue" })
            {
                var property = entity.FindProperty(name)!;
                Assert.Equal(28, property.GetPrecision());
                Assert.Equal(12, property.GetScale());
            }
            Assert.False(entity.FindProperty("SourceId")!.IsNullable);
            Assert.False(entity.FindProperty("UnitId")!.IsNullable);
            Assert.False(entity.FindProperty("OriginalUnit")!.IsNullable);
        }
    }

    [Fact]
    public void EntitiesUseDatabaseGeneratedConcurrencyTokens()
    {
        using var context = CreateContext();
        foreach (var entity in context.Model.GetEntityTypes()
                     .Where(x => typeof(Entity).IsAssignableFrom(x.ClrType)))
        {
            var token = entity.FindProperty(nameof(Entity.RowVersion))!;
            Assert.True(token.IsConcurrencyToken);
            Assert.Equal(ValueGenerated.OnAddOrUpdate, token.ValueGenerated);
        }
    }

    [Fact]
    public void PasswordHashIsExcludedFromJson()
    {
        var user = new User
        {
            Email = "reader@example.test", NormalizedEmail = "READER@EXAMPLE.TEST",
            DisplayName = "Reader", PasswordHash = "sensitive-test-hash"
        };
        var json = JsonSerializer.Serialize(user);
        Assert.DoesNotContain("PasswordHash", json);
        Assert.DoesNotContain("sensitive-test-hash", json);
    }
}
