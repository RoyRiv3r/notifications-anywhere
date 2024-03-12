# Notification Anywhere

## Overview

**Notification Anywhere** is a lightweight Windows application that allows you to customize the position of Windows notifications. It gives you the ability to:

- **Position notifications on any monitor:** Choose the specific monitor where you want notifications to appear.
- **Fine-tune the position:** Adjust the horizontal and vertical offset of notifications using sliders.
- **Reset to default:** Easily reset the notification position to the default bottom-right corner.
- **Test notifications:** Send a test notification to see the positioning in action.
- **Launch on startup:** Optionally configure the application to launch automatically when Windows starts.

Tested on Windows 11 Build 22621 & Windows 10 Build 19043


## Why use Notification Anywhere?

- **Improved workflow:** Place notifications where they are most convenient for you, reducing distractions and improving productivity.
- **Multi-monitor setups:** Ideal for users with multiple monitors, allowing you to direct notifications to the screen you're currently using.
- **Customization:** Fine-tune the exact position of notifications to match your preferences.

## How it works

Notification Anywhere runs in the background and monitors for new Windows notifications. When a notification appears, it automatically repositions it based on your configured settings.

## Screenshots

![image](https://github.com/RoyRiv3r/notifications-anywhere/assets/41067116/317ba268-c553-4788-90c4-92ec68f34745)

## Installation and Usage

1. Download the latest release from the [Releases](https://github.com/RoyRiv3r/notifications-anywhere/releases/tag/1.1) page. 
2. Extract the downloaded ZIP file.
3. Run the `NotificationAnywhere.exe` executable.
4. Use the tray icon to access the settings and position the notifications.

# Compiling

- Clone the repo

- Run `start build.cmd` in the command prompt to compile it.

Note: You don't need to install any version of .net to compile this, the compiler is built into windows.

# Issues

If you encounter a problem such as being detected as a virus, make sure to add it to the exclusion list in the Windows antivirus program. This program uses low-level APIs call, Registry manipulation to save the position, and running in the background which could be associated with potentially malicious activities and flagged as a virus.

# Credit

This code is based on https://github.com/SamarthCat/notifications-at-top

## License

This project is licensed under the [MIT License]().

