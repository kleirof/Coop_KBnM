using BepInEx;
using UnityEngine;
using HarmonyLib;
using BepInEx.Bootstrap;

namespace CoopKBnM
{
	[BepInDependency("etgmodding.etg.mtgapi")]
	[BepInPlugin(GUID, NAME, VERSION)]
	public class CoopKBnMModule : BaseUnityPlugin
	{
		public const string GUID = "kleirof.etg.coopkbnm";
		public const string NAME = "Coop KB&M";
		public const string VERSION = "1.0.4";
		public const string TEXT_COLOR = "#FFFFCC";

		public static bool isCoopViewLoaded = false;
		public static bool secondWindowActive = false;

		private static GameObject rawInputObject;

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

			Harmony harmony = new Harmony(GUID);
			harmony.PatchAll();
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
	}
}
