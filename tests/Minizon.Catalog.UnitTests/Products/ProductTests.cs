using Minizon.Catalog.Domain.Products;

namespace Minizon.Catalog.UnitTests.Products;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_ReturnsProduct()
    {
        var result = Product.Create("Coffee mug", 9.99m);

        Assert.True(result.IsSuccess);
        Assert.Equal("Coffee mug", result.Value.Name);
        Assert.Equal(9.99m, result.Value.Price);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(7, result.Value.Id.Version);
    }

    [Fact]
    public void Create_WithSurroundingSpaces_StoresTrimmedName()
    {
        var result = Product.Create("  Coffee mug  ", 9.99m);

        Assert.True(result.IsSuccess);
        Assert.Equal("Coffee mug", result.Value.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\u00A0")]
    public void Create_WithEmptyName_ReturnsNameEmpty(string? name)
    {
        var result = Product.Create(name, 9.99m);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ProductErrors.NameEmptyCode, error.Code);
    }

    [Fact]
    public void Create_WithNameLongerThanMax_ReturnsNameTooLong()
    {
        var result = Product.Create(new string('a', Product.MaxNameLength + 1), 9.99m);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ProductErrors.NameTooLongCode, error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNotPositivePrice_ReturnsPriceNotPositive(int price)
    {
        var result = Product.Create("Coffee mug", price);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ProductErrors.PriceNotPositiveCode, error.Code);
    }

    [Fact]
    public void Create_WithEmptyNameAndZeroPrice_ReturnsBothErrors()
    {
        var result = Product.Create("", 0m);

        Assert.True(result.IsFailure);
        Assert.Equal(
            [ProductErrors.NameEmptyCode, ProductErrors.PriceNotPositiveCode],
            result.Errors.Select(e => e.Code));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(199)]
    [InlineData(200)]
    public void Create_WithNameWithinLengthLimit_ReturnsProduct(int length)
    {
        var name = new string('a', length);

        var result = Product.Create(name, 9.99m);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Name);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Create_WithPaddedNameAtLengthLimit_StoresTrimmedName()
    {
        var name = new string('a', 200);

        var result = Product.Create($"  {name}  ", 9.99m);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Name);
    }

    [Fact]
    public void Create_WithPaddedNameOverLengthLimit_ReturnsOnlyNameTooLong()
    {
        var result = Product.Create($"  {new string('a', 201)}  ", 9.99m);

        Assert.True(result.IsFailure);
        Assert.Equal("product.name.too_long", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Create_WithVeryLongName_ReturnsOnlyNameTooLong()
    {
        var result = Product.Create(new string('a', 10_000), 9.99m);

        Assert.True(result.IsFailure);
        Assert.Equal("product.name.too_long", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Create_With100Emoji_ReturnsProduct()
    {
        var name = string.Concat(Enumerable.Repeat("\U0001F600", 100));

        var result = Product.Create(name, 9.99m);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Name);
    }

    [Fact]
    public void Create_With101Emoji_ReturnsOnlyNameTooLong()
    {
        var name = string.Concat(Enumerable.Repeat("\U0001F600", 101));

        var result = Product.Create(name, 9.99m);

        Assert.True(result.IsFailure);
        Assert.Equal("product.name.too_long", Assert.Single(result.Errors).Code);
    }

    public static TheoryData<decimal> PositivePriceExtremes =>
        new() { 0.0000000000000000000000000001m, decimal.MaxValue };

    [Theory]
    [MemberData(nameof(PositivePriceExtremes))]
    public void Create_WithPositivePriceExtreme_PreservesPrice(decimal price)
    {
        var result = Product.Create("Coffee mug", price);

        Assert.True(result.IsSuccess);
        Assert.Equal(price, result.Value.Price);
    }

    public static TheoryData<decimal> NonPositivePriceExtremes =>
        new() { -0.0000000000000000000000000001m, decimal.MinValue };

    [Theory]
    [MemberData(nameof(NonPositivePriceExtremes))]
    public void Create_WithNonPositivePriceExtreme_ReturnsOnlyPriceNotPositive(decimal price)
    {
        var result = Product.Create("Coffee mug", price);

        Assert.True(result.IsFailure);
        Assert.Equal("product.price.not_positive", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Create_WithNegativeZeroPrice_ReturnsOnlyPriceNotPositive()
    {
        var negativeZero = new decimal(0, 0, 0, isNegative: true, scale: 0);

        var result = Product.Create("Coffee mug", negativeZero);

        Assert.True(result.IsFailure);
        Assert.Equal("product.price.not_positive", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Create_WithTooLongNameAndNegativePrice_ReturnsEachApplicableErrorOnce()
    {
        var result = Product.Create(new string('a', 201), -1m);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ["product.name.too_long", "product.price.not_positive"],
            result.Errors.Select(error => error.Code).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Create_TwiceWithSameValidData_AssignsDistinctIds()
    {
        var first = Product.Create("Coffee mug", 9.99m);
        var second = Product.Create("Coffee mug", 9.99m);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value.Id, second.Value.Id);
    }
}
