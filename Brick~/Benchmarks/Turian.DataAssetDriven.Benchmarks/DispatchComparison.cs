using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

static class DispatchComparison
{
    const int iterations = 1_000_000;
    const int rounds = 11;

    public static void Run(string[] args)
    {
        var results = new List<Result>();
        Console.WriteLine($"{RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription}; "
            + $"{RuntimeInformation.ProcessArchitecture}; tiering={Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "default"}");
        Console.WriteLine($"{iterations:N0} raises/sample; {rounds} shuffled rounds; four-byte record struct; setup excluded");
        var configurations = new[] { (1, 0), (1, 1), (1, 8), (1, 32), (1024, 1) };
        var caseIndex = Array.IndexOf(args, "--case");
        if (caseIndex >= 0)
        {
            configurations = configurations.Where(item => $"{item.Item1}:{item.Item2}" == args[caseIndex + 1]).ToArray();
            if (configurations.Length != 1) { throw new ArgumentException("Unknown topics:listeners case"); }
        }
        var warmupIndex = Array.IndexOf(args, "--warmup-ms");
        var warmupMs = warmupIndex >= 0 ? int.Parse(args[warmupIndex + 1], CultureInfo.InvariantCulture) : 0;
        foreach (var (topics, listeners) in configurations)
        {
            Measure(topics, listeners, results, warmupMs, args);
        }
        var pathIndex = Array.IndexOf(args, "--output");
        if (pathIndex >= 0)
        {
            var path = Path.GetFullPath(args[pathIndex + 1]);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(new Report(DateTimeOffset.UtcNow, args, BrokerAttribution.SourceHash,
                RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription,
                RuntimeInformation.ProcessArchitecture.ToString(), iterations, rounds, results),
                new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Results: {path}");
        }
    }

    static void Measure(int topics, int listeners, List<Result> results, int warmupMs, string[] args)
    {
        var scenarios = BrokerAttribution.Requested(args) ? BrokerAttribution.Create(topics, listeners, args) : new DispatchScenario[]
        {
            new DirectScenario(topics, listeners),
            new EventScenario(topics, listeners),
            new TypedScenario(topics, listeners),
            new KeyedScenario(topics, listeners),
            new MessagePipeScenario(topics, listeners, keyed: false, traced: false),
            new MessagePipeScenario(topics, listeners, keyed: true, traced: false),
            new MessagePipeScenario(topics, listeners, keyed: true, traced: true),
            new DddScenario(topics, listeners, assets: false, traced: false),
            new DddScenario(topics, listeners, assets: true, traced: false),
            new DddScenario(topics, listeners, assets: true, traced: true, timestampOnly: true),
            new DddScenario(topics, listeners, assets: true, traced: true),
        };
        try
        {
            foreach (var scenario in scenarios)
            {
                for (var i = 0; i < 5; i++) { CheckedBatch(scenario, 100_000); }
                var start = Stopwatch.GetTimestamp();
                while (Stopwatch.GetElapsedTime(start).TotalMilliseconds < warmupMs)
                {
                    CheckedBatch(scenario, 100_000);
                }
            }
            var samples = scenarios.Select(_ => new List<double>()).ToArray();
            var allocations = new long[scenarios.Length];
            var random = new Random(55);
            var order = Enumerable.Range(0, scenarios.Length).ToArray();
            for (var round = 0; round < rounds; round++)
            {
                random.Shuffle(order);
                foreach (var index in order)
                {
                    var scenario = scenarios[index];
                    var before = scenario.Received;
                    var bytes = GC.GetAllocatedBytesForCurrentThread();
                    var start = Stopwatch.GetTimestamp();
                    scenario.Publish(iterations);
                    var end = Stopwatch.GetTimestamp();
                    bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
                    Verify(scenario, iterations, before);
                    samples[index].Add((end - start) * (1_000_000_000d / Stopwatch.Frequency) / iterations);
                    allocations[index] = Math.Max(allocations[index], bytes);
                }
            }
            Console.WriteLine($"Topics={topics}; listeners/topic={listeners}");
            for (var i = 0; i < scenarios.Length; i++)
            {
                samples[i].Sort();
                var result = new Result(scenarios[i].Name, topics, listeners, samples[i][rounds / 2],
                    samples[i][1], samples[i][rounds - 2], allocations[i], samples[i].ToArray());
                results.Add(result);
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"  {result.Name,-27} {result.MedianNs,9:F2} ns  p10-p90 {result.P10Ns:F2}-{result.P90Ns:F2}; {result.MaxAllocatedBytes} bytes/sample"));
                if (allocations[i] != 0) { throw new InvalidOperationException($"{result.Name} allocated during publish"); }
            }
        }
        finally
        {
            foreach (var scenario in scenarios) { scenario.Dispose(); }
        }
    }

    static void CheckedBatch(DispatchScenario scenario, int count)
    {
        var before = scenario.Received;
        scenario.Publish(count);
        Verify(scenario, count, before);
    }

    static void Verify(DispatchScenario scenario, int count, long before)
    {
        if (scenario.Received - before != (long)count * scenario.ListenerCount)
        {
            throw new InvalidOperationException($"{scenario.Name} dropped or duplicated a delivery");
        }
        scenario.VerifyTrace();
    }

    sealed record Result(string Name, int Topics, int Listeners, double MedianNs, double P10Ns,
        double P90Ns, long MaxAllocatedBytes, double[] SamplesNs);

    sealed record Report(DateTimeOffset Timestamp, string[] Arguments, string AttributionSourceHash, string Runtime, string OperatingSystem, string Architecture,
        int Iterations, int Rounds, List<Result> Results);
}
