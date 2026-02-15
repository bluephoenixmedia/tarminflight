using Godot;
using System;

public partial class PlanetRandomizer : MeshInstance3D
{
    [Export]
    public Texture2D[] PlanetTextures;

    public override void _Ready()
    {
        GD.Print("PlanetRandomizer: Ready called.");
        if (PlanetTextures != null && PlanetTextures.Length > 0)
        {
            GD.Print($"PlanetRandomizer: Found {PlanetTextures.Length} textures.");
            var random = new Random();
            int index = random.Next(PlanetTextures.Length);
            Texture2D selectedTexture = PlanetTextures[index];
            GD.Print($"PlanetRandomizer: Selected texture {selectedTexture.ResourcePath}");

            StandardMaterial3D mat = new StandardMaterial3D();
            mat.AlbedoTexture = selectedTexture;
            mat.EmissionEnabled = true;
            mat.Emission = new Color(0.1f, 0.1f, 0.1f);
            mat.EmissionTexture = selectedTexture;
            
            this.MaterialOverride = mat;
        }
        else
        {
            GD.PrintErr("PlanetRandomizer: No textures found!");
            StandardMaterial3D debugMat = new StandardMaterial3D();
            debugMat.AlbedoColor = new Color(1, 0, 1); // Magenta debug color
            this.MaterialOverride = debugMat;
        }
    }
}
