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
		public static class MessageType {
			public static MessageTypeDef rxSensorOne;
			public static MessageTypeDef rxSensorTwo;
		}

		[DefOf]
		public static class Job {
			public static JobDef rxDryDetonatorWire;
			public static JobDef rxSwitchThing;
			public static JobDef rxDetonateExplosives;
			public static JobDef rxSmoothFoamWall;
			public static JobDef rxInstallUpgrade;
			public static JobDef rxRedButtonFever;
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
			public static DesignationDef rxSwitchThing;
			public static DesignationDef rxDetonatorWireDryOff;
			public static DesignationDef rxFoamWallSmooth;
			public static DesignationDef rxInstallUpgrade;
		}

		[DefOf]
		public static class Stat {
			public static StatDef rxPortableDetonatorNumUses;
			public static StatDef rxExplosiveChunkYield;
			public static StatDef rxExplosiveMiningYield;
			public static StatDef rxExplosiveWoodYield;
			public static StatDef rxPowerConsumption;
			public static StatDef rxSignalRange;
			public static StatDef rxSunExposure;
			public static StatDef rxPowerCapacity;
			public static StatDef rxVentingPower;
			public static StatDef rxSensorAngle;
			public static StatDef rxSensorRange;
			public static StatDef rxSensorSpeed;
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
		public static class Materials {
			public static readonly Material BatteryBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f));
			public static readonly Material BatteryBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f));
		}

		[StaticConstructorOnStartup]
		public static class Graphics {
			private const LinkFlags OverlayAtlasLinkFlags = LinkFlags.Custom3;

			public static readonly Graphic FlareOverlayNormal = GraphicDatabase.Get<Graphic_Single>("rxFlare", ShaderDatabase.TransparentPostLight);
			public static readonly Graphic FlareOverlayStrong = GraphicDatabase.Get<Graphic_Single>("rxFlareStrong", ShaderDatabase.TransparentPostLight);
			public static readonly Graphic FlareOverlayGreen = GraphicDatabase.Get<Graphic_Single>("rxFlareGreen", ShaderDatabase.TransparentPostLight);
			public static readonly Graphic DetWireOverlayAtlas = GraphicDatabase.Get<Graphic_Single>("rxDetWire/detWireOverlayAtlas", ShaderDatabase.MetaOverlay);
			public static readonly Graphic DetWireOverlayEndpoint = GraphicDatabase.Get<Graphic_Single>("rxDetWire/connectionPointOverlay", ShaderDatabase.MetaOverlay);
			public static readonly Graphic DetWireOverlayCrossing = GraphicDatabase.Get<Graphic_Single>("rxDetWire/crossingOverlay", ShaderDatabase.MetaOverlay);

			static Graphics() {
				DetWireOverlayAtlas = GraphicUtility.WrapLinked(DetWireOverlayAtlas, LinkDrawerType.Basic);
				DetWireOverlayAtlas.data = new GraphicData { linkFlags = OverlayAtlasLinkFlags };
				FlareOverlayNormal.drawSize = FlareOverlayStrong.drawSize = Vector2.one;
			}
		}

		[StaticConstructorOnStartup]
		public static class Textures {
			public static Texture2D rxUIDetonateManual;
			public static Texture2D rxUIDetonate;
			public static Texture2D rxUIDryOff;
			public static Texture2D rxUIArm;
			public static Texture2D rxUIAutoReplace;
			public static Texture2D rxUIChannelBasic1;
			public static Texture2D rxUIChannelBasic2;
			public static Texture2D rxUIChannelBasic3;
			public static Texture2D rxUIChannelKeypadAtlas;
			public static Texture2D rxUIDetonatorPortable;
			public static Texture2D rxUISelectWire;
			public static Texture2D rxUIUpgrade;
			public static Texture2D rxUISensorSettings;
			public static Texture2D rxGasVentArrow;
			public static Texture2D rxProximitySensorArc;

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