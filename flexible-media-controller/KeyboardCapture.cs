using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace flexible_media_controller
{
    class KeyboardCapture
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int REPETITION = 0x1E;
        private const int WH_KEYBOARD_LL = 13;

        private LowLevelKeyboardProc _hookProc;
        private IntPtr _hookID = IntPtr.Zero;
        private Dictionary<VirtualKey, bool> keyStatus;

        public List<KeyValuePair<SortedSet<VirtualKey>, KeyCapturedProc>> Combinations { get; }

        public delegate void KeyCapturedProc();
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam,
                                                     IntPtr lParam);
        public KeyboardCapture()
        {
            _hookProc = HookCallback;
            keyStatus = new Dictionary<VirtualKey, bool>();
            Array keys = Enum.GetValues(typeof(VirtualKey));
            foreach (VirtualKey key in keys)
            {
                try
                {
                    keyStatus.Add(key, false);
                }
                catch (System.ArgumentException ex)
                { //Skip repetitions in VirtualKey Enum
                }
            }
            Combinations = new List<KeyValuePair<SortedSet<VirtualKey>,
                                                 KeyCapturedProc>>();
            _hookID = SetHook(_hookProc);
        }

        ~KeyboardCapture()
        {
            UnhookWindowsHookEx(_hookID);
        }

        #region imports
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);
        #endregion

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule currentModule = currentProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(currentModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (Enum.IsDefined(typeof(VirtualKey), vkCode))
                {
                    keyStatus[(VirtualKey)vkCode] = true;
                    ProcessKey(Combinations, keyStatus);
                }
            }
            else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (Enum.IsDefined(typeof(VirtualKey), vkCode))
                    keyStatus[(VirtualKey)vkCode] = false;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private async void ProcessKey(List<KeyValuePair<SortedSet<VirtualKey>,
            KeyCapturedProc>> list, Dictionary<VirtualKey, bool> status)
        {
            await Task.Run(() =>
            {
                foreach (var comb in list)
                {
                    bool combinationDown = true;
                    foreach (var key in comb.Key)
                    {
                        if (!status[key])    //If key isn't down
                        {
                            combinationDown = false;
                            break;
                        }
                    }
                    if (combinationDown)
                        comb.Value();
                }
            });
        }

        public bool AddCombination(SortedSet<VirtualKey> combination,
                                   KeyCapturedProc handler)
        {
            if (CombinationExists(combination)) return false;
            var pair = new KeyValuePair<SortedSet<VirtualKey>,
                                        KeyCapturedProc>(combination, handler);
            Combinations.Add(pair);
            return true;
        }

        public void ClearCombinations()
        {
            Combinations.Clear();
        }

        private bool CombinationExists(SortedSet<VirtualKey> combination)
        {
            var comparer = SortedSet<VirtualKey>.CreateSetComparer();
            foreach (var c in Combinations)
                if (comparer.Equals(combination, c.Key)) return true;
            return false;
        }
    }
}
