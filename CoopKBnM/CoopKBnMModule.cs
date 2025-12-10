using BepInEx;
using UnityEngine;
using HarmonyLib;
using BepInEx.Bootstrap;
using System;
using System.Reflection;

namespace CoopKBnM
{
	[BepInDependency("etgmodding.etg.mtgapi")]
	[BepInPlugin(GUID, NAME, VERSION)]
	public class CoopKBnMModule : BaseUnityPlugin
	{
		public const string GUID = "kleirof.etg.coopkbnm";
		public const string NAME = "Coop KB&M";
		public const string VERSION = "1.0.7";
		public const string TEXT_COLOR = "#FFFFCC";

		public static bool isCoopViewLoaded = false;
		public static bool secondWindowActive = false;

		private static GameObject rawInputObject;
        private Harmony harmony;

		public void Start()
		{
			OptionsManager.OnStart();

			ETGModMainBehaviour.WaitForGameManagerStart(GMStart);

			rawInputObject = new GameObject("Raw Input Object");
			DontDestroyOnLoad(rawInputObject);
			rawInputObject.AddComponent<RawInputHandler>();

			if (Chainloader.PluginInfos.TryGetValue("kleirof.etg.coopview", out PluginInfo pluginInfo))
			{
				isCoopViewLoaded = true;
			}

			harmony = new Harmony(GUID);
			harmony.PatchAll();
			DoOptionalPatches();
		}

		private void Update()
		{
			if (OptionsManager.masterControlsOptionsScrollablePanelObject == null && !OptionsManager.isInitializingOptions)
			{
				StartCoroutine(OptionsManager.InitializeOptions());
			}
        }

		public static void Log(string text, string color = "FFFFFF")
		{
			ETGModConsole.Log($"<color={color}>{text}</color>");
		}

		public void GMStart(GameManager g)
		{
			Log($"{NAME} v{VERSION} started successfully.", TEXT_COLOR);
		}

        private void DoOptionalPatches()
        {
            if (Chainloader.PluginInfos.ContainsKey("kleirof.etg.customcursor"))
            {
                Type cursorManagerType = AccessTools.TypeByName("CustomCursor.CursorManager");

                MethodInfo methodInfo1 = AccessTools.Method(cursorManagerType, "SetCustomCursorIsOn", null, null);
                MethodInfo fixMethodInfo1 = AccessTools.Method(typeof(CoopKBnMPatches.SetCustomCursorIsOnPatchClass), nameof(CoopKBnMPatches.SetCustomCursorIsOnPatchClass.SetCustomCursorIsOnPostfix), null, null);
                harmony.Patch(methodInfo1, null, new HarmonyMethod(fixMethodInfo1), null, null, null);

				MethodInfo methodInfo2 = AccessTools.Method(cursorManagerType, "SetPlayerOneCursor", null, null);
				MethodInfo fixMethodInfo2 = AccessTools.Method(typeof(CoopKBnMPatches.SetPlayerOneCursorPatchClass), nameof(CoopKBnMPatches.SetPlayerOneCursorPatchClass.SetPlayerOneCursorPostfix), null, null);
				harmony.Patch(methodInfo2, null, new HarmonyMethod(fixMethodInfo2), null, null, null);
				
				MethodInfo methodInfo3 = AccessTools.Method(cursorManagerType, "SetPlayerOneCursorModulation", null, null);
				MethodInfo fixMethodInfo3 = AccessTools.Method(typeof(CoopKBnMPatches.SetPlayerOneCursorModulationPatchClass), nameof(CoopKBnMPatches.SetPlayerOneCursorModulationPatchClass.SetPlayerOneCursorModulationPostfix), null, null);
				harmony.Patch(methodInfo3, null, new HarmonyMethod(fixMethodInfo3), null, null, null);
				
				MethodInfo methodInfo4 = AccessTools.Method(cursorManagerType, "SetPlayerOneCursorScale", null, null);
				MethodInfo fixMethodInfo4 = AccessTools.Method(typeof(CoopKBnMPatches.SetPlayerOneCursorScalePatchClass), nameof(CoopKBnMPatches.SetPlayerOneCursorScalePatchClass.SetPlayerOneCursorScalePostfix), null, null);
				harmony.Patch(methodInfo4, null, new HarmonyMethod(fixMethodInfo4), null, null, null);
				
				MethodInfo methodInfo5 = AccessTools.Method(cursorManagerType, "SetPlayerTwoCursor", null, null);
				MethodInfo fixMethodInfo5 = AccessTools.Method(typeof(CoopKBnMPatches.SetPlayerTwoCursorPatchClass), nameof(CoopKBnMPatches.SetPlayerTwoCursorPatchClass.SetPlayerTwoCursorPostfix), null, null);
				harmony.Patch(methodInfo5, null, new HarmonyMethod(fixMethodInfo5), null, null, null);
				
				MethodInfo methodInfo6 = AccessTools.Method(cursorManagerType, "SetPlayerTwoCursorModulation", null, null);
				MethodInfo fixMethodInfo6 = AccessTools.Method(typeof(CoopKBnMPatches.SetPlayerTwoCursorModulationPatchClass), nameof(CoopKBnMPatches.SetPlayerTwoCursorModulationPatchClass.SetPlayerTwoCursorModulationPostfix), null, null);
				harmony.Patch(methodInfo6, null, new HarmonyMethod(fixMethodInfo6), null, null, null);
				
				MethodInfo methodInfo7 = AccessTools.Method(cursorManagerType, "SetPlayerTwoCursorScale", null, null);
				MethodInfo fixMethodInfo7 = AccessTools.Method(typeof(CoopKBnMPatches.SetPlayerTwoCursorScalePatchClass), nameof(CoopKBnMPatches.SetPlayerTwoCursorScalePatchClass.SetPlayerTwoCursorScalePostfix), null, null);
				harmony.Patch(methodInfo7, null, new HarmonyMethod(fixMethodInfo7), null, null, null);
			}
        }
    }
}
