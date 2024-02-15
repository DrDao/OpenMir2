﻿using OpenMir2;
using OpenMir2.Data;
using OpenMir2.Extensions;
using OpenMir2.Packets.ClientPackets;
using OpenMir2.Packets.ServerPackets;
using System.Text;
using System.Threading.Channels;
using SystemModule;
using SystemModule.Actors;

namespace M2Server.Net
{
    public class ChannelMessageHandler
    {
        private readonly GameGate _channelGate;
        private readonly SocketSendQueue _sendQueue;
        private object RunSocketSection { get; }
        public readonly string ConnectionId;

        public ChannelMessageHandler(GameGate channelGate)
        {
            _channelGate = channelGate;
            ConnectionId = _channelGate.ConnectionId;
            RunSocketSection = new object();
            _sendQueue = new SocketSendQueue(channelGate);
            Start();
        }

        public GameGate GateInfo => _channelGate;

        public void Start()
        {
            _sendQueue.ProcessSendQueue(CancellationToken.None);
        }

        public void Stop()
        {
            //await _sendQueue.Stop();
        }

        /// <summary>
        /// 添加到网关发送队列
        /// M2Server -> GameGate
        /// </summary>
        /// <returns></returns>
        public void ProcessBufferSend(byte[] sendData)
        {
            if (!GateInfo.Connected)
            {
                return;
            }
            const string sExceptionMsg = "[Exception] TRunSocket::SendGateBuffers -> SendBuff";
            try
            {
                int sendBuffLen = sendData.Length;
                if (sendBuffLen == 0) //不发送空包
                {
                    return;
                }
                _sendQueue.SendMessage(sendData);
                M2Share.NetworkMonitor.Send(sendBuffLen);
            }
            catch (Exception e)
            {
                LogService.Error(sExceptionMsg);
                LogService.Error(e.StackTrace);
            }
        }

        internal void SendCheck(ushort nIdent)
        {
            ServerMessage msgHeader = new ServerMessage
            {
                PacketCode = Grobal2.PacketCode,
                Socket = 0,
                Ident = nIdent,
                PackLength = 0
            };
            byte[] data = SerializerUtil.Serialize(msgHeader);
            _sendQueue.SendMessage(data);
        }

        public void ProcessDataBuffer(ServerMessage packetHeader, ReadOnlySpan<byte> message)
        {
            const string sExceptionMsg = "[Exception] GameGate::ProcessReceiveBuffer";
            try
            {
                if (packetHeader.PacketCode != Grobal2.PacketCode)
                {
                    return;
                }

                if (packetHeader.PackLength > 0)
                {
                    ExecGateBuffers(packetHeader, message, packetHeader.PackLength);
                }
                else
                {
                    ExecGateBuffers(packetHeader, null, 0);
                }
            }
            catch (Exception ex)
            {
                LogService.Error(sExceptionMsg);
                LogService.Error(ex.StackTrace);
            }
        }

        /// <summary>
        /// 执行网关封包消息
        /// </summary>
        private void ExecGateBuffers(ServerMessage msgPacket, ReadOnlySpan<byte> msgBuff, int nMsgLen)
        {
            const string sExceptionMsg = "[Exception] ThreadSocket::ExecGateMsg";
            try
            {
                int nUserIdx;
                switch (msgPacket.Ident)
                {
                    case Grobal2.GM_OPEN:
                        string userIp = HUtil32.GetString(msgBuff, nMsgLen);
                        nUserIdx = OpenNewUser(msgPacket.Socket, msgPacket.SessionId, userIp, ref GateInfo.UserList);
                        SendNewUserMsg(GateInfo.ConnectionId, msgPacket.Socket, msgPacket.SessionId, nUserIdx + 1);
                        GateInfo.UserCount++;
                        break;
                    case Grobal2.GM_CLOSE:
                        CloseUser(msgPacket.Socket);
                        break;
                    case Grobal2.GM_CHECKCLIENT:
                        GateInfo.SendKeepAlive = true;
                        break;
                    case Grobal2.GM_RECEIVE_OK:
                        GateInfo.CheckStatus = true;
                        break;
                    case Grobal2.GM_DATA:
                        SessionUser gateUser = null;
                        if (msgPacket.SessionIndex >= 1)
                        {
                            nUserIdx = msgPacket.SessionIndex - 1;
                            if (GateInfo.UserList.Count > nUserIdx)
                            {
                                gateUser = GateInfo.UserList[nUserIdx];
                                if (gateUser != null && gateUser.Socket != msgPacket.Socket)
                                {
                                    gateUser = null;
                                }
                            }
                        }
                        if (gateUser == null)
                        {
                            for (int i = 0; i < GateInfo.UserList.Count; i++)
                            {
                                if (GateInfo.UserList[i] == null)
                                {
                                    continue;
                                }
                                if (GateInfo.UserList[i].Socket == msgPacket.Socket)
                                {
                                    gateUser = GateInfo.UserList[i];
                                    break;
                                }
                            }
                        }
                        if (gateUser != null)
                        {
                            if (gateUser.PlayObject != null && gateUser.WorldEngine != null)
                            {
                                if (gateUser.Certification && nMsgLen >= 12)
                                {
                                    CommandMessage mesaagePacket = new CommandMessage();
                                    mesaagePacket.Recog = BitConverter.ToInt32(msgBuff[..4]);
                                    mesaagePacket.Ident = BitConverter.ToUInt16(msgBuff.Slice(4, 2));
                                    mesaagePacket.Param = BitConverter.ToUInt16(msgBuff.Slice(6, 2));
                                    mesaagePacket.Tag = BitConverter.ToUInt16(msgBuff.Slice(8, 2));
                                    mesaagePacket.Series = BitConverter.ToUInt16(msgBuff.Slice(10, 2));
                                    if (nMsgLen == 12)
                                    {
                                        SystemShare.WorldEngine.ProcessUserMessage(gateUser.PlayObject, mesaagePacket, null);
                                    }
                                    else
                                    {
                                        string codeBuff = EDCode.DeCodeString(msgBuff[12..^1]); //var sMsg = EDCode.DeCodeString(HUtil32.GetString(msgBuff, 12, msgBuff.Length - 13));
                                        SystemShare.WorldEngine.ProcessUserMessage(gateUser.PlayObject, mesaagePacket, codeBuff);
                                    }
                                }
                            }
                            else
                            {
                                DoClientCertification(gateUser, msgPacket.Socket, msgBuff.ToString(Encoding.ASCII));
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogService.Error(sExceptionMsg);
                LogService.Error(ex);
            }
        }

        private bool GetCertification(ReadOnlySpan<char> sMsg, ref string sAccount, ref string sChrName, ref int nSessionId, ref int nVersion, ref bool boFlag, ref byte[] tHwid, ref int gateId)
        {
            bool result = false;
            string sCodeStr = string.Empty;
            string sClientVersion = string.Empty;
            string sHwid = string.Empty;
            string sIdx = string.Empty;
            string sGateId = string.Empty;
            const string sExceptionMsg = "[Exception] ThreadSocket::DoClientCertification -> GetCertification";
            try
            {
                ReadOnlySpan<char> packetMsg = sMsg[1..];
                string sData = EDCode.DeCodeString(packetMsg);
                if (sData.Length > 2 && sData[0] == '*' && sData[1] == '*')
                {
                    sData = sData.AsSpan()[2..sData.Length].ToString();
                    sData = HUtil32.GetValidStr3(sData, ref sAccount, HUtil32.Backslash);
                    sData = HUtil32.GetValidStr3(sData, ref sChrName, HUtil32.Backslash);
                    sData = HUtil32.GetValidStr3(sData, ref sCodeStr, HUtil32.Backslash);
                    sData = HUtil32.GetValidStr3(sData, ref sClientVersion, HUtil32.Backslash);
                    sData = HUtil32.GetValidStr3(sData, ref sIdx, HUtil32.Backslash);
                    sData = HUtil32.GetValidStr3(sData, ref sHwid, HUtil32.Backslash);
                    sData = HUtil32.GetValidStr3(sData, ref sGateId, HUtil32.Backslash);
                    nSessionId = HUtil32.StrToInt(sCodeStr, 0);
                    if (sIdx == "0")
                    {
                        boFlag = true;
                    }
                    else
                    {
                        boFlag = false;
                    }
                    if (!string.IsNullOrEmpty(sAccount) && !string.IsNullOrEmpty(sChrName) && nSessionId >= 2 && !string.IsNullOrEmpty(sHwid))
                    {
                        nVersion = HUtil32.StrToInt(sClientVersion, 0);
                        tHwid = MD5.MD5UnPrInt(sHwid);
                        result = true;
                    }
                    gateId = HUtil32.StrToInt(sGateId, -1);
                    if (gateId == -1)
                    {
                        result = false;
                    }
                    LogService.Debug($"Account:[{sAccount}] ChrName:[{sChrName}] Code:[{sCodeStr}] ClientVersion:[{sClientVersion}] HWID:[{sHwid}]");
                }
            }
            catch (Exception ex)
            {
                LogService.Error(sExceptionMsg);
                LogService.Error(ex);
            }
            return result;
        }

        private void DoClientCertification(SessionUser gateUser, int nSocket, string sMsg)
        {
            string sAccount = string.Empty;
            string sChrName = string.Empty;
            int nSessionId = 0;
            bool boFlag = false;
            int nClientVersion = 0;
            int nPayMent = 0;
            int nPayMode = 0;
            long nPlayTime = 0L;
            byte[] hwid = MD5.EmptyDigest;
            int gateIdx = 0;
            const string sExceptionMsg = "[Exception] ThreadSocket::DoClientCertification";
            const string sDisable = "*disable*";
            try
            {
                if (string.IsNullOrEmpty(gateUser.Account))
                {
                    if (HUtil32.TagCount(sMsg, '!') > 0)
                    {
                        HUtil32.ArrestStringEx(sMsg, "#", "!", ref sMsg);
                        if (GetCertification(sMsg, ref sAccount, ref sChrName, ref nSessionId, ref nClientVersion, ref boFlag, ref hwid, ref gateIdx))
                        {
                            AccountSession sessInfo = M2Share.Authentication.GetAdmission(sAccount, gateUser.IPaddr, nSessionId, ref nPayMode, ref nPayMent, ref nPlayTime);
                            if (sessInfo != null && nPayMent > 0)
                            {
                                gateUser.Certification = true;
                                gateUser.Account = sAccount.Trim();
                                gateUser.SessionId = nSessionId;
                                LoadDBInfo loadRcdInfo = new LoadDBInfo
                                {
                                    Account = sAccount,
                                    ChrName = sChrName,
                                    sIPaddr = gateUser.IPaddr,
                                    SessionID = nSessionId,
                                    SoftVersionDate = nClientVersion,
                                    PayMent = nPayMent,
                                    PayMode = nPayMode,
                                    SocketId = nSocket,
                                    GSocketIdx = gateUser.SocketId,
                                    GateIdx = gateIdx,
                                    NewUserTick = HUtil32.GetTickCount(),
                                    PlayTime = nPlayTime
                                };
                                M2Share.FrontEngine.AddToLoadRcdList(loadRcdInfo);
                            }
                            else
                            {
                                gateUser.Account = sDisable;
                                gateUser.Certification = false;
                                CloseUser(nSocket);
                                LogService.Warn($"会话验证失败.Account:{sAccount} SessionId:{nSessionId} Address:{gateUser.IPaddr}");
                            }
                        }
                        else
                        {
                            gateUser.Account = sDisable;
                            gateUser.Certification = false;
                            CloseUser(nSocket);
                        }
                    }
                }
            }
            catch
            {
                LogService.Error(sExceptionMsg);
            }
        }

        public void CloseUser(int nSocket)
        {
            if (GateInfo == null || GateInfo.UserList == null)
            {
                return;
            }
            if (GateInfo.UserList.Count > 0)
            {
                HUtil32.EnterCriticalSections(RunSocketSection);
                try
                {
                    for (int i = 0; i < GateInfo.UserList.Count; i++)
                    {
                        if (GateInfo.UserList[i] != null)
                        {
                            SessionUser gateUser = GateInfo.UserList[i];
                            if (gateUser == null)
                            {
                                continue;
                            }
                            if (gateUser.Socket == nSocket)
                            {
                                if (gateUser.FrontEngine != null)
                                {
                                    gateUser.FrontEngine.DeleteHuman(i, gateUser.Socket);
                                }
                                if (gateUser.PlayObject != null)
                                {
                                    if (!gateUser.PlayObject.OffLineFlag)
                                    {
                                        gateUser.PlayObject.BoSoftClose = true;
                                    }
                                    if (gateUser.PlayObject.Ghost && !gateUser.PlayObject.BoReconnection)
                                    {
                                        M2Share.Authentication.SendHumanLogOutMsg(gateUser.Account, gateUser.SessionId);
                                    }
                                    if (gateUser.PlayObject.BoSoftClose && gateUser.PlayObject.BoReconnection && gateUser.PlayObject.BoEmergencyClose)
                                    {
                                        M2Share.Authentication.SendHumanLogOutMsg(gateUser.Account, gateUser.SessionId);
                                    }
                                }
                                GateInfo.UserList[i] = null;
                                GateInfo.UserCount -= 1;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    HUtil32.LeaveCriticalSections(RunSocketSection);
                }
            }
        }

        private static int OpenNewUser(int socket, ushort socketId, string sIPaddr, ref IList<SessionUser> userList)
        {
            SessionUser gateUser = new SessionUser
            {
                Account = string.Empty,
                IPaddr = sIPaddr,
                Socket = socket,
                SocketId = socketId,
                SessionId = 0,
                WorldEngine = null,
                FrontEngine = null,
                PlayObject = null,
                Certification = false
            };
            for (int i = 0; i < userList.Count; i++)
            {
                if (userList[i] == null)
                {
                    userList[i] = gateUser;
                    return i;
                }
            }
            userList.Add(gateUser);
            LogService.Info($"玩家链接...[{sIPaddr}]");
            return userList.Count - 1;
        }

        private void SendNewUserMsg(string connectionId, int nSocket, ushort socketId, int nUserIdex)
        {
            ServerMessage msgHeader = new ServerMessage();
            msgHeader.PacketCode = Grobal2.PacketCode;
            msgHeader.Socket = nSocket;
            msgHeader.SessionId = socketId;
            msgHeader.Ident = Grobal2.GM_SERVERUSERINDEX;
            msgHeader.SessionIndex = (ushort)nUserIdex;
            msgHeader.PackLength = 0;
            byte[] data = SerializerUtil.Serialize(msgHeader);
            ProcessBufferSend(data);
        }

        /// <summary>
        /// 设置用户对应网关编号
        /// </summary>
        public void SetGateUserList(int nSocket, IPlayerActor playObject)
        {
            HUtil32.EnterCriticalSection(RunSocketSection);
            try
            {
                for (int i = 0; i < GateInfo.UserList.Count; i++)
                {
                    SessionUser gateUserInfo = GateInfo.UserList[i];
                    if (gateUserInfo != null && gateUserInfo.Socket == nSocket)
                    {
                        gateUserInfo.FrontEngine = null;
                        gateUserInfo.WorldEngine = SystemShare.WorldEngine;
                        gateUserInfo.PlayObject = playObject;
                        break;
                    }
                }
            }
            finally
            {
                HUtil32.LeaveCriticalSection(RunSocketSection);
            }
        }

        private class SocketSendQueue
        {
            private Channel<byte[]> SendQueue { get; }
            private string ConnectionId { get; }

            public SocketSendQueue(GameGate gateInfo)
            {
                SendQueue = Channel.CreateUnbounded<byte[]>();
                ConnectionId = gateInfo.ConnectionId;
            }

            /// <summary>
            /// 获取队列消息数量
            /// </summary>
            public int GetQueueCount => SendQueue.Reader.Count;

            /// <summary>
            /// 添加到发送队列
            /// </summary>
            public void SendMessage(byte[] buffer)
            {
                SendQueue.Writer.TryWrite(buffer);
            }

            public async Task Stop()
            {
                if (SendQueue.Reader.Count > 0)
                {
                    await SendQueue.Reader.Completion;
                }
            }

            /// <summary>
            /// 处理队列数据并发送到GameGate
            /// M2Server.-> GameGate
            /// </summary>
            public void ProcessSendQueue(CancellationToken cancellation)
            {
                Task.Factory.StartNew(async () =>
                {
                    while (await SendQueue.Reader.WaitToReadAsync(cancellation))
                    {
                        while (SendQueue.Reader.TryRead(out byte[] buffer))
                        {
                            try
                            {
                                M2Share.NetChannel.Send(ConnectionId, buffer);
                            }
                            catch (Exception ex)
                            {
                                LogService.Error(ex.StackTrace);
                            }
                            //GameGate.Socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
                        }
                    }
                }, cancellation);
            }
        }
    }
}