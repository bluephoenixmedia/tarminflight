using Godot;
using System;
using System.Collections.Generic;

public enum SpaceItemType
{
    Planet,
    AsteroidField,
    SpaceStation,
    AbandonedShip,
    Wreckage
}

public class PlanetData
{
    public float Radius; // km
    public float Gravity; // g
    public float Temperature; // C
    public string AtmosphereComposition;
    public string Weather;
}

public class SpaceItem
{
    public string Name;
    public SpaceItemType Type;
    public Vector3 Position; // Logical position in the chunk relative to center (0,0,0)
    public float InteractionDistance = 200.0f; // Distance from center of object to arrive at
    public string ResourcePath; // Path to scene file if applicable
    public PlanetData Data; // Null if not a planet
}

public class Chunk
{
    public Vector2I Coordinates; // Grid coordinates (e.g., 0,0)
    public List<SpaceItem> Items = new List<SpaceItem>();
}

public partial class UniverseManager : Node
{
    public static UniverseManager Instance { get; private set; }

    public List<Chunk> Chunks = new List<Chunk>();
    public Chunk CurrentChunk;
    public SpaceItem CurrentLocation = null; // null means deep space

    private Random _random = new Random();

    public override void _Ready()
    {
        Instance = this;
        GenerateUniverse();
    }

    private void GenerateUniverse()
    {
        Chunks.Clear();
        // Generate 3x3 grid centered at 0,0
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Chunk newChunk = new Chunk();
                newChunk.Coordinates = new Vector2I(x, y);
                GenerateChunkContents(newChunk);
                Chunks.Add(newChunk);
            }
        }
        
        // Start player in center chunk (0,0)
        CurrentChunk = GetChunk(0, 0);
        GD.Print($"[UNIVERSE] Universe Generated. Current Chunk: {CurrentChunk.Coordinates}. Total Chunks: {Chunks.Count}");
        GD.Print($"[UNIVERSE] Initial Chunk Items: {CurrentChunk.Items.Count}");
        foreach(var item in CurrentChunk.Items)
        {
             GD.Print($"[UNIVERSE] - {item.Name} at {item.Position} ({item.Type})");
        }
    }

    public Chunk GetChunk(int x, int y)
    {
        var c = Chunks.Find(c => c.Coordinates.X == x && c.Coordinates.Y == y);
        if (c == null) GD.PrintErr($"[UNIVERSE] Chunk {x},{y} NOT FOUND!");
        return c;
    }

    private void GenerateChunkContents(Chunk chunk)
    {
        // Ensure at least one planet per chunk
        int planetCount = _random.Next(1, 4);
        for (int i = 0; i < planetCount; i++)
        {
            chunk.Items.Add(CreateRandomItem(SpaceItemType.Planet));
        }

        // Random other stuff
        int otherCount = _random.Next(2, 6);
        for (int i = 0; i < otherCount; i++)
        {
            SpaceItemType type = (SpaceItemType)_random.Next(1, 5); // Skip Planet (0)
            chunk.Items.Add(CreateRandomItem(type));
        }
    }

    private SpaceItem CreateRandomItem(SpaceItemType type)
    {
        SpaceItem item = new SpaceItem();
        item.Type = type;
        
        switch (type)
        {
            case SpaceItemType.Planet:
                item.Name = $"Planet {_random.Next(100, 999)}-{(char)_random.Next('A', 'Z')}";
                item.ResourcePath = "res://scenes/Planet.tscn";
                
                // Generate Planet Data
                item.Data = new PlanetData();
                item.Data.Radius = _random.Next(3000, 15000); // km
                item.Data.Gravity = (float)(_random.NextDouble() * 2.5 + 0.5); // 0.5g to 3.0g
                item.Data.Temperature = _random.Next(-200, 500); // Celsius
                
                string[] atmospheres = { "Nitrogen-Oxygen", "Carbon Dioxide", "Hydrogen-Helium", "Methane", "None", "Sulfur Dioxide" };
                item.Data.AtmosphereComposition = atmospheres[_random.Next(atmospheres.Length)];
                
                string[] weathers = { "Clear", "Stormy", "Acid Rain", "Dust Storms", "High Winds", "Calm" };
                item.Data.Weather = weathers[_random.Next(weathers.Length)];
                break;
                
            case SpaceItemType.SpaceStation:
                item.Name = $"Station {_random.Next(100, 999)}-{(char)_random.Next('A', 'Z')}";
                string[] stations = { "Floating_Station_01.fbx", "Floating_Station_02.fbx" };
                item.ResourcePath = $"res://assets/models/{stations[_random.Next(stations.Length)]}";
                break;
                
            case SpaceItemType.AbandonedShip:
                item.Name = $"Derelict Ship {_random.Next(100, 999)}-{(char)_random.Next('A', 'Z')}";
                // Spaceship_01.fbx to Spaceship_10.fbx
                int shipNum = _random.Next(1, 11);
                string shipFile = $"Spaceship_{shipNum:D2}.fbx"; // pads with 0 e.g. 01
                item.ResourcePath = $"res://assets/models/{shipFile}";
                break;
                
            case SpaceItemType.Wreckage:
                item.Name = $"Debris Field {_random.Next(100, 999)}-{(char)_random.Next('A', 'Z')}";
                string[] wreckage = { 
                    "Destroyed_Satellite_Plate.fbx", 
                    "Space suit .fbx", 
                    "Space Helmet .fbx",
                    "Fusil box.fbx"
                };
                item.ResourcePath = $"res://assets/models/{wreckage[_random.Next(wreckage.Length)]}";
                break;

            case SpaceItemType.AsteroidField:
                item.Name = $"Asteroid Field {_random.Next(100, 999)}-{(char)_random.Next('A', 'Z')}";
                // Maybe use Drone here as a patrol?
                // Or proper asteroids if we have them. 
                // User mentioned Drone. Let's make Asteroid Field have Drones for now or randomize.
                if (_random.NextDouble() > 0.5)
                {
                    item.Name = $"Drone Patrol {_random.Next(100, 999)}";
                    item.ResourcePath = "res://assets/models/Drone.fbx";
                }
                else
                {
                    // Fallback or Placeholder for actual asteroids if no model
                    item.ResourcePath = ""; // Will use sphere placeholder
                }
                break;
                
            default:
                item.Name = $"{type} {_random.Next(100, 999)}";
                break;
        }

        // Realistic distances: 5000 to 50000 units away from center?
        // Or much larger? Let's say 10,000 to 100,000 for "Scanning distance"
        // Godot units are usually meters. 100km is 100,000 units.
        float dist = _random.Next(5000, 50000); 
        Vector3 dir = new Vector3((float)_random.NextDouble() - 0.5f, (float)_random.NextDouble() - 0.5f, (float)_random.NextDouble() - 0.5f).Normalized();
        item.Position = dir * dist;

        return item;
    }

    // Helper to get distance from a point (like the ship) to an item
    public float GetDistanceTo(Vector3 shipPosition, SpaceItem item)
    {
        // If we are in the same "scene" context, we might use local coords.
        // But here 'item.Position' is the 'Simulated' position in the chunk.
        // 'shipPosition' depends on if we reset the origin.
        // For now, assume ship is near 0,0,0 relative to the "Sector Center" if we haven't warped.
        // If we have warped to another item, our "0,0,0" is THAT item.
        return item.Position.DistanceTo(shipPosition); 
    }
}
