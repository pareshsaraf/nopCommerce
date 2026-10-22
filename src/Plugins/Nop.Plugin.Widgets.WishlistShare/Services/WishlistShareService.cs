using Nop.Core;
using Nop.Data;
using Nop.Plugin.Widgets.WishlistShare.Domain;
using Nop.Services.Orders;

namespace Nop.Plugin.Widgets.WishlistShare.Services;

/// <summary>
/// Wishlist share links
/// </summary>
public class WishlistShareService
{
    #region Fields

    private readonly ICustomWishlistService _customWishlistService;
    private readonly IRepository<WishlistShareLink> _wishlistShareRepository;

    #endregion

    #region Ctor

    public WishlistShareService(ICustomWishlistService customWishlistService,
        IRepository<WishlistShareLink> wishlistShareRepository)
    {
        _customWishlistService = customWishlistService;
        _wishlistShareRepository = wishlistShareRepository;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates a public link to a customer's wishlist
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="customWishlistId">Custom wishlist identifier. 0 is the default wishlist.</param>
    /// <param name="expiryDays">Expiry period. Must be 7, 30, or 90.</param>
    /// <param name="utcNow">Creation instant. Pass null to use the current UTC time.</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the new share link
    /// </returns>
    public virtual async Task<WishlistShareLink> CreateShareAsync(int customerId, int storeId, int customWishlistId, int expiryDays, DateTime? utcNow = null)
    {
        if (customerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(customerId));

        if (storeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(storeId));

        if (customWishlistId < 0)
            throw new ArgumentOutOfRangeException(nameof(customWishlistId));

        var now = utcNow ?? DateTime.UtcNow;
        var share = new WishlistShareLink
        {
            CustomerId = customerId,
            StoreId = storeId,
            CustomWishlistId = customWishlistId,
            Token = Guid.NewGuid().ToString("D"),
            ExpiresOnUtc = WishlistShareExpiry.GetExpiresOnUtc(expiryDays, now),
            CreatedOnUtc = now
        };

        if (customWishlistId > 0)
        {
            var wishlist = await _customWishlistService.GetCustomWishlistByIdAsync(customWishlistId);
            if (wishlist == null || wishlist.CustomerId != customerId)
                throw new NopException("The wishlist does not belong to this customer.");
        }

        await _wishlistShareRepository.InsertAsync(share);

        return share;
    }

    /// <summary>
    /// Gets a share link by its public token
    /// </summary>
    /// <param name="token">Public token</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the share link, or null when the token is missing
    /// </returns>
    public virtual async Task<WishlistShareLink> GetByTokenAsync(string token)
    {
        if (!Guid.TryParse(token, out var parsed))
            return null;

        var normalized = parsed.ToString("D");
        var shares = await _wishlistShareRepository.GetAllAsync(query => query.Where(share => share.Token == normalized));

        return shares.FirstOrDefault();
    }

    /// <summary>
    /// Gets active share links for one wishlist, newest first
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="customWishlistId">Custom wishlist identifier. 0 is the default wishlist.</param>
    /// <param name="utcNow">Instant to compare. Pass null to use the current UTC time.</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the active links
    /// </returns>
    public virtual async Task<IList<WishlistShareLink>> GetActiveSharesAsync(int customerId, int storeId, int customWishlistId, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        return await _wishlistShareRepository.GetAllAsync(query =>
            query.Where(share => share.CustomerId == customerId
                                 && share.StoreId == storeId
                                 && share.CustomWishlistId == customWishlistId
                                 && share.ExpiresOnUtc > now)
                .OrderByDescending(share => share.CreatedOnUtc)
                .Take(WishlistShareDefaults.MaxListedLinks));
    }

    /// <summary>
    /// Gets a value indicating whether the link still works
    /// </summary>
    /// <param name="share">Share link</param>
    /// <param name="utcNow">Instant to compare. Pass null to use the current UTC time.</param>
    /// <returns>True while the link has not expired</returns>
    public virtual bool IsActive(WishlistShareLink share, DateTime? utcNow = null)
    {
        if (share == null)
            return false;

        return WishlistShareExpiry.IsActive(share.ExpiresOnUtc, utcNow ?? DateTime.UtcNow);
    }

    #endregion
}
