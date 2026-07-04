using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace SelectShrineRecipe;

public static class ModInfo
{
    public const string Guid = "SelectShrineRecipe";
    public const string Name = "Select Shrine Recipe";
    public const string Version = "1.0.3";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class Plugin : BaseUnityPlugin
{
    internal static Plugin? Instance;
    internal static ConfigEntry<bool>? EnableMod;
    internal static ConfigEntry<bool>? ShowHiddenRecipe;
    internal static ConfigEntry<bool>? UnlearnedRecipeOnly;
    internal static ConfigEntry<int>? ChoiceCount;
    internal static ConfigEntry<bool>? ShowPreviousRecipe;
    internal static ConfigEntry<string>? LastRecipeId;

    /// <summary>
    /// 設定項目を登録し、Harmonyパッチを適用する。
    /// </summary>
    private void Awake()
    {
        Instance = this;
        EnableMod = Config.Bind("General", "EnableMod", true, "Enable this mod");
        ShowHiddenRecipe = Config.Bind("General", "ShowHiddenRecipe", false, "Show hidden recipes");
        UnlearnedRecipeOnly = Config.Bind("General", "UnlearnedRecipeOnly", false, "Only show unlearned recipes");
        ChoiceCount = Config.Bind("General", "ChoiceCount", 0, "Number of choices to display. 0 = All.");
        ShowPreviousRecipe = Config.Bind("General", "ShowPreviousRecipe", false, "Show previous recipe button");
        LastRecipeId = Config.Bind("General", "LastRecipeId", "", "Recipe id saved automatically for the previous recipe button. Manual edits require a valid recipe id; invalid or unavailable ids will not show the button.");
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModInfo.Guid);
    }

    /// <summary>
    /// 呼び出し元名付きでデバッグログを出力する。
    /// </summary>
    internal static void LogDebug(object message, [CallerMemberName] string caller = "")
    {
        Instance?.Logger.LogDebug($"[{caller}] {message}");
    }

    /// <summary>
    /// 情報ログを出力する。
    /// </summary>
    internal static void LogInfo(object message)
    {
        Instance?.Logger.LogInfo(message);
    }

    /// <summary>
    /// エラーログを出力する。
    /// </summary>
    internal static void LogError(object message)
    {
        Instance?.Logger.LogError(message);
    }
}
