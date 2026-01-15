namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Function to get type reference from source item.
/// </summary>
/// <typeparam name="T">Type of source.</typeparam>
/// <param name="item">Source item.</param>
/// <returns>
/// Returned type reference or <see langword="null"/> if type reference cannot be returned.
/// </returns>
public delegate TypeReference? InterfaceGetter<in T>(T item);