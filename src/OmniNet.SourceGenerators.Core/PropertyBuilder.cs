namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Builder to generate type's property.
/// </summary>
public ref partial struct PropertyBuilder
{
    private (ITypeSymbol ClassSymbol, string PropertyName)? _inheritDoc;
    private ImmutableArray<AttributeData>? _attributes;
    private Accessibility _accessibility;
    private bool _isRequired;
    private bool _implicitGetter;
    private bool _implicitSetter;
    private bool _newModifier;
    private VirtualModifier _virtualModifier = VirtualModifier.None;
    private readonly StringBuilderWrapper _sbWrapper;
    private readonly ITypeSymbol _propertyType;
    private readonly string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyBuilder"/> struct.
    /// </summary>
    /// <param name="sbWrapper">Wrapped string builder used as store.</param>
    /// <param name="propertyType">Type of property.</param>
    /// <param name="name">Property name.</param>
    internal PropertyBuilder(StringBuilderWrapper sbWrapper, ITypeSymbol propertyType, string name)
    {
        _sbWrapper = sbWrapper;
        _propertyType = propertyType;
        _name = name;
    }

    /// <summary>
    /// Sets generated property documentation inheritance from other property of type symbol.
    /// </summary>
    /// <param name="typeSymbol">Type whose property's documentation will be inherited.</param>
    /// <param name="propertyName">Name of the property whose documentation will be inherited.</param>
    /// <returns>Self builder.</returns>
    public PropertyBuilder WithInheritDoc(ITypeSymbol typeSymbol, string propertyName)
    {
        _inheritDoc = (typeSymbol, propertyName);
        return this;
    }

    /// <summary>
    /// Sets generated property getter accessor as implicit.
    /// </summary>
    /// <param name="value">Whether getter will be implicit.</param>
    /// <returns>Self builder.</returns>
    /// <remarks>At least one of accessors must be set.</remarks>
    public PropertyBuilder WithImplicitGetter(bool value = true)
    {
        _implicitGetter = value;
        return this;
    }

    /// <summary>
    /// Sets generated property setter accessor as implicit.
    /// </summary>
    /// <param name="value">Whether setter will be implicit.</param>
    /// <returns>Self builder.</returns>
    /// <remarks>At least one of accessors must be set.</remarks>
    public PropertyBuilder WithImplicitSetter(bool value = true)
    {
        _implicitSetter = value;
        return this;
    }

    /// <summary>
    /// Sets generated property accessibility.
    /// </summary>
    /// <param name="accessibility">Desired accessibility.</param>
    /// <returns>Self builder.</returns>
    public PropertyBuilder WithAccessibility(Accessibility accessibility)
    {
        _accessibility = accessibility;
        return this;
    }

    /// <summary>
    /// Sets <c>required</c> modifier to property.
    /// </summary>
    /// <param name="value">Whether the <c>required</c> modifier will be set.</param>
    /// <returns>Self builder.</returns>
    public PropertyBuilder WithRequired(bool value = true)
    {
        _isRequired = value;
        return this;
    }

    /// <summary>
    /// Sets the <c>new</c> modifier to explicitly hide a member inherited from a base class.
    /// </summary>
    /// <param name="value">Whether the <c>new</c> modifier will be set.</param>
    /// <returns>Self builder.</returns>
    public PropertyBuilder WithNew(bool value = true)
    {
        _newModifier = value;
        return this;
    }

    /// <summary>
    /// Sets the <c>virtual</c> modifier for the property.
    /// </summary>
    /// <param name="value">Whether the <c>virtual</c> modifier will be set.</param>
    /// <returns>Self builder.</returns>
    public PropertyBuilder WithVirtual(bool value = true) => WithVirtualModifier(VirtualModifier.Virtual, value);

    /// <summary>
    /// Sets the <c>abstract</c> modifier for the property.
    /// </summary>
    /// <param name="value">Whether the <c>abstract</c> modifier will be set.</param>
    /// <returns>Self builder.</returns>
    public PropertyBuilder WithAbstract(bool value = true) => WithVirtualModifier(VirtualModifier.Abstract, value);

    /// <summary>
    /// Sets the <c>override</c> modifier for the property.
    /// </summary>
    /// <param name="value">Whether the <c>override</c> modifier will be set.</param>
    /// <returns>Self builder.</returns>
    public PropertyBuilder WithOverride(bool value = true) => WithVirtualModifier(VirtualModifier.Override, value);

    private PropertyBuilder WithVirtualModifier(VirtualModifier virtualModifier, bool value)
    {
        if (!value)
        {
            if (_virtualModifier != virtualModifier)
            {
                // TODO emit error (diagnostics)
                return this;
            }
            _virtualModifier = VirtualModifier.None;
        }
        else
        {
            _virtualModifier = virtualModifier;
        }

        return this;
    }

    /// <summary>
    /// Sets the collection of attributes on property.
    /// </summary>
    /// <param name="attributes">Collection of attributes to be applied on property.</param>
    /// <returns>Self builder.</returns>
    public PropertyBuilder WithAttributes(ImmutableArray<AttributeData> attributes)
    {
        _attributes = attributes;
        return this;
    }

    /// <summary>
    /// Appends generated property definition to the source code.
    /// </summary>
    public readonly void Append()
    {
        // Documentation.
        if (_inheritDoc is { } inheritDoc)
        {
            _sbWrapper.Append("/// <inheritdoc ").AppendDocSymbolReference(inheritDoc.ClassSymbol, inheritDoc.PropertyName).AppendLine("/>");
        }

        // Defined attributes.
        if (_attributes is { Length: > 0 } attributes)
        {
            foreach (var attribute in attributes)
            {
                _sbWrapper.Append('[');
                _sbWrapper.AppendWithNamespace(attribute.AttributeClass!);

                if (attribute.ConstructorArguments is { Length: > 0 } args)
                {
                    _sbWrapper.Append('(');
                    var argumentWritten = false;
                    foreach (var typedConstant in args)
                    {
                        if (argumentWritten)
                        {
                            _sbWrapper.Append(',');
                        }
                        else
                        {
                            argumentWritten = true;
                        }

                        _sbWrapper.Append(typedConstant.ToCSharpString());
                    }
                    _sbWrapper.Append(')');
                }
                _sbWrapper.AppendLine("]");
            }
        }

        // Modifiers.
        if (_accessibility != Accessibility.NotApplicable)
        {
            _sbWrapper.Append(SyntaxFacts.GetText(_accessibility)).Append(' ');
        }

        if (_isRequired)
        {
            _sbWrapper.Append("required ");
        }

        if (_newModifier)
        {
            _sbWrapper.Append("new ");
        }

        // Property type and name.
        _sbWrapper.AppendWithNamespace(_propertyType).Append(' ').Append(_name);

        // Implementation.
        switch (_implicitGetter, _implicitSetter)
        {
            case (true, false):
                _sbWrapper.Append(" { get; }");
                break;
            case (true, true):
                _sbWrapper.Append(" { get; set; }");
                break;
            case (false, true):
                _sbWrapper.Append(" { set; }");
                break;
            case (false, false):
                // TODO emit diagnostics, not accessor specified
                break;
        }

        _sbWrapper.AppendLine();
    }
}
