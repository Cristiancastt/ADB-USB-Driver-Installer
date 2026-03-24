using System.Globalization;
using System.Resources;

namespace AdbDriverInstaller.CLI.Localization;

public sealed class Localizer
{
    private readonly ResourceManager _rm;

    public static Localizer Instance { get; private set; } = null!;

    public CultureInfo Culture { get; }

    public Localizer()
    {
        _rm = new ResourceManager("AdbDriverInstaller.CLI.Resources.Strings",
            typeof(Localizer).Assembly);

        Culture = DetectCulture();
        CultureInfo.CurrentUICulture = Culture;
        Instance = this;
    }

    public string this[string key] => _rm.GetString(key, Culture) ?? key;

    public string Format(string key, params object[] args)
        => string.Format(this[key], args);

    public string LanguageName => Culture.TwoLetterISOLanguageName switch
    {
        "es" => "Español",
        "ru" => "Русский",
        "pt" => "Português",
        "zh" => "中文",
        _ => "English"
    };

    private static CultureInfo DetectCulture()
    {
        var current = CultureInfo.CurrentUICulture;
        var lang = current.TwoLetterISOLanguageName;

        // Map to our supported cultures
        return lang switch
        {
            "es" => new CultureInfo("es"),
            "ru" => new CultureInfo("ru"),
            "pt" => new CultureInfo("pt"),
            "zh" => new CultureInfo("zh"),
            _ => new CultureInfo("en")
        };
    }
}
