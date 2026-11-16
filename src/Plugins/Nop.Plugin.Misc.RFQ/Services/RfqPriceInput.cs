using System.Globalization;

namespace Nop.Plugin.Misc.RFQ.Services;

/// <summary>
/// Parses prices posted by HTML number inputs and decides whether a quote request price changed
/// </summary>
public static class RfqPriceInput
{
    /// <summary>
    /// Parses a value posted by an input of type number. Browsers always submit those with a '.' decimal separator.
    /// </summary>
    /// <param name="value">Posted value</param>
    /// <param name="amount">Parsed amount</param>
    /// <returns>True when the value is a number</returns>
    public static bool TryParseNumber(string value, out decimal amount)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    /// <summary>
    /// Keeps the stored primary-currency price when the customer resubmits the amount already shown.
    /// Otherwise returns the entered working-currency amount converted to the primary store currency.
    /// </summary>
    /// <param name="currentPrimaryPrice">Price currently stored, in the primary store currency</param>
    /// <param name="enteredWorkingPrice">Price posted from the form, in the working currency</param>
    /// <param name="displayedWorkingPrice">Rounded working-currency price that was rendered</param>
    /// <param name="enteredConvertedToPrimary">Entered price converted to the primary store currency</param>
    /// <returns>Price to store, in the primary store currency</returns>
    public static decimal ResolvePrimaryUnitPrice(decimal currentPrimaryPrice, decimal enteredWorkingPrice, decimal displayedWorkingPrice, decimal enteredConvertedToPrimary)
    {
        if (enteredWorkingPrice == displayedWorkingPrice)
            return currentPrimaryPrice;

        return enteredConvertedToPrimary;
    }
}
