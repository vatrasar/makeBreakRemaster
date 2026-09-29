namespace makeBreak.Src.Core.Config;

public sealed class AppConfig
{
    public const string SectionName = "AppConfig";

    public int TimeForLongBreakSeconds { get; set; } = 300;

    public int TimeForShortBreakSeconds { get; set; } = 120;

    public int TimeToStartLongBreakSeconds { get; set; } = 900;

    public int TimeToStartShortBreakSeconds { get; set; } = 300;
 
    public bool AreVoiceNotificationsEnabled { get; set; } = true;

    public string AudioOutputDeviceId { get; set; } = "default";

    public string VoicePromptsDirectory { get; set; } = "Assets/VoicePrompts";

    public string ConfigFileName { get; set; } = "conf.txt";

    public string WorkTimeDatabaseFileName { get; set; } = "worktime.db";
}