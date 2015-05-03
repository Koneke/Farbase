using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum NM3MessageType
    {
        dev_command,
        message,
        create_world,
        create_station,
        create_planet,
        create_unit,
        move_unit,
        set_unit_moves,
        new_player,
        assign_id,
        name_player,
        current_player,
        client_ready,
        client_unready,
        pass_turn,
        attack,
        hurt,
        build_unit,
        player_set_money,
        player_set_diplo,
        station_set_loyalty,
        station_buy_loyalty,
        replenish_player
    }

    public class NM3Sig
    {
        public static Dictionary<NM3MessageType, NM3Sig> sigs =
            new Dictionary<NM3MessageType, NM3Sig>();

        public static NM3Sig Get(NM3MessageType messageType) {
            return sigs[messageType];
        }

        public readonly NM3MessageType MessageType;
        public List<Type> ArgumentTypes;
        public List<string> Arguments;

        public NM3Sig(NM3MessageType messageType)
        {
            MessageType = messageType;

            if (sigs.ContainsKey(MessageType))
                throw new Exception();

            ArgumentTypes = new List<Type>();
            Arguments = new List<string>();

            //auto register
            sigs.Add(MessageType, this);
        }

        public NM3Sig AddArgument<T>(string name)
        {
            if(Arguments.Contains(name))
                throw new ArgumentException();

            ArgumentTypes.Add(typeof(T));
            Arguments.Add(name);

            return this;
        }
    }

    public class NetMessage3
    {
        private static Dictionary<string, NM3MessageType> fromString;
        private static Dictionary<NM3MessageType, string> toString;

        public static NM3MessageType FromString(string s)
        {
            return fromString[s.ToLower()];
        }

        private static void registerTypeName(
            string name,
            NM3MessageType type
        ) {
            fromString.Add(name, type);
            toString.Add(type, name);
        }

        public static void Setup()
        {
            fromString = new Dictionary<string, NM3MessageType>();
            toString = new Dictionary<NM3MessageType, string>();

            SetupTypeNames();
            SetupSignatures();

            if (fromString.Count != NM3Sig.sigs.Count)
                throw new Exception();
        }

        private static void SetupTypeNames()
        {
            registerTypeName(
                "msg",
                NM3MessageType.message
            );
            registerTypeName(
                "create-world",
                NM3MessageType.create_world
            );
            registerTypeName(
                "create-station",
                NM3MessageType.create_station
            );
            registerTypeName(
                "create-planet",
                NM3MessageType.create_planet
            );
            registerTypeName(
                "create-unit",
                NM3MessageType.create_unit
            );
            registerTypeName(
                "move",
                NM3MessageType.move_unit
            );
            registerTypeName(
                "set-moves",
                NM3MessageType.set_unit_moves
            );
            registerTypeName(
                "new-player",
                NM3MessageType.new_player
            );
            registerTypeName(
                "replenish",
                NM3MessageType.replenish_player
            );
            registerTypeName(
                "assign-id",
                NM3MessageType.assign_id
            );
            registerTypeName(
                "name",
                NM3MessageType.name_player
            );
            registerTypeName(
                "current-player",
                NM3MessageType.current_player
            );
            registerTypeName(
                "ready",
                NM3MessageType.client_ready
            );
            registerTypeName(
                "unready",
                NM3MessageType.client_unready
            );
            registerTypeName(
                "pass",
                NM3MessageType.pass_turn
            );
            registerTypeName(
                "dev",
                NM3MessageType.dev_command
            );
            registerTypeName(
                "attack",
                NM3MessageType.attack
            );
            registerTypeName(
                "hurt",
                NM3MessageType.hurt
            );
            registerTypeName(
                "build-unit",
                NM3MessageType.build_unit
            );
            registerTypeName(
                "set-money",
                NM3MessageType.player_set_money
            );
            registerTypeName(
                "set-diplo",
                NM3MessageType.player_set_diplo
            );
            registerTypeName(
                "station-set-loyalty",
                NM3MessageType.station_set_loyalty
            );
            registerTypeName(
                "station-buy-loyalty",
                NM3MessageType.station_buy_loyalty
            );
        }

        private static void SetupSignatures()
        {
            new NM3Sig(NM3MessageType.message)
                .AddArgument<string>("message")
            ;

            new NM3Sig(NM3MessageType.create_world)
                .AddArgument<int>("width")
                .AddArgument<int>("height")
            ;

            new NM3Sig(NM3MessageType.create_station)
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            new NM3Sig(NM3MessageType.create_planet)
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            new NM3Sig(NM3MessageType.create_unit)
                .AddArgument<string>("type")
                .AddArgument<int>("owner")
                .AddArgument<int>("id")
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            new NM3Sig(NM3MessageType.move_unit)
                .AddArgument<int>("id")
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            new NM3Sig(NM3MessageType.set_unit_moves)
                .AddArgument<int>("id")
                .AddArgument<int>("amount")
            ;

            new NM3Sig(NM3MessageType.new_player)
                .AddArgument<int>("id")
            ;

            //todo: can probably be replaced by using pass from server to client
            new NM3Sig(NM3MessageType.replenish_player)
                .AddArgument<int>("id")
            ;

            new NM3Sig(NM3MessageType.assign_id)
                .AddArgument<int>("id")
            ;

            new NM3Sig(NM3MessageType.name_player)
                .AddArgument<int>("id")
                .AddArgument<string>("name")
                .AddArgument<string>("color")
            ;

            //should probably be id and not index
            new NM3Sig(NM3MessageType.current_player)
                .AddArgument<int>("index")
            ;

            new NM3Sig(NM3MessageType.client_ready)
            ;

            new NM3Sig(NM3MessageType.client_unready)
            ;

            new NM3Sig(NM3MessageType.pass_turn)
            ;

            new NM3Sig(NM3MessageType.dev_command)
                .AddArgument<int>("number")
            ;

            new NM3Sig(NM3MessageType.attack)
                .AddArgument<int>("attackerid")
                .AddArgument<int>("targetid")
            ;

            new NM3Sig(NM3MessageType.hurt)
                .AddArgument<int>("id")
                .AddArgument<int>("amount")
            ;

            new NM3Sig(NM3MessageType.build_unit)
                .AddArgument<string>("type")
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            new NM3Sig(NM3MessageType.player_set_money)
                .AddArgument<int>("id")
                .AddArgument<int>("amount")
            ;

            new NM3Sig(NM3MessageType.player_set_diplo)
                .AddArgument<int>("id")
                .AddArgument<int>("amount")
            ;

            new NM3Sig(NM3MessageType.station_set_loyalty)
                .AddArgument<int>("id")
                .AddArgument<int>("station-x")
                .AddArgument<int>("station-y")
                .AddArgument<int>("amount")
            ;

            new NM3Sig(NM3MessageType.station_buy_loyalty)
                .AddArgument<int>("id")
                .AddArgument<int>("station-x")
                .AddArgument<int>("station-y")
            ;
        }

        // ----------------------------------------------- //

        public NM3Sig Signature;
        private List<object> messageArguments;
        public int Sender;

        public object Get(string key)
        {
            return messageArguments
                [Signature.Arguments.IndexOf(key.ToLower())];
        }

        public T Get<T>(string key)
        {
            return (T)Get(key);
        }

        //setup the netmessage from a formatted string,
        //usually received from the server.
        //e.g. "move-unit:0,10,10"
        //called from the NetMessage3(string) ctor.
        private void setup(string formatted)
        {
            string command;
            List<string> arguments;
            if (formatted.Contains(':'))
            {
                command = formatted.Split(':')[0];
                arguments =
                    formatted
                        .Split(':')[1]
                        .Split(',')
                        .ToList();
            }
            else
            {
                command = formatted;
                arguments = new List<string>();
            }

            arguments.RemoveAll(s => s == "");

            Signature = NM3Sig.Get(FromString(command));

            if (Signature.Arguments.Count != arguments.Count)
                throw new ArgumentException();

            messageArguments = new List<object>();

            var handlers = new Dictionary<Type, Func<string, object>>
            {
                { typeof(int), s => Int32.Parse(s) },
                { typeof(string), s => s }
            };

            for (int i = 0; i < arguments.Count; i++)
            {
                messageArguments.Add(
                    handlers
                        [Signature.ArgumentTypes[i]]
                        (arguments[i])
                );
            }
        }

        private void setup(NM3Sig signature, List<object> arguments)
        {
            Signature = signature;

            if (Signature.Arguments.Count != arguments.Count)
                throw new ArgumentException();

            messageArguments = new List<object>();
            messageArguments.AddRange(arguments);
        }

        public NetMessage3(string formatted) { setup(formatted); }

        public NetMessage3(
            NM3MessageType messageType,
            params object[] param
        ) {
            NM3Sig signature = NM3Sig.Get(messageType);
            setup(
                signature,
                new List<object>(param)
            );
        }

        //return a formatted string of the message, used for sending the
        //message to the server/client.
        public string Format()
        {
            return string.Format(
                "{0}:{1}",
                toString[Signature.MessageType],
                string.Join(",", messageArguments)
            );
        }
    }

    public abstract class fbNetMessage
    {
        public int Sender;
        public bool Trash;

        protected fbNetMessage()
        {
            Sender = -1; //no explicit sender/it's the server
            Trash = false;
        }

        public abstract string GetMessageType();
        public abstract int GetExpectedArguments();

        public abstract string Format();

        public static fbNetMessage Spawn(
            string command,
            List<string> arguments
        ) {
            fbNetMessage message;

            switch (command)
            {
                case MsgMessage.Command:
                    message = new MsgMessage(arguments);
                    break;

                case CreateWorldMessage.Command:
                    message = new CreateWorldMessage(arguments);
                    break;

                case CreateStationMessage.Command:
                    message = new CreateStationMessage(arguments);
                    break;

                case CreatePlanetMessage.Command:
                    message = new CreatePlanetMessage(arguments);
                    break;

                case CreateUnitMessage.Command:
                    message = new CreateUnitMessage(arguments);
                    break;

                case MoveUnitMessage.Command:
                    message = new MoveUnitMessage(arguments);
                    break;

                case SetUnitMovesMessage.Command:
                    message = new SetUnitMovesMessage(arguments);
                    break;

                case NewPlayerMessage.Command:
                    message = new NewPlayerMessage(arguments);
                    break;

                case ReplenishPlayerMessage.Command:
                    message = new ReplenishPlayerMessage(arguments);
                    break;

                case AssignIDMessage.Command:
                    message = new AssignIDMessage(arguments);
                    break;

                case NameMessage.Command:
                    message = new NameMessage(arguments);
                    break;

                case CurrentPlayerMessage.Command:
                    message = new CurrentPlayerMessage(arguments);
                    break;

                case UnreadyMessage.Command:
                    message = new UnreadyMessage();
                    break;

                case ReadyMessage.Command:
                    message = new ReadyMessage();
                    break;

                case PassMessage.Command:
                    message = new PassMessage();
                    break;

                case DevCommandMessage.Command:
                    message = new DevCommandMessage(arguments);
                    break;

                case AttackMessage.Command:
                    message = new AttackMessage(arguments);
                    break;

                case HurtMessage.Command:
                    message = new HurtMessage(arguments);
                    break;

                case BuildUnitMessage.Command:
                    message = new BuildUnitMessage(arguments);
                    break;

                case SetMoneyMessage.Command:
                    message = new SetMoneyMessage(arguments);
                    break;

                case SetDiploMessage.Command:
                    message = new SetDiploMessage(arguments);
                    break;

                case PurchaseStationLoyaltyMessage.Command:
                    message = new PurchaseStationLoyaltyMessage(arguments);
                    break;

                default:
                    string argline = "";
                    if (arguments.Count > 0)
                        argline += arguments.Aggregate((c, n) => c + "," + n);

                    string msg =
                        string.Format(
                            "Received unknown command {0}:{1}.",
                            command,
                            argline
                        );

                    arguments = new List<string> { msg };
                    message = new MsgMessage(arguments);

                    message.Trash = true;
                    break;
            }

            if (arguments.Count != message.GetExpectedArguments())
                throw new ArgumentException();

            return message;
        }
    }

    public class MsgMessage : fbNetMessage
    {
        public const string Command = "msg";
        public string Content;

        public override int GetExpectedArguments() { return 1; }
        public override string GetMessageType() { return Command; }

        public MsgMessage(List<string> arguments)
        {
            Content = arguments[0];
        }

        public MsgMessage(string text)
        {
            Content = text;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                Content
            );
        }
    }

    public class CreateWorldMessage : fbNetMessage
    {
        public const string Command = "create-world";
        public override int GetExpectedArguments() { return 2; }
        public override string GetMessageType() { return Command; }

        public int w;
        public int h;

        public CreateWorldMessage(List<string> arguments)
        {
            w = Int32.Parse(arguments[0]);
            h = Int32.Parse(arguments[1]);
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2}",
                Command,
                w, h
            );
        }
    }

    public class CreateStationMessage : fbNetMessage
    {
        public const string Command = "create-station";
        public override int GetExpectedArguments() { return 2; }
        public override string GetMessageType() { return Command; }

        public int x;
        public int y;

        public CreateStationMessage(List<string> arguments)
        {
            x = Int32.Parse(arguments[0]);
            y = Int32.Parse(arguments[1]);
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2}",
                Command,
                x, y
            );
        }
    }

    public class CreatePlanetMessage : fbNetMessage
    {
        public const string Command = "create-planet";
        public override int GetExpectedArguments() { return 2; }
        public override string GetMessageType() { return Command; }

        public int x;
        public int y;

        public CreatePlanetMessage(
            List<string> arguments
        ) {
            x = Int32.Parse(arguments[0]);
            y = Int32.Parse(arguments[1]);
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2}",
                Command,
                x, y
            );
        }
    }

    public class CreateUnitMessage : fbNetMessage
    {
        public const string Command = "create-unit";
        public override int GetExpectedArguments() { return 5; }
        public override string GetMessageType() { return Command; }

        public string type;
        public int id;
        public int owner;
        public int x;
        public int y;

        public CreateUnitMessage(
            List<string> arguments
        ) {
            type = arguments[0];
            owner = Int32.Parse(arguments[1]);
            id = Int32.Parse(arguments[2]);
            x = Int32.Parse(arguments[3]);
            y = Int32.Parse(arguments[4]);
        }

        public CreateUnitMessage(
            string type,
            int owner,
            int id,
            int x,
            int y
        ) {
            this.type = type;
            this.owner = owner;
            this.id = id;
            this.x = x;
            this.y = y;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2},{3},{4},{5}",
                Command,
                type,
                owner,
                id,
                x,
                y
            );
        }
    }

    public class MoveUnitMessage : fbNetMessage
    {
        public const string Command = "move";
        public override int GetExpectedArguments() { return 3; }
        public override string GetMessageType() { return Command; }

        public int id;
        public int x;
        public int y;

        public MoveUnitMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
            x = Int32.Parse(arguments[1]);
            y = Int32.Parse(arguments[2]);
        }

        public MoveUnitMessage(
            int id,
            int x,
            int y
        ) {
            this.id = id;
            this.x = x;
            this.y = y;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2},{3}",
                Command,
                id, x, y
            );
        }
    }

    public class SetUnitMovesMessage : fbNetMessage
    {
        public const string Command = "set-moves";
        public override int GetExpectedArguments() { return 2; }
        public override string GetMessageType() { return Command; }

        public int id;
        public int amount;

        public SetUnitMovesMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
            amount = Int32.Parse(arguments[1]);
        }

        public SetUnitMovesMessage(
            int id,
            int amount
        ) {
            this.id = id;
            this.amount = amount;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2}",
                Command,
                id, amount
            );
        }
    }

    public class NewPlayerMessage : fbNetMessage
    {
        public const string Command = "new-player";
        public override int GetExpectedArguments() { return 1; }
        public override string GetMessageType() { return Command; }

        public int id;

        public NewPlayerMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                id
            );
        }
    }

    public class ReplenishPlayerMessage : fbNetMessage
    {
        public const string Command = "replenish";
        public override int GetExpectedArguments() { return 1; }
        public override string GetMessageType() { return Command; }

        public int id;

        public ReplenishPlayerMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
        }

        public ReplenishPlayerMessage(
            int id
        ) {
            this.id = id;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                id
            );
        }
    }

    public class AssignIDMessage : fbNetMessage
    {
        public const string Command = "assign-id";
        public override int GetExpectedArguments() { return 1; }
        public override string GetMessageType() { return Command; }

        public int id;

        public AssignIDMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                id
            );
        }
    }

    public class NameMessage : fbNetMessage
    {
        public const string Command = "name";
        public override int GetExpectedArguments() { return 3; }
        public override string GetMessageType() { return Command; }

        public int id;
        public string name;
        public Color color;

        public NameMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
            name = arguments[1];
            color = ExtensionMethods.ColorFromString(arguments[2]);
        }

        public NameMessage(
            int id,
            string name,
            Color color
        ) {
            this.id = id;
            this.name = name;
            this.color = color;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2},{3}",
                Command,
                id,
                name,
                ExtensionMethods.ColorToString(color)
            );
        }
    }

    public class CurrentPlayerMessage : fbNetMessage
    {
        public const string Command = "current-player";
        public override int GetExpectedArguments() { return 1; }
        public override string GetMessageType() { return Command; }

        public int index;

        public CurrentPlayerMessage(
            List<string> arguments
        ) {
            index = Int32.Parse(arguments[0]);
        }

        public CurrentPlayerMessage(
            int index
        ) {
            this.index = index;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                index
            );
        }
    }

    public class ReadyMessage : fbNetMessage
    {
        public const string Command = "ready";
        public override int GetExpectedArguments() { return 0; }
        public override string GetMessageType() { return Command; }

        public override string Format()
        {
            return string.Format(
                "{0}",
                Command
            );
        }
    }

    public class UnreadyMessage : fbNetMessage
    {
        public const string Command = "unready";
        public override int GetExpectedArguments() { return 0; }
        public override string GetMessageType() { return Command; }

        public override string Format()
        {
            return string.Format(
                "{0}",
                Command
            );
        }
    }

    public class PassMessage : fbNetMessage
    {
        public const string Command = "pass";
        public override string GetMessageType() { return Command; }
        public override int GetExpectedArguments() { return 0; }

        public override string Format()
        {
            return string.Format(
                "{0}",
                Command
            );
        }
    }

    public class DevCommandMessage : fbNetMessage
    {
        public const string Command = "dev";
        public override string GetMessageType() { return Command; }
        public override int GetExpectedArguments() { return 1; }

        public int Number;

        public DevCommandMessage(List<string> arguments)
        {
            Number = Int32.Parse(arguments[0]);
        }

        public DevCommandMessage(int number)
        {
            Number = number;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                Number
            );
        }
    }

    public class AttackMessage : fbNetMessage
    {
        public const string Command = "attack";
        public override string GetMessageType() { return Command; }
        public override int GetExpectedArguments() { return 2; }

        public int attackerid;
        public int targetid;

        public AttackMessage(List<string> arguments)
        {
            attackerid = Int32.Parse(arguments[0]);
            targetid = Int32.Parse(arguments[1]);
        }

        public AttackMessage(int aid, int tid)
        {
            attackerid = aid;
            targetid = tid;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2}",
                Command,
                attackerid,
                targetid
            );
        }
    }

    public class HurtMessage : fbNetMessage
    {
        public const string Command = "hurt";
        public override string GetMessageType() { return Command; }
        public override int GetExpectedArguments() { return 2; }

        public int id;
        public int amount;

        public HurtMessage(List<string> arguments)
        {
            id = Int32.Parse(arguments[0]);
            amount = Int32.Parse(arguments[1]);
        }

        public HurtMessage(int id, int amount)
        {
            this.id = id;
            this.amount = amount;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2}",
                Command,
                id,
                amount
            );
        }
    }

    public class BuildUnitMessage : fbNetMessage
    {
        //type, station-x, station-y

        public const string Command = "build-unit";
        public override string GetMessageType() { return Command; }
        public override int GetExpectedArguments() { return 3; }

        public string type;
        public int x, y;

        public BuildUnitMessage(
            List<string> arguments
        ) {
            type = arguments[0];
            x = Int32.Parse(arguments[1]);
            y = Int32.Parse(arguments[2]);
        }

        public BuildUnitMessage(
            string type,
            int x, int y
        ) {
            this.type = type;
            this.x = x;
            this.y = y;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2},{3}",
                Command,
                type,
                x, y
            );
        }
    }

    public class SetMoneyMessage : fbNetMessage
    {
        public const string Command = "set-money";
        public override string GetMessageType() { return Command; }
        public override int GetExpectedArguments() { return 2; }

        public int id, amount;

        public SetMoneyMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
            amount = Int32.Parse(arguments[1]);
        }

        public SetMoneyMessage(
            int id,
            int amount
        ) {
            this.id = id;
            this.amount = amount;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2}",
                Command,
                id,
                amount
            );
        }
    }

    public class SetDiploMessage : fbNetMessage
    {
        public const string Command = "set-diplo";
        public override string GetMessageType() { return Command; }
        public override int GetExpectedArguments() { return 2; }

        public int id;
        public int amount;

        public SetDiploMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
            amount = Int32.Parse(arguments[1]);
        }

        public SetDiploMessage(
            int id, int amount
        ) {
            this.id = id;
            this.amount = amount;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2}",
                Command,
                id,
                amount
            );
        }
    }

    public class SetStationLoyaltyMessage : fbNetMessage
    {
        public const string Command = "set-station-loyalty";
        public override string GetMessageType() { return Command; }

        public override int GetExpectedArguments()
        {
            //player id, station x, station y, amount
            return 4;
        }

        public int id, stationX, stationY, loyalty;

        public SetStationLoyaltyMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
            stationX = Int32.Parse(arguments[1]);
            stationY = Int32.Parse(arguments[2]);
            loyalty = Int32.Parse(arguments[3]);
        }

        public SetStationLoyaltyMessage(
            int id, int sx, int sy, int amount
        ) {
            this.id = id;
            stationX = sx;
            stationY = sy;
            loyalty = amount;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2},{3},{4}",
                Command,
                id,
                stationX,
                stationY,
                loyalty
            );
        }
    }

    public class PurchaseStationLoyaltyMessage : fbNetMessage
    {
        public const string Command = "purchase-loyalty";
        public override string GetMessageType() { return Command; }

        public override int GetExpectedArguments()
        {
            //player id, station x, station y;
            return 3;
        }

        public int id, stationX, stationY;

        public PurchaseStationLoyaltyMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
            stationX = Int32.Parse(arguments[1]);
            stationY = Int32.Parse(arguments[2]);
        }

        public PurchaseStationLoyaltyMessage(
            int id, int sx, int sy
        ) {
            this.id = id;
            stationX = sx;
            stationY = sy;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2},{3}",
                Command,
                id,
                stationX,
                stationY
            );
        }
    }
}
