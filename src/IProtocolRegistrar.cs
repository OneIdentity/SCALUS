namespace Sulu
{
    interface IProtocolRegistrar
    {
        string GetRegisteredCommand(string protocol);
        bool IsSuluRegistered(string command);
        bool Unregister(string protocol);
        bool Register(string protocol);
    }
}
