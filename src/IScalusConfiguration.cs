using scalus.Dto;
using System;
using System.Collections.Generic;

namespace scalus
{
    interface IProtocolHandler : IDisposable
    {
        void Run();
    }

    interface IScalusConfiguration
    {
        IProtocolHandler GetProtocolHandler(string uri);

        ScalusConfig GetConfiguration(string path = null);
        List<string> ValidationErrors { get; }
    }

    public interface IScalusApiConfiguration
    {
        ScalusConfig GetConfiguration();
        ScalusConfig SaveConfiguration(ScalusConfig configuration);
        List<string> ValidationErrors { get; }
    }


}
