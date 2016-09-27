using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;

namespace RemoteExplosives {
	/*
	 * This is a catch-all for all tasks that are not attached to any building or item, but require ticking or execution at map load.
	 * Injects trader stock generators, generates recipe copies for the workbench and forwards ticks and updates to other components.
	 */
	public class MapComponent_RemoteExplosivesInjector : MapComponent {
		private readonly ThingCategoryDef explosivesItemCategory = ThingCategoryDef.Named("Explosives");
		private readonly MethodInfo objectCloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
		private readonly bool showDebugControls = false;

		private AutoReplaceWatcher replaceWatcher;
		
		private const int ComponentValueInSteel = 40;

		private bool MustPerformInjection {
			get { return DefDatabase<TraderStockInjectorDef>.DefCount > 0; }
		}

		public MapComponent_RemoteExplosivesInjector() {
			if (MustPerformInjection) { // perform only once per def reload
				InjectTraderStocks();
				InjectSteelRecipeVariants();
				InjectIEDComps();
			}
			EnsureComponentIsActive();
			ValueInterpolator.Instance.Initialize(Time.realtimeSinceStartup);
			CallbackScheduler.Instance.Initialize(Find.TickManager.TicksGame);
			DistributedTickScheduler.Instance.Initialize(Find.TickManager.TicksGame);
			replaceWatcher = new AutoReplaceWatcher();
		}

		public override void ExposeData() {
			Scribe_Deep.LookDeep(ref replaceWatcher, "replaceWatcher");
			if(replaceWatcher == null) replaceWatcher = new AutoReplaceWatcher();
		}

		public override void MapComponentUpdate() {
			base.MapComponentUpdate();
			ValueInterpolator.Instance.Update(Time.realtimeSinceStartup);
		}

		public override void MapComponentOnGUI() {
			base.MapComponentOnGUI();
			if (showDebugControls) DrawDebugControls();
		}

		public override void MapComponentTick() {
			base.MapComponentTick();
			var currentTick = Find.TickManager.TicksGame;
			CallbackScheduler.Instance.Tick(currentTick);
			DistributedTickScheduler.Instance.Tick(currentTick);
			replaceWatcher.Tick();
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
				RemoteExplosivesUtility.Log(string.Format("Injected stock generators for {0} traders", affectedTraders.Count));
			}

			// Unless all defs are reloaded, we no longer need the injector defs
			DefDatabase<TraderStockInjectorDef>.Clear();
		}

		/**
		 * Injects copies of explosives recipes, changing components into an equivalent amount of steel
		 */
		private void InjectSteelRecipeVariants() {
			int injectCount = 0;
			foreach (var explosiveRecipe in GetAllExplosivesRecipes().ToList()) {
				var variant = TryMakeRecipeVariantWithSteel(explosiveRecipe);
				if (variant != null) {
					DefDatabase<RecipeDef>.Add(variant);
					injectCount++;
				}
			}

			if(injectCount>0) {
				RemoteExplosivesUtility.Log(string.Format("Injected {0} alternate explosives recipes.", injectCount));
			}
		}

		/**
		 * Add comps to vanilla IED's so that they can be triggered by the manual detonator
		 */
		private void InjectIEDComps() {
			var ieds = new[] {
				DefDatabase<ThingDef>.GetNamedSilentFail("TrapIEDBomb"),
				DefDatabase<ThingDef>.GetNamedSilentFail("TrapIEDIncendiary")
			};
			foreach (var thingDef in ieds) {
				if (thingDef == null) continue;
				thingDef.comps.Add(new CompProperties_WiredDetonationReceiver());
				thingDef.comps.Add(new CompProperties_AutoReplaceable());
			}

		}

		/**
		 * This is a sneaky way to ensure the component is active even in games where the mod was not active at map creation
		 */
		private void EnsureComponentIsActive() {
			LongEventHandler.ExecuteWhenFinished(() => {
				var components = Find.Map.components;
				if(components.Any(c => c is MapComponent_RemoteExplosivesInjector)) return;
				Find.Map.components.Add(this);
			});
		}

		private IEnumerable<RecipeDef> GetAllExplosivesRecipes() {
			return DefDatabase<RecipeDef>.AllDefs.Where(d => {
				var product = d.products.FirstOrDefault();
				return product != null && product.thingDef!=null && product.thingDef.thingCategories!=null && product.thingDef.thingCategories.Contains(explosivesItemCategory);
			});
		} 
		
		// Will return null if recipe requires no components
		private RecipeDef TryMakeRecipeVariantWithSteel(RecipeDef recipeOriginal) {
			var recipeCopy = (RecipeDef)objectCloneMethod.Invoke(recipeOriginal, null);
			recipeCopy.defName += RemoteExplosivesUtility.InjectedRecipeNameSuffix;

			var newFixedFilter = new ThingFilter();
			foreach (var allowedThingDef in recipeOriginal.fixedIngredientFilter.AllowedThingDefs) {
				if (allowedThingDef == ThingDefOf.Component) continue;
				newFixedFilter.SetAllow(allowedThingDef, true);
			}
			newFixedFilter.SetAllow(ThingDefOf.Steel, true);
			recipeCopy.fixedIngredientFilter = newFixedFilter;
			recipeCopy.defaultIngredientFilter = null;

			float numComponentsRequired = 0;
			var newIngredientList = new List<IngredientCount>(recipeOriginal.ingredients);
			foreach (var ingredientCount in newIngredientList) {
				if (ingredientCount.filter.Allows(ThingDefOf.Component)) {
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

		private void DrawDebugControls(){
			if(Widgets.ButtonText(new Rect(10, 10, 50, 20), "Cloud")) {
				DebugTools.curTool = new DebugTool("GasCloud placer", () => {
					const float concentration = 100000;
					var cell = Gen.MouseCell();
					var cloud = Find.ThingGrid.ThingAt<GasCloud>(cell);
					if(cloud!=null) {
						cloud.ReceiveConcentration(concentration);
					} else {
						cloud = (GasCloud)ThingMaker.MakeThing(ThingDef.Named("Gas_Sleeping"));
						cloud.ReceiveConcentration(concentration);
						GenPlace.TryPlaceThing(cloud, cell, ThingPlaceMode.Direct);
					}
				});
			}
			if(Widgets.ButtonText(new Rect(10, 30, 50, 20), "Spark")) {
				DebugTools.curTool = new DebugTool("Spark", () => {
					EffecterDef.Named("SparkweedIgnite").Spawn().Trigger(Gen.MouseCell(), null);
				});
			}
			if(Widgets.ButtonText(new Rect(10, 50, 50, 20), "Failure")) {
				DebugTools.curTool = new DebugTool("Failure", () => {
					EffecterDef.Named("DetCordFailure").Spawn().Trigger(Gen.MouseCell(), null);
				});
			}

		}
	}
}