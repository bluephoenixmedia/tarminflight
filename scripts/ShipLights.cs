using Godot;
using System;
using System.Collections.Generic;

public partial class ShipLights : Node
{
    [Export]
    public float PulseSpeed = 2.0f;

    [Export]
    public float PulseDepth = 0.2f; // How much energy fluctuates (percentage)

    private List<OmniLight3D> _lights = new List<OmniLight3D>();
    private List<float> _baseEnergies = new List<float>();

    public override void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            if (child is OmniLight3D light)
            {
                _lights.Add(light);
                _baseEnergies.Add(light.LightEnergy);
            }
        }
    }

    public override void _Process(double delta)
    {
        float time = Time.GetTicksMsec() / 1000.0f;
        float wave = Mathf.Sin(time * PulseSpeed);
        
        // Map wave (-1 to 1) to a multiplier (1 - depth to 1 + depth) -> Actually usually just dimming looks better or strictly oscillating?
        // Let's do: base * (1 + wave * depth)
        // If depth is 0.2, multiplier ranges from 0.8 to 1.2.
        
        float multiplier = 1.0f + (wave * PulseDepth);

        for (int i = 0; i < _lights.Count; i++)
        {
            _lights[i].LightEnergy = _baseEnergies[i] * multiplier;
        }
    }
}
