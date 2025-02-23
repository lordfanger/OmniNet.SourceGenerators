namespace OmniNet.SourceGenerators.Core;

/// <summary>
/// Provides incremental symbol values based on the specified syntax nodes and symbols marked by source generated attribute.
/// </summary>
/// <typeparam name="TSyntaxNode">The type of the syntax node to transform.</typeparam>
/// <typeparam name="TSymbol">The type of the symbols.</typeparam>
public readonly ref struct IncrementalSymbolValuesProvider<TSyntaxNode, TSymbol>(SyntaxValueProvider syntaxValueProvider, IGeneratorAttribute generatorAttribute, Func<TSyntaxNode, CancellationToken, bool>? predicate)
    where TSyntaxNode : SyntaxNode
    where TSymbol : ISymbol
{
    /// <summary>
    /// Transforms the symbol values using the provided transform function.
    /// </summary>
    /// <typeparam name="T">The type of the transformed values.</typeparam>
    /// <param name="transform">The transform function.</param>
    /// <returns>An instance of <see cref="IncrementalValuesProvider{T}"/>.</returns>
    public IncrementalValuesProvider<T> Transform<T>(
        Func<TSymbol, ImmutableArray<AttributeData>, CancellationToken, T> transform)
    {
        var composedPredicate = ComposedPredicate();
        return syntaxValueProvider.ForAttributeWithMetadataName(generatorAttribute.TypeFullName, composedPredicate, ComposedTransform);

        T ComposedTransform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            var symbol = context.TargetSymbol;
            var attributes = context.Attributes;
            var result = transform((TSymbol)symbol, attributes, cancellationToken);
            return result;
        }
    }

    /// <summary>
    /// Composes the predicate function by wrapping the provided predicate and ensure that node is of type <typeparamref name="TSyntaxNode" />.
    /// </summary>
    /// <returns>The composed predicate function.</returns>
    private Func<SyntaxNode, CancellationToken, bool> ComposedPredicate()
    {
        var wrappedPredicate = predicate;
        return (node, cancellationToken) =>
        {
            if (node is not TSyntaxNode symbol) return false;
            return wrappedPredicate?.Invoke(symbol, cancellationToken) ?? true;
        };
    }
}
