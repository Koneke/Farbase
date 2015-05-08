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
        public List<TechID> Tech;

        public Player(string name, int id, Color color)
        {
            Name = name;
            ID = id;
            Color = color;
            Tech = new List<TechID>();
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
            engine.Subscribe(EventHandler, EventType.BuildUnitEvent);
            engine.Subscribe(EventHandler, EventType.CreateUnitEvent);
            engine.Subscribe(EventHandler, EventType.PlayerDisconnect);
            engine.Subscribe(EventHandler, EventType.SetProjectEvent);
            engine.Subscribe(EventHandler, EventType.ProjectFinishedEvent);
        }

        public void Initialize()
        {
            //need to be centralized to somewhere
            //world sets the exact same stuff up, DRY

            UnitType scout = new UnitType(UnitTypes.Scout);
            scout.Name = "scout";
            scout.Texture = "unit-scout";
            scout.Moves = 3;
            scout.Strength = 3;
            scout.Attacks = 1;
            scout.Cost = 10;
            scout.ConstructionTime = 5;

            UnitType worker = new UnitType(UnitTypes.Fighter);
            worker.Name = "worker";
            worker.Texture = "unit-worker";
            worker.Moves = 1;
            worker.Strength = 1;
            worker.Cost = 5;
            worker.ConstructionTime = 5;
            worker.Abilities.Add(UnitAbilites.Mining);

            new Tech( //self registers in constructor
                TechID.FighterTech,
                "fighters",
                10,
                1
            );

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

        public bool CanResearch(
            Player player,
            TechID tech,
            Station s
        ) {
            if (s == null) return false; //only build at stations
            if (s.Owner != We) return false; //and only stations owed by us
            if (player.Money < Tech.Techs[tech].Cost) return false;

            return
                !player.Tech.Contains(tech) &&
                Tech.Techs[tech].Prerequisites
                    .All(
                        pr => player.Tech.Contains(pr)
                    );
        }

        public bool CanBuild(
            Player player,
            UnitType unitType,
            Station s
        ) {
            if (s == null) return false; //only build at stations
            if (s.Owner != We) return false; //and only stations owed by us
            if (s.Tile.Unit != null) return false; //only empty tiles
            if (player.Money < unitType.Cost) return false;

            //make sure we have the needed tech for the unit
            if (!unitType.Prerequisites.All(pr => player.Tech.Contains(pr)))
                return false;

            return true;
        }

        public void Build(UnitType unitType, Station s)
        {
            EventHandler.Push(
                new BuildUnitEvent(
                    unitType.Type,
                    We,
                    s.Position.X,
                    s.Position.Y
                )
            );
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
                    Log.Add(message.Get<string>("message"));
                    break;

                case NM3MessageType.world_create:
                    World = new fbWorld(
                        this,
                        message.Get<int>("width"),
                        message.Get<int>("height")
                    );
                    break;

                case NM3MessageType.station_create:
                    World.SpawnStation(
                        message.Get<int>("owner"),
                        message.Get<int>("id"),
                        message.Get<int>("x"),
                        message.Get<int>("y")
                    );
                    break;

                case NM3MessageType.station_set_project:
                    Station s = World.Stations[message.Get<int>("station-id")];

                    ProjectType type =
                        (ProjectType)message.Get<int>("projecttype");

                    switch (type)
                    {
                        case ProjectType.UnitProject:
                            s.Project = new BuildingProject(
                                this,
                                message.Get<int>("owner"),
                                s,
                                message.Get<int>("length"),
                                UnitType.GetType(
                                    (UnitTypes)
                                    message.Get<int>("project")
                                )
                            );
                            break;

                        case ProjectType.TechProject:
                            s.Project = new TechProject(
                                this,
                                message.Get<int>("owner"),
                                s,
                                message.Get<int>("length"),
                                (TechID)message.Get<int>("project")
                            );
                            break;

                        default:
                            throw new ArgumentException();
                    }

                    break;

                case NM3MessageType.player_add_tech:
                    World.Players[message.Get<int>("id")]
                        .Tech.Add((TechID)message.Get<int>("tech"));
                    break;

                case NM3MessageType.planet_create:
                    World.SpawnPlanet(
                        message.Get<int>("x"),
                        message.Get<int>("y")
                    );
                    break;

                case NM3MessageType.unit_create:
                    EventHandler.Push(
                        new CreateUnitEvent(
                            UnitType.GetType(
                                (UnitTypes)
                                message.Get<int>("type")
                            ),
                            message.Get<int>("owner"),
                            message.Get<int>("id"),
                            message.Get<int>("x"),
                            message.Get<int>("y")
                        )
                    );
                    break;

                case NM3MessageType.unit_move:
                    EventHandler.Push(
                        new UnitMoveEvent(
                            message.Get<int>("id"),
                            message.Get<int>("x"),
                            message.Get<int>("y"),
                            false
                        )
                    );
                    break;

                case NM3MessageType.player_new:
                    World.AddPlayer(
                        new Player(
                            "Unnamed player",
                            message.Get<int>("id"),
                            Color.White
                        )
                    );
                    break;

                case NM3MessageType.player_assign_id:
                    We = message.Get<int>("id");
                    break;

                case NM3MessageType.player_name:
                    EventHandler.Push(
                        new NameEvent(
                            message.Get<int>("id"),
                            message.Get<string>("name"),
                            message.Get<string>("color")
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
                    Unit u = World.Units[message.Get<int>("id")];
                    u.Moves = message.Get<int>("moves");
                    u.Attacks = message.Get<int>("attacks");
                    u.Strength = message.Get<int>("strength");

                    //todo: should probably be an event
                    if (u.Strength <= 0)
                        World.DespawnUnit(u);
                    break;

                case NM3MessageType.player_status:
                    World.GetPlayer(message.Get<int>("id"))
                        .Money = message.Get<int>("money");
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
