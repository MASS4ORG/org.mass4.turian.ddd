namespace Turian.Tests;

/// <summary>A failed Play activation still disposes all runtime asset state.</summary>
[Collection(SerialTests.Name)]
public sealed class DataAssetPlayLifecycleTests
{
    /// <summary>Assets attached before a component throws during Awake are detached from the abandoned session.</summary>
    [Fact]
    public void FailedAwakeDisposesDataAssetSession()
    {
        var root = new Node();
        root.Components.Add(new DataAssetFailingAwake());
        var host = Substitute.For<IPlaySceneHost>();
        host.CurrentSceneRoot.Returns(root);
        using var editor = new ServiceCollection().BuildServiceProvider();
        var play = new PlayModeService(host, new AssetDatabase(), editor, NullLogger.Instance);
        DataAssetFailingAwake.Asset = new DataAssetVariableInt { DefaultValue = 4 };
        try
        {
            Assert.False(play.Start());
            Assert.Null(play.DataAssets);
            Assert.False(DataAssetFailingAwake.Asset.IsRuntime);
            Assert.Equal(4, DataAssetFailingAwake.Asset.Value);
        }
        finally { DataAssetFailingAwake.Asset = null; }
    }
}

/// <summary>A component that rejects scene activation after attaching a runtime asset.</summary>
[TypeId("66e1810f-178c-47a4-bf08-116f25d4472c")]
public sealed class DataAssetFailingAwake : Component
{
    /// <summary>The retained runtime asset used to inspect failed-start cleanup.</summary>
    public static DataAssetVariableInt? Asset { get; set; }

    /// <inheritdoc />
    public override void OnAwake()
    {
        var session = Services!.GetRequiredService<DataAssetSession>();
        session.Attach(Asset!);
        Asset!.Value = 99;
        throw new InvalidOperationException("Reject Play activation.");
    }
}
