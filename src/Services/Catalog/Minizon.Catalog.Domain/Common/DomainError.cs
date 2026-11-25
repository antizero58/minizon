namespace Minizon.Catalog.Domain.Common;

/// <summary>
/// A violated domain invariant. <see cref="Code"/> is stable public behavior; <see cref="Message"/> is for logs and developers.
/// </summary>
public sealed record DomainError(string Code, string Message);
