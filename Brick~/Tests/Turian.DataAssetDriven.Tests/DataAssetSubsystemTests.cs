namespace Turian.Tests;

/// <summary>Designer-facing subsystem wrappers preserve event ownership and asset-enum transitions.</summary>
public sealed class DataAssetSubsystemTests
{
    /// <summary>Input readers forward only press/release edges and leave neutral input when the session ends.</summary>
    [Fact]
    public void InputReaderPublishesEdgesAndResets()
    {
        var input = new BufferedInputSource();
        var maps = InputActionsAsset.CreateDefault();
        var reader = new InputReader { Actions = maps, Action = "Gameplay/Jump" };
        var pressed = new DataAssetEvent();
        var released = new DataAssetEvent();
        reader.OnPressed = [pressed];
        reader.OnReleased = [released];
        var presses = 0;
        var releases = 0;
        using (var session = new DataAssetSession())
        {
            foreach (var asset in new DataAsset[] { maps, reader, pressed, released }) { session.Attach(asset); }
            using var down = pressed.Subscribe(() => presses++);
            using var up = released.Subscribe(() => releases++);
            var service = new InputActionService(input);
            service.Load(maps);
            var component = new InputReaderComponent { Readers = [reader] };
            input.PushKeyDown(Key.Space);
            service.Update();
            component.OnFixedUpdate(0);
            Assert.True(reader.IsPressed);
            Assert.Equal(1, reader.Axis);
            service.Update();
            component.OnFixedUpdate(0);
            Assert.Equal(1, presses);
            input.PushKeyUp(Key.Space);
            service.Update();
            component.OnFixedUpdate(0);
            Assert.Equal(1, releases);
            reader.Action = "Missing";
            Assert.False(reader.IsPressed);
            reader.Action = "Gameplay/Jump";
            input.PushKeyDown(Key.Space);
            service.Update();
            Assert.True(reader.IsPressed);
        }
        Assert.False(maps.Find("Gameplay/Jump")!.IsPressed);
    }

    /// <summary>Shared trigger assets select exactly one authored FSM transition in list order.</summary>
    [Fact]
    public void StateMachineUsesAuthoredTransitionOrder()
    {
        using var session = new DataAssetSession();
        var trigger = new DataAssetEvent();
        session.Attach(trigger);
        var idle = new AssetEnumValue { Name = "Idle" };
        var running = new AssetEnumValue { Name = "Running" };
        var completed = new AssetEnumValue { Name = "Completed" };
        var machine = new DataAssetStateMachineComponent
        {
            InitialState = idle,
            Transitions =
            [
                new DataAssetStateTransition { Trigger = trigger, From = idle, To = running },
                new DataAssetStateTransition { Trigger = trigger, From = running, To = completed }
            ]
        };
        machine.OnEnable();
        Assert.Single(session.Listeners(trigger.Id));
        trigger.Raise();
        Assert.Same(running, machine.CurrentState);
        trigger.Raise();
        Assert.Same(completed, machine.CurrentState);
        machine.OnDisable();
        Assert.Empty(session.Listeners(trigger.Id));
    }

    /// <summary>Event components tolerate authored editor assets before a runtime session exists.</summary>
    [Fact]
    public void EditorActivationDoesNotSubscribeAuthoredAssets()
    {
        var channel = new DataAssetEvent();
        var listener = new DataAssetListenerComponent { Event = channel };
        listener.OnEnable();
        listener.OnDisable();
        var machine = new DataAssetStateMachineComponent
        {
            Transitions = [new DataAssetStateTransition { Trigger = channel }]
        };
        machine.OnEnable();
        machine.OnDisable();
        Assert.False(channel.IsRuntime);
    }

    /// <summary>Constants cannot be mutated while hydrated, and scalar UI writes reject mismatched payloads.</summary>
    [Fact]
    public void ConstantsAndVariableTypeChecks()
    {
        using var session = new DataAssetSession();
        var constant = new DataAssetConstInt { Value = 7 };
        var variable = new DataAssetVariableInt();
        session.Attach(constant);
        session.Attach(variable);
        Assert.Throws<InvalidOperationException>(() => constant.Value = 8);
        Assert.Equal(7, constant.Value);
        var adapter = (IDataAssetValue)variable;
        Assert.False(adapter.TryWriteValue("4"));
        Assert.True(adapter.TryWriteValue(4));
        Assert.Equal(4, adapter.ReadValue());
    }
}
