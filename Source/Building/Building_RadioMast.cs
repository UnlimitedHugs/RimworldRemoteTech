using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// This guy only blinks his overlay- the actual wireless routing is done using CompWirelessDetonationGridNode
	/// </summary>
	public class Building_RadioMast : Building {
		private const int FlareAlphaLevels = 16;

		private GraphicData_Blinker BlinkerData {
			get { return Graphic.data as GraphicData_Blinker; }
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			if (BlinkerData == null) RemoteExplosivesController.Instance.Logger.Error($"{nameof(Building_RadioMast)} needs {nameof(GraphicData_Blinker)} in def {def.defName}");
		}

		public override void Draw() {
			base.Draw();
			// limit the number of possible alpha levels to avoid creating lots of materials
			var props = BlinkerData;
			var alpha = Mathf.Round(Mathf.Max(0f, Mathf.Sin(((Find.TickManager.TicksGame + thingIDNumber * 1000) * Mathf.PI) / Mathf.Max(.1f, props.blinkerIntervalNormal))) * FlareAlphaLevels) / FlareAlphaLevels;
			if(alpha > 0) RemoteExplosivesUtility.DrawFlareOverlay(Resources.Graphics.FlareOverlayNormal, DrawPos, props, alpha);
		}
	}
}