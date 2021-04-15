using scalus.Dto;
using System.Collections.Generic;

namespace scalus
{
    interface IProtocolHandlerFactory
    {
        public IProtocolHandler Create(string uri, ApplicationConfig config);
    }
}
