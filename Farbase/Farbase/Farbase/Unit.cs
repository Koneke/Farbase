using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum UnitAbilites
    {
        Mining
    }

    public class UnitType
    {
        private static Dictionary<string, UnitType> types =
            new Dictionary<string, UnitType>();

        public static List<UnitType> UnitTypes
        {
            get { return types.Values.ToList(); }
        }

        public UnitType()
        {
            Abilities = new List<UnitAbilites>();
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
        public string Texture;
        public int Cost;
        public int Moves;
        public int Attacks;
        public int Strength;
        public List<UnitAbilites> Abilities;
    }

    public class Unit : IAnimateable
    {
        // this is stuff we probably want in a sprite class
        // or something like that later.
        // this works for now, since the only animateable thing in
        // the game is the units (at the moment).
        // === === === === === === === === === ===

        private Animateable animateable;
        public Animateable GetAnimateable() { return animateable; }
        public AnimationValues GetAnimationValues()
        {
            return new AnimationValues(fPosition);
        }

        // === === === === === === === === === ===

        private fbWorld World;

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
                return World.Map.At(x, y);
            }
        }

        public int Moves;
        public int Attacks;
        public int Strength;

        public Unit(
            fbWorld world,
            UnitType unitType,
            int owner,
            int id,
            int x,
            int y
        ) {
            animateable = new Animateable(this);

            World = world;
            UnitType = unitType;
            Owner = owner;
            ID = id;
            this.x = x;
            this.y = y;
            Moves = UnitType.Moves;
            Attacks = UnitType.Attacks;
            Strength = UnitType.Strength;
        }

        public void Recharge()
        {
            Moves = UnitType.Moves;
            Attacks = UnitType.Attacks;

            if (Tile.Station != null)
            {
                if (Strength < UnitType.Strength)
                    Strength++;
            }
        }

        public bool CanMoveTo(Vector2i position)
        {
            //only bad condition atm is collision
            if (World.Map.At(position).Unit == null)
                return true;
            return false;
        }

        public bool CanAttack(Vector2i position)
        {
            if (Attacks <= 0)
                return false;

            Vector2i delta = Position - position;
            //only neighbours
            if (Math.Abs(delta.X) > 1 || Math.Abs(delta.Y) > 1)
                return false;

            if (World.Map.At(position).Unit != null)
                if (World.Map.At(position).Unit.Owner != Owner)
                    return true;

            return false;
        }

        public void MoveTo(int tx, int ty)
        {
            World.Map.At(x, y).Unit = null;
            x = tx;
            y = ty;
            World.Map.At(tx, ty).Unit = this;
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

        public bool HasAbility(UnitAbilites ability)
        {
            return UnitType.Abilities.Contains(ability);
        }
    }
}
