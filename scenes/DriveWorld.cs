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
        private Label distLabel;
        private Label fps;
        private float furthestDistance = 0;

        private Vector2 vehicleStartPos = new Vector2(280, 120);


        public override void _Ready()
        {
            base._Ready();

            noGoinBack = GetNode<StaticBody2D>("NoGoinBack");

            levelChunkA = GD.Load<PackedScene>("res://scenes/DriveChunks/StarterChunk.tscn").Instance<Node2D>();
            AddChild(levelChunkA);

            levelChunkB = GetRandomLevelChunk();
            AddChild(levelChunkB);
            levelChunkB.MoveLocalX(960);

            distLabel = GetNode<Label>("UI/DistanceLabel");
            fps = GetNode<Label>("UI/fps");
        }

        public void SetVehicle(Vehicle vehicle)
        {
            this.vehicle = vehicle;

            vehicle.SetPositionWithWheels(vehicleStartPos);

            var selfDestructBtn = GetNode("UI/SelfDestruct");
            selfDestructBtn.Connect("pressed", vehicle, nameof(vehicle.SelfDestruct));
            selfDestructBtn.Connect("mouse_entered", GlobalNodes.INSTANCE, nameof(GlobalNodes.UIClick));
        }

        public override void _Process(float delta)
        {
            base._Process(delta);

            float dist = vehicle.Position.x / 32f;
            if (dist > furthestDistance)
                furthestDistance = dist;

            bool kilo = furthestDistance > 1000f;
            if (kilo)
            {
                distLabel.Text = $"{(furthestDistance * 0.001f):0.00}km";
            }
            else
            {
                distLabel.Text = $"{furthestDistance:0}m";
            }

            fps.Text = $"{Engine.GetFramesPerSecond()}";
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