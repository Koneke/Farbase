using System;
using System.Globalization;
using System.Linq;
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

        public static bool IsLetter(this char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        public static bool IsNumber(this char c)
        {
            return (c >= '0' && c <= '9');
        }

        public static char ToUpper(this char c)
        {
            if (c >= 'a' && c <= 'z')
            {
                int a = 'a';
                int A = 'A';
                return (char)(c - (a - A));
            }
            return c;
        }

        public static string Capitalize(
            this string s,
            bool lowerRest = true
        ) {
            string rest = s.Substring(1, s.Length - 1);
            return string.Format(
                "{0}{1}",
                s[0].ToUpper(),
                lowerRest
                    ? rest.ToLower()
                    : rest
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

        public Vector2i(int i)
        {
            X = Y = i;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }

        public static Vector2i operator +(Vector2i a, Vector2i b)
        {
            return new Vector2i(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2i operator -(Vector2i a, Vector2i b)
        {
            return new Vector2i(a.X - b.X, a.Y - b.Y);
        }

        public static Vector2i operator *(Vector2i a, int scalar)
        {
            return new Vector2i(a.X * scalar, a.Y * scalar);
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
        ) : this(texture, destination, null, depth, coloring) { }

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
            engine.Draw(this);
        }
    }

    public class Font
    {
        //currently only fixed size, because it's a million times easier.
        public Vector2 CharSize;
        public Texture2D FontSheet;

        public Vector2 Measure(string s)
        {
            int newLineCount = s.Count(c => c == '\n');

            string longestLine =
                s.Split('\n')
                    .OrderByDescending(str => str.Length)
                    .ElementAt(0);

            return CharSize
                * new Vector2(longestLine.Length, 1 + newLineCount);
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
            int col = 0;
            int row = 0;
            foreach (char c in Text)
            {
                if (c == '\n')
                {
                    col = 0;
                    row++;
                    continue;
                }

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
                            Position +
                                Font.CharSize * new Vector2(col++, row),
                            Font.CharSize
                        ),
                    new fbRectangle(fontSpot, Font.CharSize),
                    Depth,
                    Coloring
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

        public fbRectangle(Vector2i position, Vector2 size)
            : this(position.ToVector2(), size) { }

        public fbRectangle(Vector2i position, Vector2i size)
            : this(position.ToVector2(), size.ToVector2()) { }

        public fbRectangle(Vector2 position, Vector2i size)
            : this(position, size.ToVector2()) { }

        public fbRectangle(Vector2 position, int size)
        {
            Position = position;
            Size = new Vector2(size);
        }

        public fbRectangle(Vector2i position, int size)
            : this(position.ToVector2(), size) { }

        public fbRectangle(Vector2 position, float width, float height)
        {
            Position = position;
            Size = new Vector2(width, height);
        }

        public fbRectangle(Vector2i position, int width, int height)
            : this(position.ToVector2(), width, height) { }

        public fbRectangle(float x, float y, Vector2 size)
        {
            Position = new Vector2(x, y);
            Size = size;
        }

        public fbRectangle(float x, float y, Vector2i size)
            : this(x, y, size.ToVector2()) { }

        public fbRectangle(float x, float y, float width, float height)
        {
            Position = new Vector2(x, y);
            Size = new Vector2(width, height);
        }

        public fbRectangle Shrink(int amount)
        {
            return new fbRectangle(
                Position + new Vector2(amount / 2f),
                Size - new Vector2(amount)
            );
        }

        public fbRectangle Grow(int amount)
        {
            return new fbRectangle(
                Position - new Vector2(amount / 2f),
                Size + new Vector2(amount)
            );
        }

        public bool Contains(Vector2 mousePosition)
        {
            Vector2 relativePosition = (mousePosition - Position);
            return
                relativePosition.X > 0 && relativePosition.Y > 0 &&
                relativePosition.X < Size.X && relativePosition.Y < Size.Y;
        }
    }
}
