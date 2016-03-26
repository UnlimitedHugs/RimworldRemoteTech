using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

/*
 * A CompExplosive that can be quietely lit but will also use the regular fuse animation & sound when triggered by damage
 */
public class CompCustomExplosive : CompExplosive {

	private static readonly int PawnNotifyCellCount = GenRadial.NumCellsInRadius(4.5f);

	private bool silentWickStarted;
	private int silentWickTicksLeft;
	private MethodInfo pawnNotifyMethod;
	private int silentWickTotalTicks;

	public override void Initialize(CompProperties p) {
		base.Initialize(p);

		// some reflection to get access to an internal method
		pawnNotifyMethod = typeof(Pawn_MindState).GetMethod("Notify_WickStarted", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	public override void CompTick() {
		base.CompTick();
		if(!silentWickStarted) return;
		silentWickTicksLeft--;
		if(silentWickTicksLeft<=0) Detonate();
	}

	public override void PostExposeData() {
		base.PostExposeData();
		Scribe_Values.LookValue(ref silentWickStarted, "silentWickStarted", false);
		Scribe_Values.LookValue(ref silentWickTicksLeft, "silentWickTicksLeft", 0);
		Scribe_Values.LookValue(ref silentWickTotalTicks, "silentWickTotalTicks", 0);
	}

	public void StartSilentWick() {
		if(WickStarted) return;
		silentWickTotalTicks = silentWickTicksLeft = props.wickTicks.RandomInRange;
		silentWickStarted = true;
		NotifyNearbyPawns();
	}

	public bool WickStarted {
		get { return wickStarted || silentWickStarted; }
	}

	public int WickTotalTicks {

		get { return silentWickTotalTicks; }

	}

	public int WickTicksRemaining {

		get { return silentWickTicksLeft; }

	}

	private void NotifyNearbyPawns() {
		Room room = parent.GetRoom();
		for (int i = 0; i < PawnNotifyCellCount; i++) {
			var c = parent.Position + GenRadial.RadialPattern[i];
			if (c.InBounds()) {
				var thingList = c.GetThingList();
				for (int j = 0; j < thingList.Count; j++) {
					var p = thingList[j] as Pawn;
					if (p != null
						&& p.RaceProps.intelligence >= Intelligence.Humanlike
						&& !p.Drafted
						&& p.Position.GetRoom() == room
						&& GenSight.LineOfSight(parent.Position, p.Position, true))
					//p.mindState.Notify_WickStarted(parent);
					pawnNotifyMethod.Invoke(p.mindState, new object[] { parent });
				}
			}
		}
	}
}