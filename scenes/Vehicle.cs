using Godot;
using System.Collections.Generic;
using Fragile.Parts;
using Point = Fragile.Construction.Point;

namespace Fragile
{
    public class Vehicle : RigidBody2D
    {
        private static RectangleShape2D cellShape = new RectangleShape2D() { Extents = new Vector2(16, 16) };
        private static RectangleShape2D springShape = new RectangleShape2D() { Extents = new Vector2(10, 4) };
        private static CircleShape2D wheelShape = new CircleShape2D() { Radius = 16f };
        private static PhysicsMaterial wheelPhysMat = new PhysicsMaterial() { Bounce = .2f, Rough = true, Friction = 1f };
        private static PackedScene engineSmokeScene = GD.Load<PackedScene>("res://scenes/EngineSmoke.tscn");
        private static PackedScene nutsNBoltsScene = GD.Load<PackedScene>("res://scenes/NutsNBolts.tscn");

        public List<RigidBody2D> Wheels = new List<RigidBody2D>();
        public List<PinJoint2D> PinJoints = new List<PinJoint2D>();

        private float acceleration = 10000f;
        private float maxSpeed = 25f;
        private float accelerationScale = 0f;
        private float maxSpeedScale = 0f;

        private float forceNeededToBreak = 16f;
        private float jumpStrength = 128f;

        private Tween tween;

        private Dictionary<CollisionShape2D, Point> colliderPoints = new Dictionary<CollisionShape2D, Point>();
        private Dictionary<Point, CollisionShape2D> pointColliders = new Dictionary<Point, CollisionShape2D>();
        private Dictionary<Point, Sprite> pointSprites = new Dictionary<Point, Sprite>();
        private Dictionary<Point, (RigidBody2D wheel, PinJoint2D pin, Sprite wheelSprite)> pointWheels = new Dictionary<Point, (RigidBody2D wheel, PinJoint2D pin, Sprite wheelSprite)>();
        private Dictionary<Point, Particles2D> pointEngineSmokes = new Dictionary<Point, Particles2D>();
        private List<RigidBody2D> springWheels = new List<RigidBody2D>();
        private List<CollisionShape2D> springs = new List<CollisionShape2D>();
        private List<int> springShapeIdxs = new List<int>();

        private float velocityLenDelta = 0f;
        private Vector2 lastFrameVelocity = Vector2.Zero;
        private uint lastColliderHitIndex = 0;
        private bool showSmoke = false;
        private bool canSpring = true;
        private bool isJumping = false;

        private float maxVelLenDel = 0;

        public override void _Ready()
        {
            base._Ready();

            tween = new Tween();
            AddChild(tween);

            ContactMonitor = true;
            ContactsReported = 1;

            Connect("body_shape_entered", this, nameof(BodyShapeEntered));
        }

        public override void _Input(InputEvent evt)
        {
            base._Input(evt);

            if (evt.IsActionPressed("g_jump") && canSpring)
            {
                SpringJump();
            }
        }

        public override void _Process(float delta)
        {
            base._Process(delta);

            if ((Input.IsActionJustPressed("g_accelerate") || Input.IsActionJustPressed("g_brake")) && !showSmoke)
            {
                SetEngineSmokeEmitting(true);
            }
            else if (showSmoke && !Input.IsActionPressed("g_accelerate") && !Input.IsActionPressed("g_brake"))
            {
                SetEngineSmokeEmitting(false);
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            float inputMove = 0f;
            float inputTilt = 0f;

            if (Input.IsActionPressed("g_brake"))
                inputMove -= 1f;

            if (Input.IsActionPressed("g_accelerate"))
                inputMove += 1f;

            if (Input.IsActionPressed("g_tilt_left"))
                inputTilt -= 1f;

            if (Input.IsActionPressed("g_tilt_right"))
                inputTilt += 1f;

            // Tilt control
            ApplyTorqueImpulse(inputTilt * delta * 50000 * accelerationScale);
            if (Mathf.Abs(AppliedTorque) > 5f)
            {
                AppliedTorque = 5f * Mathf.Sign(AppliedTorque);
            }

            // Acceleration
            if (!isJumping)
            {
                foreach (var wheel in Wheels)
                {
                    if (wheel.AngularVelocity < maxSpeed * maxSpeedScale)
                        wheel.ApplyTorqueImpulse(inputMove * acceleration * accelerationScale * delta);

                    wheel.AppliedTorque -= (wheel.AppliedTorque * .999f * delta);
                }
            }

            // Hit detection ~ basically figures out how abruptly the velocity changes
            Vector2 d = LinearVelocity * delta * Mass;
            velocityLenDelta = (lastFrameVelocity - d).LengthSquared();
            lastFrameVelocity = d;

            if (velocityLenDelta > maxVelLenDel)
                maxVelLenDel = velocityLenDelta;

            // Velocity changed sharply - probably hit something
            if (velocityLenDelta > forceNeededToBreak)
            {
                var hitShape = ShapeOwnerGetOwner(ShapeFindOwner((int)lastColliderHitIndex));

                if (hitShape != null && hitShape is CollisionShape2D hitCollisionShape)
                    BreakPart(hitCollisionShape, true);
                else
                    GD.PrintErr($"hitShape == null\nlastColliderHitIndex=={lastColliderHitIndex}");
            }
        }

        public void BodyShapeEntered(RID bodyRID, Node body, int bodyShapeIndex, uint localShapeIndex)
        {
            // Saves index of the last hit shape so it knows which part should break
            if (!springShapeIdxs.Contains((int)localShapeIndex))
                lastColliderHitIndex = localShapeIndex;

            // GD.Print($"BodyShapeEntered({bodyRID.ToString()}, {body}, {bodyShapeIndex}, {localShapeIndex})");
        }

        public void AddSprite(Texture texture, Point gridPos)
        {
            AddSprite(texture, gridPos, Vector2.Zero);
        }

        public void AddSprite(Texture texture, Point gridPos, Vector2 offset)
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

        public void AddSquareCollider(Point gridPos)
        {
            if (pointColliders.ContainsKey(gridPos))
                return;

            CollisionShape2D collisionShape2D = new CollisionShape2D()
            {
                Position = new Vector2((gridPos.x * 32) + cellShape.Extents.x, (gridPos.y * 32) + cellShape.Extents.y),
                Shape = cellShape
            };

            colliderPoints.Add(collisionShape2D, gridPos);
            pointColliders.Add(gridPos, collisionShape2D);

            AddChild(collisionShape2D);
        }

        public void AddWheel(Point gridPos, Parts.WheelPart wheelPart)
        {
            AddSprite(wheelPart.Texture, gridPos);
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
                Offset = new Vector2(0, -16),
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

            Wheels.Add(wheel);
            PinJoints.Add(pinJoint);

            pointWheels.Add(gridPos, (wheel, pinJoint, wheelSprite));

            if (wheelPart == Parts.Parts.WheelSpring)
            {
                springWheels.Add(wheel);


                CollisionShape2D spring = new CollisionShape2D()
                {
                    Shape = springShape,
                    Position = (gridPos * 32).ToVector2() + new Vector2(16, 32)
                };

                AddChild(spring);

                springs.Add(spring);
                springShapeIdxs.Add(spring.GetIndex());
            }
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

        private void BreakPart(CollisionShape2D partCollider, bool checkFloating)
        {
            if (!colliderPoints.ContainsKey(partCollider))
                return;

            Point partPoint = colliderPoints[partCollider];
            Part part = Construction.GetGridPart(partPoint + Construction.RootPartPos);

            NewNutsNBolts(partCollider.GlobalPosition);

            if (part is MainPart mainPart)
            {
                Sprite sprite = pointSprites[partPoint];

                if (sprite == null)
                {
                    GD.PrintErr($"Failed to get sprite from partCollider in BreakPart({partCollider.ToString()}).");
                }
                else
                {
                    RigidBody2D brokenPart = new RigidBody2D()
                    {
                        Position = sprite.GlobalPosition,
                        Rotation = Rotation,
                        PhysicsMaterialOverride = GlobalNodes.RoughPhysMat
                    };

                    Weight -= mainPart.Mass;

                    RemoveChild(sprite);
                    RemoveChild(partCollider);

                    brokenPart.AddChild(sprite);
                    brokenPart.AddChild(partCollider);

                    sprite.Position = Vector2.Zero;
                    partCollider.Position = new Vector2(16, 16);

                    pointSprites.Remove(colliderPoints[partCollider]);
                    colliderPoints.Remove(partCollider);
                    pointColliders.Remove(partPoint);

                    GetParent().AddChild(brokenPart);

                    Construction.SetGridPart(partPoint + Construction.RootPartPos, null);

                    if (mainPart is EnginePart enginePart)
                    {
                        RemoveEngineStats(enginePart);
                        RemoveEngineSmoke(partPoint);
                    }

                    if (mainPart is WheelPart wheelPart)
                    {
                        var wheelNodes = pointWheels[partPoint];

                        wheelNodes.pin.QueueFree();

                        Wheels.Remove(wheelNodes.wheel);
                        PinJoints.Remove(wheelNodes.pin);
                        pointWheels.Remove(partPoint);

                        Construction.SetGridPart(partPoint + new Point(0, 1) + Construction.RootPartPos, null);

                        if (wheelPart == Parts.Parts.WheelSpring)
                        {
                            int idx = springWheels.IndexOf(wheelNodes.wheel);
                            springWheels.RemoveAt(idx);
                            springs[idx].QueueFree();
                            springs.RemoveAt(idx);
                        }
                    }
                    else
                    {
                        foreach (var extraOffset in mainPart.ExtraParts)
                        {
                            var extraPoint = partPoint + extraOffset;

                            var extraCollider = pointColliders[extraPoint];

                            RemoveChild(extraCollider);
                            brokenPart.AddChild(extraCollider);
                            extraCollider.Position = new Vector2(16 + (extraOffset.x * 32), 16 + (extraOffset.y * 32));

                            colliderPoints.Remove(extraCollider);
                            pointColliders.Remove(extraPoint);

                            Construction.SetGridPart(extraPoint + Construction.RootPartPos, null);
                        }
                    }

                    brokenPart.ApplyImpulse(new Vector2(GlobalNodes.RNG.Randf(), GlobalNodes.RNG.Randf()), Vector2.Up * 200f);
                }
            }
            else if (part is ExtraPart extraPart)
            {
                BreakPart(pointColliders[extraPart.OwnerPart - Construction.RootPartPos], false);
            }

            // Break any disconnected parts
            if (checkFloating)
            {
                foreach (var gridPoint in Construction.GetDisconnectedParts())
                {
                    var pos = gridPoint - Construction.RootPartPos;

                    CollisionShape2D collider;
                    if (pointColliders.TryGetValue(pos, out collider))
                        BreakPart(collider, false);
                }
            }
        }

        public void AddEngineStats(EnginePart enginePart)
        {
            accelerationScale += enginePart.Acceleration;
            maxSpeedScale += enginePart.MaxSpeed;
        }

        public void RemoveEngineStats(EnginePart enginePart)
        {
            accelerationScale -= enginePart.Acceleration;
            maxSpeedScale -= enginePart.MaxSpeed;
        }

        public void AddEngineSmoke(Point gridPos, Vector2 offset)
        {
            Particles2D newEngineSmoke = engineSmokeScene.Instance<Particles2D>();
            newEngineSmoke.Position = (gridPos * 32).ToVector2() + offset;
            pointEngineSmokes.Add(gridPos, newEngineSmoke);
            newEngineSmoke.Emitting = false;
            AddChild(newEngineSmoke);
        }

        public void RemoveEngineSmoke(Point point)
        {
            pointEngineSmokes[point].QueueFree();
            pointEngineSmokes.Remove(point);
        }

        public void SetEngineSmokeEmitting(bool emit)
        {
            foreach (var smoke in pointEngineSmokes.Values)
            {
                smoke.Emitting = emit;
            }

            showSmoke = emit;
        }

        private async void NewNutsNBolts(Vector2 position)
        {
            var p = nutsNBoltsScene.Instance<Particles2D>();
            p.Emitting = true;
            GetParent().AddChild(p);
            p.Position = position;

            var timer = GetTree().CreateTimer(p.Lifetime);
            await ToSignal(timer, "timeout");

            p.QueueFree();
        }

        private async void SpringJump()
        {
            canSpring = false;
            isJumping = true;

            var springDir = Vector2.Down.Rotated(Rotation);

            foreach (var spring in springs)
            {
                Vector2 springStartPos = spring.Position;
                spring.Disabled = false;

                tween.InterpolateProperty(spring, "position", springStartPos, springStartPos + (jumpStrength * springDir), .5f);
                tween.InterpolateProperty(spring, "position", springStartPos + (jumpStrength * springDir), springStartPos, .2f, delay: .5f);
                tween.InterpolateProperty(spring, "disabled", false, true, .01f, delay: .5f);
            }

            tween.Start();

            var timer = GetTree().CreateTimer(.7f);
            await ToSignal(timer, "timeout");

            isJumping = false;

            timer = GetTree().CreateTimer(.2f);
            await ToSignal(timer, "timeout");

            canSpring = true;
        }

        public void SelfDestruct()
        {
            foreach (var dir in Construction.FourDirs)
            {
                if (pointColliders.ContainsKey(dir))
                {
                    BreakPart(pointColliders[dir], true);
                }
            }
        }
    }
}