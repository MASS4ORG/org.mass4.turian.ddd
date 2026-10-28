using System.Numerics;

namespace Turian.Engine.Core;

/// <summary>A designer-created Float event with a closed runtime type.</summary>
[TypeId("df5c00d6-ab53-51eb-80c1-7814c75a3831")]
[CreateAssetMenu(fileName: "New Float Event.dataasset", path: "DDD/DataAssetEvent<T>/Float")]
public sealed class DataAssetEventFloat : DataAssetEvent<float>;

/// <summary>A designer-created Int event with a closed runtime type.</summary>
[TypeId("697ff4d3-666f-50e8-a35a-b8d4daa06d94")]
[CreateAssetMenu(fileName: "New Int Event.dataasset", path: "DDD/DataAssetEvent<T>/Int")]
public sealed class DataAssetEventInt : DataAssetEvent<int>;

/// <summary>A designer-created Bool event with a closed runtime type.</summary>
[TypeId("ace3bbbc-1359-5bef-ac0a-59c775e3c296")]
[CreateAssetMenu(fileName: "New Bool Event.dataasset", path: "DDD/DataAssetEvent<T>/Bool")]
public sealed class DataAssetEventBool : DataAssetEvent<bool>;

/// <summary>A designer-created Vector2 event with a closed runtime type.</summary>
[TypeId("f915480c-8fb0-5530-a778-22c15d88c965")]
[CreateAssetMenu(fileName: "New Vector2 Event.dataasset", path: "DDD/DataAssetEvent<T>/Vector2")]
public sealed class DataAssetEventVector2 : DataAssetEvent<Vector2>;

/// <summary>A designer-created Vector3 event with a closed runtime type.</summary>
[TypeId("1956ed19-8724-503c-a690-2ae52acf050a")]
[CreateAssetMenu(fileName: "New Vector3 Event.dataasset", path: "DDD/DataAssetEvent<T>/Vector3")]
public sealed class DataAssetEventVector3 : DataAssetEvent<Vector3>;

/// <summary>A designer-created AssetEnum event with a closed runtime type.</summary>
[TypeId("53aa064a-8a21-594c-bfd0-7cd1b693e87e")]
[CreateAssetMenu(fileName: "New Asset Enum Event.dataasset", path: "DDD/DataAssetEvent<T>/Asset Enum")]
public sealed class DataAssetEventAssetEnum : DataAssetEvent<AssetEnumValue>;

/// <summary>A designer-created Vector2 variable with a closed runtime type.</summary>
[TypeId("72c15fa4-3db7-52ab-80fa-6c86db6abd3a")]
[CreateAssetMenu(fileName: "New Vector2 Variable.dataasset", path: "DDD/DataAssetVariable<T>/Vector2")]
public sealed class DataAssetVariableVector2 : DataAssetVariable<Vector2>;

/// <summary>A designer-created Vector3 variable with a closed runtime type.</summary>
[TypeId("c01dccbc-8cc9-5a9b-a7d7-daa68ef5e95e")]
[CreateAssetMenu(fileName: "New Vector3 Variable.dataasset", path: "DDD/DataAssetVariable<T>/Vector3")]
public sealed class DataAssetVariableVector3 : DataAssetVariable<Vector3>;

/// <summary>A designer-created Vector2 const with a closed runtime type.</summary>
[TypeId("9aee6614-b297-5657-bc6d-323d00a54170")]
[CreateAssetMenu(fileName: "New Vector2 Constant.dataasset", path: "DDD/DataAssetConst<T>/Vector2")]
public sealed class DataAssetConstVector2 : DataAssetConst<Vector2>;

/// <summary>A designer-created Vector3 const with a closed runtime type.</summary>
[TypeId("de609862-6b7d-53f5-bff7-5dd3c616c6dc")]
[CreateAssetMenu(fileName: "New Vector3 Constant.dataasset", path: "DDD/DataAssetConst<T>/Vector3")]
public sealed class DataAssetConstVector3 : DataAssetConst<Vector3>;

/// <summary>A designer-created AssetEnum const with a closed runtime type.</summary>
[TypeId("8c955b13-d594-5266-af6f-3e9f74dd05fe")]
[CreateAssetMenu(fileName: "New Asset Enum Constant.dataasset", path: "DDD/DataAssetConst<T>/Asset Enum")]
public sealed class DataAssetConstAssetEnum : DataAssetConst<AssetEnumValue>;

/// <summary>A designer-created AssetEnum runtimeset with a closed runtime type.</summary>
[TypeId("60e2b5b1-4394-5d5c-8c40-2119a686f156")]
[CreateAssetMenu(fileName: "New Asset Enum Set.dataasset", path: "DDD/DataAssetRuntimeSet<T>/Asset Enum")]
public sealed class DataAssetRuntimeSetAssetEnum : DataAssetRuntimeSet<AssetEnumValue>;

/// <summary>A designer-created DataAsset runtimeset with a closed runtime type.</summary>
[TypeId("5e70f41c-87a2-5a8d-bdd6-40d4ea4f32f1")]
[CreateAssetMenu(fileName: "New DataAsset Set.dataasset", path: "DDD/DataAssetRuntimeSet<T>/DataAsset")]
public sealed class DataAssetRuntimeSetDataAsset : DataAssetRuntimeSet<DataAsset>;
