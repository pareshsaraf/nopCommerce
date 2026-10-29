using FluentMigrator;
using Nop.Core.Domain.Orders;
using Nop.Data.Extensions;

namespace Nop.Data.Migrations.UpgradeTo500;

[NopSchemaMigration("2026-10-29 00:00:00", "Wishlist share link with expiry")]
public class WishlistShareLinkMigration : ForwardOnlyMigration
{
    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        this.AddOrAlterColumnFor<CustomWishlist>(t => t.ShareToken)
            .AsGuid()
            .Nullable();

        this.AddOrAlterColumnFor<CustomWishlist>(t => t.ShareTokenExpiresOnUtc)
            .AsDateTime2()
            .Nullable();
    }
}
