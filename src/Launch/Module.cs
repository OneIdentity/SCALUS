using Autofac;

namespace Sulu.Launch
{
    class Module : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Sulu.Launch.Options>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<Sulu.Launch.Application>().Named<IApplication>("launch").SingleInstance();
        }
    }
}
