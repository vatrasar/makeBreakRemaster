using makeBreak.Src.Core.Domain.Models;

namespace makeBreak.Src.Core.Domain.RepositoryContracts;

/// <summary>
/// Loads and persists break configuration to the plain-text <c>conf.txt</c> file
/// located in the per-user data folder (~/.local/share/makeBreak on Linux).
/// </summary>
public interface IConfigRepository
{
    /// <summary>
    /// Reads the schedule values from <c>conf.txt</c>.
    /// Invoked by <c>ConfigService</c> at application startup.
    /// </summary>
    BreakConfig? Load();

    /// <summary>
    /// Writes the schedule values to <c>conf.txt</c> (creating the file if needed).
    /// Invoked by <c>ConfigService</c> whenever settings are saved.
    /// </summary>
    void Save(BreakConfig config);
}