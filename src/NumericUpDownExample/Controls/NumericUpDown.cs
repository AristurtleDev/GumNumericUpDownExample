using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.Wireframe;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
using RenderingLibrary.Graphics;

namespace NumericUpDownExample.Controls;

/// <summary>
/// Represents a spin box control that allows users to input numeric values using keyboard entry or increment/decrement
/// buttons.
/// </summary>
/// <typeparam name="T">The numeric type that implements INumber and IMinMaxValue interfaces. Supported types include
/// int, float, double, decimal, and other numeric primitives.</typeparam>
/// <remarks>
/// The control provides visual feedback through enabled/disabled spinner buttons based on current value and constraints.
/// Value synchronization occurs bidirectionally between the text input and the numeric value, with validation applied
/// during conversion.
/// </remarks>
public class NumericUpDown<T> : ContainerRuntime where T : struct, INumber<T>, IMinMaxValue<T>
{

#nullable enable
    #region Constants

    private const int SPINNER_CONTAINER_WIDTH = 30;
    private const int SPINNER_CONTAINER_RIGHT_MARGIN = 2;
    private const int TEXTBOX_VERTICAL_PADDING = 4;
    private const int TEXT_BOX_LEFT_PADDING = 2;
    private const int SPINNER_BUTTON_OVERLAP = 1;
    private const int DOUBLE_CLICK_TIME_MS = 500;

    // Calculate TextBox width to leave space for spinner container plus margins
    private const int TEXT_BOX_WIDTH_REDUCTION = -(SPINNER_CONTAINER_WIDTH + SPINNER_CONTAINER_RIGHT_MARGIN + TEXT_BOX_LEFT_PADDING);

    #endregion

    #region Private Fields

    private T? _value;
    private T _minimum = T.MinValue;
    private T _maximum = T.MaxValue;
    private T _increment = T.One;
    private string _formatString = string.Empty;
    private NumberFormatInfo _numberFormat = NumberFormatInfo.CurrentInfo;
    private NumberStyles _parsingNumberStyle = NumberStyles.Any;
    private bool _clipValueToMinMax = true;
    private bool _allowSpin = true;
    private bool _showButtonSpinner = true;
    private bool _isSyncingTextAndValueProperties;
    private bool _internalValueSet;
    private bool _isEditing = false;
    private int _lastClickTime;
    private bool _isReadOnly;

    #endregion

    #region Visual Components

    /// <summary>
    /// Gets the background nine-slice sprite that provides the visual border and background for the control.
    /// </summary>
    public NineSliceRuntime Background { get; private set; }

    /// <summary>
    /// Gets the text input component that allows direct keyboard entry of numeric values.
    /// </summary>
    public TextBox TextBox { get; private set; }

    /// <summary>
    /// Gets the container that holds the increment and decrement buttons.
    /// </summary>
    public ContainerRuntime SpinnerContainer { get; private set; }

    /// <summary>
    /// Gets the button that increases the numeric value by the increment amount.
    /// </summary>
    public Button IncrementButton { get; private set; }

    /// <summary>
    /// Gets the button that decreases the numeric value by the increment amount.
    /// </summary>
    public Button DecrementButton { get; private set; }

    /// <summary>
    /// Gets the upward-pointing arrow icon displayed on the increment button.
    /// </summary>
    public SpriteRuntime UpArrowIcon { get; private set; }

    /// <summary>
    /// Gets the downward-pointing arrow icon displayed on the decrement button.
    /// </summary>
    public SpriteRuntime DownArrowIcon { get; private set; }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the numeric value of the control.
    /// </summary>
    /// <value>
    /// The current numeric value, or null if no value is set. When set, the value is automatically constrained to the
    /// Minimum and Maximum range if ClipValueToMinMax is true.
    /// </value>
    /// <remarks>
    /// Setting this property triggers value validation, text synchronization, and UI state updates. The setter will not
    /// trigger change events if the new value equals the current value.
    /// </remarks>
    public T? Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            T? oldValue = _value;
            T? newValue = value;

            if (_clipValueToMinMax && newValue.HasValue)
            {
                newValue = T.Max(_minimum, T.Min(_maximum, newValue.Value));
            }

            _value = newValue;
            OnValueChanged(oldValue, newValue);
        }
    }

    /// <summary>
    /// Gets or sets the minimum allowable value for the control.
    /// </summary>
    /// <value>
    /// The minimum value constraint. Defaults to T.MinValue for the numeric type.
    /// </value>
    /// <remarks>
    /// When set, if the new minimum is greater than the current Maximum, the Maximum will be automatically adjusted to
    /// equal the new minimum. If ClipValueToMinMax is true and the current Value is below the new minimum, the Value
    /// will be adjusted upward.
    /// </remarks>
    public T Minimum
    {
        get => _minimum;
        set
        {
            if (_minimum == value)
            {
                return;
            }

            _minimum = value;

            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }

            if (_clipValueToMinMax && _value.HasValue)
            {
                Value = T.Max(Minimum, _value.Value);
            }

            UpdateSpinnerState();
        }
    }

    /// <summary>
    /// Gets or sets the maximum allowable value for the control.
    /// </summary>
    /// <value>
    /// The maximum value constraint. Defaults to T.MaxValue for the numeric type.
    /// </value>
    /// <remarks>
    /// When set, if the new maximum is less than the current Minimum, the Minimum will be automatically adjusted to
    /// equal the new maximum. If ClipValueToMinMax is true and the current Value exceeds the new maximum, the Value
    /// will be adjusted downward.
    /// </remarks>
    public T Maximum
    {
        get => _maximum;
        set
        {
            if (_maximum == value)
            {
                return;
            }

            _maximum = value;

            if (_minimum > _maximum)
            {
                _minimum = _maximum;
            }

            if (_clipValueToMinMax && _value.HasValue)
            {
                Value = T.Min(_maximum, _value.Value);
            }

            UpdateSpinnerState();
        }
    }

    /// <summary>
    /// Gets or sets the amount by which the value changes when using the increment or decrement buttons.
    /// </summary>
    /// <value>
    /// The step increment for spinner operations. Defaults to T.One. Setting this to T.Zero disables spinner button
    /// functionality.
    /// </value>
    public T Increment
    {
        get => _increment;
        set
        {
            if (_increment == value)
            {
                return;
            }

            _increment = value;
            UpdateSpinnerState();
        }
    }

    /// <summary>
    /// Gets or sets the format string used when converting the numeric value to text for display.
    /// </summary>
    /// <value>
    /// A standard or custom numeric format string. When empty, the default ToString behavior is used. Supports both
    /// composite format strings (containing {0}) and direct format specifiers.
    /// </value>
    /// <remarks>
    /// Changes to this property immediately update the displayed text. The format string is applied using the current
    /// NumberFormat provider.
    /// </remarks>
    public string FormatString
    {
        get => _formatString;
        set
        {
            if (_formatString == value)
            {
                return;
            }

            _formatString = value ?? string.Empty;

            if (!_isSyncingTextAndValueProperties)
            {
                SyncTextAndValueProperties(false, null, true);
            }
        }
    }

    /// <summary>
    /// Gets or sets the NumberFormatInfo used for parsing and formatting numeric values.
    /// </summary>
    /// <value>
    /// The format provider for number parsing and display. Defaults to NumberFormatInfo.CurrentInfo. Setting null
    /// reverts to CurrentInfo.
    /// </value>
    /// <remarks>
    /// This affects both text-to-value parsing during user input and value-to-text formatting for display.
    /// </remarks>
    public NumberFormatInfo NumberFormat
    {
        get => _numberFormat;
        set
        {
            if (_numberFormat == value)
            {
                return;
            }

            _numberFormat = value ?? NumberFormatInfo.CurrentInfo;

            if (!_isSyncingTextAndValueProperties)
            {
                SyncTextAndValueProperties(false, null, true);
            }
        }
    }

    /// <summary>
    /// Gets or sets the NumberStyles used when parsing text input into numeric values.
    /// </summary>
    /// <value>
    /// The parsing style flags. Defaults to NumberStyles.Any, which allows the most flexible input parsing including
    /// currency symbols, thousands separators, and whitespace.
    /// </value>
    public NumberStyles ParsingNumberStyle
    {
        get => _parsingNumberStyle;
        set
        {
            if (_parsingNumberStyle == value)
            {
                return;
            }

            _parsingNumberStyle = value;
        }
    }

    /// <summary>
    /// Gets or sets whether values are automatically constrained to the Minimum and Maximum range.
    /// </summary>
    /// <value>
    /// true to automatically clip values to the valid range; false to allow out-of-range values and throw exceptions
    /// during validation.
    /// </value>
    /// <remarks>
    /// When true, values exceeding the range are silently adjusted. When false, setting out-of-range values throws
    /// ArgumentOutOfRangeException.
    /// </remarks>
    public bool ClipValueToMinMax
    {
        get => _clipValueToMinMax;
        set
        {
            if (_clipValueToMinMax == value)
            {
                return;
            }

            _clipValueToMinMax = value;

            if (value && _value.HasValue)
            {
                Value = T.Max(_minimum, T.Min(_maximum, _value.Value));
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the increment and decrement buttons can modify the value.
    /// </summary>
    /// <value>
    /// true to enable spinner button functionality; false to disable button interactions while preserving text input
    /// capability.
    /// </value>
    public bool AllowSpin
    {
        get => _allowSpin;
        set
        {
            if (_allowSpin == value)
            {
                return;
            }

            _allowSpin = value;
            UpdateSpinnerState();
        }
    }

    /// <summary>
    /// Gets or sets whether the increment and decrement buttons are visible.
    /// </summary>
    /// <value>
    /// true to show the spinner buttons; false to hide them and expand the text input area.
    /// </value>
    /// <remarks>
    /// When hidden, the text input area expands to fill the available width, providing a text-only numeric input
    /// experience.
    /// </remarks>
    public bool ShowButtonSpinner
    {
        get => _showButtonSpinner;
        set
        {
            if (_showButtonSpinner == value)
            {
                return;
            }

            _showButtonSpinner = value;
            UpdateSpinnerVisibility();
        }
    }

    /// <summary>
    /// Gets or sets the text content of the input field.
    /// </summary>
    /// <value>
    /// The raw text as entered by the user. This may differ from the formatted display of the Value property during
    /// editing.
    /// </value>
    /// <remarks>
    /// Direct text assignment bypasses value parsing and may result in temporarily invalid states until focus is lost
    /// or validation occurs.
    /// </remarks>
    public string Text
    {
        get => TextBox.Text;
        set => TextBox.Text = value;
    }

    /// <summary>
    /// Gets or sets the placeholder text displayed when the input field is empty and unfocused.
    /// </summary>
    /// <value>
    /// The watermark text to guide user input, or null for no placeholder.
    /// </value>
    public string? PlaceHolder
    {
        get => TextBox.Placeholder;
        set => TextBox.Placeholder = value;
    }

    /// <summary>
    /// Gets or sets whether the text input field is read-only.
    /// </summary>
    /// <value>
    /// true to prevent text editing while allowing spinner button interactions (if AllowSpin is true); false to allow
    /// full text input.
    /// </value>
    /// <remarks>
    /// When read-only, users can still interact with spinner buttons unless AllowSpin is also false.
    /// </remarks>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set
        {
            if (_isReadOnly == value)
            {
                return;
            }

            _isReadOnly = value;
            UpdateSpinnerState();
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the Value property changes through user interaction or programmatic assignment.
    /// </summary>
    /// <remarks>
    /// This event fires after value validation and constraint application, providing both the previous and new values.
    /// The event is triggered by text input validation, spinner button clicks, and direct Value property assignment.
    /// </remarks>
    public event EventHandler<NumericUpDownValueChangedEventArgs<T>>? ValueChanged;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the NumericUpDown control with default dimensions and styling.
    /// </summary>
    /// <remarks>
    public NumericUpDown()
    {
        Width = 150;
        Height = 50;

        _lastClickTime = Environment.TickCount;

        CreateBackground();
        CreateTextBox();
        CreateSpinnerContainer();
        CreateIncrementButton();
        CreateUpArrowIcon();
        CreateDecrementButton();
        CreateDownArrowIcon();
    }

    [MemberNotNull(nameof(Background))]
    private void CreateBackground()
    {
        Background = new NineSliceRuntime();

        Background.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.X = 0;

        Background.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.Y = 0;

        Background.XOrigin = HorizontalAlignment.Center;
        Background.YOrigin = VerticalAlignment.Center;

        Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.Width = 0;

        Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.Height = 0;

        Background.Color = Styling.ActiveStyle.Colors.DarkGray;
        Background.Texture = Styling.ActiveStyle.SpriteSheet;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);

        AddChild(Background);
    }

    [MemberNotNull(nameof(TextBox))]
    private void CreateTextBox()
    {
        TextBox = new();
        TextBox.IsReadOnly = true;
        TextBox.IsCaretVisibleWhenNotFocused = false;
        TextBox.IsCaretVisibleWhenReadOnly = false;

        TextBoxVisual visual = (TextBoxVisual)TextBox.Visual;

        visual.PlaceholderTextInstance.Text = string.Empty;

        visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        visual.X = TEXT_BOX_LEFT_PADDING;

        visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        visual.Y = 0;

        visual.XOrigin = HorizontalAlignment.Left;
        visual.YOrigin = VerticalAlignment.Center;

        visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        visual.Width = TEXT_BOX_WIDTH_REDUCTION;

        visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        visual.Height = -TEXTBOX_VERTICAL_PADDING;

        TextBox.PreviewTextInput += OnTextBoxPreviewTextInput;
        TextBox.TextChanged += HandleTextBoxTextChanged;
        TextBox.GotFocus += HandleTextBoxGotFocus;
        TextBox.LostFocus += HandleTextBoxLostFocus;
        TextBox.Visual.Click += HandleTextBoxClick;
        TextBox.Visual.MouseWheelScroll += HandleTextBoxMouseWheelScroll;
        TextBox.Visual.Dragging += HandleTextboxDragging;

        AddChild(visual);
    }

    [MemberNotNull(nameof(SpinnerContainer))]
    private void CreateSpinnerContainer()
    {
        SpinnerContainer = new ContainerRuntime();

        SpinnerContainer.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        SpinnerContainer.X = -SPINNER_CONTAINER_RIGHT_MARGIN;

        SpinnerContainer.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        SpinnerContainer.Y = 0;

        SpinnerContainer.XOrigin = HorizontalAlignment.Right;
        SpinnerContainer.YOrigin = VerticalAlignment.Center;

        SpinnerContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        SpinnerContainer.Width = SPINNER_CONTAINER_WIDTH;

        SpinnerContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        SpinnerContainer.Height = -TEXTBOX_VERTICAL_PADDING;

        AddChild(SpinnerContainer);
    }

    [MemberNotNull(nameof(IncrementButton))]
    private void CreateIncrementButton()
    {
        IncrementButton = new Button();
        IncrementButton.Text = string.Empty;

        ButtonVisual visual = (ButtonVisual)IncrementButton.Visual;

        visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        visual.X = 0;

        visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        visual.Y = 0;

        visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        visual.Width = 0;

        visual.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        visual.Height = 50;

        IncrementButton.Click += HandleIncrementButtonClick;

        SpinnerContainer.AddChild(visual);
    }

    [MemberNotNull(nameof(DecrementButton))]
    private void CreateDecrementButton()
    {
        DecrementButton = new Button();
        DecrementButton.Text = string.Empty;

        ButtonVisual visual = (ButtonVisual)DecrementButton.Visual;

        visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        visual.X = 0;

        visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        visual.Y = -SPINNER_BUTTON_OVERLAP;

        visual.YOrigin = VerticalAlignment.Bottom;

        visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        visual.Width = 0;

        visual.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        visual.Height = 50;

        DecrementButton.Click += HandleDecrementButtonClick;

        SpinnerContainer.AddChild(visual);
    }

    [MemberNotNull(nameof(UpArrowIcon))]
    private void CreateUpArrowIcon()
    {
        UpArrowIcon = new SpriteRuntime();

        UpArrowIcon.Name = nameof(UpArrowIcon);

        UpArrowIcon.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        UpArrowIcon.X = 0;

        UpArrowIcon.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        UpArrowIcon.Y = 0;

        UpArrowIcon.XOrigin = HorizontalAlignment.Center;
        UpArrowIcon.YOrigin = VerticalAlignment.Center;

        UpArrowIcon.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        UpArrowIcon.Width = 100;

        UpArrowIcon.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        UpArrowIcon.Height = 100;

        UpArrowIcon.Texture = Styling.ActiveStyle.SpriteSheet;
        UpArrowIcon.ApplyState(Styling.ActiveStyle.Icons.Arrow1);
        UpArrowIcon.Rotation = 90;

        IncrementButton.AddChild(UpArrowIcon);
    }

    [MemberNotNull(nameof(DownArrowIcon))]
    private void CreateDownArrowIcon()
    {
        DownArrowIcon = new SpriteRuntime();

        DownArrowIcon.Name = nameof(DownArrowIcon);

        DownArrowIcon.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        DownArrowIcon.X = 0;

        DownArrowIcon.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        DownArrowIcon.Y = 0;

        DownArrowIcon.XOrigin = HorizontalAlignment.Center;
        DownArrowIcon.YOrigin = VerticalAlignment.Center;

        DownArrowIcon.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        DownArrowIcon.Width = 100;

        DownArrowIcon.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        DownArrowIcon.Height = 100;

        DownArrowIcon.Texture = Styling.ActiveStyle.SpriteSheet;
        DownArrowIcon.ApplyState(Styling.ActiveStyle.Icons.Arrow1);
        DownArrowIcon.Rotation = -90;

        DecrementButton.AddChild(DownArrowIcon);
    }

    #endregion

    #region Event Handler Methods

    private void OnTextBoxPreviewTextInput(object? sender, TextCompositionEventArgs args)
    {
        // Only validate when in editing mode
        if (!_isEditing)
        {
            return;
        }

        // Check each character in the input text
        foreach (char character in args.Text)
        {
            // Allow control characters (backspace, delete, etc.)
            if (char.IsControl(character))
            {
                continue;
            }

            if (!IsValidNumericInput(character))
            {
                // Invalid character found, mark as handled to prevent input
                args.Handled = true;
                return;
            }
        }
    }

    private bool IsValidNumericInput(char character)
    {
        // Always allow digits
        if (char.IsDigit(character))
        {
            return true;
        }

        string currentText = TextBox.Text ?? string.Empty;
        int caretPosition = TextBox.CaretIndex;

        char decimalSeparator = NumberFormat.NumberDecimalSeparator[0];
        char groupSeparator = NumberFormat.NumberGroupSeparator[0];
        char negativeSign = NumberFormat.NegativeSign[0];
        char positiveSign = NumberFormat.PositiveSign[0];

        if (character == decimalSeparator)
        {
            // Only allow for floating point types and if there isn't already a
            // decimal separator.
            return IsFloatingPointType() && !currentText.Contains(decimalSeparator);
        }

        if (character == groupSeparator)
        {
            // Group separators are allowed.
            // Depending on cultures this could be . or ,
            return true;
        }

        if (character == negativeSign || character == positiveSign)
        {
            // Allow negative and positive signs only at the beginning
            return caretPosition == 0;
        }

        // otherwise, all other characters are invalid, return false.
        return false;
    }

    private static bool IsFloatingPointType()
    {
        Type type = typeof(T);
        return type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }

    private void HandleTextBoxTextChanged(object? sender, EventArgs e)
    {
        if (!_isSyncingTextAndValueProperties)
        {
            SyncTextAndValueProperties(true, TextBox.Text);
        }
    }

    private void HandleTextBoxGotFocus(object? sender, EventArgs e)
    {
        // Not sure yet if need to do something on gaining focus.
        // Doesn't seem like we need to, just keeping this here incase.
    }

    private void HandleTextBoxLostFocus(object? sender, EventArgs e)
    {
        TextBox.IsReadOnly = true;
        _isEditing = false;

        // Commit the current text when losing focus
        SyncTextAndValueProperties(true, TextBox.Text, true);
    }

    private void HandleTextBoxMouseWheelScroll(object? sender, RoutedEventArgs e)
    {
        // Only scroll if not in text editing mode or if not readonly
        if (_isEditing || _isReadOnly)
        {
            return;
        }

        Cursor cursor = GumService.Default.Cursor;

        if (cursor.ScrollWheelChange > 0)
        {
            OnIncrement();
        }
        else if (cursor.ScrollWheelChange < 0)
        {
            OnDecrement();
        }
    }

    private void HandleTextboxDragging(object? sender, EventArgs e)
    {
        // If we're not in edit mode, then keep the selection length to 0
        // so that mouse dragging for edits doesn't create the selection visual
        if (!_isEditing)
        {
            TextBox.SelectionLength = 0;
        }

        // Only handle dragging if not in text editing mode or if not readonly
        if (_isEditing || _isReadOnly)
        {
            return;
        }

        Cursor cursor = GumService.Default.Cursor;
        int delta = cursor.XChange;

        if (delta > 0)
        {
            OnIncrement();
        }
        else if (delta < 0)
        {
            OnDecrement();
        }
    }

    private void HandleTextBoxClick(object? sender, EventArgs e)
    {
        // If we're already in edit mode, or read only, exit early.
        if (_isEditing || _isReadOnly)
        {
            return;
        }

        // Check for double click by comparing the current time with last time clicked
        int currentTime = Environment.TickCount;
        if (currentTime - _lastClickTime <= DOUBLE_CLICK_TIME_MS)
        {
            // Double-click detected, enter editing mode
            _isEditing = true;
            TextBox.IsReadOnly = false;
            TextBox.IsFocused = true;
            SyncTextAndValueProperties(false, null, false);
        }
        else
        {
            // Single click just focuses but doesn't enter edit mode
            if (!TextBox.IsFocused)
            {
                TextBox.IsFocused = true; ;
            }
        }

        _lastClickTime = currentTime;
    }

    private void HandleIncrementButtonClick(object? sender, EventArgs e)
    {
        if (AllowSpin)
        {
            OnIncrement();
        }
    }

    private void HandleDecrementButtonClick(object? sender, EventArgs e)
    {
        if (AllowSpin)
        {
            OnDecrement();
        }
    }

    #endregion

    #region Value/Text Synchronization

    private bool SyncTextAndValueProperties(bool updatedValueFromText, string? text, bool forceTextUpdate = false)
    {
        if (_isSyncingTextAndValueProperties)
        {
            return true;
        }

        _isSyncingTextAndValueProperties = true;

        bool parsedTextIsValid = true;

        try
        {
            if (updatedValueFromText)
            {
                try
                {
                    T? newValue = ConvertTextToValue(text);

                    if (newValue != Value)
                    {
                        SetValueInternal(newValue);
                    }
                }
                catch
                {
                    parsedTextIsValid = false;
                }
            }

            if (forceTextUpdate || (!updatedValueFromText && parsedTextIsValid))
            {
                string newText = ConvertValueToText();

                if (TextBox.Text != newText)
                {
                    TextBox.Text = newText;
                }
            }

            UpdateSpinnerState();
        }
        finally
        {
            _isSyncingTextAndValueProperties = false;
        }

        return parsedTextIsValid;
    }

    private T? ConvertTextToValue(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        // Check if the current value text matches exactly
        string currentValueText = ConvertValueToText();
        if (string.Equals(currentValueText, text, StringComparison.Ordinal))
        {
            return Value;
        }

        if (T.TryParse(text, ParsingNumberStyle, NumberFormat, out T result))
        {
            if (ClipValueToMinMax)
            {
                result = T.Max(Minimum, T.Min(Maximum, result));
            }
            else
            {
                ValidateMinMax(result);
            }

            return result;
        }

        throw new FormatException($"Unable to parse '{text}' as a {typeof(T)} value");
    }

    private string ConvertValueToText()
    {
        if (!Value.HasValue)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(FormatString))
        {
            if (FormatString.Contains("{0"))
            {
                return string.Format(NumberFormat, FormatString, Value.Value);
            }

            return Value.Value.ToString(FormatString, NumberFormat);
        }

        return Value.Value.ToString(null, NumberFormat);
    }

    private void ValidateMinMax(T value)
    {
        if (value < Minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Value must be greater than or equal to the minimum value of {Minimum}");
        }

        if (value > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Value must be less than or equal to the maximum value of {Maximum}");
        }
    }

    #endregion

    #region Increment/Decrement Logic

    private void OnIncrement()
    {
        T result;

        if (Value.HasValue)
        {
            result = Value.Value + Increment;
        }
        else
        {
            result = Minimum != T.MinValue ? Minimum : T.Zero;
        }

        Value = T.Max(Minimum, T.Min(Maximum, result));
    }

    private void OnDecrement()
    {
        T result;

        if (Value.HasValue)
        {
            result = Value.Value - Increment;
        }
        else
        {
            result = Maximum != T.MaxValue ? Maximum : T.Zero;
        }

        Value = T.Max(Minimum, T.Min(Maximum, result));
    }

    #endregion

    #region State Updates

    private void UpdateSpinnerState()
    {
        bool canIncrement = AllowSpin &&
                            !_isReadOnly &&
                            Increment != T.Zero &&
                            (!Value.HasValue || Value.Value < Maximum);

        bool canDecrement = AllowSpin &&
                            !_isReadOnly &&
                            Increment != T.Zero &&
                            (!Value.HasValue || Value.Value > Minimum);

        IncrementButton.IsEnabled = canIncrement;
        DecrementButton.IsEnabled = canDecrement;
    }

    private void UpdateSpinnerVisibility()
    {
        IncrementButton.Visual.Visible = ShowButtonSpinner;
        DecrementButton.Visual.Visible = ShowButtonSpinner;
    }

    #endregion

    #region Value Changed Handling

    private void OnValueChanged(T? oldValue, T? newValue)
    {
        if (!_internalValueSet)
        {
            SyncTextAndValueProperties(false, null, true);
        }

        UpdateSpinnerState();
        RaiseValueChangedEvent(oldValue, newValue);
    }

    private void RaiseValueChangedEvent(T? oldValue, T? newValue)
    {
        if (ValueChanged == null)
        {
            return;
        }

        NumericUpDownValueChangedEventArgs<T> args = new(oldValue, newValue);
        ValueChanged(this, args);
    }

    private void SetValueInternal(T? value)
    {
        _internalValueSet = true;
        try
        {
            Value = value;
        }
        finally
        {
            _internalValueSet = false;
        }
    }

    #endregion
#nullable disable

}
