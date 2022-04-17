using Godot;

namespace Fragile
{
    public class BaseMenu : Control
    {
        [Export(PropertyHint.File, "*tscn")]
        private string nextScenePath = "";
        private TransitionRect transitionRect;

        public override void _Ready()
        {
            base._Ready();

            transitionRect = GetNode<TransitionRect>("TransitionRect");
            transitionRect.Connect(nameof(TransitionRect.Finished), this, nameof(TransitionFinished));
            transitionRect.FadeIn(2f);

            var nextSceneButton = GetNode("Main/NextSceneButton");
            nextSceneButton.Connect("pressed", transitionRect, nameof(TransitionRect.FadeOut), new Godot.Collections.Array() { 2f });
            nextSceneButton.Connect("mouse_entered", GlobalNodes.INSTANCE, nameof(GlobalNodes.UIClick));
        }

        private void TransitionFinished(bool fadeIn)
        {
            if (!fadeIn)
            {
                GetTree().ChangeScene(nextScenePath);
            }
        }
    }
}