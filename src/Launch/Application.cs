namespace Sulu.Launch
{
    class Application : IApplication
    {
        Launch.Options Options { get; }
        public Application(Launch.Options options)
        {
            this.Options = options;
        }

        public int Run()
        {
            Serilog.Log.Debug($"Launch is not yet implemented: {Options.Url}");
            return 0;
        }
    }
}
