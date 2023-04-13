using System.Diagnostics.CodeAnalysis;

namespace DispatchSharp.QueueTypes;

/// <summary>
/// A value with an optional name string
/// </summary>
/// <typeparam name="T">Contained value</typeparam>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public struct Named<T>
{
    /// <summary>
    /// Optional: Name of this item
    /// </summary>
    public string? Name;
        
    /// <summary>
    /// Value of the item
    /// </summary>
    public T Value;
}