using Godot;
using Fragile.Parts;

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
            public static bool operator ==(Point a, Point b) => a.x == b.x && a.y == b.y;
            public static bool operator !=(Point a, Point b) => a.x != b.x || a.y != b.y;

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

            public override string ToString()
            {
                return $"({x}, {y})";
            }
        }


        private const int GRID_HEIGHT = 8;
        private const int GRID_WIDTH = 12;
        private static Texture rootTex = GD.Load<Texture>("res://textures/root.png");

        private static Part[,] partsGrid = new Part[GRID_WIDTH, GRID_HEIGHT];

        private Node2D grid;
        private Label debug;
        private Point mouseGridPos = new Point(0, 0);
        private MainPart mousePart = Parts.Parts.EngineLarge;
        private Point rootPartPos = new Point(6, 4);

        public override void _Ready()
        {
            base._Ready();

            grid = GetNode<Node2D>("Grid");
            debug = GetNode<Label>("debug");

            partsGrid[rootPartPos.x, rootPartPos.y] = Parts.Parts.RootPart;
        }

        public override void _Process(float delta)
        {
            base._Process(delta);

            Update();
        }

        public override void _Input(InputEvent evt)
        {
            base._Input(evt);

            if (evt is InputEventMouseMotion emm)
            {
                mouseGridPos = new Point((emm.Position - grid.Position) / 32);
                debug.Text = mouseGridPos.ToString();
                Update();
            }
            else if (evt is InputEventMouseButton emb && emb.Pressed)
            {
                if (emb.ButtonIndex == (int)ButtonList.Left)
                {
                    TryPlacePart(mouseGridPos, mousePart);
                }
                else if (emb.ButtonIndex == (int)ButtonList.Right)
                {
                    RemovePart(mouseGridPos);
                }
            }

            if (evt is InputEventKey ek && ek.Pressed)
            {
                switch (ek.Scancode)
                {
                    case (int)KeyList.Key1:
                        mousePart = Parts.Parts.Body;
                        break;
                    case (int)KeyList.Key2:
                        mousePart = Parts.Parts.EngineLarge;
                        break;
                    case (int)KeyList.Key3:
                        mousePart = Parts.Parts.Wheel;
                        break;
                    case (int)KeyList.Space:
                        RemoveDisconnectedParts();
                        break;
                    case (int)KeyList.Enter:
                        RemoveDisconnectedParts();
                        var vehicle = ConstructVehicle();
                        vehicle.SetPositionWithWheels(new Vector2(480 / 2, 0));
                        break;
                }
            }
        }

        public override void _Draw()
        {
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                DrawLine(grid.Position + new Vector2(x * 32, 0), grid.Position + new Vector2(x * 32, GRID_HEIGHT * 32), Colors.DarkRed, 2);
            }

            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                DrawLine(grid.Position + new Vector2(0, y * 32), grid.Position + new Vector2(GRID_WIDTH * 32, y * 32), Colors.DarkCyan, 2);

                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    if (!IsGridPointEmpty(x, y))
                    {
                        // DrawRect(new Rect2(grid.Position + new Vector2(x * 32, y * 32), new Vector2(32, 32)), Colors.RoyalBlue);
                        if (partsGrid[x, y] is MainPart mainPart)
                        {
                            DrawTexture(mainPart.Texture, grid.Position + mainPart.TexOffset + new Vector2(x * 32, y * 32));
                        }
                        else if (partsGrid[x, y] is RootPart)
                        {
                            DrawTexture(rootTex, grid.Position + new Vector2(x * 32, y * 32));
                        }
                    }
                }
            }

            int mouseX = (int)mouseGridPos.x;
            int mouseY = (int)mouseGridPos.y;

            if (mousePart != null && mouseX >= 0 && mouseX < GRID_WIDTH && mouseY >= 0 && mouseY < GRID_HEIGHT)
            {
                bool clear = true;

                if (partsGrid[mouseX, mouseY] != null)
                    clear = false;

                foreach (var extraPos in mousePart.ExtraParts)
                {
                    if (!IsGridPointEmpty(mouseX + (int)extraPos.x, mouseY + (int)extraPos.y))
                    {
                        clear = false;
                        break;
                    }
                }

                DrawRect(new Rect2(grid.Position + new Vector2(mouseX * 32, mouseY * 32), new Vector2(32, 32)), clear ? Colors.Green : Colors.Red, false, 4);
                foreach (var extraPos in mousePart.ExtraParts)
                {
                    DrawRect(new Rect2(grid.Position + new Vector2((mouseX + (int)extraPos.x) * 32, (mouseY + (int)extraPos.y) * 32), new Vector2(32, 32)), clear ? Colors.Green : Colors.Red, false, 4);
                }
            }
        }

        private bool TryPlacePart(Point point, Part part)
        {
            if (!IsGridPointEmpty(point))
            {
                return false;
            }
            else
            {
                partsGrid[point.x, point.y] = part;

                if (part is MainPart mainPart)
                {
                    bool allClear = true;

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
                if (partsGrid[point.x, point.y] is MainPart mainPart)
                {
                    foreach (Point extraPos in mainPart.ExtraParts)
                    {
                        Point p = point + extraPos;
                        partsGrid[p.x, p.y] = null;
                    }
                }

                partsGrid[point.x, point.y] = null;

                Update();

                //TODO: check if connected to main vehicle part
                RemoveDisconnectedParts();
            }
        }

        private void RemoveDisconnectedParts()
        {
            System.Collections.Generic.HashSet<Point> connectedParts = new System.Collections.Generic.HashSet<Point>();
            System.Collections.Generic.List<Point> workingParts = new System.Collections.Generic.List<Point>();
            workingParts.Add(rootPartPos);
            connectedParts.Add(rootPartPos);
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

            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    Point p = new Point(x, y);

                    if (!IsGridPointEmpty(p))
                    {
                        if (!connectedParts.Contains(p))
                        {
                            RemovePart(p);
                        }
                    }
                }
            }

            Update();
        }

        private static bool IsPointInGrid(Point point)
        {
            return IsPointInGrid(point.x, point.y);
        }

        private static bool IsPointInGrid(int x, int y)
        {
            return x >= 0 && x < GRID_WIDTH && y >= 0 && y < GRID_HEIGHT;
        }

        private bool IsGridPointEmpty(Point point)
        {
            return IsGridPointEmpty(point.x, point.y);
        }

        private bool IsGridPointEmpty(int x, int y)
        {
            if (IsPointInGrid(x, y))
            {
                return partsGrid[x, y] == null;
            }

            // Out of bounds
            return false;
        }

        private Vehicle ConstructVehicle()
        {
            Vehicle vehicle = new Vehicle();

            AddChild(vehicle);
            vehicle.Owner = this;

            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    Part p = partsGrid[x, y];

                    if (p != null)
                    {
                        Point pos = new Point(x, y) - rootPartPos;

                        if (p is RootPart)
                        {
                            vehicle.AddSprite(rootTex, pos);
                        }
                        else if (p is WheelPart wheelPart)
                        {
                            vehicle.AddWheel(pos, wheelPart);
                        }
                        else if (p is MainPart mainPart)
                        {
                            vehicle.AddSprite(mainPart.Texture, pos, mainPart.TexOffset);
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
    }
}

