﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;
using AvalonDock.Layout;
using Microsoft.Win32;
using ModernIde.Helpers;
using ModernIde.Helpers.Native;
using ModernIde.Helpers.Net;
using ModernIde.Dialogs;
using ModernIde.Windows.Pages;

namespace ModernIde.Windows
{
    /// <summary>
    ///     Interaction logic for Home.xaml
    /// </summary>
    public partial class Home
    {
        private int _lastDocumentIndex = -1;

        public Home()
        {
            InitializeComponent();

            DwmDropShadow.DropShadowToWindow(this);

            UpdateTitleText("");
            UpdateStatusText("Ready...");

            //Window_StateChanged(null, null);
            ClearTabs();

            if (App.ModernIdeStorage.ModernIdeSettings.StartpageShowOnLoad)
                AddTabModule(TabGenre.StartPage);

            // Do sidebar Loading stuff
            //SwitchXBDMSidebarLocation(App.AssemblyStorage.AssemblySettings.applicationXBDMSidebarLocation);
            //XBDMSidebarTimerEvent();

            // Set width/height/state from last session
            if (!double.IsNaN(App.ModernIdeStorage.ModernIdeSettings.ApplicationSizeHeight) &&
                App.ModernIdeStorage.ModernIdeSettings.ApplicationSizeHeight > MinHeight)
                Height = App.ModernIdeStorage.ModernIdeSettings.ApplicationSizeHeight;
            if (!double.IsNaN(App.ModernIdeStorage.ModernIdeSettings.ApplicationSizeWidth) &&
                App.ModernIdeStorage.ModernIdeSettings.ApplicationSizeWidth > MinWidth)
                Width = App.ModernIdeStorage.ModernIdeSettings.ApplicationSizeWidth;

            WindowState = App.ModernIdeStorage.ModernIdeSettings.ApplicationSizeMaximize
                ? WindowState.Maximized
                : WindowState.Normal;
            Window_StateChanged(null, null);

            AllowDrop = true;
            App.ModernIdeStorage.ModernIdeSettings.HomeWindow = this;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr handle = (new WindowInteropHelper(this)).Handle;
            HwndSource hwndSource = HwndSource.FromHwnd(handle);
            if (hwndSource != null)
                hwndSource.AddHook(WindowProc);

            ProcessCommandLineArgs(Environment.GetCommandLineArgs());

            if (App.ModernIdeStorage.ModernIdeSettings.ApplicationUpdateOnStartup)
                StartUpdateCheck();
        }

        private void StartUpdateCheck()
        {
            var worker = new BackgroundWorker();
            worker.DoWork += CheckForUpdates;
            worker.RunWorkerCompleted += UpdateCheckCompleted;
            worker.RunWorkerAsync();
        }

        private void CheckForUpdates(object sender, DoWorkEventArgs e)
        {
            // Grab JSON Update package from the server
            e.Result = Updates.GetUpdateInfo();
        }

        private void UpdateCheckCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
                return;

            var updateInfo = (UpdateInfo) e.Result;
            if (Updater.UpdateAvailable(updateInfo))
                MetroUpdateDialog.Show(updateInfo, true);
        }

        private void dockManager_ActiveContentChanged(object sender, EventArgs e)
        {
            if (documentManager.SelectedContentIndex != _lastDocumentIndex)
            {
                // Selection Changed, lets do dis
                LayoutContent tab = documentManager.SelectedContent;

                if (tab != null)
                    UpdateTitleText(tab.Title.Replace("__", "_").Replace(".map", ""));

                if (tab != null && tab.Title == "Start Page")
                    ((StartPage) tab.Content).UpdateRecents();

                if (tab == null)
                {
                    documentManager.SelectedContentIndex = 0;
                    UpdateTitleText("");
                }

                _lastDocumentIndex = documentManager.SelectedContentIndex;
            }
        }

        // File
        private void menuOpenCacheFile_Click(object sender, RoutedEventArgs e)
        {
            OpenContentFile(ContentTypes.Map);
        }

        // View
        private void menuViewStartPage_Click(object sender, RoutedEventArgs e)
        {
            AddTabModule(TabGenre.StartPage);
        }

        private void menuOpenSettings_Click(object sender, EventArgs e)
        {
            AddTabModule(TabGenre.Settings);
        }

        // Help
        private void menuHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            MetroAbout.Show();
        }

        private void menuHelpUpdater_Click(object sender, RoutedEventArgs e)
        {
            var thrd = new Thread(Updater.BeginUpdateProcess);
            thrd.Start();
        }

        // Goodbye Sweet Evelyn
        private void menuCloseApplication_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #region Waste of Space, idk man

        private void Home_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //var app = (App)Application.Current;
        }

        private void Window_PreviewDrop_1(object sender, DragEventArgs e)
        {
        }

        #endregion

        #region More WPF Annoyance

        private void ResizeDrop_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double yadjust = Height + e.VerticalChange;
            double xadjust = Width + e.HorizontalChange;

            if (xadjust > MinWidth)
                Width = xadjust;
            if (yadjust > MinHeight)
                Height = yadjust;
        }

        private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double xadjust = Width + e.HorizontalChange;

            if (xadjust > MinWidth)
                Width = xadjust;
        }

        private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double yadjust = Height + e.VerticalChange;

            if (yadjust > MinHeight)
                Height = yadjust;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    borderFrame.BorderThickness = new Thickness(1, 1, 1, 23);
                    btnActionRestore.Visibility = Visibility.Collapsed;
                    btnActionMaximize.Visibility =
                        ResizeDropVector.Visibility =
                            ResizeDrop.Visibility =
                                ResizeRight.Visibility = ResizeBottom.Visibility = Visibility.Visible;
                    break;
                case WindowState.Maximized:
                    borderFrame.BorderThickness = new Thickness(0, 0, 0, 23);
                    btnActionRestore.Visibility = Visibility.Visible;
                    btnActionMaximize.Visibility =
                        ResizeDropVector.Visibility =
                            ResizeDrop.Visibility =
                                ResizeRight.Visibility = ResizeBottom.Visibility = Visibility.Collapsed;
                    break;
            }
            /*
			 * ResizeDropVector
			 * ResizeDrop
			 * ResizeRight
			 * ResizeBottom
			 */
        }

        private void btnActionSupport_Click(object sender, RoutedEventArgs e)
        {
            // Load support page?
        }

        private void btnActionMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnActionRestore_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
        }

        private void btnActionMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
        }

        private void btnActionClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #region Maximize Workspace Workarounds

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }

        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            var mmi = (Monitor_Workarea.MINMAXINFO) Marshal.PtrToStructure(lParam, typeof (Monitor_Workarea.MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            const int monitorDefaulttonearest = 0x00000002;
            IntPtr monitor = Monitor_Workarea.MonitorFromWindow(hwnd, monitorDefaulttonearest);

            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new Monitor_Workarea.MONITORINFO();
                Monitor_Workarea.GetMonitorInfo(monitor, monitorInfo);
                Monitor_Workarea.RECT rcWorkArea = monitorInfo.rcWork;
                Monitor_Workarea.RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);

                /*
				mmi.ptMaxPosition.x = Math.Abs(scrn.Bounds.Left - scrn.WorkingArea.Left);
				mmi.ptMaxPosition.y = Math.Abs(scrn.Bounds.Top - scrn.WorkingArea.Top);
				mmi.ptMaxSize.x = Math.Abs(scrn.Bounds.Right - scrn.WorkingArea.Left);
				mmi.ptMaxSize.y = Math.Abs(scrn.Bounds.Bottom - scrn.WorkingArea.Top);
				*/
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        #endregion

        #endregion

        #region Content Management

        public enum ContentTypes
        {
            Map
        }

        private readonly Dictionary<ContentTypes, ContentFileHandler> _contentFileHandlers = new Dictionary
            <ContentTypes, ContentFileHandler>
        {
            {
                ContentTypes.Map, new ContentFileHandler(
                    "ModernIde - Open Blam Cache File",
                    "Blam Cache File (*.map)|*.map",
                    (home, file) => home.AddCacheTabModule(file))
            },
        };

        /// <summary>
        ///     Open a new Blam Engine File
        /// </summary>
        /// <param name="contentType">Type of content to open</param>
        public void OpenContentFile(ContentTypes contentType)
        {
            ContentFileHandler handler;
            if (!_contentFileHandlers.TryGetValue(contentType, out handler)) return;

            var ofd = new OpenFileDialog
            {
                Title = handler.Title,
                Filter = handler.Filter,
                Multiselect = handler.AllowMultipleFiles,
            };

            if (!(bool) ofd.ShowDialog(this)) return;

            if (handler.AllowMultipleFiles)
                foreach (string file in ofd.FileNames)
                    handler.FileHandler(this, file);
            else
                handler.FileHandler(this, ofd.FileName);
        }

        private class ContentFileHandler
        {
            public readonly Action<Home, string> FileHandler;

            public ContentFileHandler(string title, string filter, Action<Home, string> handler,
                bool allowMultipleFiles = true)
            {
                Title = title;
                Filter = filter;
                AllowMultipleFiles = allowMultipleFiles;
                FileHandler = handler;
            }

            public string Title { get; private set; }
            public string Filter { get; private set; }
            public bool AllowMultipleFiles { get; private set; }
        };

        #endregion

        #region Tab Manager

        public enum TabGenre
        {
            StartPage,
            Settings,
            NetworkPoking,
            PluginGenerator,
            Welcome,
            PluginConverter,

            MemoryManager,
            VoxelConverter,
            PostGenerator
        }

        public void ExternalTabClose(TabGenre tabGenre)
        {
            string tabHeader = "";
            switch (tabGenre)
            {
                case TabGenre.StartPage:
                    tabHeader = "Start Page";
                    break;
                case TabGenre.Settings:
                    tabHeader = "Settings";
                    break;
            }

            LayoutDocument toRemove = null;
            foreach (
                LayoutContent tab in
                    documentManager.Children.Where(tab => tab.Title == tabHeader && tab is LayoutDocument))
                toRemove = (LayoutDocument) tab;

            if (toRemove != null)
                documentManager.Children.Remove(toRemove);
        }

        public void ExternalTabClose(LayoutDocument tab)
        {
            documentManager.Children.Remove(tab);

            if (documentManager.Children.Count > 0)
                documentManager.SelectedContentIndex = documentManager.Children.Count - 1;
        }

        public void ClearTabs()
        {
            documentManager.Children.Clear();
        }

        /// <summary>
        ///     Add a new Blam Cache Editor Container
        /// </summary>
        /// <param name="cacheLocation">Path to the Blam Cache File</param>
        public void AddCacheTabModule(string cacheLocation)
        {
            // Check Map isn't already open
            foreach (LayoutContent tab in documentManager.Children.Where(tab => tab.ContentId == cacheLocation))
            {
                documentManager.SelectedContentIndex = documentManager.IndexOfChild(tab);
                return;
            }

            var newCacheTab = new LayoutDocument
            {
                ContentId = cacheLocation,
                Title = "",
                ToolTip = cacheLocation
            };
            /*newCacheTab.Content = new HaloMap(cacheLocation, newCacheTab,
                App.AssemblyStorage.AssemblySettings.HalomapTagSort);*/
            documentManager.Children.Add(newCacheTab);
            documentManager.SelectedContentIndex = documentManager.IndexOfChild(newCacheTab);
        }

        /// <summary>
        ///     Add a new XBox Screenshot Editor Container
        /// </summary>
        /// <param name="tempImageLocation">Path to the temporary location of the image</param>
        public void AddScrenTabModule(string tempImageLocation)
        {
            var newScreenshotTab = new LayoutDocument
            {
                ContentId = tempImageLocation,
                Title = "Screenshot",
                ToolTip = tempImageLocation
            };
            documentManager.Children.Add(newScreenshotTab);
            documentManager.SelectedContentIndex = documentManager.IndexOfChild(newScreenshotTab);
        }

        public void AddTabModule(TabGenre tabG, bool singleInstance = true)
        {
            var tab = new LayoutDocument();

            switch (tabG)
            {
                case TabGenre.StartPage:
                    tab.Title = "Start Page";
                    tab.Content = new StartPage();
                    break;
                case TabGenre.Welcome:
                    tab.Title = "Welcome";
                    tab.Content = new WelcomePage();
                    break;
                case TabGenre.Settings:
                    tab.Title = "Settings";
                    tab.Content = new SettingsPage();
                    break;
            }

            if (singleInstance)
                foreach (LayoutContent tabb in documentManager.Children.Where(tabb => tabb.Title == tab.Title))
                {
                    documentManager.SelectedContentIndex = documentManager.IndexOfChild(tabb);
                    return;
                }

            documentManager.Children.Add(tab);
            documentManager.SelectedContentIndex = documentManager.IndexOfChild(tab);
        }

        #endregion

        #region Public Access Modifiers

        private readonly DispatcherTimer _statusUpdateTimer = new DispatcherTimer();

        /// <summary>
        ///     Set the title text of Assembly
        /// </summary>
        /// <param name="title">Current Title, Assembly shall add the rest for you.</param>
        public void UpdateTitleText(string title)
        {
            string suffix = "ModernIde";
            if (!string.IsNullOrWhiteSpace(title))
                suffix = " - " + suffix;

            Title = title + suffix;
            lblTitle.Text = title + suffix;
        }

        /// <summary>
        ///     Set the status text of Assembly
        /// </summary>
        /// <param name="status">Current Status of Assembly</param>
        public void UpdateStatusText(string status)
        {
            Status.Text = status;

            _statusUpdateTimer.Stop();
            _statusUpdateTimer.Interval = new TimeSpan(0, 0, 0, 4);
            _statusUpdateTimer.Tick += statusUpdateCleaner_Clear;
            _statusUpdateTimer.Start();
        }

        private void statusUpdateCleaner_Clear(object sender, EventArgs e)
        {
            Status.Text = "Ready...";
        }

        #endregion

        #region Opacity Masking

        public int OpacityIndex;

        public void ShowMask()
        {
            OpacityIndex++;
            OpacityRect.Visibility = Visibility.Visible;
        }

        public void HideMask()
        {
            OpacityIndex--;

            if (OpacityIndex == 0)
                OpacityRect.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Drag&Drop Support

        private void HomeWindow_Drop(object sender, DragEventArgs e)
        {
            // FIXME: Boot into Win7, to fix this. (Win8's UAC is so fucked up... No drag and drop on win8 it seems...)
            //string[] draggedFiles = (string[])e.Data.GetData(DataFormats.FileDrop, true);
        }

        #endregion

        #region Startup

        public bool ProcessCommandLineArgs(IList<string> args)
        {
            if (args != null && args.Count > 1)
            {
                string[] commandArgs = args.Skip(1).ToArray();
                if (commandArgs[0].StartsWith("assembly://"))
                    commandArgs[0] = commandArgs[0].Substring(11).Trim('/');

                // Decide what to do
                Activate();
                switch (commandArgs[0].ToLower())
                {
                    case "open":
                        // Determine type of file, and start it up, yo
                        if (commandArgs.Length > 1)
                            StartupDetermineType(commandArgs[1]);
                        break;

                    case "update":
                        // Show Update
                        menuHelpUpdater_Click(null, null);
                        break;

                    case "about":
                        // Show About
                        menuHelpAbout_Click(null, null);
                        break;

                    case "settings":
                        // Show Settings
                        menuOpenSettings_Click(null, null);
                        break;

                    default:
                        return true;
                }
            }

            return true;
        }

        private void StartupDetermineType(string path)
        {
            try
            {
                if (File.Exists(path))
                {/*
                    // Magic Check
                    string magic;
                    using (var stream = new EndianReader(File.OpenRead(path), Endian.BigEndian))
                        magic = stream.ReadAscii(0x04).ToLower();

                    switch (magic)
                    {
                        case "head":
                        case "daeh":
                            // Map File
                            AddCacheTabModule(path);
                            return;
                    }*/
                }

                MetroMessageBox.Show("Unable to find file", "The selected file could no longer be found");
            }
            catch (Exception ex)
            {
                MetroException.Show(ex);
            }
        }

        #endregion
    }
}