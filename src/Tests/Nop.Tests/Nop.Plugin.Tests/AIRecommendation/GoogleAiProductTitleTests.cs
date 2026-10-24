using AwesomeAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.AIRecommendation.GoogleAI.Services;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.AIRecommendation;

[TestFixture]
public class GoogleAiProductTitleTests
{
    [Test]
    public void BlankShortDescriptionUsesProductName()
    {
        GoogleAiProductTitle.Get(new Product { Name = "Blue running shoes", ShortDescription = "" })
            .Should().Be("Blue running shoes");

        GoogleAiProductTitle.Get(new Product { Name = "Blue running shoes", ShortDescription = null })
            .Should().Be("Blue running shoes");

        GoogleAiProductTitle.Get(new Product { Name = "Blue running shoes", ShortDescription = "   " })
            .Should().Be("Blue running shoes");
    }

    [Test]
    public void ShortDescriptionIsUsedWhenItHasText()
    {
        GoogleAiProductTitle.Get(new Product { Name = "Blue running shoes", ShortDescription = "Lightweight road shoe" })
            .Should().Be("Lightweight road shoe");
    }

    [Test]
    public void TitleIsCappedAtRetailLimit()
    {
        var name = new string('n', 1200);

        GoogleAiProductTitle.Get(new Product { Name = name, ShortDescription = "" })
            .Should().HaveLength(1000);
    }
}
