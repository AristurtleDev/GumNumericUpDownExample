# NumericUpDown Control for Gum UI & MonoGame

A generic numeric input control for Gum UI framework with MonoGame, supporting any numeric type that implements `INumber<T>` and `IMinMaxValue<T>`.

## Features

### Multiple Input Methods

- **Up/Down Buttons**: Click the spinner buttons to increment/decrement values.
- **Mouse Wheel**: Hover over the control and scroll to adjust values.
- **Click & Drag**: Click and drag horizontally to adjust values.
- **Manual Entry**: Double-click the text field to enter edit mode for direct typing.

### Type Safety & Flexibility

- **Generic Implementation**: Works with `int`, `float`, `double`, `decimal`, and other numeric types
- **Automatic Type Validation**: Input is restricted based on the numeric type (e.g., no decimals for integer types)
- **Culture-Aware Formatting**: Respects `NumberFormatInfo` for localization

### Customizable Properties

- **Value Range**: Set `Minimum` and `Maximum` bounds with optional clipping
- **Increment Steps**: Configure how much the value changes per interaction
- **Format Strings**: Control display formatting (e.g., "000.00" for fixed decimal places)
- **Number Styles**: Customize parsing behavior for various input formats

### Smart UI Behavior

- **Visual Feedback**: Spinner buttons automatically enable/disable based on range limits
- **Input Validation**: Character input is restricted to valid numeric symbols
- **Position-Aware Input**: Decimal points and negative signs are only allowed in appropriate positions

## Quick Start

```csharp
using NumericUpDownExample.Controls;

// Integer control
NumericUpDown<int> intUpDown = new();
intUpDown.Value = 10;
intUpDown.Minimum = 0;
intUpDown.Maximum = 100;
intUpDown.Increment = 1;
intUpDown.ValueChanged += OnIntValueChanged;
intUpDown.AddToRoot();

// Float control with formatting
NumericUpDown<float floatUpDown = new();
floatUpDown.Value = 5.5f;
floatUpDown.Minimum = 0.0f;
floatUpDown.Maximum = 10.0f;
floatUpDown.Increment = 0.25f;
floatUpDown.FormatString = "F2"; // Show 2 decimal places
floatUpDown.ValueChanged += OnFloatValueChanged;
floatUpDown.AddToRoot();
```

## Usage Examples

### Basic Integer Control

```csharp
NumericUpDown<int> intControl = new()
{
    Value = 0,
    Minimum = -100,
    Maximum = 100,
    Increment = 5,
    X = 100,
    Y = 100
};
intControl.ValueChanged += (sender, e) =>
{
    Console.WriteLine($"Value changed from {e.OldValue} to {e.NewValue}");
};
intControl.AddToRoot();
```

### Formatted Decimal Control

```csharp
NumericUpDown<decimal> decimalControl = new()
{
    Value = 123.45m,
    Minimum = 0m,
    Maximum = 999.99m,
    Increment = 0.01m,
    FormatString = "C2", // Currency format with 2 decimals
    ClipValueToMinMax = true
};
decimalControl.AddToRoot();
```

### Custom Number Format

```csharp
NumericUpDown<double> customControl = new()
{
    Value = 1234.567,
    FormatString = "000.00", // Zero-padded format
    NumberFormat = new NumberFormatInfo
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = "."
    }
};
```

## Key Properties

| Property             | Type               | Description                                                                                                               |
| -------------------- | ------------------ | ------------------------------------------------------------------------------------------------------------------------- |
| `Value`              | `T?`               | The current numeric value.                                                                                                |
| `Minimum`            | `T`                | Minimum allowable value.                                                                                                  |
| `Maximum`            | `T`                | Maximum allowable value.                                                                                                  |
| `Increment`          | `T`                | Step size for button/wheel operations.                                                                                    |
| `FormatString`       | `string`           | Display format (supports standard .NET format strings).                                                                   |
| `NumberFormat`       | `NumberFormatInfo` | Culture-specific number formatting.                                                                                       |
| `ParsingNumberStyle` | `NumberStyles`     | Input parsing behavior.                                                                                                   |
| `ClipValueToMinMax`  | `bool`             | Whether to automatically constrain values to range.                                                                       |
| `ReadOnly`           | `bool`             | Whether the value can be modified. This affects value modification for direct input, spinners, mouse wheel, and dragging. |

## Events

### ValueChanged

Fired when the control's value changes through any input method:

```csharp
control.ValueChanged += (sender, e) =>
{
    if (e.NewValue.HasValue)
    {
        // Handle the new value
        ProcessValue(e.NewValue.Value);
    }
};
```

## License

This project is provided as an example implementation and as such is licensed under The Unlicense.
Feel free to use and modify the `NumericUpDown<T>` control in your own projects.
To view the full license text please refer to the [LICENSE](LICENSE) file.
