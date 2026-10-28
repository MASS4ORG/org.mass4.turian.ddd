namespace Turian.Tests;

/// <summary>Generated event definitions and listener ownership work through actual component lifecycle calls.</summary>
public sealed class DataAssetWiringTests
{
    /// <summary>Generated subscriptions survive reactivation and release when a component is destroyed.</summary>
    [Fact]
    public void GeneratedHandlerOwnsSubscription()
    {
        using var session = new DataAssetSession();
        var channel = new WiringDamageEvent();
        session.Attach(channel);
        var listener = new WiringDamageListener { Damage = channel };
        var node = new Node();
        node.Components.Add(listener);
        node.Awake(null);
        channel.Raise(new WiringDamage(2));
        Assert.Equal(2, listener.Total);
        listener.IsActive = false;
        channel.Raise(new WiringDamage(3));
        Assert.Equal(2, listener.Total);
        listener.IsActive = true;
        channel.Raise(new WiringDamage(4));
        Assert.Equal(6, listener.Total);
        Assert.Equal(listener.Id, Assert.Single(session.Listeners(channel.Id)).Owner);
        listener.DestroyImmediate();
        Assert.Empty(session.Listeners(channel.Id));
    }

    /// <summary>The generated closed event definition registers an AOT-compatible serializer and stable type GUID.</summary>
    [Fact]
    public void TypedEventDefinitionHasGeneratedSerialization()
    {
        var channel = new WiringDamageEvent();
        Assert.True(GeneratedSerializers.TryGet(typeof(WiringDamageEvent), out _));
        var loaded = DataAsset.DeserializePayload(Serializer.Serialize<DataAsset>(channel));
        Assert.IsType<WiringDamageEvent>(loaded);
        Assert.Equal(channel.Id, loaded.Id);
    }
}

/// <summary>A record payload used to verify generated event assets.</summary>
[DataAssetEventDefinition("WiringDamageEvent", "29378570-acb1-431d-93d4-9d7d74c5829e")]
public readonly record struct WiringDamage(int Amount);

/// <summary>A listener whose typed subscription and disposal are generated.</summary>
public sealed partial class WiringDamageListener : Component
{
    /// <summary>The designer-assigned channel.</summary>
    public DataAssetEvent<WiringDamage>? Damage { get; set; }

    /// <summary>The sum received through the event asset.</summary>
    [JsonIgnore]
    public int Total { get; private set; }

    [DataAssetListen(nameof(Damage), 7)]
    void OnDamage(WiringDamage damage) => Total += damage.Amount;
}
