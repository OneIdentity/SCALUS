using Autofac;

namespace scalus.Unregister
{
    internal class Module : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Options>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<Application>().Named<IApplication>("unregister").SingleInstance();
        }
    }
}
