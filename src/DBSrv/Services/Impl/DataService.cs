﻿using DBSrv.Conf;
using DBSrv.Storage;
using OpenMir2.DataHandlingAdapters;

namespace DBSrv.Services.Impl
{
    /// <summary>
    /// 玩家数据服务
    /// DBSrv->GameSvr
    /// </summary>
    public class DataService : IService
    {
        private readonly IPlayDataStorage _playDataStorage;
        private readonly ICacheStorage _cacheStorage;
        private readonly TcpService _serverSocket;
        private readonly ClientSession _loginService;
        private readonly SettingsModel _setting;

        public DataService(SettingsModel conf, ClientSession loginService, IPlayDataStorage playDataStorage, ICacheStorage cacheStorage)
        {
            _setting = conf;
            _loginService = loginService;
            _playDataStorage = playDataStorage;
            _cacheStorage = cacheStorage;
            _serverSocket = new TcpService();
            _serverSocket.Connected += Connecting;
            _serverSocket.Disconnected += Disconnected;
            _serverSocket.Received += Received;
        }

        public void Initialize()
        {
            TouchSocketConfig touchSocketConfig = new TouchSocketConfig();
            touchSocketConfig.SetListenIPHosts(new IPHost[1]
            {
                new IPHost(IPAddress.Parse(_setting.ServerAddr), _setting.ServerPort)
            }).SetTcpDataHandlingAdapter(() => new PlayerDataFixedHeaderDataHandlingAdapter());
            _serverSocket.Setup(touchSocketConfig);
        }

        public void Start()
        {
            _serverSocket.Start();
            _playDataStorage.LoadQuickList();
            LogService.Info($"玩家数据存储服务[{_setting.ServerAddr}:{_setting.ServerPort}]已启动.等待链接...");
        }

        public void Stop()
        {
            _serverSocket.Stop();
        }

        private Task Received(IClient client, ReceivedDataEventArgs e)
        {
            if (e.RequestInfo is not PlayerDataMessageFixedHeaderRequestInfo fixedHeader)
            {
                return Task.CompletedTask;
            }

            if (fixedHeader.Header.PacketCode != Grobal2.PacketCode)
            {
                LogService.Error("验证玩家数据封包头出现异常...");
                return Task.CompletedTask;
            }
            SocketClient clientSoc = (SocketClient)client;
            ServerRequestData messageData = SerializerUtil.Deserialize<ServerRequestData>(fixedHeader.Message);
            ProcessMessagePacket(clientSoc.Id, messageData);
            return Task.CompletedTask;
        }

        private Task Connecting(IClient client, ConnectedEventArgs e)
        {
            SocketClient clientSoc = (SocketClient)client;
            if (!DBShare.CheckServerIP(clientSoc.IP))
            {
                LogService.Warn("非法服务器连接: " + clientSoc.IP);
                clientSoc.Close();
            }
            LogService.Info("服务器连接: " + clientSoc.IP);
            return Task.CompletedTask;
        }

        private Task Disconnected(IClient client, DisconnectEventArgs e)
        {
            return Task.CompletedTask;
        }

        private void ProcessMessagePacket(string connectionId, ServerRequestData requestData)
        {
            int queryId = requestData.QueryId;
            ServerRequestMessage requestMessage = SerializerUtil.Deserialize<ServerRequestMessage>(EDCode.DecodeBuff(requestData.Message));
            int packetLen = requestData.Message.Length + requestData.Packet.Length + ServerDataPacket.FixedHeaderLen;
            if (packetLen >= Messages.DefBlockSize && queryId > 0 && requestData.Packet != null && requestData.Sign != null)
            {
                byte[] sData = EDCode.DecodeBuff(requestData.Packet);
                int checkCode = HUtil32.MakeLong((ushort)(queryId ^ 170), (ushort)packetLen);
                if (checkCode <= 0)
                {
                    ProcessServerMsg(queryId, requestMessage, sData, connectionId);
                    return;
                }
                if (requestData.Sign.Length <= 0)
                {
                    ProcessServerMsg(queryId, requestMessage, sData, connectionId);
                    return;
                }
                byte[] signatureBuff = BitConverter.GetBytes(checkCode);
                short signatureId = BitConverter.ToInt16(signatureBuff);
                byte[] signBuff = EDCode.DecodeBuff(requestData.Sign);
                short signId = BitConverter.ToInt16(signBuff);
                if (signId == signatureId)
                {
                    ProcessServerMsg(queryId, requestMessage, sData, connectionId);
                    return;
                }
                if (_serverSocket.TryGetSocketClient(connectionId, out SocketClient client))
                {
                    client.Close();
                }
                LogService.Error($"关闭错误的任务{queryId}查询请求.");
                return;
            }
            ServerRequestData responsePack = new ServerRequestData();
            ServerRequestMessage messagePacket = new ServerRequestMessage(Messages.DBR_FAIL, 0, 0, 0, 0);
            responsePack.Message = EDCode.EncodeBuffer(SerializerUtil.Serialize(messagePacket));
            SendRequest(connectionId, queryId, responsePack);
        }

        private void ProcessServerMsg(int nQueryId, ServerRequestMessage packet, byte[] sData, string connectionId)
        {
            switch (packet.Ident)
            {
                case Messages.DB_LOADHUMANRCD:
                    LoadHumanRcd(nQueryId, sData, connectionId);
                    break;
                case Messages.DB_SAVEHUMANRCD:
                    SaveHumanRcd(nQueryId, packet.Recog, sData, connectionId);
                    break;
                case Messages.DB_SAVEHUMANRCDEX:
                    SaveHumanRcdEx(nQueryId, sData, packet.Recog, connectionId);
                    break;
                default:
                    ServerRequestData responsePack = new ServerRequestData();
                    ServerRequestMessage messagePacket = new ServerRequestMessage(Messages.DBR_FAIL, 0, 0, 0, 0);
                    responsePack.Message = EDCode.EncodeBuffer(SerializerUtil.Serialize(messagePacket));
                    SendRequest(connectionId, nQueryId, responsePack);
                    break;
            }
        }

        private void LoadHumanRcd(int queryId, byte[] data, string connectionId)
        {
            LoadCharacterData loadHumanPacket = SerializerUtil.Deserialize<LoadCharacterData>(data);
            if (loadHumanPacket.SessionId <= 0)
            {
                return;
            }
            CharacterDataInfo humanRcd = null;
            bool boFoundSession = false;
            int nCheckCode = -1;
            if ((!string.IsNullOrEmpty(loadHumanPacket.Account)) && (!string.IsNullOrEmpty(loadHumanPacket.ChrName)))
            {
                nCheckCode = _loginService.CheckSessionLoadRcd(loadHumanPacket.Account, loadHumanPacket.UserAddr, loadHumanPacket.SessionId, ref boFoundSession);
                if ((nCheckCode < 0) || !boFoundSession)
                {
                    LogService.Warn("[非法请求] " + "帐号: " + loadHumanPacket.Account + " IP: " + loadHumanPacket.UserAddr + " 标识: " + loadHumanPacket.SessionId);
                }
            }
            if ((nCheckCode == 1) || boFoundSession)
            {
                int nIndex = _playDataStorage.Index(loadHumanPacket.ChrName);
                if (nIndex >= 0)
                {
                    humanRcd = _cacheStorage.Get(loadHumanPacket.ChrName, out bool isExist);
                    if (!isExist)
                    {
                        if (!_playDataStorage.Get(loadHumanPacket.ChrName, ref humanRcd))
                        {
                            nCheckCode = -2;
                        }
                    }
                }
                else
                {
                    nCheckCode = -3;
                }
            }
            ServerRequestData responsePack = new ServerRequestData();
            if ((nCheckCode == 1) || boFoundSession)
            {
                LoadPlayerDataPacket loadHumData = new LoadPlayerDataPacket();
                loadHumData.ChrName = EDCode.EncodeString(loadHumanPacket.ChrName);
                loadHumData.HumDataInfo = humanRcd;
                ServerRequestMessage messagePacket = new ServerRequestMessage(Messages.DBR_LOADHUMANRCD, 1, 0, 0, 1);
                responsePack.Message = EDCode.EncodeBuffer(SerializerUtil.Serialize(messagePacket));
                SendRequest(connectionId, queryId, responsePack, loadHumData);
                LogService.Debug($"获取玩家[{loadHumanPacket.ChrName}]数据成功");
            }
            else
            {
                ServerRequestMessage messagePacket = new ServerRequestMessage(Messages.DBR_LOADHUMANRCD, nCheckCode, 0, 0, 0);
                responsePack.Message = EDCode.EncodeBuffer(SerializerUtil.Serialize(messagePacket));
                SendRequest(connectionId, queryId, responsePack);
            }
        }

        private void SaveHumanRcd(int queryId, int nRecog, byte[] sMsg, string connectionId)
        {
            try
            {
                SaveCharacterData saveHumDataPacket = SerializerUtil.Deserialize<SaveCharacterData>(sMsg);
                if (saveHumDataPacket == null)
                {
                    LogService.Error("保存玩家数据出错.");
                    return;
                }
                string sUserId = saveHumDataPacket.Account;
                string sChrName = saveHumDataPacket.ChrName;
                CharacterDataInfo humanRcd = saveHumDataPacket.CharacterData;
                bool bo21 = humanRcd == null;
                if (!bo21)
                {
                    bo21 = true;
                    humanRcd.Header.SetName(sChrName);
                    int nIndex = _playDataStorage.Index(sChrName);
                    if (nIndex < 0)
                    {
                        _playDataStorage.Add(humanRcd);
                        nIndex = _playDataStorage.Index(sChrName);
                    }
                    if (nIndex >= 0)
                    {
                        _cacheStorage.Add(sChrName, humanRcd);
                        _playDataStorage.Update(sChrName, humanRcd);
                        bo21 = false;
                    }
                    _loginService.SetSessionSaveRcd(sUserId);
                }
                ServerRequestData responsePack = new ServerRequestData();
                if (!bo21)
                {
                    ServerRequestMessage messagePacket = new ServerRequestMessage(Messages.DBR_SAVEHUMANRCD, 1, 0, 0, 0);
                    responsePack.Message = EDCode.EncodeBuffer(SerializerUtil.Serialize(messagePacket));
                    SendRequest(connectionId, queryId, responsePack);
                }
                else
                {
                    ServerRequestMessage messagePacket = new ServerRequestMessage(Messages.DBR_LOADHUMANRCD, 0, 0, 0, 0);
                    responsePack.Message = EDCode.EncodeBuffer(SerializerUtil.Serialize(messagePacket));
                    SendRequest(connectionId, queryId, responsePack);
                }
            }
            catch (Exception e)
            {
                LogService.Error(e);
            }
        }

        private void SaveHumanRcdEx(int nQueryId, byte[] sMsg, int nRecog, string connectionId)
        {
            SaveCharacterData saveHumDataPacket = SerializerUtil.Deserialize<SaveCharacterData>(sMsg);
            if (saveHumDataPacket == null)
            {
                LogService.Error("保存玩家数据出错.");
                return;
            }
            SaveHumanRcd(nQueryId, nRecog, sMsg, connectionId);
        }

        private void SendRequest(string connectionId, int queryId, ServerRequestData requestPacket)
        {
            requestPacket.QueryId = queryId;
            int checkCode;
            if (requestPacket.Packet != null)
            {
                checkCode = GetCheckCode(queryId, requestPacket);
            }
            else
            {
                requestPacket.Packet = Array.Empty<byte>();
                checkCode = HUtil32.MakeLong((ushort)(queryId ^ 170), (ushort)(requestPacket.Message.Length + ServerDataPacket.FixedHeaderLen));
            }
            byte[] nCheckCode = BitConverter.GetBytes(checkCode);
            requestPacket.Sign = EDCode.EncodeBuffer(nCheckCode);
            SendMessage(connectionId, SerializerUtil.Serialize(requestPacket));
        }

        private void SendRequest<T>(string connectionId, int queryId, ServerRequestData requestPacket, T packet) where T : new()
        {
            requestPacket.QueryId = queryId;
            if (packet != null)
            {
                requestPacket.Packet = EDCode.EncodeBuffer(SerializerUtil.Serialize(packet));
            }
            int signId = GetCheckCode(queryId, requestPacket);
            requestPacket.Sign = EDCode.EncodeBuffer(BitConverter.GetBytes(signId));
            SendMessage(connectionId, SerializerUtil.Serialize(requestPacket));
        }

        private int GetCheckCode(int queryId, ServerRequestData packet)
        {
            return HUtil32.MakeLong((ushort)(queryId ^ 170), (ushort)(packet.Message.Length + packet.Packet.Length + ServerDataPacket.FixedHeaderLen));
        }

        private void SendMessage(string connectionId, byte[] sendBuffer)
        {
            ServerDataPacket serverMessage = new ServerDataPacket
            {
                PacketCode = Grobal2.PacketCode,
                PacketLen = (ushort)sendBuffer.Length
            };
            byte[] dataBuff = SerializerUtil.Serialize(serverMessage);
            byte[] data = new byte[ServerDataPacket.FixedHeaderLen + sendBuffer.Length];
            MemoryCopy.BlockCopy(dataBuff, 0, data, 0, data.Length);
            MemoryCopy.BlockCopy(sendBuffer, 0, data, dataBuff.Length, sendBuffer.Length);
            _serverSocket.Send(connectionId, data);
        }
    }
}