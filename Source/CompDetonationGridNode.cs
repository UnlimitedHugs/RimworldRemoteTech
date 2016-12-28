using Verse;

namespace RemoteExplosives {
	/**
	 * Base class for comps involved in the detonation grid. Required to print the overlay section layer.
	 */
	[StaticConstructorOnStartup]
	public abstract class CompDetonationGridNode : ThingComp {
		private const string OverlayAtlasPath = "DetWire/det_wire_overlay_atlas";
		private const string OverlayEndpointPath = "DetWire/connection_point_overlay";
		private const LinkFlags OverlayAtlasLinkFlags = LinkFlags.Custom3;
		private static readonly Graphic OverlayAtlasGraphic;
		private static readonly Graphic OverlayEndpointGraphic;

		static CompDetonationGridNode() {
			var atlasBase = GraphicDatabase.Get<Graphic_Single>(OverlayAtlasPath, ShaderDatabase.MetaOverlay);
			OverlayAtlasGraphic = GraphicUtility.WrapLinked(atlasBase, LinkDrawerType.Basic);
			OverlayAtlasGraphic.data = new GraphicData { linkFlags = OverlayAtlasLinkFlags };
			OverlayEndpointGraphic = GraphicDatabase.Get<Graphic_Single>(OverlayEndpointPath, ShaderDatabase.MetaOverlay);
		}

		private IntVec3 cachedPosition = IntVec3.Invalid;

		public override void PostSpawnSetup() {
			base.PostSpawnSetup();
			if (parent is Building) {
				cachedPosition = parent.Position;
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Buildings);
			}
		}

		public override void PostDeSpawn(Map map) {
			base.PostDeSpawn(map);
			if (cachedPosition.IsValid) {
				map.mapDrawer.MapMeshDirty(cachedPosition, MapMeshFlag.Buildings);
			}
		}

		public abstract void PrintForDetonationGrid(SectionLayer layer);

		protected void PrintConnection(SectionLayer layer) {
			OverlayAtlasGraphic.Print(layer, parent);
		}

		protected void PrintEndpoint(SectionLayer layer) {
			OverlayEndpointGraphic.Print(layer, parent);
		}
	}
}