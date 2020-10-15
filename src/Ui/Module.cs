using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sulu.Ui
{
    class Module : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Sulu.Ui.Options>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<Sulu.Ui.Application>().Named<IApplication>("ui");
        }
    }
}
