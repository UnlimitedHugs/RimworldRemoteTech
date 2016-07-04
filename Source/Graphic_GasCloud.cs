using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/**
	 * An advanced version of Graphic_Random with support for alpha, position offset, scaling and rotation
	 */
	public class Graphic_GasCloud : Graphic_Collection {
		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing) {
			var cloud = (GasCloud)thing;
			var materialColor = new Color(color.r, color.g, color.b, color.a * cloud.spriteAlpha);
			var subGraphicId = (cloud.relativeZOrder + thing.Position.x + thing.Position.y) % subGraphics.Length;
			var defaultMat = subGraphics[subGraphicId].MatSide;
			Material material = MaterialPool.MatFrom(new MaterialRequest((Texture2D)defaultMat.mainTexture, defaultMat.shader, materialColor));

			Matrix4x4 matrix = default(Matrix4x4);
			var drawPos = cloud.DrawPos;
			var altitude = Altitudes.AltitudeFor(thing.def.altitudeLayer) + Altitudes.AltInc * cloud.relativeZOrder;
			matrix.SetTRS(new Vector3(drawPos.x+cloud.spriteOffset.x, altitude, drawPos.z+cloud.spriteOffset.y), Quaternion.AngleAxis(cloud.spriteRotation, Vector3.up), new Vector3(drawSize.x*cloud.spriteScaleMultiplier.x, 0, drawSize.y*cloud.spriteScaleMultiplier.y));
			Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
		}

		public override string ToString() {
			return string.Concat(new object[] { "GasCloud(path=", path, ", shader=", Shader, ", color=", color, ", variants=", subGraphics.Length, ")" });
		}
	}
}
