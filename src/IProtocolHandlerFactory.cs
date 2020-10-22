using Sulu.Dto;

namespace Sulu
{
    interface IProtocolHandlerFactory
    {
        public IProtocolHandler Create(string uri, ApplicationConfig config);
    }
}
