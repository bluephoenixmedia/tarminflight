using Godot;
using System;

public partial class Interactable : StaticBody3D
{
	[Export] public string PromptMessage = "Interact";

	public virtual void Interact()
	{
		GD.Print("Interacted with " + Name);
	}
}
