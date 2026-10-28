namespace Turian.Engine.Core;

/// <summary>The previous and current value of a shared gameplay variable.</summary>
public readonly record struct DataAssetValueChange<T>(T Previous, T Current);

/// <summary>A designer-authored default with a session-owned mutable value and typed change subscriptions.</summary>
public class DataAssetVariable<T> : DataAsset, IDataAssetValue
{
    [NonSerialized]
    DataAssetEventChannel<DataAssetValueChange<T>>? runtimeChannel;

    DataAssetEventChannel<DataAssetValueChange<T>> Channel => runtimeChannel ??
        throw new InvalidOperationException("This DataAsset has not been hydrated into a running session.");

    internal override void InitializeRuntime(DataAssetSession session) =>
        runtimeChannel = session.Bus.Prepare<DataAssetValueChange<T>>(Id);

    internal override void ResetRuntime()
    {
        runtimeChannel = null;
        base.ResetRuntime();
    }

    /// <summary>The authored value restored for each new Play/export session.</summary>
    public T DefaultValue { get; set; } = default!;

    /// <summary>An optional designer-wired channel receiving the new value after each change.</summary>
    public DataAssetEvent<T>? OnChanged { get; set; }

    /// <summary>The session value while playing, or the authored default when inspecting a stopped asset.</summary>
    [JsonIgnore, Hide]
    public T Value
    {
        get => RuntimeSession is null ? DefaultValue : State().Value;
        set
        {
            var state = State();
            if (EqualityComparer<T>.Default.Equals(state.Value, value)) { return; }
            var previous = state.Value;
            state.Value = value;
            Channel.Raise(new DataAssetValueChange<T>(previous, value));
            OnChanged?.Raise(value);
        }
    }

    /// <summary>Subscribes to actual value changes; dispose the handle when its owner is disabled or destroyed.</summary>
    public IDisposable Subscribe(Action<DataAssetValueChange<T>> listener, int priority = 0, Guid owner = default) =>
        Channel.Subscribe(listener, priority, owner);

    object? IDataAssetValue.ReadValue() => Value;

    bool IDataAssetValue.TryWriteValue(object? value)
    {
        if (value is not T typed) { return false; }
        Value = typed;
        return true;
    }

    ValueState State() => RequireRuntimeSession().State(this,
        static asset => new ValueState(((DataAssetVariable<T>)asset).DefaultValue));

    sealed class ValueState(T value)
    {
        internal T Value = value;
    }
}

/// <summary>A float shared variable available in Studio without defining a type in game code.</summary>
[TypeId("b1bdeba3-75c9-48e7-9451-5b2670a334f4")]
[CreateAssetMenu(fileName: "New Float Variable.dataasset", path: "DDD/DataAssetVariable<T>/Float")]
public sealed class DataAssetVariableFloat : DataAssetVariable<float>;

/// <summary>An integer shared variable available in Studio without defining a type in game code.</summary>
[TypeId("cf27586a-7df6-4a26-a992-ea115397b4f6")]
[CreateAssetMenu(fileName: "New Int Variable.dataasset", path: "DDD/DataAssetVariable<T>/Int")]
public sealed class DataAssetVariableInt : DataAssetVariable<int>;

/// <summary>A boolean shared variable available in Studio without defining a type in game code.</summary>
[TypeId("48b69ee0-a5b9-43fd-8277-9f974acef47f")]
[CreateAssetMenu(fileName: "New Bool Variable.dataasset", path: "DDD/DataAssetVariable<T>/Bool")]
public sealed class DataAssetVariableBool : DataAssetVariable<bool>;

/// <summary>A shared asset-enum choice, allowing designers to add and select factions without code changes.</summary>
[TypeId("3d7a296d-764a-42ba-9cdb-579ac0636508")]
[CreateAssetMenu(fileName: "New Enum Variable.dataasset", path: "DDD/DataAssetVariable<T>/Asset Enum")]
public sealed class DataAssetVariableAssetEnum : DataAssetVariable<AssetEnumValue>;
