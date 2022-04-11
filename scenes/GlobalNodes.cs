using Godot;

namespace Fragile
{
    public class GlobalNodes : Node
    {
        public static RandomNumberGenerator RNG { get; } = new RandomNumberGenerator();

        static GlobalNodes()
        {
            RNG.Randomize();
        }
    }
}