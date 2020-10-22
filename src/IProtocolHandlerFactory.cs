using Sulu.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sulu
{
    interface IProtocolHandlerFactory
    {
        public IProtocolHandler Create(string uri, ApplicationConfig config);
    }
}
