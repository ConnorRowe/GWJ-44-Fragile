using Godot;

namespace Fragile
{
    public static class SaveData
    {
        private const string filePath = "user://save.ini";
        private static ConfigFile Save { get; } = new ConfigFile();

        public static int MaxDistance => (int)Save.GetValue("Fragile", "max_distance");

        static SaveData()
        {
            var e = Save.Load(filePath);

            if (e == Error.Ok && Save.HasSection("Fragile"))
            {
                GD.Print("Loaded save data.");
            }
            else
            {
                GD.PrintErr("Error loading save. Re-creating.", e);
                Save.SetValue("Fragile", "max_distance", 0);
                Save.Save(filePath);
                Save.Load(filePath);
            }
        }

        public static void SaveMaxDistance(int maxDistance)
        {
            Save.SetValue("Fragile", "max_distance", maxDistance);
            Save.Save(filePath);
        }
    }
}