using Godot;

namespace Fragile.Parts
{
    public static class Parts
    {
        private static readonly Construction.Point UP = new Construction.Point(0, -1);
        private static readonly Construction.Point RIGHT = new Construction.Point(1, 0);
        private static readonly Construction.Point DOWN = new Construction.Point(0, 1);
        private static readonly Construction.Point LEFT = new Construction.Point(-1, 0);

        public static RootPart RootPart = new RootPart();
        public static MainPart Body = new MainPart(20, "Body", GD.Load<Texture>("res://textures/body.png"), Vector2.Zero);
        public static MainPart EngineLarge = new MainPart(15, "Large Engine", GD.Load<Texture>("res://textures/engine.png"), new Vector2(-32, -32), new Construction.Point[2] { LEFT, UP });
        public static WheelPart Wheel = new WheelPart(30, "Standard Wheel", GD.Load<Texture>("res://textures/wheel.png"), GD.Load<Texture>("res://textures/axle.png"), GD.Load<Texture>("res://textures/wheelonly.png"));
    }

    public abstract class Part : Godot.Object
    {
        public string PartName { get; set; }
    }

    public sealed class ExtraPart : Part
    {
        public Construction.Point OwnerPart { get; }

        public ExtraPart() { }
        public ExtraPart(Construction.Point ownerPart)
        {
            OwnerPart = ownerPart;
        }
    }

    public sealed class RootPart : Part { }

    public class MainPart : Part
    {
        public int Strength { get; }
        public Construction.Point[] ExtraParts { get; } = new Construction.Point[0];
        public Texture Texture { get; }
        public Vector2 TexOffset { get; }

        public MainPart() { }
        public MainPart(int strength, string partName, Texture texture, Vector2 texOffset)
        {
            Strength = strength;
            PartName = partName;
            Texture = texture;
            TexOffset = texOffset;
        }
        public MainPart(int strength, string partName, Texture texture, Vector2 texOffset, Construction.Point[] extraParts) : this(strength, partName, texture, texOffset)
        {
            ExtraParts = extraParts;
        }
    }

    public sealed class WheelPart : MainPart
    {
        public Texture AxleTex { get; }
        public Texture WheelTex { get; }

        public WheelPart(int strength, string partName, Texture partTexture, Texture axleTexture, Texture wheelTexture) : base(strength, partName, partTexture, Vector2.Zero, new Construction.Point[] { new Construction.Point(0, 1) })
        {
            AxleTex = axleTexture;
            WheelTex = wheelTexture;
        }
    }
}