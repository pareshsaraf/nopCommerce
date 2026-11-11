using Moq;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Misc.PunchOut;
using Nop.Plugin.Misc.PunchOut.Domain;
using Nop.Plugin.Misc.PunchOut.Services;
using Nop.Services.Authentication;
using Nop.Services.Common;
using Nop.Services.Orders;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.PunchOut;

[TestFixture]
public class PunchOutSessionCartTests
{
    [Test]
    public async Task ClearPunchoutSessionData_RemovesSubmittedCartBeforeSignOut()
    {
        var customer = new Customer { Id = 15 };
        var store = new Store { Id = 3 };
        var sessionJson = JsonConvert.SerializeObject(new PunchOutSession
        {
            SessionId = "session-1",
            CustomerId = customer.Id,
            StoreId = store.Id,
            IsActive = true
        });

        var workContext = new Mock<IWorkContext>();
        workContext.Setup(c => c.GetCurrentCustomerAsync()).ReturnsAsync(customer);

        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(c => c.GetCurrentStoreAsync()).ReturnsAsync(store);

        var attributes = new Mock<IGenericAttributeService>();
        attributes.Setup(s => s.GetAttributeAsync<string>(customer, PunchOutDefaults.PunchOutSessionAttribute, store.Id, null))
            .ReturnsAsync(sessionJson);
        attributes.Setup(s => s.SaveAttributeAsync<string>(customer, PunchOutDefaults.PunchOutSessionAttribute, null, store.Id))
            .Returns(Task.CompletedTask);

        var cache = new Mock<IStaticCacheManager>();
        cache.Setup(c => c.PrepareKeyForDefaultCache(It.IsAny<CacheKey>(), It.IsAny<object[]>()))
            .Returns((CacheKey key, object[] _) => key);
        cache.Setup(c => c.RemoveAsync(It.IsAny<CacheKey>(), It.IsAny<object[]>()))
            .Returns(Task.CompletedTask);

        var cart = new Mock<IShoppingCartService>();
        cart.Setup(s => s.ClearShoppingCartAsync(customer, store.Id)).Returns(Task.CompletedTask);

        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

        var service = CreateService(workContext.Object, storeContext.Object, attributes.Object, cache.Object, cart.Object, authentication.Object);

        await service.ClearPunchoutSessionDataAsync();

        cart.Verify(s => s.ClearShoppingCartAsync(customer, store.Id), Times.Once);
        authentication.Verify(s => s.SignOutAsync(), Times.Once);
    }

    [Test]
    public async Task ClearPunchoutSessionData_LeavesCartWhenNoSessionExists()
    {
        var customer = new Customer { Id = 15 };
        var store = new Store { Id = 3 };

        var workContext = new Mock<IWorkContext>();
        workContext.Setup(c => c.GetCurrentCustomerAsync()).ReturnsAsync(customer);

        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(c => c.GetCurrentStoreAsync()).ReturnsAsync(store);

        var attributes = new Mock<IGenericAttributeService>();
        attributes.Setup(s => s.GetAttributeAsync<string>(customer, PunchOutDefaults.PunchOutSessionAttribute, store.Id, null))
            .ReturnsAsync((string)null);
        attributes.Setup(s => s.SaveAttributeAsync<string>(customer, PunchOutDefaults.PunchOutSessionAttribute, null, store.Id))
            .Returns(Task.CompletedTask);

        var cart = new Mock<IShoppingCartService>();
        var authentication = new Mock<IAuthenticationService>();

        var service = CreateService(workContext.Object, storeContext.Object, attributes.Object, new Mock<IStaticCacheManager>().Object, cart.Object, authentication.Object);

        await service.ClearPunchoutSessionDataAsync();

        cart.Verify(s => s.ClearShoppingCartAsync(It.IsAny<Customer>(), It.IsAny<int>()), Times.Never);
        authentication.Verify(s => s.SignOutAsync(), Times.Never);
    }

    private static PunchOutService CreateService(
        IWorkContext workContext,
        IStoreContext storeContext,
        IGenericAttributeService genericAttributeService,
        IStaticCacheManager staticCacheManager,
        IShoppingCartService shoppingCartService,
        IAuthenticationService authenticationService)
    {
        return new PunchOutService(
            null,
            authenticationService,
            null,
            null,
            null,
            genericAttributeService,
            null,
            null,
            null,
            null,
            null,
            null,
            shoppingCartService,
            null,
            staticCacheManager,
            storeContext,
            null,
            null,
            workContext,
            null,
            null,
            null);
    }
}
