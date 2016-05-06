using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RemoteExplosives {
	/**
	 * Injects StockGenerators into existing traders. Technically, not a component of the map, but it provides a covenient entry point at map load.
	 */
	public class MapComponent_TraderStockInjector : MapComponent {
		public MapComponent_TraderStockInjector() {
			InjectTraderStocks();
		}

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
	}
}