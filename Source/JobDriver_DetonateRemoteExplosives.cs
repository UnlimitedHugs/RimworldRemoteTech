using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RemoteExplosives {
	public class JobDriver_DetonateRemoteExplosives : JobDriver {
		public static string JobDefName = "JobDef_DetonateRemoteExplosives";

		protected override IEnumerable<Toil> MakeNewToils() {
			var table = TargetThingA as Building_DetonatorTable;
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);
			yield return new Toil {
				initAction = ()=>{
					if(table!=null && table.WantsDetonation()) {
						table.DoDetonation();
					} 
				},
				defaultCompleteMode = ToilCompleteMode.Instant,
			};
			
			yield return Toils_Reserve.Release(TargetIndex.A);
		}
	}
}
