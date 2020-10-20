using Sulu.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sulu
{
    interface IProtocolHandler : IDisposable
    {
        void Run();
    }

    interface ISuluConfiguration
    {
        IProtocolHandler GetProtocolHandler(string uri);
    }

    public interface ISuluApiConfiguration
    {
        SuluConfig GetConfiguration();

        SuluConfig SaveConfiguration(SuluConfig configuration);
    }


}
