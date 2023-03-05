using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteTech;

/// <summary>
///     A tab for a workbench that allows switching between using Steel and Components to make recipes.
///     It works by displaying a different set of recipes depending on the setting.
/// </summary>
public class ITab_ExplosivesBills : ITab_Bills
{
    private static readonly Vector2 WinSize = new Vector2(370f, 480f);
    private RecipeVariantType currentVariant;
    private Bill mouseoverBill;
    private Vector2 scrollPosition = default;

    private float viewHeight = 1000f;

    public ITab_ExplosivesBills()
    {
        size = WinSize;
        labelKey = "TabBills";
    }

    public override void FillTab()
    {
        Text.Font = GameFont.Small;
        const float Padding = 10f, Spacing = 6f, SettingsRowHeight = 29f;
        var canvasRect = new Rect(0f, 18f, WinSize.x, WinSize.y).ContractedBy(Padding);

        var settingsRect = new Rect(canvasRect.x, canvasRect.y, canvasRect.width,
            (Padding * 2f) + (SettingsRowHeight * 3f) + (Spacing * 2f));
        Widgets.DrawMenuSection(settingsRect);
        var settingsContent = settingsRect.ContractedBy(Padding);

        Text.Anchor = TextAnchor.MiddleLeft;
        var rowRect = new Rect(settingsContent.x, settingsContent.y, settingsContent.width, SettingsRowHeight);
        var sparkpowderVariant = (currentVariant & RecipeVariantType.Sparkpowder) != 0;
        Widgets.Label(rowRect, "BillsTab_MainMaterial_label".Translate());
        if (Widgets.ButtonText(rowRect.RightHalf(),
                sparkpowderVariant
                    ? "BillsTab_MaterialButton_sparkpowder_mode".Translate()
                    : "BillsTab_MaterialButton_silver_mode".Translate()))
        {
            currentVariant ^= RecipeVariantType.Sparkpowder;
        }

        rowRect.y += SettingsRowHeight + Spacing;
        var steelVariant = (currentVariant & RecipeVariantType.Steel) != 0;
        Widgets.Label(rowRect, "BillsTab_SecondaryMaterial_label".Translate());
        if (Widgets.ButtonText(rowRect.RightHalf(),
                steelVariant
                    ? "BillsTab_MaterialButton_steel_mode".Translate()
                    : "BillsTab_MaterialButton_component_mode".Translate()))
        {
            currentVariant ^= RecipeVariantType.Steel;
        }

        rowRect.y += SettingsRowHeight + Spacing;
        Widgets.Label(rowRect, "BillsTab_WorkAmount_label".Translate());
        var workPercent = 100f * (sparkpowderVariant ? RemoteTechController.SilverReplacementWorkMultiplier : 1f) *
                          (steelVariant ? RemoteTechController.ComponentReplacementWorkMultiplier : 1f);
        Widgets.Label(rowRect.RightHalf(), $"{workPercent:F0}%");
        Text.Anchor = TextAnchor.UpperLeft;

        const float InfoBtnSize = 24f;
        var infoRect = new Rect(rowRect.xMax - InfoBtnSize, rowRect.y + ((rowRect.height - InfoBtnSize) / 2f),
            InfoBtnSize, InfoBtnSize);
        TooltipHandler.TipRegion(infoRect, "BillsTab_Materials_info".Translate());
        GUI.DrawTexture(infoRect, Resources.Textures.InfoButtonIcon);

        var recipesRect = new Rect(canvasRect.x, canvasRect.y + settingsRect.height + Spacing, canvasRect.width,
            canvasRect.height - (settingsRect.height + Spacing));

        List<FloatMenuOption> RecipeOptionsMaker()
        {
            var list = new List<FloatMenuOption>();
            for (var i = 0; i < SelTable.def.AllRecipes.Count; i++)
            {
                var recipe = SelTable.def.AllRecipes[i];
                var variant = recipe.GetModExtension<MakeRecipeVariants>();
                if (variant != null && variant.Variant != currentVariant)
                {
                    continue;
                }

                var locked = recipe.researchPrerequisite != null && !recipe.AvailableNow && !DebugSettings.godMode;
                var researchTip = "";
                if (locked)
                {
                    researchTip = "BillsTab_ResearchRequired".Translate(recipe.researchPrerequisite.label);
                }

                var option = new FloatMenuOptionWithTooltip(recipe.LabelCap, delegate
                    {
                        var map = Find.CurrentMap;
                        if (map == null || !map.mapPawns.FreeColonists.Any(recipe.PawnSatisfiesSkillRequirements))
                        {
                            Bill.CreateNoPawnsWithSkillDialog(recipe);
                        }

                        var bill = recipe.MakeNewBill();
                        SelTable.billStack.AddBill(bill);
                    }, researchTip, MenuOptionPriority.Default, null, null, 29f,
                    rect => Widgets.InfoCardButton(rect.x + 5f, rect.y + ((rect.height - 24f) / 2f), recipe));
                option.Disabled = locked;
                list.Add(option);
            }

            if (list.Count == 0)
            {
                list.Add(new FloatMenuOption("(no recipes)", null));
            }

            return list;
        }

        mouseoverBill =
            SelTable.billStack.DoListing(recipesRect, RecipeOptionsMaker, ref scrollPosition, ref viewHeight);
    }

    public override void TabUpdate()
    {
        if (mouseoverBill == null)
        {
            return;
        }

        mouseoverBill.TryDrawIngredientSearchRadiusOnMap(SelTable.Position);
        mouseoverBill = null;
    }
}