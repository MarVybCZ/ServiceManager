using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceManager.PInvoke
{     
    public partial class NativeMethods
    {
        /// Return Type: BOOL->int
        ///hService: SC_HANDLE->SC_HANDLE__*
        ///dwServiceType: DWORD->unsigned int
        ///dwStartType: DWORD->unsigned int
        ///dwErrorControl: DWORD->unsigned int
        ///lpBinaryPathName: LPCWSTR->WCHAR*
        ///lpLoadOrderGroup: LPCWSTR->WCHAR*
        ///lpdwTagId: LPDWORD->DWORD*
        ///lpDependencies: LPCWSTR->WCHAR*
        ///lpServiceStartName: LPCWSTR->WCHAR*
        ///lpPassword: LPCWSTR->WCHAR*
        ///lpDisplayName: LPCWSTR->WCHAR*
        [System.Runtime.InteropServices.DllImportAttribute("advapi32.dll", EntryPoint = "ChangeServiceConfigW")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfigW(IntPtr hService, uint dwServiceType, uint dwStartType, uint dwErrorControl, [System.Runtime.InteropServices.InAttribute()] [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lpBinaryPathName, [System.Runtime.InteropServices.InAttribute()] [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lpLoadOrderGroup, System.IntPtr lpdwTagId, [System.Runtime.InteropServices.InAttribute()] [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lpDependencies, [System.Runtime.InteropServices.InAttribute()] [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lpServiceStartName, [System.Runtime.InteropServices.InAttribute()] [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lpPassword, [System.Runtime.InteropServices.InAttribute()] [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lpDisplayName);

    }

}
