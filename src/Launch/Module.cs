using Autofac;

namespace scalus.Launch
{
    class Module : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<scalus.Launch.Options>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<scalus.Launch.Application>().Named<IApplication>("launch").SingleInstance();
        }
    }
}
