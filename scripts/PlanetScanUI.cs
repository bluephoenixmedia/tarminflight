using Godot;
using System;

public partial class PlanetScanUI : Control
{
    private Label _nameLabel;
    private Label _typeLabel;
    private Label _gravityLabel;
    private Label _radiusLabel;
    private Label _atmosphereLabel;
    private Label _temperatureLabel;
    private Label _weatherLabel;
    private Button _closeButton;

    public override void _Ready()
    {
        // Assuming a specific node structure. I will create the TSCN to match this.
        _nameLabel = GetNode<Label>("Panel/VBoxContainer/NameLabel");
        _typeLabel = GetNode<Label>("Panel/VBoxContainer/TypeLabel");
        _gravityLabel = GetNode<Label>("Panel/VBoxContainer/GridContainer/GravityValue");
        _radiusLabel = GetNode<Label>("Panel/VBoxContainer/GridContainer/RadiusValue");
        _atmosphereLabel = GetNode<Label>("Panel/VBoxContainer/GridContainer/AtmosphereValue");
        _temperatureLabel = GetNode<Label>("Panel/VBoxContainer/GridContainer/TemperatureValue");
        _weatherLabel = GetNode<Label>("Panel/VBoxContainer/GridContainer/WeatherValue");
        
        _closeButton = GetNode<Button>("Panel/VBoxContainer/CloseButton");
        _closeButton.Pressed += OnClosePressed;
    }

    public void Populate(SpaceItem item)
    {
        if (item == null) return;
        
        _nameLabel.Text = item.Name;
        _typeLabel.Text = item.Type.ToString();

        if (item.Data != null)
        {
            _gravityLabel.Text = $"{item.Data.Gravity:F2} g";
            _radiusLabel.Text = $"{item.Data.Radius:F0} km";
            _atmosphereLabel.Text = item.Data.AtmosphereComposition;
            _temperatureLabel.Text = $"{item.Data.Temperature} C";
            _weatherLabel.Text = item.Data.Weather;
        }
        else
        {
            _gravityLabel.Text = "N/A";
            _radiusLabel.Text = "N/A";
            _atmosphereLabel.Text = "N/A";
            _temperatureLabel.Text = "N/A";
            _weatherLabel.Text = "N/A";
        }
    }

    private void OnClosePressed()
    {
        Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }
}
