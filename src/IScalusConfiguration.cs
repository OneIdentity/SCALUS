using scalus.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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
        List<string> SaveConfiguration(ScalusConfig configuration);
        List<string> ValidationErrors { get; }
    }


}
