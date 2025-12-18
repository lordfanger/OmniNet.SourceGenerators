namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Builder to generate method parameters.
/// </summary>
public ref struct MethodParametersBuilder
{
    private readonly StringBuilderWrapper _sbWrapper;
    private readonly MethodBuilder _parent;
    private bool _anyParameterWritten;

    /// <summary>
    /// Initializes a new instance of the <see cref="MethodParametersBuilder"/> struct.
    /// </summary>
    /// <param name="sbWrapper">Wrapped string builder used as store.</param>
    /// <param name="parent">Parent method builder.</param>
    internal MethodParametersBuilder(StringBuilderWrapper sbWrapper, MethodBuilder parent)
    {
        _sbWrapper = sbWrapper;
        _parent = parent;
    }

    /// <summary>
    /// Adds a parameter to the method.
    /// </summary>
    /// <param name="type">Type of parameter.</param>
    /// <param name="name">Name of parameter.</param>
    /// <returns>Self builder.</returns>
    public MethodParametersBuilder AddParameter(ITypeSymbol type, string name)
    {
        AppendParameterSeparator();
        _sbWrapper.AppendWithNamespace(type).Append(' ').Append(name);
        return this;
    }

    /// <summary>
    /// Adds a parameter with default value to the method.
    /// </summary>
    /// <param name="type">Type of parameter.</param>
    /// <param name="name">Name of parameter.</param>
    /// <param name="defaultValue">Default value expression.</param>
    /// <returns>Self builder.</returns>
    public MethodParametersBuilder AddParameter(ITypeSymbol type, string name, string defaultValue)
    {
        AppendParameterSeparator();
        _sbWrapper.AppendWithNamespace(type).Append(' ').Append(name).Append(" = ").Append(defaultValue);
        return this;
    }

    /// <summary>
    /// Adds a ref parameter to the method.
    /// </summary>
    /// <param name="type">Type of parameter.</param>
    /// <param name="name">Name of parameter.</param>
    /// <returns>Self builder.</returns>
    public MethodParametersBuilder AddRefParameter(ITypeSymbol type, string name)
    {
        AppendParameterSeparator();
        _sbWrapper.Append("ref ").AppendWithNamespace(type).Append(' ').Append(name);
        return this;
    }

    /// <summary>
    /// Adds an out parameter to the method.
    /// </summary>
    /// <param name="type">Type of parameter.</param>
    /// <param name="name">Name of parameter.</param>
    /// <returns>Self builder.</returns>
    public MethodParametersBuilder AddOutParameter(ITypeSymbol type, string name)
    {
        AppendParameterSeparator();
        _sbWrapper.Append("out ").AppendWithNamespace(type).Append(' ').Append(name);
        return this;
    }

    /// <summary>
    /// Adds an in parameter to the method.
    /// </summary>
    /// <param name="type">Type of parameter.</param>
    /// <param name="name">Name of parameter.</param>
    /// <returns>Self builder.</returns>
    public MethodParametersBuilder AddInParameter(ITypeSymbol type, string name)
    {
        AppendParameterSeparator();
        _sbWrapper.Append("in ").AppendWithNamespace(type).Append(' ').Append(name);
        return this;
    }

    /// <summary>
    /// Adds a params parameter to the method.
    /// </summary>
    /// <param name="elementType">Element type of params array.</param>
    /// <param name="name">Name of parameter.</param>
    /// <returns>Self builder.</returns>
    public MethodParametersBuilder AddParamsParameter(ITypeSymbol elementType, string name)
    {
        AppendParameterSeparator();
        _sbWrapper.Append("params ").AppendWithNamespace(elementType).Append("[] ").Append(name);
        return this;
    }

    /// <summary>
    /// Adds parameters from existing method symbol.
    /// </summary>
    /// <param name="method">Method to copy parameters from.</param>
    /// <returns>Self builder.</returns>
    public MethodParametersBuilder AddParametersFrom(IMethodSymbol method)
    {
        foreach (var parameter in method.Parameters)
        {
            AppendParameterSeparator();

            // Handle ref kind
            switch (parameter.RefKind)
            {
                case RefKind.Ref:
                    _sbWrapper.Append("ref ");
                    break;
                case RefKind.Out:
                    _sbWrapper.Append("out ");
                    break;
                case RefKind.In:
                    _sbWrapper.Append("in ");
                    break;
            }

            // Handle params
            if (parameter.IsParams)
            {
                _sbWrapper.Append("params ");
            }

            // Type and name
            _sbWrapper.AppendWithNamespace(parameter.Type).Append(' ').Append(parameter.Name);

            // Default value
            if (parameter.HasExplicitDefaultValue)
            {
                _sbWrapper.Append(" = ");
                if (parameter.ExplicitDefaultValue == null)
                {
                    _sbWrapper.Append("null");
                }
                else
                {
                    _sbWrapper.Append(parameter.ExplicitDefaultValue.ToString());
                }
            }
        }

        return this;
    }

    /// <summary>
    /// Opens body builder for method with body.
    /// </summary>
    /// <returns>Body builder.</returns>
    public MethodBodyBuilder OpenBody()
    {
        _sbWrapper.AppendLine(")");
        return new MethodBodyBuilder(_sbWrapper);
    }

    /// <summary>
    /// Appends abstract/interface method declaration (no body).
    /// </summary>
    public void AppendAbstract()
    {
        _sbWrapper.AppendLine(");");
    }

    /// <summary>
    /// Appends expression-bodied method.
    /// </summary>
    /// <param name="expression">Expression for method body.</param>
    public void AppendExpression(string expression)
    {
        _sbWrapper.Append(") => ").Append(expression).AppendLine(";");
    }

    /// <summary>
    /// Appends parameter separator if needed.
    /// </summary>
    private void AppendParameterSeparator()
    {
        if (_anyParameterWritten)
        {
            _sbWrapper.Append(", ");
        }
        else
        {
            _anyParameterWritten = true;
        }
    }
}
