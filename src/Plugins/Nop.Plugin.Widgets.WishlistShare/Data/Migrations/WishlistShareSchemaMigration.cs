using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using Nop.Plugin.Widgets.WishlistShare.Domain;

namespace Nop.Plugin.Widgets.WishlistShare.Data.Migrations;

/// <summary>
/// Creates the wishlist share table when the plugin is installed
/// </summary>
[NopMigration("2026-10-22 15:00:00", "Widgets.WishlistShare base schema", MigrationProcessType.Installation)]
public class WishlistShareLinkSchemaMigration : Migration
{
    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        this.CreateTableIfNotExists<WishlistShareLink>();

        var tableName = NameCompatibilityManager.GetTableName(typeof(WishlistShareLink));
        if (!Schema.Table(tableName).Index("IX_WishlistShareLink_Token").Exists())
        {
            Create.Index("IX_WishlistShareLink_Token")
                .OnTable(tableName)
                .OnColumn(nameof(WishlistShareLink.Token)).Ascending()
                .WithOptions().Unique();
        }
    }

    /// <summary>
    /// Collects the DOWN migration expressions
    /// </summary>
    public override void Down()
    {
        this.DeleteTableIfExists<WishlistShareLink>();
    }
}
