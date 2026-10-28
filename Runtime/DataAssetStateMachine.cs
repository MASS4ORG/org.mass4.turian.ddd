namespace Turian.Engine.Core;

/// <summary>An extensible asset-enum value that designers can use for states, factions and damage kinds.</summary>
[TypeId("6617ed3a-425c-44b2-9553-73fc0c209b62")]
[CreateAssetMenu(fileName: "New Asset Enum.dataasset", path: "DDD/Asset Enum")]
public sealed class AssetEnumValue : EnumValueAsset;

/// <summary>A designer-authored transition selected by an event asset and asset-enum state identities.</summary>
[TypeId("c67a5baf-e974-490e-917c-4315418e2736")]
[CreateAssetMenu(fileName: "New State Transition.dataasset", path: "DDD/State Transition")]
public sealed class DataAssetStateTransition : DataAsset
{
    /// <summary>The source state; null permits any state.</summary>
    public AssetEnumValue? From { get; set; }

    /// <summary>The state entered when this transition fires.</summary>
    public AssetEnumValue? To { get; set; }

    /// <summary>The gameplay event which requests this transition.</summary>
    public DataAssetEvent? Trigger { get; set; }

    /// <summary>Responses invoked after entering the new state.</summary>
    public List<DataAssetResponse> Responses { get; set; } = [];
}

/// <summary>Exposes an FSM through asset-enum states and event-driven transition definitions.</summary>
[TypeId("2ae8e9a6-c19b-4699-a57f-0cc0fdaee087")]
[ComponentContextMenu("DataAsset/State Machine")]
public sealed class DataAssetStateMachineComponent : Component
{
    readonly List<IDisposable> subscriptions = [];
    bool transitioning;

    /// <summary>The state entered on first activation.</summary>
    public AssetEnumValue? InitialState { get; set; }

    /// <summary>The transitions in tie-breaking order; at most one transition runs for each trigger raise.</summary>
    public List<DataAssetStateTransition> Transitions { get; set; } = [];

    /// <summary>The current runtime state, excluded from scene and asset serialization.</summary>
    [JsonIgnore]
    public AssetEnumValue? CurrentState { get; private set; }

    /// <inheritdoc />
    public override void OnEnable()
    {
        OnDisable();
        CurrentState ??= InitialState;
        var triggers = new HashSet<Guid>();
        try
        {
            foreach (var transition in Transitions)
            {
                if (transition.Trigger is { IsRuntime: true } trigger && triggers.Add(trigger.Id))
                {
                    subscriptions.Add(trigger.Subscribe(() => Move(trigger.Id), owner: Id));
                }
            }
        }
        catch { OnDisable(); throw; }
    }

    /// <inheritdoc />
    public override void OnDisable()
    {
        foreach (var handle in subscriptions) { handle.Dispose(); }
        subscriptions.Clear();
    }

    void Move(Guid trigger)
    {
        if (transitioning) { return; }
        foreach (var transition in Transitions)
        {
            if (!Matches(transition, trigger)) { continue; }
            Take(transition);
            break;
        }
    }

    bool Matches(DataAssetStateTransition transition, Guid trigger)
    {
        if (transition.Trigger is not { } source || source.Id != trigger || transition.To is null) { return false; }
        return transition.From is not { } from || from.Id == CurrentState?.Id;
    }

    void Take(DataAssetStateTransition transition)
    {
        transitioning = true;
        try
        {
            CurrentState = transition.To;
            foreach (var response in transition.Responses) { response.Execute(); }
        }
        finally { transitioning = false; }
    }
}
