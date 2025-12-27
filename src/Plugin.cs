using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;

namespace SelectShrineRecipe;

public static class ModInfo
{
    public const string Guid = "SelectShrineRecipe";
    public const string Name = "Select Shrine Recipe";
    public const string Version = "1.0.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class Plugin : BaseUnityPlugin
{
    internal static Plugin? Instance;
    internal BepInEx.Configuration.ConfigEntry<bool>? ShowHiddenRecipe;

    private void Awake()
    {
        Instance = this;
        ShowHiddenRecipe = Config.Bind("General", "ShowHiddenRecipe", false, "Show hidden recipes");
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