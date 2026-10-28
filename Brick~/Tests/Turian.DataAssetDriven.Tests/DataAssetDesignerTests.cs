using System.Numerics;

namespace Turian.Tests;

/// <summary>Designer values and input adapters preserve change-only delivery, disabled maps and session reset.</summary>
public sealed class DataAssetDesignerTests
{
    /// <summary>Multiple fixed ticks in one input frame never repeat edges or held-state changes.</summary>
    [Fact]
    public void FixedTickReadersPublishChangesAndRespectDisabledMaps()
    {
        var source = new BufferedInputSource();
        var maps = InputActionsAsset.CreateDefault();
        var pressed = new DataAssetEvent();
        var released = new DataAssetEvent();
        var held = new DataAssetVariableBool();
        var axis = new DataAssetVariableFloat();
        var vector = new DataAssetVariableVector2();
        var reader = new InputReader
        {
            Actions = maps, Action = "Gameplay/Move", OnPressed = [pressed], OnReleased = [released],
            Held = held, AxisValue = axis, VectorValue = vector
        };
        var component = new InputReaderComponent { Readers = [reader], Input = source };
        component.OnFixedUpdate(0);
        using (var session = new DataAssetSession())
        {
            foreach (var asset in new DataAsset[] { maps, reader, pressed, released, held, axis, vector }) { session.Attach(asset); }
            var downs = 0;
            var ups = 0;
            var changes = 0;
            using var down = pressed.Subscribe(() => { Assert.True(held.Value); downs++; });
            using var up = released.Subscribe(() => { Assert.False(held.Value); ups++; });
            using var changed = vector.Subscribe(_ => changes++);
            source.PushKeyDown(Key.D);
            component.OnFixedUpdate(0);
            component.OnFixedUpdate(0);
            Assert.Equal(1, downs);
            Assert.Equal(1, changes);
            Assert.Equal(1, axis.Value);
            Assert.Equal(Vector2.UnitX, vector.Value);
            Assert.Equal(Vector2.UnitX, reader.Vector);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++) { component.OnFixedUpdate(0); }
            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
            maps.Maps[0].Enabled = false;
            component.OnFixedUpdate(0);
            component.OnFixedUpdate(0);
            Assert.Equal(1, ups);
            Assert.Equal(2, changes);
            Assert.Equal(Vector2.Zero, vector.Value);
            maps.Maps[0].Enabled = true;
            component.OnFixedUpdate(0);
            Assert.Equal(2, downs);
            reader.Action = "Missing";
            component.OnFixedUpdate(0);
            Assert.Equal(2, ups);
            Assert.False(reader.IsPressed);
        }
        Assert.Equal(Vector2.Zero, vector.Value);
        Assert.False(held.Value);
    }

    /// <summary>References choose inline values or live variables without mutating serialized defaults.</summary>
    [Fact]
    public void ReferencesSwitchSourcesAndPreserveAuthoredDefaults()
    {
        var variable = new DataAssetVariableInt { DefaultValue = 10 };
        var reference = new Turian.Engine.DataAssetDriven.DataAssetReference<int> { ConstantValue = 3, Variable = variable };
        Assert.Equal(3, reference.Value);
        Assert.Throws<InvalidOperationException>(() => reference.Value = 4);
        reference.ConstantValue = 4;
        Assert.Equal(4, reference.ConstantValue);
        reference.UseConstant = false;
        Assert.Equal(10, reference.Value);
        using (var session = new DataAssetSession())
        {
            session.Attach(variable);
            reference.Value = 12;
            Assert.Equal(12, variable.Value);
            Assert.Equal(10, variable.DefaultValue);
            reference.UseConstant = true;
            Assert.Equal(4, reference.Value);
            reference.Variable = null;
            reference.UseConstant = false;
            Assert.Throws<InvalidOperationException>(() => reference.Value = 8);
            Assert.Equal(4, reference.Value);
        }
        Assert.Equal(10, variable.Value);
    }

    /// <summary>The inspector exposes authored sources and a typed picker without an editable runtime value.</summary>
    [Fact]
    public void ReferenceInspectorEditsSourcesAndSelectsCompatibleVariables()
    {
        var reference = new Turian.Engine.DataAssetDriven.DataAssetReference<int> { ConstantValue = 3 };
        var fields = InspectorForms.Build(reference).Sections[0].Fields;
        Assert.DoesNotContain(fields, static field => field.Name == "Value");
        var slot = fields.Single(static field => field.Name == "Variable");
        var picker = ReferenceField.TryCreate(slot);
        Assert.NotNull(picker);
        var variable = new DataAssetVariableInt { DefaultValue = 12 };
        Assert.True(picker.SetTarget(variable));
        Assert.Same(variable, reference.Variable);
        Assert.Equal(3, reference.Value);
        Assert.True(fields.Single(static field => field.Name == "UseConstant").SetValue(false));
        Assert.Equal(12, reference.Value);
        Assert.False(picker.SetTarget(new DataAssetVariableBool()));
        Assert.True(picker.Clear());
        Assert.Null(reference.Variable);
        Assert.Equal(3, reference.Value);
    }

    /// <summary>Stopped assets expose authored defaults and typed channels instead of an unsafe session-value setter.</summary>
    [Fact]
    public void VariableInspectorEditsDefaultsAndTypedChangeChannels()
    {
        var variable = new DataAssetVariableInt();
        var fields = InspectorForms.Build(variable).Sections[0].Fields;
        Assert.DoesNotContain(fields, static field => field.Name == "Value");
        Assert.True(fields.Single(static field => field.Name == "DefaultValue").SetValue(5));
        Assert.Equal(5, variable.DefaultValue);
        var picker = ReferenceField.TryCreate(fields.Single(static field => field.Name == "OnChanged"));
        Assert.NotNull(picker);
        var channel = new DataAssetEventInt();
        Assert.True(picker.SetTarget(channel));
        Assert.Same(channel, variable.OnChanged);
        Assert.False(picker.SetTarget(new DataAssetEventBool()));
    }

    /// <summary>Nested reference fields hydrate their GUID variable and release it with the runtime session.</summary>
    [Fact]
    public void NestedReferencesHydrateThroughTheAssetLoader()
    {
        var root = Path.Combine(Path.GetTempPath(), "turian-ddd-reference-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Assets"));
        try
        {
            var database = new AssetDatabase();
            var variable = new DataAssetVariableInt { DefaultValue = 7 };
            var carrier = new DesignerReferenceAsset
            {
                Amount = new Turian.Engine.DataAssetDriven.DataAssetReference<int> { UseConstant = false, Variable = variable }
            };
            foreach (var asset in new DataAsset[] { variable, carrier })
            {
                var path = Path.Combine(root, "Assets", asset.Id + ".dataasset");
                Serializer.Save(path, asset);
                var metadata = new DataAssetAsset { Id = asset.Id, RelativePath = path };
                Serializer.Save(path + ".meta", metadata);
                Assert.True(database.RegisterAsset(metadata));
            }
            using var session = new DataAssetSession();
            var loader = new RuntimeAssetLoader(database, session);
            var loaded = Load<DesignerReferenceAsset>(loader, carrier.Id);
            Assert.True(loaded.Amount.Variable!.IsRuntime);
            Assert.Equal(7, loaded.Amount.Value);
            loaded.Amount.Value = 11;
            Assert.Equal(11, loaded.Amount.Value);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    static T Load<T>(IAssetLoader loader, Guid id) where T : DataAsset =>
        loader.LoadContentAsync<T>(id).GetAwaiter().GetResult()!;
}

/// <summary>A serializable asset with an inline-or-variable field used by hydration regression tests.</summary>
[TypeId("a7b9e79b-7d0a-4d9c-b6d2-dcd91d6e4b26")]
public sealed class DesignerReferenceAsset : DataAsset
{
    /// <summary>The designer's selected value source.</summary>
    public Turian.Engine.DataAssetDriven.DataAssetReference<int> Amount { get; set; } = new();
}
