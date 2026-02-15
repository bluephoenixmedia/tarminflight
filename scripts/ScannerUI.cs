using Godot;
using System;
using System.Collections.Generic;

public partial class ScannerUI : Control
{
    private VBoxContainer _itemList;
    private Button _scanButton;
    private Label _statusLabel;
    
    // We will need a reference to the Ship to initiate warp, or call UniverseManager directly?
    // Better to have an event or call a method on the Ship if the Ship drives the warp.
    // For now, let's assume we can access the Ship player node or use a Signal.
    [Signal]
    public delegate void WarpRequestedEventHandler(Vector3 targetPosition, string targetName);

    public override void _Ready()
    {
        // Setup UI references (assuming a standard layout we will create in .tscn)
        // Setup UI references (assuming a standard layout we will create in .tscn)
        _itemList = GetNode<VBoxContainer>("Panel/VBoxContainer/ScrollContainer/VBoxContainer");
        _scanButton = GetNode<Button>("Panel/VBoxContainer/ScanButton");
        _statusLabel = GetNode<Label>("Panel/VBoxContainer/StatusLabel");

        _scanButton.Pressed += OnScanPressed;
        
        // Initial state
        _statusLabel.Text = "Systems Ready. Initiate Scan.";
    }

    private void OnScanPressed()
    {
        _statusLabel.Text = "Scanning...";
        PopulateList();
    }

    private void PopulateList()
    {
        // Clear existing
        foreach (Node child in _itemList.GetChildren())
        {
            child.QueueFree();
        }

        Chunk currentChunk = UniverseManager.Instance.CurrentChunk;
        if (currentChunk == null)
        {
            _statusLabel.Text = "Error: Nav Computer Offline (No Chunk)";
            return;
        }

        _statusLabel.Text = $"Sector {currentChunk.Coordinates} Scanned. {currentChunk.Items.Count} Signals Detected.";

        foreach (var item in currentChunk.Items)
        {
            // Create a row for each item
            HBoxContainer row = new HBoxContainer();
            
            Label nameLabel = new Label();
            nameLabel.Text = $"{item.Name} [{item.Type}]";
            nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(nameLabel);

            Label distLabel = new Label();
            // Calculate distance
            // Assuming Player is at 0,0,0 relative to the "Sector" when entered? 
            // Or if we track player absolute pos, this might be tricky. 
            // For now, just use item.Position.Length() if player is effectively at center.
            // If we want real distance relative to ship:
            Node3D player = GetTree().GetFirstNodeInGroup("Player") as Node3D; 
            float dist = 0;
            if (player != null)
            {
               dist = item.Position.DistanceTo(player.GlobalPosition); // Approximation if we reset origin
            }
            else 
            {
               dist = item.Position.Length();
            }
            
            distLabel.Text = $"{dist:F0} km"; // 'Units' to 'km'
            row.AddChild(distLabel);

            Button warpBtn = new Button();
            warpBtn.Text = "WARP";
            warpBtn.Pressed += () => InitiateWarp(item); // Capture variable
            row.AddChild(warpBtn);

            _itemList.AddChild(row);
        }
    }

    private void InitiateWarp(SpaceItem item)
    {
        GD.Print($"Warp requested to {item.Name}");
        EmitSignal(SignalName.WarpRequested, item.Position, item.Name);
        _statusLabel.Text = $"Warp sequence initiated to {item.Name}...";
    }
}
