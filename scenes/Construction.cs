using Godot;
using Fragile.Parts;
using System.Collections.Generic;

namespace Fragile
{
    public class Construction : Node2D
    {
        public struct Point
        {
            public int x;
            public int y;

            public Point(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public Point(Vector2 v)
            {
                this.x = (int)v.x;
                this.y = (int)v.y;
            }

            public static Point operator +(Point a, Point b) => new Point(a.x + b.x, a.y + b.y);
            public static Point operator -(Point a, Point b) => new Point(a.x - b.x, a.y - b.y);
            public static Point operator *(Point a, int b) => new Point(a.x * b, a.y * b);
            public static bool operator ==(Point a, Point b) => a.x == b.x && a.y == b.y;
            public static bool operator !=(Point a, Point b) => a.x != b.x || a.y != b.y;

            public override string ToString() => $"({x}, {y})";

            public override bool Equals(object obj)
            {
                if (obj is Point p)
                {
                    return this == p;
                }
                else
                    return false;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashcode = 1430287;
                    hashcode = hashcode * 7302013 ^ x.GetHashCode();
                    hashcode = hashcode * 7302013 ^ y.GetHashCode();
                    return hashcode;
                }
            }

            public Vector2 ToVector2() => new Vector2(x, y);
        }

        private const int GRID_HEIGHT = 8;
        private const int GRID_WIDTH = 12;
        private static Texture rootTex = GD.Load<Texture>("res://textures/root.png");
        private static Texture gridCursorGreenTex = GD.Load<Texture>("res://textures/grid_cursor_green.png");
        private static Texture gridCursorRedTex = GD.Load<Texture>("res://textures/grid_cursor_red.png");
        private static Texture blockedTex = GD.Load<Texture>("res://textures/blocked.png");
        private static Texture disconnectedTex = GD.Load<Texture>("res://textures/disconnected.png");
        public static Point[] FourDirs { get; } = new Point[4] { new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0) };

        private static Part[,] partsGrid = new Part[GRID_WIDTH, GRID_HEIGHT];
        public static Point RootPartPos { get; } = new Point(6, 4);
        public static bool DrawGrid { get; set; } = true;
        public static Label Debug;

        private Node2D grid;
        private Point cursorGridPos = new Point(0, 0);
        private MainPart cursorPart = Parts.Parts.Body;
        private Sprite gridCursor;
        private Sprite[] extraGridCursors;
        private Sprite cursorWarning;
        private Tween tween;
        private Label selectedPartLabel;
        private Vehicle vehicle;
        private AudioStreamPlayer buildPlayer;
        private AudioStreamPlayer dismantlePlayer;
        private PartTooltip partTooltip;
        private bool cursorClear;
        private bool cursorConnected;
        private TextureProgress powerProgress;
        private TextureProgress weightProgress;
        private float power = 0;
        private float weight = 0;
        private float time;
        private Label powerWarning;


        private Dictionary<MainPart, int> inventory = new Dictionary<MainPart, int>() { { Parts.Parts.Body, 99 }, { Parts.Parts.WheelStandard, 99 }, { Parts.Parts.EngineSmall, 99 }, { Parts.Parts.EngineLarge, 99 }, { Parts.Parts.WheelSpring, 99 } };
        private Dictionary<MainPart, PartButton> partButtons = new Dictionary<MainPart, PartButton>();

        public override void _Ready()
        {
            base._Ready();

            grid = GetNode<Node2D>("Grid");
            gridCursor = GetNode<Sprite>("Grid/GridCursor");
            tween = GetNode<Tween>("Tween");
            extraGridCursors = new Sprite[3] { gridCursor.GetChild<Sprite>(0), gridCursor.GetChild<Sprite>(1), gridCursor.GetChild<Sprite>(2) };
            selectedPartLabel = GetNode<Label>("SelectedPartLabel");
            cursorWarning = GetNode<Sprite>("Grid/GridCursor/Warning");
            buildPlayer = GetNode<AudioStreamPlayer>("BuildPlayer");
            dismantlePlayer = GetNode<AudioStreamPlayer>("DismantlePlayer");
            partTooltip = GetNode<PartTooltip>("PartTooltip");
            powerProgress = GetNode<TextureProgress>("PowerProgress");
            weightProgress = GetNode<TextureProgress>("WeightProgress");
            powerWarning = GetNode<Label>("PowerWarning");

            Debug = GetNode<Label>("debug");

            // Place root part
            partsGrid[RootPartPos.x, RootPartPos.y] = Parts.Parts.RootPart;
            Update();

            partButtons.Add(Parts.Parts.Body, GetNode<PartButton>("BodyButton"));
            partButtons.Add(Parts.Parts.WheelStandard, GetNode<PartButton>("Wheel"));
            partButtons.Add(Parts.Parts.EngineSmall, GetNode<PartButton>("SmallEngine"));
            partButtons.Add(Parts.Parts.EngineLarge, GetNode<PartButton>("LargeEngine"));
            partButtons.Add(Parts.Parts.WheelSpring, GetNode<PartButton>("WheelSpring"));

            partButtons[Parts.Parts.Body].SetPart(Parts.Parts.Body);
            partButtons[Parts.Parts.WheelStandard].SetPart(Parts.Parts.WheelStandard);
            partButtons[Parts.Parts.EngineSmall].SetPart(Parts.Parts.EngineSmall);
            partButtons[Parts.Parts.EngineLarge].SetPart(Parts.Parts.EngineLarge);
            partButtons[Parts.Parts.WheelSpring].SetPart(Parts.Parts.WheelSpring);

            foreach (var part in partButtons.Keys)
            {
                partButtons[part].Connect("pressed", this, nameof(PartButtonPressed), new Godot.Collections.Array() { part });
                partButtons[part].Connect("mouse_entered", GlobalNodes.INSTANCE, nameof(GlobalNodes.UIClick));
                partButtons[part].Connect("mouse_entered", this, nameof(ShowTooltip), new Godot.Collections.Array() { part });
                partButtons[part].Connect("mouse_exited", this, nameof(HideTooltip));
            }
        }

        public override void _Input(InputEvent evt)
        {
            base._Input(evt);

            if (evt is InputEventMouseMotion emm)
            {
                var mousePos = (emm.Position - grid.Position);
                if (mousePos.x < 0)
                    mousePos.x -= 64f;
                UpdateGridCursor(new Point(mousePos / 32), false);

                partTooltip.RectPosition = emm.Position;
            }
            else if (evt is InputEventMouseButton emb && emb.Pressed)
            {
                if (emb.ButtonIndex == (int)ButtonList.Left)
                {
                    if (gridCursor.Visible && cursorClear && cursorConnected)
                    {
                        TryPlacePart(cursorGridPos, cursorPart);
                        UpdateGridCursor(cursorGridPos, true);
                    }
                }
                else if (emb.ButtonIndex == (int)ButtonList.Right)
                {
                    RemovePart(cursorGridPos);
                    UpdateGridCursor(cursorGridPos, true);
                }
            }

            if (evt is InputEventKey ek && ek.Pressed)
            {
                switch (ek.Scancode)
                {
                    case (int)KeyList.Enter:
                        // Construct vehicle and go to DriveWorld
                        SwitchToDriveWorld();

                        break;
                }
            }
        }

        public override void _Process(float delta)
        {
            base._Process(delta);

            powerProgress.Value = Mathf.Lerp((float)powerProgress.Value, power * 0.2f, delta * 2f);
            weightProgress.Value = Mathf.Lerp((float)weightProgress.Value, weight * 0.01f, delta * 2f);

            if (Mathf.IsEqualApprox((float)powerProgress.Value, 0f))
            {
                time += delta * 2f;
                powerWarning.Visible = true;
                float s = .8f + (Mathf.Cos(time) * .4f);
                powerWarning.RectScale = new Vector2(s, s);
            }
            else
            {
                powerWarning.Visible = false;
            }
        }

        public override void _Draw()
        {
            if (!DrawGrid)
                return;

            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    if (!IsGridPointEmpty(x, y))
                    {
                        Part p = partsGrid[x, y];

                        if (p is WheelPart wheelPart)
                        {
                            DrawTexture(wheelPart.WheelTex, grid.Position + new Vector2(x * 32, y * 32));
                            DrawTexture(wheelPart.Texture, grid.Position + new Vector2(x * 32, y * 32));
                        }
                        else if (p is MainPart mainPart)
                        {
                            DrawTexture(mainPart.Texture, grid.Position + mainPart.TexOffset + new Vector2(x * 32, y * 32));
                        }
                        else if (p is RootPart)
                        {
                            DrawTexture(rootTex, grid.Position + new Vector2(x * 32, y * 32));
                        }
                    }
                }
            }
        }

        private bool TryPlacePart(Point point, MainPart part)
        {
            if (!IsGridPointEmpty(point))
            {
                return false;
            }
            else
            {
                if (inventory[part] <= 0)
                    return false;

                inventory[part]--;
                partButtons[part].SetCount(inventory[part]);

                partsGrid[point.x, point.y] = part;

                if (part is MainPart mainPart)
                {
                    bool allClear = true;

                    weight += mainPart.Mass;
                    if (mainPart is EnginePart enginePart)
                        power += enginePart.MaxSpeed;

                    foreach (Point extraPos in mainPart.ExtraParts)
                    {
                        if (!IsGridPointEmpty(point + extraPos))
                        {
                            allClear = false;
                            break;
                        }
                    }

                    if (!allClear)
                    {
                        partsGrid[point.x, point.y] = null;

                        return false;
                    }

                    foreach (Point extraPos in mainPart.ExtraParts)
                    {
                        Point p = point + extraPos;
                        partsGrid[p.x, p.y] = new ExtraPart(point);
                    }
                }

                Update();

                buildPlayer.Play();

                return true;
            }
        }

        private void RemovePart(Point point)
        {
            if (IsGridPointEmpty(point))
            {
                return;
            }

            Part part = partsGrid[point.x, point.y];

            if (part is RootPart)
            {
                return;
            }
            else if (part is ExtraPart extraPart)
            {
                RemovePart(extraPart.OwnerPart);
            }
            else
            {
                if (part is MainPart mainPart)
                {
                    foreach (Point extraPos in mainPart.ExtraParts)
                    {
                        Point p = point + extraPos;
                        partsGrid[p.x, p.y] = null;
                    }

                    weight -= mainPart.Mass;
                    if (mainPart is EnginePart enginePart)
                        power -= enginePart.MaxSpeed;

                    // Refund inventory
                    partButtons[mainPart].SetCount(++inventory[mainPart]);
                }

                partsGrid[point.x, point.y] = null;

                Update();

                dismantlePlayer.Play();

                //TODO: check if connected to main vehicle part
                RemoveDisconnectedParts();
            }
        }

        private void RemoveDisconnectedParts()
        {
            foreach (Point p in GetDisconnectedParts())
            {
                RemovePart(p);
            }
        }

        public static List<Point> GetDisconnectedParts()
        {
            List<Point> connectedParts = new List<Point>();
            List<Point> workingParts = new List<Point>();
            workingParts.Add(RootPartPos);
            connectedParts.Add(RootPartPos);
            Point[] directions = new Point[4] { new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0) };

            while (workingParts.Count > 0)
            {
                Point partPos = workingParts[0];

                foreach (var dir in directions)
                {
                    Point offset = partPos + dir;

                    if (IsPointInGrid(offset) && !IsGridPointEmpty(offset))
                    {
                        if (!connectedParts.Contains(offset))
                        {
                            workingParts.Add(offset);
                            connectedParts.Add(offset);
                        }
                    }
                }

                workingParts.RemoveAt(0);
            }

            List<Point> disconnectedParts = new List<Point>();

            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    Point p = new Point(x, y);

                    if (!IsGridPointEmpty(p))
                    {
                        if (!connectedParts.Contains(p))
                        {
                            disconnectedParts.Add(p);
                        }
                    }
                }
            }

            return disconnectedParts;
        }

        private static bool IsPointInGrid(Point point)
        {
            return IsPointInGrid(point.x, point.y);
        }

        private static bool IsPointInGrid(int x, int y)
        {
            return x >= 0 && x < GRID_WIDTH && y >= 0 && y < GRID_HEIGHT;
        }

        private static bool IsGridPointEmpty(Point point)
        {
            return IsGridPointEmpty(point.x, point.y);
        }

        private static bool IsGridPointEmpty(int x, int y)
        {
            if (IsPointInGrid(x, y))
            {
                return partsGrid[x, y] == null;
            }

            // Out of bounds
            return false;
        }

        private static bool IsGridPointConnected(Point point)
        {
            bool connected = false;
            foreach (var dir in FourDirs)
            {
                if (IsPointInGrid(point + dir) && !IsGridPointEmpty(point + dir))
                {
                    connected = true;
                    break;
                }
            }

            return connected;
        }

        private Vehicle ConstructVehicle(Node parent)
        {
            Vehicle vehicle = new Vehicle()
            {
                GravityScale = 0,
                Layers = 0,
                Position = DriveWorld.VehicleStartPos
            };

            parent.AddChild(vehicle);
            vehicle.ZIndex = 1;

            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    Part p = partsGrid[x, y];

                    if (p != null)
                    {
                        Point pos = new Point(x, y) - RootPartPos;

                        if (p is MainPart m)
                            vehicle.Weight += m.Mass;

                        if (p is RootPart)
                        {
                            vehicle.AddSprite(rootTex, pos);

                            Camera2D camera2D = new Camera2D()
                            {
                                SmoothingEnabled = true,
                                Current = true,
                                LimitLeft = 0,
                                LimitBottom = 270,
                                Position = (pos * 32).ToVector2() + new Vector2(128, 64),
                                ProcessMode = Camera2D.Camera2DProcessMode.Physics
                            };

                            vehicle.AddChild(camera2D);
                        }
                        else if (p is WheelPart wheelPart)
                        {
                            vehicle.AddWheel(pos, wheelPart);
                        }
                        else if (p is MainPart mainPart)
                        {
                            vehicle.AddSprite(mainPart.Texture, pos, mainPart.TexOffset);
                        }

                        if (p is EnginePart enginePart)
                        {
                            vehicle.AddEngineStats(enginePart);
                            vehicle.AddEngineSmoke(pos, enginePart.SmokeOffset);
                        }

                        if (!(p is ExtraPart extraPart && partsGrid[extraPart.OwnerPart.x, extraPart.OwnerPart.y] is WheelPart))
                            vehicle.AddSquareCollider(pos);
                    }
                }
            }

            return vehicle;
        }

        private void ClearGrid()
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    if (!(partsGrid[x, y] is RootPart))
                        partsGrid[x, y] = null;
                }
            }

            Update();
        }

        public static Part GetGridPart(Point point)
        {
            if (IsPointInGrid(point))
                return partsGrid[point.x, point.y];
            else
                return null;
        }

        public static void SetGridPart(Point point, Part value)
        {
            if (IsPointInGrid(point))
                partsGrid[point.x, point.y] = value;
        }

        private void UpdateGridCursor(Point cursorPoint, bool forceFullUpdate)
        {
            if (IsPointInGrid(cursorPoint))
            {
                if (!gridCursor.Visible)
                {
                    // Show

                    gridCursor.Visible = true;
                }

                if (cursorGridPos != cursorPoint || forceFullUpdate)
                {
                    cursorGridPos = cursorPoint;

                    GlobalNodes.UIClick();

                    gridCursor.Position = (cursorGridPos * 32).ToVector2() + new Vector2(17, 17);

                    tween.Stop(gridCursor);

                    // tween.InterpolateProperty(gridCursor, "scale", Vector2.One, new Vector2(.8f, .8f), .4f, Tween.TransitionType.Bounce);
                    tween.InterpolateProperty(gridCursor, "scale", new Vector2(.8f, .8f), Vector2.One, .4f, Tween.TransitionType.Elastic, Tween.EaseType.Out);
                    tween.Start();

                    // Extra parts
                    cursorClear = true;
                    cursorConnected = false;

                    cursorClear = IsGridPointEmpty(cursorGridPos);
                    cursorConnected = IsGridPointConnected(cursorGridPos);

                    if (cursorClear)
                    {
                        foreach (var extraPos in cursorPart.ExtraParts)
                        {
                            if (!IsGridPointEmpty(cursorGridPos + extraPos))
                            {
                                cursorClear = false;
                                break;
                            }
                            else if (IsGridPointConnected(cursorGridPos + extraPos))
                            {
                                cursorConnected = true;
                            }
                        }
                    }

                    gridCursor.Texture = (cursorClear && cursorConnected) ? gridCursorGreenTex : gridCursorRedTex;

                    if (!cursorClear)
                    {
                        cursorWarning.Visible = true;
                        cursorWarning.Texture = blockedTex;
                    }
                    else if (!cursorConnected)
                    {
                        cursorWarning.Visible = true;
                        cursorWarning.Texture = disconnectedTex;
                    }
                    else
                    {
                        cursorWarning.Visible = false;
                    }

                    for (int i = 0; i < extraGridCursors.Length; i++)
                    {
                        if (i < cursorPart.ExtraParts.Length)
                        {
                            extraGridCursors[i].Position = (cursorPart.ExtraParts[i] * 32).ToVector2();
                            extraGridCursors[i].Texture = gridCursor.Texture;
                            extraGridCursors[i].Visible = true;
                        }
                        else
                        {
                            extraGridCursors[i].Visible = false;
                        }
                    }
                }
            }
            else
            {
                if (gridCursor.Visible)
                {
                    // Hide
                    gridCursor.Visible = false;
                }
            }
        }

        private void SetCursorPart(MainPart part)
        {
            cursorPart = part;
            UpdateGridCursor(cursorGridPos, true);
        }

        private void PartButtonPressed(MainPart part)
        {
            SetCursorPart(part);
            selectedPartLabel.Text = $"Selected part:\n{part.PartName}";
        }

        private void SwitchToDriveWorld()
        {
            DriveWorld driveWorld = GD.Load<PackedScene>("res://scenes/DriveWorld.tscn").Instance<DriveWorld>();

            RemoveDisconnectedParts();
            vehicle = ConstructVehicle(driveWorld);
            driveWorld.SetVehicle(vehicle);

            GetTree().Root.AddChild(driveWorld);
            QueueFree();
        }

        private void ShowTooltip(MainPart part)
        {
            partTooltip.UpdateFromPart(part);

            tween.Stop(partTooltip);
            tween.InterpolateProperty(partTooltip, "rect_scale", partTooltip.RectScale, Vector2.One, .5f, Tween.TransitionType.Bounce, Tween.EaseType.Out);
            tween.Start();
        }

        private void HideTooltip()
        {
            tween.Stop(partTooltip);
            tween.InterpolateProperty(partTooltip, "rect_scale", partTooltip.RectScale, Vector2.Down, .25f, Tween.TransitionType.Bounce, Tween.EaseType.Out);
            tween.Start();
        }
    }
}

