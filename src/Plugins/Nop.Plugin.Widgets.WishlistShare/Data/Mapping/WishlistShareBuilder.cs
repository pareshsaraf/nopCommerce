using System.Data;
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Stores;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Widgets.WishlistShare.Domain;

namespace Nop.Plugin.Widgets.WishlistShare.Data.Mapping;

/// <summary>
/// Represents a wishlist share entity builder
/// </summary>
public class WishlistShareLinkBuilder : NopEntityBuilder<WishlistShareLink>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(WishlistShareLink.CustomerId)).AsInt32().NotNullable().ForeignKey<Customer>(onDelete: Rule.None)
            .WithColumn(nameof(WishlistShareLink.StoreId)).AsInt32().NotNullable().ForeignKey<Store>(onDelete: Rule.None)
            .WithColumn(nameof(WishlistShareLink.Token)).AsString(36).NotNullable();
    }
}
