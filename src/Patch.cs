using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace SelectShrineRecipe
{
    [HarmonyPatch]
    internal class Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TraitShrine), nameof(TraitShrine._OnUse))]
        private static bool Prefix(TraitShrine __instance)
        {
            if (__instance.Shrine.id != "invention")
                // 車輪の祠以外の場合は既存処理を実行
                return true;

            // レシピリストが未構築の場合は構築
            if (RecipeManager.list.Count == 0)
            {
                RecipeManager.BuildList();
            }

            int lvBonus = 10;
            var player = EClass.player;
            var pc = EClass.pc;

            // 習得可能なレシピを抽出
            // 条件:
            // 1. 最初から覚えているものではない
            // 2. 工場が必要 または 即席作成可能
            // 3. まだ覚えていない
            // 4. 必要スキルレベルを満たしている (+15のボーナス込み)
            // 5. 隠しレシピではない
            var candidates = RecipeManager.list.Where(r =>
                !r.alwaysKnown &&
                (r.NeedFactory || r.IsQuickCraft) &&
                pc.Evalue(r.GetReqSkill().id) + 5 + lvBonus >= r.row.LV &&
                !r.row.ContainsTag("hiddenRecipe")
            ).ToList();

            if (candidates.Count == 0)
            {
                Msg.Say("nothingHappens");
                return false;
            }

            // ソート: カテゴリ -> レベル -> 名前
            candidates.Sort((a, b) =>
            {
                int c = String.Compare(a.row.Category.id, b.row.Category.id, StringComparison.Ordinal);
                if (c != 0) return c;
                int l = a.row.LV.CompareTo(b.row.LV);
                if (l != 0) return l;
                return String.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });

            // UIを表示して選択させる
            EClass.ui.AddLayer<LayerList>()
                .SetSize(600)
                .SetHeader("chooseRecipe") // "レシピを選択" (lang key or raw string)
                .SetList2(
                    candidates,
                    (r) =>
                    {
                        bool known = player.recipes.knownRecipes.ContainsKey(r.id);
                        return $"{r.Name} (Lv.{r.row.LV}) {(known ? "match_learned".lang() : "")}";
                    },
                    (r, _) =>
                    {
                        // 選択時の処理: レシピを習得 (Addは既知の場合もカウントを増やす)
                        if (!player.recipes.knownRecipes.ContainsKey(r.id))
                            Msg.Say("learnRecipeIdea");
                        player.recipes.Add(r.id);
                    },
                    (_, item) =>
                    {
                        // ツールチップ設定などをここで行うことも可能
                    }
                );

            // 既存処理をスキップ
            return false;
        }
    }
}