namespace Turian.Tests;

public sealed partial class DataAssetGeneratorTests
{
    /// <summary>Nullable signal channels generate no-payload subscriptions without rejecting their handler.</summary>
    [Fact]
    public void NullableSignalListenerCompiles()
    {
        const string source = """
            #nullable enable
            public partial class NullableListener : Turian.Engine.Core.Component
            {
                public Turian.Engine.Core.DataAssetEvent? Channel { get; set; }
                [Turian.DataAssetListen(nameof(Channel))]
                void Receive() { }
            }
            """;
        CSharpGeneratorDriver.Create(
                [LoadFromGenerator<IIncrementalGenerator>("Turian.CSharp.CodeGenerator.DataAssetWiringGenerator")
                    .AsSourceGenerator()], parseOptions: ParseOptions)
            .RunGeneratorsAndUpdateCompilation(Compile(source), out var output, out var diagnostics,
                TestContext.Current.CancellationToken);
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(output.GetDiagnostics(TestContext.Current.CancellationToken),
            static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>Payload mismatches and unsupported component shapes fail at compile time.</summary>
    [Theory]
    [InlineData("partial", "float")]
    [InlineData("", "int")]
    public void WiringGeneratorRejectsInvalidHandler(string modifier, string payload)
    {
        var source = $$"""
            using Turian.Engine.Core;
            public {{modifier}} class Listener : Component
            {
                public DataAssetEvent<int> Channel { get; set; }
                [Turian.DataAssetListen(nameof(Channel))]
                void Receive({{payload}} value) { }
            }
            """;
        var driver = CSharpGeneratorDriver.Create(
                [LoadFromGenerator<IIncrementalGenerator>("Turian.CSharp.CodeGenerator.DataAssetWiringGenerator")
                    .AsSourceGenerator()], parseOptions: ParseOptions)
            .RunGenerators(Compile(source), TestContext.Current.CancellationToken);
        Assert.Contains(driver.GetRunResult().Diagnostics, static diagnostic => diagnostic.Id == "TURDDD001");
    }

    /// <summary>Generated definitions compile for record struct payloads and preserve explicit type identities.</summary>
    [Fact]
    public void EventDefinitionGeneratorCompiles()
    {
        const string source = """
            [Turian.DataAssetEventDefinition("HitEvent", "242ddcac-6112-4663-b991-90f0e91bb9ce")]
            public readonly record struct Hit(int Amount);
            """;
        CSharpGeneratorDriver.Create(
                [LoadFromGenerator<IIncrementalGenerator>("Turian.CSharp.CodeGenerator.DataAssetEventGenerator")
                    .AsSourceGenerator()], parseOptions: ParseOptions)
            .RunGeneratorsAndUpdateCompilation(Compile(source), out var output, out var diagnostics,
                TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics);
        Assert.Empty(output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }
}
