using System.Threading.Tasks;

namespace api_mmo.ArtifactsApi;

public struct IntervalValue(int min, int current, int max)
{
  public int Min { get; set; } = min;
  public int Current { get; set; } = current;
  public int Max { get; set; } = max;
}

public class Cooldown()
{
  public int Total_seconds { get; set; }
  public int Remaining_seconds { get; set; }
  public DateTime Started_at { get; set; }
  public DateTime Expiration { get; set; }
  public string Reason { get; set; } = "";

}

public class PlayerCharacter(ArtifactsClient client, ArtifactsCharacter artifactsCharacter) : IArtifactsEntity
{

  ArtifactsCharacter _artifactCharacter = artifactsCharacter;

  /// <summary>
  /// Reference to the underlying ArtifactsCharacter object. This reference might be updated when the character performs actions.
  /// </summary>
  public ArtifactsCharacter ArtifactCharacter { get => _artifactCharacter; }

  public int Cooldown { get => _artifactCharacter.Cooldown; }

  public DateTime? CooldownExpiration { get => _artifactCharacter.Cooldown_expiration; }
  public ArtifactsClient Client { get; set; } = client;
  public string Name { get => _artifactCharacter.Name; }

  public Coordinates Position { get => new(_artifactCharacter.X, _artifactCharacter.Y); }
  public IntervalValue Health { get => new(0, _artifactCharacter.Hp, _artifactCharacter.Max_hp); }
  public IntervalValue Experience { get => new(0, _artifactCharacter.Xp, _artifactCharacter.Max_xp); }
  public string Skin { get => _artifactCharacter.Skin; }


  public async Task<Maybe> Move(int x, int y)
  {
    await WaitForCooldown();
    var res = await Client.MoveCharacterAsync(Name, x, y);

    if (res is Success<MoveResponseData> data)
    {
      _artifactCharacter = data.Value.Character;
      return Maybe.Success();
    }
    return Maybe.Failure("Failed to move character");
  }

  public async Task<Maybe> Attack()
  {
    await WaitForCooldown();
    var res = await Client.AttackCharacterAsync(Name);
    if (res is Success<FightData> data)
    {
      _artifactCharacter = data.Value.Character;
    }
    return res;
  }

  public async Task<Maybe> Gather()
  {
    await WaitForCooldown();
    var res = await Client.GatherCharacterAsync(Name);
    if (res is Success<GatherData> data)
    {
      _artifactCharacter = data.Value.Character;
    }
    return res;
  }

  public async Task<Maybe<BankDepositData>> BankDeposit(string itemCode, int quantity)
  {
    await WaitForCooldown();
    var res = await Client.BankDepositAsync(Name, itemCode, quantity);
    if (res is Success<BankDepositData> data)
    {
      _artifactCharacter = data.Value.Character;
    }
    return res;
  }

  public async Task<Maybe<BankWithdrawData>> BankWithdraw(string itemCode, int quantity)
  {
    await WaitForCooldown();
    var res = await Client.BankWithdrawAsync(Name, itemCode, quantity);
    if (res is Success<BankWithdrawData> data)
    {
      _artifactCharacter = data.Value.Character;
    }
    return res;
  }

  public async Task<Maybe<CraftItemData>> CraftItem(string itemCode, int quantity)
  {
    await WaitForCooldown();
    var res = await Client.CraftItemAsync(Name, itemCode, quantity);
    if (res is Success<CraftItemData> data)
    {
      _artifactCharacter = data.Value.Character;
    }
    return res;
  }


  public async Task<Maybe<RestData>> Rest()
  {
    await WaitForCooldown();
    var res = await Client.RestCharacterAsync(Name);
    if (res is Success<RestData> data)
    {
      _artifactCharacter = data.Value.Character;
    }
    return res;
  }

  public async Task WaitForCooldown()
  {
    if (CooldownExpiration != null)
    {
      var now = DateTime.UtcNow;
      var remaining = CooldownExpiration.Value - now;
      if (remaining.TotalSeconds > 0)
      {
        LogMsg($"Waiting for cooldown: {remaining.TotalSeconds} seconds");
        await Task.Delay(remaining.Add(TimeSpan.FromMilliseconds(50)));
      }
    }
  }

  public void TakeDamage(int damage)
  {
    throw new NotImplementedException();
  }

  public void Heal(int health)
  {
    throw new NotImplementedException();
  }

  public void LogMsg(string msg)
  {
    OutputHandler.WriteLine($"[{Name}] {msg}", GetPlayerColor());
  }

  private ConsoleColor GetPlayerColor()
  {
    return Skin switch
    {
      "men1" => ConsoleColor.Red,
      "men2" => ConsoleColor.Blue,
      "men3" => ConsoleColor.DarkGreen,
      "women1" => ConsoleColor.Magenta,
      "women2" => ConsoleColor.Green,
      "women3" => ConsoleColor.Yellow,
      _ => ConsoleColor.Gray,
    };
  }
}