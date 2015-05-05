using System;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Farbase
{
    public class Player
    {
        public const int DiplomacyPointsMax = 100;

        public int ID;
        public string Name;
        public Color Color;
        public int Money;
        public int DiplomacyPoints;
        public List<int> OwnedUnits; 

        public Player(string name, int id, Color color)
        {
            Name = name;
            ID = id;
            Color = color;
            OwnedUnits = new List<int>();
        }
    }

    public class fbGame
    {
        private fbEngine engine;

        private Dictionary<string, Property> properties;

        //our ID
        public int We = -1;

        public bool OurTurn {
            get { return World.PlayerIDs[World.CurrentPlayerIndex] == We; }
        }

        public Player LocalPlayer {
            get
            {
                return We == -1
                    ? null
                    : World.Players[We];
            }
        }

        //client side of world
        public static fbWorld World;

        public List<string> Log;

        private GameEventHandler eventHandler;

        public fbGame(fbEngine engine)
        {
            NetMessage3.Setup();

            this.engine = engine;
            Unit.Game = this;
            fbNetClient.Game = this;

            engine.SetSize(1280, 720);
            properties = new Dictionary<string, Property>();

            Log = new List<string>();

            eventHandler = new GameEventHandler();

            Initialize();
        }

        public void Initialize()
        {
            //need to be centralized to somewhere
            //world sets the exact same stuff up, DRY
            UnitType scout = new UnitType();
            scout.Texture = engine.GetTexture("scout");
            scout.Moves = 2;
            scout.Strength = 3;
            scout.Attacks = 1;
            scout.Cost = 10;
            UnitType.RegisterType("scout", scout);

            UnitType worker = new UnitType();
            worker.Texture = engine.GetTexture("worker");
            worker.Moves = 1;
            worker.Strength = 1;
            worker.Cost = 5;
            UnitType.RegisterType("worker", worker);
        }

        public Property GetProperty(string name)
        {
            if(properties.ContainsKey(name.ToLower()))
                return properties[name.ToLower()];
            return null;
        }

        public void RegisterProperty(string name, Property property)
        {
            properties.Add(name.ToLower(), property);
        }


        public void Update()
        {
            foreach (Event e in engine.Poll(NameEvent.Type))
                HandleEvent((NameEvent)e);

            foreach (Event e in engine.Poll(UnitMoveEvent.Type))
                HandleEvent((UnitMoveEvent)e);

            //have yet to get map data from server
            //this is a pretty clumpsy way of doing shit, but it
            //works for the time being
            if (World == null) return;

            GetProperty("player-names")
                .SetValue(
                    World.PlayerIDs
                        .Select(id => World.Players[id].Name)
                        .ToList()
                );

            GetProperty("current-player-name")
                .SetValue(World.CurrentPlayer.Name);

            GetProperty("current-player-id")
                .SetValue(World.CurrentPlayer.ID);

            GetProperty("local-player-name")
                .SetValue(LocalPlayer.Name);

            GetProperty("local-player-id")
                .SetValue(LocalPlayer.ID);

            GetProperty("local-player-money")
                .SetValue(LocalPlayer.Money);

            GetProperty("local-player-diplo")
                .SetValue(LocalPlayer.DiplomacyPoints);

        }

        public void HandleEvent(NameEvent ne)
        {
            World.Players[ne.ID].Name = ne.Name;
            World.Players[ne.ID].Color = ne.Color;
        }

        public void HandleEvent(UnitMoveEvent ume)
        {
            Unit u = World.UnitLookup[ume.ID];
            u.MoveTo(ume.x, ume.y);
        }

        public void HandleNetMessage(NetMessage3 message)
        {
            switch (message.Signature.MessageType)
            {
                case NM3MessageType.message:
                    Log.Add((string)message.Get("message"));
                    break;

                case NM3MessageType.create_world:
                    World = new fbWorld(
                        (int)message.Get("width"),
                        (int)message.Get("height")
                    );
                    break;

                case NM3MessageType.create_station:
                    World.SpawnStation(
                        (int)message.Get("owner"),
                        (int)message.Get("x"),
                        (int)message.Get("y")
                    );
                    break;

                case NM3MessageType.create_planet:
                    World.SpawnPlanet(
                        (int)message.Get("x"),
                        (int)message.Get("y")
                    );
                    break;

                case NM3MessageType.create_unit:
                    World.SpawnUnit(
                        (string)message.Get("type"),
                        (int)message.Get("owner"),
                        (int)message.Get("id"),
                        (int)message.Get("x"),
                        (int)message.Get("y")
                    );
                    break;

                case NM3MessageType.move_unit:
                    engine.QueueEvent(
                        new UnitMoveEvent(
                            (int)message.Get("id"),
                            (int)message.Get("x"),
                            (int)message.Get("y")
                        )
                    );
                    break;

                case NM3MessageType.set_unit_moves:
                    World.UnitLookup
                        [(int)message.Get("id")].Moves =
                         (int)message.Get("amount");
                    break;

                case NM3MessageType.new_player:
                    World.AddPlayer(
                        new Player(
                            "Unnnamed player",
                            (int)message.Get("id"),
                            Color.White
                        )
                    );
                    break;

                case NM3MessageType.replenish_player:
                    World.PassTo((int)message.Get("id"));
                    break;

                case NM3MessageType.assign_id:
                    We = (int)message.Get("id");
                    break;

                case NM3MessageType.name_player:
                    engine.QueueEvent(
                        new NameEvent(
                            (int)message.Get("id"),
                            (string)message.Get("name"),
                            (string)message.Get("color")
                        )
                    );
                    break;

                case NM3MessageType.current_player:
                    World.CurrentPlayerIndex =
                        (int)message.Get("index");

                    //todo: event this?
                    Log.Add(
                        string.Format(
                            "It is now {0}'s turn.",
                            World.CurrentPlayer.Name
                        )
                    );
                    break;

                case NM3MessageType.hurt:
                    World.UnitLookup[(int)message.Get("id")]
                        .Hurt((int)message.Get("amount"));
                    break;

                case NM3MessageType.player_set_money:
                    World.Players[(int)message.Get("id")]
                        .Money = (int)message.Get("amount");
                    break;

                case NM3MessageType.client_ready:
                    engine.NetClient.Ready = true;
                    break;

                case NM3MessageType.client_unready:
                    engine.NetClient.Ready = false;
                    break;

                default:
                    throw new Exception();
            }
        }
    }
}
