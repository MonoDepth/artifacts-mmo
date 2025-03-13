namespace api_mmo;

public class OutputHandler
{
  private static string Command = "";
  private static readonly Lock _consoleLock = new();

  private static readonly List<string> _commandHistory = [];
  private static int _historyIndex = 0;
  public static void WriteLine(string Message, ConsoleColor Color = ConsoleColor.White)
  {
    _consoleLock.Enter();
    var orginalColor = Console.ForegroundColor;
    try
    {
      Console.ForegroundColor = Color;
      if (Command != "")
      {
        ClearConsole();
      }
      Console.CursorLeft = 0;
      Console.WriteLine(Message);
      Console.ForegroundColor = orginalColor;
      if (Message != Command)
      {
        Console.Write(Command);
      }
    }
    finally
    {
      Console.ForegroundColor = orginalColor;
      _consoleLock.Exit();
    }
  }

  public static string? ReadInput()
  {
    ConsoleKeyInfo cki;

    cki = Console.ReadKey(true);

    if (cki.Key == ConsoleKey.Escape)
    {
      Command = "";
    }
    else if (cki.Key == ConsoleKey.UpArrow)
    {
      if (_historyIndex > 0)
      {
        _historyIndex--;
        Command = _commandHistory[_historyIndex];
        WriteCurrentCommand();
      }
    }
    else if (cki.Key == ConsoleKey.DownArrow)
    {
      if (_historyIndex < _commandHistory.Count - 1)
      {
        _historyIndex++;
        Command = _commandHistory[_historyIndex];
        WriteCurrentCommand();
      }
    }
    else if (cki.Key == ConsoleKey.Backspace)
    {
      ClearConsole();
      if (Command.Length > 0)
      {
        Command = Command[..^1];
      }
      Console.CursorLeft = 0;
      _consoleLock.Enter();
      try
      {
        var orgColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(Command);
        Console.ForegroundColor = orgColor;
      }
      finally
      {
        _consoleLock.Exit();
      }
    }
    else if (cki.Key == ConsoleKey.Enter)
    {
      Console.CursorLeft = 0;
      ClearConsole();
      string TempCommand = Command;
      WriteLine(Command);
      Command = "";
      _commandHistory.Add(TempCommand);

      if (_commandHistory.Count > 100)
      {
        _commandHistory.RemoveAt(0);
      }

      _historyIndex = _commandHistory.Count;
      return TempCommand;
    }
    else
    {
      Command += cki.KeyChar;
      WriteCurrentCommand();
    }
    return null;
  }

  static void WriteCurrentCommand()
  {
    ClearConsole();
    Console.CursorLeft = 0;
    _consoleLock.Enter();
    var orgColor = Console.ForegroundColor;
    try
    {
      Console.ForegroundColor = ConsoleColor.White;
      Console.Write(Command);

    }
    finally
    {
      Console.ForegroundColor = orgColor;
      _consoleLock.Exit();
    }
  }

  static void ClearConsole()
  {
    _consoleLock.Enter();
    try
    {
      if (Command.Length == 0)
      {
        Console.Write("");
        return;
      }
      for (int i = 0; i < Command.Length; i++)
      {
        Console.Write(" ");
      }
    }
    finally
    {
      _consoleLock.Exit();
    }
  }
}