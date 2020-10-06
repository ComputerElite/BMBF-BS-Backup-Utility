using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QSE_Update
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        String exe = System.Reflection.Assembly.GetEntryAssembly().Location;

        public MainWindow()
        {
            InitializeComponent();
            exe = exe.Replace("\\BMBF_BS_Backup_Utility_Updater.exe", "");
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
            Thread.Sleep(5000);

            if (File.Exists(exe + "\\Newtonsoft.Json.dll"))
            {
                File.Delete(exe + "\\Newtonsoft.Json.dll");
            }
            if (File.Exists(exe + "\\Newtonsoft.Json.xml"))
            {
                File.Delete(exe + "\\Newtonsoft.Json.xml");
            }
            if (File.Exists(exe + "\\BMBF BS Backup Utility.exe"))
            {
                File.Delete(exe + "\\BMBF BS Backup Utility.exe");
            }

            if (!Directory.Exists(exe + "\\tmp"))
            {
                Directory.CreateDirectory(exe + "\\tmp");
            }

            using (WebClient client = new WebClient())
            {
                client.DownloadFile("https://raw.githubusercontent.com/ComputerElite/BMBF-BS-Backup-Utility/main/Update.txt", exe + "\\tmp\\Update.txt");
            }

            StreamReader VReader = new StreamReader(exe + "\\tmp\\Update.txt");

            String line;
            int l = 0;

            int MajorU = 0;
            int MinorU = 0;
            int PatchU = 0;
            String URL = "";
            while ((line = VReader.ReadLine()) != null)
            {
                if (l == 0)
                {
                    URL = line;
                }
                if (l == 1)
                {
                    MajorU = Convert.ToInt32(line);
                }
                if (l == 2)
                {
                    MinorU = Convert.ToInt32(line);
                }
                if (l == 3)
                {
                    PatchU = Convert.ToInt32(line);
                }
                l++;
            }

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(URL, exe + "\\tmp\\BMBF_BS_Backup_Utility_V_" + MajorU + "_" + MinorU + "_" + PatchU + ".zip");
            }
            ZipFile.ExtractToDirectory(exe + "\\tmp\\BMBF_BS_Backup_Utility_V_" + MajorU + "_" + MinorU + "_" + PatchU + ".zip", exe);
            txtbox.AppendText("\nFinished Updating");
            File.Delete(exe + "\\tmp\\BMBF_BS_Backup_Utility_V_" + MajorU + "_" + MinorU + "_" + PatchU + ".zip");
            ProcessStartInfo s = new ProcessStartInfo();
            s.CreateNoWindow = false;
            s.UseShellExecute = false;
            s.FileName = exe + "\\BMBF BS Backup Utility.exe";
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(s))
                {
                }
                this.Close();
            }
            catch
            {

            }
        }
    }
}
