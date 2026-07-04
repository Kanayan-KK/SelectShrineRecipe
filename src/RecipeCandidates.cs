using System;
using System.Collections.Generic;
using System.Linq;

namespace SelectShrineRecipe;

internal static class RecipeCandidates
{
    internal static List<RecipeSource> Build()
    {
        if (RecipeManager.list.Count == 0)
            RecipeManager.BuildList();

        const int lvBonus = 10;
        var player = EClass.player;
        var pc = EClass.pc;
        var showHidden = Plugin.ShowHiddenRecipe?.Value ?? false;
        var unlearnedOnly = Plugin.UnlearnedRecipeOnly?.Value ?? false;

        // 現在の設定で表示できるレシピ候補を作る。
        var candidates = RecipeManager.list.Where(r =>
            !r.alwaysKnown &&
            (r.NeedFactory || r.IsQuickCraft) &&
            pc.Evalue(r.GetReqSkill().id) + 5 + lvBonus >= r.row.LV &&
            (showHidden || !r.row.ContainsTag("hiddenRecipe")) &&
            (!unlearnedOnly || !player.recipes.knownRecipes.ContainsKey(r.id))
        ).ToList();

        // 表示数制限があれば、履歴表示も同じ候補集合だけを使う。
        if (Plugin.ChoiceCount != null && Plugin.ChoiceCount.Value > 0 && candidates.Count > Plugin.ChoiceCount.Value)
        {
            var rng = new Random();
            candidates = candidates.OrderBy(_ => rng.Next()).Take(Plugin.ChoiceCount.Value).ToList();
        }

        candidates.Sort(Compare);
        return candidates;
    }

    private static int Compare(RecipeSource a, RecipeSource b)
    {
        int c = string.Compare(a.row.Category.GetRoot().id, b.row.Category.GetRoot().id, StringComparison.Ordinal);
        if (c != 0)
            return c;

        return a.row.LV.CompareTo(b.row.LV);
    }
}
