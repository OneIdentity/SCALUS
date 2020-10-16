using System;
using System.Collections.Generic;
using System.Text;

namespace Sulu.Unregister
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
                if (Registrar.IsSuluRegistered(protocol))
                {
                    if(!Registrar.Unregister(protocol))
                    {
                        UserInteraction.Error($"{protocol}: Unable to remove Sulu protocol registration. Try running this program again with administrator privileges.");
                        return 1;
                    }
                    UserInteraction.Error($"{protocol}: Unregistered Sulu as default URL protocol handler.");
                }
                else
                {
                    UserInteraction.Message($"{protocol}: Sulu does not appear to be registered for this protocol, skipping.");
                }
            }
            return 0;
        }
    }
}
