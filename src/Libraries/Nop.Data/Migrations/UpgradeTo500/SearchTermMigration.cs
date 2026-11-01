using FluentMigrator;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Data.Extensions;
using Nop.Data.Mapping;

namespace Nop.Data.Migrations.UpgradeTo500;

[NopSchemaMigration("2026-03-01 00:00:01", "SearchTerm migration")]
public class SearchTermMigration : ForwardOnlyMigration
{
    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        // Delete in this migration's transaction, ahead of the NOT NULL columns below.
        // TruncateAsync runs on another connection and was not waited on, so those columns
        // were added while rows remained and the upgrade failed (PostgreSQL 23502).
        Delete.FromTable(NameCompatibilityManager.GetTableName(typeof(SearchTerm))).AllRows();

        this.DeleteColumnsIfExists<SearchTerm>(["Count"]);

        this.AddOrAlterColumnFor<SearchTerm>(t => t.CreatedOnUtc)
            .AsDateTime2();

        this.AddOrAlterForeignKeyColumnFor<SearchTerm, Customer>(t => t.CustomerId);

        this.AddOrAlterColumnFor<SearchTerm>(t => t.Deleted)
            .AsBoolean();
    }
}
