using Godot;

namespace Fragile
{
    public class GlobalNodes : Node
    {
        public static RandomNumberGenerator RNG { get; } = new RandomNumberGenerator();
        public static PhysicsMaterial RoughPhysMat { get; } = new PhysicsMaterial() { Friction = 1f, Rough = true };
        public static AudioStreamPlayer WindPlayer { get; private set; }
        public static AudioEffectPitchShift WindPitchShift { get; private set; }

        public static GlobalNodes INSTANCE;

        private static AudioStreamPlayer uiClickPlayer;
        private static AudioStreamPlayer bumpPlayer;
        private static AudioStreamPlayer smashPlayer;
        private static AudioStreamPlayer wheelBumpPlayer;
        private static AudioStreamPlayer explosionPlayer;
        private static PackedScene tntExplosionScene = GD.Load<PackedScene>("res://scenes/TntExplosion.tscn");
        private static AudioStreamSample explosionSample = GD.Load<AudioStreamSample>("res://audio/explosion.wav");

        static GlobalNodes()
        {
            RNG.Randomize();
        }

        public static void LoadFromDirectory<T>(string fileDirectory, out T[] objectArrayOut, string targetFileExtension = ".png") where T : Godot.Object
        {
            System.Collections.Generic.List<T> objectList = new System.Collections.Generic.List<T>();
            Directory directory = new Directory();
            directory.Open(fileDirectory);
            directory.ListDirBegin(skipNavigational: true);

            string file = directory.GetNext();
            int extLen = targetFileExtension.Length;
            do
            {
                // check extension
                if (file.Length - 1 >= targetFileExtension.Length && file.Substring(file.Length - extLen) == targetFileExtension)
                {
                    objectList.Add(GD.Load<T>(fileDirectory + file));
                    GD.Print($"Loaded {fileDirectory}{file}");
                }

                file = directory.GetNext();
            } while (!file.Empty());

            objectArrayOut = objectList.ToArray();
        }

        public static void UIClick()
        {
            if (!uiClickPlayer.Playing)
                uiClickPlayer.Play();
        }

        public override void _Ready()
        {
            base._Ready();

            INSTANCE = this;

            uiClickPlayer = GetNode<AudioStreamPlayer>("UIClickPlayer");
            bumpPlayer = GetNode<AudioStreamPlayer>("BumpPlayer");
            smashPlayer = GetNode<AudioStreamPlayer>("SmashPlayer");
            wheelBumpPlayer = GetNode<AudioStreamPlayer>("WheelBumpPlayer");
            WindPlayer = GetNode<AudioStreamPlayer>("WindPlayer");
            explosionPlayer = GetNode<AudioStreamPlayer>("ExplosionPlayer");
            WindPitchShift = (AudioEffectPitchShift)AudioServer.GetBusEffect(AudioServer.GetBusIndex("Wind"), 1);

            GD.Print($"Loaded max_distance as [{SaveData.MaxDistance}]");
        }

        public void MakeTntExplosion(Vector2 pos, Node parent)
        {
            Particles2D explosion = tntExplosionScene.Instance<Particles2D>();
            explosion.Emitting = true;
            explosion.Position = pos;
            parent.AddChild(explosion);

            AudioStreamPlayer2D explosionSound = new AudioStreamPlayer2D()
            {
                MaxDistance = 640,
                Bus = "SFX",
                Stream = explosionSample,
                Autoplay = true
            };

            explosion.AddChild(explosionSound);

            GetTree().CreateTimer(explosion.Lifetime).Connect("timeout", explosion, "queue_free");
        }

        public override void _Input(InputEvent evt)
        {
            base._Input(evt);

            if (evt is InputEventKey ek && ek.Pressed)
            {
                switch (ek.Scancode)
                {
                    case (int)KeyList.F11:
                        OS.WindowFullscreen = !OS.WindowFullscreen;
                        break;
                    case (int)KeyList.F12:
                        PrintStrayNodes();
                        break;
                }
            }
        }

        public static void VehicleBump()
        {
            if (!bumpPlayer.Playing)
                bumpPlayer.Play();
        }

        public static void VehiclePartBreak()
        {
            smashPlayer.Play();
        }

        public static void WheelBump(float volumeDb)
        {
            if (!wheelBumpPlayer.Playing || volumeDb > wheelBumpPlayer.VolumeDb)
            {
                wheelBumpPlayer.VolumeDb = volumeDb;
                wheelBumpPlayer.Play();
            }
        }

        public static void Explosion()
        {
            explosionPlayer.Play();
        }
    }
}