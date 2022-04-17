using Godot;

namespace Fragile
{
    public class Intro : Node2D
    {
        public override void _Ready()
        {
            base._Ready();

            GetNode("AnimationPlayer").Connect("animation_finished", this, nameof(SwitchToMainMenu));
        }

        private void SwitchToMainMenu(string animName)
        {
            if (animName == "ThemeWildCards")
            {
                GetTree().ChangeScene("res://scenes/menus/MainMenu.tscn");
                QueueFree();
            }
        }
    }
}