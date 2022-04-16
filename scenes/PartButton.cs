using Godot;

namespace Fragile
{
    public class PartButton : Button
    {
        private Label countLabel;

        public override void _Ready()
        {
            base._Ready();

            countLabel = GetNode<Label>("MarginContainer/CountLabel");
        }

        public void SetPart(Parts.MainPart part)
        {
            if (part is Parts.WheelPart wheelPart)
            {
                GetNode<TextureRect>("MarginContainer1/PartIcon").Texture = wheelPart.WheelTex;
            }
            else
                GetNode<TextureRect>("MarginContainer1/PartIcon").Texture = part.Texture;
            var size = part.Texture.GetSize();
            float scale = 32f / (size.x > size.y ? size.x : size.y);
            var margins = GetNode<MarginContainer>("MarginContainer1");
            margins.RectScale = new Vector2(scale, scale);
            margins.MarginLeft = -(int)((scale - 1) * 4);
            margins.MarginTop = margins.MarginLeft;

        }

        public void SetCount(int count)
        {
            countLabel.Text = $"x {count}";

            Disabled = count == 0;
        }
    }
}