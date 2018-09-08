using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	/// <summary>
	/// A battery that has variable capacity based on the rxPowerCapacity stat of the parent thing.
	/// Also contains overlay drawing and exploding logic form Building_Battery.
	/// </summary>
	public class CompStatBattery : CompPowerBattery, IBatteryPropsProvider {
		private const float MinChargeToExplode = .75f;
		private const float ChargeToLoseWhenExplode = .8f;
		private const float ExplodeChancePerDamage = 0.05f;

		private CompProperties_BatteryWithBar customProps;
		public CompProperties_Battery ReplacementProps {
			get {
				customProps = customProps ?? props as CompProperties_BatteryWithBar ?? new CompProperties_BatteryWithBar();
				customProps.storedEnergyMax = statMaxEnergy;
				return customProps;
			}
		}
		
		private CachedValue<float> statMaxEnergy;
		private Sustainer wickSustainer;

		// saved
		private int ticksToExplode;
		
		public override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);
			if(signal == CompUpgrade.UpgradeCompleteSignal) statMaxEnergy.Recache();
		}

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			base.PostSpawnSetup(respawningAfterLoad);
			statMaxEnergy = parent.GetCachedStat(Resources.Stat.rxPowerCapacity);
			if (statMaxEnergy <= 0f) RemoteExplosivesController.Instance.Logger.Error($"{nameof(CompStatBattery)} has zero power capacity. Missing rxPowerCapacity stat in def {parent.def.defName}?");
		}

		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Values.Look(ref ticksToExplode, "ticksToExplode");
		}

		public override void PostDraw() {
			var fillPercent = StoredEnergy / ReplacementProps.storedEnergyMax;
			var rotation = parent.Rotation;
			rotation.Rotate(RotationDirection.Clockwise);
			var r = new GenDraw.FillableBarRequest {
				center = parent.DrawPos + customProps.barOffset,
				size = customProps.barSize,
				fillPercent = fillPercent,
				filledMat = Resources.Materials.BatteryBarFilledMat,
				unfilledMat = Resources.Materials.BatteryBarUnfilledMat,
				margin = customProps.barMargin,
				rotation = rotation
			};
			GenDraw.DrawFillableBar(r);
			if (ticksToExplode > 0 && parent.Spawned) {
				parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.BurningWick);
			}
		}

		public override string CompInspectStringExtra() {
			var batteryProps = (CompProperties_BatteryWithBar)ReplacementProps;
			var stored = StoredEnergy;
			var s = new StringBuilder();
			s.Append($"{"PowerBatteryStored".Translate()}: {stored:F0} / {batteryProps.storedEnergyMax:F0} Wd");
			s.Append($"\n{"PowerBatteryEfficiency".Translate()}: {batteryProps.efficiency * 100f:F0}%");
			if (stored > 0f && batteryProps.passiveDischargeWatts > 0) {
				s.Append($"\n{"SelfDischarging".Translate()}: {batteryProps.passiveDischargeWatts:F0} W");
			}
			return s.ToString();
		}

		public override void CompTick() {
			var batteryProps = (CompProperties_BatteryWithBar)ReplacementProps;
			if (batteryProps.passiveDischargeWatts > 0f) {
				DrawPower(Mathf.Min(batteryProps.passiveDischargeWatts * WattsToWattDaysPerTick, StoredEnergy));
			}

			if (!parent.Spawned) return;
			if (ticksToExplode > 0) {
				if (wickSustainer == null) {
					StartWickSustainer();
				} else {
					wickSustainer.Maintain();
				}
				ticksToExplode--;
				if (ticksToExplode == 0) {
					var randomCell = parent.OccupiedRect().RandomCell;
					float radius = Rand.Range(0.5f, 1f) * 3f;
					GenExplosion.DoExplosion(randomCell, parent.Map, radius, DamageDefOf.Flame, null);
					DrawPower(Mathf.Max(ChargeToLoseWhenExplode * statMaxEnergy, StoredEnergy));
				}
			}
		}

		public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt) {
			base.PostPostApplyDamage(dinfo, totalDamageDealt);
			if (!parent.Destroyed && ticksToExplode == 0 && dinfo.Def == DamageDefOf.Flame && Rand.Value < ExplodeChancePerDamage && StoredEnergy > MinChargeToExplode * statMaxEnergy) {
				ticksToExplode = Rand.Range(70, 150);
				StartWickSustainer();
			}
		}

		private void StartWickSustainer() {
			var info = SoundInfo.InMap(parent, MaintenanceType.PerTick);
			wickSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
		}
	}
}