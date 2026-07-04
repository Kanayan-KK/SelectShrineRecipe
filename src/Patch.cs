using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace SelectShrineRecipe;

[HarmonyPatch]
internal class Patch
{
    /// <summary>
    /// 車輪の祠の使用処理をレシピ選択ダイアログへ差し替える。
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TraitPowerStatue), nameof(TraitPowerStatue.OnUse))]
    private static bool Prefix(TraitPowerStatue __instance, Chara c)
    {
        // Mod無効時はバニラ処理へ戻す。
        if (Plugin.EnableMod != null && !Plugin.EnableMod.Value)
            return true;

        // 車輪の祠以外はバニラ処理へ戻す。
        if (__instance is not TraitShrine shrine || shrine.Shrine.id != "invention")
            return true;

        // 未実装の祠はバニラ処理へ戻す。
        if (!shrine.IsImplemented())
            return true;

        var previousCandidates = RecipeCandidates.BuildWithoutChoiceCount();

        // 候補がなければバニラのランダム習得処理へ戻す。
        if (previousCandidates.Count == 0)
            return true;

        var candidates = RecipeCandidates.Build();
        var lastRecipe = GetLastRecipe(previousCandidates);
        var layer = EClass.ui.AddLayer<LayerList>();
        layer.SetSize(600);

        Action? showCategories = null;
        Action<string?>? showRecipes = null;

        // カテゴリダイアログを表示する。
        showCategories = () =>
        {
            layer.SetHeader("Select Category");
            var catItems = BuildCategoryItems(candidates, lastRecipe);
            layer.SetList2(catItems, i => i.Text ?? "", (i, _) =>
            {
                // 前回選択ボタンはレシピ選択として扱う。
                if (i.IsLastRecipe)
                {
                    SelectRecipe(i, layer, showCategories, shrine);
                    return;
                }

                showRecipes?.Invoke(i.Id);
            }, null, autoClose: false);
        };

        // カテゴリごとのレシピダイアログを表示する。
        showRecipes = (catId) =>
        {
            var recipes = GetRecipes(candidates, catId);
            layer.SetHeader(GetHeader(catId));
            var menuItems = new List<RecipeMenuItem> { new RecipeMenuItem { Text = "[ Back ]", IsBack = true } };
            menuItems.AddRange(recipes.Select(r => new RecipeMenuItem { Text = r.Name, Source = r }));

            layer.SetList2(menuItems, GetRecipeText, (i, _) => SelectRecipe(i, layer, showCategories, shrine), (_, item) =>
            {
                // 長いレシピ名は省略せず表示する。
                item.button1.mainText.horizontalOverflow = UnityEngine.HorizontalWrapMode.Overflow;
            }, autoClose: false);
        };

        showCategories();
        return false;
    }

    /// <summary>
    /// 設定が有効な場合だけ前回選択レシピを返す。
    /// </summary>
    private static RecipeSource? GetLastRecipe(List<RecipeSource> previousCandidates)
    {
        // Previousボタンが無効なら表示しない。
        if (Plugin.ShowPreviousRecipe == null || !Plugin.ShowPreviousRecipe.Value)
            return null;

        var candidateMap = previousCandidates.GroupBy(r => r.id).ToDictionary(g => g.Key, g => g.First());
        return LastRecipeSelection.GetValidRecipe(candidateMap);
    }

    /// <summary>
    /// カテゴリ一覧に表示する項目を作成する。
    /// </summary>
    private static List<RecipeMenuItem> BuildCategoryItems(List<RecipeSource> candidates, RecipeSource? lastRecipe)
    {
        var items = new List<RecipeMenuItem>();

        // 前回選択レシピが有効なら先頭に追加する。
        if (lastRecipe != null)
            items.Add(new RecipeMenuItem { Text = $"Previous: {lastRecipe.Name}", Source = lastRecipe, IsLastRecipe = true });

        items.Add(new RecipeMenuItem { Text = "All" });
        var rootCats = candidates.Select(r => r.row.Category.GetRoot()).Distinct().OrderBy(c => c.GetName());
        foreach (var c in rootCats)
            items.Add(new RecipeMenuItem { Text = c.GetName(), Id = c.id });

        return items;
    }

    /// <summary>
    /// 選択されたカテゴリに対応するレシピ候補を返す。
    /// </summary>
    private static List<RecipeSource> GetRecipes(List<RecipeSource> candidates, string? catId)
    {
        return candidates.Where(r => catId == null || r.row.Category.GetRoot().id == catId).ToList();
    }

    /// <summary>
    /// レシピ一覧ダイアログのヘッダー文字列を返す。
    /// </summary>
    private static string GetHeader(string? catId)
    {
        return catId == null ? "All" : EClass.sources.categories.map[catId].GetName();
    }

    /// <summary>
    /// レシピ一覧に表示する行テキストを作成する。
    /// </summary>
    private static string GetRecipeText(RecipeMenuItem item)
    {
        // 戻るボタンは固定文言を表示する。
        if (item.IsBack)
            return item.Text ?? "";

        // レシピがない項目は空表示にする。
        if (item.Source == null)
            return "";

        int recipeLv = EClass.player.recipes.knownRecipes.TryGetValue(item.Source.id, out int v) ? v : 0;
        return $"{item.Text} (Lv.{item.Source.row.LV}) Lv.{recipeLv}";
    }

    /// <summary>
    /// レシピ選択または戻る操作を処理する。
    /// </summary>
    private static void SelectRecipe(RecipeMenuItem item, LayerList layer, Action? showCategories, TraitShrine shrine)
    {
        // 戻るボタンはカテゴリ一覧へ戻す。
        if (item.IsBack)
        {
            showCategories?.Invoke();
            return;
        }

        // レシピがなければ何もしない。
        if (item.Source == null)
            return;

        // レシピ選択時だけ車輪の祠を使用済みにする。
        UseShrine(shrine);

        // 未習得時だけ発想メッセージを出す。
        if (!EClass.player.recipes.knownRecipes.ContainsKey(item.Source.id))
            Msg.Say("learnRecipeIdea");

        EClass.player.recipes.Add(item.Source.id);
        LastRecipeSelection.Record(item.Source.id);
        layer.Close();
    }

    /// <summary>
    /// 車輪の祠の使用演出と消費処理を実行する。
    /// </summary>
    private static void UseShrine(TraitShrine shrine)
    {
        Msg.Say("shrine_power", shrine.owner);
        SE.Play("shrine");
        shrine.owner.PlayEffect("buff");
        shrine.owner.isOn = false;
        shrine.owner.rarity = Rarity.Normal;
        shrine.owner.renderer.RefreshExtra();
    }
}
