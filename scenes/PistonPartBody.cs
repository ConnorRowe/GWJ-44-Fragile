using Godot;

namespace Fragile
{
    public class PistonPartBody : Node2D
    {
        private CollisionShape2D collisionShape2D;
        public CollisionShape2D CollisionShape2D { get { return collisionShape2D; } set { collisionShape2D = value; collisionStartPos = value.Position; } }
        private Vector2 collisionStartPos;
        [Export]
        public Vector2 CollisionShapeOffset { get { return (collisionShape2D == null ? Vector2.Zero : collisionShape2D.Position); } set { collisionShape2D.Position = collisionStartPos + value; } }

        public void SetDisabled(bool disabled)
        {
            collisionShape2D.Disabled = disabled;
        }
    }
}