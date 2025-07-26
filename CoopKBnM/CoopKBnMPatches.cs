using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using InControl;

namespace CoopKBnM
{
    public static class CoopKBnMPatches
    {
        public static bool customCursorIsOn;
        public static Texture2D playerOneCursor;
        public static Color playerOneCursorModulation;
        public static float playerOneCursorScale;
        public static Texture2D playerTwoCursor;
        public static Color playerTwoCursorModulation;
        public static float playerTwoCursorScale;

        public static void EmitCall<T>(this ILCursor iLCursor, string methodName, Type[] parameters = null, Type[] generics = null)
        {
            MethodInfo methodInfo = AccessTools.Method(typeof(T), methodName, parameters, generics);
            iLCursor.Emit(OpCodes.Call, methodInfo);
        }

        public static T GetFieldInEnumerator<T>(object instance, string fieldNamePattern)
        {
            return (T)instance.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => f.Name.Contains("$" + fieldNamePattern) || f.Name.Contains("<" + fieldNamePattern + ">"))
                .GetValue(instance);
        }

        public static bool TheNthTime(this Func<bool> predict, int n = 1)
        {
            for (int i = 0; i < n; ++i)
            {
                if (!predict())
                    return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(FoyerCharacterSelectFlag), nameof(FoyerCharacterSelectFlag.Update))]
        public class FoyerCharacterSelectFlagUpdatePatchClass
        {
            [HarmonyILManipulator]
            public static void FoyerCharacterSelectFlagUpdatePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.Before,
                    x => x.Match(OpCodes.Brfalse)
                    ))).TheNthTime(2))
                {
                    crs.EmitCall<FoyerCharacterSelectFlagUpdatePatchClass>(nameof(FoyerCharacterSelectFlagUpdatePatchClass.FoyerCharacterSelectFlagUpdatePatchCall));
                }
            }

            private static bool FoyerCharacterSelectFlagUpdatePatchCall(bool orig)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(BraveInput), nameof(BraveInput.AssignActionsDevice))]
        public class AssignActionsDevicePatchClass
        {
            [HarmonyILManipulator]
            public static void AssignActionsDevicePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<BraveInput>("m_playerID")
                    ))).TheNthTime(2))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<AssignActionsDevicePatchClass>(nameof(AssignActionsDevicePatchClass.AssignActionsDevicePatchCall));
                }
            }

            private static bool AssignActionsDevicePatchCall(bool orig, BraveInput self)
            {
                return orig && self.m_activeGungeonActions.Device != null;
            }
        }

        [HarmonyPatch(typeof(PlayerAction), nameof(PlayerAction.UpdateBindings))]
        public class UpdateBindingsPatchClass
        {
            [HarmonyILManipulator]
            public static void UpdateBindingsPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.Before,
                    x => x.Match(OpCodes.Brfalse)
                    ))).TheNthTime(2))
                {
                    crs.Emit(OpCodes.Ldloca_S, (byte)11);
                    crs.Emit(OpCodes.Ldloc_S, (byte)8);
                    crs.Emit(OpCodes.Ldarg_1);
                    crs.Emit(OpCodes.Ldarg_2);
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<UpdateBindingsPatchClass>(nameof(UpdateBindingsPatchClass.UpdateBindingsPatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                x => x.MatchCallvirt<BindingSource>("GetValue")
                ))
                {
                    crs.Emit(OpCodes.Ldloc_S, (byte)8);
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<UpdateBindingsPatchClass>(nameof(UpdateBindingsPatchClass.UpdateBindingsPatchCall_2));
                }
            }

            private static bool UpdateBindingsPatchCall_1(bool orig, ref float value, BindingSource bindingSource, ulong updateTick, float deltaTime, PlayerAction self)
            {
                if (orig == true)
                {
                    if (GameManager.HasInstance && GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER
                        && !BraveInput.GetInstanceForPlayer(0).IsKeyboardAndMouse(false)
                        && BraveInput.GetInstanceForPlayer(1).IsKeyboardAndMouse(false)
                        && bindingSource is KeyBindingSource
                        && OptionsManager.isShareOneKeyboardMode
                        && (self.Name == "Cancel"
                        || self.Name == "Pause"
                        || self.Name == "Menu Select"
                        || self.Name == "Select Left"
                        || self.Name == "Select Right"
                        || self.Name == "Select Up"
                        || self.Name == "Select Down"))
                    {
                        value = bindingSource.GetValue(self.Device);
                        self.UpdateWithValue(value, updateTick, deltaTime);
                    }
                }
                return orig;
            }

            private static float UpdateBindingsPatchCall_2(float orig, BindingSource bindingSource, PlayerAction self)
            {
                if (bindingSource is KeyBindingSource && OptionsManager.restrictKeyboardInputPort)
                    return RawInputHandler.GetValue(bindingSource as KeyBindingSource, self.Device, RawInputHandler.IsFirstKeyboard(self));
                if (bindingSource is MouseBindingSource)
                {
                    if (OptionsManager.restrictMouseInputPort)
                        return RawInputHandler.GetValue(bindingSource as MouseBindingSource, self.Device, RawInputHandler.IsFirstMouse(self));
                    return RawInputHandler.GetValue(bindingSource as MouseBindingSource, self.Device);
                }
                return orig;
            }
        }

        [HarmonyPatch(typeof(BraveOptionsMenuItem), nameof(BraveOptionsMenuItem.DetermineAvailableOptions))]
        public class DetermineAvailableOptionsPatchClass
        {
            [HarmonyPrefix]
            public static bool DetermineAvailableOptionsPrefix(BraveOptionsMenuItem __instance)
            {
                switch (__instance.optionType)
                {
                    case (BraveOptionsMenuItem.BraveOptionsOptionType)OptionsManager.BraveOptionsOptionType.PLAYER_ONE_KEYBOARD_PORT:
                        List<string> keyboardList = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? new List<string> { "键盘 1", "键盘 2" } : new List<string> { "Keyboard 1", "Keyboard 2" };
                        __instance.labelOptions = keyboardList.ToArray();
                        break;
                    case (BraveOptionsMenuItem.BraveOptionsOptionType)OptionsManager.BraveOptionsOptionType.PLAYER_ONE_MOUSE_PORT:
                        List<string> mouseList = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? new List<string> { "鼠标 1", "鼠标 2" } : new List<string> { "Mouse 1", "Mouse 2" };
                        __instance.labelOptions = mouseList.ToArray();
                        break;
                    default:
                        break;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BraveOptionsMenuItem), nameof(BraveOptionsMenuItem.HandleLeftRightArrowValueChanged))]
        public class HandleLeftRightArrowValueChangedPatchClass
        {
            [HarmonyPostfix]
            public static void HandleLeftRightArrowValueChangedPostfix(BraveOptionsMenuItem __instance)
            {
                switch (__instance.optionType)
                {
                    case (BraveOptionsMenuItem.BraveOptionsOptionType)OptionsManager.BraveOptionsOptionType.PLAYER_ONE_KEYBOARD_PORT:
                        OptionsManager.currentPlayerOneKeyboardPort = __instance.m_selectedIndex;
                        GameManager.Instance.StartCoroutine(OptionsManager.ReassignKeyboardAndMouseCrt());
                        Debug.Log($"Coop KBnM: Current player one keyboard port set to {__instance.m_selectedIndex}");
                        break;
                    case (BraveOptionsMenuItem.BraveOptionsOptionType)OptionsManager.BraveOptionsOptionType.PLAYER_ONE_MOUSE_PORT:
                        OptionsManager.currentPlayerOneMousePort = __instance.m_selectedIndex;
                        GameManager.Instance.StartCoroutine(OptionsManager.ReassignKeyboardAndMouseCrt());
                        Debug.Log($"Coop KBnM: Current player two keyboard port set to {__instance.m_selectedIndex}");
                        if (!OptionsManager.isInitializingOptions)
                        {
                            float temp = OptionsManager.normalizedPlayerOneMouseSensitivity;
                            OptionsManager.normalizedPlayerOneMouseSensitivity = OptionsManager.normalizedPlayerTwoMouseSensitivity;
                            OptionsManager.normalizedPlayerTwoMouseSensitivity = temp;
                        }
                        if (OptionsManager.playerOneMouseSensitivityPanelObject != null)
                        {
                            OptionsManager.playerOneMouseSensitivityPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/MusicVolumeProgressBar").GetComponent<dfProgressBar>().Value = OptionsManager.normalizedPlayerOneMouseSensitivity;
                        }
                        if (OptionsManager.playerTwoMouseSensitivityPanelObject != null)
                        {
                            OptionsManager.playerTwoMouseSensitivityPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/MusicVolumeProgressBar").GetComponent<dfProgressBar>().Value = OptionsManager.normalizedPlayerTwoMouseSensitivity;
                        }
                        RawInputHandler.playerOneMouseSensitivity = Mathf.Clamp(4 * OptionsManager.normalizedPlayerOneMouseSensitivity + 0.4f, 0.4f, 4.4f);
                        RawInputHandler.playerTwoMouseSensitivity = Mathf.Clamp(4 * OptionsManager.normalizedPlayerTwoMouseSensitivity + 0.4f, 0.4f, 4.4f);
                        break;
                    default:
                        break;
                }
            }

            [HarmonyILManipulator]
            public static void HandleLeftRightArrowValueChangedPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameOptions>("set_CurrentPreferredFullscreenMode")
                    ))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<HandleLeftRightArrowValueChangedPatchClass>(nameof(HandleLeftRightArrowValueChangedPatchClass.HandleLeftRightArrowValueChangedPatchCall));
                }
            }

            private static void HandleLeftRightArrowValueChangedPatchCall(BraveOptionsMenuItem self)
            {
                GameManager.Options.CurrentPreferredFullscreenMode = self.m_selectedIndex != 0 ? GameOptions.PreferredFullscreenMode.WINDOWED : GameOptions.PreferredFullscreenMode.BORDERLESS;
            }
        }

        [HarmonyPatch(typeof(BraveOptionsMenuItem), nameof(BraveOptionsMenuItem.HandleCheckboxValueChanged))]
        public class HandleCheckboxValueChangedPatchClass
        {
            [HarmonyPrefix]
            public static bool HandleCheckboxValueChangedPrefix(BraveOptionsMenuItem __instance)
            {
                switch (__instance.optionType)
                {
                    case (BraveOptionsMenuItem.BraveOptionsOptionType)OptionsManager.BraveOptionsOptionType.SHARE_ONE_KEYBOARD_MODE:
                        bool newValue = __instance.m_selectedIndex != 0;
                        if (OptionsManager.isShareOneKeyboardMode != newValue)
                        {
                            OptionsManager.isShareOneKeyboardMode = newValue;
                            Debug.Log($"Coop KBnM: Is share one keyboard mode set to {newValue}");
                            if (OptionsManager.isShareOneKeyboardMode)
                            {
                                OptionsManager.SaveBindingData(false);
                                OptionsManager.LoadBindingData(true);
                                OptionsManager.RemoveDuplicateBindings();
                            }
                            else
                            {
                                OptionsManager.SaveBindingData(true);
                                OptionsManager.LoadBindingData(false);
                            }
                            OptionsManager.SaveBindingInfoToCachedOptions();
                            BraveInput.SaveBindingInfoToOptions();
                            GameOptions.Save();
                        }
                        GameManager.Instance.StartCoroutine(OptionsManager.ReassignKeyboardAndMouseCrt());
                        break;
                    default:
                        break;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BraveOptionsMenuItem), nameof(BraveOptionsMenuItem.HandleFillbarValueChanged))]
        public class HandleFillbarValueChangedPatchClass
        {
            [HarmonyPrefix]
            public static bool HandleFillbarValueChangedPrefix(BraveOptionsMenuItem __instance)
            {
                switch (__instance.optionType)
                {
                    case (BraveOptionsMenuItem.BraveOptionsOptionType)OptionsManager.BraveOptionsOptionType.PLAYER_ONE_MOUSE_SENSITIVITY:
                        RawInputHandler.playerOneMouseSensitivity = Mathf.Clamp(4 * __instance.m_actualFillbarValue + 0.4f, 0.4f, 4.4f);
                        OptionsManager.normalizedPlayerOneMouseSensitivity = __instance.m_actualFillbarValue;
                        Debug.Log($"Coop KBnM: Normalized player one mouse sensitivity set to {__instance.m_actualFillbarValue}");
                        break;
                    case (BraveOptionsMenuItem.BraveOptionsOptionType)OptionsManager.BraveOptionsOptionType.PLAYER_TWO_MOUSE_SENSITIVITY:
                        RawInputHandler.playerTwoMouseSensitivity = Mathf.Clamp(4 * __instance.m_actualFillbarValue + 0.4f, 0.4f, 4.4f);
                        OptionsManager.normalizedPlayerTwoMouseSensitivity = __instance.m_actualFillbarValue;
                        Debug.Log($"Coop KBnM: Normalized player two mouse sensitivity set to {__instance.m_actualFillbarValue}");
                        break;
                    default:
                        break;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BraveOptionsMenuItem), nameof(BraveOptionsMenuItem.DoSelectedAction))]
        public class DoSelectedActionPatchClass
        {
            [HarmonyILManipulator]
            public static void DoSelectedActionPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchStsfld<FullOptionsMenuController>("CurrentBindingPlayerTargetIndex")
                    ))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<DoSelectedActionPatchClass>(nameof(DoSelectedActionPatchClass.DoSelectedActionPatchCall_1));
                }
                crs.Index = 0;

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdcI4(0)
                    ))).TheNthTime(4))
                {
                    crs.EmitCall<DoSelectedActionPatchClass>(nameof(DoSelectedActionPatchClass.DoSelectedActionPatchCall_2));
                }
            }

            private static int DoSelectedActionPatchCall_1(int orig, BraveOptionsMenuItem self)
            {
                return self.name == "EditKeyboardBindingsButtonPanel" ? 0 : 1;
            }

            private static int DoSelectedActionPatchCall_2(int orig)
            {
                return 1;
            }
        }

        [HarmonyPatch(typeof(KeyboardBindingMenuOption), nameof(KeyboardBindingMenuOption.InitializeKeyboard))]
        public class InitializeKeyboardPatchClass
        {
            [HarmonyILManipulator]
            public static void InitializeKeyboardPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_CurrentGameType")
                    ))
                {
                    crs.EmitCall<InitializeKeyboardPatchClass>(nameof(InitializeKeyboardPatchClass.InitializeKeyboardPatchCall));
                }
            }

            private static int InitializeKeyboardPatchCall(int orig)
            {
                return 0;
            }
        }

        [HarmonyPatch(typeof(KeyboardBindingMenuOption), nameof(KeyboardBindingMenuOption.EnterAssignmentMode))]
        public class EnterAssignmentModePatchClass
        {
            [HarmonyPostfix]
            public static void EnterAssignmentModePostfix(KeyboardBindingMenuOption __instance, bool isAlternateKey)
            {
                GungeonActions activeActions = __instance.GetBestInputInstance().ActiveActions;
                PlayerAction actionFromType = activeActions.GetActionFromType(__instance.ActionType);
                BindingListenOptions bindingOptions = actionFromType.ListenOptions;
                bindingOptions.OnBindingFound = delegate (PlayerAction action, BindingSource binding)
                {
                    if (binding == new KeyBindingSource(new Key[]
                    {
                        Key.Escape
                    }))
                    {
                        action.StopListeningForBinding();
                        GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.ClearModalKeyBindingDialog(null, null);
                        return false;
                    }
                    if (binding == new KeyBindingSource(new Key[]
                    {
                        Key.Delete
                    }))
                    {
                        action.StopListeningForBinding();
                        GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.ClearModalKeyBindingDialog(null, null);
                        return false;
                    }
                    if (__instance.IsControllerMode && binding is KeyBindingSource)
                    {
                        return false;
                    }
                    if (binding is KeyBindingSource && OptionsManager.isShareOneKeyboardMode && BraveInput.m_instances.Count > 1 && action.Owner == BraveInput.GetInstanceForPlayer(1).ActiveActions && BraveInput.GetInstanceForPlayer(0).ActiveActions.HasBinding(binding))
                    {
                        return false;
                    }
                    if (binding is KeyBindingSource && OptionsManager.isShareOneKeyboardMode && BraveInput.m_instances.Count > 1 && action.Owner == BraveInput.GetInstanceForPlayer(0).ActiveActions && BraveInput.GetInstanceForPlayer(1).ActiveActions.HasBinding(binding))
                    {
                        int count = BraveInput.GetInstanceForPlayer(1).ActiveActions.Actions.Count;
                        for (int j = 0; j < count; j++)
                        {
                            BraveInput.GetInstanceForPlayer(1).ActiveActions.Actions[j].HardRemoveBinding(binding);
                        }
                    }
                    action.StopListeningForBinding();
                    if (!__instance.m_parentOptionsMenu.ActionIsMultibindable(__instance.ActionType, activeActions))
                    {
                        __instance.m_parentOptionsMenu.ClearBindingFromAllControls(FullOptionsMenuController.CurrentBindingPlayerTargetIndex, binding);
                    }
                    action.SetBindingOfTypeByNumber(binding, binding.BindingSourceType, (!isAlternateKey) ? 0 : 1, bindingOptions.OnBindingAdded);
                    GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.ToggleKeyBindingDialogState(binding);
                    __instance.Initialize();
                    return false;
                };
            }
        }

        [HarmonyPatch(typeof(GungeonActions), nameof(GungeonActions.ReinitializeDefaults))]
        public class ReinitializeDefaultsPatchClass
        {
            [HarmonyPostfix]
            public static void ReinitializeDefaultsPostfix()
            {
                if (OptionsManager.isShareOneKeyboardMode)
                    OptionsManager.RemoveDuplicateBindings();
            }
        }

        [HarmonyPatch(typeof(GungeonActions), nameof(GungeonActions.InitializeSwappedTriggersPreset))]
        public class InitializeSwappedTriggersPresetPatchClass
        {
            [HarmonyPostfix]
            public static void InitializeSwappedTriggersPresetPostfix()
            {
                if (OptionsManager.isShareOneKeyboardMode)
                    OptionsManager.RemoveDuplicateBindings();
            }
        }

        [HarmonyPatch(typeof(Input), nameof(Input.mousePosition), MethodType.Getter)]
        public class Get_MousePositionPatchClass
        {
            [HarmonyPrefix]
            public static bool Get_MousePositionPrefix(ref Vector3 __result)
            {
                __result = RawInputHandler.firstMousePosition;
                return false;
            }
        }

        [HarmonyPatch(typeof(MouseBindingSource), nameof(MouseBindingSource.PositiveScrollWheelIsActive))]
        public class PositiveScrollWheelIsActivePatchClass
        {
            [HarmonyPrefix]
            public static bool PositiveScrollWheelIsActivePrefix(float threshold, ref bool __result)
            {
                float num = Mathf.Max(0f, RawInputHandler.GetPublicWheel() * MouseBindingSource.ScaleZ);
                __result = num > threshold;
                return false;
            }
        }

        [HarmonyPatch(typeof(MouseBindingSource), nameof(MouseBindingSource.NegativeScrollWheelIsActive))]
        public class NegativeScrollWheelIsActivePatchClass
        {
            [HarmonyPrefix]
            public static bool NegativeScrollWheelIsActivePrefix(float threshold, ref bool __result)
            {
                float num = Mathf.Min(RawInputHandler.GetPublicWheel() * MouseBindingSource.ScaleZ, 0f);
                __result = num < -threshold;
                return false;
            }
        }

        [HarmonyPatch(typeof(Input), nameof(Input.GetAxis))]
        public class GetAxisPatchClass
        {
            [HarmonyPrefix]
            public static bool GetAxisPrefix(string axisName, ref float __result)
            {
                if (axisName == "Mouse ScrollWheel")
                {
                    __result = RawInputHandler.GetPublicSmoothWheelValue();
                    return false;
                }
                else
                    return true;
            }
        }

        [HarmonyPatch(typeof(GameCursorController), nameof(GameCursorController.DrawCursor))]
        public class DrawCursorPatchClass
        {
            [HarmonyILManipulator]
            public static void DrawCursorPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCall<GameCursorController>("get_showMouseCursor")
                    ))
                {
                    crs.EmitCall<DrawCursorPatchClass>(nameof(DrawCursorPatchClass.DrawCursorPatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<BraveInput>("IsKeyboardAndMouse")
                    ))
                {
                    crs.EmitCall<DrawCursorPatchClass>(nameof(DrawCursorPatchClass.DrawCursorPatchCall_2));
                }
            }

            [HarmonyPostfix]
            public static void DrawCursorPostfix(GameCursorController __instance)
            {
                if (!GameManager.HasInstance)
                    return;

                if (GameCursorController.showMouseCursor && (GameManager.HasInstance ? GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER : false))
                {
                    if (!CoopKBnMModule.isCoopViewLoaded || !CoopKBnMModule.secondWindowActive)
                    {
                        if (RawInputHandler.ShowPublicCursor)
                        {
                            Texture2D texture2D;
                            Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                            float scale = 1f;
                            if (customCursorIsOn)
                            {
                                if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && !BraveInput.GetInstanceForPlayer(0).IsKeyboardAndMouse(false) && BraveInput.GetInstanceForPlayer(1).IsKeyboardAndMouse(false))
                                {
                                    texture2D = playerTwoCursor;
                                    color = playerTwoCursorModulation;
                                    scale = playerTwoCursorScale;
                                }
                                else
                                {
                                    texture2D = playerOneCursor;
                                    color = playerOneCursorModulation;
                                    scale = playerOneCursorScale;
                                }
                                if (texture2D == null)
                                {
                                    texture2D = __instance.normalCursor;
                                    int currentCursorIndex = GameManager.Options.CurrentCursorIndex;
                                    if (currentCursorIndex >= 0 && currentCursorIndex < __instance.cursors.Length)
                                        texture2D = __instance.cursors[currentCursorIndex];
                                }
                            }
                            else
                            {
                                texture2D = __instance.normalCursor;
                                int currentCursorIndex = GameManager.Options.CurrentCursorIndex;

                                if (currentCursorIndex >= 0 && currentCursorIndex < __instance.cursors.Length)
                                    texture2D = __instance.cursors[currentCursorIndex];

                                if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && !BraveInput.GetInstanceForPlayer(0).IsKeyboardAndMouse(false) && BraveInput.GetInstanceForPlayer(1).IsKeyboardAndMouse(false))
                                    color = new Color(0.402f, 0.111f, 0.32f);
                            }

                            Vector2 mousePosition = RawInputHandler.firstMousePosition;
                            mousePosition.y = (float)Screen.height - mousePosition.y;
                            Vector2 vector = new Vector2((float)texture2D.width, (float)texture2D.height) * (float)((!(Pixelator.Instance != null)) ? 3 : ((int)Pixelator.Instance.ScaleTileScale)) * scale;
                            Rect screenRect = new Rect(mousePosition.x + 0.5f - vector.x / 2f, mousePosition.y + 0.5f - vector.y / 2f, vector.x, vector.y);
                            Graphics.DrawTexture(screenRect, texture2D, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, color);
                        }
                        else
                        {
                            if (RawInputHandler.ShowPlayerOneMouseCursor)
                            {
                                Texture2D texture2D;
                                Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                                float scale = 1f;
                                if (customCursorIsOn)
                                {
                                    texture2D = playerOneCursor;
                                    color = playerOneCursorModulation;
                                    scale = playerOneCursorScale;
                                    if (texture2D == null)
                                    {
                                        texture2D = __instance.normalCursor;
                                        int currentCursorIndex = GameManager.Options.CurrentCursorIndex;
                                        if (currentCursorIndex >= 0 && currentCursorIndex < __instance.cursors.Length)
                                            texture2D = __instance.cursors[currentCursorIndex];
                                    }
                                }
                                else
                                {
                                    texture2D = __instance.normalCursor;
                                    int currentCursorIndex = GameManager.Options.CurrentCursorIndex;

                                    if (currentCursorIndex >= 0 && currentCursorIndex < __instance.cursors.Length)
                                        texture2D = __instance.cursors[currentCursorIndex];
                                }
                                Vector2 mousePosition1;
                                if (!OptionsManager.restrictMouseInputPort)
                                    mousePosition1 = RawInputHandler.firstMousePosition;
                                else
                                {
                                    if (OptionsManager.currentPlayerOneMousePort == 1)
                                        mousePosition1 = RawInputHandler.secondMousePosition;
                                    else
                                        mousePosition1 = RawInputHandler.firstMousePosition;
                                }
                                mousePosition1.y = (float)Screen.height - mousePosition1.y;
                                Vector2 vector1 = new Vector2((float)texture2D.width, (float)texture2D.height) * (float)((!(Pixelator.Instance != null)) ? 3 : ((int)Pixelator.Instance.ScaleTileScale)) * scale;
                                Rect screenRect1 = new Rect(mousePosition1.x + 0.5f - vector1.x / 2f, mousePosition1.y + 0.5f - vector1.y / 2f, vector1.x, vector1.y);
                                Graphics.DrawTexture(screenRect1, texture2D, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, color);
                            }

                            if (RawInputHandler.ShowPlayerTwoMouseCursor)
                            {
                                Texture2D texture2D;
                                Color color = new Color(0.402f, 0.111f, 0.32f);
                                float scale = 1f;
                                if (customCursorIsOn)
                                {
                                    texture2D = playerTwoCursor;
                                    color = playerTwoCursorModulation;
                                    scale = playerTwoCursorScale;
                                    if (texture2D == null)
                                    {
                                        texture2D = __instance.normalCursor;
                                        int currentCursorIndex = GameManager.Options.CurrentCursorIndex;
                                        if (currentCursorIndex >= 0 && currentCursorIndex < __instance.cursors.Length)
                                            texture2D = __instance.cursors[currentCursorIndex];
                                    }
                                }
                                else
                                {
                                    texture2D = __instance.normalCursor;
                                    int currentCursorIndex = GameManager.Options.CurrentCursorIndex;

                                    if (currentCursorIndex >= 0 && currentCursorIndex < __instance.cursors.Length)
                                        texture2D = __instance.cursors[currentCursorIndex];
                                }
                                Vector2 mousePosition2;
                                if (!OptionsManager.restrictMouseInputPort)
                                    mousePosition2 = RawInputHandler.firstMousePosition;
                                else
                                {
                                    if (OptionsManager.currentPlayerOneMousePort == 0)
                                        mousePosition2 = RawInputHandler.secondMousePosition;
                                    else
                                        mousePosition2 = RawInputHandler.firstMousePosition;
                                }
                                mousePosition2.y = (float)Screen.height - mousePosition2.y;
                                Vector2 vector2 = new Vector2((float)texture2D.width, (float)texture2D.height) * (float)((!(Pixelator.Instance != null)) ? 3 : ((int)Pixelator.Instance.ScaleTileScale)) * scale;
                                Rect screenRect2 = new Rect(mousePosition2.x + 0.5f - vector2.x / 2f, mousePosition2.y + 0.5f - vector2.y / 2f, vector2.x, vector2.y);
                                Graphics.DrawTexture(screenRect2, texture2D, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, color);
                            }
                        }
                    }
                    else
                    {
                        if (RawInputHandler.ShowPublicCursor)
                        {
                            Texture2D texture2D;
                            Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                            float scale = 1f;
                            if (customCursorIsOn)
                            {
                                if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && !BraveInput.GetInstanceForPlayer(0).IsKeyboardAndMouse(false) && BraveInput.GetInstanceForPlayer(1).IsKeyboardAndMouse(false))
                                {
                                    texture2D = playerTwoCursor;
                                    color = playerTwoCursorModulation;
                                    scale = playerTwoCursorScale;
                                }
                                else
                                {
                                    texture2D = playerOneCursor;
                                    color = playerOneCursorModulation;
                                    scale = playerOneCursorScale;
                                }
                                if (texture2D == null)
                                {
                                    texture2D = __instance.normalCursor;
                                    int currentCursorIndex = GameManager.Options.CurrentCursorIndex;
                                    if (currentCursorIndex >= 0 && currentCursorIndex < __instance.cursors.Length)
                                        texture2D = __instance.cursors[currentCursorIndex];
                                }
                            }
                            else
                            {
                                texture2D = __instance.normalCursor;
                                int currentCursorIndex = GameManager.Options.CurrentCursorIndex;

                                if (currentCursorIndex >= 0 && currentCursorIndex < __instance.cursors.Length)
                                    texture2D = __instance.cursors[currentCursorIndex];

                                if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && !BraveInput.GetInstanceForPlayer(0).IsKeyboardAndMouse(false) && BraveInput.GetInstanceForPlayer(1).IsKeyboardAndMouse(false))
                                    color = new Color(0.402f, 0.111f, 0.32f);
                            }
                            Vector2 mousePosition = RawInputHandler.firstMousePosition;
                            mousePosition.y = (float)Screen.height - mousePosition.y;
                            Vector2 vector = new Vector2((float)texture2D.width, (float)texture2D.height) * (float)((!(Pixelator.Instance != null)) ? 3 : ((int)Pixelator.Instance.ScaleTileScale)) * scale;
                            Rect screenRect = new Rect(mousePosition.x + 0.5f - vector.x / 2f, mousePosition.y + 0.5f - vector.y / 2f, vector.x, vector.y);
                            Graphics.DrawTexture(screenRect, texture2D, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, color);
                        }
                        else
                        {
                            if ((RawInputHandler.ShowPlayerOneMouseCursor && OptionsManager.isPrimaryPlayerOnMainCamera) || (RawInputHandler.ShowPlayerTwoMouseCursor && !OptionsManager.isPrimaryPlayerOnMainCamera))
                            {
                                Texture2D texture2D;
                                Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                                float scale = 1f;
                                if (customCursorIsOn)
                                {
                                    if (!OptionsManager.isPrimaryPlayerOnMainCamera)
                                    {
                                        texture2D = playerTwoCursor;
                                        color = playerTwoCursorModulation;
                                        scale = playerTwoCursorScale;
                                    }
                                    else
                                    {
                                        texture2D = playerOneCursor;
                                        color = playerOneCursorModulation;
                                        scale = playerOneCursorScale;
                                    }
                                    if (texture2D == null)
                                    {
                                        texture2D = __instance.normalCursor;
                                        int currentCursorIndex = GameManager.Options.CurrentCursorIndex;
                                        if (currentCursorIndex >= 0 && currentCursorIndex < __instance.cursors.Length)
                                            texture2D = __instance.cursors[currentCursorIndex];
                                    }
                                }
                                else
                                {
                                    texture2D = __instance.normalCursor;
                                    int currentCursorIndex = GameManager.Options.CurrentCursorIndex;

                                    if (currentCursorIndex >= 0 && currentCursorIndex < __instance.cursors.Length)
                                        texture2D = __instance.cursors[currentCursorIndex];

                                    if (!OptionsManager.isPrimaryPlayerOnMainCamera)
                                        color = new Color(0.402f, 0.111f, 0.32f);
                                }
                                Vector2 mousePosition;
                                if (!OptionsManager.restrictMouseInputPort)
                                    mousePosition = RawInputHandler.firstMousePosition;
                                else
                                {
                                    if (OptionsManager.isPrimaryPlayerOnMainCamera)
                                        mousePosition = OptionsManager.currentPlayerOneMousePort == 0 ? RawInputHandler.firstMousePosition : RawInputHandler.secondMousePosition;
                                    else
                                        mousePosition = OptionsManager.currentPlayerOneMousePort != 0 ? RawInputHandler.firstMousePosition : RawInputHandler.secondMousePosition;
                                }

                                mousePosition.y = (float)Screen.height - mousePosition.y;
                                Vector2 vector = new Vector2((float)texture2D.width, (float)texture2D.height) * (float)((!(Pixelator.Instance != null)) ? 3 : ((int)Pixelator.Instance.ScaleTileScale)) * scale;
                                Rect screenRect = new Rect(mousePosition.x + 0.5f - vector.x / 2f, mousePosition.y + 0.5f - vector.y / 2f, vector.x, vector.y);
                                Graphics.DrawTexture(screenRect, texture2D, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, color);
                            }
                        }
                    }
                }
            }

            private static bool DrawCursorPatchCall_1(bool orig)
            {
                return orig && (GameManager.HasInstance ? GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER : true);
            }

            private static bool DrawCursorPatchCall_2(bool orig)
            {
                return orig && !OptionsManager.restrictMouseInputPort;
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.DetermineAimPointInWorld))]
        public class DetermineAimPointInWorldPatchClass
        {
            [HarmonyILManipulator]
            public static void DetermineAimPointInWorldPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<Vector3>("x")
                    ))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<DetermineAimPointInWorldPatchClass>(nameof(DetermineAimPointInWorldPatchClass.DetermineAimPointInWorldPatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<Vector3>("y")
                    ))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<DetermineAimPointInWorldPatchClass>(nameof(DetermineAimPointInWorldPatchClass.DetermineAimPointInWorldPatchCall_2));
                }
            }

            private static float DetermineAimPointInWorldPatchCall_1(float orig, PlayerController self)
            {
                if (OptionsManager.restrictMouseInputPort)
                {
                    if (self.IsPrimaryPlayer)
                    {
                        if (OptionsManager.currentPlayerOneMousePort == 0)
                            return RawInputHandler.firstMousePosition.x;
                        else
                            return RawInputHandler.secondMousePosition.x;
                    }
                    else
                    {
                        if (OptionsManager.currentPlayerOneMousePort == 0)
                            return RawInputHandler.secondMousePosition.x;
                        else
                            return RawInputHandler.firstMousePosition.x;
                    }
                }
                else
                    return orig;
            }

            private static float DetermineAimPointInWorldPatchCall_2(float orig, PlayerController self)
            {
                if (OptionsManager.restrictMouseInputPort)
                {
                    if (self.IsPrimaryPlayer)
                    {
                        if (OptionsManager.currentPlayerOneMousePort == 0)
                            return RawInputHandler.firstMousePosition.y;
                        else
                            return RawInputHandler.secondMousePosition.y;
                    }
                    else
                    {
                        if (OptionsManager.currentPlayerOneMousePort == 0)
                            return RawInputHandler.secondMousePosition.y;
                        else
                            return RawInputHandler.firstMousePosition.y;
                    }
                }
                else
                    return orig;
            }
        }

        [HarmonyPatch(typeof(FullOptionsMenuController), nameof(FullOptionsMenuController.ToggleToPanel))]
        public class ToggleToPanelPatchClass
        {
            [HarmonyPostfix]
            public static void ToggleToPanelPostfix(FullOptionsMenuController __instance, dfScrollPanel targetPanel, bool doFocus)
            {
                OptionsManager.fullOptionsMenuController = __instance;
                if (targetPanel == __instance.TabControls)
                {
                    int indexCanSelect = 0;
                    for (; indexCanSelect < targetPanel.Controls.Count; ++indexCanSelect)
                    {
                        if (targetPanel.Controls[indexCanSelect].CanFocus)
                            break;
                    }

                    if (targetPanel.Controls.Count > 0 && indexCanSelect < targetPanel.Controls.Count)
                    {
                        __instance.PrimaryCancelButton.GetComponent<UIKeyControls>().down = targetPanel.Controls[indexCanSelect];
                        __instance.PrimaryConfirmButton.GetComponent<UIKeyControls>().down = targetPanel.Controls[indexCanSelect];
                        __instance.PrimaryResetDefaultsButton.GetComponent<UIKeyControls>().down = targetPanel.Controls[indexCanSelect];
                        targetPanel.Controls[indexCanSelect].GetComponent<BraveOptionsMenuItem>().up = __instance.PrimaryConfirmButton;
                        targetPanel.Controls[indexCanSelect].Focus(true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BraveOptionsMenuItem), nameof(BraveOptionsMenuItem.Awake))]
        public class BraveOptionsMenuItemAwakePatchClass
        {
            [HarmonyPostfix]
            public static void BraveOptionsMenuItemAwake_Post(BraveOptionsMenuItem __instance)
            {
                if (__instance.optionType == BraveOptionsMenuItem.BraveOptionsOptionType.FULLSCREEN)
                {
                    __instance.labelOptions = new string[]
                    {
                        "Borderless",
                        "Windowed"
                    };
                }
            }
        }

        [HarmonyPatch(typeof(BraveOptionsMenuItem), nameof(BraveOptionsMenuItem.GetIndexFromFullscreenMode))]
        public class GetIndexFromFullscreenModePatchClass
        {
            [HarmonyPrefix]
            public static bool GetIndexFromFullscreenModePrefix(GameOptions.PreferredFullscreenMode fMode, ref int __result)
            {
                __result = fMode != GameOptions.PreferredFullscreenMode.WINDOWED ? 0 : 1;
                return false;
            }
        }

        [HarmonyPatch(typeof(GameOptions), nameof(GameOptions.CurrentPreferredFullscreenMode), MethodType.Setter)]
        public class SetCurrentPreferredFullscreenModePatchClass
        {
            [HarmonyILManipulator]
            public static void SetCurrentPreferredFullscreenModePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchStfld<GameOptions>("m_preferredFullscreenMode")
                    ))
                {
                    crs.EmitCall<SetCurrentPreferredFullscreenModePatchClass>(nameof(SetCurrentPreferredFullscreenModePatchClass.SetCurrentPreferredFullscreenModePatchCall));
                }
            }

            private static GameOptions.PreferredFullscreenMode SetCurrentPreferredFullscreenModePatchCall(GameOptions.PreferredFullscreenMode orig)
            {
                if (orig == GameOptions.PreferredFullscreenMode.FULLSCREEN)
                    return GameOptions.PreferredFullscreenMode.BORDERLESS;
                return orig;
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.DoSetResolution))]
        public class DoSetResolutionPatchClass
        {
            [HarmonyPrefix]
            public static bool DoSetResolutionPrefix()
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(dfInputManager), nameof(dfInputManager.Update))]
        public class DfInputManagerUpdatePatchClass
        {
            [HarmonyILManipulator]
            public static void DfInputManagerUpdatePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchStloc(3)
                    ))
                {
                    crs.Emit(OpCodes.Ldloca_S, (byte)3);
                    crs.Emit(OpCodes.Ldloc_2);
                    crs.EmitCall<DfInputManagerUpdatePatchClass>(nameof(DfInputManagerUpdatePatchClass.DfInputManagerUpdatePatchCall));
                }
            }

            private static void DfInputManagerUpdatePatchCall(ref bool orig, Vector2 vector)
            {
                orig = ((BraveInput.SecondaryPlayerInstance != null && BraveInput.SecondaryPlayerInstance.ActiveActions != null) ?
                    (BraveInput.SecondaryPlayerInstance.ActiveActions.LastInputType == BindingSourceType.MouseBindingSource
                    || (BraveInput.SecondaryPlayerInstance.ActiveActions.LastInputType == BindingSourceType.KeyBindingSource && vector.magnitude > float.Epsilon)) :
                    false)
                    || BraveInput.PrimaryPlayerInstance.ActiveActions.LastInputType == BindingSourceType.MouseBindingSource
                    || (BraveInput.PrimaryPlayerInstance.ActiveActions.LastInputType == BindingSourceType.KeyBindingSource && vector.magnitude > float.Epsilon)
                    || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0);
            }
        }

        [HarmonyPatch(typeof(KeyboardBindingMenuOption), nameof(KeyboardBindingMenuOption.GetBestInputInstance))]
        public class KeyboardBindingMenuOptionGetBestInputInstancePatchClass
        {
            [HarmonyILManipulator]
            public static void KeyboardBindingMenuOptionGetBestInputInstancePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdsfld<Foyer>("DoMainMenu")))
                {
                    crs.EmitCall<KeyboardBindingMenuOptionGetBestInputInstancePatchClass>(nameof(KeyboardBindingMenuOptionGetBestInputInstancePatchClass.KeyboardBindingMenuOptionGetBestInputInstancePatchCall));
                }
            }

            private static bool KeyboardBindingMenuOptionGetBestInputInstancePatchCall(bool orig)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(GameOptions), nameof(GameOptions.GetBestInputInstance))]
        public class GameOptionsGetBestInputInstancePatchClass
        {
            [HarmonyILManipulator]
            public static void GameOptionsGetBestInputInstancePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdsfld<Foyer>("DoMainMenu")))
                {
                    crs.EmitCall<GameOptionsGetBestInputInstancePatchClass>(nameof(GameOptionsGetBestInputInstancePatchClass.GameOptionsGetBestInputInstancePatchCall));
                }
            }

            private static bool GameOptionsGetBestInputInstancePatchCall(bool orig)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(BraveInput), nameof(BraveInput.CheckForActionInitialization))]
        public class CheckForActionInitializationPatchClass
        {
            [HarmonyILManipulator]
            public static void CheckForActionInitializationPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdsfld<GameManager>("PreventGameManagerExistence")))
                {
                    crs.EmitCall<CheckForActionInitializationPatchClass>(nameof(CheckForActionInitializationPatchClass.CheckForActionInitializationPatchCall_1));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdcI4(0)))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<CheckForActionInitializationPatchClass>(nameof(CheckForActionInitializationPatchClass.CheckForActionInitializationPatchCall_2));
                }
            }

            private static bool CheckForActionInitializationPatchCall_1(bool orig)
            {
                return true;
            }

            private static int CheckForActionInitializationPatchCall_2(int orig, BraveInput self)
            {
                return (GameManager.PreventGameManagerExistence || (GameManager.Instance.PrimaryPlayer == null && self.m_playerID == 0) || (GameManager.Instance.PrimaryPlayer != null && self.m_playerID == GameManager.Instance.PrimaryPlayer.PlayerIDX))
                    ? 0 : 1;
            }
        }

        [HarmonyPatch(typeof(BraveInput), nameof(BraveInput.SaveBindingInfoToOptions))]
        public class SaveBindingInfoToOptionsPatchClass
        {
            [HarmonyILManipulator]
            public static void SaveBindingInfoToOptionsPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCall<UnityEngine.Object>("op_Equality")))
                {
                    crs.EmitCall<SaveBindingInfoToOptionsPatchClass>(nameof(SaveBindingInfoToOptionsPatchClass.SaveBindingInfoToOptionsPatchCall));
                }
            }

            private static bool SaveBindingInfoToOptionsPatchCall(bool orig)
            {
                if (orig)
                {
                    for (int i = 0; i < BraveInput.m_instances.Count; i++)
                    {
                        if (BraveInput.m_instances[i].m_playerID == 0)
                            GameManager.Options.playerOneBindingDataV2 = BraveInput.m_instances[i].ActiveActions.Save();
                        else
                            GameManager.Options.playerTwoBindingDataV2 = BraveInput.m_instances[i].ActiveActions.Save();
                    }
                }
                return orig;
            }
        }

        [HarmonyPatch(typeof(FullOptionsMenuController), nameof(FullOptionsMenuController.CloseAndApplyChanges))]
        public class CloseAndApplyChangesPatchClass
        {
            [HarmonyPostfix]
            public static void CloseAndApplyChangesPostfix()
            {
                CoopKBnMPreferences.SavePreferences();
            }
        }

        [HarmonyPatch(typeof(FullOptionsMenuController), nameof(FullOptionsMenuController.CloseAndRevertChanges))]
        public class CloseAndRevertChangesPatchClass
        {
            [HarmonyPostfix]
            public static void CloseAndRevertChangesPostfix()
            {
                CoopKBnMPreferences.SavePreferences();
            }
        }

        [HarmonyPatch(typeof(PlayerActionSet), nameof(PlayerActionSet.Load))]
        public class LoadPatchClass
        {
            [HarmonyPostfix]
            public static void LoadPostfix()
            {
                GameManager.Instance.StartCoroutine(OptionsManager.ReassignKeyboardAndMouseCrt());
                GameManager.Instance.StartCoroutine(OptionsManager.DelayedRemoveDuplicateBindingsCrt());
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.HandleDeviceShift))]
        public class HandleDeviceShiftPatchClass
        {
            [HarmonyPostfix]
            public static void HandleDeviceShiftPostfix()
            {
                GameManager.Instance.StartCoroutine(OptionsManager.ReassignKeyboardAndMouseCrt());
            }
        }

        [HarmonyPatch(typeof(BraveInput), nameof(BraveInput.ForceLoadBindingInfoFromOptions))]
        public class ForceLoadBindingInfoFromOptionsPatchClass
        {
            [HarmonyILManipulator]
            public static void ForceLoadBindingInfoFromOptionsPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdsfld<GameManager>("PreventGameManagerExistence")))
                {
                    crs.EmitCall<ForceLoadBindingInfoFromOptionsPatchClass>(nameof(ForceLoadBindingInfoFromOptionsPatchClass.ForceLoadBindingInfoFromOptionsPatchCall_1));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<BraveInput>("m_playerID")))
                {
                    crs.EmitCall<ForceLoadBindingInfoFromOptionsPatchClass>(nameof(ForceLoadBindingInfoFromOptionsPatchClass.ForceLoadBindingInfoFromOptionsPatchCall_2));
                }
                crs.Index = 0;

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdcI4(0)
                    ))).TheNthTime(2))
                {
                    crs.Emit(OpCodes.Ldloc_0);
                    crs.EmitCall<ForceLoadBindingInfoFromOptionsPatchClass>(nameof(ForceLoadBindingInfoFromOptionsPatchClass.ForceLoadBindingInfoFromOptionsPatchCall_3));
                }
            }

            private static bool ForceLoadBindingInfoFromOptionsPatchCall_1(bool orig)
            {
                return true;
            }

            private static int ForceLoadBindingInfoFromOptionsPatchCall_2(int orig)
            {
                return 0;
            }

            private static int ForceLoadBindingInfoFromOptionsPatchCall_3(int orig, int i)
            {
                return (GameManager.PreventGameManagerExistence
                    || (GameManager.Instance.PrimaryPlayer == null && BraveInput.m_instances[i].m_playerID == 0)
                    || (GameManager.Instance.PrimaryPlayer != null && BraveInput.m_instances[i].m_playerID == GameManager.Instance.PrimaryPlayer.PlayerIDX))
                    ? 0 : 1;
            }
        }

        [HarmonyPatch(typeof(BraveInput), nameof(BraveInput.ReassignAllControllers))]
        public class ReassignAllControllersPatchClass
        {
            [HarmonyILManipulator]
            public static void ReassignAllControllersPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCall<UnityEngine.Object>("op_Equality")))
                {
                    crs.EmitCall<ReassignAllControllersPatchClass>(nameof(ReassignAllControllersPatchClass.ReassignAllControllersPatchCall_1));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<BraveInput>("m_playerID")))
                {
                    crs.EmitCall<ReassignAllControllersPatchClass>(nameof(ReassignAllControllersPatchClass.ReassignAllControllersPatchCall_2));
                }
                crs.Index = 0;

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdcI4(0)
                    ))).TheNthTime(4))
                {
                    crs.Emit(OpCodes.Ldloc_S, (byte)4);
                    crs.EmitCall<ReassignAllControllersPatchClass>(nameof(ReassignAllControllersPatchClass.ReassignAllControllersPatchCall_3));
                }
            }

            private static bool ReassignAllControllersPatchCall_1(bool orig)
            {
                return true;
            }

            private static int ReassignAllControllersPatchCall_2(int orig)
            {
                return 0;
            }

            private static int ReassignAllControllersPatchCall_3(int orig, int i)
            {
                return ((GameManager.Instance.PrimaryPlayer == null && BraveInput.m_instances[i].m_playerID == 0)
                    || (GameManager.Instance.PrimaryPlayer != null && BraveInput.m_instances[i].m_playerID == GameManager.Instance.PrimaryPlayer.PlayerIDX))
                    ? 0 : 1;
            }
        }

        [HarmonyPatch(typeof(BraveInput), nameof(BraveInput.SavePlayerlessBindingsToOptions))]
        public class SavePlayerlessBindingsToOptionsPatchClass
        {
            [HarmonyPostfix]
            public static void SavePlayerlessBindingsToOptionsPostfix()
            {
                if (BraveInput.m_instances.Count > 1)
                {
                    if (BraveInput.m_instances[1].m_playerID == 1)
                    {
                        GameManager.Options.playerTwoBindingDataV2 = BraveInput.m_instances[1].ActiveActions.Save();
                    }
                }
            }
        }

        public class SetCustomCursorIsOnPatchClass
        {
            public static void SetCustomCursorIsOnPostfix(bool value)
            {
                customCursorIsOn = value;
            }
        }

        public class SetPlayerOneCursorPatchClass
        {
            public static void SetPlayerOneCursorPostfix(Texture2D value)
            {
                playerOneCursor = value;
            }
        }

        public class SetPlayerOneCursorModulationPatchClass
        {
            public static void SetPlayerOneCursorModulationPostfix(Color value)
            {
                playerOneCursorModulation = value;
            }
        }

        public class SetPlayerOneCursorScalePatchClass
        {
            public static void SetPlayerOneCursorScalePostfix(float value)
            {
                playerOneCursorScale = value;
            }
        }

        public class SetPlayerTwoCursorPatchClass
        {
            public static void SetPlayerTwoCursorPostfix(Texture2D value)
            {
                playerTwoCursor = value;
            }
        }

        public class SetPlayerTwoCursorModulationPatchClass
        {
            public static void SetPlayerTwoCursorModulationPostfix(Color value)
            {
                playerTwoCursorModulation = value;
            }
        }

        public class SetPlayerTwoCursorScalePatchClass
        {
            public static void SetPlayerTwoCursorScalePostfix(float value)
            {
                playerTwoCursorScale = value;
            }
        }
    }
}
