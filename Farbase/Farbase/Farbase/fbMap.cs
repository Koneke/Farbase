using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public class fbMap
    {
        private Tile[,] map;

        public int Width;
        public int Height;

        //should be generalized to structure
        public Dictionary<int, Station> Stations;
        //should probably have ids later..?
        //are they structures..?
        public List<Planet> Planets;

        public fbMap(int w, int h)
        {
            Width = w;
            Height = h;

            map = new Tile[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    map[x, y] = new Tile(this, x, y);

            Stations = new Dictionary<int, Station>();
            Planets = new List<Planet>();
        }

        public Tile At(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return null;
            return map[x, y];
        }

        public Tile At(Vector2i p)
        {
            return At(p.X, p.Y);
        }

        public Tile At(Vector2 position)
        {
            return At(
                (int)position.X,
                (int)position.Y
            );
        }
    }
}