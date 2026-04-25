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
    public const string Version = "1.0.1";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class Plugin : BaseUnityPlugin
{
    internal static Plugin? Instance;
    internal static ConfigEntry<bool>? EnableMod;
    internal static ConfigEntry<bool>? ShowHiddenRecipe;
    internal static ConfigEntry<bool>? UnlearnedRecipeOnly;
    internal static ConfigEntry<int>? ChoiceCount;

    private void Awake()
    {
        Instance = this;
        EnableMod = Config.Bind("General", "EnableMod", true, "Enable this mod");
        ShowHiddenRecipe = Config.Bind("General", "ShowHiddenRecipe", false, "Show hidden recipes");
        UnlearnedRecipeOnly = Config.Bind("General", "UnlearnedRecipeOnly", false, "Only show unlearned recipes");
        ChoiceCount = Config.Bind("General", "ChoiceCount", 0, "Number of choices to display. 0 = All.");
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModInfo.Guid);
    }

    internal static void LogDebug(object message, [CallerMemberName] string caller = "")
    {
        Instance?.Logger.LogDebug($"[{caller}] {message}");
    }

    internal static void LogInfo(object message)
    {
        Instance?.Logger.LogInfo(message);
    }

    internal static void LogError(object message)
    {
        Instance?.Logger.LogError(message);
    }
}