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
			public static SoundDef RemoteDetonatorLever;
			public static SoundDef RemoteFoamSpray;
			public static SoundDef RemoteFoamSolidify;
			public static SoundDef RemoteExplosiveArmed;
			public static SoundDef RemoteExplosiveBeep;
			public static SoundDef RemoteChannelChange;
			public static SoundDef RemoteUIDialClick;
			public static SoundDef RemoteEmpCharge;
			public static SoundDef RemoteMiningCavein;
		}

		[DefOf]
		public static class Damage {
			public static DamageDef FoamWallRekt;
		}

		[DefOf]
		public static class WorkType {
			public static WorkTypeDef Cleaning;
		}

		[DefOf]
		public static class Job {
			public static JobDef InstallChannelsComponent;
			public static JobDef DryDetonatorWire;
			public static JobDef SwitchRemoteExplosive;
			public static JobDef DetonateExplosives;
			public static JobDef SmoothFoamWall;
		}

		[DefOf]
		public static class Thing {
			public static ThingDef TableDetonator;
			public static ThingDef CollapsedRoofRocks;
			public static ThingDef Gas_Sleeping;
			public static ThingDef FoamWallSmooth;
			public static ThingDef FoamWallBricks;
			public static ThingDef PlantSparkweed;
		}

		[DefOf]
		public static class Research {
			public static ResearchProjectDef RemoteExplosivesChannels;
		}

		[DefOf]
		public static class KeyBinging {
			public static KeyBindingDef RemoteExplosivesNextChannel;
			public static KeyBindingDef RemoteTableDetonate;
			public static KeyBindingDef RemoteExplosiveArm;
			public static KeyBindingDef RemoteExplosiveAutoReplace;
			public static KeyBindingDef PortableDetonatorDetonate;
		}

		[DefOf]
		public static class Designation {
			public static DesignationDef RemoteExplosiveSwitch;
			public static DesignationDef DetonatorWireDryOff;
			public static DesignationDef FoamWallSmooth;
		}

		[DefOf]
		public static class Stat {
			public static StatDef PortableDetonatorRange;
			public static StatDef PortableDetonatorNumUses;
			public static StatDef ExplosiveChunkYield;
			public static StatDef ExplosiveMiningYield;
			public static StatDef ExplosiveWoodYield;
		}

		[DefOf]
		public static class Effecter {
			public static EffecterDef SparkweedIgnite;
			public static EffecterDef DetWireFailure;
		}

		[DefOf]
		public static class ThingCategory {
			public static ThingCategoryDef Explosives;
		}

		[StaticConstructorOnStartup]
		public static class Graphics {
			private const LinkFlags OverlayAtlasLinkFlags = LinkFlags.Custom3;

			public static readonly Graphic FlareOverlayNormal = GraphicDatabase.Get<Graphic_Single>("mine_flare", ShaderDatabase.TransparentPostLight);
			public static readonly Graphic FlareOverlayStrong = GraphicDatabase.Get<Graphic_Single>("mine_flare_strong", ShaderDatabase.TransparentPostLight);
			public static readonly Graphic DetWireOverlayAtlas = GraphicDatabase.Get<Graphic_Single>("DetWire/det_wire_overlay_atlas", ShaderDatabase.MetaOverlay);
			public static readonly Graphic DetWireOverlayEndpoint = GraphicDatabase.Get<Graphic_Single>("DetWire/connection_point_overlay", ShaderDatabase.MetaOverlay);

			static Graphics() {
				DetWireOverlayAtlas = GraphicUtility.WrapLinked(DetWireOverlayAtlas, LinkDrawerType.Basic);
				DetWireOverlayAtlas.data = new GraphicData { linkFlags = OverlayAtlasLinkFlags };
			}
		}

		[StaticConstructorOnStartup]
		public static class Textures {
			public static Texture2D UI_Trigger;
			public static Texture2D UIDetonate;
			public static Texture2D UIChannelComponent;
			public static Texture2D UIDryOff;
			public static Texture2D UIArm;
			public static Texture2D UIAutoReplace;
			public static Texture2D UIChannel0;
			public static Texture2D UIChannel1;
			public static Texture2D UIChannel2;
			public static Texture2D UIDetonatorPortable;
			public static Texture2D UISelectWire;

			public static readonly Texture2D WallSmoothMenuIcon = ContentFinder<Texture2D>.Get("Things/Building/Linked/WallSmooth_MenuIcon");

			static Textures() {
				foreach (var fieldInfo in typeof(Textures).GetFields(HugsLibUtility.AllBindingFlags)) {
					if (fieldInfo.IsInitOnly) continue;
					fieldInfo.SetValue(null, ContentFinder<Texture2D>.Get(fieldInfo.Name));
				}
			}
		}
	}
}