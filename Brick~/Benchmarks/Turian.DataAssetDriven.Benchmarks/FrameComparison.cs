using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;

static class FrameComparison
{
    const int frames = 120;
    const int rounds = 11;

    internal static void Run(string[] args)
    {
        var results = new List<Result>();
        var cases = new List<FrameCase> { new(0, FrameDispatch.Direct) };
        foreach (var count in new[] { 1000, 10000 })
        {
            foreach (var mode in Enum.GetValues<FrameDispatch>()) { cases.Add(new FrameCase(count, mode)); }
        }
        try
        {
            foreach (var item in cases) { item.Run(frames); }
            var ambient = GC.GetAllocatedBytesForCurrentThread();
            cases[0].Run(frames);
            ambient = GC.GetAllocatedBytesForCurrentThread() - ambient;
            var samples = cases.Select(_ => new List<double>()).ToArray();
            var maximumAllocations = new long[cases.Count];
            var order = Enumerable.Range(0, cases.Count).ToArray();
            var random = new Random(55);
            for (var round = 0; round < rounds; round++)
            {
                random.Shuffle(order);
                foreach (var index in order)
                {
                    var item = cases[index];
                    var delivered = item.World.Delivered;
                    var allocated = GC.GetAllocatedBytesForCurrentThread();
                    var start = Stopwatch.GetTimestamp();
                    item.Run(frames);
                    var elapsed = Stopwatch.GetTimestamp() - start;
                    allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
                    if (allocated != ambient) { throw new InvalidOperationException($"Unexpected frame-loop allocation: {allocated}"); }
                    if (item.World.Delivered - delivered != (long)item.Raises * frames)
                    {
                        throw new InvalidOperationException("Frame benchmark missed a gameplay delivery");
                    }
                    if (!float.IsFinite(item.World.Checksum)) { throw new InvalidOperationException("Non-finite gameplay state"); }
                    samples[index].Add(elapsed * 1000d / Stopwatch.Frequency / frames);
                    maximumAllocations[index] = Math.Max(maximumAllocations[index], allocated);
                }
            }
            for (var i = 0; i < cases.Count; i++)
            {
                samples[i].Sort();
                var item = cases[i];
                var result = new Result(item.Mode.ToString(), item.Raises, samples[i][rounds / 2],
                    samples[i][1], samples[i][rounds - 2], maximumAllocations[i], samples[i].ToArray());
                results.Add(result);
                Console.WriteLine($"{result.Mode} N={result.Raises}: {result.MedianMs:F6} ms/frame "
                    + $"p10-p90 {result.P10Ms:F6}-{result.P90Ms:F6}; {result.MaxAllocatedBytes / frames} ambient bytes/frame; 0 publish-loop bytes");
            }
            var outputIndex = Array.IndexOf(args, "--output");
            if (outputIndex >= 0)
            {
                var path = Path.GetFullPath(args[outputIndex + 1]);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(new Report(DateTimeOffset.UtcNow,
                    Environment.Version.ToString(), Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
                    frames, rounds, results), new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"Results: {path}");
            }
        }
        finally { foreach (var item in cases) { item.Dispose(); } }
    }

    sealed record Result(string Mode, int Raises, double MedianMs, double P10Ms, double P90Ms,
        long MaxAllocatedBytes, double[] SamplesMs);
    sealed record Report(DateTimeOffset Timestamp, string Runtime, string? Tiering, int FramesPerSample,
        int Rounds, List<Result> Results);

    enum FrameDispatch { Direct, CSharpEvent, DataAsset }

    readonly record struct Hit(int Subject, float Damage, Vector3 Impulse);

    sealed class World
    {
        readonly Entity[] entities = Enumerable.Range(0, 512).Select(_ => new Entity { Health = 100 }).ToArray();
        internal long Delivered;
        internal int Knockouts;
        internal float Checksum => entities.Sum(static entity => entity.Position.LengthSquared() + entity.Health);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Receive(Hit hit)
        {
            ref var entity = ref entities[hit.Subject];
            entity.Velocity = (entity.Velocity + hit.Impulse) * 0.98f;
            entity.Position += entity.Velocity * (1f / 60);
            entity.Health = MathF.Max(0, entity.Health - hit.Damage);
            if (entity.Health == 0) { entity.Health = 100; Knockouts++; }
            entity.Dirty = true;
            Delivered++;
        }

        struct Entity
        {
            internal Vector3 Position;
            internal Vector3 Velocity;
            internal float Health;
            internal bool Dirty;
        }
    }

    sealed class Source
    {
        internal event Action<Hit>? Raised;
        internal void Raise(Hit hit) => Raised?.Invoke(hit);
    }

    sealed class Emitter(int count, Action<Hit> raise) : IFixedTickSystem
    {
        public void FixedTick(long tick, double deltaTime)
        {
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < count; i++)
            {
                raise(new Hit((int)((i * 17L + tick * 31) & 511), 0.25f, new Vector3(0.02f, -0.01f, 0.03f)));
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            if (allocated != 0) { throw new InvalidOperationException($"Publish loop allocated {allocated} bytes"); }
        }
        public void AfterFixedTicks(double alpha) { }
    }

    sealed class FrameCase : IDisposable
    {
        readonly DataAssetSession session = new();
        readonly IDisposable? subscription;
        readonly SceneTicker ticker;
        internal readonly World World = new();
        internal readonly FrameDispatch Mode;
        internal readonly int Raises;

        internal FrameCase(int raises, FrameDispatch mode)
        {
            Raises = raises;
            Mode = mode;
            Action<Hit> publish;
            if (mode == FrameDispatch.DataAsset)
            {
                var channel = new DataAssetEvent<Hit>();
                session.Attach(channel);
                subscription = channel.Subscribe(World.Receive);
                publish = channel.Raise;
            }
            else if (mode == FrameDispatch.CSharpEvent)
            {
                var source = new Source();
                source.Raised += World.Receive;
                publish = source.Raise;
            }
            else { publish = World.Receive; }
            ticker = new SceneTicker(new SceneManager(new AssetDatabase()))
            {
                DataAssets = session,
                Systems = [new Emitter(raises, publish)],
            };
        }

        internal void Run(int count)
        {
            for (var i = 0; i < count; i++) { ticker.StepFrame(1d / 60); }
        }

        public void Dispose()
        {
            subscription?.Dispose();
            session.Dispose();
            ticker.Async.Dispose();
        }
    }
}
