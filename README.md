<div align="center">

# ⏳ makeBreak

**A desktop break-time manager that confirms when your break is over.**

A cross-platform desktop application that organizes and enforces break times while you work at the computer. Like *safeYourEyes*, but with a key difference: when a break finishes, you confirm it with a button — so the app can more accurately track your **real work time**.

C# / Avalonia / ReactiveUI / .NET 10

</div>

---

## ✨ Features

- **Break scheduling** — tracks work sessions and automatically triggers **short** and **long** breaks based on the configured intervals.
- **Break screen** — a fullscreen view showing a countdown until the break ends. The **"Break over"** button confirms the end of the break, letting the app better determine your real working time.
- **System tray integration** — runs from the system tray, from which you can:
  - open the settings dialog;
  - view the progress of the short/long work intervals;
  - **pause** / **resume** break scheduling;
  - quit the application.
- **Settings dialog** — configure four parameters:
  - long break duration (minutes),
  - short break duration (seconds),
  - time between short breaks (minutes),
  - time between long breaks (minutes).
- **Progress window** — progress bars showing the current state of the short and long work intervals.
- **Config persistence** — settings are persisted to a `conf.txt` file.
- **Startup integration** — intended to run as a startup program (e.g. Ubuntu "Startup Applications").

---

## 🧱 Architecture

A feature-oriented architecture with clean layers:

```
project/
├── App.axaml / App.axaml.cs        # App startup, configuration, tray
├── Assets/                          # Resources (icons)
├── Src/
│   ├── Core/                         # Models, enums, services, contracts, MVVM
│   ├── Features/                     # Break, Progress, Settings, Shell, Work
│   ├── Infrastructure/               # Navigation, DI, config repository
│   └── Shared/                       # Global styles and strings
├── Tests/makeBreak.Tests/           # Unit tests (xUnit)
└── packaging/build-deb.sh           # Debian package build script
```

- **Navigation & reactivity** — ReactiveUI routing plus the MVI-like state pattern (`ViewModelBase<TState>`).
- **Dependency injection** — `Microsoft.Extensions.DependencyInjection`.
- **Configuration** — `Microsoft.Extensions.Configuration` via the `IOptions<T>` pattern; values live in `appsettings.json`.
- **Database** — Entity Framework Core with SQLite.
- **UI look** — FluentTheme with custom palettes and Light/Dark mode support.

---

## 🖥️ Tech stack

| | |
|---|---|
| Framework | .NET 10.0 |
| UI | Avalonia 11.3 |
| Reactivity | ReactiveUI 20.x |
| Database | SQLite (EF Core) |
| Configuration | Microsoft.Extensions.Configuration |
| Tests | xUnit |

---

## 🚀 Build & run

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

The app is installed under **`/opt/makebreak`**. Your settings (`conf.txt`) are stored in **`~/.local/share/makeBreak`**, so they survive package upgrades.

### Startup integration

Ubuntu → *Startup Applications* → Add → command:

```
/opt/makebreak/makeBreak
```

---

## ⚙️ Configuration

Default values are read from `appsettings.json` (`AppConfig` section):

| Key | Default | Meaning |
|---|---|---|
| `TimeForLongBreakSeconds` | 300 | Long break duration |
| `TimeForShortBreakSeconds` | 120 | Short break duration |
| `TimeToStartLongBreakSeconds` | 900 | Working time before a long break |
| `TimeToStartShortBreakSeconds` | 300 | Working time before a short break |
| `ConfigFileName` | `conf.txt` | Name of the file storing saved settings |

Settings changed in the dialog override these defaults and are saved to `conf.txt` in `~/.local/share/makeBreak/`.

---

## 🧪 Testing

```bash
cd project
dotnet test
```

---

## 📍 Status / roadmap

The project is being rewritten from an original Python (PyQt5) version to Avalonia UI (.NET). The full specification lives in `reimplementation-spec.md`.

---

## 📄 License

Add a license (e.g. MIT) before publishing.