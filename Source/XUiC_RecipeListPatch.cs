using HarmonyLib;

[HarmonyPatch]
public class XUiC_RecipeListPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_RecipeList), "RefreshCurrentRecipes")]
    public static void RefreshCurrentRecipesPostfix(XUiC_RecipeList __instance)
    {
        // 收藏后强制重新排序
        if (__instance != null)
        {
            __instance.resortRecipes = true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(XUiC_RecipeList), "CompareRecipeInfos")]
    public static bool CompareRecipeInfosPrefix(XUiC_RecipeList __instance, XUiC_RecipeList.RecipeInfo lhs, XUiC_RecipeList.RecipeInfo rhs, ref int __result)
    {
        if (__instance == null || lhs.Equals(default(XUiC_RecipeList.RecipeInfo)) || rhs.Equals(default(XUiC_RecipeList.RecipeInfo)) || lhs.recipe == null || rhs.recipe == null)
        {
            return true;
        }

        // 1. 原有 比较追踪状态
        if (lhs.recipe.IsTracked != rhs.recipe.IsTracked)
        {
            __result = !lhs.recipe.IsTracked ? 1 : -1;
            return false;
        }

        // 2. 原有 比较挑战状态
        if (lhs.recipe.isChallenge != rhs.recipe.isChallenge)
        {
            __result = !lhs.recipe.isChallenge ? 1 : -1;
            return false;
        }

        // 3. 原有 比较任务状态
        if (lhs.recipe.isQuest != rhs.recipe.isQuest)
        {
            __result = !lhs.recipe.isQuest ? 1 : -1;
            return false;
        }

        // 4. 新增 添加收藏IsFavorite的比较逻辑
        bool isLhsFavorite = XUiM_Recipes.GetRecipeIsFavorite(__instance.xui, lhs.recipe);
        bool isRhsFavorite = XUiM_Recipes.GetRecipeIsFavorite(__instance.xui, rhs.recipe);

        if (isLhsFavorite != isRhsFavorite)
        {
            __result = isLhsFavorite ? -1 : 1; // 收藏排在前面
            return false;
        }

        return true;
    }
}