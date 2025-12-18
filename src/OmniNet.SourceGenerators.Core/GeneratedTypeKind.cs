namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Kind of generated type.
/// </summary>
public enum GeneratedTypeKind
{
    /// <summary>
    /// Reference object. <br />
    /// <code>class</code>
    /// </summary>
    Class,

    /// <summary>
    /// Interface. <br />
    /// <code>interface</code>
    /// </summary>
    Interface,

    /// <summary>
    /// Value object. <br />
    /// <code>struct</code>
    /// </summary>
    Struct,

    /// <summary>
    /// Reference record. <br />
    /// <code>record</code>
    /// </summary>
    Record,

    /// <summary>
    /// Record struct. <br />
    /// <code>record struct</code>
    /// </summary>
    RecordStruct
}