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
using UnityEngine.Assertions.Must;

namespace REPOCrack
{
    [BepInPlugin("errorawa.repo.crack", "REPO破解Mod", "0.2.0")]
    public class REPOCrack : BaseUnityPlugin
    {
        void Awake()
        {
            this.Patch();
        }
        void Patch()    //加载Mod & 注册Log服务
        {
            Harmony harmony = new Harmony("errorawa.repo.crack");
            BepInEx.Logging.Logger.Sources.Add(Log);
            REPOCrack.Log.LogMessage("欢迎使用REPO破解Mod！");
            REPOCrack.Log.LogMessage("Mod作者: ERROR");
            REPOCrack.Log.LogMessage("作者QQ: 2218878302");
            REPOCrack.Log.LogMessage("请勿将该Mod用于商业途径！");
            harmony.PatchAll();
        }
        public static void SetAppId()       //读取服务器ID
        {
                if (File.Exists("C:\\PhotonPunGlobal.txt"))
                {
                    REPOCrack.Log.LogInfo("找到服务端(国际区)");
                    REPOCrack.Region = "jp";
                    REPOCrack.AppIdRealtime = File.ReadAllText("C:\\PhotonPunGlobal.txt");
                }
                if (File.Exists("C:\\PhotonPunChina.txt"))
                {
                    REPOCrack.Log.LogInfo("找到服务端(国区)");
                    REPOCrack.Region = "cn";
                    REPOCrack.AppIdRealtime = File.ReadAllText("C:\\PhotonPunChina.txt");
                }
                if (File.Exists("C:\\PhotonVoice.txt"))
                {
                    REPOCrack.Log.LogInfo("找到语音服务");
                    REPOCrack.AppIdVoice = File.ReadAllText("C:\\PhotonVoice.txt");
                }
        }
        public static string EncryptionECB(string encryString)
        {
		return "make it yourself";
        }

        public static string[] DecryptionECB(string decryString)
        {
		return "make it yourself";
        }

        public static string AppIdRealtime = "none";
        public static string AppIdVoice = "none";
        public static string Region = "jp";
        public static string RoomName = "none";
        public static string VoiceRoomName = "none";
        public static string[] inviteCode = new string[5];
        public static EnterRoomParams enterRoomParams = new EnterRoomParams();
        public static bool isHost = false;
        public static bool serverLog = false;
        public static bool passwordLog = false;
        public static ManualLogSource Log = new ManualLogSource("REPOCrack");
    }

    [HarmonyPatch(typeof(NetworkConnect))]
    public class NetworkConnectPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Start")]
        public static void StartPrefix(NetworkConnect __instance)
        {
            if(!REPOCrack.isHost)   //加入房间，传入房间ID
            {
                Traverse.Create(__instance).Field("RoomName").SetValue(REPOCrack.RoomName);
                REPOCrack.Log.LogInfo("正在加入房间: " + Traverse.Create(__instance).Field("RoomName").GetValue<string>());
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch("CreateLobby", MethodType.Enumerator)]
        public static IEnumerable<CodeInstruction> CreateLobbyPatch(IEnumerable<CodeInstruction> instructions)
        {   //去除原有代码中的 房间ID & 服务器区域 Steam验证
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
                                        REPOCrack.Log.LogInfo("注入JoinRoomName:" + i.ToString() + "-" + (i + 5).ToString());
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
                                            REPOCrack.Log.LogInfo("注入JoinRegion: " + i.ToString() + "-" + (i + 6).ToString());
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
                                        REPOCrack.Log.LogInfo("注入RemoveVersionCheck: " + (i + 5).ToString());
                                        index = i + 6;
                                        codes[i + 5].opcode = OpCodes.Brtrue; //反转 判断 游戏版本是否一致
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
                                    REPOCrack.Log.LogInfo("注入CreateFixedRegion: " + i.ToString() + "-" + (i + 4).ToString());
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
                        REPOCrack.Log.LogInfo("注入RemoveSteamAuth: " + i.ToString() + "-" + (i + 1).ToString());
                        codes[i].opcode = OpCodes.Nop;    //去除发送Steam认证信息（SendSteamAuthTicket）
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
        {   //更改连接服务器
            if (!REPOCrack.serverLog)   //IEnumerator的特性，所以需要设置flag防止重复执行
            {
                REPOCrack.serverLog = true;
                if(REPOCrack.isHost)
                {
                    REPOCrack.SetAppId();   //房主 设置服务器ID
                }
            }
            //设置连接属性
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = REPOCrack.Region;
            PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = true;
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime = REPOCrack.AppIdRealtime;
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdVoice = REPOCrack.AppIdVoice;
            PhotonNetwork.PhotonServerSettings.AppSettings.AuthMode = AuthModeOption.AuthOnce;
            if (REPOCrack.Region == "cn")
            {
                PhotonNetwork.PhotonServerSettings.AppSettings.Server = "ns.photonengine.cn";
            }
            else
            {
                PhotonNetwork.PhotonServerSettings.AppSettings.Server = "ns.photonengine.io";
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("TryJoiningRoom")]
        public static bool TryJoiningRoomPatch(NetworkConnect __instance)
        {   //覆写加入房间（防止加入不存在的房间导致卡加载）
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 6,
                PlayerTtl = 300000,
                IsVisible = false
            };
            if(REPOCrack.isHost)
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
                appSettings.FixedRegion = "jp";
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
                        REPOCrack.Log.LogInfo("注入OnStatusChanged: " + i.ToString());
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
                REPOCrack.Log.LogError("语音服务连接超时，重连中");
                __instance.OpJoinOrCreateRoom(REPOCrack.enterRoomParams);
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
            REPOCrack.Log.LogInfo("移除Steam认证");
            PhotonNetwork.AuthValues.AuthType = CustomAuthenticationType.None;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnLobbyCreated")]
        public static void OnLobbyCreatedPostfix(Result _result, Lobby _lobby)
        {   //创建邀请码并复制到剪贴板
            REPOCrack.RoomName = _lobby.Id.ToString();
            GUIUtility.systemCopyBuffer = REPOCrack.EncryptionECB(string.Concat(new string[]
            {
                "true·",
                REPOCrack.AppIdRealtime,
                "·",
                REPOCrack.RoomName,
                "·",
                REPOCrack.AppIdVoice,
                "·",
                REPOCrack.Region
            }));
            REPOCrack.Log.LogInfo("邀请码已复制到剪贴板");
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
        {   //覆写开房间打开的界面
            var codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_S)
                {
                    REPOCrack.Log.LogInfo("注入HostGame: " + i.ToString());
                    codes[i].operand = 11;
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch("ButtonEventHostGame")]
        public static void ButtonEventHostGamePrefix()
        {   //重置flag & 添加界面参数为多人游戏
            REPOCrack.serverLog = false;
            REPOCrack.passwordLog = false;
            REPOCrack.isHost = true;
            REPOCrack.RoomName = "none";
            SemiFunc.MainMenuSetMultiplayer();
        }

        [HarmonyPrefix]
        [HarmonyPatch("ButtonEventPlayRandom")]
        public static bool ButtonEventPlayRandomPatch()
        {   //覆写公共大厅功能
            REPOCrack.Log.LogWarning("公共大厅功能未开发！");
            MenuManager.instance.PageCloseAll();
            MenuManager.instance.PagePopUp("别看了", UnityEngine.Color.yellow, "公共大厅功能没做", "好的", true);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("ButtonEventJoinGame")]
        public static bool ButtonEventJoinGamePatch()
        {   //覆写加入房间功能
            REPOCrack.serverLog = false;    //重置flag
            REPOCrack.passwordLog = false;
            REPOCrack.isHost = false;
            string systemCopyBuffer = GUIUtility.systemCopyBuffer;
            try    //解密邀请码
            {
                if (systemCopyBuffer.Substring(systemCopyBuffer.Length - 1, 1) == "=")
                {
                    try
                    {
                        REPOCrack.inviteCode = REPOCrack.DecryptionECB(systemCopyBuffer);
                    }
                    catch
                    {
                        REPOCrack.inviteCode[0] = "false";
                        REPOCrack.inviteCode[1] = "邀请码已过期";
                        REPOCrack.inviteCode[2] = "超时啦！";
                    }
                }
                else
                {
                    REPOCrack.inviteCode[0] = "false";
                    REPOCrack.inviteCode[1] = "未识别到邀请码";
                    REPOCrack.inviteCode[2] = "笨比，你码呢？";
                }
            }
            catch
            {
                REPOCrack.inviteCode[0] = "false";
                REPOCrack.inviteCode[1] = "未识别到邀请码";
                REPOCrack.inviteCode[2] = "诶？你剪贴板怎么是空的";
            }
            ulong lobbyId;
            if (REPOCrack.inviteCode[0] == "true" && ulong.TryParse(REPOCrack.inviteCode[2], out lobbyId))
            {   //邀请码解密成功，设置服务器参数并加入房间
                REPOCrack.AppIdRealtime = REPOCrack.inviteCode[1];
                REPOCrack.RoomName = REPOCrack.inviteCode[2];
                REPOCrack.AppIdVoice = REPOCrack.inviteCode[3];
                REPOCrack.Region = REPOCrack.inviteCode[4];
                REPOCrack.Log.LogInfo("请求加入房间: " + lobbyId.ToString());
                SteamManagerPatch.RequestGameLobbyJoin(lobbyId);
            }
            else
            {   //邀请码解密失败
                REPOCrack.Log.LogError("加入房间失败！");
                MenuManager.instance.PagePopUp("错误", UnityEngine.Color.red , REPOCrack.inviteCode[1], REPOCrack.inviteCode[2], true);
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
            if (!REPOCrack.passwordLog)
            {
                REPOCrack.passwordLog = true;
                REPOCrack.Log.LogInfo("移除房间密码");
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
                            REPOCrack.Log.LogInfo("注入VoiceRoomTtl: " + (i + 1).ToString());
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
            REPOCrack.VoiceRoomName = voiceRoomName;
            REPOCrack.enterRoomParams = new EnterRoomParams
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
                    REPOCrack.Log.LogWarning("语音服务二次连接中");
                    LoadBalancingClient loadBalancingClient = new LoadBalancingClient();
                    loadBalancingClient.OpRejoinRoom(REPOCrack.VoiceRoomName);
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
}
