// ReSharper disable UnassignedField.Global
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	public class GraphicData_SidedBase : GraphicData {
		public string baseFrontTexPath;
		public string baseSideTexPath;
		public Vector2 baseOffset;

		public Texture2D BaseFrontTex { get; private set; }
		public Texture2D BaseSideTex { get; private set; }

		public void PostLoad() {
			LongEventHandler.ExecuteWhenFinished(() => {
				BaseFrontTex = ContentFinder<Texture2D>.Get(baseFrontTexPath);
				BaseSideTex = ContentFinder<Texture2D>.Get(baseSideTexPath);
			});
		}
	}
}