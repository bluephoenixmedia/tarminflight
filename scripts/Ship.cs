using Godot;
using System;

public partial class Ship : CharacterBody3D
{
	[Export] public float MaxSpeed = 50.0f;
	[Export]
	public float Acceleration = 20.0f;
	[Export]
	public float RotationSpeed = 2.0f;

	private float _currentSpeed = 0.0f;
	private bool _isPiloted = false;

	public bool IsPiloted { get { return _isPiloted; } }

	public void SetPiloted(bool piloted)
	{
		_isPiloted = piloted;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_isPiloted) 
		{
			// Even if not piloted, maybe drift or just dampen speed?
			// For now, let's keep previous speed logic if we want momentum, but likely we want to stop.
			// _currentSpeed = Mathf.MoveToward(_currentSpeed, 0, Acceleration * (float)delta);
			return;
		}


		float dt = (float)delta;
        

		// Rotation (Pitch/Yaw/Roll)
		Vector3 rotInput = Vector3.Zero;
		// User requested inverted controls (or just swapped).
		// Previously: "move_forward" (W) -> -1, "move_backward" (S) -> +1
		// We negate the result to invert.
		rotInput.X = -Input.GetAxis("move_forward", "move_backward"); // Pitch
		rotInput.Y = Input.GetAxis("move_right", "move_left");       // Yaw (A/D)
		// flt roll = Input.GetAxis("roll_left", "roll_right"); // Q/E (TODO: Add Input Map)

		// Apply Rotation
		RotateObjectLocal(Vector3.Right, rotInput.X * RotationSpeed * dt);
		RotateObjectLocal(Vector3.Up, rotInput.Y * RotationSpeed * dt);
		
		// Thrust
		float thrust = 0;
		if (Input.IsActionPressed("ui_accept")) // Space
			thrust = 1;
		else if (Input.IsActionPressed("interact")) // E (Used for thrust if holding?) - Maybe define new inputs later
			thrust = 0; // Placeholder

		// Forward Movement (Always forward when throttled up, for now just hold Space to move)
		// Starflight style: You usually set a speed.
		
		if (Input.IsActionPressed("ui_accept")) // Space to accelerate
		{
			_currentSpeed = Mathf.MoveToward(_currentSpeed, MaxSpeed, Acceleration * dt);
		}
		else
		{
			_currentSpeed = Mathf.MoveToward(_currentSpeed, 0, Acceleration * dt); // Auto-brake for now
		}

		Velocity = -GlobalTransform.Basis.Z * _currentSpeed;
		MoveAndSlide();
	}
}
