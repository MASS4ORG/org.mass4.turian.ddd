static class DataAssetEventBenchmarks
{
    public static void Run()
    {
        using var session = new DataAssetSession();
        var channel = new DataAssetEvent<Payload>();
        session.Attach(channel);
        long total = 0;
        using var subscription = channel.Subscribe(payload => total += payload.Value);
        const int iterations = 1_000_000;
        for (var i = 0; i < 100_000; i++) { channel.Raise(new Payload(1)); }
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < iterations; i++) { channel.Raise(new Payload(1)); }
        var elapsed = Stopwatch.GetElapsedTime(start);
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Console.WriteLine($"DataAssetEvent record struct: {allocated} bytes / {iterations} raises; "
                          + $"{elapsed.TotalNanoseconds / iterations:F2} ns/raise; delivered {total}");
        if (allocated != 0 || total != 1_100_000) { throw new InvalidOperationException("Dispatch benchmark failed."); }
    }

    readonly record struct Payload(int Value);
}
