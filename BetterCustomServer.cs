using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using Platforms;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BetterCustomServer
{
    [BepInPlugin("errorawa.repo.customserver", "BetterCustomServer", "0.5.1")]
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
            BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Message, BetterCustomServer.language.Value == "ZH" ? "BetterCustomServer 加载完毕" : "Loaded BetterCustomServer mod");
        }

        void Bind()
        {
            RegionConfig = Config.Bind<string>("Server", "Server Region", "h k", new ConfigDescription("Select server region", new AcceptableValueList<string>("a s i a", "a u", "c a e", "e u", "h k", "i n", "j p", "r u", "r u e", "z a", "s a", "k r", "t r", "u a e", "u s", "u s w", "u s s c")));
            isChinaServerConfig = Config.Bind<bool>("Server", "Use China Server", false, "If enable this option, Region will only work on Voice Server.");
            useVoice = Config.Bind<bool>("Server", "Use Voice Server", true, "If disable this option, the lobby will not support voice chat.");
            playerCount = Config.Bind<int>("Server", "Max Player Count", 8, new ConfigDescription("Change max player count.", new AcceptableValueRange<int>(2, 20)));
            language = Config.Bind<string>("Debug", "Language", "ZH", new ConfigDescription("Select mod language", new AcceptableValueList<string>("EN", "ZH")));
            enableLog = Config.Bind<bool>("Debug", "Log Output", true, "Enable log output");
        }

        public static new void Logger(BepInEx.Logging.LogLevel logLevel, string logContent)
        {
            if (enableLog.Value)
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
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "找到游戏服务器（国际区）" : "Found Game Server (Global)");
                BetterCustomServer.AppIdRealtime = File.ReadAllText("C:\\PhotonPunGlobal.txt");
                isChinaServer = false;
                findServer = true;
            }
            if (File.Exists("C:\\PhotonPunChina.txt") && BetterCustomServer.isChinaServerConfig.Value)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "找到游戏服务器（中国区）" : "Found Game Server (China)");
                BetterCustomServer.AppIdRealtime = File.ReadAllText("C:\\PhotonPunChina.txt");
                isChinaServer = true;
                findServer = true;
            }
            else if (findServer && BetterCustomServer.isChinaServerConfig.Value)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Warning, BetterCustomServer.language.Value == "ZH" ? "未找到中国区服务器，但你依旧可以使用国际区服务器创建大厅" : "China Server not found, but you can still use Global Server to create lobby.");
            }
            if (!findServer && BetterCustomServer.AppIdRealtime == "none")
            {
                return false;
            }
            if (useVoice.Value)
            {
                if (File.Exists("C:\\PhotonVoice.txt"))
                {
                    BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "找到语音服务器" : "Found Voice Server");
                    BetterCustomServer.AppIdVoice = File.ReadAllText("C:\\PhotonVoice.txt");
                }
                else
                {
                    BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Warning, BetterCustomServer.language.Value == "ZH" ? "未找到语音服务器，但你依旧可以创建大厅" : "Voice Server not found, but you can still create lobby.");
                }
            }
            else
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "已禁用语音服务器" : "Voice Server disabled.");
                BetterCustomServer.AppIdVoice = "none";
            }
            BetterCustomServer.Region = BetterCustomServer.RegionConfig.Value.Replace(" ", "");
            return true;
        }
        public static string EncryptionECB(string encryptString)     //AES ECB加密
        {
            string text = DateTime.Now.Year.ToString().Substring(2, 2) + DateTime.Now.ToString("MMddHH");
            string s = text + text;
            byte[] inArray;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(s);
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform transform = aes.CreateEncryptor();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(encryptString);
                        }
                    }
                    inArray = memoryStream.ToArray();
                }
            }
            return Convert.ToBase64String(inArray);
        }

        public static string[] DecryptionECB(string decryString)    //AES ECB解密
        {
            string text = DateTime.Now.Year.ToString().Substring(2, 2) + DateTime.Now.ToString("MMddHH");
            string s = text + text;
            string text2 = string.Empty;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(s);
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform transform = aes.CreateDecryptor();
                using (MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(decryString)))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            text2 = streamReader.ReadToEnd();
                        }
                    }
                }
            }
            return text2.Split('·');
        }

        public static void CopyInviteCode()
        {   //复制邀请码
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
            BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "已将邀请码复制到剪贴板" : "Already copy the InviteCode to clipboard");
        }

        public static ConfigEntry<string> RegionConfig;
        public static ConfigEntry<bool> isChinaServerConfig;
        public static ConfigEntry<bool> useVoice;
        public static ConfigEntry<int> playerCount;
        public static ConfigEntry<bool> enableLog;
        public static ConfigEntry<string> language;
        public static string AppIdRealtime = "none";
        public static string AppIdVoice = "none";
        public static string Region = "none";
        public static string RoomName = "none";
        public static string VoiceRoomName = "none";
        public static bool isChinaServer = false;
        public static string[] inviteCode = new string[5];
        public static EnterRoomParams enterRoomParams = new EnterRoomParams();
        public static bool isHost = false;
        public static bool[] isKicked = new bool[2] { false, false };
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
            if (!BetterCustomServer.isHost)   //加入房间，传入房间ID
            {
                Traverse.Create(__instance).Field("RoomName").SetValue(BetterCustomServer.RoomName);
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, (BetterCustomServer.language.Value == "ZH" ? "加入大厅：" : "Joining Lobby: ") + Traverse.Create(__instance).Field("RoomName").GetValue<string>());
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch("CreateLobby", MethodType.Enumerator)]
        public static IEnumerable<CodeInstruction> CreateLobbyPatch(IEnumerable<CodeInstruction> instructions)
        {   //去除原有代码中的 房间ID & 服务器区域
            var codes = instructions.ToList();
            bool flag = false;
            int index = 0;
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
                                        BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, (BetterCustomServer.language.Value == "ZH" ? "注入 JoinRoomName：" : "Patch JoinRoomName:") + i.ToString() + "-" + (i + 5).ToString());
                                        flag = true;
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
            if (!flag)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "未找到 JoinRoomName" : "JoinRoomName not found");
            }
            flag = false;
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
                                            BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, (BetterCustomServer.language.Value == "ZH" ? "注入 JoinRegion：" : "Patch JoinRegion: ") + i.ToString() + "-" + (i + 6).ToString());
                                            flag = true;
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
            if (!flag)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "未找到 JoinRegion" : "JoinRegion not found");
            }
            flag = false;
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
                                    BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, (BetterCustomServer.language.Value == "ZH" ? "注入 CreateFixedRegion：" : "Patch CreateFixedRegion: ") + i.ToString() + "-" + (i + 4).ToString());
                                    flag = true;
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
            if (!flag)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "未找到 CreateFixedRegion" : "CreateFixedRegion not found");
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
                PlayerTtl = 10000,
                IsVisible = false
            };
            if (BetterCustomServer.isHost)
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
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "设置Voice连接区域：" : "Set Voice Connect Region: " + BetterCustomServer.Region);
                appSettings.FixedRegion = BetterCustomServer.Region;
                appSettings.Server = "ns.photonengine.io";
                PhotonNetwork.AuthValues = new AuthenticationValues
                {
                    AuthType = CustomAuthenticationType.None
                };
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnStatusChanged")]
        public static bool OnStatusChanged(LoadBalancingClient __instance, ref StatusCode statusCode)
        {   //重写语音掉线执行的内容
            if (statusCode == StatusCode.TimeoutDisconnect || statusCode == StatusCode.DisconnectByServerTimeout)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Error, BetterCustomServer.language.Value == "ZH" ? "语音服务器连接超时，正在重连" : "Voice Server connect timeout. Reconnecting");
                __instance.OpJoinOrCreateRoom(BetterCustomServer.enterRoomParams);
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("OpLeaveRoom")]
        public static void OpLeaveRoomPrefix(ref bool becomeInactive, ref bool sendAuthCookie)
        {   //判断你是否是被房主踢出的
            if (!becomeInactive && PhotonNetwork.EnableCloseConnection && BetterCustomServer.isKicked[1] == false)
            {
                BetterCustomServer.isKicked[0] = true;
                PhotonNetwork.EnableCloseConnection = false;
                GameDirector.instance.OutroStart();
                Traverse.Create(NetworkManager.instance).Field("leavePhotonRoom").SetValue(true);
            }
        }
    }

    [HarmonyPatch(typeof(PlatformManager))]
    public class PlatformManagerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("SetPhotonAuthTicket")]
        public static void SetPhotonAuthTicketPostfix()
        {
            BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "移除Photon认证" : "Remove Photon Auth");
            PhotonNetwork.AuthValues.AuthType = CustomAuthenticationType.None;
        }
    }

    [HarmonyPatch(typeof(SteamLobbyManager))]
    public class SteamLobbyManagerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnLobbyCreated")]
        public static void OnLobbyCreatedPostfix(Result _result, Lobby _lobby)
        {   //创建邀请码并复制到剪贴板
            BetterCustomServer.RoomName = _lobby.Id.ToString();
            BetterCustomServer.CopyInviteCode();
        }
        public static void RequestGameLobbyJoin(ulong lobbyId)
        {   //加入房间功能
            Traverse.Create(SteamLobbyManager.instance).Method("OnGameLobbyJoinRequested", new Lobby(lobbyId), default(SteamId)).GetValue();
        }
    }

    [HarmonyPatch(typeof(MenuPageMain))]
    public class MenuPageMainPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Start")]
        public static void StartPrefix()
        {   //判断踢出，显示提示
            if (BetterCustomServer.isKicked[0] == true && BetterCustomServer.isKicked[1] == false)
            {
                MenuManager.instance.PagePopUp("Info", UnityEngine.Color.red, BetterCustomServer.language.Value == "ZH" ? "你已被踢出大厅" : "You were kicked out of lobby", "OK", true);
            }
            BetterCustomServer.isKicked[0] = false;
            BetterCustomServer.isKicked[1] = false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch("ButtonEventHostGame")]
        public static IEnumerable<CodeInstruction> ButtonEventHostGamePatch(IEnumerable<CodeInstruction> instructions)
        {   //覆写开房间打开的界面  去除选择最佳区域界面
            var codes = instructions.ToList();
            bool flag = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_S)
                {
                    BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, (BetterCustomServer.language.Value == "ZH" ? "注入 HostGame：" : "Patch HostGame: ") + i.ToString());
                    flag = true;
                    codes[i].operand = 11;
                    break;
                }
            }
            if (!flag)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "未找到 HostGame" : "HostGame not found");
            }
            return codes.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch("ButtonEventHostGame")]
        public static bool ButtonEventHostGamePrefix()
        {   //重置flag & 添加界面参数为多人游戏
            BetterCustomServer.passwordLog = false;
            BetterCustomServer.isHost = true;
            if (!BetterCustomServer.SetAppId())   //设置服务器ID
            {
                MenuManager.instance.PagePopUp("ERROR", UnityEngine.Color.red, BetterCustomServer.language.Value == "ZH" ? "未找到服务器 AppId" : "Server AppId not found!", BetterCustomServer.language.Value == "ZH" ? "你 AppId 呢？" : "Where is your AppId?", true);
                return false;
            }
            SemiFunc.MainMenuSetMultiplayer();
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("ButtonEventPlayRandom")]
        public static bool ButtonEventPlayRandomPatch()
        {   //覆写公共大厅功能
            BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Warning, BetterCustomServer.language.Value == "ZH" ? "不支持公共大厅！" : "Not support Public Game!");
            MenuManager.instance.PageCloseAll();
            MenuManager.instance.PagePopUp("Sorry", UnityEngine.Color.yellow, BetterCustomServer.language.Value == "ZH" ? "我没做这个功能" : "I don't make this function", "OK", true);
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
                        BetterCustomServer.inviteCode[1] = BetterCustomServer.language.Value == "ZH" ? "邀请码已过期" : "The InviteCode has timed out.";
                        BetterCustomServer.inviteCode[2] = BetterCustomServer.language.Value == "ZH" ? "请重新复制一个邀请码" : "Please recopy an InviteCode";
                    }
                }
                else
                {
                    BetterCustomServer.inviteCode[0] = "none";
                    BetterCustomServer.inviteCode[1] = BetterCustomServer.language.Value == "ZH" ? "未找到邀请码" : "InviteCode not found.";
                    BetterCustomServer.inviteCode[2] = BetterCustomServer.language.Value == "ZH" ? "你码呢？" : "Where is your InviteCode?";
                }
            }
            catch
            {
                BetterCustomServer.inviteCode[0] = "none";
                BetterCustomServer.inviteCode[1] = BetterCustomServer.language.Value == "ZH" ? "未找到邀请码" : "InviteCode not found.";
                BetterCustomServer.inviteCode[2] = BetterCustomServer.language.Value == "ZH" ? "你剪贴板是空的" : "Your clipboard is empty";
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
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, (BetterCustomServer.language.Value == "ZH" ? "请求加入大厅：" : "Request join Lobby: ") + lobbyId.ToString());
                SteamLobbyManagerPatch.RequestGameLobbyJoin(lobbyId);
            }
            else
            {   //邀请码解密失败
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Error, BetterCustomServer.language.Value == "ZH" ? "加入大厅失败！" : "Join Lobby failed!");
                MenuManager.instance.PagePopUp("ERROR", UnityEngine.Color.red, BetterCustomServer.inviteCode[1], BetterCustomServer.inviteCode[2], true);
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
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "移除大厅密码" : "Remove Lobby password");
            }
            __instance.ConfirmButton();
        }
    }

    [HarmonyPatch(typeof(MenuPageLobby))]
    public class MenuPageLobbyPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("ButtonLeave")]
        public static void ButtonLeavePrefix()
        {   //自己点的退出按钮，不判断为踢出
            BetterCustomServer.isKicked[1] = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("ButtonInvite")]
        public static bool ButtonInvitePatch()
        {   //覆写邀请按钮为复制邀请码
            BetterCustomServer.CopyInviteCode();
            MenuManager.instance.PagePopUp("Info", UnityEngine.Color.blue, BetterCustomServer.language.Value == "ZH" ? "已将邀请码复制到剪贴板" : "Already copy the InviteCode to clipboard", "OK", true);
            return false;
        }
    }

    [HarmonyPatch(typeof(NetworkManager))]
    public class NetworkManagerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("BanPlayer")]
        public static void BanPlayerPostfix(ref PlayerAvatar _playerAvatar)
        {   //覆写踢出功能
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions
            {
                TargetActors = new int[]
                {
                    _playerAvatar.photonView.OwnerActorNr
                }
            };
            PhotonNetwork.NetworkingClient.OpRaiseEvent(203, null, raiseEventOptions, SendOptions.SendReliable);
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
            bool flag = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Dup)
                {
                    if (codes[i + 1].opcode == OpCodes.Ldc_I4)
                    {
                        if (codes[i + 2].opcode == OpCodes.Stfld)
                        {
                            BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, (BetterCustomServer.language.Value == "ZH" ? "注入 VoiceRoomTtl：" : "Patch VoiceRoomTtl: ") + (i + 1).ToString());
                            flag = true;
                            codes[i + 1].operand = 0x2710;
                            break;
                        }
                    }
                }
            }
            if (!flag)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "未找到 VoiceRoomTtl" : "VoiceRoomTtl not found");
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
                    PlayerTtl = 10000
                },
                RoomName = voiceRoomName
            };
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnOperationResponseReceived")]
        public static bool OnOperationResponseReceivedPatch(VoiceFollowClient __instance, ref OperationResponse _operationResponse)
        {   //覆写首次连接语音失败的异常接收
            if (_operationResponse.ReturnCode != 0)
            {
                if (_operationResponse.OperationCode == 226)
                {   //换用OpRejoinRoom重新连接
                    BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Warning, BetterCustomServer.language.Value == "ZH" ? "再次连接语音服务器" : "Connect to Voice Server again");
                    LoadBalancingClient loadBalancingClient = new LoadBalancingClient();
                    loadBalancingClient.OpRejoinRoom(BetterCustomServer.VoiceRoomName);
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(RunManager))]
    public class RunManagerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("ChangeLevel")]
        public static void ChangeLevelPostfix(RunManager __instance)
        {   //判断当前场景是否可用踢出
            if (__instance.levelCurrent == __instance.levelLobbyMenu)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "启用踢出" : "Enable kick");
                PhotonNetwork.EnableCloseConnection = true;
            }
            else
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "禁用踢出" : "Disable kick");
                PhotonNetwork.EnableCloseConnection = false;
            }
        }
    }

    [HarmonyPatch(typeof(UnityMicrophone))]
    public class UnityMicrophonePatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch("CheckDevice")]
        public static IEnumerable<CodeInstruction> CheckDevicePatch(IEnumerable<CodeInstruction> instructions)
        {   //去除 语音部分 Warning 日志
            var codes = instructions.ToList();
            bool flag = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr)
                {
                    if (codes[i].operand.ToString() == "microphone does not support suggested frequency {0} (min: {1}, max: {2}). Setting to {2}")
                    {
                        flag = true;
                        codes[i - 2].opcode = OpCodes.Ldc_I4_4;
                        BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, (BetterCustomServer.language.Value == "ZH" ? "注入 CheckDevice：" : "Patch CheckDevice: ") + (i - 2).ToString());
                        break;
                    }
                }
            }
            if (!flag)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "未找到 CheckDevice" : "CheckDevice not found");
            }
            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(VoiceClient))]
    public class VoiceClientPatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch("onFrame")]
        public static IEnumerable<CodeInstruction> onFramePatch(IEnumerable<CodeInstruction> instructions)
        {   //去除 语音部分 Warning 日志
            var codes = instructions.ToList();
            bool flag = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr)
                {
                    if (codes[i].operand.ToString() == "[PV] Frame event for voice #")
                    {
                        flag = true;
                        codes[i - 1].opcode = OpCodes.Ldc_I4_4;
                        BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, (BetterCustomServer.language.Value == "ZH" ? "注入 onFrame：" : "Patch onFrame: ") + (i - 1).ToString());
                        break;
                    }
                }
            }
            if (!flag)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "未找到 onFrame" : "onFrame not found");
            }
            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(PhotonVoiceView))]
    public class PhotonVoiceViewPatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch("Start")]
        public static IEnumerable<CodeInstruction> StartPatch(IEnumerable<CodeInstruction> instructions)
        {   //去除 部分语音 Warning 日志
            var codes = instructions.ToList();
            bool flag = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr)
                {
                    if (codes[i].operand.ToString() == "PhotonVoiceView.RecorderInUse.TransmitEnabled is false, don't forget to set it to true to enable transmission.")
                    {
                        flag = true;
                        codes[i - 1].opcode = OpCodes.Ldc_I4_4;
                        BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, (BetterCustomServer.language.Value == "ZH" ? "注入 PhotonVoiceView：" : "Patch PhotonVoiceView: ") + (i - 1).ToString());
                        break;
                    }
                }
            }
            if (!flag)
            {
                BetterCustomServer.Logger(BepInEx.Logging.LogLevel.Info, BetterCustomServer.language.Value == "ZH" ? "未找到 PhotonVoiceView" : "PhotonVoiceView not found");
            }
            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(Sound))]
    public class SoundPatch
    {
        [HarmonyFinalizer]
        [HarmonyPatch("PlayLoop")]
        public static Exception PlayLoopFinalizer()
        {   //拦截音频异常日志输出
            return null;
        }
    }

    [HarmonyPatch(typeof(ItemBattery))]
    public class ItemBatteryPatch
    {
        [HarmonyFinalizer]
        [HarmonyPatch("BatteryLookAt")]
        public static Exception BatteryLookAtFinalizer()
        {   //拦截物品电量异常日志输出
            return null;
        }
    }

    [HarmonyPatch(typeof(PowerCrystalValuable))]
    public class PowerCrystalValuablePatch
    {
        [HarmonyFinalizer]
        [HarmonyPatch("Update")]
        public static Exception UpdateFinalizer()
        {   //拦截异常日志输出
            return null;
        }
    }

    [HarmonyPatch(typeof(PlayerCrawlTrigger))]
    public class PlayerCrawlTriggerPatch
    {
        [HarmonyFinalizer]
        [HarmonyPatch("Update")]
        public static Exception UpdateFinalizer()
        {   //拦截异常日志输出
            return null;
        }
    }

    [HarmonyPatch(typeof(ItemMine))]
    public class ItemMinePath
    {
        [HarmonyFinalizer]
        [HarmonyPatch("Update")]
        public static Exception UpdateFinalizer()
        {   //拦截异常日志输出
            return null;
        }
    }

    [HarmonyPatch(typeof(ItemGrenade))]
    public class ItemGrenadePath
    {
        [HarmonyFinalizer]
        [HarmonyPatch("FixedUpdate")]
        public static Exception FixedUpdateFinalizer()
        {   //拦截异常日志输出
            return null;
        }
    }
}
