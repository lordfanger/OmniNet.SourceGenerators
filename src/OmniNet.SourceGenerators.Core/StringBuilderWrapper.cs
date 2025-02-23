namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Wrapped string builder for source code generation.
/// </summary>
/// <param name="sb">Wrapped string builder used as store.</param>
internal class StringBuilderWrapper(StringBuilder sb)
{
    private bool _newLine;

    private int Indentation { get; set; }

    /// <summary>
    /// Appends text to the source code.
    /// </summary>
    /// <param name="text">Text to be appended into the source code.</param>
    /// <returns>Self wrapper.</returns>
    /// <remarks>If added text is not empty and current position is on new line the proper indentation level is appended first.</remarks>
    public StringBuilderWrapper Append(string text)
    {
        if (text.Length == 0) return this;

        EnsureIndentation();
        sb.Append(text);
        return this;
    }

    /// <summary>
    /// Appends character to the source code.
    /// </summary>
    /// <param name="c">Character to append into the source code.</param>
    /// <returns>Self wrapper.</returns>
    /// <remarks>If current position is on new line the proper indentation level is appended first.</remarks>
    public StringBuilderWrapper Append(char c)
    {
        EnsureIndentation();
        sb.Append(c);
        return this;
    }

    /// <summary>
    /// Appends the default line terminator to the source code.
    /// </summary>
    /// <returns>Self wrapper.</returns>
    public StringBuilderWrapper AppendLine() => AppendLine("");

    /// <summary>
    /// Appends text ended by default line terminator to the source code.
    /// </summary>
    /// <param name="text">Text to be appended into the source code.</param>
    /// <returns>Self wrapper.</returns>
    /// <remarks>If added text is not empty and current position is on new line the proper indentation level is appended first.</remarks>
    public StringBuilderWrapper AppendLine(string text)
    {
        if (text.Length == 0)
        {
            // Do not append indentation and append only new line terminator.
            sb.AppendLine();
        }
        else
        {
            EnsureIndentation();
            sb.AppendLine(text);
        }

        _newLine = true;
        return this;
    }

    /// <summary>
    /// Appends full namespace identifier including <c>global::</c> prefix to the source code.
    /// </summary>
    /// <param name="namespaceSymbol">Namespace to append.</param>
    /// <returns>Self wrapper.</returns>
    /// <remarks>If current position is on new line the proper indentation level is appended first.</remarks>
    public StringBuilderWrapper AppendNamespace(INamespaceSymbol namespaceSymbol)
        => AppendNamespace(namespaceSymbol, true);

    /// <summary>
    /// Appends full namespace identifier but <b>without</b> <c>global::</c> prefix to the source code.
    /// </summary>
    /// <param name="namespaceSymbol">Namespace to append.</param>
    /// <returns>Self wrapper.</returns>
    /// <remarks>If current position is on new line the proper indentation level is appended first.</remarks>
    public StringBuilderWrapper AppendNamespaceWithoutGlobal(INamespaceSymbol namespaceSymbol)
        => AppendNamespace(namespaceSymbol, false);

    /// <summary>
    /// Appends full namespace identifier to the source code.
    /// </summary>
    /// <param name="namespaceSymbol">Namespace to append.</param>
    /// <param name="includeGlobal">Whether <c>global::</c> prefix should be also appended.</param>
    /// <returns>Self wrapper.</returns>
    private StringBuilderWrapper AppendNamespace(INamespaceSymbol? namespaceSymbol, bool includeGlobal)
    {
        if (namespaceSymbol == null) return this;
        if (includeGlobal) Append("global::");
        return AppendNamespaceWithoutGlobal(namespaceSymbol, false);
    }

    /// <summary>
    /// Appends full namespace identifier but <b>without</b> <c>global::</c> prefix to the source code.
    /// </summary>
    /// <param name="namespaceSymbol">Namespace to append.</param>
    /// <param name="parent">Whether dot separator should be also appended at the end. This parameter is used for recursion.</param>
    /// <returns>Self wrapper.</returns>
    private StringBuilderWrapper AppendNamespaceWithoutGlobal(INamespaceSymbol namespaceSymbol, bool parent)
    {
        if (namespaceSymbol.IsGlobal()) return this;
        if (namespaceSymbol.ContainingNamespace is { } parentNamespaceSymbol)
        {
            AppendNamespaceWithoutGlobal(parentNamespaceSymbol, true);
        }

        Append(namespaceSymbol.Name);
        if (parent) sb.Append('.');
        return this;
    }

    /// <summary>
    /// Appends full type identifier including namespace with <c>global::</c> prefix and property name to the source code using syntax for documentation.
    /// </summary>
    /// <param name="typeSymbol">Type which property to append as documentation reference.</param>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>Self wrapper.</returns>
    public StringBuilderWrapper AppendDocSymbolReference(ITypeSymbol typeSymbol, string propertyName) => Append("cref=\"").AppendWithNamespace(typeSymbol, forDoc: true).Append('.').Append(propertyName).Append("\"");

    /// <summary>
    /// Appends full type identifier including namespace with <c>global::</c> prefix into source code or documentation.
    /// Optionally using keyword if eligible (<paramref name="allowSpecialCase"/>) or as part of documentation (<paramref name="forDoc"/>).
    /// </summary>
    /// <param name="typeSymbol">Type symbol to append.</param>
    /// <param name="allowSpecialCase">If allowed to use C# keyword as type identifier.<para>Defaults to <see langword="true"/>.</para></param>
    /// <param name="forDoc">Whether to use documentation syntax.<para>Defaults to <see langword="false"/>.</para></param>
    /// <returns>Self wrapper.</returns>
    public StringBuilderWrapper AppendWithNamespace(ITypeSymbol typeSymbol, bool allowSpecialCase = true, bool forDoc = false)
    {
        if (typeSymbol is ITypeParameterSymbol typeParameterSymbol)
        {
            return Append(typeParameterSymbol.Name);
        }

        var typeArguments = GetTypeArguments(typeSymbol);
        if (typeArguments is { Length: 1 })
        {
            if (typeSymbol.SpecialType == SpecialType.System_Nullable_T || typeSymbol.OriginalDefinition is { SpecialType: SpecialType.System_Nullable_T })
            {
                return AppendWithNamespace(typeArguments[0], allowSpecialCase, forDoc).Append('?');
            }
            if (typeSymbol.SpecialType == SpecialType.System_Array)
            {
                // TODO multi dimensional array?
                AppendWithNamespace(typeArguments[0], allowSpecialCase, forDoc).Append("[]");
                if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated) sb.Append('?');
                return this;
            }
        }

        if (allowSpecialCase)
        {
            // Keywords used by C# for defined types.
            var keyword = (typeSymbol.SpecialType, typeSymbol.NullableAnnotation == NullableAnnotation.Annotated) switch
            {
                (SpecialType.System_Boolean, _)    => "bool",
                (SpecialType.System_Object, true)  => "object?",
                (SpecialType.System_Object, false) => "object",
                (SpecialType.System_Void, _)       => "void",
                (SpecialType.System_Char, _)       => "char",
                (SpecialType.System_SByte, _)      => "sbyte",
                (SpecialType.System_Byte, _)       => "byte",
                (SpecialType.System_Int16, _)      => "short",
                (SpecialType.System_UInt16, _)     => "ushort",
                (SpecialType.System_Int32, _)      => "int",
                (SpecialType.System_UInt32, _)     => "uint",
                (SpecialType.System_Int64, _)      => "long",
                (SpecialType.System_UInt64, _)     => "ulong",
                (SpecialType.System_Decimal, _)    => "decimal",
                (SpecialType.System_Single, _)     => "float",
                (SpecialType.System_Double, _)     => "double",
                (SpecialType.System_String, true)  => "string?",
                (SpecialType.System_String, false) => "string",
                _                                  => null
            };
            if (keyword != null) return Append(keyword);
        }

        if (typeSymbol.ContainingNamespace is { } namespaceSymbol)
        {
            AppendNamespace(namespaceSymbol);

            // Append dot only if containing namespace is not global.
            if (namespaceSymbol.IsNotGlobal())
            {
                sb.Append('.');
            }
        }
        sb.Append(typeSymbol.Name);
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated) sb.Append('?');

        if (!typeArguments.IsEmpty)
        {
            sb.Append(forDoc ? '{' : '<');
            var anyBefore = false;
            foreach (var typeArgument in typeArguments)
            {
                if (anyBefore)
                {
                    sb.Append(", ");
                }
                else
                {
                    anyBefore = true;
                }

                AppendWithNamespace(typeArgument, forDoc: forDoc);
            }
            sb.Append(forDoc ? '}' : '>');
        }

        return this;

        static ImmutableArray<ITypeSymbol> GetTypeArguments(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is INamedTypeSymbol { TypeArguments: { Length: > 0 } typeArguments }) return typeArguments;
            return ImmutableArray<ITypeSymbol>.Empty;
        }
    }

    /// <summary>
    /// <inheritdoc cref="IndentationContext(StringBuilderWrapper)"/>
    /// </summary>
    /// <returns><inheritdoc cref="IndentationContext" path="/summary"/></returns>
    /// <remarks>Can be used with <c>using</c> declaration.</remarks>
    public IndentationContext CreateNewIndentationContext() => new(this);

    /// <summary>
    /// Appends indentation after new line if not already appended.
    /// </summary>
    private void EnsureIndentation()
    {
        if (!_newLine) return;
        if (Indentation > 0)
        {
            sb.Append('\t', Indentation);
        }

        _newLine = false;
    }

    /// <summary>
    /// Returns content of the inner <see cref="StringBuilder"/>.
    /// </summary>
    /// <returns>Content of the inner <see cref="StringBuilder"/>.</returns>
    public override string ToString() => sb.ToString();

    /// <summary>
    /// Ref struct disposable handle to end indentation context.
    /// </summary>
    public readonly ref struct IndentationContext
    {
        private readonly StringBuilderWrapper _sbWrapper;

        /// <summary>
        /// Creates new indentation context increasing indentation level by one.
        /// </summary>
        /// <param name="sbWrapper">Wrapper used by handle.</param>
        public IndentationContext(StringBuilderWrapper sbWrapper)
        {
            _sbWrapper = sbWrapper;
            _sbWrapper.Indentation++;
        }

        /// <summary>
        /// Decrease the indentation level.
        /// </summary>
        /// <remarks>No check is performed. It is caller responsibility to call this method exactly once.</remarks>
        public void Dispose()
        {
            _sbWrapper.Indentation--;
        }
    }
}