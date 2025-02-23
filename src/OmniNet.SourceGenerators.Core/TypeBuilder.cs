namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Builder to generate type's members.
/// </summary>
public ref struct TypeBuilder
{
    private bool _anyMemberWritten = false;
    private readonly StringBuilderWrapper.IndentationContext _indentationContext;
    private readonly StringBuilderWrapper _sbWrapper;

    /// <summary><inheritdoc cref="TypeBuilder" path="/summary"/></summary>
    /// <param name="sbWrapper"><inheritdoc cref="StringBuilderWrapper" path="/summary"/></param>
    internal TypeBuilder(StringBuilderWrapper sbWrapper)
    {
        _sbWrapper = sbWrapper;
        _indentationContext = sbWrapper.CreateNewIndentationContext();
    }

    /// <summary>
    /// Start building new property.
    /// </summary>
    /// <param name="propertyType">Type of property to generate.</param>
    /// <param name="name">Name of property to generate.</param>
    /// <returns>Property builder to build new property.</returns>
    public PropertyBuilder BuildProperty(ITypeSymbol propertyType, string name)
    {
        SeparateMembers();
        return new PropertyBuilder(_sbWrapper, propertyType, name);
    }

    /// <summary>
    /// Separate members with new line if any was written.
    /// </summary>
    private void SeparateMembers()
    {
        if (_anyMemberWritten)
        {
            _sbWrapper.AppendLine();
        }
        else
        {
            _anyMemberWritten = true;
        }
    }

    /// <summary>
    /// Closes type.
    /// </summary>
    /// <remarks>No method should be called afterward.</remarks>
    public void Dispose()
    {
        // TODO set flag and raise diagnostic if any method is called after dispose, it could help when it is hard to catch in inner builders...

        _indentationContext.Dispose();
        _sbWrapper.AppendLine("}");
    }
}