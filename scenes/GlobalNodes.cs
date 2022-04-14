using Godot;

namespace Fragile
{
    public class GlobalNodes : Node
    {
        public static RandomNumberGenerator RNG { get; } = new RandomNumberGenerator();
        public static PhysicsMaterial RoughPhysMat { get; } = new PhysicsMaterial() { Friction = 1f, Rough = true };

        public static GlobalNodes INSTANCE;

        private static AudioStreamPlayer uiClickPlayer;

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
        }
    }
}