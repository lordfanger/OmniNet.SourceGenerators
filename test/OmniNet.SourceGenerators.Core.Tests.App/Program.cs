using OmniNet.SourceGenerators.Core.Tests.Generator.Template.Attributes;

namespace OmniNet.SourceGenerators.Core.Tests.App;

// Custom interface to test explicit interface implementation
public interface IMyInterface
{
    string Name { get; set; }
    int GetValue();
}

// Generic interface with static members (C# 11+)
public interface IMyGenericInterface<T> where T : IMyGenericInterface<T>
{
    static abstract string StaticProperty { get; }
    static abstract T Create();
}

[TestGenerate]
public partial class MyGeneratedClass : IMyInterface, IMyGenericInterface<MyGeneratedClass>
{
}

internal class Program
{
    public static void Main()
    {
        var obj = new MyGeneratedClass { Id = "test-id" };
        Console.WriteLine($"Id: {obj.Id}");

        var x = MyGeneratedClass.CreateDefault();
        Console.WriteLine($"Default Id: {x.Id}");

        // Test property initializers
        var y = new MyGeneratedClass { Id = "initialized-test" };
        Console.WriteLine($"DefaultCounter: {y.DefaultCounter}"); // Should be 10
        Console.WriteLine($"DefaultName: {y.DefaultName}"); // Should be "DefaultValue"
        
        // Test modified values
        y.DefaultCounter = 20;
        Console.WriteLine($"Modified DefaultCounter: {y.DefaultCounter}"); // Should be 20
    }
}

