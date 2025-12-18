using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OmniNet.SourceGenerators.Core;
using OmniNet.SourceGenerators.Core.Tests.Generator.Template.Attributes;

namespace OmniNet.Web.Builder.SourceGenerators;

[Generator]
public class SourceGenerator : IIncrementalGenerator
{
    private static readonly ConcurrentDictionary<string, Lazy<Assembly>> _assemblyCache = new();

    private static Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name);
        var assemblyName = name.Name;

        if (assemblyName == null)
            return null;

        // Rychlý check - máme vůbec tento resource?
        var resourceName = $"Dependencies\\{assemblyName}.dll";
        if (Assembly.GetExecutingAssembly().GetManifestResourceInfo(resourceName) == null)
            return null;

        // Cache s Lazy zajistí jediné načtení
        var lazy = _assemblyCache.GetOrAdd(assemblyName, key => new Lazy<Assembly>(() =>
        {
            using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Dependencies\\{key}.dll")
                ?? throw new InvalidOperationException($"Resource Dependencies\\{key}.dll not found");

            using var memoryStream = new MemoryStream();
            resourceStream.CopyTo(memoryStream);

#pragma warning disable RS1035 // Assembly.Load is required for embedded dependencies in source generators
            return Assembly.Load(memoryStream.ToArray());
#pragma warning restore RS1035
        }));

        return lazy.Value;
    }

    static SourceGenerator()
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
    }

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var innerGenerator = new InnerGenerator();
        innerGenerator.Initialize(context);
    }
}

file readonly struct InnerGenerator
{
    private static readonly IGeneratorAttribute _testsGenerateAttribute = SourceGeneratorProvider.GetAttribute<TestGenerateAttribute>();

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(PostInitialization);
        var omniStoreItems = context.SyntaxProvider.ForAttributeWithMetadataName(_testsGenerateAttribute.TypeFullName, IsClassAttribute, GetTestItems);
        context.RegisterSourceOutput(omniStoreItems.Collect(), GenerateTestItems);
    }

    private static void PostInitialization(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource(_testsGenerateAttribute);
    }

    private static TestItemData GetTestItems(GeneratorAttributeSyntaxContext context, CancellationToken _)
    {
        var classNode = (ITypeSymbol)context.TargetSymbol;
        return new TestItemData(context.SemanticModel, classNode);
    }

    private static void GenerateTestItems(SourceProductionContext context, ImmutableArray<TestItemData> testItems)
    {
        if (testItems.IsEmpty) return;

        foreach (var (semanticModel, testItem) in testItems)
        {
            var compilation = semanticModel.Compilation;
            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);
            var voidType = compilation.GetSpecialType(SpecialType.System_Void);

            var sb = new SourceBuilder();
            {
                using var type = sb.AppendFileNamespace(testItem.ContainingNamespace)
                    .BuildClass(testItem.Name)
                    .WithAccessibility(testItem.DeclaredAccessibility)
                    .WithPartial()
                    .Append()
                    .AppendOpenType();

                type.BuildProperty(stringType, "Id")
                    .WithAccessibility(Accessibility.Public)
                    .WithRequired()
                    .WithImplicitGetter()
                    .WithImplicitSetter(initOnly: true)
                    .Append();

                type.BuildProperty(stringType, "StaticName")
                    .WithAccessibility(Accessibility.Public)
                    .WithStatic()
                    .WithExplicitGetterExpression("\"StaticNameValue\"")
                    .Append();

                // Test method: simple void method
                type.BuildMethod("DoSomething")
                    .WithAccessibility(Accessibility.Public)
                    .OpenParameters()
                    .OpenBody()
                        .AppendLine("// Do something")
                    .Dispose();

                // Test method: method with return value
                type.BuildMethod("GetName", stringType)
                    .WithAccessibility(Accessibility.Public)
                    .OpenParameters()
                    .OpenBody()
                        .AppendReturn("Id")
                    .Dispose();

                // Test method: method with parameters
                type.BuildMethod("SetValue")
                    .WithAccessibility(Accessibility.Public)
                    .OpenParameters()
                        .AddParameter(stringType, "key")
                        .AddParameter(intType, "value", "0")
                    .OpenBody()
                        .AppendLine("// Set value logic")
                    .Dispose();

                // Test method: expression-bodied method
                type.BuildMethod("ToString", stringType)
                    .WithAccessibility(Accessibility.Public)
                    .WithOverride()
                    .OpenParameters()
                    .AppendExpression("$\"TestItem: {Id}\"");

                // Test method: static method
                type.BuildMethod("CreateDefault", testItem)
                    .WithAccessibility(Accessibility.Public)
                    .WithStatic()
                    .OpenParameters()
                    .OpenBody()
                        .AppendReturn($"new {testItem.Name} {{ Id = \"default\" }}")
                    .Dispose();

                // Test method: virtual method
                type.BuildMethod("Calculate", intType)
                    .WithAccessibility(Accessibility.Public)
                    .WithVirtual()
                    .OpenParameters()
                        .AddParameter(intType, "x")
                        .AddParameter(intType, "y")
                    .OpenBody()
                        .AppendReturn("x + y")
                    .Dispose();
            }

            sb.AddToContext(context, testItem);
        }
    }

    private static bool IsClassAttribute(SyntaxNode node, CancellationToken token) => node is ClassDeclarationSyntax;

    private record struct TestItemData(SemanticModel ContextSemanticModel, ITypeSymbol Class);
}