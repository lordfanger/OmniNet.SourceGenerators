# OmniNet.SourceGenerators.Core

A core library providing fluent builders and helpers for creating C# source generators with ease. Write source generators with minimal boilerplate using a type-safe, performant API designed for zero-allocation builder patterns.

## Installation

```shell
dotnet add package OmniNet.SourceGenerators.Core
```

[![NuGet](https://img.shields.io/nuget/v/OmniNet.SourceGenerators.Core.svg)](https://www.nuget.org/packages/OmniNet.SourceGenerators.Core)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/lordfanger/OmniNet.SourceGenerators)

## Features

- **Zero-allocation builders** - Uses `readonly ref struct` for stack allocation and maximum performance
- **Fluent API** - Chainable methods for intuitive, readable code generation
- **Type-safe** - Leverage C#'s type system to catch errors at compile time
- **Comprehensive** - Generate classes, interfaces, structs, records, properties, methods, nested types, and more
- **Incremental generator support** - Built for modern incremental source generators
- **Fully tested** - 65+ unit tests ensuring reliability and correctness

### Core Builders

- **SourceBuilder** - Entry point for generating C# source code files
- **OpeningTypeBuilder** - Configure type declarations (class, interface, struct, record) with modifiers
- **TypeBuilder** - Build type members (properties, methods, etc.) within a type
- **PropertyBuilder** - Generate properties with modifiers, accessors, initializers, and attributes
- **MethodBuilder** - Generate methods with modifiers, parameters, and bodies
- **MethodParametersBuilder** - Define method parameters (including ref/out/in/params/defaults)
- **MethodBodyBuilder** - Write method bodies (statements, return, throw, etc.)
- **TypeInheritanceBuilder** - Specify base classes and interfaces

### Utilities

- **TypeReference** - Flexible type specification (from ITypeSymbol, string, or namespace+name)
- **IncrementalSymbolValuesProvider** - Helper for creating incremental source generators
- **SourceGeneratorProvider** - Access generated attributes stored as assembly resources

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

        // Add properties using PropertyBuilder - with string type
        type.BuildProperty("string", "GeneratedProperty")
            .WithAccessibility(Accessibility.Public)
            .WithImplicitGetter()
            .WithImplicitSetter()
            .Append();

        // Add methods using MethodBuilder - with string return type
        type.BuildMethod("GetName", "string")
            .WithAccessibility(Accessibility.Public)
            .OpenParameters()
            .OpenBody()
                .AppendReturn("GeneratedProperty")
            .Dispose();

        type.BuildMethod("SetValue")
            .WithAccessibility(Accessibility.Public)
            .OpenParameters()
                .AddParameter("string", "key")
                .AddParameter("int", "value", "0")
            .OpenBody()
                .AppendLine("// Set value logic")
            .Dispose();

        type.BuildMethod("ToString", "string")
            .WithAccessibility(Accessibility.Public)
            .WithOverride()
            .OpenParameters()
            .AppendExpression("$\"Generated: {GeneratedProperty}\"");

        sb.AddToContext(context, TypeReference.FromSymbol(typeSymbol));
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
sb.AddToContext(context, TypeReference.FromSymbol(typeSymbol));
sb.AddToContext(context, namespaceSymbol, "FileName", "suffix");
```

### TypeReference

Represents a type that can be specified in multiple ways:

```csharp
// Option 1: From ITypeSymbol (use factory method)
var stringType = compilation.GetSpecialType(SpecialType.System_String);
type.BuildProperty(TypeReference.FromSymbol(stringType), "Name");

// Option 2: Plain string (implicit conversion - recommended for simplicity)
type.BuildProperty("string", "Name");
type.BuildProperty("int", "Count");

// Option 3: Fully qualified string (implicit conversion)
type.BuildProperty("global::System.Collections.Generic.List<int>", "Numbers");

// Option 4: Namespace + type name tuple (implicit conversion)
var myNamespace = compilation.GetCompilationNamespace("MyCompany.Domain");
type.BuildProperty((myNamespace, "CustomEntity"), "Entity");
type.BuildProperty((myNamespace, "Repository<T>"), "Repo");
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

Generates type members within a type declaration. All builder methods accept `TypeReference` for specifying types.

```csharp
using var type = /* ... */.AppendOpenType();

// Build properties using string type (implicit conversion - simplest)
type.BuildProperty("string", "PropertyName")
    .WithAccessibility(Accessibility.Public)
    .WithImplicitGetter()
    .WithImplicitSetter()
    .Append();

// Build properties using ITypeSymbol (when working with Roslyn symbols)
var stringType = compilation.GetSpecialType(SpecialType.System_String);
type.BuildProperty(stringType, "PropertyName")  // Overload accepts ITypeSymbol directly
    .WithAccessibility(Accessibility.Public)
    .WithImplicitGetter()
    .WithImplicitSetter()
    .Append();

// Build void method
type.BuildMethod("MethodName")
    .WithAccessibility(Accessibility.Public)
    .OpenParameters()
    .OpenBody()
        .AppendLine("// method body")
    .Dispose();

// Build method with return type (string)
type.BuildMethod("MethodName", "int")
    .WithAccessibility(Accessibility.Public)
    .OpenParameters()
    .OpenBody()
        .AppendReturn("42")
    .Dispose();

// Build method with ITypeSymbol return type
type.BuildMethod("GetValue", intSymbol)  // Overload accepts ITypeSymbol directly
    .WithAccessibility(Accessibility.Public)
    .OpenParameters()
    .AppendExpression("42");

// Build nested types
using var nestedType = type.BuildNestedClass("NestedClass")
    .WithAccessibility(Accessibility.Public)
    .Append()
    .AppendOpenType();

nestedType.BuildProperty("string", "NestedProperty")
    .WithAccessibility(Accessibility.Public)
    .WithImplicitGetter()
    .Append();

// Nested types can be nested further
using var deeplyNested = nestedType.BuildNestedClass("DeeplyNestedClass")
    .WithAccessibility(Accessibility.Private)
    .Append()
    .AppendOpenType();

// All nested type methods available:
// - BuildNestedClass(name)
// - BuildNestedInterface(name)
// - BuildNestedStruct(name)
// - BuildNestedRecord(name)
// - BuildNestedRecordStruct(name)

// Multiple members are automatically separated by blank lines
type.BuildProperty("int", "Age")
    .WithAccessibility(Accessibility.Public)
    .WithImplicitGetter()
    .WithImplicitSetter()
    .Append();

type.BuildProperty("string", "Name")
    .WithAccessibility(Accessibility.Public)
    .WithImplicitGetter()
    .WithImplicitSetter()
    .Append();
// Generates:
// public int Age { get; set; }
//
// public string Name { get; set; }
```

**Note:** TypeBuilder implements `IDisposable` to close the type braces. Always use `using var type = ...` or manually call `type.Dispose()` when done. This applies to both top-level and nested types.

### MethodBuilder

Configures method generation with full control over modifiers, parameters, and body. All type parameters accept `TypeReference`.

```csharp
type.BuildMethod("Name")
    .WithAccessibility(Accessibility.Public)
    .WithStatic()
    .WithAsync()
    .WithVirtual() // or .WithOverride(), .WithAbstract()
    .WithNew()
    .WithInheritDoc(TypeReference.FromSymbol(baseType), "MethodName")
    .WithAttributes(attributeDataArray)
    .OpenParameters()
        .AddParameter("string", "param")
        .AddRefParameter("int", "refParam")
        .AddOutParameter("bool", "outParam")
        .AddParamsParameter("object", "paramsArray")
    .OpenBody()
        .AppendLine("// method body")
        .AppendReturn("value")
    .Dispose();

// Expression-bodied method
 type.BuildMethod("ToString", "string")
    .WithAccessibility(Accessibility.Public)
    .WithOverride()
    .OpenParameters()
    .AppendExpression("$\"Name: {Property}\"");

// Abstract/interface method
 type.BuildMethod("Calculate", "int")
    .WithAccessibility(Accessibility.Public)
    .WithAbstract()
    .OpenParameters()
        .AddParameter("int", "x")
    .AppendAbstract();

// Explicit interface implementation
type.BuildMethod("ToString", "string")
    .WithExplicitInterfaceImplementation("System.IFormattable")
    .OpenParameters()
        .AddParameter("string", "format")
        .AddParameter("System.IFormatProvider", "formatProvider")
    .OpenBody()
        .AppendReturn("\"formatted string\"")
    .Dispose();
// Generates: string System.IFormattable.ToString(string format, System.IFormatProvider formatProvider) { ... }
```

### MethodParametersBuilder

Builder for method parameters. All type parameters accept `TypeReference`.

```csharp
.OpenParameters()
    .AddParameter("string", "name")
    .AddParameter("int", "count", "0")  // with default value
    .AddRefParameter("bool", "refName")
    .AddOutParameter("decimal", "outName")
    .AddInParameter("ReadOnlySpan<char>", "inName")
    .AddParamsParameter("object", "paramsName")
    .AddParametersFrom(methodSymbol)  // copy from existing method
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

Configures property generation with full control over modifiers and accessors. Property type accepts `TypeReference`.

```csharp
// Using string type (implicit conversion - simplest)
type.BuildProperty("string", "Name")
    .WithAccessibility(Accessibility.Public)
    .WithRequired()                              // required modifier
    .WithNew()                                   // new modifier
    .WithVirtual()                               // virtual modifiers (or WithOverride, WithAbstract)
    .WithImplicitGetter()                        // { get; }
    .WithImplicitSetter(initOnly: true)          // { init; }
    .WithInheritDoc("BaseTypeName", "PropertyName")  // inheritdoc
    .WithAttributes(attributeDataArray)          // copy attributes
    .Append();

// Using ITypeSymbol
var stringType = compilation.GetSpecialType(SpecialType.System_String);
type.BuildProperty(TypeReference.FromSymbol(stringType), "Name")
    .WithAccessibility(Accessibility.Public)
    .WithImplicitGetter()
    .WithImplicitSetter()
    .Append();

// Using namespace + type name
type.BuildProperty((myNamespace, "CustomType"), "Custom")
    .WithAccessibility(Accessibility.Public)
    .WithImplicitGetter()
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

## Testing

This library includes comprehensive unit tests covering all major functionality:

- **PropertyBuilderTests** - 17 tests covering property generation scenarios
- **MethodBuilderTests** - 21 tests covering method generation scenarios  
- **TypeBuilderTests** - 21 tests covering type creation and member building
- **NestedTypeBuilderTests** - 15 tests covering nested type generation scenarios

Run tests with:
```shell
dotnet test
```

All tests verify generated syntax is correct and follows C# conventions.

## Performance

This library is designed with performance in mind:

- **Zero-allocation builders** - All builders use `readonly ref struct` to avoid heap allocations
- **No LINQ in hot paths** - Direct iteration for maximum performance
- **StringBuilder reuse** - Efficient string building with minimal allocations
- **Span<T> usage** - Uses modern C# features for zero-copy operations where applicable

## Examples

### Nested Types

Generate nested types with full support for all type kinds and access modifiers:

```csharp
var sb = new SourceBuilder();

using var outerClass = sb.BuildClass("Container")
    .WithAccessibility(Accessibility.Public)
    .WithPartial()
    .Append()
    .AppendOpenType();

// Add property to outer class
outerClass.BuildProperty("string", "Name")
    .WithAccessibility(Accessibility.Public)
    .WithImplicitGetter()
    .WithImplicitSetter()
    .Append();

// Create nested class with private accessibility
using var nestedClass = outerClass.BuildNestedClass("Data")
    .WithAccessibility(Accessibility.Private)
    .Append()
    .AppendOpenType();

nestedClass.BuildProperty("int", "Value")
    .WithAccessibility(Accessibility.Public)
    .WithImplicitGetter()
    .Append();

// Create nested struct
using var nestedStruct = outerClass.BuildNestedStruct("Settings")
    .WithAccessibility(Accessibility.Public)
    .Append()
    .AppendOpenType();

nestedStruct.BuildProperty("bool", "IsEnabled")
    .WithAccessibility(Accessibility.Public)
    .WithImplicitGetter()
    .WithImplicitSetter()
    .Append();

// Generates:
// public partial class Container
// {
//     public string Name { get; set; }
//
//     private class Data
//     {
//         public int Value { get; }
//     }
//
//     public struct Settings
//     {
//         public bool IsEnabled { get; set; }
//     }
// }
```

**Nested Type Methods:**
- `BuildNestedClass(string name)` - Create nested class
- `BuildNestedInterface(string name)` - Create nested interface
- `BuildNestedStruct(string name)` - Create nested struct
- `BuildNestedRecord(string name)` - Create nested record
- `BuildNestedRecordStruct(string name)` - Create nested record struct

**Access Modifiers:**
- **Nested types in classes** support all modifiers: `public`, `private`, `protected`, `internal`, `protected internal`, `private protected`
- **Nested types in structs** support: `public`, `private`, `internal` (protected modifiers not allowed)
- **Default accessibility** for nested types is `private` (unlike top-level types which default to `internal`)

See the `test/OmniNet.SourceGenerators.Core.Tests.Generator` project for a complete working example of a source generator using this library.

## Requirements

- .NET Standard 2.0 (main library)
- .NET 10.0 (test projects)
- Microsoft.CodeAnalysis.CSharp 4.0.1+

## Repository

- **GitHub**: https://github.com/lordfanger/OmniNet.SourceGenerators
- **NuGet**: https://www.nuget.org/packages/OmniNet.SourceGenerators.Core

## License

MIT License - see [LICENSE](LICENSE) for details.
