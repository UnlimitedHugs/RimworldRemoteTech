// ReSharper disable UnassignedField.Global
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Auto-filled repository of all external resources referenced in the code
	/// </summary>
	public static class Resources {
		[DefOf]
		public static class Sound {
			public static SoundDef rxDetonatorLever;
			public static SoundDef rxFoamSpray;
			public static SoundDef rxFoamSolidify;
			public static SoundDef rxArmed;
			public static SoundDef rxBeep;
			public static SoundDef rxChannelChange;
			public static SoundDef rxDialClick;
			public static SoundDef rxEmpCharge;
			public static SoundDef rxMiningCavein;
		}

		[DefOf]
		public static class Damage {
			public static DamageDef rxFoamWallStuck;
		}

		[DefOf]
		public static class Job {
			public static JobDef rxDryDetonatorWire;
			public static JobDef rxSwitchRemoteExplosives;
			public static JobDef rxDetonateExplosives;
			public static JobDef rxSmoothFoamWall;
			public static JobDef rxInstallUpgrade;
		}

		[DefOf]
		public static class Thing {
			public static ThingDef rxTableDetonator;
			public static ThingDef rxCollapsedRoofRocks;
			public static ThingDef rxGas_Sleeping;
			public static ThingDef rxFoamWallSmooth;
			public static ThingDef rxFoamWallBricks;
			public static ThingDef rxPlantSparkweed;
		}

		[DefOf]
		public static class Research {
			public static ResearchProjectDef rxChannels;
			public static ResearchProjectDef rxChannelsAdvanced;
		}

		[DefOf]
		public static class KeyBinging {
			public static KeyBindingDef rxNextChannel;
			public static KeyBindingDef rxRemoteTableDetonate;
			public static KeyBindingDef rxArm;
			public static KeyBindingDef rxAutoReplace;
			public static KeyBindingDef rxPortableDetonatorDetonate;
		}

		[DefOf]
		public static class Designation {
			public static DesignationDef rxRemoteExplosiveSwitch;
			public static DesignationDef rxDetonatorWireDryOff;
			public static DesignationDef rxFoamWallSmooth;
			public static DesignationDef rxInstallUpgrade;
		}

		[DefOf]
		public static class Stat {
			public static StatDef rxPortableDetonatorRange;
			public static StatDef rxPortableDetonatorNumUses;
			public static StatDef rxExplosiveChunkYield;
			public static StatDef rxExplosiveMiningYield;
			public static StatDef rxExplosiveWoodYield;
		}

		[DefOf]
		public static class Effecter {
			public static EffecterDef rxSparkweedIgnite;
			public static EffecterDef rxDetWireFailure;
		}

		[DefOf]
		public static class ThingCategory {
			public static ThingCategoryDef rxExplosives;
		}

		[StaticConstructorOnStartup]
		public static class Graphics {
			private const LinkFlags OverlayAtlasLinkFlags = LinkFlags.Custom3;

			public static readonly Graphic FlareOverlayNormal = GraphicDatabase.Get<Graphic_Single>("mine_flare", ShaderDatabase.TransparentPostLight);
			public static readonly Graphic FlareOverlayStrong = GraphicDatabase.Get<Graphic_Single>("mine_flare_strong", ShaderDatabase.TransparentPostLight);
			public static readonly Graphic DetWireOverlayAtlas = GraphicDatabase.Get<Graphic_Single>("DetWire/det_wire_overlay_atlas", ShaderDatabase.MetaOverlay);
			public static readonly Graphic DetWireOverlayEndpoint = GraphicDatabase.Get<Graphic_Single>("DetWire/connection_point_overlay", ShaderDatabase.MetaOverlay);
			public static readonly Graphic DetWireOverlayCrossing = GraphicDatabase.Get<Graphic_Single>("DetWire/crossing_overlay", ShaderDatabase.MetaOverlay);

			static Graphics() {
				DetWireOverlayAtlas = GraphicUtility.WrapLinked(DetWireOverlayAtlas, LinkDrawerType.Basic);
				DetWireOverlayAtlas.data = new GraphicData { linkFlags = OverlayAtlasLinkFlags };
			}
		}

		[StaticConstructorOnStartup]
		public static class Textures {
			public static Texture2D UI_Trigger;
			public static Texture2D UIDetonate;
			public static Texture2D UIDryOff;
			public static Texture2D UIArm;
			public static Texture2D UIAutoReplace;
			public static Texture2D UIChannelBasic1;
			public static Texture2D UIChannelBasic2;
			public static Texture2D UIChannelBasic3;
			public static Texture2D UIChannelKeypadAtlas;
			public static Texture2D UIDetonatorPortable;
			public static Texture2D UISelectWire;
			public static Texture2D UIUpgrade;
			public static Texture2D gas_vent_arrow;

			public static readonly Texture2D WallSmoothMenuIcon = ContentFinder<Texture2D>.Get("Things/Building/Linked/WallSmooth_MenuIcon");

			// defines sprite offsets within the channel keypad atlas
			public static readonly KepadAtlas KeypadAtlasCoords = new KepadAtlas();
			public class KepadAtlas {
				private const float Cell = .25f;
				private const float TxSize = (31f / 32f) * Cell;
				public readonly Rect[] Keys = {
					new Rect(0f, Cell*3, TxSize, TxSize),
					new Rect(Cell, Cell*3, TxSize, TxSize),
					new Rect(Cell*2, Cell*3, TxSize, TxSize),
					new Rect(0f, Cell*2, TxSize, TxSize),
					new Rect(Cell, Cell*2, TxSize, TxSize),
					new Rect(Cell*2, Cell*2, TxSize, TxSize),
					new Rect(0f, Cell, TxSize, TxSize),
					new Rect(Cell, Cell, TxSize, TxSize)
				};
				public readonly Rect OutlineOff = new Rect(0f, 0f, TxSize, TxSize);
				public readonly Rect OutlineHighlight = new Rect(Cell, 0f, TxSize, TxSize);
				public readonly Rect OutlineSelected = new Rect(Cell*2, 0f, TxSize, TxSize);
			}

			static Textures() {
				foreach (var fieldInfo in typeof(Textures).GetFields(HugsLibUtility.AllBindingFlags)) {
					if (fieldInfo.IsInitOnly) continue;
					fieldInfo.SetValue(null, ContentFinder<Texture2D>.Get(fieldInfo.Name));
				}
			}
		}
	}
}