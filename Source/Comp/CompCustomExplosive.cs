using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	/* 
	 * An explosive with a timer. Can be triggered silently, but will revert to the vanilla wick if it takes enough damage.
	 */
	public class CompCustomExplosive : ThingComp {
		private bool wickStarted;
		private int wickTicksLeft;
		private Sustainer wickSoundSustainer;
		private bool detonated;
		private int wickTotalTicks;
		private bool wickIsSilent;

		protected Map parentMap;
		protected IntVec3 parentPosition;

		private CompProperties_Explosive ExplosiveProps {
			get { return (CompProperties_Explosive)props; }
		}

		public int WickTotalTicks {
			get { return wickTotalTicks; }
		}

		public int WickTicksLeft {
			get { return wickTicksLeft; }
		}

		protected int StartWickThreshold {
			get {
				return Mathf.RoundToInt(ExplosiveProps.startWickHitPointsPercent * parent.MaxHitPoints);
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			// cache map&position to allow charges destroyed in mass exposions to still be effective
			base.PostSpawnSetup(respawningAfterLoad);
			parentMap = parent.Map;
			parentPosition = parent.Position;
		}

		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Values.Look(ref wickStarted, "wickStarted", false);
			Scribe_Values.Look(ref wickTicksLeft, "wickTicksLeft", 0);
			Scribe_Values.Look(ref wickTotalTicks, "wickTotalTicks", 0);
			Scribe_Values.Look(ref wickIsSilent, "wickIsSilent", false);
		}

		public override void CompTick() {
			if (!wickStarted) return;
			if (wickSoundSustainer == null) {
				if (!wickIsSilent) {
					StartWickSustainer();
				}
			} else {
				wickSoundSustainer.Maintain();
			}
			wickTicksLeft--;
			if (wickTicksLeft <= 0) {
				Detonate();
			}
		}

		private void StartWickSustainer() {
			SoundDefOf.MetalHitImportant.PlayOneShot(parent);
			var info = SoundInfo.InMap(parent, MaintenanceType.PerTick);
			wickSoundSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
		}

		public override void PostDraw() {
			if (wickStarted && !wickIsSilent) {
				parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.BurningWick);
			}
		}

		public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt) {
			if (parent.HitPoints <= 0) {
				if (dinfo.Def.externalViolence) {
					Detonate();
				}
			} else if (wickStarted && (dinfo.Def == DamageDefOf.Stun || (wickIsSilent && dinfo.Def == DamageDefOf.EMP))) { // silent wick can be stopped by EMP
				StopWick();
			} else if (!wickStarted && StartWickThreshold!=0 && parent.HitPoints <= StartWickThreshold && dinfo.Def.externalViolence) {
				StartWick(false);
			}
		}

		public void StartWick(bool silent) {
			if (wickStarted && !silent) wickIsSilent = false;
			if (wickStarted) return;
			wickIsSilent = silent;
			wickStarted = true;
			wickTotalTicks = wickTicksLeft = ExplosiveProps.wickTicks.RandomInRange;
			if (ExplosiveProps.explosiveDamageType != null) {
				GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(parent, ExplosiveProps.explosiveDamageType);
			}
		}

		public void StopWick() {
			wickStarted = false;
		}

		public bool WickStarted {
			get { return wickStarted; }
		}

		protected virtual void Detonate() {
			if (detonated) return;
			detonated = true;
			if (!parent.Destroyed) {
				parent.Destroy(DestroyMode.KillFinalize);
			}
			var exProps = ExplosiveProps;
			if (exProps.explosiveDamageType != null) {
				float num = exProps.explosiveRadius;
				if (parent.stackCount > 1 && exProps.explosiveExpandPerStackcount > 0f) {
					num += Mathf.Sqrt((parent.stackCount - 1)*exProps.explosiveExpandPerStackcount);
				}
				GenExplosion.DoExplosion(parentPosition, parentMap, num, exProps.explosiveDamageType, parent);
			}
		}

		
	}
}