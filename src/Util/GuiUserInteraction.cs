// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GuiUserInteraction.cs" company="One Identity Inc.">
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
namespace OneIdentity.Scalus.Util
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Windows;
    using OneIdentity.Scalus.Platform.Windows;

    internal class GuiUserInteraction : WindowHost<Messages>, IUserInteraction
    {
        public GuiUserInteraction()
        {
        }

        protected override bool AutoClose { get => StartupCache.Count == 0; }

        private List<string> StartupCache { get; } = new List<string>();

        private bool Silent { get; set; }

        public void Error(string error)
        {
            SetString("ERROR: " + error);
        }

        public void Message(string message)
        {
            SetString(message);
        }

        public void Silence()
        {
            Silent = true;
        }

        protected override void OnStarted()
        {
            foreach (var item in StartupCache)
            {
                SetString(item);
            }

            Window.Activate();
        }

        private void SetString(string message)
        {
            if (Silent)
            {
                return;
            }

            if (IsRunning)
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    if (Window != null)
                    {
                        Window.TextBox.Text += message + Environment.NewLine;
                    }
                });
            }
            else
            {
                StartWindowing();
                StartupCache.Add(message);
            }
        }
    }
}
#endif