using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Farbase
{
    public class UnitType
    {
        private static Dictionary<string, UnitType> types =
            new Dictionary<string, UnitType>();

        public static List<UnitType> UnitTypes
        {
            get { return types.Values.ToList(); }
        }

        public static void RegisterType(string name, UnitType type)
        {
            name = name.ToLower();
            types.Add(name, type);
            type.Name = name;
        }

        public static UnitType GetType(string name)
        {
            name = name.ToLower();
            return types[name];
        }

        public string Name;
        public Texture2D Texture;
        public int Cost;
        public int Moves;
        public int Attacks;
        public int Strength;
    }

    public class Unit
    {
        public static fbGame Game;

        public UnitType UnitType;
        public int Owner;
        public int ID;

        public int x, y;

        public Vector2i Position
        {
            get { return new Vector2i(x, y); }
        }

        //ugly and should not remain
        //it's really only because some pieces of the code still only
        //takes xna Vector2
        public Vector2 fPosition
        {
            get { return new Vector2(x, y); }
        }

        public Tile Tile
        {
            get {
                return fbGame.World.Map.At(x, y);
            }
        }

        public int Moves;
        public int Attacks;
        public int Strength;

        public Unit(
            UnitType unitType,
            int owner,
            int id,
            int x,
            int y
        ) {
            UnitType = unitType;
            Owner = owner;
            ID = id;
            this.x = x;
            this.y = y;
            Moves = UnitType.Moves;
            Attacks = UnitType.Attacks;
            Strength = UnitType.Strength;
        }

        public Unit(
            UnitType unitType,
            int owner,
            int id,
            Vector2 position
        ) : this(unitType, owner, id, (int)position.X, (int)position.Y) {
        }

        public void Replenish()
        {
            Moves = UnitType.Moves;
            Attacks = UnitType.Attacks;

            //workers generate cash if they start the turn on a planet
            /*if(UnitType == UnitType.GetType("worker"))
                if (Tile.Planet != null)
                    fbGame.World.Players[Owner].Money += 10;*/

            if (Tile.Station != null)
            {
                if (Strength < UnitType.Strength)
                    Strength++;
            }
        }

        public bool CanMoveTo(Vector2i position)
        {
            //only bad condition atm is collision
            if (fbGame.World.Map.At(position).Unit == null)
                return true;
            return false;
        }

        public bool CanAttack(Vector2i position)
        {
            Vector2i delta = Position - position;
            //only neighbours
            if (Math.Abs(delta.X) > 1 || Math.Abs(delta.Y) > 1)
                return false;

            if (fbGame.World.Map.At(position).Unit != null)
                if (fbGame.World.Map.At(position).Unit.Owner != Owner)
                    return true;

            return false;
        }

        public void MoveTo(int tx, int ty)
        {
            Tile.Unit = null;
            x = tx;
            y = ty;
            fbGame.World.Map.At(tx, ty).Unit = this;
        }

        public Vector2i StepTowards(Vector2i goal)
        {
            //return the next move order for the path towards the given point.

            //todo: real pathfinding.
            //      at the moment, I think the fast naive approach is enough,
            //      since overall, there's only very, very few possible
            //      obstacles in the game (namely, only units).
            //      there's apparently a load of space in space, whoda thunk.

            Vector2i delta = goal - Position;
            delta.X = delta.X.Clamp(-1, 1);
            delta.Y = delta.Y.Clamp(-1, 1);

            Vector2i moveOrder = new Vector2i(0);

            if (delta.X == 0 && delta.Y == 0)
                return moveOrder;

            if (CanMoveTo(Position + delta))
                return delta;

            if (
                delta.X != 0 &&
                CanMoveTo(Position + new Vector2i(delta.X, 0))
            )
                return new Vector2i(delta.X, 0);

            if (CanMoveTo(Position + new Vector2i(0, delta.Y)))
                return new Vector2i(0, delta.Y);

            return new Vector2i(0);
        }

        public void Hurt(int amount)
        {
            Strength -= amount;
            if (Strength <= 0)
            {
                //fbGame.World.DespawnUnit(this)?
                fbGame.World.Units.Remove(this);
                fbGame.World.Map.At(x, y).Unit = null;
                fbGame.World.UnitLookup.Remove(ID);
                fbGame.World.Players[Owner].OwnedUnits.Remove(ID);
            }
        }
    }
}
