using System.Globalization;

namespace Nop.Plugin.Misc.RFQ.Services;

/// <summary>
/// Quote prices are stored in the primary currency on the same basis as a shopping-cart unit price.
/// Checkout runs that amount through tax again, so a tax-display price must not be stored as-is.
/// </summary>
public static class RfqCatalogPrice
{
    /// <summary>
    /// Parses a value posted by an input of type number. Browsers always submit those with a '.' decimal separator.
    /// </summary>
    public static bool TryParseNumber(string value, out decimal amount)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    /// <summary>
    /// Converts a price shown to the customer back to the catalog price basis.
    /// Inverse of <c>TaxService.CalculatePrice</c>: tax added for display is divided back out, tax removed for display is multiplied back in.
    /// </summary>
    public static decimal ToCatalogPrice(decimal displayPrice, decimal taxRatePercent, bool pricesIncludeTax, bool displayIncludesTax)
    {
        if (taxRatePercent == decimal.Zero || pricesIncludeTax == displayIncludesTax)
            return displayPrice;

        if (displayIncludesTax)
            return displayPrice / (1 + taxRatePercent / 100m);

        return displayPrice * (1 + taxRatePercent / 100m);
    }

    /// <summary>
    /// Resubmitting the price already rendered must not replace the stored catalog price.
    /// The rendered amount is in the working currency and may already include or exclude tax for display.
    /// </summary>
    public static decimal KeepWhenUnchanged(decimal currentCatalogPrice, decimal enteredDisplayPrice, decimal renderedDisplayPrice, decimal editedCatalogPrice)
    {
        if (enteredDisplayPrice == renderedDisplayPrice)
            return currentCatalogPrice;

        return editedCatalogPrice;
    }
}
