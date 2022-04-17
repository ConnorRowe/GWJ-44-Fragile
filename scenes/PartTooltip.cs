using Godot;

namespace Fragile
{
    public class PartTooltip : Panel
    {
        private Label partName;
        private Label partDesc;
        private Label shortcut;

        public override void _Ready()
        {
            partName = GetNode<Label>("MarginContainer/VBoxContainer/PartName");
            partDesc = GetNode<Label>("MarginContainer/VBoxContainer/PartDesc");
            shortcut = GetNode<Label>("MarginContainer/VBoxContainer/Shortcut");
        }

        public void UpdateFromPart(Parts.MainPart mainPart, int shortcut)
        {
            partName.Text = mainPart.PartName;
            partDesc.Text = mainPart.PartDesc;
            this.shortcut.Text = $" Shortcut: {shortcut}";
        }
    }
}