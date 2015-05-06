using System;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

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

    public class fbGame
    {
        public fbEventHandler EventHandler;

        //whether or not the world is ready to be interacted with
        //(i.e. has been loaded, and not waiting for the server to solve stuff
        //on its side of things)
        public bool Ready;

        private Dictionary<string, Property> properties;

        //our ID
        public int We = -1;

        public bool OurTurn {
            get {
                return World.CurrentID == We;
            }
        }

        public Player LocalPlayer {
            get
            {
                return We == -1
                    ? null
                    : World.GetPlayer(We);
            }
        }

        public fbWorld World;

        public List<string> Log;

        public fbGame()
        {
            properties = new Dictionary<string, Property>();
            Log = new List<string>();
            Initialize();
        }

        //moving this out like this and *MANUALLY* calling it in fbApplication.
        //this means that fbGame doesn't even need an engine reference any more.
        //that in turn means that we can keep an entire fbGame in our server
        //app, which acts exactly like a normal client would.
        //only difference is that we give it another eventhandler.
        public void SetupClientSideEventHandler(fbEngine engine)
        {
            EventHandler = new GameEventHandler(this, engine);
            engine.Subscribe(EventHandler, EventType.NameEvent);
            engine.Subscribe(EventHandler, EventType.UnitMoveEvent);
            engine.Subscribe(EventHandler, EventType.BuildStationEvent);
            engine.Subscribe(EventHandler, EventType.PlayerDisconnect);
        }

        public void Initialize()
        {
            //need to be centralized to somewhere
            //world sets the exact same stuff up, DRY
            //we probably want to enum the types too
            UnitType scout = new UnitType();
            scout.Texture = "scout";
            scout.Moves = 2;
            scout.Strength = 3;
            scout.Attacks = 1;
            scout.Cost = 10;
            UnitType.RegisterType("scout", scout);

            UnitType worker = new UnitType();
            worker.Texture = "worker";
            worker.Moves = 1;
            worker.Strength = 1;
            worker.Cost = 5;
            worker.Abilities.Add(UnitAbilites.Mining);
            UnitType.RegisterType("worker", worker);

            SetupProperties();
        }

        private void SetupProperties()
        {
            RegisterProperty(
                "current-player-name",
                new Property<string>("")
            );

            RegisterProperty(
                "current-player-id",
                new Property<int>(0)
            );

            RegisterProperty(
                "local-player-name",
                new Property<string>("")
            );

            RegisterProperty(
                "local-player-id",
                new Property<int>(0)
            );

            RegisterProperty(
                "local-player-money",
                new Property<int>(0)
            );

            RegisterProperty(
                "player-names",
                new ListProperty<string>(new List<string>())
            );
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
            //have yet to get map data from server
            //this is a pretty clumpsy way of doing shit, but it
            //works for the time being
            if (World == null) return;

            GetProperty("player-names")
                .SetValue(
                    World.Players.Keys
                        .Select(p => World.Players[p].Name)
                        .ToList()
                );

            GetProperty("current-player-name")
                .SetValue(World.Players[World.CurrentID].Name);

            GetProperty("current-player-id")
                .SetValue(World.Players[World.CurrentID].ID);

            GetProperty("local-player-name")
                .SetValue(LocalPlayer.Name);

            GetProperty("local-player-id")
                .SetValue(LocalPlayer.ID);

            GetProperty("local-player-money")
                .SetValue(LocalPlayer.Money);

        }

        public void HandleNetMessage(NetMessage3 message)
        {
            switch (message.Signature.MessageType)
            {
                case NM3MessageType.message:
                    Log.Add((string)message.Get("message"));
                    break;

                case NM3MessageType.world_create:
                    World = new fbWorld(
                        (int)message.Get("width"),
                        (int)message.Get("height")
                    );
                    break;

                case NM3MessageType.station_create:
                    World.SpawnStation(
                        (int)message.Get("owner"),
                        (int)message.Get("x"),
                        (int)message.Get("y")
                    );
                    break;

                case NM3MessageType.planet_create:
                    World.SpawnPlanet(
                        (int)message.Get("x"),
                        (int)message.Get("y")
                    );
                    break;

                case NM3MessageType.unit_create:
                    World.SpawnUnit(
                        (string)message.Get("type"),
                        (int)message.Get("owner"),
                        (int)message.Get("id"),
                        (int)message.Get("x"),
                        (int)message.Get("y")
                    );
                    break;

                case NM3MessageType.unit_move:
                    EventHandler.Push(
                        new UnitMoveEvent(
                            (int)message.Get("id"),
                            (int)message.Get("x"),
                            (int)message.Get("y")
                        )
                    );
                    break;

                case NM3MessageType.player_new:
                    World.AddPlayer(
                        new Player(
                            "Unnnamed player",
                            (int)message.Get("id"),
                            Color.White
                        )
                    );
                    break;

                case NM3MessageType.player_replenish:
                    World.PassTo(World.GetPlayer(message.Get<int>("id")));
                    break;

                case NM3MessageType.player_assign_id:
                    We = (int)message.Get("id");
                    break;

                case NM3MessageType.player_name:
                    EventHandler.Push(
                        new NameEvent(
                            (int)message.Get("id"),
                            (string)message.Get("name"),
                            (string)message.Get("color")
                        )
                    );
                    break;

                case NM3MessageType.player_current:
                    World.CurrentID = message.Get<int>("index");

                    //todo: event this?
                    Log.Add(
                        string.Format(
                            "It is now {0}'s turn.",
                            World.Players[World.CurrentID].Name
                        )
                    );
                    break;

                case NM3MessageType.unit_status:
                    Unit u = World.UnitLookup[message.Get<int>("id")];
                    u.Moves = message.Get<int>("moves");
                    u.Attacks = message.Get<int>("attacks");
                    u.Strength = message.Get<int>("strength");

                    //todo: should probably be an event
                    if (u.Strength <= 0)
                        u.Despawn();
                    break;

                case NM3MessageType.player_status:
                    World.GetPlayer((int)message.Get("id"))
                        .Money = (int)message.Get("money");
                    break;

                case NM3MessageType.client_disconnect:
                    EventHandler.Push(
                        new PlayerDisconnectEvent(
                            message.Get<int>("id")
                        )
                    );
                    break;

                case NM3MessageType.client_pass:
                    World.Pass();
                    break;

                case NM3MessageType.client_ready:
                    Ready = true;
                    break;

                case NM3MessageType.client_unready:
                    Ready = false;
                    break;

                default:
                    throw new Exception();
            }
        }
    }
}
