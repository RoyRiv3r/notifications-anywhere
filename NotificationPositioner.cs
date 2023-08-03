using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using Microsoft.Win32;
using System.IO;

public class Program
{
    #region PositionForm Class

    public class PositionForm : Form
    {
        public Button ResetButton { get; private set; } 
        public TrackBar XSlider { get; private set; }
        public TrackBar YSlider { get; private set; }
        public Button TestNotificationButton { get; private set; }
        public ComboBox MonitorSelector { get; private set; }
        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {

            XSlider.Maximum = Screen.PrimaryScreen.Bounds.Width;
            YSlider.Maximum = Screen.PrimaryScreen.Bounds.Height;

            XSlider.Value = 0;
            YSlider.Value = 0;

            MonitorSelector.Items.Clear();
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                MonitorSelector.Items.Add(String.Format("Monitor {0}", i + 1));
            }
            MonitorSelector.SelectedIndex = 0;

            ProgramUtilities.SavePosition(
                XSlider.Value,
                YSlider.Value,
                MonitorSelector.SelectedIndex
            );
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            }
            base.Dispose(disposing);
        }
        public PositionForm()
        {
            this.Text = "Notification Position";
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimizeBox = false;

            Label xSliderLabel = new Label
            {
                Text = "LEFT/RIGHT",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top
            };
            XSlider = new TrackBar
            {
                Minimum = -500,
                Maximum = Screen.PrimaryScreen.Bounds.Width,
                TickStyle = TickStyle.None,
                Dock = DockStyle.Top
            };

            Label separator1 = new Label { Dock = DockStyle.Top, Height = 10 };

            Label ySliderLabel = new Label
            {
                Text = "TOP/BOTTOM",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top
            };
            YSlider = new TrackBar
            {
                Minimum = -500,
                Maximum = Screen.PrimaryScreen.Bounds.Height,
                TickStyle = TickStyle.None,
                Dock = DockStyle.Top
            };

            Label separator2 = new Label { Dock = DockStyle.Top, Height = 10 };

            TestNotificationButton = new Button
            {
                Text = "Test Notification",
                Dock = DockStyle.Top
            };
            Label separator3 = new Label { Dock = DockStyle.Top, Height = 10 };

            Label monitorSelectorLabel = new Label
            {
                Text = "Monitor",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top
            };
            MonitorSelector = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Top
            };

            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                MonitorSelector.Items.Add(String.Format("Monitor {0}", i + 1));
            }
            ResetButton = new Button { Text = "Reset Position", Dock = DockStyle.Top };

            Label separator4 = new Label { Dock = DockStyle.Top, Height = 10 };
            MonitorSelector.SelectedIndex = 0;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            Controls.Add(MonitorSelector);
            Controls.Add(monitorSelectorLabel);
            Controls.Add(separator3);
            Controls.Add(TestNotificationButton);
            Controls.Add(separator2);
            Controls.Add(YSlider);
            Controls.Add(ySliderLabel);
            Controls.Add(separator1);
            Controls.Add(XSlider);
            Controls.Add(xSliderLabel);
            Controls.Add(ResetButton); 
            Controls.Add(separator4); 
            this.FormClosing += (sender, e) =>
            {
                e.Cancel = true;
                this.Hide();
            };
            ResetButton.Click += (sender, e) =>
            {

                XSlider.Value = 0;
                YSlider.Value = 0;

                MonitorSelector.SelectedIndex = 0;


                ProgramUtilities.SavePosition(
                    XSlider.Value,
                    YSlider.Value,
                    MonitorSelector.SelectedIndex
                );
            };
        }
    }
}

    #endregion

    #region NativeMethods Class

public class NativeMethods
{

    public const int SWP_NOSIZE = 0x0001;
    public const int SWP_NOZORDER = 0x0004;
    public const int SWP_SHOWWINDOW = 0x0040;
    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(
        IntPtr parentHandle,
        IntPtr hWndChildAfter,
        string className,
        string windowTitle
    );

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    public static extern IntPtr SetWindowPos(
        IntPtr hWnd,
        int hWndInsertAfter,
        int x,
        int Y,
        int cx,
        int cy,
        int wFlags
    );

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hwnd, ref Rectangle rectangle);
}

    #endregion

    #region ProgramUtilities Class

public class ProgramUtilities
{
    public static string GetNotificationTitle()
    {
        CultureInfo currentCulture = CultureInfo.CurrentUICulture;
        string languageCode = currentCulture.TwoLetterISOLanguageName;
        switch (languageCode)
        {
            case "en":
                return "New notification";
            case "fr":
                return "Nouvelle notification";
            case "es":
                return "Nueva notificación";
            case "ja":
                return "新しい通知";
            case "pt":
                return "Nova notificação";
            case "de":
                return "Neue Benachrichtigung";
            case "zh":
                return "新通知";
            case "it":
                return "Nuova notifica";
            case "pl":
                return "Nowe powiadomienie";
            case "sv":
                return "Ny avisering";
            case "da":
                return "Ny meddelelse";
            case "no":
                return "Ny melding";
            default:
                return null;
        }
    }

    public static Icon LoadIconFromBase64String(string base64String)
    {
        byte[] iconBytes = Convert.FromBase64String(base64String);
        using (MemoryStream stream = new MemoryStream(iconBytes))
        {
            return new Icon(stream);
        }
    }

    public static void SetStartup(bool enable)
    {
        RegistryKey startupKey = Registry.CurrentUser.OpenSubKey(
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
            true
        );

        if (enable)
        {
            startupKey.SetValue("NotificationPositioner", Application.ExecutablePath);
        }
        else
        {
            startupKey.DeleteValue("NotificationPositioner", false);
        }

        startupKey.Close();
    }
    public static bool IsStartupEnabled()
    {
        RegistryKey startupKey = Registry.CurrentUser.OpenSubKey(
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
            true
        );
        bool enabled = startupKey.GetValue("NotificationPositioner") != null;
        startupKey.Close();
        return enabled;
    }

    public static void SavePosition(int x, int y, int monitorIndex)
    {
        RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\NotificationPositioner");
        key.SetValue("PositionX", x);
        key.SetValue("PositionY", y);
        key.SetValue("MonitorIndex", monitorIndex);
        key.Close();
    }

    public static void LoadPosition(out Point position, out int monitorIndex)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\NotificationPositioner");
        if (key == null)
        {
            position = new Point(Screen.PrimaryScreen.Bounds.Width - 300, 100);
            monitorIndex = 0;
            return;
        }

        int x = (int)key.GetValue("PositionX", Screen.PrimaryScreen.Bounds.Width - 300);
        int y = (int)key.GetValue("PositionY", 100);
        int mi = (int)key.GetValue("MonitorIndex", 0);
        key.Close();
        position = new Point(x, y);
        monitorIndex = mi;
    }

    public static bool IsLanguageSupported()
    {
        CultureInfo currentCulture = CultureInfo.CurrentUICulture;
        string languageCode = currentCulture.TwoLetterISOLanguageName;

        switch (languageCode)
        {
            case "en":
            case "fr":
            case "es":
            case "ja":
            case "pt":
            case "de":
            case "zh":
            case "it":
            case "pl":
            case "sv":
            case "da":
            case "no":
                return true;
            default:
                return false;
        }
    }
    public static void PositionNotification(IntPtr hwnd, int monitorIndex)
    {
        Rectangle monitorBounds = Screen.AllScreens[monitorIndex].Bounds;

        Rectangle NotifyRect = new Rectangle();
        NativeMethods.GetWindowRect(hwnd, ref NotifyRect);
        NotifyRect.Width = NotifyRect.Width - NotifyRect.X;
        NotifyRect.Height = NotifyRect.Height - NotifyRect.Y;

        int xPos = monitorBounds.Left + monitorBounds.Width - NotifyRect.Width;
        int yPos = monitorBounds.Top + 100;

        NativeMethods.SetWindowPos(
            hwnd,
            0,
            xPos,
            yPos,
            0,
            0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_SHOWWINDOW
        );
    }
    private static void GetPositionForMonitor(
        string notificationTitle,
        int monitorIndex,
        int xOffset,
        int yOffset,
        out int xPos,
        out int yPos
    )
    {
        xPos = 0;
        yPos = 0;

        Screen[] screens = Screen.AllScreens;
        if (monitorIndex >= 0 && monitorIndex < screens.Length)
        {
            Rectangle monitorBounds = screens[monitorIndex].Bounds;

            Rectangle notifyRect = new Rectangle();
            IntPtr hwnd = NativeMethods.FindWindow("Windows.UI.Core.CoreWindow", notificationTitle);
            NativeMethods.GetWindowRect(hwnd, ref notifyRect);
            notifyRect.Width = notifyRect.Width - notifyRect.X;
            notifyRect.Height = notifyRect.Height - notifyRect.Y;

            xPos = monitorBounds.Left + monitorBounds.Width - notifyRect.Width - xOffset;
            yPos = monitorBounds.Top + monitorBounds.Height - notifyRect.Height - yOffset - 100;
        }
    }
    public static void ShowTestNotification()
    {
        NotifyIcon testNotification = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Visible = true,
            BalloonTipTitle = "Test Notification",
            BalloonTipText = "This is a test notification.",
            BalloonTipIcon = ToolTipIcon.Info
        };

        testNotification.ShowBalloonTip(3000);

        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer
        {
            Interval = 3000,
            Enabled = true
        };

        timer.Tick += (sender, e) =>
        {
            testNotification.Visible = false;
            ((System.Windows.Forms.Timer)sender).Stop();
        };
    }

    #endregion

    #region Main Method
    public static void Main(string[] args)
    {
        bool createdNew;
        using (Mutex mutex = new Mutex(true, "NotificationPositioner", out createdNew))
        {
            if (createdNew)
            {
                if (!IsLanguageSupported())
                {
                    MessageBox.Show(
                        "Your system language is not supported. The application will now exit.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Program.PositionForm positionForm = new Program.PositionForm();
                string notificationTitle = GetNotificationTitle();

                ManualResetEvent exitSignal = new ManualResetEvent(false);
                ContextMenuStrip contextMenu = new ContextMenuStrip();
                ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit");
                ToolStripMenuItem positionNotificationMenuItem = new ToolStripMenuItem(
                    "Position Notification"
                );
                Point initialPosition;
                int initialMonitorIndex;
                ProgramUtilities.LoadPosition(out initialPosition, out initialMonitorIndex);

                positionForm.XSlider.Value = initialPosition.X;
                positionForm.YSlider.Value = initialPosition.Y;
                positionForm.MonitorSelector.SelectedIndex = initialMonitorIndex;
                positionForm.XSlider.ValueChanged += (sender, e) =>
                {
                    ProgramUtilities.SavePosition(
                        positionForm.XSlider.Value,
                        positionForm.YSlider.Value,
                        positionForm.MonitorSelector.SelectedIndex
                    );
                };

                positionForm.YSlider.ValueChanged += (sender, e) =>
                {
                    ProgramUtilities.SavePosition(
                        positionForm.XSlider.Value,
                        positionForm.YSlider.Value,
                        positionForm.MonitorSelector.SelectedIndex
                    );
                };

                positionForm.MonitorSelector.SelectedIndexChanged += (sender, e) =>
                {
                    ProgramUtilities.SavePosition(
                        positionForm.XSlider.Value,
                        positionForm.YSlider.Value,
                        positionForm.MonitorSelector.SelectedIndex
                    );
                };
                // icon base64
                string iconBase64String =
                    "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AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACgAAAAwAAAAYAAAAAEAIAAAAAAAACQAAAAAAAAAAAAAAAAAAAAAAABHKyT/SCsk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9IKyT/SCsk/0crJP9IKyT/SCsk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/SCsk/0grJP9HKyT/Ryok/0grJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0grJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKiT/Rysk/0crJP9IKyT/SCsk/0grJP9HKyT/SCsk/0grJP9IKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/SCsk/0grJP9HKyT/SCsk/0grJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/SCsk/0grJP9IKyT/SCsk/0grJP9IKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/SCsk/0grJP9IKyT/SCsk/0grJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/SCsk/0grJP9HKyT/SCsk/0crJP9IKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0grJP9HKyT/SCsk/0grJP9IKyT/Rysk/0crJP9HKyX/SCsk/0grJP9IKyT/Rysk/0crJP9IKyT/SCsk/0grJP9IKyT/SCsk/0cr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JP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0grJP9IKyT/SCsk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9IKyT/SCsk/0grJP9FKSP/Qygi/zsjHf8zHhn/LRoW/yoYFP8rGRX/KhkV/ywZFv8rGRX/KxkV/ysZFf8rGRX/MR0Z/zYgG/9DKCL/RSkj/0grJP9IKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9IKyT/SCsk/0crJP9IKyT/SCsk/0grJP9HKyT/Rysk/0grJP9HKyT/Rysk/0crJP9IKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/PyYg/zwjHv88JB7/OyQe/0AnIP89JB7/PCQe/z4lH/88JB7/RSkj/0cqJP9IKyT/SCsk/0crJP9HKyT/Rysk/0grJP9HKyT/Rysk/0grJP9HKyT/Rysk/0crJP9IKyT/SCsk/0crJP9HKyT/Rysk/0crJP9HKyT/SCsk/0crJP9HKyT/SCsk/0grJP9IKyT/Rysk/0crJP9IKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/SCsk/0grJP9HKyT/SCsk/0grJf9IKyT/SCsk/0grJP9IKyT/Rysk/0cqJP9HKyT/Rysk/0crJP9HKyT/Rysk/0grJP9IKyT/SCsk/0grJP9HKyT/SCsk/0grJP9IKyT/SCsk/0grJP9IKyT/Rysk/0crJP9IKyT/SCsk/0crJP9IKyT/Rysk/0crJP9IKyT/SCsk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9IKyT/SCsk/0crJP9HKyT/Rysk/0crJP9HKyT/SCsk/0crJP9HKyT/Rysk/0crJP9HKyT/SCsk/0crJP9HKyT/SCsk/0grJP9HKyT/SCsk/0crJP9HKyT/Rysk/0grJP9HKyT/SCsk/0grJP9IKyT/Rysk/0crJP9HKyT/SCsk/0crJP9IKyT/SCsk/0grJP9IKyT/SCsk/0crJP9HKyT/Rysk/0crJP9HKyT/SCsk/0crJP9HKyT/Rysk/0grJP9IKyT/SCsk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/SCsk/0grJP9HKyT/Rysk/0grJP9HKyT/Rysk/0crJP9IKyT/SCsk/0grJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9IKyT/Rysk/0crJP9HKyT/Rysk/0crJP9IKyT/SCsk/0grJP9IKyT/SCsk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP9HKyT/Rysk/0grJP9HKyT/Rysk/0crJP9HKyT/Rysk/0crJP8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
                NotifyIcon trayIcon = new NotifyIcon
                {
                    ContextMenuStrip = contextMenu,
                    Visible = true,
                    Icon = LoadIconFromBase64String(iconBase64String),
                    Text = "Notification Positioner"
                };
                ToolStripMenuItem launchOnStartupMenuItem = new ToolStripMenuItem(
                    "Launch on Windows Startup"
                ) {
                    Checked = IsStartupEnabled(),
                    CheckOnClick = true
                };

                launchOnStartupMenuItem.Click += (sender, e) =>
                {
                    ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
                    SetStartup(menuItem.Checked);
                };

                positionNotificationMenuItem.Click += (sender, e) =>
                {
                    positionForm.Show();
                    positionForm.BringToFront();
                };

                positionForm.TestNotificationButton.Click += (sender, e) =>
                {
                    ShowTestNotification();
                };

                CancellationTokenSource cts = new CancellationTokenSource();

                contextMenu.Items.Add(positionNotificationMenuItem);
                contextMenu.Items.Add(exitMenuItem);

                contextMenu.Items.Add(launchOnStartupMenuItem);
                contextMenu.Items.Add(exitMenuItem);
                trayIcon.ContextMenuStrip = contextMenu;

                BackgroundWorker bgWorker = new BackgroundWorker();
                bgWorker.WorkerReportsProgress = false;
                bgWorker.WorkerSupportsCancellation = true;

                bgWorker.DoWork += (sender, e) =>
                {
                    var token = cts.Token;

                    while (!token.IsCancellationRequested)
                    {
                        var hwnd = NativeMethods.FindWindow(
                            "Windows.UI.Core.CoreWindow",
                            notificationTitle
                        );

                        if (hwnd != IntPtr.Zero)
                        {
                            int monitorIndex = positionForm.MonitorSelector.SelectedIndex;

                            int xOffset = positionForm.XSlider.Value;
                            int yOffset = positionForm.YSlider.Value;

                            int xPos,
                                yPos;
                            GetPositionForMonitor(
                                notificationTitle,
                                monitorIndex,
                                xOffset,
                                yOffset,
                                out xPos,
                                out yPos
                            );

                            NativeMethods.RECT rect;
                            NativeMethods.GetWindowRect(hwnd, out rect);
                            int currentX = rect.Left;
                            int currentY = rect.Top;

                            if (currentX != xPos || currentY != yPos)
                            {
                                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_HIDE);

                                NativeMethods.SetWindowPos(
                                    hwnd,
                                    0,
                                    xPos,
                                    yPos,
                                    0,
                                    0,
                                    NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER
                                );
                                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOW);
                            }
                        }

                        Thread.Sleep(10);
                    }
                };

                exitMenuItem.Click += (sender, e) =>
                {
                    trayIcon.Visible = false;
                    cts.Cancel();
                    Application.Exit();
                    Environment.Exit(0);
                };

                bgWorker.RunWorkerAsync();
                Application.Run();
                mutex.ReleaseMutex();
            }
            else
            {
                MessageBox.Show(
                    "An instance of the application is already running.",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }
    }
    #endregion
}
