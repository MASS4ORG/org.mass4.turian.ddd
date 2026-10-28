using System.Text.Json;
using Turian.Engine.Core;

namespace Turian.Engine.DataAssetDriven;

/// <summary>A designer field choosing an inline constant or a shared variable, with generated GUID hydration.</summary>
public sealed class DataAssetReference<T> : IEmbeddedAssetValue<DataAssetReference<T>>
{
    WeakReference<IdObject>? owner;
    string member = string.Empty;

    /// <summary>Uses the inline value when true; otherwise reads and writes the selected variable.</summary>
    public bool UseConstant { get; set; } = true;

    /// <summary>The authored inline constant, also read when no variable is assigned.</summary>
    public T ConstantValue
    {
        get;
        set
        {
            field = value;
            if (owner?.TryGetTarget(out var target) == true) { ObjectReferences.Forget(target, member + ".ConstantValue"); }
        }
    } = default!;

    /// <summary>The shared variable; replacing it also discards any unresolved earlier wire.</summary>
    public DataAssetVariable<T>? Variable
    {
        get;
        set
        {
            field = value;
            if (owner?.TryGetTarget(out var target) == true) { ObjectReferences.Forget(target, member + ".Variable"); }
        }
    }

    /// <summary>Reads either source; gameplay writes require an assigned variable rather than changing a constant.</summary>
    [JsonIgnore, Hide]
    public T Value
    {
        get => UseConstant || Variable is null ? ConstantValue : Variable.Value;
        set
        {
            if (UseConstant || Variable is null) { throw new InvalidOperationException("Select a variable before writing a reference value."); }
            Variable.Value = value;
        }
    }

    /// <inheritdoc />
    public void WriteJson(Utf8JsonWriter writer, JsonSerializerOptions options, IdObject owner, string member)
    {
        BindOwner(owner, member);
        writer.WriteStartObject();
        writer.WriteBoolean(nameof(UseConstant), UseConstant);
        writer.WritePropertyName(nameof(ConstantValue));
        if (ConstantValue is DataAsset asset)
        {
            ObjectReferences.WriteEmbedded(writer, owner, member + ".ConstantValue", asset);
        }
        else if (ObjectReferences.TryGetUnresolved(owner, member + ".ConstantValue", out _))
        {
            ObjectReferences.WriteEmbedded(writer, owner, member + ".ConstantValue", null);
        }
        else { GeneratedJson.Write(writer, ConstantValue, options); }
        writer.WritePropertyName(nameof(Variable));
        ObjectReferences.WriteEmbedded(writer, owner, member + ".Variable", Variable);
        writer.WriteEndObject();
    }

    void BindOwner(IdObject target, string path)
    {
        if (owner?.TryGetTarget(out var previous) == true && ReferenceEquals(previous, target) && member == path) { return; }
        ObjectReferences.Forget(target, path + ".ConstantValue");
        ObjectReferences.Forget(target, path + ".Variable");
        owner = new WeakReference<IdObject>(target);
        member = path;
    }

    /// <inheritdoc />
    public static DataAssetReference<T> ReadJson(JsonElement json, JsonSerializerOptions options, IdObject owner, string member)
    {
        var result = new DataAssetReference<T>
        {
            owner = new WeakReference<IdObject>(owner),
            member = member,
            UseConstant = !json.TryGetProperty(nameof(UseConstant), out var mode) || mode.GetBoolean()
        };
        if (json.TryGetProperty(nameof(ConstantValue), out var constant))
        {
            if (constant.ValueKind == JsonValueKind.Object && constant.TryGetProperty("$ref", out _))
            {
                ObjectReferences.ReadEmbedded<T>(owner, member + ".ConstantValue", constant,
                    value => result.ConstantValue = value!);
            }
            else { result.ConstantValue = GeneratedJson.Read<T>(constant, options)!; }
        }
        if (json.TryGetProperty(nameof(Variable), out var variable))
        {
            ObjectReferences.ReadEmbedded<DataAssetVariable<T>>(owner, member + ".Variable", variable,
                value => result.Variable = value);
        }
        return result;
    }
}
