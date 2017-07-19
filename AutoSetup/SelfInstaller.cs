using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AutoSetup
{
    public class SelfInstaller
    {
        public bool Canceled { get; set; }

        public int Install(string msiFileName)
        {
            NativeMethods.MyMsiInstallUIHandler oldHandler = null;
            try
            {
                string logPath = "Setup.log";
                NativeMethods.MsiEnableLog(NativeMethods.LogMode.Verbose, logPath, 0u);
                NativeMethods.MsiSetInternalUI(2, IntPtr.Zero);

                oldHandler = NativeMethods.MsiSetExternalUI(new NativeMethods.MyMsiInstallUIHandler(MsiProgressHandler),
                                                          NativeMethods.LogMode.ExternalUI,
                                                          IntPtr.Zero);
                string param = "ACTION=INSTALL";
                int code = (int)NativeMethods.MsiInstallProduct(msiFileName, param);
                return code;
            }
            catch (Exception ex)
            {
                return -1;
            }
            finally
            {
                // 一定要把默认的handler设回去。
                if (oldHandler != null)
                {
                    NativeMethods.MsiSetExternalUI(oldHandler, NativeMethods.LogMode.None, IntPtr.Zero);
                }
            }
        }

        //最重要的就是这个方法了，这里仅演示了如何cancel一个安装，更多详情请参考MSDN文档
        private int MsiProgressHandler(IntPtr context, int messageType, string message)
        {
            if (this.Canceled)
            {
                // 这个返回值会告诉msi, cancel当前的安装
                return 2;
            }
            return 1;
        }
    }
}
