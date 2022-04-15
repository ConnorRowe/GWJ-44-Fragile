using Godot;

namespace Fragile
{
    public class PartTooltip : Panel
    {
        private Label partName;
        private Label partDesc;

        public override void _Ready()
        {
            partName = GetNode<Label>("MarginContainer/VBoxContainer/PartName");
            partDesc = GetNode<Label>("MarginContainer/VBoxContainer/PartDesc");
        }

        public void UpdateFromPart(Parts.MainPart mainPart)
        {
            partName.Text = mainPart.PartName;
            partDesc.Text = mainPart.PartDesc;
        }
    }
}