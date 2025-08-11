using System;
using Gum.Forms;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using NumericUpDownExample.Controls;

namespace NumericUpDownExample;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private readonly string _instructionText =
    """
    1. You can click the up and down buttons to increment and decrement the values
    2. You can hover over and use the mouse wheel to increment and decrement the values
    3. You can click and drag left and right to increment and decrement the values
    4. The current vale of the integer box is {0}
    5. The current value of the float box is {1}
    """;

    GumService GumUI => GumService.Default;
    private Label _instructionsLabel;

    // Tracks the value that is input in the int numeric up/down
    private int _intValue;

    // Tracks the value that is input in the float numeric up/down
    private float _floatValue = 0;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        GumUI.Initialize(this, DefaultVisualsVersion.V2);

        // Create a simple NumericUpDown that uses int type
        // Since it's int type, it will not allow decimals as input.
        NumericUpDown<int> intUpDown = new();
        intUpDown.Value = _intValue;
        intUpDown.AddToRoot();
        intUpDown.ValueChanged += OnIntValueChanged;
        intUpDown.X = 300;
        intUpDown.Y = 500;

        // Create a float NumericUpDow but with some more complex settings
        // Since this is a float type, it will allow decimals as input
        NumericUpDown<float> floatUpDown = new();
        floatUpDown.Value = _floatValue;
        floatUpDown.AddToRoot();
        floatUpDown.ValueChanged += OnFloatValueChanged;
        floatUpDown.X = 500;
        floatUpDown.Y = 500;

        // We can adjust how much the value changes when incrementing or decrementing
        // using the buttons, using the mouse wheel, or if you click+drag left and right
        // on the control
        floatUpDown.Increment = 0.25f;

        // We can also format the string for the visual
        // For example, we'll require 3 digits before decimal and only allow two after
        floatUpDown.FormatString = "000.00";

        // Can also set a minimum and maximum value
        floatUpDown.Minimum = 0.0f;
        floatUpDown.Maximum = 100.0f;

        // This labels below have nothing to do with the sample and is only used
        // to provide text output on the screen, you can ignore this part
        _instructionsLabel = new();
        _instructionsLabel.X = 10;
        _instructionsLabel.Y = 10;
        _instructionsLabel.TextComponent.Wrap = true;
        _instructionsLabel.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        _instructionsLabel.Visual.Width = 0;
        _instructionsLabel.Text = string.Format(_instructionText, _intValue, _floatValue);
        _instructionsLabel.AddToRoot();

    }

    private void OnIntValueChanged(object sender, NumericUpDownValueChangedEventArgs<int> e)
    {
        if (e.NewValue.HasValue)
        {
            _intValue = e.NewValue.Value;
            _instructionsLabel.Text = string.Format(_instructionText, _intValue, _floatValue);
        }
    }


    private void OnFloatValueChanged(object sender, NumericUpDownValueChangedEventArgs<float> e)
    {
        if (e.NewValue.HasValue)
        {
            _floatValue = e.NewValue.Value;
            _instructionsLabel.Text = string.Format(_instructionText, _intValue, _floatValue);
        }
    }


    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        GumUI.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumUI.Draw();
    }
}
