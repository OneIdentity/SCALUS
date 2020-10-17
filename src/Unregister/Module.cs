using Autofac;

namespace Sulu.Unregister
{
    class Module : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Sulu.Unregister.Options>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<Sulu.Unregister.Application>().Named<IApplication>("unregister").SingleInstance();
        }
    }
}
