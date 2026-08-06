using System.Globalization;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.RepositoryContracts;

namespace makeBreak.Src.Infrastructure.Data.Repositories;

/// <summary>
/// Reads and writes the four schedule values to <c>conf.txt</c> located in the per-user data folder (~/.local/share/makeBreak on Linux).
/// </summary>
public sealed class ConfigFileRepository : IConfigRepository
{
    private const int ExpectedLineCount = 4;

    private readonly string _filePath;

    public ConfigFileRepository(string filePath) => _filePath = filePath;

    public BreakConfig? Load()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        string[] lines = File.ReadAllLines(_filePath);

        if (lines.Length < ExpectedLineCount)
        {
            return null;
        }

        if (!TryParseSeconds(lines[0], out int timeForLongBreak) ||
            !TryParseSeconds(lines[1], out int timeForShortBreak) ||
            !TryParseSeconds(lines[2], out int timeToStartLongBreak) ||
            !TryParseSeconds(lines[3], out int timeToStartShortBreak))
        {
            return null;
        }

        return new BreakConfig
        {
            TimeForLongBreak = timeForLongBreak,
            TimeForShortBreak = timeForShortBreak,
            TimeToStartLongBreak = timeToStartLongBreak,
            TimeToStartShortBreak = timeToStartShortBreak,
        };
    }

    public void Save(BreakConfig config)
    {
        string[] lines =
        {
            config.TimeForLongBreak.ToString(CultureInfo.InvariantCulture),
            config.TimeForShortBreak.ToString(CultureInfo.InvariantCulture),
            config.TimeToStartLongBreak.ToString(CultureInfo.InvariantCulture),
            config.TimeToStartShortBreak.ToString(CultureInfo.InvariantCulture),
        };

        File.WriteAllLines(_filePath, lines);
    }

    private static bool TryParseSeconds(string line, out int value) =>
        int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 1;
}