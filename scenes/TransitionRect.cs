using Godot;

namespace Fragile
{
    public class TransitionRect : TextureRect
    {
        [Signal]
        public delegate void Finished(bool fadeIn);

        private Tween tween;
        private bool fadeIn = false;

        public override void _Ready()
        {
            tween = GetNode<Tween>("Tween");

            tween.Connect("tween_all_completed", this, nameof(TweenAllCompleted));
        }

        public void FadeIn(float duration = .5f)
        {
            tween.StopAll();
            tween.InterpolateProperty(Material, "shader_param/percent", 1f, 0f, duration, Tween.TransitionType.Cubic, Tween.EaseType.In);
            tween.Start();

            fadeIn = true;
        }

        public void FadeOut(float duration = .5f)
        {
            tween.StopAll();
            tween.InterpolateProperty(Material, "shader_param/percent", 0f, 1f, duration, Tween.TransitionType.Cubic, Tween.EaseType.Out);
            tween.Start();

            fadeIn = false;
        }

        private void TweenAllCompleted()
        {
            EmitSignal(nameof(Finished), fadeIn);
        }
    }
}