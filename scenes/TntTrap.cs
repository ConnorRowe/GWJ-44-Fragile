using Godot;

namespace Fragile
{
    public class TntTrap : RigidBody2D
    {
        private static CircleShape2D explodeShape = new CircleShape2D() { Radius = 72 };
        private Physics2DShapeQueryParameters explodeQueryParams;
        private AudioStreamPlayer2D beepPlayer2D;

        private int count = 0;
        private Timer timer;
        private AnimatedSprite animSprite;
        private bool activated = false;
        private bool explodeTrace = false;

        public override void _Ready()
        {
            animSprite = GetNode<AnimatedSprite>("AnimatedSprite");
            beepPlayer2D = GetNode<AudioStreamPlayer2D>("BeepPlayer2D");
            timer = GetNode<Timer>("Timer");
            timer.Connect("timeout", this, nameof(Timeout));
            Connect("body_entered", this, nameof(BodyEntered));

            explodeQueryParams = new Physics2DShapeQueryParameters()
            {
                CollideWithAreas = false,
                CollideWithBodies = true,
                Exclude = new Godot.Collections.Array() { this },
                Transform = new Transform2D(0f, Position)
            };

            explodeQueryParams.SetShape(explodeShape);
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            if (explodeTrace)
            {
                explodeTrace = false;

                var spaceState = GetWorld2d().DirectSpaceState;

                // {
                // 	position: Vector2 # point in world space for collision
                // 	normal: Vector2 # normal in world space for collision
                // 	collider: Object # Object collided or null (if unassociated)
                // 	collider_id: ObjectID # Object it collided against
                // 	rid: RID # RID it collided against
                // 	shape: int # shape index of collider
                // 	metadata: Variant() # metadata of collider
                // }

                explodeQueryParams.Transform = new Transform2D(0f, GlobalPosition);

                var results = spaceState.IntersectShape(explodeQueryParams);

                System.Collections.Generic.List<CollisionShape2D> partColliders = new System.Collections.Generic.List<CollisionShape2D>();

                Vehicle vehicle = null;

                foreach (Godot.Collections.Dictionary hit in results)
                {
                    if (hit["collider"] is RigidBody2D rigidBody2D)
                    {
                        if (rigidBody2D is TntTrap tntTrap)
                        {
                            tntTrap.Explode();
                        }

                        if (rigidBody2D is Vehicle v)
                        {
                            if (vehicle == null)
                                vehicle = v;

                            var hitShape = vehicle.ShapeOwnerGetOwner(vehicle.ShapeFindOwner((int)hit["shape"]));

                            if (hitShape != null && hitShape is CollisionShape2D hitCollisionShape)
                                partColliders.Add(hitCollisionShape);

                        }
                        else
                        {
                            rigidBody2D.ApplyImpulse(rigidBody2D.Position - GlobalPosition, GlobalPosition.DirectionTo(rigidBody2D.Position) * 250f);
                        }
                    }
                }

                if (vehicle != null)
                {
                    for (int i = 0; i < partColliders.Count; i++)
                    {
                        vehicle.BreakPart(partColliders[i], i == (partColliders.Count - 1));
                    }
                }

                QueueFree();
            }
        }

        private void Timeout()
        {
            count++;

            if (count < 4)
            {
                animSprite.Frame = count;
                beepPlayer2D.Play();
            }
            else
            {
                Explode();
            }
        }

        private void BodyEntered(Node node)
        {
            if (node is RigidBody2D)
                Activate();
        }

        private void Activate()
        {
            if (!activated)
            {
                activated = true;

                Timeout();
                timer.Start();
            }
        }

        private void Explode()
        {
            // BOOM
            timer.Stop();
            explodeTrace = true;

            GlobalNodes.INSTANCE.MakeTntExplosion(Position, GetParent());
            GetNode<CollisionShape2D>("CollisionShape2D").Disabled = true;
            animSprite.Visible = false;

            count = 5;
        }
    }
}