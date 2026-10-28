namespace Turian.Engine.Core;

/// <summary>Forwards a designer-assigned event to response assets with explicit enable/disable ownership.</summary>
[TypeId("78e5ad68-1c30-4280-b767-b90f61f7b40b")]
[ComponentContextMenu("DataAsset/Listener")]
public class DataAssetListenerComponent : Component
{
    IDisposable? subscription;

    /// <summary>The gameplay channel this component listens to.</summary>
    public DataAssetEvent? Event { get; set; }

    /// <summary>Higher priorities run first; ties retain subscription order.</summary>
    public int Priority { get; set; }

    /// <summary>Response assets executed in authored order, without method names or reflection.</summary>
    public List<DataAssetResponse> Responses { get; set; } = [];

    /// <inheritdoc />
    public override void OnEnable()
    {
        subscription?.Dispose();
        subscription = Event is { IsRuntime: true } channel ? channel.Subscribe(Respond, Priority, Id) : null;
    }

    /// <inheritdoc />
    public override void OnDisable()
    {
        subscription?.Dispose();
        subscription = null;
    }

    void Respond()
    {
        foreach (var response in Responses) { response.Execute(); }
    }
}

/// <summary>A designer-wired response with a statically compiled implementation.</summary>
public abstract class DataAssetResponse : DataAsset
{
    /// <summary>Executes on the game-loop thread; asset references are hydrated before scene activation.</summary>
    public abstract void Execute();
}

/// <summary>Raises response channels in their authored order.</summary>
[TypeId("41a2ea92-d72e-4792-b645-fe66b41bf4d7")]
[CreateAssetMenu(fileName: "New Raise Response.dataasset", path: "DDD/Response/Raise Events")]
public sealed class DataAssetRaiseResponse : DataAssetResponse
{
    /// <summary>The event assets raised by this response.</summary>
    public List<DataAssetEvent> Events { get; set; } = [];

    /// <inheritdoc />
    public override void Execute()
    {
        RequireRuntimeSession().EnsureMainThread();
        foreach (var channel in Events) { channel.Raise(); }
    }
}

/// <summary>Changes an integer variable through designer wiring.</summary>
[TypeId("026e6f3a-50d5-4acd-980b-441457a62b6c")]
[CreateAssetMenu(fileName: "New Int Response.dataasset", path: "DDD/Response/Set or Add Int")]
public sealed class DataAssetIntResponse : DataAssetResponse
{
    /// <summary>The shared variable to change.</summary>
    public DataAssetVariableInt? Variable { get; set; }

    /// <summary>The authored amount to set or add.</summary>
    public int Amount { get; set; } = 1;

    /// <summary>Whether to add the amount to the current value instead of replacing it.</summary>
    public bool Add { get; set; } = true;

    /// <inheritdoc />
    public override void Execute()
    {
        RequireRuntimeSession().EnsureMainThread();
        if (Variable is not null) { Variable.Value = Add ? Variable.Value + Amount : Amount; }
    }
}
