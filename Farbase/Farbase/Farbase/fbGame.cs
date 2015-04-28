using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Farbase
{
    public class Player
    {
        public int ID;
        public string Name;
        public Color Color;
        public int Money;
        public List<int> OwnedUnits; 

        public Player(string name, int id, Color color)
        {
            Name = name;
            ID = id;
            Color = color;
            OwnedUnits = new List<int>();
        }
    }

    public class Map
    {
        private Tile[,] map;

        public Map(int w, int h)
        {
            Width = w;
            Height = h;

            map = new Tile[w, h];
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                map[x, y] = new Tile(x, y);
        }

        public int Width;
        public int Height;

        public Tile At(int x, int y)
        {
            return map[x, y];
        }

        public Tile At(Vector2i p)
        {
            return map[p.X, p.Y];
        }

        public Tile At(Vector2 position)
        {
            return map[
                (int)position.X,
                (int)position.Y
            ];
        }
    }

    public class Tile
    {
        public Unit Unit;
        public Station Station;
        public Planet Planet;
        public Vector2 Position;

        public Tile(int x, int y)
        {
            Position = new Vector2(x, y);
        }
    }

    public class Station
    {
        public Vector2 Position;
        public Tile Tile
            { get { return fbGame.World.Map.At(Position); } }
    }

    public class Planet
    {
        public Vector2 Position;
        public Tile Tile
            { get { return fbGame.World.Map.At(Position); } }
    }

    public class fbGame
    {
        private fbApplication app;

        private fbEngine engine { get { return app.Engine; } }

        //our ID
        public int We = -1;

        public bool OurTurn {
            get { return World.PlayerIDs[World.CurrentPlayerIndex] == We; }
        }
        //client side of world
        public static fbWorld World;

        public List<string> Log;

        public fbGame(fbApplication app)
        {
            this.app = app;
            Unit.Game = this;
            fbNetClient.Game = this;
            Initialize();
        }

        public void Initialize()
        {
            engine.SetSize(1280, 720);

            Log = new List<string>();

            UnitType scout = new UnitType();
            scout.Texture = engine.GetTexture("scout");
            scout.Moves = 2;
            scout.Strength = 3;
            scout.Attacks = 1;
            UnitType.RegisterType("scout", scout);

            UnitType worker = new UnitType();
            worker.Texture = engine.GetTexture("worker");
            worker.Moves = 1;
            worker.Strength = 1;
            UnitType.RegisterType("worker", worker);
        }

        public void Update()
        {
            foreach (Event e in engine.Poll("name"))
                HandleEvent((NameEvent)e);
        }

        public void HandleEvent(NameEvent e)
        {
            World.Players[e.ID].Name = e.Name;
            World.Players[e.ID].Color = e.Color;
        }
    }
}
