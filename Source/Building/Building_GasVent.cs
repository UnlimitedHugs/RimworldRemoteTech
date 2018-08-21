using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	public class Building_GasVent : Building {
		// improves draining of the last view points from source room
		private const float MinSourceConcentration = 3f;

		private Room targetRoom;
		private Room sourceRoom;
		private IntVec3 targetCell;
		private IntVec3 sourceCell;
		private CompPowerTrader powerComp;
		private bool roomsAreValid;
		private float moveBuffer;
		private BuildingProperties_GasVent ventProps;
		private CachedValue<float> statVentAmount;

		private bool PowerOn {
			get { return powerComp == null || powerComp.PowerOn; }
		}

		public Building_GasVent() {
			statVentAmount = new CachedValue<float>(() => this.GetStatValue(Resources.Stat.rxVentingPower));
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			powerComp = GetComp<CompPowerTrader>();
			targetCell = Position + IntVec3.North.RotatedBy(Rotation);
			sourceCell = Position + IntVec3.South.RotatedBy(Rotation);
			ventProps = def.building as BuildingProperties_GasVent;
			if (ventProps == null) {
				RemoteExplosivesController.Instance.Logger.Error("Building_GasVent requires BuildingProperties_GasVent");
			}
			ValidateRooms();
		}

		public override void Tick() {
			base.Tick();
			if(!PowerOn || ventProps == null) return;

			// move gas
			if (roomsAreValid) {
				var sourceCloud = RemoteExplosivesUtility.TryFindGasCloudAt(Map, sourceCell);
				if (sourceCloud != null) {
					RemoteExplosivesUtility.ReportPowerUse(this);
					// move only whole units of concentration
					moveBuffer += Mathf.Min(sourceCloud.Concentration - MinSourceConcentration, statVentAmount / GenTicks.TicksPerRealSecond);
					if (moveBuffer > 1) {
						var moveAmount = Mathf.FloorToInt(moveBuffer);
						RemoteExplosivesUtility.DeployGas(Map, targetCell, sourceCloud.def, moveAmount);
						sourceCloud.ReceiveConcentration(-moveAmount);
						moveBuffer -= moveAmount;
					}
				}
			}

			// equalize heat
			if (this.IsHashIntervalTick(GenTicks.TicksPerRealSecond)) {
				ValidateRooms();

				if (roomsAreValid && ValidateHeatExchange()) {
					float pointTemp;
					if (targetRoom.UsesOutdoorTemperature) {
						pointTemp = targetRoom.Temperature;
					} else if (sourceRoom.UsesOutdoorTemperature) {
						pointTemp = sourceRoom.Temperature;
					} else {
						pointTemp = (targetRoom.Temperature * targetRoom.CellCount + sourceRoom.Temperature * sourceRoom.CellCount) / (targetRoom.CellCount + sourceRoom.CellCount);
					}
					if (!targetRoom.UsesOutdoorTemperature) {
						EqualizeHeat(targetRoom, pointTemp, ventProps.heatExchangedPerSecond);
					}
					if (!sourceRoom.UsesOutdoorTemperature) {
						EqualizeHeat(sourceRoom, pointTemp, ventProps.heatExchangedPerSecond);
					}
				}
			}
		}

		private void EqualizeHeat(Room room, float targetTemp, float rate) {
			var moveAmount = Mathf.Abs(room.Temperature - targetTemp) * rate;
			if (targetTemp < room.Temperature) {
				room.Group.Temperature = Mathf.Max(targetTemp, room.Temperature - moveAmount);
			} else if (targetTemp > room.Temperature) {
				room.Group.Temperature = Mathf.Min(targetTemp, room.Temperature + moveAmount);
			}
		}

		protected override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);
			statVentAmount.Recache();
		}

		public override string GetInspectString() {
			var str = new StringBuilder(base.GetInspectString());
			str.AppendLine();
			str.Append("GasVent_ventedPerSecond".Translate(statVentAmount.Value));
			if (!roomsAreValid) {
				str.AppendLine();	
				str.Append("GasVent_blocked".Translate());
			}
			return str.ToString();
		}

		private void ValidateRooms() {
			if (targetCell.Impassable(Map) || sourceCell.Impassable(Map) || !Map.regionAndRoomUpdater.Enabled) {
				roomsAreValid = false;
			} else {
				targetRoom = GridsUtility.GetRoom(Position + IntVec3.North.RotatedBy(Rotation), Map);
				sourceRoom = GridsUtility.GetRoom(Position + IntVec3.South.RotatedBy(Rotation), Map);
				roomsAreValid = targetRoom != null && sourceRoom != null && targetRoom != sourceRoom;
			}
		}

		private bool ValidateHeatExchange() {
			return !targetRoom.UsesOutdoorTemperature || !sourceRoom.UsesOutdoorTemperature;
		}
	}
}