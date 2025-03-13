using System.Text.Json;
using System.Text.Json.Serialization;

namespace api_mmo;


public class Settings
{
    public readonly static string ApiUrl = "";
    public string ApiKey { get; set; } = "";

    public SmartCharacter[] Characters { get; set; } = [];

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static Settings LoadSettings(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<Settings>(json, _serializerOptions);

            if (settings == null)
            {
                return SaveDefaultSettings(path);
            }

            return settings;
        }
        catch (FileNotFoundException)
        {
            return SaveDefaultSettings(path);
        }
    }

    private static Settings SaveDefaultSettings(string path)
    {
        var newSettings = new Settings()
        {
            ApiKey = "",
            Characters =
            [
                new()
                    {
                        Name = "Character1",
                        Actions =
                        [
                            new SmartCharacterAction()
                            {
                                Name = "Action1",
                                Cascade = false,
                                If = "",
                                While = "",
                                Do =
                                [
                                    "Move 1 1",
                                    "Attack"
                                ]
                            }
                        ],
                        OnFailure =
                        {
                            ["ArtifactsInventoryFull"] = new SmartCharacterFailure()
                            {
                                Do =
                                [
                                    "Move 4 1",
                                    "foreach $item in $player.inventory.items do deposit $item $player.inventory.$item.count",
                                ]
                            }
                        }
                    }
            ]
        };

        File.WriteAllText(path, JsonSerializer.Serialize(newSettings, _serializerOptions));
        return newSettings;
    }

}

public class SmartCharacter
{
    public string Name { get; set; } = "";
    public SmartCharacterAction[] Actions { get; set; } = [];
    public Dictionary<string, SmartCharacterFailure> OnFailure { get; set; } = [];
}

public class SmartCharacterAction
{
    public string Name { get; set; } = "";
    public bool Cascade { get; set; } = false;
    public string If { get; set; } = "";
    public string While { get; set; } = "";
    public string[] Do { get; set; } = [];
}

public class SmartCharacterFailure
{
    public string[] Do { get; set; } = [];
}