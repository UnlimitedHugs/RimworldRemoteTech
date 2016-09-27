using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	 /*
	 * A wire to connect the detonator to explosives.
	 * Will get wet during rain and have a chance to fail when used unless dried.
	 */
	[StaticConstructorOnStartup]
	public class Building_DetonatorCord : Building {
		private const float FreezeTemperature = -1f;
		private const float WetWeatherThreshold = .5f;
		private const float TicksPerDay = 60000;
		private const float RareTicksPerDay = TicksPerDay/GenTicks.TickRareInterval;
		private const float MaxWetness = 1f;

		private static readonly Texture2D UITex_DryOff = ContentFinder<Texture2D>.Get("UIDryOff");

		private BuildingProperties_DetonatorCord CustomProps {
			get {
				if (!(def.building is BuildingProperties_DetonatorCord)) throw new Exception("Building_DetonatorCord requires BuildingProperties_DetonatorCord");
				return def.building as BuildingProperties_DetonatorCord;
			}
		}

		public bool WantDrying {
			get { return wantDrying; }
		}

		public int DryOffJobDuration {
			get { return CustomProps.dryOffJobDurationTicks; }
		}

		private float Wetness {
			get { return wetness; }
			set { wetness = Mathf.Min(value, Mathf.Max(MaxWetness, 0)); }
		}

		private float wetness;
		private bool wantDrying;

		public override void SpawnSetup() {
			base.SpawnSetup();
			var comp = GetComp<CompWiredDetonationTransmitter>();
			if (comp != null) comp.signalPassageTest = SignalPassageTest;
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.LookValue(ref wetness, "wetness", 0);
			Scribe_Values.LookValue(ref wantDrying, "wantDrying", false);
		}

		public override void TickRare() {
			base.TickRare();
			var room = Position.GetRoom();
			var temperature = room == null ? 0 : room.Temperature;
			var frozen = temperature < FreezeTemperature;
			var wetWeather = Find.WeatherManager.RainRate > WetWeatherThreshold;
			if (wetWeather) {
				if (!frozen && !IsCovered()) {
					Wetness = MaxWetness;
				}
			} else {
				if (Wetness > 0 && temperature > 0) {
					Wetness -= (1 / (CustomProps.daysToSelfDry * RareTicksPerDay)) * (temperature/CustomProps.baseDryingTemperature);
				}
			}
			if (wantDrying && Wetness == 0) {
				wantDrying = false;
				UpdateDesignation();
			}
		}

		public override string GetInspectString() {
			return string.Concat(
				wetness > 0 ? "Cord_inspect_wet".Translate(Mathf.Round(wetness*100)) : "Cord_inspect_dry".Translate(),
				", ",
				IsCovered() ? "Cord_inspect_covered".Translate() : "Cord_inspect_exposed".Translate());
		}

		public override IEnumerable<Gizmo> GetGizmos() {
			if (wetness > 0) {
				yield return new Command_Toggle {
					toggleAction = DryGizmoAction,
					isActive = () => wantDrying,
					icon = UITex_DryOff,
					defaultLabel = "Cord_dry_label".Translate(),
					defaultDesc = "Cord_dry_desc".Translate(),
					hotKey = KeyBindingDefOf.Misc1
				};
			}

			foreach (var gizmo in base.GetGizmos()) {
				yield return gizmo;
			}
		}

		public void DryOff() {
			Wetness = 0;
			wantDrying = false;
			UpdateDesignation();
		}

		private void DryGizmoAction() {
			wantDrying = !wantDrying;
			UpdateDesignation();
		}

		private bool SignalPassageTest() {
			var failChance = wetness > 0 ? CustomProps.failureChanceWhenFullyWet * wetness : 0;
			var success = failChance > 0 ? Rand.Range(0, 1f) > failChance : true;
			if(!success) DoFailure();
			return success;
		}

		private bool IsCovered() {
			return Position.Roofed() || Find.EdificeGrid[CellIndices.CellToIndex(Position)] != null;
		}

		private void DoFailure() {
			if(CustomProps.failureEffecter!=null) CustomProps.failureEffecter.Spawn().Trigger(Position, null);
			Destroy(DestroyMode.Kill);
			// try spawn fire in own or adjacent cell
			var adjacents = GenAdj.CardinalDirections.ToList();
			adjacents.Shuffle();
			adjacents.Add(IntVec3.Zero);
			adjacents.Reverse();
			Fire created = null;
			foreach (var adjacent in adjacents) {
				var candidatePos = adjacent + Position;
				FireUtility.TryStartFireIn(candidatePos, Rand.Range(.4f, .6f));
				created = Find.ThingGrid.ThingAt<Fire>(candidatePos);
				if (created != null) break;
			}
			Alert_DetonatorCordFailure.Instance.ReportFailue(created);
		}

		private void UpdateDesignation() {
			bool enable = wantDrying;
			var designationDef = RemoteExplosivesUtility.DryOffDesigationDef;
			var hasDesignation = Find.DesignationManager.DesignationOn(this, designationDef) != null;
			if (!hasDesignation && enable) {
				Find.DesignationManager.AddDesignation(new Designation(this, designationDef));
			} else if (hasDesignation && !enable) {
				Find.DesignationManager.RemoveDesignation(Find.DesignationManager.DesignationOn(this, designationDef));
			}
		}
	}
}