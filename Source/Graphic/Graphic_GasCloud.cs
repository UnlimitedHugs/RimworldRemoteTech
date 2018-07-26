using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/* 
	 * An advanced version of Graphic_Random with support for alpha, position offset, scaling and rotation
	 */
	public class Graphic_GasCloud : Graphic_Collection {
		private const float DistinctAlphaLevels = 128f;

		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation) {
			var cloud = (GasCloud)thing;
			// round alpha to avoid creating too many new materials
			var alpha = Mathf.Round(cloud.spriteAlpha * DistinctAlphaLevels) / DistinctAlphaLevels;
			var materialColor = new Color(color.r, color.g, color.b, color.a*alpha);
			var subGraphicId = (cloud.relativeZOrder + thing.Position.x + thing.Position.y) % subGraphics.Length;
			var defaultMat = subGraphics[subGraphicId].MatSingle;
			var material = MaterialPool.MatFrom(new MaterialRequest((Texture2D)defaultMat.mainTexture, defaultMat.shader, materialColor));
			var drawPos = cloud.DrawPos;
			var altitude = Altitudes.AltitudeFor(thing.def.altitudeLayer) + Altitudes.AltInc * cloud.relativeZOrder;
			var matrix = Matrix4x4.TRS(new Vector3(drawPos.x + cloud.spriteOffset.x, altitude, drawPos.z + cloud.spriteOffset.y), Quaternion.AngleAxis(cloud.spriteRotation, Vector3.up), new Vector3(drawSize.x * cloud.spriteScaleMultiplier.x, 0, drawSize.y * cloud.spriteScaleMultiplier.y));
			Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
		}

		public override string ToString() {
			return string.Concat("GasCloud(path=", path, ", shader=", Shader, ", color=", color, ", variants=", subGraphics.Length, ")");
		}
	}
}
