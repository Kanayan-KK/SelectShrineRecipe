using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace SelectShrineRecipe;

[HarmonyPatch]
internal class Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TraitShrine), nameof(TraitShrine._OnUse))]
    private static bool Prefix(TraitShrine __instance)
    {
        // Mod無効時はバニラ処理へ戻す。
        if (Plugin.EnableMod != null && !Plugin.EnableMod.Value)
            return true;

        // 車輪の祠以外はバニラ処理へ戻す。
        if (__instance.Shrine.id != "invention")
            return true;

        var candidates = RecipeCandidates.Build();
        if (candidates.Count == 0)
            return true;

        // UIで使う候補と前回選択レシピを準備する。
        var candidateMap = candidates.GroupBy(r => r.id).ToDictionary(g => g.Key, g => g.First());
        var lastRecipe = LastRecipeSelection.GetValidRecipe(candidateMap);
        var layer = EClass.ui.AddLayer<LayerList>();
        layer.SetSize(600);

        Action? showCategories = null;
        Action<string?>? showRecipes = null;

        // カテゴリダイアログ表示処理
        showCategories = () =>
        {
            layer.SetHeader("Select Category");
            var catItems = BuildCategoryItems(candidates, lastRecipe);
            layer.SetList2(catItems, i => i.Text ?? "", (i, _) =>
            {
                if (i.IsLastRecipe)
                {
                    SelectRecipe(i, layer, showCategories);
                    return;
                }

                showRecipes?.Invoke(i.Id);
            }, null, autoClose: false);
        };

        // カテゴリごとのダイアログ表示処理
        showRecipes = (catId) =>
        {
            var recipes = GetRecipes(candidates, catId);
            layer.SetHeader(GetHeader(catId));
            var menuItems = new List<RecipeMenuItem> { new RecipeMenuItem { Text = "[ Back ]", IsBack = true } };
            menuItems.AddRange(recipes.Select(r => new RecipeMenuItem { Text = r.Name, Source = r }));

            layer.SetList2(menuItems, GetRecipeText, (i, _) => SelectRecipe(i, layer, showCategories), (_, item) =>
            {
                // 長いレシピ名は省略せず表示する。
                item.button1.mainText.horizontalOverflow = UnityEngine.HorizontalWrapMode.Overflow;
            }, autoClose: false);
        };

        showCategories();
        return false;
    }

    private static List<RecipeMenuItem> BuildCategoryItems(List<RecipeSource> candidates, RecipeSource? lastRecipe)
    {
        var items = new List<RecipeMenuItem>();
        if (lastRecipe != null)
            items.Add(new RecipeMenuItem { Text = $"Previous: {lastRecipe.Name}", Source = lastRecipe, IsLastRecipe = true });

        items.Add(new RecipeMenuItem { Text = "All" });
        var rootCats = candidates.Select(r => r.row.Category.GetRoot()).Distinct().OrderBy(c => c.GetName());
        foreach (var c in rootCats)
            items.Add(new RecipeMenuItem { Text = c.GetName(), Id = c.id });

        return items;
    }

    private static List<RecipeSource> GetRecipes(List<RecipeSource> candidates, string? catId)
    {
        return candidates.Where(r => catId == null || r.row.Category.GetRoot().id == catId).ToList();
    }

    private static string GetHeader(string? catId)
    {
        return catId == null ? "All" : EClass.sources.categories.map[catId].GetName();
    }

    private static string GetRecipeText(RecipeMenuItem item)
    {
        if (item.IsBack)
            return item.Text ?? "";

        if (item.Source == null)
            return "";

        int recipeLv = EClass.player.recipes.knownRecipes.TryGetValue(item.Source.id, out int v) ? v : 0;
        return $"{item.Text} (Lv.{item.Source.row.LV}) Lv.{recipeLv}";
    }

    private static void SelectRecipe(RecipeMenuItem item, LayerList layer, Action? showCategories)
    {
        if (item.IsBack)
        {
            showCategories?.Invoke();
            return;
        }

        if (item.Source == null)
            return;

        // 未習得時だけ発想メッセージを出す。
        if (!EClass.player.recipes.knownRecipes.ContainsKey(item.Source.id))
            Msg.Say("learnRecipeIdea");

        EClass.player.recipes.Add(item.Source.id);
        LastRecipeSelection.Record(item.Source.id);
        layer.Close();
    }
}
