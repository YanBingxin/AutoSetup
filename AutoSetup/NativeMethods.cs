using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace AutoSetup
{
    internal static class NativeMethods
    {
        [DllImport("msi.dll", CharSet = CharSet.Auto)]
        internal static extern int MsiSetInternalUI(int dwUILevel, IntPtr phWnd);

        [DllImport("msi.dll", CharSet = CharSet.Auto)]
        internal static extern MyMsiInstallUIHandler MsiSetExternalUI([MarshalAs(UnmanagedType.FunctionPtr)] 
            MyMsiInstallUIHandler puiHandler,
            NativeMethods.LogMode dwMessageFilter, IntPtr pvContext);

        [DllImport("msi.dll", CharSet = CharSet.Auto)]
        internal static extern uint MsiInstallProduct([MarshalAs(UnmanagedType.LPWStr)] string szPackagePath,
            [MarshalAs(UnmanagedType.LPWStr)]
            string szCommandLine);

        [DllImport("msi.dll", CharSet = CharSet.Auto)]
        internal static extern uint MsiEnableLog(NativeMethods.LogMode dwLogMode,
            [MarshalAs(UnmanagedType.LPWStr)] string szLogFile,
            uint dwLogAttributes);

        internal delegate int MyMsiInstallUIHandler(IntPtr context, int messageType,
            [MarshalAs(UnmanagedType.LPWStr)] string message);

        [Flags]
        internal enum LogMode : uint
        {
            None = 0u,
            Verbose = 4096u,
            ExternalUI = 20239u
        }

        public static List<string> GetFrameworkVersion()
        {
            List<string> versions = new List<string>();
            using (RegistryKey ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
            {
                foreach (string versionKeyName in ndpKey.GetSubKeyNames())
                {
                    if (versionKeyName.StartsWith("v"))
                    {
                        RegistryKey versionKey = ndpKey.OpenSubKey(versionKeyName);
                        string name = (string)versionKey.GetValue("Version", "");
                        string sp = versionKey.GetValue("SP", "").ToString();
                        string install = versionKey.GetValue("Install", "").ToString();
                        if (install == "") //no install info, ust be later
                            versions.Add(versionKeyName + "  " + name);
                        else
                        {
                            if (sp != "" && install == "1")
                            {
                                versions.Add(versionKeyName + "  " + name + "  SP" + sp);
                            }

                        }
                        if (name != "")
                        {
                            continue;
                        }
                        foreach (string subKeyName in versionKey.GetSubKeyNames())
                        {
                            RegistryKey subKey = versionKey.OpenSubKey(subKeyName);
                            name = (string)subKey.GetValue("Version", "");
                            if (name != "")
                                sp = subKey.GetValue("SP", "").ToString();
                            install = subKey.GetValue("Install", "").ToString();
                            if (install == "") //no install info, ust be later
                                versions.Add(versionKeyName + "  " + name);
                            else
                            {
                                if (sp != "" && install == "1")
                                {
                                    versions.Add("  " + subKeyName + "  " + name + "  SP" + sp);
                                }
                                else if (install == "1")
                                {
                                    versions.Add("  " + subKeyName + "  " + name);
                                }

                            }

                        }

                    }
                }
            }
            return versions;
        }
    }
}
