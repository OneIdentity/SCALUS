using Autofac;

namespace scalus.Unregister
{
    class Module : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<scalus.Unregister.Options>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<scalus.Unregister.Application>().Named<IApplication>("unregister").SingleInstance();
        }
    }
}
