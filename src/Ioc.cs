using Autofac;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sulu
{
    public static class Ioc
    {
        public static IContainer RegisterApplication()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ApplicationBuilder>().As<IApplicationBuilder>().SingleInstance();

            builder.RegisterModule<Launch.Module>();
            builder.RegisterModule<Ui.Module>();
            builder.RegisterModule<Register.Module>();
            builder.RegisterModule<Unregister.Module>();

            return builder.Build();
        }
    }
}
