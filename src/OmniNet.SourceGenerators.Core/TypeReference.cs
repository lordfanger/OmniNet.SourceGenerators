namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Represents a type reference that can be constructed from an ITypeSymbol, a plain string, or a namespace with type name.
/// </summary>
public readonly struct TypeReference
{
    private readonly ITypeSymbol? _typeSymbol;
    private readonly string? _rawString;
    private readonly (INamespaceSymbol? Namespace, string TypeName)? _namespaceAndTypeName;
    private readonly bool _isInitialized;

    private TypeReference(ITypeSymbol typeSymbol)
    {
        _typeSymbol = typeSymbol;
        _rawString = null;
        _namespaceAndTypeName = null;
        _isInitialized = true;
    }

    private TypeReference(string rawString)
    {
        _typeSymbol = null;
        _rawString = rawString;
        _namespaceAndTypeName = null;
        _isInitialized = true;
    }

    private TypeReference((INamespaceSymbol? Namespace, string TypeName) namespaceAndTypeName)
    {
        _typeSymbol = null;
        _rawString = null;
        _namespaceAndTypeName = namespaceAndTypeName;
        _isInitialized = true;
    }

    /// <summary>
    /// Indicates whether this TypeReference represents a void type.
    /// </summary>
    public bool IsVoid => !_isInitialized;

    /// <summary>
    /// Creates a TypeReference from an ITypeSymbol.
    /// </summary>
    /// <param name="typeSymbol">Type symbol to convert.</param>
    internal static TypeReference FromSymbol(ITypeSymbol typeSymbol)
        => new(typeSymbol);

    /// <summary>
    /// Implicit conversion from string to TypeReference.
    /// </summary>
    /// <param name="rawString">String representation of the type.</param>
    public static implicit operator TypeReference(string rawString)
        => new(rawString);

    /// <summary>
    /// Implicit conversion from (INamespaceSymbol?, string) tuple to TypeReference.
    /// </summary>
    /// <param name="value">Tuple containing optional namespace and type name.</param>
    public static implicit operator TypeReference((INamespaceSymbol? Namespace, string TypeName) value)
        => new(value);

    /// <summary>
    /// Appends this type reference to the StringBuilderWrapper.
    /// </summary>
    /// <param name="sbWrapper">The string builder wrapper to append to.</param>
    /// <param name="allowSpecialCase">Whether to use C# keywords for special types (only applies to ITypeSymbol variant).</param>
    /// <param name="forDoc">Whether to use documentation syntax (only applies to ITypeSymbol variant).</param>
    internal void AppendTo(StringBuilderWrapper sbWrapper, bool allowSpecialCase = true, bool forDoc = false)
    {
        if (_typeSymbol is not null)
        {
            sbWrapper.AppendWithNamespace(_typeSymbol, allowSpecialCase, forDoc);
        }
        else if (_rawString is not null)
        {
            sbWrapper.Append(_rawString);
        }
        else if (_namespaceAndTypeName is { } nsAndType)
        {
            if (nsAndType.Namespace is not null && nsAndType.Namespace.IsNotGlobal())
            {
                sbWrapper.AppendNamespace(nsAndType.Namespace).Append('.');
            }
            sbWrapper.Append(nsAndType.TypeName);
        }
    }

    /// <summary>
    /// Gets a string representation suitable for use in file names.
    /// </summary>
    /// <returns>A sanitized string that can be used in file names.</returns>
    internal string GetTypeNameForFile(out INamespaceSymbol? namespaceSymbol)
    {
        if (_typeSymbol is not null)
        {
            namespaceSymbol = _typeSymbol.ContainingNamespace;
            return _typeSymbol.Name;
        }
        else if (_rawString is not null)
        {
            namespaceSymbol = null;
            return _rawString;
        }
        else if (_namespaceAndTypeName is { } nsAndType)
        {
            namespaceSymbol = nsAndType.Namespace;
            var typeName = nsAndType.TypeName;
            return typeName;
        }

        // TODO : This case should not happen. Consider emit diagnostic.
        namespaceSymbol = null;
        return "Generated";
    }
}
