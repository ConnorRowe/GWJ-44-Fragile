using Godot;

namespace Fragile
{
    [Tool]
    public class LevelPolygon : Polygon2D
    {
        private Color outlineColour = Colors.White;
        private float thickness = 1f;
        private System.Collections.Generic.Dictionary<int, Texture> milestoneTextures = new System.Collections.Generic.Dictionary<int, Texture>()
        {
            {int.MaxValue, GD.Load<Texture>("res://textures/checker_bg_1000.png")},
            {500, GD.Load<Texture>("res://textures/checker_bg_500.png")},
            {250, GD.Load<Texture>("res://textures/checker_bg_200.png")},
            {150, GD.Load<Texture>("res://textures/checker_bg_100.png")},
            {50, GD.Load<Texture>("res://textures/checker_bg_50.png")},
            {int.MinValue, GD.Load<Texture>("res://textures/checker_bg.png")},
        };

        [Export]
        public Color OutlineColour { get { return outlineColour; } set { outlineColour = value; Update(); } }
        [Export]
        public float Thickness { get { return thickness; } set { thickness = value; Update(); } }

        public override void _Ready()
        {
            base._Ready();

            if (!Engine.EditorHint)
            {
                StaticBody2D staticBody = new StaticBody2D()
                {
                    PhysicsMaterialOverride = GlobalNodes.RoughPhysMat
                };
                CollisionPolygon2D collisionPolygon = new CollisionPolygon2D()
                {
                    Polygon = this.Polygon
                };

                AddChild(staticBody);
                staticBody.AddChild(collisionPolygon);
            }
        }

        public override void _Draw()
        {
            base._Draw();

            DrawPolyline(Polygon, outlineColour, thickness);
            DrawLine(Polygon[Polygon.Length - 1], Polygon[0], outlineColour, thickness);
        }

        public void UpdateTextureFromPos()
        {
            foreach (int milestone in milestoneTextures.Keys)
            {
                if (GlobalPosition.x / 32 >= milestone)
                {
                    Texture = milestoneTextures[milestone];
                    break;
                }
            }
        }
    }
}