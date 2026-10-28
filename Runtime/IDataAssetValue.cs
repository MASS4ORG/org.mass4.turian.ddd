namespace Turian.Engine.Core;

/// <summary>Exposes a shared value to editor and UI adapters without reflective member access.</summary>
public interface IDataAssetValue
{
    /// <summary>Reads the current value; value types are boxed only at this presentation boundary.</summary>
    object? ReadValue();

    /// <summary>Writes an exactly matching payload type; false reports a type mismatch without coercion.</summary>
    bool TryWriteValue(object? value);
}
