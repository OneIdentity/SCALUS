using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace scalus.Util
{
    public static class RegistryUtils
    {
        private static RegistryKey EnsureWritable(RegistryKey key)
        {
            var keyName = key.Name;
            return GetPrefix(ref keyName).OpenSubKey(keyName, true);
        }

        private static RegistryKey EnsurePath(string path)
        {
            try
            {
                var curKey = GetPrefix(ref path);
                var parts = path.Split('\\');

                foreach (var part in parts)
                {
                    var key = curKey;
                    try
                    {
                        curKey = key.CreateSubKey(part); // Try to create/open read/write
                    }
                    catch (UnauthorizedAccessException)
                    {
                        curKey = key.OpenSubKey(part); // Try to open readonly
                    }
                    if (curKey == null) return null;
                }
                return EnsureWritable(curKey); // Explicitly reopen with write access
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static RegistryKey GetKey(string path)
        {
            ValidateNotNullOrWhiteSpace(path, nameof(path));
            var regKey = GetPrefix(ref path);
            regKey = regKey?.OpenSubKey(path, true);

            return regKey;
        }

        public static void DeleteValue(string path, string name)
        {
            var key = GetKey(path);
            if (key == null) return;
            if (key.GetValue(name) == null) return;
            key.DeleteValue(name);
        }

        public static IEnumerable<string> GetValueNames(string path)
        {
            var key = GetKey(path);
            if (key == null) return Enumerable.Empty<string>();
            return key.GetValueNames();
        }

        public static bool DeleteKey(string path)
        {
            ValidateNotNullOrWhiteSpace(path, nameof(path));
            var regKey = GetPrefix(ref path);
            var parts = path.Split('\\');
            for (var i = 0; i < parts.Length - 1; i++)
            {
                regKey = regKey.OpenSubKey(parts[i], true);
                if (regKey == null)
                {
                    return false;
                }
            }
            try
            {
                regKey.DeleteSubKeyTree(parts[parts.Length - 1]);
            }
            catch(System.ArgumentException ex)
            {
                Serilog.Log.Error($"Error deleting key {path}: {ex.Message}");
                return false;
            }

            return true;
        }

        public static bool PathExists(string path)
        {
            ValidateNotNullOrWhiteSpace(path, nameof(path));
            var regKey = GetPrefix(ref path);
            regKey = regKey?.OpenSubKey(path);

            return regKey != null;
        }

        public static string GetStringValue(string path, string key)
        {
            ValidateNotNullOrWhiteSpace(path, nameof(path));
            ValidateNotNull(key, nameof(key));

            var regKey = GetPrefix(ref path);
            regKey = regKey?.OpenSubKey(path);

            var value = regKey?.GetValue(key);

            return value as string;
        }

        public static string[] GetStringArray(string path, string key)
        {
            ValidateNotNullOrWhiteSpace(path, nameof(path));
            ValidateNotNull(key, nameof(key));

            var regKey = GetPrefix(ref path);
            regKey = regKey?.OpenSubKey(path);

            var value = regKey?.GetValue(key);

            if (value is string[])
            {
                return value as string[];
            }
            return new[] {value?.ToString()};
        }

        public static DateTime? GetStringValueAsDateTime(string path, string key)
        {
            var stringValue = GetStringValue(path, key);
            if (string.IsNullOrEmpty(stringValue)) return null;

            DateTime dateTimeValue;
            if (DateTime.TryParse(stringValue, out dateTimeValue))
            {
                return dateTimeValue;
            }
            return null;
        }

        public static bool GetBoolValue(string path, string key)
        {
            ValidateNotNullOrWhiteSpace(path, nameof(path));
            ValidateNotNull(key, nameof(key));

            var regKey = GetPrefix(ref path);
            regKey = regKey?.OpenSubKey(path);

            var value = regKey?.GetValue(key);

            return Convert.ToBoolean(value);
        }

        public static int GetIntValue(string path, string key)
        {
            ValidateNotNullOrWhiteSpace(path, nameof(path));
            ValidateNotNull(key, nameof(key));

            var regKey = GetPrefix(ref path);
            regKey = regKey?.OpenSubKey(path);

            var value = regKey?.GetValue(key);

            return Convert.ToInt32(value);
        }

        public static bool SetValue<T>(string path, string key, T value, RegistryValueKind? kind = null)
        {
            ValidateNotNullOrWhiteSpace(path, nameof(path));
            ValidateNotNull(key, nameof(key));

            var regKey = EnsurePath(path);
            if (regKey == null)
            {
                return false;
            }

            if (kind == null)
            {
                regKey.SetValue(key, value);
            }
            else
            {
                regKey.SetValue(key, value, kind.Value);
            }

            return true;
        }

        private static RegistryKey GetPrefix(ref string path)
        {
            if (path.StartsWith("HKEY_LOCAL_MACHINE\\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(19);
                return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            }
            if (path.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(5);
                return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            }
            if (path.StartsWith("HKEY_CLASSES_ROOT\\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(18);
                return Registry.ClassesRoot;
            }
            if (path.StartsWith("HKCR\\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(5);
                return Registry.ClassesRoot;
            }
            if (path.StartsWith("HKEY_CURRENT_USER\\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(18);
                return Registry.CurrentUser;
            }
            if (path.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(5);
                return Registry.CurrentUser;
            }
            if (path.StartsWith("HKEY_USERS\\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(11);
                return Registry.Users;
            }
            if (path.StartsWith("HKU\\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(4);
                return Registry.Users;
            }
            if (path.StartsWith("HKEY_CURRENT_CONFIG\\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(20);
                return Registry.CurrentConfig;
            }
            if (path.StartsWith("HKCC\\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(5);
                return Registry.CurrentConfig;
            }
            return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        }

        private static string ValidateNotNullOrWhiteSpace(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(name);
            }
            return value;
        }

        private static string ValidateNotNull(string value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }
            return value;
        }
    }
}
