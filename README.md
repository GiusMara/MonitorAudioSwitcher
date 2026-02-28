\# MonitorAudioSwitcher



Automatically switch your Windows audio output device based on the active monitor — no more manual switching every time you change screen.




---



!\[MonitorAudioSwitcher Screenshot](screenshot\_v0.0.0.png)



---



\## What it does



If you use multiple monitors — each connected to a different audio device (e.g. a TV with built-in speakers, a monitor with headphones, a soundbar) — MonitorAudioSwitcher automatically changes your Windows default audio output whenever you switch focus to a different screen.



\## Features



\- Automatically switches the default audio output based on the active monitor

\- Detects monitor plug/unplug events and reacts in real time

\- Saves your monitor-to-device configuration to a file — no need to reconfigure every time

\- Optional auto-start with Windows (enable it with a single checkbox)

\- Runs silently in the system tray with its own icon

\- Reopen the window at any time from the tray icon



\## Requirements



\- Windows 10 / 11 (x64)

\- No installation needed — single executable

\- Administrator privileges (required to change the default audio device)



\## Getting Started



\### Option A — Download the release \*(recommended)\*



Head to the \[Releases](https://github.com/GiusMara/MonitorAudioSwitcher/releases) page, download the latest `MonitorAudioSwitcher.exe` and run it as administrator. That's it.



\### Option B — Build from source



1\. Clone the repository:

&nbsp;  ```bash

&nbsp;  git clone https://github.com/GiusMara/MonitorAudioSwitcher.git

&nbsp;  cd MonitorAudioSwitcher

&nbsp;  ```

2\. Open the project in \*\*Visual Studio 2022\*\* or \*\*VS Code\*\* with the C# extension.

3\. Build in Release mode targeting x64:

&nbsp;  ```bash

&nbsp;  dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

&nbsp;  ```

4\. Run the generated `.exe` as administrator.



\## How to use



1\. Launch the app as administrator.

2\. The app will detect all connected monitors automatically.

3\. For each monitor, select the corresponding audio output device from the dropdown.

4\. Click \*\*"Start auto audio"\*\* to enable automatic switching.

5\. \*(Optional)\* Check \*\*"Start with Windows"\*\* to launch the app automatically at boot.

6\. Save your configuration — next time the app will remember your setup.

7\. The app minimizes to the system tray. Click the tray icon to reopen it at any time.



\## Roadmap



\- Friendlier monitor display names

\- Per-application audio routing (route different apps to different devices on separate monitors simultaneously)



\## Known Limitations



\- Requires running as administrator to change the default audio device via Windows API.

\- Monitor names may show as device IDs rather than friendly names \*(to be improved)\*.



\## Contributing



Contributions, issues and feature requests are welcome. Feel free to open an \[issue](https://github.com/GiusMara/MonitorAudioSwitcher/issues) or submit a pull request.



\## Support



If you find this project useful and want to support its development:



\[!\[Donate via PayPal](https://www.paypal.com/donate/?hosted\_button\_id=58HLSJN778Z4W)



\## License



This project is licensed under the \[MIT License](LICENSE).



