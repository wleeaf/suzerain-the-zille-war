using System.Collections;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace ZilleWar;

/// <summary>
/// The mod's mark on the main menu: a small title block under the game's logo, in the game's own fonts and gold,
/// "THE ZILLE WAR" between two rules and "Wleeaf Edition" beneath it, plus the mod's version beside the game's.
/// Built once per menu panel from fresh TextMeshPro objects (no cloned localizers to overwrite the text).
/// With UserData/ZilleWar/menuprobe.flag, it also writes menu_probe.txt and menu_probe.png for layout work.
/// </summary>
internal static class MainMenuBadge
{
    internal const string Version = "1.0.0";
    private const string BadgeName = "ZilleWar Badge";
    private static readonly Color Gold = new(0.965f, 0.878f, 0.612f, 1f);        // the game's notification gold
    private static readonly Color Parchment = new(0.86f, 0.82f, 0.72f, 0.92f);
    private static readonly Color Rule = new(0.965f, 0.878f, 0.612f, 0.55f);

    internal static void Apply(MainMenuPanel panel)
    {
        try
        {
            if (panel == null || panel.panelRect == null)
            {
                return;
            }

            Transform anchor = panel.logoBackground != null ? panel.logoBackground : panel.panelRect;
            if (anchor.Find(BadgeName) == null)
            {
                Build(panel, anchor);
            }

            StampVersion(panel);

            if (File.Exists(Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "ZilleWar", "menuprobe.flag")))
            {
                MelonCoroutines.Start(Probe(panel));
            }
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Main menu badge: {ex.Message}");
        }
    }

    private static void Build(MainMenuPanel panel, Transform anchor)
    {
        TMP_Text fontSource = (TMP_Text)panel.headerTitle ?? panel.versionText;
        TMP_Text bodySource = (TMP_Text)panel.versionText ?? panel.headerTitle;

        // The maroon title band is 160 high with the SUZERAIN logo centred in it. The logo moves up 20 and the badge
        // sits beneath it inside the band, as the game's subtitle: the menu below is left exactly as it was.
        Transform logo = anchor.Find("Logo");
        if (logo != null)
        {
            RectTransform lrt = logo.Cast<RectTransform>();
            lrt.anchoredPosition += new Vector2(0f, 20f);
        }

        var root = new GameObject(BadgeName);
        var rt = root.AddComponent<RectTransform>();
        rt.SetParent(anchor, false);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(440f, 62f);

        // Fine gold rules either side of the title, like the game's own separators.
        Line(rt, "Rule Left", new Vector2(-148f, 43f), 64f);
        Line(rt, "Rule Right", new Vector2(148f, 43f), 64f);

        TextMeshProUGUI title = Text(rt, "Title", "THE ZILLE WAR", fontSource, 20f, Gold, new Vector2(0f, 44f), 220f, 28f);
        title.characterSpacing = 14f;

        TextMeshProUGUI sub = Text(rt, "Edition", "Wleeaf Edition", bodySource, 14f, Parchment, new Vector2(0f, 19f), 300f, 20f);
        sub.fontStyle = FontStyles.Italic;
        sub.characterSpacing = 4f;

        Core.Log.Msg($"Main menu badge placed under {anchor.name}.");
    }

    private static TextMeshProUGUI Text(RectTransform parent, string name, string text, TMP_Text source, float size, Color color,
        Vector2 pos, float width, float height)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, height);

        var t = go.AddComponent<TextMeshProUGUI>();
        if (source != null)
        {
            t.font = source.font;
            t.fontSharedMaterial = source.fontSharedMaterial;
        }

        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    private static void Line(RectTransform parent, string name, Vector2 pos, float width)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 1.5f);
        var img = go.AddComponent<Image>();
        img.color = Rule;
        img.raycastTarget = false;
    }

    /// <summary>Adds the mod's name and version after the game's own version line, once.</summary>
    private static void StampVersion(MainMenuPanel panel)
    {
        TextMeshProUGUI v = panel.versionText;
        if (v == null || string.IsNullOrEmpty(v.text) || v.text.Contains("Zille War"))
        {
            return;
        }

        v.text += $"  ·  The Zille War {Version}";
    }

    private static IEnumerator Probe(MainMenuPanel panel)
    {
        yield return new WaitForSeconds(30f);
        string dir = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "ZilleWar");
        var sb = new StringBuilder();
        sb.AppendLine($"Menu probe, screen {Screen.width}x{Screen.height}");
        foreach (Graphic g in panel.GetComponentsInChildren<Graphic>(false))
        {
            if (g == null || !g.isActiveAndEnabled)
            {
                continue;
            }

            Rect r = ScreenRect(g.rectTransform, g.canvas);
            string extra = "";
            TMP_Text t = g.TryCast<TMP_Text>();
            if (t != null)
            {
                extra = $" font={t.font?.name} size={t.fontSize} text=\"{t.text?.Replace("\n", " | ")}\"";
            }
            else if (g.TryCast<Image>() is Image img)
            {
                extra = $" sprite={img.sprite?.name}";
            }

            sb.AppendLine($"{NodePath(g.transform)} [{g.GetIl2CppType().Name}] rect=({r.xMin:0},{r.yMin:0})-({r.xMax:0},{r.yMax:0}) color={g.color}{extra}");
        }

        File.WriteAllText(Path.Combine(dir, "menu_probe.txt"), sb.ToString());
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "menu_probe.png"));
        Core.Log.Msg("Menu probe written.");
    }

    private static Rect ScreenRect(RectTransform rt, Canvas canvas)
    {
        var corners = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
        rt.GetWorldCorners(corners);
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            minX = Math.Min(minX, p.x);
            minY = Math.Min(minY, p.y);
            maxX = Math.Max(maxX, p.x);
            maxY = Math.Max(maxY, p.y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static string NodePath(Transform t)
    {
        string p = t.name;
        for (Transform c = t.parent; c != null; c = c.parent)
        {
            p = c.name + "/" + p;
        }

        return p;
    }
}

[HarmonyPatch(typeof(MainMenuPanel), nameof(MainMenuPanel.Start))]
internal static class MainMenuPanelStartPatch
{
    private static void Postfix(MainMenuPanel __instance) => MainMenuBadge.Apply(__instance);
}

[HarmonyPatch(typeof(MainMenuPanel), nameof(MainMenuPanel.SetupContinueButton))]
internal static class MainMenuPanelSetupPatch
{
    private static void Postfix(MainMenuPanel __instance) => MainMenuBadge.Apply(__instance);
}
