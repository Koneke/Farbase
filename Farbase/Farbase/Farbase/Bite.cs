using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Farbase
{
    public static class ExtensionMethods
    {
        public static T Clamp<T>(this T val, T min, T max)
            where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            if (val.CompareTo(max) > 0) return max;
            return val;
        }

        public static Color ColorFromString(string s)
        {
            if (s.Length > 8)
                throw new ArgumentException();

            Color c = new Color();
            c.R = byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber);
            c.G = byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber);
            c.B = byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber);
            c.A = byte.Parse(s.Substring(6, 2), NumberStyles.HexNumber);

            return c;
        }

        public static string ColorToString(Color c)
        {
            return string.Format(
                "{0:X2}{1:X2}{2:X2}{3:X2}",
                c.R, c.G, c.B, c.A
            );
        }
    }

    public class Vector2i
    {
        public int X;
        public int Y;

        public Vector2i(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class DrawCall
    {
        public Texture2D Texture;
        public fbRectangle Destination;
        public fbRectangle Source;
        public int Depth;
        public Color Coloring;

        public DrawCall(
            Texture2D texture,
            fbRectangle destination,
            int depth = 0,
            Color coloring = default(Color)
        ) : this(texture, destination, null, depth, coloring)
        { }

        public DrawCall(
            Texture2D texture,
            fbRectangle destination,
            fbRectangle source,
            int depth = 0,
            Color coloring = default(Color)
        ) {
            Texture = texture;
            Destination = destination;
            Source = source;
            Depth = depth;

            Coloring =
                coloring == default(Color)
                ? Color.White
                : coloring;
        }

        public void Draw(fbEngine engine)
        {
            throw new NotImplementedException();
        }
    }

    public class Font
    {
        //currently only fixed size, because it's a million times easier.
        public Vector2 CharSize;
        public Texture2D FontSheet;

        public Vector2 Measure(string s)
        {
            return CharSize * new Vector2(s.Length, 1);
        }
    }

    public class TextCall
    {
        public String Text;
        public Font Font;
        public Vector2 Position;
        public int Depth;
        public Color Coloring;

        public TextCall(
            string text,
            Font font,
            Vector2 position,
            int depth = 0,
            Color coloring = default(Color)
        ) {
            Text = text;
            Font = font;
            Position = position;
            Depth = depth;

            Coloring = coloring == default(Color)
                ? Color.White
                : coloring;
        }

        public void Draw(fbEngine engine)
        {
            for(int i = 0; i < Text.Length; i++)
            {
                int c = Text[i];
                Vector2 fontSpot =
                    new Vector2(
                        c % 16,
                        (c - (c % 16)) / 16f
                    ) * (Font.CharSize + new Vector2(1))
                    + new Vector2(1);

                engine.Draw(
                    new DrawCall(
                        Font.FontSheet,
                        new fbRectangle(
                            Position + new Vector2(Font.CharSize.X * i, 0),
                            Font.CharSize
                        ),
                        new fbRectangle(fontSpot, Font.CharSize),
                        Depth,
                        Color.White
                    )
                );
            }
        }

        public TextCall RightAlign()
        {
            Position.X -= Font.Measure(Text).X;
            return this;
        }
    }

    public class fbRectangle
    {
        public Vector2 Position, Size;

        public fbRectangle(Vector2 position, Vector2 size)
        {
            Position = position;
            Size = size;
        }

        public fbRectangle(Vector2 position, float width, float height)
        {
            Position = position;
            Size = new Vector2(width, height);
        }

        public fbRectangle(float x, float y, Vector2 size)
        {
            Position = new Vector2(x, y);
            Size = size;
        }

        public fbRectangle(float x, float y, float width, float height)
        {
            Position = new Vector2(x, y);
            Size = new Vector2(width, height);
        }
    }

}
