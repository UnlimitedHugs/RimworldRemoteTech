using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using System.Linq;

namespace RemoteExplosives {
	/**
	 * Technically, not a component of the map (stores no data), but it provides a covenient entry point at map load.
	 */
	public class MapComponent_RemoteExplosivesInjector : MapComponent {
		private readonly ThingCategoryDef explosivesItemCategory = ThingCategoryDef.Named("Explosives");
		private readonly RecipeDef BasicRemoteBombRecipe = DefDatabase<RecipeDef>.GetNamed("MakeRemoteBomb");
		private readonly MethodInfo cloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

		private const int ComponentValueInSteel = 40;

		public MapComponent_RemoteExplosivesInjector() {
			InjectTraderStocks();
			InjectSteelRecipeVariants();
		}

		/**
		 * Injects StockGenerators into existing traders.
		 */
		private void InjectTraderStocks() {
			var allInjectors = DefDatabase<TraderStockInjectorDef>.AllDefs;
			var affectedTraders = new List<TraderKindDef>();
			foreach (var injectorDef in allInjectors) {
				if (injectorDef.traderDef == null || injectorDef.stockGenerators.Count == 0) continue;
				affectedTraders.Add(injectorDef.traderDef);
				foreach (var stockGenerator in injectorDef.stockGenerators) {
					injectorDef.traderDef.stockGenerators.Add(stockGenerator);
				}
			}
			if(affectedTraders.Count>0) {
				Log.Message(string.Format("[RemoteExplosives] Injected stock generators for {0} traders", affectedTraders.Count));
			}

			// Unless all defs are reloaded, we no longer need the injector defs
			DefDatabase<TraderStockInjectorDef>.Clear();
		}

		/**
		 * Injects copies of explosives recipes, changing components into an equivalent amount of steel
		 */
		private void InjectSteelRecipeVariants() {
			if(SteelRecipesInjected()) return;

			int injectCount = 0;
			foreach (var explosiveRecipe in GetAllExplosivesRecipes().ToList()) {
				var variant = TryMakeRecipeVariantWithSteel(explosiveRecipe);
				if (variant != null) {
					DefDatabase<RecipeDef>.Add(variant);
					injectCount++;
				}
			}

			if(injectCount>0) {
				Log.Message(string.Format("[RemoteExplosives] Injected {0} alternate explosives recipes.", injectCount));
			}
		}

		private bool SteelRecipesInjected() {
			return DefDatabase<RecipeDef>.GetNamedSilentFail(BasicRemoteBombRecipe.defName + RemoteExplosivesUtility.InjectedRecipeNameSuffix) != null;
		}

		private IEnumerable<RecipeDef> GetAllExplosivesRecipes() {
			return DefDatabase<RecipeDef>.AllDefs.Where(d => {
				var product = d.products.FirstOrDefault();
				return product != null && product.thingDef!=null && product.thingDef.thingCategories!=null && product.thingDef.thingCategories.Contains(explosivesItemCategory);
			});
		} 
		
		// Will retrn null if recipe reqires no components
		private RecipeDef TryMakeRecipeVariantWithSteel(RecipeDef recipeOriginal) {
			var recipeCopy = (RecipeDef)cloneMethod.Invoke(recipeOriginal, null);
			recipeCopy.defName += RemoteExplosivesUtility.InjectedRecipeNameSuffix;

			var newFixedFilter = new ThingFilter();
			foreach (var allowedThingDef in recipeOriginal.fixedIngredientFilter.AllowedThingDefs) {
				if (allowedThingDef == ThingDefOf.Components) continue;
				newFixedFilter.SetAllow(allowedThingDef, true);
			}
			newFixedFilter.SetAllow(ThingDefOf.Steel, true);
			recipeCopy.fixedIngredientFilter = newFixedFilter;
			recipeCopy.defaultIngredientFilter = null;

			float numComponentsRequired = 0;
			var newIngredientList = new List<IngredientCount>(recipeOriginal.ingredients);
			foreach (var ingredientCount in newIngredientList) {
				if (ingredientCount.filter.Allows(ThingDefOf.Components)) {
					numComponentsRequired = ingredientCount.GetBaseCount();
					newIngredientList.Remove(ingredientCount);
					break;
				}
			}
			if (numComponentsRequired == 0) return null;

			var steelFilter = new ThingFilter();
			steelFilter.SetAllow(ThingDefOf.Steel, true);
			var steelIngredient = new IngredientCount {filter = steelFilter};
			steelIngredient.SetBaseCount(ComponentValueInSteel * numComponentsRequired);
			newIngredientList.Add(steelIngredient);
			recipeCopy.ingredients = newIngredientList;
			recipeCopy.ResolveReferences();
			return recipeCopy;
		} 
	}
}