namespace Turian.Engine.Core;

/// <summary>An empty gameplay signal with no boxed payload.</summary>
public readonly record struct DataAssetSignal;

/// <summary>Lets Studio raise a typed channel's default payload through compiled generic code.</summary>
public interface IDataAssetEventDebug
{
    /// <summary>Raises a default payload on the owning game-loop thread.</summary>
    void RaiseDefault();
}

/// <summary>A designer-wired gameplay channel identified by its asset GUID.</summary>
[TypeId("49f8b256-0d71-4f87-92cb-41ed3ec839b8")]
[CreateAssetMenu(fileName: "New Event.dataasset", path: "DDD/DataAssetEvent")]
public class DataAssetEvent : DataAssetEvent<DataAssetSignal>
{
    /// <summary>Raises synchronously on the game-loop thread; listener exceptions propagate and stop delivery.</summary>
    public void Raise() => Raise(default);

    /// <summary>Queues a worker signal for the fixed-tick boundary; false means the bounded queue is full.</summary>
    public bool TryQueue() => TryQueue(default);

    /// <summary>Subscribes until the returned handle or session is disposed.</summary>
    public IDisposable Subscribe(Action listener, int priority = 0, Guid owner = default)
    {
        ArgumentNullException.ThrowIfNull(listener);
        return Subscribe(_ => listener(), priority, owner);
    }
}

/// <summary>A typed gameplay channel; use record struct payloads for allocation-free raises.</summary>
public class DataAssetEvent<T> : DataAsset, IDataAssetEventDebug
{
    [NonSerialized]
    DataAssetEventChannel<T>? runtimeChannel;

    DataAssetEventChannel<T> Channel => runtimeChannel ??
        throw new InvalidOperationException("This DataAsset has not been hydrated into a running session.");

    internal override void InitializeRuntime(DataAssetSession session) =>
        runtimeChannel = session.Bus.Prepare<T>(Id);

    internal override void ResetRuntime()
    {
        runtimeChannel = null;
        base.ResetRuntime();
    }

    void IDataAssetEventDebug.RaiseDefault() => Raise(default!);

    /// <summary>Raises on the session's game-loop thread in descending priority, then subscription order.</summary>
    public void Raise(T payload) => Channel.Raise(payload);

    /// <summary>Queues a worker payload for tick-boundary delivery; false means the bounded queue is full.</summary>
    public bool TryQueue(T payload) => Channel.TryQueue(payload);

    /// <summary>Subscribes with explicit ownership; newly added listeners join subsequent raises, including nested raises.</summary>
    public IDisposable Subscribe(Action<T> listener, int priority = 0, Guid owner = default) =>
        Channel.Subscribe(listener, priority, owner);
}
