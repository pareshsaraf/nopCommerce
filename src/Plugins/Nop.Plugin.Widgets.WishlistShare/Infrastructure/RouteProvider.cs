using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Infrastructure;

namespace Nop.Plugin.Widgets.WishlistShare.Infrastructure;

/// <summary>
/// Represents plugin route provider
/// </summary>
public class RouteProvider : BaseRouteProvider, IRouteProvider
{
    /// <summary>
    /// Register routes
    /// </summary>
    /// <param name="endpointRouteBuilder">Route builder</param>
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        var lang = GetLanguageRoutePattern();

        endpointRouteBuilder.MapControllerRoute(name: WishlistShareDefaults.PublicRouteName,
            pattern: $"{lang}/shared-wishlist/{{token:guid}}",
            defaults: new { controller = "WishlistShare", action = "Share" });

        endpointRouteBuilder.MapControllerRoute(name: WishlistShareDefaults.CreateRouteName,
            pattern: "wishlist-share/create",
            defaults: new { controller = "WishlistShare", action = "Create" });
    }

    /// <summary>
    /// Gets a priority of route provider
    /// </summary>
    public int Priority => 0;
}
