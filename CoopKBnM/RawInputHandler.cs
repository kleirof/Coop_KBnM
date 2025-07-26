using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Keys = CoopKBnM.KeyCodeMaps.Keys;
using InControl;

namespace CoopKBnM
{
    public class RawInputHandler : MonoBehaviour
    {
        private const int WM_INPUT = 0x00FF;
        private const int RIM_INPUT = 0x10000003;
        private const int RIDEV_INPUTSINK = 0x00000100;

        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_ALT = 0x0001;

        private const uint RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001;
        private const uint RI_MOUSE_LEFT_BUTTON_UP = 0x0002;
        private const uint RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004;
        private const uint RI_MOUSE_RIGHT_BUTTON_UP = 0x0008;
        private const uint RI_MOUSE_MIDDLE_BUTTON_DOWN = 0x0010;
        private const uint RI_MOUSE_MIDDLE_BUTTON_UP = 0x0020;
        private const uint RI_MOUSE_BUTTON_4_DOWN = 0x0040;
        private const uint RI_MOUSE_BUTTON_4_UP = 0x0080;
        private const uint RI_MOUSE_BUTTON_5_DOWN = 0x0100;
        private const uint RI_MOUSE_BUTTON_5_UP = 0x0200;
        private const uint RI_MOUSE_WHEEL = 0x0400;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterClass(ref WNDCLASS lpWndClass);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevice, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll")]
        private static extern int GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pDataSize, uint cbSizeHeader);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        private const uint WS_POPUP = 0x80000000;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;
        private const uint WM_DESTROY = 0x0002;
        private const uint WM_ShortcutKey = 0x0312;
        private const int SW_HIDE = 0;

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWMOUSE
        {
            public ushort usFlags;
            public ushort _;
            public ushort usButtonFlags;
            public short usButtonData;
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WNDCLASS
        {
            public uint style;
            public WNDPROC lpfn;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public IntPtr lpszMenuName;
            public string lpszClassName;
        }

        private delegate IntPtr WNDPROC(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static IntPtr _windowHandle;
        private static WNDPROC _windowProc;

        private static IntPtr rawBuffer;

        public static HashSet<KeyCode> firstKeyboardCurrentFrameKeysDown = new HashSet<KeyCode>();
        public static HashSet<KeyCode> firstKeyboardPressedKeys = new HashSet<KeyCode>();
        public static HashSet<KeyCode> firstKeyboardCurrentFrameKeysUp = new HashSet<KeyCode>();

        public static HashSet<KeyCode> secondKeyboardCurrentFrameKeysDown = new HashSet<KeyCode>();
        public static HashSet<KeyCode> secondKeyboardPressedKeys = new HashSet<KeyCode>();
        public static HashSet<KeyCode> secondKeyboardCurrentFrameKeysUp = new HashSet<KeyCode>();

        public static Vector2 firstMousePosition;
        public static Vector2 secondMousePosition;

        private static IntPtr firstKeyboardDevice = IntPtr.Zero;
        private static IntPtr firstMouseDevice = IntPtr.Zero;

        public static GungeonActions firstKeyboardActionSet;
        public static GungeonActions firstMouseActionSet;

        public static float playerOneMouseSensitivity = 2f;
        public static float playerTwoMouseSensitivity = 2f;

        public static string shortcutKeyString = "Alt + Shift + X";
        private const int shortcutKeyId = 101;

        public static bool temporarilyEnableSystemMouse = true;

        public bool lastInputIsMouse = false;
        public bool lastInputIsKeyboard = false;

        private static Camera mainCamera;

        public static float playerOneMouseSensitivityMultiplier = 1f;
        public static float playerTwoMouseSensitivityMultiplier = 1f;

        private static Camera MainCamera
        {
            get
            {
                if (mainCamera == null)
                    mainCamera = Camera.main;
                return mainCamera;
            }
        }

        public class SmoothWheel
        {
            private float smoothScrollValue = 0f;
            private float gravity = 15f;
            private float sensitivity = 15f;

            public SmoothWheel(float gravity = 15f, float sensitivity = 15f)
            {
                this.gravity = gravity;
                this.sensitivity = sensitivity;
            }

            public float GetSmoothWheelValue(float rawScrollValue)
            {
                if (Mathf.Abs(rawScrollValue) > 10f)
                {
                    smoothScrollValue = Mathf.Lerp(smoothScrollValue, rawScrollValue, Time.unscaledDeltaTime * sensitivity);
                }
                else
                {
                    smoothScrollValue = Mathf.Lerp(smoothScrollValue, 0, Time.unscaledDeltaTime * gravity);
                }

                if (Mathf.Abs(smoothScrollValue) < 3f)
                    smoothScrollValue = 0f;
                smoothScrollValue = Mathf.Clamp(smoothScrollValue / 120f, -1f, 1f);

                return smoothScrollValue;
            }
        }

        public static SmoothWheel firstMouseSmoothWheel = new SmoothWheel();
        public static SmoothWheel secondMouseSmoothWheel = new SmoothWheel();

        public struct MouseStatus
        {
            public bool leftButtonDown;
            public bool leftButtonUp;
            public bool leftButton;

            public bool rightButtonDown;
            public bool rightButtonUp;
            public bool rightButton;

            public bool middleButtonDown;
            public bool middleButtonUp;
            public bool middleButton;

            public bool x1ButtonDown;
            public bool x1ButtonUp;
            public bool x1Button;

            public bool x2ButtonDown;
            public bool x2ButtonUp;
            public bool x2Button;

            public int rawX;
            public int rawY;
            public int wheel;
        }

        public static MouseStatus firstMouseStatus;
        public static MouseStatus secondMouseStatus;

        private void Start()
        {
            ETGModMainBehaviour.WaitForGameManagerStart(GMStart);

            firstMousePosition = new Vector2(Screen.width / 2, Screen.height / 2);
            secondMousePosition = new Vector2(Screen.width / 2, Screen.height / 2);
        }

        private IEnumerator InitializeRawInput()
        {
            yield return null;
            yield return null;
            yield return null;
            InitializeMessageHandler();
            SetGlobalShortcutKey();
        }

        public void GMStart(GameManager g)
        {
            if (GameManager.Options != null && GameManager.Options.CurrentPreferredFullscreenMode == GameOptions.PreferredFullscreenMode.FULLSCREEN)
            {
                GameManager.Options.CurrentPreferredFullscreenMode = GameOptions.PreferredFullscreenMode.BORDERLESS;
                BraveOptionsMenuItem.HandleScreenDataChanged(Screen.width, Screen.height);
            }

            StartCoroutine(InitializeRawInput());
        }

        private void OnApplicationQuit()
        {
            UnregisterHotKey(_windowHandle, shortcutKeyId);
            ReleaseMessageHandler();
        }

        private void LateUpdate()
        {
            firstKeyboardCurrentFrameKeysDown.Clear();
            secondKeyboardCurrentFrameKeysDown.Clear();
            firstKeyboardCurrentFrameKeysUp.Clear();
            secondKeyboardCurrentFrameKeysUp.Clear();

            firstMouseStatus.leftButtonDown = false;
            firstMouseStatus.leftButtonUp = false;
            firstMouseStatus.rightButtonDown = false;
            firstMouseStatus.rightButtonUp = false;
            firstMouseStatus.middleButtonDown = false;
            firstMouseStatus.middleButtonUp = false;
            firstMouseStatus.x1ButtonDown = false;
            firstMouseStatus.x1ButtonUp = false;
            firstMouseStatus.x2ButtonDown = false;
            firstMouseStatus.x2ButtonUp = false;
            firstMouseStatus.wheel = 0;
            firstMouseStatus.rawX = 0;
            firstMouseStatus.rawY = 0;

            secondMouseStatus.leftButtonDown = false;
            secondMouseStatus.leftButtonUp = false;
            secondMouseStatus.rightButtonDown = false;
            secondMouseStatus.rightButtonUp = false;
            secondMouseStatus.middleButtonDown = false;
            secondMouseStatus.middleButtonUp = false;
            secondMouseStatus.x1ButtonDown = false;
            secondMouseStatus.x1ButtonUp = false;
            secondMouseStatus.x2ButtonDown = false;
            secondMouseStatus.x2ButtonUp = false;
            secondMouseStatus.wheel = 0;
            secondMouseStatus.rawX = 0;
            secondMouseStatus.rawY = 0;

            if (!Application.isFocused)
            {
                firstKeyboardPressedKeys.Clear();
                secondKeyboardPressedKeys.Clear();

                firstMouseStatus.leftButton = false;
                firstMouseStatus.rightButton = false;
                firstMouseStatus.middleButton = false;
                firstMouseStatus.x1Button = false;
                firstMouseStatus.x2Button = false;

                secondMouseStatus.leftButton = false;
                secondMouseStatus.rightButton = false;
                secondMouseStatus.middleButton = false;
                secondMouseStatus.x1Button = false;
                secondMouseStatus.x2Button = false;

                temporarilyEnableSystemMouse = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                if (Input.anyKeyDown)
                    temporarilyEnableSystemMouse = false;
            }

            if (Input.GetKeyDown(KeyCode.LeftWindows) || Input.GetKeyDown(KeyCode.RightWindows))
            {
                temporarilyEnableSystemMouse = true;
            }
            if (!temporarilyEnableSystemMouse && GameManager.HasInstance)
                Cursor.lockState = CursorLockMode.Locked;
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private static void RegisterDevices()
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[4];

            rid[0].usUsagePage = 0x01;
            rid[0].usUsage = 0x02;
            rid[0].dwFlags = 0;
            rid[0].hwndTarget = _windowHandle;

            rid[1].usUsagePage = 0x01;
            rid[1].usUsage = 0x02;
            rid[1].dwFlags = 0;
            rid[1].hwndTarget = _windowHandle;

            rid[2].usUsagePage = 0x01;
            rid[2].usUsage = 0x06;
            rid[2].dwFlags = 0;
            rid[2].hwndTarget = _windowHandle;

            rid[3].usUsagePage = 0x01;
            rid[3].usUsage = 0x06;
            rid[3].dwFlags = 0;
            rid[3].hwndTarget = _windowHandle;

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
            {
                Console.WriteLine("Failed to register raw input devices.");
                return;
            }
        }

        public static void InitializeMessageHandler()
        {
            rawBuffer = Marshal.AllocHGlobal((int)64);
            _windowProc = new WNDPROC(WindowProc);

            WNDCLASS wndClass = new WNDCLASS
            {
                style = 0,
                lpfn = _windowProc,
                lpszClassName = "RawInputWindowClass",
                hInstance = GetModuleHandle(null)
            };

            RegisterClass(ref wndClass);

            _windowHandle = CreateWindowEx(
                WS_EX_TOOLWINDOW,
                "RawInputWindowClass",
                "RawInputWindow",
                WS_POPUP,
                0, 0, 0, 0,
                IntPtr.Zero,
                IntPtr.Zero,
                GetModuleHandle(null),
                IntPtr.Zero);

            if (_windowHandle != IntPtr.Zero)
            {
                ShowWindow(_windowHandle, SW_HIDE);
            }

            if (_windowHandle == IntPtr.Zero)
            {
                ETGModConsole.Log("Failed to create raw input window.");
            }

            RegisterDevices();
        }

        public static void ReleaseMessageHandler()
        {
            if (_windowHandle != IntPtr.Zero)
                DestroyWindow(_windowHandle);
            Marshal.FreeHGlobal(rawBuffer);
        }

        private static IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_DESTROY:
                    return IntPtr.Zero;

                case WM_ShortcutKey:
                    int shortcutKeyId = wParam.ToInt32();
                    HandleShortcutKeyPressed(shortcutKeyId);
                    return IntPtr.Zero;

                case WM_INPUT:
                    ProcessRawInput(lParam);
                    return DefWindowProc(hWnd, msg, wParam, lParam);

                default:
                    return DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }

        public static void SetGlobalShortcutKey()
        {
            uint modifiers = MOD_SHIFT | MOD_ALT;
            uint key = (uint)Keys.X;

            if (!RegisterHotKey(_windowHandle, shortcutKeyId, modifiers, key))
            {
                ETGModConsole.Log($"Failed to register shortcut key {shortcutKeyString}.");
                shortcutKeyString = "None";
            }
        }

        private static void HandleShortcutKeyPressed(int shortcutKeyId)
        {
            switch (shortcutKeyId)
            {
                case 101:
                    temporarilyEnableSystemMouse = true;
                    break;

                default:
                    break;
            }
        }

        private static void ProcessRawInput(IntPtr lParam)
        {
            uint dwSize = 0;
            if (GetRawInputData(lParam, 0x10000003, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == 0)
            {
                //Console.WriteLine($"Input data size: {dwSize - Marshal.SizeOf(typeof(RAWINPUTHEADER))}");
            }
            else
            {
                Console.WriteLine("Failed to get input data size.");
                return;
            }

            if (dwSize > 64)
            {
                return;
            }

            if (GetRawInputData(lParam, 0x10000003, rawBuffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) != dwSize)
            {
                Console.WriteLine("GetRawInputData does not return correct size.");
                return;
            }

            RAWINPUTHEADER header = (RAWINPUTHEADER)Marshal.PtrToStructure(rawBuffer, typeof(RAWINPUTHEADER));

            if (header.dwType == 0)
            {
                IntPtr mouseDataPtr = new IntPtr(rawBuffer.ToInt64() + Marshal.SizeOf(typeof(RAWINPUTHEADER)));
                RAWMOUSE mouse = (RAWMOUSE)Marshal.PtrToStructure(mouseDataPtr, typeof(RAWMOUSE));

                if (firstMouseDevice == IntPtr.Zero)
                {
                    firstMouseDevice = header.hDevice;
                }

                if (mouse.lLastX != 0 || mouse.lLastY != 0)
                {
                    if (header.hDevice == firstMouseDevice)
                    {
                        firstMouseStatus.rawX = mouse.lLastX;
                        firstMouseStatus.rawY = mouse.lLastY;

                        firstMousePosition.x += firstMouseStatus.rawX * playerOneMouseSensitivity * playerOneMouseSensitivityMultiplier;
                        firstMousePosition.y -= firstMouseStatus.rawY * playerOneMouseSensitivity * playerOneMouseSensitivityMultiplier;

                        if (MainCamera != null)
                            firstMousePosition = new Vector2(Mathf.Clamp(firstMousePosition.x, (Screen.width - MainCamera.pixelWidth) / 2, (Screen.width + MainCamera.pixelWidth) / 2), Mathf.Clamp(firstMousePosition.y, (Screen.height - MainCamera.pixelHeight) / 2, (Screen.height + MainCamera.pixelHeight) / 2));
                        else
                            firstMousePosition = new Vector2(Mathf.Clamp(firstMousePosition.x, 0, Screen.width), Mathf.Clamp(firstMousePosition.y, 0, Screen.height));
                    }
                    else
                    {
                        secondMouseStatus.rawX = mouse.lLastX;
                        secondMouseStatus.rawY = mouse.lLastY;

                        if (ShowTwoMouseCursor)
                        {
                            secondMousePosition.x += secondMouseStatus.rawX * playerTwoMouseSensitivity * playerTwoMouseSensitivityMultiplier;
                            secondMousePosition.y -= secondMouseStatus.rawY * playerTwoMouseSensitivity * playerTwoMouseSensitivityMultiplier;

                            if (MainCamera != null)
                                secondMousePosition = new Vector2(Mathf.Clamp(secondMousePosition.x, (Screen.width - MainCamera.pixelWidth) / 2, (Screen.width + MainCamera.pixelWidth) / 2), Mathf.Clamp(secondMousePosition.y, (Screen.height - MainCamera.pixelHeight) / 2, (Screen.height + MainCamera.pixelHeight) / 2));
                            else
                                secondMousePosition = new Vector2(Mathf.Clamp(secondMousePosition.x, 0, Screen.width), Mathf.Clamp(secondMousePosition.y, 0, Screen.height));
                        }
                        else
                        {
                            firstMousePosition.x += secondMouseStatus.rawX * playerTwoMouseSensitivity * playerOneMouseSensitivityMultiplier;
                            firstMousePosition.y -= secondMouseStatus.rawY * playerTwoMouseSensitivity * playerOneMouseSensitivityMultiplier;

                            if (MainCamera != null)
                                firstMousePosition = new Vector2(Mathf.Clamp(firstMousePosition.x, (Screen.width - MainCamera.pixelWidth) / 2, (Screen.width + MainCamera.pixelWidth) / 2), Mathf.Clamp(firstMousePosition.y, (Screen.height - MainCamera.pixelHeight) / 2, (Screen.height + MainCamera.pixelHeight) / 2));
                            else
                                firstMousePosition = new Vector2(Mathf.Clamp(firstMousePosition.x, 0, Screen.width), Mathf.Clamp(firstMousePosition.y, 0, Screen.height));
                        }
                    }
                    //Console.WriteLine($"Mouse Device ID: {header.hDevice} | Mouse moved: X={mouse.lLastX}, Y={mouse.lLastY}");
                }

                if ((mouse.usButtonFlags & RI_MOUSE_LEFT_BUTTON_DOWN) != 0) // 左键按下
                {
                    if (header.hDevice == firstMouseDevice)
                    {
                        firstMouseStatus.leftButtonDown = true;
                        firstMouseStatus.leftButton = true;
                    }
                    else
                    {
                        secondMouseStatus.leftButtonDown = true;
                        secondMouseStatus.leftButton = true;
                    }
                    //Console.WriteLine($"Mouse Device ID: {header.hDevice} | Left button down");
                }
                if ((mouse.usButtonFlags & RI_MOUSE_LEFT_BUTTON_UP) != 0) // 左键松开
                {
                    if (header.hDevice == firstMouseDevice)
                    {
                        firstMouseStatus.leftButtonUp = true;
                        firstMouseStatus.leftButton = false;
                    }
                    else
                    {
                        secondMouseStatus.leftButtonUp = true;
                        secondMouseStatus.leftButton = false;
                    }
                    //Console.WriteLine($"Mouse Device ID: {header.hDevice} | Left button up");
                }
                if ((mouse.usButtonFlags & RI_MOUSE_RIGHT_BUTTON_DOWN) != 0) // 右键按下
                {
                    if (header.hDevice == firstMouseDevice)
                    {
                        firstMouseStatus.rightButtonDown = true;
                        firstMouseStatus.rightButton = true;
                    }
                    else
                    {
                        secondMouseStatus.rightButtonDown = true;
                        secondMouseStatus.rightButton = true;
                    }
                    //Console.WriteLine($"Mouse Device ID: {header.hDevice} | Right button down");
                }
                if ((mouse.usButtonFlags & RI_MOUSE_RIGHT_BUTTON_UP) != 0) // 右键松开
                {
                    if (header.hDevice == firstMouseDevice)
                    {
                        firstMouseStatus.rightButtonUp = true;
                        firstMouseStatus.rightButton = false;
                    }
                    else
                    {
                        secondMouseStatus.rightButtonUp = true;
                        secondMouseStatus.rightButton = false;
                    }
                    //Console.WriteLine($"Mouse Device ID: {header.hDevice} | Right button up");
                }
                if ((mouse.usButtonFlags & RI_MOUSE_MIDDLE_BUTTON_DOWN) != 0) // 中键按下
                {
                    if (header.hDevice == firstMouseDevice)
                    {
                        firstMouseStatus.middleButtonDown = true;
                        firstMouseStatus.middleButton = true;
                    }
                    else
                    {
                        secondMouseStatus.middleButtonDown = true;
                        secondMouseStatus.middleButton = true;
                    }
                    //Console.WriteLine($"Mouse Device ID: {header.hDevice} | Middle button down");
                }
                if ((mouse.usButtonFlags & RI_MOUSE_MIDDLE_BUTTON_UP) != 0) // 中键松开
                {
                    if (header.hDevice == firstMouseDevice)
                    {
                        firstMouseStatus.middleButtonUp = true;
                        firstMouseStatus.middleButton = false;
                    }
                    else
                    {
                        secondMouseStatus.middleButtonUp = true;
                        secondMouseStatus.middleButton = false;
                    }
                    //Console.WriteLine($"Mouse Device ID: {header.hDevice} | Middle button up");
                }
                if ((mouse.usButtonFlags & RI_MOUSE_BUTTON_4_DOWN) != 0) // x1键按下
                {
                    if (header.hDevice == firstMouseDevice)
                    {
                        firstMouseStatus.x1ButtonDown = true;
                        firstMouseStatus.x1Button = true;
                    }
                    else
                    {
                        secondMouseStatus.x1ButtonDown = true;
                        secondMouseStatus.x1Button = true;
                    }
                    //Console.WriteLine($"Mouse Device ID: {header.hDevice} | Middle button down");
                }
                if ((mouse.usButtonFlags & RI_MOUSE_BUTTON_4_UP) != 0) // x1键松开
                {
                    if (header.hDevice == firstMouseDevice)
                    {
                        firstMouseStatus.x1ButtonUp = true;
                        firstMouseStatus.x1Button = false;
                    }
                    else
                    {
                        secondMouseStatus.x1ButtonUp = true;
                        secondMouseStatus.x1Button = false;
                    }
                    //Console.WriteLine($"Mouse Device ID: {header.hDevice} | Middle button up");
                }
                if ((mouse.usButtonFlags & RI_MOUSE_BUTTON_5_DOWN) != 0) // x2键按下
                {
                    if (header.hDevice == firstMouseDevice)
                    {
                        firstMouseStatus.x2ButtonDown = true;
                        firstMouseStatus.x2Button = true;
                    }
                    else
                    {
                        secondMouseStatus.x2ButtonDown = true;
                        secondMouseStatus.x2Button = true;
                    }
                    //Console.WriteLine($"Mouse Device ID: {header.hDevice} | Middle button down");
                }
                if ((mouse.usButtonFlags & RI_MOUSE_BUTTON_5_UP) != 0) // x2键松开
                {
                    if (header.hDevice == firstMouseDevice)
                    {
                        firstMouseStatus.x2ButtonUp = true;
                        firstMouseStatus.x2Button = false;
                    }
                    else
                    {
                        secondMouseStatus.x2ButtonUp = true;
                        secondMouseStatus.x2Button = false;
                    }
                    //Console.WriteLine($"Mouse Device ID: {header.hDevice} | Middle button up");
                }

                if ((mouse.usButtonFlags & RI_MOUSE_WHEEL) != 0) // 垂直滚轮
                {
                    if (header.hDevice == firstMouseDevice)
                    {
                        firstMouseStatus.wheel = mouse.usButtonData;
                    }
                    else
                    {
                        secondMouseStatus.wheel = mouse.usButtonData;
                    }
                    //Console.WriteLine($"Mouse Device ID: {header.hDevice} | Vertical Wheel Scrolled: {mouse.usButtonData}");
                }
            }
            else if (header.dwType == 1)
            {
                IntPtr keyboardDataPtr = new IntPtr(rawBuffer.ToInt64() + Marshal.SizeOf(typeof(RAWINPUTHEADER)));
                RAWKEYBOARD keyboard = (RAWKEYBOARD)Marshal.PtrToStructure(keyboardDataPtr, typeof(RAWKEYBOARD));

                if (firstKeyboardDevice == IntPtr.Zero)
                {
                    firstKeyboardDevice = header.hDevice;
                }


                if (KeyCodeMaps.KeysToKeyCodeMap.TryGetValue((KeyCodeMaps.Keys)keyboard.VKey, out KeyCode keycode))
                {
                    if ((keyboard.Flags & 0x0001) == 0)
                    {
                        if (header.hDevice == firstKeyboardDevice)
                        {
                            if (!firstKeyboardPressedKeys.Contains(keycode))
                            {
                                //Console.WriteLine($"Key Down: {keyboard.VKey}, MakeCode: {keyboard.MakeCode}");
                                firstKeyboardCurrentFrameKeysDown.Add(keycode);
                                firstKeyboardPressedKeys.Add(keycode);
                            }
                        }
                        else
                        {
                            if (!secondKeyboardPressedKeys.Contains(keycode))
                            {
                                //Console.WriteLine($"Key Down: {keyboard.VKey}, MakeCode: {keyboard.MakeCode}");
                                secondKeyboardCurrentFrameKeysDown.Add(keycode);
                                secondKeyboardPressedKeys.Add(keycode);
                            }
                        }
                    }
                    else if ((keyboard.Flags & 0x0001) == 1)
                    {
                        if (header.hDevice == firstKeyboardDevice)
                        {
                            //Console.WriteLine($"Key Up: {keyboard.VKey}, MakeCode: {keyboard.MakeCode}");
                            firstKeyboardPressedKeys.Remove(keycode);
                            firstKeyboardCurrentFrameKeysUp.Add(keycode);
                        }
                        else
                        {
                            //Console.WriteLine($"Key Up: {keyboard.VKey}, MakeCode: {keyboard.MakeCode}");
                            secondKeyboardPressedKeys.Remove(keycode);
                            secondKeyboardCurrentFrameKeysUp.Add(keycode);
                        }
                    }
                }
            }
        }

        public static bool GetKey(KeyCode key, bool isFirstKeyboard)
        {
            if (isFirstKeyboard)
                return firstKeyboardPressedKeys.Contains(key);
            else
                return secondKeyboardPressedKeys.Contains(key);
        }

        public static bool GetIsPressed(KeyInfo self, bool isFirstKeyboard)
        {
            int num = self.keyCodes.Length;
            for (int i = 0; i < num; i++)
            {
                if (GetKey(self.keyCodes[i], isFirstKeyboard))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool GetIsPressed(KeyCombo self, bool isFirstKeyboard)
        {
            if (self.includeSize == 0)
            {
                return false;
            }
            bool flag = true;
            for (int i = 0; i < self.includeSize; i++)
            {
                int includeInt = self.GetIncludeInt(i);
                flag = (flag && GetIsPressed(KeyInfo.KeyList[includeInt], isFirstKeyboard));
            }
            for (int j = 0; j < self.excludeSize; j++)
            {
                int excludeInt = self.GetExcludeInt(j);
                if (KeyInfo.KeyList[excludeInt].IsPressed)
                {
                    return false;
                }
            }
            return flag;
        }

        public static bool GetState(KeyBindingSource self, InputDevice inputDevice, bool isFirstKeyboard)
        {
            return GetIsPressed(self.Control, isFirstKeyboard);
        }

        public static float GetValue(KeyBindingSource self, InputDevice inputDevice, bool isFirstKeyboard)
        {
            return (!GetState(self, inputDevice, isFirstKeyboard)) ? 0f : 1f;
        }

        public static bool IsFirstKeyboard(PlayerAction action)
        {
            return action.Owner == firstKeyboardActionSet;
        }

        public static float GetValue(MouseBindingSource self, InputDevice inputDevice, bool isFirstMouse)
        {
            int num = MouseBindingSource.buttonTable[(int)self.Control];
            if (num >= 0)
            {
                return (!GetMouseButton(num, isFirstMouse)) ? 0f : 1f;
            }
            switch (self.Control)
            {
                case Mouse.NegativeX:
                    return -Mathf.Min(GetRawX(isFirstMouse) * MouseBindingSource.ScaleX, 0f);
                case Mouse.PositiveX:
                    return Mathf.Max(0f, GetRawX(isFirstMouse) * MouseBindingSource.ScaleX);
                case Mouse.NegativeY:
                    return -Mathf.Min(GetRawY(isFirstMouse) * MouseBindingSource.ScaleY, 0f);
                case Mouse.PositiveY:
                    return Mathf.Max(0f, GetRawY(isFirstMouse) * MouseBindingSource.ScaleY);
                case Mouse.PositiveScrollWheel:
                    return Mathf.Max(0f, GetWheel(isFirstMouse) * MouseBindingSource.ScaleZ);
                case Mouse.NegativeScrollWheel:
                    return -Mathf.Min(GetWheel(isFirstMouse) * MouseBindingSource.ScaleZ, 0f);
                default:
                    return 0f;
            }
        }

        public static float GetValue(MouseBindingSource self, InputDevice inputDevice)
        {
            int num = MouseBindingSource.buttonTable[(int)self.Control];
            if (num >= 0)
            {
                return (!MouseBindingSource.SafeGetMouseButton(num)) ? 0f : 1f;
            }
            switch (self.Control)
            {
                case Mouse.NegativeX:
                    return -Mathf.Min(GetPublicRawX() * MouseBindingSource.ScaleX, 0f);
                case Mouse.PositiveX:
                    return Mathf.Max(0f, GetPublicRawX() * MouseBindingSource.ScaleX);
                case Mouse.NegativeY:
                    return -Mathf.Min(GetPublicRawY() * MouseBindingSource.ScaleY, 0f);
                case Mouse.PositiveY:
                    return Mathf.Max(0f, GetPublicRawY() * MouseBindingSource.ScaleY);
                case Mouse.PositiveScrollWheel:
                    return Mathf.Max(0f, GetPublicWheel() * MouseBindingSource.ScaleZ);
                case Mouse.NegativeScrollWheel:
                    return -Mathf.Min(GetPublicWheel() * MouseBindingSource.ScaleZ, 0f);
                default:
                    return 0f;
            }
        }

        private static bool GetMouseButton(int button, bool isFirstMouse)
        {
            if (isFirstMouse)
            {
                switch (button)
                {
                    case 0:
                        return firstMouseStatus.leftButton;
                    case 1:
                        return firstMouseStatus.rightButton;
                    case 2:
                        return firstMouseStatus.middleButton;
                    case 3:
                        return firstMouseStatus.x1Button;
                    case 4:
                        return firstMouseStatus.x2Button;
                    default:
                        return false;
                }
            }
            else
            {
                switch (button)
                {
                    case 0:
                        return secondMouseStatus.leftButton;
                    case 1:
                        return secondMouseStatus.rightButton;
                    case 2:
                        return secondMouseStatus.middleButton;
                    case 3:
                        return secondMouseStatus.x1Button;
                    case 4:
                        return secondMouseStatus.x2Button;
                    default:
                        return false;
                }
            }
        }

        private static int GetRawX(bool isFirstMouse)
        {
            if (isFirstMouse)
                return firstMouseStatus.rawX;
            else
                return secondMouseStatus.rawX;
        }

        private static int GetRawY(bool isFirstMouse)
        {
            if (isFirstMouse)
                return firstMouseStatus.rawY;
            else
                return secondMouseStatus.rawY;
        }

        private static int GetWheel(bool isFirstMouse)
        {
            if (isFirstMouse)
                return firstMouseStatus.wheel;
            else
                return secondMouseStatus.wheel;
        }

        public static bool IsFirstMouse(PlayerAction action)
        {
            return action.Owner == firstMouseActionSet;
        }

        private static int GetPublicRawX()
        {
            if (firstMouseStatus.rawX != 0)
                return firstMouseStatus.rawX;
            else if (secondMouseStatus.rawX != 0)
                return secondMouseStatus.rawX;
            else
                return 0;
        }

        private static int GetPublicRawY()
        {
            if (firstMouseStatus.rawY != 0)
                return firstMouseStatus.rawY;
            else if (secondMouseStatus.rawY != 0)
                return secondMouseStatus.rawY;
            else
                return 0;
        }

        public static int GetPublicWheel()
        {
            if (firstMouseStatus.wheel != 0)
                return firstMouseStatus.wheel;
            else if (secondMouseStatus.wheel != 0)
                return secondMouseStatus.wheel;
            else
                return 0;
        }

        public static float GetPublicSmoothWheelValue()
        {
            if (firstMouseStatus.wheel != 0)
                return firstMouseSmoothWheel.GetSmoothWheelValue(firstMouseStatus.wheel);
            else if (secondMouseStatus.wheel != 0)
                return secondMouseSmoothWheel.GetSmoothWheelValue(secondMouseStatus.wheel);
            else
                return 0;
        }

        public static bool ShowTwoMouseCursor
        {
            get
            {
                if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
                    return false;
                if (GameCursorController.CursorOverride.Value)
                    return false;
                if (GameManager.Instance.IsLoadingLevel)
                    return false;
                if (GameManager.IsReturningToBreach)
                    return false;
                if (GameManager.Instance.IsPaused)
                    return false;
                if (Minimap.Instance.IsFullscreen)
                    return false;
                if (!OptionsManager.restrictMouseInputPort)
                    return false;
                return true;
            }
        }

        public static bool ShowPublicCursor
        {
            get
            {
                if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
                    return false;
                if (!GameCursorController.showMouseCursor)
                    return false;
                if (GameManager.Instance.IsPaused)
                    return true;
                if (Minimap.Instance.IsFullscreen)
                    return true;
                return false;
            }
        }

        public static bool ShowPlayerOneMouseCursor
        {
            get
            {
                if (!BraveInput.HasInstanceForPlayer(0))
                    return false;
                return BraveInput.GetInstanceForPlayer(0).IsKeyboardAndMouse(false);
            }
        }

        public static bool ShowPlayerTwoMouseCursor
        {
            get
            {
                if (!BraveInput.HasInstanceForPlayer(1))
                    return false;
                return BraveInput.GetInstanceForPlayer(1).IsKeyboardAndMouse(false);
            }
        }
    }
}