using System;
using System.Linq;
using Shouldly;
using Xunit;

namespace TP04.Allergens;

/// <summary>
/// TP final: pruebas unitarias del catálogo de alérgenos (sin ABP ni base de datos).
/// </summary>
public class AllergenCatalog_Tests
{
    [Fact]
    public void All_Should_Contain_The_14_Allergens_Without_Duplicates()
    {
        AllergenCatalog.All.Count.ShouldBe(14);
        AllergenCatalog.All.Distinct().Count().ShouldBe(14);
        AllergenCatalog.All.ShouldAllBe(tag => tag.StartsWith("en:"));
    }

    [Theory]
    [InlineData("en:milk")]
    [InlineData("en:gluten")]
    [InlineData("en:sulphur-dioxide-and-sulphites")]
    [InlineData("en:molluscs")]
    public void IsKnown_Should_Return_True_When_Tag_Is_In_Catalog(string tag)
    {
        AllergenCatalog.IsKnown(tag).ShouldBeTrue();
    }

    [Theory]
    [InlineData(" EN:Milk ")]
    [InlineData("EN:SESAME-SEEDS")]
    public void IsKnown_Should_Return_True_When_Tag_Differs_Only_In_Case_Or_Spaces(string tag)
    {
        AllergenCatalog.IsKnown(tag).ShouldBeTrue();
    }

    [Theory]
    [InlineData("en:kiwi")]
    [InlineData("milk")]          // sin el prefijo "en:" no es el tag canónico
    [InlineData("es:leche")]
    public void IsKnown_Should_Return_False_When_Tag_Is_Not_In_Catalog(string tag)
    {
        AllergenCatalog.IsKnown(tag).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsKnown_Should_Return_False_When_Tag_Is_Null_Or_Blank(string? tag)
    {
        AllergenCatalog.IsKnown(tag).ShouldBeFalse();
    }

    [Theory]
    [InlineData("en:milk", "Allergen:milk")]
    [InlineData("en:sesame-seeds", "Allergen:sesame-seeds")]
    [InlineData(" EN:SESAME-SEEDS ", "Allergen:sesame-seeds")]
    public void GetLocalizationKey_Should_Return_Key_Without_Prefix_When_Tag_Is_Known(string tag, string expected)
    {
        AllergenCatalog.GetLocalizationKey(tag).ShouldBe(expected);
    }

    [Theory]
    [InlineData("en:kiwi")]
    [InlineData("")]
    public void GetLocalizationKey_Should_Throw_ArgumentException_When_Tag_Is_Unknown(string tag)
    {
        Should.Throw<ArgumentException>(() => AllergenCatalog.GetLocalizationKey(tag));
    }

    [Fact]
    public void GetLocalizationKey_Should_Return_A_Key_For_Every_Allergen()
    {
        AllergenCatalog.All
            .Select(AllergenCatalog.GetLocalizationKey)
            .ShouldAllBe(key => key.StartsWith("Allergen:") && !key.StartsWith("Allergen:en:"));
    }
}
