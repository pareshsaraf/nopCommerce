using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Http;
using Nop.Plugin.Widgets.WishlistShare.Models;
using Nop.Plugin.Widgets.WishlistShare.Services;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Web.Controllers;

namespace Nop.Plugin.Widgets.WishlistShare.Controllers;

/// <summary>
/// Creates and opens public wishlist links
/// </summary>
[AutoValidateAntiforgeryToken]
public class WishlistShareController : BasePublicController
{
    #region Fields

    protected readonly ICustomerService _customerService;
    protected readonly ICustomWishlistService _customWishlistService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IPermissionService _permissionService;
    protected readonly IProductService _productService;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStoreContext _storeContext;
    protected readonly IUrlRecordService _urlRecordService;
    protected readonly IWorkContext _workContext;
    protected readonly ShoppingCartSettings _shoppingCartSettings;
    protected readonly WishlistShareService _wishlistShareService;

    #endregion

    #region Ctor

    public WishlistShareController(ICustomerService customerService,
        ICustomWishlistService customWishlistService,
        IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        IProductService productService,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        IUrlRecordService urlRecordService,
        IWorkContext workContext,
        ShoppingCartSettings shoppingCartSettings,
        WishlistShareService wishlistShareService)
    {
        _customerService = customerService;
        _customWishlistService = customWishlistService;
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _permissionService = permissionService;
        _productService = productService;
        _shoppingCartService = shoppingCartService;
        _storeContext = storeContext;
        _urlRecordService = urlRecordService;
        _workContext = workContext;
        _shoppingCartSettings = shoppingCartSettings;
        _wishlistShareService = wishlistShareService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates a share link for the current customer's wishlist
    /// </summary>
    /// <param name="expiryDays">Expiry period in days</param>
    /// <param name="customWishlistId">Custom wishlist identifier. 0 is the default wishlist.</param>
    [HttpPost]
    public virtual async Task<IActionResult> Create(int expiryDays, int customWishlistId)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermission.PublicStore.ENABLE_WISHLIST))
            return RedirectToRoute(NopRouteNames.General.HOMEPAGE);

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (customer == null || customer.Deleted || !customer.Active || customer.IsSystemAccount)
            return Challenge();

        var store = await _storeContext.GetCurrentStoreAsync();

        try
        {
            await _wishlistShareService.CreateShareAsync(customer.Id, store.Id, customWishlistId, expiryDays);
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.Created"));
        }
        catch (ArgumentOutOfRangeException exception) when (exception.ParamName == "expiryDays")
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.InvalidExpiry"));
        }
        catch (Exception exception) when (exception is ArgumentOutOfRangeException or NopException)
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.InvalidWishlist"));
        }

        return RedirectToWishlist(customWishlistId);
    }

    /// <summary>
    /// Opens a public wishlist from its share token
    /// </summary>
    /// <param name="token">Public token</param>
    public virtual async Task<IActionResult> Share(string token)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermission.PublicStore.ENABLE_WISHLIST))
            return RedirectToRoute(NopRouteNames.General.HOMEPAGE);

        var share = await _wishlistShareService.GetByTokenAsync(token);
        if (share == null)
            return Unavailable(await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.Unavailable"), StatusCodes.Status404NotFound);

        if (!_wishlistShareService.IsActive(share))
        {
            var expiredOn = await FormatUserTimeAsync(share.ExpiresOnUtc);
            var message = string.Format(await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.Expired"), expiredOn);
            return Unavailable(message, StatusCodes.Status410Gone);
        }

        var store = await _storeContext.GetCurrentStoreAsync();
        if (share.StoreId != store.Id && !_shoppingCartSettings.CartsSharedBetweenStores)
            return Unavailable(await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.Unavailable"), StatusCodes.Status404NotFound);

        var customer = await _customerService.GetCustomerByIdAsync(share.CustomerId);
        if (customer == null || customer.Deleted || !customer.Active)
            return Unavailable(await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.Unavailable"), StatusCodes.Status404NotFound);

        string wishlistName = null;
        if (share.CustomWishlistId > 0)
        {
            var customWishlist = await _customWishlistService.GetCustomWishlistByIdAsync(share.CustomWishlistId);
            if (customWishlist == null || customWishlist.CustomerId != customer.Id)
                return Unavailable(await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.Unavailable"), StatusCodes.Status404NotFound);

            wishlistName = customWishlist.Name;
        }

        var fullName = await _customerService.GetCustomerFullNameAsync(customer);
        var title = string.IsNullOrWhiteSpace(fullName)
            ? await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.PublicTitleAnonymous")
            : string.Format(await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.PublicTitle"), fullName);

        if (!string.IsNullOrEmpty(wishlistName))
            title = $"{title} - {wishlistName}";

        var model = new SharedWishlistModel
        {
            Title = title,
            ExpiresOn = string.Format(await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.AvailableUntil"), await FormatUserTimeAsync(share.ExpiresOnUtc))
        };

        var listId = share.CustomWishlistId > 0 ? share.CustomWishlistId : (int?)null;
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, store.Id, customWishlistId: listId);
        foreach (var item in cart)
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            if (product == null || product.Deleted || !product.Published)
                continue;

            model.Items.Add(new SharedWishlistItemModel
            {
                ProductName = await _localizationService.GetLocalizedAsync(product, entity => entity.Name),
                ProductSeName = await _urlRecordService.GetSeNameAsync(product),
                Quantity = item.Quantity
            });
        }

        if (model.Items.Count == 0)
            model.Message = await _localizationService.GetResourceAsync("Plugins.Widgets.WishlistShare.Empty");

        return View(WishlistShareDefaults.PublicViewPath, model);
    }

    #endregion

    #region Utilities

    private IActionResult RedirectToWishlist(int customWishlistId)
    {
        if (customWishlistId > 0)
            return RedirectToRoute(NopRouteNames.General.WISHLIST, new { list = customWishlistId });

        return RedirectToRoute(NopRouteNames.General.WISHLIST);
    }

    private IActionResult Unavailable(string message, int statusCode)
    {
        Response.StatusCode = statusCode;
        return View(WishlistShareDefaults.PublicViewPath, new SharedWishlistModel
        {
            Unavailable = true,
            Title = message,
            Message = message
        });
    }

    private async Task<string> FormatUserTimeAsync(DateTime utc)
    {
        var userTime = await _dateTimeHelper.ConvertToUserTimeAsync(utc, DateTimeKind.Utc);
        return userTime.ToString("g");
    }

    #endregion
}
