namespace Nop.Plugin.Widgets.WishlistShare;

/// <summary>
/// Represents plugin constants
/// </summary>
public static class WishlistShareDefaults
{
    /// <summary>
    /// Gets the system name
    /// </summary>
    public static string SystemName => "Widgets.WishlistShare";

    /// <summary>
    /// Gets the public share route name
    /// </summary>
    public static string PublicRouteName => "WishlistShare.Public";

    /// <summary>
    /// Gets the route that creates a share link
    /// </summary>
    public static string CreateRouteName => "WishlistShare.Create";

    /// <summary>
    /// Gets how many active links the wishlist widget lists
    /// </summary>
    public static int MaxListedLinks => 10;

    /// <summary>
    /// Gets the widget view path
    /// </summary>
    public static string WidgetViewPath => "~/Plugins/Widgets.WishlistShare/Views/PublicInfo.cshtml";

    /// <summary>
    /// Gets the public share view path
    /// </summary>
    public static string PublicViewPath => "~/Plugins/Widgets.WishlistShare/Views/Shared.cshtml";
}
