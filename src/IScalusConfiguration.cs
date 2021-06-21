using scalus.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace scalus
{
    interface IProtocolHandler : IDisposable
    {
        void Run(bool preview = false);
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
