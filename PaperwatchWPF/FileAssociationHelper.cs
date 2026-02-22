using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PaperwatchWPF
{
    public static class FileAssociationHelper
    {
        public static void SetAsDefault(string extension, string progId, string description)
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? throw new Exception("Could not find EXE path.");

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
                {
                    key.SetValue("", description);
                    using (RegistryKey shellKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        shellKey.SetValue("", $"\"{exePath}\" \"%1\"");
                    }
                }

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}"))
                {
                    key.SetValue("", progId);
                }

                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting file association: {ex.Message}");
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
