using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using System.Runtime.InteropServices;
using System.Windows.Forms; // KeyEventArgs用
using GTA.UI;
using System.Media;
using System.IO;
using System.Xml.Linq;
using System.Reflection;
using Control = GTA.Control;
using GTA.NaturalMotion;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace AdvancedTurnSignals
{
    public class Class1 : Script //Scriptは継承。継承すると継承元の情報を利用できる。(この場合はSHV.NETの情報。なお、スクリプト作成には必須。）
    {
        #region AdvancedTurnSignals.ini_variable

        private  Keys left; //キー設定を用意
        private  Keys right; //キー設定を用意
        private  Keys hazard; //キー設定を用意
        private  Keys leftM; //キー設定を用意
        private  Keys rightM; //キー設定を用意
        private  Keys hazardM; //キー設定を用意
        /*
        private  Keys escape = Keys.Escape; //キー設定を用意
        private  Keys pause1; //キー設定を用意
        private  Keys pause2; //キー設定を用意
        */
        private  bool use_button = false; //ボタン設定を使用するか
        private  string raw_leftC; //ボタン設定を用意
        private  string raw_rightC; //ボタン設定を用意
        private  string raw_hazardC; //ボタン設定を用意
        private  string raw_leftCM; //ボタン設定を用意
        private  string raw_rightCM; //ボタン設定を用意
        private  string raw_hazardCM; //ボタン設定を用意
        private  Control leftC; //ボタン設定を用意
        private  Control rightC; //ボタン設定を用意
        private  Control hazardC; //ボタン設定を用意
        private  Control? leftCM; //ボタン設定を用意(NULL OK)
        private  Control? rightCM; //ボタン設定を用意(NULL OK)
        private  Control? hazardCM; //ボタン設定を用意(NULL OK)
        private  bool enabled = true; //有効または無効
        private  bool autooff = true; //自動消灯
        private  bool keyboard_comp = false; //キーボード互換モード
        private  string raw_autooff_duration = "1000"; //キーを離してから方向指示器が自動で消えるまでの時間
        private  int autooff_duration = 1000; //キーを離してから方向指示器が自動で消えるまでの時間
        private  string raw_ready_angle = "15"; //自動消灯に最低限必要な角度
        private  string raw_off_angle = "10"; //自動消灯する角度
        private  Single ready_angle = 15; //自動消灯に最低限必要な角度(コード内で使用)
        private  Single off_angle = 10; //自動消灯する角度(コード内で使用)
        private  Single? toggle_angle = null; //ウインカーと反対側にハンドルを切った場合に使用する。
        private  bool autoon = false; //自動点灯
        private  string raw_on_angle = "10"; //自動点灯する角度
        private  Single on_angle = 10; //自動点灯に最低限必要な角度(コード内で使用)
        private  string raw_on_speed = "10"; //自動点灯する速度
        private  Single on_speed = 10; //自動点灯できる最高速度(コード内で使用)
        private  bool use_sound = true;
        private  bool inter_sound = true; //カスタムサウンドロード失敗時にデフォルトサウンドを使用するかのフラグ
        #endregion

        //最大角度：バニラで-+40
        //ReadyAngleは現実の最大角度240度のうち90度設定と合うよう、40度のうち15度と設定。
        //OffAngleは現実の最大角度240度のうち60度設定と合うよう、40度のうち10度と設定。

        List<VehicleClass> black_class_list = new List<VehicleClass>  //使用不可の乗り物属性リスト
        {
            VehicleClass.Boats,
            VehicleClass.Cycles,
            VehicleClass.Helicopters,
            VehicleClass.Planes,
            VehicleClass.Trains
        };

         bool turn_left = false; //ハザード用
         bool turn_right = false;
         bool is_hazard = false; //ハザード状態か
         bool off_ready = false; //自動消灯アングルまでハンドルが切られているか

        //サウンド関連
        XElement xml = null; //XML File
        private SoundPlayer player = null;
        private List<string> requireAudio = new List<string>();
        private List<string> audiofiles = new List<string>();
        private string[] defaultsounds = new string[4]; //要素数は4だがカウント(添え字）は0スタートである点に注意。
        private string[] selectedsounds = new string[4]; //要素数は4だがカウント(添え字）は0スタートである点に注意。
        private bool reload_hint_showed = false; //リロードすると音声ファイルの再読み込みが行われるヒントを表示したかどうか

        #region Notify_variable
        //バージョン情報
        private static AssemblyName assembly = Assembly.GetExecutingAssembly().GetName(); //アセンブリ情報
        private string ver = assembly.Version.ToString(3); // 形式は0.0.0

        NotificationIcon icon = NotificationIcon.Default; //通知アイコン

        private Dictionary<string,string> localizations = new Dictionary<string, string> 
        {
            {"scriptname", "" },
            {"versionchar","" },
            {"information", "" },
            {"warning", "" },
            {"error", "" },
            {"loaded", "" },
            {"duration_warn", "" },
            {"num_warn", "" },
            {"overnum_warn", "" },
            {"overangle_err", "" },
            {"audio_warn", "" },
            {"default_warn", "" },
            {"audio_err", "" },
            {"tips", "" },
            {"reload_tips", "" }
        };
        private Dictionary<string,Control> Buttons = new Dictionary<string, Control>
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
        private List<Keys> ActiveKeys = new List<Keys>();
        private List<Control> ActiveControls = new List<Control>();

        #endregion

        public Class1()
        {
            #region AdvancedTurnSignals.ini
            ScriptSettings ini = ScriptSettings.Load(@"scripts\AdvancedTurnSignals.ini"); //INI File
            // iniのデータを読み込む (セクション、キー、デフォルト値)
            left = ini.GetValue<Keys>("Keys", "Left", Keys.J);
            right = ini.GetValue<Keys>("Keys", "Right", Keys.K);
            hazard = ini.GetValue<Keys>("Keys", "Hazard", Keys.I);
            leftM = ini.GetValue<Keys>("Keys", "LeftModifier", Keys.None);
            rightM = ini.GetValue<Keys>("Keys", "RightModifier", Keys.None);
            hazardM = ini.GetValue<Keys>("Keys", "HazardModifier", Keys.None);

            use_button = ini.GetValue<bool>("Buttons", "UseButton", false);
            raw_leftC = ini.GetValue<string>("Buttons", "Left", "LB");
            raw_rightC = ini.GetValue<string>("Buttons", "Right", "RB");
            raw_hazardC = ini.GetValue<string>("Buttons", "Hazard", "PadUp");
            raw_leftCM = ini.GetValue<string>("Buttons", "LeftModifier", "None");
            raw_rightCM = ini.GetValue<string>("Buttons", "RightModifier", "None");
            raw_hazardCM = ini.GetValue<string>("Buttons", "HazardModifier", "None");
            // pause1 = ini.GetValue<Keys>("Keys", "PrimaryPause", Keys.P);
            // pause2 = ini.GetValue<Keys>("Keys", "SecondaryPause", Keys.None);
            enabled = ini.GetValue<bool>("General", "Enabled", true);
            use_sound = ini.GetValue<bool>("General", "UseSound", true);
            inter_sound = ini.GetValue<bool>("General", "InterSound", true);

            autooff = ini.GetValue<bool>("Autooff", "Autooff", true);
            raw_ready_angle = ini.GetValue<string>("Autooff", "ReadyAngle", "15");
            raw_off_angle = ini.GetValue<string>("Autooff", "OffAngle", "10");
            keyboard_comp = ini.GetValue<bool>("Autooff", "KeyboardComp", false);
            raw_autooff_duration = ini.GetValue<string>("Autooff", "AutooffDuration", "1000");

            autoon = ini.GetValue<bool>("Autoon", "Autoon", false);
            raw_on_angle = ini.GetValue<string>("Autoon", "OnAngle", "10");
            raw_on_speed = ini.GetValue<string>("Autoon", "OnSpeed", "10");
            #endregion


            #region Localization.ini
            ScriptSettings local = ScriptSettings.Load(@"scripts\AdvancedTurnSignals\Localization.ini"); //INI File
            // iniのデータを読み込む (セクション、キー、デフォルト値)
            localizations["scriptname"] = local.GetValue<string>("General", "ScriptName", assembly.Name);
            localizations["versionchar"] = local.GetValue<string>("General", "VersionChar", "~q~v");

            localizations["information"] = local.GetValue<string>("Level", "Information", "~b~Information");
            localizations["warning"] = local.GetValue<string>("Level", "Warning", "~o~Warning");
            localizations["error"] = local.GetValue<string>("Level", "Error", "~r~Error");

            localizations["loaded"] = local.GetValue<string>("Message", "Loaded", "~g~has loaded!");
            localizations["button_warn"] = local.GetValue<string>("Message", "ButtonWarn", "~r~The ~y~{0} ~r~controller button specification is ~h~incorrect. ~h~~n~~o~The default value ~b~({1})~o~ is used.");
            localizations["num_warn"] = local.GetValue<string>("Message", "NumWarn", "~y~{0} ~r~in {1} is ~h~not a number. ~h~~n~~o~The default number ~b~({2}) ~o~will be used.");
            localizations["overnum_warn"] = local.GetValue<string>("Message", "OverAngleWarn", "~o~There are settings where the steering wheel angle exceeds GTA5's maximum steering angle of ~h~40 ~h~~o~degrees.~n~The script will continue to work, ~y~but the turn signal ~h~may not turn off automatically.");
            localizations["overangle_err"] = local.GetValue<string>("Message", "OverAngleErr", "~r~OffAngle value ~y~{0} ~r~cannot be higher than ReadyAngle value ~b~{1}~r~. ~n~~o~Auto-off feature has been ~h~disabled.");
            localizations["duration_warn"] = local.GetValue<string>("Message", "DurationWarn", "~y~{0} ~r~in AutooffDuration is ~h~not a number. ~h~~n~~o~The default number ~b~(1000) ~o~will be used.");
            localizations["audio_warn"] = local.GetValue<string>("Message", "AudioNotFoundWarn", "~r~Audio file ~y~({0})~r~ not found. ~n~~o~AdvancedTurnSignals will continue to work, but ~h~no audio will play~h~ if you perform operations that use the specified audio file.");
            localizations["default_warn"] = local.GetValue<string>("Message", "UseDefaultAudioWarn", "~r~Audio file ~y~({0})~r~ not found. ~n~~o~AdvancedTurnSignals plays default sounds when possible.");
            localizations["audio_err"] = local.GetValue<string>("Message", "AudioLoadErr", "~r~Failed to retrieve audio file. ~n~~o~UseSound feature has been ~h~disabled.");

            localizations["tips"] = local.GetValue<string>("Tips", "Tips", "~b~Tip");
            localizations["reload_tips"] = local.GetValue<string>("Tips", "Reload", "If you want to ~o~reload the audio file~s~, press the ~b~ReloadKey~s~ specified in ~y~ScriptHookVDotNet.ini~s~ to reload the script.");
            #endregion

            if (enabled)
            {
                KeyUp += keyUp;
                KeyDown += keyDown;
                Tick += onTick;


                icon = NotificationIcon.LsCustoms; //ロスサントス・カスタムアイコン
                Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["information"], localizations["loaded"]); //~h~で太字。2回目に使うとそこから先を太字解除。

                #region AdvancedTurnSignals.ini-Load

                if(use_button)
                {
                    if (!Buttons.ContainsKey(raw_leftC))
                    {
                        string message = localizations["button_warn"];
                        message = message.Replace("{0}", "Left"); //特定部分を変数の値に置き換える
                        message = message.Replace("{1}", "LB");

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_leftC = "LB";
                    }
                    if (!Buttons.ContainsKey(raw_rightC))
                    {
                        string message = localizations["button_warn"];
                        message = message.Replace("{0}", "Right"); //特定部分を変数の値に置き換える
                        message = message.Replace("{1}", "RB");

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_rightC = "RB";
                    }
                    if (!Buttons.ContainsKey(raw_hazardC))
                    {
                        string message = localizations["button_warn"];
                        message = message.Replace("{0}", "Hazard"); //特定部分を変数の値に置き換える
                        message = message.Replace("{1}", "PadUp");

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_hazardC = "PadUp";
                    }
                    leftC = Buttons[raw_leftC];
                    rightC = Buttons[raw_rightC];
                    hazardC = Buttons[raw_hazardC];

                    if (raw_leftCM.ToLower() != "none" && !Buttons.ContainsKey(raw_leftCM))
                    {
                        string message = localizations["button_warn"];
                        message = message.Replace("{0}", "LeftModifier"); //特定部分を変数の値に置き換える
                        message = message.Replace("{1}", "None");

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_leftCM = "None";
                        leftCM = null;
                    }
                    else if(raw_leftCM.ToLower() != "none")
                    {
                        leftCM = Buttons[raw_leftCM];
                    }
                    else
                    {
                        leftCM = null;
                    }

                    if (raw_rightCM.ToLower() != "none" && !Buttons.ContainsKey(raw_rightC))
                    {
                        string message = localizations["button_warn"];
                        message = message.Replace("{0}", "RightModifier"); //特定部分を変数の値に置き換える
                        message = message.Replace("{1}", "None");

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_rightCM = "None";
                        rightCM = null;
                    }
                    else if (raw_rightCM.ToLower() != "none")
                    {
                        rightCM = Buttons[raw_rightCM];
                    }
                    else
                    {
                        rightCM = null;
                    }

                    if (raw_hazardCM.ToLower() != "none" && !Buttons.ContainsKey(raw_hazardC))
                    {
                        string message = localizations["button_warn"];
                        message = message.Replace("{0}", "HazardModifier"); //特定部分を変数の値に置き換える
                        message = message.Replace("{1}", "None");

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_hazardCM = "None";
                        hazardCM = null;
                    }
                    else if (raw_hazardCM.ToLower() != "none")
                    {
                        hazardCM = Buttons[raw_hazardCM];
                    }
                    else
                    {
                        hazardCM = null;
                    }

                }

                if (autooff)
                {
                    float res = 0; //数値変換結果
                    int ires = 0;

                    if (!float.TryParse(raw_ready_angle, out res)) //数値でない場合
                    {
                        string message = localizations["num_warn"];
                        message = message.Replace("{0}", raw_ready_angle); //特定部分を変数の値に置き換える
                        message = message.Replace("{1}", "ReadyAngle"); //原因部分を設定項目に置き換える
                        message = message.Replace("{2}", ready_angle.ToString()); //特定部分を変数の値に置き換える

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"],message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_ready_angle = "15";
                    }

                    if (!float.TryParse(raw_off_angle, out res)) //数値でない場合
                    {
                        string message = localizations["num_warn"];
                        message = message.Replace("{0}", raw_off_angle); //特定部分を変数の値に置き換える
                        message = message.Replace("{1}", "OffAngle"); //原因部分を設定項目に置き換える
                        message = message.Replace("{2}", off_angle.ToString()); //特定部分を変数の値に置き換える

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_off_angle = "10";
                    }

                    if (!int.TryParse(raw_autooff_duration, out ires)) //数値でない場合
                    {
                        string message = localizations["duration_warn"];
                        message = message.Replace("{0}", raw_autooff_duration); //特定部分を変数の値に置き換える

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_autooff_duration = "1000";
                    }
                }

                if (autoon)
                {
                    float res = 0;
                    if (!float.TryParse(raw_on_angle, out res)) //数値でない場合
                    {
                        string message = localizations["num_warn"];
                        message = message.Replace("{0}", raw_on_angle); //特定部分を変数の値に置き換える
                        message = message.Replace("{1}", "OnAngle"); //原因部分を設定項目に置き換える
                        message = message.Replace("{2}", on_angle.ToString()); //特定部分を変数の値に置き換える

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_on_angle = "10";
                    }

                    if (!float.TryParse(raw_on_speed, out res)) //数値でない場合
                    {
                        string message = localizations["num_warn"];
                        message = message.Replace("{0}", raw_on_speed); //特定部分を変数の値に置き換える
                        message = message.Replace("{1}", "OnSpeed"); //原因部分を設定項目に置き換える
                        message = message.Replace("{2}", on_speed.ToString()); //特定部分を変数の値に置き換える

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_on_speed = "10";
                    }

                    //使用できる形に変換
                    ready_angle = float.Parse(raw_ready_angle);
                    off_angle = float.Parse(raw_off_angle);
                    autooff_duration = int.Parse(raw_autooff_duration);
                    on_angle = int.Parse(raw_on_angle);
                    on_speed = int.Parse(raw_on_speed);
                }

                if (ready_angle > 40 || off_angle > 40 || on_angle > 40) //GTAVの角度40度を超えた設定の場合
                {
                    icon = NotificationIcon.Blocked; //禁止アイコン
                    Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], localizations["overnum_warn"]); //~h~で太字。2回目に使うとそこから先を太字解除。

                }
                if (ready_angle < off_angle) //ReadyAngleよりもOffAngleのほうが数値が高い場合
                {
                    string message = localizations["overangle_err"];
                    message = message.Replace("{0}", off_angle.ToString()); //特定部分を変数の値に置き換える
                    message = message.Replace("{1}", ready_angle.ToString()); //特定部分を変数の値に置き換える

                    icon = NotificationIcon.LesterDeathwish; //骸骨のアイコン
                    Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["error"], message); //~h~で太字。2回目に使うとそこから先を太字解除。
                    autooff = false;
                }

                #endregion

                #region TurnSignalSoundSetup.xml-Load

                if (use_sound)
                {
                    bool audiofail = false;
                    //xmlファイルを指定する
                    xml = XElement.Load(@"scripts\AdvancedTurnSignals\TurnSignalSoundSetup.xml");

                    //タグ内のサウンド情報を取得する
                    IEnumerable<string> data = from item in xml.Elements("DefaultSettings").Elements("TURN_LEVER_INTRO")
                                                   select item.Value;
                    defaultsounds[0] = data.ElementAt(0); //返ってきた値の最初を取得(もともと1個しかないけど)
                    data = from item in xml.Elements("DefaultSettings").Elements("TURN_LEVER_OUTRO")
                           select item.Value;
                    defaultsounds[1] = data.ElementAt(0); //返ってきた値の最初を取得(もともと1個しかないけど)
                    data = from item in xml.Elements("DefaultSettings").Elements("INDICATOR_SOUND")
                           select item.Value;
                    defaultsounds[2] = data.ElementAt(0); //返ってきた値の最初を取得(もともと1個しかないけど)
                    data = from item in xml.Elements("DefaultSettings").Elements("HAZARD_BUTTON")
                           select item.Value;
                    defaultsounds[3] = data.ElementAt(0); //返ってきた値の最初を取得(もともと1個しかないけど)

                    foreach (string s in defaultsounds)
                    {
                        requireAudio.Add($@"scripts\AdvancedTurnSignals\TurnSignalSounds\{s}.wav");
                    }

                    try
                    {
                        audiofiles = Directory.GetFiles(@"scripts\AdvancedTurnSignals\TurnSignalSounds").Where(x => Path.GetExtension(x) == ".wav").ToList();
                    }
                    catch (Exception e)
                    {
                        audiofail = true;
                    }

                    if (!audiofail)
                    {
                        foreach (var audiofile in requireAudio)
                        {
                            if (!audiofiles.Contains(audiofile))
                            {
                                string af = audiofile.Replace(@"scripts\AdvancedTurnSignals\TurnSignalSounds\", ""); //パス部分の削除
                                string message = localizations["audio_warn"];
                                message = message.Replace("{0}",af); //特定部分を変数の値に置き換える

                                icon = NotificationIcon.Blocked; //禁止アイコン
                                Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。

                                if (!reload_hint_showed) //ヒントを表示していない場合
                                {
                                    icon = NotificationIcon.LsCustoms; //ロスサントス・カスタムアイコン
                                    Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["information"], $"{localizations["tips"]}~s~ - {localizations["reload_tips"]}"); //~h~で太字。2回目に使うとそこから先を太字解除。
                                    reload_hint_showed= true;
                                }
                            }
                        }
                    }
                    else
                    {
                        icon = NotificationIcon.LesterDeathwish; //骸骨のアイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["error"], localizations["audio_err"]); //~h~で太字。2回目に使うとそこから先を太字解除。
                        use_sound = false;

                    }
                }

                #endregion

                Task.Run(() => { pause_tick(); });
            }
#if DEBUG
            Notification.Show("Advanced turn signals is READY!");
#endif

        }

        private void sounds_load(string vehicle_name)
        {
            //SoundSetup→Vehiclesのタグ内の情報を取得する
            IEnumerable<XElement> infos = from item in xml.Elements("SoundSetup").Elements("Vehicles")
                                          select item;

            bool exist_custom_settings = false;
            //SoundSetup→Vehicles分ループして、存在チェック
            foreach (XElement info in infos)
            {
                if(info.Element("Vehicle").Value == vehicle_name)
                {
                    exist_custom_settings = true;
                    break;
                }
            }

            if (exist_custom_settings)
            {
                //「vehicle」がvehicle_nameであるサウンドの「ファイル名」を取得する。
                IEnumerable<string> data = from item in xml.Elements("SoundSetup")
                                           where item.Element("Vehicles").Element("Vehicle").Value == vehicle_name
                                           select item.Element("Sounds").Element("TURN_LEVER_INTRO").Value;
                selectedsounds[0] = data.ElementAt(0);
                data = from item in xml.Elements("SoundSetup")
                       where item.Element("Vehicles").Element("Vehicle").Value == vehicle_name
                       select item.Element("Sounds").Element("TURN_LEVER_OUTRO").Value;
                selectedsounds[1] = data.ElementAt(0);
                data = from item in xml.Elements("SoundSetup")
                       where item.Element("Vehicles").Element("Vehicle").Value == vehicle_name
                       select item.Element("Sounds").Element("INDICATOR_SOUND").Value;
                selectedsounds[2] = data.ElementAt(0);
                data = from item in xml.Elements("SoundSetup")
                       where item.Element("Vehicles").Element("Vehicle").Value == vehicle_name
                       select item.Element("Sounds").Element("HAZARD_BUTTON").Value;
                selectedsounds[3] = data.ElementAt(0);

                for (int i = 0; i < selectedsounds.Length; i++) //lengthは4。添え字は0スタート。4に満たない間ということは0スタートで3まで（0,1,2,3）要素数の総数と一致。ちょうどいいね。
                {
                    if (!audiofiles.Contains($@"scripts\AdvancedTurnSignals\TurnSignalSounds\{selectedsounds[i]}.wav"))
                    {
                        if (inter_sound)
                        {
                            string message = localizations["default_warn"];
                            message = message.Replace("{0}", $"{selectedsounds[i]}.wav"); //特定部分を変数の値に置き換える

                            icon = NotificationIcon.Blocked; //禁止アイコン
                            Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。

                            // デフォルトサウンドに一時的に置き換え
                            selectedsounds[i] = defaultsounds[i];
                        }
                        else
                        {
                            string message = localizations["audio_warn"];
                            message = message.Replace("{0}", $"{selectedsounds[i]}.wav"); //特定部分を変数の値に置き換える

                            icon = NotificationIcon.Blocked; //禁止アイコン
                            Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。


                        }
                        if (!reload_hint_showed) //ヒントを表示していない場合
                        {
                            icon = NotificationIcon.LsCustoms; //ロスサントス・カスタムアイコン
                            Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["information"], $"{localizations["tips"]}~s~ - {localizations["reload_tips"]}"); //~h~で太字。2回目に使うとそこから先を太字解除。
                            reload_hint_showed = true;
                        }

                    }
                }
            }
            else //存在しない場合はデフォルト設定を使用
            {
                for (int i = 0; i < selectedsounds.Length; i++) //lengthは4。添え字は0スタート。4に満たない間ということは0スタートで3まで（0,1,2,3）要素数の総数と一致。ちょうどいいね。
                {
                    selectedsounds[i] = defaultsounds[i];
                }
            }
        }

        /*
        0: TURN_LEVER_INTRO
        1: TURN_LEVER_OUTRO
        2: INDICATOR_SOUND 
        3: HAZARD_BUTTON
        */
        private void signal_manual(string mode = "turn")
        {
            if (player != null)
            {
                player.Stop();
                player.Dispose();
                player = null;
            }

            //読み込む
            if (mode == "turn")
            {
                player = new SoundPlayer($@"scripts\AdvancedTurnSignals\TurnSignalSounds\{selectedsounds[0]}.wav");
            }
            else if(mode == "hazard")
            {
                player = new SoundPlayer($@"scripts\AdvancedTurnSignals\TurnSignalSounds\{selectedsounds[3]}.wav");
            }
            
            //再生する
            player.PlaySync();

            //一度破棄
            player.Stop();
            player.Dispose();
            player = null;

            if(Game.Player.Character.CurrentVehicle.IsLeftIndicatorLightOn | Game.Player.Character.CurrentVehicle.IsRightIndicatorLightOn)
            {
                player = new SoundPlayer($@"scripts\AdvancedTurnSignals\TurnSignalSounds\{selectedsounds[2]}.wav");
                player.PlayLooping();
            }
        }

        private void signal_autooff()
        {
            if (player != null) //再生中なら一度破棄
            {
                player.Stop();
                player.Dispose();
                player = null;
            }
            //読み込む
            player = new SoundPlayer($@"scripts\AdvancedTurnSignals\TurnSignalSounds\{selectedsounds[1]}.wav");
            //再生する
            player.PlaySync();
            //用済みなので破棄
            player.Stop();
            player.Dispose();
            //nullを入れてしまうとTick側の再生処理が反応してしまうのでDisposeまで

            if (Game.Player.Character.CurrentVehicle.IsLeftIndicatorLightOn | Game.Player.Character.CurrentVehicle.IsRightIndicatorLightOn)
            {
                player = new SoundPlayer($@"scripts\AdvancedTurnSignals\TurnSignalSounds\{selectedsounds[2]}.wav");
                player.PlayLooping();
            }

        }

        private bool digital_OK = false;
        private bool digital_angled = false;
        private async void digital_autooff()
        {
            digital_OK = false; //再び待機
            // Notification.Show($"Duration: {autooff_duration}");
            await Task.Delay(autooff_duration); //指定時間経過後に
            if(Math.Abs(Game.Player.Character.CurrentVehicle.SteeringAngle) < off_angle) //自動消灯角度まで戻っている場合
            {
                // Notification.Show("Digital OK!");
                digital_angled = true;
                digital_OK= true;
            }
            else if(digital_angled) //一度アングルを超えているなら
            {
                // Notification.Show($"Digital NG! {Math.Abs(Game.Player.Character.CurrentVehicle.SteeringAngle)} / {off_angle}");
                digital_OK = true;
            }
        }

        /// <summary>
        /// 方向指示器が作動できる乗り物であるか
        /// </summary>
        /// <param name="vehicle">現在の車両情報</param>
        /// <returns>Trueで方向指示器の作動可能</returns>
        private bool available(Vehicle vehicle)
        {
            if (black_class_list.Contains(vehicle.ClassType))
            {
                return false;
            }
            return true;
        }

        Task ts;

        /// <summary>
        /// 方向指示器の実行
        /// </summary>
        /// <param name="state">作動箇所</param>
        /// <param name="cv">現在の車両情報</param>
        private void Execute(string state, Vehicle cv)
        {
            if (state == "L")
            {
                if (use_sound)
                {
                    // ウインカー再生
                    ts = Task.Run(() => { signal_manual(); });
                }

                if (!is_hazard)
                {
                    cv.IsRightIndicatorLightOn = false; //消灯
                    cv.IsLeftIndicatorLightOn = !cv.IsLeftIndicatorLightOn; //ON/OFF 切り替え
                    turn_left = cv.IsLeftIndicatorLightOn;
                    if (cv.IsLeftIndicatorLightOn && autooff) //ウインカー有効時
                    {
                        toggle_angle = cv.SteeringAngle;
                        off_ready = is_off_angle("L"); //自動消灯できる角度まで切っているか
                        digital_OK = false;
                    }
                }
                else
                {
                    turn_left = !turn_left;
                }
            }
            else if (state == "R")
            {
                if (use_sound)
                {
                    // ウインカー再生
                    ts = Task.Run(() => { signal_manual(); });
                }
                if (!is_hazard)
                {
                    cv.IsLeftIndicatorLightOn = false; //消灯
                    cv.IsRightIndicatorLightOn = !cv.IsRightIndicatorLightOn; //ON/OFF 切り替え

                    turn_right = cv.IsRightIndicatorLightOn;
                    if (cv.IsRightIndicatorLightOn && autooff)
                    {
                        toggle_angle = cv.SteeringAngle;
                        off_ready = is_off_angle("L"); //自動消灯できる角度まで切っているか
                        digital_OK = false;
                    }
                }
                else
                {
                    turn_right = !turn_right;
                }
            }
            else if (state  == "H")
            {
                if (cv.IsLeftIndicatorLightOn && cv.IsRightIndicatorLightOn) //ハザード状態の場合のみ
                {
                    cv.IsLeftIndicatorLightOn = turn_left; //前回の状態に戻す
                    cv.IsRightIndicatorLightOn = turn_right;
                    is_hazard = false;
                }
                else
                {
                    turn_left = cv.IsLeftIndicatorLightOn; //状態を保存
                    turn_right = cv.IsRightIndicatorLightOn;

                    cv.IsRightIndicatorLightOn = true; //点灯
                    cv.IsLeftIndicatorLightOn = true; //点灯
                    is_hazard = true;
                }
                if (use_sound)
                {
                    ts = Task.Run(() => { signal_manual("hazard"); });
                }

            }
        }

        bool game_pause = false;
        int pause_judge = 0;
        private async void pause_tick()
        {
            while (true)
            {
                await Task.Delay(100);
                if (game_pause)
                {
                    pause_judge++;
                    if (pause_judge > 3)
                    {
                        if (player != null)
                        {
                            player.Stop();
                            player.Dispose();
                            player = null;
                        }
                    }
                }
                else //falseにされたら
                {
                    pause_judge = 0;
                }
                game_pause = true;
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey); //仮想キーコードからのキー状態取得メソッド
        private void LRkeyUp()
        {
            #region ShiftKey
            if(GetKeyState(0xA0) < 2 && ActiveKeys.Contains(Keys.LShiftKey)) // LShiftKey
            {
                ActiveKeys.Remove(Keys.LShiftKey);
            }
            if(GetKeyState(0xA1) < 2 && ActiveKeys.Contains(Keys.RShiftKey)) // RShiftKey
            {
                ActiveKeys.Remove(Keys.RShiftKey);
            }
            #endregion
            #region ControlKey
            if(GetKeyState(0xA2) < 2 && ActiveKeys.Contains(Keys.LControlKey)) // LControlKey
            {
                ActiveKeys.Remove(Keys.LControlKey);
                Notification.Show("REMOVE:L");
            }
            if(GetKeyState(0xA3) < 2 && ActiveKeys.Contains(Keys.RControlKey)) // RControlKey
            {
                ActiveKeys.Remove(Keys.RControlKey);
                Notification.Show("REMOVE:R");
            }
            #endregion
            #region AltKey
            if(GetKeyState(0xA4) < 2 && ActiveKeys.Contains(Keys.LMenu)) // LMenu
            {
                ActiveKeys.Remove(Keys.LMenu);
            }
            if(GetKeyState(0xA5) < 2 && ActiveKeys.Contains(Keys.RMenu)) // RMenu
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
            if(GetKeyState(0xA1) < 0 && !ActiveKeys.Contains(Keys.RShiftKey)) // RShiftKey
            {
                ActiveKeys.Add(Keys.RShiftKey);
            }
            #endregion
            #region ControlKey
            if (GetKeyState(0xA2) < 0 && !ActiveKeys.Contains(Keys.LControlKey)) // LControlKey
            {
                ActiveKeys.Add(Keys.LControlKey);
                Notification.Show("ADD:L");
            }
            if(GetKeyState(0xA3) < 0 && !ActiveKeys.Contains(Keys.RControlKey)) // RControlKey
            {
                ActiveKeys.Add(Keys.RControlKey);
                Notification.Show("ADD:R");
            }
            #endregion
            #region AltKey
            if (GetKeyState(0xA4) < 0 && !ActiveKeys.Contains(Keys.LMenu)) // LMenu
            {
                ActiveKeys.Add(Keys.LMenu);
            }
            if(GetKeyState(0xA5) < 0 && !ActiveKeys.Contains(Keys.RMenu)) // RMenu
            {
                ActiveKeys.Add(Keys.RMenu);
            }
            #endregion

        }

        Task da;
        private void keyUp(object sender, KeyEventArgs e)
        {
            Vehicle cv = Game.Player.Character.CurrentVehicle;

            if (enabled && keyboard_comp && cv != null && off_ready && cv.ClassType != VehicleClass.Motorcycles && !black_class_list.Contains(cv.ClassType) && cv.IsLeftIndicatorLightOn | cv.IsRightIndicatorLightOn) //条件に合ったら
            {
                da = Task.Run(() => { digital_autooff(); });
            }
            if (ActiveKeys.Contains(e.KeyCode))
            {
                ActiveKeys.Remove(e.KeyCode);
            }
            if (new Keys[] { Keys.ControlKey, Keys.ShiftKey, Keys.Menu }.Contains(e.KeyCode)) // Ctrl, Alt ,Shiftの場合 (下でelseを使わない理由はLR指定をしていない場合の対策)
            {
                LRkeyUp();
            }
        }

        private void keyDown(object sender, KeyEventArgs e)
        {
            if(new Keys[] { Keys.ControlKey, Keys.ShiftKey, Keys.Menu }.Contains(e.KeyCode)) // Ctrl, Alt ,Shiftの場合 (下でelseを使わない理由はLR指定をしていない場合の対策)
            {
                LRkeyDown();
            }
            if (e.KeyCode != Keys.Escape && !ActiveKeys.Contains(e.KeyCode)) //ESCはゲームがポーズして押しっぱなし判定になってしまうため除外
            {
                ActiveKeys.Add(e.KeyCode);
            }
            Vehicle cv = Game.Player.Character.CurrentVehicle;

            if (enabled && cv != null && new Keys[] { left, right, hazard }.Contains(e.KeyCode)) //有効かつ車に乗っているかつ設定したキーバインドが押されている
            {
                bool a = available(cv);
                if (a) //方向指示器が作動できる乗り物である場合
                {
                    //修飾キーが押されているか（ない場合は普通に実行）
                    if (ActiveKeys.Contains(left) && leftM == Keys.None | ActiveKeys.Contains(leftM))
                    {
                        Execute("L", cv);
                    }
                    else if (ActiveKeys.Contains(right) && rightM == Keys.None | ActiveKeys.Contains(rightM))
                    {
                        Execute("R", cv);
                    }
                    else if (ActiveKeys.Contains(hazard) && hazardM == Keys.None | ActiveKeys.Contains(hazardM))
                    {
                        Execute("H", cv);
                    }
                }
            }

            if (enabled && cv != null && available(cv) && autoon) //自動点灯が有効である
            {
                if (!cv.IsLeftIndicatorLightOn && cv.SteeringAngle > on_angle && cv.Speed > 0 && cv.Speed * 2 < on_speed) //ウインカー無効＆OnAngleを超えている＆OnSpeed以下である場合
                {
                    Execute("L", cv);
                }
                else if (!cv.IsRightIndicatorLightOn && cv.SteeringAngle < -on_angle && cv.Speed > 0 && cv.Speed * 2 < on_speed)
                {
                    Execute("R", cv);
                }
            }
        }

        bool controller_executed = false; //コントローラーによるウインカー操作がされたか(Trueの場合は操作済みのためExecute呼び出しをしない)
        private void controller(Vehicle cv)
        {
            #region コントローラリスナ
            if (Game.IsControlJustPressed(leftC) && !ActiveControls.Contains(leftC))
            {
                ActiveControls.Add(leftC);
            }
            if (Game.IsControlJustPressed(rightC) && !ActiveControls.Contains(rightC))
            {
                ActiveControls.Add(rightC);
            }
            if (Game.IsControlJustPressed(hazardC) && !ActiveControls.Contains(hazardC))
            {
                ActiveControls.Add(hazardC);
            }
            if (leftCM != null && Game.IsControlJustPressed((Control)leftCM) && !ActiveControls.Contains((Control)leftCM)) //キャストでNULL許容型解除
            {
                ActiveControls.Add((Control)leftCM);
            }
            if (rightCM != null && Game.IsControlJustPressed((Control)rightCM) && !ActiveControls.Contains((Control)rightCM))
            {
                ActiveControls.Add((Control)rightCM);
            }
            if (hazardCM != null && Game.IsControlJustPressed((Control)hazardCM) && !ActiveControls.Contains((Control)hazardCM))
            {
                ActiveControls.Add((Control)hazardCM);
            }

            if (Game.IsControlJustReleased(leftC) && ActiveControls.Contains(leftC))
            {
                ActiveControls.Remove(leftC);
            }
            if (Game.IsControlJustReleased(rightC) && ActiveControls.Contains(rightC))
            {
                ActiveControls.Remove(rightC);
            }
            if (Game.IsControlJustReleased(hazardC) && ActiveControls.Contains(hazardC))
            {
                ActiveControls.Remove(hazardC);
            }
            if (leftCM != null && Game.IsControlJustReleased((Control)leftCM) && ActiveControls.Contains((Control)leftCM)) //キャストでNULL許容型解除
            {
                ActiveControls.Remove((Control)leftCM);
            }
            if (rightCM != null && Game.IsControlJustReleased((Control)rightCM) && ActiveControls.Contains((Control)rightCM))
            {
                ActiveControls.Remove((Control)rightCM);
            }
            if (hazardCM != null && Game.IsControlJustReleased((Control)hazardCM) && ActiveControls.Contains((Control)hazardCM))
            {
                ActiveControls.Remove((Control)hazardCM);
            }


            #endregion

            if(ActiveControls.Count == 0) //指定されているコントローラーボタンがすべて離されたら
            {
                controller_executed = false; //再度ウインカー操作が可能になる
            }

            if (enabled && cv != null && available(cv) && ActiveKeys.Count == 0 && !controller_executed) //有効かつ方向指示器が動作できる車に乗っており、キーが押されていない、また、指定したコントローラーのボタンがすべて一度離されている
            {
                if (ActiveControls.Contains(leftC) && leftCM == null | ActiveControls.Contains((Control)leftCM))
                {
                    controller_executed = true;
                    Execute("L", cv);
                }
                else if (ActiveControls.Contains(rightC) && rightCM == null | ActiveControls.Contains((Control)rightCM))
                {
                    controller_executed = true;
                    Execute("R", cv);
                }
                else if (ActiveControls.Contains(hazardC) && hazardCM == null | ActiveControls.Contains((Control)hazardCM))
                {
                    controller_executed = true;
                    Execute("H", cv);
                }
            }
            
        }

        //Steering Angle = マイナスが右回し、プラスが左まわし。

        private bool is_off_angle(string LR)
        {
            Vehicle cv = Game.Player.Character.CurrentVehicle;

            if (off_ready) //すでにTrueの場合は結果を変えない
            {
                return true;
            }
            else if (LR == "L" && cv.SteeringAngle > ready_angle) //自動消灯できる角度まで切っている
            {
                return true;
            }
            else if (LR == "R" && cv.SteeringAngle < -ready_angle) //自動消灯できる角度まで切っている
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private Vehicle gcv = null;

        private void onTick(object sender, EventArgs e)
        {

#if DEBUG
            GTA.UI.Screen.ShowSubtitle($"Key: {ActiveKeys.Count} / Controls: {ActiveControls.Count}");
#endif

            if (pause_judge > 3)
            {
                ActiveKeys.Clear(); //押しっぱなし判定の対策としてポーズ画面から解除したらリセット
                ActiveControls.Clear();
            }
            game_pause = false;
            Vehicle cv = Game.Player.Character.CurrentVehicle;
            if (cv != gcv) //以前保存した車両と情報が違う場合
            {
                gcv = cv; //車両情報更新
                if (cv != null)
                {
                    sounds_load(cv.DisplayName);
                }
            }
            if (cv == null && player != null)  //非乗車状態でサウンドが鳴っている場合
            {
                player.Stop();
                player.Dispose();
                player = null;
            }
            else if (cv != null && player == null && cv.IsLeftIndicatorLightOn | cv.IsRightIndicatorLightOn) //車両乗車時にウインカーが有効の場合
            {
                player = new SoundPlayer($@"scripts\AdvancedTurnSignals\TurnSignalSounds\{selectedsounds[2]}.wav"); //サウンド再生(loop)
                player.PlayLooping();
            }

            //サウンドが再生されておらず、非同期処理のメモリリソースが解放されていない場合
            if (ts != null && player == null)
            {
                ts.Dispose();
                ts = null;
            }
            if (use_button && ActiveKeys.Count == 0) //ボタン設定を使用する場合
            {
                controller(cv);
            }
            /*
            if (Game.IsControlJustPressed(Control.FrontendPause))
            {
                await Task.Delay(1000);
                // is_pause();
            }
            */


            if (autooff && cv != null && cv.ClassType != VehicleClass.Motorcycles) //自動消灯オン+車に乗っている+バイクでない
            {
                if (!is_hazard) //ハザード状態でない
                {
                    if (cv.IsLeftIndicatorLightOn) //Left indicator
                    {
                        off_ready = is_off_angle("L"); //自動消灯できる角度まで切っているか
                        if (cv.SteeringAngle < toggle_angle - off_angle) //ウインカーと反対側にハンドルを切った場合
                        {
                            cv.IsLeftIndicatorLightOn = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                ts = Task.Run(() => { signal_autooff(); });
                            }


                        }
                        else if (keyboard_comp && off_ready && digital_OK)
                        {
                            cv.IsLeftIndicatorLightOn = false; //消灯
                            off_ready = false;
                            digital_OK = false;
                            digital_angled = false;
                            da.Dispose();
                            if (use_sound)
                            {
                                ts = Task.Run(() => { signal_autooff(); });
                            }
                        }
                        else if (!keyboard_comp && off_ready && cv.SteeringAngle < off_angle) //自動消灯条件を満たした場合
                        {
                            cv.IsLeftIndicatorLightOn = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                ts = Task.Run(() => { signal_autooff(); });
                            }
                        }
                    }
                    else if (cv.IsRightIndicatorLightOn) //Right indicator
                    {
                        off_ready = is_off_angle("R"); //自動消灯できる角度まで切っているか

                        if (cv.SteeringAngle > toggle_angle + off_angle) //ウインカーと反対側にハンドルを切った場合
                        {
                            cv.IsRightIndicatorLightOn = false;
                            off_ready = false;

                            if (use_sound)
                            {
                                ts = Task.Run(() => { signal_autooff(); });
                            }

                        }
                        else if (keyboard_comp && off_ready && digital_OK)
                        {
                            cv.IsRightIndicatorLightOn = false; //消灯
                            off_ready = false;
                            digital_OK = false;
                            digital_angled = false;
                            da.Dispose();
                            if (use_sound)
                            {
                                ts = Task.Run(() => { signal_autooff(); });
                            }
                        }
                        else if (!keyboard_comp && off_ready && cv.SteeringAngle > -off_angle)  //自動消灯条件を満たした場合
                        {
                            cv.IsRightIndicatorLightOn = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                ts = Task.Run(() => { signal_autooff(); });
                            }
                        }
                    }
                }
                else //ハザード状態の場合
                {
                    if (turn_left) //Left indicator
                    {
                        off_ready = is_off_angle("L"); //自動消灯できる角度まで切っているか

                        if (cv.SteeringAngle < toggle_angle - off_angle) //ウインカーと反対側にハンドルを切った場合
                        {
                            turn_left = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                ts = Task.Run(() => { signal_autooff(); });
                            }
                        }
                        else if (keyboard_comp && off_ready && digital_OK)
                        {
                            turn_left = false; //消灯
                            off_ready = false;
                            digital_OK = false;
                            digital_angled = false;
                            da.Dispose();
                            if (use_sound)
                            {
                                ts = Task.Run(() => { signal_autooff(); });
                            }
                        }
                        else if (!keyboard_comp && off_ready && cv.SteeringAngle < off_angle) //自動消灯条件を満たした場合
                        {
                            turn_left = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                ts = Task.Run(() => { signal_autooff(); });
                            }
                        }
                    }
                    else if (turn_right) //Right indicator
                    {
                        off_ready = is_off_angle("R"); //自動消灯できる角度まで切っているか
                        if (cv.SteeringAngle > toggle_angle + off_angle) //ウインカーと反対側にハンドルを切った場合
                        {
                            turn_right = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                ts = Task.Run(() => { signal_autooff(); });
                            }
                        }
                        else if (keyboard_comp && off_ready && digital_OK)
                        {
                            turn_right = false; //消灯
                            off_ready = false;
                            digital_OK = false;
                            digital_angled = false;
                            da.Dispose();
                            if (use_sound)
                            {
                                ts = Task.Run(() => { signal_autooff(); });
                            }
                        }
                        else if (!keyboard_comp && off_ready && cv.SteeringAngle > -off_angle) //自動消灯条件を満たした場合
                        {
                            turn_right = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                ts = Task.Run(() => { signal_autooff(); });
                            }
                        }
                    }

                }
            }

        }
    }

}
