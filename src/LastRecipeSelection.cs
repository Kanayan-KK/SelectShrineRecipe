using System.Collections.Generic;

namespace SelectShrineRecipe;

internal static class LastRecipeSelection
{
    /// <summary>
    /// 前回選択レシピが現在の候補に含まれる場合だけ返す。
    /// </summary>
    internal static RecipeSource? GetValidRecipe(IReadOnlyDictionary<string, RecipeSource> candidates)
    {
        var id = Plugin.LastRecipeId?.Value ?? "";

        // 前回選択が未保存ならボタンを出さない。
        if (id.IsEmpty())
            return null;

        return candidates.TryGetValue(id, out var recipe) ? recipe : null;
    }

    /// <summary>
    /// 前回選択レシピIDを非表示設定へ保存する。
    /// </summary>
    internal static void Record(string id)
    {
        // 空IDは保存しない。
        if (id.IsEmpty())
            return;

        // 非表示設定に前回選択IDを保存する。
        if (Plugin.LastRecipeId != null)
            Plugin.LastRecipeId.Value = id;
    }
}
