using System.Runtime.CompilerServices;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

readonly record struct DispatchPayload(int Value);

sealed class DispatchReceiver
{
    internal long Total;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Receive(DispatchPayload payload) => Total += payload.Value;
}

abstract class DispatchScenario(string name, int topics, int listeners) : IDisposable
{
    protected readonly Guid[] Keys = Enumerable.Range(0, topics).Select(_ => Guid.NewGuid()).ToArray();
    protected readonly DispatchReceiver[][] Receivers = Enumerable.Range(0, topics)
        .Select(_ => Enumerable.Range(0, listeners).Select(_ => new DispatchReceiver()).ToArray()).ToArray();
    protected readonly List<IDisposable> Subscriptions = [];
    public string Name => name;
    public int ListenerCount => listeners;
    public long Received => Receivers.Sum(topic => topic.Sum(receiver => receiver.Total));
    public abstract void Publish(int count);
    public virtual void VerifyTrace() { }
    public virtual void Dispose()
    {
        foreach (var subscription in Subscriptions) { subscription.Dispose(); }
    }
}

sealed class DirectScenario(int topics, int listeners) : DispatchScenario("Direct method calls", topics, listeners)
{
    public override void Publish(int count)
    {
        for (var i = 0; i < count; i++)
        {
            foreach (var receiver in Receivers[i & (Keys.Length - 1)]) { receiver.Receive(new DispatchPayload(1)); }
        }
    }
}

sealed class EventSource
{
    internal event Action<DispatchPayload>? Raised;
    internal void Publish(DispatchPayload payload) => Raised?.Invoke(payload);
}

sealed class EventScenario : DispatchScenario
{
    readonly EventSource[] sources;

    internal EventScenario(int topics, int listeners) : base("C# events", topics, listeners)
    {
        sources = Keys.Select(_ => new EventSource()).ToArray();
        for (var t = 0; t < topics; t++)
        {
            foreach (var receiver in Receivers[t]) { sources[t].Raised += receiver.Receive; }
        }
    }

    public override void Publish(int count)
    {
        for (var i = 0; i < count; i++) { sources[i & (Keys.Length - 1)].Publish(new DispatchPayload(1)); }
    }
}

sealed class TypedTopic(Action<DispatchPayload>[] handlers)
{
    internal void Publish(DispatchPayload payload)
    {
        foreach (var handler in handlers) { handler(payload); }
    }
}

sealed class TypedScenario : DispatchScenario
{
    readonly TypedTopic[] sources;

    internal TypedScenario(int topics, int listeners) : base("Typed pub/sub", topics, listeners) =>
        sources = Receivers.Select(topic => new TypedTopic(topic.Select(receiver =>
            new Action<DispatchPayload>(receiver.Receive)).ToArray())).ToArray();

    public override void Publish(int count)
    {
        for (var i = 0; i < count; i++) { sources[i & (Keys.Length - 1)].Publish(new DispatchPayload(1)); }
    }
}

sealed class KeyedScenario : DispatchScenario
{
    readonly Dictionary<Guid, TypedTopic> sources;

    internal KeyedScenario(int topics, int listeners) : base("GUID pub/sub", topics, listeners) =>
        sources = Keys.Select((key, t) => (key, topic: new TypedTopic(Receivers[t].Select(receiver =>
            new Action<DispatchPayload>(receiver.Receive)).ToArray()))).ToDictionary(item => item.key, item => item.topic);

    public override void Publish(int count)
    {
        for (var i = 0; i < count; i++) { sources[Keys[i & (Keys.Length - 1)]].Publish(new DispatchPayload(1)); }
    }
}

sealed class MessagePipeScenario : DispatchScenario
{
    readonly ServiceProvider provider;
    readonly IPublisher<DispatchPayload>[] keyless;
    readonly IPublisher<Guid, DispatchPayload> publisher;
    readonly DispatchTraceFilter filter = new();
    readonly bool keyed;
    readonly bool traced;

    internal MessagePipeScenario(int topics, int listeners, bool keyed, bool traced)
        : base(keyed ? traced ? "MessagePipe GUID + filter" : "MessagePipe GUID" : "MessagePipe keyless", topics, listeners)
    {
        this.keyed = keyed;
        this.traced = traced;
        var services = new ServiceCollection();
        services.AddMessagePipe(options => options.EnableAutoRegistration = false);
        provider = services.BuildServiceProvider();
        publisher = provider.GetRequiredService<IPublisher<Guid, DispatchPayload>>();
        var subscriber = provider.GetRequiredService<ISubscriber<Guid, DispatchPayload>>();
        keyless = new IPublisher<DispatchPayload>[topics];
        var options = provider.GetRequiredService<MessagePipeOptions>();
        for (var t = 0; t < topics; t++)
        {
            var broker = new MessageBroker<DispatchPayload>(new MessageBrokerCore<DispatchPayload>(
                provider.GetRequiredService<MessagePipeDiagnosticsInfo>(), options),
                provider.GetRequiredService<FilterAttachedMessageHandlerFactory>());
            keyless[t] = broker;
            foreach (var receiver in Receivers[t])
            {
                Subscriptions.Add(keyed
                    ? traced ? subscriber.Subscribe(Keys[t], receiver.Receive, filter)
                        : subscriber.Subscribe(Keys[t], receiver.Receive)
                    : broker.Subscribe(receiver.Receive));
            }
        }
    }

    public override void Publish(int count)
    {
        if (keyed)
        {
            for (var i = 0; i < count; i++) { publisher.Publish(Keys[i & (Keys.Length - 1)], new DispatchPayload(1)); }
        }
        else
        {
            for (var i = 0; i < count; i++) { keyless[i & (Keys.Length - 1)].Publish(new DispatchPayload(1)); }
        }
    }

    public override void VerifyTrace()
    {
        if (traced && filter.Count != Received) { throw new InvalidOperationException("MessagePipe filter missed a delivery"); }
    }

    public override void Dispose()
    {
        base.Dispose();
        provider.Dispose();
    }

    sealed class DispatchTraceFilter : MessageHandlerFilter<DispatchPayload>
    {
        internal long Count;
        public override void Handle(DispatchPayload message, Action<DispatchPayload> next)
        {
            Count++;
            next(message);
        }
    }
}

sealed class DddScenario : DispatchScenario
{
    readonly DataAssetSession session = new();
    readonly DataAssetEvent<DispatchPayload>[] channels;
    readonly DataAssetEventChannel<DispatchPayload>[] endpoints;
    readonly bool assets;
    readonly bool traced;
    readonly DispatchObserver observer = new();
    readonly ClockObserver clockObserver = new();
    readonly bool timestampOnly;
    long published;

    internal DddScenario(int topics, int listeners, bool assets, bool traced, bool timestampOnly = false)
        : base(assets ? traced ? timestampOnly ? "DDD DataAsset + clock" : "DDD DataAsset + trace" : "DDD DataAsset" : "DDD broker (no DataAsset)", topics, listeners)
    {
        this.assets = assets;
        this.traced = traced;
        this.timestampOnly = timestampOnly;
        channels = assets ? Keys.Select(key => new DataAssetEvent<DispatchPayload> { Id = key }).ToArray() : [];
        endpoints = new DataAssetEventChannel<DispatchPayload>[topics];
        if (traced) { session.Observer = timestampOnly ? clockObserver : observer; }
        for (var t = 0; t < topics; t++)
        {
            if (assets) { session.Attach(channels[t]); }
            else { endpoints[t] = session.Bus.Prepare<DispatchPayload>(Keys[t]); }
            foreach (var receiver in Receivers[t])
            {
                Subscriptions.Add(assets ? channels[t].Subscribe(receiver.Receive)
                    : endpoints[t].Subscribe(receiver.Receive, 0, Guid.Empty));
            }
        }
    }

    public override void Publish(int count)
    {
        if (assets)
        {
            for (var i = 0; i < count; i++) { channels[i & (Keys.Length - 1)].Raise(new DispatchPayload(1)); }
        }
        else
        {
            for (var i = 0; i < count; i++) { endpoints[i & (Keys.Length - 1)].Raise(new DispatchPayload(1)); }
        }
        published += count;
    }

    public override void VerifyTrace()
    {
        if (traced && (timestampOnly ? clockObserver.Count : observer.Count) != published) { throw new InvalidOperationException("DDD observer missed a raise"); }
    }

    public override void Dispose()
    {
        base.Dispose();
        session.Dispose();
    }

    sealed class ClockObserver : IDataAssetEventObserver
    {
        internal long Count;
        public void OnRaised(in DataAssetEventTrace trace)
        {
            if (trace.Timestamp <= 0) { throw new InvalidOperationException("Traced raise has no timestamp"); }
            Count++;
        }
    }

    sealed class DispatchObserver : IDataAssetEventObserver
    {
        readonly DataAssetEventTrace[] entries = new DataAssetEventTrace[1024];
        internal long Count;
        public void OnRaised(in DataAssetEventTrace trace)
        {
            entries[(int)(Count++ & 1023)] = trace;
        }
    }
}
