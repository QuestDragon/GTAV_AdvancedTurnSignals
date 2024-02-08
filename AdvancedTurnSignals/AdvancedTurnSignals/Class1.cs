using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using System.Runtime.InteropServices;
using System.Windows.Forms; // KeyEventArgs用
using GTA.UI;
using GTA.Math;
using System.Media;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;
using GTA.NaturalMotion;
using System.Reflection;

namespace AdvancedTurnSignals
{
    public class Class1 : Script //Scriptは継承。継承すると継承元の情報を利用できる。(この場合はSHV.NETの情報。なお、スクリプト作成には必須。）
    {
        #region AdvancedTurnSignals.ini_variable
        //iniファイルの読み込みに必要な処理。Pythonのimport文、C#のUsing文の派生版みたいなもんよ
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileStringW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
        private static Keys left; //キー設定を用意
        private static Keys right; //キー設定を用意
        private static Keys hazard; //キー設定を用意
        private static Keys escape = Keys.Escape; //キー設定を用意
        private static Keys pause1; //キー設定を用意
        private static Keys pause2; //キー設定を用意
        private static bool enabled = true; //有効または無効
        private static bool autooff = true; //自動消灯
        private static string raw_ready_angle = "15"; //自動消灯に最低限必要な角度
        private static string raw_off_angle = "10"; //自動消灯する角度
        private static Single ready_angle = 15; //自動消灯に最低限必要な角度(コード内で使用)
        private static Single off_angle = 10; //自動消灯する角度(コード内で使用)
        private static Single? toggle_angle = null; //ウインカーと反対側にハンドルを切った場合に使用する。
        public static bool use_sound = true;
        public static bool inter_sound = true; //カスタムサウンドロード失敗時にデフォルトサウンドを使用するかのフラグ
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

        static bool turn_left = false; //ハザード用
        static bool turn_right = false;
        static bool is_hazard = false; //ハザード状態か
        static bool off_ready = false; //自動消灯アングルまでハンドルが切られているか

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
            {"readyangle_warn", "" },
            {"offangle_warn", "" },
            {"overangle_warn", "" },
            {"overangle_err", "" },
            {"audio_warn", "" },
            {"default_warn", "" },
            {"audio_err", "" },
            {"tips", "" },
            {"reload_tips", "" }
        };

        #endregion

        public Class1()
        {
            #region AdvancedTurnSignals.ini
            ScriptSettings ini = ScriptSettings.Load(@"scripts\AdvancedTurnSignals.ini"); //INI File
            // iniのデータを読み込む (セクション、キー、デフォルト値)
            left = ini.GetValue<Keys>("Keys", "Left", Keys.J);
            right = ini.GetValue<Keys>("Keys", "Right", Keys.K);
            hazard = ini.GetValue<Keys>("Keys", "Hazard", Keys.I);
            pause1 = ini.GetValue<Keys>("Keys", "PrimaryPause", Keys.P);
            pause2 = ini.GetValue<Keys>("Keys", "SecondaryPause", Keys.None);
            enabled = ini.GetValue<bool>("General", "Enabled", true);
            use_sound = ini.GetValue<bool>("General", "UseSound", true);
            inter_sound = ini.GetValue<bool>("General", "InterSound", true);

            autooff = ini.GetValue<bool>("Autooff", "Autooff", true);
            raw_ready_angle = ini.GetValue<string>("Autooff", "ReadyAngle", "15");
            raw_off_angle = ini.GetValue<string>("Autooff", "OffAngle", "10");
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
            localizations["readyangle_warn"] = local.GetValue<string>("Message", "ReadyAngleWarn", "~y~{0} ~r~in ReadyAngle is ~h~not a number. ~h~~n~~o~The default number ~b~(15) ~o~will be used.");
            localizations["offangle_warn"] = local.GetValue<string>("Message", "OffAngleWarn", "~y~{0} ~r~in OffAngle is ~h~not a number. ~h~~n~~o~The default number ~b~(10) ~o~will be used.");
            localizations["overangle_warn"] = local.GetValue<string>("Message", "OverAngleWarn", "~o~There are settings where the steering wheel angle exceeds GTA5's maximum steering angle of ~h~40 ~h~~o~degrees.~n~The script will continue to work, ~y~but the turn signal ~h~may not turn off automatically.");
            localizations["overangle_err"] = local.GetValue<string>("Message", "OverAngleErr", "~r~OffAngle value ~y~{0} ~r~cannot be higher than ReadyAngle value ~b~{1}~r~. ~n~~o~Auto-off feature has been ~h~disabled.");
            localizations["audio_warn"] = local.GetValue<string>("Message", "AudioNotFoundWarn", "~r~Audio file ~y~({0})~r~ not found. ~n~~o~AdvancedTurnSignals will continue to work, but ~h~no audio will play~h~ if you perform operations that use the specified audio file.");
            localizations["default_warn"] = local.GetValue<string>("Message", "UseDefaultAudioWarn", "~r~Audio file ~y~({0})~r~ not found. ~n~~o~AdvancedTurnSignals plays default sounds when possible.");
            localizations["audio_err"] = local.GetValue<string>("Message", "AudioLoadErr", "~r~Failed to retrieve audio file. ~n~~o~UseSound feature has been ~h~disabled.");

            localizations["tips"] = local.GetValue<string>("Tips", "Tips", "~b~Tip");
            localizations["reload_tips"] = local.GetValue<string>("Tips", "Reload", "If you want to ~o~reload the audio file~s~, press the ~b~ReloadKey~s~ specified in ~y~ScriptHookVDotNet.ini~s~ to reload the script.");
            #endregion

            if (enabled)
            {
                KeyDown += keyDown;
                Tick += onTick;

                icon = NotificationIcon.LsCustoms; //ロスサントス・カスタムアイコン
                Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["information"], localizations["loaded"]); //~h~で太字。2回目に使うとそこから先を太字解除。


                if (autooff)
                {
                    float res = 0; //数値変換結果

                    if (!float.TryParse(raw_ready_angle, out res)) //数値でない場合
                    {
                        string message = localizations["readyangle_warn"];
                        message = message.Replace("{0}", raw_ready_angle); //特定部分を変数の値に置き換える

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"],message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_ready_angle = "15";
                    }

                    if (!float.TryParse(raw_off_angle, out res)) //数値でない場合
                    {
                        string message = localizations["offangle_warn"];
                        message = message.Replace("{0}", raw_off_angle); //特定部分を変数の値に置き換える

                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], message); //~h~で太字。2回目に使うとそこから先を太字解除。
                        raw_off_angle = "10";
                    }

                    //使用できる形に変換
                    ready_angle = float.Parse(raw_ready_angle);
                    off_angle = float.Parse(raw_off_angle);
                    if (ready_angle > 40 || off_angle > 40) //GTAVの角度40度を超えた設定の場合
                    {
                        icon = NotificationIcon.Blocked; //禁止アイコン
                        Notification.Show(icon, $"{localizations["scriptname"]}~s~ - {localizations["versionchar"]}{ver}", localizations["warning"], localizations["overangle_warn"]); //~h~で太字。2回目に使うとそこから先を太字解除。

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
                }
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

            Task.Delay(1000);
            if (Game.Player.Character.CurrentVehicle.IsLeftIndicatorLightOn | Game.Player.Character.CurrentVehicle.IsRightIndicatorLightOn)
            {
                player = new SoundPlayer($@"scripts\AdvancedTurnSignals\TurnSignalSounds\{selectedsounds[2]}.wav");
                player.PlayLooping();
            }

        }

        /// <summary>
        /// 方向指示器が作動できる乗り物であるか
        /// </summary>
        /// <param name="vehicle">車両オブジェクト</param>
        /// <returns>Trueで方向指示器の作動可能</returns>
        private bool available(Vehicle vehicle)
        {
            if (black_class_list.Contains(vehicle.ClassType))
            {
                return false;
            }
            return true;
        }

        private async void keyDown(object sender, KeyEventArgs e)
        {

            Vehicle cv = Game.Player.Character.CurrentVehicle;

            if (enabled && cv != null && new Keys[] { left,right,hazard }.Contains(e.KeyCode)) //有効かつ車に乗っているかつ設定したキーバインドが押されている
            {
                bool a = available(cv);
                if (a) //方向指示器が作動できる乗り物である場合
                {
                    if (e.KeyCode == left)
                    {
                        if (use_sound)
                        {
                            // ウインカー再生
                            Task t = Task.Run(() => { signal_manual(); });
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

                            }
                        }
                        else
                        {
                            turn_left = !turn_left;
                        }
                    }
                    else if (e.KeyCode == right)
                    {
                        if (use_sound)
                        {
                            // ウインカー再生
                            Task t = Task.Run(() => { signal_manual(); });
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
                            }
                        }
                        else
                        {
                            turn_right = !turn_right;
                        }
                    }
                    else if (e.KeyCode == hazard)
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
                            Task t = Task.Run(() => { signal_manual("hazard"); });
                        }

                    }

                }
            }

            if (use_sound && new Keys[] { pause1,pause2,escape }.Contains(e.KeyCode))
            {
                await Task.Delay(1000);
                if (Game.IsPaused) //ポーズ中の場合
                {
                    if (player != null)
                    {
                        player.Stop();
                        player.Dispose();
                        player = null;
                    }
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
            Vehicle cv = Game.Player.Character.CurrentVehicle;
            if(cv != gcv) //以前保存した車両と情報が違う場合
            {
                gcv = cv; //車両情報更新
                if(cv != null)
                {
                    sounds_load(cv.DisplayName);
                }
            }
            if(cv == null && player!= null)  //非乗車状態でサウンドが鳴っている場合
            {
                player.Stop();
                player.Dispose();
                player = null;
            }
            else if(cv != null && player == null && cv.IsLeftIndicatorLightOn | cv.IsRightIndicatorLightOn) //車両乗車時にウインカーが有効の場合
            {
                player = new SoundPlayer($@"scripts\AdvancedTurnSignals\TurnSignalSounds\{selectedsounds[2]}.wav"); //サウンド再生(loop)
                player.PlayLooping();

            }

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
                                Task t = Task.Run(() => { signal_autooff(); });
                            }


                        }
                        else if (off_ready && cv.SteeringAngle < off_angle) //自動消灯条件を満たした場合
                        {
                            cv.IsLeftIndicatorLightOn = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                Task t = Task.Run(() => { signal_autooff(); });
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
                                Task t = Task.Run(() => { signal_autooff(); });
                            }

                        }
                        else if (off_ready && cv.SteeringAngle > -off_angle)  //自動消灯条件を満たした場合
                        {
                            cv.IsRightIndicatorLightOn = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                Task t = Task.Run(() => { signal_autooff(); });
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
                                Task t = Task.Run(() => { signal_autooff(); });
                            }
                        }
                        else if (off_ready && cv.SteeringAngle < off_angle) //自動消灯条件を満たした場合
                        {
                            turn_left = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                Task t = Task.Run(() => { signal_autooff(); });
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
                                Task t = Task.Run(() => { signal_autooff(); });
                            }
                        }
                        else if (off_ready && cv.SteeringAngle > -off_angle) //自動消灯条件を満たした場合
                        {
                            turn_right = false; //消灯
                            off_ready = false;

                            if (use_sound)
                            {
                                Task t = Task.Run(() => { signal_autooff(); });
                            }
                        }
                    }

                }
            }

        }
    }

}
