using System;
using System.Collections.Generic;
using System.Linq;

namespace Farbase
{
    public enum NM3MessageType
    {
        dev_command,
        message,

        client_disconnect,
        client_ready,
        client_unready,
        client_pass,

        planet_create,

        player_add_tech,
        player_assign_id,
        player_current,
        player_name,
        player_new,
        player_status,

        station_build,
        station_create,
        station_set_project,

        unit_attack,
        unit_bombard,
        unit_build,
        unit_create,
        unit_move,
        unit_status,

        world_create,
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
            //=== === === === === === === === === ===//

            SetupSignature(
                "dev",
                NM3MessageType.dev_command
            )
                .AddArgument<int>("number")
            ;

            SetupSignature(
                "msg",
                NM3MessageType.message
            )
                .AddArgument<string>("message")
            ;

            //=== === === === === === === === === ===//

            SetupSignature(
                "disconnect",
                NM3MessageType.client_disconnect
            )
                .AddArgument<int>("id")
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
                NM3MessageType.client_pass
            );

            //=== === === === === === === === === ===//

            SetupSignature(
                "create-planet",
                NM3MessageType.planet_create
            )
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            //=== === === === === === === === === ===//

            SetupSignature(
                "player-add-tech",
                NM3MessageType.player_add_tech
            )
                .AddArgument<int>("id")
                .AddArgument<int>("tech") //int -> enum
            ;

            SetupSignature(
                "assign-id",
                NM3MessageType.player_assign_id
            )
                .AddArgument<int>("id")
            ;

            SetupSignature(
                "current-player",
                NM3MessageType.player_current
            )
                .AddArgument<int>("index")
            ;

            SetupSignature(
                "name",
                NM3MessageType.player_name
            )
                .AddArgument<int>("id")
                .AddArgument<string>("name")
                .AddArgument<string>("color")
            ;

            SetupSignature(
                "new-player",
                NM3MessageType.player_new
            )
                .AddArgument<int>("id")
            ;

            SetupSignature(
                "player-status",
                NM3MessageType.player_status
            )
                .AddArgument<int>("id")
                .AddArgument<int>("money")
            ;

            //=== === === === === === === === === ===//

            SetupSignature(
                "create-station",
                NM3MessageType.station_create
            )
                .AddArgument<int>("owner")
                .AddArgument<int>("id")
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            SetupSignature(
                "build-station",
                NM3MessageType.station_build
            )
                .AddArgument<int>("owner")
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            SetupSignature(
                "station-set-project",
                NM3MessageType.station_set_project
            )
                .AddArgument<int>("owner")
                .AddArgument<int>("station-id")
                .AddArgument<int>("length")
                .AddArgument<int>("projecttype")
                .AddArgument<int>("project")
            ;

            //=== === === === === === === === === ===//

            SetupSignature(
                "unit-attack",
                NM3MessageType.unit_attack
            )
                .AddArgument<int>("attackerid")
                .AddArgument<int>("targetid")
            ;

            SetupSignature(
                "unit-bombard",
                NM3MessageType.unit_bombard
            )
                .AddArgument<int>("attackerid")
                .AddArgument<int>("targetid")
            ;

            SetupSignature(
                "build-unit",
                NM3MessageType.unit_build
            )
                .AddArgument<int>("type")
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            SetupSignature(
                "create-unit",
                NM3MessageType.unit_create
            )
                .AddArgument<int>("type")
                .AddArgument<int>("owner")
                .AddArgument<int>("id")
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            SetupSignature(
                "move",
                NM3MessageType.unit_move
            )
                .AddArgument<int>("id")
                .AddArgument<int>("x")
                .AddArgument<int>("y")
            ;

            SetupSignature(
                "unit-status",
                NM3MessageType.unit_status
            )
                .AddArgument<int>("id")
                .AddArgument<int>("moves")
                .AddArgument<int>("attacks")
                .AddArgument<int>("strength")
            ;

            //=== === === === === === === === === ===//

            SetupSignature(
                "create-world",
                NM3MessageType.world_create
            )
                .AddArgument<int>("width")
                .AddArgument<int>("height")
            ;
        }

        // ----------------------------------------------- //

        public NM3Sig Signature;
        private List<object> messageArguments;
        public int Sender;

        public T Get<T>(string key)
        {
            return (T)messageArguments
                [Signature.Arguments.IndexOf(key.ToLower())];
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
                { typeof(string), s => s },
                { typeof(bool), s => bool.Parse(s) },
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
            if(messageArguments.Count > 0)
                return string.Format(
                    "{0}:{1};",
                    toString[Signature.MessageType],
                    string.Join(",", messageArguments)
                );

            return string.Format(
                "{0};",
                toString[Signature.MessageType]
            );
        }
    }
}
