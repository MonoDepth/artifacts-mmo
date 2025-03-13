using api_mmo.ArtifactsApi;
using api_mmo.PlayerControllers;

namespace api_mmo.Tests;

public class EvalEngineTests
{
    [Theory]
    [InlineData("$player.level > 2", true)]
    [InlineData("$player.level > 10", false)]
    [InlineData("$player.level >= 2", true)]
    [InlineData("$player.level < 100", true)]
    [InlineData("$player.level <= 100", true)]
    [InlineData("$player.level == 10", true)]
    [InlineData("$player.level != 12", true)]
    [InlineData("$player.level > 2 && $player.level < 20", true)]
    [InlineData("$player.level > 20 || $player.level < 11", true)]
    [InlineData("$player.level > 10 && $player.level < 10", false)]
    [InlineData("$player.inventory.item1.count > 10", false)]
    [InlineData("$player.inventory.item2.count == 20", true)]
    [InlineData("$player.inventory.count < $player.inventory.max", true)]
    [InlineData("$player.position == 1,1 ", true)]
    public void Conditions_EvalsCorrect(string condition, bool expected)
    {
        var eval = new EvaluationEngine(TestData.TestPlayer);
        var result = eval.EvaluateCondition(condition);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetInventory_EnumeratesAllItems()
    {
        var eval = new EvaluationEngine(TestData.TestPlayer);
        var inventory = eval.GetCharacterValue<IEnumerable<IEnumerableProperty>>("$player.inventory.items");
        Assert.IsAssignableFrom<IEnumerable<IEnumerableProperty>>(inventory);
        Assert.Equal(TestData.ArtifactsTestChar.Inventory.Count, inventory.Count());
    }
}

public static class TestData {
    public static ArtifactsClient ArtifactsTestClient {get;} = new("");

    public static ArtifactsCharacter ArtifactsTestChar {get;} = new()
    {
        Name = "TestChar",
        Max_hp = 100,
        Hp = 100,
        Level = 10,
        Xp = 0,
        X = 1,
        Y = 1,
        Inventory_max_items = 1000,
        Inventory =
        [
            new ArtifactsInventory { Slot= 0, Code = "item1", Quantity = 10 },
            new ArtifactsInventory { Slot= 1, Code = "item2", Quantity = 20 },
            new ArtifactsInventory { Slot= 2, Code = "item3", Quantity = 30 },
            new ArtifactsInventory { Slot= 3, Code = "item4", Quantity = 40 },
            new ArtifactsInventory { Slot= 4, Code = "item5", Quantity = 50 },
        ]
    };


    public static PlayerCharacter PlayerTestChar {get;} = new(ArtifactsTestClient, ArtifactsTestChar);
    public static TestPlayerController TestPlayer {get;} = new(PlayerTestChar);
}
