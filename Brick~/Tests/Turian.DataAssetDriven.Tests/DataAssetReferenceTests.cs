using System.Text.Json;

namespace Turian.Tests;

/// <summary>Inline designer references retain missing GUIDs, accept nulls and hydrate asset-enum constants.</summary>
public sealed class DataAssetReferenceTests
{
    /// <summary>Unresolved or mismatched targets survive saving; a designer replacement clears the old GUID.</summary>
    [Fact]
    public async Task PendingReferencesSurviveUntilResolvedOrReassigned()
    {
        var variable = new DataAssetVariableInt();
        var original = new DesignerReferenceAsset { Amount = new() { UseConstant = false, Variable = variable } };
        var copy = Serializer.LoadData<DesignerReferenceAsset>(Serializer.Serialize<DataAsset>(original))!;
        var loader = Substitute.For<IAssetLoader>();
        loader.PreloadAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        await ObjectReferences.ResolveAsync(copy, loader);
        Assert.Null(copy.Amount.Variable);
        Assert.Contains(variable.Id.ToString(), Serializer.Serialize<DataAsset>(copy), StringComparison.Ordinal);
        var wrong = new DataAssetEvent();
        loader.LoadContentAsync<DataAsset>(variable.Id).Returns(wrong);
        await ObjectReferences.ResolveAsync(copy, loader);
        Assert.Null(copy.Amount.Variable);
        Assert.True(ObjectReferences.TryGetUnresolved(copy, "Amount.Variable", out _));
        loader.LoadContentAsync<DataAsset>(variable.Id).Returns(variable);
        await ObjectReferences.ResolveAsync(copy, loader);
        Assert.Same(variable, copy.Amount.Variable);
        Assert.False(ObjectReferences.TryGetUnresolved(copy, "Amount.Variable", out _));
        var missing = Serializer.LoadData<DesignerReferenceAsset>(Serializer.Serialize<DataAsset>(original))!;
        var replaced = Serializer.LoadData<DesignerReferenceAsset>(Serializer.Serialize<DataAsset>(original))!;
        replaced.Amount = new() { ConstantValue = 3 };
        Assert.DoesNotContain(variable.Id.ToString(), Serializer.Serialize<DataAsset>(replaced), StringComparison.Ordinal);
        missing.Amount.Variable = null;
        await ObjectReferences.ResolveAsync(missing, loader);
        Assert.Null(missing.Amount.Variable);
        Assert.DoesNotContain(variable.Id.ToString(), Serializer.Serialize<DataAsset>(missing), StringComparison.Ordinal);
    }

    /// <summary>Null fields and null/empty wires do not create pending setters or lose inline defaults.</summary>
    [Fact]
    public void NullReferencesAndMalformedWiresAreHandledExplicitly()
    {
        var asset = new DesignerReferenceAsset { Amount = null! };
        Assert.Null(Serializer.LoadData<DesignerReferenceAsset>(Serializer.Serialize<DataAsset>(asset))!.Amount);
        asset.Amount = new() { ConstantValue = 9 };
        var json = Serializer.Serialize<DataAsset>(asset);
        var copy = Serializer.LoadData<DesignerReferenceAsset>(json)!;
        Assert.Equal(9, copy.Amount.Value);
        Assert.False(ObjectReferences.TryGetUnresolved(copy, "Amount.Variable", out _));
        var empty = json.Replace("\"Variable\": null", "\"Variable\": {\"$ref\":\"00000000-0000-0000-0000-000000000000\"}", StringComparison.Ordinal);
        Assert.Null(Serializer.LoadData<DesignerReferenceAsset>(empty)!.Amount.Variable);
        var invalid = json.Replace("\"Variable\": null", "\"Variable\": 3", StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => Serializer.LoadData<DesignerReferenceAsset>(invalid));
    }

    /// <summary>Asset-enum inline constants are references too, preserving GUIDs and resolving shared payload instances.</summary>
    [Fact]
    public async Task AssetConstantsHydrateAndDesignerReplacementClearsPendingIdentity()
    {
        var faction = new AssetEnumValue { Name = "Rival" };
        var authored = new DesignerEnumReferenceAsset { Faction = new() { ConstantValue = faction } };
        var json = Serializer.Serialize<DataAsset>(authored);
        var copy = Serializer.LoadData<DesignerEnumReferenceAsset>(json)!;
        Assert.Null(copy.Faction.ConstantValue);
        Assert.Contains(faction.Id.ToString(), Serializer.Serialize<DataAsset>(copy), StringComparison.Ordinal);
        var loader = Substitute.For<IAssetLoader>();
        loader.PreloadAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        loader.LoadContentAsync<DataAsset>(faction.Id).Returns(faction);
        await ObjectReferences.ResolveAsync(copy, loader);
        Assert.Same(faction, copy.Faction.Value);
        var replaced = Serializer.LoadData<DesignerEnumReferenceAsset>(json)!;
        replaced.Faction.ConstantValue = null!;
        Assert.DoesNotContain(faction.Id.ToString(), Serializer.Serialize<DataAsset>(replaced), StringComparison.Ordinal);
    }
}

/// <summary>A generated inline asset-enum reference fixture.</summary>
[TypeId("64016f6e-9b71-4f5f-99a7-c9003e6ce493")]
public sealed class DesignerEnumReferenceAsset : DataAsset
{
    /// <summary>The inline or shared faction choice.</summary>
    public Turian.Engine.DataAssetDriven.DataAssetReference<AssetEnumValue> Faction { get; set; } = new();
}
