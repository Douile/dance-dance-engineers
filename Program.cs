using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public static Random random = new Random();
        public class DanceSprite
        {
            public MySprite Sprite;
            public bool Success = false;
            private int _Index;
            public int Index { get { return _Index; } }

            public DanceSprite(int index, SpriteType type, string data, float rotation)
            {
                _Index = index;
                Sprite = new MySprite(type, data, rotation: rotation);
            }

            public static DanceSprite TOP_LEFT()
            {
                return new DanceSprite(0, SpriteType.TEXTURE, "Arrow", (float) Math.PI * 1.75f);
            }
            public static DanceSprite LEFT()
            {
                return new DanceSprite(3, SpriteType.TEXTURE, "Arrow", (float)Math.PI * 1.5f);
            }
            public static DanceSprite BOTTOM_LEFT()
            {
                return new DanceSprite(6, SpriteType.TEXTURE, "Arrow", (float) Math.PI * 1.25f);
            }
            public static DanceSprite BOTTOM()
            {
                return new DanceSprite(7, SpriteType.TEXTURE, "Arrow", (float) Math.PI);
            }
            public static DanceSprite BOTTOM_RIGHT()
            {
                return new DanceSprite(8, SpriteType.TEXTURE, "Arrow", (float) Math.PI * 0.75f);
            }
            public static DanceSprite RIGHT()
            {
                return new DanceSprite(5, SpriteType.TEXTURE, "Arrow", (float) Math.PI * 0.5f);
            }
            public static DanceSprite TOP_RIGHT()
            {
                return new DanceSprite(2, SpriteType.TEXTURE, "Arrow", (float) Math.PI * 0.25f);
            }
            public static DanceSprite TOP()
            {
                return new DanceSprite(1, SpriteType.TEXTURE, "Arrow", 0.0f);
            }
            public static DanceSprite CENTER()
            {
                return new DanceSprite(4, SpriteType.TEXTURE, "Cross", 0.0f);
            }
            public static DanceSprite[] ALL()
            {
                return new DanceSprite[]
                {
                    TOP_LEFT(),
                    TOP(),
                    TOP_RIGHT(),
                    LEFT(),
                    CENTER(),
                    RIGHT(),
                    BOTTOM_LEFT(),
                    BOTTOM(),
                    BOTTOM_RIGHT()
                };
            }
            public static DanceSprite Random()
            {
                DanceSprite[] all = ALL();
                return all[random.Next(all.Length)];
            }
            
            public static MySprite Convert(DanceSprite sprite)
            {
                return sprite.Sprite;
            }
        }

        public class Display
        {
            private IMyTextSurface surface;
            public IMyTextSurface Surface { get { return surface;  } }
            private RectangleF viewport;
            public RectangleF Viewport { get { return viewport; } }
            public Display(IMyTextSurface surface)
            {
                this.surface = surface;
                viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f,surface.SurfaceSize);
            }

            private void Setup()
            {
                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.BackgroundAlpha = 0;
                surface.ScriptBackgroundColor = Color.Black;
                surface.ScriptForegroundColor = Color.White;
            }

            virtual public void DrawFrame(MySpriteDrawFrame frame) { }

            public MySpriteDrawFrame Draw()
            {
                Setup();
                MySpriteDrawFrame frame = surface.DrawFrame();
                DrawFrame(frame);
                return frame;
            }

            public Vector2 Size()
            {
                return Viewport.Size;
            }

            public Vector2 Center()
            {
                return Viewport.Center;
            }
        }
        
        public static bool Collides(MySprite a, MySprite b)
        {
            Vector2 pa = a.Position.Value, pb = b.Position.Value,
                sa = a.Size.Value, sb = b.Size.Value;
            return (pa.X + sa.X >= pb.X && pa.X <= pb.X + sb.X) &&
                (pa.Y + sa.Y >= pb.Y && pa.Y <= pb.Y + sb.Y);
        }

        public class Game : Display
        {
            private static Vector2 SIZE = new Vector2(25f, 25f);
            private MySprite activeBox;
            private List<DanceSprite> queuedMoves;
            private Vector2 speed = new Vector2(0f, 0.5f);
            private Program program;
            private int points = 0;

            public Game(IMyTextSurface surface, Program program) : base(surface) {
                this.program = program;
                queuedMoves = new List<DanceSprite>();
                queuedMoves.Add(NextMove());
                Vector2 position = Size();
                position.X = 0;
                position.Y *= 0.7f;
                Vector2 size = Size() * 1.2f;
                size.Y = 45;
                activeBox = new MySprite(SpriteType.TEXTURE, "SquareSimple", position: position + Viewport.Position, size: size, color: Color.White);
            }
            
            override public void DrawFrame(MySpriteDrawFrame frame)
            {
                if (queuedMoves.Count == 0) queuedMoves.Add(NextMove());
                using (frame)
                {
                    bool collision = false, over = true;
                    for (int i = queuedMoves.Count - 1; i >= 0; i--)
                    {
                        Vector2 oldPos = queuedMoves[i].Sprite.Position.Value;
                        Vector2 nextPos = oldPos + speed;
                        if (nextPos.Y - Viewport.Y >= Size().Y)
                        {
                            queuedMoves.RemoveAt(i);
                        }
                        else
                        {
                            DanceSprite newMove = queuedMoves[i];
                            newMove.Sprite.Position = nextPos;
                            if (Collides(newMove.Sprite, activeBox))
                            {
                                collision = true;
                                if (program._floor != null)
                                {
                                    List<MyDetectedEntityInfo> entities = new List<MyDetectedEntityInfo>();
                                    program._sensor.DetectedEntities(entities);
                                    foreach (MyDetectedEntityInfo entity in entities)
                                    {
                                        if (EntityOver(program._floor[newMove.Index].Block, entity))
                                        {
                                            if (newMove.Success == false)
                                            {
                                                points += 1;
                                                newMove.Success = true;
                                            }
                                        }
                                    }
                                }
                                over &= newMove.Success;
                            }
                            queuedMoves[i] = newMove;
                            if ((i == queuedMoves.Count - 1) && (nextPos.Y - Viewport.Position.Y >= Size().Y * 0.2)) queuedMoves.Add(NextMove());
                        }
                    }
                    if (collision)
                    {
                        activeBox.Color = over ? Color.Green : Color.Red;
                    } else
                    {
                        activeBox.Color = Color.White;
                    }
           
                    frame.Add(new MySprite()
                    {
                        Type = SpriteType.TEXT,
                        Data = String.Format("Points {0}", points),
                        Position = Viewport.Position,
                        RotationOrScale = 1.0f,
                        Color = Color.Red,
                        Alignment = TextAlignment.LEFT,
                        FontId = "White"
                    });
                    frame.Add(activeBox);
                    frame.AddRange(queuedMoves.ConvertAll(move => move.Sprite));
                }
            }

            public DanceSprite NextMove()
            {
                DanceSprite sprite = DanceSprite.Random();
                Vector2 pos = Center();
                pos.Y = 0;
                sprite.Sprite.Position = pos+Viewport.Position;
                sprite.Sprite.Size = SIZE;
                return sprite;
            }
        }

        public class FilledDisplay : Display
        {
            private MySprite sprite;
            private IMyTerminalBlock _block;
            public IMyTerminalBlock Block { get { return _block;  } }
            public FilledDisplay(IMyTextSurface surface, MySprite sprite, IMyTerminalBlock block) : base(surface)
            {
                _block = block;
                this.sprite = sprite;
            }
            public override void DrawFrame(MySpriteDrawFrame frame)
            {
                using (frame)
                {
                    sprite.Size = Viewport.Size;
                    sprite.Position = Viewport.Center;
                    frame.Add(sprite);
                }
            }
        }

        public class Logger
        {
            private IMyTextSurface surface;
            private string[] buffer;
            private int bufferpos = 0;

            public Logger(IMyTextSurface surface)
            {
                this.surface = surface;
                surface.ContentType = ContentType.TEXT_AND_IMAGE;
                surface.WriteText("", false);
                surface.ClearImagesFromSelection();
                surface.FontSize = 0.6f;
                Vector2 linesize = Measure(" ");
                int lines = (int)Math.Floor(surface.SurfaceSize.Y / linesize.Y);
                this.buffer = new string[lines];
            }

            public Vector2 Measure(string text)
            {
                return this.surface.MeasureStringInPixels(new StringBuilder().Append(text), this.surface.Font, this.surface.FontSize);
            }

            public void Log(String text)
            {
                string output = "";
                if (bufferpos < buffer.Length)
                {
                    buffer[bufferpos] = text;
                    bufferpos += 1;
                    for (int i = 0; i < bufferpos; i++)
                    {
                        output += buffer[i] + "\n";
                    }
                }
                else
                {
                    for (int i = 1; i < buffer.Length; i++)
                    {
                        output += buffer[i] + "\n";
                        buffer[i - 1] = buffer[i];
                    }
                    buffer[bufferpos] = text;
                    output += text + "\n";
                }
                this.surface.WriteText(output, false);
            }
        }

        public List<Display> _displays;
        public FilledDisplay[] _floor;
        public static Logger _logger;
        public IMySensorBlock _sensor;

        public Program()
        {
            _logger = new Logger(Me.GetSurface(0));
            _displays = new List<Display>();
            // _displays.Add(new Game(Me.GetSurface(0), this));
            _floor = EnumerateFloor();
            _sensor = (IMySensorBlock) GridTerminalSystem.GetBlockWithName("F_SENSOR");
            Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update1;
            Log("Initialized");
        }


        public static void Log(string text)
        {
            _logger.Log(text);
        }

        public static bool IsOfType(UpdateType source, UpdateType test)
        {
            return (source & test) != 0;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (IsOfType(updateSource, UpdateType.Trigger | UpdateType.Terminal))
            {
                string command = argument.ToLower();
                switch (command)
                {
                    case "clear":
                        _displays.Clear();
                        Log("Cleared");
                        return;
                    case "floor":
                        _floor = EnumerateFloor();
                        _sensor = (IMySensorBlock)GridTerminalSystem.GetBlockWithName("F_SENSOR");
                        Log("Enumerated");
                        return;
                }
                IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(argument);
                if (block != null)
                {
                    if (block is IMyTextSurface)
                    {
                        _displays.Add(new Game((IMyTextSurface) block, this));
                        Log(String.Format("Added (#{0} displays)", _displays.Count));
                        return;
                    }
                    if (block is IMyTextSurfaceProvider)
                    {
                        _displays.Add(new Game(((IMyTextSurfaceProvider)block).GetSurface(0), this));
                        Log(String.Format("Added (#{0} displays)", _displays.Count));
                        return;
                    }
                }
                Log("Not found");
            }
            if (IsOfType(updateSource, UpdateType.Update1 | UpdateType.Once))
            {
                foreach(Display display in _displays) {
                    display.Draw();
                }
            }
        }

        public FilledDisplay[] EnumerateFloor()
        {
            FilledDisplay[] floor = new FilledDisplay[9];
            IMyCubeBlock topLeft = GridTerminalSystem.GetBlockWithName("P_FLOOR");
            if (topLeft != null && topLeft is IMyTextPanel)
            {
                DanceSprite[] sprites = DanceSprite.ALL();
                Vector3I origin = topLeft.Position;
                Vector3I Y = Base6Directions.GetIntVector(topLeft.Orientation.Up) * -1;
                Vector3I X = Base6Directions.GetIntVector(topLeft.Orientation.Left) * -1;
                Vector3I[] positions = new Vector3I[9];
                for (int y=0;y<3;y++)
                {
                    for (int x=0;x<3;x++)
                    {
                        positions[(y * 3) + x] = origin + (Y * y) + (X * x);
                    }
                }
                int found = 0;
                List<IMyTextPanel> panels = new List<IMyTextPanel>();
                GridTerminalSystem.GetBlocksOfType(panels);
                foreach (IMyTextPanel panel in panels)
                {
                    for (int i=0;i<positions.Length;i++)
                    {
                        if (panel.Position == positions[i])
                        {
                            floor[i] = new FilledDisplay(panel, sprites[i].Sprite, panel);
                            floor[i].Draw();
                            Log(String.Format("Addded floor #{0} in position {1}", i, panel.Position));
                            found++;
                        }
                    }
                }
            }
            return floor;
        }

        public interface Comparison<T>
        {
            bool Compare(T a, T b);
        }

        public static class DoubleComparisons
        {

            public class GreaterOrEqual : Comparison<double>
            {
                public bool Compare(double a, double b)
                {
                    return a >= b;
                }
            }
            public class LessOrEqual : Comparison<double>
            {
                public bool Compare(double a, double b)
                {
                    return a <= b;
                }
            }
        }

        public static bool VectorCompare(Vector3D a, Vector3D b, Comparison<double> comparitor)
        {
            return comparitor.Compare(a.X, b.X) &&
                comparitor.Compare(a.Y, b.Y) &&
                comparitor.Compare(a.Z, b.Z);
        }

        public static bool EntityOver(IMyCubeBlock block, MyDetectedEntityInfo entity)
        {
            Vector3D center = block.GetPosition();
            Vector3D min = center - new Vector3D(1.25);
            Vector3D max = center + new Vector3D(1.25);
            return VectorCompare(entity.Position, min, new DoubleComparisons.GreaterOrEqual()) &&
                VectorCompare(entity.Position, max, new DoubleComparisons.LessOrEqual());
        }
    }
}
