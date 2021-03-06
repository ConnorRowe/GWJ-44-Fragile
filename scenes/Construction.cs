using Godot;
using Fragile.Parts;
using System.Collections.Generic;

namespace Fragile
{
    public class Construction : Node2D
    {
        public struct Point
        {
            public static Point Up { get; } = new Point(0, -1);
            public static Point Right { get; } = new Point(1, 0);
            public static Point Down { get; } = new Point(0, 1);
            public static Point Left { get; } = new Point(-1, 0);

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

            public Point Rotated(float r)
            {
                return new Point(Mathf.RoundToInt((x * Mathf.Cos(r)) - (y * Mathf.Sin(r))), Mathf.RoundToInt((x * Mathf.Sin(r)) + (y * Mathf.Cos(r))));
            }
        }

        private const int GRID_HEIGHT = 8;
        private const int GRID_WIDTH = 12;
        private static Texture rootTex = GD.Load<Texture>("res://textures/root.png");
        private static Texture gridCursorGreenTex = GD.Load<Texture>("res://textures/grid_cursor_green.png");
        private static Texture gridCursorRedTex = GD.Load<Texture>("res://textures/grid_cursor_red.png");
        private static Texture gridCursorDarkGreenTex = GD.Load<Texture>("res://textures/grid_cursor_darkgreen.png");
        private static Texture gridCursorDarkRedTex = GD.Load<Texture>("res://textures/grid_cursor_darkred.png");
        private static Texture gridCursorArrowGreenTex = GD.Load<Texture>("res://textures/grid_cursor_arrow_green.png");
        private static Texture gridCursorArrowRedTex = GD.Load<Texture>("res://textures/grid_cursor_arrow_red.png");
        private static Texture blockedTex = GD.Load<Texture>("res://textures/blocked.png");
        private static Texture disconnectedTex = GD.Load<Texture>("res://textures/disconnected.png");
        private static Texture longPlungerTex = GD.Load<Texture>("res://textures/long_plunger.png");
        private static PhysicsMaterial vehiclePhysMat = new PhysicsMaterial()
        {
            Friction = 0,
            Rough = false,
            Bounce = .7f
        };
        public static Point[] FourDirs { get; } = new Point[4] { new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0) };

        private static Part[,] partsGrid = new Part[GRID_WIDTH, GRID_HEIGHT];
        private static float[,] rotationGrid = new float[GRID_WIDTH, GRID_HEIGHT];
        public static Point RootPartPos { get; } = new Point(6, 4);
        public static bool DrawGrid { get; set; } = true;
        public static Label Debug;
        public static bool QuickRestart { get; set; } = false;

        private Node2D grid;
        private Point cursorGridPos = new Point(0, 0);
        private MainPart cursorPart = Parts.Parts.Body;
        private Sprite gridCursor;
        private Sprite[] extraGridCursors;
        private Sprite cursorWarning;
        private Sprite cursorArrow;
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
        private int wheelCount = 0;
        private TransitionRect transitionRect;
        private float cursorRotation = 0f;


        private Dictionary<MainPart, int> inventory = new Dictionary<MainPart, int>()
        {
            { Parts.Parts.Body, 10 },
            { Parts.Parts.WheelStandard, 3 },
            { Parts.Parts.EngineSmall, 3 },
            { Parts.Parts.EngineLarge, 0 },
            { Parts.Parts.WheelJumbo, 0},
            { Parts.Parts.Piston, 0 },
        };
        private Dictionary<MainPart, PartButton> partButtons = new Dictionary<MainPart, PartButton>();
        public static Dictionary<int, (MainPart part, int count)[]> MileStoneParts { get; } = new Dictionary<int, (MainPart part, int count)[]>()
        {
            { 50, new (MainPart part, int count)[] {(Parts.Parts.Body, 8), (Parts.Parts.EngineSmall, 2)} },
            { 100, new (MainPart part, int count)[] {(Parts.Parts.EngineLarge, 1), (Parts.Parts.WheelStandard, 2)} },
            { 200, new (MainPart part, int count)[] {(Parts.Parts.EngineLarge, 2), (Parts.Parts.Body, 16), (Parts.Parts.WheelJumbo, 2)} },
            { 500, new (MainPart part, int count)[] {(Parts.Parts.Piston, 2), (Parts.Parts.WheelJumbo, 2)} },
            { 1000, new (MainPart part, int count)[] {(Parts.Parts.Body, 65), (Parts.Parts.EngineLarge, 96), (Parts.Parts.EngineSmall, 94), (Parts.Parts.Piston, 97), (Parts.Parts.WheelStandard, 94), (Parts.Parts.WheelJumbo, 95)} },
        };
        private static Dictionary<MainPart, int> partButtonShortcuts = new Dictionary<MainPart, int>()
        {
            { Parts.Parts.Body, 1 },
            { Parts.Parts.WheelStandard, 2 },
            { Parts.Parts.EngineSmall, 3 },
            { Parts.Parts.EngineLarge, 4 },
            { Parts.Parts.WheelJumbo, 5},
            { Parts.Parts.Piston, 6 }
        };

        public override void _Ready()
        {
            base._Ready();

            grid = GetNode<Node2D>("Grid");
            gridCursor = GetNode<Sprite>("Grid/GridCursor");
            tween = GetNode<Tween>("Tween");
            extraGridCursors = new Sprite[5] { gridCursor.GetChild<Sprite>(0), gridCursor.GetChild<Sprite>(1), gridCursor.GetChild<Sprite>(2), gridCursor.GetChild<Sprite>(3), gridCursor.GetChild<Sprite>(4) };
            selectedPartLabel = GetNode<Label>("SelectedPartLabel");
            cursorWarning = GetNode<Sprite>("Grid/GridCursor/Warning");
            cursorArrow = GetNode<Sprite>("Grid/GridCursor/Arrow");
            buildPlayer = GetNode<AudioStreamPlayer>("BuildPlayer");
            dismantlePlayer = GetNode<AudioStreamPlayer>("DismantlePlayer");
            partTooltip = GetNode<PartTooltip>("PartTooltip");
            powerProgress = GetNode<TextureProgress>("PowerProgress");
            weightProgress = GetNode<TextureProgress>("WeightProgress");
            powerWarning = GetNode<Label>("PowerWarning");
            transitionRect = GetNode<TransitionRect>("CanvasLayer/TransitionRect");

            Debug = GetNode<Label>("debug");

            // Place root part
            partsGrid[RootPartPos.x, RootPartPos.y] = Parts.Parts.RootPart;

            partButtons.Add(Parts.Parts.Body, GetNode<PartButton>("BodyButton"));
            partButtons.Add(Parts.Parts.WheelStandard, GetNode<PartButton>("Wheel"));
            partButtons.Add(Parts.Parts.EngineSmall, GetNode<PartButton>("SmallEngine"));
            partButtons.Add(Parts.Parts.EngineLarge, GetNode<PartButton>("LargeEngine"));
            partButtons.Add(Parts.Parts.Piston, GetNode<PartButton>("Piston"));
            partButtons.Add(Parts.Parts.WheelJumbo, GetNode<PartButton>("JumboWheel"));

            partButtons[Parts.Parts.Body].SetPart(Parts.Parts.Body);
            partButtons[Parts.Parts.WheelStandard].SetPart(Parts.Parts.WheelStandard);
            partButtons[Parts.Parts.EngineSmall].SetPart(Parts.Parts.EngineSmall);
            partButtons[Parts.Parts.EngineLarge].SetPart(Parts.Parts.EngineLarge);
            partButtons[Parts.Parts.Piston].SetPart(Parts.Parts.Piston);
            partButtons[Parts.Parts.WheelJumbo].SetPart(Parts.Parts.WheelJumbo);

            foreach (var part in partButtons.Keys)
            {
                partButtons[part].Connect("pressed", this, nameof(PartButtonPressed), new Godot.Collections.Array() { part });
                partButtons[part].Connect("mouse_entered", GlobalNodes.INSTANCE, nameof(GlobalNodes.UIClick));
                partButtons[part].Connect("mouse_entered", this, nameof(ShowTooltip), new Godot.Collections.Array() { part });
                partButtons[part].Connect("mouse_exited", this, nameof(HideTooltip));
            }

            GetNode("DriveButton").Connect("pressed", this, nameof(SwitchToDriveWorld));

            UnlockMilestoneParts();

            Update();

            transitionRect.FadeIn(1.5f);
            transitionRect.Connect(nameof(TransitionRect.Finished), this, nameof(TransitionFinished));

            //Tutorial
            if (SaveData.MaxDistance < 25)
            {
                GetNode<AcceptDialog>("TutorialIntro").Popup_(new Rect2(117, 94, 330, 82));

                GetNode("TutorialIntro").Connect("confirmed", GetNode("TutorialGrid"), "popup", new Godot.Collections.Array() { new Rect2(172, 12, 255, 88) });
                GetNode("TutorialGrid").Connect("confirmed", GetNode("TutorialParts"), "popup", new Godot.Collections.Array() { new Rect2(103, 40, 247, 112) });
                GetNode("TutorialParts").Connect("confirmed", GetNode("TutorialRotation"), "popup", new Godot.Collections.Array() { new Rect2(129, 62, 225, 103) });
                GetNode("TutorialRotation").Connect("confirmed", GetNode("TutorialDials"), "popup", new Godot.Collections.Array() { new Rect2(96, 169, 190, 67) });
                GetNode("TutorialDials").Connect("confirmed", GetNode("TutorialDrive"), "popup", new Godot.Collections.Array() { new Rect2(96, 133, 190, 97) });
                GetNode("TutorialDrive").Connect("confirmed", GetNode("TutorialFinal"), "popup", new Godot.Collections.Array() { new Rect2(173, 52, 207, 133) });
            }

            if (QuickRestart)
            {
                QuickRestart = false;

                TransitionFinished(false);
            }
            else
                GlobalNodes.ConstructionTheme();
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

            if (evt.IsActionPressed("rotate_cw"))
            {
                cursorRotation = Mathf.Wrap(cursorRotation + (Mathf.Pi * .5f), 0f, Mathf.Tau);
                UpdateGridCursor(cursorGridPos, true);
            }
            if (evt.IsActionPressed("rotate_ccw"))
            {
                cursorRotation = Mathf.Wrap(cursorRotation - (Mathf.Pi * .5f), 0f, Mathf.Tau);
                UpdateGridCursor(cursorGridPos, true);
            }
        }

        public override void _Process(float delta)
        {
            base._Process(delta);

            powerProgress.Value = DLerp(powerProgress.Value, power * 0.2f, delta * 3f);
            weightProgress.Value = DLerp(weightProgress.Value, weight * 0.01f, delta * 3f);

            if (Mathf.IsEqualApprox((float)powerProgress.Value, power, .03f))
                powerProgress.Value = power;

            if (Mathf.IsEqualApprox((float)weightProgress.Value, weight, .03f))
                weightProgress.Value = weight;

            if (power == 0f)
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

                        var offset = p is MainPart mp ? mp.TexOffset : new Vector2(-16, -16);

                        if (p is PistonPart pistonPart)
                        {
                            DrawSetTransformMatrix(new Transform2D(rotationGrid[x, y], grid.Position + new Vector2(x * 32, y * 32) + new Vector2(16, 16) + new Vector2(0, 16).Rotated(rotationGrid[x, y])));
                            DrawTexture(longPlungerTex, offset);
                        }

                        DrawSetTransformMatrix(new Transform2D(rotationGrid[x, y], grid.Position + new Vector2(x * 32, y * 32) + new Vector2(16, 16)));

                        if (p is WheelPart wheelPart)
                        {
                            DrawTexture(wheelPart.WheelTex, offset);
                            DrawTexture(wheelPart.Texture, offset);
                        }
                        else if (p is MainPart mainPart)
                        {
                            DrawTexture(mainPart.Texture, offset);
                        }
                        else if (p is RootPart)
                        {
                            DrawTexture(rootTex, offset);
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
                rotationGrid[point.x, point.y] = cursorRotation;

                if (part is MainPart mainPart)
                {

                    weight += mainPart.Mass;
                    if (mainPart is EnginePart enginePart)
                        power += enginePart.MaxSpeed;
                    if (mainPart is WheelPart wheelPart)
                        wheelCount++;

                    // bool allClear = true;

                    // foreach (var extraPart in mainPart.ExtraParts)
                    // {
                    //     if (!IsGridPointEmpty(point + extraPart.point))
                    //     {
                    //         allClear = false;
                    //         break;
                    //     }
                    // }

                    // if (!allClear)
                    // {
                    //     partsGrid[point.x, point.y] = null;

                    //     return false;
                    // }

                    foreach (var extraPoint in mainPart.ExtraParts)
                    {
                        Point p = point + extraPoint.point.Rotated(cursorRotation);
                        partsGrid[p.x, p.y] = new ExtraPart(point, extraPoint.isSolid);
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
                    foreach (var mExtraPart in mainPart.ExtraParts)
                    {
                        Point p = point + mExtraPart.point.Rotated(rotationGrid[point.x, point.y]);
                        partsGrid[p.x, p.y] = null;
                    }

                    weight -= mainPart.Mass;
                    if (mainPart is EnginePart enginePart)
                        power -= enginePart.MaxSpeed;
                    if (mainPart is WheelPart wheelPart)
                        wheelCount++;

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

        private static bool IsGridPointSolid(Point point)
        {
            if (partsGrid[point.x, point.y] is Part part)
            {
                return part.IsSolid;
            }
            else
            {
                return false;
            }
        }

        private static bool IsGridPointConnected(Point point)
        {
            bool connected = false;
            foreach (var dir in FourDirs)
            {
                var translated = point + dir;

                if (IsPointInGrid(translated) && !IsGridPointEmpty(translated) && IsGridPointSolid(translated))
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
                Position = DriveWorld.VehicleStartPos,
                PhysicsMaterialOverride = vehiclePhysMat,
                ContinuousCd = RigidBody2D.CCDMode.CastRay
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
                            vehicle.AddSprite(rootTex, pos, new Vector2(-16, -16), 0);

                            vehicle.Camera2D = new Camera2D()
                            {
                                SmoothingEnabled = true,
                                Current = true,
                                LimitLeft = 0,
                                LimitBottom = 270,
                                ProcessMode = Camera2D.Camera2DProcessMode.Physics
                            };

                            vehicle.AddChild(vehicle.Camera2D);
                        }
                        else if (p is WheelPart wheelPart)
                        {
                            vehicle.AddWheel(pos, wheelPart, rotationGrid[x, y]);
                        }
                        else if (p is PistonPart pistonPart)
                        {
                            vehicle.AddPiston(pos, pistonPart, rotationGrid[x, y]);
                        }
                        else if (p is MainPart mainPart)
                        {
                            vehicle.AddSprite(mainPart.Texture, pos, mainPart.TexOffset, rotationGrid[x, y]);
                        }

                        if (p is EnginePart enginePart)
                        {
                            vehicle.AddEngineStats(enginePart);
                            vehicle.AddEngineSmoke(pos, enginePart.SmokeOffset, rotationGrid[x, y]);
                        }

                        bool addCollider = true;

                        if (p is ExtraPart extraPart)
                        {
                            Part ownerPart = partsGrid[extraPart.OwnerPart.x, extraPart.OwnerPart.y];
                            addCollider = !(ownerPart is WheelPart || ownerPart is PistonPart);
                        }

                        if (addCollider)
                            vehicle.AddSquareCollider(pos);
                    }
                }
            }

            return vehicle;
        }

        public static void ClearGrid()
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    if (!(partsGrid[x, y] is RootPart))
                        partsGrid[x, y] = null;
                }
            }
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

        public static float GetGridRotation(Point point)
        {
            if (IsPointInGrid(point))
                return rotationGrid[point.x, point.y];
            else
                return 0f;
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
                        foreach (var extraPart in cursorPart.ExtraParts)
                        {
                            var point = cursorGridPos + extraPart.point.Rotated(cursorRotation);

                            if (!IsGridPointEmpty(point))
                            {
                                cursorClear = false;
                                break;
                            }
                            else if (IsGridPointConnected(point) && extraPart.isSolid)
                            {
                                cursorConnected = true;
                            }
                        }
                    }

                    gridCursor.Texture = (cursorClear && cursorConnected) ? gridCursorGreenTex : gridCursorRedTex;

                    // Arrow
                    cursorArrow.Texture = (cursorClear && cursorConnected) ? gridCursorArrowGreenTex : gridCursorArrowRedTex;
                    cursorArrow.Rotation = cursorRotation;

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
                            extraGridCursors[i].Position = (cursorPart.ExtraParts[i].point.Rotated(cursorRotation) * 32).ToVector2();
                            extraGridCursors[i].Texture = (cursorClear && cursorConnected) ? (cursorPart.ExtraParts[i].isSolid ? gridCursorGreenTex : gridCursorDarkGreenTex) : (cursorPart.ExtraParts[i].isSolid ? gridCursorRedTex : gridCursorDarkRedTex);
                            extraGridCursors[i].Visible = true;
                            extraGridCursors[i].ZIndex = cursorPart.ExtraParts[i].isSolid ? 1 : 0;
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
            if (inventory[part] == 0)
                return;

            SetCursorPart(part);
            selectedPartLabel.Text = $"Selected part:\n{part.PartName}";
        }

        private void SwitchToDriveWorld()
        {
            if (wheelCount > 0 && power > 0)
            {
                transitionRect.FadeOut(new Color("52333f"), 2f);
            }
            else
            {
                //Popup
                GetNode<AcceptDialog>("AcceptDialog").PopupCentered(new Vector2(276, 58));
            }
        }

        private void ShowTooltip(MainPart part)
        {
            partTooltip.UpdateFromPart(part, partButtonShortcuts[part]);

            partTooltip.Visible = true;
        }

        private void HideTooltip()
        {
            partTooltip.Visible = false;
        }

        private static double DLerp(double a, double b, float t)
        {
            return a + (b - a) * t;
        }

        private void UnlockMilestoneParts()
        {
            float maxDist = SaveData.MaxDistance;

            foreach (var milestone in MileStoneParts.Keys)
            {
                if (maxDist >= milestone)
                {
                    foreach (var parts in MileStoneParts[milestone])
                    {
                        inventory[parts.part] += parts.count;
                    }
                }
            }

            foreach (var part in partButtons.Keys)
            {
                partButtons[part].SetCount(inventory[part]);
            }
        }

        private void TransitionFinished(bool fadeIn)
        {
            if (!fadeIn)
            {
                // Switch to drive world
                DriveWorld driveWorld = GD.Load<PackedScene>("res://scenes/DriveWorld.tscn").Instance<DriveWorld>();

                RemoveDisconnectedParts();
                vehicle = ConstructVehicle(driveWorld);
                driveWorld.SetVehicle(vehicle);

                GetTree().Root.AddChild(driveWorld);
                QueueFree();
            }
        }

        public static void OverwriteGrid(Part[,] newPartsGrid)
        {
            partsGrid = newPartsGrid;
        }

        public static Part[,] ClonePartsGrid()
        {
            Part[,] copy = new Part[GRID_WIDTH, GRID_HEIGHT];
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    copy[x, y] = partsGrid[x, y];
                }
            }

            return copy;
        }
    }
}

