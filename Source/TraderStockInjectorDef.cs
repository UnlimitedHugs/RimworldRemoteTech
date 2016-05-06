using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RemoteExplosives {
	public class TraderStockInjectorDef : Def {
		public TraderKindDef traderDef;
		public List<StockGenerator> stockGenerators = new List<StockGenerator>();

		public override void PostLoad() {
			base.PostLoad();
			foreach (StockGenerator current in stockGenerators) {
				current.PostLoad();
			}
		}
		
		public override void ResolveReferences() {
			base.ResolveReferences();
			foreach (StockGenerator current in stockGenerators) {
				current.ResolveReferences(traderDef);
			}
		}
	}
}
