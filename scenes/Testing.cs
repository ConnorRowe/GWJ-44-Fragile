using Godot;

namespace Fragile
{
    public class Testing : Node2D
    {
        private RigidBody2D wheel1;
        private RigidBody2D wheel2;
        private float acceleration = 30000;
        private float maxSpeed = 50;

        public override void _Ready()
        {
            base._Ready();

            wheel1 = GetNode<RigidBody2D>("Wheel");
            wheel2 = GetNode<RigidBody2D>("Wheel2");
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            float input = 0f;

            if (Input.IsKeyPressed((int)KeyList.A))
            {
                input -= 1f;
            }

            if (Input.IsKeyPressed((int)KeyList.D))
            {
                input += 1f;
            }

            if (wheel1.AngularVelocity < maxSpeed)
                wheel1.ApplyTorqueImpulse(input * acceleration * delta);
            if (wheel2.AngularVelocity < maxSpeed)
                wheel2.ApplyTorqueImpulse(input * acceleration * delta);

            wheel1.AppliedTorque -= (wheel1.AppliedTorque * 0.999f * delta);
            wheel2.AppliedTorque -= (wheel2.AppliedTorque * 0.999f * delta);
        }
    }
}