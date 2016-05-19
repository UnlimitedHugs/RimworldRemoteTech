using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	public class CompChemicalExplosive : CompCustomExplosive {
		private CompProperties_ChemicalExplosive customProps;

		public override void Initialize(CompProperties p) {
			base.Initialize(p);
			customProps = (CompProperties_ChemicalExplosive)p;
		}

		protected override void Detonate() {
			base.Detonate();
			if (customProps.SpawnThingDef == null) return;
			var thing = ThingMaker.MakeThing(customProps.SpawnThingDef);
			GenPlace.TryPlaceThing(thing, parent.Position, ThingPlaceMode.Direct);
			if(thing is Building_FoamBlob && customProps.NumFoamBlobs>1) {
				(thing as Building_FoamBlob).SetSpreadingCharges(customProps.NumFoamBlobs-1);
			}
		}

		public override void PostDestroy(DestroyMode mode, bool wasSpawned) {
			base.PostDestroy(mode, wasSpawned);
			if(wasSpawned && mode == DestroyMode.Kill && customProps.BreakSound!=null) {
				customProps.BreakSound.PlayOneShot(parent);
			}
		}
	}
}
