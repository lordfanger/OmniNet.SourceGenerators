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
    }
}
