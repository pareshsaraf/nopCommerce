namespace Nop.Plugin.Widgets.WishlistShare.Models;

/// <summary>
/// Wishlist share box on the owner's wishlist page
/// </summary>
public class WishlistShareWidgetModel
{
    /// <summary>
    /// Gets or sets the custom wishlist identifier. 0 is the default wishlist.
    /// </summary>
    public int CustomWishlistId { get; set; }

    /// <summary>
    /// Gets or sets the expiry choices
    /// </summary>
    public IList<WishlistShareExpiryOptionModel> ExpiryOptions { get; set; } = new List<WishlistShareExpiryOptionModel>();

    /// <summary>
    /// Gets or sets the active links
    /// </summary>
    public IList<WishlistShareLinkModel> Links { get; set; } = new List<WishlistShareLinkModel>();
}

/// <summary>
/// One expiry choice
/// </summary>
public class WishlistShareExpiryOptionModel
{
    /// <summary>
    /// Gets or sets the period in days
    /// </summary>
    public int Days { get; set; }

    /// <summary>
    /// Gets or sets the label
    /// </summary>
    public string Text { get; set; }
}

/// <summary>
/// One active public link
/// </summary>
public class WishlistShareLinkModel
{
    /// <summary>
    /// Gets or sets the absolute public URL
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the formatted expiry
    /// </summary>
    public string ExpiresOn { get; set; }
}
