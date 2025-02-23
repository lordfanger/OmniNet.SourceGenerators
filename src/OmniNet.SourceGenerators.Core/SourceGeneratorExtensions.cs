using System.Diagnostics.CodeAnalysis;

namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Extension methods for source generators.
/// </summary>
public static class SourceGeneratorExtensions
{
    /// <summary>
    /// Adds attribute source code into the source generating context.
    /// </summary>
    /// <param name="context">Source generating context.</param>
    /// <param name="attribute">Handle to generated attribute.</param>
    /// <returns>Returns the same source generating context passed as argument <paramref name="context"/>.</returns>
    public static IncrementalGeneratorPostInitializationContext AddSource(this IncrementalGeneratorPostInitializationContext context, IGeneratorAttribute attribute)
    {
        context.AddSource(attribute.GeneratedFilePath, SourceText.From(attribute.SourceCode, Encoding.UTF8));
        return context;
    }

    /// <summary>
    /// Creates a <see cref="IncrementalSymbolValuesProvider{TSyntaxNode, TSymbol}"/> for all type symbols that has generated <paramref name="attribute"/>.
    /// </summary>
    /// <param name="syntaxValueProvider">Source generating context.</param>
    /// <param name="attribute">Handle to generated attribute.</param>
    /// <returns>Returns <see cref="IncrementalSymbolValuesProvider{TSyntaxNode, TSymbol}"/> that can be used to build transformation pipeline.</returns>
    public static IncrementalSymbolValuesProvider<ClassDeclarationSyntax, ITypeSymbol> ForTypeWithAttribute(this SyntaxValueProvider syntaxValueProvider, IGeneratorAttribute attribute)
    {
        var provider = new IncrementalSymbolValuesProvider<ClassDeclarationSyntax, ITypeSymbol>(syntaxValueProvider, attribute, null);
        return provider;
    }

    /// <summary>
    /// Extension method returning whether namespace symbol is representing global namespace.
    /// </summary>
    /// <param name="namespaceSymbol">Symbol representing namespace.</param>
    /// <returns>Returns <see langword="true"/> if <paramref name="namespaceSymbol"/> is global namespace or <see langword="null"/>.</returns>
    public static bool IsGlobal(this INamespaceSymbol? namespaceSymbol) => namespaceSymbol is null or { IsGlobalNamespace: true };

    /// <summary>
    /// Extension method returning whether namespace symbol is representing normal namespace.
    /// </summary>
    /// <param name="namespaceSymbol">Symbol representing namespace.</param>
    /// <returns>Returns <see langword="false"/> if <paramref name="namespaceSymbol"/> is global namespace or <see langword="null"/>.</returns>
    public static bool IsNotGlobal([NotNullWhen(true)] this INamespaceSymbol? namespaceSymbol) => !namespaceSymbol.IsGlobal();
}