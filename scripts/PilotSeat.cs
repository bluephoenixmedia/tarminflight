using Godot;
using System;

public partial class PilotSeat : Interactable
{
	[Export] public Node3D CockpitCameraPosition;
	[Export] public Ship ParentShip;

	public override void Interact()
	{
		// Notify player to sit
		// Since we don't have a direct reference to the player in Interact(), 
		// we assume the caller (Player) handles the logic if we return something, 
		// OR we rely on the Player calling Interact, and WE call back to them.
		
		// But Player.cs calls `node.Call("Interact")`. 
		// Let's emit a signal or finding the player.
		
		var player = GetTree().GetFirstNodeInGroup("Player") as Player;
		if (player == null)
		{
			// Fallback: Find by name if group is missing
			player = GetNodeOrNull<Player>("../Player"); // Assuming Player is sibling in Ship.tscn
		}

		if (player != null)
		{
			player.EnterPilotMode(ParentShip, CockpitCameraPosition);
			GD.Print("Taking control of the ship.");
		}
		else
		{
			GD.PrintErr("PilotSeat: Could not find Player node!");
		}
	}
}
