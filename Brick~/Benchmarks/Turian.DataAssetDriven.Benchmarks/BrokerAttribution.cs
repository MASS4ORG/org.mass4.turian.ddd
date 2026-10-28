using Turian.Benchmarks;

static class BrokerAttribution
{
    internal const string SourceHash = "2a76e3bc68893e7cbc92a2a0cf7d218dab6c7fd07d1919946bc1bc94e17f51a1";
    static readonly string[] flags = ["--no-timestamp", "--no-lock", "--no-guards", "--no-priority", "--ref-dispatch"];

    internal static bool Requested(string[] args) => args.Contains("--attribution") || args.Contains("--floor")
        || flags.Any(args.Contains);

    internal static DispatchScenario[] Create(int topics, int listeners, string[] args)
    {
        var mode = args.Contains("--floor") ? 31 : 0;
        for (var i = 0; i < flags.Length; i++) { if (args.Contains(flags[i])) { mode |= 1 << i; } }
        int[] modes = mode == 0 ? [0, 1, 2, 4, 8, 16, 3, 7, 23, 31] : [mode];
        return [new TypedScenario(topics, listeners), new DddScenario(topics, listeners, true, false),
            new DddScenario(topics, listeners, false, false),
            .. modes.Select(value => Create(value, topics, listeners))];
    }

    static DispatchScenario Create(int mode, int topics, int listeners) => mode switch
    {
        0 => new AttributionScenario<AttributionOptions0>("Attribution baseline", topics, listeners),
        1 => new AttributionScenario<AttributionOptions1>("--no-timestamp", topics, listeners),
        2 => new AttributionScenario<AttributionOptions2>("--no-lock", topics, listeners),
        3 => new AttributionScenario<AttributionOptions3>("--no-timestamp + --no-lock", topics, listeners),
        4 => new AttributionScenario<AttributionOptions4>("--no-guards", topics, listeners),
        5 => new AttributionScenario<AttributionOptions5>("--no-timestamp + --no-guards", topics, listeners),
        6 => new AttributionScenario<AttributionOptions6>("--no-lock + --no-guards", topics, listeners),
        7 => new AttributionScenario<AttributionOptions7>("--no-timestamp + --no-lock + --no-guards", topics, listeners),
        8 => new AttributionScenario<AttributionOptions8>("--no-priority", topics, listeners),
        9 => new AttributionScenario<AttributionOptions9>("--no-timestamp + --no-priority", topics, listeners),
        10 => new AttributionScenario<AttributionOptions10>("--no-lock + --no-priority", topics, listeners),
        11 => new AttributionScenario<AttributionOptions11>("--no-timestamp + --no-lock + --no-priority", topics, listeners),
        12 => new AttributionScenario<AttributionOptions12>("--no-guards + --no-priority", topics, listeners),
        13 => new AttributionScenario<AttributionOptions13>("--no-timestamp + --no-guards + --no-priority", topics, listeners),
        14 => new AttributionScenario<AttributionOptions14>("--no-lock + --no-guards + --no-priority", topics, listeners),
        15 => new AttributionScenario<AttributionOptions15>("--no-timestamp + --no-lock + --no-guards + --no-priority", topics, listeners),
        16 => new AttributionScenario<AttributionOptions16>("--ref-dispatch", topics, listeners),
        17 => new AttributionScenario<AttributionOptions17>("--no-timestamp + --ref-dispatch", topics, listeners),
        18 => new AttributionScenario<AttributionOptions18>("--no-lock + --ref-dispatch", topics, listeners),
        19 => new AttributionScenario<AttributionOptions19>("--no-timestamp + --no-lock + --ref-dispatch", topics, listeners),
        20 => new AttributionScenario<AttributionOptions20>("--no-guards + --ref-dispatch", topics, listeners),
        21 => new AttributionScenario<AttributionOptions21>("--no-timestamp + --no-guards + --ref-dispatch", topics, listeners),
        22 => new AttributionScenario<AttributionOptions22>("--no-lock + --no-guards + --ref-dispatch", topics, listeners),
        23 => new AttributionScenario<AttributionOptions23>("--no-timestamp + --no-lock + --no-guards + --ref-dispatch", topics, listeners),
        24 => new AttributionScenario<AttributionOptions24>("--no-priority + --ref-dispatch", topics, listeners),
        25 => new AttributionScenario<AttributionOptions25>("--no-timestamp + --no-priority + --ref-dispatch", topics, listeners),
        26 => new AttributionScenario<AttributionOptions26>("--no-lock + --no-priority + --ref-dispatch", topics, listeners),
        27 => new AttributionScenario<AttributionOptions27>("--no-timestamp + --no-lock + --no-priority + --ref-dispatch", topics, listeners),
        28 => new AttributionScenario<AttributionOptions28>("--no-guards + --no-priority + --ref-dispatch", topics, listeners),
        29 => new AttributionScenario<AttributionOptions29>("--no-timestamp + --no-guards + --no-priority + --ref-dispatch", topics, listeners),
        30 => new AttributionScenario<AttributionOptions30>("--no-lock + --no-guards + --no-priority + --ref-dispatch", topics, listeners),
        31 => new AttributionScenario<AttributionOptions31>("--floor", topics, listeners),
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };
}

sealed class AttributionScenario<TOptions> : DispatchScenario where TOptions : struct, IAttributionOptions
{
    readonly DataAssetSession session = new();
    readonly AttributionEventBus<TOptions> bus;
    readonly AttributionAsset<TOptions>[] channels;

    internal AttributionScenario(string name, int topics, int listeners) : base(name, topics, listeners)
    {
        bus = new AttributionEventBus<TOptions>(256, session.EnsureMainThread);
        channels = Keys.Select(key => new AttributionAsset<TOptions>(bus, key)).ToArray();
        for (var t = 0; t < topics; t++)
        {
            foreach (var receiver in Receivers[t])
            {
                Subscriptions.Add(bus.Subscribe<DispatchPayload>(Keys[t], receiver.Receive, 0, Guid.Empty));
            }
        }
    }

    public override void Publish(int count)
    {
        for (var i = 0; i < count; i++) { channels[i & (Keys.Length - 1)].Raise(new DispatchPayload(1)); }
    }

    public override void Dispose()
    {
        base.Dispose();
        bus.Dispose();
        session.Dispose();
    }
}

sealed class AttributionAsset<TOptions> : DataAsset where TOptions : struct, IAttributionOptions
{
    readonly AttributionEventBus<TOptions>.Topic<DispatchPayload> reference;
    AttributionEventBus<TOptions>? runtime;

    internal AttributionAsset(AttributionEventBus<TOptions> bus, Guid id)
    {
        Id = id;
        runtime = bus;
        reference = bus.Reference<DispatchPayload>(id);
    }

    internal void Raise(DispatchPayload payload)
    {
        var bus = TOptions.Guards ? runtime ?? throw new InvalidOperationException("No runtime session") : runtime!;
        if (TOptions.Reference) { bus.RaiseReference(reference, Id, payload); }
        else { bus.Raise(Id, payload); }
    }
}
