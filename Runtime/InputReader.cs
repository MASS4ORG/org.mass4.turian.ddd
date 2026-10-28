using System.Numerics;

namespace Turian.Engine.Core;

/// <summary>Wraps an input action as change-driven gameplay channels and shared held values.</summary>
[TypeId("f755f7e8-b313-4d47-94a4-083d68a580d8")]
[CreateAssetMenu(fileName: "New Input Reader.dataasset", path: "DDD/InputReader")]
public sealed class InputReader : DataAsset
{
    /// <summary>The input maps containing the selected action.</summary>
    public InputActionsAsset? Actions { get; set; }

    /// <summary>The data path selecting an action; never a method dispatch name.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Channels raised once when the sampled action becomes pressed.</summary>
    public List<DataAssetEvent> OnPressed { get; set; } = [];

    /// <summary>Channels raised once when the sampled action becomes released.</summary>
    public List<DataAssetEvent> OnReleased { get; set; } = [];

    /// <summary>The optional shared held-state variable, updated before edge delivery.</summary>
    public DataAssetVariableBool? Held { get; set; }

    /// <summary>The optional scalar variable; unchanged samples do not publish changes.</summary>
    public DataAssetVariableFloat? AxisValue { get; set; }

    /// <summary>The optional two-axis variable for actions such as movement.</summary>
    public DataAssetVariableVector2? VectorValue { get; set; }

    /// <summary>The selected action's current held state.</summary>
    [JsonIgnore]
    public bool IsPressed => Resolve(State())?.IsPressed ?? false;

    /// <summary>The selected action's current scalar value.</summary>
    [JsonIgnore]
    public float Axis => Resolve(State())?.ReadAxis() ?? 0;

    /// <summary>The selected action's current vector value.</summary>
    [JsonIgnore]
    public Vector2 Vector => Resolve(State())?.ReadVector() ?? Vector2.Zero;

    /// <summary>Publishes an already sampled action; repeated samples produce no duplicate edges or value changes.</summary>
    public void Publish() => Publish(Resolve(State()), State());

    internal void Poll(IInputSource source)
    {
        var state = State();
        var action = Resolve(state);
        if (state.Map?.Enabled == false) { action?.Reset(); }
        else { action?.Update(source); }
        Publish(action, state);
    }

    void Publish(InputAction? action, ReaderState state)
    {
        var pressed = action?.IsPressed ?? false;
        PublishValues(action, pressed);
        if (state.Pressed == pressed) { return; }
        state.Pressed = pressed;
        foreach (var channel in pressed ? OnPressed : OnReleased) { channel.Raise(); }
    }

    void PublishValues(InputAction? action, bool pressed)
    {
        if (Held is not null) { Held.Value = pressed; }
        if (AxisValue is not null) { AxisValue.Value = action?.ReadAxis() ?? 0; }
        if (VectorValue is not null) { VectorValue.Value = action?.ReadVector() ?? Vector2.Zero; }
    }

    ReaderState State() => RequireRuntimeSession().State(this, static _ => new ReaderState());

    InputAction? Resolve(ReaderState state)
    {
        if (!ReferenceEquals(state.Asset, Actions) || state.Path != Action)
        {
            state.Asset = Actions;
            state.Path = Action;
            state.Action = Actions?.Find(Action);
            state.Map = null;
            if (Actions is not null && state.Action is not null)
            {
                foreach (var map in Actions.Maps)
                {
                    if (map.Actions.Contains(state.Action)) { state.Map = map; break; }
                }
            }
        }
        return state.Action;
    }

    sealed class ReaderState
    {
        internal InputActionsAsset? Asset;
        internal string? Path;
        internal InputAction? Action;
        internal InputActionMap? Map;
        internal bool Pressed;
    }
}

/// <summary>Samples designer-assigned readers at the fixed-tick boundary, with change-only gameplay delivery.</summary>
[TypeId("cc71344c-a83b-4893-aa9f-b4db7cc94430")]
[ComponentContextMenu("DataAsset/Input Reader")]
public sealed class InputReaderComponent : Component
{
    /// <summary>The designer-assigned action wrappers.</summary>
    public List<InputReader> Readers { get; set; } = [];

    /// <summary>The engine's input snapshot; optional for tests and externally sampled actions.</summary>
    [InjectService(Optional = true), JsonIgnore, Hide]
    public IInputSource? Input { get; set; }

    /// <inheritdoc />
    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        foreach (var reader in Readers)
        {
            if (!reader.IsRuntime) { continue; }
            if (Input is null) { reader.Publish(); }
            else { reader.Poll(Input); }
        }
    }
}
