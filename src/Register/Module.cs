using Autofac;

namespace scalus.Register
{
    class Module : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<scalus.Register.Options>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<scalus.Register.Application>().Named<IApplication>("register").SingleInstance();
        }
    }
}
