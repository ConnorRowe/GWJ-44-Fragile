using Godot;

namespace Fragile
{
    public class Intro : Node2D
    {
        public override void _Ready()
        {
            base._Ready();

            GetNode("AnimationPlayer").Connect("animation_finished", this, nameof(AnimFinished));
        }

        public override void _Input(InputEvent evt)
        {
            if (evt is InputEventKey ek && ek.Pressed && ek.Scancode == (int)KeyList.Space)
                SwitchToMainMenu();
        }

        private void SwitchToMainMenu()
        {
            GetTree().ChangeScene("res://scenes/menus/MainMenu.tscn");
            QueueFree();
        }

        private void AnimFinished(string animName)
        {
            if (animName == "ThemeWildCards")
            {
                SwitchToMainMenu();
            }
        }
    }
}