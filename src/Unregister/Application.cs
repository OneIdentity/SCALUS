namespace scalus.Unregister
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
            return Registration.UnRegister(Options.Protocols, Options.UserMode, Options.UseSudo) ? 0 : 1;
        }
    }
}
