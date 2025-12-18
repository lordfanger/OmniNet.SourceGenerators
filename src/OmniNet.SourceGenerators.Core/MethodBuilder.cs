namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Builder to generate type's method.
/// </summary>
public ref partial struct MethodBuilder
{
    private (ITypeSymbol TypeSymbol, string MethodName)? _inheritDoc;
    private ImmutableArray<AttributeData>? _attributes;
    private Accessibility _accessibility;
    private bool _isStatic;
    private bool _isAsync;
    private bool _isPartial;
    private bool _newModifier;
    private VirtualModifier _virtualModifier = VirtualModifier.None;
    private readonly StringBuilderWrapper _sbWrapper;
    private readonly string _name;
    private readonly ITypeSymbol? _returnType; // null = void

    /// <summary>
    /// Initializes a new instance of the <see cref="MethodBuilder"/> struct.
    /// </summary>
    /// <param name="sbWrapper">Wrapped string builder used as store.</param>
    /// <param name="name">Method name.</param>
    /// <param name="returnType">Return type of method. If null, method will be void.</param>
    internal MethodBuilder(StringBuilderWrapper sbWrapper, string name, ITypeSymbol? returnType)
    {
        _sbWrapper = sbWrapper;
        _name = name;
        _returnType = returnType;
    }

    /// <summary>
    /// Sets generated method documentation inheritance from other method of type symbol.
    /// </summary>
    /// <param name="typeSymbol">Type whose method's documentation will be inherited.</param>
    /// <param name="methodName">Name of the method whose documentation will be inherited.</param>
    /// <returns>Self builder.</returns>
    public MethodBuilder WithInheritDoc(ITypeSymbol typeSymbol, string methodName)
    {
        _inheritDoc = (typeSymbol, methodName);
        return this;
    }

    /// <summary>
    /// Sets generated method accessibility.
    /// </summary>
    /// <param name="accessibility">Desired accessibility.</param>
    /// <returns>Self builder.</returns>
    public MethodBuilder WithAccessibility(Accessibility accessibility)
    {
        _accessibility = accessibility;
        return this;
    }

    /// <summary>
    /// Sets the <c>static</c> modifier to method.
    /// </summary>
    /// <param name="value">Whether the <c>static</c> modifier will be set.</param>
    /// <returns>Self builder.</returns>
    public MethodBuilder WithStatic(bool value = true)
    {
        _isStatic = value;
        return this;
    }

    /// <summary>
    /// Sets the <c>async</c> modifier to method.
    /// </summary>
    /// <param name="value">Whether the <c>async</c> modifier will be set.</param>
    /// <returns>Self builder.</returns>
    public MethodBuilder WithAsync(bool value = true)
    {
        _isAsync = value;
        return this;
    }

    /// <summary>
    /// Sets the <c>partial</c> modifier to method.
    /// </summary>
    /// <param name="value">Whether the <c>partial</c> modifier will be set.</param>
    /// <returns>Self builder.</returns>
    public MethodBuilder WithPartial(bool value = true)
    {
        _isPartial = value;
        return this;
    }

    /// <summary>
    /// Sets the <c>new</c> modifier to explicitly hide a member inherited from a base class.
    /// </summary>
    /// <param name="value">Whether the <c>new</c> modifier will be set.</param>
    /// <returns>Self builder.</returns>
    public MethodBuilder WithNew(bool value = true)
    {
        _newModifier = value;
        return this;
    }

    /// <summary>
    /// Sets the <c>virtual</c> modifier for the method.
    /// </summary>
    /// <param name="value">Whether the <c>virtual</c> modifier will be set.</param>
    /// <returns>Self builder.</returns>
    public MethodBuilder WithVirtual(bool value = true) => WithVirtualModifier(VirtualModifier.Virtual, value);

    /// <summary>
    /// Sets the <c>abstract</c> modifier for the method.
    /// </summary>
    /// <param name="value">Whether the <c>abstract</c> modifier will be set.</param>
    /// <returns>Self builder.</returns>
    public MethodBuilder WithAbstract(bool value = true) => WithVirtualModifier(VirtualModifier.Abstract, value);

    /// <summary>
    /// Sets the <c>override</c> modifier for the method.
    /// </summary>
    /// <param name="value">Whether the <c>override</c> modifier will be set.</param>
    /// <returns>Self builder.</returns>
    public MethodBuilder WithOverride(bool value = true) => WithVirtualModifier(VirtualModifier.Override, value);

    private MethodBuilder WithVirtualModifier(VirtualModifier virtualModifier, bool value)
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
    /// Sets the collection of attributes on method.
    /// </summary>
    /// <param name="attributes">Collection of attributes to be applied on method.</param>
    /// <returns>Self builder.</returns>
    public MethodBuilder WithAttributes(ImmutableArray<AttributeData> attributes)
    {
        _attributes = attributes;
        return this;
    }

    /// <summary>
    /// Opens parameter builder to add parameters to method.
    /// </summary>
    /// <returns>Parameters builder.</returns>
    public MethodParametersBuilder OpenParameters()
    {
        AppendDocumentationAndSignature();
        return new MethodParametersBuilder(_sbWrapper, this);
    }

    /// <summary>
    /// Appends documentation and method signature (modifiers, return type, name).
    /// </summary>
    private void AppendDocumentationAndSignature()
    {
        // Documentation.
        if (_inheritDoc is { } inheritDoc)
        {
            _sbWrapper.Append("/// <inheritdoc ").AppendDocSymbolReference(inheritDoc.TypeSymbol, inheritDoc.MethodName).AppendLine("/>");
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

        if (_isStatic)
        {
            _sbWrapper.Append("static ");
        }

        if (_newModifier)
        {
            _sbWrapper.Append("new ");
        }

        switch (_virtualModifier)
        {
            case VirtualModifier.Virtual:
                _sbWrapper.Append("virtual ");
                break;
            case VirtualModifier.Abstract:
                _sbWrapper.Append("abstract ");
                break;
            case VirtualModifier.Override:
                _sbWrapper.Append("override ");
                break;
        }

        if (_isPartial)
        {
            _sbWrapper.Append("partial ");
        }

        if (_isAsync)
        {
            _sbWrapper.Append("async ");
        }

        // Return type and method name.
        if (_returnType != null)
        {
            _sbWrapper.AppendWithNamespace(_returnType).Append(' ');
        }
        else
        {
            _sbWrapper.Append("void ");
        }

        _sbWrapper.Append(_name).Append('(');
    }
}
