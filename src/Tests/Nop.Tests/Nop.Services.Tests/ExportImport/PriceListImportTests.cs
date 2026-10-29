using AwesomeAssertions;
using Nop.Core.Domain.PriceLists;
using Nop.Services.Catalog;
using Nop.Services.ExportImport;
using Nop.Services.PriceLists;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ExportImport;

[TestFixture]
public class PriceListImportTests : ServiceTest
{
    private IExportManager _exportManager;
    private IImportManager _importManager;
    private IPriceListService _priceListService;
    private IProductService _productService;

    [OneTimeSetUp]
    public void SetUp()
    {
        _exportManager = GetService<IExportManager>();
        _importManager = GetService<IImportManager>();
        _priceListService = GetService<IPriceListService>();
        _productService = GetService<IProductService>();
    }

    [Test]
    public async Task ReimportingAPriceListPreservesAnEmptyManualPrice()
    {
        var product = await _productService.GetProductBySkuAsync("BP_20_WSP");
        product.Should().NotBeNull();

        var priceList = new PriceList
        {
            Name = "Round-trip price list",
            Active = true,
            PriceCalculationType = PriceCalculationTypeEnum.PercentageDecrease,
            PriceCalculationValue = 10
        };
        await _priceListService.InsertPriceListAsync(priceList);

        var priceListItem = new PriceListItem
        {
            PriceListId = priceList.Id,
            ProductId = product.Id
        };
        await _priceListService.InsertPriceListItemAsync(priceListItem);

        try
        {
            await RoundTripAsync(priceList);

            (await ReloadAsync(priceList.Id, product.Id)).ManualPrice.Should().BeNull();

            priceListItem.ManualPrice = 0;
            await _priceListService.UpdatePriceListItemAsync(priceListItem);
            await RoundTripAsync(priceList);

            (await ReloadAsync(priceList.Id, product.Id)).ManualPrice.Should().Be(0);

            priceListItem.ManualPrice = 19.5M;
            await _priceListService.UpdatePriceListItemAsync(priceListItem);
            await RoundTripAsync(priceList);

            (await ReloadAsync(priceList.Id, product.Id)).ManualPrice.Should().Be(19.5M);
        }
        finally
        {
            var items = await _priceListService.GetPriceListItemsByPriceListIdAsync(priceList.Id);
            foreach (var item in items)
                await _priceListService.DeletePriceListItemAsync(item);

            await _priceListService.DeletePriceListAsync(priceList);
        }
    }

    private async Task RoundTripAsync(PriceList priceList)
    {
        var bytes = await _exportManager.ExportPriceListsToXlsxAsync([priceList]);
        using var stream = new MemoryStream(bytes);
        await _importManager.ImportPriceListsFromXlsxAsync(stream);
    }

    private async Task<PriceListItem> ReloadAsync(int priceListId, int productId)
    {
        var items = await _priceListService.GetPriceListItemsByPriceListIdAsync(priceListId);

        return items.First(item => item.ProductId == productId);
    }
}
