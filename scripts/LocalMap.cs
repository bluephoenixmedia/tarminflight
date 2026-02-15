using Godot;
using System;
using System.Collections.Generic;

public partial class LocalMap : Control
{
    [Export] public float MapRange = 2000.0f; // Range in world units to show
    [Export] public Node3D PlayerShip; // Can be assigned or found

    private Control _shipIcon;
    private Control _blipContainer;
    private Dictionary<Node3D, Control> _blips = new Dictionary<Node3D, Control>();

    private double _timeSinceLastDump = 0;

    public override void _Ready()
    {
        GD.Print("[RADAR] LocalMap Ready initialized.");
        _shipIcon = GetNode<Control>("ShipIcon");
        _blipContainer = GetNode<Control>("BlipContainer");
        
        // Find player if not assigned
        if (PlayerShip == null)
        {
             PlayerShip = GetTree().GetFirstNodeInGroup("Player") as Node3D;
             if (PlayerShip == null) GD.PrintErr("[RADAR] PlayerShip NOT FOUND!");
             else GD.Print($"[RADAR] PlayerShip found: {PlayerShip.Name}");
        }

        // FAILSAFE: Auto-add known children of Ship to RadarTargets if they seem like space objects
        // This handles cases where the Group is removed from scenes.
        if (PlayerShip != null && PlayerShip.GetParent() is Node3D shipNode)
        {
            foreach(Node child in shipNode.GetChildren())
            {
                if (child.Name.ToString().Contains("Planet") || child.Name.ToString().Contains("Asteroid"))
                {
                    if (!child.IsInGroup("RadarTargets"))
                    {
                        child.AddToGroup("RadarTargets");
                        GD.Print($"[RADAR] Auto-added 'RadarTargets' group to {child.Name}");
                    }
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        if (PlayerShip == null) return;
        
        UpdateBlips();

        // ASCII Dump every 10 seconds
        _timeSinceLastDump += delta;
        if (_timeSinceLastDump > 10.0)
        {
            _timeSinceLastDump = 0;
            DumpAsciiRadar();
        }
    }

    private void DumpAsciiRadar()
    {
        var targets = GetTree().GetNodesInGroup("RadarTargets");
        Vector3 forward3D = -PlayerShip.GlobalTransform.Basis.Z;
        Vector2 forward2D = new Vector2(forward3D.X, forward3D.Z).Normalized();
        GD.Print($"\n=== RADAR ASCII DUMP (Targets: {targets.Count}) ===");
        GD.Print($"[RADAR] Ship Forward Vector (XZ): {forward2D}");
        GD.Print($"[RADAR] Ship Heading Angle: {Mathf.RadToDeg(forward2D.Angle())} degrees");
        
        // Create 20x20 grid
        char[,] grid = new char[21, 21];
        for(int y=0; y<21; y++)
            for(int x=0; x<21; x++) 
                grid[x,y] = '.';

        // Center
        grid[10,10] = 'P'; // Player

        foreach (Node node in targets)
        {
            if (node is Node3D target)
            {
                // Calculate position relative to player
                Vector3 rel = PlayerShip.GlobalTransform.Basis.Inverse() * (target.GlobalPosition - PlayerShip.GlobalPosition);
                // Map Range coverage: -MapRange to +MapRange
                // Grid: 0..20, Center 10.
                // Scale factor: 10 units / MapRange
                
                float xPerc = rel.X / MapRange;
                float zPerc = rel.Z / MapRange; // Z is forward/back

                int gx = 10 + (int)(xPerc * 10);
                int gy = 10 + (int)(zPerc * 10);

                if (gx >= 0 && gx <= 20 && gy >= 0 && gy <= 20)
                {
                    grid[gx, gy] = 'O'; // Object
                }
            }
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for(int y=0; y<21; y++)
        {
            for(int x=0; x<21; x++)
            {
                sb.Append(grid[x,y]);
            }
            sb.Append("\n");
        }
        GD.Print(sb.ToString());
        GD.Print("=========================================\n");
    }

    private void UpdateBlips()
    {
        // 1. Identify targets
        var targets = GetTree().GetNodesInGroup("RadarTargets");
        // GD.Print($"[RADAR] Targets Found: {targets.Count}"); // Comment out to avoid spam, or print only on change?
        // Let's print if count changes or every N frames? 
        // For debugging "not seeing anything", let's print if 0.
        if (targets.Count == 0)
        {
             // GD.Print("[RADAR] No targets found in group 'RadarTargets'.");
        }
        else
        {
             // GD.Print($"[RADAR] Updating {targets.Count} targets.");
        }

        // Remove invalid blips
        List<Node3D> toRemove = new List<Node3D>();
        foreach (var node in _blips.Keys)
        {
            if (!IsInstanceValid(node) || !targets.Contains(node))
            {
                toRemove.Add(node);
            }
        }
        foreach (var node in toRemove)
        {
            _blips[node].QueueFree();
            _blips.Remove(node);
        }

        // Add/Update blips
        foreach (Node node in targets)
        {
            Node3D target3D = node as Node3D;
            if (target3D == null) continue;

            if (!_blips.ContainsKey(target3D))
            {
                CreateBlip(target3D);
            }

            UpdateBlipPosition(target3D, _blips[target3D]);
        }
    }

    private void CreateBlip(Node3D target)
    {
        ColorRect blip = new ColorRect();
        blip.Size = new Vector2(4, 4);
        blip.Color = new Color(1, 0, 0); // Red for enemies? Or generic
        blip.PivotOffset = blip.Size / 2;
        
        // Differentiate?
        // If target has metadata or name?
        if (target.Name.ToString().Contains("Planet")) blip.Color = Colors.Cyan;
        else if (target.Name.ToString().Contains("Station")) blip.Color = Colors.Green;
        
        _blipContainer.AddChild(blip);
        _blips[target] = blip;
    }

    private void UpdateBlipPosition(Node3D target, Control blip)
    {
        // 1. Get relative position in Global Space
        Vector3 diff = target.GlobalPosition - PlayerShip.GlobalPosition;
        
        // 2. Project to 2D (XZ plane)
        Vector2 diff2D = new Vector2(diff.X, diff.Z);
        
        // 3. Get Player's Heading on XZ plane (ignoring Pitch/Roll)
        // Forward is -Z in Godot
        Vector3 forward3D = -PlayerShip.GlobalTransform.Basis.Z;
        Vector2 forward2D = new Vector2(forward3D.X, forward3D.Z).Normalized();
        
        // 4. Calculate Angle
        // We want to rotate the world so that 'forward2D' points UP (0, -1)
        // Godot Angle(): Right=(1,0) is 0. Down=(0,1) is PI/2. Up=(0,-1) is -PI/2.
        
        // If forward2D is (0, -1) [-PI/2], we want 0 rotation (it's already Up).
        // If forward2D is (1, 0) [0], we want -PI/2 rotation to make it Up.
        // Rotation = (-PI/2) - HeadingAngle.
        
        float headingAngle = forward2D.Angle();
        float rotationNeeded = -Mathf.Pi / 2.0f - headingAngle;
        
        Vector2 screenPos = diff2D.Rotated(rotationNeeded);
        
        // CORRECTION: User reported horizontal mirroring (Left objects appear on Right).
        // This is likely due to UV mapping on the screen mesh.
        // We simply flip the X coordinate here.
        screenPos.X = -screenPos.X;

        // Scale to map size
        Vector2 mapSize = Size;
        Vector2 center = mapSize / 2;
        
        float scale = (Mathf.Min(mapSize.X, mapSize.Y) / 2.0f) / MapRange;

        screenPos *= scale;
        
        // Clamp to circle or square?
        if (screenPos.Length() > (Mathf.Min(mapSize.X, mapSize.Y) / 2.0f))
        {
            blip.Visible = false;
        }
        else
        {
            blip.Visible = true;
            blip.Position = center + screenPos - blip.PivotOffset;
            
            // Optional: Rotate blip if it's an arrow?
            // blip.Rotation = ...
        }
    }
}
