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

        public TrackBar OpacitySlider { get; private set; }
        public IntPtr notificationWindowHandle = IntPtr.Zero;

        public CheckBox ClickThroughCheckBox { get; private set; }

        public void SetClickThrough(IntPtr hwnd, bool enabled)
        {
            const int GWL_EXSTYLE = -20;
            const int WS_EX_TRANSPARENT = 0x20;

            int extendedStyle = NativeMethods.GetWindowLong(hwnd, GWL_EXSTYLE);
            if (enabled)
            {
                extendedStyle |= WS_EX_TRANSPARENT;
            }
            else
            {
                extendedStyle &= ~WS_EX_TRANSPARENT;
            }
            NativeMethods.SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle);
        }

        public void ResetNotificationWindowOpacity()
        {
            if (notificationWindowHandle != IntPtr.Zero)
                {
                    WindowOpacity.ApplyToWindow(notificationWindowHandle, 255); 
                }
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e) 
        {
            int maxWidth = 0;
            int maxHeight = 0;

            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.Bounds.Width > maxWidth)
                {
                    maxWidth = screen.Bounds.Width;
                }

                if (screen.Bounds.Height > maxHeight)
                {
                    maxHeight = screen.Bounds.Height;
                }
            }

            Screen selectedScreen = Screen.AllScreens[MonitorSelector.SelectedIndex];
            XSlider.Maximum = selectedScreen.Bounds.Width;
            YSlider.Maximum = selectedScreen.Bounds.Height;

            int savedIndex = MonitorSelector.SelectedIndex;
            MonitorSelector.Items.Clear();
            for (int i = 0; i < Screen.AllScreens.Length; i++) 
            {
                MonitorSelector.Items.Add(String.Format("Monitor {0}", i + 1));
            }
            if (savedIndex < MonitorSelector.Items.Count) 
            {
                MonitorSelector.SelectedIndex = savedIndex;
            } 
            else 
            {
                MonitorSelector.SelectedIndex = 0;
            }
            ProgramUtilities.SavePosition( XSlider.Value, YSlider.Value, MonitorSelector.SelectedIndex );
        }
        private void PositionForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (notificationWindowHandle != IntPtr.Zero)
            {
                SetClickThrough(notificationWindowHandle, false);
            }
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
            this.Text = "Notification Anywhere";
            this.Size = new Size(300, 400);
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimizeBox = false;
            this.TopMost = true;
            Label opacitySliderLabel = new Label
            {
                Text = "Opacity",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top
            };
            OpacitySlider = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 100, 
                TickStyle = TickStyle.None,
                Dock = DockStyle.Top
            };
            int savedOpacity = ProgramUtilities.LoadOpacity();
            OpacitySlider.Value = savedOpacity;
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

            Label separator1 = new Label { Dock = DockStyle.Top, Height = 5 };

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

            MonitorSelector.SelectedIndexChanged += (sender, e) => 
            {
                if (MonitorSelector.SelectedIndex >= 0 && MonitorSelector.SelectedIndex < Screen.AllScreens.Length)
                {
                    Screen selectedScreen = Screen.AllScreens[MonitorSelector.SelectedIndex];
                    XSlider.Maximum = selectedScreen.Bounds.Width;
                    YSlider.Maximum = selectedScreen.Bounds.Height;
                }
            };
            ClickThroughCheckBox = new CheckBox
            {
                Text = "Enable Click Through Notification",
                Dock = DockStyle.Top,
                Checked = false 
            };
            
            ResetButton = new Button { Text = "RESET", Dock = DockStyle.Top };

            Label separator4 = new Label { Dock = DockStyle.Top, Height = 5 };

            Label separator5 = new Label { Dock = DockStyle.Top, Height = 5 };

            MonitorSelector.SelectedIndex = 0;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            Controls.Add(TestNotificationButton);
            Controls.Add(separator2);
            Controls.Add(MonitorSelector);
            Controls.Add(monitorSelectorLabel);


            Controls.Add(ClickThroughCheckBox);
            Controls.Add(separator5); 
            Controls.Add(OpacitySlider);      
            Controls.Add(opacitySliderLabel);

            Controls.Add(separator3);

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
                Screen selectedScreen = Screen.AllScreens[MonitorSelector.SelectedIndex];
                Rectangle monitorBounds = selectedScreen.Bounds;

                XSlider.Value = monitorBounds.Width;
                YSlider.Value = monitorBounds.Height - 50;
                OpacitySlider.Value = 100; 
                
                ClickThroughCheckBox.Checked = false;
                ProgramUtilities.SaveClickThroughState(ClickThroughCheckBox.Checked);

                ProgramUtilities.SavePosition(XSlider.Value, YSlider.Value, MonitorSelector.SelectedIndex);
                ProgramUtilities.SaveOpacity(OpacitySlider.Value);
            };
            OpacitySlider.ValueChanged += (sender, e) =>
            {
                if (notificationWindowHandle != IntPtr.Zero)
                {
                    WindowOpacity.ApplyToWindow(notificationWindowHandle, (byte)(OpacitySlider.Value * 2.55));
                }
            };
            ClickThroughCheckBox.Checked = ProgramUtilities.LoadClickThroughState();
            
            ClickThroughCheckBox.CheckedChanged += (sender, e) =>
            {
                if (notificationWindowHandle != IntPtr.Zero)
                {
                    SetClickThrough(notificationWindowHandle, ClickThroughCheckBox.Checked);
                }
                ProgramUtilities.SaveClickThroughState(ClickThroughCheckBox.Checked);
            };

            this.FormClosing += PositionForm_FormClosing;

        }
    }
}

    #endregion

    #region NativeMethods Class

public class WindowOpacity
{
    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_LAYERED = 0x80000;
    public const int LWA_ALPHA = 0x2;

    public static void ApplyToWindow(IntPtr hwnd, byte opacity)
    {
        SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_LAYERED);
        SetLayeredWindowAttributes(hwnd, 0, opacity, LWA_ALPHA);
    }
}


public class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);


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
            case "ru":
                return "Новое уведомление";
            case "ar":
                return "إشعار جديد";
            case "hi":
                return "नई सूचना";
            case "ko":
                return "새로운 알림";
            default:
                return null;
        }
    }

    public static void SaveClickThroughState(bool enabled)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\NotificationAnywhere"))
        {
            key.SetValue("ClickThroughEnabled", enabled ? 1 : 0);
        }
    }

    public static bool LoadClickThroughState()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\NotificationAnywhere"))
        {
            if (key != null)
            {
                object value = key.GetValue("ClickThroughEnabled");
                return value != null && (int)value != 0;
            }
            return false;  
        }
    }

    public static void SaveOpacity(int value)
    {
        RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\NotificationAnywhere");
        key.SetValue("Opacity", value);
        key.Close();
    }

    public static int LoadOpacity()
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\NotificationAnywhere");
        if (key == null)
        {
            return 100; 
        }

        int opacity = (int)key.GetValue("Opacity", 100);
        key.Close();
        return opacity;
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
            startupKey.SetValue("NotificationAnywhere", Application.ExecutablePath);
        }
        else
        {
            startupKey.DeleteValue("NotificationAnywhere", false);
        }

        startupKey.Close();
    }
    public static bool IsStartupEnabled()
    {
        RegistryKey startupKey = Registry.CurrentUser.OpenSubKey(
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
            true
        );
        bool enabled = startupKey.GetValue("NotificationAnywhere") != null;
        startupKey.Close();
        return enabled;
    }

    public static void SavePosition(int x, int y, int monitorIndex)
    {
        RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\NotificationAnywhere");
        key.SetValue("PositionX", x);
        key.SetValue("PositionY", y);
        key.SetValue("MonitorIndex", monitorIndex);
        key.Close();
    }

    public static void LoadPosition(out Point position, out int monitorIndex)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\NotificationAnywhere");
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
            case "ru":
            case "ar":
            case "hi":
            case "ko":
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

    public static void AdjustNotificationPosition(IntPtr hwnd, int monitorIndex)
    {
        Rectangle monitorBounds = Screen.AllScreens[monitorIndex].Bounds;

        NativeMethods.RECT notificationRect;
        NativeMethods.GetWindowRect(hwnd, out notificationRect);
        int notificationWidth = notificationRect.Right - notificationRect.Left;
        int notificationHeight = notificationRect.Bottom - notificationRect.Top;

        int newXPos = Math.Min(monitorBounds.Right - notificationWidth, monitorBounds.Left);
        int newYPos = Math.Min(monitorBounds.Bottom - notificationHeight, monitorBounds.Top);

        NativeMethods.SetWindowPos(
            hwnd,
            0,
            newXPos,
            newYPos,
            0,
            0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_SHOWWINDOW
        );
    }

    #endregion

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
                ToolStripMenuItem positionNotificationMenuItem = new ToolStripMenuItem(
                    "Notification Option"
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
            
                // icon base64
                string iconBase64String =
                    "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DfDsgQz/7IMM/+2FC//thwv/7YgL/+6KCv/ujAr/7o0J/++PCf/vkQn/75MI//CUCP/wlgf/8ZgH//GaB//xmwb/8p0G//KfBf/yoAX/86IF//OkBP/zpgT/86YD8P+vDxAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADqfQ3Q638N/+yADf/sggz/7IQM/+2GC//thwv/7YkL/+6MDP/wmCP/86lD//S0WP/1uWH/9bph//W3V//0sEL/86Qi//GaB//xnAb/8p4G//KfBf/yoQX/86ME//OlBP/zpgT/86cD0AAAAAAAAAAAAAAAAAAAAAAAAAAA6XkNgOt8Dv/rfg3/7H8N/+yBDP/sgwz/7YUM/+2GC//vkiD/869Y//jOl//758z//vbr///8+f///Pn//vfs//zqzP/51Zr/9blW//KkG//ynQb/8p4F//KgBf/zogX/86QE//OlBP/0pwP/86kDgAAAAAAAAAAAAAAAAO9/DxDpeQ7w63sO/+t9Df/rfg3/7IAN/+yCDP/shAz/8Jcu//bEhv/87dr//fTn//reuf/4zpX/98eE//fHg//40JP/++C3//715//98Nr/+M2F//OrK//ynQb/8p8F//KhBf/zowT/86QE//OmBP/zpwPw/68PEAAAAAAAAAAA6nUPoOp4Dv/qeg7/63wO/+t9Df/rfw3/7IEM/++VLv/3ypf//fTo//vjxv/1u3P/8aE4/++UGP/vjwr/75AJ//CYFv/zqDb/9sBs//vmxP/+9un/+dWW//OqKv/yngb/8qAF//OiBf/zowT/86UE//SnA//zqAOgAAAAAAAAAADpdA/w6ncP/+p5Dv/rew7/63wN/+t+Df/uiiH/9b+D//3z6P/627f/8qdN/+6NFf/uigr/7owK/+6QDf/vkQ3/75EJ/++TCP/wmA//9LJJ//vgtP/+9un/+M2D//OlG//ynwX/8qEF//OiBf/zpAT/86YE//OnA/AAAAAA6HIPUOl0D//qdg//6ngP/+p6Dv/rew7/634O//GkUv/87Nr/++PI//GmUP/tiRH/7YcL/+6JCv/uiwv/+dep//rZrP/vkAn/75II//CUCP/wlw7/9LNK//znxv/979j/9btT//KeB//yoAX/8qEF//OjBP/zpQT/9KcE//WoA1Docg+g6XMQ/+l1D//qdw//6nkO/+t6Dv/tiCT/9sWS//306f/1u3v/7YgW/+2FDP/thgv/7YgL/+6KC//+9uz//vjw/++PCf/vkQn/75MI//CUCP/xmhH/98Vz//726v/505P/86Yd//KfBf/yoAX/86IF//OkBP/zpgT/9KYDkOhwD8DpchD/6XQP/+p2D//qeA//6nkO/++VPv/74sr/+t2///CaPv/sggz/7IQM/+2FC//thwv/7YkL//CYJv/wmSb/744J/++QCf/vkgj/8JMI//CVCP/zrDn/++O8//zoxv/0sj7/8p4G//KfBf/yoQX/86ME//OlBP/zpgPA528P0OlxEP/pcxD/6XUP/+p3D//qeA7/8J5P//306//3zaL/7Ycb/+yBDP/sgwz/7YQM/+2GC//tiAv/7ooL/+6LCv/ujQr/748J/++RCf/vkgj/8JQI//GeGv/51Jn//vTl//W5Uf/xnQb/8p4F//KgBf/zogX/86QE//KlBNDobxH/6XAQ/+lyEP/pdA//6nYP/+p3D//woVf//vv4//bFlP/rfg3/7IAN/+yCDP/sgwz/7YUL/+2HDP/++PD//vnz/+6MCv/ujgn/75AJ/++RCf/wkwj/8JcM//jNiP/++vP/9rxc//GcBv/ynQb/8p8F//KhBf/zowT/86QE/+huEf/obxH/6XEQ/+lzEP/pdQ//6nYP//CgVv/++/j/9sWV/+t+Dv/rfw3/7IEM/+yCDP/thAz/7YYM//748P/++fP/7osK/+6NCv/vjwn/75AJ/++SCP/wlw7/+M2K//758f/2u1v/8ZsG//GcBv/yngb/8qAF//KiBf/zowT/520R0OhuEf/ocBD/6XIQ/+l0EP/qdQ//8JtO//3y6P/4zqX/7IYf/+t+Df/sgA3/7IEM/+yDDP/thQz//vfw//759P/uigr/7owK/+6OCf/vjwn/75EJ//CcHf/51Jz//fPi//W3Uf/xmgf/8ZsG//KdBv/ynwX/8qEF//KhBs/naxHA6G0R/+hvEf/pcRD/6XMQ/+l0D//ukD3/+t7F//rfxf/vmUT/630N/+t/Df/sgA3/7IIM/+yEDf/+9/D//vn0/+6JCv/uiwr/7o0K/++OCf/vkAn/86o+//vkwf/75cL/8608//GZB//xmgb/8ZwG//KeBv/yoAX/8aAFwOdqE6DobBH/6G4R/+hwEP/pchD/6XMQ/+uBJP/1wJD//fPq//S5f//sghj/634N/+x/Df/sgQz/7IMN//727v/++PL/7YgL/+6KCv/ujAr/7o0J//CUFP/2w3j//vbr//jPkf/xnxz/8JgH//GZB//xmwb/8p0G//KfBf/ynwWQ6GwTUOdrEf/obRH/6G8R/+lxEP/pchD/6XUQ/++ZTf/75tL/++XQ//KmXP/sghb/634N/+yADf/sggz/98qW//fMmf/thwv/7YkL/+6LCv/vkBP/9LFW//zpzv/86tH/9LFN//CVCP/wlwf/8ZgH//GaBv/xnAb/8p4G//KfBlAAAAAA5moS8OhsEf/obhH/6HAQ/+lxEP/pcxD/6n4e//Ozef/98OT/+t7D//KnXf/shBr/7H8N/+yBDP/sgwz/7YQM/+2GC//tiAv/748U//OwWf/74sH//fPk//bCef/wmRf/8JQI//CWCP/wlwf/8ZkH//GbBv/wnAbvAAAAAAAAAADnaROg52sS/+htEf/obxH/6HAQ/+lyEP/pdA//7IYq//W9iv/98OT/++bR//W9hv/wnUf/7owk/+yFEv/shRH/75Ah//GkRf/2wYD//OnQ//3y5P/3yIj/8Zwm/++RCf/wkwj/8JUI//CWB//xmAf/8ZoH//CcBqAAAAAAAAAAAOdvFyDmaRLv6GwR/+huEf/obxH/6XEQ/+lzEP/pdQ//7IYq//OzeP/75dD//fTr//rhyP/40an/98ua//fLmf/506j/++PH//727f/86dH/9r53//CaJ//vjgn/75AJ/++SCP/wlAj/8JUI//CXB//wmAfv/58PEAAAAAAAAAAAAAAAAOdpEYDnaxL/6G0R/+huEf/ocBD/6XIQ/+l0EP/pdQ//64Ae/++bSv/1vYf/+t3A//3v4v/++PL//vjy//3w4f/637//9saK//KmSf/vkRj/7owK/+6NCf/vjwn/75EJ/++TCP/wlAj/8JYH//GXB4AAAAAAAAAAAAAAAAAAAAAA728fEOZpEs/obBH/6G0R/+hvEf/pcRD/6XMQ/+l0D//qdg//6ngP/+uCH//vkzn/8J5K//GjUf/xpFL/8aFJ/++aOP/ujx7/7YcL/+6JCv/uiwr/7owK/++OCf/vkAn/75II//CTCP/wlQjPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA728fEOZqEu/obBH/6G4R/+hwEP/pchD/6XMQ/+l1D//qdw//6ngO/+p6Dv/rfA7/634N/+x/Df/sgQz/7IMM/+2FDP/thgv/7YgL/+6KCv/uiwr/7o0K/++PCf/vkQn/7pII7++fDxAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA6WoVMOZqEe/obRH/6G8R/+lxEP/pchD/6XQP/+p2D//qdw//6nkO/+t7Dv/rfQ3/634N/+yADf/sggz/7IQM/+2FC//thwv/7YkL/+6KCv/ujAr/744J/+6QCe/vlAowAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA728fEOdsEc/obhH/6HAQ/+lxEP/pcxD/6XUP/+p2D//qeA7/6noO/+t8Dv/rfQ3/638N/+yBDP/sgwz/7YQM/+2GC//tiAv/7okK/+6LCv/tjQvP748PEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAOdtEYDnbhHv6HAQ/+lyEP/pdA//6nUP/+p3D//qeQ7/63sO/+t8Df/rfg3/7IAN/+yCDP/sgwz/7YUL/+2HC//shwvv7ooKfwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAOdvFyDobhGf6HEQ7+lzEP/pdA//6nYP/+p4D//qeg7/63sO/+t9Df/rfw3/7IEM/+yCDP/rhAzv7YYLn++PDxAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA6HIPUOhzEJ/odRC/6ncQz+p5Dv/reg7/63wOz+p+Db/rgA6f64EMTwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
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
                bgWorker.WorkerReportsProgress = true;
                bgWorker.WorkerSupportsCancellation = true;
                object lockObject = new object();
                positionForm.MonitorSelector.SelectedIndexChanged += (sender, e) => {
                    ProgramUtilities.SavePosition(
                        positionForm.XSlider.Value,
                        positionForm.YSlider.Value,
                        positionForm.MonitorSelector.SelectedIndex
                    );
                    bgWorker.ReportProgress(0);
                };

                bgWorker.ProgressChanged += (sender, e) => {
                    IntPtr hwnd = NativeMethods.FindWindow("Windows.UI.Core.CoreWindow", notificationTitle);
                    lock (lockObject) {
                        hwnd = positionForm.notificationWindowHandle;
                    }
                    if (hwnd != IntPtr.Zero) {
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
                            lock (lockObject) {
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
                                WindowOpacity.ApplyToWindow(hwnd, (byte)(positionForm.OpacitySlider.Value * 2.55));
                                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOW);
                            }
                        } else {
                            positionForm.notificationWindowHandle = IntPtr.Zero;
                            positionForm.SetClickThrough(IntPtr.Zero, false);
                        }

                        token.WaitHandle.WaitOne(1);
                    }
                };
                positionForm.OpacitySlider.ValueChanged += (sender, e) =>
                {
                    if (positionForm.notificationWindowHandle != IntPtr.Zero)
                    {
                        WindowOpacity.ApplyToWindow(positionForm.notificationWindowHandle, (byte)(positionForm.OpacitySlider.Value * 2.55));
                    }

                    ProgramUtilities.SaveOpacity(positionForm.OpacitySlider.Value);
                };

                exitMenuItem.Click += (sender, e) =>
                {
                    trayIcon.Visible = false;
                    cts.Cancel();
                    positionForm.ResetNotificationWindowOpacity(); 
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
