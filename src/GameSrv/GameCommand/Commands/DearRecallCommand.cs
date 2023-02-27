﻿using GameSrv.Player;
using SystemModule.Enums;

namespace GameSrv.GameCommand.Commands {
    /// <summary>
    /// 夫妻传送，将对方传送到自己身边，对方必须允许传送。
    /// </summary>
    [Command("DearRecall", "夫妻传送", "(夫妻传送，将对方传送到自己身边，对方必须允许传送。)", 0)]
    public class DearRecallCommond : GameCommand {
        [ExecuteCommand]
        public void Execute(PlayObject PlayObject) {
            if (string.IsNullOrEmpty(PlayObject.DearName)) {
                PlayObject.SysMsg("你没有结婚!!!", MsgColor.Red, MsgType.Hint);
                return;
            }
            if (PlayObject.Envir.Flag.boNODEARRECALL) {
                PlayObject.SysMsg("本地图禁止夫妻传送!!!", MsgColor.Red, MsgType.Hint);
                return;
            }
            if (PlayObject.DearHuman == null) {
                if (PlayObject.Gender == 0) {
                    PlayObject.SysMsg("你的老婆不在线!!!", MsgColor.Red, MsgType.Hint);
                }
                else {
                    PlayObject.SysMsg("你的老公不在线!!!", MsgColor.Red, MsgType.Hint);
                }
                return;
            }
            if (HUtil32.GetTickCount() - PlayObject.DearRecallTick < 10000) {
                PlayObject.SysMsg("稍等会才能再次使用此功能!!!", MsgColor.Red, MsgType.Hint);
                return;
            }
            PlayObject.DearRecallTick = HUtil32.GetTickCount();
            if (PlayObject.DearHuman.CanDearRecall) {
                PlayObject.RecallHuman(PlayObject.DearHuman.ChrName);
            }
            else {
                PlayObject.SysMsg(PlayObject.DearHuman.ChrName + " 不允许传送!!!", MsgColor.Red, MsgType.Hint);
            }
        }
    }
}