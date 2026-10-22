namespace Nop.Plugin.Widgets.WishlistShare.Services;

/// <summary>
/// Expiry rules for a public wishlist link
/// </summary>
public static class WishlistShareExpiry
{
    /// <summary>
    /// Gets the expiry periods a customer can choose, in days
    /// </summary>
    public static readonly int[] AllowedDays = [7, 30, 90];

    /// <summary>
    /// Gets a value indicating whether the period is one of the supported choices
    /// </summary>
    /// <param name="expiryDays">Expiry period in days</param>
    /// <returns>True when the period is 7, 30, or 90 days</returns>
    public static bool IsAllowed(int expiryDays)
    {
        return Array.IndexOf(AllowedDays, expiryDays) >= 0;
    }

    /// <summary>
    /// Gets the UTC instant when a link created at <paramref name="utcNow"/> expires
    /// </summary>
    /// <param name="expiryDays">Expiry period in days. Must be 7, 30, or 90.</param>
    /// <param name="utcNow">Creation instant</param>
    /// <returns>Expiry instant in UTC</returns>
    public static DateTime GetExpiresOnUtc(int expiryDays, DateTime utcNow)
    {
        if (!IsAllowed(expiryDays))
            throw new ArgumentOutOfRangeException(nameof(expiryDays), expiryDays, "Wishlist share expiry must be 7, 30, or 90 days.");

        return AsUtc(utcNow).AddDays(expiryDays);
    }

    /// <summary>
    /// Gets a value indicating whether the link still works
    /// </summary>
    /// <param name="expiresOnUtc">Expiry instant</param>
    /// <param name="utcNow">Instant to compare</param>
    /// <returns>True while the expiry instant is still in the future</returns>
    public static bool IsActive(DateTime expiresOnUtc, DateTime utcNow)
    {
        return AsUtc(expiresOnUtc) > AsUtc(utcNow);
    }

    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
    }
}
