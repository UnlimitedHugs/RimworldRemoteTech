using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteTech {
	public class Building_GasVent : Building {
		// improves draining of the last few points from source room
		private const float MinSourceConcentration = 3f;

		private IntVec3 targetCell;
		private IntVec3 sourceCell;
		private CompPowerTrader powerComp;
		private float moveBuffer;
		private BuildingProperties_GasVent ventProps;
		private CachedValue<float> statVentAmount;

		private bool PowerOn {
			get { return powerComp == null || powerComp.PowerOn; }
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			powerComp = GetComp<CompPowerTrader>();
			statVentAmount = this.GetCachedStat(Resources.Stat.rxVentingPower);
			targetCell = Position + IntVec3.North.RotatedBy(Rotation);
			sourceCell = Position + IntVec3.South.RotatedBy(Rotation);
			ventProps = def.building as BuildingProperties_GasVent;
			if (ventProps == null) {
				RemoteTechController.Instance.Logger.Error("Building_GasVent requires BuildingProperties_GasVent");
			}
		}

		public override void Tick() {
			base.Tick();
			if(!PowerOn || ventProps == null) return;

			PushGas();

			if (this.IsHashIntervalTick(GenTicks.TicksPerRealSecond)) {
				PushHeat();
			}
		}

		private void PushGas() {
			if (FrontAndBackAreAccessble()) {
				var sourceCloud = RemoteTechUtility.TryFindGasCloudAt(Map, sourceCell);
				if (sourceCloud != null) {
					RemoteTechUtility.ReportPowerUse(this);
					// move only whole units of concentration
					moveBuffer += Mathf.Min(sourceCloud.Concentration - MinSourceConcentration, statVentAmount / GenTicks.TicksPerRealSecond);
					if (moveBuffer > 1) {
						var moveAmount = Mathf.FloorToInt(moveBuffer);
						RemoteTechUtility.DeployGas(Map, targetCell, sourceCloud.def, moveAmount);
						sourceCloud.ReceiveConcentration(-moveAmount);
						moveBuffer -= moveAmount;
					}
				}
			}
		}

		private void PushHeat() {
			var heatExchangeIsValid = TryGetHeatExchangeRooms(out Room sourceRoom, out Room targetRoom);
			if (heatExchangeIsValid) {
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

		private void EqualizeHeat(Room room, float targetTemp, float rate) {
			var moveAmount = Mathf.Abs(room.Temperature - targetTemp) * rate;
			if (targetTemp < room.Temperature) {
				room.Temperature = Mathf.Max(targetTemp, room.Temperature - moveAmount);
			} else if (targetTemp > room.Temperature) {
				room.Temperature = Mathf.Min(targetTemp, room.Temperature + moveAmount);
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
			if (!FrontAndBackAreAccessble()) {
				str.AppendLine();	
				str.Append("GasVent_blocked".Translate());
			}
			return str.ToString();
		}

		private bool FrontAndBackAreAccessble() {
			return !(targetCell.Impassable(Map) || sourceCell.Impassable(Map));
		}

		private bool TryGetHeatExchangeRooms(out Room sourceRoom, out Room targetRoom) {
			if (FrontAndBackAreAccessble() && Map.regionAndRoomUpdater.Enabled) {
				targetRoom = GridsUtility.GetRoom(Position + IntVec3.North.RotatedBy(Rotation), Map);
				sourceRoom = GridsUtility.GetRoom(Position + IntVec3.South.RotatedBy(Rotation), Map);
				if (targetRoom != null && sourceRoom != null && targetRoom != sourceRoom) {
					return true;
				}
			}
			sourceRoom = targetRoom = null;
			return false;
		}
	}
}