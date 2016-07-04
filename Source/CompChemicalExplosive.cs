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
			if (customProps.spawnThingDef == null) return;
			var thing = ThingMaker.MakeThing(customProps.spawnThingDef);
			GenPlace.TryPlaceThing(thing, parent.Position, ThingPlaceMode.Direct);
			if (thing is Building_FoamBlob) {
				if (customProps.numFoamBlobs > 1) {
					(thing as Building_FoamBlob).SetSpreadingCharges(customProps.numFoamBlobs - 1);
				}
			} else if(thing is GasCloud) {
				if (customProps.gasConcentration > 0) {
					(thing as GasCloud).ReceiveConcentration(customProps.gasConcentration);
				}
			}
		}

		public override void PostDestroy(DestroyMode mode, bool wasSpawned) {
			base.PostDestroy(mode, wasSpawned);
			if(wasSpawned && mode == DestroyMode.Kill && customProps.breakSound!=null) {
				customProps.breakSound.PlayOneShot(parent);
			}
		}
	}
}
