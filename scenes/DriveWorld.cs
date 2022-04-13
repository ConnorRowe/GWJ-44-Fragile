using Godot;

namespace Fragile
{
    public class DriveWorld : Node2D
    {
        private static PackedScene[] levelChunks;

        static DriveWorld()
        {
            GlobalNodes.LoadFromDirectory<PackedScene>("res://scenes/DriveChunks/RandomChunks/", out levelChunks, ".tscn");
        }

        private Vehicle vehicle;
        private Node2D levelChunkA;
        private Node2D levelChunkB;
        private StaticBody2D noGoinBack;


        public override void _Ready()
        {
            base._Ready();

            noGoinBack = GetNode<StaticBody2D>("NoGoinBack");

            levelChunkA = GetRandomLevelChunk();
            AddChild(levelChunkA);

            levelChunkB = GetRandomLevelChunk();
            AddChild(levelChunkB);
            levelChunkB.MoveLocalX(960);
        }

        public void SetVehicle(Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            if (vehicle != null)
            {
                if (vehicle.Position.x >= levelChunkB.Position.x + 240)
                {
                    levelChunkA.QueueFree();
                    levelChunkA = levelChunkB;

                    levelChunkB = GetRandomLevelChunk();
                    AddChild(levelChunkB);
                    levelChunkB.Position = levelChunkA.Position + new Vector2(960, 0);

                    noGoinBack.Position = levelChunkA.Position;

                    GD.Print("Generated more level...");
                }
            }
        }

        private Node2D GetRandomLevelChunk()
        {
            return levelChunks[GlobalNodes.RNG.RandiRange(0, levelChunks.Length - 1)].Instance<Node2D>();
        }
    }
}