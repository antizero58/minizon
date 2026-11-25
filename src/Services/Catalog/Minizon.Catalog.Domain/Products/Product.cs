using Minizon.Catalog.Domain.Common;

namespace Minizon.Catalog.Domain.Products;

public sealed class Product
{
    /// <summary>Maximum name length in UTF-16 code units, after trimming.</summary>
    public const int MaxNameLength = 200;

    private Product(Guid id, string name, decimal price)
    {
        Id = id;
        Name = name;
        Price = price;
    }

    public Guid Id { get; }

    public string Name { get; }

    public decimal Price { get; }

    /// <summary>
    /// Creates a product. The name is trimmed; all violated invariants are returned at once.
    /// </summary>
    public static Result<Product> Create(string? name, decimal price)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        var errors = new List<DomainError>();

        if (trimmedName.Length == 0)
        {
            errors.Add(ProductErrors.NameEmpty);
        }
        else if (trimmedName.Length > MaxNameLength)
        {
            errors.Add(ProductErrors.NameTooLong);
        }

        if (price <= 0)
        {
            errors.Add(ProductErrors.PriceNotPositive);
        }

        return errors.Count > 0
            ? Result.Failure<Product>(errors)
            : Result.Success(new Product(Guid.CreateVersion7(), trimmedName, price));
    }
}
