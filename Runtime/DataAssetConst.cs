namespace Turian.Engine.Core;

/// <summary>An authored value with no mutable runtime storage.</summary>
public class DataAssetConst<T> : DataAsset
{
    /// <summary>The designer-authored constant; gameplay reads it without acquiring runtime state.</summary>
    public T Value
    {
        get;
        set
        {
            if (RuntimeSession is not null) { throw new InvalidOperationException("Runtime constants are read-only."); }
            field = value;
        }
    } = default!;
}

/// <summary>A designer-authored float constant.</summary>
[TypeId("db1528ca-0dac-4849-bbfe-fb58559fdceb")]
[CreateAssetMenu(fileName: "New Float Constant.dataasset", path: "DDD/DataAssetConst<T>/Float")]
public sealed class DataAssetConstFloat : DataAssetConst<float>;

/// <summary>A designer-authored integer constant.</summary>
[TypeId("95d56b13-bfe0-4f11-905c-d43d4fe50a9c")]
[CreateAssetMenu(fileName: "New Int Constant.dataasset", path: "DDD/DataAssetConst<T>/Int")]
public sealed class DataAssetConstInt : DataAssetConst<int>;

/// <summary>A designer-authored boolean constant.</summary>
[TypeId("fce76a7f-9ebe-4c99-b077-8f2e14161424")]
[CreateAssetMenu(fileName: "New Bool Constant.dataasset", path: "DDD/DataAssetConst<T>/Bool")]
public sealed class DataAssetConstBool : DataAssetConst<bool>;
