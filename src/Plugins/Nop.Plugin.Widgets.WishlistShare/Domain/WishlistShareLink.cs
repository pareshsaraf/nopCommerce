using Nop.Core;

namespace Nop.Plugin.Widgets.WishlistShare.Domain;

/// <summary>
/// A public, expiring link to a customer's wishlist
/// </summary>
public class WishlistShareLink : BaseEntity
{
    /// <summary>
    /// Gets or sets the customer identifier
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the store identifier
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the custom wishlist identifier. 0 is the default wishlist.
    /// </summary>
    public int CustomWishlistId { get; set; }

    /// <summary>
    /// Gets or sets the public token
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the link stops working
    /// </summary>
    public DateTime ExpiresOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time of creation
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }
}
