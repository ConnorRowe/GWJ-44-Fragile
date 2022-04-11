using Godot;
using System.Collections.Generic;

namespace Fragile
{
    public class Vehicle : RigidBody2D
    {
        private static RectangleShape2D cellShape = new RectangleShape2D() { Extents = new Vector2(16, 16) };
        private static CircleShape2D wheelShape = new CircleShape2D() { Radius = 16f };
        private static PhysicsMaterial wheelPhysMat = new PhysicsMaterial() { Bounce = .2f };

        public Stack<RigidBody2D> Wheels = new Stack<RigidBody2D>();
        public Stack<PinJoint2D> PinJoints = new Stack<PinJoint2D>();
        private float acceleration = 30000;
        private float maxSpeed = 50;
        private Dictionary<CollisionShape2D, Construction.Point> colliderPoints = new Dictionary<CollisionShape2D, Construction.Point>();
        private Dictionary<Construction.Point, Sprite> pointSprites = new Dictionary<Construction.Point, Sprite>();
        private Dictionary<Construction.Point, (RigidBody2D wheel, PinJoint2D pin, Sprite wheelSprite)> pointWheels = new Dictionary<Construction.Point, (RigidBody2D wheel, PinJoint2D pin, Sprite wheelSprite)>();

        public override void _Input(InputEvent evt)
        {
            base._Input(evt);

            if (evt is InputEventKey ek && ek.Pressed && ek.Scancode == (int)KeyList.K)
            {
                foreach (var key in colliderPoints.Keys)
                {
                    if (GlobalNodes.RNG.Randf() > .5f)
                    {
                        BreakPart(key);
                        break;
                    }
                }

            }
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            // Control wheels

            float input = 0f;

            if (Input.IsKeyPressed((int)KeyList.A))
            {
                input -= 1f;
            }

            if (Input.IsKeyPressed((int)KeyList.D))
            {
                input += 1f;
            }

            foreach (var wheel in Wheels)
            {
                if (wheel.AngularVelocity < maxSpeed)
                    wheel.ApplyTorqueImpulse(input * acceleration * delta);

                wheel.AppliedTorque -= (wheel.AppliedTorque * .999f * delta);
            }
        }

        public void AddSprite(Texture texture, Construction.Point gridPos)
        {
            AddSprite(texture, gridPos, Vector2.Zero);
        }

        public void AddSprite(Texture texture, Construction.Point gridPos, Vector2 offset)
        {
            Sprite sprite = new Sprite()
            {
                Texture = texture,
                Position = new Vector2(gridPos.x * 32, gridPos.y * 32),
                Offset = offset,
                Centered = false
            };

            pointSprites.Add(gridPos, sprite);

            AddChild(sprite);
        }

        public void AddSquareCollider(Construction.Point gridPos)
        {
            CollisionShape2D collisionShape2D = new CollisionShape2D()
            {
                Position = new Vector2((gridPos.x * 32) + cellShape.Extents.x, (gridPos.y * 32) + cellShape.Extents.y),
                Shape = cellShape
            };

            colliderPoints.Add(collisionShape2D, gridPos);

            AddChild(collisionShape2D);
        }

        public void AddWheel(Construction.Point gridPos, Parts.WheelPart wheelPart)
        {
            AddSprite(wheelPart.AxleTex, gridPos);
            AddSquareCollider(gridPos);

            RigidBody2D wheel = new RigidBody2D()
            {
                Mass = 3f,
                PhysicsMaterialOverride = wheelPhysMat,
                Position = new Vector2((gridPos.x * 32) + 16, ((gridPos.y + 1) * 32) + 16f)
            };

            CollisionShape2D collisionShape2D = new CollisionShape2D()
            {
                Shape = wheelShape
            };

            GetParent().AddChild(wheel);
            wheel.AddChild(collisionShape2D);

            Sprite wheelSprite = new Sprite()
            {
                Texture = wheelPart.WheelTex,
                Offset = new Vector2(0, -16)
            };

            wheel.AddChild(wheelSprite);

            PinJoint2D pinJoint = new PinJoint2D()
            {
                Softness = 4f,
                Position = wheel.Position
            };

            GetParent().AddChild(pinJoint);

            pinJoint.NodeA = pinJoint.GetPathTo(wheel);
            pinJoint.NodeB = pinJoint.GetPathTo(this);

            Wheels.Push(wheel);
            PinJoints.Push(pinJoint);

            pointWheels.Add(gridPos, (wheel, pinJoint, wheelSprite));
        }

        public void SetPositionWithWheels(Vector2 position)
        {
            Vector2 d = position - Position;

            foreach (var wheel in Wheels)
            {
                wheel.Position += d;
            }
            foreach (var pin in PinJoints)
            {
                pin.Position += d;
            }

            Position = position;
        }

        private void BreakPart(CollisionShape2D partCollider)
        {
            Sprite sprite = pointSprites[colliderPoints[partCollider]];

            if (sprite == null)
            {
                GD.PrintErr($"Failed to get sprite from partCollider in BreakPart({partCollider.ToString()}).");
            }
            else
            {
                RigidBody2D brokenPart = new RigidBody2D()
                {
                    Position = sprite.GlobalPosition
                };

                RemoveChild(sprite);
                RemoveChild(partCollider);

                brokenPart.AddChild(sprite);
                brokenPart.AddChild(partCollider);

                sprite.Position = Vector2.Zero;
                partCollider.Position = new Vector2(16, 16);

                pointSprites.Remove(colliderPoints[partCollider]);
                colliderPoints.Remove(partCollider);

                GetParent().AddChild(brokenPart);
            }
        }
    }
}