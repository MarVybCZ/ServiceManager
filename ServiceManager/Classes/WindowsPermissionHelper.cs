using System;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Win32;

namespace ServiceManager.Classes
{
    public static class WindowsPermissionHelper
    {
        /// <summary>
        /// Check if the current user is running as administrator
        /// </summary>
        public static bool IsRunningAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the application has access to Windows services
        /// </summary>
        public static bool CanAccessWindowsServices()
        {
            try
            {
                // Try to get services list - this requires permissions
                ServiceController.GetServices();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if we can access service control manager
        /// </summary>
        public static bool CanAccessServiceControlManager()
        {
            try
            {
                // Try to access the registry key that contains service information
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
                {
                    return key != null;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get a human-readable message about current permission status
        /// </summary>
        public static string GetPermissionStatusMessage()
        {
            if (IsRunningAsAdministrator())
            {
                return "Running with administrator privileges.";
            }

            if (CanAccessWindowsServices())
            {
                return "Limited access - some operations may require administrator privileges.";
            }

            return "No access to Windows services - administrator privileges required.";
        }
    }
}