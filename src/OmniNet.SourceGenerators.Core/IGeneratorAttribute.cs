namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Handle to generated attribute.
/// </summary>
public interface IGeneratorAttribute
{
    /// <summary>
    /// Relative path used for generated attribute.
    /// </summary>
    public string GeneratedFilePath { get; }

    /// <summary>
    /// Source code to emit for generated attribute.
    /// </summary>
    public string SourceCode { get; }

    /// <summary>
    /// Type name of generated attribute.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Full type name of generated attribute.
    /// </summary>
    public string TypeFullName { get; }
}