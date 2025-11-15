namespace Shared.Contract.Owned;

/// <summary>
/// Owned service
/// </summary>
public interface IOwnedService<TService>
{
    /// <summary>
    /// Owned value
    /// </summary>
    TService Value { get; }
}
