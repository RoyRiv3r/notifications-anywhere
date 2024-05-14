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
    /// 
    /// This class represents the form used to control the positioning and appearance of notifications.
    /// 
    public class PositionForm : Form
    {
        // The class name of the notification window.
        public const string notificationWindowClassName = "TeamsWebView";
        // public const string notificationWindowCaption = "Microsoft Teams";

        // An object used for locking to prevent race conditions.
        public object lockObject = new object();

        // A list of window handles for the notification windows.
        private List<IntPtr> notificationWindowHandles = new List<IntPtr>();

        // UI elements for controlling notification position and appearance.
        public Button ResetButton { get; private set; }
        public TrackBar XSlider { get; private set; }
        public TrackBar YSlider { get; private set; }
        public Button TestNotificationButton { get; private set; }
        public ComboBox MonitorSelector { get; private set; }
        public TrackBar OpacitySlider { get; private set; }

        // Window handles for the notification and Teams notification windows.
        public IntPtr notificationWindowHandle = IntPtr.Zero;
        public IntPtr teamsNotificationWindowHandle = IntPtr.Zero;

        // Checkbox for enabling click-through functionality.
        public CheckBox ClickThroughCheckBox { get; private set; }

        // Background worker for monitoring the notification windows.
        public BackgroundWorker monitorWorker;

        // Trackbars for controlling the horizontal and vertical position of Teams notifications.
        public TrackBar horizontalSlider;
        public TrackBar verticalSlider;

        /// 
        /// Constructor for the PositionForm class.
        /// 
        public PositionForm()
        {
            InitializeComponent();    // Initialize the UI components.
            InitializeMonitorWorker();    // Initialize the background worker for monitoring notifications.
            InitializeEventHandlers();   // Initialize event handlers for UI elements.
            LoadSettings();            // Load saved settings for notification position and appearance.
        }

        /// 
        /// Initializes the UI components of the PositionForm.
        /// 
        private void InitializeComponent()
        {
            // Set the form's properties.
            Text = "Notification Anywhere";
            Size = new Size(300, 490);
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.Fixed3D;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox = true;
            TopMost = true;

            // Create a TableLayoutPanel to organize the UI elements.
            var tableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                Padding = new Padding(2)
            };

            // Create the UI elements.
            ResetButton = new Button { Text = "RESET", Dock = DockStyle.Top, Margin = new Padding(2, 2, 2, 2) };
            OpacitySlider = CreateTrackBar(null, 0, 100, 100);
            XSlider = CreateTrackBar(null, -500, Screen.PrimaryScreen.Bounds.Width);
            YSlider = CreateTrackBar(null, -500, Screen.PrimaryScreen.Bounds.Height);
            MonitorSelector = CreateMonitorSelector();
            TestNotificationButton = new Button { Text = "Test Notification", Dock = DockStyle.Top, Margin = new Padding(2, 2, 2, 2) };
            horizontalSlider = CreateTrackBar(null, -500, GetMaxScreenWidth());
            verticalSlider = CreateTrackBar(null, -500, GetMaxScreenHeight());
            ClickThroughCheckBox = new CheckBox
            {
                Text = "Click Through Notification",
                Checked = false,
                Anchor = AnchorStyles.None,
                AutoSize = true,
                Margin = new Padding(1, 1, 1, 1)
            };

            // Add the UI elements to the TableLayoutPanel.
            AddControl(tableLayoutPanel, ResetButton);

            AddControl(tableLayoutPanel, CreateSeparator());
            AddControl(tableLayoutPanel, CreateLabel("Windows Notifications"));
            AddControl(tableLayoutPanel, YSlider);
            AddControl(tableLayoutPanel, XSlider);

            AddControl(tableLayoutPanel, CreateSeparator());
            AddControl(tableLayoutPanel, CreateLabel("Microsoft Teams"));
            AddControl(tableLayoutPanel, verticalSlider);
            AddControl(tableLayoutPanel, horizontalSlider);

            AddControl(tableLayoutPanel, CreateSeparator());
            AddControl(tableLayoutPanel, CreateLabel("Opacity"));
            AddControl(tableLayoutPanel, OpacitySlider);

            AddControl(tableLayoutPanel, CreateSeparator());
            AddControl(tableLayoutPanel, ClickThroughCheckBox);

            AddControl(tableLayoutPanel, CreateSeparator());
            AddControl(tableLayoutPanel, CreateLabel("Monitor"));
            AddControl(tableLayoutPanel, MonitorSelector);

            AddControl(tableLayoutPanel, TestNotificationButton);

            // Add the TableLayoutPanel to the form's controls.
            Controls.Add(tableLayoutPanel);
        }

        /// 
        /// Adds a control to the TableLayoutPanel.
        /// 
        /// 
        /// The TableLayoutPanel to add the control to.
        /// 
        /// The control to add to the TableLayoutPanel.
        /// 
        private void AddControl(TableLayoutPanel panel, Control control)
        {
            panel.RowCount++;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Margin = new Padding(1, 1, 1, 1);
            panel.Controls.Add(control, 0, panel.RowCount - 1);
        }

        /// 
        /// Initializes the background worker that monitors notification windows.
        /// 
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

        /// 
        /// Initializes the event handlers for the UI elements.
        /// 
        private void InitializeEventHandlers()
        {
            // Prevent the form from closing when the close button is clicked.
            FormClosing += (sender, e) =>
            {
                e.Cancel = true;
                Hide();
            };

            // Reset the notification position and appearance to default values when the Reset button is clicked.
            ResetButton.Click += (sender, e) =>
            {
                ResetSettings();
            };

            // Update the opacity of the notification window when the Opacity slider value changes.
            OpacitySlider.ValueChanged += (sender, e) =>
            {
                UpdateOpacity();
                ProgramUtilities.SaveOpacity(OpacitySlider.Value);
            };

            // Update the click-through functionality of the notification window when the Click Through checkbox is checked or unchecked.
            ClickThroughCheckBox.CheckedChanged += (sender, e) =>
            {
                UpdateClickThrough();
                ProgramUtilities.SaveClickThroughState(ClickThroughCheckBox.Checked);
            };

            // Save the notification position when the X or Y slider values change.
            XSlider.ValueChanged += (sender, e) =>
            {
                SavePosition();
            };

            YSlider.ValueChanged += (sender, e) =>
            {
                SavePosition();
            };

            // Update the slider maximum values and save the notification position when the selected monitor changes.
            MonitorSelector.SelectedIndexChanged += (sender, e) =>
            {
                SetSliderMaxValues();
                SavePosition();
                monitorWorker.ReportProgress(0);
            };

            // Update the position of Teams notifications when the horizontal or vertical slider values change.
            horizontalSlider.ValueChanged += (sender, e) =>
            {
                UpdateTeamsNotificationPosition();
            };

            verticalSlider.ValueChanged += (sender, e) =>
            {
                UpdateTeamsNotificationPosition();
            };
        }

        /// 
        /// Loads the saved settings for notification position and appearance.
        /// 
        private void LoadSettings()
        {
            Point initialPosition;
            int initialMonitorIndex;

            // Load the saved position and monitor index.
            ProgramUtilities.LoadPosition(out initialPosition, out initialMonitorIndex);

            // Set the initial values of the X, Y, and Monitor sliders.
            XSlider.Value = initialPosition.X;
            YSlider.Value = initialPosition.Y;
            MonitorSelector.SelectedIndex = initialMonitorIndex;

            // Set the maximum values for the X and Y sliders based on the selected monitor.
            SetSliderMaxValues();

            int initialHorizontalValue, initialVerticalValue;

            // Load the saved position of Teams notifications.
            ProgramUtilities.LoadTeamsPosition(out initialHorizontalValue, out initialVerticalValue);

            // Set the initial values of the horizontal and vertical sliders for Teams notifications.
            horizontalSlider.Value = initialHorizontalValue;
            verticalSlider.Value = initialVerticalValue;

            // Load the saved opacity and click-through state.
            OpacitySlider.Value = ProgramUtilities.LoadOpacity();
            ClickThroughCheckBox.Checked = ProgramUtilities.LoadClickThroughState();
        }

        /// 
        /// Resets the notification position and appearance to default values.
        /// 
        public void ResetSettings()
        {
            Screen selectedScreen = Screen.AllScreens[MonitorSelector.SelectedIndex];
            Rectangle monitorBounds = selectedScreen.Bounds;

            // Set the default values for the X, Y, and Opacity sliders.
            XSlider.Value = monitorBounds.Width - 100;
            YSlider.Value = monitorBounds.Height - 150;
            OpacitySlider.Value = 100;

            // Disable click-through and save the setting.
            ClickThroughCheckBox.Checked = false;
            ProgramUtilities.SaveClickThroughState(ClickThroughCheckBox.Checked);

            // Save the notification position and opacity.
            SavePosition();
            ProgramUtilities.SaveOpacity(OpacitySlider.Value);
        }

        /// 
        /// Resets the notification opacity and click-through state to default values.
        /// 
        public void ResetDefault()
        {
            // Set the opacity slider to its maximum value (100).
            OpacitySlider.Value = 100;

            // Disable click-through for the notification window.
            SetClickThrough(notificationWindowHandle, false);
        }

        /// 
        /// Creates a TrackBar control with the specified label, minimum, maximum, and initial value.
        /// 
        /// 
        /// The text of the label to display above the TrackBar (optional).
        /// 
        /// The minimum value of the TrackBar.
        /// 
        /// The maximum value of the TrackBar.
        /// 
        /// The initial value of the TrackBar.
        /// 
        /// Returns the created TrackBar control.
        /// 
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

        /// 
        /// Creates a ComboBox control for selecting the monitor to display notifications on.
        /// 
        /// Returns the created ComboBox control.
        /// 
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

        /// 
        /// Creates a Label control with the specified text.
        /// 
        /// 
        /// The text to display in the Label.
        /// 
        /// Returns the created Label control.
        /// 
        private Label CreateLabel(string text)
        {
            return new Label { Text = text, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Top };
        }

        /// 
        /// Creates a Panel control that acts as a separator.
        /// 
        /// Returns the created Panel control.
        /// 
        private Panel CreateSeparator()
        {
            return new Panel
            {
                Height = 1,
                Dock = DockStyle.Top,
                BackColor = Color.Gray
            };
        }

        /// 
        /// Gets the maximum screen width across all monitors.
        /// 
        /// Returns the maximum screen width.
        /// 
        private int GetMaxScreenWidth()
        {
            return Screen.AllScreens.Max(s => s.Bounds.Width);
        }

        /// 
        /// Gets the maximum screen height across all monitors.
        /// 
        /// Returns the maximum screen height.
        /// 
        private int GetMaxScreenHeight()
        {
            return Screen.AllScreens.Max(s => s.Bounds.Height);
        }

        /// 
        /// Sets the maximum values for the X and Y sliders based on the selected monitor.
        /// 
        private void SetSliderMaxValues()
        {
            Screen selectedScreen = Screen.AllScreens[MonitorSelector.SelectedIndex];
            XSlider.Maximum = selectedScreen.Bounds.Width;
            YSlider.Maximum = selectedScreen.Bounds.Height;
        }

        /// 
        /// Saves the current notification position to the registry.
        /// 
        private void SavePosition()
        {
            ProgramUtilities.SavePosition(
                XSlider.Value,
                YSlider.Value,
                MonitorSelector.SelectedIndex
            );
        }

        /// 
        /// Updates the opacity of the notification window.
        /// 
        private void UpdateOpacity()
        {
            if (notificationWindowHandle != IntPtr.Zero)
            {
                NativeMethods.ApplyToWindow(notificationWindowHandle, (byte)(OpacitySlider.Value * 2.55));
                NativeMethods.ApplyToWindow(teamsNotificationWindowHandle, (byte)(OpacitySlider.Value * 2.55));
            }
        }

        /// 
        /// Sets the click-through functionality of the specified window.
        /// 
        /// 
        /// The handle of the window to modify.
        /// 
        /// True to enable click-through, false to disable.
        /// 
        public void SetClickThrough(IntPtr hwnd, bool enabled)
        {
            int extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            extendedStyle = enabled ? extendedStyle | NativeMethods.WS_EX_TRANSPARENT : extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT;
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle);
        }

        /// 
        /// Updates the click-through functionality of the notification window.
        /// 
        private void UpdateClickThrough()
        {
            if (notificationWindowHandle != IntPtr.Zero)
            {
                SetClickThrough(notificationWindowHandle, ClickThroughCheckBox.Checked);
                SetClickThrough(teamsNotificationWindowHandle, ClickThroughCheckBox.Checked);
            }
        }

        /// 
        /// Background worker method that monitors notification windows and updates their positions.
        /// 
        /// 
        /// The sender of the event.
        /// 
        /// The event arguments.
        /// 
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

        /// 
        /// Event handler for the monitorWorker's ProgressChanged event. Updates the position of notification windows based on the current settings.
        /// 
        /// 
        /// The sender of the event.
        /// 
        /// The event arguments.
        /// 
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

        /// 
        /// Updates the position of the notification window to the specified coordinates.
        /// 
        /// 
        /// The X coordinate of the new position.
        /// 
        /// The Y coordinate of the new position.
        /// 
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

        /// 
        /// Updates the position of Teams notifications based on the current values of the horizontal and vertical sliders.
        /// 
        private void UpdateTeamsNotificationPosition()
        {
            UpdateNotificationPosition(horizontalSlider.Value, verticalSlider.Value);
        }

        /// 
        /// Event handler for the FormClosing event. Resets the opacity and click-through state of the notification window to default values.
        /// 
        /// 
        /// The sender of the event.
        /// 
        /// The event arguments.
        /// 
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
/// 
/// This class provides access to native Windows API functions.
/// 
public class NativeMethods
{
    /// 
    /// Delegate for the EnumWindows function.
    /// 
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    /// 
    /// Enumerates all top-level windows on the screen.
    /// 
    /// 
    /// A callback function that is called for each window found.
    /// 
    /// Application-defined data passed to the callback function.
    /// 
    /// Returns true if the function succeeds, false otherwise.
    /// 
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    /// 
    /// Retrieves the name of the class to which the specified window belongs.
    /// 
    /// 
    /// A handle to the window.
    /// 
    /// A StringBuilder to receive the class name.
    /// 
    /// The maximum number of characters to copy to the StringBuilder.
    /// 
    /// Returns the number of characters copied to the StringBuilder, not including the terminating null character.
    /// 
    [DllImport("user32.dll")]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    /// 
    /// Retrieves a handle to the top-level window whose class name and window name match the specified strings.
    /// 
    /// 
    /// The class name of the window to find.
    /// 
    /// The window name of the window to find.
    /// 
    /// Returns a handle to the window if found, otherwise returns IntPtr.Zero.
    /// 
    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    /// 
    /// Retrieves the dimensions of the bounding rectangle of the specified window.
    /// 
    /// 
    /// A handle to the window.
    /// 
    /// A RECT structure that receives the dimensions of the bounding rectangle.
    /// 
    /// Returns true if the function succeeds, false otherwise.
    /// 
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    /// 
    /// Changes the size, position, and Z order of a window.
    /// 
    /// 
    /// A handle to the window.
    /// 
    /// A handle to the window to precede the positioned window in the Z order.
    /// 
    /// The new position of the left side of the window, in client coordinates.
    /// 
    /// The new position of the top of the window, in client coordinates.
    /// 
    /// The new width of the window, in pixels.
    /// 
    /// The new height of the window, in pixels.
    /// 
    /// The window sizing and positioning flags.
    /// 
    /// Returns true if the function succeeds, false otherwise.
    /// 
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    // Constants for use with the SetWindowPos function.
    public const int HWND_TOPMOST = -1;
    public const int SWP_NOACTIVATE = 0x0010;
    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;
    public const int SWP_NOSIZE = 0x0001;
    public const int SWP_NOZORDER = 0x0004;

    /// 
    /// Changes an attribute of the specified window.
    /// 
    /// 
    /// A handle to the window.
    /// 
    /// The zero-based offset to the value to be set.
    /// 
    /// The replacement value.
    /// 
    /// Returns the previous value of the specified 32-bit integer.
    /// 
    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    /// 
    /// Retrieves information about the specified window.
    /// 
    /// 
    /// A handle to the window.
    /// 
    /// The zero-based offset to the value to be retrieved.
    /// 
    /// Returns the requested value.
    /// 
    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    /// 
    /// Sets the opacity and transparency color key of a layered window.
    /// 
    /// 
    /// A handle to the layered window.
    /// 
    /// The transparency color key.
    /// 
    /// The opacity of the layered window.
    /// 
    /// The layering flags.
    /// 
    /// Returns true if the function succeeds, false otherwise.
    /// 
    [DllImport("user32.dll")]
    public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    // Constants for use with the SetWindowLong and SetLayeredWindowAttributes functions.
    public const int WS_EX_TRANSPARENT = 0x20;
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_LAYERED = 0x80000;
    public const int LWA_ALPHA = 0x2;

    /// 
    /// Applies the specified opacity to the specified window.
    /// 
    /// 
    /// A handle to the window.
    /// 
    /// The opacity to apply to the window.
    /// 
    public static void ApplyToWindow(IntPtr hwnd, byte opacity)
    {
        SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_LAYERED);
        SetLayeredWindowAttributes(hwnd, 0, opacity, LWA_ALPHA);
    }

    public const int SWP_SHOWWINDOW = 0x0040;

    /// 
    /// Defines the coordinates of the upper-left and lower-right corners of a rectangle.
    /// 
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// 
    /// Sets the specified window's show state.
    /// 
    /// 
    /// A handle to the window.
    /// 
    /// Controls how the window is to be shown.
    /// 
    /// Returns true if the function succeeds, false otherwise.
    /// 
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// 
    /// Changes the size, position, and Z order of a window.
    /// 
    /// 
    /// A handle to the window.
    /// 
    /// A handle to the window to precede the positioned window in the Z order.
    /// 
    /// The new position of the left side of the window, in client coordinates.
    /// 
    /// The new position of the top of the window, in client coordinates.
    /// 
    /// The new width of the window, in pixels.
    /// 
    /// The new height of the window, in pixels.
    /// 
    /// The window sizing and positioning flags.
    /// 
    /// Returns the window's previous position if the function succeeds, otherwise returns IntPtr.Zero.
    /// 
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
/// 
/// This class contains utility methods for the application.
/// 
public class ProgramUtilities
{
    private static readonly string registryKeyPath = @"Software\NotificationAnywhere";

    /// 
    /// Checks if the current system language is supported by the application.
    /// 
    /// Returns true if the language is supported, false otherwise.
    /// 
    public static bool IsLanguageSupported()
    {
        CultureInfo currentCulture = CultureInfo.CurrentUICulture;
        string languageCode = currentCulture.TwoLetterISOLanguageName;

        string[] supportedLanguages = { "en", "fr", "es", "ja", "pt", "de", "zh", "it", "pl", "sv", "da", "no", "ru", "ar", "hi", "ko" };
        return supportedLanguages.Contains(languageCode);
    }

    /// 
    /// Gets the appropriate notification title based on the current system language.
    /// 
    /// Returns the notification title for the current language, or null if the language is not supported.
    /// 
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

    /// 
    /// Saves the position of Teams notifications to the registry.
    /// 
    /// 
    /// The horizontal position of Teams notifications.
    /// 
    /// The vertical position of Teams notifications.
    /// 
    public static void SaveTeamsPosition(int horizontalValue, int verticalValue)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryKeyPath))
        {
            key.SetValue("TeamsHorizontalPosition", horizontalValue);
            key.SetValue("TeamsVerticalPosition", verticalValue);
        }
    }

    /// 
    /// Loads the saved position of Teams notifications from the registry.
    /// 
    /// 
    /// The horizontal position of Teams notifications.
    /// 
    /// The vertical position of Teams notifications.
    /// 
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

    /// 
    /// Saves the click-through state of notifications to the registry.
    /// 
    /// 
    /// True to enable click-through, false to disable.
    /// 
    public static void SaveClickThroughState(bool enabled)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryKeyPath))
        {
            key.SetValue("ClickThroughEnabled", enabled ? 1 : 0);
        }
    }

    /// 
    /// Loads the click-through state of notifications from the registry.
    /// 
    /// Returns true if click-through is enabled, false otherwise.
    /// 
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


    /// 
    /// Saves the notification opacity to the registry.
    /// 
    /// 
    /// The opacity value to save.
    /// 
    public static void SaveOpacity(int value)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryKeyPath))
        {
            key.SetValue("Opacity", value);
        }
    }

    /// 
    /// Loads the notification opacity from the registry.
    /// 
    /// Returns the saved opacity value, or 100 if no value is found.
    /// 
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

    /// 
    /// Loads an Icon from a base64-encoded string.
    /// 
    /// <param name="base64String">The base64-encoded string representing the icon.</param>
    /// <returns>The Icon object loaded from the base64 string.</returns>
    /// 
    public static Icon LoadIconFromBase64String(string base64String)
    {
        byte[] iconBytes = Convert.FromBase64String(base64String);
        using (MemoryStream stream = new MemoryStream(iconBytes))
        {
            return new Icon(stream);
        }
    }

    /// 
    /// Sets or removes the application from Windows startup.
    /// 
    /// <param name="enable">True to enable startup, false to disable.</param>
    /// 
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

    /// 
    /// Checks if the application is set to launch on Windows startup.
    /// 
    /// <returns>True if startup is enabled, false otherwise.</returns>
    /// 
    public static bool IsStartupEnabled()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
        {
            return key != null && key.GetValue("NotificationAnywhere") != null;
        }
    }

    /// 
    /// Saves the notification position to the registry.
    /// 
    /// <param name="x">The X coordinate of the notification position.</param>
    /// <param name="y">The Y coordinate of the notification position.</param>
    /// <param name="monitorIndex">The index of the monitor to display the notification on.</param>
    /// 
    public static void SavePosition(int x, int y, int monitorIndex)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryKeyPath))
        {
            key.SetValue("PositionX", x);
            key.SetValue("PositionY", y);
            key.SetValue("MonitorIndex", monitorIndex);
        }
    }

    /// 
    /// Loads the notification position from the registry.
    /// 
    /// <param name="position">The Point object to store the loaded notification position.</param>
    /// <param name="monitorIndex">The integer to store the index of the monitor to display the notification on.</param>
    /// 
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

    /// 
    /// Calculates the notification position for the specified monitor and offsets.
    /// 
    /// <param name="notificationTitle">The title of the notification window.</param>
    /// <param name="monitorIndex">The index of the monitor to display the notification on.</param>
    /// <param name="xOffset">The X offset for the notification position.</param>
    /// <param name="yOffset">The Y offset for the notification position.</param>
    /// <param name="xPos">The integer to store the calculated X coordinate of the notification position.</param>
    /// <param name="yPos">The integer to store the calculated Y coordinate of the notification position.</param>
    /// 
    public static void GetPositionForMonitor(string notificationTitle, int monitorIndex, int xOffset, int yOffset, out int xPos, out int yPos)
    {
        xPos = 0;
        yPos = 0;

        // Adjustable buffer for off-screen movement
        int buffer = 100;

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

                xPos = monitorBounds.Left + xOffset - 300;
                xPos = Math.Max(monitorBounds.Left - 300 - buffer, Math.Min(xPos, monitorBounds.Right - notificationSize.Width + 300 + buffer));

                int midPoint = monitorBounds.Height / 2;
                if (yOffset < midPoint)
                {
                    yPos = monitorBounds.Top + yOffset - buffer;
                    yPos = Math.Max(monitorBounds.Top - buffer, yPos);
                }
                else
                {
                    yPos = monitorBounds.Bottom - (monitorBounds.Height - yOffset) - notificationSize.Height + buffer;
                    yPos = Math.Min(monitorBounds.Bottom - notificationSize.Height + buffer, yPos);
                }
            }
        }
    }

    /// 
    /// Displays a test notification balloon.
    /// 
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
    /// 
    /// The main entry point for the application.
    /// 
    /// <param name="args">Command line arguments.</param>
    /// 
    public static void Main(string[] args)
    {
        // Ensures only one instance of the application is running at a time.
        bool createdNew;
        using (Mutex mutex = new Mutex(true, "NotificationAnywhere", out createdNew))
        {
            if (createdNew)
            {
                // Checks if the current system language is supported.
                if (!ProgramUtilities.IsLanguageSupported())
                {
                    MessageBox.Show(
                    "Your system language is not supported. The application will now exit.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                    );
                    return;
                }
                // Enables visual styles and sets compatible text rendering.
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Creates an instance of the PositionForm.
                Program.PositionForm positionForm = new Program.PositionForm();

                // Gets the appropriate notification title based on the current system language.
                string notificationTitle = ProgramUtilities.GetNotificationTitle();

                // Creates a manual reset event for signaling application exit.
                ManualResetEvent exitSignal = new ManualResetEvent(false);

                // Creates a context menu for the tray icon.
                ContextMenuStrip contextMenu = new ContextMenuStrip();

                // Creates a menu item for exiting the application.
                ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit");

                // Creates a menu item for opening the notification options.
                ToolStripMenuItem positionNotificationMenuItem = new ToolStripMenuItem("Notification Option");

                // Base64 encoded string representing the application's icon.
                string iconBase64String = "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DfDsgQz/7IMM/+2FC//thwv/7YgL/+6KCv/ujAr/7o0J/++PCf/vkQn/75MI//CUCP/wlgf/8ZgH//GaB//xmwb/8p0G//KfBf/yoAX/86IF//OkBP/zpgT/86YD8P+vDxAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADqfQ3Q638N/+yADf/sggz/7IQM/+2GC//thwv/7YkL/+6MDP/wmCP/86lD//S0WP/1uWH/9bph//W3V//0sEL/86Qi//GaB//xnAb/8p4G//KfBf/yoQX/86ME//OlBP/zpgT/86cD0AAAAAAAAAAAAAAAAAAAAAAAAAAA6XkNgOt8Dv/rfg3/7H8N/+yBDP/sgwz/7YUM/+2GC//vkiD/869Y//jOl//758z//vbr///8+f///Pn//vfs//zqzP/51Zr/9blW//KkG//ynQb/8p4F//KgBf/zogX/86QE//OlBP/0pwP/86kDgAAAAAAAAAAAAAAAAO9/DxDpeQ7w63sO/+t9Df/rfg3/7IAN/+yCDP/shAz/8Jcu//bEhv/87dr//fTn//reuf/4zpX/98eE//fHg//40JP/++C3//715//98Nr/+M2F//OrK//ynQb/8p8F//KhBf/zowT/86QE//OmBP/zpwPw/68PEAAAAAAAAAAA6nUPoOp4Dv/qeg7/63wO/+t9Df/rfw3/7IEM/++VLv/3ypf//fTo//vjxv/1u3P/8aE4/++UGP/vjwr/75AJ//CYFv/zqDb/9sBs//vmxP/+9un/+dWW//OqKv/yngb/8qAF//OiBf/zowT/86UE//SnA//zqAOgAAAAAAAAAADpdA/w6ncP/+p5Dv/rew7/63wN/+t+Df/uiiH/9b+D//3z6P/627f/8qdN/+6NFf/uigr/7owK/+6QDf/vkQ3/75EJ/++TCP/wmA//9LJJ//vgtP/+9un/+M2D//OlG//ynwX/8qEF//OiBf/zpAT/86YE//OnA/AAAAAA6HIPUOl0D//qdg//6ngP/+p6Dv/rew7/634O//GkUv/87Nr/++PI//GmUP/tiRH/7YcL/+6JCv/uiwv/+dep//rZrP/vkAn/75II//CUCP/wlw7/9LNK//znxv/979j/9btT//KeB//yoAX/8qEF//OjBP/zpQT/9KcE//WoA1Docg+g6XMQ/+l1D//qdw//6nkO/+t6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";


                // Creates a tray icon for the application.
                NotifyIcon trayIcon = new NotifyIcon
                {
                    ContextMenuStrip = contextMenu,
                    Visible = true,
                    Icon = ProgramUtilities.LoadIconFromBase64String(iconBase64String),
                    Text = "Notification Option"
                };

                // Handles mouse clicks on the tray icon.
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

                // Creates a menu item for enabling or disabling launch on Windows startup.
                ToolStripMenuItem launchOnStartupMenuItem = new ToolStripMenuItem("Launch on Windows Startup")
                {
                    Checked = ProgramUtilities.IsStartupEnabled(),
                    CheckOnClick = true
                };

                // Handles clicks on the "Launch on Windows Startup" menu item.
                launchOnStartupMenuItem.Click += (sender, e) =>
                {
                    ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
                    ProgramUtilities.SetStartup(menuItem.Checked);
                };

                // Handles clicks on the "Notification Option" menu item.
                positionNotificationMenuItem.Click += (sender, e) =>
                {
                    positionForm.Show();
                    positionForm.BringToFront();
                };

                // Handles clicks on the "Test Notification" button in the PositionForm.
                positionForm.TestNotificationButton.Click += (sender, e) =>
                {
                    ProgramUtilities.ShowTestNotification();
                };

                // Creates a cancellation token source for canceling the background worker.
                CancellationTokenSource cts = new CancellationTokenSource();

                // Adds the menu items to the context menu.
                contextMenu.Items.Add(positionNotificationMenuItem);
                contextMenu.Items.Add(exitMenuItem);
                contextMenu.Items.Add(launchOnStartupMenuItem);
                contextMenu.Items.Add(exitMenuItem);
                trayIcon.ContextMenuStrip = contextMenu;

                // Creates a background worker for monitoring and repositioning notifications.
                BackgroundWorker bgWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };

                // Creates an object for locking shared resources.
                object lockObject = new object();

                // Handles the background worker's ProgressChanged event, which occurs when a notification is detected.
                bgWorker.ProgressChanged += (sender, e) =>
                {
                    // Gets the handle of the notification window.
                    IntPtr hwnd = NativeMethods.FindWindow("Windows.UI.Core.CoreWindow", notificationTitle);

                    // Locks the lockObject to prevent race conditions.
                    lock (lockObject)
                    {
                        // Updates the position form's notification window handle.
                        hwnd = positionForm.notificationWindowHandle;
                    }

                    // If a notification window handle is found:
                    if (hwnd != IntPtr.Zero)
                    {
                        // Gets the selected monitor index, X offset, and Y offset from the position form.
                        int monitorIndex = positionForm.MonitorSelector.SelectedIndex;
                        int xOffset = positionForm.XSlider.Value;
                        int yOffset = positionForm.YSlider.Value;

                        // Calculates the new X and Y position for the notification window based on the selected monitor and offsets.
                        int xPos, yPos;
                        ProgramUtilities.GetPositionForMonitor(
                            notificationTitle,
                            monitorIndex,
                            xOffset,
                            yOffset,
                            out xPos,
                            out yPos
                        );

                        // Temporarily hides the notification window.
                        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_HIDE);

                        // Repositions the notification window to the calculated coordinates.
                        NativeMethods.SetWindowPos(
                            hwnd,
                            0,
                            xPos,
                            yPos,
                            0,
                            0,
                            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER
                        );

                        // Shows the notification window again.
                        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOW);
                    }
                };

                // Defines the background worker's work method, which runs on a separate thread.
                bgWorker.DoWork += (sender, e) =>
                {
                    // Gets the cancellation token for the background worker.
                    var token = cts.Token;

                    // Loops until the cancellation token is signaled.
                    while (!token.IsCancellationRequested)
                    {
                        // Attempts to find a notification window with the specified title.
                        var hwnd = NativeMethods.FindWindow(
                            "Windows.UI.Core.CoreWindow",
                            notificationTitle
                        );

                        // If a notification window is found:
                        if (hwnd != IntPtr.Zero)
                        {
                            // Locks the lockObject to prevent race conditions.
                            lock (lockObject)
                            {
                                // Updates the position form's notification window handle.
                                positionForm.notificationWindowHandle = hwnd;

                                // Applies the click-through setting to the notification window.
                                positionForm.SetClickThrough(hwnd, positionForm.ClickThroughCheckBox.Checked);
                            }

                            // Gets the selected monitor index, X offset, and Y offset from the position form.
                            int monitorIndex = positionForm.MonitorSelector.SelectedIndex;
                            int xOffset = positionForm.XSlider.Value;
                            int yOffset = positionForm.YSlider.Value;

                            // Calculates the new X and Y position for the notification window based on the selected monitor and offsets.
                            int xPos, yPos;
                            ProgramUtilities.GetPositionForMonitor(
                                notificationTitle,
                                monitorIndex,
                                xOffset,
                                yOffset,
                                out xPos,
                                out yPos
                            );

                            // Gets the current position of the notification window.
                            NativeMethods.RECT rect;
                            NativeMethods.GetWindowRect(hwnd, out rect);
                            int currentX = rect.Left;
                            int currentY = rect.Top;

                            // If the calculated position is different from the current position:
                            if (currentX != xPos || currentY != yPos)
                            {
                                // Temporarily hides the notification window.
                                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_HIDE);

                                // Repositions the notification window to the calculated coordinates.
                                NativeMethods.SetWindowPos(
                                    hwnd,
                                    0,
                                    xPos,
                                    yPos,
                                    0,
                                    0,
                                    NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER
                                );

                                // Shows the notification window again.
                                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOW);
                            }

                            // Applies the opacity setting to the notification window.
                            byte opacity = (byte)(positionForm.OpacitySlider.Value * 2.55);
                            NativeMethods.ApplyToWindow(hwnd, opacity);
                        }
                        else
                        {
                            // If no notification window is found, resets the position form's notification window handle and click-through setting.
                            positionForm.notificationWindowHandle = IntPtr.Zero;
                            positionForm.SetClickThrough(IntPtr.Zero, false);
                        }

                        // Waits for 1 millisecond or until the cancellation token is signaled.
                        token.WaitHandle.WaitOne(1);
                    }
                };

                // Handles clicks on the "Exit" menu item.
                exitMenuItem.Click += (sender, e) =>
                {
                    // Hides the tray icon, cancels the background worker, resets the notification window to its default state, and exits the application.
                    trayIcon.Visible = false;
                    cts.Cancel();
                    positionForm.ResetDefault();
                    Application.Exit();
                    Environment.Exit(0);
                };

                // Starts the background worker.
                bgWorker.RunWorkerAsync();

                // Starts the application's message loop.
                Application.Run();

                // Releases the mutex to allow other instances of the application to run.
                mutex.ReleaseMutex();
            }
            else
            {
                // If another instance of the application is already running, displays a warning message.
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