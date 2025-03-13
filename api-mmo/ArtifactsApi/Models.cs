using System.Diagnostics.CodeAnalysis;

namespace api_mmo.ArtifactsApi;

public struct Coordinates(int x, int y)
{
  public int X { get; set; } = x;
  public int Y { get; set; } = y;

  public readonly override bool Equals(object? obj)
  {
    if (obj is Coordinates other)
    {
      return X == other.X && Y == other.Y;
    }
    return false;
  }

  public override readonly int GetHashCode()
  {
    return HashCode.Combine(X, Y);
  }
  public static bool operator ==(Coordinates left, Coordinates right)
  {
    return left.Equals(right);
  }

  public static bool operator !=(Coordinates left, Coordinates right)
  {
    return !(left == right);
  }

  public override readonly string ToString()
  {
    return $"{X},{Y}";
  }
}

public abstract class Maybe
{
  public abstract string GetResultName();
  public abstract bool IsSuccess { get; protected set; }
  public bool IsFailure => !IsSuccess;
  public abstract string GetMessage();
  public static Maybe Success() => new Success();
  public static Maybe Failure(string errorMessage) => new Failure(errorMessage);
}

public abstract class Maybe<T> : Maybe
{
  public static Maybe<T> Success(T value) => new Success<T>(value);
  public static new Maybe<T> Failure(string errorMessage) => new Failure<T>(errorMessage);
}

public class Success() : Maybe
{
  public override bool IsSuccess { get; protected set; } = true;
  public override string GetResultName() => "Maybe.Success";
  public override string GetMessage() => "Success";
}

public class Failure : Maybe
{
  public override bool IsSuccess { get; protected set; } = false;
  public override string GetResultName() => "Maybe.Failure";
  public string? ErrorMessage { get; private set; }
  public Exception? Exception { get; private set; }
  public Failure(string errorMessage)
  {
    ErrorMessage = errorMessage;
  }

  public Failure(Exception exception)
  {
    Exception = exception;
  }

  public override string GetMessage()
  {
    return ErrorMessage ?? Exception?.Message ?? "Unknown error";
  }
}

public class Success<T>(T value) : Maybe<T>
{
  public override bool IsSuccess { get; protected set; } = true;
  public override string GetResultName() => typeof(T).Name;
  public override string GetMessage() => "Success";
  public T Value { get; set; } = value;
}

public class Failure<T> : Maybe<T>
{
  public override bool IsSuccess { get; protected set; } = false;
  public override string GetResultName() => typeof(T).Name;
  public string? ErrorMessage { get; private set; }
  public Exception? Exception { get; private set; }

  public Failure(string errorMessage)
  {
    ErrorMessage = errorMessage;
  }

  public Failure(Exception exception)
  {
    Exception = exception;
  }

  public override string GetMessage()
  {
    return ErrorMessage ?? Exception?.Message ?? "Unknown error";
  }
}