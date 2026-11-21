using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>What happened in one operation, compared with the map it started from. Feeds the detailed
/// after-action report shown at the end of the operation and the newspaper coverage.</summary>
internal sealed class AfterAction
{
    public int EnemyDead, PlayerDead, TilesGained, TilesLost;
    public List<string> CitiesTaken = new(), CitiesLost = new(), CitiesRetaken = new(), EnemyCitiesLeft = new();
    public string Headline = string.Empty, CitiesTakenSentence = string.Empty, LossesSentence = string.Empty;

    public static AfterAction Compute(int k, Dictionary<string, bool> before, Dictionary<string, bool> after)
    {
        var a = new AfterAction
        {
            EnemyDead = Variables.GetInt(ZilleCampaign.EnemyDeadVar(k)),
            PlayerDead = Variables.GetInt(ZilleCampaign.PlayerDeadVar(k)),
        };
        foreach ((string tile, bool nowPlayer) in after)
        {
            bool wasPlayer = before.TryGetValue(tile, out bool w) && w;
            if (nowPlayer && !wasPlayer)
            {
                a.TilesGained++;
            }
            else if (!nowPlayer && wasPlayer)
            {
                a.TilesLost++;
            }
        }

        foreach (TownSpec t in ZilleData.Towns.Where(t => t.Role == "city" && t.Tile != null))
        {
            bool was = before.TryGetValue(t.Tile, out bool w) && w;
            bool now = after.TryGetValue(t.Tile, out bool n) && n;
            if (now && !was)
            {
                (t.Side == "zille" ? a.CitiesTaken : a.CitiesRetaken).Add(t.Name);
            }
            else if (!now && was)
            {
                a.CitiesLost.Add(t.Name);
            }

            if (!now && t.Side == "zille")
            {
                a.EnemyCitiesLeft.Add(t.Name);
            }
        }

        a.Headline = a.TilesGained > 0
            ? $"Rizian forces advanced across {a.TilesGained} districts of Zille{(a.TilesLost > 0 ? $" while ceding {a.TilesLost} elsewhere" : string.Empty)}."
            : a.TilesLost > 0 ? $"Wehzek counterattacks pushed our lines back across {a.TilesLost} districts." : "The front line barely moved.";
        a.CitiesTakenSentence = a.CitiesTaken.Count > 0 ? $"Rizian flags now fly over {Join(a.CitiesTaken)}." : string.Empty;
        a.LossesSentence = a.CitiesLost.Count > 0 ? $"And {Join(a.CitiesLost)} has been lost to the enemy." : string.Empty;
        return a;
    }

    /// <summary>Estimated men lost: a destroyed formation is roughly a battalion with its support.</summary>
    public string PlayerDeadMen => Men(PlayerDead, 7);
    public string EnemyDeadMen => Men(EnemyDead, 13);

    private static string Men(int units, int salt) => units <= 0 ? "a handful of" : $"{units * 850 + (units * 137 + salt * 31) % 300:N0}";

    public string FullReport(ObjectiveInfo info, bool won)
    {
        var s = new System.Text.StringBuilder();
        s.Append("GENERAL STAFF OF THE ROYAL RIZIAN ARMY, MONTAKLAR\n");
        s.Append($"After-Action Report: {info.Codename}\n");
        s.Append("Classification: For His Majesty's eyes only\n\n");

        s.Append($"1. OBJECTIVE\n{info.Aim}\n\n");

        s.Append("2. OUTCOME\n");
        s.Append(won
            ? $"The objective has been achieved. {(info.Target != null ? $"{info.Target} is in Rizian hands." : "No Wehzek unit remains on the mainland.")}\n\n"
            : $"The objective has not been achieved. {(info.Target != null ? $"{info.Target} remains in Wehzek hands." : "Wehzek units still hold ground on the mainland.")}\n\n");

        s.Append($"3. THE FRONT\n{Headline}");
        if (CitiesTaken.Count > 0)
        {
            s.Append($" Cities taken: {Join(CitiesTaken)}.");
        }

        if (CitiesRetaken.Count > 0)
        {
            s.Append($" Rizian cities liberated: {Join(CitiesRetaken)}.");
        }

        if (CitiesLost.Count > 0)
        {
            s.Append($" Cities lost: {Join(CitiesLost)}.");
        }

        s.Append("\n\n4. LOSSES\n");
        s.Append($"Royal Army: {PlayerDead} formations destroyed in action, an estimated {PlayerDeadMen} men killed, wounded or missing.\n");
        s.Append($"Wehzek forces: {EnemyDead} formations destroyed, an estimated {EnemyDeadMen} men killed, wounded or captured.\n\n");

        s.Append("5. THE ENEMY\n");
        s.Append($"Intelligence rates the Wehzek garrison as {ZilleCampaign.Tier.ToLowerInvariant()}. ");
        s.Append(ZilleCampaign.Tier switch
        {
            "Strong" => "Convoys from the Wehzek mainland are crossing the Strait of Kreleck without interference, and every night brings fresh men and ammunition. ",
            "Default" => "Wehlen continues to supply the garrison by sea, though no longer in growing numbers. ",
            "Strained" => "The garrison is short of fuel and shells; convoys from the mainland arrive late, and some do not arrive at all. ",
            _ => "Deserters report half rations, idle tanks without fuel and rifles without ammunition. ",
        });
        s.Append(EnemyCitiesLeft.Count > 0 ? $"Wehlen still holds {Join(EnemyCitiesLeft)}.\n" : "Wehlen no longer holds any city on the mainland.\n");

        List<string> notes = EventNotes();
        if (notes.Count > 0)
        {
            s.Append("\n6. FROM THE FIELD\n");
            foreach (string n in notes)
            {
                s.Append($"- {n}\n");
            }
        }

        s.Append($"\n{(notes.Count > 0 ? 7 : 6)}. ASSESSMENT\n");
        s.Append(EnemyCitiesLeft.Count == 0
            ? "The remaining Wehzek positions cannot hold for long. The Staff recommends an operation to clear them before Wehlen can evacuate its men or land new ones."
            : won
                ? $"The enemy is off balance. The Staff recommends pressing the advantage before Wehlen can reinforce {EnemyCitiesLeft[0]}."
                : "The Staff requests time to regroup and resupply, and warns that every week of delay lets Wehlen bring more men across the Strait of Kreleck.");
        s.Append("\n\nGeneral Carlos Azaro, Chief of the General Staff");
        return s.ToString();
    }

    private static List<string> EventNotes()
    {
        var notes = new List<string>();
        void Note(string flag, string text)
        {
            if (Variables.GetBool(CityEvents.Flag(flag)))
            {
                notes.Add(text);
            }
        }

        Note("Zille_BannerRaised", "In Zille, the Golden Banner was raised over the old Duke Fedo Sazon square.");
        Note("Zille_Restraint", "In Zille, our troops were ordered to protect the Wehzek residents. Foreign correspondents have taken note.");
        Note("Nanta_PortSeized", "At Nanta, the Wehzek supply ships and their stores were seized for our own army.");
        Note("Nanta_PortSabotaged", "At Nanta, the piers were scuttled. Nothing will land there until the war is over.");
        Note("Isor_Fishermen", "The fishing families of Isor have hung the old colors from their boats.");
        Note("Auremina_MinesSecured", "The Auremina gold mines are under guard.");
        Note("Auremina_MinersFreed", "The miners of Auremina have been declared free men of Rizia.");
        Note("Evacuation", "Civilians were evacuated ahead of the Wehzek advance.");
        Note("Counterattack", "Our commanders were ordered to retake every position lost.");
        return notes;
    }

    private static string Join(List<string> l) =>
        l.Count <= 1 ? string.Join("", l) : string.Join(", ", l.Take(l.Count - 1)) + " and " + l[^1];
}
