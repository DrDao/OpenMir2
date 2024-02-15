using GameGate.Conf;
using GameGate.Packet;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using MD5 = OpenMir2.MD5;

namespace GameGate.Services
{
    /// <summary>
    /// 用户会话封包处理
    /// </summary>
    public class ClientSession : IDisposable
    {
        private readonly SessionSpeedRule gameSpeed;
        private readonly SessionInfo _session;
        private readonly object _syncObj;
        private ClientThread ClientThread { get; set; }
        private SendQueue SendQueue { get; set; }
        private List<DelayMessage> MsgList { get; set; }
        private int LastDirection { get; set; }
        private bool HandleLogin { get; set; }
        private bool KickFlag { get; set; }
        public int SvrListIdx { get; set; }
        private int SvrObjectId { get; set; }
        private int SendCheckTick { get; set; }
        private CheckStep Stat { get; set; }
        private byte ServiceId { get; set; }
        /// <summary>
        /// 会话密钥
        /// 用于OTP动态口令验证
        /// </summary>
        private string SessionKey { get; set; }
        private long FinishTick { get; set; }

        /// <summary>
        /// ServerMessage
        /// </summary>
        private ServerMessage SendMsg;

        public ClientSession(byte serviceId, SessionInfo session, ClientThread clientThread, SendQueue sendQueue)
        {
            ServiceId = serviceId;
            _session = session;
            ClientThread = clientThread;
            SendQueue = sendQueue;
            MsgList = new List<DelayMessage>();
            KickFlag = false;
            Stat = CheckStep.CheckLogin;
            LastDirection = -1;
            _syncObj = new object();
            gameSpeed = new SessionSpeedRule();
            SessionKey = Guid.NewGuid().ToString("N");
            SendMsg = new ServerMessage
            {
                PacketCode = Grobal2.PacketCode,
                Socket = _session.SckHandle,
                Ident = Grobal2.GM_DATA,
                SessionIndex = SvrListIdx
            };
        }

        public SessionSpeedRule GetGameSpeed()
        {
            return gameSpeed;
        }

        public ClientThread ServerThread => ClientThread;

        public SessionInfo Session => _session;

        private static GateConfig Config => ConfigManager.Instance.GateConfig;

        private void Kick(byte code)
        {
            Session.Socket.Close();
        }

        private readonly byte[] _httpSpan = HUtil32.GetBytes("http://");
        private readonly byte[] _httpsSpan = HUtil32.GetBytes("https://");
        private readonly byte[] _commandFlagSpan = HUtil32.GetBytes("@");
        private readonly byte[] _whisperFlagSpan = HUtil32.GetBytes("/");

        /// <summary>
        /// 处理客户端封包
        /// </summary>
        public void ProcessSessionPacket(ClientPacketMessage messagePacket)
        {
            _session.ReceiveTick = HUtil32.GetTickCount();
            int currentTick;
            if (KickFlag)
            {
                KickFlag = false;
                return;
            }
            Span<byte> destinationSpan = messagePacket.Data.AsSpan();
            ushort packetLen = messagePacket.BuffLen;
            if (Config.IsDefenceCCPacket && (packetLen >= 5))
            {
                if (destinationSpan[..7].SequenceEqual(_httpSpan))
                {
                    //HTTP封包，直接丢弃
                    //if (LogManager.g_pLogMgr.CheckLevel(6))
                    //{
                    //    Console.WriteLine("CC Attack, Kick: " + m_pUserOBJ.pszIPAddr);
                    //}
                    //Misc.KickUser(m_pUserOBJ.nIPAddr);
                    KickFlag = false;
                    return;
                }
                if (destinationSpan[..7].SequenceEqual(_httpsSpan))
                {
                    KickFlag = false;
                }
            }
            if ((Stat == CheckStep.CheckLogin) || (Stat == CheckStep.SendCheck))
            {
                currentTick = HUtil32.GetTickCount();
                if (0 == SendCheckTick)
                {
                    SendCheckTick = currentTick;
                }

                if ((currentTick - SendCheckTick) > 1000 * 5) // 如果5 秒 没有回数据 就下发数据
                {
                    Stat = CheckStep.SendSmu;
                }
            }

            // 如果下发成功  得多少秒有数据如果没有的话，那就是有问题
            if ((Stat == CheckStep.SendFinsh))
            {
                currentTick = HUtil32.GetTickCount();
                if ((currentTick - FinishTick) > 1000 * 10)
                {
                    SendKickMsg(12);
                    return;
                }
            }

            bool success = false;
            if (HandleLogin)
            {
                if (packetLen < GateShare.CommandFixedLength)
                {
                    if (packetLen == 1) //心跳包
                    {
                        return;
                    }
                    _session.Socket.Close(); //关闭异常会话
                    LogService.Info("异常消息封包，关闭会话...");
                    return;
                }

                Span<byte> tempBuff = destinationSpan[2..^1]; //跳过#1....! 只保留消息内容
                int deCodeLen = 0;
                Span<byte> decodeBuff = EncryptUtil.DecodeSpan(tempBuff, tempBuff.Length, ref deCodeLen);

                if (deCodeLen < 12)
                {
                    LogService.Debug("解析数据包小于12...解析后长度:" + deCodeLen);
                    _session.Socket.Close();//关闭异常会话
                    return;
                }

                CommandMessage clientMessage = MemoryMarshal.Read<CommandMessage>(decodeBuff);

                //if (Config.EnableOtp)
                //{
                //    if (CltCmd.OtpCode <= 0)
                //    {
                //        LogQueue.Enqueue("动态加密口令错误，剔除链接.", 1);
                //        Kick(100);
                //        return;
                //    }
                //    var authSuccess = _authenticator.ValidateTwoFactorPIN(SessionKey, CltCmd.OtpCode.ToString());
                //    if (!authSuccess)
                //    {
                //        LogQueue.Enqueue("动态加密口令验证失效,剔除链接.", 1);
                //        Kick(100);
                //        return;
                //    }
                //}

                if (packetLen > Config.NomClientPacketSize)
                {
                    LogService.Info("Kick off user,over nom client packet size: " + packetLen);
                    // Misc.KickUser(m_pUserOBJ.nIPAddr);
                    return;
                }
                int delayMsgCount;
                int dwDelay;
                int nInterval;
                switch (clientMessage.Ident)
                {
                    case Messages.CM_GUILDUPDATENOTICE:
                    case Messages.CM_GUILDUPDATERANKINFO:
                        if (packetLen > Config.MaxClientPacketSize)
                        {
                            LogService.Info("Kick off user,over max client packet size: " + packetLen);
                            // Misc.KickUser(m_pUserOBJ.nIPAddr);
                            return;
                        }
                        break;
                    case Messages.CM_WALK:
                    case Messages.CM_RUN:
                        if (Config.IsMoveInterval) // 700
                        {
                            currentTick = HUtil32.GetTickCount();
                            int nMoveInterval;
                            if (gameSpeed.SpeedLimit)
                            {
                                nMoveInterval = Config.MoveInterval + Config.PunishMoveInterval;
                            }
                            else
                            {
                                nMoveInterval = Config.MoveInterval;
                            }
                            nInterval = currentTick - gameSpeed.MoveTick;
                            if ((nInterval >= nMoveInterval))
                            {
                                gameSpeed.MoveTick = currentTick;
                                gameSpeed.SpellTick = currentTick - Config.MoveNextSpellCompensate;
                                if (gameSpeed.AttackTick < currentTick - Config.MoveNextAttackCompensate)
                                {
                                    gameSpeed.AttackTick = currentTick - Config.MoveNextAttackCompensate;
                                }
                                LastDirection = clientMessage.Tag;
                            }
                            else
                            {
                                if (Config.OverSpeedPunishMethod == PunishMethod.DelaySend)
                                {
                                    delayMsgCount = GetDelayMsgCount();
                                    if (delayMsgCount == 0)
                                    {
                                        dwDelay = Config.PunishBaseInterval + (int)Math.Round((nMoveInterval - nInterval) * Config.PunishIntervalRate);
                                        gameSpeed.MoveTick = currentTick + dwDelay;
                                    }
                                    else
                                    {
                                        gameSpeed.MoveTick = currentTick + (nMoveInterval - nInterval);
                                        if (delayMsgCount >= 2)
                                        {
                                            SendKickMsg(0);
                                        }
                                        return;
                                    }
                                }
                            }
                        }
                        break;
                    case Messages.CM_HIT:
                    case Messages.CM_HEAVYHIT:
                    case Messages.CM_BIGHIT:
                    case Messages.CM_POWERHIT:
                    case Messages.CM_LONGHIT:
                    case Messages.CM_WIDEHIT:
                    case Messages.CM_CRSHIT:
                    case Messages.CM_FIREHIT:
                        if (Config.IsAttackInterval)
                        {
                            currentTick = HUtil32.GetTickCount();
                            int nAttackInterval;
                            if (gameSpeed.SpeedLimit)
                            {
                                nAttackInterval = Config.AttackInterval + Config.PunishAttackInterval;
                            }
                            else
                            {
                                nAttackInterval = Config.AttackInterval;
                            }

                            int nAttackFixInterval = HUtil32._MAX(0, (nAttackInterval - Config.MaxItemSpeedRate * gameSpeed.ItemSpeed));
                            nInterval = currentTick - gameSpeed.AttackTick;
                            if ((nInterval >= nAttackFixInterval))
                            {
                                gameSpeed.AttackTick = currentTick;
                                if (Config.IsItemSpeedCompensate)
                                {
                                    gameSpeed.MoveTick = currentTick - (Config.AttackNextMoveCompensate + Config.MaxItemSpeedRate * gameSpeed.ItemSpeed); // 550
                                    gameSpeed.SpellTick = currentTick - (Config.AttackNextSpellCompensate + Config.MaxItemSpeedRate * gameSpeed.ItemSpeed); // 1150
                                }
                                else
                                {
                                    gameSpeed.MoveTick = currentTick - Config.AttackNextMoveCompensate; // 550
                                    gameSpeed.SpellTick = currentTick - Config.AttackNextSpellCompensate; // 1150
                                }

                                LastDirection = clientMessage.Tag;
                            }
                            else
                            {
                                if (Config.OverSpeedPunishMethod == PunishMethod.DelaySend)
                                {
                                    delayMsgCount = GetDelayMsgCount();
                                    if (delayMsgCount == 0)
                                    {
                                        dwDelay = Config.PunishBaseInterval + (int)Math.Round((nAttackFixInterval - nInterval) * Config.PunishIntervalRate);
                                        gameSpeed.AttackTick = currentTick + dwDelay;
                                    }
                                    else
                                    {
                                        gameSpeed.AttackTick = currentTick + (nAttackFixInterval - nInterval);
                                        if (delayMsgCount >= 2)
                                        {
                                            SendKickMsg(0);
                                        }

                                        return;
                                    }
                                }
                            }
                        }
                        break;
                    case Messages.CM_SPELL:
                        if (Config.IsSpellInterval) // 1380
                        {
                            currentTick = HUtil32.GetTickCount();
                            if (clientMessage.Tag >= 0128)
                            {
                                return;
                            }
                            if (TableDef.MaigicDelayArray[clientMessage.Tag]) // 过滤武士魔法
                            {
                                int nSpellInterval;
                                if (gameSpeed.SpeedLimit)
                                {
                                    nSpellInterval = TableDef.MaigicDelayTimeList[clientMessage.Tag] + Config.PunishSpellInterval;
                                }
                                else
                                {
                                    nSpellInterval = TableDef.MaigicDelayTimeList[clientMessage.Tag];
                                }
                                nInterval = (currentTick - gameSpeed.SpellTick);
                                if ((nInterval >= nSpellInterval))
                                {
                                    int dwNextMove;
                                    int dwNextAtt;
                                    if (TableDef.MaigicAttackArray[clientMessage.Tag])
                                    {
                                        dwNextMove = Config.SpellNextMoveCompensate;
                                        dwNextAtt = Config.SpellNextAttackCompensate;
                                    }
                                    else
                                    {
                                        dwNextMove = Config.SpellNextMoveCompensate + 80;
                                        dwNextAtt = Config.SpellNextAttackCompensate + 80;
                                    }

                                    gameSpeed.SpellTick = currentTick;
                                    gameSpeed.MoveTick = currentTick - dwNextMove;
                                    gameSpeed.AttackTick = currentTick - dwNextAtt;
                                    LastDirection = clientMessage.Tag;
                                }
                                else
                                {
                                    if (Config.OverSpeedPunishMethod == PunishMethod.DelaySend)
                                    {
                                        delayMsgCount = GetDelayMsgCount();
                                        if (delayMsgCount == 0)
                                        {
                                            dwDelay = Config.PunishBaseInterval + (int)Math.Round((nSpellInterval - nInterval) * Config.PunishIntervalRate);
                                        }
                                        else
                                        {
                                            if (delayMsgCount >= 2)
                                            {
                                                SendKickMsg(0);
                                            }
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case Messages.CM_SITDOWN:
                        if (Config.IsSitDownInterval)
                        {
                            currentTick = HUtil32.GetTickCount();
                            nInterval = (currentTick - gameSpeed.SitDownTick);
                            if (nInterval >= Config.SitDownInterval)
                            {
                                gameSpeed.SitDownTick = currentTick;
                            }
                            else
                            {
                                if (Config.OverSpeedPunishMethod == PunishMethod.DelaySend)
                                {
                                    delayMsgCount = GetDelayMsgCount();
                                    if (delayMsgCount == 0)
                                    {
                                        dwDelay = Config.PunishBaseInterval + (int)Math.Round((Config.SitDownInterval - nInterval) * Config.PunishIntervalRate);
                                        gameSpeed.SitDownTick = currentTick + dwDelay;
                                    }
                                    else
                                    {
                                        gameSpeed.SitDownTick = currentTick + (Config.SitDownInterval - nInterval);
                                        if (delayMsgCount >= 2)
                                        {
                                            SendKickMsg(0);
                                        }

                                        return;
                                    }
                                }
                            }
                        }
                        break;
                    case Messages.CM_BUTCH:
                        if (Config.IsButchInterval)
                        {
                            currentTick = HUtil32.GetTickCount();
                            nInterval = currentTick - gameSpeed.ButchTick;
                            if (nInterval >= Config.ButchInterval)
                            {
                                gameSpeed.ButchTick = currentTick;
                            }
                            else
                            {
                                if (Config.OverSpeedPunishMethod == PunishMethod.DelaySend)
                                {
                                    if (!PeekDelayMsg(clientMessage.Ident))
                                    {
                                        dwDelay = Config.PunishBaseInterval + (int)Math.Round((Config.ButchInterval - nInterval) * Config.PunishIntervalRate);
                                        gameSpeed.ButchTick = currentTick + dwDelay;
                                    }
                                    else
                                    {
                                        gameSpeed.SitDownTick = currentTick + (Config.ButchInterval - nInterval);
                                        return;
                                    }
                                }
                            }
                        }
                        break;
                    case Messages.CM_TURN:
                        if (Config.IsTurnInterval && (Config.OverSpeedPunishMethod != PunishMethod.TurnPack))
                        {
                            if (LastDirection != clientMessage.Tag)
                            {
                                currentTick = HUtil32.GetTickCount();
                                if (currentTick - gameSpeed.TurnTick >= Config.TurnInterval)
                                {
                                    LastDirection = clientMessage.Tag;
                                    gameSpeed.TurnTick = currentTick;
                                }
                                else
                                {
                                    if (Config.OverSpeedPunishMethod == PunishMethod.DelaySend)
                                    {
                                        if (!PeekDelayMsg(clientMessage.Ident))
                                        {
                                            dwDelay = Config.PunishBaseInterval + (int)Math.Round((Config.TurnInterval - (currentTick - gameSpeed.TurnTick)) * Config.PunishIntervalRate);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case Messages.CM_DEALTRY:
                        currentTick = HUtil32.GetTickCount();
                        if ((currentTick - gameSpeed.DealTick < 10000))
                        {
                            if ((currentTick - -gameSpeed.WaringTick > 2000))
                            {
                                gameSpeed.WaringTick = currentTick;
                                SendSysMsg($"攻击状态不能交易！请稍等{(10000 - currentTick + gameSpeed.DealTick) / 1000 + 1}秒……");
                            }
                            return;
                        }
                        break;
                    case Messages.CM_SAY:
                        Span<byte> msgContent = decodeBuff[12..];
                        if (Config.IsChatInterval)
                        {
                            if (!msgContent.StartsWith(_commandFlagSpan))
                            {
                                currentTick = HUtil32.GetTickCount();
                                if (currentTick - gameSpeed.SayMsgTick < Config.ChatInterval)
                                {
                                    return;
                                }
                                gameSpeed.SayMsgTick = currentTick;
                            }
                        }
                        if (deCodeLen > GateShare.CommandFixedLength)
                        {
                            if (msgContent.StartsWith(_commandFlagSpan))
                            {
                                byte[] pszChatBuffer = new byte[255];
                                string pszChatCmd = string.Empty;
                                MemoryCopy.BlockCopy(decodeBuff, GateShare.CommandFixedLength, pszChatBuffer, 0, deCodeLen - GateShare.CommandFixedLength);
                                pszChatBuffer[deCodeLen - GateShare.CommandFixedLength] = (byte)'\0';
                                //var tempStr = HUtil32.GetString(pszChatBuffer, 0, pszChatBuffer.Length);
                                //var nChatStrPos = tempStr.IndexOf(" ", StringComparison.OrdinalIgnoreCase);
                                //if (nChatStrPos > 0)
                                //{
                                //    Move(pszChatBuffer[0], pszChatCmd[0], nChatStrPos - 1);
                                //    pszChatCmd[nChatStrPos - 1] = '\0';
                                //}
                                //else
                                //{
                                //    Move(pszChatBuffer[0], pszChatCmd[0], pszChatBuffer.Length);
                                //}

                                if (GateShare.ChatCommandFilterMap.ContainsKey(pszChatCmd))
                                {
                                    CommandMessage cmd = new CommandMessage
                                    {
                                        Recog = SvrObjectId,
                                        Ident = Messages.SM_WHISPER,
                                        Param = HUtil32.MakeWord(0xFF, 56)
                                    };
                                    pszChatBuffer = HUtil32.GetBytes(string.Format(Protocol.CmdFilter, pszChatCmd));
                                    byte[] pszSendBuf = new byte[255];
                                    pszSendBuf[0] = (byte)'#';
                                    Buffer.BlockCopy(SerializerUtil.Serialize(cmd), 0, pszSendBuf, 1, pszSendBuf.Length);//Move(Cmd, m_pOverlapRecv.BBuffer[1], TCmdPack.PackSize);
                                    Buffer.BlockCopy(pszChatBuffer, 0, pszSendBuf, 13, pszChatBuffer.Length);//Move(pszChatBuffer[0], m_pOverlapRecv.BBuffer[13], pszChatBuffer.Length);
                                    //var nEnCodeLen = EncryptUtil.Encode(messagePacket.Buffer, 0, CommandPacket.Size + pszChatBuffer.Length, 1);
                                    int nEnCodeLen = EncryptUtil.Encode(decodeBuff, GateShare.CommandFixedLength + pszChatBuffer.Length, pszSendBuf);
                                    pszSendBuf[nEnCodeLen + 1] = (byte)'!';
                                    Session.Socket.Send(pszSendBuf, SocketFlags.None);
                                    //m_tIOCPSender.SendData(m_pOverlapSend, pszSendBuf[0], nEnCodeLen + 2);
                                    return;
                                }

                                if (Config.IsSpaceMoveNextPickupInterval)
                                {
                                    string buffString = HUtil32.GetString(pszChatBuffer, 0, pszChatBuffer.Length);
                                    if (string.Compare(buffString, Config.SpaceMoveCommand, StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        gameSpeed.PickupTick = HUtil32.GetTickCount() + Config.SpaceMoveNextPickupInterval;
                                    }
                                }

                            }
                            else if (Config.IsChatFilter)
                            {
                                if (msgContent.StartsWith(_whisperFlagSpan))
                                {
                                    byte[] pszChatBuffer = new byte[255];
                                    string pszChatCmd = string.Empty;
                                    //Move((nABuf + TCmdPack.PackSize as string), pszChatBuffer[0], nDeCodeLen - TCmdPack.PackSize);
                                    MemoryCopy.BlockCopy(destinationSpan, GateShare.CommandFixedLength, pszChatBuffer, 0, deCodeLen - GateShare.CommandFixedLength);
                                    pszChatBuffer[deCodeLen - GateShare.CommandFixedLength] = (byte)'\0';
                                    //var tempStr = HUtil32.GetString(pszChatBuffer, 0, pszChatBuffer.Length); //todo 需要优化此处,使用Span处理
                                    //var nChatStrPos = tempStr.IndexOf(" ", StringComparison.OrdinalIgnoreCase);
                                    //if (nChatStrPos > 0)
                                    //{
                                    //    //Move(pszChatBuffer[0], pszChatCmd[0], nChatStrPos - 1);
                                    //    //pszChatCmd[nChatStrPos - 1] = '\0';
                                    //    //szChatBuffer = pszChatBuffer[nChatStrPos];
                                    //    //var fChatFilter = AbusiveFilter.CheckChatFilter(ref szChatBuffer, ref Succeed);
                                    //    //if ((fChatFilter > 0) && !Succeed)
                                    //    //{
                                    //    //    LogService.Info("Kick off user,saying in filter");
                                    //    //    return;
                                    //    //}
                                    //    //if (fChatFilter == 2)
                                    //    //{
                                    //    //    var buffString = HUtil32.GetString(pszChatBuffer, 0, pszChatBuffer.Length);
                                    //    //    pszChatBuffer = $"{pszChatCmd} {szChatBuffer}";
                                    //    //    nDeCodeLen = pszChatBuffer.Length + TCmdPack.PackSize;
                                    //    //    Move(pszChatBuffer[0], (nABuf + TCmdPack.PackSize as string), pszChatBuffer.Length);
                                    //    //}
                                    //}
                                }
                                else if (!msgContent.StartsWith(_commandFlagSpan))
                                {
                                    byte[] pszChatBuffer = new byte[255];
                                    MemoryCopy.BlockCopy(destinationSpan, GateShare.CommandFixedLength, pszChatBuffer, 0, deCodeLen - GateShare.CommandFixedLength);
                                    pszChatBuffer[deCodeLen - GateShare.CommandFixedLength] = (byte)'\0';
                                    //var tempStr = HUtil32.GetString(pszChatBuffer, 0, pszChatBuffer.Length);//todo 需要优化此处,使用Span处理
                                    ////szChatBuffer = nABuf + CommandPacket.Size;
                                    //var isSucceed = false;
                                    //var fChatFilter = GateShare.AbusiveFilter.CheckChatFilter(ref tempStr, ref isSucceed);
                                    //if ((fChatFilter > 0) && !isSucceed)
                                    //{
                                    //    LogService.Info("Kick off user,saying in filter");
                                    //    return;
                                    //}
                                    //if (fChatFilter == 2)
                                    //{
                                    //    deCodeLen = pszChatBuffer.Length + CommandMessage.Size;
                                    //    //  Move(szChatBuffer[1], (nABuf + TCmdPack.PackSize as string), szChatBuffer.Length);
                                    //}
                                }
                            }
                        }
                        break;
                    case Messages.CM_PICKUP:
                        if (Config.IsPickupInterval)
                        {
                            currentTick = HUtil32.GetTickCount();
                            if (currentTick - gameSpeed.PickupTick > Config.PickupInterval)
                            {
                                gameSpeed.PickupTick = currentTick;
                            }
                            else
                            {
                                return;
                            }
                        }

                        break;
                    case Messages.CM_EAT:
                        if (Config.IsEatInterval)
                        {
                            if (clientMessage.Series == 0 || clientMessage.Series == 1 || clientMessage.Series == 3)
                            {
                                currentTick = HUtil32.GetTickCount();
                                if (currentTick - gameSpeed.EatTick > Config.EatInterval)
                                {
                                    gameSpeed.EatTick = currentTick;
                                }
                                else
                                {
                                    CommandMessage eatPacket = default;
                                    eatPacket.Recog = clientMessage.Recog;
                                    eatPacket.Ident = Messages.SM_EAT_FAIL;
                                    byte[] pszSendBuf = new byte[GateShare.CommandFixedLength];
                                    pszSendBuf[0] = (byte)'#';
                                    int nEnCodeLen = EncryptUtil.Encode(SerializerUtil.Serialize(eatPacket), GateShare.CommandFixedLength, pszSendBuf);
                                    pszSendBuf[nEnCodeLen + 1] = (byte)'!';
                                    ClientThread.SendSrvPacket(pszSendBuf);
                                    return;
                                }
                            }
                        }
                        break;
                }

                byte[] bodyBuffer;
                int sendLen;
                SendMsg.SessionIndex = SvrListIdx;
                if (deCodeLen > CommandMessage.Size)
                {
                    byte[] sendBuffer = new byte[messagePacket.BuffLen - CommandMessage.Size + 1];
                    int tLen = EncryptUtil.Encode(decodeBuff, deCodeLen - CommandMessage.Size, sendBuffer);
                    SendMsg.PackLength = CommandMessage.Size + tLen + 1;
                    sendLen = ServerMessage.PacketSize + SendMsg.PackLength;
                    bodyBuffer = GateShare.BytePool.Rent(sendLen);
                    MemoryCopy.BlockCopy(decodeBuff, 0, bodyBuffer, ServerMessage.PacketSize, CommandMessage.Size);
                    MemoryCopy.BlockCopy(tempBuff, GateShare.CommandFixedLength, bodyBuffer, ServerMessage.PacketSize + CommandMessage.Size, tLen);//消息体
                }
                else
                {
                    sendLen = ServerMessage.PacketSize + decodeBuff.Length;
                    bodyBuffer = GateShare.BytePool.Rent(sendLen);
                    SendMsg.PackLength = CommandMessage.Size;
                    MemoryCopy.BlockCopy(decodeBuff, 0, bodyBuffer, ServerMessage.PacketSize, decodeBuff.Length);
                }
                MemoryCopy.BlockCopy(SerializerUtil.Serialize(SendMsg), 0, bodyBuffer, 0, ServerMessage.PacketSize); //复制消息头
                ClientThread.SendSrvPacket(bodyBuffer[..sendLen]);
                GateShare.BytePool.Return(bodyBuffer);
            }
            else
            {
                string tempStr = EDCode.DeCodeString(destinationSpan[2..(packetLen - 1)]);
                ClientLogin(tempStr, packetLen, "", ref success);
                if (!success)
                {
                    LogService.Info("客户端登陆消息处理失败，剔除链接");
                    Kick(1);
                }
            }
        }

        /// <summary>
        /// 处理延时消息
        /// </summary>
        public void ProcessDelayMessage()
        {
            if (GetDelayMsgCount() <= 0)
            {
                return;
            }

            DelayMessage delayMsg = null;
            while (GetDelayMessage(ref delayMsg))
            {
                if (delayMsg.BufLen > 0)
                {
                    ClientThread.SendSrvPacket(delayMsg.Buffer); //发送消息到GameSvr
                    int dwCurrentTick = HUtil32.GetTickCount();
                    switch (delayMsg.Cmd)
                    {
                        case Messages.CM_BUTCH:
                            gameSpeed.ButchTick = dwCurrentTick;
                            break;
                        case Messages.CM_SITDOWN:
                            gameSpeed.SitDownTick = dwCurrentTick;
                            break;
                        case Messages.CM_TURN:
                            gameSpeed.TurnTick = dwCurrentTick;
                            break;
                        case Messages.CM_WALK:
                        case Messages.CM_RUN:
                            gameSpeed.MoveTick = dwCurrentTick;
                            gameSpeed.SpellTick = dwCurrentTick - Config.MoveNextSpellCompensate; //1200
                            if (gameSpeed.AttackTick < dwCurrentTick - Config.MoveNextAttackCompensate)
                            {
                                gameSpeed.AttackTick = dwCurrentTick - Config.MoveNextAttackCompensate; //900
                            }

                            LastDirection = delayMsg.Dir;
                            break;
                        case Messages.CM_HIT:
                        case Messages.CM_HEAVYHIT:
                        case Messages.CM_BIGHIT:
                        case Messages.CM_POWERHIT:
                        case Messages.CM_LONGHIT:
                        case Messages.CM_WIDEHIT:
                        case Messages.CM_CRSHIT:
                        case Messages.CM_FIREHIT:
                            if (gameSpeed.AttackTick < dwCurrentTick)
                            {
                                gameSpeed.AttackTick = dwCurrentTick;
                            }

                            if (Config.IsItemSpeedCompensate)
                            {
                                gameSpeed.MoveTick = dwCurrentTick - (Config.AttackNextMoveCompensate + Config.MaxItemSpeedRate * gameSpeed.ItemSpeed); // 550
                                gameSpeed.SpellTick = dwCurrentTick - (Config.AttackNextSpellCompensate + Config.MaxItemSpeedRate * gameSpeed.ItemSpeed); // 1150
                            }
                            else
                            {
                                gameSpeed.MoveTick = dwCurrentTick - Config.AttackNextMoveCompensate; // 550
                                gameSpeed.SpellTick = dwCurrentTick - Config.AttackNextSpellCompensate; // 1150
                            }
                            LastDirection = delayMsg.Dir;
                            break;
                        case Messages.CM_SPELL:
                            gameSpeed.SpellTick = dwCurrentTick;
                            int nNextMov;
                            int nNextAtt;
                            if (TableDef.MaigicAttackArray[delayMsg.Mag])
                            {
                                nNextMov = Config.SpellNextMoveCompensate;
                                nNextAtt = Config.SpellNextAttackCompensate;
                            }
                            else
                            {
                                nNextMov = Config.SpellNextMoveCompensate + 80;
                                nNextAtt = Config.SpellNextAttackCompensate + 80;
                            }

                            gameSpeed.MoveTick = dwCurrentTick - nNextMov; // 550
                            if (gameSpeed.AttackTick < dwCurrentTick - nNextAtt) // 900
                            {
                                gameSpeed.AttackTick = dwCurrentTick - nNextAtt;
                            }

                            LastDirection = delayMsg.Dir;
                            break;
                    }
                }
            }
        }

        private bool PeekDelayMsg(int nCmd)
        {
            bool result = false;
            int i = 0;
            while (MsgList.Count > i)
            {
                int iCmd = MsgList[i].Cmd;
                if (nCmd == Messages.CM_HIT)
                {
                    if ((iCmd == Messages.CM_HIT) || (iCmd == Messages.CM_HEAVYHIT) ||
                        (iCmd == Messages.CM_BIGHIT) || (iCmd == Messages.CM_POWERHIT) ||
                        (iCmd == Messages.CM_LONGHIT) || (iCmd == Messages.CM_WIDEHIT) ||
                        (iCmd == Messages.CM_CRSHIT) || (iCmd == Messages.CM_FIREHIT))
                    {
                        result = true;
                        break;
                    }
                    else
                    {
                        i++;
                    }
                }
                else if (nCmd == Messages.CM_RUN)
                {
                    if ((iCmd == Messages.CM_WALK) || (iCmd == Messages.CM_RUN))
                    {
                        result = true;
                        break;
                    }
                    else
                    {
                        i++;
                    }
                }
                else if (iCmd == nCmd)
                {
                    result = true;
                    break;
                }
                else
                {
                    i++;
                }
            }

            return result;
        }

        private int GetDelayMsgCount()
        {
            return MsgList.Count;
        }

        /// <summary>
        /// 获取延时消息
        /// </summary>
        private bool GetDelayMessage(ref DelayMessage delayMsg)
        {
            HUtil32.EnterCriticalSection(_syncObj);
            bool result = false;
            int count = 0;
            while (MsgList.Count > count)
            {
                DelayMessage msg = MsgList[count];
                if (msg.DelayTime != 0 && HUtil32.GetTickCount() < msg.DelayTime)
                {
                    count++;
                    continue;
                }
                MsgList.RemoveAt(count);
                delayMsg = new DelayMessage();
                delayMsg.Mag = msg.Mag;
                delayMsg.Dir = msg.Dir;
                delayMsg.Cmd = msg.Cmd;
                delayMsg.BufLen = msg.BufLen;
                delayMsg.Buffer = msg.Buffer;
                result = true;
            }
            HUtil32.LeaveCriticalSection(_syncObj);
            return result;
        }

        /// <summary>
        /// 发送延时处理消息
        /// </summary>
        private void SendDelayMsg(int magicId, byte nDir, int nIdx, int nLen, byte[] pMsg, int delayTime)
        {
            const ushort delayBufferLen = 1024;
            if (nLen > 0 && nLen <= delayBufferLen)
            {
                DelayMessage delayMsg = new DelayMessage
                {
                    Mag = magicId,
                    Dir = nDir,
                    Cmd = nIdx,
                    DelayTime = HUtil32.GetTickCount() + delayTime,
                    BufLen = nLen,
                    Buffer = pMsg
                };
                MsgList.Add(delayMsg);
            }
            if (magicId > 0)
            {
                LogService.Debug($"发送延时处理消息:User:[{_session.ChrName}] MagicID:[{magicId}] DelayTime:[{delayTime}]");
            }
        }

        private void SendPacketMessage(SessionMessage sessionPacket)
        {
            SendQueue.AddClientQueue(sessionPacket);
        }

        /// <summary>
        /// 处理GameSvr消息 
        /// 处理后发送到游戏客户端
        /// </summary>
        public async Task ProcessServerPacket(byte serviceId, ServerSessionMessage message)
        {
            if (Session.Socket == null)
            {
                return;
            }
            short bufferLen = message.BuffLen;
            var sendLen = 0;
            if (bufferLen < 0)//走路 攻击等消息
            {
                sendLen = (-bufferLen + 2);//bufferLen本身为负数，需要使用-来转为整数
                using var byteBlock = new ValueByteBlock(sendLen);
                byteBlock.Write((byte)'#');
                byteBlock.Write(message.Buffer);
                byteBlock.Write((byte)'!');
                byteBlock.SeekToStart();//将游标重置
                var buffer = new byte[byteBlock.Len];//定义一个数组容器
                byteBlock.Read(buffer);//读取数据到容器，并返回读取的长度r
                await ClientThread.SendGateQueue(Session.ConnectionId, buffer, buffer.Length);
            }
            else
            {
                sendLen = bufferLen + CommandMessage.Size;
                using var byteBlock = new ValueByteBlock(sendLen);
                byteBlock.Write((byte)'#');

                EncryptUtil.Encode(message.Buffer, CommandMessage.Size, byteBlock);//消息头
                if (bufferLen > CommandMessage.Size)
                {
                    byteBlock.Write(message.Buffer, CommandMessage.Size, bufferLen - CommandMessage.Size);
                }

                byteBlock.Write((byte)'!');

                if (bufferLen > 8)
                {
                    CommandMessage commandMessage = SerializerUtil.Deserialize<CommandMessage>(message.Buffer);
                    switch (commandMessage.Ident)
                    {
                        case Messages.SM_RUSH:
                            if (SvrObjectId == commandMessage.Recog)
                            {
                                int dwCurrentTick = HUtil32.GetTickCount();
                                gameSpeed.MoveTick = dwCurrentTick;
                                gameSpeed.AttackTick = dwCurrentTick;
                                gameSpeed.SpellTick = dwCurrentTick;
                                gameSpeed.SitDownTick = dwCurrentTick;
                                gameSpeed.ButchTick = dwCurrentTick;
                                gameSpeed.DealTick = dwCurrentTick;
                            }
                            break;
                        case Messages.SM_NEWMAP:
                        case Messages.SM_CHANGEMAP:
                        case Messages.SM_LOGON:
                            if (SvrObjectId == 0)
                            {
                                SvrObjectId = commandMessage.Recog;
                            }
                            break;
                        case Messages.SM_PLAYERCONFIG:

                            break;
                        case Messages.SM_CHARSTATUSCHANGED:
                            if (SvrObjectId == commandMessage.Recog)
                            {
                                gameSpeed.DefItemSpeed = commandMessage.Series;
                                gameSpeed.ItemSpeed = HUtil32._MIN(Config.MaxItemSpeed, commandMessage.Series);
                                //_mNChrStutas = HUtil32.MakeLong(param, tag);
                                //message.Buffer[10] = (byte)_gameSpeed.ItemSpeed; //同时限制客户端
                            }
                            break;
                        case Messages.SM_HWID:
                            if (Config.IsProcClientHardwareID)
                            {
                                switch (commandMessage.Series)
                                {
                                    case 1:
                                        LogService.Debug("封机器码");
                                        break;
                                    case 2:
                                        LogService.Debug("清理机器码");
                                        GateShare.HardwareFilter.ClearDeny();
                                        GateShare.HardwareFilter.SaveDenyList();
                                        break;
                                }
                            }
                            break;
                        case Messages.SM_RUNGATELOGOUT:
                            SendKickMsg(2);
                            break;
                    }
                }

                byteBlock.SeekToStart();//将游标重置
                var buffer = new byte[byteBlock.Len];//定义一个数组容器
                byteBlock.Read(buffer);//读取数据到容器，并返回读取的长度
                await ClientThread.SendGateQueue(Session.ConnectionId, buffer, buffer.Length);
            }
        }

        private void SendKickMsg(int killType)
        {
            string sendMsg = string.Empty;
            //var defMsg = new CommandMessage();
            switch (killType)
            {
                case 0:
                    if (Config.IsKickOverSpeed)
                    {
                    }
                    sendMsg = Config.OverSpeedSendBack;
                    break;
                case 1:
                    sendMsg = Config.PacketDecryptFailed;
                    break;
                case 2:
                    sendMsg = "当前登录帐号正在其它位置登录，本机已被强行离线";
                    break;
                case 4: //todo 版本号验证
                    //defMsg.Cmd = Messages.SM_VERSION_FAIL;
                    break;
                case 5:
                    sendMsg = Config.ClientOverCntMsg;
                    break;
                case 6:
                    sendMsg = Config.HWIDBlockedMsg;
                    break;
                case 12:
                    sendMsg = "反外挂模块更新失败,请重启客户端!!!!";
                    break;
            }

            LogService.Debug(sendMsg);

            //defMsg.UID = m_nSvrObject;
            //defMsg.Cmd = Messages.SM_SYSMESSAGE;
            //defMsg.X = HUtil32.MakeWord(0xFF, 0xF9);
            //defMsg.Y = 0;
            //defMsg.Direct = 0;

            //byte[] TempBuf = new byte[1024];
            //byte[] SendBuf = new byte[1024];
            //SendBuf[0] = (byte)'#';
            ////Move(Cmd, TempBuf[1], TCmdPack.PackSize);
            //var iLen = 0;
            //if (!string.IsNullOrEmpty(SendMsg))
            //{
            //    //Move(SendMsg[1], TempBuf[13], SendMsg.Length);
            //    TempBuf = HUtil32.GetBytes(SendMsg);
            //    iLen = TCmdPack.PackSize + SendMsg.Length;
            //}
            //else
            //{
            //    iLen = TCmdPack.PackSize;
            //}
            //iLen = Misc.EncodeBuf(TempBuf, iLen, SendBuf);
            //SendBuf[iLen + 1] = (byte)'!';
            ////m_tIOCPSender.SendData(m_pOverlapSend, SendBuf[0], iLen + 2);
            //_session.Socket.Send(SendBuf);
            //m_KickFlag = kick;
        }

        /// <summary>
        /// 处理登录数据
        /// </summary>
        private void ClientLogin(string loginData, int nLen, string addr, ref bool success)
        {
            if (nLen < GateShare.LoginPacketMaxLen && nLen > 15)
            {
                if (loginData[0] != '*' || loginData[1] != '*')
                {
                    LogService.Info($"[ClientLogin] Kicked 1: {loginData}");
                    success = false;
                    return;
                }
                string sDataText = loginData.AsSpan()[2..].ToString();
                string sHumName = string.Empty;//人物名称
                string sAccount = string.Empty;//账号
                string szCert = string.Empty;
                string szClientVerNo = string.Empty;//客户端版本号
                string szCode = string.Empty;
                string szHarewareId = string.Empty;//硬件ID

                sDataText = HUtil32.GetValidStr3(sDataText, ref sAccount, HUtil32.Backslash);
                sDataText = HUtil32.GetValidStr3(sDataText, ref sHumName, HUtil32.Backslash);

                if ((sAccount.Length >= 4) && (sAccount.Length <= 12) && (sHumName.Length > 2) && (sHumName.Length < 15))
                {
                    sDataText = HUtil32.GetValidStr3(sDataText, ref szCert, HUtil32.Backslash);
                    sDataText = HUtil32.GetValidStr3(sDataText, ref szClientVerNo, HUtil32.Backslash);
                    sDataText = HUtil32.GetValidStr3(sDataText, ref szCode, HUtil32.Backslash);
                    HUtil32.GetValidStr3(sDataText, ref szHarewareId, HUtil32.Backslash);
                    if (szCert.Length <= 0 || szCert.Length > 8)
                    {
                        success = false;
                        return;
                    }
                    if (szClientVerNo.Length < 8)
                    {
                        LogService.Info($"[ClientLogin] Kicked 2: {sHumName} clientVer validation failed.");
                        success = false;
                        return;
                    }
                    if (szCode.Length != 10)
                    {
                        success = false;
                        return;
                    }
                    if (GateShare.PunishList.TryGetValue(sHumName, out ClientSession userType))
                    {
                        gameSpeed.SpeedLimit = true;
                        GateShare.PunishList[sHumName] = this;
                    }
                    byte[] hardWareDigest = MD5.EmptyDigest;
                    if (Config.IsProcClientHardwareID)
                    {
                        if (string.IsNullOrEmpty(szHarewareId) || (szHarewareId.Length > 256) || ((szHarewareId.Length % 2) != 0))
                        {
                            LogService.Info($"[ClientLogin] Kicked 3: {sHumName}");
                            SendKickMsg(4);
                            return;
                        }
                        string src = szHarewareId;
                        string key = Config.ProClientHardwareKey;
                        bool fMatch = false;
                        int srcLen = src.Length / 2;
                        byte[] dest = new byte[srcLen];
                        try
                        {
                            int srcAsc, tmpSrcAsc, offSet = Convert.ToInt32("$" + src[..2]), keyPos = 0, i = 0;
                            for (int srcPos = 3; srcPos < src.Length; srcPos += 2)
                            {
                                srcAsc = Convert.ToInt32("$" + src.Substring(srcPos - 1, 2));
                                keyPos = keyPos < key.Length ? keyPos + 1 : 1;
                                tmpSrcAsc = srcAsc ^ key[keyPos];
                                tmpSrcAsc = tmpSrcAsc <= offSet ? 255 + tmpSrcAsc - offSet : tmpSrcAsc - offSet;
                                dest[i++] = (byte)(tmpSrcAsc);
                                offSet = srcAsc;
                            }
                        }
                        catch (Exception)
                        {
                            fMatch = true;
                        }
                        if (fMatch)
                        {
                            LogService.Info($"[ClientLogin] Kicked 5: {sHumName}");
                            SendKickMsg(4);
                            return;
                        }
                        HardwareHeader pHardwareHeader = ClientPacket.ToPacket<HardwareHeader>(dest);
                        //todo:session会话里面需要存用户ip
                        LogService.Info($"HWID: {MD5.MD5Print(pHardwareHeader.Md5Digest)}  {sHumName.Trim()}  {addr}");
                        if (pHardwareHeader.MagicCode == 0x13F13F13)
                        {
                            if (MD5.MD5Match(MD5.EmptyDigest, pHardwareHeader.Md5Digest))
                            {
                                LogService.Info($"[ClientLogin] Kicked 6: {sHumName}");
                                SendKickMsg(4);
                                return;
                            }
                            hardWareDigest = pHardwareHeader.Md5Digest;
                            bool overClientCount = false;
                            if (GateShare.HardwareFilter.IsFilter(hardWareDigest, ref overClientCount))
                            {
                                LogService.Info($"[ClientLogin] Kicked 7: {sHumName}");
                                if (overClientCount)
                                {
                                    SendKickMsg(5);
                                }
                                else
                                {
                                    SendKickMsg(6);
                                }
                                return;
                            }
                        }
                        else
                        {
                            LogService.Info($"[ClientLogin] Kicked 8: {sHumName}");
                            SendKickMsg(4);
                            return;
                        }
                    }
                    _session.Account = sAccount;
                    _session.ChrName = sHumName;

                    string hardwareStr = Config.IsProcClientHardwareID ? MD5.MD5Print(hardWareDigest) : "000000000000000000000000000000";
                    string loginPacket = $"**{sAccount}/{sHumName}/{szCert}/{szClientVerNo}/{szCode}/{hardwareStr}/{ServiceId}";
                    byte[] tempBuf = HUtil32.GetBytes(loginPacket);
                    byte[] loginDataPacket = new byte[tempBuf.Length + ServerMessage.PacketSize + 100];
                    int encodeLen = EncryptUtil.Encode(tempBuf, tempBuf.Length, loginDataPacket, ServerMessage.PacketSize + 2);
                    loginDataPacket[ServerMessage.PacketSize + 0] = (byte)'#';
                    loginDataPacket[ServerMessage.PacketSize + 1] = (byte)'0';
                    loginDataPacket[ServerMessage.PacketSize + encodeLen + 2] = (byte)'!';

                    ServerMessage packetHeader = new ServerMessage
                    {
                        PacketCode = Grobal2.PacketCode,
                        Ident = Grobal2.GM_DATA,
                        Socket = _session.SckHandle,
                        SessionId = _session.SessionId,
                        SessionIndex = _session.SessionIndex,
                        PackLength = encodeLen + 3
                    };

                    byte[] packetBuff = SerializerUtil.Serialize(packetHeader);
                    MemoryCopy.BlockCopy(packetBuff, 0, loginDataPacket, 0, packetBuff.Length);

                    SendLoginMessage(loginDataPacket[..(ServerMessage.PacketSize + packetHeader.PackLength)]);

                    success = true;
                    HandleLogin = true;
                    LogService.Debug($"[ClientLogin] {sAccount} {sHumName} {addr} {szCert} {szClientVerNo} {szCode} {MD5.MD5Print(hardWareDigest)} {ServiceId}");
                    /*var secretKey = _authenticator.GenerateSetupCode("openmir2", sAccount, SessionKey, 5);
                    LogService.Info($"动态密钥:{secretKey.AccountSecretKey}", 1);
                    LogService.Info($"动态验证码：{secretKey.ManualEntryKey}", 1);
                    LogService.Info($"{_authenticator.DefaultClockDriftTolerance.TotalMilliseconds}秒后验证新的密钥,容错5秒.", 1);*/
                }
                else
                {
                    LogService.Info($"[ClientLogin] Kicked 2: {loginData}");
                    success = false;
                }
            }
            else
            {
                LogService.Info($"[ClientLogin] Kicked 0: {loginData}");
                success = false;
            }
        }

        /// <summary>
        /// 发送登录验证封包
        /// </summary>
        private void SendLoginMessage(byte[] packet)
        {
            SendDelayMsg(0, 0, 0, packet.Length, packet, 1);
        }

        private void SendSysMsg(string szMsg)
        {
            if ((ClientThread == null) || !ClientThread.IsConnected)
            {
                return;
            }
            byte[] tempBuf = new byte[1024];
            CommandMessage clientPacket = new CommandMessage();
            //clientPacket.UID = SvrObjectId;
            //clientPacket.Cmd = Messages.SM_SYSMESSAGE;
            //clientPacket.X = HUtil32.MakeWord(0xFF, 0xF9);
            //clientPacket.Y = 0;
            //clientPacket.Direct = 0;
            Buffer.BlockCopy(SerializerUtil.Serialize(clientPacket), 0, tempBuf, 0, GateShare.CommandFixedLength);
            byte[] sBuff = HUtil32.GetBytes(szMsg);
            Buffer.BlockCopy(sBuff, 0, tempBuf, 13, sBuff.Length);
            int iLen = GateShare.CommandFixedLength + szMsg.Length;
            byte[] sendBuf = GateShare.BytePool.Rent(iLen + 1);
            sendBuf[0] = (byte)'#';
            iLen = EncryptUtil.Encode(tempBuf, iLen, sendBuf);
            sendBuf[iLen + 1] = (byte)'!';
            //SendQueue.AddClientQueue(_session.ConnectionId, _session.ThreadId, sendBuf);
        }

        public void Dispose()
        {
            //todo 
        }
    }
}