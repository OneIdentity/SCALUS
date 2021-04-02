using scalus.Dto;

namespace scalus
{
    interface IProtocolHandlerFactory
    {
        public IProtocolHandler Create(string uri, ApplicationConfig config);
    }
}
