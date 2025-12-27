using OmniNet.SourceGenerators.Core.Tests.Generator.Template.Attributes;

namespace OmniNet.SourceGenerators.Core.Tests.App;

[TestGenerate]
public partial class MyGeneratedClass { }

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
