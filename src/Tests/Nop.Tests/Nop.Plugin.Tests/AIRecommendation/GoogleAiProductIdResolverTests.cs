using AwesomeAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.AIRecommendation.GoogleAI.Services;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.AIRecommendation;

[TestFixture]
public class GoogleAiProductIdResolverTests
{
    [Test]
    public void NumericSkuResolvesToTheProductThatOwnsTheSku()
    {
        var productsBySku = new List<Product> { new() { Id = 5, Sku = "100050" } };
        var productsById = new List<Product> { new() { Id = 100050, Sku = "OTHER" } };

        var ids = GoogleAiProductIdResolver.Resolve(["100050"], productsBySku, new List<ProductAttributeCombination>(), productsById, new List<ProductAttributeCombination>());

        ids.Should().Equal(5);
    }

    [Test]
    public void NumericCombinationSkuResolvesToParentProduct()
    {
        var combinationsBySku = new List<ProductAttributeCombination>
        {
            new() { Id = 9, ProductId = 42, Sku = "15" }
        };
        var productsById = new List<Product> { new() { Id = 15 } };

        var ids = GoogleAiProductIdResolver.Resolve(["15"], new List<Product>(), combinationsBySku, productsById, new List<ProductAttributeCombination>());

        ids.Should().Equal(42);
    }

    [Test]
    public void ProductWithoutSkuResolvesFromItsId()
    {
        var productsById = new List<Product> { new() { Id = 5 } };

        var ids = GoogleAiProductIdResolver.Resolve(["5"], new List<Product>(), new List<ProductAttributeCombination>(), productsById, new List<ProductAttributeCombination>());

        ids.Should().Equal(5);
    }

    [Test]
    public void CombinationWithoutSkuResolvesToParentProduct()
    {
        var productsById = new List<Product> { new() { Id = 15, Sku = "SHIRT" } };
        var combinationsById = new List<ProductAttributeCombination>
        {
            new() { Id = 15, ProductId = 42, Sku = string.Empty }
        };

        var ids = GoogleAiProductIdResolver.Resolve(["15"], new List<Product>(), new List<ProductAttributeCombination>(), productsById, combinationsById);

        ids.Should().Equal(42);
    }

    [Test]
    public void LeadingZerosStayOnTheSkuAndAreNotReadAsAnEntityId()
    {
        GoogleAiProductIdResolver.GetEntityIds(["08", "8"]).Should().Equal(8);

        var productsBySku = new List<Product> { new() { Id = 3, Sku = "08" } };
        var productsById = new List<Product> { new() { Id = 8 } };

        var ids = GoogleAiProductIdResolver.Resolve(["08"], productsBySku, new List<ProductAttributeCombination>(), productsById, new List<ProductAttributeCombination>());

        ids.Should().Equal(3);
    }

    [Test]
    public void PreservesRankOrderAndCollapsesDuplicateProducts()
    {
        var productsBySku = new List<Product>
        {
            new() { Id = 2, Sku = "B" },
            new() { Id = 1, Sku = "A" }
        };
        var combinationsBySku = new List<ProductAttributeCombination>
        {
            new() { Id = 9, ProductId = 1, Sku = "A-RED" }
        };

        var ids = GoogleAiProductIdResolver.Resolve(["B", "A-RED", "A", "missing"], productsBySku, combinationsBySku, new List<Product>(), new List<ProductAttributeCombination>());

        ids.Should().Equal(2, 1);
    }

    [Test]
    public void DeletedProductWithoutSkuDoesNotHideACombinationWithTheSameId()
    {
        var productsById = new List<Product> { new() { Id = 15, Deleted = true } };
        var combinationsById = new List<ProductAttributeCombination>
        {
            new() { Id = 15, ProductId = 42 }
        };

        var ids = GoogleAiProductIdResolver.Resolve(["15"], new List<Product>(), new List<ProductAttributeCombination>(), productsById, combinationsById);

        ids.Should().Equal(42);
    }
}
