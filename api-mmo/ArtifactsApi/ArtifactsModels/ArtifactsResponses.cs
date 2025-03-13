namespace api_mmo.ArtifactsApi;

public class AllCharactersResponse
{
    public List<ArtifactsCharacter> Data { get; set; } = [];
}

public class MoveResponseData
{
    public ArtifactsCooldown Cooldown { get; set; } = new ArtifactsCooldown();
    public ArtifactsDestination Destination { get; set; } = new ArtifactsDestination();
    public ArtifactsCharacter Character { get; set; } = new ArtifactsCharacter();
}

public class FightData
{
    public ArtifactsCooldown Cooldown { get; set; } = new ArtifactsCooldown();
    public ArtifactsCharacter Character { get; set; } = new ArtifactsCharacter();
    public ArtifactsFight Fight { get; set; } = new ArtifactsFight();
}

public class GatherData
{
    public ArtifactsCooldown Cooldown { get; set; } = new ArtifactsCooldown();
    public ArtifactsCharacter Character { get; set; } = new ArtifactsCharacter();
    public ArtifactsGather Gather { get; set; } = new ArtifactsGather();
}

public class BankDepositData
{
    public ArtifactsCooldown Cooldown { get; set; } = new ArtifactsCooldown();
    public ArtifactsCharacter Character { get; set; } = new ArtifactsCharacter();

    //There's also bank and item data, but we don't need it for now
}

public class BankWithdrawData
{
    public ArtifactsCooldown Cooldown { get; set; } = new ArtifactsCooldown();
    public ArtifactsCharacter Character { get; set; } = new ArtifactsCharacter();

    //There's also bank and item data, but we don't need it for now
}

public class CraftItemData
{
    public ArtifactsCooldown Cooldown { get; set; } = new ArtifactsCooldown();
    public ArtifactsCharacter Character { get; set; } = new ArtifactsCharacter();

    //There's also item data, but we don't need it for now
}

public class RestData
{
    public ArtifactsCooldown Cooldown { get; set; } = new ArtifactsCooldown();
    public ArtifactsCharacter Character { get; set; } = new ArtifactsCharacter();
    public int Hp_restored { get; set; }
}

public class ArtifacstResponse<T>(T data)
{
    public T Data { get; private set; } = data;
}

public abstract class ArtifactsInventoryFull;