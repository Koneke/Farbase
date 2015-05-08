using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum UnitTypes
    {
        Worker,
        Scout,
        Fighter
    }

    public enum UnitAbilites
    {
        Mining
    }

    public class UnitType
    {
        private static Dictionary<UnitTypes, UnitType> types =
            new Dictionary<UnitTypes, UnitType>();

        public static List<UnitType> UnitTypes
        {
            get { return types.Values.ToList(); }
        }

        public static UnitType GetType(UnitTypes type)
        {
            return types[type];
        }

        public UnitTypes Type;
        public string Name;
        public string Texture;
        public int Cost;
        public int ConstructionTime;
        public int Moves;
        public int Attacks;
        public int Strength;
        public List<UnitAbilites> Abilities;
        public List<TechID> Prerequisites;

        public UnitType(UnitTypes type)
        {
            Type = type;
            Abilities = new List<UnitAbilites>();
            Prerequisites = new List<TechID>();

            types.Add(type, this);
        }
    }

    public class Unit : IAnimateable
    {
        // this is stuff we probably want in a sprite class
        // or something like that later.
        // this works for now, since the only animateable thing in
        // the game is the units (at the moment).
        // === === === === === === === === === ===

        private Animateable animateable;
        public Animateable GetAnimateable()
        {
            return animateable;
        }
        public AnimationValues GetAnimationValues()
        {
            return new AnimationValues(fPosition);
        }

        // === === === === === === === === === ===

        public static int IDCounter = 0;

        private fbGame game;
        public fbWorld World;

        public UnitType UnitType;
        public int Owner;
        public int ID;
        //vector this?
        public int x, y;

        public Vector2i WarpTarget;
        public int WarpCountdown;

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
            game = World.Game;

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

            if (WarpTarget != null && WarpCountdown >= 0)
            {
                WarpCountdown = Math.Max(0, WarpCountdown - 1);

                if (WarpCountdown == 0 && CanMoveTo(WarpTarget))
                {
                    game.EventHandler.Push(
                        new UnitMoveEvent(
                            ID,
                            WarpTarget,
                            true
                        )
                    );

                    WarpTarget = null;
                    WarpCountdown = -1;
                }
            }

            if (Tile.Station != null)
            {
                if (Strength < UnitType.Strength)
                    Strength++;
            }
        }

        public bool CanMoveTo(Vector2i position)
        {
            if (WarpTarget != null && WarpCountdown > 0) return false;
            if (Moves <= 0) return false;

            //only bad condition atm is collision
            if (World.Map.At(position).Unit == null)
                return true;
            return false;
        }

        public bool CanAttack(Vector2i position)
        {
            if (Attacks <= 0) return false;

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
