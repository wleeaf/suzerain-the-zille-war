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

    public string FullReport(ObjectiveInfo info, bool won)
    {
        var s = new System.Text.StringBuilder();
        s.Append($"OBJECTIVE: {info.Aim}\n");
        s.Append(won ? "RESULT: Objective achieved.\n\n" : "RESULT: Objective not achieved.\n\n");
        s.Append($"GROUND: {Headline}\n");
        if (CitiesTaken.Count > 0)
        {
            s.Append($"CITIES TAKEN: {Join(CitiesTaken)}.\n");
        }

        if (CitiesRetaken.Count > 0)
        {
            s.Append($"CITIES LIBERATED: {Join(CitiesRetaken)}.\n");
        }

        if (CitiesLost.Count > 0)
        {
            s.Append($"CITIES LOST: {Join(CitiesLost)}.\n");
        }

        s.Append($"\nCASUALTIES: Royal Army, {PlayerDead} units destroyed. Wehzek forces, {EnemyDead} units destroyed.\n");
        s.Append($"ENEMY: Intelligence now rates the Wehzek garrison as {ZilleCampaign.Tier.ToLowerInvariant()}. ");
        s.Append(EnemyCitiesLeft.Count > 0 ? $"Wehlen still holds {Join(EnemyCitiesLeft)}.\n" : "Wehlen no longer holds any city on the mainland.\n");

        List<string> notes = EventNotes();
        if (notes.Count > 0)
        {
            s.Append("\nFROM THE FIELD:\n");
            foreach (string n in notes)
            {
                s.Append($"- {n}\n");
            }
        }

        s.Append("\nASSESSMENT: ");
        s.Append(EnemyCitiesLeft.Count == 0
            ? "The General Staff believes the remaining Wehzek positions cannot hold for long."
            : won
                ? $"The General Staff recommends pressing the advantage before Wehlen can reinforce {EnemyCitiesLeft[0]}."
                : "The General Staff requests time to regroup, and warns that every week of delay lets Wehlen bring more men across the Strait of Kreleck.");
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
