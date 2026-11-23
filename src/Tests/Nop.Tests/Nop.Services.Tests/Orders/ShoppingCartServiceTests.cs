using AwesomeAssertions;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Services.Stores;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Orders;

[TestFixture]
public class ShoppingCartServiceTests : ServiceTest
{
    private IShoppingCartService _shoppingCartService;
    private ICustomerService _customerService;
    private IGenericAttributeService _genericAttributeService;
    private IRepository<ShoppingCartItem> _sciRepository;
    private Customer _customer;
    private int _storeId;
    private int _productId;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _shoppingCartService = GetService<IShoppingCartService>();
        _customerService = GetService<ICustomerService>();
        _genericAttributeService = GetService<IGenericAttributeService>();
        _sciRepository = GetService<IRepository<ShoppingCartItem>>();

        _customer = new Customer
        {
            CustomerGuid = Guid.NewGuid(),
            Email = $"stash-{Guid.NewGuid():N}@example.com",
            CreatedOnUtc = DateTime.UtcNow,
            LastActivityDateUtc = DateTime.UtcNow,
            Active = true
        };
        await _customerService.InsertCustomerAsync(_customer);

        _storeId = (await GetService<IStoreService>().GetAllStoresAsync()).First().Id;
        _productId = (await GetService<IProductService>().GetProductBySkuAsync("FR_451_RB")).Id;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _sciRepository.DeleteAsync(item => item.CustomerId == _customer.Id);
        await _customerService.DeleteCustomerAsync(_customer);
    }

    [Test]
    public async Task ReplacingActiveCartKeepsStashedItemsUntilTheVendorSelectionIsCleared()
    {
        var activeItem = new ShoppingCartItem
        {
            CustomerId = _customer.Id,
            ProductId = _productId,
            StoreId = _storeId,
            Quantity = 1,
            ShoppingCartType = ShoppingCartType.ShoppingCart,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };
        var stashedItem = new ShoppingCartItem
        {
            CustomerId = _customer.Id,
            ProductId = _productId,
            StoreId = _storeId,
            Quantity = 2,
            ShoppingCartType = ShoppingCartType.Stash,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };

        await _sciRepository.InsertAsync(activeItem);
        await _sciRepository.InsertAsync(stashedItem);
        await _genericAttributeService.SaveAttributeAsync(_customer, NopCustomerDefaults.ShoppingCartVendorAttribute, 5, _storeId);

        // Same order as accepting a quote: drop the active cart, then clear the vendor
        // without pulling the stash into that cart.
        await _shoppingCartService.ClearShoppingCartAsync(_customer, _storeId);
        await _shoppingCartService.SetShoppingCartVendorAsync(_customer, null, _storeId, reassignItems: false);

        var vendorId = await _genericAttributeService.GetAttributeAsync<int?>(_customer, NopCustomerDefaults.ShoppingCartVendorAttribute, _storeId);
        vendorId.Should().BeNull();

        // Read through the repository. GetShoppingCartAsync keeps a short-term cache of the
        // list it just deleted, so that cache still contains the removed active item.
        var remaining = _sciRepository.Table.Where(item => item.CustomerId == _customer.Id).ToList();
        remaining.Should().ContainSingle(item => item.Id == stashedItem.Id && item.ShoppingCartType == ShoppingCartType.Stash);

        // Leaving quote mode (or placing the quote order) returns those items to the cart.
        await _shoppingCartService.SetShoppingCartVendorAsync(_customer, null, _storeId);

        remaining = _sciRepository.Table.Where(item => item.CustomerId == _customer.Id).ToList();
        remaining.Should().ContainSingle(item => item.Id == stashedItem.Id && item.ShoppingCartType == ShoppingCartType.ShoppingCart);
    }
}
