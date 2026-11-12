using System.Globalization;
using AwesomeAssertions;
using Nop.Plugin.Payments.PayPalCommerce.Services.Api.Models;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.Payments;

[TestFixture]
public class PayPalCommerceApiValueTests
{
    [Test]
    public void PayerActionUrl_ResolvesLowercaseStatus_UnderTurkishCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var turkish = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentCulture = turkish;
            CultureInfo.CurrentUICulture = turkish;

            // "i".ToUpper() is "İ" under tr-TR, so a culture-sensitive comparison drops the approval link.
            var order = new Order
            {
                Status = "payer_action_required",
                Links = [new Link { Rel = "payer-action", Href = "https://paypal.example/approve" }]
            };
            order.PayerActionUrl.Should().Be("https://paypal.example/approve");

            var token = new PaymentToken
            {
                Status = "payer_action_required",
                Links = [new Link { Rel = "approve", Href = "https://paypal.example/token" }]
            };
            token.PayerActionUrl.Should().Be("https://paypal.example/token");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
