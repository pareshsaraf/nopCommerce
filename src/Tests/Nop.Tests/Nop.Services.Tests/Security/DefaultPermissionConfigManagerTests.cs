using AwesomeAssertions;
using Nop.Core.Domain.Customers;
using Nop.Services.Security;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Security;

[TestFixture]
public class DefaultPermissionConfigManagerTests
{
    [Test]
    public void PriceListPermissionsAreNotGrantedToVendors()
    {
        var configs = new DefaultPermissionConfigManager().AllConfigs;

        var priceListPermissions = configs.Where(config =>
            config.SystemName is StandardPermission.Catalog.PRICE_LISTS_VIEW
                or StandardPermission.Catalog.PRICE_LISTS_CREATE_EDIT_DELETE
                or StandardPermission.Catalog.PRICE_LISTS_IMPORT_EXPORT).ToList();

        priceListPermissions.Should().HaveCount(3);
        priceListPermissions.Should().OnlyContain(config =>
            config.DefaultCustomerRoles.Contains(NopCustomerDefaults.AdministratorsRoleName) &&
            !config.DefaultCustomerRoles.Contains(NopCustomerDefaults.VendorsRoleName));
    }
}
