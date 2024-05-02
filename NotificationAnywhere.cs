using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;



public class Program
{
    #region PositionForm Class
    public class PositionForm : Form
    {
        public const string notificationWindowClassName = "TeamsWebView";
        // public const string notificationWindowCaption = "Microsoft Teams";
        public object lockObject = new object();
        private List<IntPtr> notificationWindowHandles = new List<IntPtr>();
        public Button ResetButton { get; private set; }
        public TrackBar XSlider { get; private set; }
        public TrackBar YSlider { get; private set; }
        public Button TestNotificationButton { get; private set; }
        public ComboBox MonitorSelector { get; private set; }
        public TrackBar OpacitySlider { get; private set; }
        public IntPtr notificationWindowHandle = IntPtr.Zero;
        public IntPtr teamsNotificationWindowHandle = IntPtr.Zero;
        public CheckBox ClickThroughCheckBox { get; private set; }
        public BackgroundWorker monitorWorker;
        public TrackBar horizontalSlider;
        public TrackBar verticalSlider;
        public PositionForm()
        {
            InitializeComponent();
            InitializeMonitorWorker();
            InitializeEventHandlers();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = "Notification Anywhere";
            Size = new Size(300, 450);
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox = true;
            TopMost = true;

            ResetButton = new Button { Text = "RESET", Dock = DockStyle.Top };
            OpacitySlider = CreateTrackBar("Opacity", 0, 100, 100);
            XSlider = CreateTrackBar("Windows Notifications", -500, Screen.PrimaryScreen.Bounds.Width);
            YSlider = CreateTrackBar(null, -500, Screen.PrimaryScreen.Bounds.Height);
            MonitorSelector = CreateMonitorSelector();
            TestNotificationButton = new Button { Text = "Test Notification", Dock = DockStyle.Top };
            horizontalSlider = CreateTrackBar("Microsoft Teams", -500, GetMaxScreenWidth());
            verticalSlider = CreateTrackBar(null, -500, GetMaxScreenHeight());
            ClickThroughCheckBox = new CheckBox { Text = "Enable Click Through Notification", Dock = DockStyle.Top, Checked = false };

            Controls.AddRange(new Control[]
            {
            TestNotificationButton, CreateSeparator(),
            MonitorSelector, CreateLabel("Monitor"), ClickThroughCheckBox, CreateSeparator(),
            OpacitySlider, CreateLabel("Opacity"), CreateSeparator(),
            verticalSlider, horizontalSlider, CreateLabel("Microsoft Teams"), CreateSeparator(), YSlider,
            XSlider, CreateLabel("Windows Notifications"), ResetButton, CreateSeparator()
            });
        }

        private void InitializeMonitorWorker()
        {
            monitorWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            monitorWorker.DoWork += MonitorWorker_DoWork;
            monitorWorker.ProgressChanged += MonitorWorker_ProgressChanged;
            monitorWorker.RunWorkerAsync();
        }

        private void InitializeEventHandlers()
        {
            FormClosing += (sender, e) =>
            {
                e.Cancel = true;
                Hide();
            };

            ResetButton.Click += (sender, e) =>
            {
                ResetSettings();
            };

            OpacitySlider.ValueChanged += (sender, e) =>
            {
                UpdateOpacity();
                ProgramUtilities.SaveOpacity(OpacitySlider.Value);
            };

            ClickThroughCheckBox.CheckedChanged += (sender, e) =>
            {
                UpdateClickThrough();
                ProgramUtilities.SaveClickThroughState(ClickThroughCheckBox.Checked);
            };

            XSlider.ValueChanged += (sender, e) =>
            {
                SavePosition();
            };

            YSlider.ValueChanged += (sender, e) =>
            {
                SavePosition();
            };

            MonitorSelector.SelectedIndexChanged += (sender, e) =>
            {
                SetSliderMaxValues();
                SavePosition();
                monitorWorker.ReportProgress(0);
            };

            horizontalSlider.ValueChanged += (sender, e) =>
            {
                UpdateTeamsNotificationPosition();
            };

            verticalSlider.ValueChanged += (sender, e) =>
            {
                UpdateTeamsNotificationPosition();
            };
        }

        private void LoadSettings()
        {
            Point initialPosition;
            int initialMonitorIndex;
            ProgramUtilities.LoadPosition(out initialPosition, out initialMonitorIndex);

            XSlider.Value = initialPosition.X;
            YSlider.Value = initialPosition.Y;
            MonitorSelector.SelectedIndex = initialMonitorIndex;
            SetSliderMaxValues();

            int initialHorizontalValue, initialVerticalValue;
            ProgramUtilities.LoadTeamsPosition(out initialHorizontalValue, out initialVerticalValue);
            horizontalSlider.Value = initialHorizontalValue;
            verticalSlider.Value = initialVerticalValue;

            OpacitySlider.Value = ProgramUtilities.LoadOpacity();
            ClickThroughCheckBox.Checked = ProgramUtilities.LoadClickThroughState();
        }

        public void ResetSettings()
        {
            Screen selectedScreen = Screen.AllScreens[MonitorSelector.SelectedIndex];
            Rectangle monitorBounds = selectedScreen.Bounds;

            XSlider.Value = monitorBounds.Width;
            YSlider.Value = monitorBounds.Height - 50;
            OpacitySlider.Value = 100;

            ClickThroughCheckBox.Checked = false;
            ProgramUtilities.SaveClickThroughState(ClickThroughCheckBox.Checked);

            SavePosition();
            ProgramUtilities.SaveOpacity(OpacitySlider.Value);
        }

        public void ResetDefault()
        {
            OpacitySlider.Value = 100;
            SetClickThrough(notificationWindowHandle, false);
        }

        private TrackBar CreateTrackBar(string labelText, int minimum, int maximum, int value = 0)
        {
            TrackBar trackBar = new TrackBar
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = value,
                TickStyle = TickStyle.None,
                Dock = DockStyle.Top
            };

            if (!string.IsNullOrEmpty(labelText))
            {
                Label label = new Label
                {
                    Text = labelText,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Top
                };
                Controls.Add(label);
            }

            return trackBar;
        }

        private ComboBox CreateMonitorSelector()
        {
            ComboBox comboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Top
            };

            for (int i = 0; i < Screen.AllScreens.Length; i++)
                comboBox.Items.Add(string.Format("Monitor {0}", i + 1));

            return comboBox;
        }

        private Label CreateLabel(string text)
        {
            return new Label { Text = text, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Top };
        }

        private Panel CreateSeparator()
        {
            return new Panel
            {
                Height = 1,
                Dock = DockStyle.Top,
                BackColor = Color.Gray
            };
        }

        private int GetMaxScreenWidth()
        {
            return Screen.AllScreens.Max(s => s.Bounds.Width);
        }

        private int GetMaxScreenHeight()
        {
            return Screen.AllScreens.Max(s => s.Bounds.Height);
        }

        private void SetSliderMaxValues()
        {
            Screen selectedScreen = Screen.AllScreens[MonitorSelector.SelectedIndex];
            XSlider.Maximum = selectedScreen.Bounds.Width;
            YSlider.Maximum = selectedScreen.Bounds.Height;
        }

        private void SavePosition()
        {
            ProgramUtilities.SavePosition(
                XSlider.Value,
                YSlider.Value,
                MonitorSelector.SelectedIndex
            );
        }

        private void UpdateOpacity()
        {
            if (notificationWindowHandle != IntPtr.Zero)
            {
                NativeMethods.ApplyToWindow(notificationWindowHandle, (byte)(OpacitySlider.Value * 2.55));
                NativeMethods.ApplyToWindow(teamsNotificationWindowHandle, (byte)(OpacitySlider.Value * 2.55));
            }
        }

        public void SetClickThrough(IntPtr hwnd, bool enabled)
        {
            int extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            extendedStyle = enabled ? extendedStyle | NativeMethods.WS_EX_TRANSPARENT : extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT;
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle);
        }

        private void UpdateClickThrough()
        {
            if (notificationWindowHandle != IntPtr.Zero)
            {
                SetClickThrough(notificationWindowHandle, ClickThroughCheckBox.Checked);
                SetClickThrough(teamsNotificationWindowHandle, ClickThroughCheckBox.Checked);
            }
        }

        private void MonitorWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                notificationWindowHandles.Clear();
                NativeMethods.EnumWindows((hwnd, param) =>
                {
                    StringBuilder className = new StringBuilder(256);
                    NativeMethods.GetClassName(hwnd, className, className.Capacity);
                    if (className.ToString() == notificationWindowClassName)
                    {
                        NativeMethods.RECT rect;
                        NativeMethods.GetWindowRect(hwnd, out rect);
                        int width = rect.Right - rect.Left;
                        int height = rect.Bottom - rect.Top;
                        int[] validHeights = { 176, 252, 424, 444, 404, 520, 652, 692, 136, 272, 384, 308, 556 };
                        if (width == 372 && validHeights.Contains(height))
                        {
                            notificationWindowHandles.Add(hwnd);
                        }
                    }
                    return true;
                }, IntPtr.Zero);

                monitorWorker.ReportProgress(0, notificationWindowHandles.ToArray());
                Thread.Sleep(1);
            }
        }

        private void MonitorWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            IntPtr[] notificationWindowHandles = (IntPtr[])e.UserState;
            lock (lockObject)
            {
                int horizontalValue, verticalValue;
                ProgramUtilities.LoadTeamsPosition(out horizontalValue, out verticalValue);

                Screen selectedScreen = Screen.AllScreens[MonitorSelector.SelectedIndex];
                Rectangle monitorBounds = selectedScreen.Bounds;

                horizontalValue = Math.Max(Math.Min(horizontalValue, monitorBounds.Right), monitorBounds.Left);

                int currentVerticalPosition = verticalValue;

                if (notificationWindowHandles != null)
                {
                    foreach (IntPtr hwnd in notificationWindowHandles)
                    {
                        NativeMethods.RECT rect;
                        if (NativeMethods.GetWindowRect(hwnd, out rect))
                        {
                            int windowHeight = rect.Bottom - rect.Top;

                            verticalValue = Math.Max(Math.Min(currentVerticalPosition, monitorBounds.Bottom - windowHeight), monitorBounds.Top);

                            NativeMethods.SetWindowPos(hwnd, (IntPtr)NativeMethods.HWND_TOPMOST, horizontalValue, verticalValue, 0, 0, NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOSIZE);

                            currentVerticalPosition += windowHeight;
                        }
                    }
                }

                if (teamsNotificationWindowHandle != IntPtr.Zero)
                {
                    UpdateNotificationPosition(horizontalValue, verticalValue);
                }
            }
        }

        private void UpdateNotificationPosition(int x, int y)
        {
            Screen selectedScreen = Screen.AllScreens[MonitorSelector.SelectedIndex];
            Rectangle screenBounds = selectedScreen.Bounds;

            NativeMethods.RECT windowRect;
            NativeMethods.GetWindowRect(teamsNotificationWindowHandle, out windowRect);
            int windowWidth = windowRect.Right - windowRect.Left;
            int windowHeight = windowRect.Bottom - windowRect.Top;

            x = Math.Max(screenBounds.Left, Math.Min(x, screenBounds.Right - windowWidth));
            y = Math.Max(screenBounds.Top, Math.Min(y, screenBounds.Bottom - windowHeight));

            NativeMethods.SetWindowPos(teamsNotificationWindowHandle, (IntPtr)NativeMethods.HWND_TOPMOST, x, y, 0, 0, NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOSIZE);
            ProgramUtilities.SaveTeamsPosition(x, y);
        }

        private void UpdateTeamsNotificationPosition()
        {
            UpdateNotificationPosition(horizontalSlider.Value, verticalSlider.Value);
        }

        private void PositionForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (notificationWindowHandle != IntPtr.Zero)
            {
                NativeMethods.ApplyToWindow(notificationWindowHandle, 255);
                SetClickThrough(notificationWindowHandle, false);
            }
        }
    }

}
#endregion PositionForm Class
#region NativeMethods Class
public class NativeMethods
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public const int HWND_TOPMOST = -1;
    public const int SWP_NOACTIVATE = 0x0010;
    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;
    public const int SWP_NOSIZE = 0x0001;
    public const int SWP_NOZORDER = 0x0004;

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    public const int WS_EX_TRANSPARENT = 0x20;
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_LAYERED = 0x80000;
    public const int LWA_ALPHA = 0x2;

    public static void ApplyToWindow(IntPtr hwnd, byte opacity)
    {
        SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_LAYERED);
        SetLayeredWindowAttributes(hwnd, 0, opacity, LWA_ALPHA);
    }

    public const int SWP_SHOWWINDOW = 0x0040;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

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

}
#endregion NativeMethods Class
#region ProgramUtilities Class
public class ProgramUtilities
{
    private static readonly string registryKeyPath = @"Software\NotificationAnywhere";
    public static bool IsLanguageSupported()
    {
        CultureInfo currentCulture = CultureInfo.CurrentUICulture;
        string languageCode = currentCulture.TwoLetterISOLanguageName;

        string[] supportedLanguages = { "en", "fr", "es", "ja", "pt", "de", "zh", "it", "pl", "sv", "da", "no", "ru", "ar", "hi", "ko" };
        return supportedLanguages.Contains(languageCode);
    }

    public static string GetNotificationTitle()
    {
        CultureInfo currentCulture = CultureInfo.CurrentUICulture;
        string languageCode = currentCulture.TwoLetterISOLanguageName;

        Dictionary<string, string> notificationTitles = new Dictionary<string, string>
    {
        { "en", "New notification" },
        { "fr", "Nouvelle notification" },
        { "es", "Nueva notificación" },
        { "ja", "新しい通知" },
        { "pt", "Nova notificação" },
        { "de", "Neue Benachrichtigung" },
        { "zh", "新通知" },
        { "it", "Nuova notifica" },
        { "pl", "Nowe powiadomienie" },
        { "sv", "Ny avisering" },
        { "da", "Ny meddelelse" },
        { "no", "Ny melding" },
        { "ru", "Новое уведомление" },
        { "ar", "إشعار جديد" },
        { "hi", "नई सूचना" },
        { "ko", "새로운 알림" }
    };

        return notificationTitles.ContainsKey(languageCode) ? notificationTitles[languageCode] : null;
    }

    public static void SaveTeamsPosition(int horizontalValue, int verticalValue)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryKeyPath))
        {
            key.SetValue("TeamsHorizontalPosition", horizontalValue);
            key.SetValue("TeamsVerticalPosition", verticalValue);
        }
    }

    public static void LoadTeamsPosition(out int horizontalValue, out int verticalValue)
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryKeyPath))
        {
            if (key == null)
            {
                horizontalValue = Screen.PrimaryScreen.Bounds.Width - 300;
                verticalValue = 100;
                return;
            }
            horizontalValue = (int)key.GetValue("TeamsHorizontalPosition", Screen.PrimaryScreen.Bounds.Width - 300);
            verticalValue = (int)key.GetValue("TeamsVerticalPosition", 100);
        }
    }

    public static void SaveClickThroughState(bool enabled)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryKeyPath))
        {
            key.SetValue("ClickThroughEnabled", enabled ? 1 : 0);
        }
    }

    public static bool LoadClickThroughState()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryKeyPath, true))
        {
            if (key != null)
            {
                object value = key.GetValue("ClickThroughEnabled");
                if (value == null)
                {
                    key.SetValue("ClickThroughEnabled", 0);
                    return false;
                }
                return (int)value != 0;
            }
            return false;
        }
    }


    public static void SaveOpacity(int value)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryKeyPath))
        {
            key.SetValue("Opacity", value);
        }
    }

    public static int LoadOpacity()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryKeyPath))
        {
            if (key == null)
            {
                return 100;
            }
            return (int)key.GetValue("Opacity", 100);
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
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
        {
            if (enable)
            {
                key.SetValue("NotificationAnywhere", Application.ExecutablePath);
            }
            else
            {
                key.DeleteValue("NotificationAnywhere", false);
            }
        }
    }
    public static bool IsStartupEnabled()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
        {
            return key != null && key.GetValue("NotificationAnywhere") != null;
        }
    }

    public static void SavePosition(int x, int y, int monitorIndex)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryKeyPath))
        {
            key.SetValue("PositionX", x);
            key.SetValue("PositionY", y);
            key.SetValue("MonitorIndex", monitorIndex);
        }
    }

    public static void LoadPosition(out Point position, out int monitorIndex)
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryKeyPath))
        {
            if (key == null)
            {
                position = new Point(Screen.PrimaryScreen.Bounds.Width - 300, 100);
                monitorIndex = 0;
                return;
            }

            int x = (int)key.GetValue("PositionX", Screen.PrimaryScreen.Bounds.Width - 300);
            int y = (int)key.GetValue("PositionY", 100);
            monitorIndex = (int)key.GetValue("MonitorIndex", 0);
            position = new Point(x, y);
        }
    }

    public static void GetPositionForMonitor(
        string notificationTitle,
        int monitorIndex,
        int xOffset,
        int yOffset,
        out int xPos,
        out int yPos)
    {
        xPos = 0;
        yPos = 0;
        Screen[] screens = Screen.AllScreens;
        if (monitorIndex >= 0 && monitorIndex < screens.Length)
        {
            Rectangle monitorBounds = screens[monitorIndex].Bounds;

            IntPtr hwnd = NativeMethods.FindWindow("Windows.UI.Core.CoreWindow", notificationTitle);
            if (hwnd != IntPtr.Zero)
            {
                NativeMethods.RECT rect;
                NativeMethods.GetWindowRect(hwnd, out rect);
                Size notificationSize = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);

                bool anchorBottom = true;
                bool anchorRight = true;

                xPos = anchorRight ? monitorBounds.Right - (monitorBounds.Width - xOffset) - notificationSize.Width : monitorBounds.Left + (monitorBounds.Width - xOffset);
                yPos = anchorBottom ? monitorBounds.Bottom - (monitorBounds.Height - yOffset) - notificationSize.Height : monitorBounds.Top + yOffset;

                if (anchorRight && xPos < monitorBounds.Left)
                {
                    xPos = monitorBounds.Left;
                    anchorRight = false;
                }

                if (yPos < monitorBounds.Top)
                {
                    yPos = monitorBounds.Top;
                }
                else if (yPos + notificationSize.Height > monitorBounds.Bottom)
                {
                    anchorBottom = false;
                    yPos = monitorBounds.Top + yOffset;

                    if (yPos + notificationSize.Height > monitorBounds.Bottom)
                    {
                        yPos = monitorBounds.Top + (monitorBounds.Height - notificationSize.Height) / 2;
                    }
                }
            }
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

        testNotification.ShowBalloonTip(10000);

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

    #region Main Method
    public static void Main(string[] args)
    {
        bool createdNew;
        using (Mutex mutex = new Mutex(true, "NotificationAnywhere", out createdNew))
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
                ToolStripMenuItem positionNotificationMenuItem = new ToolStripMenuItem("Notification Option");

                string iconBase64String = "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DfDsgQz/7IMM/+2FC//thwv/7YgL/+6KCv/ujAr/7o0J/++PCf/vkQn/75MI//CUCP/wlgf/8ZgH//GaB//xmwb/8p0G//KfBf/yoAX/86IF//OkBP/zpgT/86YD8P+vDxAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADqfQ3Q638N/+yADf/sggz/7IQM/+2GC//thwv/7YkL/+6MDP/wmCP/86lD//S0WP/1uWH/9bph//W3V//0sEL/86Qi//GaB//xnAb/8p4G//KfBf/yoQX/86ME//OlBP/zpgT/86cD0AAAAAAAAAAAAAAAAAAAAAAAAAAA6XkNgOt8Dv/rfg3/7H8N/+yBDP/sgwz/7YUM/+2GC//vkiD/869Y//jOl//758z//vbr///8+f///Pn//vfs//zqzP/51Zr/9blW//KkG//ynQb/8p4F//KgBf/zogX/86QE//OlBP/0pwP/86kDgAAAAAAAAAAAAAAAAO9/DxDpeQ7w63sO/+t9Df/rfg3/7IAN/+yCDP/shAz/8Jcu//bEhv/87dr//fTn//reuf/4zpX/98eE//fHg//40JP/++C3//715//98Nr/+M2F//OrK//ynQb/8p8F//KhBf/zowT/86QE//OmBP/zpwPw/68PEAAAAAAAAAAA6nUPoOp4Dv/qeg7/63wO/+t9Df/rfw3/7IEM/++VLv/3ypf//fTo//vjxv/1u3P/8aE4/++UGP/vjwr/75AJ//CYFv/zqDb/9sBs//vmxP/+9un/+dWW//OqKv/yngb/8qAF//OiBf/zowT/86UE//SnA//zqAOgAAAAAAAAAADpdA/w6ncP/+p5Dv/rew7/63wN/+t+Df/uiiH/9b+D//3z6P/627f/8qdN/+6NFf/uigr/7owK/+6QDf/vkQ3/75EJ/++TCP/wmA//9LJJ//vgtP/+9un/+M2D//OlG//ynwX/8qEF//OiBf/zpAT/86YE//OnA/AAAAAA6HIPUOl0D//qdg//6ngP/+p6Dv/rew7/634O//GkUv/87Nr/++PI//GmUP/tiRH/7YcL/+6JCv/uiwv/+dep//rZrP/vkAn/75II//CUCP/wlw7/9LNK//znxv/979j/9btT//KeB//yoAX/8qEF//OjBP/zpQT/9KcE//WoA1Docg+g6XMQ/+l1D//qdw//6nkO/+t6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";

                NotifyIcon trayIcon = new NotifyIcon
                {
                    ContextMenuStrip = contextMenu,
                    Visible = true,
                    Icon = LoadIconFromBase64String(iconBase64String),
                    Text = "Notification Option"
                };

                trayIcon.MouseClick += (sender, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        if (positionForm.Visible)
                        {
                            positionForm.Hide();
                        }
                        else
                        {
                            positionForm.Show();
                            positionForm.BringToFront();
                        }
                    }
                };

                ToolStripMenuItem launchOnStartupMenuItem = new ToolStripMenuItem("Launch on Windows Startup")
                {
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

                BackgroundWorker bgWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };
                object lockObject = new object();

                bgWorker.ProgressChanged += (sender, e) =>
                {
                    IntPtr hwnd = NativeMethods.FindWindow("Windows.UI.Core.CoreWindow", notificationTitle);
                    lock (lockObject)
                    {
                        hwnd = positionForm.notificationWindowHandle;
                    }
                    if (hwnd != IntPtr.Zero)
                    {
                        int monitorIndex = positionForm.MonitorSelector.SelectedIndex;
                        int xOffset = positionForm.XSlider.Value;
                        int yOffset = positionForm.YSlider.Value;
                        int xPos, yPos;
                        ProgramUtilities.GetPositionForMonitor(
                            notificationTitle,
                            monitorIndex,
                            xOffset,
                            yOffset,
                            out xPos,
                            out yPos
                        );

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
                };

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
                            lock (lockObject)
                            {
                                positionForm.notificationWindowHandle = hwnd;
                                positionForm.SetClickThrough(hwnd, positionForm.ClickThroughCheckBox.Checked);
                            }
                            int monitorIndex = positionForm.MonitorSelector.SelectedIndex;
                            int xOffset = positionForm.XSlider.Value;
                            int yOffset = positionForm.YSlider.Value;

                            int xPos, yPos;
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
                            byte opacity = (byte)(positionForm.OpacitySlider.Value * 2.55);
                            NativeMethods.ApplyToWindow(hwnd, opacity);
                        }
                        else
                        {
                            positionForm.notificationWindowHandle = IntPtr.Zero;
                            positionForm.SetClickThrough(IntPtr.Zero, false);
                        }

                        token.WaitHandle.WaitOne(1);
                    }
                };

                exitMenuItem.Click += (sender, e) =>
                {
                    trayIcon.Visible = false;
                    cts.Cancel();
                    positionForm.ResetDefault();
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
    #endregion Main Method

}
#endregion ProgramUtilities Class