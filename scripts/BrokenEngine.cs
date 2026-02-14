using Godot;
using System;

public partial class BrokenEngine : Interactable
{
	private bool _isRepaired = false;

	public override void Interact()
	{
		if (_isRepaired)
		{
			GD.Print("Engine System: ONLINE. Diagnostics Normal.");
			return;
		}

		GD.Print("Repairing Engine...");
		_isRepaired = true;
		
		// Visual Feedback (Changing color to Green)
		var meshInstance = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
		if (meshInstance != null)
		{
			var material = new StandardMaterial3D();
			material.AlbedoColor = new Color(0, 1, 0); // Green
			material.EmissionEnabled = true;
			material.Emission = new Color(0, 1, 0);
			material.EmissionEnergyMultiplier = 2.0f;
			meshInstance.MaterialOverride = material;
		}
		
		GD.Print("SUCCESS: Engine Repaired! Fuel Flow Restored.");
	}
}
