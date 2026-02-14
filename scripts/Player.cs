using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 5.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float Sensitivity = 0.003f;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public Node3D Head;
	public Camera3D Camera;
	public RayCast3D InteractionRay;

	private bool _isPiloting = false;
	private Ship _currentShip;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		Head = GetNode<Node3D>("Head");
		Camera = Head.GetNode<Camera3D>("Camera3D");
		InteractionRay = Camera.GetNode<RayCast3D>("InteractionRay");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Quit Game
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			GetTree().Quit();
		}

		// Allow exiting pilot mode check first
		if (_isPiloting)
		{
			if (Input.IsActionJustPressed("interact")) // Press E to exit
			{
				ExitPilotMode();
			}
			return; // Don't process mouse look if piloting
		}

		if (@event is InputEventMouseMotion mouseMotion)
		{
			Head.RotateY(-mouseMotion.Relative.X * Sensitivity);
			Camera.RotateX(-mouseMotion.Relative.Y * Sensitivity);
			
			// Clamp camera rotation
			Vector3 cameraRot = Camera.Rotation;
			cameraRot.X = Mathf.Clamp(cameraRot.X, Mathf.DegToRad(-90), Mathf.DegToRad(90));
			Camera.Rotation = cameraRot;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isPiloting) return; // Disable movement processing

		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
			velocity.Y = JumpVelocity;

		// Get the input direction and handle the movement/deceleration.
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		// Transform input direction to be relative to the Head's Y rotation (yaw)
		Vector3 direction = (Head.GlobalTransform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();

		// Handle Interaction
		if (Input.IsActionJustPressed("interact"))
		{
			TryInteract();
		}
	}

	private void TryInteract()
	{
		if (InteractionRay.IsColliding())
		{
			var collider = InteractionRay.GetCollider();
			if (collider is Node node && node.HasMethod("Interact"))
			{
				node.Call("Interact");
			}
		}
	}

	public void EnterPilotMode(Ship ship, Node3D cockpitCam)
	{
		_isPiloting = true;
		_currentShip = ship;
		_currentShip.SetPiloted(true);

		// Snap to cockpit
		// We re-parent to the ship so we move with it smoothly without physics jitter
		if (GetParent() != ship)
		{
			GetParent().RemoveChild(this);
			ship.AddChild(this);
		}
		
		// Reset local position/rotation to match the camera mount
		GlobalPosition = cockpitCam.GlobalPosition;
		GlobalRotation = cockpitCam.GlobalRotation;
		
		// Reset Head and Camera rotation to look forward
		Head.Rotation = Vector3.Zero;
		Camera.Rotation = Vector3.Zero;
		
		// Optional: Hide the player body mesh if you have one
	}

	public void ExitPilotMode()
	{
		_isPiloting = false;
		if (_currentShip != null)
		{
			_currentShip.SetPiloted(false);
			_currentShip = null;
		}

		// When exiting, we are still child of the ship, which is fine!
		// We just re-enable physics processing (automatically done by setting _isPiloting false)
		// But we might want to ensure we're standing up? 
		// For now, staying seated visually but regaining movement control will make us 'pop' out of the seat as soon as we move.
		// Let's add a small offset to 'stand up'
		Position += new Vector3(0, 0, 1.0f); 
	}
}
