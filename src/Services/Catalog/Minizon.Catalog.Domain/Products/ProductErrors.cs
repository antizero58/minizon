using Minizon.Catalog.Domain.Common;

namespace Minizon.Catalog.Domain.Products;

/// <summary>
/// Error codes and errors for <see cref="Product"/>. Codes are public behavior (ADR 0002).
/// </summary>
public static class ProductErrors
{
    public const string NameEmptyCode = "product.name.empty";
    public const string NameTooLongCode = "product.name.too_long";
    public const string PriceNotPositiveCode = "product.price.not_positive";

    public static DomainError NameEmpty { get; } =
        new(NameEmptyCode, "Product name must not be empty.");

    public static DomainError NameTooLong { get; } =
        new(NameTooLongCode, $"Product name must not be longer than {Product.MaxNameLength} characters.");

    public static DomainError PriceNotPositive { get; } =
        new(PriceNotPositiveCode, "Product price must be greater than zero.");
}
