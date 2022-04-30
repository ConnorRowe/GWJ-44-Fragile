using Godot;
using Point = Fragile.Construction.Point;

namespace Fragile.Parts
{
    public static class Parts
    {
        public static RootPart RootPart = new RootPart();
        public static MainPart Body = new MainPart(3, "Body", "Just connects other parts together.", GD.Load<Texture>("res://textures/body.png"), new Vector2(-16, -16));
        public static EnginePart EngineSmall = new EnginePart
        (
            8,
            "Small Engine",
            "Provides a little bit of power.",
            GD.Load<Texture>("res://textures/engine_small.png"),
            new Vector2(-16, -16),
            .5f,
            .5f,
            new Vector2(16, 16)
        );
        public static EnginePart EngineLarge = new EnginePart
        (
            12,
            "Large Engine",
            "Provides a hefty boost of power.",
            GD.Load<Texture>("res://textures/engine_large.png"),
            new Vector2(-48, -48),
            1.5f,
            1.5f,
            new Vector2(-24, 0),
            new (Point point, bool isSolid)[2] { (Point.Left, true), (Point.Up, true) }
        );
        public static WheelPart WheelStandard = new WheelPart
        (
            6,
            "Standard Wheel",
            "Lets you roll along.",
            GD.Load<Texture>("res://textures/axle_standard.png"),
            GD.Load<Texture>("res://textures/wheel_standard.png")
        );
        public static JumboWheelPart WheelJumbo = new JumboWheelPart
        (
            12,
            "Jumbo Wheel",
            "Like a standard wheel, but jumbo :D",
            GD.Load<Texture>("res://textures/axle_jumbo.png"),
            GD.Load<Texture>("res://textures/wheel_jumbo.png")
        );
        public static PistonPart Piston = new PistonPart(6, "Piston", "Can launch you up with the power of hydraulics! [press spacebar to activate]");
    }

    public abstract class Part : Godot.Object
    {
        public string PartName { get; protected set; }
        public string PartDesc { get; protected set; }
        public bool IsSolid { get; protected set; } = true;
    }

    public sealed class ExtraPart : Part
    {
        public Point OwnerPart { get; }

        public ExtraPart() { }
        public ExtraPart(Point ownerPart, bool isSolid)
        {
            OwnerPart = ownerPart;
            IsSolid = isSolid;
        }
    }

    public sealed class RootPart : Part { }

    public class MainPart : Part
    {
        public int Mass { get; }
        public (Point point, bool isSolid)[] ExtraParts { get; protected set; } = new (Point, bool)[0];
        public Texture Texture { get; }
        public Vector2 TexOffset { get; }

        public MainPart() { }
        public MainPart(int mass, string partName, string partDesc, Texture texture, Vector2 texOffset)
        {
            Mass = mass;
            PartName = partName;
            PartDesc = partDesc;
            Texture = texture;
            TexOffset = texOffset;
            IsSolid = true;
        }
        public MainPart(int mass, string partName, string partDesc, Texture texture, Vector2 texOffset, (Point point, bool isSolid)[] extraParts) : this(mass, partName, partDesc, texture, texOffset)
        {
            ExtraParts = extraParts;
        }
    }

    public class WheelPart : MainPart
    {
        public Texture WheelTex { get; }

        public WheelPart(int mass, string partName, string partDesc, Texture partTexture, Texture wheelTexture) : base(mass, partName, partDesc, partTexture, new Vector2(-16, -16), new (Point point, bool isSolid)[1] { (new Point(0, 1), false) })
        {
            WheelTex = wheelTexture;
        }
    }

    public sealed class JumboWheelPart : WheelPart
    {
        public JumboWheelPart(int mass, string partName, string partDesc, Texture partTexture, Texture wheelTexture) : base(mass, partName, partDesc, partTexture, wheelTexture)
        {
            this.ExtraParts = new (Point point, bool isSolid)[5] { (new Point(1, 0), true), (new Point(0, 1), false), (new Point(1, 1), false), (new Point(0, 2), false), (new Point(1, 2), false) };
        }
    }

    public sealed class EnginePart : MainPart
    {
        public float MaxSpeed { get; }
        public float Acceleration { get; }
        public Vector2 SmokeOffset { get; }

        public EnginePart(int mass, string partName, string partDesc, Texture texture, Vector2 texOffset, float maxSpeed, float acceleration, Vector2 smokeOffset) : base(mass, partName, partDesc, texture, texOffset)
        {
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            SmokeOffset = smokeOffset;
        }

        public EnginePart(int mass, string partName, string partDesc, Texture texture, Vector2 texOffset, float maxSpeed, float acceleration, Vector2 smokeOffset, (Point point, bool isSolid)[] extraParts) : base(mass, partName, partDesc, texture, texOffset, extraParts)
        {
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            SmokeOffset = smokeOffset;
        }
    }

    public sealed class PistonPart : MainPart
    {
        public float Power { get; } = 400;

        public PistonPart(int mass, string partName, string partDesc) : base(mass, partName, partDesc, GD.Load<Texture>("res://textures/piston_housing.png"), new Vector2(-16, -16))
        {
            this.ExtraParts = new (Point point, bool isSolid)[2] { (Point.Down, false), (new Point(0, 2), false) };
        }
    }
}