using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace SelectShrineRecipe
{
    [HarmonyPatch]
    internal class Patch
    {
        internal class MenuItem
        {
            public string? Text;
            public string? Id;
            public RecipeSource? Source;
            public bool IsCategory;
            public bool IsBack;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TraitShrine), nameof(TraitShrine._OnUse))]
        private static bool Prefix(TraitShrine __instance)
        {
            // 車輪の祠以外の場合は既存処理を実行
            if (__instance.Shrine.id != "invention")
                return true;

            // レシピリストが未構築の場合は構築
            if (RecipeManager.list.Count == 0)
                RecipeManager.BuildList();

            const int lvBonus = 10;
            var player = EClass.player;
            var pc = EClass.pc;

            // レシピを取得
            var showHidden = Plugin.Instance?.ShowHiddenRecipe?.Value ?? false;
            var candidates = RecipeManager.list.Where(r =>
                    !r.alwaysKnown && // 最初から覚えているスキルは除外
                    (r.NeedFactory || r.IsQuickCraft) && // クラフト可能なレシピ
                    pc.Evalue(r.GetReqSkill().id) + 5 + lvBonus >= r.row.LV &&
                    (showHidden || !r.row.ContainsTag("hiddenRecipe")) // 現物から習得のみの隠しレシピ
            ).ToList();

            // レシピが見つからない場合は既存処理を実行
            if (candidates.Count == 0)
                return true;

            // ソート: ルートカテゴリ -> レベル
            candidates.Sort((a, b) =>
            {
                int c = String.Compare(a.row.Category.GetRoot().id, b.row.Category.GetRoot().id,
                    StringComparison.Ordinal);
                if (c != 0) return c;
                return a.row.LV.CompareTo(b.row.LV);
            });

            // UIレイヤーの作成
            var layer = EClass.ui.AddLayer<LayerList>();
            layer.SetSize(600);

            // カテゴリの収集＆ソート
            var rootCats = candidates.Select(r => r.row.Category.GetRoot()).Distinct().OrderBy(c => c.GetName());

            // カテゴリ一覧表示用リスト作成
            var catItems = new List<MenuItem>();

            // Allカテゴリ追加
            catItems.Add(new MenuItem { Text = "All", Id = null, IsCategory = true });

            // カテゴリ追加
            foreach (var c in rootCats)
                catItems.Add(new MenuItem { Text = c.GetName(), Id = c.id, IsCategory = true });

            // 前方宣言
            Action<string?>? showRecipes = null;

            // カテゴリ一覧表示処理
            var showCats = () =>
            {
                layer.SetHeader("Select Category");
                layer.SetList2(catItems, (i) => i.Text ?? "", (i, _) =>
                {
                    // カテゴリ選択 -> レシピ表示
                    showRecipes?.Invoke(i.Id);
                }, null, autoClose: false);
            };

            // レシピ一覧表示処理
            showRecipes = (catId) =>
            {
                string header = catId == null ? "All" : EClass.sources.categories.map[catId].GetName();
                layer.SetHeader(header);

                var menuItems = new List<MenuItem>();

                // Backボタン追加
                menuItems.Add(new MenuItem { Text = "[ Back ]", IsBack = true });

                var filtered = candidates.Where(r => catId == null || r.row.Category.GetRoot().id == catId);

                foreach (var r in filtered)
                    menuItems.Add(new MenuItem { Text = r.Name, Source = r });

                layer.SetList2(menuItems,
                    (i) =>
                    {
                        // レシピ名表示処理
                        if (i.IsBack)
                            return i.Text ?? "";

                        if (i.Source == null)
                            return "";

                        // 習得レベル取得
                        int recipeLv = player.recipes.knownRecipes.TryGetValue(i.Source.id, out int v) ? v : 0;
                        return $"{i.Text} (Lv.{i.Source.row.LV}) Lv.{recipeLv}";
                    },
                    (i, _) =>
                    {
                        // レシピ名クリック時処理

                        // Back クリック時
                        if (i.IsBack)
                        {
                            showCats();
                            return;
                        }

                        if (i.Source == null)
                            return;

                        // レシピ習得時メッセージ表示
                        if (!player.recipes.knownRecipes.ContainsKey(i.Source.id))
                            Msg.Say("learnRecipeIdea");

                        player.recipes.Add(i.Source.id);

                        layer.Close();
                    },
                    (_, item) =>
                    {
                        // テキストの自動改行を無効化
                        item.button1.mainText.horizontalOverflow = UnityEngine.HorizontalWrapMode.Overflow;
                    }, autoClose: false);
            };

            // 初期表示: カテゴリ一覧
            showCats();

            // 既存処理をスキップ
            return false;
        }
    }
}