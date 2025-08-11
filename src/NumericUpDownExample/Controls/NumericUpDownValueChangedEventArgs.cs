using System;
using System.Numerics;

namespace NumericUpDownExample.Controls;

/// <summary>
/// Provides data for the ValueChanged event.
/// </summary>
public class NumericUpDownValueChangedEventArgs<T> : EventArgs where T : struct, INumber<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Gets the previous value.
    /// </summary>
    public T? OldValue { get; }

    /// <summary>
    /// Gets the new value.
    /// </summary>
    public T? NewValue { get; }

    /// <summary>
    /// Initializes a new instance of the NumericUpDownValueChangedEventArgs class.
    /// </summary>
    /// <param name="oldValue">The previous value.</param>
    /// <param name="newValue">The new value.</param>
    public NumericUpDownValueChangedEventArgs(T? oldValue, T? newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
