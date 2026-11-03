using AwesomeAssertions;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Orders;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Orders;

[TestFixture]
public class ShoppingCartServiceMoveWishlistTests : ServiceTest
{
    private IShoppingCartService _shoppingCartService;
    private ICustomWishlistService _customWishlistService;
    private ICustomerService _customerService;
    private IProductService _productService;
    private IRepository<ShoppingCartItem> _shoppingCartItemRepository;
    private IRepository<CustomWishlist> _customWishlistRepository;

    private Customer _owner;
    private Customer _otherCustomer;
    private int _productId;
    private int _storeId;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _shoppingCartService = GetService<IShoppingCartService>();
        _customWishlistService = GetService<ICustomWishlistService>();
        _customerService = GetService<ICustomerService>();
        _productService = GetService<IProductService>();
        _shoppingCartItemRepository = GetService<IRepository<ShoppingCartItem>>();
        _customWishlistRepository = GetService<IRepository<CustomWishlist>>();

        _owner = await InsertCustomerAsync();
        _otherCustomer = await InsertCustomerAsync();
        _productId = (await _productService.GetProductBySkuAsync("FR_451_RB")).Id;
        _storeId = 1;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await DeleteCustomerDataAsync(_owner);
        await DeleteCustomerDataAsync(_otherCustomer);
        await _customerService.DeleteCustomerAsync(_owner);
        await _customerService.DeleteCustomerAsync(_otherCustomer);
    }

    [TearDown]
    public async Task TearDown()
    {
        await DeleteCustomerDataAsync(_owner);
        await DeleteCustomerDataAsync(_otherCustomer);
    }

    [Test]
    public async Task MoveItemToCustomWishlistShouldNotDeleteAnotherCustomersCartItem()
    {
        var cartItem = await InsertItemAsync(_owner, ShoppingCartType.ShoppingCart, quantity: 2);

        await _shoppingCartService.MoveItemToCustomWishlistAsync(_otherCustomer, cartItem.Id);

        var reloaded = await _shoppingCartItemRepository.GetByIdAsync(cartItem.Id);
        reloaded.Should().NotBeNull();
        reloaded.CustomerId.Should().Be(_owner.Id);
        reloaded.Quantity.Should().Be(2);
        reloaded.ShoppingCartType.Should().Be(ShoppingCartType.ShoppingCart);
        reloaded.CustomWishlistId.Should().BeNull();
    }

    [Test]
    public async Task MoveItemToCustomWishlistShouldNotReassignAnotherCustomersWishlistItem()
    {
        var wishlist = await InsertWishlistAsync(_otherCustomer);
        var wishlistItem = await InsertItemAsync(_owner, ShoppingCartType.Wishlist, quantity: 1);

        await _shoppingCartService.MoveItemToCustomWishlistAsync(_otherCustomer, wishlistItem.Id, wishlist.Id);

        var reloaded = await _shoppingCartItemRepository.GetByIdAsync(wishlistItem.Id);
        reloaded.Should().NotBeNull();
        reloaded.CustomerId.Should().Be(_owner.Id);
        reloaded.CustomWishlistId.Should().BeNull();
        reloaded.Quantity.Should().Be(1);
    }

    [Test]
    public async Task MoveItemToCustomWishlistShouldMoveTheOwnersWishlistItem()
    {
        var wishlist = await InsertWishlistAsync(_owner);
        var wishlistItem = await InsertItemAsync(_owner, ShoppingCartType.Wishlist, quantity: 1);

        await _shoppingCartService.MoveItemToCustomWishlistAsync(_owner, wishlistItem.Id, wishlist.Id);

        var reloaded = await _shoppingCartItemRepository.GetByIdAsync(wishlistItem.Id);
        reloaded.Should().NotBeNull();
        reloaded.CustomWishlistId.Should().Be(wishlist.Id);
        reloaded.Quantity.Should().Be(1);
    }

    private async Task<Customer> InsertCustomerAsync()
    {
        var customer = new Customer
        {
            CustomerGuid = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@wishlist-move.test",
            CreatedOnUtc = DateTime.UtcNow,
            LastActivityDateUtc = DateTime.UtcNow,
            Active = true
        };
        await _customerService.InsertCustomerAsync(customer);

        return customer;
    }

    private async Task<ShoppingCartItem> InsertItemAsync(Customer customer, ShoppingCartType cartType, int quantity)
    {
        var item = new ShoppingCartItem
        {
            CustomerId = customer.Id,
            ProductId = _productId,
            StoreId = _storeId,
            ShoppingCartType = cartType,
            Quantity = quantity,
            AttributesXml = string.Empty,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };
        await _shoppingCartItemRepository.InsertAsync(item);

        return item;
    }

    private async Task<CustomWishlist> InsertWishlistAsync(Customer customer)
    {
        var wishlist = new CustomWishlist
        {
            Name = $"List {Guid.NewGuid():N}",
            CustomerId = customer.Id,
            CreatedOnUtc = DateTime.UtcNow
        };
        await _customWishlistService.AddCustomWishlistAsync(wishlist);

        return wishlist;
    }

    private async Task DeleteCustomerDataAsync(Customer customer)
    {
        if (customer == null || customer.Id == 0)
            return;

        await _shoppingCartItemRepository.DeleteAsync(item => item.CustomerId == customer.Id);
        await _customWishlistRepository.DeleteAsync(wishlist => wishlist.CustomerId == customer.Id);
    }
}
