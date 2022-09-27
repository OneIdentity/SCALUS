// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowHost.cs" company="One Identity Inc.">
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

#if WPF
namespace OneIdentity.Scalus.Platform.Windows
{
    using System;
    using System.Threading;
    using System.Windows;

    public class WindowHost<TWindow> : IDisposable
        where TWindow : Window,  new()
    {
        private bool disposedValue;

        public WindowHost()
        {
            // create a thread
            NewWindowThread = new Thread(new ThreadStart(() =>
            {
                // create and show the window
                Window = new TWindow();
                Window.Show();

                // start the Dispatcher processing
                IsRunning = true;
                OnStarted();
                System.Windows.Threading.Dispatcher.Run();
            }));

            // set the apartment state
            NewWindowThread.SetApartmentState(ApartmentState.STA);

            // make the thread a background thread
            NewWindowThread.IsBackground = true;

            // start the thread
            NewWindowThread.Start();
        }

        public TWindow Window { get; set; }

        protected bool IsRunning { get; private set; }

        private Thread NewWindowThread { get; }

        private Action<TWindow> StartupCallback { get; set; }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void OnStartup(Action<TWindow> startupCallback)
        {
            StartupCallback = startupCallback;
        }

        protected virtual void OnStarted()
        {
            if (StartupCallback != null)
            {
                System.Windows.Threading.Dispatcher.FromThread(NewWindowThread)
                    .Invoke(StartupCallback, Window);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    var dispatcher = System.Windows.Threading.Dispatcher.FromThread(NewWindowThread);
                    dispatcher?.Invoke(() => Window.Close());

                    NewWindowThread.Join(TimeSpan.FromSeconds(2));
                }

                disposedValue = true;
            }
        }
    }
}
#endif