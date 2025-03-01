using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using InControl;

namespace CoopKBnM
{
    public class OptionsManager
    {
        internal static bool isInitializingOptions = false;
        internal static GameObject masterControlsOptionsScrollablePanelObject;
        private static GameObject optionsMenuPanelDaveObject;

        private static GameObject editKeyboardBindingsButtonPanelCopyObject;
        private static GameObject playerOneKeyboardPortArrowSelectorPanelObject;
        private static GameObject playerOneMousePortArrowSelectorPanelObject;
        private static GameObject shareOneKeyboardPanelObject;
        internal static GameObject playerOneMouseSensitivityPanelObject;
        internal static GameObject playerTwoMouseSensitivityPanelObject;
        private static GameObject systemMouseLabelPanelTipPanelObject;
        private static GameObject shareOneKeyBoardWarningPanelObject;

        public static bool restrictKeyboardInputPort = true;
        public static bool restrictMouseInputPort = true;

        public static bool isShareOneKeyboardMode = false;
        public static int currentPlayerOneKeyboardPort = 0;
        public static int currentPlayerOneMousePort = 0;

        public static float normalizedPlayerOneMouseSensitivity = 0.4f;
        public static float normalizedPlayerTwoMouseSensitivity = 0.4f;

        public static bool isPrimaryPlayerOnMainCamera;

        internal static FullOptionsMenuController fullOptionsMenuController;

        internal static string sharingBindingData = string.Empty;
        internal static string nonSharingBindingData = string.Empty;

        public enum BraveOptionsOptionType
        {
            PLAYER_ONE_KEYBOARD_PORT = 0x100,
            PLAYER_ONE_MOUSE_PORT = 0x101,
            SHARE_ONE_KEYBOARD_MODE = 0x102,
            PLAYER_ONE_MOUSE_SENSITIVITY = 0x103,
            PLAYER_TWO_MOUSE_SENSITIVITY = 0x104,
        }

        internal static void OnStart()
        {
            try
            {
                CoopKBnMPreferences.LoadPreferences();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load Coop View Preferences." + e);
            }
            RawInputHandler.playerOneMouseSensitivity = Mathf.Clamp(4 * normalizedPlayerOneMouseSensitivity + 0.4f, 0.4f, 4.4f);
            RawInputHandler.playerTwoMouseSensitivity = Mathf.Clamp(4 * normalizedPlayerTwoMouseSensitivity + 0.4f, 0.4f, 4.4f);
        }

        internal static IEnumerator InitializeOptions()
        {
            isInitializingOptions = true;

            while (true)
            {
                if (optionsMenuPanelDaveObject == null)
                    optionsMenuPanelDaveObject = GameObject.Find("OptionsMenuPanelDave");
                if (optionsMenuPanelDaveObject == null)
                {
                    yield return null;
                    continue;
                }
                FullOptionsMenuController fullOptionsMenuController = optionsMenuPanelDaveObject.GetComponent<FullOptionsMenuController>();
                if (fullOptionsMenuController == null)
                {
                    yield return null;
                    continue;
                }
                if (!fullOptionsMenuController.IsVisible)
                {
                    yield return null;
                    continue;
                }
                if (masterControlsOptionsScrollablePanelObject == null)
                    masterControlsOptionsScrollablePanelObject = GameObject.Find("MasterControlsOptionsScrollablePanel");
                if (masterControlsOptionsScrollablePanelObject != null)
                    break;
                yield return null;
            }
            while (editKeyboardBindingsButtonPanelCopyObject == null)
            {
                GameObject editKeyboardBindingsButtonPanelObject = GameObject.Find("EditKeyboardBindingsButtonPanel");
                if (editKeyboardBindingsButtonPanelObject != null)
                {
                    GameObject origEditKeyboardBindingsButtonObject = editKeyboardBindingsButtonPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/EditKeyboardBindingsButton").gameObject;
                    editKeyboardBindingsButtonPanelCopyObject = UnityEngine.Object.Instantiate(editKeyboardBindingsButtonPanelObject, editKeyboardBindingsButtonPanelObject.transform.parent);
                    editKeyboardBindingsButtonPanelCopyObject.name = "EditKeyboardBindingsButtonPanelCopy";
                    GameObject editKeyboardBindingsButtonObject = editKeyboardBindingsButtonPanelCopyObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/EditKeyboardBindingsButton").gameObject;

                    editKeyboardBindingsButtonObject.GetComponent<dfButton>().Text = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? "2P Coop KB&M 编辑按键" : "2P Coop KB&M Edit Keyboard Bindings";
                    editKeyboardBindingsButtonObject.GetComponent<dfButton>().HoverTextColor = new Color32(255, 163, 52, 255);
                    origEditKeyboardBindingsButtonObject.GetComponent<dfButton>().Text = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? "1P Coop KB&M 编辑按键" : "1P Coop KB&M Edit Keyboard Bindings";
                    origEditKeyboardBindingsButtonObject.GetComponent<dfButton>().HoverTextColor = new Color32(255, 163, 52, 255);
                    editKeyboardBindingsButtonPanelObject.GetComponent<dfPanel>().ZOrder = 4;
                    editKeyboardBindingsButtonPanelCopyObject.GetComponent<dfPanel>().ZOrder = 10;
                }
                if (editKeyboardBindingsButtonPanelCopyObject == null)
                    yield return null;
            }

            while (playerOneKeyboardPortArrowSelectorPanelObject == null)
            {
                GameObject controllerTypeArrowSelectorPanelObject = GameObject.Find("ControllerTypeArrowSelectorPanel");
                if (controllerTypeArrowSelectorPanelObject != null)
                {
                    playerOneKeyboardPortArrowSelectorPanelObject = UnityEngine.Object.Instantiate(controllerTypeArrowSelectorPanelObject, controllerTypeArrowSelectorPanelObject.transform.parent);
                    playerOneKeyboardPortArrowSelectorPanelObject.name = "PlayerOneKeyboardPortArrowSelectorPanel";
                    GameObject optionsArrowSelectorLabelObject = playerOneKeyboardPortArrowSelectorPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/OptionsArrowSelectorLabel").gameObject;

                    optionsArrowSelectorLabelObject.GetComponent<dfLabel>().Text = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? "1P Coop KB&M 键盘接口" : "1P Coop KB&M Keyboard Port";
                    playerOneKeyboardPortArrowSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().optionType = (BraveOptionsMenuItem.BraveOptionsOptionType)BraveOptionsOptionType.PLAYER_ONE_KEYBOARD_PORT;
                    playerOneKeyboardPortArrowSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().DetermineAvailableOptions();
                    playerOneKeyboardPortArrowSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().m_selectedIndex = currentPlayerOneKeyboardPort;
                    playerOneKeyboardPortArrowSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().HandleValueChanged();
                    playerOneKeyboardPortArrowSelectorPanelObject.GetComponent<dfPanel>().ZOrder = 4;
                }
                if (playerOneKeyboardPortArrowSelectorPanelObject == null)
                    yield return null;
            }

            while (shareOneKeyboardPanelObject == null)
            {
                GameObject allowUnknownControllersObject = masterControlsOptionsScrollablePanelObject.transform.Find("AllowUnknownControllers").gameObject;

                if (allowUnknownControllersObject != null)
                {
                    shareOneKeyboardPanelObject = UnityEngine.Object.Instantiate(allowUnknownControllersObject, allowUnknownControllersObject.transform.parent);
                    shareOneKeyboardPanelObject.name = "ShareOneKeyboard";
                    GameObject checkboxLabelObject = shareOneKeyboardPanelObject.transform.Find("Panel/CheckboxLabel").gameObject;

                    shareOneKeyboardPanelObject.GetComponent<BraveOptionsMenuItem>().m_selectedIndex = isShareOneKeyboardMode ? 1 : 0;
                    shareOneKeyboardPanelObject.GetComponent<BraveOptionsMenuItem>().optionType = (BraveOptionsMenuItem.BraveOptionsOptionType)BraveOptionsOptionType.SHARE_ONE_KEYBOARD_MODE;
                    shareOneKeyboardPanelObject.GetComponent<BraveOptionsMenuItem>().HandleValueChanged();

                    dfLabel label = checkboxLabelObject.GetComponent<dfLabel>();
                    Vector2 origSize = label.obtainRenderer().MeasureString(label.Text);
                    label.Text = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? "Coop KB&M 共享一个键盘" : "Coop KB&M Share One Keyboard";
                    Vector2 size = label.obtainRenderer().MeasureString(label.Text);
                    GameObject panelObject = shareOneKeyboardPanelObject.transform.Find("Panel").gameObject;
                    dfPanel panel = panelObject.GetComponent<dfPanel>();
                    Vector2 panelSize = panel.Size;
                    panel.SuspendLayout();
                    panel.Size = new Vector2(panelSize.x + size.x - origSize.x, panelSize.y);
                    panel.ResumeLayout();
                    shareOneKeyboardPanelObject.GetComponent<dfPanel>().ZOrder = 2;
                }
                if (shareOneKeyboardPanelObject == null)
                    yield return null;
            }

            while (playerOneMousePortArrowSelectorPanelObject == null)
            {
                GameObject controllerTypeArrowSelectorPanelObject = GameObject.Find("ControllerTypeArrowSelectorPanel");
                if (controllerTypeArrowSelectorPanelObject != null)
                {
                    playerOneMousePortArrowSelectorPanelObject = UnityEngine.Object.Instantiate(controllerTypeArrowSelectorPanelObject, controllerTypeArrowSelectorPanelObject.transform.parent);
                    playerOneMousePortArrowSelectorPanelObject.name = "PlayerOneMousePortArrowSelectorPanel";
                    GameObject optionsArrowSelectorLabelObject = playerOneMousePortArrowSelectorPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/OptionsArrowSelectorLabel").gameObject;

                    optionsArrowSelectorLabelObject.GetComponent<dfLabel>().Text = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? "1P Coop KB&M 鼠标接口" : "1P Coop KB&M Mouse Port";
                    playerOneMousePortArrowSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().optionType = (BraveOptionsMenuItem.BraveOptionsOptionType)BraveOptionsOptionType.PLAYER_ONE_MOUSE_PORT;
                    playerOneMousePortArrowSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().DetermineAvailableOptions();
                    playerOneMousePortArrowSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().m_selectedIndex = currentPlayerOneMousePort;
                    playerOneMousePortArrowSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().HandleValueChanged();
                    playerOneMousePortArrowSelectorPanelObject.GetComponent<dfPanel>().ZOrder = 5;
                }
                if (playerOneMousePortArrowSelectorPanelObject == null)
                    yield return null;
            }

            while (playerOneMouseSensitivityPanelObject == null)
            {
                GameObject controllerTypeArrowSelectorPanelObject = GameObject.Find("ControllerTypeArrowSelectorPanel");
                GameObject screenShakeOptionPanelObject = GameObject.Find("ScreenShakeOptionPanel");
                if (controllerTypeArrowSelectorPanelObject != null && screenShakeOptionPanelObject != null)
                {
                    playerOneMouseSensitivityPanelObject = UnityEngine.Object.Instantiate(screenShakeOptionPanelObject, controllerTypeArrowSelectorPanelObject.transform.parent);
                    playerOneMouseSensitivityPanelObject.name = "PlayerOneMouseSensitivityPanel";
                    GameObject musicVolumeLabelObject = playerOneMouseSensitivityPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/MusicVolumeLabel").gameObject;

                    musicVolumeLabelObject.GetComponent<dfLabel>().Text = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? "1P Coop KB&M 鼠标灵敏度" : "1P Coop KB&M Mouse Sensitivity";
                    playerOneMouseSensitivityPanelObject.GetComponent<BraveOptionsMenuItem>().optionType = (BraveOptionsMenuItem.BraveOptionsOptionType)BraveOptionsOptionType.PLAYER_ONE_MOUSE_SENSITIVITY;
                    playerOneMouseSensitivityPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/MusicVolumeProgressBar").GetComponent<dfProgressBar>().Value = normalizedPlayerOneMouseSensitivity;
                    playerOneMouseSensitivityPanelObject.GetComponent<BraveOptionsMenuItem>().m_actualFillbarValue = normalizedPlayerOneMouseSensitivity;
                    playerOneMouseSensitivityPanelObject.GetComponent<BraveOptionsMenuItem>().HandleValueChanged();
                    playerOneMouseSensitivityPanelObject.GetComponent<dfPanel>().ZOrder = 7;
                }
                if (playerOneMouseSensitivityPanelObject == null)
                    yield return null;
            }

            while (playerTwoMouseSensitivityPanelObject == null)
            {
                GameObject controllerTypeArrowSelectorPanelObject = GameObject.Find("ControllerTypeArrowSelectorPanel");
                GameObject screenShakeOptionPanelObject = GameObject.Find("ScreenShakeOptionPanel");
                if (controllerTypeArrowSelectorPanelObject != null && screenShakeOptionPanelObject != null)
                {
                    playerTwoMouseSensitivityPanelObject = UnityEngine.Object.Instantiate(screenShakeOptionPanelObject, controllerTypeArrowSelectorPanelObject.transform.parent);
                    playerTwoMouseSensitivityPanelObject.name = "PlayerTwoMouseSensitivityPanel";
                    GameObject musicVolumeLabelObject = playerTwoMouseSensitivityPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/MusicVolumeLabel").gameObject;

                    musicVolumeLabelObject.GetComponent<dfLabel>().Text = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? "2P Coop KB&M 鼠标灵敏度" : "2P Coop KB&M Mouse Sensitivity";
                    playerTwoMouseSensitivityPanelObject.GetComponent<BraveOptionsMenuItem>().optionType = (BraveOptionsMenuItem.BraveOptionsOptionType)BraveOptionsOptionType.PLAYER_TWO_MOUSE_SENSITIVITY;
                    playerTwoMouseSensitivityPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/MusicVolumeProgressBar").GetComponent<dfProgressBar>().Value = normalizedPlayerTwoMouseSensitivity;
                    playerTwoMouseSensitivityPanelObject.GetComponent<BraveOptionsMenuItem>().m_actualFillbarValue = normalizedPlayerTwoMouseSensitivity;
                    playerTwoMouseSensitivityPanelObject.GetComponent<BraveOptionsMenuItem>().HandleValueChanged();
                    playerTwoMouseSensitivityPanelObject.GetComponent<dfPanel>().ZOrder = 15;
                }
                if (playerTwoMouseSensitivityPanelObject == null)
                    yield return null;
            }

            while (systemMouseLabelPanelTipPanelObject == null)
            {
                GameObject playerOneLabelPanelObject = GameObject.Find("PlayerOneLabelPanel");
                if (playerOneLabelPanelObject != null)
                {
                    systemMouseLabelPanelTipPanelObject = UnityEngine.Object.Instantiate(playerOneLabelPanelObject, playerOneLabelPanelObject.transform.parent);
                    systemMouseLabelPanelTipPanelObject.name = "SystemMouseLabelPanelTipPanel";
                    GameObject labelObject = systemMouseLabelPanelTipPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/Label").gameObject;

                    labelObject.GetComponent<dfLabel>().Text = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? $"提示：使用快捷键 {RawInputHandler.shortcutKeyString} \n来暂时启用系统光标" : $"Tip: Use shortcut key {RawInputHandler.shortcutKeyString} \nto temporarily enable the system cursor";
                    labelObject.GetComponent<dfLabel>().Color = new Color32(0, 255, 0, 255);
                    GameObject panelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasyObject = systemMouseLabelPanelTipPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy").gameObject;
                    Vector3 position = panelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasyObject.transform.localPosition;
                    panelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasyObject.transform.localPosition = new Vector3(position.x, position.y + 0.12f * Camera.main.pixelWidth / 1920, position.z);
                    systemMouseLabelPanelTipPanelObject.GetComponent<dfPanel>().ZOrder = 0;
                }
                if (systemMouseLabelPanelTipPanelObject == null)
                    yield return null;
            }

            while (shareOneKeyBoardWarningPanelObject == null)
            {
                GameObject playerOneLabelPanelObject = GameObject.Find("PlayerOneLabelPanel");
                if (playerOneLabelPanelObject != null)
                {
                    shareOneKeyBoardWarningPanelObject = UnityEngine.Object.Instantiate(playerOneLabelPanelObject, playerOneLabelPanelObject.transform.parent);
                    shareOneKeyBoardWarningPanelObject.name = "ShareOneKeyBoardWarningPanel";
                    GameObject labelObject = shareOneKeyBoardWarningPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/Label").gameObject;

                    labelObject.GetComponent<dfLabel>().Text = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? "警告：开启“共享一个键盘”会让2P中与1P相同\n的按键绑定删除" : "Warning: Enabling 'Share One Keyboard' will cause \nthe same key binding as 1P to be deleted in 2P";
                    labelObject.GetComponent<dfLabel>().Color = new Color32(255, 0, 0, 255);
                    GameObject panelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasyObject = shareOneKeyBoardWarningPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy").gameObject;
                    Vector3 position = panelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasyObject.transform.localPosition;
                    panelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasyObject.transform.localPosition = new Vector3(position.x, position.y + 0.12f * Camera.main.pixelWidth / 1920, position.z);
                    shareOneKeyBoardWarningPanelObject.GetComponent<dfPanel>().ZOrder = 3;
                }
                if (shareOneKeyBoardWarningPanelObject == null)
                    yield return null;
            }

            dfList<dfControl> controls = GameObject.Find("MasterControlsOptionsScrollablePanel").GetComponent<dfScrollPanel>().controls;

            for (int i = 0; i < controls.Count - 1; ++i)
            {
                if (!controls[i].gameObject.GetComponent<dfPanel>().CanFocus)
                    continue;
                int j = i + 1;
                for (; j < controls.Count - 1; ++j)
                {
                    if (controls[j].gameObject.GetComponent<dfPanel>().CanFocus)
                        break;
                }
                if (j < controls.Count - 1)
                    controls[i].gameObject.GetComponent<BraveOptionsMenuItem>().down = controls[j];
            }
            for (int i = controls.Count - 1; i > 0; --i)
            {
                if (!controls[i].gameObject.GetComponent<dfPanel>().CanFocus)
                    continue;
                int j = i - 1;
                for (; j > 0; --j)
                {
                    if (controls[j].gameObject.GetComponent<dfPanel>().CanFocus)
                        break;
                }
                if (j > 0)
                    controls[i].gameObject.GetComponent<BraveOptionsMenuItem>().up = controls[j];
            }

            int indexCanSelect = 0;
            for (; indexCanSelect < controls.Count; ++indexCanSelect)
            {
                if (controls[indexCanSelect].CanFocus)
                    break;
            }

            if (controls.Count > 0 && indexCanSelect < controls.Count && optionsMenuPanelDaveObject.GetComponent<FullOptionsMenuController>().TabControls.IsVisible)
            {
                GameObject.Find("ConfirmButton").GetComponent<UIKeyControls>().down = controls[indexCanSelect];
                GameObject.Find("CancelButton").GetComponent<UIKeyControls>().down = controls[indexCanSelect];
                GameObject.Find("ResetDefaultsButton").GetComponent<UIKeyControls>().down = controls[indexCanSelect];
                controls[indexCanSelect].gameObject.GetComponent<BraveOptionsMenuItem>().up = GameObject.Find("ConfirmButton").GetComponent<dfButton>();
                controls[indexCanSelect].Focus(true);
            }

            BraveInput.GetInstanceForPlayer(1);

            yield return null;
            if (shareOneKeyboardPanelObject != null)
                shareOneKeyboardPanelObject.GetComponent<BraveOptionsMenuItem>().HandleCheckboxValueChanged();

            isInitializingOptions = false;
        }

        internal static void SaveBindingInfoToCachedOptions()
        {
            if (fullOptionsMenuController == null || fullOptionsMenuController.cloneOptions == null || GameManager.Instance.PrimaryPlayer == null)
                return;

            Debug.Log("Saving Binding Info To Cached Options");
            for (int i = 0; i < BraveInput.m_instances.Count; i++)
            {
                if (BraveInput.m_instances[i].m_playerID == GameManager.Instance.PrimaryPlayer.PlayerIDX)
                    fullOptionsMenuController.cloneOptions.playerOneBindingDataV2 = BraveInput.m_instances[i].ActiveActions.Save();
                else
                    fullOptionsMenuController.cloneOptions.playerTwoBindingDataV2 = BraveInput.m_instances[i].ActiveActions.Save();
            }
        }

        public static void RemoveDuplicateBindings()
        {
            if (isShareOneKeyboardMode && BraveInput.m_instances.Count > 1)
            {
                foreach (PlayerAction action in BraveInput.GetInstanceForPlayer(0).ActiveActions.Actions)
                {
                    foreach (BindingSource binding in action.Bindings)
                    {
                        if (binding.DeviceClass == InputDeviceClass.Keyboard)
                        {
                            foreach (PlayerAction playerTwoAction in BraveInput.GetInstanceForPlayer(1).ActiveActions.Actions)
                            {
                                playerTwoAction.HardRemoveBinding(binding);
                            }
                        }
                    }
                }
            }
        }

        internal static IEnumerator ReassignKeyboardAndMouseCrt()
        {
            yield return null;
            yield return null;
            yield return null;
            ReassignKeyboardAndMouse();
        }

        internal static IEnumerator DelayedRemoveDuplicateBindingsCrt()
        {
            yield return null;
            yield return null;
            yield return null;
            if (isShareOneKeyboardMode)
                RemoveDuplicateBindings();
        }

        internal static void ReassignKeyboardAndMouse()
        {
            Dictionary<int, int> map = GameManager.Options.PlayerIDtoDeviceIndexMap;
            BraveInput keyboardBraveInput;
            BraveInput mouseBraveInput;

            if (map.Count <= 1 || BraveInput.m_instances.Count <= 1)
            {
                restrictKeyboardInputPort = false;
                restrictMouseInputPort = false;
                return;
            }

            if (InputManager.Devices.Count > 0)
            {
                if (map[0] != InputManager.Devices.Count || map[1] != InputManager.Devices.Count)
                {
                    restrictKeyboardInputPort = false;
                    restrictMouseInputPort = false;
                    return;
                }
            }

            restrictKeyboardInputPort = true;
            restrictMouseInputPort = true;

            if (isShareOneKeyboardMode)
                restrictKeyboardInputPort = false;

            if (currentPlayerOneKeyboardPort == 0)
                keyboardBraveInput = BraveInput.m_instances[0];
            else
                keyboardBraveInput = BraveInput.m_instances[1];

            if (currentPlayerOneMousePort == 0)
                mouseBraveInput = BraveInput.m_instances[0];
            else
                mouseBraveInput = BraveInput.m_instances[1];

            GungeonActions keyboardActions = keyboardBraveInput.m_activeGungeonActions;
            if (keyboardActions != RawInputHandler.firstKeyboardActionSet)
                RawInputHandler.firstKeyboardActionSet = keyboardActions;

            GungeonActions mouseActions = mouseBraveInput.m_activeGungeonActions;
            if (mouseActions != RawInputHandler.firstMouseActionSet)
                RawInputHandler.firstMouseActionSet = mouseActions;
        }

        internal static void SaveBindingData(bool saveSharingBindingData)
        {
            if (BraveInput.m_instances.Count > 1)
            {
                if (saveSharingBindingData)
                    sharingBindingData = BraveInput.m_instances[1].ActiveActions.Save();
                else
                    nonSharingBindingData = BraveInput.m_instances[1].ActiveActions.Save();
            }
        }

        internal static void LoadBindingData(bool saveSharingBindingData)
        {
            if (BraveInput.m_instances.Count <= 1)
                return;

            string bindingData = saveSharingBindingData ? sharingBindingData : nonSharingBindingData;
            if (bindingData.Length <= 0)
                return;

            GungeonActions actions = new GungeonActions();
            GungeonActions playerTwoActions = BraveInput.m_instances[1].ActiveActions;
            actions.Load(bindingData, true);

            foreach (PlayerAction action in playerTwoActions.Actions)
            {
                List<BindingSource> bindingsToRemove = new List<BindingSource>();
                PlayerAction previousAction = actions.GetPlayerActionByName(action.Name);

                foreach (BindingSource keyBinding in action.Bindings)
                {
                    if (keyBinding.DeviceClass == InputDeviceClass.Keyboard && !previousAction.HasBinding(keyBinding))
                        bindingsToRemove.Add(keyBinding);
                }

                foreach (BindingSource keyBinding1 in bindingsToRemove)
                {
                    action.HardRemoveBinding(keyBinding1);
                }

                foreach (BindingSource keyBinding2 in previousAction.Bindings)
                {
                    if (keyBinding2.DeviceClass == InputDeviceClass.Keyboard && !action.HasBinding(keyBinding2))
                    {
                        keyBinding2.BoundTo = null;
                        action.AddBinding(keyBinding2);
                    }
                }
            }
        }
    }
}
