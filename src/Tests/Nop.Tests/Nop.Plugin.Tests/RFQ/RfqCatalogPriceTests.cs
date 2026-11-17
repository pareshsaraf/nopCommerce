using System.Globalization;
using AwesomeAssertions;
using Nop.Plugin.Misc.RFQ.Services;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.RFQ;

[TestFixture]
public class RfqCatalogPriceTests
{
    [Test]
    public void NumberInputPriceSurvivesGroupSeparatorCultures()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            foreach (var cultureName in new[] { "de-DE", "fr-FR", "en-US" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

                RfqCatalogPrice.TryParseNumber("10.50", out var amount).Should().BeTrue();
                amount.Should().Be(10.50m);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void IncludingTaxDisplayIsConvertedBackToCatalogPrice()
    {
        // Prices are entered excluding tax. The customer is shown 120 (20% VAT included).
        // Checkout must receive 100 so tax is added once.
        RfqCatalogPrice.ToCatalogPrice(120m, 20m, pricesIncludeTax: false, displayIncludesTax: true).Should().Be(100m);
    }

    [Test]
    public void ExcludingTaxDisplayKeepsPriceThatAlreadyExcludesTax()
    {
        RfqCatalogPrice.ToCatalogPrice(100m, 20m, pricesIncludeTax: false, displayIncludesTax: false).Should().Be(100m);
    }

    [Test]
    public void CatalogPriceThatAlreadyIncludesTaxIsRestoredFromExcludingDisplay()
    {
        RfqCatalogPrice.ToCatalogPrice(100m, 20m, pricesIncludeTax: true, displayIncludesTax: false).Should().Be(120m);
    }

    [Test]
    public void ResubmittedDisplayPriceKeepsStoredCatalogPrice()
    {
        RfqCatalogPrice.KeepWhenUnchanged(100m, 120m, 120m, 83.33m).Should().Be(100m);
    }

    [Test]
    public void EditedDisplayPriceUsesConvertedCatalogPrice()
    {
        RfqCatalogPrice.KeepWhenUnchanged(100m, 96m, 120m, 80m).Should().Be(80m);
    }
}
