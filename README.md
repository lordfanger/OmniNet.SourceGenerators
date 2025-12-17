# OmniNet.SourceGenerators.Core

A core library providing helpers and builders for creating C# source generators with ease.

## Installation

```shell
dotnet add package OmniNet.SourceGenerators.Core
```

## Features

- **SourceBuilder** - Fluent API for generating C# source code files
- **TypeBuilder** - Builder for generating type members (properties, etc.)
- **PropertyBuilder** - Builder for generating properties with modifiers, accessors, and attributes
- **OpeningTypeBuilder** - Builder for generating type declarations with modifiers
- **IncrementalSymbolValuesProvider** - Helper for creating incremental source generators
- **SourceGeneratorProvider** - Provider for accessing generated attributes stored as assembly resources

## Quick Start

### 1. Define your generator attribute

Create an attribute class in an `Attributes` folder in your generator project:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class MyGenerateAttribute : Attribute { }
```

### 2. Create your incremental generator

```csharp
using OmniNet.SourceGenerators.Core;

[Generator]
public class MyGenerator : IIncrementalGenerator
{
    private static readonly IGeneratorAttribute _attribute = 
        SourceGeneratorProvider.GetAttribute<MyGenerateAttribute>();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the attribute source code
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(_attribute));

        // Find all classes with the attribute
        var items = context.SyntaxProvider
            .ForTypeWithAttribute(_attribute)
            .Transform((symbol, attributes, ct) => symbol);

        context.RegisterSourceOutput(items, GenerateSource);
    }

    private static void GenerateSource(SourceProductionContext context, ITypeSymbol typeSymbol)
    {
        var sb = new SourceBuilder();
        
        using var type = sb
            .AppendFileNamespace(typeSymbol.ContainingNamespace)
            .BuildClass(typeSymbol.Name)
            .WithAccessibility(typeSymbol.DeclaredAccessibility)
            .WithPartial()
            .Append()
            .AppendOpenType();

        // Add properties using PropertyBuilder
        type.BuildProperty(compilation.GetSpecialType(SpecialType.System_String), "GeneratedProperty")
            .WithAccessibility(Accessibility.Public)
            .WithImplicitGetter()
            .WithImplicitSetter()
            .Append();

        sb.AddToContext(context, typeSymbol);
    }
}
```

## API Reference

### SourceBuilder

Entry point for generating source code files.

```csharp
var sb = new SourceBuilder();

// Add file-scoped namespace
sb.AppendFileNamespace(namespaceSymbol);

// Start building a class or interface
sb.BuildClass("MyClass");
sb.BuildInterface("IMyInterface");

// Add generated source to compilation
sb.AddToContext(context, typeSymbol);
sb.AddToContext(context, namespaceSymbol, "FileName", "suffix");
```

### OpeningTypeBuilder

Configures type declaration with modifiers.

```csharp
sb.BuildClass("MyClass")
    .WithAccessibility(Accessibility.Public)
    .WithPartial()
    .Append()           // Returns TypeInheritanceBuilder
    .AppendOpenType();  // Returns TypeBuilder
```

### TypeBuilder

Generates type members.

```csharp
using var type = /* ... */.AppendOpenType();

type.BuildProperty(propertyType, "PropertyName")
    .WithAccessibility(Accessibility.Public)
    .WithImplicitGetter()
    .WithImplicitSetter()
    .Append();
```

### PropertyBuilder

Configures property generation with full control over modifiers and accessors.

```csharp
type.BuildProperty(typeSymbol, "Name")
    .WithAccessibility(Accessibility.Public)
    .WithRequired()                              // required modifier
    .WithNew()                                   // new modifier
    .WithVirtual()                               // virtual modifiers (or WithOverride, WithAbstract)
    .WithImplicitGetter()                        // { get; }
    .WithImplicitSetter(initOnly: true)          // { init; }
    .WithInheritDoc(baseType, "PropertyName")    // inheritdoc
    .WithAttributes(attributeDataArray)          // copy attributes
    .Append();
```

### SourceGeneratorProvider

Provides access to attributes embedded as assembly resources.

```csharp
// Get handle to generated attribute
IGeneratorAttribute attribute = SourceGeneratorProvider.GetAttribute<MyAttribute>();

// Use in post-initialization
context.RegisterPostInitializationOutput(ctx => ctx.AddSource(attribute));

// Use for finding attributed symbols
context.SyntaxProvider.ForAttributeWithMetadataName(attribute.TypeFullName, ...);
```

### Extension Methods

```csharp
// Add attribute source to context
context.AddSource(generatorAttribute);

// Create provider for types with attribute
syntaxProvider.ForTypeWithAttribute(attribute);

// Check if namespace is global
namespaceSymbol.IsGlobal();
namespaceSymbol.IsNotGlobal();
```

## Requirements

- .NET Standard 2.0 compatible
- Microsoft.CodeAnalysis.CSharp

## License

MIT License - see [LICENSE](LICENSE) for details.
