using System;
using System.Collections.Generic;
using System.Linq;

namespace SelectShrineRecipe;

internal static class RecipeCandidates
{
    /// <summary>
    /// 現在の設定とキャラクター能力で選択可能なレシピ候補を作成する。
    /// </summary>
    internal static List<RecipeSource> Build()
    {
        var candidates = BuildBase();
        ApplyChoiceCount(candidates);
        candidates.Sort(Compare);
        return candidates;
    }

    /// <summary>
    /// ChoiceCount適用前のレシピ候補を作成する。
    /// </summary>
    internal static List<RecipeSource> BuildWithoutChoiceCount()
    {
        var candidates = BuildBase();
        candidates.Sort(Compare);
        return candidates;
    }

    /// <summary>
    /// 表示条件を満たす基礎候補を作成する。
    /// </summary>
    private static List<RecipeSource> BuildBase()
    {
        // レシピ一覧が未構築なら初期化する。
        if (RecipeManager.list.Count == 0)
            RecipeManager.BuildList();

        const int lvBonus = 10;
        var player = EClass.player;
        var pc = EClass.pc;
        var showHidden = Plugin.ShowHiddenRecipe?.Value ?? false;
        var unlearnedOnly = Plugin.UnlearnedRecipeOnly?.Value ?? false;

        // 現在の設定で表示できるレシピ候補を作る。
        return RecipeManager.list.Where(r =>
            !r.alwaysKnown &&
            (r.NeedFactory || r.IsQuickCraft) &&
            pc.Evalue(r.GetReqSkill().id) + 5 + lvBonus >= r.row.LV &&
            (showHidden || !r.row.ContainsTag("hiddenRecipe")) &&
            (!unlearnedOnly || !player.recipes.knownRecipes.ContainsKey(r.id))
        ).ToList();
    }

    /// <summary>
    /// ChoiceCountが有効なら候補をランダムに絞る。
    /// </summary>
    private static void ApplyChoiceCount(List<RecipeSource> candidates)
    {
        // 表示数制限がなければ全候補を使う。
        if (Plugin.ChoiceCount == null || Plugin.ChoiceCount.Value <= 0 || candidates.Count <= Plugin.ChoiceCount.Value)
            return;

        var rng = new Random();
        var selected = candidates.OrderBy(_ => rng.Next()).Take(Plugin.ChoiceCount.Value).ToList();
        candidates.Clear();
        candidates.AddRange(selected);
    }

    /// <summary>
    /// レシピ候補をルートカテゴリ、レベルの順で比較する。
    /// </summary>
    private static int Compare(RecipeSource a, RecipeSource b)
    {
        int c = string.Compare(a.row.Category.GetRoot().id, b.row.Category.GetRoot().id, StringComparison.Ordinal);

        // ルートカテゴリが違う場合はカテゴリ順で並べる。
        if (c != 0)
            return c;

        return a.row.LV.CompareTo(b.row.LV);
    }
}
