using System.Collections;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using api_mmo.ArtifactsApi;
using api_mmo.PlayerControllers;

namespace api_mmo;

public partial class EvaluationEngine(PlayerBaseController player)
{
  readonly PlayerBaseController _player = player;
  private static readonly ImmutableArray<char> reservedTokens = ImmutableArray.Create('&', '|', '<', '>', '=', '!');
  private static readonly ImmutableArray<string> reservedKeywords = ImmutableArray.Create("&&", "||", "<", "<=", ">", ">=", "==", "!=");

  [GeneratedRegex(@"^-?\d+,-?\d+$")]
  private static partial Regex CoordinateRegex();

  public bool EvaluateCondition(string condition)
  {
    var result = true;
    var tokenStart = 0;
    condition = condition.Trim();
    for (int i = 0; i < condition.Length;)
    {
      string? addOp = null;
      if (i > 0)
      {
        tokenStart = i;
        i = ExtractOperator(i, ref condition, out addOp);
        if (!reservedKeywords.Contains(addOp))
        {
          throw MakeArgumentException($"Invalid addOp {addOp}", tokenStart, condition);
        }

        if (addOp == "&&" && !result)
        {
          return false;
        }
        else if (addOp == "||" && result)
        {
          return true;
        }
      }

      tokenStart = i;
      i = ExtractToken(i, ref condition, out string currentToken);
      if (currentToken.Length == 0)
      {
        throw MakeArgumentException("Invalid left token condition", tokenStart, condition);
      }

      var leftToken = GetCharacterValue(currentToken);

      i = ExtractOperator(i, ref condition, out string operatorToken);

      tokenStart = i;
      i = ExtractToken(i, ref condition, out currentToken);
      if (currentToken.Length == 0)
      {
        throw MakeArgumentException("Invalid right token condition", tokenStart, condition);
      }

      var rightToken = GetCharacterValue(currentToken);

      var cmpResult = Comparer.DefaultInvariant.Compare(leftToken, rightToken);

      var comparisonResult = operatorToken switch
      {
        "<" => result = cmpResult < 0,
        "<=" => result = cmpResult <= 0,
        ">" => result = cmpResult > 0,
        ">=" => result = cmpResult >= 0,
        "==" => result = cmpResult == 0,
        "!=" => result = cmpResult != 0,
        _ => throw MakeArgumentException($"Invalid cmpOp {operatorToken}", i, condition)
      };

      if (addOp != null)
      {
        if (addOp == "&&")
        {
          result = result && comparisonResult;
        }
        else if (addOp == "||")
        {
          result = result || comparisonResult;
        }
      }
      else
      {
        result = comparisonResult;
      }
    }

    return result;
  }

  private static int ExtractToken(int startIndex, ref string condition, out string token)
  {
    var tokenBuilder = new StringBuilder();
    for (int i = startIndex; i < condition.Length; i++)
    {
      if (condition[i] == ' ')
      {
        token = tokenBuilder.ToString();
        for (int j = i; j < condition.Length; j++)
        {
          if (condition[j] != ' ')
          {
            return j;
          }
        }
        return condition.Length - 1;
      }

      if (reservedTokens.Contains(condition[i]))
      {
        token = tokenBuilder.ToString();
        return i;
      }

      tokenBuilder.Append(condition[i]);
    }

    token = tokenBuilder.ToString();
    return condition.Length;

  }

  private static int ExtractOperator(int startIndex, ref string condition, out string operatorToken)
  {
    var tokenBuilder = new StringBuilder();

    for (int i = startIndex; i < condition.Length; i++)
    {
      if (condition[i] == ' ')
      {
        operatorToken = tokenBuilder.ToString();

        for (int j = i; j < condition.Length; j++)
        {
          if (condition[j] != ' ')
          {
            return j;
          }
        }
        return condition.Length - 1;
      }

      if (reservedTokens.Contains(condition[i]))
      {
        tokenBuilder.Append(condition[i]);
      }
      else
      {
        operatorToken = tokenBuilder.ToString();
        return i;

      }
    }

    throw MakeArgumentException($"No right hand comparison", startIndex, tokenBuilder.ToString());
  }

  public T GetCharacterValue<T>(string valuePath)
  {
    var v = GetCharacterValue(valuePath);

    if (v is not T)
    {
      throw new ArgumentException($"Invalid type {v.GetType()} for {valuePath} expected {typeof(T)}");
    }

    return (T)v;
  }

  private object GetCharacterValue(string valuePath)
  {
    if (!valuePath.StartsWith('$'))
    {
      var v = valuePath.ToLower();
      if (v == "true")
      {
        return true;
      }
      else if (v == "false")
      {
        return false;
      }

      if (CoordinateRegex().IsMatch(valuePath))
      {
        return valuePath;
      }

      if (int.TryParse(valuePath, out var intVal))
      {
        return intVal;
      }

      return valuePath;
    }

    valuePath = valuePath[1..];
    var subProperties = valuePath.Split(".");
    var targetEntity = subProperties[0];
    var targetProperty = subProperties[1];

    if (IEq(targetEntity, "Player"))
    {
      switch (targetProperty.ToLower())
      {
        case "hp":
          return _player.Player.Health.Current;
        case "position":
          return _player.Player.Position.ToString();
        case "level":
          return _player.Player.ArtifactCharacter.Level;
        case "inventory":
          if (subProperties[2] == "items")
            return _player.Player.ArtifactCharacter.Inventory.Where(itm => itm.Quantity > 0).Select(itm => (IEnumerableProperty)itm);
          if (subProperties[2] == "count")
            return _player.Player.ArtifactCharacter.Inventory.Sum(itm => itm.Quantity);
          if (subProperties[2] == "max")
            return _player.Player.ArtifactCharacter.Inventory_max_items;

          // End reserved keywords, look for items
          var itemName = subProperties[2];
          var itemProperty = subProperties[3];
          switch (itemProperty.ToLower())
          {
            case "count":
              var item = _player.Player.ArtifactCharacter.Inventory.FirstOrDefault(itm => itm.Code == itemName);
              if (item == null)
              {
                return 0;
              }
              return item.Quantity;
          }
          break;
      }
    }
    else if (IEq(targetEntity, "world"))
    {
      throw new NotImplementedException("World properties not implemented");
    }

    throw new ArgumentException($"Property {targetProperty} not supported for {targetEntity}");
  }

  private static bool IEq(string value1, string value2)
  {
    return value1.Equals(value2, StringComparison.OrdinalIgnoreCase);
  }

  private static ArgumentException MakeArgumentException(string message, int index, string condition)
  {
    string errMsg = $"{message} at index {index}: {condition}";
    string errHintPointer = new string(' ', errMsg.Length - condition.Length + index - 1) + "^";
    return new ArgumentException($"\n{errMsg}\n{errHintPointer}");

  }

  public Func<Task<Maybe>>? DoAction(string doAction)
  {
    var cmds = doAction.ToLowerInvariant().Trim().Split(' ');

    if (cmds.Length == 0)
    {
      return null;
    }

    var action = cmds[0];

    if (action == "foreach")
    {
      // [0]foreach [1]$itemToken [2]in [3]collectionToken [4]do [5]actionCmd
      var itemToken = cmds[1];
      var collectionList = GetCharacterValue<IEnumerable<IEnumerableProperty>>(cmds[3]);
      var actionCmd = string.Join(' ', cmds[5..]);
      return async () =>
      {
        foreach (var item in collectionList)
        {
          var action = DoAction(actionCmd.Replace(itemToken, item.GetItemKey()));
          if (action != null)
          {
            Maybe res;
            if ((res = await action()).IsFailure)
            {
              _player.Player.LogMsg($"[CMD] Failed to execute action {actionCmd} for {itemToken} in {collectionList}: {res.GetMessage()}");
              return res;
            }
          }
        }
        return new Success();
      };
    }

    switch (action)
    {
      case "move":
        if (cmds.Length < 3)
        {
          _player.Player.LogMsg("[CMD] Missing coordinates");
          return null;
        }

        var x = GetCharacterValue<int>(cmds[1]);
        var y = GetCharacterValue<int>(cmds[2]);

        return () => _player.Move(new Coordinates(x, y));
      case "fight":
        return _player.Fight;
      case "gather":
        return _player.Gather;
      case "rest":
        return _player.Rest;
      case "deposit":
        if (cmds.Length < 3)
        {
          _player.Player.LogMsg("[CMD] Missing item code and quantity");
          return null;
        }

        var itemCode = GetCharacterValue<string>(cmds[1]);
        var itemQuantity = GetCharacterValue<int>(cmds[2]);

        return () => _player.BankDeposit(itemCode, itemQuantity);

      case "withdraw":
        if (cmds.Length < 3)
        {
          _player.Player.LogMsg("[CMD] Missing item code and quantity");
          return null;
        }

        var itemCodeWithdraw = GetCharacterValue<string>(cmds[1]);
        var itemQuantityWithdraw = GetCharacterValue<int>(cmds[2]);

        return () => _player.BankWithdraw(itemCodeWithdraw, itemQuantityWithdraw);
      case "craft":
        if (cmds.Length < 3)
        {
          _player.Player.LogMsg("[CMD] Missing item code");
          return null;
        }

        var itemCodeCraft = GetCharacterValue<string>(cmds[1]);
        var itemQuantityCraft = GetCharacterValue<int>(cmds[2]);

        return () => _player.Craft(itemCodeCraft, itemQuantityCraft);
      default:
        _player.Player.LogMsg($"[CMD] Invalid action {action}");
        return null;
    }
  }
}