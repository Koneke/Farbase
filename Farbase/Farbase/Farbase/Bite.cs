using System;
using Microsoft.Xna.Framework;

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
