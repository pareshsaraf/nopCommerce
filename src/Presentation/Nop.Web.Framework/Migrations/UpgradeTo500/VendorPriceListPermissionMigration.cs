using FluentMigrator;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Data;
using Nop.Data.Migrations;
using Nop.Services.Security;

namespace Nop.Web.Framework.Migrations.UpgradeTo500;

/// <summary>
/// Price lists are store-wide. The vendor role was granted them by default and can change every customer's prices.
/// </summary>
[NopUpdateMigration("2026-10-27 00:00:00", "5.00", UpdateMigrationType.Data)]
public class VendorPriceListPermissionMigration : Migration
{
    private readonly INopDataProvider _dataProvider;

    public VendorPriceListPermissionMigration(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        var vendorRole = _dataProvider.GetTable<CustomerRole>()
            .FirstOrDefault(role => role.SystemName == NopCustomerDefaults.VendorsRoleName);

        if (vendorRole == null)
            return;

        var permissionSystemNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            StandardPermission.Catalog.PRICE_LISTS_VIEW,
            StandardPermission.Catalog.PRICE_LISTS_CREATE_EDIT_DELETE,
            StandardPermission.Catalog.PRICE_LISTS_IMPORT_EXPORT
        };

        var permissionIds = _dataProvider.GetTable<PermissionRecord>()
            .ToList()
            .Where(permission => permissionSystemNames.Contains(permission.SystemName))
            .Select(permission => permission.Id)
            .ToHashSet();

        if (permissionIds.Count == 0)
            return;

        var mappings = _dataProvider.GetTable<PermissionRecordCustomerRoleMapping>()
            .Where(mapping => mapping.CustomerRoleId == vendorRole.Id)
            .ToList()
            .Where(mapping => permissionIds.Contains(mapping.PermissionRecordId))
            .ToList();

        if (mappings.Count == 0)
            return;

        _dataProvider.BulkDeleteEntities(mappings);
    }

    /// <summary>
    /// Collects the DOWN migration expressions
    /// </summary>
    public override void Down()
    {
    }
}
