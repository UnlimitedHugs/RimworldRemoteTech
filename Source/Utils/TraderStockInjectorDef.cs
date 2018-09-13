// ReSharper disable UnassignedField.Global, CollectionNeverUpdated.Global
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RemoteTech {
	public class TraderStockInjectorDef : Def {
		public TraderKindDef traderDef;
		public List<StockGenerator> stockGenerators = new List<StockGenerator>();
		
		public override void ResolveReferences() {
			base.ResolveReferences();
			foreach (var current in stockGenerators) {
				current.ResolveReferences(traderDef);
			}
		}
	}
}
