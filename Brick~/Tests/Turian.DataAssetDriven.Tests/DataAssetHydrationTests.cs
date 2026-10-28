namespace Turian.Tests;

/// <summary>GUID hydration binds referenced gameplay assets to the loader's runtime session.</summary>
public sealed class DataAssetHydrationTests
{
    /// <summary>A response and its referenced variable share one runtime session and leave no retained values on exit.</summary>
    [Fact]
    public void HydratedGraphResetsOnSessionExit()
    {
        var root = Path.Combine(Path.GetTempPath(), $"turian-ddd-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "Assets"));
        try
        {
            var database = new AssetDatabase();
            var variable = new DataAssetVariableInt { DefaultValue = 10 };
            var response = new DataAssetIntResponse { Variable = variable, Amount = 4 };
            Save(variable, "variable");
            Save(response, "response");
            var session = new DataAssetSession();
            var loader = new RuntimeAssetLoader(database, session);
            var loaded = Load<DataAssetIntResponse>(loader, response.Id);
            Assert.NotNull(loaded.Variable);
            Assert.True(loaded.IsRuntime);
            Assert.True(loaded.Variable.IsRuntime);
            Assert.Same(loaded.Variable, Load<DataAssetVariableInt>(loader, variable.Id));
            loaded.Execute();
            Assert.Equal(14, loaded.Variable.Value);
            session.Dispose();
            Assert.False(loaded.IsRuntime);
            Assert.False(loaded.Variable.IsRuntime);
            Assert.Equal(10, loaded.Variable.Value);
            Assert.Empty(session.Assets);
            return;

            void Save(DataAsset asset, string name)
            {
                var path = Path.Combine(root, "Assets", name + ".dataasset");
                Serializer.Save(path, asset);
                var metadata = new DataAssetAsset { Id = asset.Id, RelativePath = path };
                Serializer.Save(path + ".meta", metadata);
                Assert.True(database.RegisterAsset(metadata));
            }
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    static T Load<T>(IAssetLoader loader, Guid id) where T : DataAsset =>
        loader.LoadContentAsync<T>(id).GetAwaiter().GetResult()!;
}
