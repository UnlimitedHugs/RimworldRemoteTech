using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	// A tab for a workbench that allows switching between using Steel and Components to make recipes.
	// It works by displaying a different set of recipes depending on the setting.
	public class ITab_ExplosivesBills : ITab_Bills {

		private enum RecipeMode {
			 Components, Steel
		}

		private float viewHeight = 1000f;
		private Vector2 scrollPosition = default(Vector2);
		private Bill mouseoverBill;
		private static readonly Vector2 WinSize = new Vector2(370f, 480f);
		private RecipeMode currentRecipeMode = RecipeMode.Components;

		private string modeButtonLabel;
		private string modeButtonComponents;
		private string modeButtonSteel;
		private string modeButtonTooltip;

		public ITab_ExplosivesBills()
		{
			size = WinSize;
			labelKey = "TabBills";
		}

		protected override void FillTab() {
			if(modeButtonLabel == null) {
				modeButtonLabel = "BillsTab_MaterialButton_label".Translate();
				modeButtonComponents = "BillsTab_MaterialButton_component_mode".Translate();
				modeButtonSteel = "BillsTab_MaterialButton_steel_mode".Translate();
				modeButtonTooltip = "BillsTab_MaterialButton_tooltip".Translate();
			}

			var canvasRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

			var buttonRect = new Rect(canvasRect.x + 160f, canvasRect.y, 170f, 29f);
			TooltipHandler.TipRegion(buttonRect, modeButtonTooltip);
			if (Widgets.ButtonText(buttonRect, String.Format(modeButtonLabel, currentRecipeMode == RecipeMode.Components ? modeButtonComponents : modeButtonSteel))) {
				currentRecipeMode = currentRecipeMode == RecipeMode.Components ? RecipeMode.Steel : RecipeMode.Components;
			}
			
			Func<List<FloatMenuOption>> recipeOptionsMaker = delegate {
				var list = new List<FloatMenuOption>();
				for (int i = 0; i < SelTable.def.AllRecipes.Count; i++) {
					var recipe = SelTable.def.AllRecipes[i];
					if(!recipe.AvailableNow) continue;
					if(currentRecipeMode == RecipeMode.Components && IsInjectedSteelRecipe(recipe) || currentRecipeMode == RecipeMode.Steel && !IsInjectedSteelRecipe(recipe)) continue;
					
					list.Add(new FloatMenuOption(recipe.LabelCap, delegate {
						if (!Find.MapPawns.FreeColonists.Any(recipe.PawnSatisfiesSkillRequirements)) {
							Bill.CreateNoPawnsWithSkillDialog(recipe);
						}
						var bill = recipe.MakeNewBill();
						SelTable.billStack.AddBill(bill);
					}));
				}
				return list;
			};
			mouseoverBill = SelTable.billStack.DrawListing(canvasRect, recipeOptionsMaker, ref scrollPosition, ref viewHeight);
		}

		public override void TabUpdate() {
			if (mouseoverBill == null) return;
			mouseoverBill.TryDrawIngredientSearchRadiusOnMap(SelTable.Position);
			mouseoverBill = null;
		}

		private bool IsInjectedSteelRecipe(RecipeDef recipe) {
			return recipe.defName.EndsWith(RemoteExplosivesUtility.InjectedRecipeNameSuffix);
		}
	}
}
