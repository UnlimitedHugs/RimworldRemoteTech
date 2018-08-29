using HugsLib.Utils;
using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	/*
	 * A remote explosive that creates things on detonation.
	 */
	public class CompChemicalExplosive : CompCustomExplosive {
		private CompProperties_ChemicalExplosive customProps;

		public override void Initialize(CompProperties p) {
			base.Initialize(p);
			customProps = (CompProperties_ChemicalExplosive)p;
		}

		protected override void Detonate() {
			var stackCount = parent.stackCount;
			base.Detonate();
			if (customProps.spawnThingDef == null) return;
			var thing = parentPosition.GetFirstThing(parentMap, customProps.spawnThingDef);
			var existingThing = thing != null;
			if (thing == null) {
				thing = ThingMaker.MakeThing(customProps.spawnThingDef);
				GenPlace.TryPlaceThing(thing, parentPosition, parentMap, ThingPlaceMode.Direct);
			}
			if (thing is Building_FoamBlob) {
				if (customProps.numFoamBlobs > 1) {
					(thing as Building_FoamBlob).AddSpreadingCharges(customProps.numFoamBlobs * stackCount - (existingThing ? 0 : 1));
				}
			} else if(thing is GasCloud) {
				if (customProps.gasConcentration > 0) {
					(thing as GasCloud).ReceiveConcentration(customProps.gasConcentration * stackCount);
				}
			}
		}

		public override void PostDestroy(DestroyMode mode, Map map) {
			base.PostDestroy(mode, map);
			if (map != null && mode == DestroyMode.KillFinalize && customProps.breakSound != null) {
				customProps.breakSound.PlayOneShot(parent);
			}
		}
	}
}
