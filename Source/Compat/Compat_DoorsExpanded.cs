using System;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Allows Doors Expanded doors to act as blockers for gas clouds.
	/// Jail bar door and curtains do not block, even in closed state.
	/// </summary>
	internal static class Compat_DoorsExpanded {
		public static void OnDefsLoaded() {
			const string doorsModName = "Doors Expanded mod",
				doorExpandedTypeName = "DoorsExpanded.Building_DoorExpanded",
				openIntFieldName = "openInt",
				doorRemoteTypeName = "DoorsExpanded.Building_DoorRemote",
				curtainDoorDefName = "HeronCurtainTribal",
				jailDoorDefName = "PH_DoorJail";
			var doorExpandedType = GenTypes.GetTypeInAnyAssemblyNew(doorExpandedTypeName, null);
			if (doorExpandedType != null) {
				var logger = RemoteTechController.Instance.Logger;
				try {
					if (!typeof(Building).IsAssignableFrom(doorExpandedType)) {
						throw new Exception($"Expected {doorExpandedTypeName} to extend {typeof(Building).Name}");
					}
					var openIntField = doorExpandedType.GetField(openIntFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
					if (openIntField == null || openIntField.FieldType != typeof(bool)) {
						throw new Exception($"Expected {doorExpandedTypeName} to have field {openIntFieldName}");
					}
					var doorRemoteType = GenTypes.GetTypeInAnyAssemblyNew(doorRemoteTypeName, null);
					if (doorRemoteType == null || !doorExpandedType.IsAssignableFrom(doorRemoteType)) {
						throw new Exception($"Expected type {doorRemoteTypeName}, extending {doorExpandedTypeName}");
					}

					void LogDefWarning(string defName) => logger.Warning($"Expected to find def {defName} in {doorsModName}");
					var curtainDoorDef = DefDatabase<ThingDef>.GetNamedSilentFail(curtainDoorDefName);
					if (curtainDoorDef == null) {
						LogDefWarning(curtainDoorDefName);
					}
					var jailDoorDef = DefDatabase<ThingDef>.GetNamedSilentFail(jailDoorDefName);
					if (jailDoorDef == null) {
						LogDefWarning(jailDoorDefName);
					}

					var isOpenGetter = CreateGetterForField(openIntField);

					GasCloud.TraversibleBuildings.Add(doorExpandedType, (building, _) => {
						var def = building.def;
						return def == curtainDoorDef || def == jailDoorDef || isOpenGetter(building);
					});
					GasCloud.TraversibleBuildings.Add(doorRemoteType, 
						(building, _) => isOpenGetter(building)
					);

					logger.Message($"Applied compatibility layer for {doorsModName}");
				} catch (Exception e) {
					logger.Error($"Failed to apply compatibility layer for {doorsModName}: {e}");
				}

			}
		}

		private static Func<Building, bool> CreateGetterForField(FieldInfo field) {
			// create a dynamic method to avoid reflection costs
			var parentType = field.ReflectedType;
			if(parentType == null) throw new Exception("Unexpected reflection error");
			var methodName = parentType.FullName + ".get_" + field.Name;
			var setterMethod = new DynamicMethod(methodName, typeof(bool), new[] {typeof(Building)}, true);
			var gen = setterMethod.GetILGenerator();
			gen.Emit(OpCodes.Ldarg_0); // push Building
			gen.Emit(OpCodes.Castclass, parentType); // cast to Building_DoorExpanded
			gen.Emit(OpCodes.Ldfld, field); // get openInt field value
			gen.Emit(OpCodes.Ret); // return bool
			return (Func<Building, bool>)setterMethod.CreateDelegate(typeof(Func<Building, bool>));
		}
	}
}