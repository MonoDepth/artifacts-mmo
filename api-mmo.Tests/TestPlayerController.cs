using api_mmo.ArtifactsApi;
using api_mmo.PlayerControllers;

namespace api_mmo.Tests;

public class TestPlayerController(PlayerCharacter player) : PlayerBaseController(player)
{
    protected override void DecideNextAction()
    {

    }
}