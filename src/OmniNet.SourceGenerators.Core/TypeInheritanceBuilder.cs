namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Builder to generate type's inheritance.
/// </summary>
public ref struct TypeInheritanceBuilder
{
    private bool _inheritanceWritten;
    private readonly StringBuilderWrapper _sbWrapper;

    /// <summary><inheritdoc cref="TypeInheritanceBuilder" path="/summary"/></summary>
    /// <param name="sbWrapper"><inheritdoc cref="StringBuilderWrapper" path="/summary"/></param>
    internal TypeInheritanceBuilder(StringBuilderWrapper sbWrapper)
    {
        _sbWrapper = sbWrapper;
    }

    /// <summary>
    /// Appends type symbols of extended interface or base class to source code.
    /// </summary>
    /// <param name="interfaces">Collection of type symbols of extended interface or base class.</param>
    /// <returns>Self builder.</returns>
    /// <remarks>If type symbol is not first appended by any method or type itself is interface, it must be type symbol of interface.</remarks>
    public TypeInheritanceBuilder AppendInheritance(ImmutableArray<ITypeSymbol> interfaces)
    {
        foreach (var interfaceSymbol in interfaces)
        {
            AppendInheritance(interfaceSymbol);
        }
        return this;
    }

    /// <summary>
    /// Appends type symbols of extended interface or base class to source code returned from source items by function that.
    /// </summary>
    /// <typeparam name="T">Type of source item.</typeparam>
    /// <param name="items">Collection of source items.</param>
    /// <param name="interfaceGetter"><inheritdoc cref="InterfaceGetter{T}" path="/summary"/></param>
    /// <returns>Self builder.</returns>
    /// <remarks>If returned type symbol is not first appended by any method or type itself is interface, it must be type symbol of interface.</remarks>
    public TypeInheritanceBuilder AppendInheritance<T>(ImmutableArray<T> items, InterfaceGetter<T> interfaceGetter)
    {
        foreach (var item in items)
        {
            var interfaceSymbol = interfaceGetter(item);
            if (interfaceSymbol == null) continue;

            AppendInheritance(interfaceSymbol);
        }

        return this;
    }

    /// <summary>
    /// Appends extended interface or base class by name and namespace to source code without any checks.
    /// </summary>
    /// <param name="namespaceSymbol">Namespace of extended interface or base class.</param>
    /// <param name="name">Name of extended interface or base class</param>
    /// <returns>Self builder.</returns>
    /// <remarks>No check for whether the type exists or must be an interface is performed.</remarks>
    public TypeInheritanceBuilder AppendInheritance(INamespaceSymbol? namespaceSymbol, string name)
    {
        WriteBeforeInheritance();

        if (namespaceSymbol.IsNotGlobal())
        {
            _sbWrapper.AppendNamespace(namespaceSymbol).Append('.');
        }

        _sbWrapper.Append(name);
        return this;
    }

    /// <summary>
    /// Appends extended interface or base class to source code.
    /// </summary>
    /// <param name="typeSymbol">Type symbol of extended interface or base class.</param>
    /// <returns>Self builder.</returns>
    /// <remarks>If type symbol is not first appended by any method or type itself is interface, it must be an interface.</remarks>
    public TypeInheritanceBuilder AppendInheritance(ITypeSymbol typeSymbol)
    {
        WriteBeforeInheritance();

        // TODO emit diagnostic if type symbol must be an interface, but is not or is sealed/struct/static - not needed, but could help
        _sbWrapper.AppendWithNamespace(typeSymbol);
        return this;
    }

    /// <summary>
    /// Appends type opening to source code.
    /// </summary>
    /// <returns>Type builder to build type's members.</returns>
    public TypeBuilder AppendOpenType()
    {
        _sbWrapper.AppendLine();
        _sbWrapper.AppendLine("{");
        return new TypeBuilder(_sbWrapper);
    }

    /// <summary>
    /// Appends delimiter (":" or ",") between type symbols based on the current context.
    /// </summary>
    private void WriteBeforeInheritance()
    {
        if (_inheritanceWritten)
        {
            _sbWrapper.Append(", ");
        }
        else
        {
            _sbWrapper.Append(" : ");
            _inheritanceWritten = true;
        }
    }
}