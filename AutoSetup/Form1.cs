using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AutoSetup
{
    public partial class Form1 : Form
    {
        private SelfInstaller _installer = null;
        static bool Canceled = false;
        bool framework = false;
        Process pro = null;

        internal Form1()
        {
            InitializeComponent();

            this.Shown += InstallProcessForm_Shown;
        }

        private void InstallProcessForm_Shown(object sender, EventArgs e)
        {
            string[] files = GetSetupFiles();
            if (files.Length < 1)
            {
                MessageBox.Show("未配置安装文件列表");
                this.Close();
            }
            //当窗口打开后就开始后台的安装
            BackgroundWorker bg = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bg.ProgressChanged += _installerBGWorker_ProgressChanged;
            bg.DoWork += delegate
            {
                bg.ReportProgress(0, "正在 检查目标机器.Net版本 ...");
                Thread.Sleep(1000);
                List<string> versions = NativeMethods.GetFrameworkVersion();
                if (versions.Find(v => v.ToUpper().Contains("V4")) != null)
                    framework = true;
                bg.ReportProgress(0, framework ? "发现 .Net4.0运行环境 ..." : "未发现 .Net4.0运行环境 ...");
                Thread.Sleep(1000);
                if (!framework)
                {
                    using (Process process = Process.Start(Path.Combine(Application.StartupPath, @"Framework4.0\Framework4.0.exe")))
                    {
                        pro = process;
                        bg.ReportProgress(0, "正在安装 " + " Framework4.0 ...");
                        process.WaitForExit();
                        string returnCode = process.ExitCode == 0 ? "安装成功 " : "安装失败 ";
                        bg.ReportProgress(0, returnCode + " Framework4.0");
                        Thread.Sleep(1000);
                    }
                    using (Process process = Process.Start(Path.Combine(Application.StartupPath, @"Framework4.0\Framework4.0_Hans.exe")))
                    {
                        pro = process;
                        bg.ReportProgress(0, "正在安装 " + " Framework4.0简体中文语言包 ...");
                        process.WaitForExit();
                        string returnCode = process.ExitCode == 0 ? "安装成功 " : "安装失败 ";
                        bg.ReportProgress(0, returnCode + " Framework4.0简体中文语言包");
                        Thread.Sleep(1000);
                    }
                }
            };
            bg.RunWorkerCompleted += delegate
            {
                InstallFiles(files);
            };
            bg.RunWorkerAsync();
        }

        private string[] GetSetupFiles()
        {
            if (File.Exists("Config.ini"))
            {
                List<string> fs = new List<string>();
                foreach (var file in File.ReadAllLines(@"Config.ini"))
                {
                    if (File.Exists(Path.Combine(Application.StartupPath, file)))
                    {
                        fs.Add(file);
                    }
                }
                return fs.ToArray();
            }
            else
            {
                using (StreamWriter sw = File.CreateText("Config.ini"))
                {
                    sw.WriteLine("安装文件配置列表");
                }
            }
            return new string[] { };
        }

        private void InstallFiles(string[] files)
        {
            List<BackgroundWorker> installBgs = new List<BackgroundWorker>();
            for (int i = 0; i < files.Length; i++)
            {
                string path = Path.Combine(Application.StartupPath, files[i]);
                if (!File.Exists(path))
                    continue;
                if (path.ToLower().EndsWith(".msi"))
                    installBgs.Add(InitializeMSIInstallBg(path, i, files.Length));
                else
                    installBgs.Add(InitializeInstallBg(path, i, files.Length));
            }
            //组织安装顺序
            for (int i = 0; i < installBgs.Count; i++)
            {
                int n = i;
                if (n + 1 < installBgs.Count)
                    installBgs[n].RunWorkerCompleted += delegate
                    {
                        if (!Canceled)
                            installBgs[n + 1].RunWorkerAsync();
                    };
                else
                    installBgs[n].RunWorkerCompleted += delegate
                    {
                        MessageBox.Show("安装完毕");
                        this.Close();
                    };
            }
            //开始后台安装
            if (installBgs.Count > 0)
                installBgs[0].RunWorkerAsync();
            else
            {
                MessageBox.Show("安装完毕");
                this.Close();
            }
        }
        /// <summary>
        ///初始化exe文件安装
        /// </summary>
        /// <param name="file"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private BackgroundWorker InitializeInstallBg(string file, int index, int count)
        {
            string name = Path.GetFileName(file);
            BackgroundWorker bg = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bg.DoWork += delegate(object sender, DoWorkEventArgs e)
            {
                bg.ReportProgress((int)(index * 100.0 / count), "正在安装 " + name + " ...(" + (index + 1) + "/" + count + ")");
                using (Process process = Process.Start(file))
                {
                    pro = process;
                    process.WaitForExit();
                    string returnCode = process.ExitCode == 0 ? "安装成功 " : "安装失败 ";
                    bg.ReportProgress((int)((index + 1) * 100.0 / count), returnCode + name);
                    Thread.Sleep(1000);
                }
            };
            bg.ProgressChanged += _installerBGWorker_ProgressChanged;
            return bg;
        }
        /// <summary>
        ///初始化msi文件安装
        /// </summary>
        /// <param name="file"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private BackgroundWorker InitializeMSIInstallBg(string file, int index, int count)
        {
            string name = Path.GetFileName(file);
            BackgroundWorker bg = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bg.DoWork += delegate(object sender, DoWorkEventArgs e)
            {
                bg.ReportProgress((int)(index * 100.0 / count), "正在安装 " + name + " ...(" + (index + 1) + "/" + count + ")");
                _installer = new SelfInstaller();
                string returnCode = _installer.Install(file) == 0 ? "安装成功 " : "安装失败 ";
                bg.ReportProgress((int)((index + 1) * 100.0 / count), returnCode + name);
                Thread.Sleep(1000);
            };
            bg.ProgressChanged += _installerBGWorker_ProgressChanged;
            return bg;
        }

        private void _installerBGWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // 消息通过 e.UserState 传回，并通过label显示在窗口上
            string message = e.UserState.ToString();
            this.progressBar.Value = e.ProgressPercentage;
            this.label1.Text = message;
            if (message == "正在取消安装 ...")
            {
                this.btnCancel.Enabled = false;
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "程序正在进行安装,您确实要取消安装吗?", "询问", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Canceled = true;
                if (_installer != null)
                    _installer.Canceled = true;
                if (pro != null)
                {
                    pro.CloseMainWindow();
                    pro.Close();
                }
                this.Close();
            }
        }
    }
}
