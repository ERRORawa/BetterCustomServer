using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using Photon.Pun;
using Photon.Realtime;
using System.IO;
using Photon.Voice.Unity;
using Photon.Voice.PUN;
using System.Runtime.CompilerServices;
using Photon.Voice;
using ExitGames.Client.Photon;
using System.Security.Cryptography;
using System.Text;
using Steamworks;
using Steamworks.Data;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine.Assertions.Must;
using Microsoft.SqlServer.Server;

namespace BetterCustomServer
{
    [BepInPlugin("errorawa.repo.customserver", "BetterCustomServer", "0.2.0")]
    public class BetterCustomServer : BaseUnityPlugin
    {
        void Awake()
        {
            this.Patch();
        }
        void Patch()    //加载Mod & 注册Log服务
        {
            Harmony harmony = new Harmony("errorawa.repo.customserver");
            BepInEx.Logging.Logger.Sources.Add(Log);
            this.Bind();
            harmony.PatchAll();
            BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Message, "Loaded BetterCustomServer mod");
        }

        void Bind()
        {
            RegionConfig = Config.Bind<string>("Server", "Server Region", "jp", new ConfigDescription("Select server region", new AcceptableValueList<string>("asia", "au", "cae", "eu", "in", "jp", "ru", "rue", "za", "sa", "kr", "tr", "us", "usw")));
            isChinaServerConfig = Config.Bind<bool>("Server", "Use China Server", false, "If enable this option, Region will only work on Voice Server.");
            useVoice = Config.Bind<bool>("Server", "Use Voice Server", true, "If disable this option, the lobby will not support voice chat.");
            playerCount = Config.Bind<int>("Server", "Max Player Count", 8, new ConfigDescription("Change max player count.", new AcceptableValueRange<int>(2, 20)));
            enableLog = Config.Bind<bool>("Logger", "Log Output", true, "Enable log output");

        }

        public static void Logger(BepInEx.Logging.LogLevel logLevel, string logContent)
        {
            if(enableLog.Value)
            {
                switch (logLevel)
                {
                    case BepInEx.Logging.LogLevel.Info:
                        BetterCustomServer.Log.LogInfo(logContent);
                        break;
                    case BepInEx.Logging.LogLevel.Error:
                        BetterCustomServer.Log.LogError(logContent);
                        break;
                    case BepInEx.Logging.LogLevel.Warning:
                        BetterCustomServer.Log.LogWarning(logContent);
                        break;
                    case BepInEx.Logging.LogLevel.Message:
                        BetterCustomServer.Log.LogMessage(logContent);
                        break;
                }
            }
        }
        public static bool SetAppId()       //读取服务器ID
        {
            BetterCustomServer.RoomName = "none";
            bool findServer = false;
            if (File.Exists("C:\\PhotonPunGlobal.txt"))
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Found Game Server (Global)");
                BetterCustomServer.AppIdRealtime = File.ReadAllText("C:\\PhotonPunGlobal.txt");
                isChinaServer = false;
                findServer = true;
            }
            if (File.Exists("C:\\PhotonPunChina.txt") && BetterCustomServer.isChinaServerConfig.Value)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Found Game Server (China)");
                BetterCustomServer.AppIdRealtime = File.ReadAllText("C:\\PhotonPunChina.txt");
                isChinaServer = true;
                findServer = true;
            }
            else if(findServer && BetterCustomServer.isChinaServerConfig.Value)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Warning, "China Server not found, but you can still use Global Server to create lobby.");
            }
            if(!findServer && BetterCustomServer.AppIdRealtime == "none")
            {
                return false;
            }
            if (useVoice.Value)
            {
                if (File.Exists("C:\\PhotonVoice.txt"))
                {
                    BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Found Voice Server");
                    BetterCustomServer.AppIdVoice = File.ReadAllText("C:\\PhotonVoice.txt");
                }
                else
                {
                    BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Warning, "Voice Server not found, but you can still create lobby.");
                }
            }
            else
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Voice Server disabled.");
                BetterCustomServer.AppIdVoice = "none";
            }
            BetterCustomServer.Region = BetterCustomServer.RegionConfig.Value;
            return true;
        }
        public static string EncryptionECB(string encryString)
        {
            return "do it yourself";
        }

        public static string[] DecryptionECB(string decryString)
        {
            return "do it yourself";
        }

        public static void CopyInviteCode()
        {
            string useChinaServer = "false";
            if (BetterCustomServer.isChinaServer)
            {
                useChinaServer = "true";
            }
            GUIUtility.systemCopyBuffer = BetterCustomServer.EncryptionECB(string.Concat(new string[]
            {
                useChinaServer,
                "·",
                BetterCustomServer.AppIdRealtime,
                "·",
                BetterCustomServer.RoomName,
                "·",
                BetterCustomServer.AppIdVoice,
                "·",
                BetterCustomServer.Region
            }));
            BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Alread copy the InviteCode to clipboard");
        }

        public static ConfigEntry<string> RegionConfig;
        public static ConfigEntry<bool> isChinaServerConfig;
        public static ConfigEntry<bool> useVoice;
        public static ConfigEntry<int> playerCount;
        public static ConfigEntry<bool> enableLog;
        public static string AppIdRealtime = "none";
        public static string AppIdVoice = "none";
        public static string Region = "none";
        public static string RoomName = "none";
        public static string VoiceRoomName = "none";
        public static bool isChinaServer = false;
        public static string[] inviteCode = new string[5];
        public static EnterRoomParams enterRoomParams = new EnterRoomParams();
        public static bool isHost = false;
        public static bool passwordLog = false;
        public static ManualLogSource Log = new ManualLogSource("BetterCustomServer");
    }

    [HarmonyPatch(typeof(NetworkConnect))]
    public class NetworkConnectPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Start")]
        public static void StartPrefix(NetworkConnect __instance)
        {
            if(!BetterCustomServer.isHost)   //加入房间，传入房间ID
            {
                Traverse.Create(__instance).Field("RoomName").SetValue(BetterCustomServer.RoomName);
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Joining Lobby: " + Traverse.Create(__instance).Field("RoomName").GetValue<string>());
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch("CreateLobby", MethodType.Enumerator)]
        public static IEnumerable<CodeInstruction> CreateLobbyPatch(IEnumerable<CodeInstruction> instructions)
        {   //去除原有代码中的 房间ID & 服务器区域
            var codes = instructions.ToList();
            int index = 13;
            for (int i = index; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_1)
                {
                    if (codes[i + 1].opcode == OpCodes.Ldsfld)
                    {
                        if (codes[i + 2].opcode == OpCodes.Ldflda)
                        {
                            if (codes[i + 3].opcode == OpCodes.Ldstr)
                            {
                                if (codes[i + 3].operand.ToString() == "RoomName" && codes[i + 4].opcode == OpCodes.Call)
                                {
                                    if (codes[i + 5].opcode == OpCodes.Stfld)
                                    {
                                        BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Patch JoinRoomName:" + i.ToString() + "-" + (i + 5).ToString());
                                        index = i + 6;
                                        codes[i].opcode = OpCodes.Nop;    //房员的 RoomName
                                        codes[i + 1].opcode = OpCodes.Nop;
                                        codes[i + 2].opcode = OpCodes.Nop;
                                        codes[i + 3].opcode = OpCodes.Nop;
                                        codes[i + 4].opcode = OpCodes.Nop;
                                        codes[i + 5].opcode = OpCodes.Nop;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            for (int i = index; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call)
                {
                    if (codes[i + 1].opcode == OpCodes.Ldfld)
                    {
                        if (codes[i + 2].opcode == OpCodes.Ldsfld)
                        {
                            if (codes[i + 3].opcode == OpCodes.Ldflda)
                            {
                                if (codes[i + 4].opcode == OpCodes.Ldstr)
                                {
                                    if (codes[i + 4].operand.ToString() == "Region" && codes[i + 5].opcode == OpCodes.Call)
                                    {
                                        if (codes[i + 6].opcode == OpCodes.Stfld)
                                        {
                                            BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Patch JoinRegion: " + i.ToString() + "-" + (i + 6).ToString());
                                            index = i + 7;
                                            codes[i].opcode = OpCodes.Nop;    //房员的 FixedRegion
                                            codes[i + 1].opcode = OpCodes.Nop;
                                            codes[i + 2].opcode = OpCodes.Nop;
                                            codes[i + 3].opcode = OpCodes.Nop;
                                            codes[i + 4].opcode = OpCodes.Nop;
                                            codes[i + 5].opcode = OpCodes.Nop;
                                            codes[i + 6].opcode = OpCodes.Nop;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            for (int i = index; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_S)
                {
                    if (codes[i + 1].opcode == OpCodes.Ldsfld)
                    {
                        if (codes[i + 2].opcode == OpCodes.Ldfld)
                        {
                            if (codes[i + 3].opcode == OpCodes.Ldfld)
                            {
                                if (codes[i + 4].opcode == OpCodes.Call)
                                {
                                    if (codes[i + 5].opcode == OpCodes.Brfalse)
                                    {
                                        BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Patch RemoveVersionCheck: " + (i + 5).ToString());
                                        index = i + 6;
                                        codes[i + 1].opcode = OpCodes.Nop;  //去除版本检查
                                        codes[i + 2].opcode = OpCodes.Nop;
                                        codes[i + 3].opcode = OpCodes.Ldstr;
                                        codes[i + 3].operand = "nop";
                                        codes[i + 5].opcode = OpCodes.Brtrue;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            for (int i = index; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call)
                {
                    if (codes[i + 1].opcode == OpCodes.Ldfld)
                    {
                        if (codes[i + 2].opcode == OpCodes.Ldsfld)
                        {
                            if (codes[i + 3].opcode == OpCodes.Ldfld)
                            {
                                if (codes[i + 4].opcode == OpCodes.Stfld)
                                {
                                    BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Patch CreateFixedRegion: " + i.ToString() + "-" + (i + 4).ToString());
                                    index = i + 5;
                                    codes[i].opcode = OpCodes.Nop;    //房主的 FixedRegion
                                    codes[i + 1].opcode = OpCodes.Nop;
                                    codes[i + 2].opcode = OpCodes.Nop;
                                    codes[i + 3].opcode = OpCodes.Nop;
                                    codes[i + 4].opcode = OpCodes.Nop;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            for (int i = index; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldsfld)
                {
                    if (codes[i + 1].opcode == OpCodes.Callvirt)
                    {
                        BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Patch RemoveSteamAuth: " + i.ToString() + "-" + (i + 1).ToString());
                        codes[i].opcode = OpCodes.Nop;    //去除发送Steam认证信息（会导致游戏报错无法创建房间）
                        codes[i + 1].opcode = OpCodes.Nop;
                        break;
                    }
                }
            }
            return codes.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch("CreateLobby", MethodType.Enumerator)]
        public static void CreateLobbyPrefix()
        {
            //设置连接属性
            PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = true;
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime = BetterCustomServer.AppIdRealtime;
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdVoice = BetterCustomServer.AppIdVoice;
            PhotonNetwork.PhotonServerSettings.AppSettings.AuthMode = AuthModeOption.AuthOnce;
            if (BetterCustomServer.isChinaServer)
            {
                PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "cn";
                PhotonNetwork.PhotonServerSettings.AppSettings.Server = "ns.photonengine.cn";
            }
            else
            {
                PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = BetterCustomServer.Region;
                PhotonNetwork.PhotonServerSettings.AppSettings.Server = "ns.photonengine.io";
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("TryJoiningRoom")]
        public static bool TryJoiningRoomPatch(NetworkConnect __instance)
        {   //覆写加入房间（防止加入不存在的房间导致卡加载）
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = BetterCustomServer.playerCount.Value,
                PlayerTtl = 300000,
                IsVisible = false
            };
            if(BetterCustomServer.isHost)
            {
                PhotonNetwork.JoinOrCreateRoom(Traverse.Create(__instance).Field("RoomName").GetValue<string>(), roomOptions, DataDirector.instance.privateLobby, null);
            }
            else
            {
                PhotonNetwork.JoinRoom(Traverse.Create(__instance).Field("RoomName").GetValue<string>(), null);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(LoadBalancingClient))]
    public class LoadBalancingClientPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("ConnectUsingSettings")]
        public static void ConnectUsingSettingsPrefix(LoadBalancingClient __instance, ref AppSettings appSettings)
        {   //覆写语音连接服务器
            if (__instance.ClientType == ClientAppType.Voice)
            {
                appSettings.FixedRegion = BetterCustomServer.Region;
                appSettings.Server = "ns.photonengine.io";
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch("OnStatusChanged")]
        public static IEnumerable<CodeInstruction> OnStatusChangedPatch(IEnumerable<CodeInstruction> instructions)
        {   //移除TimeoutDisconnect执行的内容
            var codes = instructions.ToList();
            for(int i = 400; i < codes.Count; i++)
            {
                if (codes[i + 6].opcode == OpCodes.Ldstr && codes[i + 18].opcode == OpCodes.Ldc_I4_8)
                {
                    if (codes[i + 6].operand.ToString() == "Connection lost. OnStatusChanged to {0}. Client state was: {1}. {2}")
                    {
                        BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Patch OnStatusChanged: " + i.ToString());
                        codes[i].opcode = OpCodes.Ret;
                        break;
                    }
                }
            }
            return codes.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnStatusChanged")]
        public static bool OnStatusChanged(LoadBalancingClient __instance, ref StatusCode statusCode)
        {   //重写语音掉线执行的内容
            if(statusCode == StatusCode.TimeoutDisconnect)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Error, "Voice Server connect timeout. Reconnecting");
                __instance.OpJoinOrCreateRoom(BetterCustomServer.enterRoomParams);
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(SteamManager))]
    public class SteamManagerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("SendSteamAuthTicket")]
        public static bool SendSteamAuthTicketPatch()
        {   //防止认证类型被更改为Steam（语音服务会无法连接）
            BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Remove Steam Auth");
            PhotonNetwork.AuthValues.AuthType = CustomAuthenticationType.None;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnLobbyCreated")]
        public static void OnLobbyCreatedPostfix(Result _result, Lobby _lobby)
        {   //创建邀请码并复制到剪贴板
            BetterCustomServer.RoomName = _lobby.Id.ToString();
            BetterCustomServer.CopyInviteCode();
        }
        public static void RequestGameLobbyJoin(ulong lobbyId)
        {   //加入房间功能
            Traverse.Create(SteamManager.instance).Method("OnGameLobbyJoinRequested", new Lobby(lobbyId), default(SteamId)).GetValue();
        }
    }

    [HarmonyPatch(typeof(MenuPageMain))]
    public class MenuPageMainPatch
    {   //主界面
        [HarmonyTranspiler]
        [HarmonyPatch("ButtonEventHostGame")]
        public static IEnumerable<CodeInstruction> ButtonEventHostGamePatch(IEnumerable<CodeInstruction> instructions)
        {   //覆写开房间打开的界面  去除选择最佳区域界面
            var codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_S)
                {
                    BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Patch HostGame: " + i.ToString());
                    codes[i].operand = 11;
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch("ButtonEventHostGame")]
        public static bool ButtonEventHostGamePrefix()
        {   //重置flag & 添加界面参数为多人游戏
            BetterCustomServer.passwordLog = false;
            BetterCustomServer.isHost = true;
            if(!BetterCustomServer.SetAppId())   //设置服务器ID
            {
                MenuManager.instance.PagePopUp("ERROR", UnityEngine.Color.red, "Server AppId not found!", "Where is your AppId?", true);
                return false;
            }
            SemiFunc.MainMenuSetMultiplayer();
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("ButtonEventPlayRandom")]
        public static bool ButtonEventPlayRandomPatch()
        {   //覆写公共大厅功能
            BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Warning, "Not support Public Game!");
            MenuManager.instance.PageCloseAll();
            MenuManager.instance.PagePopUp("Sorry", UnityEngine.Color.yellow, "I don't make this function", "OK", true);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("ButtonEventJoinGame")]
        public static bool ButtonEventJoinGamePatch()
        {   //覆写加入房间功能
            BetterCustomServer.passwordLog = false;
            BetterCustomServer.isHost = false;
            string systemCopyBuffer = GUIUtility.systemCopyBuffer;
            try    //解密邀请码
            {
                if (systemCopyBuffer.Substring(systemCopyBuffer.Length - 1, 1) == "=")
                {
                    try
                    {
                        BetterCustomServer.inviteCode = BetterCustomServer.DecryptionECB(systemCopyBuffer);
                    }
                    catch
                    {
                        BetterCustomServer.inviteCode[0] = "none";
                        BetterCustomServer.inviteCode[1] = "The InviteCode has timed out.";
                        BetterCustomServer.inviteCode[2] = "Please recopy an InviteCode";
                    }
                }
                else
                {
                    BetterCustomServer.inviteCode[0] = "none";
                    BetterCustomServer.inviteCode[1] = "InviteCode not found.";
                    BetterCustomServer.inviteCode[2] = "Where is your InviteCode?";
                }
            }
            catch
            {
                BetterCustomServer.inviteCode[0] = "none";
                BetterCustomServer.inviteCode[1] = "InviteCode not found.";
                BetterCustomServer.inviteCode[2] = "Your clipboard is empty";
            }
            ulong lobbyId;
            if (BetterCustomServer.inviteCode[0] != "none" && ulong.TryParse(BetterCustomServer.inviteCode[2], out lobbyId))
            {   //邀请码解密成功，设置服务器参数并加入房间
                BetterCustomServer.AppIdRealtime = BetterCustomServer.inviteCode[1];
                BetterCustomServer.RoomName = BetterCustomServer.inviteCode[2];
                BetterCustomServer.AppIdVoice = BetterCustomServer.inviteCode[3];
                BetterCustomServer.Region = BetterCustomServer.inviteCode[4];
                if (BetterCustomServer.inviteCode[0] == "true")
                {
                    BetterCustomServer.isChinaServer = true;
                }
                else
                {
                    BetterCustomServer.isChinaServer = false;
                }
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Request join Lobby: " + lobbyId.ToString());
                SteamManagerPatch.RequestGameLobbyJoin(lobbyId);
            }
            else
            {   //邀请码解密失败
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Error, "Join Lobby failed!");
                MenuManager.instance.PagePopUp("ERROR", UnityEngine.Color.red , BetterCustomServer.inviteCode[1], BetterCustomServer.inviteCode[2], true);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MenuPagePassword))]
    public class MenuPagePasswordPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void UpdatePostfix(MenuPagePassword __instance)
        {   //去除房间密码功能
            if (!BetterCustomServer.passwordLog)
            {
                BetterCustomServer.passwordLog = true;
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Remove Lobby password");
            }
            __instance.ConfirmButton();
        }
    }

    [HarmonyPatch(typeof(VoiceFollowClient))]
    public class VoiceFollowClientPatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch("JoinVoiceRoom")]
        public static IEnumerable<CodeInstruction> JoinVoiceRoomPatch(IEnumerable<CodeInstruction> instructions)
        {   //覆写语音房间的非活跃超时参数
            var codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Dup)
                {
                    if (codes[i + 1].opcode == OpCodes.Ldc_I4)
                    {
                        if (codes[i + 2].opcode == OpCodes.Stfld)
                        {
                            BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, "Patch VoiceRoomTtl: " + (i + 1).ToString());
                            codes[i + 1].operand = 0x7530;
                            break;
                        }
                    }
                }
            }
            return codes.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch("JoinVoiceRoom")]
        public static void JoinVoiceRoomPrefix(ref string voiceRoomName)
        {   //存储语音房间变量，用于掉线重连
            BetterCustomServer.VoiceRoomName = voiceRoomName;
            BetterCustomServer.enterRoomParams = new EnterRoomParams
            {
                RoomOptions = new RoomOptions
                {
                    IsVisible = false,
                    PlayerTtl = 30000
                },
                RoomName = voiceRoomName
            };
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnOperationResponseReceived")]
        public static bool OnOperationResponseReceivedPatch(VoiceFollowClient __instance, ref OperationResponse operationResponse)
        {   //覆写首次连接语音失败的异常接收
            if (operationResponse.ReturnCode != 0)
            {
                if(operationResponse.OperationCode == 226)
                {   //换用OpRejoinRoom重新连接
                    BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Warning, "Connect to Voice Server again");
                    LoadBalancingClient loadBalancingClient = new LoadBalancingClient();
                    loadBalancingClient.OpRejoinRoom(BetterCustomServer.VoiceRoomName);
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Sound))]
    public class SoundPatch
    {
        [HarmonyFinalizer]
        [HarmonyPatch("PlayLoop")]
        public static Exception PlayLoopFinalizer()
        {   //拦截 PlayLoop 的异常日志输出
            return null;
        }
    }

    [HarmonyPatch(typeof(ItemBattery))]
    public class ItemBatteryPatch
    {
        [HarmonyFinalizer]
        [HarmonyPatch("BatteryLookAt")]
        public static Exception BatteryLookAtFinalizer()
        {   //拦截 BatteryLookAt 的异常日志输出
            return null;
        }
    }

    [HarmonyPatch(typeof(PowerCrystalValuable))]
    public class PowerCrystalValuablePatch
    {
        [HarmonyFinalizer]
        [HarmonyPatch("Update")]
        public static Exception UpdateFinalizer()
        {   //拦截 Update 的异常日志输出
            return null;
        }
    }

    [HarmonyPatch(typeof(ChatManager))]
    public class ChatManagerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("MessageSend")]
        public static bool MessageSendPrefix(ChatManager __instance, bool _possessed)
        {
            if (!_possessed)
            {
                if (Traverse.Create(__instance).Field("chatMessage").GetValue<string>() == "/copy")
                {
                    BetterCustomServer.CopyInviteCode();
                    return false;
                }
            }
            return true;
        }
    }
}
