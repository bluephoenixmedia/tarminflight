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
		
		// Determine Gravity Direction (Down)
		// If we are parented to a Ship (or anything really), use its Down vector for Artificial Gravity.
		// If not, use Global Down (-Y).
		Vector3 gravityDir = Vector3.Down;
		if (GetParent() is Node3D parentNode)
		{
			// Transform local Down (0, -1, 0) to Global
			// Actually, simpler: The parent's -Y basis vector is "Down" locally.
			gravityDir = -parentNode.GlobalTransform.Basis.Y.Normalized();
			
			// Rotate the player to align with the new gravity?
			// Ideally we want the player's Up to be -gravityDir.
			// But for now, let's just apply force.
			UpDirection = -gravityDir;
		}

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += gravityDir * gravity * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
			velocity += UpDirection * JumpVelocity;

		// Get the input direction and handle the movement/deceleration.
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		
		// MOVEMENT LOGIC:
		// We want to move forward/right relative to the Head's view, projected onto the floor plane (perpendicular to Gravity).
		// 1. Get Forward/Right from Head
		Vector3 camForward = -Head.GlobalTransform.Basis.Z;
		Vector3 camRight = Head.GlobalTransform.Basis.X;
		
		// 2. Project onto plane defined by UpDirection
		// Plane normal = UpDirection.
		// Vector3.ProjectOnPlane(vector, normal)
		
		// But Head is child of Player, which... we haven't rotated to match UpDirection explicitly every frame,
		// relying on initial placement.
		// Let's project camera vectors against UpDirection.
		
		// Manual project: v - (v . n) * n
		Vector3 forwardProjected = (camForward - (camForward.Dot(UpDirection) * UpDirection)).Normalized();
		Vector3 rightProjected = (camRight - (camRight.Dot(UpDirection) * UpDirection)).Normalized();
		
		Vector3 direction = (forwardProjected * -inputDir.Y + rightProjected * inputDir.X).Normalized();

		if (direction != Vector3.Zero)
		{
			// Apply speed along the surface
			// We remove existing velocity along the movement plane first? 
			// No, standard character controller logic:
			
			// We want to set the planar velocity to (direction * Speed)
			// But keep the vertical (gravity) velocity.
			
			Vector3 verticalVelocity = velocity.Project(UpDirection);
			Vector3 planarVelocity = direction * Speed;
			
			velocity = verticalVelocity + planarVelocity;
		}
		else
		{
			// Decelerate planar velocity
			Vector3 verticalVelocity = velocity.Project(UpDirection);
			Vector3 planarVelocity = velocity - verticalVelocity;
			
			planarVelocity = planarVelocity.MoveToward(Vector3.Zero, Speed);
			velocity = verticalVelocity + planarVelocity;
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
		GD.Print("[PLAYER] Entering Pilot Mode");
		_isPiloting = true;
		_currentShip = ship;
		_currentShip.SetPiloted(true);

		// Snap to cockpit
		if (GetParent() != ship)
		{
			GetParent().RemoveChild(this);
			ship.AddChild(this);
		}
		
		GlobalPosition = cockpitCam.GlobalPosition;
		GlobalRotation = cockpitCam.GlobalRotation;
		
		Head.Rotation = Vector3.Zero;
		Camera.Rotation = Vector3.Zero;
	}

	public void ExitPilotMode()
	{
		GD.Print("[PLAYER] Exiting Pilot Mode");
		_isPiloting = false;
		if (_currentShip != null)
		{
			_currentShip.SetPiloted(false);
			_currentShip = null;
		}

        Input.MouseMode = Input.MouseModeEnum.Captured;
		GD.Print($"[PLAYER] MouseMode set to Captured. IsPiloting: {_isPiloting}");

		Position += new Vector3(0, 0, 1.5f); 
        Velocity = Vector3.Zero;
		GD.Print($"[PLAYER] Position reset to {Position}");
	}
}
