﻿using M2Server.Actor;
using M2Server.Monster.Monsters;
using OpenMir2;
using OpenMir2.Consts;
using OpenMir2.Data;
using OpenMir2.Enums;
using OpenMir2.Packets.ClientPackets;
using OpenMir2.Packets.ServerPackets;
using SystemModule;
using SystemModule.Actors;
using SystemModule.Castles;
using SystemModule.Data;
using SystemModule.Enums;

namespace M2Server.Player
{
    public partial class PlayObject
    {
        public override void Run()
        {
            int tObjCount;
            int nInteger;
            const string sPayMentExpire = "您的帐户充值时间已到期!!!";
            const string sDisConnectMsg = "游戏被强行中断!!!";
            const string sExceptionMsg1 = "[Exception] PlayObject::Run -> Operate 1";
            const string sExceptionMsg2 = "[Exception] PlayObject::Run -> Operate 2 # {0} Ident:{1} Sender:{2} wP:{3} nP1:{4} nP2:{5} np3:{6} Msg:{7}";
            const string sExceptionMsg4 = "[Exception] PlayObject::Run -> ClearObj";
            try
            {
                if (Dealing)
                {
                    if (GetPoseCreate() != DealCreat || DealCreat == this || DealCreat == null)
                    {
                        DealCancel();
                    }
                }
                if (SystemShare.Config.PayMentMode == 3)
                {
                    if (HUtil32.GetTickCount() - AccountExpiredTick > QueryExpireTick)//一分钟查询一次账号游戏到期时间
                    {
                        ExpireTime = ExpireTime - 60;//游戏时间减去一分钟
                        //IdSrvClient.Instance.SendUserPlayTime(UserAccount, ExpireTime);
                        AccountExpiredTick = HUtil32.GetTickCount();
                        CheckExpiredTime();
                    }
                    if (AccountExpired)
                    {
                        SysMsg(sPayMentExpire, MsgColor.Red, MsgType.Hint);
                        SysMsg(sDisConnectMsg, MsgColor.Red, MsgType.Hint);
                        BoEmergencyClose = true;
                        AccountExpired = false;
                    }
                }
                if (FireHitSkill && (HUtil32.GetTickCount() - LatestFireHitTick) > 20 * 1000)
                {
                    FireHitSkill = false;
                    SysMsg(MessageSettings.SpiritsGone, MsgColor.Red, MsgType.Hint);
                    SendSocket("+UFIR");
                }
                if (TwinHitSkill && (HUtil32.GetTickCount() - LatestTwinHitTick) > 60 * 1000)
                {
                    TwinHitSkill = false;
                    SendSocket("+UTWN");
                }
                if (IsTimeRecall && HUtil32.GetTickCount() > TimeRecallTick) //执行 TimeRecall回到原地
                {
                    IsTimeRecall = false;
                    SpaceMove(TimeRecallMoveMap, TimeRecallMoveX, TimeRecallMoveY, 0);
                }
                if (!Death && ((IncSpell > 0) || (IncHealth > 0) || (IncHealing > 0)))
                {
                    int dwInChsTime = 600 - HUtil32._MIN(400, WAbil.Level * 10);
                    if (((HUtil32.GetTickCount() - IncHealthSpellTick) >= dwInChsTime) && !Death)
                    {
                        int incHealthTick = HUtil32._MIN(200, HUtil32.GetTickCount() - IncHealthSpellTick - dwInChsTime);
                        IncHealthSpellTick = HUtil32.GetTickCount() + incHealthTick;
                        if ((IncSpell > 0) || (IncHealth > 0) || (PerHealing > 0))
                        {
                            if (PerHealth <= 0)
                            {
                                PerHealth = 1;
                            }
                            if (PerSpell <= 0)
                            {
                                PerSpell = 1;
                            }
                            if (PerHealing <= 0)
                            {
                                PerHealing = 1;
                            }
                            int nHP;
                            if (IncHealth < PerHealth)
                            {
                                nHP = IncHealth;
                                IncHealth = 0;
                            }
                            else
                            {
                                nHP = PerHealth;
                                IncHealth -= PerHealth;
                            }
                            int nMP;
                            if (IncSpell < PerSpell)
                            {
                                nMP = IncSpell;
                                IncSpell = 0;
                            }
                            else
                            {
                                nMP = PerSpell;
                                IncSpell -= PerSpell;
                            }
                            if (IncHealing < PerHealing)
                            {
                                nHP += IncHealing;
                                IncHealing = 0;
                            }
                            else
                            {
                                nHP += PerHealing;
                                IncHealing -= PerHealing;
                            }
                            PerHealth = (byte)(WAbil.Level / 10 + 5);
                            PerSpell = (byte)(WAbil.Level / 10 + 5);
                            PerHealing = 5;
                            IncHealthSpell(nHP, nMP);
                            if (WAbil.HP == WAbil.MaxHP)
                            {
                                IncHealth = 0;
                                IncHealing = 0;
                            }
                            if (WAbil.MP == WAbil.MaxMP)
                            {
                                IncSpell = 0;
                            }
                        }
                    }
                }
                else
                {
                    IncHealthSpellTick = HUtil32.GetTickCount();
                }
                for (int i = 0; i < 20; i++) //个人定时器
                {
                    if (AutoTimerStatus[i] > 500)
                    {
                        if ((HUtil32.GetTickCount() - AutoTimerTick[i]) > AutoTimerStatus[i])
                        {
                            if (SystemShare.ManageNPC != null)
                            {
                                AutoTimerTick[i] = HUtil32.GetTickCount();
                                ScriptGotoCount = 0;
                                SystemShare.ManageNPC.GotoLable(this, "@OnTimer" + i, false);
                            }
                        }
                    }
                }
                bool boNeedRecalc = false;
                if (StatusTimeArr[PoisonState.STATETRANSPARENT] == 0)
                {
                    AbilMagBubbleDefence = false;
                }
                for (int i = 0; i < ExtraAbil.Length; i++)
                {
                    if (ExtraAbil[i] > 0)
                    {
                        if (HUtil32.GetTickCount() > ExtraAbilTimes[i])
                        {
                            ExtraAbil[i] = 0;
                            ExtraAbilFlag[i] = 0;
                            boNeedRecalc = true;
                            switch (i)
                            {
                                case 0:
                                    SysMsg("攻击力恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                                case 1:
                                    SysMsg("魔法力恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                                case 2:
                                    SysMsg("精神力恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                                case 3:
                                    SysMsg("攻击速度恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                                case 4:
                                    SysMsg("体力值恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                                case 5:
                                    SysMsg("魔法值恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                                case 6:
                                    SysMsg("攻击能力恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                            }
                        }
                        else if (ExtraAbilFlag[i] == 0 && HUtil32.GetTickCount() > ExtraAbilTimes[i] - 10000)
                        {
                            ExtraAbilFlag[i] = 1;
                            switch (i)
                            {
                                case AbilConst.EABIL_DCUP:
                                    SysMsg("攻击力10秒后恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                                case AbilConst.EABIL_MCUP:
                                    SysMsg("魔法力10秒后恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                                case AbilConst.EABIL_SCUP:
                                    SysMsg("精神力10秒后恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                                case AbilConst.EABIL_HITSPEEDUP:
                                    SysMsg("攻击速度10秒后恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                                case AbilConst.EABIL_HPUP:
                                    SysMsg("体力值10秒后恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                                case AbilConst.EABIL_MPUP:
                                    SysMsg("魔法值10秒后恢复正常。", MsgColor.Green, MsgType.Hint);
                                    break;
                            }
                        }
                    }
                }
                if (boNeedRecalc)
                {
                    HealthSpellChanged();
                }
                PlaySuperRock();
                if (IsTimeGoto && (HUtil32.GetTickCount() > TimeGotoTick)) //Delaygoto延时跳转
                {
                    IsTimeGoto = false;
                    ((IMerchant)TimeGotoNpc)?.GotoLable(this, TimeGotoLable, false);
                }
                // 增加挂机
                if (OffLineFlag && HUtil32.GetTickCount() > KickOffLineTick)
                {
                    OffLineFlag = false;
                    BoSoftClose = true;
                }
                if (IsDelayCall && (HUtil32.GetTickCount() - DelayCallTick) > DelayCall)
                {
                    IsDelayCall = false;
                    INormNpc normNpc = SystemShare.WorldEngine.FindMerchant(DelayCallNpc) ?? SystemShare.WorldEngine.FindNpc(DelayCallNpc);
                    if (normNpc != null)
                    {
                        normNpc.GotoLable(this, DelayCallLabel, false);
                    }
                }
                if ((HUtil32.GetTickCount() - DecPkPointTick) > SystemShare.Config.DecPkPointTime)// 减少PK值
                {
                    DecPkPointTick = HUtil32.GetTickCount();
                    if (PkPoint > 0)
                    {
                        DecPkPoint(SystemShare.Config.DecPkPointCount);
                    }
                }
                if ((HUtil32.GetTickCount() - DecLightItemDrugTick) > SystemShare.Config.DecLightItemDrugTime)
                {
                    DecLightItemDrugTick += SystemShare.Config.DecLightItemDrugTime;
                    UseLamp();
                    CheckPkStatus();
                }
                if ((HUtil32.GetTickCount() - CheckDupObjTick) > 3000)
                {
                    CheckDupObjTick = HUtil32.GetTickCount();
                    GetStartPoint();
                    tObjCount = Envir.GetXYObjCount(CurrX, CurrY);
                    if (tObjCount >= 2)
                    {
                        if (!BoDuplication)
                        {
                            BoDuplication = true;
                            DupStartTick = HUtil32.GetTickCount();
                        }
                    }
                    else
                    {
                        BoDuplication = false;
                    }
                    if ((tObjCount >= 3 && ((HUtil32.GetTickCount() - DupStartTick) > 3000) || tObjCount == 2
                        && ((HUtil32.GetTickCount() - DupStartTick) > 10000)) && ((HUtil32.GetTickCount() - DupStartTick) < 20000))
                    {
                        CharPushed(M2Share.RandomNumber.RandomByte(8), 1);
                    }
                }
                IUserCastle castle = SystemShare.CastleMgr.InCastleWarArea(this);
                if (castle != null && castle.UnderWar)
                {
                    ChangePkStatus(true);
                }
                if ((HUtil32.GetTickCount() - DiscountForNightTick) > 1000)
                {
                    DiscountForNightTick = HUtil32.GetTickCount();
                    int wHour = DateTime.Now.Hour;
                    int wMin = DateTime.Now.Minute;
                    int wSec = DateTime.Now.Second;
                    int wMSec = DateTime.Now.Millisecond;
                    if (SystemShare.Config.DiscountForNightTime && (wHour == SystemShare.Config.HalfFeeStart || wHour == SystemShare.Config.HalfFeeEnd))
                    {
                        if (wMin == 0 && wSec <= 30 && (HUtil32.GetTickCount() - LogonTick) > 60000)
                        {
                            LogonTimcCost();
                            LogonTick = HUtil32.GetTickCount();
                            LogonTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        }
                    }
                    if (MyGuild != null)
                    {
                        if (MyGuild.GuildWarList.Count > 0)
                        {
                            bool boInSafeArea = InSafeArea();
                            if (boInSafeArea != IsSafeArea)
                            {
                                IsSafeArea = boInSafeArea;
                                RefNameColor();
                            }
                        }
                    }
                    if (castle != null && castle.UnderWar)
                    {
                        if (Envir == castle.PalaceEnvir && MyGuild != null)
                        {
                            if (!castle.IsMember(this))
                            {
                                if (castle.IsAttackGuild(MyGuild))
                                {
                                    if (castle.CanGetCastle(MyGuild))
                                    {
                                        castle.GetCastle(MyGuild);
                                        SystemShare.WorldEngine.SendServerGroupMsg(Messages.SS_211, M2Share.ServerIndex, MyGuild.GuildName);
                                        if (castle.InPalaceGuildCount() <= 1)
                                        {
                                            castle.StopWallconquestWar();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        ChangePkStatus(false);
                    }
                    if (NameColorChanged)
                    {
                        NameColorChanged = false;
                        RefUserState();
                        RefShowName();
                    }
                }
            }
            catch
            {
                LogService.Error(sExceptionMsg1);
            }
            ProcessMessage processMsg = default;
            try
            {
                GetMessageTick = HUtil32.GetTickCount();
                while (((HUtil32.GetTickCount() - GetMessageTick) < SystemShare.Config.HumanGetMsgTime) && GetMessage(GetMessageTick, ref processMsg))
                {
                    if (!Operate(processMsg))
                    {
                        break;
                    }
                }
                if (BoEmergencyClose || BoKickFlag || BoSoftClose)
                {
                    if (SwitchData)
                    {
                        MapName = SwitchMapName;
                        CurrX = SwitchMapX;
                        CurrY = SwitchMapY;
                    }
                    MakeGhost();
                    if (BoKickFlag)
                    {
                        SendDefMessage(Messages.SM_OUTOFCONNECTION, 0, 0, 0, 0);
                    }
                    if (!BoReconnection && BoSoftClose)
                    {
                        MyGuild = SystemShare.GuildMgr.MemberOfGuild(ChrName);
                        if (MyGuild != null)
                        {
                            MyGuild.SendGuildMsg(ChrName + " 已经退出游戏.");
                            SystemShare.WorldEngine.SendServerGroupMsg(Messages.SS_208, M2Share.ServerIndex, MyGuild.GuildName + '/' + "" + '/' + ChrName + " has exited the game.");
                        }
                        //IdSrvClient.Instance.SendHumanLogOutMsg(UserAccount, SessionId);
                    }
                }
            }
            catch (Exception e)
            {
                if (processMsg.wIdent == 0)
                {
                    MakeGhost();//用于处理 人物异常退出，但人物还在游戏中问题
                }
                LogService.Error(Format(sExceptionMsg2, ChrName, processMsg.wIdent, processMsg.ActorId, processMsg.wParam, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, processMsg.Msg));
                LogService.Error(e.Message);
            }
            bool boTakeItem = false;
            // 检查身上的装备有没不符合
            for (int i = 0; i < UseItems.Length; i++)
            {
                if (UseItems[i] != null && UseItems[i].Index > 0)
                {
                    StdItem stdItem = SystemShare.EquipmentSystem.GetStdItem(UseItems[i].Index);
                    if (stdItem != null)
                    {
                        if (!CheckItemsNeed(stdItem))
                        {
                            // m_ItemList.Add((UserItem));
                            UserItem userItem = UseItems[i];
                            if (AddItemToBag(userItem))
                            {
                                SendAddItem(userItem);
                                WeightChanged();
                                boTakeItem = true;
                            }
                            else
                            {
                                int dropWide = HUtil32._MIN(SystemShare.Config.DropItemRage, 3);
                                if (DropItemDown(UseItems[i], dropWide, false, 0, ActorId))
                                {
                                    boTakeItem = true;
                                }
                            }
                            if (boTakeItem)
                            {
                                SendDelItems(UseItems[i]);
                                UseItems[i].Index = 0;
                                RecalcAbilitys();
                            }
                        }
                    }
                    else
                    {
                        UseItems[i].Index = 0;
                    }
                }
            }
            tObjCount = GameGold;
            if (BoDecGameGold && (HUtil32.GetTickCount() - DecGameGoldTick) > DecGameGoldTime)
            {
                DecGameGoldTick = HUtil32.GetTickCount();
                if (GameGold >= DecGameGold)
                {
                    GameGold -= DecGameGold;
                    nInteger = DecGameGold;
                }
                else
                {
                    nInteger = GameGold;
                    GameGold = 0;
                    BoDecGameGold = false;
                    MoveToHome();
                }
                if (M2Share.GameLogGameGold)
                {
                    // M2Share.EventSource.AddEventLog(Grobal2.LogGameGold, Format(CommandHelp.GameLogMsg1, MapName, CurrX, CurrY, ChrName, SystemShare.Config.GameGoldName, nInteger, '-', "Auto"));
                }
            }
            if (BoIncGameGold && (HUtil32.GetTickCount() - IncGameGoldTick) > IncGameGoldTime)
            {
                IncGameGoldTick = HUtil32.GetTickCount();
                if (GameGold + IncGameGold < 2000000)
                {
                    GameGold += IncGameGold;
                    nInteger = IncGameGold;
                }
                else
                {
                    GameGold = 2000000;
                    nInteger = 2000000 - GameGold;
                    BoIncGameGold = false;
                }
                if (M2Share.GameLogGameGold)
                {
                    //M2Share.EventSource.AddEventLog(Grobal2.LogGameGold, Format(CommandHelp.GameLogMsg1, MapName, CurrX, CurrY, ChrName, SystemShare.Config.GameGoldName, nInteger, '-', "Auto"));
                }
            }
            if (!BoDecGameGold && Envir.Flag.boDECGAMEGOLD)
            {
                if ((HUtil32.GetTickCount() - DecGameGoldTick) > Envir.Flag.nDECGAMEGOLDTIME * 1000)
                {
                    DecGameGoldTick = HUtil32.GetTickCount();
                    if (GameGold >= Envir.Flag.nDECGAMEGOLD)
                    {
                        GameGold -= Envir.Flag.nDECGAMEGOLD;
                        nInteger = Envir.Flag.nDECGAMEGOLD;
                    }
                    else
                    {
                        nInteger = GameGold;
                        GameGold = 0;
                        BoDecGameGold = false;
                        MoveToHome();
                    }
                    if (M2Share.GameLogGameGold)
                    {
                        //M2Share.EventSource.AddEventLog(Grobal2.LogGameGold, Format(CommandHelp.GameLogMsg1, MapName, CurrX, CurrY, ChrName, SystemShare.Config.GameGoldName, nInteger, '-', "Map"));
                    }
                }
            }
            if (!BoIncGameGold && Envir.Flag.boINCGAMEGOLD)
            {
                if ((HUtil32.GetTickCount() - IncGameGoldTick) > (Envir.Flag.nINCGAMEGOLDTIME * 1000))
                {
                    IncGameGoldTick = HUtil32.GetTickCount();
                    if (GameGold + Envir.Flag.nINCGAMEGOLD <= 2000000)
                    {
                        GameGold += Envir.Flag.nINCGAMEGOLD;
                        nInteger = Envir.Flag.nINCGAMEGOLD;
                    }
                    else
                    {
                        nInteger = 2000000 - GameGold;
                        GameGold = 2000000;
                    }
                    if (M2Share.GameLogGameGold)
                    {
                        // M2Share.EventSource.AddEventLog(Grobal2.LogGameGold, Format(CommandHelp.GameLogMsg1, MapName, CurrX, CurrY, ChrName, SystemShare.Config.GameGoldName, nInteger, '+', "Map"));
                    }
                }
            }
            if (tObjCount != GameGold)
            {
                SendUpdateMsg(Messages.RM_GOLDCHANGED, 0, 0, 0, 0, "");
            }
            if (Envir.Flag.Fight3Zone)
            {
                FightZoneDieCount++;
                if (MyGuild != null)
                {
                    MyGuild.TeamFightWhoDead(ChrName);
                }
                if (LastHiter != null && LastHiter.Race == ActorRace.Play)
                {
                    PlayObject lastHiterPlay = LastHiter as PlayObject;
                    if (lastHiterPlay.MyGuild != null && MyGuild != null)
                    {
                        lastHiterPlay.MyGuild.TeamFightWhoWinPoint(LastHiter.ChrName, 100);
                        string tStr = lastHiterPlay.MyGuild.GuildName + ':' + lastHiterPlay.MyGuild.ContestPoint + "  " + MyGuild.GuildName + ':' + MyGuild.ContestPoint;
                        SystemShare.WorldEngine.CryCry(Messages.RM_CRY, Envir, CurrX, CurrY, 1000, SystemShare.Config.CryMsgFColor, SystemShare.Config.CryMsgBColor, "- " + tStr);
                    }
                }
            }
            if (Envir.Flag.boINCGAMEPOINT)
            {
                if ((HUtil32.GetTickCount() - IncGamePointTick) > (Envir.Flag.nINCGAMEPOINTTIME * 1000))
                {
                    IncGamePointTick = HUtil32.GetTickCount();
                    if (GamePoint + Envir.Flag.nINCGAMEPOINT <= 2000000)
                    {
                        GamePoint += Envir.Flag.nINCGAMEPOINT;
                        nInteger = Envir.Flag.nINCGAMEPOINT;
                    }
                    else
                    {
                        GamePoint = 2000000;
                        nInteger = 2000000 - GamePoint;
                    }
                    if (M2Share.GameLogGamePoint)
                    {
                        //M2Share.EventSource.AddEventLog(Grobal2.LogGamePoint, Format(CommandHelp.GameLogMsg1, MapName, CurrX, CurrY, ChrName, SystemShare.Config.GamePointName, nInteger, '+', "Map"));
                    }
                }
            }
            if (Envir.Flag.boDECHP && (HUtil32.GetTickCount() - DecHpTick) > (Envir.Flag.nDECHPTIME * 1000))
            {
                DecHpTick = HUtil32.GetTickCount();
                if (WAbil.HP > Envir.Flag.nDECHPPOINT)
                {
                    WAbil.HP -= (ushort)Envir.Flag.nDECHPPOINT;
                }
                else
                {
                    WAbil.HP = 0;
                }
                HealthSpellChanged();
            }
            if (Envir.Flag.boINCHP && (HUtil32.GetTickCount() - IncHpTick) > (Envir.Flag.nINCHPTIME * 1000))
            {
                IncHpTick = HUtil32.GetTickCount();
                if (WAbil.HP + Envir.Flag.nDECHPPOINT < WAbil.MaxHP)
                {
                    WAbil.HP += (ushort)Envir.Flag.nDECHPPOINT;
                }
                else
                {
                    WAbil.HP = WAbil.MaxHP;
                }
                HealthSpellChanged();
            }
            // 降饥饿点
            if (SystemShare.Config.HungerSystem)
            {
                if ((HUtil32.GetTickCount() - DecHungerPointTick) > 1000)
                {
                    DecHungerPointTick = HUtil32.GetTickCount();
                    if (HungerStatus > 0)
                    {
                        tObjCount = GetMyStatus();
                        HungerStatus -= 1;
                        if (tObjCount != GetMyStatus())
                        {
                            RefMyStatus();
                        }
                    }
                    else
                    {
                        if (SystemShare.Config.HungerDecHP)
                        {
                            // 减少涨HP，MP
                            HealthTick -= 60;
                            SpellTick -= 10;
                            SpellTick = HUtil32._MAX(0, SpellTick);
                            PerHealth -= 1;
                            PerSpell -= 1;
                            if (WAbil.HP > WAbil.HP / 100)
                            {
                                WAbil.HP -= (ushort)HUtil32._MAX(1, WAbil.HP / 100);
                            }
                            else
                            {
                                if (WAbil.HP <= 2)
                                {
                                    WAbil.HP = 0;
                                }
                            }
                            HealthSpellChanged();
                        }
                    }
                }
            }
            if ((HUtil32.GetTickCount() - ExpRateTick) > 1000)
            {
                ExpRateTick = HUtil32.GetTickCount();
                if (KillMonExpRateTime > 0)
                {
                    KillMonExpRateTime -= 1;
                    if (KillMonExpRateTime == 0)
                    {
                        KillMonExpRate = 100;
                        SysMsg("经验倍数恢复正常...", MsgColor.Red, MsgType.Hint);
                    }
                }
                if (PowerRateTime > 0)
                {
                    PowerRateTime -= 1;
                    if (PowerRateTime == 0)
                    {
                        PowerRate = 100;
                        SysMsg("攻击力倍数恢复正常...", MsgColor.Red, MsgType.Hint);
                    }
                }
            }
            if (SystemShare.Config.ReNewChangeColor && ReLevel > 0 && (HUtil32.GetTickCount() - ReColorTick) > SystemShare.Config.ReNewNameColorTime)
            {
                ReColorTick = HUtil32.GetTickCount();
                ReColorIdx++;
                if (ReColorIdx >= SystemShare.Config.ReNewNameColor.Length)
                {
                    ReColorIdx = 0;
                }
                NameColor = SystemShare.Config.ReNewNameColor[ReColorIdx];
                RefNameColor();
            }
            // 检测侦听私聊对像
            if (WhisperHuman != null)
            {
                if (WhisperHuman.Death || WhisperHuman.Ghost)
                {
                    WhisperHuman = null;
                }
            }
            try
            {
                if ((HUtil32.GetTickCount() - ClearInvalidObjTick) > 30 * 1000)
                {
                    ClearInvalidObjTick = HUtil32.GetTickCount();
                    if (DearHuman != 0)
                    {
                        var playerDear = SystemShare.ActorMgr.Get<IPlayerActor>(DearHuman);
                        if (playerDear.Death || playerDear.Ghost)
                        {
                            DearHuman = 0;
                        }
                    }
                    if (IsMaster)
                    {
                        for (int i = MasterList.Count - 1; i >= 0; i--)
                        {
                            if (MasterList[i].Death || MasterList[i].Ghost)
                            {
                                MasterList.RemoveAt(i);
                            }
                        }
                    }
                    else
                    {
                        if (MasterHuman != 0)
                        {
                            var playerMaster = SystemShare.ActorMgr.Get<IPlayerActor>(MasterHuman);
                            if((playerMaster.Death || playerMaster.Ghost))
                            {
                                MasterHuman = 0;
                            }
                        }
                    }

                    // 清组队已死亡成员
                    if (GroupOwner != 0)
                    {
                        IPlayerActor groupOwnerPlay = (IPlayerActor)SystemShare.ActorMgr.Get(GroupOwner);
                        if (groupOwnerPlay.Death || groupOwnerPlay.Ghost)
                        {
                            GroupOwner = 0;
                        }
                    }

                    if (GroupOwner == ActorId)
                    {
                        for (int i = GroupMembers.Count - 1; i >= 0; i--)
                        {
                            IActor baseObject = GroupMembers[i];
                            if (baseObject.Death || baseObject.Ghost)
                            {
                                GroupMembers.RemoveAt(i);
                            }
                        }
                    }

                    // 检查交易双方 状态
                    if ((DealCreat != null) && DealCreat.Ghost)
                    {
                        DealCreat = null;
                    }
                }
            }
            catch (Exception e)
            {
                LogService.Error(sExceptionMsg4);
                LogService.Error(e.Message);
            }
            if (AutoGetExpPoint > 0 && (AutoGetExpEnvir == null || AutoGetExpEnvir == Envir) && (HUtil32.GetTickCount() - AutoGetExpTick) > AutoGetExpTime)
            {
                AutoGetExpTick = HUtil32.GetTickCount();
                if (!AutoGetExpInSafeZone || AutoGetExpInSafeZone && InSafeZone())
                {
                    GetExp(AutoGetExpPoint);
                }
            }

            if (!Death)
            {
                if (WAbil.HP == 0)
                {
                    if (((LastHiter == null) || LastHiter.Race == ActorRace.Play && !((IPlayerActor)LastHiter).UnRevival))
                    {
                        if (Race == ActorRace.Play && Revival && ((HUtil32.GetTickCount() - RevivalTick) > SystemShare.Config.RevivalTime))
                        {
                            RevivalTick = HUtil32.GetTickCount();
                            ItemDamageRevivalRing();
                            WAbil.HP = WAbil.MaxHP;
                            HealthSpellChanged();
                            SysMsg(MessageSettings.RevivalRecoverMsg, MsgColor.Green, MsgType.Hint);
                        }
                    }
                }
            }

            base.Run();
        }

        protected override bool Operate(ProcessMessage processMsg)
        {
            int nObjCount;
            int delayTime = 0;
            int nMsgCount;
            bool result = true;
            IActor baseObject = null;
            if (processMsg.ActorId > 0)
            {
                baseObject = SystemShare.ActorMgr.Get(processMsg.ActorId);
            }
            switch (processMsg.wIdent)
            {
                case Messages.CM_QUERYUSERNAME:
                    ClientQueryUserName(processMsg.nParam1, processMsg.nParam2, processMsg.nParam3);
                    break;
                case Messages.CM_QUERYBAGITEMS: //僵尸攻击：不断刷新包裹发送大量数据，导致网络阻塞
                    if ((HUtil32.GetTickCount() - QueryBagItemsTick) > 30 * 1000)
                    {
                        QueryBagItemsTick = HUtil32.GetTickCount();
                        ClientQueryBagItems();
                    }
                    else
                    {
                        SysMsg(MessageSettings.QUERYBAGITEMS, MsgColor.Red, MsgType.Hint);
                    }
                    break;
                case Messages.CM_QUERYUSERSTATE:
                    ClientQueryUserInformation(processMsg.nParam1, processMsg.nParam2, processMsg.nParam3);
                    break;
                case Messages.CM_QUERYUSERSET:
                    ClientQueryUserSet(processMsg);
                    break;
                case Messages.CM_DROPITEM:
                    if (ClientDropItem(processMsg.Msg, processMsg.nParam1))
                    {
                        SendDefMessage(Messages.SM_DROPITEM_SUCCESS, processMsg.nParam1, 0, 0, 0, processMsg.Msg);
                    }
                    else
                    {
                        SendDefMessage(Messages.SM_DROPITEM_FAIL, processMsg.nParam1, 0, 0, 0, processMsg.Msg);
                    }
                    break;
                case Messages.CM_PICKUP:
                    if (CurrX == processMsg.nParam2 && CurrY == processMsg.nParam3)
                    {
                        ClientPickUpItem();
                    }
                    break;
                case Messages.CM_OPENDOOR:
                    ClientOpenDoor(processMsg.nParam2, processMsg.nParam3);
                    break;
                case Messages.CM_TAKEONITEM:
                    ClientTakeOnItems((byte)processMsg.nParam2, processMsg.nParam1, processMsg.Msg);
                    break;
                case Messages.CM_TAKEOFFITEM:
                    ClientTakeOffItems((byte)processMsg.nParam2, processMsg.nParam1, processMsg.Msg);
                    break;
                case Messages.CM_EAT:
                    ClientUseItems(processMsg.nParam1, processMsg.Msg);
                    break;
                case Messages.CM_BUTCH:
                    if (!ClientGetButchItem(processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, (byte)processMsg.wParam, ref delayTime))
                    {
                        if (delayTime != 0)
                        {
                            nMsgCount = GetDigUpMsgCount();
                            if (nMsgCount >= SystemShare.Config.MaxDigUpMsgCount)
                            {
                                OverSpeedCount++;
                                if (OverSpeedCount > SystemShare.Config.OverSpeedKickCount)
                                {
                                    if (SystemShare.Config.KickOverSpeed)
                                    {
                                        SysMsg(MessageSettings.KickClientUserMsg, MsgColor.Red, MsgType.Hint);
                                        BoEmergencyClose = true;
                                    }
                                    if (SystemShare.Config.ViewHackMessage)
                                    {
                                        //LogService.Warn(Format(CommandHelp.BunOverSpeed, ChrName, delayTime, nMsgCount));
                                    }
                                }
                                SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");// 如果超速则发送攻击失败信息
                            }
                            else
                            {
                                if (delayTime < SystemShare.Config.DropOverSpeed)
                                {
                                    if (TestSpeedMode)
                                    {
                                        SysMsg(Format("速度异常 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                    }
                                    SendSocket(M2Share.GetGoodTick);
                                }
                                else
                                {
                                    SendSelfDelayMsg(processMsg.wIdent, processMsg.wParam, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, "", delayTime);
                                    result = false;
                                }
                            }
                        }
                    }
                    break;
                case Messages.CM_MAGICKEYCHANGE:
                    ClientChangeMagicKey((ushort)processMsg.nParam1, (char)processMsg.nParam2);
                    break;
                case Messages.CM_SOFTCLOSE:
                    if (!OffLineFlag)
                    {
                        BoReconnection = true;
                        BoSoftClose = true;
                        if (processMsg.wParam == 1)
                        {
                            BoEmergencyClose = true;
                        }
                    }
                    break;
                case Messages.CM_CLICKNPC:
                    ClientClickNpc(processMsg.nParam1);
                    break;
                case Messages.CM_MERCHANTDLGSELECT:
                    ClientMerchantDlgSelect(processMsg.nParam1, processMsg.Msg);
                    break;
                case Messages.CM_MERCHANTQUERYSELLPRICE:
                    ClientMerchantQuerySellPrice(processMsg.nParam1, HUtil32.MakeLong((short)processMsg.nParam2, (short)processMsg.nParam3), processMsg.Msg);
                    break;
                case Messages.CM_USERSELLITEM:
                    ClientUserSellItem(processMsg.nParam1, HUtil32.MakeLong((ushort)processMsg.nParam2, (ushort)processMsg.nParam3), processMsg.Msg);
                    break;
                case Messages.CM_USERBUYITEM:
                    ClientUserBuyItem(processMsg.wIdent, processMsg.nParam1, HUtil32.MakeLong((ushort)processMsg.nParam2, (ushort)processMsg.nParam3), 0, processMsg.Msg);
                    break;
                case Messages.CM_USERGETDETAILITEM:
                    ClientUserBuyItem(processMsg.wIdent, processMsg.nParam1, 0, processMsg.nParam2, processMsg.Msg);
                    break;
                case Messages.CM_DROPGOLD:
                    if (processMsg.nParam1 > 0)
                    {
                        ClientDropGold(processMsg.nParam1);
                    }
                    break;
                case Messages.CM_TEST:
                    SendDefMessage(1, 0, 0, 0, 0);
                    break;
                case Messages.CM_GROUPMODE:
                    if (processMsg.nParam2 == 0)
                    {
                        ClientGroupClose();
                    }
                    else
                    {
                        AllowGroup = true;
                    }
                    if (AllowGroup)
                    {
                        SendDefMessage(Messages.SM_GROUPMODECHANGED, 0, 1, 0, 0);
                    }
                    else
                    {
                        SendDefMessage(Messages.SM_GROUPMODECHANGED, 0, 0, 0, 0);
                    }
                    break;
                case Messages.CM_CREATEGROUP:
                    ClientCreateGroup(processMsg.Msg.Trim());
                    break;
                case Messages.CM_ADDGROUPMEMBER:
                    ClientAddGroupMember(processMsg.Msg.Trim());
                    break;
                case Messages.CM_DELGROUPMEMBER:
                    ClientDelGroupMember(processMsg.Msg.Trim());
                    break;
                case Messages.CM_USERREPAIRITEM:
                    ClientRepairItem(processMsg.nParam1, HUtil32.MakeLong((ushort)processMsg.nParam2, (ushort)processMsg.nParam3), processMsg.Msg);
                    break;
                case Messages.CM_MERCHANTQUERYREPAIRCOST:
                    ClientQueryRepairCost(processMsg.nParam1, HUtil32.MakeLong((ushort)processMsg.nParam2, (ushort)processMsg.nParam3), processMsg.Msg);
                    break;
                case Messages.CM_DEALTRY:
                    ClientDealTry(processMsg.Msg.Trim());
                    break;
                case Messages.CM_DEALADDITEM:
                    ClientAddDealItem(processMsg.nParam1, processMsg.Msg);
                    break;
                case Messages.CM_DEALDELITEM:
                    ClientDelDealItem(processMsg.nParam1, processMsg.Msg);
                    break;
                case Messages.CM_DEALCANCEL:
                    ClientCancelDeal();
                    break;
                case Messages.CM_DEALCHGGOLD:
                    ClientChangeDealGold(processMsg.nParam1);
                    break;
                case Messages.CM_DEALEND:
                    ClientDealEnd();
                    break;
                case Messages.CM_USERSTORAGEITEM:
                    ClientStorageItem(processMsg.nParam1, HUtil32.MakeLong((ushort)processMsg.nParam2, (ushort)processMsg.nParam3), processMsg.Msg);
                    break;
                case Messages.CM_USERTAKEBACKSTORAGEITEM:
                    ClientTakeBackStorageItem(processMsg.nParam1, HUtil32.MakeLong((ushort)processMsg.nParam2, (ushort)processMsg.nParam3), processMsg.Msg);
                    break;
                case Messages.CM_WANTMINIMAP:
                    ClientGetMinMap();
                    break;
                case Messages.CM_USERMAKEDRUGITEM:
                    ClientMakeDrugItem(processMsg.nParam1, processMsg.Msg);
                    break;
                case Messages.CM_OPENGUILDDLG:
                    ClientOpenGuildDlg();
                    break;
                case Messages.CM_GUILDHOME:
                    ClientGuildHome();
                    break;
                case Messages.CM_GUILDMEMBERLIST:
                    ClientGuildMemberList();
                    break;
                case Messages.CM_GUILDADDMEMBER:
                    ClientGuildAddMember(processMsg.Msg);
                    break;
                case Messages.CM_GUILDDELMEMBER:
                    ClientGuildDelMember(processMsg.Msg);
                    break;
                case Messages.CM_GUILDUPDATENOTICE:
                    ClientGuildUpdateNotice(processMsg.Msg);
                    break;
                case Messages.CM_GUILDUPDATERANKINFO:
                    ClientGuildUpdateRankInfo(processMsg.Msg);
                    break;
                case Messages.CM_1042:
                    LogService.Warn("[非法数据] " + ChrName);
                    break;
                case Messages.CM_ADJUST_BONUS:
                    ClientAdjustBonus(processMsg.nParam1, processMsg.Msg);
                    break;
                case Messages.CM_GUILDALLY:
                    ClientGuildAlly();
                    break;
                case Messages.CM_GUILDBREAKALLY:
                    ClientGuildBreakAlly(processMsg.Msg);
                    break;
                case Messages.CM_TURN:
                    if (ClientChangeDir(processMsg.wIdent, processMsg.nParam1, processMsg.nParam2, (byte)processMsg.wParam, ref delayTime))
                    {
                        ActionTick = HUtil32.GetTickCount();
                        SendSocket(M2Share.GetGoodTick);
                    }
                    else
                    {
                        if (delayTime == 0)
                        {
                            SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");
                        }
                        else
                        {
                            nMsgCount = GetTurnMsgCount();
                            if (nMsgCount >= SystemShare.Config.MaxTurnMsgCount)
                            {
                                OverSpeedCount++;
                                if (OverSpeedCount > SystemShare.Config.OverSpeedKickCount)
                                {
                                    if (SystemShare.Config.KickOverSpeed)
                                    {
                                        SysMsg(MessageSettings.KickClientUserMsg, MsgColor.Red, MsgType.Hint);
                                        BoEmergencyClose = true;
                                    }
                                    if (SystemShare.Config.ViewHackMessage)
                                    {
                                        //LogService.Warn(Format(CommandHelp.BunOverSpeed, ChrName, delayTime, nMsgCount));
                                    }
                                }
                                SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");// 如果超速则发送攻击失败信息
                            }
                            else
                            {
                                if (delayTime < SystemShare.Config.DropOverSpeed)
                                {
                                    SendSocket(M2Share.GetGoodTick);
                                    if (TestSpeedMode)
                                    {
                                        SysMsg(Format("速度异常 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                    }
                                }
                                else
                                {
                                    SendSelfDelayMsg(processMsg.wIdent, processMsg.wParam, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, "", delayTime);
                                    result = false;
                                }
                            }
                        }
                    }
                    break;
                case Messages.CM_WALK:
                    if (ClientWalkXY(processMsg.wIdent, (short)processMsg.nParam1, (short)processMsg.nParam2, processMsg.LateDelivery, ref delayTime))
                    {
                        ActionTick = HUtil32.GetTickCount();
                        SendSocket(M2Share.GetGoodTick);
                    }
                    else
                    {
                        if (delayTime == 0)
                        {
                            SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");
                        }
                        else
                        {
                            nMsgCount = GetWalkMsgCount();
                            if (nMsgCount >= SystemShare.Config.MaxWalkMsgCount)
                            {
                                OverSpeedCount++;
                                if (OverSpeedCount > SystemShare.Config.OverSpeedKickCount)
                                {
                                    if (SystemShare.Config.KickOverSpeed)
                                    {
                                        SysMsg(MessageSettings.KickClientUserMsg, MsgColor.Red, MsgType.Hint);
                                        BoEmergencyClose = true;
                                    }
                                    if (SystemShare.Config.ViewHackMessage)
                                    {
                                        //LogService.Warn(Format(CommandHelp.WalkOverSpeed, ChrName, delayTime, nMsgCount));
                                    }
                                }
                                SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");// 如果超速则发送攻击失败信息
                                if (TestSpeedMode)
                                {
                                    SysMsg(Format("速度异常 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                }
                            }
                            else
                            {
                                if (delayTime > SystemShare.Config.DropOverSpeed && SystemShare.Config.SpeedControlMode == 1 && IsFilterAction)
                                {
                                    SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");
                                    if (TestSpeedMode)
                                    {
                                        SysMsg(Format("速度异常 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                    }
                                }
                                else
                                {
                                    if (TestSpeedMode)
                                    {
                                        SysMsg(Format("操作延迟 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                    }
                                    SendSelfDelayMsg(processMsg.wIdent, processMsg.wParam, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, "", delayTime);
                                    result = false;
                                }
                            }
                        }
                    }
                    break;
                case Messages.CM_HORSERUN:
                    if (ClientHorseRunXY(processMsg.wIdent, (short)processMsg.nParam1, (short)processMsg.nParam2, processMsg.LateDelivery, ref delayTime))
                    {
                        ActionTick = HUtil32.GetTickCount();
                        SendSocket(M2Share.GetGoodTick);
                    }
                    else
                    {
                        if (delayTime == 0)
                        {
                            SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");
                        }
                        else
                        {
                            nMsgCount = GetRunMsgCount();
                            if (nMsgCount >= SystemShare.Config.MaxRunMsgCount)
                            {
                                OverSpeedCount++;
                                if (OverSpeedCount > SystemShare.Config.OverSpeedKickCount)
                                {
                                    if (SystemShare.Config.KickOverSpeed)
                                    {
                                        SysMsg(MessageSettings.KickClientUserMsg, MsgColor.Red, MsgType.Hint);
                                        BoEmergencyClose = true;
                                    }
                                    if (SystemShare.Config.ViewHackMessage)
                                    {
                                        // LogService.Warn(Format(CommandHelp.RunOverSpeed, ChrName, delayTime, nMsgCount));
                                    }
                                }
                                SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, ""); // 如果超速则发送攻击失败信息
                                if (TestSpeedMode)
                                {
                                    SysMsg(Format("速度异常 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                }
                            }
                            else
                            {
                                if (TestSpeedMode)
                                {
                                    SysMsg(Format("操作延迟 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                }
                                SendSelfDelayMsg(processMsg.wIdent, processMsg.wParam, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, "", delayTime);
                                result = false;
                            }
                        }
                    }
                    break;
                case Messages.CM_RUN:
                    if (ClientRunXY(processMsg.wIdent, (short)processMsg.nParam1, (short)processMsg.nParam2, processMsg.nParam3, ref delayTime))
                    {
                        ActionTick = HUtil32.GetTickCount();
                        SendSocket(M2Share.GetGoodTick);
                    }
                    else
                    {
                        if (delayTime == 0)
                        {
                            SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");
                        }
                        else
                        {
                            nMsgCount = GetRunMsgCount();
                            if (nMsgCount >= SystemShare.Config.MaxRunMsgCount)
                            {
                                OverSpeedCount++;
                                if (OverSpeedCount > SystemShare.Config.OverSpeedKickCount)
                                {
                                    if (SystemShare.Config.KickOverSpeed)
                                    {
                                        SysMsg(MessageSettings.KickClientUserMsg, MsgColor.Red, MsgType.Hint);
                                        BoEmergencyClose = true;
                                    }
                                    if (SystemShare.Config.ViewHackMessage)
                                    {
                                        //LogService.Warn(Format(CommandHelp.RunOverSpeed, ChrName, delayTime, nMsgCount));
                                    }
                                }
                                SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, ""); // 如果超速则发送攻击失败信息
                            }
                            else
                            {
                                if (delayTime > SystemShare.Config.DropOverSpeed && SystemShare.Config.SpeedControlMode == 1 && IsFilterAction)
                                {
                                    SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");
                                    if (TestSpeedMode)
                                    {
                                        SysMsg(Format("速度异常 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                    }
                                }
                                else
                                {
                                    if (TestSpeedMode)
                                    {
                                        SysMsg(Format("操作延迟 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                    }
                                    SendSelfDelayMsg(processMsg.wIdent, processMsg.wParam, processMsg.nParam1, processMsg.nParam2, Messages.CM_RUN, "", delayTime);
                                    result = false;
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
                case Messages.CM_TWINHIT:
                case Messages.CM_FIREHIT:
                    if (ClientHitXY(processMsg.wIdent, processMsg.nParam1, processMsg.nParam2, (byte)processMsg.wParam, processMsg.LateDelivery, ref delayTime))
                    {
                        ActionTick = HUtil32.GetTickCount();
                        SendSocket(M2Share.GetGoodTick);
                    }
                    else
                    {
                        if (delayTime == 0)
                        {
                            SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");
                        }
                        else
                        {
                            nMsgCount = GetHitMsgCount();
                            if (nMsgCount >= SystemShare.Config.MaxHitMsgCount)
                            {
                                OverSpeedCount++;
                                if (OverSpeedCount > SystemShare.Config.OverSpeedKickCount)
                                {
                                    if (SystemShare.Config.KickOverSpeed)
                                    {
                                        SysMsg(MessageSettings.KickClientUserMsg, MsgColor.Red, MsgType.Hint);
                                        BoEmergencyClose = true;
                                    }
                                    if (SystemShare.Config.ViewHackMessage)
                                    {
                                        //LogService.Warn(Format(CommandHelp.HitOverSpeed, ChrName, delayTime, nMsgCount));
                                    }
                                }
                                SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");// 如果超速则发送攻击失败信息
                            }
                            else
                            {
                                if (delayTime > SystemShare.Config.DropOverSpeed && SystemShare.Config.SpeedControlMode == 1 && IsFilterAction)
                                {
                                    SendSocket(M2Share.GetGoodTick);
                                    if (TestSpeedMode)
                                    {
                                        SysMsg(Format("速度异常 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                    }
                                }
                                else
                                {
                                    if (TestSpeedMode)
                                    {
                                        SysMsg("操作延迟 Ident: " + processMsg.wIdent + " Time: " + delayTime, MsgColor.Red, MsgType.Hint);
                                    }
                                    SendSelfDelayMsg(processMsg.wIdent, processMsg.wParam, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, "", delayTime);
                                    result = false;
                                }
                            }
                        }
                    }
                    break;
                case Messages.CM_SITDOWN:
                    if (ClientSitDownHit(processMsg.nParam1, processMsg.nParam2, (byte)processMsg.wParam, ref delayTime))
                    {
                        ActionTick = HUtil32.GetTickCount();
                        SendSocket(M2Share.GetGoodTick);
                    }
                    else
                    {
                        if (delayTime == 0)
                        {
                            SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");
                        }
                        else
                        {
                            nMsgCount = GetSiteDownMsgCount();
                            if (nMsgCount >= SystemShare.Config.MaxSitDonwMsgCount)
                            {
                                OverSpeedCount++;
                                if (OverSpeedCount > SystemShare.Config.OverSpeedKickCount)
                                {
                                    if (SystemShare.Config.KickOverSpeed)
                                    {
                                        SysMsg(MessageSettings.KickClientUserMsg, MsgColor.Red, MsgType.Hint);
                                        BoEmergencyClose = true;
                                    }
                                    if (SystemShare.Config.ViewHackMessage)
                                    {
                                        // LogService.Warn(Format(CommandHelp.BunOverSpeed, ChrName, delayTime, nMsgCount));
                                    }
                                }
                                SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");// 如果超速则发送攻击失败信息
                            }
                            else
                            {
                                if (delayTime < SystemShare.Config.DropOverSpeed)
                                {
                                    SendSocket(M2Share.GetGoodTick);
                                    if (TestSpeedMode)
                                    {
                                        SysMsg(Format("速度异常 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                    }
                                }
                                else
                                {
                                    if (TestSpeedMode)
                                    {
                                        SysMsg(Format("操作延迟 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                    }
                                    SendSelfDelayMsg(processMsg.wIdent, processMsg.wParam, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, "", delayTime);
                                    result = false;
                                }
                            }
                        }
                    }
                    break;
                case Messages.CM_SPELL:
                    if (ClientSpellXY(processMsg.wIdent, processMsg.wParam, (short)processMsg.nParam1, (short)processMsg.nParam2, SystemShare.ActorMgr.Get(processMsg.nParam3), processMsg.LateDelivery, ref delayTime))
                    {
                        ActionTick = HUtil32.GetTickCount();
                        SendSocket(M2Share.GetGoodTick);
                    }
                    else
                    {
                        if (delayTime == 0)
                        {
                            SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");
                        }
                        else
                        {
                            nMsgCount = GetSpellMsgCount();
                            if (nMsgCount >= SystemShare.Config.MaxSpellMsgCount)
                            {
                                OverSpeedCount++;
                                if (OverSpeedCount > SystemShare.Config.OverSpeedKickCount)
                                {
                                    if (SystemShare.Config.KickOverSpeed)
                                    {
                                        SysMsg(MessageSettings.KickClientUserMsg, MsgColor.Red, MsgType.Hint);
                                        BoEmergencyClose = true;
                                    }
                                    if (SystemShare.Config.ViewHackMessage)
                                    {
                                        // LogService.Warn(Format(CommandHelp.SpellOverSpeed, ChrName, delayTime, nMsgCount));
                                    }
                                }
                                SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");// 如果超速则发送攻击失败信息
                            }
                            else
                            {
                                if (delayTime > SystemShare.Config.DropOverSpeed && SystemShare.Config.SpeedControlMode == 1 && IsFilterAction)
                                {
                                    SendRefMsg(Messages.RM_MOVEFAIL, 0, 0, 0, 0, "");
                                    if (TestSpeedMode)
                                    {
                                        SysMsg(Format("速度异常 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                    }
                                }
                                else
                                {
                                    if (TestSpeedMode)
                                    {
                                        SysMsg(Format("操作延迟 Ident: {0} Time: {1}", processMsg.wIdent, delayTime), MsgColor.Red, MsgType.Hint);
                                    }
                                    SendSelfDelayMsg(processMsg.wIdent, processMsg.wParam, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, "", delayTime);
                                    result = false;
                                }
                            }
                        }
                    }
                    break;
                case Messages.CM_SAY:
                    ProcessUserLineMsg(processMsg.Msg);
                    break;
                case Messages.CM_PASSWORD:
                    ProcessClientPassword(processMsg);
                    break;
                case Messages.CM_QUERYVAL:
                    ProcessQueryValue(processMsg.nParam1, processMsg.Msg);
                    break;
                case Messages.RM_WALK:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_WALK, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, HUtil32.MakeWord((ushort)processMsg.wParam, baseObject.Light));
                        CharDesc walkmessage = default;
                        walkmessage.Feature = baseObject.GetFeature(baseObject);
                        walkmessage.Status = baseObject.CharStatus;
                        SendSocket(ClientMsg, EDCode.EncodePacket(walkmessage));
                    }
                    break;
                case Messages.RM_HORSERUN:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_HORSERUN, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, HUtil32.MakeWord((ushort)processMsg.wParam, baseObject.Light));
                        CharDesc horserunmessage = default;
                        horserunmessage.Feature = baseObject.GetFeature(baseObject);
                        horserunmessage.Status = baseObject.CharStatus;
                        SendSocket(ClientMsg, EDCode.EncodePacket(horserunmessage));
                    }
                    break;
                case Messages.RM_RUN:
                    if (processMsg.ActorId != ActorId && baseObject != null)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_RUN, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, HUtil32.MakeWord((ushort)processMsg.wParam, baseObject.Light));
                        CharDesc runmessage = default;
                        runmessage.Feature = baseObject.GetFeature(baseObject);
                        runmessage.Status = baseObject.CharStatus;
                        SendSocket(ClientMsg, EDCode.EncodePacket(runmessage));
                    }
                    break;
                case Messages.RM_HIT:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_HIT, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg);
                    }
                    break;
                case Messages.RM_HEAVYHIT:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_HEAVYHIT, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg, processMsg.Msg);
                    }
                    break;
                case Messages.RM_BIGHIT:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_BIGHIT, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg);
                    }
                    break;
                case Messages.RM_SPELL:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_SPELL, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg, processMsg.nParam3.ToString());
                    }
                    break;
                case Messages.RM_SPELL2:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_POWERHIT, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg);
                    }
                    break;
                case Messages.RM_MOVEFAIL:
                    ClientMsg = Messages.MakeMessage(Messages.SM_MOVEFAIL, ActorId, CurrX, CurrY, Dir);
                    CharDesc movefailmessage = default;
                    movefailmessage.Feature = baseObject.GetFeatureToLong();
                    movefailmessage.Status = baseObject.CharStatus;
                    SendSocket(ClientMsg, EDCode.EncodePacket(movefailmessage));
                    break;
                case Messages.RM_LONGHIT:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_LONGHIT, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg);
                    }
                    break;
                case Messages.RM_WIDEHIT:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_WIDEHIT, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg);
                    }
                    break;
                case Messages.RM_FIREHIT:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_FIREHIT, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg);
                    }
                    break;
                case Messages.RM_CRSHIT:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_CRSHIT, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg);
                    }
                    break;
                case Messages.RM_41:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_41, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg);
                    }
                    break;
                case Messages.RM_TWINHIT:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_TWINHIT, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg);
                    }
                    break;
                case Messages.RM_43:
                    if (processMsg.ActorId != ActorId)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_43, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg);
                    }
                    break;
                case Messages.RM_TURN:
                case Messages.RM_PUSH:
                case Messages.RM_RUSH:
                case Messages.RM_RUSHKUNG:
                    if (processMsg.ActorId != ActorId || processMsg.wIdent == Messages.RM_PUSH || processMsg.wIdent == Messages.RM_RUSH || processMsg.wIdent == Messages.RM_RUSHKUNG)
                    {
                        switch (processMsg.wIdent)
                        {
                            case Messages.RM_PUSH:
                                ClientMsg = Messages.MakeMessage(Messages.SM_BACKSTEP, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, HUtil32.MakeWord((ushort)processMsg.wParam, baseObject.Light));
                                break;
                            case Messages.RM_RUSH:
                                ClientMsg = Messages.MakeMessage(Messages.SM_RUSH, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, HUtil32.MakeWord((ushort)processMsg.wParam, baseObject.Light));
                                break;
                            case Messages.RM_RUSHKUNG:
                                ClientMsg = Messages.MakeMessage(Messages.SM_RUSHKUNG, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, HUtil32.MakeWord((ushort)processMsg.wParam, baseObject.Light));
                                break;
                            default:
                                ClientMsg = Messages.MakeMessage(Messages.SM_TURN, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, HUtil32.MakeWord((ushort)processMsg.wParam, baseObject.Light));
                                break;
                        }
                        CharDesc turnmessage = default;
                        turnmessage.Feature = baseObject.GetFeature(baseObject);
                        turnmessage.Status = baseObject.CharStatus;
                        string sendActonMsg = EDCode.EncodePacket(turnmessage);
                        nObjCount = GetChrColor(baseObject);
                        if (!string.IsNullOrEmpty(processMsg.Msg))
                        {
                            sendActonMsg = sendActonMsg + EDCode.EncodeString($"{processMsg.Msg}/{nObjCount}");
                        }
                        SendSocket(ClientMsg, sendActonMsg);
                        if (processMsg.wIdent == Messages.RM_TURN)
                        {
                            nObjCount = baseObject.GetFeatureToLong();
                            SendDefMessage(Messages.SM_FEATURECHANGED, processMsg.ActorId, HUtil32.LoWord(nObjCount), HUtil32.HiWord(nObjCount), baseObject.GetFeatureEx());
                        }
                    }
                    break;
                case Messages.RM_STRUCK:
                case Messages.RM_STRUCK_MAG:
                    if (processMsg.wParam > 0)
                    {
                        if (processMsg.ActorId == ActorId)
                        {
                            if (SystemShare.ActorMgr.Get(processMsg.nParam3) != null)
                            {
                                if (SystemShare.ActorMgr.Get(processMsg.nParam3).Race == ActorRace.Play)
                                {
                                    SetPkFlag(SystemShare.ActorMgr.Get(processMsg.nParam3));
                                }
                                SetLastHiter(SystemShare.ActorMgr.Get(processMsg.nParam3));
                            }
                            if (this.MyGuild != null && this.Castle != null)
                            {
                                if (SystemShare.CastleMgr.IsCastleMember(this) != null && SystemShare.ActorMgr.Get(processMsg.nParam3) != null)
                                {
                                    if (SystemShare.ActorMgr.Get(processMsg.nParam3).Race == ActorRace.Guard)
                                    {
                                        ((GuardUnit)SystemShare.ActorMgr.Get(processMsg.nParam3)).CrimeforCastle = true;
                                        ((GuardUnit)SystemShare.ActorMgr.Get(processMsg.nParam3)).CrimeforCastleTime = HUtil32.GetTickCount();
                                    }
                                }
                            }
                            HealthTick = 0;
                            SpellTick = 0;
                            PerHealth -= 1;
                            PerSpell -= 1;
                            StruckTick = HUtil32.GetTickCount();
                        }
                        if (processMsg.ActorId != 0)
                        {
                            if (processMsg.ActorId == ActorId && SystemShare.Config.DisableSelfStruck || baseObject.Race == ActorRace.Play && SystemShare.Config.DisableStruck)
                            {
                                baseObject.SendRefMsg(Messages.RM_HEALTHSPELLCHANGED, 0, 0, 0, 0, "");
                            }
                            else
                            {
                                ClientMsg = Messages.MakeMessage(Messages.SM_STRUCK, processMsg.ActorId, baseObject.WAbil.HP, baseObject.WAbil.MaxHP, processMsg.wParam);
                                MessageBodyWL struckMessage = default;
                                struckMessage.Param1 = baseObject.GetFeature(this);
                                struckMessage.Param2 = baseObject.CharStatus;
                                struckMessage.Tag1 = processMsg.nParam3;
                                if (processMsg.wIdent == Messages.RM_STRUCK_MAG)
                                {
                                    struckMessage.Tag2 = 1;
                                }
                                else
                                {
                                    struckMessage.Tag2 = 0;
                                }
                                SendSocket(ClientMsg, EDCode.EncodePacket(struckMessage));
                            }
                        }
                    }
                    break;
                case Messages.RM_MAGHEALING:
                    if ((IncHealing + processMsg.nParam1) < 300)
                    {
                        IncHealing += (ushort)processMsg.nParam1;
                        PerHealing = 5;
                    }
                    else
                    {
                        IncHealing = 300;
                    }
                    break;
                case Messages.RM_DEATH:
                    if (processMsg.nParam3 == 1)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_NOWDEATH, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        if (processMsg.ActorId == ActorId)
                        {
                            if (SystemShare.FunctionNPC != null)
                            {
                                SystemShare.FunctionNPC.GotoLable(this, "@OnDeath", false);
                            }
                        }
                    }
                    else
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_DEATH, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                    }
                    CharDesc deathmessage = default;
                    deathmessage.Feature = baseObject.GetFeature(this);
                    deathmessage.Status = baseObject.CharStatus;
                    SendSocket(ClientMsg, EDCode.EncodePacket(deathmessage));
                    break;
                case Messages.RM_DISAPPEAR:
                    ClientMsg = Messages.MakeMessage(Messages.SM_DISAPPEAR, processMsg.ActorId, 0, 0, 0);
                    SendSocket(ClientMsg);
                    break;
                case Messages.RM_SKELETON:
                    ClientMsg = Messages.MakeMessage(Messages.SM_SKELETON, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                    CharDesc skeletonmessage = default;
                    skeletonmessage.Feature = baseObject.GetFeature(this);
                    skeletonmessage.Status = baseObject.CharStatus;
                    SendSocket(ClientMsg, EDCode.EncodePacket(skeletonmessage));
                    break;
                case Messages.RM_USERNAME:
                    ClientMsg = Messages.MakeMessage(Messages.SM_USERNAME, processMsg.ActorId, GetChrColor(baseObject), 0, 0);
                    SendSocket(ClientMsg, EDCode.EncodeString(processMsg.Msg));
                    break;
                case Messages.RM_WINEXP:
                    ClientMsg = Messages.MakeMessage(Messages.SM_WINEXP, Abil.Exp, HUtil32.LoWord(processMsg.nParam1), HUtil32.HiWord(processMsg.nParam1), 0);
                    SendSocket(ClientMsg);
                    break;
                case Messages.RM_LEVELUP:
                    ClientMsg = Messages.MakeMessage(Messages.SM_LEVELUP, Abil.Exp, Abil.Level, 0, 0);
                    SendSocket(ClientMsg);
                    ClientMsg = Messages.MakeMessage(Messages.SM_ABILITY, Gold, HUtil32.MakeWord((byte)Job, 99), HUtil32.LoWord(GameGold), HUtil32.HiWord(GameGold));
                    SendSocket(ClientMsg, EDCode.EncodeMessage(WAbil));
                    SendDefMessage(Messages.SM_SUBABILITY, HUtil32.MakeLong(HUtil32.MakeWord(AntiMagic, 0), 0), HUtil32.MakeWord(HitPoint, SpeedPoint), HUtil32.MakeWord(AntiPoison, PoisonRecover), HUtil32.MakeWord(HealthRecover, SpellRecover));
                    break;
                case Messages.RM_CHANGENAMECOLOR:
                    SendDefMessage(Messages.SM_CHANGENAMECOLOR, processMsg.ActorId, GetChrColor(baseObject), 0, 0);
                    break;
                case Messages.RM_LOGON:
                    ClientMsg = Messages.MakeMessage(Messages.SM_NEWMAP, ActorId, CurrX, CurrY, DayBright());
                    SendSocket(ClientMsg, EDCode.EncodeString(MapFileName));
                    SendMsg(Messages.RM_CHANGELIGHT, 0, 0, 0, 0);
                    SendLogon();
                    SendServerConfig();
                    ClientQueryUserName(ActorId, CurrX, CurrY);
                    RefUserState();
                    SendMapDescription();
                    SendGoldInfo(true);
                    ClientMsg = Messages.MakeMessage(Messages.SM_VERSION_FAIL, SystemShare.Config.ClientFile1_CRC, HUtil32.LoWord(SystemShare.Config.ClientFile2_CRC), HUtil32.HiWord(SystemShare.Config.ClientFile2_CRC), 0);
                    SendSocket(ClientMsg, "<<<<<<");
                    break;
                case Messages.RM_HEAR:
                case Messages.RM_WHISPER:
                case Messages.RM_CRY:
                case Messages.RM_SYSMESSAGE:
                case Messages.RM_GROUPMESSAGE:
                case Messages.RM_SYSMESSAGE2:
                case Messages.RM_GUILDMESSAGE:
                case Messages.RM_SYSMESSAGE3:
                case Messages.RM_MOVEMESSAGE:
                case Messages.RM_MERCHANTSAY:
                    switch (processMsg.wIdent)
                    {
                        case Messages.RM_HEAR:
                            ClientMsg = Messages.MakeMessage(Messages.SM_HEAR, processMsg.ActorId, HUtil32.MakeWord((ushort)processMsg.nParam1, (ushort)processMsg.nParam2), 0, 1);
                            break;
                        case Messages.RM_WHISPER:
                            ClientMsg = Messages.MakeMessage(Messages.SM_WHISPER, processMsg.ActorId, HUtil32.MakeWord((ushort)processMsg.nParam1, (ushort)processMsg.nParam2), 0, 1);
                            break;
                        case Messages.RM_CRY:
                            ClientMsg = Messages.MakeMessage(Messages.SM_HEAR, processMsg.ActorId, HUtil32.MakeWord((ushort)processMsg.nParam1, (ushort)processMsg.nParam2), 0, 1);
                            break;
                        case Messages.RM_SYSMESSAGE:
                            ClientMsg = Messages.MakeMessage(Messages.SM_SYSMESSAGE, processMsg.ActorId, HUtil32.MakeWord((ushort)processMsg.nParam1, (ushort)processMsg.nParam2), 0, 1);
                            break;
                        case Messages.RM_GROUPMESSAGE:
                            ClientMsg = Messages.MakeMessage(Messages.SM_SYSMESSAGE, processMsg.ActorId, HUtil32.MakeWord((ushort)processMsg.nParam1, (ushort)processMsg.nParam2), 0, 1);
                            break;
                        case Messages.RM_GUILDMESSAGE:
                            ClientMsg = Messages.MakeMessage(Messages.SM_GUILDMESSAGE, processMsg.ActorId, HUtil32.MakeWord((ushort)processMsg.nParam1, (ushort)processMsg.nParam2), 0, 1);
                            break;
                        case Messages.RM_MERCHANTSAY:
                            ClientMsg = Messages.MakeMessage(Messages.SM_MERCHANTSAY, processMsg.ActorId, HUtil32.MakeWord((ushort)processMsg.nParam1, (ushort)processMsg.nParam2), 0, 1);
                            break;
                        case Messages.RM_MOVEMESSAGE:
                            ClientMsg = Messages.MakeMessage(Messages.SM_MOVEMESSAGE, processMsg.ActorId, HUtil32.MakeWord((ushort)processMsg.nParam1, (ushort)processMsg.nParam2), processMsg.nParam3, processMsg.wParam);
                            break;
                    }
                    SendSocket(ClientMsg, EDCode.EncodeString(processMsg.Msg));
                    break;
                case Messages.RM_ABILITY:
                    ClientMsg = Messages.MakeMessage(Messages.SM_ABILITY, Gold, HUtil32.MakeWord((byte)Job, 99), HUtil32.LoWord(GameGold), HUtil32.HiWord(GameGold));
                    SendSocket(ClientMsg, EDCode.EncodeMessage(WAbil));
                    break;
                case Messages.RM_HEALTHSPELLCHANGED:
                    ClientMsg = Messages.MakeMessage(Messages.SM_HEALTHSPELLCHANGED, processMsg.ActorId, baseObject.WAbil.HP, baseObject.WAbil.MP, baseObject.WAbil.MaxHP);
                    SendSocket(ClientMsg);
                    break;
                case Messages.RM_DAYCHANGING:
                    ClientMsg = Messages.MakeMessage(Messages.SM_DAYCHANGING, 0, Bright, DayBright(), 0);
                    SendSocket(ClientMsg);
                    break;
                case Messages.RM_ITEMSHOW:
                    SendDefMessage(Messages.SM_ITEMSHOW, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, processMsg.wParam, processMsg.Msg);
                    break;
                case Messages.RM_ITEMHIDE:
                    SendDefMessage(Messages.SM_ITEMHIDE, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, 0);
                    break;
                case Messages.RM_DOOROPEN:
                    SendDefMessage(Messages.SM_OPENDOOR_OK, 0, processMsg.nParam1, processMsg.nParam2, 0);
                    break;
                case Messages.RM_DOORCLOSE:
                    SendDefMessage(Messages.SM_CLOSEDOOR, 0, processMsg.nParam1, processMsg.nParam2, 0);
                    break;
                case Messages.RM_SENDUSEITEMS:
                    SendUseItems();
                    break;
                case Messages.RM_WEIGHTCHANGED:
                    SendDefMessage(Messages.SM_WEIGHTCHANGED, WAbil.Weight, WAbil.WearWeight, WAbil.HandWeight, (((WAbil.Weight + WAbil.WearWeight + WAbil.HandWeight) ^ 0x3A5F) ^ 0x1F35) ^ 0xaa21);
                    break;
                case Messages.RM_FEATURECHANGED:
                    SendDefMessage(Messages.SM_FEATURECHANGED, processMsg.ActorId, HUtil32.LoWord(processMsg.nParam1), HUtil32.HiWord(processMsg.nParam1), processMsg.wParam);
                    break;
                case Messages.RM_CLEAROBJECTS:
                    VisibleEvents.Clear();
                    for (int i = 0; i < VisibleItems.Count; i++)
                    {
                        VisibleItems[i] = null;
                    }
                    VisibleItems.Clear();
                    SendDefMessage(Messages.SM_CLEAROBJECTS, 0, 0, 0, 0);
                    break;
                case Messages.RM_CHANGEMAP:
                    MapMoveTick = HUtil32.GetTickCount();
                    SendDefMessage(Messages.SM_CHANGEMAP, ActorId, CurrX, CurrY, DayBright(), processMsg.Msg);
                    RefUserState();
                    SendMapDescription();
                    SendServerConfig();
                    break;
                case Messages.RM_BUTCH:
                    if (processMsg.ActorId != 0)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_BUTCH, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg);
                    }
                    break;
                case Messages.RM_MAGICFIRE:
                    ClientMsg = Messages.MakeMessage(Messages.SM_MAGICFIRE, processMsg.ActorId, HUtil32.LoWord(processMsg.nParam2), HUtil32.HiWord(processMsg.nParam2), processMsg.nParam1);
                    byte[] by = BitConverter.GetBytes(processMsg.nParam3);
                    string sSendStr = EDCode.EncodeBuffer(by, by.Length);
                    SendSocket(ClientMsg, sSendStr);
                    break;
                case Messages.RM_MAGICFIREFAIL:
                    SendDefMessage(Messages.SM_MAGICFIRE_FAIL, processMsg.ActorId, 0, 0, 0);
                    break;
                case Messages.RM_SENDMYMAGIC:
                    SendUseMagic();
                    break;
                case Messages.RM_MAGIC_LVEXP:
                    SendDefMessage(Messages.SM_MAGIC_LVEXP, processMsg.nParam1, processMsg.nParam2, HUtil32.LoWord(processMsg.nParam3), HUtil32.HiWord(processMsg.nParam3));
                    break;
                case Messages.RM_DURACHANGE:
                    SendDefMessage(Messages.SM_DURACHANGE, processMsg.nParam1, processMsg.wParam, HUtil32.LoWord(processMsg.nParam2), HUtil32.HiWord(processMsg.nParam2));
                    break;
                case Messages.RM_MERCHANTDLGCLOSE:
                    SendDefMessage(Messages.SM_MERCHANTDLGCLOSE, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_SENDGOODSLIST:
                    SendDefMessage(Messages.SM_SENDGOODSLIST, processMsg.nParam1, processMsg.nParam2, 0, 0, processMsg.Msg);
                    break;
                case Messages.RM_SENDUSERSELL:
                    SendDefMessage(Messages.SM_SENDUSERSELL, processMsg.nParam1, processMsg.nParam2, 0, 0, processMsg.Msg);
                    break;
                case Messages.RM_SENDBUYPRICE:
                    SendDefMessage(Messages.SM_SENDBUYPRICE, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_USERSELLITEM_OK:
                    SendDefMessage(Messages.SM_USERSELLITEM_OK, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_USERSELLITEM_FAIL:
                    SendDefMessage(Messages.SM_USERSELLITEM_FAIL, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_BUYITEM_SUCCESS:
                    SendDefMessage(Messages.SM_BUYITEM_SUCCESS, processMsg.nParam1, HUtil32.LoWord(processMsg.nParam2), HUtil32.HiWord(processMsg.nParam2), 0);
                    break;
                case Messages.RM_BUYITEM_FAIL:
                    SendDefMessage(Messages.SM_BUYITEM_FAIL, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_SENDDETAILGOODSLIST:
                    SendDefMessage(Messages.SM_SENDDETAILGOODSLIST, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, 0, processMsg.Msg);
                    break;
                case Messages.RM_GOLDCHANGED:
                    SendDefMessage(Messages.SM_GOLDCHANGED, Gold, HUtil32.LoWord(GameGold), HUtil32.HiWord(GameGold), 0);
                    break;
                case Messages.RM_GAMEGOLDCHANGED:
                    SendGoldInfo(false);
                    break;
                case Messages.RM_CHANGELIGHT:
                    SendDefMessage(Messages.SM_CHANGELIGHT, processMsg.ActorId, baseObject.Light, (short)SystemShare.Config.nClientKey, 0);
                    break;
                case Messages.RM_LAMPCHANGEDURA:
                    SendDefMessage(Messages.SM_LAMPCHANGEDURA, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_CHARSTATUSCHANGED:
                    SendDefMessage(Messages.SM_CHARSTATUSCHANGED, processMsg.ActorId, HUtil32.LoWord(processMsg.nParam1), HUtil32.HiWord(processMsg.nParam1), this.HitSpeed);
                    break;
                case Messages.RM_GROUPCANCEL:
                    SendDefMessage(Messages.SM_GROUPCANCEL, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_SENDUSERREPAIR:
                case Messages.RM_SENDUSERSREPAIR:
                    SendDefMessage(Messages.SM_SENDUSERREPAIR, processMsg.nParam1, processMsg.nParam2, 0, 0);
                    break;
                case Messages.RM_USERREPAIRITEM_OK:
                    SendDefMessage(Messages.SM_USERREPAIRITEM_OK, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, 0);
                    break;
                case Messages.RM_SENDREPAIRCOST:
                    SendDefMessage(Messages.SM_SENDREPAIRCOST, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_USERREPAIRITEM_FAIL:
                    SendDefMessage(Messages.SM_USERREPAIRITEM_FAIL, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_USERSTORAGEITEM:
                    SendDefMessage(Messages.SM_SENDUSERSTORAGEITEM, processMsg.nParam1, processMsg.nParam2, 0, 0);
                    break;
                case Messages.RM_USERGETBACKITEM:
                    SendSaveItemList(processMsg.nParam1);
                    break;
                case Messages.RM_SENDDELITEMLIST:
                    IList<DeleteItem> delItemList = (IList<DeleteItem>)SystemShare.ActorMgr.GetOhter(processMsg.nParam1);
                    SendDelItemList(delItemList);
                    SystemShare.ActorMgr.RevomeOhter(processMsg.nParam1);
                    break;
                case Messages.RM_USERMAKEDRUGITEMLIST:
                    SendDefMessage(Messages.SM_SENDUSERMAKEDRUGITEMLIST, processMsg.nParam1, processMsg.nParam2, 0, 0, processMsg.Msg);
                    break;
                case Messages.RM_MAKEDRUG_SUCCESS:
                    SendDefMessage(Messages.SM_MAKEDRUG_SUCCESS, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_MAKEDRUG_FAIL:
                    SendDefMessage(Messages.SM_MAKEDRUG_FAIL, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_ALIVE:
                    ClientMsg = Messages.MakeMessage(Messages.SM_ALIVE, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                    CharDesc alivemessage = default;
                    alivemessage.Feature = baseObject.GetFeature(this);
                    alivemessage.Status = baseObject.CharStatus;
                    SendSocket(ClientMsg, EDCode.EncodePacket(alivemessage));
                    break;
                case Messages.RM_DIGUP:
                    ClientMsg = Messages.MakeMessage(Messages.SM_DIGUP, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, HUtil32.MakeWord((ushort)processMsg.wParam, baseObject.Light));
                    MessageBodyWL digupMessage = default;
                    digupMessage.Param1 = baseObject.GetFeature(this);
                    digupMessage.Param2 = baseObject.CharStatus;
                    digupMessage.Tag1 = processMsg.nParam3;
                    digupMessage.Tag1 = 0;
                    SendSocket(ClientMsg, EDCode.EncodePacket(digupMessage));
                    break;
                case Messages.RM_DIGDOWN:
                    ClientMsg = Messages.MakeMessage(Messages.SM_DIGDOWN, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, 0);
                    SendSocket(ClientMsg);
                    break;
                case Messages.RM_FLYAXE:
                    if (SystemShare.ActorMgr.Get(processMsg.nParam3) != null)
                    {
                        MessageBodyW flyaxeMessage = default;
                        flyaxeMessage.Param1 = (ushort)SystemShare.ActorMgr.Get(processMsg.nParam3).CurrX;
                        flyaxeMessage.Param2 = (ushort)SystemShare.ActorMgr.Get(processMsg.nParam3).CurrY;
                        flyaxeMessage.Tag1 = HUtil32.LoWord(processMsg.nParam3);
                        flyaxeMessage.Tag2 = HUtil32.HiWord(processMsg.nParam3);
                        ClientMsg = Messages.MakeMessage(Messages.SM_FLYAXE, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.wParam);
                        SendSocket(ClientMsg, EDCode.EncodePacket(flyaxeMessage));
                    }
                    break;
                case Messages.RM_LIGHTING:
                    if (SystemShare.ActorMgr.Get(processMsg.nParam3) != null)
                    {
                        MessageBodyWL lightingMessage = default;
                        lightingMessage.Param1 = SystemShare.ActorMgr.Get(processMsg.nParam3).CurrX;
                        lightingMessage.Param2 = SystemShare.ActorMgr.Get(processMsg.nParam3).CurrY;
                        lightingMessage.Tag1 = processMsg.nParam3;
                        lightingMessage.Tag2 = processMsg.wParam;
                        ClientMsg = Messages.MakeMessage(Messages.SM_LIGHTING, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, baseObject.Dir);
                        SendSocket(ClientMsg, EDCode.EncodePacket(lightingMessage));
                    }
                    break;
                case Messages.RM_10205:
                    SendDefMessage(Messages.SM_716, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3);
                    break;
                case Messages.RM_CHANGEGUILDNAME:
                    SendChangeGuildName();
                    break;
                case Messages.RM_SUBABILITY:
                    SendDefMessage(Messages.SM_SUBABILITY, HUtil32.MakeLong(HUtil32.MakeWord(AntiMagic, 0), 0), HUtil32.MakeWord(HitPoint, SpeedPoint), HUtil32.MakeWord(AntiPoison, PoisonRecover), HUtil32.MakeWord(HealthRecover, SpellRecover));
                    break;
                case Messages.RM_BUILDGUILD_OK:
                    SendDefMessage(Messages.SM_BUILDGUILD_OK, 0, 0, 0, 0);
                    break;
                case Messages.RM_BUILDGUILD_FAIL:
                    SendDefMessage(Messages.SM_BUILDGUILD_FAIL, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_DONATE_OK:
                    SendDefMessage(Messages.SM_DONATE_OK, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_DONATE_FAIL:
                    SendDefMessage(Messages.SM_DONATE_FAIL, processMsg.nParam1, 0, 0, 0);
                    break;
                case Messages.RM_MYSTATUS:
                    SendDefMessage(Messages.SM_MYSTATUS, 0, (short)GetMyStatus(), 0, 0);
                    break;
                case Messages.RM_MENU_OK:
                    SendDefMessage(Messages.SM_MENU_OK, processMsg.nParam1, 0, 0, 0, processMsg.Msg);
                    break;
                case Messages.RM_SPACEMOVE_FIRE:
                case Messages.RM_SPACEMOVE_FIRE2:
                    if (processMsg.wIdent == Messages.RM_SPACEMOVE_FIRE)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_SPACEMOVE_HIDE, processMsg.ActorId, 0, 0, 0);
                    }
                    else
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_SPACEMOVE_HIDE2, processMsg.ActorId, 0, 0, 0);
                    }
                    SendSocket(ClientMsg);
                    break;
                case Messages.RM_SPACEMOVE_SHOW:
                case Messages.RM_SPACEMOVE_SHOW2:
                    if (processMsg.wIdent == Messages.RM_SPACEMOVE_SHOW)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_SPACEMOVE_SHOW, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, HUtil32.MakeWord((ushort)processMsg.wParam, baseObject.Light));
                    }
                    else
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_SPACEMOVE_SHOW2, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, HUtil32.MakeWord((ushort)processMsg.wParam, baseObject.Light));
                    }
                    CharDesc showMessage = default;
                    showMessage.Feature = baseObject.GetFeature(this);
                    showMessage.Status = baseObject.CharStatus;
                    string sendMsg = EDCode.EncodePacket(showMessage);
                    nObjCount = GetChrColor(baseObject);
                    if (!string.IsNullOrEmpty(processMsg.Msg))
                    {
                        sendMsg = sendMsg + EDCode.EncodeString(processMsg.Msg + '/' + nObjCount);
                    }
                    SendSocket(ClientMsg, sendMsg);
                    break;
                case Messages.RM_RECONNECTION:
                    BoReconnection = true;
                    SendDefMessage(Messages.SM_RECONNECT, 0, 0, 0, 0, processMsg.Msg);
                    break;
                case Messages.RM_HIDEEVENT:
                    SendDefMessage(Messages.SM_HIDEEVENT, processMsg.nParam1, processMsg.wParam, processMsg.nParam2, processMsg.nParam3);
                    break;
                case Messages.RM_SHOWEVENT:
                    ShortMessage shortMessage = new ShortMessage();
                    shortMessage.Ident = HUtil32.HiWord(processMsg.nParam2);
                    shortMessage.wMsg = 0;
                    ClientMsg = Messages.MakeMessage(Messages.SM_SHOWEVENT, processMsg.nParam1, processMsg.wParam, processMsg.nParam2, processMsg.nParam3);
                    SendSocket(ClientMsg, EDCode.EncodePacket(shortMessage));
                    break;
                case Messages.RM_ADJUST_BONUS:
                    SendAdjustBonus();
                    break;
                case Messages.RM_10401:
                    ChangeServerMakeSlave((SlaveInfo)SystemShare.ActorMgr.GetOhter(processMsg.nParam1));
                    SystemShare.ActorMgr.RevomeOhter(processMsg.nParam1);
                    break;
                case Messages.RM_OPENHEALTH:
                    SendDefMessage(Messages.SM_OPENHEALTH, processMsg.ActorId, baseObject.WAbil.HP, baseObject.WAbil.MaxHP, 0);
                    break;
                case Messages.RM_CLOSEHEALTH:
                    SendDefMessage(Messages.SM_CLOSEHEALTH, processMsg.ActorId, 0, 0, 0);
                    break;
                case Messages.RM_BREAKWEAPON:
                    SendDefMessage(Messages.SM_BREAKWEAPON, processMsg.ActorId, 0, 0, 0);
                    break;
                case Messages.RM_ABILSEEHEALGAUGE:
                    SendDefMessage(Messages.SM_INSTANCEHEALGUAGE, processMsg.ActorId, baseObject.WAbil.HP, baseObject.WAbil.MaxHP, 0);
                    break;
                case Messages.RM_CHANGEFACE:
                    if (processMsg.nParam1 != 0 && processMsg.nParam2 != 0)
                    {
                        ClientMsg = Messages.MakeMessage(Messages.SM_CHANGEFACE, processMsg.nParam1, HUtil32.LoWord(processMsg.nParam2), HUtil32.HiWord(processMsg.nParam2), 0);
                        CharDesc changeFaceMessage = default;
                        changeFaceMessage.Feature = SystemShare.ActorMgr.Get(processMsg.nParam2).GetFeature(this);
                        changeFaceMessage.Status = SystemShare.ActorMgr.Get(processMsg.nParam2).CharStatus;
                        SendSocket(ClientMsg, EDCode.EncodePacket(changeFaceMessage));
                    }
                    break;
                case Messages.RM_PASSWORD:
                    SendDefMessage(Messages.SM_PASSWORD, 0, 0, 0, 0);
                    break;
                case Messages.RM_PLAYDICE:
                    MessageBodyWL playDiceMessage = default;
                    playDiceMessage.Param1 = processMsg.nParam1;
                    playDiceMessage.Param2 = processMsg.nParam2;
                    playDiceMessage.Tag1 = processMsg.nParam3;
                    ClientMsg = Messages.MakeMessage(Messages.SM_PLAYDICE, processMsg.ActorId, processMsg.wParam, 0, 0);
                    SendSocket(ClientMsg, EDCode.EncodePacket(playDiceMessage) + EDCode.EncodeString(processMsg.Msg));
                    break;
                case Messages.RM_PASSWORDSTATUS:
                    ClientMsg = Messages.MakeMessage(Messages.SM_PASSWORDSTATUS, processMsg.ActorId, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3);
                    SendSocket(ClientMsg, processMsg.Msg);
                    break;
                case Messages.RM_SENDDEALOFFFORM:// 打开出售物品窗口
                    SendDefMessage(Messages.SM_SENDDEALOFFFORM, processMsg.nParam1, processMsg.nParam2, 0, 0, processMsg.Msg);
                    break;
                case Messages.RM_QUERYYBSELL:// 查询正在出售的物品
                    ClientMsg = Messages.MakeMessage(Messages.SM_QUERYYBSELL, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, processMsg.wParam);
                    SendSocket(ClientMsg, processMsg.Msg);
                    break;
                case Messages.RM_QUERYYBDEAL:// 查询可以的购买物品
                    ClientMsg = Messages.MakeMessage(Messages.SM_QUERYYBDEAL, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, processMsg.wParam);
                    SendSocket(ClientMsg, processMsg.Msg);
                    break;
                case Messages.CM_SELLOFFADDITEM:// 客户端往出售物品窗口里加物品 
                    ClientAddSellOffItem(processMsg.nParam1, processMsg.Msg);
                    break;
                case Messages.CM_SELLOFFDELITEM:// 客户端删除出售物品窗里的物品 
                    ClientDelSellOffItem(processMsg.nParam1, processMsg.Msg);
                    break;
                case Messages.CM_SELLOFFCANCEL:// 客户端取消元宝寄售 
                    ClientCancelSellOff();
                    break;
                case Messages.CM_SELLOFFEND:// 客户端元宝寄售结束 
                    ClientSellOffEnd(processMsg.Msg, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3);
                    break;
                case Messages.CM_CANCELSELLOFFITEMING:// 取消正在寄售的物品(出售人)
                    ClientCancelSellOffIng();
                    break;
                case Messages.CM_SELLOFFBUYCANCEL:// 取消寄售 物品购买(购买人)
                    ClientBuyCancelSellOff(processMsg.Msg);// 出售人
                    break;
                case Messages.CM_SELLOFFBUY:// 确定购买寄售物品
                    ClientBuySellOffItme(processMsg.Msg);// 出售人
                    break;
                case Messages.RM_SELLOFFCANCEL:// 元宝寄售取消出售
                    ClientMsg = Messages.MakeMessage(Messages.SM_SellOffCANCEL, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, processMsg.wParam);
                    SendSocket(ClientMsg, processMsg.Msg);
                    break;
                case Messages.RM_SELLOFFADDITEM_OK:// 客户端往出售物品窗口里加物品 成功
                    ClientMsg = Messages.MakeMessage(Messages.SM_SELLOFFADDITEM_OK, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, processMsg.wParam);
                    SendSocket(ClientMsg, processMsg.Msg);
                    break;
                case Messages.RM_SellOffADDITEM_FAIL:// 客户端往出售物品窗口里加物品 失败
                    ClientMsg = Messages.MakeMessage(Messages.SM_SellOffADDITEM_FAIL, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, processMsg.wParam);
                    SendSocket(ClientMsg, processMsg.Msg);
                    break;
                case Messages.RM_SELLOFFDELITEM_OK:// 客户端删除出售物品窗里的物品 成功
                    ClientMsg = Messages.MakeMessage(Messages.SM_SELLOFFDELITEM_OK, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, processMsg.wParam);
                    SendSocket(ClientMsg, processMsg.Msg);
                    break;
                case Messages.RM_SELLOFFDELITEM_FAIL:// 客户端删除出售物品窗里的物品 失败
                    ClientMsg = Messages.MakeMessage(Messages.SM_SELLOFFDELITEM_FAIL, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, processMsg.wParam);
                    SendSocket(ClientMsg, processMsg.Msg);
                    break;
                case Messages.RM_SELLOFFEND_OK:// 客户端元宝寄售结束 成功
                    ClientMsg = Messages.MakeMessage(Messages.SM_SELLOFFEND_OK, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, processMsg.wParam);
                    SendSocket(ClientMsg, processMsg.Msg);
                    break;
                case Messages.RM_SELLOFFEND_FAIL:// 客户端元宝寄售结束 失败
                    ClientMsg = Messages.MakeMessage(Messages.SM_SELLOFFEND_FAIL, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, processMsg.wParam);
                    SendSocket(ClientMsg, processMsg.Msg);
                    break;
                case Messages.RM_SELLOFFBUY_OK:// 购买成功
                    ClientMsg = Messages.MakeMessage(Messages.SM_SELLOFFBUY_OK, processMsg.nParam1, processMsg.nParam2, processMsg.nParam3, processMsg.wParam);
                    SendSocket(ClientMsg, processMsg.Msg);
                    break;
                case Messages.RM_PLAYERKILLMONSTER:
                    KillTargetTrigger(processMsg.wParam, processMsg.nParam1);
                    break;
                case Messages.RM_SPIRITSUITE:
                    ProcessSpiritSuite();
                    break;
                case Messages.RM_MASTERDIEMUTINY:
                    ProcessSlaveMutiny();
                    break;
                case Messages.RM_MASTERDIEGHOST:
                    ProcessSlaveGhost();
                    break;
                default:
                    result = base.Operate(processMsg);
                    break;
            }
            return result;
        }

        public override void Disappear()
        {
            if (BoReadyRun)
            {
                DisappearA();
            }
            if (Transparent && HideMode)
            {
                StatusTimeArr[PoisonState.STATETRANSPARENT] = 0;
            }
            if (GroupOwner != 0)
            {
                IPlayerActor groupOwnerPlay = (IPlayerActor)SystemShare.ActorMgr.Get(GroupOwner);
                groupOwnerPlay.DelMember(this);
            }
            if (MyGuild != null)
            {
                MyGuild.DelHumanObj(this);
            }
            LogonTimcCost();
            base.Disappear();
        }

        public override void DropUseItems(int baseObject)
        {
            const string sExceptionMsg = "[Exception] PlayObject::DropUseItems";
            try
            {
                if (AngryRing || NoDropUseItem)
                {
                    return;
                }
                IList<DeleteItem> dropItemList = new List<DeleteItem>();
                StdItem stdItem;
                for (int i = 0; i < UseItems.Length; i++)
                {
                    if (UseItems[i] == null)
                    {
                        continue;
                    }
                    stdItem = SystemShare.EquipmentSystem.GetStdItem(UseItems[i].Index);
                    if (stdItem != null)
                    {
                        if ((stdItem.ItemDesc & 8) != 0)
                        {
                            dropItemList.Add(new DeleteItem() { MakeIndex = UseItems[i].MakeIndex });
                            if (stdItem.NeedIdentify == 1)
                            {
                                //M2Share.EventSource.AddEventLog(16, MapName + "\t" + CurrX + "\t" + CurrY + "\t" + ChrName + "\t" + stdItem.Name + "\t" + UseItems[i].MakeIndex + "\t" + HUtil32.BoolToIntStr(Race == ActorRace.Play) + "\t" + '0');
                            }
                            UseItems[i].Index = 0;
                        }
                    }
                }
                int nRate = PvpLevel() > 2 ? SystemShare.Config.DieRedDropUseItemRate : SystemShare.Config.DieDropUseItemRate;
                for (int i = 0; i < UseItems.Length; i++)
                {
                    if (M2Share.RandomNumber.Random(nRate) != 0)
                    {
                        continue;
                    }
                    if (UseItems[i] != null && M2Share.InDisableTakeOffList(UseItems[i].Index))
                    {
                        continue;
                    }
                    // 检查是否在禁止取下列表,如果在列表中则不掉此物品
                    int dropWide = HUtil32._MIN(SystemShare.Config.DropItemRage, 3);
                    if (DropItemDown(UseItems[i], dropWide, true, baseObject, ActorId))
                    {
                        stdItem = SystemShare.EquipmentSystem.GetStdItem(UseItems[i].Index);
                        if (stdItem != null)
                        {
                            if ((stdItem.ItemDesc & 10) == 0)
                            {
                                if (Race == ActorRace.Play)
                                {
                                    dropItemList.Add(new DeleteItem()
                                    {
                                        ItemName = SystemShare.EquipmentSystem.GetStdItemName(UseItems[i].Index),
                                        MakeIndex = UseItems[i].MakeIndex
                                    });
                                }
                                UseItems[i].Index = 0;
                            }
                        }
                    }
                }
                if (dropItemList.Count > 0)
                {
                    int objectId = HUtil32.Sequence();
                    SystemShare.ActorMgr.AddOhter(objectId, dropItemList);
                    SendMsg(Messages.RM_SENDDELITEMLIST, 0, objectId, 0, 0);
                }
            }
            catch (Exception ex)
            {
                LogService.Error(sExceptionMsg);
                LogService.Error(ex.StackTrace);
            }
        }

        /// <summary>
        /// 蜡烛勋章减少持久
        /// </summary>
        private void UseLamp()
        {
            const string sExceptionMsg = "[Exception] PlayObject::UseLamp";
            try
            {
                if (UseItems[ItemLocation.RighThand] != null && UseItems[ItemLocation.RighThand].Index > 0)
                {
                    StdItem stdItem = SystemShare.EquipmentSystem.GetStdItem(UseItems[ItemLocation.RighThand].Index);
                    if ((stdItem == null) || (stdItem.SpecialPwr != 0))
                    {
                        return;
                    }
                    int nOldDura = HUtil32.Round((ushort)(UseItems[ItemLocation.RighThand].Dura / 1000.0));
                    ushort nDura;
                    if (SystemShare.Config.DecLampDura)
                    {
                        nDura = (ushort)(UseItems[ItemLocation.RighThand].Dura - 1);
                    }
                    else
                    {
                        nDura = UseItems[ItemLocation.RighThand].Dura;
                    }
                    if (nDura <= 0)
                    {
                        UseItems[ItemLocation.RighThand].Dura = 0;
                        if (Race == ActorRace.Play)
                        {
                            SendDelItems(UseItems[ItemLocation.RighThand]);
                        }
                        UseItems[ItemLocation.RighThand].Index = 0;
                        Light = GetMyLight();
                        SendRefMsg(Messages.RM_CHANGELIGHT, 0, 0, 0, 0, "");
                        SendMsg(Messages.RM_LAMPCHANGEDURA, 0, 0, 0, 0);
                        RecalcAbilitys();
                    }
                    else
                    {
                        UseItems[ItemLocation.RighThand].Dura = nDura;
                    }
                    if (nOldDura != HUtil32.Round(nDura / 1000.0))
                    {
                        SendMsg(Messages.RM_LAMPCHANGEDURA, 0, UseItems[ItemLocation.RighThand].Dura, 0, 0);
                    }
                }
            }
            catch
            {
                LogService.Error(sExceptionMsg);
            }
        }

        private int GetDigUpMsgCount()
        {
            int result = 0;
            for (int i = 0; i < MsgQueue.Count; i++)
            {
                if (MsgQueue.TryPeek(out SendMessage sendMessage, out _))
                {
                    if (sendMessage.wIdent == Messages.CM_BUTCH)
                    {
                        result++;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 攻击消息数量
        /// </summary>
        /// <returns></returns>
        private int GetHitMsgCount()
        {
            int result = 0;
            for (int i = 0; i < MsgQueue.Count; i++)
            {
                if (MsgQueue.TryPeek(out SendMessage sendMessage, out _))
                {
                    if (sendMessage.wIdent >= Messages.CM_HIT || sendMessage.wIdent <= Messages.CM_FIREHIT)
                    {
                        result++;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 魔法消息数量
        /// </summary>
        /// <returns></returns>
        private int GetSpellMsgCount()
        {
            int result = 0;
            for (int i = 0; i < MsgQueue.Count; i++)
            {
                if (MsgQueue.TryPeek(out SendMessage sendMessage, out _))
                {
                    if (sendMessage.wIdent == Messages.CM_SPELL)
                    {
                        result++;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 跑步消息数量
        /// </summary>
        /// <returns></returns>
        private int GetRunMsgCount()
        {
            int result = 0;
            for (int i = 0; i < MsgQueue.Count; i++)
            {
                if (MsgQueue.TryPeek(out SendMessage sendMessage, out _))
                {
                    if (sendMessage.wIdent == Messages.CM_RUN)
                    {
                        result++;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 走路消息数量
        /// </summary>
        /// <returns></returns>
        private int GetWalkMsgCount()
        {
            int result = 0;
            for (int i = 0; i < MsgQueue.Count; i++)
            {
                if (MsgQueue.TryPeek(out SendMessage sendMessage, out _))
                {
                    if (sendMessage.wIdent == Messages.CM_WALK)
                    {
                        result++;
                    }
                }
            }
            return result;
        }

        private int GetTurnMsgCount()
        {
            int result = 0;
            for (int i = 0; i < MsgQueue.Count; i++)
            {
                if (MsgQueue.TryPeek(out SendMessage sendMessage, out _))
                {
                    if (sendMessage.wIdent == Messages.CM_TURN)
                    {
                        result++;
                    }
                }
            }
            return result;
        }

        private int GetSiteDownMsgCount()
        {
            int result = 0;
            for (int i = 0; i < MsgQueue.Count; i++)
            {
                if (MsgQueue.TryPeek(out SendMessage sendMessage, out _))
                {
                    if (sendMessage.wIdent == Messages.CM_SITDOWN)
                    {
                        result++;
                    }
                }
            }
            return result;
        }
    }
}