using api_mmo;
using api_mmo.ArtifactsApi;
using api_mmo.PlayerControllers;

var settings = Settings.LoadSettings("settings.json");

if (settings.ApiKey == "")
{
  OutputHandler.WriteLine("Please set your API key in settings.json", ConsoleColor.Red);
  return;
}

var playerControllers = new List<PlayerBaseController>();

var watcher = new FileSystemWatcher("./", "settings.json")
{
    NotifyFilter = NotifyFilters.LastWrite
};


var client = new ArtifactsClient(settings.ApiKey);

var charactersRes = await client.GetPlayerCharactersAsync();


if (charactersRes is Success<List<PlayerCharacter>> characters)
{
  watcher.EnableRaisingEvents = true;
  watcher.Changed += (sender, e) => TriggerReload();

  foreach (var character in settings.Characters)
  {
    var player = characters.Value.Find(p => p.Name.ToLower() == character.Name.ToLower());
    if (player == null)
    {
      OutputHandler.WriteLine($"[WARN] Character {character.Name} not found", ConsoleColor.Red);
      continue;
    }

    var controller = new SmartController(character, player);
    playerControllers.Add(controller);
  }

  Task.WaitAll(playerControllers.Select(c => c.StartCycle()));
}
else if (charactersRes is Failure<List<PlayerCharacter>> failure)
{
  OutputHandler.WriteLine($"Failed to get player characters: {failure.GetMessage()}");
}

void TriggerReload() {
  try
  {
    watcher.EnableRaisingEvents = false;
    Thread.Sleep(500);
    var settings = Settings.LoadSettings("settings.json");

    if (settings.ApiKey == "")
    {
      OutputHandler.WriteLine("Reload failed, Please set your API key in settings.json", ConsoleColor.Red);
      return;
    }

    foreach (var character in settings.Characters)
    {
      var player = playerControllers.Find(p => p.Player.Name.Equals(character.Name, StringComparison.OrdinalIgnoreCase));
      if (player == null)
      {
        var newPlayer = characters.Value.Find(p => p.Name.Equals(character.Name, StringComparison.OrdinalIgnoreCase));
        if (newPlayer == null)
        {
          OutputHandler.WriteLine($"[WARN] Character {character.Name} not found", ConsoleColor.Red);
          continue;
        }

        var controller = new SmartController(character, newPlayer);
        playerControllers.Add(controller);
      }
      else if (player is SmartController smartController)
      {
        smartController.Reload(character);
      }
    }

    OutputHandler.WriteLine("Reloaded settings", ConsoleColor.Green);
  }
  finally
  {
    watcher.EnableRaisingEvents = true;
  }
}