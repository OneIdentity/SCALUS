namespace Sulu.Unregister
{
    class Application : IApplication
    {
        Options Options { get; }
        IRegistration Registration{ get; }

        public Application(Options options, IRegistration registration)
        {
            Options = options;
            Registration = registration;
        }

        public int Run()
        {
            return Registration.UnRegister(Options.Protocols) ? 0 : 1;
        }
    }
}
