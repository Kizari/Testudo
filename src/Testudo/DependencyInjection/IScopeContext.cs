namespace Testudo;

/// <summary>
/// Holds state pertaining to the current dependency injection scope.
/// </summary>
public interface IScopeContext
{
    /// <summary>
    /// The service provider associated with the current scope.
    /// </summary>
    IServiceProvider? ServiceProvider { get; set; }
}