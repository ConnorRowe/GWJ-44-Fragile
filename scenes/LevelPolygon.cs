using Godot;

namespace Fragile
{
    [Tool]
    public class LevelPolygon : Polygon2D
    {
        private static PhysicsMaterial physicsMaterial = new PhysicsMaterial() { Friction = 1f, Rough = true };
        private Color outlineColour = Colors.White;
        private float thickness = 1f;

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
                    PhysicsMaterialOverride = physicsMaterial
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


    }
}