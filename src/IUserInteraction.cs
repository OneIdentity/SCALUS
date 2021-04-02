namespace scalus
{
    interface IUserInteraction
    {
        void Message(string message);
        void Error(string error);
    }
}
