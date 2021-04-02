using scalus.Dto;
using System;

namespace scalus
{
    interface IProtocolHandler : IDisposable
    {
        void Run();
    }

    interface IScalusConfiguration
    {
        IProtocolHandler GetProtocolHandler(string uri);

        ScalusConfig GetConfiguration();
    }

    public interface IScalusApiConfiguration
    {
        ScalusConfig GetConfiguration();

        ScalusConfig SaveConfiguration(ScalusConfig configuration);
    }


}
