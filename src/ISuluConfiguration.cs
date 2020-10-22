using Sulu.Dto;
using System;

namespace Sulu
{
    interface IProtocolHandler : IDisposable
    {
        void Run();
    }

    interface ISuluConfiguration
    {
        IProtocolHandler GetProtocolHandler(string uri);

        SuluConfig GetConfiguration();
    }

    public interface ISuluApiConfiguration
    {
        SuluConfig GetConfiguration();

        SuluConfig SaveConfiguration(SuluConfig configuration);
    }


}
