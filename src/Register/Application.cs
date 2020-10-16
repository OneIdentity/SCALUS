using Microsoft.VisualBasic.CompilerServices;
using Sulu.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sulu.Register
{
    class Application : IApplication
    {
        Options Options { get; }

        IUserInteraction UserInteraction { get; }

        IProtocolRegistrar Registrar { get; }

        public Application(Options options, IUserInteraction userInteraction, IProtocolRegistrar registrar)
        {
            Options = options;
            UserInteraction = userInteraction;
            Registrar = registrar;
        }

        public int Run()
        {
            foreach (var protocol in Options.Protocols)
            {
                var command = Registrar.GetRegisteredCommand(protocol);
                if (!string.IsNullOrEmpty(command) && !Registrar.IsSuluRegistered(protocol) && !Options.Force)
                {
                    UserInteraction.Error($"{protocol}: Protocol is already registered by another application ({command}). Use -f to overwrite.");
                    return 1;
                }

                if (!Registrar.Unregister(protocol))
                {
                    UserInteraction.Error($"{protocol}: Unable to remove existing protocol registration. Try running this program again with administrator privileges.");
                    return 1;
                }

                if (Registrar.Register(protocol))
                {
                    UserInteraction.Message($"{protocol}: Successfully registered Sulu as the default protocol handler.");
                }
                else
                {
                    UserInteraction.Error($"{protocol}: Failed to register Sulu as the default protocol handler. Try running this program again with administrator privileges.");
                    return 1;
                }
            }
            return 0;
        }
    }
}
