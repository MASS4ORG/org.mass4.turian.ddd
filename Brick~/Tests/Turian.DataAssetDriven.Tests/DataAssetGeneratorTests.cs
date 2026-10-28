namespace Turian.Tests;

/// <summary>Compile-time checks for generated typed event definitions and listener wiring.</summary>
[Collection(SerialTests.Name)]
public sealed partial class DataAssetGeneratorTests
{
    static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    static CSharpCompilation Compile(string source) =>
        CSharpCompilation.Create(
            "GeneratorTest",
            [CSharpSyntaxTree.ParseText(source, ParseOptions)],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    static T LoadFromGenerator<T>(string typeName)
    {
        var root = typeof(DataAssetGeneratorTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "Turian.SourceRoot").Value!;

#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        var assembly = Assembly.LoadFrom(Path.Combine(root, "Turian", "Editor", "CSharp", "CodeGenerator", "bin",
            configuration, "netstandard2.0", "Turian.CSharp.CodeGenerator.dll"));
        return (T)Activator.CreateInstance(assembly.GetType(typeName, throwOnError: true)!)!;
    }
}
