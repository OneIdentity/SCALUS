using Autofac;

namespace Sulu.Register
{
    class Module : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Sulu.Register.Options>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<Sulu.Register.Application>().Named<IApplication>("register").SingleInstance();
        }
    }
}
