using Autofac;

namespace scalus.Ui
{
    class Module : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<scalus.Ui.Options>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<scalus.Ui.Application>().Named<IApplication>("ui").SingleInstance();
        }
    }
}
