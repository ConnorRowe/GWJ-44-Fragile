using Godot;

namespace Fragile
{
    [Tool]
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

        public override void _EnterTree()
        {
            base._EnterTree();

            if (Engine.EditorHint)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void FadeIn(float duration = .5f)
        {
            tween.RemoveAll();
            tween.InterpolateProperty(Material, "shader_param/percent", 1f, 0f, duration, Tween.TransitionType.Cubic, Tween.EaseType.In);
            tween.Start();

            fadeIn = true;
        }

        public void FadeIn(Color colour, float duration = .5f)
        {
            ((ShaderMaterial)this.Material).SetShaderParam("bg_colour", colour);
            FadeIn(duration);
        }

        public void FadeOut(float duration = .5f)
        {
            tween.RemoveAll();
            tween.InterpolateProperty(Material, "shader_param/percent", 0f, 1f, duration, Tween.TransitionType.Cubic, Tween.EaseType.Out);
            tween.Start();

            fadeIn = false;
        }

        public void FadeOut(Color colour, float duration = .5f)
        {
            ((ShaderMaterial)this.Material).SetShaderParam("bg_colour", colour);
            FadeOut(duration);
        }

        private void TweenAllCompleted()
        {
            EmitSignal(nameof(Finished), fadeIn);
        }
    }
}