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
    private bool _isWarping = false;
    
    private ScannerUI _scannerUI;
    private PlanetScanUI _planetScanUI;
    private CanvasLayer _uiLayer;
    private Node3D _currentSpaceObject; // The currently spawned object we are near
    private SpaceItem _currentSpaceItem; // The data for the current object

	public bool IsPiloted { get { return _isPiloted; } }

	public void SetPiloted(bool piloted)
	{
		_isPiloted = piloted;
	}

    public override void _Ready()
    {
        // Setup Scanner UI
        _uiLayer = new CanvasLayer();
        AddChild(_uiLayer);
        
        var uiScene = GD.Load<PackedScene>("res://scenes/ScannerUI.tscn");
        if (uiScene != null)
        {
            _scannerUI = uiScene.Instantiate<ScannerUI>();
            _scannerUI.Visible = false;
            _uiLayer.AddChild(_scannerUI);
            _scannerUI.WarpRequested += OnWarpRequested;
        }

        var scanScene = GD.Load<PackedScene>("res://scenes/PlanetScanUI.tscn");
        if (scanScene != null)
        {
            _planetScanUI = scanScene.Instantiate<PlanetScanUI>();
            _planetScanUI.Visible = false;
            _uiLayer.AddChild(_planetScanUI);
        }

        // Setup Local Map
        var subViewport = GetNode<SubViewport>("InstrumentPanel/ScreenCenter/SubViewport");
        var screenMesh = GetNode<CsgBox3D>("InstrumentPanel/ScreenCenter");
        
        if (subViewport != null && screenMesh != null)
        {
            subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
            
            // Create a new material
            StandardMaterial3D mat = new StandardMaterial3D();
            
            // IMPORTANT: Set base color to black so if texture has transparency or issues, it's not white.
            mat.AlbedoColor = Colors.Black; 
            
            mat.AlbedoTexture = subViewport.GetTexture();
            mat.EmissionEnabled = true;
            mat.Emission = Colors.Black; // Use texture for color
            mat.EmissionEnergyMultiplier = 1.0f;
            mat.EmissionTexture = subViewport.GetTexture();
            
            screenMesh.Material = mat;
            GD.Print("Ship: Local Map Material Applied with ViewportTexture.");
        }
        else
        {
            GD.PrintErr("Ship: Failed to find SubViewport or ScreenCenter for LocalMap.");
        }
    }

    public override void _Input(InputEvent @event)
    {
        // Toggle Scanner with 'M' key
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.M && _isPiloted && !_isWarping)
            {
                ToggleScanner();
            }
            else if (keyEvent.Keycode == Key.N && _isPiloted && !_isWarping)
            {
                PerformScan();
            }
        }
    }

    private void ToggleScanner()
    {
        _scannerUI.Visible = !_scannerUI.Visible;
        if (_scannerUI.Visible)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _planetScanUI.Visible = false; // Close other UI
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    private void PerformScan()
    {
        if (_currentSpaceItem == null)
        {
            GD.Print("Ship: No object to scan.");
            return;
        }

        if (_currentSpaceItem.Type == SpaceItemType.Planet)
        {
            _planetScanUI.Populate(_currentSpaceItem);
            _planetScanUI.Visible = true;
            _scannerUI.Visible = false; // Close other UI
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            GD.Print($"Ship: Scanning {_currentSpaceItem.Name}... No planetary data.");
            // Could show a generic "No Data" or different UI for other objects logic here
        }
    }

    private async void OnWarpRequested(Vector3 targetPos, string targetName)
    {
        if (_isWarping) return;
        _isWarping = true;
        
        // Close UI
        _scannerUI.Visible = false;
        _planetScanUI.Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;

        GD.Print($"Ship: Initiating Warp to {targetName} at {targetPos}...");

        // 1. Rotate to face target (Instant snap for prototype)
        // We need to look at the target. Since targetPos is relative to the "Sector Center", 
        // and we might be anywhere, let's assume we reset origin or calculate vector.
        // For simplicity: We treat 'targetPos' as the destination. 
        // If we are at (0,0,0), we look at targetPos.
        if (targetPos != Vector3.Zero)
        {
            LookAt(GlobalPosition + targetPos.Normalized(), Vector3.Up);
        }

        // 2. Charge / Wind up
        var camera = GetViewport().GetCamera3D();
        float originalFov = camera.Fov;
        
        Tween tween = CreateTween();
        tween.TweenProperty(camera, "fov", 110.0f, 1.5f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
        
        await ToSignal(GetTree().CreateTimer(1.5f), "timeout");

        // 3. Warp Travel (Visuals only, maybe shake)
        // Move ship or just fake it. 
        // Let's spawn particles or something in the future.
        // For now, fast forward.
        GD.Print("Ship: WARP ENGAGED");
        _currentSpeed = MaxSpeed * 10; // Fake speed reading
        
        // Travel time based on distance? Let's just do 3 seconds.
        await ToSignal(GetTree().CreateTimer(3.0f), "timeout");

        // 4. Arrival
        GD.Print($"Ship: Arrived at {targetName}");
        _currentSpeed = 0;
        
        // Clean up previous object if dynamic
        if (_currentSpaceObject != null)
        {
            _currentSpaceObject.QueueFree();
            _currentSpaceObject = null;
        }
        _currentSpaceItem = null; // Clear item reference

        // Teleport Ship to arrival point (300 units away from target, facing it)
        // We assume we are already facing the target from Step 1.
        // Forward is -Z, so +Z is backwards. We want to be 300 units 'back' from the target.
        GlobalPosition = targetPos + (GlobalTransform.Basis.Z * 300.0f);

        // Spawn new object
        // We find the item in the manager to get the path
        var item = UniverseManager.Instance.CurrentChunk.Items.Find(i => i.Name == targetName);
        _currentSpaceItem = item; // Store reference

        if (item != null && !string.IsNullOrEmpty(item.ResourcePath))
        {
            var scene = GD.Load<PackedScene>(item.ResourcePath);
            if (scene != null)
            {
                _currentSpaceObject = scene.Instantiate<Node3D>();
                _currentSpaceObject.AddToGroup("RadarTargets");
                GD.Print($"[SHIP] Spawned {_currentSpaceObject.Name} from {item.ResourcePath} and added to 'RadarTargets'.");
                GetParent().AddChild(_currentSpaceObject); // Add to world
                _currentSpaceObject.GlobalPosition = targetPos;
            }
        }
        else if (item != null)
        {
             GD.Print($"Ship: No scene for {item.Type}. Spawning placeholder.");
             MeshInstance3D sphere = new MeshInstance3D();
             SphereMesh mesh = new SphereMesh();
             mesh.Radius = 50;
             mesh.Height = 100;
             sphere.Mesh = mesh;
             _currentSpaceObject = sphere;
             GetParent().AddChild(sphere);
             _currentSpaceObject.GlobalPosition = targetPos;
        }

        // 5. Cooldown / Reset
        tween = CreateTween();
        tween.TweenProperty(camera, "fov", originalFov, 1.0f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        
        _isWarping = false;
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

        // Disable movement controls during warp
        if (_isWarping)
        {
            Velocity = -GlobalTransform.Basis.Z * _currentSpeed; // Keep moving fast
            MoveAndSlide();
            return;
        }
        
        // Cannot control ship if UI is open (Mouse Visible)
        if (Input.MouseMode == Input.MouseModeEnum.Visible)
        {
            // Auto-brake
            _currentSpeed = Mathf.MoveToward(_currentSpeed, 0, Acceleration * (float)delta);
            Velocity = -GlobalTransform.Basis.Z * _currentSpeed;
            MoveAndSlide();
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
        else if (Input.IsKeyPressed(Key.Shift)) // Shift to Reverse
        {
            _currentSpeed = Mathf.MoveToward(_currentSpeed, -MaxSpeed * 0.5f, Acceleration * dt);
        }
		else
		{
			_currentSpeed = Mathf.MoveToward(_currentSpeed, 0, Acceleration * dt); // Auto-brake for now
		}

		Velocity = -GlobalTransform.Basis.Z * _currentSpeed;
		MoveAndSlide();
	}
}
