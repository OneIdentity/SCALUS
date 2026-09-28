// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UiWindowSettings.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Util
{
    using System;
    using System.IO;
    using System.Text.Json;
    using Serilog;

    internal sealed class UiWindowSettings
    {
        public const int DefaultWidth = 1240;
        public const int DefaultHeight = 840;
        public const int MinimumWidth = 960;
        public const int MinimumHeight = 640;
        public const int MaximumDimension = 16384;

        public int Width { get; set; } = DefaultWidth;

        public int Height { get; set; } = DefaultHeight;

        public bool Maximized { get; set; }

        public static UiWindowSettings Load(string path)
        {
            if (!File.Exists(path))
            {
                return new UiWindowSettings();
            }

            try
            {
                var settings = ScalusJson.DeserializeUiWindowSettings(File.ReadAllText(path));
                return Normalize(settings);
            }
            catch (JsonException ex)
            {
                Log.Warning(ex, "Could not read UI window settings from {Path}; using defaults", path);
            }
            catch (IOException ex)
            {
                Log.Warning(ex, "Could not read UI window settings from {Path}; using defaults", path);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warning(ex, "Could not read UI window settings from {Path}; using defaults", path);
            }

            return new UiWindowSettings();
        }

        public static void Save(string path, int width, int height, bool maximized)
        {
            var settings = Normalize(new UiWindowSettings
            {
                Width = width,
                Height = height,
                Maximized = maximized,
            });

            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, ScalusJson.Serialize(settings));
            }
            catch (IOException ex)
            {
                Log.Warning(ex, "Could not save UI window settings to {Path}", path);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warning(ex, "Could not save UI window settings to {Path}", path);
            }
        }

        private static UiWindowSettings Normalize(UiWindowSettings settings)
        {
            if (settings == null)
            {
                return new UiWindowSettings();
            }

            settings.Width = Math.Clamp(settings.Width, MinimumWidth, MaximumDimension);
            settings.Height = Math.Clamp(settings.Height, MinimumHeight, MaximumDimension);
            return settings;
        }
    }
}
