// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Ioc.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
//
//   ONE IDENTITY LLC. MAKES NO REPRESENTATIONS OR
//   WARRANTIES ABOUT THE SUITABILITY OF THE SOFTWARE,
//   EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//   TO THE IMPLIED WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE, OR
//   NON-INFRINGEMENT.  ONE IDENTITY LLC. SHALL NOT BE
//   LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE
//   AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
//   THIS SOFTWARE OR ITS DERIVATIVES.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Autofac;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;

    public static class Ioc
    {
        public static IContainer RegisterApplication(Serilog.ILogger logger)
        {
            var builder = new ContainerBuilder();

            // Standard type registrations
            builder.RegisterInstance(logger).As<Serilog.ILogger>().SingleInstance();
            builder.RegisterType<CommandLineHandler>().As<ICommandLineParser>().SingleInstance();
            builder.RegisterType<Registration>().As<IRegistration>().SingleInstance();
            builder.RegisterType<ScalusConfiguration>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ProtocolHandlerFactory>().AsImplementedInterfaces().SingleInstance();

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
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                builder.RegisterLinuxComponents();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                builder.RegisterOsxComponents();
            }
            else
            {
                // Register the "unsupported platform" components with preserve existing defaults, so they only
                // take effect if nothing else registered to implement the service
                builder.RegisterType<UnsupportedPlatformRegistrar>().AsImplementedInterfaces().SingleInstance().PreserveExistingDefaults();
            }

            builder.RegisterType<UserInteraction>().AsImplementedInterfaces().SingleInstance().PreserveExistingDefaults();
            builder.RegisterType<OsServicesBase>().AsImplementedInterfaces().SingleInstance().PreserveExistingDefaults();
        }

        private static void RegisterWindowsComponents(this ContainerBuilder builder)
        {
            //builder.RegisterType<GuiUserInteraction>().AsImplementedInterfaces().SingleInstance();
            if (OperatingSystem.IsWindows())
            {
                builder.RegisterType<WindowsBasicProtocolRegistrar>().AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<WindowsProtocolRegistrar>().AsImplementedInterfaces().SingleInstance();
            }
        }

        private static void RegisterLinuxComponents(this ContainerBuilder builder)
        {
            builder.RegisterType<UnixProtocolRegistrar>().AsImplementedInterfaces().SingleInstance();
        }

        private static void RegisterOsxComponents(this ContainerBuilder builder)
        {
            builder.RegisterType<MacOSProtocolRegistrar>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MacOSUserDefaultRegistrar>().AsImplementedInterfaces().SingleInstance();
        }
    }
}
