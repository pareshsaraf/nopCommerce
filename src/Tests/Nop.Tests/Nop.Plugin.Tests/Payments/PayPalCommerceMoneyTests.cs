using System.Globalization;
using AwesomeAssertions;
using Nop.Plugin.Payments.PayPalCommerce.Services;
using Nop.Plugin.Payments.PayPalCommerce.Services.Api.Models;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.Payments;

[TestFixture]
public class PayPalCommerceMoneyTests
{
    [Test]
    public void ConvertMoney_ParsesPayPalAmount_UnderCommaDecimalCultures()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            // Webhook refunds call ConvertMoney. These cultures are the store default
            // when that language is first in the list, and PayPal sends no culture cookie.
            foreach (var cultureName in new[] { "de-DE", "fr-FR", "tr-TR", "es-ES" })
            {
                var culture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                PayPalCommerceServiceManager.ConvertMoney(new Money { Value = "10.50" }).Should().Be(10.50m);
                PayPalCommerceServiceManager.ConvertMoney(new Money { Value = "1234.56" }).Should().Be(1234.56m);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
