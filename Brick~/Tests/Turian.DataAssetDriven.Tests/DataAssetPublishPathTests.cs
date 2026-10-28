using System.Runtime.CompilerServices;

namespace Turian.Tests;

/// <summary>Instance dispatch preserves snapshot, lifetime, tracing and thread guarantees.</summary>
public sealed class DataAssetPublishPathTests
{
    /// <summary>An exception stops the current snapshot without corrupting subsequent delivery.</summary>
    [Fact]
    public void ExceptionSuppressesLaterListenersAndNextRaiseRecovers()
    {
        using var session = new DataAssetSession();
        var channel = new DataAssetEvent();
        session.Attach(channel);
        var reject = true;
        var later = 0;
        using var first = channel.Subscribe(() =>
        {
            if (reject) { throw new InvalidOperationException("handler"); }
        }, 10);
        using var second = channel.Subscribe(() => later++);
        Assert.Throws<InvalidOperationException>(channel.Raise);
        Assert.Equal(0, later);
        reject = false;
        channel.Raise();
        Assert.Equal(1, later);
    }

    /// <summary>An addition joins nested and subsequent raises while the outer snapshot stays immutable.</summary>
    [Fact]
    public void AdditionJoinsNestedRaiseAndPreservesStablePriorityTies()
    {
        using var session = new DataAssetSession();
        var channel = new DataAssetEvent<int>();
        session.Attach(channel);
        var calls = new List<int>();
        IDisposable? added = null;
        using var first = channel.Subscribe(value =>
        {
            calls.Add(10 + value);
            if (value != 1) { return; }
            added = channel.Subscribe(item => calls.Add(30 + item));
            channel.Raise(2);
        });
        using var second = channel.Subscribe(value => calls.Add(20 + value));
        channel.Raise(1);
        Assert.Equal([11, 12, 22, 32, 21], calls);
        added!.Dispose();
    }

    /// <summary>Ending the session within a listener suppresses pending listeners and queued work.</summary>
    [Fact]
    public void DisposalDuringImmediateRaiseInvalidatesRetainedEndpoint()
    {
        using var session = new DataAssetSession();
        var channel = new DataAssetEvent<int>();
        session.Attach(channel);
        var endpoint = session.Bus.Prepare<int>(channel.Id);
        var later = 0;
        using var first = channel.Subscribe(_ => session.Dispose(), 10);
        using var second = channel.Subscribe(_ => later++);
        Assert.True(channel.TryQueue(9));
        channel.Raise(1);
        Assert.Equal(0, later);
        Assert.Empty(session.Assets);
        Assert.Empty(session.Listeners(channel.Id));
        Assert.False(endpoint.TryQueue(2));
        Assert.Throws<ObjectDisposedException>(() => endpoint.Raise(3));
        Assert.Throws<InvalidOperationException>(() => channel.Raise(4));
        Assert.Throws<ObjectDisposedException>(() => session.Observer = new Recorder());
    }

    /// <summary>A retained asset gets fresh channel state in its next Play session.</summary>
    [Fact]
    public void ReattachmentCannotResurrectOldSubscriptions()
    {
        var channel = new DataAssetEvent<int>();
        var old = 0;
        var current = 0;
        var first = new DataAssetSession();
        first.Attach(channel);
        using var oldHandle = channel.Subscribe(_ => old++);
        var endpoint = first.Bus.Prepare<int>(channel.Id);
        first.Dispose();
        using var second = new DataAssetSession();
        second.Attach(channel);
        using var currentHandle = channel.Subscribe(_ => current++);
        oldHandle.Dispose();
        channel.Raise(1);
        Assert.Equal(0, old);
        Assert.Equal(1, current);
        Assert.Throws<ObjectDisposedException>(() => endpoint.Raise(2));
    }

    /// <summary>Cached and newly hydrated channels follow observer switches and retain listener ownership.</summary>
    [Fact]
    public void LazyTracingUpdatesCachedChannelsAndKeepsSequenceAndOwners()
    {
        using var session = new DataAssetSession();
        var channel = new DataAssetEvent();
        session.Attach(channel);
        var owner = Guid.NewGuid();
        using var handle = channel.Subscribe(static () => { }, priority: 7, owner: owner);
        channel.Raise();
        var first = new Recorder();
        session.Observer = first;
        channel.Raise();
        var next = new DataAssetEvent();
        session.Attach(next);
        next.Raise();
        Assert.Equal(2, first.Count);
        Assert.Equal(channel.Id, first.Entries[0].Topic);
        Assert.Equal(1, first.Entries[0].ListenerCount);
        Assert.True(first.Entries[0].Timestamp > 0);
        Assert.True(first.Entries[1].Timestamp >= first.Entries[0].Timestamp);
        var replacement = new Recorder();
        session.Observer = replacement;
        channel.Raise();
        session.Observer = null;
        channel.Raise();
        session.Observer = replacement;
        channel.Raise();
        Assert.Equal(2, replacement.Count);
        Assert.Equal(7, replacement.Entries[1].Sequence);
        var listener = Assert.Single(session.Listeners(channel.Id));
        Assert.Equal(owner, listener.Owner);
        Assert.Equal(7, listener.Priority);
        Assert.Equal(1, listener.Sequence);
    }

    /// <summary>The same cheap guard detects worker misuse in Debug and Release builds.</summary>
    [Fact]
    public void WorkersCannotMutateSubscriptionsOrObservers()
    {
        using var session = new DataAssetSession();
        var channel = new DataAssetEvent();
        session.Attach(channel);
        using var handle = channel.Subscribe(static () => { });
        Exception? failure = null;
        var worker = new Thread(() =>
        {
            try
            {
                Assert.Throws<InvalidOperationException>(channel.Raise);
                Assert.Throws<InvalidOperationException>(() => channel.Subscribe(static () => { }));
                Assert.Throws<InvalidOperationException>(handle.Dispose);
                Assert.Throws<InvalidOperationException>(() => session.Observer = new Recorder());
            }
            catch (Exception error) { failure = error; }
        });
        worker.Start();
        worker.Join();
        Assert.Null(failure);
        Assert.Single(session.Listeners(channel.Id));
    }

    /// <summary>Forgotten subscription handles and queued reference payloads release their owners on teardown.</summary>
    [Fact]
    public void RetainedHandlesAndAssetsDoNotKeepRuntimeOwnersAlive()
    {
        var session = new DataAssetSession();
        var (channel, handle, owner) = RetainOwner(session);
        session.Dispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(owner.IsAlive);
        Assert.False(channel.IsRuntime);
        handle.Dispose();
        GC.KeepAlive(channel);
        GC.KeepAlive(handle);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (DataAssetEvent<Owned>, IDisposable, WeakReference) RetainOwner(DataAssetSession session)
    {
        var channel = new DataAssetEvent<Owned>();
        session.Attach(channel);
        var receiver = new Receiver();
        var handle = channel.Subscribe(receiver.Receive);
        Assert.True(channel.TryQueue(new Owned(receiver, 1)));
        return (channel, handle, new WeakReference(receiver));
    }

    readonly record struct Owned(object Owner, int Value);

    sealed class Receiver
    {
        internal int Value;
        internal void Receive(Owned payload) => Value += payload.Value;
    }

    sealed class Recorder : IDataAssetEventObserver
    {
        internal readonly DataAssetEventTrace[] Entries = new DataAssetEventTrace[8];
        internal int Count;
        public void OnRaised(in DataAssetEventTrace trace) => Entries[Count++] = trace;
    }
}
