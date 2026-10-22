using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Widgets.WishlistShare.Models;
using Nop.Plugin.Widgets.WishlistShare.Services;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework.Components;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Plugin.Widgets.WishlistShare.Components;

/// <summary>
/// Renders the share box on the owner's wishlist page
/// </summary>
public class WishlistShareViewComponent : NopViewComponent
{
    #region Fields

    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPermissionService _permissionService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWebHelper _webHelper;
    protected readonly IWorkContext _workContext;
    protected readonly WishlistShareService _wishlistShareService;

    #endregion

    #region Ctor

    public WishlistShareViewComponent(IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        IPermissionService permissionService,
        IStoreContext storeContext,
        IWebHelper webHelper,
        IWorkContext workContext,
        WishlistShareService wishlistShareService)
    {
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _permissionService = permissionService;
        _storeContext = storeContext;
        _webHelper = webHelper;
        _workContext = workContext;
        _wishlistShareService = wishlistShareService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Invoke the widget view component
    /// </summary>
    /// <param name="widgetZone">Widget zone name</param>
    /// <param name="additionalData">Additional data</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        if (additionalData is not WishlistModel wishlist || !wishlist.IsEditable)
            return Content(string.Empty);

        if (!await _permissionService.AuthorizeAsync(StandardPermission.PublicStore.ENABLE_WISHLIST))
            return Content(string.Empty);

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (customer == null || customer.Deleted || !customer.Active || customer.IsSystemAccount || customer.CustomerGuid != wishlist.CustomerGuid)
            return Content(string.Empty);

        var store = await _storeContext.GetCurrentStoreAsync();
        var customWishlistId = wishlist.ListId.GetValueOrDefault();
        var daysFormat = await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.Days");
        var expiresFormat = await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.ExpiresOn");

        var model = new WishlistShareWidgetModel
        {
            CustomWishlistId = customWishlistId
        };

        foreach (var days in WishlistShareExpiry.AllowedDays)
        {
            model.ExpiryOptions.Add(new WishlistShareExpiryOptionModel
            {
                Days = days,
                Text = string.Format(daysFormat, days)
            });
        }

        var shares = await _wishlistShareService.GetActiveSharesAsync(customer.Id, store.Id, customWishlistId);
        foreach (var share in shares)
        {
            var url = Url.RouteUrl(WishlistShareDefaults.PublicRouteName, new { token = share.Token }, _webHelper.GetCurrentRequestProtocol());
            if (string.IsNullOrEmpty(url))
                continue;

            var expiresOn = await _dateTimeHelper.ConvertToUserTimeAsync(share.ExpiresOnUtc, DateTimeKind.Utc);
            model.Links.Add(new WishlistShareLinkModel
            {
                Url = url,
                ExpiresOn = string.Format(expiresFormat, expiresOn.ToString("g"))
            });
        }

        return await ViewAsync(WishlistShareDefaults.WidgetViewPath, model);
    }

    #endregion
}
