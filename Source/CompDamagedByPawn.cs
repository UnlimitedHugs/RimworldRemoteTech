using System.Collections.Generic;
using Verse;

namespace RemoteExplosives {
	public class CompDamagedByPawn : ThingComp {

		private CompPropertiesDamagedByPawn CustomProps {
			get { return props as CompPropertiesDamagedByPawn; }
		}

		private List<Pawn> touchingPawns = new List<Pawn>(1);

		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Collections.LookList(ref touchingPawns, "touchingPawns", LookMode.MapReference);
			if (Scribe.mode == LoadSaveMode.LoadingVars && touchingPawns == null) {
				touchingPawns = new List<Pawn>();
			}
		}

		public override void CompTick() {
			base.CompTick();
			Log.Message("1");
		}
	}
}