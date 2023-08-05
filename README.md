# Overview

This program is a Windows application that allows the user to control the position of desktop notifications. Users can customize the location of notifications by adjusting the X and Y sliders in the application. The program also has the option to launch on Windows startup.

Tested on Windows 11 Build 22621 & Windows 10 Build 19043

Key Features:

- Customizable position of desktop notifications
- Option to launch on Windows startup
- Test notification button to check the current position settings
- Compatible with multiple languages OS for notifications
- Choose the display monitor for notifications.

![150003](https://github.com/N3ars/NotificationPositioner/assets/41067116/d0a83b8b-c972-4ddc-8350-1ea725797724)



# Key Components

# PositionForm Class

This class represents the main form of the application. It contains UI components for adjusting the notification position and monitor selection, as well as event handlers for user interactions.

UI Components:

- XSlider: Horizontal slider for adjusting the X-axis position of notifications.
- YSlider: Vertical slider for adjusting the Y-axis position of notifications.
- MonitorSelector: ComboBox for selecting the display monitor for notifications.
- TestNotificationButton: Button for triggering a test notification.
- ResetButton: Button for resetting the notification position and monitor selection to their default values.

Event Handlers:

- OnDisplaySettingsChanged: Updates the sliders and monitor selector when the display settings change.
- FormClosing: Prevents the form from closing and hides it instead.
- ResetButton.Click: Resets the notification position and monitor selection to their default values.

NativeMethods Class

This class contains the P/Invoke declarations for interacting with native Windows API functions.
ProgramUtilities Class

This class contains utility methods for the application, including:

- GetNotificationTitle: Retrieves the notification title based on the current system language.
- LoadIconFromBase64String: Loads an application icon from a base64-encoded string.
- SetStartup: Enables or disables the application to launch on Windows startup.
- IsStartupEnabled: Checks if the application is set to launch on Windows startup.
- SavePosition: Saves the notification position and monitor selection settings.
- LoadPosition: Loads the saved notification position and monitor selection settings.
- IsLanguageSupported: Checks if the current system language is supported by the application.
- PositionNotification: Positions the notification on the specified monitor.
- ShowTestNotification: Displays a test notification.

# Main Method

The main method handles the application's initialization, UI setup, and event handling. It ensures that only a single instance of the application is running at a time. It also sets up a background worker to monitor for new notifications and position them according to the user's settings.

# Usage

To use the application, simply run the executable. The application will appear as a tray icon. Right-click the tray icon to access the context menu where you can adjust the position of notifications and enable/disable launching on Windows startup.

- Download the latest release from [here](https://github.com/N3ars/NotificationPositioner/releases/download/1.0/NotificationPositionerv1.exe)

# Compiling

- Clone the repo

- Run `start build.cmd` in the command prompt to compile it.

Note: You don't need to install any version of .net to compile this, the compiler is built into windows.

# Issues

If you encounter a problem such as being detected as a virus, make sure to add it to the exclusion list in the Windows antivirus program. This program uses low-level APIs call, Registry manipulation to save the position, and running in the background which could be associated with potentially malicious activities and flagged as a virus.

# Credit

This code is based on https://github.com/SamarthCat/notifications-at-top
