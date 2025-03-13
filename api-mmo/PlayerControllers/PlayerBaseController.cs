using System.Collections.Concurrent;
using api_mmo.ArtifactsApi;
using Microsoft.VisualBasic;

namespace api_mmo.PlayerControllers;

public abstract class PlayerBaseController(PlayerCharacter player)
{
  public PlayerCharacter Player { get; private set; } = player;

  protected Maybe LastActionResult { get; private set; } = Maybe.Success();

  protected readonly ConcurrentQueue<Func<Task<Maybe>>> _actions = new();
  CancellationTokenSource _cancellationTokenSource = new();

  protected bool actionRunning = false;
  protected bool playLoopRunning = false;

  public async Task StartCycle()
  {
    _cancellationTokenSource = new CancellationTokenSource();
    if (!playLoopRunning)
    {
      playLoopRunning = true;
      await PlayLoop();
    }
  }

  public void StopCycle()
  {
    _actions.Clear();
    _cancellationTokenSource.Cancel();
  }

  public async Task<Maybe> Fight()
  {
    LogMsg("Attacking");
    var res = await Player.Attack();
    if (res is Failure<FightData> failure)
    {
      LogMsg($"Failed to attack: {failure.GetMessage()}");
    }
    else if (res is Success<FightData> success)
    {
      LogMsg(success.Value.Fight.Result);
      //LogMsg($"{success.Value.Fight.Result}\n{string.Join($"\n[{Player.Name}]", success.Value.Fight.Logs)}");
    }

    return res;
  }

  public async Task<Maybe> Gather()
  {
    LogMsg("Gathering resources");
    var res = await Player.Gather();
    if (res is Failure<GatherData> failure)
    {
      LogMsg($"Failed to gather: {failure.GetMessage()}");
    }
    else if (res is Success<GatherData> success)
    {
      LogMsg($"Gathered {string.Join(", ", success.Value.Gather.Items.Select(x => $"{x.Quantity} {x.Code}"))} resources");
    }

    return res;
  }

  public async Task<Maybe> BankDeposit(string itemCode, int quantity)
  {
    LogMsg($"Depositing resources to bank: {quantity} {itemCode}");
    var res = await Player.BankDeposit(itemCode, quantity);
    if (res is Failure<BankDepositData> failure)
    {
      LogMsg($"Failed to deposit: {failure.GetMessage()}");
    }
    else if (res is Success<BankDepositData>)
    {
      LogMsg($"Deposited {quantity} {itemCode}");
    }

    return res;
  }

  public async Task<Maybe> BankWithdraw(string itemCode, int quantity)
  {
    LogMsg($"Withdrawing resources from bank: {quantity} {itemCode}");
    var res = await Player.BankWithdraw(itemCode, quantity);
    if (res is Failure<BankWithdrawData> failure)
    {
      LogMsg($"Failed to withdraw: {failure.GetMessage()}");
    }
    else if (res is Success<BankWithdrawData>)
    {
      LogMsg($"Withdrew {quantity} {itemCode}");
    }

    return res;
  }

  public async Task<Maybe> Craft(string itemCode, int quantity)
  {
    LogMsg($"Crafting {quantity} {itemCode}");
    var res = await Player.CraftItem(itemCode, quantity);
    if (res is Failure<CraftItemData> failure)
    {
      LogMsg($"Failed to craft: {failure.GetMessage()}");
    }
    else if (res is Success<CraftItemData>)
    {
      LogMsg($"Crafted {quantity} {itemCode}");
    }

    return res;
  }

  public async Task<Maybe> Rest()
  {
    LogMsg("Resting");
    var restRes = await Player.Rest();
    if (restRes is Failure<RestData> failureRest)
    {
      LogMsg($"Failed to rest: {failureRest.GetMessage()}");
    }
    else if (restRes is Success<RestData> successRest)
    {
      LogMsg($"Recovered {successRest.Value.Hp_restored} health");
    }

    return restRes;
  }

  public async Task<Maybe> Move(Coordinates coordinates)
  {
    LogMsg($"Moving to {coordinates}");
    var res = await Player.Move(coordinates.X, coordinates.Y);
    if (res is Failure failure)
    {
      LogMsg($"Failed to move: {failure.GetMessage()}");
    }

    return res;
  }

  private async Task PlayLoop()
  {
    while (playLoopRunning)
    {
      if (_cancellationTokenSource.Token.IsCancellationRequested)
      {
        await Task.Delay(1000);
        continue;
      }

      if (_actions.TryDequeue(out var action))
      {
        actionRunning = true;
        try
        {
          var res = await action();
          LastActionResult = res;
          if (res is Failure failure)
          {
            LogMsg($"Failed to perform action: {failure.GetMessage()}");
            _actions.Clear();
            DecideNextAction();
          }
        }
        finally
        {
          actionRunning = false;
        }
      }
      else
      {
        DecideNextAction();
      }
    }
  }

  public async Task PerformAction(Func<Task<Maybe>> action)
  {
    var res = await action();
    if (res is Failure failure)
    {
      LogMsg($"Failed to perform action: {failure.GetMessage()}");
    }
  }

  public void EnqueueAction(Func<Task<Maybe>> action)
  {
    _actions.Enqueue(action);
  }

  protected void LogMsg(string msg)
  {
    Player.LogMsg(msg);
  }

  /// <summary>
  /// Decide the next action to take and add it to the <see cref="_actions"/> list. Each class should implement logic based on the player's current state and profession.
  /// </summary>
  protected abstract void DecideNextAction();

}