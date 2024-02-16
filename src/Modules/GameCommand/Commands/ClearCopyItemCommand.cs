﻿using OpenMir2.Packets.ClientPackets;
using SystemModule;
using SystemModule.Actors;
using SystemModule.Enums;

namespace CommandModule.Commands
{
    /// <summary>
    /// 清除游戏中指定玩家复制物品
    /// </summary>
    [Command("ClearCopyItem", "清除游戏中指定玩家复制物品", "人物名称", 10)]
    public class ClearCopyItemCommand : GameCommand
    {
        [ExecuteCommand]
        public void Execute(string[] @params, IPlayerActor PlayerActor)
        {
            if (@params == null)
            {
                return;
            }
            string sHumanName = @params.Length > 0 ? @params[0] : "";
            UserItem userItem;
            UserItem userItem1;
            string s14;
            if (string.IsNullOrEmpty(sHumanName))
            {
                PlayerActor.SysMsg(Command.CommandHelp, MsgColor.Red, MsgType.Hint);
                return;
            }
            IPlayerActor targerObject = SystemShare.WorldEngine.GetPlayObject(sHumanName);
            if (targerObject == null)
            {
                PlayerActor.SysMsg(string.Format(CommandHelp.NowNotOnLineOrOnOtherServer, sHumanName), MsgColor.Red, MsgType.Hint);
                return;
            }
            for (int i = targerObject.ItemList.Count - 1; i >= 0; i--)
            {
                if (targerObject.ItemList.Count <= 0)
                {
                    break;
                }

                userItem = targerObject.ItemList[i];
                s14 = SystemShare.EquipmentSystem.GetStdItemName(userItem.Index);
                for (int j = i - 1; j >= 0; j--)
                {
                    userItem1 = targerObject.ItemList[j];
                    if (SystemShare.EquipmentSystem.GetStdItemName(userItem1.Index) == s14 && userItem.MakeIndex == userItem1.MakeIndex)
                    {
                        PlayerActor.ItemList.RemoveAt(j);
                        break;
                    }
                }
            }

            for (int i = targerObject.StorageItemList.Count - 1; i >= 0; i--)
            {
                if (targerObject.StorageItemList.Count <= 0)
                {
                    break;
                }
                userItem = targerObject.StorageItemList[i];
                s14 = SystemShare.EquipmentSystem.GetStdItemName(userItem.Index);
                for (int j = i - 1; j >= 0; j--)
                {
                    userItem1 = targerObject.StorageItemList[j];
                    if (SystemShare.EquipmentSystem.GetStdItemName(userItem1.Index) == s14 &&
                        userItem.MakeIndex == userItem1.MakeIndex)
                    {
                        PlayerActor.StorageItemList.RemoveAt(j);
                        break;
                    }
                }
            }
        }
    }
}