# OmniNet.SourceGenerators.Core

A core library providing helpers and builders for creating C# source generators with ease.

## Installation

```shell
dotnet add package OmniNet.SourceGenerators.Core
```

## Features

- **SourceBuilder** - Fluent API for generating C# source code files
- **TypeBuilder** - Builder for generating type members (properties, methods, etc.)
- **PropertyBuilder** - Builder for generating properties with modifiers, accessors, and attributes
- **MethodBuilder** - Builder for generating methods with modifiers, parameters, and bodies
- **MethodParametersBuilder** - Builder for method parameters (including ref/out/in/params/defaults)
- **MethodBodyBuilder** - Builder for method bodies (statements, return, throw, etc.)
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
        var stringType = context.Compilation.GetSpecialType(SpecialType.System_String);
        var intType = context.Compilation.GetSpecialType(SpecialType.System_Int32);

        using var type = sb
            .AppendFileNamespace(typeSymbol.ContainingNamespace)
            .BuildClass(typeSymbol.Name)
            .WithAccessibility(typeSymbol.DeclaredAccessibility)
            .WithPartial()
            .Append()
            .AppendOpenType();

        // Add properties using PropertyBuilder
        type.BuildProperty(stringType, "GeneratedProperty")
            .WithAccessibility(Accessibility.Public)
            .WithImplicitGetter()
            .WithImplicitSetter()
            .Append();

        // Add methods using MethodBuilder
        type.BuildMethod("GetName", stringType)
            .WithAccessibility(Accessibility.Public)
            .OpenParameters()
            .OpenBody()
                .AppendReturn("GeneratedProperty")
            .Dispose();

        type.BuildMethod("SetValue")
            .WithAccessibility(Accessibility.Public)
            .OpenParameters()
                .AddParameter(stringType, "key")
                .AddParameter(intType, "value", "0")
            .OpenBody()
                .AppendLine("// Set value logic")
            .Dispose();

        type.BuildMethod("ToString", stringType)
            .WithAccessibility(Accessibility.Public)
            .WithOverride()
            .OpenParameters()
            .AppendExpression("$\"Generated: {GeneratedProperty}\"");

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

type.BuildMethod("MethodName") // void method
    .WithAccessibility(Accessibility.Public)
    .OpenParameters()
    .OpenBody()
        .AppendLine("// method body")
    .Dispose();

type.BuildMethod("MethodName", returnType) // method with return type
    .WithAccessibility(Accessibility.Public)
    .OpenParameters()
    .OpenBody()
        .AppendReturn("expression")
    .Dispose();
```

### MethodBuilder

Configures method generation with full control over modifiers, parameters, and body.

```csharp
type.BuildMethod("Name")
    .WithAccessibility(Accessibility.Public)
    .WithStatic()
    .WithAsync()
    .WithVirtual() // or .WithOverride(), .WithAbstract()
    .WithNew()
    .WithInheritDoc(baseType, "MethodName")
    .WithAttributes(attributeDataArray)
    .OpenParameters()
        .AddParameter(typeSymbol, "param")
        .AddRefParameter(typeSymbol, "refParam")
        .AddOutParameter(typeSymbol, "outParam")
        .AddParamsParameter(typeSymbol, "paramsArray")
    .OpenBody()
        .AppendLine("// method body")
        .AppendReturn("value")
    .Dispose();

// Expression-bodied method
 type.BuildMethod("ToString", stringType)
    .WithAccessibility(Accessibility.Public)
    .WithOverride()
    .OpenParameters()
    .AppendExpression("$\"Name: {Property}\"");

// Abstract/interface method
 type.BuildMethod("Calculate", intType)
    .WithAccessibility(Accessibility.Public)
    .WithAbstract()
    .OpenParameters()
        .AddParameter(intType, "x")
    .AppendAbstract();
```

### MethodParametersBuilder

Builder for method parameters.

```csharp
.OpenParameters()
    .AddParameter(typeSymbol, "name")
    .AddParameter(typeSymbol, "name", "defaultValue")
    .AddRefParameter(typeSymbol, "refName")
    .AddOutParameter(typeSymbol, "outName")
    .AddInParameter(typeSymbol, "inName")
    .AddParamsParameter(typeSymbol, "paramsName")
    .AddParametersFrom(methodSymbol)
```

### MethodBodyBuilder

Builder for method body.

```csharp
.OpenBody()
    .AppendLine("// code")
    .AppendReturn("expression")
    .AppendThrow("new Exception()")
    .Dispose();
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
