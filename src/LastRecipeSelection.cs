using System.Collections.Generic;

namespace SelectShrineRecipe;

internal static class LastRecipeSelection
{
    internal static RecipeSource? GetValidRecipe(IReadOnlyDictionary<string, RecipeSource> candidates)
    {
        var id = Plugin.LastRecipeId?.Value ?? "";
        if (id.IsEmpty())
            return null;

        return candidates.TryGetValue(id, out var recipe) ? recipe : null;
    }

    internal static void Record(string id)
    {
        if (id.IsEmpty())
            return;

        if (Plugin.LastRecipeId != null)
            Plugin.LastRecipeId.Value = id;
    }
}
