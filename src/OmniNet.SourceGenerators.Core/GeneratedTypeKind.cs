namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Kind of generated type.
/// </summary>
public enum GeneratedTypeKind
{
    /// <summary>
    /// Reference object.
    /// <code>class</code>
    /// </summary>
    Class,

    /// <summary>
    /// Interface.
    /// <code>interface</code>
    /// </summary>
    Interface,

    /// <summary>
    /// Value object.
    /// <code>struct</code>
    /// </summary>
    Struct
}