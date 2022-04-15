using Godot;

namespace Fragile
{
    public class WheelBody2D : RigidBody2D
    {
        private Vector2 lastFrameVelocity = Vector2.Zero;
        private float velocityLenDelta = 0;

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            Vector2 v = LinearVelocity * delta;
            velocityLenDelta = (lastFrameVelocity - v).LengthSquared();
            lastFrameVelocity = v;

            if (velocityLenDelta > 2.5f)
            {
                GlobalNodes.WheelBump(GD.Linear2Db(Mathf.Clamp(velocityLenDelta / 20f, 0f, 1f)));
            }
        }
    }
}