using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

/// <summary>
/// Custom wishlist service interface
/// </summary>
public partial interface ICustomWishlistService
{
    /// <summary>
    /// Retrieves all custom wishlists associated with the specified customer.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer whose custom wishlists are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of  <see
    /// cref="CustomWishlist"/> objects associated with the specified customer. If multiple wishlists  are not allowed,
    /// an empty list is returned.</returns>
    Task<IList<CustomWishlist>> GetAllCustomWishlistsAsync(int customerId);

    /// <summary>
    /// Adds a custom wishlist item to the repository.
    /// </summary>
    /// <param name="item">The custom wishlist item to add. Cannot be <see langword="null"/>.</param>
    Task AddCustomWishlistAsync(CustomWishlist item);

    /// <summary>
    /// Removes a custom wishlist item with the specified identifier.
    /// </summary>
    /// <param name="itemId">The unique identifier of the custom wishlist item to remove. Must be a valid identifier of an existing item.</param>
    Task RemoveCustomWishlistAsync(int itemId);

    /// <summary>
    /// Updates an existing custom wishlist in the data store if it exists.
    /// </summary>
    /// <param name="item">The custom wishlist to update. The wishlist must have a valid identifier corresponding to an existing entry.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    Task UpdateCustomWishlistAsync(CustomWishlist item);

    /// <summary>
    /// Retrieves a custom wishlist by its unique identifier.
    /// </summary>
    /// <param name="itemId">The unique identifier of the custom wishlist to retrieve. Must be a positive integer.</param>
    /// <returns>A <see cref="CustomWishlist"/> object representing the custom wishlist with the specified identifier. Returns
    /// null if no wishlist is found with the given identifier.</returns>
    Task<CustomWishlist> GetCustomWishlistByIdAsync(int itemId);

    /// <summary>
    /// Retrieves a custom wishlist by its shareable link token, provided the token has not expired.
    /// </summary>
    /// <param name="shareToken">The share token (GUID) embedded in the public shareable link.</param>
    /// <returns>A <see cref="CustomWishlist"/> whose <see cref="CustomWishlist.ShareToken"/> matches <paramref name="shareToken"/>
    /// and whose <see cref="CustomWishlist.ShareTokenExpiresOnUtc"/> is in the future; otherwise <see langword="null"/>.</returns>
    Task<CustomWishlist> GetCustomWishlistByShareTokenAsync(Guid shareToken);

    /// <summary>
    /// Generates (or renews) a 30-day shareable link token for the specified custom wishlist and persists the change.
    /// </summary>
    /// <param name="wishlistId">The identifier of the custom wishlist for which to generate the share token.</param>
    /// <returns>The updated <see cref="CustomWishlist"/> containing the new <see cref="CustomWishlist.ShareToken"/>
    /// and <see cref="CustomWishlist.ShareTokenExpiresOnUtc"/> values, or <see langword="null"/> when no wishlist
    /// with <paramref name="wishlistId"/> exists.</returns>
    Task<CustomWishlist> GenerateShareTokenAsync(int wishlistId);
}
