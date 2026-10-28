namespace Turian.Tests;

/// <summary>Authored defaults and runtime values remain isolated across Play sessions.</summary>
public sealed class DataAssetValueTests
{
    /// <summary>Values change through typed notifications and reset without changing the serialized default.</summary>
    [Fact]
    public void VariableChangesAndResets()
    {
        var variable = new DataAssetVariableInt { DefaultValue = 10 };
        var changed = new DataAssetEvent<int>();
        variable.OnChanged = changed;
        Assert.Equal(10, variable.Value);
        Assert.Throws<InvalidOperationException>(() => variable.Value = 20);
        using (var session = new DataAssetSession())
        {
            session.Attach(variable);
            session.Attach(changed);
            var changes = new List<DataAssetValueChange<int>>();
            var values = new List<int>();
            using var handle = variable.Subscribe(changes.Add);
            using var wired = changed.Subscribe(values.Add);
            variable.Value = 20;
            variable.Value = 20;
            Assert.Equal([new DataAssetValueChange<int>(10, 20)], changes);
            Assert.Equal([20], values);
            Assert.Equal(10, variable.DefaultValue);
            Assert.DoesNotContain("\"Value\"", Serializer.Serialize<DataAsset>(variable), StringComparison.Ordinal);
        }
        Assert.Equal(10, variable.Value);
        using var next = new DataAssetSession();
        next.Attach(variable);
        Assert.Equal(10, variable.Value);
    }

    /// <summary>Runtime set membership never becomes authored scene references and clears on exit.</summary>
    [Fact]
    public void RuntimeSetTracksOnceAndClears()
    {
        var set = new DataAssetRuntimeSetNode();
        IReadOnlyList<Node>? retainedView = null;
        Assert.Empty(set.Items);
        using (var session = new DataAssetSession())
        {
            session.Attach(set);
            var changes = new List<DataAssetSetChange<Node>>();
            using var membership = set.Subscribe(changes.Add);
            var first = new Node();
            var second = new Node();
            Assert.True(set.Add(first));
            Assert.False(set.Add(first));
            Assert.True(set.Add(second));
            retainedView = set.Items;
            Assert.Equal([first, second], set.Items);
            Assert.True(set.Remove(first));
            Assert.False(set.Remove(first));
            Assert.Equal([true, true, false], changes.Select(static change => change.Added));
            Assert.DoesNotContain("Items", Serializer.Serialize<DataAsset>(set), StringComparison.Ordinal);
        }
        Assert.Empty(set.Items);
        Assert.Empty(retainedView!);
    }

    /// <summary>Components communicate through assets, and disabling the listener disposes its subscription.</summary>
    [Fact]
    public void ListenerExecutesDesignerAssignedResponse()
    {
        using var session = new DataAssetSession();
        var channel = new DataAssetEvent();
        var variable = new DataAssetVariableInt();
        var response = new DataAssetIntResponse { Variable = variable, Amount = 3 };
        foreach (var asset in new DataAsset[] { channel, variable, response }) { session.Attach(asset); }
        var listener = new DataAssetListenerComponent { Event = channel, Responses = [response] };
        listener.OnEnable();
        listener.OnEnable();
        Assert.Single(session.Listeners(channel.Id));
        channel.Raise();
        Assert.Equal(3, variable.Value);
        listener.OnDisable();
        channel.Raise();
        Assert.Equal(3, variable.Value);
        Assert.Empty(session.Listeners(channel.Id));
    }

    /// <summary>A response asset raises its designer-wired event list in authored order.</summary>
    [Fact]
    public void RaiseResponseExecutesWiredEvents()
    {
        using var session = new DataAssetSession();
        var channel = new DataAssetEvent();
        var response = new DataAssetRaiseResponse { Events = [channel] };
        session.Attach(channel);
        session.Attach(response);
        var count = 0;
        using var listener = channel.Subscribe(() => count++);
        response.Execute();
        Assert.Equal(1, count);
    }

    /// <summary>Schema zero migrates and unsupported future schemas reject before hydration.</summary>
    [Fact]
    public void SchemaMigrationRejectsFutureVersions()
    {
        var json = Serializer.Serialize<DataAsset>(new DataAssetVariableInt { DefaultValue = 7 });
        var old = json.Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": 0", StringComparison.Ordinal);
        Assert.Equal(1, DataAsset.DeserializePayload(old)!.SchemaVersion);
        var future = json.Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": 999", StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => DataAsset.DeserializePayload(future));
    }
}
