using Nop.Core;
using Nop.Core.Domain.Cms;
using Nop.Plugin.Widgets.WishlistShare.Components;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Widgets.WishlistShare;

/// <summary>
/// Represents the wishlist share widget
/// </summary>
public class WishlistSharePlugin : BasePlugin, IWidgetPlugin
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly ISettingService _settingService;
    protected readonly WidgetSettings _widgetSettings;

    #endregion

    #region Ctor

    public WishlistSharePlugin(ILocalizationService localizationService,
        ISettingService settingService,
        WidgetSettings widgetSettings)
    {
        _localizationService = localizationService;
        _settingService = settingService;
        _widgetSettings = widgetSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets widget zones where this widget should be rendered
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the widget zones
    /// </returns>
    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>([PublicWidgetZones.WishlistBottom]);
    }

    /// <summary>
    /// Gets a type of a view component for displaying widget
    /// </summary>
    /// <param name="widgetZone">Name of the widget zone</param>
    /// <returns>View component type</returns>
    public Type GetWidgetViewComponent(string widgetZone)
    {
        return typeof(WishlistShareViewComponent);
    }

    /// <summary>
    /// Install plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task InstallAsync()
    {
        if (!_widgetSettings.ActiveWidgetSystemNames.Contains(PluginDescriptor.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Add(PluginDescriptor.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Widgets.WishlistShare.Title"] = "Share with an expiring link",
            ["Plugins.Widgets.WishlistShare.Hint"] = "Anyone with the link can view this wishlist until it expires.",
            ["Plugins.Widgets.WishlistShare.Expiry"] = "Link expires after",
            ["Plugins.Widgets.WishlistShare.Days"] = "{0} days",
            ["Plugins.Widgets.WishlistShare.Create"] = "Create share link",
            ["Plugins.Widgets.WishlistShare.Created"] = "Your share link is ready.",
            ["Plugins.Widgets.WishlistShare.InvalidExpiry"] = "Choose an expiry of 7, 30, or 90 days.",
            ["Plugins.Widgets.WishlistShare.InvalidWishlist"] = "That wishlist could not be shared.",
            ["Plugins.Widgets.WishlistShare.ExistingLinks"] = "Active share links",
            ["Plugins.Widgets.WishlistShare.ExpiresOn"] = "Expires {0}",
            ["Plugins.Widgets.WishlistShare.PublicTitle"] = "{0}'s wishlist",
            ["Plugins.Widgets.WishlistShare.PublicTitleAnonymous"] = "Shared wishlist",
            ["Plugins.Widgets.WishlistShare.Unavailable"] = "This wishlist link is not available.",
            ["Plugins.Widgets.WishlistShare.Expired"] = "This wishlist link expired on {0}.",
            ["Plugins.Widgets.WishlistShare.Empty"] = "This wishlist is empty.",
            ["Plugins.Widgets.WishlistShare.AvailableUntil"] = "Available until {0}"
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UninstallAsync()
    {
        if (_widgetSettings.ActiveWidgetSystemNames.Contains(PluginDescriptor.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Remove(PluginDescriptor.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Widgets.WishlistShare");

        await base.UninstallAsync();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
    /// </summary>
    public bool HideInWidgetList => false;

    #endregion
}
