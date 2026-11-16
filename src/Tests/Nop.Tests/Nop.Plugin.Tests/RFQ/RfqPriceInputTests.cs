using System.Globalization;
using AwesomeAssertions;
using Nop.Plugin.Misc.RFQ.Services;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.RFQ;

[TestFixture]
public class RfqPriceInputTests
{
    [Test]
    public void NumberInputPriceSurvivesGroupSeparatorCultures()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            foreach (var cultureName in new[] { "de-DE", "nl-NL", "es-ES", "it-IT", "pt-BR", "tr-TR", "fr-FR", "en-US" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

                RfqPriceInput.TryParseNumber("10.50", out var amount).Should().BeTrue();
                amount.Should().Be(10.50m);

                RfqPriceInput.TryParseNumber("-1.50", out var negative).Should().BeTrue();
                negative.Should().Be(-1.50m);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void UneditedWorkingCurrencyPriceKeepsPrimaryAmount()
    {
        // Customer resubmits the rounded working-currency price already on the form.
        RfqPriceInput.ResolvePrimaryUnitPrice(10.50m, 9.66m, 9.66m, 9.66m).Should().Be(10.50m);
    }

    [Test]
    public void EditedWorkingCurrencyPriceUsesConvertedPrimaryAmount()
    {
        RfqPriceInput.ResolvePrimaryUnitPrice(10.50m, 12.00m, 9.66m, 13.04m).Should().Be(13.04m);
    }
}
