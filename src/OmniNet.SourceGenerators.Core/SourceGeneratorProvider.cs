namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Provides access to generated attributes stored as assembly resources.
/// </summary>
public static class SourceGeneratorProvider
{
    /// <summary>
    /// Gets handle to generated attribute.
    /// </summary>
    /// <typeparam name="TAttribute">Concrete type of attribute. The same type is generated into the assembly.</typeparam>
    /// <returns>Returns handle to generated attribute.</returns>
    /// <remarks>The type must be located inside Attributes folder in the root of project.</remarks>
    public static IGeneratorAttribute GetAttribute<TAttribute>()
        where TAttribute : Attribute
        => AssemblyGeneratorAttributesHelper.CreateAttribute<TAttribute>();
}