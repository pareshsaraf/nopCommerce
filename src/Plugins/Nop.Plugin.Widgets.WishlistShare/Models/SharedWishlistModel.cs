namespace Nop.Plugin.Widgets.WishlistShare.Models;

/// <summary>
/// Public wishlist opened from a share link
/// </summary>
public class SharedWishlistModel
{
    /// <summary>
    /// Gets or sets a value indicating whether the link cannot be opened
    /// </summary>
    public bool Unavailable { get; set; }

    /// <summary>
    /// Gets or sets the page title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the status message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the formatted availability date
    /// </summary>
    public string ExpiresOn { get; set; }

    /// <summary>
    /// Gets or sets the products
    /// </summary>
    public IList<SharedWishlistItemModel> Items { get; set; } = new List<SharedWishlistItemModel>();
}

/// <summary>
/// A product on a shared wishlist
/// </summary>
public class SharedWishlistItemModel
{
    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// Gets or sets the product search-engine name
    /// </summary>
    public string ProductSeName { get; set; }

    /// <summary>
    /// Gets or sets the quantity
    /// </summary>
    public int Quantity { get; set; }
}
