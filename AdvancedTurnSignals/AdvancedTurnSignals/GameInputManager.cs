using GTA;
using GTA.UI;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Control = GTA.Control;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace AdvancedTurnSignals
{
    public class GameInputManager
    {
        private enum KeyBinds
        {
            KeyLeft,
            KeyLeftMod,
            KeyRight,
            KeyRightMod,
            KeyHazard,
            KeyHazardMod
        }

        private enum ButtonBinds
        {
            ButtonLeft,
            ButtonLeftMod,
            ButtonRight,
            ButtonRightMod,
            ButtonHazard,
            ButtonHazardMod
        }

        private Dictionary<string, Control> AvailableButtons = new Dictionary<string, Control>
        {
            {"LB", Control.ScriptLB},
            { "LS", Control.ScriptLS },
            {"LT", Control.ScriptLT },
            {"PadDown", Control.ScriptPadDown },
            {"PadLeft", Control.ScriptPadLeft },
            {"PadRight", Control.ScriptPadRight },
            {"PadUp", Control.ScriptPadUp },
            {"RB", Control.ScriptRB },
            {"RS", Control.ScriptRS },
            {"RT", Control.ScriptRT },
            {"A", Control.ScriptRDown },
            {"B", Control.ScriptRRight },
            {"Y", Control.ScriptRUp },
            {"X", Control.ScriptRLeft },
            {"Select", Control.ScriptSelect }
        };

        // readonly = 再代入不可。一度設定（代入）したらそのあとは代入できない。
        // ※今回の例ではHashSetを代入している。この先ActiveKeys = ○○ とはできないが、ActiveKeys.Addなど、HashSetに対しての操作はできる。
        // Android Studioで使われているKotlinのvalと同様の挙動だと思っていい。
        // ちなみに、定数であるConstは宣言と同時に固定値を指定したらあとは書き換え不可。
        // プロパティやメソッドを使用して宣言もConstではできない。その場合はReadonlyを使うと、最初だけ何でも入れられるので、あとから変えられて事故が起きるのを防ぐ…という場合に便利。
        private readonly SignalController signalController;
        public readonly HashSet<Keys> ActiveKeys = new HashSet<Keys>();
        public readonly HashSet<Control> ActiveControls = new HashSet<Control>();
        private Dictionary<KeyBinds, Keys> KeyboardSettings = new Dictionary<KeyBinds, Keys>()
        {
            {KeyBinds.KeyLeft,  Keys.J},
            {KeyBinds.KeyRight,  Keys.K},
            {KeyBinds.KeyHazard,  Keys.I},
            {KeyBinds.KeyLeftMod,  Keys.None},
            {KeyBinds.KeyRightMod,  Keys.None},
            {KeyBinds.KeyHazardMod,  Keys.None}
        };
        private Dictionary<ButtonBinds, Control?> JoyPadSettings = new Dictionary<ButtonBinds, Control?>()
        {
            {ButtonBinds.ButtonLeft,null },
            {ButtonBinds.ButtonRight,null },
            {ButtonBinds.ButtonHazard,null },
            {ButtonBinds.ButtonLeftMod,null },
            {ButtonBinds.ButtonRightMod,null },
            {ButtonBinds.ButtonHazardMod,null }
        };

        private CancellationTokenSource DigitalAutoOffCanceller; //キーボード互換モードでのAutoOffやり直し用
        private readonly bool ScriptEnabled; //1回限りの代入
        public readonly bool KeyboardCompatibility; //1回限りの代入
        private bool ControllerInterLock; //コントローラー使用時のキーボード操作制限

        public GameInputManager(SignalController controller)
        {
            signalController = controller;
            ScriptEnabled = Utils.GetSettings(BoolSettingsItem.ScriptEnabled);
            KeyboardCompatibility = Utils.GetSettings(BoolSettingsItem.KeyBoardCompatible);
            ControllerInterLock = false;

            KeybindLoad();
        }

        /// <summary>
        /// キーボード設定をiniから読み込みます。
        /// </summary>
        public void KeybindLoad()
        {
            // ローカル関数。KeybindLoadメソッド内でしか使えない。
            Control? ConvertStrToCtrl(string BindName, string ctrl, string default_bind)
            {
                // 未指定の場合はNull。
                if (ctrl.ToLower() == "none")
                {
                    return null;
                }

                if (!AvailableButtons.ContainsKey(ctrl))
                {
                    string message = Lang.GetLangData(LangKeys.button_warn);
                    message = message.Replace("{0}", BindName); //特定部分を変数の値に置き換える
                    message = message.Replace("{1}", default_bind);

                    Utils.ShowNotification(NotificationIcon.Blocked, NotificationClass.warning, message);
                    return AvailableButtons[default_bind];
                }
                return AvailableButtons[ctrl];
            }

            ScriptSettings ini = ScriptSettings.Load(@"scripts\AdvancedTurnSignals.ini"); //INI File
            // iniのデータを読み込む (セクション、キー、デフォルト値)
            KeyboardSettings[KeyBinds.KeyLeft] = ini.GetValue("Keys", "Left", Keys.J);
            KeyboardSettings[KeyBinds.KeyRight] = ini.GetValue("Keys", "Right", Keys.K);
            KeyboardSettings[KeyBinds.KeyHazard] = ini.GetValue("Keys", "Hazard", Keys.I);
            KeyboardSettings[KeyBinds.KeyLeftMod] = ini.GetValue("Keys", "LeftModifier", Keys.None);
            KeyboardSettings[KeyBinds.KeyRightMod] = ini.GetValue("Keys", "RightModifier", Keys.None);
            KeyboardSettings[KeyBinds.KeyHazardMod] = ini.GetValue("Keys", "HazardModifier", Keys.None);

            JoyPadSettings[ButtonBinds.ButtonLeft] = ConvertStrToCtrl("Left", ini.GetValue("Buttons", "Left", "LB"), "LB");
            JoyPadSettings[ButtonBinds.ButtonRight] = ConvertStrToCtrl("Right", ini.GetValue("Buttons", "Right", "RB"), "RB");
            JoyPadSettings[ButtonBinds.ButtonHazard] = ConvertStrToCtrl("Hazard", ini.GetValue("Buttons", "Hazard", "PadUp"), "PadUp");
            JoyPadSettings[ButtonBinds.ButtonLeftMod] = ConvertStrToCtrl("LeftModifier", ini.GetValue("Buttons", "LeftModifier", "None"), "None");
            JoyPadSettings[ButtonBinds.ButtonRightMod] = ConvertStrToCtrl("RightModifier", ini.GetValue("Buttons", "RightModifier", "None"), "None");
            JoyPadSettings[ButtonBinds.ButtonHazardMod] = ConvertStrToCtrl("HazardModifier", ini.GetValue("Buttons", "HazardModifier", "None"), "None");

        }

        #region User32.dll


        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey); //仮想キーコードからのキー状態取得メソッド
        private void LRkeyUp()
        {
            #region ShiftKey
            if (GetKeyState(0xA0) >= 0 && ActiveKeys.Contains(Keys.LShiftKey)) // LShiftKey
            {
                ActiveKeys.Remove(Keys.LShiftKey);
            }
            if (GetKeyState(0xA1) >= 0 && ActiveKeys.Contains(Keys.RShiftKey)) // RShiftKey
            {
                ActiveKeys.Remove(Keys.RShiftKey);
            }
            #endregion
            #region ControlKey
            if (GetKeyState(0xA2) >= 0 && ActiveKeys.Contains(Keys.LControlKey)) // LControlKey
            {
                ActiveKeys.Remove(Keys.LControlKey);
            }
            if (GetKeyState(0xA3) >= 0 && ActiveKeys.Contains(Keys.RControlKey)) // RControlKey
            {
                ActiveKeys.Remove(Keys.RControlKey);
            }
            #endregion
            #region AltKey
            if (GetKeyState(0xA4) >= 0 && ActiveKeys.Contains(Keys.LMenu)) // LMenu
            {
                ActiveKeys.Remove(Keys.LMenu);
            }
            if (GetKeyState(0xA5) >= 0 && ActiveKeys.Contains(Keys.RMenu)) // RMenu
            {
                ActiveKeys.Remove(Keys.RMenu);
            }
            #endregion

        }

        private void LRkeyDown()
        {
            #region ShiftKey
            if (GetKeyState(0xA0) < 0 && !ActiveKeys.Contains(Keys.LShiftKey)) // LShiftKey
            {
                ActiveKeys.Add(Keys.LShiftKey);
            }
            if (GetKeyState(0xA1) < 0 && !ActiveKeys.Contains(Keys.RShiftKey)) // RShiftKey
            {
                ActiveKeys.Add(Keys.RShiftKey);
            }
            #endregion
            #region ControlKey
            if (GetKeyState(0xA2) < 0 && !ActiveKeys.Contains(Keys.LControlKey)) // LControlKey
            {
                ActiveKeys.Add(Keys.LControlKey);
            }
            if (GetKeyState(0xA3) < 0 && !ActiveKeys.Contains(Keys.RControlKey)) // RControlKey
            {
                ActiveKeys.Add(Keys.RControlKey);
            }
            #endregion
            #region AltKey
            if (GetKeyState(0xA4) < 0 && !ActiveKeys.Contains(Keys.LMenu)) // LMenu
            {
                ActiveKeys.Add(Keys.LMenu);
            }
            if (GetKeyState(0xA5) < 0 && !ActiveKeys.Contains(Keys.RMenu)) // RMenu
            {
                ActiveKeys.Add(Keys.RMenu);
            }
            #endregion

        }

        #endregion

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Notification.Show("OnKeyDown!: " + e.KeyCode);

            // Ctrl, Alt ,Shiftの場合 (下でelseを使わない理由はLR指定をしていない場合の対策。LRタイプと共通タイプの2種類が一度に追加されてしまうが、まあ仕方ない。)
            if (new Keys[] { Keys.ControlKey, Keys.ShiftKey, Keys.Menu }.Contains(e.KeyCode))
            {
                LRkeyDown();
            }
            if (e.KeyCode != Keys.Escape && !ActiveKeys.Contains(e.KeyCode)) //ESCはゲームがポーズして押しっぱなし判定になってしまうため除外
            {
                ActiveKeys.Add(e.KeyCode);
                // Notification.Show("KeyAdd!: " + e.KeyCode);
            }

            Vehicle cv = Utils.GetCurrentVehicle();
            if (!ScriptEnabled || cv == null || !Utils.IsIndicatorAvailable(cv)) return;

            Keys KeyL = KeyboardSettings[KeyBinds.KeyLeft];
            Keys KeyR = KeyboardSettings[KeyBinds.KeyRight];
            Keys KeyH = KeyboardSettings[KeyBinds.KeyHazard];
            Keys KeyLm = KeyboardSettings[KeyBinds.KeyLeftMod];
            Keys KeyRm = KeyboardSettings[KeyBinds.KeyRightMod];
            Keys KeyHm = KeyboardSettings[KeyBinds.KeyHazardMod];


            if (new Keys[] { KeyL, KeyR, KeyH }.Contains(e.KeyCode)) //有効かつ車に乗っているかつ設定したキーバインドが押されている
            {
                //修飾キーが押されているか（ない場合は普通に実行）
                if (ActiveKeys.Contains(KeyL) && KeyLm == Keys.None | ActiveKeys.Contains(KeyLm))
                {
                    signalController.Activate(cv, IndicatorState.Left,Utils.IsBike(cv));
                }
                else if (ActiveKeys.Contains(KeyR) && KeyRm == Keys.None | ActiveKeys.Contains(KeyRm))
                {
                    signalController.Activate(cv, IndicatorState.Right, Utils.IsBike(cv));
                }
                else if (ActiveKeys.Contains(KeyH) && KeyHm == Keys.None | ActiveKeys.Contains(KeyHm))
                {
                    signalController.Activate(cv, IndicatorState.Hazard, Utils.IsBike(cv));
                }
            }
        }

        public void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (ActiveKeys.Contains(e.KeyCode)) ActiveKeys.Remove(e.KeyCode);
            // Ctrl, Alt ,Shiftの場合 (下でelseを使わない理由はLR指定をしていない場合の対策)
            if (new Keys[] { Keys.ControlKey, Keys.ShiftKey, Keys.Menu }.Contains(e.KeyCode))
            {
                LRkeyUp();
            }

            Vehicle cv = Utils.GetCurrentVehicle();
            if (!ScriptEnabled || cv == null) return; //スクリプト無効、または車両に乗っていない
            if (!Utils.IsIndicatorAvailable(cv) || signalController.currentState == IndicatorState.Off) return; //方向指示器が使えない車両、または方向指示器がOFF

            // キーボード互換ON、AutoOff作動可能、バイク以外、左または右ウインカー有効時
            if (KeyboardCompatibility && signalController.Off_OK && cv.ClassType != VehicleClass.Motorcycles && signalController.currentState != IndicatorState.Hazard)
            {
                DigitalAutoOffCanceller?.Cancel();
                DigitalAutoOffCanceller = new CancellationTokenSource();
                Task.Run(() => { signalController.digital_AutoOff(DigitalAutoOffCanceller.Token); });
            }

        }

        // ＝＝＝＝＝＝＝＝＝＝コントローラー関係＝＝＝＝＝＝＝＝＝＝

        /// <summary>
        /// コントローラー用制御メソッド 
        /// </summary>
        /// <param name="cv">制御対象車両</param>
        public void controller(Vehicle cv)
        {
            // ※シグコンのTick用。UseButtonがTrueでないとこのメソッドは呼ばれない。
            #region コントローラリスナ_V2.1
            // ローカル関数。Controllerメソッド内でのみ使用可能。
            void CheckActiveControls(Control? c) //Ver2.0～ 統合
            {
                if (!c.HasValue) return; //Nullチェック
                bool isPressed = Game.IsControlPressed(c.Value);
                if (isPressed && !ActiveControls.Contains(c.Value))
                {
                    ActiveControls.Add(c.Value);
                }
                else if (!isPressed && ActiveControls.Contains(c.Value))
                {
                    ActiveControls.Remove(c.Value);
                }
            }
            bool ModifierState(Control? c)
            {
                // Ver1.2.4修正：!= nullからHasValueプロパティによる比較方式に変更。
                // Ver2.0～ : ローカル関数化
                if (!c.HasValue) return true; //Modifier未設定の場合はTrue
                return ActiveControls.Contains(c.Value); //設定時はActiveControlsに含まれているかどうかを返す
            }

            //まずはメインボタンから。
            CheckActiveControls(JoyPadSettings[ButtonBinds.ButtonLeft]);
            CheckActiveControls(JoyPadSettings[ButtonBinds.ButtonRight]);
            CheckActiveControls(JoyPadSettings[ButtonBinds.ButtonHazard]);
            //Modifier
            CheckActiveControls(JoyPadSettings[ButtonBinds.ButtonLeftMod]);
            CheckActiveControls(JoyPadSettings[ButtonBinds.ButtonRightMod]);
            CheckActiveControls(JoyPadSettings[ButtonBinds.ButtonHazardMod]);

            #endregion

            if (ActiveControls.Count == 0 && ControllerInterLock) //指定されているコントローラーボタンがすべて離されたら ※暴走抑止用
            {
                ControllerInterLock = false; //再度ウインカー操作が可能になる
            }

            //有効かつ方向指示器が動作できる車に乗っており、キーが押されていない、また、指定したコントローラーのボタンがすべて一度離されている
            if (ScriptEnabled && cv != null && Utils.IsIndicatorAvailable(cv) && ActiveKeys.Count == 0 && !ControllerInterLock)
            {
                // ModifierButtonを設定している場合は、ActiveControlsに含まれているかどうかをBoolに入れる。
                bool leftCMstate = ModifierState(JoyPadSettings[ButtonBinds.ButtonLeftMod]);
                bool rightCMstate = ModifierState(JoyPadSettings[ButtonBinds.ButtonLeftMod]);
                bool hazardCMstate = ModifierState(JoyPadSettings[ButtonBinds.ButtonLeftMod]);

                // 正直メインボタンはNullにならない（読み込み時に設定が不正の場合はデフォルト値が入るため）
                Control BtnL = JoyPadSettings[ButtonBinds.ButtonLeft].Value;
                Control BtnR = JoyPadSettings[ButtonBinds.ButtonRight].Value;
                Control BtnH = JoyPadSettings[ButtonBinds.ButtonHazard].Value;

                // 動作
                if (ActiveControls.Contains(BtnL) && leftCMstate)
                {
                    ControllerInterLock = true; //インターロックON
                    signalController.Activate(cv, IndicatorState.Left, Utils.IsBike(cv));
                }
                else if (ActiveControls.Contains(BtnR) && rightCMstate)
                {
                    ControllerInterLock = true; //インターロックON
                    signalController.Activate(cv, IndicatorState.Right, Utils.IsBike(cv));
                }
                else if (ActiveControls.Contains(BtnH) && hazardCMstate)
                {
                    ControllerInterLock = true; //インターロックON
                    signalController.Activate(cv, IndicatorState.Hazard, Utils.IsBike(cv));
                }
            }

        }
    }
}