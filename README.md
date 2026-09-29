<div align="center">

<img src="Assets/ikona.svg" alt="makeBreak icon" width="128" height="128" />

# ⏳ makeBreak

**A desktop break-time manager that confirms when your break is over.**

A cross-platform desktop application that organizes and enforces break times while you work at the computer. Like *safeYourEyes*, but with a key difference: when a break finishes, you confirm it with a button — allowing the app to accurately track your **real work time** and **break overtime**.

C# / Avalonia / ReactiveUI / .NET 10

</div>

---

## ✨ Features

- **Break scheduling** — tracks work sessions and automatically triggers **short** and **long** breaks based on configured intervals.
- **Break screen with overtime counter** — a fullscreen view showing a live countdown until the break ends. Once the countdown completes:
  - the **"Break over"** button activates to confirm the end of your break;
  - an **overtime counter** (`+mm:ss` / `+h:mm:ss`) begins tracking any extra time spent before returning to work;
  - an on-screen **mute button** allows you to silence voice prompts for the current break session.
- **Voice alerts & multi-stage reminders** — plays spoken audio prompts when a break finishes to remind you to get back to work. If you remain on break, periodic reminders play every minute and escalate across three stages:
  - **Initial prompt** (`FirstPrompt`) — triggered as soon as the break timer ends;
  - **100% Overtime** (`SecondPrompt`) — triggered when overtime reaches the break's scheduled duration;
  - **200% Overtime** (`ThirdPrompt`) — escalated prompts when overtime exceeds double the break duration.
- **Relax mode** — toggleable via the system tray menu (`Relax`). Keeps break countdowns and reminders running so you don't forget eye breaks during leisure (browsing, videos, reading), while **pausing work-time tracking** so your productive statistics remain accurate.
- **System tray integration** — runs unobtrusively in the system tray, allowing you to:
  - open the **Settings** dialog;
  - view current interval progress;
  - toggle **Relax mode**;
  - **pause** / **resume** break scheduling;
  - quit the application.
- **Settings dialog** — full control over schedule, audio, and alerts:
  - long break duration (minutes),
  - short break duration (seconds),
  - time between short breaks (minutes),
  - time between long breaks (minutes),
  - enable / disable voice notifications toggle,
  - **audio output device selector** (Default, All devices simultaneously, or specific detected sinks),
  - **volume slider** (0–100%) for voice alerts,
  - **test audio button** to preview alert playback and volume.
- **Audio device management** — discovers audio outputs on Linux via PipeWire and WirePlumber (`pw-dump`, `wpctl`) and plays audio using `pw-play`, `gst-play-1.0`, or `ffplay`. Supports broadcasting alerts to **all audio outputs** simultaneously.
- **Progress window** — compact progress bars displaying the progress toward the next short and long breaks.
- **Work time & wasted time tracking** — records both active work time and break overtime ("wasted time") per day in an SQLite database (`worktime.db`).
- **Statistics window** — visualizes activity history with customizable views:
  - switch between **Work Time** (productive work) and **Wasted Time** (overtime during breaks) modes;
  - view by **Week**, **Month** (current month days), or **Year** (last 12 months);
  - highlights your **best day** in Work Time mode or **worst day** in Wasted Time mode.
- **Config persistence** — settings (timers, audio device, volume, voice alerts) are persisted to `conf.txt` in the per-user data folder (`~/.local/share/makeBreak/`).
- **Startup integration** — intended to run as a startup program (e.g. Ubuntu "Startup Applications").

---

## 🧱 Architecture

A feature-oriented architecture with clean layers:

```
project/
├── App.axaml / App.axaml.cs        # App startup, configuration, tray menu
├── Assets/                          # Vector icon (ikona.svg), PNG icon, VoicePrompts
│   └── VoicePrompts/                # Multi-stage audio alert files (.mp3)
├── Src/
│   ├── Core/                         # Models, enums, services, contracts, MVVM
│   │   ├── Domain/                   # Domain entities, models, interfaces, services
│   │   │   └── Services/             # BreakScheduler, BreakCoordinator, BreakVoiceAlertService, WorkTimeService
│   │   └── Config/                   # Strongly-typed AppConfig schema
│   ├── Features/                     # Break, Progress, Settings, Shell, Statistics, Work
│   ├── Infrastructure/               # Audio (playback & device discovery), EF Core SQLite, navigation, DI
│   └── Shared/                       # Global styles, colors, and localized strings
├── Tests/makeBreak.Tests/           # Comprehensive unit tests (xUnit)
└── packaging/build-deb.sh           # Debian package build script
```

- **Navigation & reactivity** — ReactiveUI routing and MVI-like state management (`ViewModelBase<TState>`).
- **Audio pipeline** — `SystemAudioPlayer` with fallback player discovery (`pw-play`, `gst-play-1.0`, `ffplay`), coupled with `SystemAudioDeviceService` for PipeWire/WirePlumber sink routing.
- **Dependency injection** — `Microsoft.Extensions.DependencyInjection`.
- **Configuration** — `Microsoft.Extensions.Configuration` via `IOptions<T>` and custom flat-file repository.
- **Database** — Entity Framework Core with SQLite (stored in the per-user data folder).
- **UI look** — FluentTheme with custom palettes, Material Icons, and Light/Dark mode support.

---

## 🖥️ Tech stack

| Component | Technology |
|---|---|
| Framework | .NET 10.0 |
| UI | Avalonia 11.3 |
| Reactivity | ReactiveUI 20.x |
| Icons | Material.Icons.Avalonia |
| Audio Playback | PipeWire (`pw-play`) / GStreamer (`gst-play-1.0`) / FFmpeg (`ffplay`) |
| Audio Routing | PipeWire (`pw-dump`) / WirePlumber (`wpctl`) |
| Database | SQLite (Entity Framework Core) |
| Configuration | Microsoft.Extensions.Configuration |
| Tests | xUnit & Moq |

---

## 🚀 Build & run

### Prerequisites (Linux Audio)

To support voice alerts and audio device routing, ensure at least one audio playback utility is installed:

```bash
# PipeWire (recommended on modern Linux/Ubuntu):
sudo apt install pipewire-bin wireplumber

# Alternatively, GStreamer or FFmpeg:
sudo apt install gstreamer1.0-plugins-base-apps
# or:
sudo apt install ffmpeg
```

### Development

```bash
cd project
dotnet restore
dotnet run
```

### Release (self-contained, Linux x64)

```bash
cd project
dotnet publish makeBreak.csproj -c Release -r linux-x64 --self-contained true -o publish/linux-x64
```

### Debian package

The `packaging/build-deb.sh` script turns the release output into an installable `.deb`:

```bash
./packaging/build-deb.sh 1.0.0
```

Result: `project/packaging/makebreak_<version>_amd64.deb`.

### Installing the `.deb` (Debian/Ubuntu)

```bash
sudo apt install ./project/packaging/makebreak_<version>_amd64.deb
```

The app is installed under **`/opt/makebreak`**. Your settings (`conf.txt`) and work history database (`worktime.db`) are stored in **`~/.local/share/makeBreak`**, surviving package upgrades.

### Startup integration

Ubuntu → *Startup Applications* → Add → command:

```
/opt/makebreak/makeBreak
```

---

## ⚙️ Configuration

### Application settings (`appsettings.json`)

Default values are read from `appsettings.json` (`AppConfig` section):

| Key | Default | Meaning |
|---|---|---|
| `TimeForLongBreakSeconds` | `300` | Long break duration (5 min) |
| `TimeForShortBreakSeconds` | `120` | Short break duration (2 min) |
| `TimeToStartLongBreakSeconds` | `900` | Working time before a long break (15 min) |
| `TimeToStartShortBreakSeconds` | `300` | Working time before a short break (5 min) |
| `AreVoiceNotificationsEnabled` | `true` | Whether voice prompts play after a break ends |
| `AudioOutputDeviceId` | `"default"` | Selected audio device ID (`default`, `all`, or device node) |
| `VoiceVolumePercent` | `100` | Playback volume for voice alerts (0–100%) |
| `VoicePromptsDirectory` | `"Assets/VoicePrompts"` | Relative directory path containing prompt audio files |
| `ConfigFileName` | `"conf.txt"` | Name of the file storing saved user settings |
| `WorkTimeDatabaseFileName` | `"worktime.db"` | Name of the SQLite work history database |

### User settings (`conf.txt`)

Settings modified in the Settings dialog override the defaults and are saved to `~/.local/share/makeBreak/conf.txt` across 7 lines:

1. `TimeForLongBreak` (seconds)
2. `TimeForShortBreak` (seconds)
3. `TimeToStartLongBreak` (seconds)
4. `TimeToStartShortBreak` (seconds)
5. `AreVoiceNotificationsEnabled` (`True` / `False`)
6. `AudioOutputDeviceId` (`default`, `all`, or node ID)
7. `VoiceVolumePercent` (`0`–`100`)

---

## 🧪 Testing

```bash
cd project
dotnet test
```

The test suite includes 114+ unit tests covering scheduling, overtime tracking, voice alert stage escalation, audio player routing, database persistence, and statistics aggregation.

---

## 📄 License

Distributed under the [MIT License](LICENSE).