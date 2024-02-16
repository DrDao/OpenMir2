using M2Server;
using OpenMir2;
using OpenMir2.Consts;
using OpenMir2.Data;
using OpenMir2.Enums;
using OpenMir2.Packets.ClientPackets;
using SystemModule;
using SystemModule.Actors;
using SystemModule.Const;
using SystemModule.Data;
using SystemModule.Enums;
using SystemModule.MagicEvent;

namespace RobotSystem.Services
{
    public partial class RobotPlayer
    {
        public override void Run()
        {
            bool boFind = false;
            try
            {
                if (!Ghost && !Death && !FixedHideMode && !StoneMode && StatusTimeArr[PoisonState.STONE] == 0)
                {
                    if (HUtil32.GetTickCount() - WalkTick > WalkSpeed)
                    {
                        WalkTick = HUtil32.GetTickCount();
                        if (TargetCret != null)
                        {
                            if (TargetCret.Death || TargetCret.Ghost || TargetCret.InSafeZone() || TargetCret.Envir != Envir || Math.Abs(CurrX - TargetCret.CurrX) > 11 || Math.Abs(CurrY - TargetCret.CurrY) > 11)
                            {
                                DelTargetCreat();
                            }
                        }
                        if (!RobotStart)
                        {
                            DelTargetCreat();
                        }
                        SearchTarget();
                        if (ManagedEnvir != Envir) // 所在地图不是挂机地图则清空目标
                        {
                            DelTargetCreat();
                        }
                        if (Thinking())
                        {
                            base.Run();
                            return;
                        }
                        if (ProtectStatus) // 守护状态
                        {
                            if (ProtectTargetX == 0 || ProtectTargetY == 0)// 取守护坐标
                            {
                                ProtectTargetX = CurrX;// 守护坐标
                                ProtectTargetY = CurrY;// 守护坐标
                            }
                            if (!ProtectDest && ManagedEnvir != null && TargetCret == null)
                            {
                                GotoProtect();
                                GotoProtectXyCount++;
                                if (Math.Abs(CurrX - ProtectTargetX) <= 3 && Math.Abs(CurrY - ProtectTargetY) <= 3)
                                {
                                    Dir = (byte)SystemShare.RandomNumber.Random(8);
                                    ProtectDest = true;
                                    GotoProtectXyCount = 0;// 是向守护坐标的累计数
                                }
                                if (GotoProtectXyCount > 20 && !ProtectDest)// 20次还没有走到守护坐标，则飞回坐标上
                                {
                                    if (Math.Abs(CurrX - ProtectTargetX) > 13 || Math.Abs(CurrY - ProtectTargetY) > 13)
                                    {
                                        SpaceMove(ManagedEnvir.MapName, ProtectTargetX, ProtectTargetY, 1);
                                        Dir = (byte)SystemShare.RandomNumber.Random(8);
                                        ProtectDest = true;
                                        GotoProtectXyCount = 0;// 是向守护坐标的累计数
                                    }
                                }
                                base.Run();
                                return;
                            }
                        }
                        if (TargetCret != null)
                        {
                            if (AttackTarget())// 攻击
                            {
                                base.Run();
                                return;
                            }
                            else if (Avoid()) // 自动躲避
                            {
                                ActionTick = HUtil32.GetTickCount() - 10;
                                AutoAvoid();
                                base.Run();
                                return;
                            }
                            else
                            {
                                if (IsNeedGotoXy())// 是否走向目标
                                {
                                    ActionTick = HUtil32.GetTickCount();
                                    TargetX = TargetCret.CurrX;
                                    TargetY = TargetCret.CurrY;
                                    if (AllowUseMagic(MagicConst.SKILL_ERGUM) && Job == 0)
                                    {
                                        GetGotoXy(TargetCret, 2);
                                    }
                                    if (Job > 0)
                                    {
                                        if (AttackLevelTarget() || TaoLevelHitAttack() && TargetCret.WAbil.MaxHP < 700 && Job == PlayerJob.Taoist)// 道法22前是否物理攻击
                                        {
                                            if (Master != null)
                                            {
                                                if (Math.Abs(Master.CurrX - CurrX) > 6 || Math.Abs(Master.CurrY - CurrY) > 6)
                                                {
                                                    base.Run();
                                                    return;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            GetGotoXy(TargetCret, 3); // 道法只走向目标3格范围
                                        }
                                    }
                                    GotoTargetXy(TargetX, TargetY, 0);
                                    base.Run();
                                    return;
                                }
                            }
                        }

                        if (IsRobot && !Ghost && !Death)
                        {
                            if (SystemShare.Config.boHPAutoMoveMap)
                            {
                                if (WAbil.HP <= Math.Round(WAbil.MaxHP * 0.3) && HUtil32.GetTickCount() - HpToMapHomeTick > 15000) // 低血时回城或回守护点 
                                {
                                    HpToMapHomeTick = HUtil32.GetTickCount();
                                    DelTargetCreat();
                                    if (ProtectStatus) // 守护状态
                                    {
                                        SpaceMove(ManagedEnvir.MapName, ProtectTargetX, ProtectTargetY, 1);// 地图移动
                                        Dir = SystemShare.RandomNumber.RandomByte(8);
                                        ProtectDest = true;
                                        GotoProtectXyCount = 0; // 是向守护坐标的累计数 
                                    }
                                    else
                                    {
                                        MoveToHome(); // 不是守护状态，直接回城
                                    }
                                }
                            }
                            if (SystemShare.Config.boAutoRepairItem)
                            {
                                if (HUtil32.GetTickCount() - AutoRepairItemTick > 15000)
                                {
                                    AutoRepairItemTick = HUtil32.GetTickCount();
                                    bool boRecalcAbilitys = false;
                                    StdItem stdItem;
                                    UserItem userItem;
                                    for (int nWhere = 0; nWhere < UseItemNames.Length; nWhere++)
                                    {
                                        if (string.IsNullOrEmpty(UseItemNames[nWhere]))
                                        {
                                            continue;
                                        }
                                        if (UseItems[nWhere].Index <= 0)
                                        {
                                            stdItem = SystemShare.EquipmentSystem.GetStdItem(UseItemNames[nWhere]);
                                            if (stdItem != null)
                                            {
                                                userItem = new UserItem();
                                                if (SystemShare.EquipmentSystem.CopyToUserItemFromName(UseItemNames[nWhere], ref userItem))
                                                {
                                                    boRecalcAbilitys = true;
                                                    if (SystemShare.StdModeMap.Contains(stdItem.StdMode))
                                                    {
                                                        if (stdItem.Shape == 130 || stdItem.Shape == 131 || stdItem.Shape == 132)
                                                        {
                                                            //SystemShare.WorldEngine.GetUnknowItemValue(UserItem);
                                                        }
                                                    }
                                                }
                                                UseItems[nWhere] = userItem;
                                                Dispose(userItem);
                                            }
                                        }
                                    }
                                    if (BagItemNames.Count > 0)
                                    {
                                        for (int i = 0; i < BagItemNames.Count; i++)
                                        {
                                            for (int j = 0; j < ItemList.Count; j++)
                                            {
                                                userItem = ItemList[j];
                                                if (userItem != null)
                                                {
                                                    stdItem = SystemShare.EquipmentSystem.GetStdItem(userItem.Index);
                                                    if (stdItem != null)
                                                    {
                                                        boFind = false;
                                                        if (string.Compare(stdItem.Name, BagItemNames[i], StringComparison.OrdinalIgnoreCase) == 0)
                                                        {
                                                            boFind = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                            if (!boFind)
                                            {
                                                userItem = new UserItem();
                                                if (SystemShare.EquipmentSystem.CopyToUserItemFromName(BagItemNames[i], ref userItem))
                                                {
                                                    if (!AddItemToBag(userItem))
                                                    {
                                                        Dispose(userItem);
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    Dispose(userItem);
                                                }
                                            }
                                        }
                                    }
                                    for (int nWhere = 0; nWhere <= UseItems.Length - 1; nWhere++)
                                    {
                                        if (UseItems[nWhere] != null && UseItems[nWhere].Index > 0)
                                        {
                                            stdItem = SystemShare.EquipmentSystem.GetStdItem(UseItems[nWhere].Index);
                                            if (stdItem != null)
                                            {
                                                if (UseItems[nWhere].DuraMax > UseItems[nWhere].Dura && stdItem.StdMode != 43)
                                                {
                                                    /*if (PlugOfCheckCanItem(3, StdItem.Name, false, 0, 0))
                                                    {
                                                        continue;
                                                    }*/
                                                    UseItems[nWhere].Dura = UseItems[nWhere].DuraMax;
                                                }
                                            }
                                        }
                                    }
                                    if (boRecalcAbilitys)
                                    {
                                        RecalcAbilitys();
                                    }
                                }
                            }
                            if (SystemShare.Config.boRenewHealth) // 自动增加HP MP
                            {
                                if (HUtil32.GetTickCount() - AutoAddHealthTick > 5000)
                                {
                                    AutoAddHealthTick = HUtil32.GetTickCount();
                                    int nPercent = WAbil.HP * 100 / WAbil.MaxHP;
                                    int nValue = WAbil.MaxHP / 10;
                                    if (nPercent < SystemShare.Config.nRenewPercent)
                                    {
                                        if (WAbil.HP + nValue >= WAbil.MaxHP)
                                        {
                                            WAbil.HP = WAbil.MaxHP;
                                        }
                                        else
                                        {
                                            WAbil.HP += (ushort)nValue;
                                        }
                                    }
                                    nValue = WAbil.MaxMP / 10;
                                    nPercent = WAbil.MP * 100 / WAbil.MaxMP;
                                    if (nPercent < SystemShare.Config.nRenewPercent)
                                    {
                                        if (WAbil.MP + nValue >= WAbil.MaxMP)
                                        {
                                            WAbil.MP = WAbil.MaxMP;
                                        }
                                        else
                                        {
                                            WAbil.MP += (ushort)nValue;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!Ghost && !Death && !FixedHideMode && !StoneMode && StatusTimeArr[PoisonState.STONE] == 0)
                    {
                        if (ProtectStatus && TargetCret == null)// 守护状态
                        {
                            if (Math.Abs(CurrX - ProtectTargetX) > 50 || Math.Abs(CurrY - ProtectTargetY) > 50)
                            {
                                ProtectDest = false;
                            }
                        }
                        if (TargetCret == null)
                        {
                            if (Master != null)
                            {
                                FollowMaster();
                            }
                            else
                            {
                                Wondering();
                            }
                        }
                    }
                    PlaySuperRock();
                }
            }
            catch (Exception ex)
            {
                LogService.Error(ex.StackTrace);
            }
            base.Run();
        }

        public override bool IsProperTarget(IActor baseObject)
        {
            bool result = false;
            if (baseObject != null)
            {
                if (base.IsProperTarget(baseObject))
                {
                    result = true;
                    if (baseObject.Master != null)
                    {
                        if (baseObject.Master == this || baseObject.Master.IsRobot && !InGuildWarArea)
                        {
                            result = false;
                        }
                    }
                    if (baseObject.IsRobot && !InGuildWarArea)// 假人不攻击假人,行会战除外
                    {
                        result = false;
                    }
                    switch (baseObject.Race)
                    {
                        case ActorRace.ArcherGuard:
                        case ActorRace.Exercise:// 不主动攻击练功师 弓箭手
                            if (baseObject.TargetCret != this)
                            {
                                result = false;
                            }
                            break;
                        case ActorRace.NPC:
                        case ActorRace.Guard:
                        case 12: // 不攻击大刀卫士
                            result = false;
                            break;
                        case ActorRace.SabukDoor:
                        case ActorRace.SabukWall:
                        case 158: // 沙巴克城门,沙巴克左城墙,宠物类
                            result = false;
                            break;
                    }
                }
                else
                {
                    if (AttatckMode == AttackMode.HAM_PKATTACK)// 红名模式，除红名目标外，受人攻击时才还击
                    {
                        if (baseObject.Race == ActorRace.Play)
                        {
                            byte targetPvpLevel = ((IPlayerActor)baseObject).PvpLevel();
                            if (PvpLevel() >= 2)
                            {
                                result = targetPvpLevel < 2;
                            }
                            else
                            {
                                result = targetPvpLevel >= 2;
                            }
                        }
                        if (IsRobot && !result)
                        {
                            if (baseObject.Race == ActorRace.Play || baseObject.Master != null)
                            {
                                if (baseObject.TargetCret != null)
                                {
                                    if (baseObject.TargetCret == this)
                                    {
                                        result = true;
                                    }
                                }
                                if (baseObject.LastHiter != null)
                                {
                                    if (baseObject.LastHiter == this)
                                    {
                                        result = true;
                                    }
                                }
                                if (baseObject.ExpHitter != null)
                                {
                                    if (baseObject.LastHiter == this)
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }
                        if (baseObject.Race == ActorRace.Play || baseObject.Master != null)// 安全区不能打人物和英雄
                        {
                            if (baseObject.InSafeZone() || InSafeZone())
                            {
                                result = false;
                            }
                        }
                        if (baseObject.Master == this)
                        {
                            result = false;
                        }
                        if (baseObject.IsRobot && (!InGuildWarArea || ((IPlayerActor)baseObject).PvpLevel() < 2)) // 假人不攻击假人,行会战除外
                        {
                            result = false;
                        }
                        switch (baseObject.Race)
                        {
                            case ActorRace.ArcherGuard:
                            case ActorRace.Exercise:// 不主动攻击练功师 弓箭手
                                if (baseObject.TargetCret != this)
                                {
                                    result = false;
                                }
                                break;
                            case ActorRace.NPC:
                            case ActorRace.Guard:
                            case 12:// 不攻击大刀卫士
                                result = false;
                                break;
                            case ActorRace.SabukDoor:
                            case ActorRace.SabukWall:
                            case 158:// 沙巴克城门,沙巴克左城墙,宠物类
                                result = false;
                                break;
                        }
                    }
                }
            }
            return result;
        }

        public override bool IsProperFriend(IActor baseObject)
        {
            return base.IsProperFriend(baseObject);
        }

        public override void SearchViewRange()
        {
            IActor baseObject;
            const string sExceptionMsg = "RoboPlayObject::SearchViewRange 1-{0} {1} {2} {3} {4}";
            if (Ghost)
            {
                return;
            }
            if (VisibleItems.Count > 0)
            {
                for (int i = 0; i < VisibleItems.Count; i++)
                {
                    VisibleItems[i].VisibleFlag = 0;
                }
            }
            MapEvent mapEvent;
            try
            {
                short nStartX = (short)(CurrX - ViewRange);
                short nEndX = (short)(CurrX + ViewRange);
                short nStartY = (short)(CurrY - ViewRange);
                short nEndY = (short)(CurrY + ViewRange);
                int dwRunTick = HUtil32.GetTickCount();
                for (short nX = nStartX; nX <= nEndX; nX++)
                {
                    for (short nY = nStartY; nY <= nEndY; nY++)
                    {
                        ref MapCellInfo cellInfo = ref Envir.GetCellInfo(nX, nY, out bool cellSuccess);
                        if (cellSuccess && cellInfo.IsAvailable)
                        {
                            if (cellInfo.ObjList == null)
                            {
                                continue;
                            }
                            int nIdx = 0;
                            while (true)
                            {
                                if (cellInfo.Count <= nIdx)
                                {
                                    break;
                                }
                                CellObject cellObject = cellInfo.ObjList[nIdx];
                                if (HUtil32.GetTickCount() - dwRunTick > 500)
                                {
                                    break;
                                }
                                if (cellInfo.IsAvailable && cellInfo.Count <= 0)
                                {
                                    cellInfo.Clear();
                                    break;
                                }
                                if (cellInfo.ObjList == null)
                                {
                                    break;
                                }
                                if (cellObject.CellObjId > 0)
                                {
                                    switch (cellObject.CellType)
                                    {
                                        case CellType.Play:
                                        case CellType.Monster:
                                            if (HUtil32.GetTickCount() - cellObject.AddTime >= 60000)
                                            {
                                                cellInfo.Remove(cellObject);
                                                if (cellInfo.Count <= 0)
                                                {
                                                    cellInfo.Clear();
                                                    break;
                                                }
                                                continue;
                                            }
                                            baseObject = SystemShare.ActorMgr.Get(cellObject.CellObjId);
                                            if (baseObject != null)
                                            {
                                                if (!baseObject.Ghost && !baseObject.FixedHideMode && !baseObject.ObMode)
                                                {
                                                    if (Race < ActorRace.Animal || Master != null || WantRefMsg || baseObject.Master != null && Math.Abs(baseObject.CurrX - CurrX) <= 3 && Math.Abs(baseObject.CurrY - CurrY) <= 3 || baseObject.Race == ActorRace.Play)
                                                    {
                                                        UpdateVisibleGay(baseObject);
                                                    }
                                                }
                                            }
                                            break;
                                        case CellType.Item:
                                            if (Race == ActorRace.Play)
                                            {
                                                if (HUtil32.GetTickCount() - cellObject.AddTime > SystemShare.Config.ClearDropOnFloorItemTime)
                                                {
                                                    if (cellObject.CellObjId > 0)
                                                    {
                                                        M2Share.CellObjectMgr.Remove(cellObject.CellObjId);
                                                    }
                                                    cellInfo.Remove(cellObject);
                                                    if (cellInfo.Count <= 0)
                                                    {
                                                        cellInfo.Clear();
                                                        break;
                                                    }
                                                    continue;
                                                }
                                                MapItem mapItem = M2Share.CellObjectMgr.Get<MapItem>(cellObject.CellObjId);
                                                if (mapItem.ItemId == 0)
                                                {
                                                    continue;
                                                }
                                                UpdateVisibleItem(nX, nY, mapItem);
                                                if (mapItem.OfBaseObject != 0 || mapItem.DropBaseObject != 0)
                                                {
                                                    if (HUtil32.GetTickCount() - mapItem.CanPickUpTick > SystemShare.Config.FloorItemCanPickUpTime)
                                                    {
                                                        mapItem.OfBaseObject = 0;
                                                        mapItem.DropBaseObject = 0;
                                                    }
                                                    else
                                                    {
                                                        if (mapItem.OfBaseObject > 0)
                                                        {
                                                            if (SystemShare.ActorMgr.Get(mapItem.OfBaseObject).Ghost)
                                                            {
                                                                mapItem.OfBaseObject = 0;
                                                            }
                                                        }
                                                        if (mapItem.DropBaseObject > 0)
                                                        {
                                                            if (SystemShare.ActorMgr.Get(mapItem.DropBaseObject).Ghost)
                                                            {
                                                                mapItem.DropBaseObject = 0;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            break;
                                        case CellType.Event:
                                            if (Race == ActorRace.Play)
                                            {
                                                if (cellObject.CellObjId < 0)
                                                {
                                                    mapEvent = M2Share.CellObjectMgr.Get<MapEvent>(cellObject.CellObjId);
                                                    UpdateVisibleEvent(nX, nY, mapEvent);
                                                }
                                            }
                                            break;
                                    }
                                }
                                nIdx++;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                LogService.Error(Format(sExceptionMsg, ChrName, MapName, CurrX, CurrY));
                KickException();
            }
            VisibleFlag nVisibleFlag;
            try
            {
                int n18 = 0;
                while (true)
                {
                    try
                    {
                        if (VisibleActors.Count <= n18)
                        {
                            break;
                        }
                        VisibleBaseObject visibleBaseObject;
                        try
                        {
                            visibleBaseObject = VisibleActors[n18];
                            nVisibleFlag = visibleBaseObject.VisibleFlag;
                        }
                        catch
                        {
                            VisibleActors.RemoveAt(n18);
                            if (VisibleActors.Count > 0)
                            {
                                continue;
                            }
                            break;
                        }
                        switch (visibleBaseObject.VisibleFlag)
                        {
                            case VisibleFlag.Hidden:
                                if (Race == ActorRace.Play)
                                {
                                    baseObject = visibleBaseObject.BaseObject;
                                    if (baseObject != null)
                                    {
                                        if (!baseObject.FixedHideMode && !baseObject.Ghost)
                                        {
                                            SendMsg(baseObject, Messages.RM_DISAPPEAR, 0, 0, 0, 0);
                                        }
                                    }
                                }
                                VisibleActors.RemoveAt(n18);
                                if (visibleBaseObject != null)
                                {
                                    Dispose(visibleBaseObject);
                                }
                                continue;
                            case VisibleFlag.Show:
                                if (Race == ActorRace.Play)
                                {
                                    baseObject = visibleBaseObject.BaseObject;
                                    if (baseObject != null)
                                    {
                                        if (baseObject != this && !baseObject.Ghost && !Ghost)
                                        {
                                            if (baseObject.Death)
                                            {
                                                if (baseObject.Skeleton)
                                                {
                                                    SendMsg(baseObject, Messages.RM_SKELETON, baseObject.Dir, baseObject.CurrX, baseObject.CurrY, 0);
                                                }
                                                else
                                                {
                                                    SendMsg(baseObject, Messages.RM_DEATH, baseObject.Dir, baseObject.CurrX, baseObject.CurrY, 0);
                                                }
                                            }
                                            else
                                            {
                                                if (baseObject != null)
                                                {
                                                    SendMsg(baseObject, Messages.RM_TURN, baseObject.Dir, baseObject.CurrX, baseObject.CurrY, 0, baseObject.GetShowName());
                                                }
                                            }
                                        }
                                    }
                                }
                                visibleBaseObject.VisibleFlag = VisibleFlag.Hidden;
                                break;
                            case VisibleFlag.Invisible:
                                visibleBaseObject.VisibleFlag = VisibleFlag.Hidden;
                                break;
                        }
                    }
                    catch
                    {
                        break;
                    }
                    n18++;
                }
            }
            catch (Exception)
            {
                LogService.Error(Format(sExceptionMsg, ChrName, MapName, CurrX, CurrY));
                KickException();
            }
            try
            {
                int position = 0;
                while (true)
                {
                    try
                    {
                        if (VisibleItems.Count <= position)
                        {
                            break;
                        }
                        VisibleMapItem visibleMapItem;
                        try
                        {
                            visibleMapItem = VisibleItems[position];
                        }
                        catch
                        {
                            VisibleItems.RemoveAt(position);
                            if (VisibleItems.Count > 0)
                            {
                                continue;
                            }
                            break;
                        }
                        if (visibleMapItem.VisibleFlag == 0)
                        {
                            VisibleItems.RemoveAt(position);
                            visibleMapItem = null;
                            if (VisibleItems.Count > 0)
                            {
                                continue;
                            }
                            break;
                        }
                    }
                    catch
                    {
                        break;
                    }
                    position++;
                }
                position = 0;
                while (true)
                {
                    try
                    {
                        if (VisibleEvents.Count <= position)
                        {
                            break;
                        }
                        try
                        {
                            mapEvent = VisibleEvents[position];
                        }
                        catch
                        {
                            VisibleEvents.RemoveAt(position);
                            if (VisibleEvents.Count > 0)
                            {
                                continue;
                            }
                            break;
                        }
                        if (mapEvent != null)
                        {
                            switch (mapEvent.VisibleFlag)
                            {
                                case VisibleFlag.Hidden:
                                    SendMsg(Messages.RM_HIDEEVENT, 0, mapEvent.Id, mapEvent.nX, mapEvent.nY);
                                    VisibleEvents.RemoveAt(position);
                                    if (VisibleEvents.Count > 0)
                                    {
                                        continue;
                                    }
                                    break;
                                case VisibleFlag.Invisible:
                                    mapEvent.VisibleFlag = 0;
                                    break;
                                case VisibleFlag.Show:
                                    SendMsg(Messages.RM_SHOWEVENT, mapEvent.EventType, mapEvent.Id, HUtil32.MakeLong(mapEvent.nX, (short)mapEvent.EventParam), mapEvent.nY);
                                    mapEvent.VisibleFlag = 0;
                                    break;
                            }
                        }
                    }
                    catch
                    {
                        break;
                    }
                    position++;
                }
            }
            catch
            {
                LogService.Error(ChrName + ',' + MapName + ',' + CurrX + ',' + CurrY + ',' + " SearchViewRange");
                KickException();
            }
        }

        public override void Struck(IActor hiter)
        {
            StruckTick = HUtil32.GetTickCount();
            if (hiter != null)
            {
                if (TargetCret == null && IsProperTarget(hiter))
                {
                    SetTargetCreat(hiter);
                }
                else
                {
                    if (hiter.Race == ActorRace.Play || hiter.Master != null && hiter.GetMaster().Race == ActorRace.Play)
                    {
                        if (TargetCret != null && (TargetCret.Race == ActorRace.Play || TargetCret.Master != null && TargetCret.GetMaster().Race == ActorRace.Play))
                        {
                            if (StruckMinXY(TargetCret, hiter) == hiter || SystemShare.RandomNumber.Random(6) == 0)
                            {
                                SetTargetCreat(hiter);
                            }
                        }
                        else
                        {
                            SetTargetCreat(hiter);
                        }
                    }
                    else
                    {
                        if (TargetCret != null && StruckMinXY(TargetCret, hiter) == hiter || SystemShare.RandomNumber.Random(6) == 0)
                        {
                            if (Job > 0 || TargetCret != null && (HUtil32.GetTickCount() - TargetFocusTick) > 1000 * 3)
                            {
                                if (IsProperTarget(hiter))
                                {
                                    SetTargetCreat(hiter);
                                }
                            }
                        }
                    }
                }
                if (hiter.Race == ActorRace.Play && !hiter.IsRobot && TargetCret == hiter)
                {
                    if (SystemShare.RandomNumber.Random(8) == 0 && AiSayMsgList.Count > 0)
                    {
                        if (HUtil32.GetTickCount() >= DisableSayMsgTick)
                        {
                            DisableSayMsg = false;
                        }
                        bool boDisableSayMsg = DisableSayMsg;
                        //g_DenySayMsgList.Lock;
                        //if (g_DenySayMsgList.GetIndex(m_sChrName) >= 0)
                        //{
                        //    boDisableSayMsg = true;
                        //}
                        //g_DenySayMsgList.UnLock;
                        if (!boDisableSayMsg)
                        {
                            SendRefMsg(Messages.RM_HEAR, 0, SystemShare.Config.btHearMsgFColor, SystemShare.Config.btHearMsgBColor, 0, ChrName + ':' + AiSayMsgList[SystemShare.RandomNumber.Random(AiSayMsgList.Count)]);
                        }
                    }
                }
            }
            if (Animal)
            {
                MeatQuality = (ushort)(MeatQuality - SystemShare.RandomNumber.Random(300));
                if (MeatQuality < 0)
                {
                    MeatQuality = 0;
                }
            }
            AttackTick = (ushort)(AttackTick + (150 - HUtil32._MIN(130, Abil.Level * 4)));
        }

        protected override void SearchTarget()
        {
            if ((TargetCret == null || HUtil32.GetTickCount() - SearchTargetTick > 1000) && RobotStart)
            {
                SearchTargetTick = HUtil32.GetTickCount();
                if (TargetCret == null || !(TargetCret != null && TargetCret.Race == ActorRace.Play) || TargetCret.Master != null && TargetCret.Master.Race == ActorRace.Play || (HUtil32.GetTickCount() - StruckTick) > 15000)
                {
                    base.SearchTarget();
                }
            }
        }

        public override void Die()
        {
            if (Resurrection) //死亡后回安全区复活
            {
                MoveToHome();
                ReAlive();
            }
            else
            {
                if (RobotStart)
                {
                    RobotStart = false;
                }
            }
            base.Die();
        }

        public override void DropUseItems(int baseObject)
        {
            const byte maxUseItem = 8;
            if (NoDropUseItem)
            {
                return;
            }
            IList<DeleteItem> dropItemList = new List<DeleteItem>();
            int nRate = PvpLevel() > 2 ? 15 : 30; //PVP红名掉落几率
            int nC = 0;
            while (true)
            {
                if (SystemShare.RandomNumber.Random(nRate) == 0)
                {
                    if (UseItems[nC] == null)
                    {
                        nC++;
                        continue;
                    }
                    int dropWide = HUtil32._MIN(SystemShare.Config.DropItemRage, 3);
                    if (DropItemDown(UseItems[nC], dropWide, true, baseObject, this.ActorId))
                    {
                        StdItem stdItem = SystemShare.EquipmentSystem.GetStdItem(UseItems[nC].Index);
                        if (stdItem != null)
                        {
                            if ((stdItem.ItemDesc & 10) == 0)
                            {
                                if (Race == ActorRace.Play)
                                {
                                    dropItemList.Add(new DeleteItem()
                                    {
                                        ItemName = SystemShare.EquipmentSystem.GetStdItemName(UseItems[nC].Index),
                                        MakeIndex = UseItems[nC].MakeIndex
                                    });
                                }
                                UseItems[nC].Index = 0;
                            }
                        }
                    }
                }
                nC++;
                if (nC >= maxUseItem)
                {
                    break;
                }
            }
            if (dropItemList.Count > 0)
            {
                int objectId = HUtil32.Sequence();
                SystemShare.ActorMgr.AddOhter(objectId, dropItemList);
                SendMsg(Messages.RM_SENDDELITEMLIST, 0, objectId, 0, 0);
            }
        }
    }
}