using GTA;
using GTA.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedTurnSignals
{
    // public enum IndicatorState { Off, Left, Right, Hazard }

    public enum NotificationClass
    {
        information,
        warning,
        error
    }

    public enum BoolSettingsItem
    {
        ScriptEnabled,
        UseSound,
        DefaultSoundEnabled,
        UseHeadlight,
        UseControllerButton,
        AutoOffEnabled,
        KeyBoardCompatible,
        AutoOnEnabled,
        AutoOnSoundEnabled
    }

    public enum FloatSettingsItem
    {
        ReadyAngleValue,
        OffAngleValue,
        KBCoffTimeValue,
        OnAngleValue,
        OnSpeedValue
    }

    internal class Utils
    {
        public static bool game_pause;
        public static bool pause_interlock;
        public static DateTime lastTickTime;
        private static readonly TimeSpan pauseThreshold = TimeSpan.FromMilliseconds(1000); //ポーズしていたと見なす時間
        // private static IndicatorState currentState = IndicatorState.Off;
        // private static IndicatorState TurnSignalState = IndicatorState.Off;

        private static Dictionary<BoolSettingsItem, bool> BooleanSettings = new Dictionary<BoolSettingsItem, bool>() {
            { BoolSettingsItem.ScriptEnabled, true },
            { BoolSettingsItem.UseSound, true },
            { BoolSettingsItem.DefaultSoundEnabled, true },
            { BoolSettingsItem.UseHeadlight, true },
            { BoolSettingsItem.UseControllerButton, true },
            { BoolSettingsItem.AutoOffEnabled, true },
            { BoolSettingsItem.KeyBoardCompatible, true },
            { BoolSettingsItem.AutoOnEnabled, true },
            { BoolSettingsItem.AutoOnSoundEnabled, true }
        };
        private static Dictionary<FloatSettingsItem, float> ValueSettings = new Dictionary<FloatSettingsItem, float>()
        {
            {FloatSettingsItem.ReadyAngleValue, 0f},
            {FloatSettingsItem.OffAngleValue, 0f},
            {FloatSettingsItem.KBCoffTimeValue ,0f},
            {FloatSettingsItem.OnAngleValue, 0f},
            {FloatSettingsItem.OnSpeedValue, 0f},
        };

        /// <summary>
        /// AdvancedTurnSignals.iniを読み込みます。
        /// </summary>
        public static void SettingsLoad()
        {
            ScriptSettings ini = ScriptSettings.Load(@"scripts\AdvancedTurnSignals.ini"); //INI File
            string SettingsErrorMessage;
            NotificationIcon SettingsErrorIcon = NotificationIcon.Blocked; //禁止アイコン

            // General
            bool enabled = ini.GetValue("General", "Enabled", true); //有効または無効
            bool use_sound = ini.GetValue("General", "UseSound", true);
            bool inter_sound = ini.GetValue("General", "InterSound", true); //カスタムサウンドロード失敗時にデフォルトサウンドを使用するかのフラグ
            bool use_headlight = ini.GetValue("General", "HeadlightMode", false); //ウインカーランプの代わりにヘッドライトを使用するかのフラグ
            bool use_button = ini.GetValue("Buttons", "UseButton", false);
            bool use_sound_AutoOn = ini.GetValue("AutoOn", "UseLeverSound", true);

            #region AutoOff
            bool AutoOff = ini.GetValue("AutoOff", "AutoOff", false); //自動消灯
            bool keyboard_comp = ini.GetValue("AutoOff", "KeyboardComp", false); //キーボード互換モード

            float ready_angle = 15f; //自動消灯に最低限必要な角度(コード内で使用)
            float off_angle = 10f; //自動消灯する角度(コード内で使用)
            float AutoOff_duration = 1000f; //キーを離してから方向指示器が自動で消えるまでの時間
            if (AutoOff)
            {
                // 読み込み結果のチェック
                if (!ini.TryGetValue("AutoOff", "ReadyAngle", out ready_angle)) //正しい値でない場合
                {
                    string raw_ready_angle = ini.GetValue("AutoOff", "ReadyAngle", "15"); //iniに書かれていた内容を読み取る
                    SettingsErrorMessage = Lang.GetLangData(LangKeys.num_warn);
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{0}", raw_ready_angle); //特定部分を変数の値に置き換える ※問題箇所
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{1}", "ReadyAngle"); //原因部分を設定項目に置き換える ※該当設定項目
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{2}", ready_angle.ToString()); //特定部分を変数の値に置き換える ※デフォルト値

                    ShowNotification(SettingsErrorIcon, NotificationClass.warning, SettingsErrorMessage);
                }

                if (!ini.TryGetValue("AutoOff", "OffAngle", out off_angle)) //正しい値でない場合
                {
                    string raw_off_angle = ini.GetValue("AutoOff", "OffAngle", "10"); //iniに書かれていた内容を読み取る
                    SettingsErrorMessage = Lang.GetLangData(LangKeys.num_warn);
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{0}", raw_off_angle); //特定部分を変数の値に置き換える ※問題箇所
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{1}", "OffAngle"); //原因部分を設定項目に置き換える ※該当設定項目
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{2}", off_angle.ToString()); //特定部分を変数の値に置き換える ※デフォルト値

                    ShowNotification(SettingsErrorIcon, NotificationClass.warning, SettingsErrorMessage);
                }

                if (!ini.TryGetValue("AutoOff", "AutoOffDuration", out AutoOff_duration)) //正しい値でない場合
                {

                    string raw_AutoOff_duration = ini.GetValue("AutoOff", "AutoOffDuration", "1000"); //iniに書かれていた内容を読み取る
                    SettingsErrorMessage = Lang.GetLangData(LangKeys.duration_warn);
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{0}", raw_AutoOff_duration); //特定部分を変数の値に置き換える  ※問題箇所

                    ShowNotification(SettingsErrorIcon, NotificationClass.warning, SettingsErrorMessage);
                }

                if (ready_angle < off_angle) //ReadyAngleよりもOffAngleのほうが数値が高い場合
                {
                    string message = Lang.GetLangData(LangKeys.overangle_err);
                    message = message.Replace("{0}", off_angle.ToString()); //特定部分を変数の値に置き換える
                    message = message.Replace("{1}", ready_angle.ToString()); //特定部分を変数の値に置き換える

                    // 骸骨アイコン
                    ShowNotification(NotificationIcon.LesterDeathwish, NotificationClass.warning, message);
                    AutoOff = false; //設定が正しくないためオフにする
                }
            }

            #endregion

            #region AutoOn
            bool AutoOn = ini.GetValue("AutoOn", "AutoOn", false); //自動点灯

            float on_angle = 10; //自動点灯に最低限必要な角度(コード内で使用)
            float on_speed = 10; //自動点灯できる最高速度(コード内で使用)

            if (AutoOn)
            {
                // 読み込み結果のチェック
                if (!ini.TryGetValue("AutoOff", "ReadyAngle", out ready_angle)) //正しい値でない場合
                {
                    string raw_on_angle = ini.GetValue("AutoOn", "OnAngle", "10"); //iniに書かれていた内容を読み取る
                    SettingsErrorMessage = Lang.GetLangData(LangKeys.num_warn);
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{0}", raw_on_angle); //特定部分を変数の値に置き換える ※問題箇所
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{1}", "OnAngle"); //原因部分を設定項目に置き換える ※該当設定項目
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{2}", on_angle.ToString()); //特定部分を変数の値に置き換える ※デフォルト値

                    ShowNotification(SettingsErrorIcon, NotificationClass.warning, SettingsErrorMessage);
                }

                if (!ini.TryGetValue("AutoOff", "OffAngle", out off_angle)) //正しい値でない場合
                {
                    string raw_on_speed = ini.GetValue("AutoOn", "OnSpeed", "10"); //iniに書かれていた内容を読み取る
                    SettingsErrorMessage = Lang.GetLangData(LangKeys.num_warn);
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{0}", raw_on_speed); //特定部分を変数の値に置き換える ※問題箇所
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{1}", "OnSpeed"); //原因部分を設定項目に置き換える ※該当設定項目
                    SettingsErrorMessage = SettingsErrorMessage.Replace("{2}", on_speed.ToString()); //特定部分を変数の値に置き換える ※デフォルト値

                    ShowNotification(SettingsErrorIcon, NotificationClass.warning, SettingsErrorMessage);
                }
            }

            #endregion

            if (AutoOff || AutoOn)
            {
                if (ready_angle > 40 || off_angle > 40 || on_angle > 40) //GTAVの角度40度を超えた設定の場合
                {
                    ShowNotification(NotificationIcon.Blocked, NotificationClass.warning, Lang.GetLangData(LangKeys.overnum_warn));
                }
            }

            // Apply to Dict
            BooleanSettings[BoolSettingsItem.ScriptEnabled] = enabled;
            BooleanSettings[BoolSettingsItem.UseSound] = use_sound;
            BooleanSettings[BoolSettingsItem.DefaultSoundEnabled] = inter_sound;
            BooleanSettings[BoolSettingsItem.UseHeadlight] = use_headlight;
            BooleanSettings[BoolSettingsItem.UseControllerButton] = use_button;
            BooleanSettings[BoolSettingsItem.AutoOffEnabled] = AutoOff;
            BooleanSettings[BoolSettingsItem.KeyBoardCompatible] = keyboard_comp;
            BooleanSettings[BoolSettingsItem.AutoOnEnabled] = AutoOn;
            BooleanSettings[BoolSettingsItem.AutoOnSoundEnabled] = use_sound_AutoOn;

            ValueSettings[FloatSettingsItem.ReadyAngleValue] = ready_angle;
            ValueSettings[FloatSettingsItem.OffAngleValue] = off_angle;
            ValueSettings[FloatSettingsItem.KBCoffTimeValue] = AutoOff_duration;
            ValueSettings[FloatSettingsItem.OnAngleValue] = on_angle;
            ValueSettings[FloatSettingsItem.OnSpeedValue] = on_speed;

        }

        /// <summary>
        /// Bool型の設定値を取得します。
        /// </summary>
        /// <param name="settings">設定項目</param>
        /// <returns>設定内容</returns>
        public static bool GetSettings(BoolSettingsItem settings)
        {
            return BooleanSettings[settings];
        }

        /// <summary>
        /// float型の設定値を取得します。
        /// </summary>
        /// <param name="settings">設定項目</param>
        /// <returns>設定内容</returns>
        public static float GetSettings(FloatSettingsItem settings)
        {
            return ValueSettings[settings];
        }

        public static bool IsBike(Vehicle vehicle)
        {
            return (vehicle.ClassType == VehicleClass.Motorcycles);
        }

        public static bool IsIndicatorAvailable(Vehicle vehicle)
        {
            if (vehicle == null) return false;

            var unsupported = new List<VehicleClass>
            {
                VehicleClass.Boats,
                VehicleClass.Cycles,
                VehicleClass.Helicopters,
                VehicleClass.Planes,
                VehicleClass.Trains
            };
            return !unsupported.Contains(vehicle.ClassType);
        }

        /*
        /// <summary>
        /// [NOTUSE]IndicatorStateを取得、および設定します。
        /// </summary>
        /// <param name="state">設定するIndicatorState。Nullで取得。</param>
        /// <returns>取得、または設定したIndicatorState。</returns>
        public static IndicatorState IndicatorStateHandler(string LogString = "", IndicatorState? state = null, bool isTurnSignalState = false)
        {
            if (state != null)
            {
                if (isTurnSignalState) TurnSignalState = state.Value;
                else currentState = state.Value;
            }

            // DISABLED
            // if (LogString != "") LogSystem.Log(currentState, TurnSignalState, LogString);

            // 三項演算子>>> isTurnSignalStateがTrueなら? TurnSignalStateを、：そうでないならIndicatorStateを Return。
            return isTurnSignalState ? TurnSignalState : currentState;
        }
        */

        /// <summary>
        /// プレイヤーが現在乗車している車両を取得します。
        /// </summary>
        /// <returns>乗車中の車両情報。</returns>
        public static Vehicle GetCurrentVehicle()
        {
            return Game.Player.Character.CurrentVehicle;
        }

        /// <summary>
        /// スクリプトからのメッセージ通知を表示します。
        /// </summary>
        /// <param name="icon">メッセージアイコン。</param>
        /// <param name="cls">メッセージの重要度。</param>
        /// <param name="message">メッセージ内容。</param>
        public static void ShowNotification(NotificationIcon icon, NotificationClass cls, string message)
        {
            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName(); //アセンブリ情報
            string ver = assembly.Version.ToString(3); //x.x.x
            string ClassString = "";

            switch (cls)
            {
                case NotificationClass.information:
                    ClassString = Lang.GetLangData(LangKeys.information);
                    break;
                case NotificationClass.warning:
                    ClassString = Lang.GetLangData(LangKeys.warning);
                    break;
                case NotificationClass.error:
                    ClassString = Lang.GetLangData(LangKeys.error);
                    break;
                default:
                    ClassString = Lang.GetLangData(LangKeys.information);
                    break;
            }

            Notification.Show(icon, $"{Lang.GetLangData(LangKeys.scriptname)}~s~ - {Lang.GetLangData(LangKeys.versionchar)}{ver}", ClassString, message); //~h~で太字。2回目に使うとそこから先を太字解除。
        }

        // private static int cclemon = 0;

        /// <summary>
        /// ポーズ状態を監視します。
        /// ※Ver2.0よりTimeSpan監視に変更
        /// </summary>
        /// <param name="ct">キャンセル用トークン。</param>
        /// <returns>なし</returns>
        public static async Task pause_tick(CancellationToken ct)
        {
            // キャンセル指示が来ない間
            while (!ct.IsCancellationRequested)
            {
                /* デバッグ用です…ｗ
                 cclemon++; Counter→頭文字がC→Cからはじまるもの→CCレモン！！ (?)
                 Notification.Show("PauseTick: Go! >>> " + cclemon);
                */

                await Task.Delay(250);

                game_pause = (DateTime.Now - lastTickTime > pauseThreshold);

                if (game_pause && !pause_interlock)
                {
                    //再生停止
                    MainScript.SoundMuteToSignalController();
                    pause_interlock = true;
                }
            }
            game_pause = false;
            pause_interlock = false;
        }
    }
}
