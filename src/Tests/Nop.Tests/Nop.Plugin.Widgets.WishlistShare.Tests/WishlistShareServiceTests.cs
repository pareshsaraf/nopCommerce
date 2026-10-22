using AwesomeAssertions;
using Moq;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Plugin.Widgets.WishlistShare.Domain;
using Nop.Plugin.Widgets.WishlistShare.Services;
using Nop.Services.Orders;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Widgets.WishlistShare.Tests;

[TestFixture]
public class WishlistShareServiceTests
{
    private Mock<IRepository<WishlistShareLink>> _repository;
    private Mock<ICustomWishlistService> _customWishlistService;
    private WishlistShareService _service;
    private readonly DateTime _utcNow = new(2026, 10, 22, 9, 30, 0, DateTimeKind.Utc);

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IRepository<WishlistShareLink>>();
        _repository
            .Setup(repository => repository.InsertAsync(It.IsAny<WishlistShareLink>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _customWishlistService = new Mock<ICustomWishlistService>();
        _service = new WishlistShareService(_customWishlistService.Object, _repository.Object);
    }

    [TestCase(7)]
    [TestCase(30)]
    [TestCase(90)]
    public async Task CreateShareAsync_AllowsSevenThirtyAndNinetyDays(int expiryDays)
    {
        var share = await _service.CreateShareAsync(customerId: 4, storeId: 1, customWishlistId: 0, expiryDays, _utcNow);

        share.CustomerId.Should().Be(4);
        share.StoreId.Should().Be(1);
        share.CustomWishlistId.Should().Be(0);
        share.CreatedOnUtc.Should().Be(_utcNow);
        share.ExpiresOnUtc.Should().Be(_utcNow.AddDays(expiryDays));
        Guid.TryParse(share.Token, out _).Should().BeTrue();
        _service.IsActive(share, _utcNow.AddDays(expiryDays).AddSeconds(-1)).Should().BeTrue();
        _service.IsActive(share, share.ExpiresOnUtc).Should().BeFalse();
        _repository.Verify(repository => repository.InsertAsync(share, true), Times.Once);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(14)]
    [TestCase(60)]
    [TestCase(365)]
    public async Task CreateShareAsync_RejectsAnyOtherExpiry(int expiryDays)
    {
        var act = () => _service.CreateShareAsync(4, 1, 0, expiryDays, _utcNow);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .Where(exception => exception.ParamName == "expiryDays");
        _repository.Verify(repository => repository.InsertAsync(It.IsAny<WishlistShareLink>(), It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public async Task CreateShareAsync_RejectsAnotherCustomersWishlist()
    {
        _customWishlistService
            .Setup(service => service.GetCustomWishlistByIdAsync(9))
            .ReturnsAsync(new CustomWishlist { Id = 9, CustomerId = 8, Name = "Gifts" });

        var act = () => _service.CreateShareAsync(4, 1, 9, 7, _utcNow);

        await act.Should().ThrowAsync<NopException>();
        _repository.Verify(repository => repository.InsertAsync(It.IsAny<WishlistShareLink>(), It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public async Task CreateShareAsync_AcceptsTheOwnersCustomWishlist()
    {
        _customWishlistService
            .Setup(service => service.GetCustomWishlistByIdAsync(9))
            .ReturnsAsync(new CustomWishlist { Id = 9, CustomerId = 4, Name = "Gifts" });

        var share = await _service.CreateShareAsync(4, 1, 9, 30, _utcNow);

        share.CustomWishlistId.Should().Be(9);
        share.ExpiresOnUtc.Should().Be(_utcNow.AddDays(30));
        _repository.Verify(repository => repository.InsertAsync(share, true), Times.Once);
    }
}
