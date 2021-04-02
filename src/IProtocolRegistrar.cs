namespace scalus
{
    interface IProtocolRegistrar
    {
        string GetRegisteredCommand(string protocol);
        bool IsScalusRegistered(string command);
        bool Unregister(string protocol);
        bool Register(string protocol);
    }
}
