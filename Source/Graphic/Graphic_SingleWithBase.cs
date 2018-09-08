using System;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/**
	 * A Graphic_Single placed on an offset base. Used to give the workbenches a custom vanilla-style 3d look.
	 * Chosen over the vanilla approach for faster loading (fewer textures to load, smaller size).
	 */
	public class Graphic_SingleWithBase : Graphic_Single {
		private GraphicData_SidedBase baseData;
		private Material baseMatFront;
		private Material baseMatSide;

		public override void Init(GraphicRequest req) {
			base.Init(req);
			baseData = data as GraphicData_SidedBase;
			if (baseData != null) {
				baseMatFront = MaterialPool.MatFrom(new MaterialRequest(baseData.BaseFrontTex, baseData.shaderType.Shader, baseData.color));
				baseMatSide = MaterialPool.MatFrom(new MaterialRequest(baseData.BaseSideTex, baseData.shaderType.Shader, baseData.color));
			}
		}

		public override Mesh MeshAt(Rot4 rot) {
			return base.MeshAt(Rot4.North); // fixes incorrect mesh orientation while vertical
		}

		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation) {
			if (baseData == null) {
				base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
				return;
			}
			
			var useFrontBase = !thing.Rotation.IsHorizontal;
			var baseOffset = new Vector3(baseData.baseOffset.x, 0, baseData.baseOffset.y - (thing.RotatedSize.z - 1) / 2f);
			var baseMat = useFrontBase ? baseMatFront : baseMatSide; 
			var baseMesh = MeshPool.GridPlane(new Vector2(thing.RotatedSize.x, 1));
			Graphics.DrawMesh(baseMesh, loc + baseOffset, Quaternion.identity, baseMat, 0);

			var mesh = MeshAt(rot);
			var rotation = QuatFromRot(rot);
			var material = MatAt(rot, thing);
			Graphics.DrawMesh(mesh, loc, rotation, material, 0);
			if (ShadowGraphic != null) {
				ShadowGraphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
			}
		}

	}
}