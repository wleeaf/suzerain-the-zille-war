using System.Text.Json;
using HarmonyLib;
using Il2Cpp;
using SuzerainModdingKit;
using UnityEngine;

namespace ZilleWar;

/// <summary>
/// The national map follows the war, the way vanilla's own map follows the Pales war ("Pales divided").
/// - A divided-Zille layer: while the war has been declared and Rizia holds part of Zille without having liberated all of
///   it, the part Rizia holds is painted into Brenas (or the Crown's colours, per the Turn 7 administration choice), the
///   rest stays Wehzek, and an orange Wehzek band runs along the front. It is built at runtime from the vanilla Zille layer
///   art and a tile index map (art/build/strategic), and redrawn whenever the front moves. Full liberation is vanilla's
///   own MapChange_Zille (run by LateState).
/// - Map statuses: Rizian cities held by Wehlen get vanilla's "Under Occupation" badge; the Zille event token shows a
///   "Front Line" badge during the fighting and a "Divided Territory" badge after a ceasefire or defeat that leaves Rizia
///   part of it; Zille city's own token appears (under military administration) once it is in Rizian hands.
/// </summary>
internal static class StrategicMap
{
    private const string LayerObjectName = "MapLayer Zille Divided (ZilleWar)";
    private const string BadgeFront = "ZilleWar_Zille_FrontLine";
    private const string BadgeDivided = "ZilleWar_Zille_Divided";
    private const string Occupied = "Rizia_City_UnderOccupation";
    private const string VarBadges = "ZilleWar.Map_Badges";      // badges currently applied, "token:status;..."

    private static readonly (string town, string token)[] RizianCities =
    {
        ("Mave Nortus", "Rizia_City_MaveNortus"), ("Jales", "Rizia_City_Jales"),
    };

    private static Texture2D _brenas, _ownState, _index, _composed;
    private static string[] _zilleTiles;
    private static string _composedKey;

    public static void Register()
    {
        Variables.Register(VarBadges);
    }

    // ------------------------------------------------------------------ the divided layer

    /// <summary>Held Zille tiles, or null when the map should show vanilla Zille (no war, or fully liberated).</summary>
    private static HashSet<string> HeldZilleTiles()
    {
        if (!Variables.GetBool(WarFlags.Declared) || ZilleCampaign.Liberated || Variables.GetBool("RiziaDLC.ZilleReturn_Success"))
        {
            return null;
        }

        var held = ZilleCampaign.CurrentOwners().Where(kv => kv.Value && ZilleData.ByName.TryGetValue(kv.Key, out TileSpec t) && t.Region == "zille")
            .Select(kv => kv.Key).ToHashSet();
        return held.Count == 0 ? null : held;
    }

    internal static void ApplyLayer(MapLayerManager mgr)
    {
        if (mgr?.mapLayers == null || !SuzerainModdingKit.GameState.IsCurrentStoryPack(SuzerainModdingKit.VanillaData.SuzerainStoryPackInfo.Rizia))
        {
            return;
        }

        MapLayer template = null;
        MapLayer ours = null;
        foreach (MapLayer l in mgr.mapLayers)
        {
            if (l != null && l.mapLayerNameInDatabase == "MapLayer_Zille_Brenas")
            {
                template = l;
            }
        }

        Transform parent = template != null ? template.transform.parent : null;
        Transform existing = parent != null ? parent.Find(LayerObjectName) : null;
        if (existing != null)
        {
            ours = existing.GetComponent<MapLayer>();
        }

        HashSet<string> held = HeldZilleTiles();
        if (held == null)
        {
            ours?.SetMapLayerState(false);
            return;
        }

        if (template == null || !LoadArt())
        {
            return;
        }

        if (ours == null)
        {
            GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, parent);
            clone.name = LayerObjectName;
            ours = clone.GetComponent<MapLayer>();
            ours.mapLayerNameInDatabase = "MapLayer_Zille_Divided";
            // a hair above the vanilla layers so it is drawn over the base map like them
            clone.transform.localPosition += new Vector3(0, 0, -0.00002f);
        }

        bool crown = Variables.GetString(EarlyT7.VarZilleAdmin) == "imperii";
        string key = (crown ? "C:" : "B:") + string.Join(",", held.OrderBy(x => x, StringComparer.Ordinal));
        if (_composed == null || _composedKey != key)
        {
            _composed = Compose(crown ? _ownState : _brenas, held);
            _composedKey = key;
            Core.Log.Msg($"Divided Zille map layer drawn: {held.Count} of {_zilleTiles.Length} Zille districts in Rizian hands.");
        }

        foreach (Renderer r in ours.modelContainer.GetComponentsInChildren<Renderer>(true))
        {
            var mat = new Material(r.sharedMaterial);
            mat.SetTexture("_Diffuse", _composed);
            r.sharedMaterial = mat;
        }

        ours.SetMapLayerState(true);
    }

    private static bool LoadArt()
    {
        if (_brenas != null && _index != null && _ownState != null && _zilleTiles != null)
        {
            return true;
        }

        try
        {
            string dir = Path.Combine(ZilleData.Dir, "strategic");
            Texture2D Load(string file)
            {
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                ImageConversion.LoadImage(t, File.ReadAllBytes(Path.Combine(dir, file)), false);
                return t;
            }

            _brenas = Load("zille_brenas.png");
            _ownState = Load("zille_ownstate.png");
            _index = Load("zille_tile_index.png");
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "zille_tile_index.json")));
            _zilleTiles = doc.RootElement.GetProperty("tiles").EnumerateArray().Select(e => e.GetString()).ToArray();
            return true;
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Divided Zille map art could not be loaded: {ex.Message}");
            return false;
        }
    }

    /// <summary>The layer art with the Wehzek-held districts cut out and an orange Wehzek band along the new front.</summary>
    private static Texture2D Compose(Texture2D art, HashSet<string> held)
    {
        int w = art.width, h = art.height;
        Color32[] src = art.GetPixels32();
        Color32[] idx = _index.GetPixels32();
        var heldIndex = new bool[256];
        for (int i = 0; i < _zilleTiles.Length; i++)
        {
            heldIndex[i + 1] = held.Contains(_zilleTiles[i]);
        }

        var outPx = new Color32[src.Length];
        var keepZille = new bool[src.Length];     // Rizian-held Zille pixels
        var wehzek = new bool[src.Length];        // Wehzek-held Zille pixels
        for (int p = 0; p < src.Length; p++)
        {
            int k = idx[p].r;
            if (k == 0)
            {
                outPx[p] = src[p];
            }
            else if (heldIndex[k])
            {
                outPx[p] = src[p];
                keepZille[p] = true;
            }
            else
            {
                outPx[p] = new Color32(0, 0, 0, 0);
                wehzek[p] = src[p].a > 0;
            }
        }

        // Wehzek border band: Wehzek pixels within R of a Rizian-held Zille pixel (separable box dilation).
        const int R = 7;
        bool[] near = Dilate(keepZille, w, h, R);
        var band = new Color32(242, 132, 52, 255);
        for (int p = 0; p < src.Length; p++)
        {
            if (wehzek[p] && near[p])
            {
                outPx[p] = band;
            }
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(outPx);
        tex.Apply(false, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    private static bool[] Dilate(bool[] m, int w, int h, int r)
    {
        var tmp = new bool[m.Length];
        for (int y = 0; y < h; y++)
        {
            int row = y * w, last = -100000;
            for (int x = 0; x < w; x++)
            {
                if (m[row + x])
                {
                    last = x;
                }

                tmp[row + x] = x - last <= r;
            }

            last = 100000;
            for (int x = w - 1; x >= 0; x--)
            {
                if (m[row + x])
                {
                    last = x;
                }

                tmp[row + x] |= last - x <= r;
            }
        }

        var outM = new bool[m.Length];
        for (int x = 0; x < w; x++)
        {
            int last = -100000;
            for (int y = 0; y < h; y++)
            {
                if (tmp[y * w + x])
                {
                    last = y;
                }

                outM[y * w + x] = y - last <= r;
            }

            last = 100000;
            for (int y = h - 1; y >= 0; y--)
            {
                if (tmp[y * w + x])
                {
                    last = y;
                }

                outM[y * w + x] |= last - y <= r;
            }
        }

        return outM;
    }

    // ------------------------------------------------------------------ map statuses

    private static void EnsureBadges()
    {
        AddBadge(BadgeFront, "Front Line", "Rizian and Wehzek troops face each other across the region. Towns change hands with every offensive.", "RedNegative");
        AddBadge(BadgeDivided, "Divided Territory",
            "The line of contact runs through the region, watched by Alliance observers. Rizia administers the districts it took; Wehlen holds the rest.", "OrangeMild");
    }

    private static void AddBadge(string name, string title, string subtitle, string color)
    {
        TokenStatusEffectData template = null;
        foreach (TokenStatusEffectData d in EntityDataManager.tokenStatusEffectsData)
        {
            if (d?.NameInDatabase == name)
            {
                return;
            }

            if (d?.NameInDatabase == Occupied)
            {
                template = d;
            }
        }

        if (template == null)
        {
            return;
        }

        var data = new TokenStatusEffectData
        {
            Id = $"0x5A12{(uint)name.GetHashCode():X8}",
            NameInDatabase = name,
            Path = "Rizia/Tokens/Token Status Effects/Military",
            AppBundleProperties = template.AppBundleProperties,
            TagsProperties = template.TagsProperties,
            TokenStatusEffectProperties = new TokenStatusEffectProperties
            {
                Title = title,
                Subtitle = subtitle,
                ImagePath = template.TokenStatusEffectProperties.ImagePath,
                ImageColorString = color,
                ProgressPercentage = -1,
                StartingTurn = -1,
                EndingTurn = -1,
                CodexEntryTitle = string.Empty,
                OnAddStatusEffectInstruction = string.Empty,
                OnRemoveStatusEffectInstruction = string.Empty,
                OnProgressCompletedInstruction = string.Empty,
            },
        };
        ZilleOperation.AddByName(EntityDataManager.tokenStatusEffectsData, data, d => d.NameInDatabase);
        ZilleOperation.AddByName(EntityDataManager.TokenStatusEffectsData, data, d => d.NameInDatabase);
    }

    private static void Lua(string code)
    {
        try
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run(code);
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Map status '{code}' failed: {ex.Message}");
        }
    }

    /// <summary>Called every story step: bring the map's badges and tokens in line with the war.</summary>
    public static void OnStep()
    {
        if (!Variables.GetBool(WarFlags.Declared))
        {
            return;
        }

        try
        {
            ApplyLayer(Managers.Instance?.mapLayerManager);    // the front may have moved since the map was set up
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Divided Zille layer failed: {ex.Message}");
        }

        EnsureBadges();
        var want = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, bool> owners = ZilleCampaign.CurrentOwners();
        bool liberated = ZilleCampaign.Liberated || Variables.GetBool("RiziaDLC.ZilleReturn_Success");

        foreach ((string town, string token) in RizianCities)
        {
            TownSpec t = ZilleData.Towns.FirstOrDefault(x => x.Name == town);
            if (t?.Tile != null && owners.TryGetValue(t.Tile, out bool ours) && !ours)
            {
                want.Add($"{token}:{Occupied}");
            }
        }

        if (!liberated && HeldZilleTiles() != null)
        {
            want.Add($"Rizia_Event_Zille:{(WarFlow.AtWar ? BadgeFront : BadgeDivided)}");
        }
        else if (!liberated && WarFlow.AtWar)
        {
            want.Add($"Rizia_Event_Zille:{BadgeFront}");
        }

        string saved = Variables.GetString(VarBadges);
        var have = new HashSet<string>((string.IsNullOrEmpty(saved) || saved == "nil" ? string.Empty : saved)
            .Split(';', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        foreach (string b in have.Except(want))
        {
            string[] p = b.Split(':');
            Lua($"RemoveTokenStatusImmediately(\"{p[0]}\", \"{p[1]}\")");
        }

        foreach (string b in want.Except(have))
        {
            string[] p = b.Split(':');
            Lua($"AddTokenStatusImmediately(\"{p[0]}\", \"{p[1]}\")");
        }

        Variables.Set(VarBadges, string.Join(";", want));

        // Zille city's own token, under military administration, once it is Rizian and the region isn't fully returned.
        bool cityHeld = Variables.GetBool(WarFlags.ZilleCityHeld);
        if (!liberated)
        {
            Lua(cityHeld ? "EnableToken(\"Rizia_City_Zille\"); UpdateCitySubtitle(\"Rizia_City_Zille\", \"Military Administration\")"
                         : "DisableToken(\"Rizia_City_Zille\")");
        }
    }
}

/// <summary>The map's layers are set up when the national map loads and when the story moves on: draw ours alongside.</summary>
[HarmonyPatch(typeof(MapLayerManager), nameof(MapLayerManager.Setup))]
internal static class Patch_MapLayers
{
    private static void Postfix(MapLayerManager __instance)
    {
        try
        {
            StrategicMap.ApplyLayer(__instance);
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Divided Zille layer failed: {ex.Message}");
        }
    }
}
