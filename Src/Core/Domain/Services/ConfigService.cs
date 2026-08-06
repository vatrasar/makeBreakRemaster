using makeBreak.Src.Core.Config;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.RepositoryContracts;
using Microsoft.Extensions.Options;

namespace makeBreak.Src.Core.Domain.Services;

/// <summary>
/// Loads and persists break configuration, merging appsettings defaults with
/// optional overrides stored in <c>conf.txt</c> in the per-user data folder.
/// </summary>
public sealed class ConfigService
{
    private readonly IOptions<AppConfig> _options;
    private readonly IConfigRepository _repository;

    public ConfigService(IOptions<AppConfig> options, IConfigRepository repository)
    {
        _options = options;
        _repository = repository;
    }

    /// <summary>
    /// Returns the active break configuration: conf.txt values when present,
    /// otherwise appsettings defaults. Invoked at application startup.
    /// </summary>
    public BreakConfig GetCurrentConfig() => _repository.Load() ?? BuildConfigFromDefaults();

    /// <summary>
    /// Applies the given schedule values and persists them to <c>conf.txt</c>.
    /// Invoked when settings are saved.
    /// </summary>
    public void Save(BreakConfig config) => _repository.Save(config);

    private BreakConfig BuildConfigFromDefaults()
    {
        AppConfig appConfig = _options.Value;

        return new BreakConfig
        {
            TimeForLongBreak = appConfig.TimeForLongBreakSeconds,
            TimeForShortBreak = appConfig.TimeForShortBreakSeconds,
            TimeToStartLongBreak = appConfig.TimeToStartLongBreakSeconds,
            TimeToStartShortBreak = appConfig.TimeToStartShortBreakSeconds,
        };
    }
}