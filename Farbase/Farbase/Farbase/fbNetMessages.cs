using System;
using System.Collections.Generic;
using System.Linq;

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

        private static NM3Sig SetupSignature(
            string command,
            NM3MessageType messageType
        ) {
            registerTypeName(command, messageType);
            return new NM3Sig(messageType);
        }

        public static void Setup()
        {
            fromString = new Dictionary<string, NM3MessageType>();
            toString = new Dictionary<NM3MessageType, string>();

            SetupSignatures();

            if (fromString.Count != NM3Sig.sigs.Count)
                throw new Exception();
        }

        private static void SetupSignatures()
        {
            SetupSignature(
                "msg",
                NM3MessageType.message
            )
                .AddArgument<string>("message")
            ;

            SetupSignature(
                "create-world",
                NM3MessageType.create_world
            )
                .AddArgument<int>("width")
                .AddArgument<int>("height")
            ;

            SetupSignature(
                "create-station",
                NM3MessageType.create_station
            )
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            SetupSignature(
                "create-planet",
                NM3MessageType.create_planet
            )
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            SetupSignature(
                "create-unit",
                NM3MessageType.create_unit
            )
                .AddArgument<string>("type")
                .AddArgument<int>("owner")
                .AddArgument<int>("id")
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            SetupSignature(
                "move",
                NM3MessageType.move_unit
            )
                .AddArgument<int>("id")
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            SetupSignature(
                "set-moves",
                NM3MessageType.set_unit_moves
            )
                .AddArgument<int>("id")
                .AddArgument<int>("amount")
            ;

            SetupSignature(
                "new-player",
                NM3MessageType.new_player
            )
                .AddArgument<int>("id")
            ;

            //todo: can probably be replaced by using pass from server to client
            SetupSignature(
                "replenish",
                NM3MessageType.replenish_player
            )
                .AddArgument<int>("id")
            ;

            SetupSignature(
                "assign-id",
                NM3MessageType.assign_id
            )
                .AddArgument<int>("id")
            ;

            SetupSignature(
                "name",
                NM3MessageType.name_player
            )
                .AddArgument<int>("id")
                .AddArgument<string>("name")
                .AddArgument<string>("color")
            ;

            SetupSignature(
                "current-player",
                NM3MessageType.current_player
            )
                .AddArgument<int>("index")
            ;

            SetupSignature(
                "ready",
                NM3MessageType.client_ready
            );

            SetupSignature(
                "unready",
                NM3MessageType.client_unready
            );

            SetupSignature(
                "pass",
                NM3MessageType.pass_turn
            );

            SetupSignature(
                "dev",
                NM3MessageType.dev_command
            )
                .AddArgument<int>("number")
            ;

            SetupSignature(
                "attack",
                NM3MessageType.attack
            )
                .AddArgument<int>("attackerid")
                .AddArgument<int>("targetid")
            ;

            SetupSignature(
                "hurt",
                NM3MessageType.hurt
            )
                .AddArgument<int>("id")
                .AddArgument<int>("amount")
            ;

            SetupSignature(
                "build-unit",
                NM3MessageType.build_unit
            )
                .AddArgument<string>("type")
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            SetupSignature(
                "set-money",
                NM3MessageType.player_set_money
            )
                .AddArgument<int>("id")
                .AddArgument<int>("amount")
            ;

            SetupSignature(
                "set-diplo",
                NM3MessageType.player_set_diplo
            )
                .AddArgument<int>("id")
                .AddArgument<int>("amount")
            ;

            SetupSignature(
                "station-set-loyalty",
                NM3MessageType.station_set_loyalty
            )
                .AddArgument<int>("id")
                .AddArgument<int>("station-x")
                .AddArgument<int>("station-y")
                .AddArgument<int>("amount")
            ;

            SetupSignature(
                "station-buy-loyalty",
                NM3MessageType.station_buy_loyalty
            )
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
}
