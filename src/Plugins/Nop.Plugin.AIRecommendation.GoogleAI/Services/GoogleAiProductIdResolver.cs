using System.Globalization;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.AIRecommendation.GoogleAI.Services;

/// <summary>
/// Maps Google Retail product ids back to nopCommerce catalog products.
/// </summary>
/// <remarks>
/// A catalog entity is uploaded with its SKU as the Retail id when the SKU is set,
/// and with its own primary key only when the SKU is empty. A combination primary key
/// is not a product id. Numeric SKUs must be resolved as SKUs.
/// </remarks>
public static class GoogleAiProductIdResolver
{
    /// <summary>
    /// Resolves Retail product ids to nopCommerce product ids, in the returned order.
    /// </summary>
    /// <param name="externalIds">Retail product ids, in rank order</param>
    /// <param name="productsBySku">Products whose SKU is one of the Retail ids</param>
    /// <param name="combinationsBySku">Attribute combinations whose SKU is one of the Retail ids</param>
    /// <param name="productsById">Products whose primary key was encoded as a Retail id</param>
    /// <param name="combinationsById">Attribute combinations whose primary key was encoded as a Retail id</param>
    /// <returns>Distinct product ids</returns>
    public static List<int> Resolve(IList<string> externalIds,
        IList<Product> productsBySku,
        IList<ProductAttributeCombination> combinationsBySku,
        IList<Product> productsById,
        IList<ProductAttributeCombination> combinationsById)
    {
        ArgumentNullException.ThrowIfNull(externalIds);
        ArgumentNullException.ThrowIfNull(productsBySku);
        ArgumentNullException.ThrowIfNull(combinationsBySku);
        ArgumentNullException.ThrowIfNull(productsById);
        ArgumentNullException.ThrowIfNull(combinationsById);

        var resolved = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var externalId in externalIds)
        {
            if (!string.IsNullOrEmpty(externalId))
                resolved.TryAdd(externalId, 0);
        }

        foreach (var product in productsBySku.OrderBy(product => product.Id))
        {
            if (product.Deleted || string.IsNullOrEmpty(product.Sku))
                continue;

            if (resolved.TryGetValue(product.Sku, out var productId) && productId == 0)
                resolved[product.Sku] = product.Id;
        }

        foreach (var combination in combinationsBySku.OrderBy(combination => combination.Id))
        {
            if (string.IsNullOrEmpty(combination.Sku))
                continue;

            if (resolved.TryGetValue(combination.Sku, out var productId) && productId == 0)
                resolved[combination.Sku] = combination.ProductId;
        }

        // Primary keys are Retail ids only for entities that have no SKU.
        foreach (var product in productsById.OrderBy(product => product.Id))
        {
            if (product.Deleted || product.Id <= 0 || !string.IsNullOrEmpty(product.Sku))
                continue;

            var key = product.Id.ToString(CultureInfo.InvariantCulture);
            if (resolved.TryGetValue(key, out var productId) && productId == 0)
                resolved[key] = product.Id;
        }

        foreach (var combination in combinationsById.OrderBy(combination => combination.Id))
        {
            if (combination.Id <= 0 || !string.IsNullOrEmpty(combination.Sku))
                continue;

            var key = combination.Id.ToString(CultureInfo.InvariantCulture);
            if (resolved.TryGetValue(key, out var productId) && productId == 0)
                resolved[key] = combination.ProductId;
        }

        var seen = new HashSet<int>();
        var productIds = new List<int>();

        foreach (var externalId in externalIds)
        {
            if (string.IsNullOrEmpty(externalId))
                continue;

            if (resolved.TryGetValue(externalId, out var productId) && productId > 0 && seen.Add(productId))
                productIds.Add(productId);
        }

        return productIds;
    }

    /// <summary>
    /// Gets entity primary keys that may have been uploaded as Retail ids.
    /// A value with leading zeros is a SKU, not an entity id.
    /// </summary>
    /// <param name="externalIds">Retail product ids</param>
    /// <returns>Candidate entity ids</returns>
    public static int[] GetEntityIds(IEnumerable<string> externalIds)
    {
        ArgumentNullException.ThrowIfNull(externalIds);

        return externalIds
            .Select(externalId => TryGetEntityId(externalId, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
    }

    private static bool TryGetEntityId(string value, out int id)
    {
        id = 0;

        if (string.IsNullOrEmpty(value))
            return false;

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id) || id <= 0)
            return false;

        return id.ToString(CultureInfo.InvariantCulture) == value;
    }
}
