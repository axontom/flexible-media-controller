using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;

namespace flexible_media_controller
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Name
        {
            get
            {
                var attribute = (AssemblyProductAttribute)Attribute
                    .GetCustomAttribute(Assembly.GetExecutingAssembly(),
                                        typeof(AssemblyProductAttribute));
                return attribute.Product;
            }
        }
        public static bool HasAdminRights
        {
            get
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        public static bool RunOnStartUp
        {
            get
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                    (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                return rk.GetValue(App.Name) != null;
            }
            set
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                    (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (value)
                    rk.SetValue(App.Name,
                        Process.GetCurrentProcess().MainModule.FileName);
                else
                    rk.DeleteValue(App.Name, false);
            }
        }
        public static void RestartAsAdmin()
        {
            var exeName = Process.GetCurrentProcess().MainModule.FileName;
            ProcessStartInfo startInfo = new ProcessStartInfo(exeName)
            {
                Verb = "runas",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(startInfo);
            Current.Shutdown();
        }
    }
}
