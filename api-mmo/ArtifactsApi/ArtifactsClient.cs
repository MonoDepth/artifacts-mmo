using api_mmo.Rest;


namespace api_mmo.ArtifactsApi;
public class ArtifactsClient
{
    const string API_ENDPOINT = "https://api.artifactsmmo.com";
    readonly string ApiKey;
    readonly RestClient _httpClient;

    public ArtifactsClient(string apiKey)
    {
        ApiKey = apiKey;
        _httpClient = new RestClient();
        _httpClient.Client.BaseAddress = new Uri(API_ENDPOINT);
        _httpClient.Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
    }

    public async Task<Maybe<List<PlayerCharacter>>> GetPlayerCharactersAsync()
    {
        var result = await _httpClient.GetAsync("/my/characters");
        if (result.IsSuccessStatusCode)
        {
            var playerCharacters = new List<PlayerCharacter>();
            var artifactsCharacter = await result.Deserialize<ArtifacstResponse<List<ArtifactsCharacter>>>();

            if (artifactsCharacter == null)
            {
                return Maybe<List<PlayerCharacter>>.Failure("Failed to deserialize response");
            }

            playerCharacters.AddRange(artifactsCharacter.Data.Select(c => new PlayerCharacter(this, c)));
            return Maybe<List<PlayerCharacter>>.Success(playerCharacters);
        }
        else
        {
            return Maybe<List<PlayerCharacter>>.Failure($"Failed to get player characters with response code: {(int)result.StatusCode}");
        }
    }

    public async Task<Maybe<MoveResponseData>> MoveCharacterAsync(string characterName, int x, int y)
    {
        var result = await _httpClient.PostAsync($"/my/{characterName}/action/move", new { x, y });
        if (result.IsSuccessStatusCode)
        {
            var moveResponse = await result.Deserialize<ArtifacstResponse<MoveResponseData>>();

            if (moveResponse == null)
            {
                return Maybe<MoveResponseData>.Failure("Failed to deserialize response");
            }

            return Maybe<MoveResponseData>.Success(moveResponse.Data);
        }
        else
        {
            return Maybe<MoveResponseData>.Failure($"Failed to move character with response code: {(int)result.StatusCode}");
        }
    }

    /// <summary>
    /// Attack the character at the current position
    /// </summary>
    /// <param name="characterName">Character name</param>
    /// <returns><see cref="FightData"/> if successfull, <see cref="ArtifactsInventoryFull"/> if full inventory else message failure message</returns>
    public async Task<Maybe> AttackCharacterAsync(string characterName)
    {
        var result = await _httpClient.PostAsync($"/my/{characterName}/action/fight");
        if (result.IsSuccessStatusCode)
        {
            var fightData = await result.Deserialize<ArtifacstResponse<FightData>>();

            if (fightData == null)
            {
                return Maybe<FightData>.Failure("Failed to deserialize response");
            }

            return Maybe<FightData>.Success(fightData.Data);
        }
        else if ((int)result.StatusCode == 497)
        {
            return Maybe<ArtifactsInventoryFull>.Failure("Inventory is full");
        }
        else
        {
            return Maybe<FightData>.Failure($"Failed to attack character with response code: {(int)result.StatusCode}");
        }
    }

    /// <summary>
    /// Gather the resource at the current position
    /// </summary>
    /// <param name="characterName">Character name</param>
    /// <returns><see cref="GatherData"/> if successfull, <see cref="ArtifactsInventoryFull"/> if full inventory else message failure message</returns>
    public async Task<Maybe> GatherCharacterAsync(string characterName)
    {
        var result = await _httpClient.PostAsync($"/my/{characterName}/action/gathering");
        if (result.IsSuccessStatusCode)
        {
            var gahterData = await result.Deserialize<ArtifacstResponse<GatherData>>();

            if (gahterData == null)
            {
                return Maybe<GatherData>.Failure("Failed to deserialize response");
            }

            return Maybe<GatherData>.Success(gahterData.Data);
        }
        else if ((int)result.StatusCode == 497)
        {
            return Maybe<ArtifactsInventoryFull>.Failure("Inventory is full");
        }
        else
        {
            return Maybe<GatherData>.Failure($"Failed to gather with response code: {(int)result.StatusCode}");
        }
    }

    public async Task<Maybe<BankDepositData>> BankDepositAsync(string characterName, string itemCode, int quantity)
    {
        var result = await _httpClient.PostAsync($"/my/{characterName}/action/bank/deposit", new { code = itemCode, quantity });
        if (result.IsSuccessStatusCode)
        {
            var depositData = await result.Deserialize<ArtifacstResponse<BankDepositData>>();

            if (depositData == null)
            {
                return Maybe<BankDepositData>.Failure("Failed to deserialize response");
            }

            return Maybe<BankDepositData>.Success(depositData.Data);
        }
        else
        {
            return Maybe<BankDepositData>.Failure($"Failed to deposit with response code: {(int)result.StatusCode}");
        }
    }

    public async Task<Maybe<BankWithdrawData>> BankWithdrawAsync(string characterName, string itemCode, int quantity)
    {
        var result = await _httpClient.PostAsync($"/my/{characterName}/action/bank/withdraw", new { code = itemCode, quantity });
        if (result.IsSuccessStatusCode)
        {
            var withdrawalData = await result.Deserialize<ArtifacstResponse<BankWithdrawData>>();

            if (withdrawalData == null)
            {
                return Maybe<BankWithdrawData>.Failure("Failed to deserialize response");
            }

            return Maybe<BankWithdrawData>.Success(withdrawalData.Data);
        }
        else
        {
            return Maybe<BankWithdrawData>.Failure($"Failed to withdraw with response code: {(int)result.StatusCode}");
        }
    }

    public async Task<Maybe<CraftItemData>> CraftItemAsync(string characterName, string itemCode, int quantity)
    {
        var result = await _httpClient.PostAsync($"/my/{characterName}/action/crafting", new { code = itemCode, quantity });
        if (result.IsSuccessStatusCode)
        {
            var craftItemData = await result.Deserialize<ArtifacstResponse<CraftItemData>>();

            if (craftItemData == null)
            {
                return Maybe<CraftItemData>.Failure("Failed to deserialize response");
            }

            return Maybe<CraftItemData>.Success(craftItemData.Data);
        }
        else
        {
            return Maybe<CraftItemData>.Failure($"Failed to craft with response code: {(int)result.StatusCode}");
        }
    }

    public async Task<Maybe<RestData>> RestCharacterAsync(string characterName)
    {
        var result = await _httpClient.PostAsync($"/my/{characterName}/action/rest");
        if (result.IsSuccessStatusCode)
        {
            var restData = await result.Deserialize<ArtifacstResponse<RestData>>();

            if (restData == null)
            {
                return Maybe<RestData>.Failure("Failed to deserialize response");
            }

            return Maybe<RestData>.Success(restData.Data);
        }
        else
        {
            return Maybe<RestData>.Failure($"Failed to rest character with response code: {(int)result.StatusCode}");
        }
    }
}

public interface IArtifactsEntity {
    public ArtifactsClient Client { get; set; }
}