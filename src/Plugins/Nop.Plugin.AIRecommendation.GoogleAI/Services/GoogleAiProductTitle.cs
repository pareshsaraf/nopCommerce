using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.AIRecommendation.GoogleAI.Services;

/// <summary>
/// Builds the title uploaded to Google Retail.
/// </summary>
/// <remarks>
/// Retail search matches <c>title</c>. Short description is optional and is blank when the merchant leaves it empty.
/// An empty title is rejected on import, and a search that still returns other hits then omits the product.
/// </remarks>
public static class GoogleAiProductTitle
{
    public static string Get(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var title = string.IsNullOrWhiteSpace(product.ShortDescription) ? product.Name : product.ShortDescription;

        return CommonHelper.EnsureMaximumLength(title, 1000);
    }
}
