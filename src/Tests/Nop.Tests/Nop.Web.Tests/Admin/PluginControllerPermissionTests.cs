using System.Reflection;
using AwesomeAssertions;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using NUnit.Framework;

namespace Nop.Tests.Nop.Web.Tests.Admin;

[TestFixture]
public class PluginControllerPermissionTests
{
    [Test]
    public void ApplyChangesRequiresManagePluginsPermission()
    {
        var method = typeof(PluginController).GetMethod(nameof(PluginController.ApplyChanges));

        method.Should().NotBeNull();

        var permission = method!.GetCustomAttribute<CheckPermissionAttribute>();

        permission.Should().NotBeNull();
        permission!.PermissionSystemName.Should().Contain(StandardPermission.Configuration.MANAGE_PLUGINS);
    }
}
