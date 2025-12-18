namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Builder to generate method body.
/// </summary>
public readonly ref struct MethodBodyBuilder
{
    private readonly StringBuilderWrapper _sbWrapper;
    private readonly StringBuilderWrapper.IndentationContext _indentationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="MethodBodyBuilder"/> struct.
    /// </summary>
    /// <param name="sbWrapper">Wrapped string builder used as store.</param>
    internal MethodBodyBuilder(StringBuilderWrapper sbWrapper)
    {
        _sbWrapper = sbWrapper;
        _sbWrapper.AppendLine("{");
        _indentationContext = _sbWrapper.CreateNewIndentationContext();
    }

    /// <summary>
    /// Appends a line of code to the method body.
    /// </summary>
    /// <param name="code">Code line to append.</param>
    /// <returns>Self builder.</returns>
    public MethodBodyBuilder AppendLine(string code)
    {
        _sbWrapper.AppendLine(code);
        return this;
    }

    /// <summary>
    /// Appends an empty line to the method body.
    /// </summary>
    /// <returns>Self builder.</returns>
    public MethodBodyBuilder AppendLine()
    {
        _sbWrapper.AppendLine();
        return this;
    }

    /// <summary>
    /// Appends a return statement to the method body.
    /// </summary>
    /// <param name="expression">Expression to return.</param>
    /// <returns>Self builder.</returns>
    public MethodBodyBuilder AppendReturn(string expression)
    {
        _sbWrapper.Append("return ").Append(expression).AppendLine(";");
        return this;
    }

    /// <summary>
    /// Appends a throw statement to the method body.
    /// </summary>
    /// <param name="exceptionExpression">Exception expression to throw.</param>
    /// <returns>Self builder.</returns>
    public MethodBodyBuilder AppendThrow(string exceptionExpression)
    {
        _sbWrapper.Append("throw ").Append(exceptionExpression).AppendLine(";");
        return this;
    }

    /// <summary>
    /// Closes method body.
    /// </summary>
    /// <remarks>No method should be called afterward.</remarks>
    public void Dispose()
    {
        _indentationContext.Dispose();
        _sbWrapper.AppendLine("}");
    }
}
