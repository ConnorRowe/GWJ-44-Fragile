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
        private static CircleShape2D wheelJumboShape = new CircleShape2D() { Radius = 24f };
        private static PhysicsMaterial wheelPhysMat = new PhysicsMaterial() { Bounce = .2f, Rough = true, Friction = 1f };
        private static PackedScene engineSmokeScene = GD.Load<PackedScene>("res://scenes/EngineSmoke.tscn");
        private static PackedScene nutsNBoltsScene = GD.Load<PackedScene>("res://scenes/NutsNBolts.tscn");
        private static PackedScene pistonScene = GD.Load<PackedScene>("res://scenes/PistonPart.tscn");

        public List<RigidBody2D> Wheels = new List<RigidBody2D>();
        public List<PinJoint2D> PinJoints = new List<PinJoint2D>();

        [Signal]
        public delegate void NoMoreEngines();

        private float acceleration = 10000f;
        private float maxSpeed = 25f;
        private float accelerationScale = 0f;
        private float maxSpeedScale = 0f;

        private float forceNeededToBreak = 20f;
        private float jumpStrength = 128f;

        public Camera2D Camera2D { get; set; }

        private Tween tween;
        private EngineSoundPlayer engineSoundPlayer;

        private Dictionary<CollisionShape2D, Point> colliderPoints = new Dictionary<CollisionShape2D, Point>();
        private Dictionary<Point, CollisionShape2D> pointColliders = new Dictionary<Point, CollisionShape2D>();
        private Dictionary<Point, Sprite> pointSprites = new Dictionary<Point, Sprite>();
        private Dictionary<Point, (RigidBody2D wheel, PinJoint2D pin, Sprite wheelSprite)> pointWheels = new Dictionary<Point, (RigidBody2D wheel, PinJoint2D pin, Sprite wheelSprite)>();
        private Dictionary<Point, Particles2D> pointEngineSmokes = new Dictionary<Point, Particles2D>();
        private Dictionary<Point, PistonPartBody> pointPistons = new Dictionary<Point, PistonPartBody>();
        private List<PistonPartBody> pistons = new List<PistonPartBody>();
        private List<int> pistonShapeIdxs = new List<int>();

        private float velocityLenDelta = 0f;
        private Vector2 lastFrameVelocity = Vector2.Zero;
        private int lastColliderHitIndex = -1;
        private bool showSmoke = false;
        private bool canJump = true;
        private bool isJumping = false;

        private float maxVelLenDel = 0;

        public override void _Ready()
        {
            base._Ready();

            tween = new Tween();
            AddChild(tween);

            engineSoundPlayer = new EngineSoundPlayer();
            AddChild(engineSoundPlayer);

            GlobalNodes.WindPlayer.Play();

            Connect("body_shape_entered", this, nameof(BodyShapeEntered));
        }

        public override void _Input(InputEvent evt)
        {
            base._Input(evt);

            if (evt.IsActionPressed("g_jump") && canJump)
            {
                ActivatePistons();
            }
        }

        public override void _Process(float delta)
        {
            base._Process(delta);

            if ((Input.IsActionJustPressed("g_accelerate") || Input.IsActionJustPressed("g_brake")) && !showSmoke)
            {
                SetEngineSmokeEmitting(true);
                if (maxSpeedScale > 0)
                    engineSoundPlayer.StartEngine();
            }
            else if (showSmoke && !Input.IsActionPressed("g_accelerate") && !Input.IsActionPressed("g_brake"))
            {
                SetEngineSmokeEmitting(false);
                if (maxSpeedScale > 0)
                    engineSoundPlayer.StopEngine();
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            if (Layers == 0)
                return;

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
            if (velocityLenDelta > forceNeededToBreak && lastColliderHitIndex > 0)
            {
                try
                {
                    var hitShape = ShapeOwnerGetOwner(ShapeFindOwner(lastColliderHitIndex));

                    if (hitShape != null && hitShape is CollisionShape2D hitCollisionShape)
                    {
                        BreakPart(hitCollisionShape, true);
                        lastColliderHitIndex = -1;
                    }
                    else
                        GD.PrintErr($"hitShape == null\nlastColliderHitIndex=={lastColliderHitIndex}");
                }
                catch (System.Exception e)
                {
                    GD.Print($"oopsie ... {e.Message}");
                }
            }
            else if (velocityLenDelta > 5f)
            {
                GlobalNodes.VehicleBump();
            }

            //Wind effect
            float windSpeedScale = Mathf.Clamp(LinearVelocity.LengthSquared() * 0.000005f, 0, 1);
            GlobalNodes.WindPlayer.VolumeDb = GD.Linear2Db(windSpeedScale);
            GlobalNodes.WindPitchShift.PitchScale = .8f + (windSpeedScale * .4f);

            // Remove vehicle rotation for camera
            if (Camera2D != null)
            {
                Camera2D.GlobalPosition = Position + new Vector2(128, 32);
            }
        }

        public void BodyShapeEntered(RID bodyRID, Node body, int bodyShapeIndex, uint localShapeIndex)
        {
            if (pistonShapeIdxs.Contains((int)localShapeIndex))
                return;

            // Saves index of the last hit shape so it knows which part should break
            lastColliderHitIndex = (int)localShapeIndex;

            // GD.Print($"bodyShapeIndex={bodyShapeIndex}, localShapeIndex={localShapeIndex}");

            // GD.Print($"BodyShapeEntered({bodyRID.ToString()}, {body}, {bodyShapeIndex}, {localShapeIndex})");
        }

        public Sprite AddSprite(Texture texture, Point gridPos)
        {
            return AddSprite(texture, gridPos, Vector2.Zero);
        }

        public Sprite AddSprite(Texture texture, Point gridPos, Vector2 offset)
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

            return sprite;
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

            bool jumbo = wheelPart is Parts.JumboWheelPart;

            if (jumbo)
                AddSquareCollider(gridPos + new Point(1, 0));

            RigidBody2D wheel = new WheelBody2D()
            {
                Mass = 3f,
                PhysicsMaterialOverride = wheelPhysMat,
                Position = this.Position + new Vector2((gridPos.x * 32) + 16, ((gridPos.y + 1) * 32) + 16f),
                Layers = 0,
                GravityScale = 0,
            };

            if (jumbo)
                wheel.Position = this.Position + new Vector2((gridPos.x + 1) * 32, ((gridPos.y + 2) * 32) + 16f);

            CollisionShape2D collisionShape2D = new CollisionShape2D()
            {
                Shape = jumbo ? wheelJumboShape : wheelShape
            };

            GetParent().AddChild(wheel);
            wheel.AddChild(collisionShape2D);

            Sprite wheelSprite = new Sprite()
            {
                Texture = wheelPart.WheelTex,
                Offset = new Vector2(0, jumbo ? -24 : -16),
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
        }

        public void AddPiston(Point gridPoint, Parts.PistonPart pistonPart)
        {
            AddSprite(pistonPart.Texture, gridPoint);

            PistonPartBody piston = pistonScene.Instance<PistonPartBody>();
            AddChild(piston);
            piston.Position = (gridPoint * 32).ToVector2() + new Vector2(16, 16);

            CollisionShape2D collisionShape2D = new CollisionShape2D()
            {
                Position = new Vector2((gridPoint.x * 32) + cellShape.Extents.x, (gridPoint.y * 32) + cellShape.Extents.y),
                Shape = cellShape,
                Disabled = true,
            };

            AddChild(collisionShape2D);
            piston.CollisionShape2D = collisionShape2D;

            pointPistons.Add(gridPoint, piston);
            pistons.Add(piston);

            pistonShapeIdxs.Add(collisionShape2D.GetIndex());
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

        public void BreakPart(CollisionShape2D partCollider, bool checkFloating)
        {
            if (!colliderPoints.ContainsKey(partCollider))
                return;

            Point partPoint = colliderPoints[partCollider];
            Part part = Construction.GetGridPart(partPoint + Construction.RootPartPos);

            NewNutsNBolts(partCollider.GlobalPosition);
            GlobalNodes.VehiclePartBreak();

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

                        if (maxSpeedScale == 0f)
                        {
                            engineSoundPlayer.StopEngine();
                            EmitSignal(nameof(NoMoreEngines));
                            GlobalNodes.INSTANCE.MakeTntExplosion(Position, GetParent());
                        }
                    }

                    if (mainPart is PistonPart pistonPart)
                    {
                        var piston = pointPistons[partPoint];
                        piston.CollisionShape2D.Disabled = true;
                        pistons.Remove(piston);
                        pointPistons.Remove(partPoint);

                        piston.CollisionShape2D.QueueFree();
                        piston.QueueFree();
                    }

                    if (mainPart is WheelPart wheelPart)
                    {
                        var wheelNodes = pointWheels[partPoint];

                        wheelNodes.pin.QueueFree();

                        Wheels.Remove(wheelNodes.wheel);
                        PinJoints.Remove(wheelNodes.pin);
                        pointWheels.Remove(partPoint);

                        Construction.SetGridPart(partPoint + new Point(0, 1) + Construction.RootPartPos, null);

                        if (wheelPart is JumboWheelPart)
                        {
                            var extraJumboCollider = pointColliders[partPoint + new Point(1, 0)];
                            RemoveChild(extraJumboCollider);
                            brokenPart.AddChild(extraJumboCollider);
                            extraJumboCollider.Position = new Vector2(48, 16);

                            foreach (var extraOffset in wheelPart.ExtraParts)
                            {
                                Construction.SetGridPart(partPoint + extraOffset + Construction.RootPartPos, null);
                            }
                        }

                        if (Wheels.Count == 0)
                        {
                            SelfDestruct();
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

        private void NewNutsNBolts(Vector2 position)
        {
            var p = nutsNBoltsScene.Instance<Particles2D>();
            p.Emitting = true;
            GetParent().AddChild(p);
            p.Position = position;

            GetTree().CreateTimer(p.Lifetime).Connect("timeout", p, "queue_free");
        }

        private void ActivatePistons()
        {
            foreach (var piston in pistons)
            {
                piston.GetNode<AnimationPlayer>("AnimationPlayer").Play("Plunge");
            }
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

        public async void ActivateCollision()
        {
            var t = GetTree().CreateTimer(.25f);
            await ToSignal(t, "timeout");

            GravityScale = 1f;
            Layers = 1;
            ContactMonitor = true;
            ContactsReported = 1;

            LinearVelocity = Vector2.Zero;
            AngularVelocity = 0f;

            foreach (var wheel in Wheels)
            {
                wheel.Layers = 1;
                wheel.GravityScale = 1;
            }
        }
    }
}