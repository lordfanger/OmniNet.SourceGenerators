namespace OmniNet.SourceGenerators.Core;

public ref partial struct PropertyBuilder
{
    /// <summary>
    /// Specifies the virtual modifier for the property.
    /// </summary>
    private enum VirtualModifier
    {
        /// <summary>
        /// No virtual modifier.
        /// </summary>
        None,

        /// <summary>
        /// Abstract modifier.
        /// </summary>
        Abstract,

        /// <summary>
        /// Virtual modifier.
        /// </summary>
        Virtual,

        /// <summary>
        /// Override modifier.
        /// </summary>
        Override
    }
}
