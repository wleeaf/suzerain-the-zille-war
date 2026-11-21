using System.Text.Json;
using System.Text.Json.Serialization;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

namespace ZilleWar;

/// <summary>Tile spec exported by tools/mapgen/export_tiles.py.</summary>
internal sealed class TileSpec
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }          // Land | Sea
    [JsonPropertyName("region")] public string Region { get; set; }      // zille | rizia | sea
    [JsonPropertyName("neighbors")] public List<string> Neighbors { get; set; } = new();
    [JsonPropertyName("shore")] public bool Shore { get; set; }
    [JsonPropertyName("border")] public bool Border { get; set; }
    [JsonPropertyName("mountain")] public bool Mountain { get; set; }
    [JsonPropertyName("fort")] public bool Fort { get; set; }
    [JsonPropertyName("town")] public string Town { get; set; }
    [JsonPropertyName("defence")] public string Defence { get; set; }
    [JsonPropertyName("quad_center_uv")] public float[] QuadCenterUv { get; set; }
    [JsonPropertyName("quad_size_uv")] public float[] QuadSizeUv { get; set; }
    [JsonPropertyName("centroid_uv")] public float[] CentroidUv { get; set; }
}

internal sealed class TownSpec
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("side")] public string Side { get; set; }          // zille | rizia
    [JsonPropertyName("role")] public string Role { get; set; }          // city | town | fort
    [JsonPropertyName("capital")] public bool Capital { get; set; }
    [JsonPropertyName("tile")] public string Tile { get; set; }
}

internal sealed class TileFile
{
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("towns")] public List<TownSpec> Towns { get; set; } = new();
    [JsonPropertyName("tiles")] public List<TileSpec> Tiles { get; set; } = new();
}

/// <summary>Loads the Zille map textures and tile data from UserData/ZilleWar/map.</summary>
internal static class ZilleData
{
    public static string Dir => Path.Combine(MelonEnvironment.UserDataDirectory, "ZilleWar", "map");

    public static List<TileSpec> Tiles { get; private set; }
    public static List<TownSpec> Towns { get; private set; }
    public static Dictionary<string, TileSpec> ByName { get; private set; }

    private static Texture2D[] _panels;
    private static readonly Dictionary<string, Texture2D> _tileTextures = new();

    public static bool LoadSpec()
    {
        string path = Path.Combine(Dir, "zille_tiles.json");
        if (!File.Exists(path))
        {
            Core.Log.Error($"Missing {path}");
            return false;
        }

        TileFile f = JsonSerializer.Deserialize<TileFile>(File.ReadAllText(path));
        Tiles = f.Tiles;
        Towns = f.Towns;
        ByName = Tiles.ToDictionary(t => t.Name);
        Core.Log.Msg($"Loaded {Tiles.Count} Zille tiles, {Towns.Count} towns.");
        return true;
    }

    /// <summary>Six 4096² panels, top-left to bottom-right (same order as Pales Map_01..06).</summary>
    public static Texture2D[] Panels
    {
        get
        {
            if (_panels == null)
            {
                _panels = new Texture2D[6];
                for (int i = 0; i < 6; i++)
                {
                    _panels[i] = LoadTexture(Path.Combine(Dir, $"map_0{i + 1}.png"), mipmaps: true, readable: false);
                    _panels[i].name = $"T_Map_Zille_0{i + 1}";
                }
            }

            return _panels;
        }
    }

    public static Texture2D TileTexture(string name)
    {
        if (!_tileTextures.TryGetValue(name, out Texture2D tex) || tex == null)
        {
            // Tile masks must stay CPU-readable: hover/picking samples their alpha.
            tex = LoadTexture(Path.Combine(Dir, "tiles", $"{name}.png"), mipmaps: false, readable: true);
            tex.name = $"ZilleTile_{name}";
            _tileTextures[name] = tex;
        }

        return tex;
    }

    private static Texture2D LoadTexture(string path, bool mipmaps, bool readable)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipmaps)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        if (!ImageConversion.LoadImage(tex, bytes, !readable))
        {
            Core.Log.Error($"Failed to decode {path}");
        }

        tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return tex;
    }
}
