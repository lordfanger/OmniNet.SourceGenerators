namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Function to get type symbol from source item.
/// </summary>
/// <typeparam name="T">Type of source.</typeparam>
/// <param name="item">Source item.</param>
/// <returns>
/// Returned source item or <see langword="null"/> if type symbol cannot be returned.
/// </returns>
public delegate ITypeSymbol? InterfaceGetter<in T>(T item);