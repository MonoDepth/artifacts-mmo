namespace api_mmo.ArtifactsApi;

public class ArtifactsCharacter
{
  public string Name { get; set; } = "";
  public string Account { get; set; } = "";
  public string Skin { get; set; } = "";
  public int Level { get; set; }
  public int Xp { get; set; }
  public int Max_xp { get; set; }
  public int Gold { get; set; }
  public int Speed { get; set; }
  public int Mining_level { get; set; }
  public int Mining_xp { get; set; }
  public int Mining_max_xp { get; set; }
  public int Woodcutting_level { get; set; }
  public int Woodcutting_xp { get; set; }
  public int Woodcutting_max_xp { get; set; }
  public int Fishing_level { get; set; }
  public int Fishing_xp { get; set; }
  public int Fishing_max_xp { get; set; }
  public int Weaponcrafting_level { get; set; }
  public int Weaponcrafting_xp { get; set; }
  public int Weaponcrafting_max_xp { get; set; }
  public int Gearcrafting_level { get; set; }
  public int Gearcrafting_xp { get; set; }
  public int Gearcrafting_max_xp { get; set; }
  public int Jewelrycrafting_level { get; set; }
  public int Jewelrycrafting_xp { get; set; }
  public int Jewelrycrafting_max_xp { get; set; }
  public int Cooking_level { get; set; }
  public int Cooking_xp { get; set; }
  public int Cooking_max_xp { get; set; }
  public int Alchemy_level { get; set; }
  public int Alchemy_xp { get; set; }
  public int Alchemy_max_xp { get; set; }
  public int Hp { get; set; }
  public int Max_hp { get; set; }
  public int Haste { get; set; }
  public int Critical_strike { get; set; }
  public int Wisdom { get; set; }
  public int Prospecting { get; set; }
  public int Attack_fire { get; set; }
  public int Attack_earth { get; set; }
  public int Attack_water { get; set; }
  public int Attack_air { get; set; }
  public int Dmg { get; set; }
  public int Dmg_fire { get; set; }
  public int Dmg_earth { get; set; }
  public int Dmg_water { get; set; }
  public int Dmg_air { get; set; }
  public int Res_fire { get; set; }
  public int Res_earth { get; set; }
  public int Res_water { get; set; }
  public int Res_air { get; set; }
  public int X { get; set; }
  public int Y { get; set; }
  public int Cooldown { get; set; }
  public DateTime? Cooldown_expiration { get; set; }
  public string Weapon_slot { get; set; } = "";
  public string Rune_slot { get; set; } = "";
  public string Shield_slot { get; set; } = "";
  public string Helmet_slot { get; set; } = "";
  public string Body_armor_slot { get; set; } = "";
  public string Leg_armor_slot { get; set; } = "";
  public string Boots_slot { get; set; } = "";
  public string Ring1_slot { get; set; } = "";
  public string Ring2_slot { get; set; } = "";
  public string Amulet_slot { get; set; } = "";
  public string Artifact1_slot { get; set; } = "";
  public string Artifact2_slot { get; set; } = "";
  public string Artifact3_slot { get; set; } = "";
  public string Utility1_slot { get; set; } = "";
  public int Utility1_slot_quantity { get; set; }
  public string Utility2_slot { get; set; } = "";
  public int Utility2_slot_quantity { get; set; }
  public string Bag_slot { get; set; } = "";
  public string Task { get; set; } = "";
  public string Task_type { get; set; } = "";
  public int Task_progress { get; set; }
  public int Task_total { get; set; }
  public int Inventory_max_items { get; set; }
  public List<ArtifactsInventory> Inventory { get; set; } = [];
}

public interface IEnumerableProperty
{
  public string GetItemKey();
}

public class ArtifactsInventory: IEnumerableProperty
{
  public int Slot { get; set; }
  public string Code { get; set; } = "";
  public int Quantity { get; set; }

  public string GetItemKey() => Code;
}

public class ArtifactsCooldown {
  public int Total_seconds { get; set; }
  public int Remaining_seconds { get; set; }
  public DateTime Started_at { get; set; }
  public DateTime Expiration { get; set; }
  public string Reason { get; set; } = "";
}

public class ArtifactsDestination
{
  public string Name { get; set; } = "";
  public string Skin { get; set; } = "";
  public int X { get; set; }
  public int Y { get; set; }
  public ArtifactsDestinationContent Content { get; set; } = new ArtifactsDestinationContent();
}

public class ArtifactsDestinationContent
{
  public string Type { get; set; } = "";
  public string Code { get; set; } = "";
}

public class ArtifactsFight
{
  public int Xp { get; set; }
  public int Gold { get; set; }
  public int Turns { get; set; }
  public string Result { get; set; } = "";
  public List<string> Logs { get; set; } = [];
  public ArtifactsBlockedHits Monster_blocked_hits { get; set; } = new ArtifactsBlockedHits();
  public ArtifactsBlockedHits Player_blocked_hits { get; set; } = new ArtifactsBlockedHits();
  public bool IsWin => Result == "win";
}

public class ArtifactsBlockedHits
{
  public int Fire { get; set; }
  public int Earth { get; set; }
  public int Water { get; set; }
  public int Air { get; set; }
  public int Total { get; set; }
}

public class ArtifactsGather
{
  public int Xp { get; set; }
  public List<ArtifactsItem> Items { get; set; } = [];
}

public class ArtifactsItem
{
  public string Code { get; set; } = "";
  public int Quantity { get; set; }
}