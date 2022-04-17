using Godot;

namespace Fragile
{
    public class DriveWorld : Node2D
    {
        private static PackedScene[] levelChunks;

        static DriveWorld()
        {
            GlobalNodes.LoadFromDirectory<PackedScene>("res://scenes/drive_chunks/random_chunks/", out levelChunks, ".tscn");
        }

        private Vehicle vehicle;
        private Node2D levelChunkA;
        private Node2D levelChunkB;
        private StaticBody2D noGoinBack;
        private Label distLabel;
        private Label fps;
        private Tween tween;
        private Panel endOfRunPanel;
        private TransitionRect transitionRect;
        private float furthestDistance = 0;
        private float personalBest = SaveData.MaxDistance;
        private bool isPersonalBest = false;
        private float startX = 0;
        private int[] milestones = new int[5] { 50, 100, 200, 500, 1000 };
        private bool transitionQuit = false;

        public static Vector2 VehicleStartPos { get; } = new Vector2(280, 56);


        public override void _Ready()
        {
            base._Ready();

            noGoinBack = GetNode<StaticBody2D>("NoGoinBack");

            levelChunkA = GD.Load<PackedScene>("res://scenes/drive_chunks/StarterChunk.tscn").Instance<Node2D>();
            AddChild(levelChunkA);

            levelChunkB = GetRandomLevelChunk();
            AddChild(levelChunkB);
            levelChunkB.MoveLocalX(960);
            levelChunkB.GetNode<LevelPolygon>("LevelPolygon").UpdateTextureFromPos();

            transitionRect = GetNode<TransitionRect>("EndOfRunUI/TransitionRect");
            transitionRect.FadeIn(new Color("52333f"), 3f);


            distLabel = GetNode<Label>("UI/DistanceLabel");
            fps = GetNode<Label>("UI/fps");

            GetNode<Label>("UI/PersonalBestLabel").Text = $"PB: {FormatDistance(SaveData.MaxDistance)}";

            tween = GetNode<Tween>("Tween");
            endOfRunPanel = GetNode<Panel>("EndOfRunUI/Panel");

            var restartButton = GetNode("EndOfRunUI/Panel/MarginContainer/Panel/MarginContainer/VBoxContainer/MarginContainer/HBoxContainer/RestartButton");
            restartButton.Connect("pressed", transitionRect, nameof(TransitionRect.FadeOut), new Godot.Collections.Array() { 2f });
            transitionRect.Connect(nameof(TransitionRect.Finished), this, nameof(TransitionFinished));
            restartButton.Connect("mouse_entered", GlobalNodes.INSTANCE, nameof(GlobalNodes.UIClick));
            var quitButton = GetNode("EndOfRunUI/Panel/MarginContainer/Panel/MarginContainer/VBoxContainer/MarginContainer/HBoxContainer/QuitButton2");
            quitButton.Connect("pressed", this, nameof(QuitPressed));
            quitButton.Connect("mouse_entered", GlobalNodes.INSTANCE, nameof(GlobalNodes.UIClick));

            GlobalNodes.MainTheme();
        }

        public void SetVehicle(Vehicle vehicle)
        {
            this.vehicle = vehicle;

            var selfDestructBtn = GetNode("UI/SelfDestruct");
            selfDestructBtn.Connect("pressed", vehicle, nameof(vehicle.SelfDestruct));
            selfDestructBtn.Connect("mouse_entered", GlobalNodes.INSTANCE, nameof(GlobalNodes.UIClick));

            vehicle.CallDeferred(nameof(vehicle.ActivateCollision));
            startX = vehicle.Position.x;

            vehicle.Connect(nameof(Vehicle.NoMoreEngines), this, nameof(NoMoreEngines));
        }

        public override void _Process(float delta)
        {
            base._Process(delta);

            if (endOfRunPanel.Visible)
                return;

            float dist = (vehicle.Position.x - startX) / 32f;
            if (dist > furthestDistance)
                furthestDistance = dist;

            bool kilo = furthestDistance > 1000f;
            if (kilo)
            {
                distLabel.Text = $"{(isPersonalBest ? "(new record!) " : "")}{(furthestDistance * 0.001f):0.00}km";
            }
            else
            {
                distLabel.Text = $"{(isPersonalBest ? "(new record!) " : "")}{furthestDistance:0}m";
            }

            fps.Text = $"{Engine.GetFramesPerSecond()}";

            if (!isPersonalBest && furthestDistance > personalBest)
            {
                isPersonalBest = true;

                distLabel.Set("custom_colors/font_color", new Color("ffee83"));
                distLabel.Set("custom_colors/font_outline_modulate", new Color("e64539"));
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);

            if (vehicle != null && !endOfRunPanel.Visible)
            {
                if (vehicle.Position.x >= levelChunkB.Position.x + 240)
                {
                    levelChunkA.QueueFree();
                    levelChunkA = levelChunkB;

                    levelChunkB = GetRandomLevelChunk();
                    AddChild(levelChunkB);
                    levelChunkB.Position = levelChunkA.Position + new Vector2(960, 0);

                    noGoinBack.Position = levelChunkA.Position;

                    levelChunkB.GetNode<LevelPolygon>("LevelPolygon").UpdateTextureFromPos();
                }
            }
        }

        private Node2D GetRandomLevelChunk()
        {
            return levelChunks[GlobalNodes.RNG.RandiRange(0, levelChunks.Length - 1)].Instance<Node2D>();
        }

        private void NoMoreEngines()
        {
            endOfRunPanel.Modulate = Colors.Transparent;
            endOfRunPanel.Visible = true;

            tween.InterpolateProperty(endOfRunPanel, "modulate", Colors.Transparent, Colors.White, .5f, Tween.TransitionType.Cubic, Tween.EaseType.In);
            tween.Start();

            string dist = FormatDistance(furthestDistance);

            //New milestone?
            int prevMilestone = 0;
            for (int i = 0; i < milestones.Length; i++)
            {
                if (personalBest >= milestones[i])
                {
                    prevMilestone = milestones[i];
                }
            }
            int newMilestone = 0;
            for (int i = 0; i < milestones.Length; i++)
            {
                if (furthestDistance >= milestones[i])
                {
                    newMilestone = milestones[i];
                }
            }

            //Unlocked new parts?
            if (newMilestone <= prevMilestone)
            {
                // Not unlocked any new parts
                GetNode<RichTextLabel>("EndOfRunUI/Panel/MarginContainer/Panel/MarginContainer/VBoxContainer/RichTextLabel2").VisibleCharacters = 0;
            }

            GetNode<Label>("EndOfRunUI/Panel/MarginContainer/Panel/MarginContainer/VBoxContainer/Title2").Text = $"You travelled {dist} before your fragile vehicle collapsed.";

            if (!isPersonalBest)
                GetNode<RichTextLabel>("EndOfRunUI/Panel/MarginContainer/Panel/MarginContainer/VBoxContainer/RichTextLabel").VisibleCharacters = 0;
            else
                SaveData.SaveMaxDistance((int)furthestDistance);

            Construction.ClearGrid();
        }

        private void QuitPressed()
        {
            transitionQuit = true;

            transitionRect.FadeOut(new Color("1b1f21"), 1f);
        }

        private void Restart()
        {
            GetTree().ChangeScene("res://scenes/Construction.tscn");

            QueueFree();
        }

        private void Quit()
        {
            GetTree().ChangeScene("res://scenes/menus/MainMenu.tscn");

            QueueFree();
        }

        private void TransitionFinished(bool fadeIn)
        {
            if (!fadeIn)
            {
                if (transitionQuit)
                    Quit();
                else
                    Restart();
            }
        }

        public static string FormatDistance(float distance)
        {
            string dist = "";
            if (distance > 1000f)
                dist = $"{(distance * 0.001f):0.00}km";
            else
                dist = $"{distance:0}m";

            return dist;
        }
    }
}