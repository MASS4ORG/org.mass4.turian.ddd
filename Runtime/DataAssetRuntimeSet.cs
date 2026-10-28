namespace Turian.Engine.Core;

/// <summary>An insertion or removal in a session-owned runtime set.</summary>
public readonly record struct DataAssetSetChange<T>(T Item, bool Added);

/// <summary>A deterministic insertion-ordered collection of live objects, stored only in the running session.</summary>
public class DataAssetRuntimeSet<T> : DataAsset where T : class
{
    [NonSerialized]
    DataAssetEventChannel<DataAssetSetChange<T>>? runtimeChannel;

    DataAssetEventChannel<DataAssetSetChange<T>> Channel => runtimeChannel ??
        throw new InvalidOperationException("This DataAsset has not been hydrated into a running session.");

    internal override void InitializeRuntime(DataAssetSession session) =>
        runtimeChannel = session.Bus.Prepare<DataAssetSetChange<T>>(Id);

    internal override void ResetRuntime()
    {
        runtimeChannel = null;
        base.ResetRuntime();
    }

    /// <summary>Subscribes to membership changes on the game-loop thread.</summary>
    public IDisposable Subscribe(Action<DataAssetSetChange<T>> listener, int priority = 0, Guid owner = default) =>
        Channel.Subscribe(listener, priority, owner);

    /// <summary>The live membership; stopped assets expose an empty collection.</summary>
    [JsonIgnore]
    public IReadOnlyList<T> Items => RuntimeSession is null ? [] : State().View;

    /// <summary>Adds a live object once; membership order is insertion order.</summary>
    public bool Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var state = State();
        if (state.Items.Contains(item)) { return false; }
        state.Items.Add(item);
        Channel.Raise(new DataAssetSetChange<T>(item, true));
        return true;
    }

    /// <summary>Removes a live object; owners must remove their membership when disabled or destroyed.</summary>
    public bool Remove(T item)
    {
        if (!State().Items.Remove(item)) { return false; }
        Channel.Raise(new DataAssetSetChange<T>(item, false));
        return true;
    }

    SetState State() => RequireRuntimeSession().State(this, static _ => new SetState());

    sealed class SetState : IDisposable
    {
        internal readonly List<T> Items = [];
        internal readonly IReadOnlyList<T> View;
        internal SetState() => View = Items.AsReadOnly();
        public void Dispose() => Items.Clear();
    }
}

/// <summary>A designer-created set of live scene nodes.</summary>
[TypeId("55b55f54-185e-48b5-afba-ad83347adb8a")]
[CreateAssetMenu(fileName: "New Node Set.dataasset", path: "DDD/DataAssetRuntimeSet<T>/Node")]
public sealed class DataAssetRuntimeSetNode : DataAssetRuntimeSet<Node>;
