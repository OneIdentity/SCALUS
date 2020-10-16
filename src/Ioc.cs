using Autofac;
using Sulu.Platform;
using Sulu.Util;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Sulu
{
    public static class Ioc
    {
        public static IContainer RegisterApplication()
        {
            var builder = new ContainerBuilder();

            // Standard type registrations
            builder.RegisterType<ApplicationBuilder>().As<IApplicationBuilder>().SingleInstance();
            builder.RegisterType<UserInteraction>().AsImplementedInterfaces().SingleInstance();

            // Perform platform-specific registrations here
            builder.RegisterPlatformSpecificComponents();

            // Register everything that derives from Autofac.Module
            builder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());
            return builder.Build();
        }

        private static void RegisterPlatformSpecificComponents(this ContainerBuilder builder)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                builder.RegisterWindowsComponents();
            }

            // Register the "unsupported platform" components with preserve existing defaults, so they only
            // take effect if nothing else registered to implement the service
            builder.RegisterType<UnsupportedPlatformRegistrar>().AsImplementedInterfaces().SingleInstance().PreserveExistingDefaults();
        }

        private static void RegisterWindowsComponents(this ContainerBuilder builder)
        {
            builder.RegisterType<WindowsProtocolRegistrar>().AsImplementedInterfaces().SingleInstance();
        }
    }
}
