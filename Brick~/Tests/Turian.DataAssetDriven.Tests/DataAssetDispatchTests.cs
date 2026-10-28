namespace Turian.Tests;

/// <summary>Gameplay event identity, ownership, mutation and threading contracts.</summary>
public sealed class DataAssetDispatchTests
{
    /// <summary>Topics isolate assets and deliver higher priority first with stable ties.</summary>
    [Fact]
    public void PriorityIdentityAndDisposal()
    {
        using var session = new DataAssetSession();
        var channel = new DataAssetEvent();
        var other = new DataAssetEvent();
        session.Attach(channel);
        session.Attach(other);
        Assert.Same(channel, session.Find(channel.Id));
        var calls = new List<int>();
        using var last = channel.Subscribe(() => calls.Add(3), -1);
        using var first = channel.Subscribe(() => calls.Add(1), 5);
        using var second = channel.Subscribe(() => calls.Add(2), 5);
        using var ignored = other.Subscribe(() => calls.Add(99));
        channel.Raise();
        Assert.Equal([1, 2, 3], calls);
        second.Dispose();
        calls.Clear();
        channel.Raise();
        Assert.Equal([1, 3], calls);
        Assert.Equal([5, -1], session.Listeners(channel.Id).Select(static entry => entry.Priority));
    }

    /// <summary>Unsubscription suppresses pending delivery and new subscribers join the next raise.</summary>
    [Fact]
    public void MutationDuringRaiseUsesStableSnapshot()
    {
        using var session = new DataAssetSession();
        var channel = new DataAssetEvent();
        session.Attach(channel);
        var calls = new List<int>();
        IDisposable? later = null;
        IDisposable? added = null;
        using var first = channel.Subscribe(() =>
        {
            calls.Add(1);
            later?.Dispose();
            added ??= channel.Subscribe(() => calls.Add(3));
        }, 10);
        later = channel.Subscribe(() => calls.Add(2));
        channel.Raise();
        Assert.Equal([1], calls);
        channel.Raise();
        Assert.Equal([1, 1, 3], calls);
        added!.Dispose();
    }

    /// <summary>Nested delivery and exceptions retain a valid immutable listener snapshot.</summary>
    [Fact]
    public void NestedRaiseAndFailureRecover()
    {
        using var session = new DataAssetSession();
        var channel = new DataAssetEvent<int>();
        session.Attach(channel);
        var calls = new List<int>();
        using var subscription = channel.Subscribe(value =>
        {
            calls.Add(value);
            if (value == 1) { channel.Raise(2); }
            if (value == 3) { throw new InvalidOperationException("listener"); }
        });
        channel.Raise(1);
        Assert.Throws<InvalidOperationException>(() => channel.Raise(3));
        channel.Raise(4);
        Assert.Equal([1, 2, 3, 4], calls);
    }

    /// <summary>Worker callbacks queue bounded typed payloads and cannot invoke listeners directly.</summary>
    [Fact]
    public void WorkersQueueUntilTickBoundary()
    {
        using var session = new DataAssetSession(queueCapacity: 2);
        var channel = new DataAssetEvent<int>();
        session.Attach(channel);
        var values = new List<int>();
        using var subscription = channel.Subscribe(values.Add);
        Exception? failure = null;
        var worker = new Thread(() =>
        {
            try
            {
                Assert.Throws<InvalidOperationException>(() => channel.Raise(99));
                Assert.True(channel.TryQueue(1));
                Assert.True(channel.TryQueue(2));
                Assert.False(channel.TryQueue(3));
            }
            catch (Exception error) { failure = error; }
        });
        worker.Start();
        worker.Join();
        Assert.Null(failure);
        Assert.Empty(values);
        session.Drain();
        Assert.Equal([1, 2], values);
        Assert.True(channel.TryQueue(4));
        session.Drain();
        Assert.Equal([1, 2, 4], values);
    }

    /// <summary>Session teardown clears listener delegates even when gameplay forgets its subscription handle.</summary>
    [Fact]
    public void SessionDisposalClearsLeakedSubscriptionsAndAssets()
    {
        var session = new DataAssetSession();
        var channel = new DataAssetEvent();
        session.Attach(channel);
        var handle = channel.Subscribe(static () => { });
        Assert.Single(session.Listeners(channel.Id));
        session.Dispose();
        Assert.Empty(session.Listeners(channel.Id));
        Assert.Empty(session.Assets);
        Assert.Null(channel.RuntimeSession);
        Assert.Throws<InvalidOperationException>(channel.Raise);
        handle.Dispose();
        session.Dispose();
    }

    /// <summary>Signals and record struct payloads allocate nothing during warmed dispatch, including tracing.</summary>
    [Fact]
    public void RaiseWithTracingAllocatesZeroBytes()
    {
        using var session = new DataAssetSession();
        var channel = new DataAssetEvent<Payload>();
        session.Attach(channel);
        var observer = new Observer();
        session.Observer = observer;
        Assert.Same(observer, session.Observer);
        long sum = 0;
        using var subscription = channel.Subscribe(value => sum += value.Value);
        for (var i = 0; i < 10_000; i++) { channel.Raise(new Payload(1)); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100_000; i++) { channel.Raise(new Payload(1)); }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Equal(110_000, sum);
        Assert.Equal(sum, observer.Count);
    }

    /// <summary>Duplicate runtime identities and cross-session sharing reject before mutation.</summary>
    [Fact]
    public void InvalidOwnershipRejects()
    {
        using var first = new DataAssetSession();
        using var second = new DataAssetSession();
        var asset = new DataAssetEvent();
        first.Attach(asset);
        Assert.Throws<InvalidOperationException>(() => second.Attach(asset));
        Assert.Throws<InvalidOperationException>(() => first.Attach(new DataAssetEvent { Id = asset.Id }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DataAssetSession(0));
    }

    readonly record struct Payload(int Value);

    /// <summary>Stopping Play from a queued listener aborts the remaining drain without indexing cleared topics.</summary>
    [Fact]
    public void SessionCanEndDuringQueuedDelivery()
    {
        using var session = new DataAssetSession();
        var first = new DataAssetEvent();
        var second = new DataAssetEvent();
        session.Attach(first);
        session.Attach(second);
        using var subscription = first.Subscribe(session.Dispose);
        first.TryQueue();
        second.TryQueue();
        session.Drain();
        Assert.Empty(session.Assets);
    }

    sealed class Observer : IDataAssetEventObserver
    {
        public long Count;
        public void OnRaised(in DataAssetEventTrace trace) => Count++;
    }
}
