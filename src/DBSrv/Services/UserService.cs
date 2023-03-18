using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DBSrv.Conf;
using DBSrv.Storage;
using DBSrv.Storage.Model;
using NLog;
using SystemModule;
using SystemModule.Data;
using SystemModule.Packets.ClientPackets;
using SystemModule.Packets.ServerPackets;
using SystemModule.Sockets;
using SystemModule.Sockets.AsyncSocketServer;

namespace DBSrv.Services
{
    /// <summary>
    /// 角色数据服务
    /// DBSrv-SelGate-Client
    /// </summary>
    public class UserService
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly DbSrvConf _conf;
        private readonly IPlayDataStorage _playDataStorage;
        private readonly IPlayRecordStorage _playRecordStorage;
        private readonly SocketServer _userSocket;
        private readonly LoginSessionService _loginService;
        private readonly Channel<UserGateMessage> _reviceQueue;
        private readonly Dictionary<string, SelGateInfo> _gateMap;

        public UserService(DbSrvConf conf, LoginSessionService loginService, IPlayRecordStorage playRecord, IPlayDataStorage playData)
        {
            _loginService = loginService;
            _playRecordStorage = playRecord;
            _playDataStorage = playData;
            _gateMap = new Dictionary<string, SelGateInfo>(StringComparer.OrdinalIgnoreCase);
            _reviceQueue = Channel.CreateUnbounded<UserGateMessage>();
            _userSocket = new SocketServer(byte.MaxValue, 1024);
            _userSocket.OnClientConnect += UserSocketClientConnect;
            _userSocket.OnClientDisconnect += UserSocketClientDisconnect;
            _userSocket.OnClientRead += UserSocketClientRead;
            _userSocket.OnClientError += UserSocketClientError;
            _conf = conf;
        }

        public void Start(CancellationToken stoppingToken)
        {
            _userSocket.Init();
            _playRecordStorage.LoadQuickList();
            _userSocket.Start(_conf.GateAddr, _conf.GatePort);
            StartMessageThread(stoppingToken);
            _logger.Info($"玩家数据网关服务[{_conf.GateAddr}:{_conf.GatePort}]已启动.等待链接...");
        }

        public void Stop()
        {
            var gateList = _gateMap.Values.ToList();
            for (var i = 0; i < gateList.Count; i++)
            {
                var gateInfo = gateList[i];
                if (gateInfo != null)
                {
                    for (var ii = 0; ii < gateInfo.UserList.Count; ii++)
                    {
                        gateInfo.UserList[ii] = null;
                    }
                    gateInfo.UserList = null;
                }
            }
        }

        public IEnumerable<SelGateInfo> GetGates => _gateMap.Values;

        /// <summary>
        /// 处理客户端请求消息
        /// </summary>
        private void StartMessageThread(CancellationToken stoppingToken)
        {
            Task.Factory.StartNew(async () =>
            {
                while (await _reviceQueue.Reader.WaitToReadAsync(stoppingToken))
                {
                    if (_reviceQueue.Reader.TryRead(out var message))
                    {
                        try
                        {
                            var selGata = _gateMap[message.ConnectionId];
                            if (selGata == null)
                            {
                                continue;
                            }
                            ProcessMessage(selGata, message.Packet);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e);
                        }
                    }
                }
            }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        private void ProcessMessage(SelGateInfo gateInfo, ServerDataMessage packet)
        {
            var sTemp = string.Empty;
            var sText = HUtil32.GetString(packet.Data, 0, packet.DataLen);
            HUtil32.ArrestStringEx(sText, "%", "$", ref sTemp);
            for (var i = 0; i < gateInfo.UserList.Count; i++)
            {
                var userInfo = gateInfo.UserList[i];
                if (userInfo != null)
                {
                    if (userInfo.SessionId == packet.SocketId)
                    {
                        userInfo.sText += sText;
                        if (sText.IndexOf("!", StringComparison.OrdinalIgnoreCase) < 1)
                        {
                            continue;
                        }
                        var sData = string.Empty;
                        if (HUtil32.TagCount(userInfo.sText, '!') <= 0)
                        {
                            return;
                        }
                        userInfo.sText = HUtil32.ArrestStringEx(userInfo.sText, "#", "!", ref sData);
                        if (string.IsNullOrEmpty(sData))
                        {
                            userInfo.sText = string.Empty;
                            continue;
                        }
                        sData = sData.Substring(1, sData.Length - 1);
                        if (sData.Length >= Messages.DefBlockSize)
                        {
                            DeCodeUserMsg(sData, gateInfo, ref userInfo);
                        }
                    }
                }
            }
        }

        private void UserSocketClientConnect(object sender, AsyncUserToken e)
        {
            var sIPaddr = e.RemoteIPaddr;
            if (!DBShare.CheckServerIP(sIPaddr))
            {
                e.Socket.Close();
                _logger.Warn("非法地址连接: " + sIPaddr);
                return;
            }
            var selGateInfo = new SelGateInfo();
            selGateInfo.Socket = e.Socket;
            selGateInfo.ConnectionId = e.ConnectionId;
            selGateInfo.RemoteEndPoint = e.EndPoint;
            selGateInfo.UserList = new List<SessionUserInfo>();
            selGateInfo.nGateID = DBShare.GetGateID(sIPaddr);
            _gateMap.Add(e.ConnectionId, selGateInfo);
            _logger.Info(string.Format(sGateOpen, 0, e.RemoteIPaddr, e.RemotePort));
        }

        private const string sGateOpen = "角色网关[{0}]({1}:{2})已打开...";
        private const string sGateClose = "角色网关[{0}]({1}:{2})已关闭...";

        private void UserSocketClientDisconnect(object sender, AsyncUserToken e)
        {
            var gateIndex = -1;
            if (_gateMap.ContainsKey(e.ConnectionId))
            {
                if (_gateMap.TryGetValue(e.ConnectionId, out var gateInfo))
                {
                    if (gateInfo != null && gateInfo.UserList != null)
                    {
                        for (var ii = 0; ii < gateInfo.UserList.Count; ii++)
                        {
                            gateInfo.UserList[ii] = null;
                        }
                        gateInfo.UserList = null;
                    }
                    _logger.Info(string.Format(sGateClose, gateIndex, e.RemoteIPaddr, e.RemotePort));
                }
                gateIndex++;
                _gateMap.Remove(e.ConnectionId);
            }
        }

        private void UserSocketClientError(object sender, AsyncSocketErrorEventArgs e)
        {

        }

        private void ProcessGateData(byte[] data, int nLen,string connectionId, ref SelGateInfo gateInfo)
        {
            var srcOffset = 0;
            Span<byte> dataBuff = data;
            try
            {
                while (nLen >= ServerDataPacket.FixedHeaderLen)
                {
                    var packetHead = dataBuff[..ServerDataPacket.FixedHeaderLen];
                    var message = ServerPacket.ToPacket<ServerDataPacket>(packetHead);
                    if (message.PacketCode != Grobal2.RunGateCode)
                    {
                        srcOffset++;
                        dataBuff = dataBuff.Slice(srcOffset, ServerDataPacket.FixedHeaderLen);
                        nLen -= 1;
                        _logger.Debug($"解析封包出现异常封包，PacketLen:[{dataBuff.Length}] Offset:[{srcOffset}].");
                        continue;
                    }
                    var nCheckMsgLen = Math.Abs(message.PacketLen + ServerDataPacket.FixedHeaderLen);
                    if (nCheckMsgLen > nLen)
                    {
                        break;
                    } 
                    var messageData = SerializerUtil.Deserialize<ServerDataMessage>(dataBuff[ServerDataPacket.FixedHeaderLen..]);
                    switch (messageData.Type)
                    {
                        case ServerDataType.KeepAlive:
                            SendKeepAlivePacket(gateInfo.ConnectionId);
                            _logger.Debug("Received SelGate Heartbeat.");
                            break;
                        case ServerDataType.Enter:
                            var sData = string.Empty;
                            var sTemp = string.Empty;
                            var sText = HUtil32.GetString(messageData.Data, 0, messageData.DataLen);
                            HUtil32.ArrestStringEx(sText, "%", "$", ref sData);
                            sData = HUtil32.GetValidStr3(sData, ref sTemp, HUtil32.Backslash);
                            OpenUser(messageData.SocketId, sData, ref gateInfo);
                            break;
                        case ServerDataType.Leave:
                            CloseUser(messageData.SocketId, ref gateInfo);
                            break;
                        case ServerDataType.Data:
                            var userMessage = new UserGateMessage();
                            userMessage.ConnectionId = connectionId;
                            userMessage.Packet = messageData;
                            _reviceQueue.Writer.TryWrite(userMessage);
                            break;
                    }
                    nLen -= nCheckMsgLen;
                    if (nLen <= 0)
                    {
                        break;
                    }
                    dataBuff = dataBuff.Slice(nCheckMsgLen, nLen);
                    gateInfo.DataLen = nLen;
                    srcOffset = 0;
                    if (nLen < ServerDataPacket.FixedHeaderLen)
                    {
                        break;
                    }
                }
                if (nLen > 0)//有部分数据被处理,需要把剩下的数据拷贝到接收缓冲的头部
                {
                    MemoryCopy.BlockCopy(dataBuff, 0, gateInfo.Data, 0, nLen);
                    gateInfo.DataLen = nLen;
                }
                else
                {
                    gateInfo.DataLen = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private void UserSocketClientRead(object sender, AsyncUserToken e)
        {
            if (_gateMap.TryGetValue(e.ConnectionId, out var gateInfo))
            {
                var nMsgLen = e.BytesReceived;
                if (gateInfo.DataLen > 0)
                {
                    var packetData = new byte[e.BytesReceived];
                    Buffer.BlockCopy(e.ReceiveBuffer, e.Offset, packetData, 0, nMsgLen);
                    MemoryCopy.BlockCopy(packetData, 0, gateInfo.Data, gateInfo.DataLen, packetData.Length);
                    ProcessGateData(gateInfo.Data, gateInfo.DataLen + nMsgLen, e.ConnectionId, ref gateInfo);
                }
                else
                {
                    Buffer.BlockCopy(e.ReceiveBuffer, e.Offset, gateInfo.Data, 0, nMsgLen);
                    ProcessGateData(gateInfo.Data, nMsgLen, e.ConnectionId, ref gateInfo);
                }
            }
        }

        public int GetUserCount()
        {
            var nUserCount = 0;
            var gateList = _gateMap.Values.ToList();
            for (var i = 0; i < gateList.Count; i++)
            {
                var gateInfo = gateList[i];
                nUserCount += gateInfo.UserList.Count;
            }
            return nUserCount;
        }

        private bool NewChrData(string sAccount, string sChrName, int nSex, int nJob, int nHair)
        {
            if (_playDataStorage.Index(sChrName) != -1) return false;
            var chrRecord = new PlayerDataInfo();
            chrRecord.Header = new RecordHeader
            {
                Name = sChrName,
                sAccount = sAccount
            };
            chrRecord.Data = new PlayerInfoData();
            chrRecord.Data.ChrName = sChrName;
            chrRecord.Data.Account = sAccount;
            chrRecord.Data.Sex = (byte)nSex;
            chrRecord.Data.Job = (byte)nJob;
            chrRecord.Data.Hair = (byte)nHair;
            _playDataStorage.Add(chrRecord);
            return true;
        }

        private void SendKeepAlivePacket(string connectionId)
        {
            var message = new ServerDataMessage();
            message.Type = ServerDataType.KeepAlive;
            SendPacket(connectionId, message);
        }

        /// <summary>
        /// 用户打开会话
        /// </summary>
        private static void OpenUser(int sessionId, string sIp, ref SelGateInfo gateInfo)
        {
            var sUserIPaddr = string.Empty;
            var sGateIPaddr = HUtil32.GetValidStr3(sIp, ref sUserIPaddr, HUtil32.Backslash);
            SessionUserInfo userInfo;
            var success = false;
            for (var i = 0; i < gateInfo.UserList.Count; i++)
            {
                userInfo = gateInfo.UserList[i];
                if ((userInfo != null) && (userInfo.SessionId == sessionId))
                {
                    success = true;
                    break;
                }
            }
            if (!success)
            {
                userInfo = new SessionUserInfo();
                userInfo.sAccount = string.Empty;
                userInfo.sUserIPaddr = sUserIPaddr;
                userInfo.sGateIPaddr = sGateIPaddr;
                userInfo.SessionId = sessionId;
                userInfo.nSessionID = 0;
                userInfo.ConnectionId = gateInfo.ConnectionId;
                userInfo.sText = string.Empty;
                userInfo.dwTick34 = HUtil32.GetTickCount();
                userInfo.dwChrTick = HUtil32.GetTickCount();
                userInfo.boChrSelected = false;
                userInfo.boChrQueryed = false;
                userInfo.nSelGateID = gateInfo.nGateID;
                gateInfo.UserList.Add(userInfo);
            }
        }

        private void CloseUser(int connId, ref SelGateInfo gateInfo)
        {
            for (var i = 0; i < gateInfo.UserList.Count; i++)
            {
                var userInfo = gateInfo.UserList[i];
                if ((userInfo != null) && (userInfo.SessionId == connId))
                {
                    if (!_loginService.GetGlobaSessionStatus(userInfo.nSessionID))
                    {
                        _loginService.SendSocketMsg(Messages.SS_SOFTOUTSESSION, userInfo.sAccount + "/" + userInfo.nSessionID);
                        _loginService.CloseSession(userInfo.sAccount, userInfo.nSessionID);
                    }
                    userInfo = null;
                    gateInfo.UserList.RemoveAt(i);
                    break;
                }
            }
        }

        private void DeCodeUserMsg(string sData, SelGateInfo gateInfo, ref SessionUserInfo userInfo)
        {
            var sDefMsg = sData.Substring(0, Messages.DefBlockSize);
            var sText = sData.Substring(Messages.DefBlockSize, sData.Length - Messages.DefBlockSize);
            var clientPacket = EDCode.DecodePacket(sDefMsg);
            switch (clientPacket.Ident)
            {
                case Messages.CM_QUERYCHR:
                    if (!userInfo.boChrQueryed || ((HUtil32.GetTickCount() - userInfo.dwChrTick) > 200))
                    {
                        userInfo.dwChrTick = HUtil32.GetTickCount();
                        if (QueryChr(sText, ref userInfo, ref gateInfo))
                        {
                            userInfo.boChrQueryed = true;
                            _logger.Debug("[QueryChr]:" + sText);
                        }
                        else
                        {
                            _logger.Debug("[QueryChr]:" + sText);
                        }
                    }
                    else
                    {
                        _logger.Warn("[Hacker Attack] QueryChr:" + userInfo.sUserIPaddr);
                    }
                    break;
                case Messages.CM_NEWCHR:
                    if ((HUtil32.GetTickCount() - userInfo.dwChrTick) > 1000)
                    {
                        userInfo.dwChrTick = HUtil32.GetTickCount();
                        if ((!string.IsNullOrEmpty(userInfo.sAccount)) && _loginService.CheckSession(userInfo.sAccount, userInfo.sUserIPaddr, userInfo.nSessionID))
                        {
                            if (NewChr(sText, ref userInfo))
                            {
                                userInfo.boChrQueryed = false;
                            }
                        }
                        else
                        {
                            OutOfConnect(userInfo);
                        }
                    }
                    else
                    {
                        _logger.Warn("[Hacker Attack] NEWCHR " + userInfo.sAccount + "/" + userInfo.sUserIPaddr);
                    }
                    break;
                case Messages.CM_DELCHR:
                    if ((HUtil32.GetTickCount() - userInfo.dwChrTick) > 1000)
                    {
                        userInfo.dwChrTick = HUtil32.GetTickCount();
                        if ((!string.IsNullOrEmpty(userInfo.sAccount)) && _loginService.CheckSession(userInfo.sAccount, userInfo.sUserIPaddr, userInfo.nSessionID))
                        {
                            DeleteChr(sText, ref userInfo);
                            userInfo.boChrQueryed = false;
                        }
                        else
                        {
                            OutOfConnect(userInfo);
                        }
                    }
                    else
                    {
                        _logger.Warn("[Hacker Attack] DELCHR " + userInfo.sAccount + "/" + userInfo.sUserIPaddr);
                    }
                    break;
                case Messages.CM_SELCHR:
                    if (userInfo.boChrQueryed)
                    {
                        if ((!string.IsNullOrEmpty(userInfo.sAccount)) && _loginService.CheckSession(userInfo.sAccount, userInfo.sUserIPaddr, userInfo.nSessionID))
                        {
                            if (SelectChr(sText, gateInfo, ref userInfo))
                            {
                                userInfo.boChrSelected = true;
                            }
                        }
                        else
                        {
                            OutOfConnect(userInfo);
                        }
                    }
                    else
                    {
                        _logger.Warn("Double send SELCHR " + userInfo.sAccount + "/" + userInfo.sUserIPaddr);
                    }
                    break;
            }
        }

        /// <summary>
        /// 查询角色
        /// </summary>
        /// <returns></returns>
        private bool QueryChr(string sData, ref SessionUserInfo userInfo, ref SelGateInfo curGate)
        {
            var sAccount = string.Empty;
            var sSendMsg = string.Empty;
            var result = false;
            var sSessionId = HUtil32.GetValidStr3(EDCode.DeCodeString(sData), ref sAccount, HUtil32.Backslash);
            var nSessionId = HUtil32.StrToInt(sSessionId, -2);
            var nChrCount = 0;
            if (_loginService.CheckSession(sAccount, userInfo.sUserIPaddr, nSessionId))
            {
                _loginService.SetGlobaSessionNoPlay(nSessionId);
                userInfo.sAccount = sAccount;
                userInfo.nSessionID = nSessionId;
                IList<PlayQuick> chrList = new List<PlayQuick>();
                if ((_playRecordStorage.FindByAccount(sAccount, ref chrList) >= 0))
                {
                    for (var i = 0; i < chrList.Count; i++)
                    {
                        var quickId = chrList[i];
                        if (quickId.SelectID != userInfo.nSelGateID) // 如果选择ID不对,则跳过
                        {
                            continue;
                        }
                        var humRecord = _playRecordStorage.GetBy(quickId.Index, ref result);
                        if (result && !humRecord.Deleted)
                        {
                            var sChrName = quickId.ChrName;
                            var nIndex = _playDataStorage.Index(sChrName);
                            if ((nIndex < 0) || (nChrCount >= 2))
                            {
                                continue;
                            }
                            QueryChr chrRecord = null;
                            if (_playDataStorage.GetQryChar(nIndex, ref chrRecord))
                            {
                                if (humRecord.Selected == 1)
                                {
                                    sSendMsg = sSendMsg + "*";
                                }
                                sSendMsg = sSendMsg + sChrName + "/" + chrRecord.Job + "/" + chrRecord.Hair + "/" + chrRecord.Level + "/" + chrRecord.Sex + "/";
                                nChrCount++;
                            }
                        }
                    }
                }
                chrList = null;
                SendUserSocket(userInfo.ConnectionId, userInfo.SessionId, EDCode.EncodeMessage(Messages.MakeMessage(Messages.SM_QUERYCHR, nChrCount, 0, 1, 0)) + EDCode.EncodeString(sSendMsg));
                result = true;
            }
            else
            {
                SendUserSocket(userInfo.ConnectionId, userInfo.SessionId, EDCode.EncodeMessage(Messages.MakeMessage(Messages.SM_QUERYCHR_FAIL, nChrCount, 0, 1, 0)));
                CloseUser(userInfo.SessionId, ref curGate);
            }
            return result;
        }

        /// <summary>
        /// 会话错误
        /// </summary>
        private void OutOfConnect(SessionUserInfo userInfo)
        {
            var msg = Messages.MakeMessage(Messages.SM_OUTOFCONNECTION, 0, 0, 0, 0);
            var sMsg = EDCode.EncodeMessage(msg);
            SendUserSocket(userInfo.ConnectionId, userInfo.SessionId, sMsg);
        }

        private int DelChrSnameToLevel(string sName)
        {
            QueryChr chrRecord = null;
            var nIndex = _playDataStorage.Index(sName);
            if (nIndex < 0)
                return 0;
            if (_playDataStorage.GetQryChar(nIndex, ref chrRecord))
            {
                return chrRecord.Level;
            }
            return 0;
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        private void DeleteChr(string sData, ref SessionUserInfo userInfo)
        {
            CommandMessage msg;
            var sChrName = EDCode.DeCodeString(sData);
            var boCheck = false;
            var nIndex = _playRecordStorage.Index(sChrName);
            if (nIndex >= 0)
            {
                var humRecord = _playRecordStorage.Get(nIndex, ref boCheck);
                if (boCheck)
                {
                    if (humRecord.sAccount == userInfo.sAccount)
                    {
                        var nLevel = DelChrSnameToLevel(sChrName);
                        if (nLevel < _conf.DeleteMinLevel)
                        {
                            humRecord.Deleted = true;
                            boCheck = _playRecordStorage.Update(nIndex, ref humRecord);
                        }
                    }
                }
            }
            if (boCheck)
            {
                msg = Messages.MakeMessage(Messages.SM_DELCHR_SUCCESS, 0, 0, 0, 0);
            }
            else
            {
                msg = Messages.MakeMessage(Messages.SM_DELCHR_FAIL, 0, 0, 0, 0);
            }
            var sMsg = EDCode.EncodeMessage(msg);
            SendUserSocket(userInfo.ConnectionId, userInfo.SessionId, sMsg);
        }

        /// <summary>
        /// 新建角色
        /// </summary>
        private bool NewChr(string sData, ref SessionUserInfo userInfo)
        {
            var sAccount = string.Empty;
            var sChrName = string.Empty;
            var sHair = string.Empty;
            var sJob = string.Empty;
            var sSex = string.Empty;
            CommandMessage msg;
            var nCode = -1;
            var data = EDCode.DeCodeString(sData);
            data = HUtil32.GetValidStr3(data, ref sAccount, HUtil32.Backslash);
            data = HUtil32.GetValidStr3(data, ref sChrName, HUtil32.Backslash);
            data = HUtil32.GetValidStr3(data, ref sHair, HUtil32.Backslash);
            data = HUtil32.GetValidStr3(data, ref sJob, HUtil32.Backslash);
            data = HUtil32.GetValidStr3(data, ref sSex, HUtil32.Backslash);
            if (!string.IsNullOrEmpty(data.Trim()))
            {
                nCode = 0;
            }
            sChrName = sChrName.Trim();
            if (sChrName.Length < 3)
            {
                nCode = 0;
            }
            if (_conf.EnglishNames && !HUtil32.IsEnglishStr(sChrName))
            {
                nCode = 0;
            }
            if (!DBShare.CheckDenyChrName(sChrName))
            {
                nCode = 2;
            }
            /*if (!DBShare.boDenyChrName)
            {
                if (!DBShare.CheckChrName(sChrName))
                {
                    nCode = 0;
                }
                for (var i = 0; i < sChrName.Length; i++)
                {
                    if ((sChrName[i] == '?') || (sChrName[i] == ' ') || (sChrName[i] == '/') || (sChrName[i] == '@') || (sChrName[i] == '?') || (sChrName[i] == '\'') ||
                        (sChrName[i] == '\'') || (sChrName[i] == '\\') || (sChrName[i] == '.') || (sChrName[i] == ',') || (sChrName[i] == ':') || (sChrName[i] == ';') ||
                        (sChrName[i] == '`') || (sChrName[i] == '~') || (sChrName[i] == '!') || (sChrName[i] == '#') || (sChrName[i] == '$') || (sChrName[i] == '%') ||
                        (sChrName[i] == '^') || (sChrName[i] == '&') || (sChrName[i] == '*') || (sChrName[i] == '(') || (sChrName[i] == ')') || (sChrName[i] == '-') ||
                        (sChrName[i] == '_') || (sChrName[i] == '+') || (sChrName[i] == '=') || (sChrName[i] == '|') || (sChrName[i] == '[') || (sChrName[i] == '{') ||
                        (sChrName[i] == ']') || (sChrName[i] == '}'))
                    {
                        nCode = 0;
                    }
                }
            }*/
            if (nCode == -1)
            {
                if (_playDataStorage.Index(sChrName) >= 0)
                {
                    nCode = 2;
                }
                if (_playRecordStorage.ChrCountOfAccount(sAccount) < 2)
                {
                    var humRecord = new PlayerRecordData();
                    humRecord.sChrName = sChrName;
                    humRecord.sAccount = sAccount;
                    humRecord.Deleted = false;
                    humRecord.Header = new RecordHeader();
                    humRecord.Header.Name = sChrName;
                    humRecord.Header.SelectID = userInfo.nSelGateID;
                    if (!_playRecordStorage.Add(humRecord))
                    {
                        nCode = 2;
                    }
                }
                else
                {
                    nCode = 3;
                }
                if (nCode == -1)
                {
                    if (NewChrData(sAccount, sChrName, HUtil32.StrToInt(sSex, 0), HUtil32.StrToInt(sJob, 0), HUtil32.StrToInt(sHair, 0)))
                    {
                        nCode = 1;
                    }
                    else
                    {
                        _playRecordStorage.Delete(sChrName); //创建角色数据失败，删除索引值
                    }
                }
                else
                {
                    _playDataStorage.Delete(sChrName);//删除人物
                    nCode = 4;
                }
            }
            if (nCode == 1)
            {
                msg = Messages.MakeMessage(Messages.SM_NEWCHR_SUCCESS, 0, 0, 0, 0);
            }
            else
            {
                msg = Messages.MakeMessage(Messages.SM_NEWCHR_FAIL, nCode, 0, 0, 0);
            }
            var sMsg = EDCode.EncodeMessage(msg);
            SendUserSocket(userInfo.ConnectionId, userInfo.SessionId, sMsg);
            return nCode == 1;
        }

        /// <summary>
        /// 选择角色
        /// </summary>
        /// <returns></returns>
        private bool SelectChr(string sData, SelGateInfo curGate, ref SessionUserInfo userInfo)
        {
            var sAccount = string.Empty;
            var sCurMap = string.Empty;
            var nRoutePort = 0;
            var result = false;
            var sChrName = HUtil32.GetValidStr3(EDCode.DeCodeString(sData), ref sAccount, HUtil32.Backslash);
            var boDataOk = false;
            if (string.Compare(userInfo.sAccount, sAccount, StringComparison.OrdinalIgnoreCase) == 0)
            {
                int nIndex;
                IList<PlayQuick> chrList = new List<PlayQuick>();
                if (_playRecordStorage.FindByAccount(sAccount, ref chrList) >= 0)
                {
                    for (var i = 0; i < chrList.Count; i++)
                    {
                        nIndex = chrList[i].Index;
                        var humRecord = _playRecordStorage.GetBy(nIndex, ref result);
                        if (result)
                        {
                            if (humRecord.sChrName == sChrName)
                            {
                                humRecord.Selected = 1;
                                _playRecordStorage.UpdateBy(nIndex, ref humRecord);
                            }
                            else
                            {
                                if (humRecord.Selected == 1)
                                {
                                    humRecord.Selected = 0;
                                    _playRecordStorage.UpdateBy(nIndex, ref humRecord);
                                }
                            }
                        }
                    }
                }
                chrList = null;
                nIndex = _playDataStorage.Index(sChrName);
                if (nIndex >= 0)
                {
                    var chrRecord = _playDataStorage.Query(nIndex);
                    if (chrRecord != null)
                    {
                        sCurMap = chrRecord.CurMap;
                        boDataOk = true;
                    }
                }
            }
            if (boDataOk)
            {
                var nMapIndex = DBShare.GetMapIndex(sCurMap);
                var sDefMsg = EDCode.EncodeMessage(Messages.MakeMessage(Messages.SM_STARTPLAY, 0, 0, 0, 0));
                var sRouteIp = GateRouteIp(curGate.RemoteEndPoint.GetIPAddress(), ref nRoutePort);
                if (_conf.DynamicIpMode)// 使用动态IP
                {
                    sRouteIp = userInfo.sGateIPaddr;
                }
                var sRouteMsg = EDCode.EncodeString(sRouteIp + "/" + (nRoutePort + nMapIndex));
                SendUserSocket(curGate.ConnectionId, userInfo.SessionId, sDefMsg + sRouteMsg);
                _loginService.SetGlobaSessionPlay(userInfo.nSessionID);
                result = true;
                //_logger.DebugLog($"玩家使用游戏网关信息 GameRun:{sRouteIp} Port:{nRoutePort + nMapIndex}");
            }
            else
            {
                SendUserSocket(curGate.ConnectionId, userInfo.SessionId, EDCode.EncodeMessage(Messages.MakeMessage(Messages.SM_STARTFAIL, 0, 0, 0, 0)));
            }
            return result;
        }

        /// <summary>
        /// 获取游戏网关
        /// </summary>
        /// <returns></returns>
        private static string GetGameGateRoute(GateRouteInfo routeInfo, ref int nGatePort)
        {
            var nGateIndex = RandomNumber.GetInstance().Random(routeInfo.GateCount);
            var result = routeInfo.GameGateIP[nGateIndex];
            nGatePort = routeInfo.GameGatePort[nGateIndex];
            return result;
        }

        private string GateRouteIp(string sGateIp, ref int nPort)
        {
            var result = string.Empty;
            nPort = 0;
            for (var i = 0; i < DBShare.RouteInfo.Length; i++)
            {
                var routeInfo = DBShare.RouteInfo[i];
                if (routeInfo == null)
                {
                    continue;
                }
                if (routeInfo.SelGateIP == sGateIp)
                {
                    result = GetGameGateRoute(routeInfo, ref nPort);
                    break;
                }
            }
            return result;
        }

        private void SendUserSocket(string connectionId, int sessionId, string sSendMsg)
        {
            var message = new ServerDataMessage();
            message.SocketId = sessionId;
            message.Data = HUtil32.GetBytes("#" + sSendMsg + "!");
            message.DataLen = (short)message.Data.Length;
            message.Type = ServerDataType.Data;
            SendPacket(connectionId, message);
        }

        private void SendPacket(string connectionId, ServerDataMessage packet)
        {
            if (!_userSocket.IsOnline(connectionId))
                return;
            SendMessage(connectionId, SerializerUtil.Serialize(packet));
        }
        
        private void SendMessage(string connectionId, byte[] sendBuffer)
        {
            var serverMessage = new ServerDataPacket
            {
                PacketCode = Grobal2.RunGateCode,
                PacketLen = (ushort)sendBuffer.Length
            };
            var dataBuff = serverMessage.GetBuffer();
            var data = new byte[ServerDataPacket.FixedHeaderLen + sendBuffer.Length];
            MemoryCopy.BlockCopy(dataBuff, 0, data, 0, data.Length);
            MemoryCopy.BlockCopy(sendBuffer, 0, data, dataBuff.Length, sendBuffer.Length);
            _userSocket.Send(connectionId, data);
        }
    }

    public struct UserGateMessage
    {
        public ServerDataMessage Packet;
        public string ConnectionId;
    }
}