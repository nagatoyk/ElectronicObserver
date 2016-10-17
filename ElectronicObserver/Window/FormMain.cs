﻿using Codeplex.Data;
using ElectronicObserver.Data;
using ElectronicObserver.Observer;
using ElectronicObserver.Resource;
using ElectronicObserver.Resource.Record;
using ElectronicObserver.Utility;
using ElectronicObserver.Window.Dialog;
using ElectronicObserver.Window.Integrate;
using ElectronicObserver.Window.Plugins;
using ElectronicObserver.Window.Support;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace ElectronicObserver.Window
{
    public partial class FormMain : Form
    {

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0112) // WM_SYSCOMMAND 
                if (m.WParam.ToInt32() == 0xF100) // SC_KEYMENU
                    return;

            base.WndProc(ref m);
        }


        #region Properties

        public DockPanel MainPanel { get { return MainDockPanel; } }
        public FormWindowCapture WindowCapture { get { return fWindowCapture; } }

        private int ClockFormat;

        /// <summary>
        /// 音量設定用フラグ
        /// -1 = 無効, そうでなければ現在の試行回数
        /// </summary>
        private int _volumeUpdateState = 0;

        #endregion

		public FormBaseAirCorps fBaseAirCorps;
		public FormJson fJson;

        #region Forms

        public List<DockContent> SubForms { get; private set; }

        public List<IPluginHost> Plugins { get; private set; }

        public static PluginUpdateManager UpdateManager;

        public FormFleet[] fFleet;
        public FormShipGroup fShipGroup;
        public FormBrowserHost fBrowser;
        public FormWindowCapture fWindowCapture;

        public Form Browser { get { return fBrowser; } }

        #endregion


        private const string LAYOUT_FILE1 = @"Settings\WindowLayout.zip";
        private const string LAYOUT_FILE2 = @"Settings\WindowLayout2.zip";
        internal const string START2_FILE = @"Record\api_start2.json";

        public static Utility.MediaPlayer MediaPlayer;

        public FormMain()
        {
            this.SuspendLayoutForDpiScale();
            this.BackColor = Utility.Configuration.Config.UI.BackColor.ColorData;
            this.ForeColor = Utility.Configuration.Config.UI.ForeColor.ColorData;

            StopRectTimer = new System.Windows.Forms.Timer();

            InitializeComponent();
            this.ResumeLayoutForDpiScale();

            MediaPlayer = new MediaPlayer();
            InsertBGMPlayerMenu();

        }

        void InsertBGMPlayerMenu()
        {
            var BgmCachePath = Utility.Configuration.Config.CacheSettings.CacheFolder + @"\kcs\resources\bgm_p";
            if (Directory.Exists(BgmCachePath))
            {
                ToolStripMenuItem Menu = new ToolStripMenuItem("BGM播放器");
                Menu.DropDownItems.Add("-");
                Menu.DropDownOpened += BGMPlayerMenu_DropDownOpened;
                StripMenu_Tool.DropDownItems.Insert(0, Menu);
            }
        }

        private void BGMPlayerMenu_DropDownOpened(object sender, EventArgs e)
        {
            //StringBuilder builder = new StringBuilder();
            //builder.Append("{");
            //bool First = true;
            //foreach (var ship in KCDatabase.Instance.MasterShips.Values)
            //{
            //    if (ship.IsAbyssalShip)
            //        continue;
            //    if (!First)
            //        builder.AppendFormat(",");
            //    builder.AppendFormat("\"{0}\":[\"{1}\",\"{2}\"]", ship.ShipID, ship.Name, ship.ResourceName);
            //    First = false;
            //}
            //builder.Append("}");
            //File.WriteAllText("d:\\1.json", builder.ToString(), Encoding.UTF8);
            //return;
            ToolStripMenuItem Menu = sender as ToolStripMenuItem;
            Menu.DropDownItems.Clear();
            Menu.DropDownItems.Add("停止(&S)").Click += StopBGM_Click;
            Menu.DropDownItems.Add("-");

			StripMenu_View_BaseAirCorps.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormBaseAirCorps];
			StripMenu_View_Json.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormJson];

            foreach (var bgm in KCDatabase.Instance.BGM_List)
            {
                var item = Menu.DropDownItems.Add(bgm.Value);
                item.Tag = bgm.Key;
                item.Click += PlayBGM_Click;
            }
        }

        private void PlayBGM_Click(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            if (item.Tag == null)
                return;
            int bgmid = (int)item.Tag;
            string shortname = bgmid.ToString();
            var BgmCachePath = Utility.Configuration.Config.CacheSettings.CacheFolder + @"\kcs\resources\bgm_p";
            var files = Directory.GetFiles(BgmCachePath, shortname + "?.swf");
            if (files.Length > 0)
            {
                string bgm_name = KCDatabase.Instance.BGM_List[bgmid];
                var mp3 = Utility.SwfDecompiler.GetSoundFile(files[0], bgm_name);
                MediaPlayer.Stop();
                MediaPlayer.IsLoop = true;
                MediaPlayer.SourcePath = mp3;
                MediaPlayer.Play();
                Utility.Logger.Add(2, "正在播放BGM: " + KCDatabase.Instance.BGM_List[bgmid]);
            }
            else
            {
                Utility.Logger.Add(2, "找不到BGM文件: " + KCDatabase.Instance.BGM_List[bgmid]);
            }
        }

        private void StopBGM_Click(object sender, EventArgs e)
        {
            MediaPlayer.Stop();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {

            if (!Directory.Exists("Settings"))
                Directory.CreateDirectory("Settings");


            //Utility.Configuration.Instance.Load();


            Utility.Logger.Instance.LogAdded += new Utility.LogAddedEventHandler((Utility.Logger.LogData data) =>
            {
                if (InvokeRequired)
                {
                    // Invokeはメッセージキューにジョブを投げて待つので、別のBeginInvokeされたジョブが既にキューにあると、
                    // それを実行してしまい、BeginInvokeされたジョブの順番が保てなくなる
                    // GUIスレッドによる処理は、順番が重要なことがあるので、GUIスレッドからInvokeを呼び出してはいけない
                    Invoke(new Utility.LogAddedEventHandler(Logger_LogAdded), data);
                }
                else
                {
                    Logger_LogAdded(data);
                }
            });
            Utility.Configuration.Instance.ConfigurationChanged += ConfigurationChanged;

            Utility.Logger.Add(2, SoftwareInformation.SoftwareNameJapanese + " 开始启动…");

            this.Text = SoftwareInformation.VersionJapanese + "（迷彩型）";
            SyncBGMPlayer.Instance.ConfigurationChanged();


        }

        private async void FormMain_Shown(object sender, EventArgs e)
        {

            // 并行加载
            var task = Task.Factory.StartNew(() => Utility.Modify.ModifyConfiguration.Instance.LoadSettings()); // 不等待完成
            task = Task.Factory.StartNew(() => ResourceManager.Instance.Load());    // 保证与以下两个任务一起完毕

            await Task.Factory.StartNew(() => RecordManager.Instance.Load());
            await Task.Factory.StartNew(() => KCDatabase.Instance.Load());
            await task;

            Icon = ResourceManager.Instance.AppIcon;

            #region Icon settings
            StripMenu_Tool_PluginManager.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormConfiguration];

            StripMenu_File_Configuration.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormConfiguration];

            StripMenu_View_Fleet.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormFleet];
            //StripMenu_View_FleetOverview.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormFleet];
            StripMenu_View_ShipGroup.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormShipGroup];
            //StripMenu_View_Dock.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormDock];
            //StripMenu_View_Arsenal.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormArsenal];
            //StripMenu_View_Headquarters.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormHeadQuarters];
            //StripMenu_View_Quest.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormQuest];
            //StripMenu_View_Information.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormInformation];
            //StripMenu_View_Compass.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormCompass];
            //StripMenu_View_Battle.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormBattle];
            StripMenu_View_Browser.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormBrowser];
            //StripMenu_View_Log.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormLog];
            StripMenu_WindowCapture.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormWindowCapture];

            StripMenu_Tool_EquipmentList.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormEquipmentList];
            StripMenu_Tool_DropRecord.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormDropRecord];
            StripMenu_Tool_DevelopmentRecord.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormDevelopmentRecord];
            StripMenu_Tool_ConstructionRecord.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormConstructionRecord];
            StripMenu_Tool_ResourceChart.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormResourceChart];
            StripMenu_Tool_AlbumMasterShip.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormAlbumShip];
            StripMenu_Tool_AlbumMasterEquipment.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormAlbumEquipment];

            StripMenu_Help_Version.Image = ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.AppIcon];
            #endregion


            APIObserver.Instance.Start(Utility.Configuration.Config.Connection.Port, this);


            MainDockPanel.Extender.FloatWindowFactory = new CustomFloatWindowFactory();


            SubForms = new List<DockContent>();

            //form init
            //注：一度全てshowしないとイベントを受け取れないので注意
            fFleet = new FormFleet[4];
            for (int i = 0; i < fFleet.Length; i++)
            {
                SubForms.Add(fFleet[i] = new FormFleet(this, i + 1));
            }

            SubForms.Add(fShipGroup = new FormShipGroup(this));
            SubForms.Add(fBrowser = new FormBrowserHost(this));
            SubForms.Add(fWindowCapture = new FormWindowCapture(this));
			SubForms.Add( fBaseAirCorps = new FormBaseAirCorps( this ) );
			SubForms.Add( fJson = new FormJson( this ) );

            PluginUpdateManager.ApplyUpdates();

            await LoadPlugins();

            string lower = Configuration.Config.Life.LayoutFilePath.ToLower();
            if (lower == LAYOUT_FILE1.ToLower())
            {
                StripMenu_File_Layout1.Checked = true;
            }
            else if (lower == LAYOUT_FILE2.ToLower())
            {
                StripMenu_File_Layout2.Checked = true;
            }

            ConfigurationChanged();     //設定から初期化


            // layout
            LoadLayout(Configuration.Config.Life.LayoutFilePath);
            task = Task.Factory.StartNew(() => SoftwareInformation.CheckUpdate());




            // 完了通知（ログインページを開く）
            fBrowser.InitializeApiCompleted();

            UIUpdateTimer.Start();

            // 初始加载本地 api_start2
            if (Utility.Configuration.Config.CacheSettings.SaveApiStart2 && File.Exists(START2_FILE))
            {
                try
                {
                    string data = File.ReadAllText(START2_FILE);
                    APIObserver.Instance.LoadResponse("/kcsapi/api_start2", data);
                }
                catch (Exception ex)
                {
                    Utility.ErrorReporter.SendErrorReport(ex, "启动时加载本地 api_start2 失败。");
                }
            }

            Utility.Logger.Add(3, "启动处理完毕。");

            StopRectTimer.Interval = 100;
            StopRectTimer.Tick += new EventHandler(StopRectTimer_Tick);
            StopRectTimer.Enabled = Configuration.Config.Life.CanScreenDock;
            // HACK: タスクバーに表示されなくなる不具合への応急処置　効くかは知らない
            Show();
        }

        System.Windows.Forms.Timer StopRectTimer;

        private object _sync = new object();

        private async Task LoadPlugins()
        {
            Plugins = new List<IPluginHost>();

            //pluginManager = new PluginManager(this);

            var path = this.GetType().Assembly.Location;
            path = path.Substring(0, path.LastIndexOf('\\') + 1) + "Plugins";
            if (Directory.Exists(path))
            {
                bool flag = false;

                // load dlls
                await Task.Factory.StartNew(() =>
               {
                   foreach (var file in Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly))
                   {
                       try
                       {

                           var assembly = Assembly.LoadFile(file);
                           var pluginTypes = assembly.GetExportedTypes().Where(t => t.GetInterface(typeof(IPluginHost).FullName) != null);
                           if (pluginTypes != null && pluginTypes.Count() > 0)
                           {
                               foreach (var pluginType in pluginTypes)
                               {
                                   var plugin = (IPluginHost)assembly.CreateInstance(pluginType.FullName);
                                   lock (_sync)
                                   {
                                       Plugins.Add(plugin);
                                   }
                               }
                           }

                       }
                       catch (ReflectionTypeLoadException refEx)
                       {
                           Utility.ErrorReporter.SendLoadErrorReport(refEx, "载入插件时出错：" + file.Substring(file.LastIndexOf('\\') + 1));
                       }
                       catch (Exception ex)
                       {
                           Utility.ErrorReporter.SendErrorReport(ex, "载入插件时出错：" + file.Substring(file.LastIndexOf('\\') + 1));
                       }
                   }
               });

                UpdateManager = new PluginUpdateManager(Plugins);

                // instance them
                foreach (var plugin in Plugins)
                {
                    try
                    {
                        if (!flag)
                        {
                            var sep = new ToolStripSeparator();
                            StripMenu_View.DropDownItems.Add(sep);
                            flag = true;
                        }

                        if (plugin.PluginType == PluginType.DockContent || (plugin.PluginType & PluginType.DockContentPlugin) == PluginType.DockContentPlugin)
                        {
                            List<DockContent> plugins = new List<DockContent>();
                            foreach (var type in plugin.GetType().Assembly.GetExportedTypes().Where(t => t.BaseType == typeof(DockContent)))
                            {
                                if (type != null)
                                {
                                    var form = (DockContent)type.GetConstructor(new[] { typeof(FormMain) }).Invoke(new object[] { this });
                                    plugins.Add(form);
                                }
                            }
                            if (plugins.Count == 1)
                            {
                                var p = plugins[0];
                                var item = new ToolStripMenuItem
                                {
                                    Text = plugin.MenuTitle ?? p.Text,
                                    Tag = p
                                };
                                if (plugin.MenuIcon != null)
                                    item.Image = plugin.MenuIcon;
                                item.Click += menuitem_Click;
                                StripMenu_View.DropDownItems.Add(item);
                            }
                            else if (plugins.Count > 1)
                            {
                                var item = new ToolStripMenuItem
                                {
                                    Text = plugin.MenuTitle ?? plugins.First().Text
                                };
                                foreach (var p in plugins)
                                {
                                    var subItem = new ToolStripMenuItem
                                    {
                                        Text = p.Text,
                                        Tag = p
                                    };
                                    if (p.ShowIcon && p.Icon != null)
                                        subItem.Image = p.Icon.ToBitmap();
                                    subItem.Click += menuitem_Click;
                                    item.DropDownItems.Add(subItem);
                                }
                                StripMenu_View.DropDownItems.Add(item);
                            }

                            SubForms.AddRange(plugins);
                        }

                        // service
                        if (plugin.PluginType == PluginType.Service || (plugin.PluginType & PluginType.ServicePlugin) == PluginType.ServicePlugin)
                        {
                            if (plugin.RunService(this))
                            {
                                Utility.Logger.Add(2, string.Format("服务 {0}({1}) 已加载。", plugin.MenuTitle, plugin.Version));
                            }
                            else
                            {
                                Utility.Logger.Add(3, string.Format("服务 {0}({1}, {2}) 加载时返回异常结果。", plugin.MenuTitle, plugin.Version, plugin.GetType().Name));
                            }
                        }

                        // dialog
                        if (plugin.PluginType == PluginType.Dialog || (plugin.PluginType & PluginType.DialogPlugin) == PluginType.DialogPlugin)
                        {
                            var item = new ToolStripMenuItem
                            {
                                Text = plugin.MenuTitle,
                                Tag = plugin
                            };
                            if (plugin.MenuIcon != null)
                                item.Image = plugin.MenuIcon;
                            item.Click += dialogPlugin_Click;
                            StripMenu_Tool.DropDownItems.Add(item);
                        }

                        // observer
                        else if (plugin.PluginType == PluginType.Observer || (plugin.PluginType & PluginType.ObserverPlugin) == PluginType.ObserverPlugin)
                        {

                            if (plugin is ObserverPlugin)
                                Utility.Configuration.Instance.AddObserverPlugin((ObserverPlugin)plugin);
                            else
                                Utility.Logger.Add(3, string.Format("观察器 {0}({1}) 类型无效。", plugin.MenuTitle, plugin.Version));
                        }

                    }
                    catch (Exception ex)
                    {
                        Utility.ErrorReporter.SendErrorReport(ex, "载入插件时出错：" + plugin);
                    }

                }
            }

        }

        void dialogPlugin_Click(object sender, EventArgs e)
        {
            var plugin = (IPluginHost)((ToolStripMenuItem)sender).Tag;
            if (plugin != null)
            {
                try
                {
                    plugin.GetToolWindow().Show(this);
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    Utility.ErrorReporter.SendErrorReport(ex, string.Format("插件显示出错：{0}({1})", plugin.MenuTitle, plugin.Version));
                }
            }
        }


        void menuitem_Click(object sender, EventArgs e)
        {
            var f = ((ToolStripMenuItem)sender).Tag as DockContent;
            if (f != null)
            {
                f.Show(this.MainDockPanel);
            }
        }


        //private void FormMain_Shown( object sender, EventArgs e ) {
        //Load で設定すると無視されるかバグる(タスクバーに出なくなる)のでここで設定
        //TopMost = Utility.Configuration.Config.Life.TopMost;	
        //}

        private void ConfigurationChanged()
        {

            var c = Utility.Configuration.Config;

            StripStatus.Visible = c.Life.ShowStatusBar;

            if (!c.Log.AutoSave)
                nowSeconds = 0;

            // Load で TopMost を変更するとバグるため(前述)
            if (UIUpdateTimer.Enabled)
                TopMost = c.Life.TopMost;

            ClockFormat = c.Life.ClockFormat;

            Font = c.UI.MainFont;
            //StripMenu.Font = Font;
            StripStatus.Font = Font;
            StripStatus.ForeColor = c.UI.ForeColor;

            MainDockPanel.Skin.AutoHideStripSkin.TextFont = Font;
            MainDockPanel.Skin.DockPaneStripSkin.TextFont = Font;

            MainDockPanel.CanClosePane =
            MainDockPanel.CanHidePane =
            MainDockPanel.AllowEndUserDocking =
            MainDockPanel.AllowSplitterDrag = !c.Life.IsLocked;

            // color theme
            foreach (var f in SubForms)
            {
                if (f is FormShipGroup)
                {
                    f.BackColor = SystemColors.Control;
                    f.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    f.BackColor = this.BackColor;
                    f.ForeColor = this.ForeColor;
                }
            }

            if (c.Life.IsLocked)
            {
                //MainDockPanel.AllowChangeLayout = false;
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            }
            else
            {
                //MainDockPanel.AllowChangeLayout = true;
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            }

            StripMenu_File_Layout_Lock.Checked = c.Life.IsLocked;
            //MainDockPanel.CanCloseFloatWindowInLock = c.Life.CanCloseFloatWindowInLock;

            if (c.Life.CanScreenDock)
                StopRectTimer.Enabled = true;

			StripMenu_File_Layout_TopMost.Checked = c.Life.TopMost;

            if (!c.Control.UseSystemVolume)
                _volumeUpdateState = -1;
        }






        private int nowSeconds = 0;

        private void UIUpdateTimer_Tick(object sender, EventArgs e)
        {

            SystemEvents.OnUpdateTimerTick();

            // 自动保存
            if (Utility.Configuration.Config.Log.AutoSave && ++nowSeconds >= Utility.Configuration.Config.Log.AutoSaveMinutes * 60)
            {
                RecordManager.Instance.Save();
                KCDatabase.Instance.Save();
                nowSeconds = 0;
            }

            // 東京標準時
            DateTime now = DateTime.UtcNow + new TimeSpan(9, 0, 0);

            switch (ClockFormat)
            {
                case 0: //時計表示
                    StripStatus_Clock.Text = now.ToString("HH\\:mm\\:ss");
                    StripStatus_Clock.ToolTipText = now.ToString("yyyy\\/MM\\/dd (ddd)");
                    break;

                case 1: //演習更新まで
                    {
                        DateTime border = now.Date.AddHours(3);
                        while (border < now)
                            border = border.AddHours(12);

                        TimeSpan ts = border - now;
                        StripStatus_Clock.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
                        StripStatus_Clock.ToolTipText = now.ToString("yyyy\\/MM\\/dd (ddd) HH\\:mm\\:ss");

                    }
                    break;

                case 2: //任務更新まで
                    {
                        DateTime border = now.Date.AddHours(5);
                        if (border < now)
                            border = border.AddHours(24);

                        TimeSpan ts = border - now;
                        StripStatus_Clock.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
                        StripStatus_Clock.ToolTipText = now.ToString("yyyy\\/MM\\/dd (ddd) HH\\:mm\\:ss");

                    }
                    break;
            }


            // WMP コントロールによって音量が勝手に変えられてしまうため、前回終了時の音量の再設定を試みる。
            // 10回試行してダメなら諦める(例外によるラグを防ぐため)
            // 起動直後にやらないのはちょっと待たないと音量設定が有効にならないから
            if (_volumeUpdateState != -1 && _volumeUpdateState < 10 && Utility.Configuration.Config.Control.UseSystemVolume)
            {

                try
                {
                    uint id = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                    float volume = Utility.Configuration.Config.Control.LastVolume;
                    bool mute = Utility.Configuration.Config.Control.LastIsMute;

                    BrowserLib.VolumeManager.SetApplicationVolume(id, volume);
                    BrowserLib.VolumeManager.SetApplicationMute(id, mute);

                    SyncBGMPlayer.Instance.SetInitialVolume((int)(volume * 100));
                    /* no NotifierManager
					foreach ( var not in NotifierManager.Instance.GetNotifiers() )
						not.SetInitialVolume( (int)( volume * 100 ) );
                    //*/

                    _volumeUpdateState = -1;

                }
                catch (Exception)
                {

                    _volumeUpdateState++;
                }
            }

        }




        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {

            if (Utility.Configuration.Config.Life.ConfirmOnClosing)
            {
                if (MessageBox.Show(SoftwareInformation.SoftwareNameJapanese + " を終了しますか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
                    == System.Windows.Forms.DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (!SaveLayout(Configuration.Config.Life.LayoutFilePath))
            {
                if (MessageBox.Show("布局保存失败，是否继续关闭？\r\n详情请查看 ErrorReport 目录中的错误日志。", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
                    == System.Windows.Forms.DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }


            Utility.Logger.Add(2, SoftwareInformation.SoftwareNameJapanese + " 即将结束…");

            UIUpdateTimer.Stop();

            fBrowser.CloseBrowser();


            SystemEvents.OnSystemShuttingDown();



            // 音量の保存
            {
                try
                {
                    uint id = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                    Utility.Configuration.Config.Control.LastVolume = BrowserLib.VolumeManager.GetApplicationVolume(id);
                    Utility.Configuration.Config.Control.LastIsMute = BrowserLib.VolumeManager.GetApplicationMute(id);

                }
                catch (Exception)
                {
                    /* ぷちっ */
                }

            }
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopRectTimer.Enabled = false;
            UpdateManager.Stop();
            Utility.Configuration.Instance.Save();
            APIObserver.Instance.Stop();
            RecordManager.Instance.Save();
            KCDatabase.Instance.Save();


            Utility.Logger.Add(2, "结束处理完毕。");

            if (Utility.Configuration.Config.Log.SaveLogFlag)
                Utility.Logger.Save(@"eolog.log");

        }



        private IDockContent GetDockContentFromPersistString(string persistString)
        {

            switch (persistString)
            {
                case "Fleet #1":
                    return fFleet[0];
                case "Fleet #2":
                    return fFleet[1];
                case "Fleet #3":
                    return fFleet[2];
                case "Fleet #4":
                    return fFleet[3];
                //case "ShipGroup":
                //	return fShipGroup;
                case "Browser":
                    return fBrowser;
                case "WindowCapture":
                    return fWindowCapture;
				case "BaseAirCorps":
					return fBaseAirCorps;
				case "Json":
					return fJson;
                default:
                    if (persistString.StartsWith("ShipGroup"))
                    {
                        fShipGroup.ConfigureFromPersistString(persistString);
                        return fShipGroup;
                    }
                    if (persistString.StartsWith(FormIntegrate.PREFIX))
                    {
                        return FormIntegrate.FromPersistString(this, persistString);
                    }
                    else
                    {
                        var form = SubForms.FirstOrDefault(f => f.GetPersistString() == persistString);
                        if (form != null)
                        {
                            return form;
                        }
                    }
                    return null;
            }

        }



        private void LoadSubWindowsLayout(Stream stream)
        {

            try
            {

                if (stream != null)
                {

                    // 取り込んだウィンドウは一旦デタッチして閉じる
                    fWindowCapture.CloseAll();

                    foreach (var f in SubForms)
                    {
                        f.Show(MainDockPanel, DockState.Document);
                        f.DockPanel = null;
                    }

                    MainDockPanel.LoadFromXml(stream, new DeserializeDockContent(GetDockContentFromPersistString));

                    /* 一度全ウィンドウを読み込むことでフォームを初期化する
					foreach ( var x in MainDockPanel.Contents ) {
						if ( x.DockHandler.DockState == DockState.Hidden ) {
							x.DockHandler.Show( MainDockPanel );
							x.DockHandler.Hide();
						} else {
							x.DockHandler.Activate();
						}
					}
					//*/

                    fWindowCapture.AttachAll();

                }
                else
                {

                    foreach (var f in SubForms)
                        f.Show(MainDockPanel);


                    foreach (var x in MainDockPanel.Contents)
                    {
                        x.DockHandler.Hide();
                    }
                }

            }
            catch (Exception ex)
            {

                Utility.ErrorReporter.SendErrorReport(ex, "子窗口布局恢复失败。");
            }

        }


        private void SaveSubWindowsLayout(Stream stream)
        {

            try
            {

                MainDockPanel.SaveAsXml(stream, Encoding.UTF8);

            }
            catch (Exception ex)
            {

                Utility.ErrorReporter.SendErrorReport(ex, "子窗口布局保存失败。");
            }

        }


        private bool LoadLayout(string path)
        {

            try
            {

                using (var stream = File.OpenRead(path))
                {

                    using (var archive = new ZipFile(stream))
                    {

                        MainDockPanel.SuspendLayout(true);
                        WindowPlacementManager.LoadWindowPlacement(this, archive.GetInputStream(archive.GetEntry("WindowPlacement.xml")));
                        LoadSubWindowsLayout(archive.GetInputStream(archive.GetEntry("SubWindowLayout.xml")));

                    }
                }


                Utility.Logger.Add(2, "窗口布局已恢复。" + path);
                return true;

            }
            catch (FileNotFoundException)
            {

                Utility.Logger.Add(3, string.Format("窗口布局文件不存在。"));
                MessageBox.Show("已初始化布局。\r\n请在「显示」菜单中添加想要的窗口。", "窗口布局文件不存在",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                fBrowser.Show(MainDockPanel);

            }
            catch (DirectoryNotFoundException)
            {

                Utility.Logger.Add(3, string.Format("窗口布局文件不存在。"));
                MessageBox.Show("已初始化布局。\r\n请在「显示」菜单中添加想要的窗口。", "窗口布局文件不存在",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                fBrowser.Show(MainDockPanel);

            }
            catch (Exception ex)
            {

                Utility.ErrorReporter.SendErrorReport(ex, "窗口布局恢复失败。");

            }
            finally
            {

                MainDockPanel.ResumeLayout(true, true);
            }

            return false;
        }

        private bool SaveLayout(string path)
        {

            try
            {

                CreateParentDirectories(path);

                using (var stream = File.Open(path, FileMode.Create))
                using (var zipStream = new ZipOutputStream(stream))
                {

                    var entry = new ZipEntry("SubWindowLayout.xml");
                    entry.DateTime = DateTime.Now;
                    zipStream.PutNextEntry(entry);
                    SaveSubWindowsLayout(zipStream);

                    entry = new ZipEntry("WindowPlacement.xml");
                    entry.DateTime = DateTime.Now;
                    zipStream.PutNextEntry(entry);
                    WindowPlacementManager.SaveWindowPlacement(this, zipStream);

                    zipStream.CloseEntry();

                }


                Utility.Logger.Add(2, "窗口布局已保存。" + path);
                return true;

            }
            catch (Exception ex)
            {

                Utility.ErrorReporter.SendErrorReport(ex, "窗口布局保存失败。");
            }

            return false;
        }

        private void CreateParentDirectories(string path)
        {

            var parents = Path.GetDirectoryName(path);

            if (!String.IsNullOrEmpty(parents))
            {
                Directory.CreateDirectory(parents);
            }

        }



        void Logger_LogAdded(Utility.Logger.LogData data)
        {

            StripStatus_Information.Text = data.Message;

        }


        private void StripMenu_Help_Version_Click(object sender, EventArgs e)
        {

            using (var dialog = new DialogVersion())
            {
                dialog.ShowDialog(this);
            }

        }

        private void StripMenu_File_Configuration_Click(object sender, EventArgs e)
        {

            using (var dialog = new DialogConfiguration(Utility.Configuration.Config))
            {
                if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {

                    dialog.ToConfiguration(Utility.Configuration.Config);
                    Utility.Configuration.Instance.Save();
                    Utility.Configuration.Instance.OnConfigurationChanged();

                }
            }
        }

        private void StripMenu_File_Close_Click(object sender, EventArgs e)
        {
            Close();
        }


        private void StripMenu_File_SaveData_Save_Click(object sender, EventArgs e)
        {

            RecordManager.Instance.Save();
        }

        private void StripMenu_File_SaveData_Load_Click(object sender, EventArgs e)
        {

            if (MessageBox.Show("セーブしていないレコードが失われる可能性があります。\r\nロードしますか？", "確認",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
                == System.Windows.Forms.DialogResult.Yes)
            {

                RecordManager.Instance.Load();
            }

        }





        private void StripMenu_Tool_AlbumMasterShip_Click(object sender, EventArgs e)
        {

            if (KCDatabase.Instance.MasterShips.Count == 0)
            {
                MessageBox.Show("艦船データが読み込まれていません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
            else
            {
                new DialogAlbumMasterShip().Show(this);
            }

        }

        private void StripMenu_Tool_AlbumMasterEquipment_Click(object sender, EventArgs e)
        {

            if (KCDatabase.Instance.MasterEquipments.Count == 0)
            {
                MessageBox.Show("装備データが読み込まれていません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
            else
            {
                new DialogAlbumMasterEquipment().Show(this);
            }

        }

        private void StripMenu_Tool_PluginManager_Click(object sender, EventArgs e)
        {
            new DialogPlugins(this).Show(this);
        }

        private void StripMenu_Tool_CopyEOBrowserExecute_Click(object sender, EventArgs e)
        {

            var eobrowser = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "EOBrowser.exe");
            var parameter = "net.pipe://localhost/" + System.Diagnostics.Process.GetCurrentProcess().Id + "/ElectronicObserver";

            string path = eobrowser + " \"" + parameter + "\"";

            MessageBox.Show(this, "已复制以下启动参数至剪贴板：\r\n" + path + "\r\n请打开“运行”对话框粘贴执行。", "启动 EOBrowser", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Clipboard.SetText(path);

        }



        private void StripMenu_Tool_EquipmentList_Click(object sender, EventArgs e)
        {

            new DialogEquipmentList().Show(this);

        }



        private void StripMenu_Help_Help_Click(object sender, EventArgs e)
        {

            if (MessageBox.Show("外部ブラウザでオンラインヘルプを開きます。\r\nよろしいですか？", "ヘルプ",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1)
                == System.Windows.Forms.DialogResult.Yes)
            {

                System.Diagnostics.Process.Start("https://github.com/andanteyk/ElectronicObserver/wiki");
            }

        }


        private void SeparatorWhitecap_Click(object sender, EventArgs e)
        {
            new DialogWhitecap().Show(this);
        }



        private void StripMenu_File_Layout_Load_Click(object sender, EventArgs e)
        {

            LoadLayout(Utility.Configuration.Config.Life.LayoutFilePath);

        }

        private void StripMenu_File_Layout_Save_Click(object sender, EventArgs e)
        {

            SaveLayout(Utility.Configuration.Config.Life.LayoutFilePath);

        }

        private void StripMenu_File_Layout_Lock_Click(object sender, EventArgs e)
        {

            bool locked = !StripMenu_File_Layout_Lock.Checked;

            MainDockPanel.CanClosePane =
            MainDockPanel.CanHidePane =
            MainDockPanel.AllowEndUserDocking =
            MainDockPanel.AllowSplitterDrag = !locked;
            MainDockPanel.AllowEndUserDocking =
            MainDockPanel.AllowSplitterDrag = !locked;

            if (locked)
            {
                //MainDockPanel.AllowChangeLayout = false;
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            }
            else
            {
                //MainDockPanel.AllowChangeLayout = true;
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            }

            Utility.Configuration.Config.Life.IsLocked = locked;

            StripMenu_File_Layout_Lock.Checked = locked;

        }

        private void StripMenu_File_Layout_Open_Click(object sender, EventArgs e)
        {

            using (var dialog = new OpenFileDialog())
            {

                dialog.Filter = "Layout Archive|*.zip|File|*";
                dialog.Title = "レイアウト ファイルを開く";


                PathHelper.InitOpenFileDialog(Utility.Configuration.Config.Life.LayoutFilePath, dialog);

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {

                    Utility.Configuration.Config.Life.LayoutFilePath = PathHelper.GetPathFromOpenFileDialog(dialog);
                    LoadLayout(Utility.Configuration.Config.Life.LayoutFilePath);

                }

            }

        }

        private void StripMenu_File_Layout_Change_Click(object sender, EventArgs e)
        {

            using (var dialog = new SaveFileDialog())
            {

                dialog.Filter = "Layout Archive|*.zip|File|*";
                dialog.Title = "レイアウト ファイルの保存";


                PathHelper.InitSaveFileDialog(Utility.Configuration.Config.Life.LayoutFilePath, dialog);

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {

                    Utility.Configuration.Config.Life.LayoutFilePath = PathHelper.GetPathFromSaveFileDialog(dialog);
                    SaveLayout(Utility.Configuration.Config.Life.LayoutFilePath);

                }
            }

        }


        private void StripMenu_Tool_ResourceChart_Click(object sender, EventArgs e)
        {

            new Dialog.DialogResourceChart().Show(this);

        }

        private bool SwitchLayout(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show(string.Format("布局文件 {0} 不存在。", path), "切换布局", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }


            Utility.Configuration.Config.Life.LayoutFilePath = path;
            return LoadLayout(path);
        }

        private void StripMenu_File_Layout1_Click(object sender, EventArgs e)
        {
            if (SwitchLayout(LAYOUT_FILE1))
            {
                StripMenu_File_Layout1.Checked = true;
                StripMenu_File_Layout2.Checked = false;
            }
        }

        private void StripMenu_File_Layout2_Click(object sender, EventArgs e)
        {
            if (SwitchLayout(LAYOUT_FILE2))
            {
                StripMenu_File_Layout1.Checked = false;
                StripMenu_File_Layout2.Checked = true;
            }
        }



        private void StripMenu_Tool_DropRecord_Click(object sender, EventArgs e)
        {

            if (KCDatabase.Instance.MasterShips.Count == 0)
            {
                MessageBox.Show("艦これを読み込んでから開いてください。", "マスターデータがありません", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (RecordManager.Instance.ShipDrop.Record.Count == 0)
            {
                MessageBox.Show("ドロップレコードがありません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            new Dialog.DialogDropRecordViewer().Show(this);

        }


        private void StripMenu_Tool_DevelopmentRecord_Click(object sender, EventArgs e)
        {

            if (KCDatabase.Instance.MasterShips.Count == 0)
            {
                MessageBox.Show("艦これを読み込んでから開いてください。", "マスターデータがありません", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (RecordManager.Instance.Development.Record.Count == 0)
            {
                MessageBox.Show("開発レコードがありません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            new Dialog.DialogDevelopmentRecordViewer().Show(this);

        }

        private void StripMenu_Tool_ConstructionRecord_Click(object sender, EventArgs e)
        {

            if (KCDatabase.Instance.MasterShips.Count == 0)
            {
                MessageBox.Show("艦これを読み込んでから開いてください。", "マスターデータがありません", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (RecordManager.Instance.Construction.Record.Count == 0)
            {
                MessageBox.Show("建造レコードがありません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            new Dialog.DialogConstructionRecordViewer().Show(this);

        }



        private void StripMenu_File_Layout_LockLayout_Click(object sender, EventArgs e)
        {

            Utility.Configuration.Config.Life.IsLocked = StripMenu_File_Layout_Lock.Checked;
            ConfigurationChanged();

        }


        private void StripMenu_File_Layout_TopMost_Click(object sender, EventArgs e)
        {

            Utility.Configuration.Config.Life.TopMost = StripMenu_File_Layout_TopMost.Checked;
            ConfigurationChanged();

        }




        private void CallPumpkinHead(string apiname, dynamic data)
        {
            new DialogHalloween().Show(this);
            APIObserver.Instance.APIList["api_port/port"].ResponseReceived -= CallPumpkinHead;
        }


        private void StripMenu_WindowCapture_AttachAll_Click(object sender, EventArgs e)
        {
            fWindowCapture.AttachAll();
        }

        private void StripMenu_WindowCapture_DetachAll_Click(object sender, EventArgs e)
        {
            fWindowCapture.DetachAll();
        }


        #region フォーム表示

        /// <summary>
        /// 子フォームを表示します。既に表示されている場合はフォームをある点に移動します。（失踪対策）
        /// </summary>
        /// <param name="form"></param>
        private void ShowForm(DockContent form)
        {
            if (form.IsFloat && form.Visible)
            {
                form.FloatPane.FloatWindow.Location = new Point(128, 128);
            }

            form.Show(MainDockPanel);
        }

        private void StripMenu_View_Fleet_1_Click(object sender, EventArgs e)
        {
            fFleet[0].Show(MainDockPanel);
        }

        private void StripMenu_View_Fleet_2_Click(object sender, EventArgs e)
        {
            fFleet[1].Show(MainDockPanel);
        }

        private void StripMenu_View_Fleet_3_Click(object sender, EventArgs e)
        {
            fFleet[2].Show(MainDockPanel);
        }

        private void StripMenu_View_Fleet_4_Click(object sender, EventArgs e)
        {
            fFleet[3].Show(MainDockPanel);
        }

        private void StripMenu_View_ShipGroup_Click(object sender, EventArgs e)
        {
            fShipGroup.Show(MainDockPanel);
        }

        private void StripMenu_View_Browser_Click(object sender, EventArgs e)
        {
            fBrowser.Show(MainDockPanel);
        }

        private void StripMenu_WindowCapture_SubWindow_Click(object sender, EventArgs e)
        {
            fWindowCapture.Show(MainDockPanel);
        }

		private void StripMenu_View_BaseAirCorps_Click( object sender, EventArgs e ) {
			ShowForm( fBaseAirCorps );
		}

		private void StripMenu_View_Json_Click( object sender, EventArgs e ) {
			ShowForm( fJson );
		}

        #endregion




        private void StopRectTimer_Tick(object sender, EventArgs e)
        {
            int height = Height;
            AutoAdsorbent(height);
        }
        public void AutoAdsorbent(int height)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                if (Cursor.Position.X > Left - 1 && Cursor.Position.X < Right && Cursor.Position.Y > Top - 1 && Cursor.Position.Y < Bottom)
                {
                    if (Top <= 0 && Left > 5 && Left < Screen.PrimaryScreen.WorkingArea.Width - Width)
                    {
                        Top = 0;
                    }
                    else if (Left <= 0)
                    {
                        Left = 0;
                    }
                    else if (Left + Width > Screen.PrimaryScreen.WorkingArea.Width)
                    {
                        Left = Screen.PrimaryScreen.WorkingArea.Width - Width;
                    }
                    else
                    {
                        if (height > 0)
                        {
                            Height = height;
                            height = 0;
                        }
                    }
                    if (!Configuration.Config.Life.CanScreenDock)
                    {
                        StopRectTimer.Enabled = false;
                    }
                }
                else
                {
                    if (!Configuration.Config.Life.CanScreenDock)
                    {
                        return;
                    }
                    if (height < 1)
                    {
                        height = Height;
                    }
                    if (Top <= 4 && Left > 5 && Left < Screen.PrimaryScreen.WorkingArea.Width - Width)
                    {
                        Top = 3 - base.Height;
                        if (Left <= 4)
                        {
                            Left = -5;
                        }
                        else if (Left + Width >= Screen.PrimaryScreen.WorkingArea.Width - 4)
                        {
                            Left = Screen.PrimaryScreen.WorkingArea.Width - Width + 5;
                        }
                    }
                    else if (Left <= 4)
                    {
                        Left = 3 - Width;
                    }
                    else if (Left + Width >= Screen.PrimaryScreen.WorkingArea.Width - 4)
                    {
                        Left = Screen.PrimaryScreen.WorkingArea.Width - 3;
                    }
                }
            }
        }
    }
}

