using System.IO;
using UnityEngine;

namespace CoopKBnM
{
	public class CoopKBnMPreferences
	{
		public class PreferencesData : ScriptableObject
		{
			public PreferencesData()
			{
				isShareOneKeyboardMode = OptionsManager.isShareOneKeyboardMode;
				currentPlayerOneKeyboardPort = OptionsManager.currentPlayerOneKeyboardPort;
				currentPlayerOneMousePort = OptionsManager.currentPlayerOneMousePort;
				normalizedPlayerOneMouseSensitivity = OptionsManager.normalizedPlayerOneMouseSensitivity;
				normalizedPlayerTwoMouseSensitivity = OptionsManager.normalizedPlayerTwoMouseSensitivity;
				sharingBindingData = OptionsManager.sharingBindingData;
				nonSharingBindingData = OptionsManager.nonSharingBindingData;
			}

			public bool isShareOneKeyboardMode;
			public int currentPlayerOneKeyboardPort;
			public int currentPlayerOneMousePort;
			public float normalizedPlayerOneMouseSensitivity;
			public float normalizedPlayerTwoMouseSensitivity;
			public string sharingBindingData;
			public string nonSharingBindingData;
		}

		private const string fileName = "CoopKBnM_Preferences.txt";

		public static void LoadPreferences()
		{
			if (File.Exists(Path.Combine(ETGMod.ResourcesDirectory, fileName)))
			{
				string json = File.ReadAllText(Path.Combine(ETGMod.ResourcesDirectory, fileName));
				PreferencesData settingsData = ScriptableObject.CreateInstance<PreferencesData>();
				JsonUtility.FromJsonOverwrite(json, settingsData);

				OptionsManager.isShareOneKeyboardMode = settingsData.isShareOneKeyboardMode;
				OptionsManager.currentPlayerOneKeyboardPort = settingsData.currentPlayerOneKeyboardPort;
				OptionsManager.currentPlayerOneMousePort = settingsData.currentPlayerOneMousePort;
				OptionsManager.normalizedPlayerOneMouseSensitivity = settingsData.normalizedPlayerOneMouseSensitivity;
				OptionsManager.normalizedPlayerTwoMouseSensitivity = settingsData.normalizedPlayerTwoMouseSensitivity;
				OptionsManager.sharingBindingData = settingsData.sharingBindingData;
				OptionsManager.nonSharingBindingData = settingsData.nonSharingBindingData;
				return;
			}
			SavePreferences();
		}

		public static void SavePreferences()
		{
			string text = JsonUtility.ToJson(ScriptableObject.CreateInstance<PreferencesData>(), true);
			if (File.Exists(Path.Combine(ETGMod.ResourcesDirectory, fileName)))
			{
				File.Delete(Path.Combine(ETGMod.ResourcesDirectory, fileName));
			}
			using (StreamWriter streamWriter = new StreamWriter(Path.Combine(ETGMod.ResourcesDirectory, fileName), true))
			{
				streamWriter.WriteLine(text);
			}
		}
	}
}
