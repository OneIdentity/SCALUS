using Autofac;

namespace Sulu.Ui
{
    class Module : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Sulu.Ui.Options>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<Sulu.Ui.Application>().Named<IApplication>("ui").SingleInstance();
        }
    }
}
