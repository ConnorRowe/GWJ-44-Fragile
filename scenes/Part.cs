using Godot;
using Point = Fragile.Construction.Point;

namespace Fragile.Parts
{
    public static class Parts
    {
        private static readonly Point UP = new Point(0, -1);
        private static readonly Point RIGHT = new Point(1, 0);
        private static readonly Point DOWN = new Point(0, 1);
        private static readonly Point LEFT = new Point(-1, 0);

        public static RootPart RootPart = new RootPart();
        public static MainPart Body = new MainPart(3, "Body", GD.Load<Texture>("res://textures/body.png"), Vector2.Zero);
        public static EnginePart EngineSmall = new EnginePart
        (
            8,
            "Small Engine",
            GD.Load<Texture>("res://textures/engine_small.png"),
            Vector2.Zero,
            .5f,
            .5f,
            new Vector2(16, 16)
        );
        public static EnginePart EngineLarge = new EnginePart
        (
            12,
            "Large Engine",
            GD.Load<Texture>("res://textures/engine_large.png"),
            new Vector2(-32, -32),
            1.5f,
            1.5f,
            new Vector2(-16, 8),
            new Point[2] { LEFT, UP }
        );
        public static WheelPart WheelStandard = new WheelPart(8, "Standard Wheel", GD.Load<Texture>("res://textures/axle_standard.png"), GD.Load<Texture>("res://textures/wheel_standard.png"));
        public static WheelPart WheelSpring = new WheelPart(8, "Spring Wheel", GD.Load<Texture>("res://textures/axle_spring.png"), GD.Load<Texture>("res://textures/wheel_spring.png"));
    }

    public abstract class Part : Godot.Object
    {
        public string PartName { get; set; }
    }

    public sealed class ExtraPart : Part
    {
        public Point OwnerPart { get; }

        public ExtraPart() { }
        public ExtraPart(Point ownerPart)
        {
            OwnerPart = ownerPart;
        }
    }

    public sealed class RootPart : Part { }

    public class MainPart : Part
    {
        public int Mass { get; }
        public Point[] ExtraParts { get; } = new Point[0];
        public Texture Texture { get; }
        public Vector2 TexOffset { get; }

        public MainPart() { }
        public MainPart(int mass, string partName, Texture texture, Vector2 texOffset)
        {
            Mass = mass;
            PartName = partName;
            Texture = texture;
            TexOffset = texOffset;
        }
        public MainPart(int mass, string partName, Texture texture, Vector2 texOffset, Point[] extraParts) : this(mass, partName, texture, texOffset)
        {
            ExtraParts = extraParts;
        }
    }

    public sealed class WheelPart : MainPart
    {
        public Texture WheelTex { get; }

        public WheelPart(int mass, string partName, Texture partTexture, Texture wheelTexture) : base(mass, partName, partTexture, Vector2.Zero, new Point[] { new Point(0, 1) })
        {
            WheelTex = wheelTexture;
        }
    }

    public sealed class EnginePart : MainPart
    {
        public float MaxSpeed { get; }
        public float Acceleration { get; }
        public Vector2 SmokeOffset { get; }

        public EnginePart(int mass, string partName, Texture texture, Vector2 texOffset, float maxSpeed, float acceleration, Vector2 smokeOffset) : base(mass, partName, texture, texOffset)
        {
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            SmokeOffset = smokeOffset;
        }

        public EnginePart(int mass, string partName, Texture texture, Vector2 texOffset, float maxSpeed, float acceleration, Vector2 smokeOffset, Point[] extraParts) : base(mass, partName, texture, texOffset, extraParts)
        {
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            SmokeOffset = smokeOffset;
        }
    }
}