using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace SFML.NET
{
    class Program
    {
        static void Main(string[] args)
        {
            var window = new simpleWindow();
            window.Run();


        }
        class simpleWindow
        {
            XYPlane plane = new XYPlane(-5, 5, -5, 5);
            public void Run()
            {
                ContextSettings settings = new();
                settings.AntialiasingLevel = 2;
                var window = new SFML.Graphics.RenderWindow(new Window.VideoMode(800, 800), "Kartinka", Styles.Close, settings);
                window.KeyPressed += Window_KeyPressed;
                window.MouseButtonPressed += MouseButton_Pressed;
                window.MouseButtonReleased += MouseButton_Released;
                window.MouseMoved += Mouse_Moved;
                window.MouseWheelScrolled += Mouse_Scrolled;
                AbobaBox box = new();
                box.AddAboba(1, 2, 2, 1, 2);
                box.AddAboba(1, -2, 2.4f, 2, 3);
                box.AddAboba(3, -1, 2.6f, -2, 1);
                box.AddAboba(4, 0, 2.2f, -1, 2);
                box.AddAboba(2.2f, 3, 2.7f, 0, 4);
                box.AddAboba(-2, 2, 2.8f, -5, 1);
                plane.AddCurve(new EquationDrawer(plane, box.Equation, 0.05, Color.Blue));
                while (window.IsOpen)
                {
                    window.DispatchEvents();
                    window.Clear(Color.White);
                    window.Draw(plane);
                    box.Step(0.005f);
                    foreach (var item in box.abobas)
                    {
                        if (item.x + item.r / box.abobas.Count > plane.xmax)
                            item.vx *= -1;
                        if (item.x - item.r / box.abobas.Count < plane.xmin)
                            item.vx *= -1;
                        if (item.y + item.r / box.abobas.Count > plane.ymax)
                            item.vy *= -1;
                        if (item.y - item.r / box.abobas.Count < plane.ymin)
                            item.vy *= -1;
                    }
                    window.Display();
                }
            }
            private void Window_KeyPressed(object sender, SFML.Window.KeyEventArgs e)
            {
                var window = (SFML.Window.Window)sender;
                if (e.Code == SFML.Window.Keyboard.Key.Escape)
                {
                    window.Close();
                }
            }
            private bool LeftMouseClicked { get; set; } = false;
            private void MouseButton_Pressed(object sender, SFML.Window.MouseButtonEventArgs e)
            {
                if (e.Button.HasFlag(Mouse.Button.Left))
                {
                    LeftMouseClicked = true;
                }

            }
            private void MouseButton_Released(object sender, SFML.Window.MouseButtonEventArgs e)
            {
                if (e.Button.HasFlag(Mouse.Button.Left))
                {
                    LeftMouseClicked = false;
                }

            }
            private void Mouse_Scrolled(object sender, SFML.Window.MouseWheelScrollEventArgs e)
            {
                if (e.Wheel.HasFlag(Mouse.Wheel.VerticalWheel))
                {
                    plane.Rescale(1 + e.Delta * 0.2f);                    
                }
            }
            private int mouseX { get; set; }
            private int mouseY { get; set; }
            private void Mouse_Moved(object sender, SFML.Window.MouseMoveEventArgs e)
            {
                var window = (SFML.Window.Window)sender;
                if (LeftMouseClicked)
                {
                    int dx = -e.X + mouseX;
                    int dy = e.Y - mouseY;
                    plane.MoveX(dx, window.Size);
                    plane.MoveY(dy, window.Size);
                    mouseX = e.X;
                    mouseY = e.Y;
                }
                else
                {
                    mouseX = e.X;
                    mouseY = e.Y;
                }
            }

        }

        class XYPlane : Transformable, Drawable
        {
            public float xmin { get; private set; }
            public float xmax { get; private set; }
            public float ymin { get; private set; }
            public float ymax { get; private set; }
            List<EquationDrawer> equations = new();
            public void MoveX(int pixels, Vector2u size)
            {
                float dx = pixels * (xmax - xmin) / (size.X);
                xmin += dx;
                xmax += dx;
            }
            public void MoveY(int pixels, Vector2u size)
            {
                float dy = pixels * (ymax - ymin) / (size.Y);
                ymin += dy;
                ymax += dy;
            }
            public void Rescale(float k)
            {
                float hx = (xmax - xmin) * (k - 1) / 2;
                float hy = (ymax - ymin) * (k - 1) / 2;
                xmin -= hx;
                xmax += hx;
                ymin -= hy;
                ymax += hy;
                foreach (var item in equations)
                {
                    item.h *= k;
                }
            }
            public XYPlane(float xmin, float xmax, float ymin, float ymax)
            {
                this.xmin = xmin;
                this.xmax = xmax;
                this.ymin = ymin;
                this.ymax = ymax;
            }
            public void AddCurve(EquationDrawer item) => equations.Add(item);
            public void Draw(RenderTarget target, RenderStates states)
            {

                foreach (var item in equations)
                {
                    item.Draw(target, states);
                }

            }

        }
        class EquationDrawer : Drawable
        {
            private XYPlane plane;
            Func<float, float, float> func;

            public EquationDrawer(XYPlane plane, Func<float, float, float> func, double h, Color color)
            {
                this.plane = plane;
                this.func = func;
                this.h = h;
                this.color = color;
            }

            public double h { get; set; }
            Color color { get; set; }
            public void Draw(RenderTarget target, RenderStates states)
            {
                Func<float, float, Vector2f> XYtoVec = (float x, float y) =>
                {
                    var size = target.Size;
                    return new Vector2f(size.X * (x - plane.xmin) / (plane.xmax - plane.xmin), size.Y * (plane.ymax - y) / (plane.ymax - plane.ymin));
                };
                int n = (int)Math.Ceiling((plane.xmax - plane.xmin) / h);
                float hx = (plane.xmax - plane.xmin) / n;
                int m = (int)Math.Ceiling((plane.ymax - plane.ymin) / h);
                float hy = (plane.ymax - plane.ymin) / m;
                float[][] Grid = new float[n][];
                float x = plane.xmin;
                float y = plane.ymin;
                for (int i = 0; i < n; i++)
                {
                    Grid[i] = new float[m];
                    y = plane.ymin;
                    for (int j = 0; j < m; j++)
                    {
                        Grid[i][j] = func(x, y);
                        y += hy;
                    }
                    x += hx;
                }
                x = plane.xmin;
                List<Vertex> verts = new();
                for (int i = 0; i < n - 1; i++)
                {
                    y = plane.ymin;
                    for (int j = 0; j < m - 1; j++)
                    {
                        //target.Draw(new Vertex[2] { new Vertex(new Vector2f(x, y), color), new Vertex(new System.Vector2f(x, y) ,color)},PrimitiveType.Lines);
                        float f1 = Grid[i][j], f2 = Grid[i + 1][j], f3 = Grid[i + 1][j + 1], f4 = Grid[i][j + 1];
                        if (f1 * f2 < 0)
                        {
                            verts.Add(new Vertex(XYtoVec((f2 * x - f1 * (x + hx)) / (f2 - f1), y), color));
                        }
                        if (f2 * f3 < 0)
                        {
                            verts.Add(new Vertex(XYtoVec(x + hx, (f3 * y - f2 * (y + hy)) / (f3 - f2)), color));
                        }
                        if (f3 * f4 < 0)
                        {
                            verts.Add(new Vertex(XYtoVec((f3 * x - f4 * (x + hx)) / (f3 - f4), y + hy), color));
                        }
                        if (f1 * f4 < 0)
                        {
                            verts.Add(new Vertex(XYtoVec(x, (f4 * y - f1 * (y + hy)) / (f4 - f1)), color));
                        }
                        if (f1 == 0)
                        {
                            verts.Add(new Vertex(XYtoVec(x, y), color));
                        }
                        if (f2 == 0)
                        {
                            verts.Add(new Vertex(XYtoVec(x + hx, y), color));
                        }
                        if (f3 == 0)
                        {
                            verts.Add(new Vertex(XYtoVec(x + hx, y + hy), color));
                        }
                        if (f4 == 0)
                        {
                            verts.Add(new Vertex(XYtoVec(x, y + hy), color));
                        }
                        if (verts.Count == 2)
                            target.Draw(new Vertex[2] { verts[0], verts[1] }, PrimitiveType.Lines);
                        verts.Clear();
                        y += hy;
                    }
                    x += hx;
                }
            }
        }

        public class AbobaBox : ITimeable
        {
            public float Equation(float x, float y)
            {
                float res = 0;
                foreach (var item in abobas)
                {
                    res += item.r / MathF.Sqrt((x - item.x) * (x - item.x) + (y - item.y) * (y - item.y)) - 1;
                }
                return res;
            }
            public List<Aboba> abobas = new();
            public void AddAboba(float x, float y, float r, float vx, float vy)
            {
                abobas.Add(new Aboba(x, y, r, vx, vy));
            }

            public void Step(float dt)
            {
                foreach (var item in abobas)
                {
                    item.Step(dt);
                }
            }
        }
        public interface ITimeable
        {
            public void Step(float dt);
        }


        public class Aboba : ITimeable
        {
            public Aboba(float x, float y, float r, float vx, float vy)
            {
                this.x = x;
                this.y = y;
                this.r = r;
                this.vx = vx;
                this.vy = vy;
            }
            public float x { get; set; }
            public float y { get; set; }
            public float r { get; set; }
            public float vx { get; set; }
            public float vy { get; set; }

            public void Step(float dt)
            {
                x += vx * dt;
                y += vy * dt;
            }
        }
    }
}
