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
            
            // Check if the class implements IMyInterface or IMyGenericInterface
            var myInterface = compilation.GetTypeByMetadataName($"{testItem.ContainingNamespace}.IMyInterface");
            var genericInterface = compilation.GetTypeByMetadataName($"{testItem.ContainingNamespace}.IMyGenericInterface`1");
            
            var implementsMyInterface = myInterface != null && testItem.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, myInterface));
            var implementsGenericInterface = genericInterface != null && testItem.AllInterfaces.Any(i => 
                i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, genericInterface));
            
            // Generate the main test class
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

                // Test property with initializer: simple value type
                type.BuildProperty(intType, "DefaultCounter")
                    .WithAccessibility(Accessibility.Public)
                    .WithImplicitGetter()
                    .WithImplicitSetter()
                    .WithInitializer("10")
                    .Append();

                // Test property with initializer: string
                type.BuildProperty(stringType, "DefaultName")
                    .WithAccessibility(Accessibility.Public)
                    .WithImplicitGetter()
                    .WithInitializer("\"DefaultValue\"")
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

                // If the class implements IMyInterface, generate the interface members
                if (implementsMyInterface && myInterface != null)
                {
                    // Implement IMyInterface.Name property
                    type.BuildProperty(stringType, "Name")
                        .WithAccessibility(Accessibility.Public)
                        .WithImplicitGetter()
                        .WithImplicitSetter()
                        .Append();

                    // Implement IMyInterface.GetValue() method
                    type.BuildMethod("GetValue", intType)
                        .WithAccessibility(Accessibility.Public)
                        .OpenParameters()
                        .OpenBody()
                            .AppendReturn("42")
                        .Dispose();
                }

                // If the class implements IMyGenericInterface<T>, generate the static interface members
                if (implementsGenericInterface)
                {
                    // Implement static abstract property
                    type.BuildProperty(stringType, "StaticProperty")
                        .WithAccessibility(Accessibility.Public)
                        .WithStatic()
                        .WithExplicitGetterExpression("\"StaticValue\"")
                        .Append();

                    // Implement static abstract method
                    type.BuildMethod("Create", testItem)
                        .WithAccessibility(Accessibility.Public)
                        .WithStatic()
                        .OpenParameters()
                        .OpenBody()
                            .AppendReturn($"new {testItem.Name} {{ Id = \"created\" }}")
                        .Dispose();
                }
            }

            // Generate a separate class to test explicit interface implementation
            GenerateExplicitInterfaceTestClass(context, testItem, compilation, stringType, intType);

            sb.AddToContext(context, testItem);
        }
    }

    private static bool IsClassAttribute(SyntaxNode node, CancellationToken token) => node is ClassDeclarationSyntax;

    private static void GenerateExplicitInterfaceTestClass(SourceProductionContext context, ITypeSymbol testItem, Compilation compilation, ITypeSymbol stringType, ITypeSymbol intType)
    {
        // Test 1: Regular explicit interface implementation with IMyInterface
        var myInterface = compilation.GetTypeByMetadataName($"{testItem.ContainingNamespace}.IMyInterface");
        if (myInterface != null)
        {
            var sb1 = new SourceBuilder();
            {
                using var type = sb1.AppendFileNamespace(testItem.ContainingNamespace)
                    .BuildClass($"{testItem.Name}WithExplicitInterface")
                    .WithAccessibility(Accessibility.Public)
                    .WithPartial()
                    .Append()
                    .AppendInheritance(myInterface)
                    .AppendOpenType();

                // Regular public property
                type.BuildProperty(stringType, "PublicName")
                    .WithAccessibility(Accessibility.Public)
                    .WithImplicitGetter()
                    .WithImplicitSetter()
                    .WithInitializer("\"Public\"")
                    .Append();

                // Explicit interface implementation property
                type.BuildProperty(stringType, "Name")
                    .WithExplicitInterfaceImplementation(myInterface)
                    .WithImplicitGetter()
                    .WithImplicitSetter()
                    .Append();

                // Explicit interface implementation method
                type.BuildMethod("GetValue", intType)
                    .WithExplicitInterfaceImplementation(myInterface)
                    .OpenParameters()
                    .OpenBody()
                        .AppendReturn("42")
                    .Dispose();
            }

            sb1.AddToContext(context, testItem, "WithExplicitInterface");
        }

        // Test 2: Generic interface with static members
        var genericInterface = compilation.GetTypeByMetadataName($"{testItem.ContainingNamespace}.IMyGenericInterface`1");
        if (genericInterface != null)
        {
            var sb2 = new SourceBuilder();
            {
                var className = $"{testItem.Name}WithStaticInterface";
                
                // Create TypeReference for the class we're generating
                TypeReference classTypeRef = className;
                
                // Create TypeReference for the constructed generic interface using string
                TypeReference constructedInterfaceRef = $"IMyGenericInterface<{className}>";
                
                using var type = sb2.AppendFileNamespace(testItem.ContainingNamespace)
                    .BuildClass(className)
                    .WithAccessibility(Accessibility.Public)
                    .WithPartial()
                    .Append()
                    .AppendInheritance(testItem.ContainingNamespace, $"IMyGenericInterface<{className}>")
                    .AppendOpenType();

                // Regular public property
                type.BuildProperty(stringType, "InstanceValue")
                    .WithAccessibility(Accessibility.Public)
                    .WithImplicitGetter()
                    .WithImplicitSetter()
                    .WithInitializer("\"Instance\"")
                    .Append();

                // Explicit static interface property implementation
                type.BuildProperty(stringType, "StaticProperty")
                    .WithExplicitInterfaceImplementation(constructedInterfaceRef)
                    .WithStatic()
                    .WithExplicitGetterExpression("\"StaticValue\"")
                    .Append();

                // Explicit static interface method implementation
                type.BuildMethod("Create", classTypeRef)
                    .WithExplicitInterfaceImplementation(constructedInterfaceRef)
                    .WithStatic()
                    .OpenParameters()
                    .OpenBody()
                        .AppendReturn($"new {className}()")
                    .Dispose();
            }

            sb2.AddToContext(context, testItem, "WithStaticInterface");
        }
    }

    private record struct TestItemData(SemanticModel ContextSemanticModel, ITypeSymbol Class);
}