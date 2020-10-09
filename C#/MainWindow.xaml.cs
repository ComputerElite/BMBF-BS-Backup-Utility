using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
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

namespace BMBF_BS_Backup_Utility
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        int MajorV = 1;
        int MinorV = 0;
        int PatchV = 2;
        Boolean Preview = false;

        String IP = "";
        Boolean draggable = true;
        Boolean running = false;
        String exe = System.Reflection.Assembly.GetEntryAssembly().Location;
        String Songs = "";
        String Playlists = "";
        String Mods = "";
        String Scores = "";
        String BackupF = "";
        


        public MainWindow()
        {
            InitializeComponent();
            UpdateB.Visibility = Visibility.Hidden;
            exe = exe.Replace("\\BMBF BS Backup Utility.exe", "");
            
            BackupF = exe + "\\Backups";

            txtbox.Text = "Output:";

            if (!Directory.Exists(exe + "\\Backups"))
            {
                Directory.CreateDirectory(exe + "\\Backups");
            }
            if (!Directory.Exists(exe + "\\tmp"))
            {
                Directory.CreateDirectory(exe + "\\tmp");
            }
            if (File.Exists(exe + "\\BMBF_BS_Backup_Utility_Updater.exe"))
            {
                File.Delete(exe + "\\BMBF_BS_Backup_Utility_Updater.exe");
            }
            getBackups();
            Update();

            RSongs.IsChecked = true;
            RPlaylists.IsChecked = true;
            RScores.IsChecked = true;
            RMods.IsChecked = true;
            RReplays.IsChecked = true;
            RSounds.IsChecked = true;
        }


        public void Backup(object sender, RoutedEventArgs e)
        {
            if(running)
            {
                running = false;
                return;
            }
            running = true;

            //Check Quest IP
            Boolean good = CheckIP();
            if (!good)
            {
                txtbox.AppendText("\n\nChoose a valid IP!");
                running = false;
                return;
            }

            //Create all Backup Folders
            Boolean good2 = BackupFSet();
            if (!good2)
            {
                txtbox.AppendText("\n\nThis Backup already exists!");
                running = false;
                return;
            }

            //Scores
            txtbox.AppendText("\n\nBacking up scores");
            adb("pull /sdcard/Android/data/com.beatgames.beatsaber/files/LocalDailyLeaderboards.dat \"" + Scores + "\"");
            adb("pull /sdcard/Android/data/com.beatgames.beatsaber/files/LocalLeaderboards.dat \"" + Scores + "\"");
            adb("pull /sdcard/Android/data/com.beatgames.beatsaber/files/PlayerData.dat \"" + Scores + "\"");
            txtbox.AppendText("\nBacked up scores\n");
            txtbox.ScrollToEnd();

            //Songs

            QSE();

            //Playlists

            PlaylistB();
            adb("pull /sdcard/BMBFData/Playlists/ \"" + Playlists + "\"");
            txtbox.ScrollToEnd();

            //Replays

            txtbox.AppendText("\n\nBacking up replays");
            adb("pull /sdcard/Android/data/com.beatgames.beatsaber/files/replays \"" + BackupF + "\"");
            txtbox.AppendText("\nBacked up replays\n");
            txtbox.ScrollToEnd();

            //Sounds

            txtbox.AppendText("\n\nBacking up sounds");
            adb("pull /sdcard/Android/data/com.beatgames.beatsaber/files/sounds \"" + BackupF + "\"");
            txtbox.AppendText("\nBacked up sounds\n");
            txtbox.ScrollToEnd();

            //Mods

            ModsB();

            txtbox.AppendText("\n\n\nBMBF and Beat Saber Backup has been made.");
            txtbox.ScrollToEnd();
            running = false;
        }

        public void Restore(object sender, RoutedEventArgs e)
        {
            if (running)
            {
                return;
            }
            running = true;

            if (Backups.SelectedIndex == 0)
            {
                txtbox.AppendText("\n\nSelect a valid Backup!");
                running = false;
                return;
            }

            //Get Backup Folders
            BackupFGet();

            //Check Quest IP
            Boolean good = CheckIP();
            if(!good)
            {
                txtbox.AppendText("\n\nChoose a valid IP!");
                running = false;
                return;
            }

            //Scores
            if ((bool)RScores.IsChecked == true)
            {
                txtbox.AppendText("\n\nPushing Scores");
                adb("push \"" + Scores + "\\LocalDailyLeaderboards.dat\" /sdcard/Android/data/com.beatgames.beatsaber/files/LocalDailyLeaderboards.dat");
                adb("push \"" + Scores + "\\LocalLeaderboards.dat\" /sdcard/Android/data/com.beatgames.beatsaber/files/LocalLeaderboards.dat");
                adb("push \"" + Scores + "\\PlayerData.dat\" /sdcard/Android/data/com.beatgames.beatsaber/files/PlayerData.dat");
                txtbox.AppendText("\nPushed Scores");
                txtbox.ScrollToEnd();
            }

            //Playlists
            if ((bool)RPlaylists.IsChecked)
            {
                PlaylistsR();
                PushPNG(Playlists + "\\Playlists");
                txtbox.ScrollToEnd();
            }

            //Replays
            if ((bool)RReplays.IsChecked)
            {
                txtbox.AppendText("\n\nPushing Replays");
                adb("push \"" + BackupF + "//replays\" /sdcard/Android/data/com.beatgames.beatsaber/files/");
                txtbox.AppendText("\nFinished Pushing Replays");
                txtbox.ScrollToEnd();
            }

            //Sounds
            if ((bool)RSounds.IsChecked)
            {
                txtbox.AppendText("\n\nPushing Sounds");
                adb("push \"" + BackupF + "//sounds\" /sdcard/Android/data/com.beatgames.beatsaber/files/");
                txtbox.AppendText("\nFinished Pushing Sounds");
                txtbox.ScrollToEnd();
            }

            //Songs
            if ((bool)RSongs.IsChecked)
            {
                txtbox.AppendText("\n\nUploading Songs");
                Upload(Songs);
                txtbox.AppendText("\nUploaded Songs");
                txtbox.ScrollToEnd();
            }

            //Mods
            if ((bool)RMods.IsChecked)
            {
                txtbox.AppendText("\n\nUploading Mods");
                Upload(Mods);
                txtbox.AppendText("\nUploaded Mods");
                txtbox.ScrollToEnd();
            }

            txtbox.AppendText("\n\n\nBMBF and Beat Saber has been restored with the selected components.");
            txtbox.ScrollToEnd();
            running = false;
        }

        public Boolean CheckIP()
        {
            getQuestIP();
            if(IP == "Quest IP")
            {
                return false;
            }
            IP = IP.Replace(":5000000", "");
            IP = IP.Replace(":500000", "");
            IP = IP.Replace(":50000", "");
            IP = IP.Replace(":5000", "");
            IP = IP.Replace(":500", "");
            IP = IP.Replace(":500", "");
            IP = IP.Replace(":50", "");
            IP = IP.Replace(":5", "");

            int count = 0;
            for(int i = 0; i < IP.Length; i++)
            {
                if(IP.Substring(i, 1) == ".")
                {
                    count++;
                }
            }
            if(count != 3)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate {
                    Quest.Text = IP;
                }));
                return false;
            }
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate {
                Quest.Text = IP;
            }));
            return true;
        }

        public void PushPNG(String Path)
        {
            String[] directories = Directory.GetFiles(Path);



            for (int i = 0; i < directories.Length; i++)
            {
                if(directories[i].EndsWith(".png"))
                {
                    txtbox.AppendText("\n\nPushing " + directories[i] + " to Quest");
                    adb("push \"" + directories[i] + "\" /sdcard/BMBFData/Playlists/");
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
                }
            }
        }

        public void PlaylistsR()
        {
            try
            {
                getQuestIP();
                

                String PlaylistsX;

                txtbox.AppendText("\n\nRestoring Playlist from " + Playlists + "\\Playlists.json");
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));



                if (!Directory.Exists(exe + "\\tmp"))
                {
                    Directory.CreateDirectory(exe + "\\tmp");
                }
                using (WebClient client = new WebClient())
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate {
                        client.DownloadFile("http://" + IP + ":50000/host/beatsaber/config", exe + "\\tmp\\OConfig.json");
                    }));

                }

                String Config = exe + "\\tmp\\OConfig.json";

                PlaylistsX = Playlists + "\\Playlists.json";

                StreamReader reader = new StreamReader(@Config);
                String line;
                String CContent = "";
                int Index = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    Index = line.IndexOf("\"Mods\":", 0, line.Length);
                    CContent = line.Substring(Index, line.Length - Index);
                }

                StreamReader Preader = new StreamReader(@PlaylistsX);
                String Pline;
                String Content = "";
                while ((Pline = Preader.ReadLine()) != null)
                {
                    Content = Pline;
                }

                String finished = Content + CContent;

                JObject o = JObject.Parse(finished);
                o.Property("SyncConfig").Remove();
                o.Property("IsCommitted").Remove();
                o.Property("BeatSaberVersion").Remove();
                

                JProperty lrs = o.Property("Config");
                o.Add(lrs.Value.Children<JProperty>());
                lrs.Remove();

                String FConfig = o.ToString();
                File.WriteAllText(exe + "\\tmp\\config.json", FConfig);

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate {
                    postChanges(exe + "\\tmp\\config.json");
                }));
                txtbox.AppendText("\n\nRestored old Playlists.");
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
            }
            catch
            {
                txtbox.AppendText("\n\n\nAn error occured (Code: BMBF100). Couldn't access BMBF Check following:");
                txtbox.AppendText("\n\n- Your Quest is on and BMBF opened");
                txtbox.AppendText("\n\n- You put in the Quests IP right.");
            }
        }

        public void postChanges(String Config)
        {
            using (WebClient client = new WebClient())
            {
                client.QueryString.Add("foo", "foo");
                client.UploadFile("http://" + IP + ":50000/host/beatsaber/config", "PUT", Config);
                client.UploadValues("http://" + IP + ":50000/host/beatsaber/commitconfig", "POST", client.QueryString);
            }
        }
        public void Sync()
        {
            System.Threading.Thread.Sleep(2000);
            using (WebClient client = new WebClient())
            {
                client.QueryString.Add("foo", "foo");
                client.UploadValues("http://" + IP + ":50000/host/beatsaber/commitconfig", "POST", client.QueryString);
            }
        }

        public void Upload(String Path)
        {
            getQuestIP();
            String[] directories = Directory.GetFiles(Path);


            for (int i = 0; i < directories.Length; i++)
            {
                WebClient client = new WebClient();

                txtbox.AppendText("\n\nUploading " + directories[i] + " to BMBF");
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate {
                    client.UploadFile("http://" + IP + ":50000/host/beatsaber/upload?overwrite", directories[i]);
                }));

                if (i%20 == 0 && i != 0)
                {
                    txtbox.AppendText("\n\nSyncing to Beat Saber");
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
                    Sync();
                    System.Threading.Thread.Sleep(2000);
                }
            }
            Sync();
        }

        public void BackupFGet()
        {

            BackupF = exe + "\\Backups\\" + Backups.SelectedValue;
            Songs = BackupF + "\\Songs";
            Mods = BackupF + "\\Mods";
            Scores = BackupF + "\\Scores";
            Playlists = BackupF + "\\Playlists";
        }

        public void ModsB()
        {
            ArrayList list = new ArrayList();
            int overwritten = 0;
            int exported = 0;
            String Name = "";
            String Source = "";

            txtbox.AppendText("\nCopying all Mods to " + exe + "\\tmp. Please be patient.");
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
            adb("pull /sdcard/BMBFData/Mods/ \"" + exe + "\\tmp\"");
            if (Directory.Exists(exe + "\\tmp\\Mods"))
            {
                Source = exe + "\\tmp\\Mods";
            }
            else
            {
                Source = exe + "\\tmp";
            }

            string[] directories = Directory.GetDirectories(Source);



            for (int i = 0; i < directories.Length; i++)
            {
                txtbox.AppendText("\n");

                try
                {
                    String dat = directories[i] + "\\" + "bmbfmod.json";
                    StreamReader reader = new StreamReader(@dat);
                    String line;
                    while ((line = reader.ReadLine()) != null)
                    {

                        if (line.Contains("\"name\":"))
                        {
                            Name = Strings(line, 3);

                            Name = Name.Substring(0, Name.Length - 1);

                            Name = Name.Replace("/", "");
                            Name = Name.Replace(":", "");
                            Name = Name.Replace("*", "");
                            Name = Name.Replace("?", "");
                            Name = Name.Replace("\"", "");
                            Name = Name.Replace("<", "");
                            Name = Name.Replace(">", "");
                            Name = Name.Replace("|", "");

                            for (int f = 0; f < Name.Length; f++)
                            {
                                if (Name.Substring(f, 1).Equals("\\"))
                                {
                                    Name = Name.Substring(0, f - 1) + Name.Substring(f + 1, Name.Length - f - 1);
                                }
                            }
                            int Time = 0;
                            while (Name.Substring(Name.Length - 1, 1).Equals(" "))
                            {
                                Name = Name.Substring(0, Name.Length - 1);
                            }

                            while (list.Contains(Name.ToLower()))
                            {
                                Time++;
                                if (Time > 1)
                                {
                                    Name = Name.Substring(0, Name.Length - 1);
                                    Name = Name + Time;
                                }
                                else
                                {
                                    Name = Name + " " + Time;
                                }

                            }
                            list.Add(Name.ToLower());
                            txtbox.AppendText("\nMod Name: " + Name);
                            txtbox.AppendText("\nFolder: " + directories[i]);

                            bool v = File.Exists(Mods + "\\" + Name + ".zip");
                            if (v)
                            {
                                File.Delete(Mods + "\\" + Name + ".zip");
                                txtbox.AppendText("\noverwritten file: " + Mods + "\\" + Name + ".zip");

                                overwritten++;
                            }

                            zip(directories[i], Mods + "\\" + Name + ".zip");
                            exported++;
                            Name = "";
                        }
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
                        txtbox.ScrollToEnd();

                    }
                    reader.Close();
                }
                catch
                {

                }


            }

            txtbox.AppendText("\n");
            txtbox.AppendText("\n");
            txtbox.AppendText("\nFinished! Backed up " + exported + " Mods");
            txtbox.ScrollToEnd();
        }

        public void PlaylistB()
        {
            try
            {
                getQuestIP();

                txtbox.AppendText("\n\nBacking up Playlist to " + Playlists + "\\Playlists.json");
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));



                if (!Directory.Exists(exe + "\\tmp"))
                {
                    Directory.CreateDirectory(exe + "\\tmp");
                }
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile("http://" + IP + ":50000/host/beatsaber/config", exe + "\\tmp\\Config.json");
                }


                String Config = exe + "\\tmp\\config.json";



                StreamReader reader = new StreamReader(@Config);
                String line;
                while ((line = reader.ReadLine()) != null)
                {
                    int Index = line.IndexOf("\"Mods\":[{", 0, line.Length);
                    String PlaylistsX = line.Substring(0, Index);
                    File.WriteAllText(Playlists + "\\Playlists.json", PlaylistsX);
                }
                txtbox.AppendText("\n\nBacked up Playlists to " + Playlists + "Playlists.json");
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
            }
            catch
            {
                txtbox.AppendText("\n\n\nAn error occured (Code: BMBF100). Couldn't access BMBF. Check following:");
                txtbox.AppendText("\n\n- You put in the Quests IP right.");
                txtbox.AppendText("\n\n- Your Quest is on.");

            }
            getBackups();
        }

        public void QSE()
        {
            ArrayList list = new ArrayList();
            ArrayList content = new ArrayList();
            ArrayList over = new ArrayList();
            int overwritten = 0;
            int exported = 0;
            String Name = "";
            String Source = "";

            txtbox.AppendText("\nCopying all Songs to " + exe + "\\tmp. Please be patient.");
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
            adb("pull /sdcard/BMBFData/CustomSongs/ \"" + exe + "\\tmp\"");
            if (Directory.Exists(exe + "\\tmp\\CustomSongs"))
            {
                Source = exe + "\\tmp\\CustomSongs";
            }
            else
            {
                Source = exe + "\\tmp";
            }

            string[] directories = Directory.GetDirectories(Source);



            for (int i = 0; i < directories.Length; i++)
            {
                txtbox.AppendText("\n");


                if (!File.Exists(directories[i] + "\\" + "Info.dat") && !File.Exists(directories[i] + "\\" + "info.dat"))
                {
                    txtbox.AppendText("\n" + directories[i] + " is no Song");
                    continue;
                }
                String dat = "";
                if (File.Exists(directories[i] + "\\" + "Info.dat"))
                {
                    dat = directories[i] + "\\" + "Info.dat";

                }
                if (File.Exists(directories[i] + "\\" + "info.dat"))
                {
                    dat = directories[i] + "\\" + "info.dat";

                }
                try
                {
                    StreamReader reader = new StreamReader(@dat);
                    String line;
                    while ((line = reader.ReadLine()) != null)
                    {

                        if (line.Contains("_songName"))
                        {
                            if (line.Contains("_version") && line.Contains("songName"))
                            {
                                //BeatSage
                                Name = Strings(line, 7);

                                Name = Name.Substring(0, Name.Length - 1);

                                //Name = Name.replaceAll("[\\]", "");
                                Name = Name.Replace("/", "");
                                Name = Name.Replace(":", "");
                                Name = Name.Replace("*", "");
                                Name = Name.Replace("?", "");
                                Name = Name.Replace("\"", "");
                                Name = Name.Replace("<", "");
                                Name = Name.Replace(">", "");
                                Name = Name.Replace("|", "");

                                for (int f = 0; f < Name.Length; f++)
                                {
                                    if (Name.Substring(f, 1).Equals("\\"))
                                    {
                                        Name = Name.Substring(0, f - 1) + Name.Substring(f + 1, Name.Length - f - 1);
                                    }
                                }
                                int Time = 0;
                                while (Name.Substring(Name.Length - 1, 1).Equals(" "))
                                {
                                    Name = Name.Substring(0, Name.Length - 1);
                                }

                                while (list.Contains(Name.ToLower()))
                                {
                                    Time++;
                                    if (Time > 1)
                                    {
                                        Name = Name.Substring(0, Name.Length - 1);
                                        Name = Name + Time;
                                    }
                                    else
                                    {
                                        Name = Name + " " + Time;
                                    }

                                }
                                list.Add(Name.ToLower());
                                txtbox.AppendText("\nSong Name: " + Name);
                                txtbox.AppendText("\nFolder: " + directories[i]);

                                bool v = File.Exists(Songs + "\\" + Name + ".zip");
                                if (v)
                                {
                                    File.Delete(Songs + "\\" + Name + ".zip");
                                    txtbox.AppendText("\noverwritten file: " + Songs + "\\" + Name + ".zip");

                                    overwritten++;
                                }

                                zip(directories[i], Songs + "\\" + Name + ".zip");
                                exported++;
                                Name = "";
                                //src = new File("");

                            }
                            else
                            {
                                //normal Map
                                Name = Strings(line, 3);

                                Name = Name.Substring(0, Name.Length - 1);

                                //Name = Name.replaceAll("[\\]", "");
                                Name = Name.Replace("/", "");
                                Name = Name.Replace(":", "");
                                Name = Name.Replace("*", "");
                                Name = Name.Replace("?", "");
                                Name = Name.Replace("\"", "");
                                Name = Name.Replace("<", "");
                                Name = Name.Replace(">", "");
                                Name = Name.Replace("|", "");

                                for (int f = 0; f < Name.Length; f++)
                                {
                                    if (Name.Substring(f, 1).Equals("\\"))
                                    {
                                        Name = Name.Substring(0, f - 1) + Name.Substring(f + 1, Name.Length - f - 1);
                                    }
                                }
                                int Time = 0;
                                while (Name.Substring(Name.Length - 1, 1).Equals(" "))
                                {
                                    Name = Name.Substring(0, Name.Length - 1);
                                }

                                while (list.Contains(Name.ToLower()))
                                {
                                    Time++;
                                    if (Time > 1)
                                    {
                                        Name = Name.Substring(0, Name.Length - 1);
                                        Name = Name + Time;
                                    }
                                    else
                                    {
                                        Name = Name + " " + Time;
                                    }

                                }
                                list.Add(Name.ToLower());
                                txtbox.AppendText("\nSong Name: " + Name);
                                txtbox.AppendText("\nFolder: " + directories[i]);

                                bool v = File.Exists(Songs + "\\" + Name + ".zip");
                                if (v)
                                {
                                    File.Delete(Songs + "\\" + Name + ".zip");
                                    txtbox.AppendText("\noverwritten file: " + Songs + "\\" + Name + ".zip");

                                    overwritten++;
                                }

                                zip(directories[i], Songs + "\\" + Name + ".zip");
                                exported++;
                                Name = "";
                                //src = new File("");

                            }
                        }
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
                        txtbox.ScrollToEnd();

                    }
                    reader.Close();
                }
                catch
                {

                }


            }

            txtbox.AppendText("\n");
            txtbox.AppendText("\n");
            
            if(exported == 0)
            {
                txtbox.AppendText("\nerror (Code: QSE110). ");
            } else
            {
                txtbox.AppendText("\nFinished! Backed up " + exported + " Songs.");
            }
            txtbox.ScrollToEnd();
        }

        public static void zip(String src, String Output)
        {
            ZipFile.CreateFromDirectory(src, Output);

        }

        public String Strings(String line, int StartIndex)
        {
            int count = 0;
            String Name = "";
            for (int n = 0; n < line.Length; n++)
            {
                if (count == StartIndex)
                {

                    Name = Name + line.Substring(n, 1);
                }

                if (line.Substring(n, 1).Equals("\""))
                {
                    count++;
                }
            }
            return Name;
        }

        public Boolean BackupFSet()
        {
            BName.Text = BName.Text.Replace("/", "");
            BName.Text = BName.Text.Replace(":", "");
            BName.Text = BName.Text.Replace("*", "");
            BName.Text = BName.Text.Replace("?", "");
            BName.Text = BName.Text.Replace("\"", "");
            BName.Text = BName.Text.Replace("<", "");
            BName.Text = BName.Text.Replace(">", "");
            BName.Text = BName.Text.Replace("|", "");

            for (int f = 0; f < BName.Text.Length; f++)
            {
                if (BName.Text.Substring(f, 1).Equals("\\"))
                {
                    BName.Text = BName.Text.Substring(0, f - 1) + BName.Text.Substring(f + 1, BName.Text.Length - f - 1);
                }
            }

            BackupF = exe + "\\Backups\\" + BName.Text;

            if(Directory.Exists(BackupF))
            {
                return false;
            }

            Songs = BackupF + "\\Songs";
            Mods = BackupF + "\\Mods";
            Scores = BackupF + "\\Scores";
            Playlists = BackupF + "\\Playlists";

            if (!Directory.Exists(Songs))
            {
                Directory.CreateDirectory(Songs);
            }
            if (!Directory.Exists(Mods))
            {
                Directory.CreateDirectory(Mods);
            }
            if (!Directory.Exists(Scores))
            {
                Directory.CreateDirectory(Scores);
            }
            if (!Directory.Exists(Playlists))
            {
                Directory.CreateDirectory(Playlists);
            }
            return true;
        }

        public void getBackups()
        {
            ArrayList Folders = new ArrayList();
            string[] Files = Directory.GetDirectories(exe + "\\Backups");
            Backups.Items.Clear();
            Backups.Items.Add("Backups");

            for (int i = 0; i < Files.Length; i++)
            {
                    Folders.Add(Files[i].Substring(Files[i].LastIndexOf("\\") + 1, Files[i].Length - 1 - Files[i].LastIndexOf("\\")));
            }

            for (int o = 0; o <Folders.Count; o++)
            {
                Backups.Items.Add(Folders[o]);
            }
            Backups.SelectedIndex = 0;
        }

        public void getQuestIP()
        {
            IP = Quest.Text;
        }

        public void adb(String Argument)
        {
            String User = System.Environment.GetEnvironmentVariable("USERPROFILE");
            ProcessStartInfo s = new ProcessStartInfo();
            s.CreateNoWindow = false;
            s.UseShellExecute = false;
            s.FileName = "adb.exe";
            s.WindowStyle = ProcessWindowStyle.Minimized;
            s.Arguments = Argument;
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(s))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch
            {

                ProcessStartInfo se = new ProcessStartInfo();
                se.CreateNoWindow = false;
                se.UseShellExecute = false;
                se.FileName = User + "\\AppData\\Roaming\\SideQuest\\platform-tools\\adb.exe";
                se.WindowStyle = ProcessWindowStyle.Minimized;
                se.Arguments = Argument;
                try
                {
                    // Start the process with the info we specified.
                    // Call WaitForExit and then the using statement will close.
                    using (Process exeProcess = Process.Start(se))
                    {
                        exeProcess.WaitForExit();
                    }
                }
                catch
                {
                    // Log error.
                    txtbox.AppendText("\n\n\nAn error Occured (Code: ADB100). Check following");
                    txtbox.AppendText("\n\n- Your Quest is connected and USB Debugging enabled.");
                    txtbox.AppendText("\n\n- You have adb installed.");
                }

            }
        }




        public void Update()
        {
            try
            {
                //Download Update.txt
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        client.DownloadFile("https://raw.githubusercontent.com/ComputerElite/BMBF-BS-Backup-Utility/main/Update.txt", exe + "\\tmp\\Update.txt");
                    }
                    catch
                    {
                        txtbox.AppendText("\n\n\nAn error Occured (Code: UD100). Couldn't check for Updates. Check following");
                        txtbox.AppendText("\n\n- Your PC has internet.");
                    }
                }
                StreamReader VReader = new StreamReader(exe + "\\tmp\\Update.txt");

                String line;
                int l = 0;

                int MajorU = 0;
                int MinorU = 0;
                int PatchU = 0;
                while ((line = VReader.ReadLine()) != null)
                {
                    if (l == 0)
                    {
                        String URL = line;
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

                if (MajorU > MajorV || MinorU > MinorV || PatchU > PatchV)
                {
                    //Newer Version available
                    UpdateB.Visibility = Visibility.Visible;
                }

                String MajorVS = Convert.ToString(MajorV);
                String MinorVS = Convert.ToString(MinorV);
                String PatchVS = Convert.ToString(PatchV);
                String MajorUS = Convert.ToString(MajorU);
                String MinorUS = Convert.ToString(MinorU);
                String PatchUS = Convert.ToString(PatchU);

                String VersionVS = MajorVS + MinorVS + PatchVS;
                int VersionV = Convert.ToInt32(VersionVS);
                String VersionUS = MajorUS + MinorUS + PatchUS + " ";
                int VersionU = Convert.ToInt32(VersionUS);
                if (VersionV > VersionU)
                {
                    //Newer Version that hasn't been released yet
                    txtbox.AppendText("\n\nLooks like you have a preview version. Downgrade now from " + MajorV + "." + MinorV + "." + PatchV + " to " + MajorU + "." + MinorU + "." + PatchU + " xD");
                    UpdateB.Visibility = Visibility.Visible;
                    UpdateB.Content = "Downgrade Now xD";
                }
                if (VersionV == VersionU && Preview)
                {
                    //User has Preview Version but a release Version has been released
                    txtbox.AppendText("\n\nLooks like you have a preview version. The release version has been released. Please Update now. ");
                    UpdateB.Visibility = Visibility.Visible;
                }

            }
            catch
            {

            }
        }

        private void Start_Update(object sender, RoutedEventArgs e)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile("https://github.com/ComputerElite/BMBF-BS-Backup-Utility/raw/main/BMBF_BS_Backup_Utility_Updater.exe", exe + "\\BMBF_BS_Backup_Utility_Updater.exe");
            }
            //Process.Start(exe + "\\QSE_Update.exe");
            ProcessStartInfo s = new ProcessStartInfo();
            s.CreateNoWindow = false;
            s.UseShellExecute = false;
            s.FileName = exe + "\\BMBF_BS_Backup_Utility_Updater.exe";
            try
            {
                using (Process exeProcess = Process.Start(s))
                {
                }
                this.Close();
            }
            catch
            {
                // Log error.
                txtbox.AppendText("\n\n\nAn error Occured (Code: UD200). Couldn't download Update.");
            }
        }


        private void Mini(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void QuestIPCheck(object sender, RoutedEventArgs e)
        {
            if (Quest.Text == "")
            {
                Quest.Text = "Quest IP";
            }
        }

        private void BackupNameCheck(object sender, RoutedEventArgs e)
        {
            if (BName.Text == "")
            {
                BName.Text = "Backup Name";
            }
        }

        public void noDrag(object sender, MouseEventArgs e)
        {
            draggable = false;
        }

        public void doDrag(object sender, MouseEventArgs e)
        {
            draggable = true;
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(exe + "\\tmp"))
            {
                Directory.Delete(exe + "\\tmp", true);
            }
            this.Close();
        }

        private void Drag(object sender, RoutedEventArgs e)
        {
            bool mouseIsDown = System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed;


            if (mouseIsDown)
            {
                if (draggable)
                {
                    this.DragMove();
                }

            }

        }

        private void ClearText(object sender, RoutedEventArgs e)
        {
            if (Quest.Text == "Quest IP")
            {
                Quest.Text = "";
            }

        }

        private void ClearTextN(object sender, RoutedEventArgs e)
        {
            if (BName.Text == "Backup Name")
            {
                BName.Text = "";
            }
        }
    }
}
