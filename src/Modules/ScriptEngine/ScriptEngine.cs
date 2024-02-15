using OpenMir2;
using OpenMir2.Common;
using OpenMir2.Data;
using OpenMir2.Enums;
using OpenMir2.Packets.ClientPackets;
using ScriptSystem.Consts;
using ScriptSystem.Processings;
using SystemModule;
using SystemModule.Actors;
using SystemModule.Const;

namespace ScriptSystem
{
    public struct GotoLabParams
    {
        public string ItemName { get; set; }
        public UserItem UserItem { get; set; }
        public bool SendSayMsg { get; set; }
    }

    /// <summary>
    /// 脚本执行引擎
    /// </summary>
    public class ScriptEngine : IScriptEngine
    {
        private ConditionProcessingSys ConditionScript { get; set; }
        private ExecutionProcessingSys ExecutionProcessing { get; set; }
        private GotoLabParams GotoLabParams;

        public ScriptEngine()
        {
            ConditionScript = new ConditionProcessingSys();
            ConditionScript.Initialize();
            ExecutionProcessing = new ExecutionProcessingSys();
            ExecutionProcessing.Initialize();
        }

        private void GotoLable(INormNpc normNpc, IPlayerActor playerActor, string sLabel, bool boExtJmp, string sMsg)
        {
            if (playerActor.LastNpc != normNpc.ActorId)
            {
                playerActor.LastNpc = 0;
            }
            ScriptInfo script = null;
            IList<ScriptInfo> scriptList = normNpc.ScriptList;
            if (string.Compare("@main", sLabel, StringComparison.OrdinalIgnoreCase) == 0)
            {
                for (int i = 0; i < scriptList.Count; i++)
                {
                    ScriptInfo script3C = scriptList[i];
                    if (script3C.RecordList.TryGetValue(sLabel, out _))
                    {
                        script = script3C;
                        playerActor.Script = script3C;
                        playerActor.LastNpc = normNpc.ActorId;
                        break;
                    }
                }
            }
            if (script == null)
            {
                if (playerActor.Script != null)
                {
                    for (int i = scriptList.Count - 1; i >= 0; i--)
                    {
                        if (scriptList[i] == playerActor.Script)
                        {
                            script = scriptList[i];
                        }
                    }
                }
                if (script == null)
                {
                    for (int i = scriptList.Count - 1; i >= 0; i--)
                    {
                        if (CheckGotoLableQuestStatus(playerActor, scriptList[i]))
                        {
                            script = scriptList[i];
                            playerActor.Script = script;
                            playerActor.LastNpc = normNpc.ActorId;
                        }
                    }
                }
            }
            if (script != null)
            {
                if (script.RecordList.TryGetValue(sLabel, out SayingRecord sayingRecord))
                {
                    if (boExtJmp && sayingRecord.boExtJmp == false)
                    {
                        return;
                    }
                    string sSendMsg = string.Empty;
                    GotoLabParams = default;
                    for (int i = 0; i < sayingRecord.ProcedureList.Count; i++)
                    {
                        SayingProcedure sayingProcedure = sayingRecord.ProcedureList[i];
                        if (GotoLableQuestCheckCondition(normNpc, playerActor, sayingProcedure.ConditionList, ref GotoLabParams))
                        {
                            sSendMsg = sSendMsg + sayingProcedure.sSayMsg;
                            if (!GotoLableQuestActionProcess(normNpc, playerActor, sayingProcedure.ActionList, ref GotoLabParams))
                            {
                                break;
                            }
                            if (GotoLabParams.SendSayMsg)
                            {
                                GotoLableSendMerChantSayMsg(normNpc, playerActor, sSendMsg, true);
                            }
                        }
                        else
                        {
                            sSendMsg = sSendMsg + sayingProcedure.sElseSayMsg;
                            if (!GotoLableQuestActionProcess(normNpc, playerActor, sayingProcedure.ElseActionList, ref GotoLabParams))
                            {
                                break;
                            }
                            if (GotoLabParams.SendSayMsg)
                            {
                                GotoLableSendMerChantSayMsg(normNpc, playerActor, sSendMsg, true);
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(sSendMsg))
                    {
                        GotoLableSendMerChantSayMsg(normNpc, playerActor, sSendMsg, false);
                    }
                }
            }
        }

        public void GotoLable(IPlayerActor playerActor, int actorId, string sLabel, bool boExtJmp = false)
        {
            INormNpc normNpc = SystemShare.ActorMgr.Get<INormNpc>(actorId);
            if (normNpc == null)
            {
                return;
            }
            GotoLable(normNpc, playerActor, sLabel, boExtJmp, string.Empty);
        }

        public void GotoLable(INormNpc normNpc, IPlayerActor playerActor, string sLabel, bool boExtJmp = false)
        {
            GotoLable(normNpc, playerActor, sLabel, boExtJmp, string.Empty);
        }

        private static bool CheckGotoLableQuestStatus(IPlayerActor playerActor, ScriptInfo scriptInfo)
        {
            bool result = true;
            if (!scriptInfo.IsQuest)
            {
                return true;
            }
            int nIndex = 0;
            while (true)
            {
                if ((scriptInfo.QuestInfo[nIndex].nRandRage > 0) && (SystemShare.RandomNumber.Random(scriptInfo.QuestInfo[nIndex].nRandRage) != 0))
                {
                    result = false;
                    break;
                }
                if (playerActor.GetQuestFlagStatus(scriptInfo.QuestInfo[nIndex].wFlag) != scriptInfo.QuestInfo[nIndex].btValue)
                {
                    result = false;
                    break;
                }
                nIndex++;
                if (nIndex >= 10)
                {
                    break;
                }
            }
            return result;
        }

        private static UserItem CheckGotoLableItemW(IPlayerActor playerActor, string sItemType, int nParam)
        {
            UserItem result = null;
            int nCount = 0;
            if (HUtil32.CompareLStr(sItemType, "[NECKLACE]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Necklace].Index > 0)
                {
                    result = playerActor.UseItems[ItemLocation.Necklace];
                }
                return result;
            }
            if (HUtil32.CompareLStr(sItemType, "[RING]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Ringl].Index > 0)
                {
                    result = playerActor.UseItems[ItemLocation.Ringl];
                }
                if (playerActor.UseItems[ItemLocation.Ringr].Index > 0)
                {
                    result = playerActor.UseItems[ItemLocation.Ringr];
                }
                return result;
            }
            if (HUtil32.CompareLStr(sItemType, "[ARMRING]", 4))
            {
                if (playerActor.UseItems[ItemLocation.ArmRingl].Index > 0)
                {
                    result = playerActor.UseItems[ItemLocation.ArmRingl];
                }
                if (playerActor.UseItems[ItemLocation.ArmRingr].Index > 0)
                {
                    result = playerActor.UseItems[ItemLocation.ArmRingr];
                }
                return result;
            }
            if (HUtil32.CompareLStr(sItemType, "[WEAPON]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Weapon].Index > 0)
                {
                    result = playerActor.UseItems[ItemLocation.Weapon];
                }
                return result;
            }
            if (HUtil32.CompareLStr(sItemType, "[HELMET]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Helmet].Index > 0)
                {
                    result = playerActor.UseItems[ItemLocation.Helmet];
                }
                return result;
            }
            if (HUtil32.CompareLStr(sItemType, "[BUJUK]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Bujuk].Index > 0)
                {
                    result = playerActor.UseItems[ItemLocation.Bujuk];
                }
                return result;
            }
            if (HUtil32.CompareLStr(sItemType, "[BELT]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Belt].Index > 0)
                {
                    result = playerActor.UseItems[ItemLocation.Belt];
                }
                return result;
            }
            if (HUtil32.CompareLStr(sItemType, "[BOOTS]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Boots].Index > 0)
                {
                    result = playerActor.UseItems[ItemLocation.Boots];
                }
                return result;
            }
            if (HUtil32.CompareLStr(sItemType, "[CHARM]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Charm].Index > 0)
                {
                    result = playerActor.UseItems[ItemLocation.Charm];
                }
                return result;
            }
            result = playerActor.CheckItemCount(sItemType, ref nCount);
            if (nCount < nParam)
            {
                result = null;
            }
            return result;
        }

        private static bool CheckGotoLableStringList(string sHumName, string sListFileName)
        {
            bool result = false;
            sListFileName = SystemShare.GetEnvirFilePath(sListFileName);
            if (File.Exists(sListFileName))
            {
                using StringList loadList = new StringList();
                try
                {
                    loadList.LoadFromFile(sListFileName);
                }
                catch
                {
                    LogService.Error("loading fail.... => " + sListFileName);
                }
                for (int i = 0; i < loadList.Count; i++)
                {
                    if (string.Compare(loadList[i].Trim(), sHumName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        result = true;
                        break;
                    }
                }
            }
            else
            {
                LogService.Error("file not found => " + sListFileName);
            }
            return result;
        }

        private static void GotoLableQuestCheckConditionSetVal(IPlayerActor playerActor, string sIndex, int nCount)
        {
            int n14 = SystemShare.GetValNameNo(sIndex);
            if (n14 >= 0)
            {
                if (HUtil32.RangeInDefined(n14, 0, 99))
                {
                    playerActor.MNVal[n14] = nCount;
                }
                else if (HUtil32.RangeInDefined(n14, 100, 119))
                {
                    SystemShare.Config.GlobalVal[n14 - 100] = nCount;
                }
                else if (HUtil32.RangeInDefined(n14, 200, 299))
                {
                    playerActor.MDyVal[n14 - 200] = nCount;
                }
                else if (HUtil32.RangeInDefined(n14, 300, 399))
                {
                    playerActor.MNMval[n14 - 300] = nCount;
                }
                else if (HUtil32.RangeInDefined(n14, 400, 499))
                {
                    SystemShare.Config.GlobaDyMval[n14 - 400] = (short)nCount;
                }
                else if (HUtil32.RangeInDefined(n14, 500, 599))
                {
                    playerActor.MNSval[n14 - 600] = nCount.ToString();
                }
            }
        }

        private static bool GotoLable_QuestCheckCondition_CheckDieMon(IPlayerActor playerActor, string monName)
        {
            bool result = string.IsNullOrEmpty(monName);
            if ((playerActor.LastHiter != null) && (playerActor.LastHiter.ChrName == monName))
            {
                result = true;
            }
            return result;
        }

        private static bool GotoLable_QuestCheckCondition_CheckKillMon(IPlayerActor playerActor, string monName)
        {
            bool result = string.IsNullOrEmpty(monName);
            if ((playerActor.TargetCret != null) && (playerActor.TargetCret.ChrName == monName))
            {
                result = true;
            }
            return result;
        }

        public static bool GotoLable_QuestCheckCondition_CheckRandomNo(IPlayerActor playerActor, string sNumber)
        {
            return playerActor.RandomNo == sNumber;
        }

        private bool QuestCheckConditionCheckUserDateType(IPlayerActor playerActor, string chrName, string sListFileName, string sDay, string param1, string param2)
        {
            string name = string.Empty;
            bool result = false;
            sListFileName = SystemShare.GetEnvirFilePath(sListFileName);
            using StringList loadList = new StringList();
            if (File.Exists(sListFileName))
            {
                try
                {
                    loadList.LoadFromFile(sListFileName);
                }
                catch
                {
                    LogService.Error("loading fail.... => " + sListFileName);
                }
            }
            int nDay = HUtil32.StrToInt(sDay, 0);
            for (int i = 0; i < loadList.Count; i++)
            {
                string sText = loadList[i].Trim();
                sText = HUtil32.GetValidStrCap(sText, ref name, new[] { ' ', '\t' });
                name = name.Trim();
                if (chrName == name)
                {
                    string ssDay = sText.Trim();
                    DateTime nnday = HUtil32.StrToDate(ssDay);
                    int useDay = HUtil32.Round(DateTime.Today.ToOADate() - nnday.ToOADate());
                    int lastDay = nDay - useDay;
                    if (lastDay < 0)
                    {
                        result = true;
                        lastDay = 0;
                    }
                    GotoLableQuestCheckConditionSetVal(playerActor, param1, useDay);
                    GotoLableQuestCheckConditionSetVal(playerActor, param2, lastDay);
                    return result;
                }
            }
            return false;
        }

        private bool GotoLableQuestCheckCondition(INormNpc normNpc, IPlayerActor playerActor, IList<QuestConditionInfo> conditionList, ref GotoLabParams gotoLabParams)
        {
            bool result = true;
            int n1C = 0;
            int nMaxDura = 0;
            int nDura = 0;
            for (int i = 0; i < conditionList.Count; i++)
            {
                QuestConditionInfo questConditionInfo = conditionList[i];
                if (ConditionScript.IsRegister(questConditionInfo.CmdCode))
                {
                    ConditionScript.Execute(normNpc, playerActor, questConditionInfo, ref result);
                    return result;
                }
                if (!string.IsNullOrEmpty(questConditionInfo.sParam1))
                {
                    if (questConditionInfo.sParam1[0] == '$')
                    {
                        string s50 = questConditionInfo.sParam1;
                        questConditionInfo.sParam1 = '<' + questConditionInfo.sParam1 + '>';
                        ConditionScript.GetVariableText(playerActor, ref s50, questConditionInfo.sParam1);
                    }
                    else if (questConditionInfo.sParam1.IndexOf(">", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        questConditionInfo.sParam1 = ConditionScript.GetLineVariableText(playerActor, questConditionInfo.sParam1);
                    }
                }
                if (!string.IsNullOrEmpty(questConditionInfo.sParam2))
                {
                    if (questConditionInfo.sParam2[0] == '$')
                    {
                        string s50 = questConditionInfo.sParam2;
                        questConditionInfo.sParam2 = '<' + questConditionInfo.sParam2 + '>';
                        ConditionScript.GetVariableText(playerActor, ref s50, questConditionInfo.sParam2);
                    }
                    else if (questConditionInfo.sParam2.IndexOf(">", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        questConditionInfo.sParam2 = ConditionScript.GetLineVariableText(playerActor, questConditionInfo.sParam2);
                    }
                }
                if (!string.IsNullOrEmpty(questConditionInfo.sParam3))
                {
                    if (questConditionInfo.sParam3[0] == '$')
                    {
                        string s50 = questConditionInfo.sParam3;
                        questConditionInfo.sParam3 = '<' + questConditionInfo.sParam3 + '>';
                        ConditionScript.GetVariableText(playerActor, ref s50, questConditionInfo.sParam3);
                    }
                    else if (questConditionInfo.sParam3.IndexOf(">", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        questConditionInfo.sParam3 = ConditionScript.GetLineVariableText(playerActor, questConditionInfo.sParam3);
                    }
                }
                if (!string.IsNullOrEmpty(questConditionInfo.sParam4))
                {
                    if (questConditionInfo.sParam4[0] == '$')
                    {
                        string s50 = questConditionInfo.sParam4;
                        questConditionInfo.sParam4 = '<' + questConditionInfo.sParam4 + '>';
                        ConditionScript.GetVariableText(playerActor, ref s50, questConditionInfo.sParam4);
                    }
                    else if (questConditionInfo.sParam4.IndexOf(">", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        questConditionInfo.sParam4 = ConditionScript.GetLineVariableText(playerActor, questConditionInfo.sParam4);
                    }
                }
                if (!string.IsNullOrEmpty(questConditionInfo.sParam5))
                {
                    if (questConditionInfo.sParam5[0] == '$')
                    {
                        string s50 = questConditionInfo.sParam5;
                        questConditionInfo.sParam5 = '<' + questConditionInfo.sParam5 + '>';
                        ConditionScript.GetVariableText(playerActor, ref s50, questConditionInfo.sParam5);
                    }
                    else if (questConditionInfo.sParam5.IndexOf(">", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        questConditionInfo.sParam5 = ConditionScript.GetLineVariableText(playerActor, questConditionInfo.sParam5);
                    }
                }
                if (!string.IsNullOrEmpty(questConditionInfo.sParam6))
                {
                    if (questConditionInfo.sParam6[0] == '$')
                    {
                        string s50 = questConditionInfo.sParam6;
                        questConditionInfo.sParam6 = '<' + questConditionInfo.sParam6 + '>';
                        ConditionScript.GetVariableText(playerActor, ref s50, questConditionInfo.sParam6);
                    }
                    else if (questConditionInfo.sParam6.IndexOf(">", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        questConditionInfo.sParam6 = ConditionScript.GetLineVariableText(playerActor, questConditionInfo.sParam6);
                    }
                }
                if (!string.IsNullOrEmpty(questConditionInfo.sOpName))
                {
                    if (questConditionInfo.sOpName.Length > 2)
                    {
                        if (questConditionInfo.sOpName[1] == '$')
                        {
                            string s50 = questConditionInfo.sOpName;
                            questConditionInfo.sOpName = '<' + questConditionInfo.sOpName + '>';
                            ConditionScript.GetVariableText(playerActor, ref s50, questConditionInfo.sOpName);
                        }
                        else if (questConditionInfo.sOpName.IndexOf(">", StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            questConditionInfo.sOpName = ConditionScript.GetLineVariableText(playerActor, questConditionInfo.sOpName);
                        }
                    }
                    /*IPlayerActor human = SystemShare.WorldEngine.GetIPlayerActor(questConditionInfo.sOpName);
                    if (human != null)
                    {
                        IPlayerActor = human;
                        if (!string.IsNullOrEmpty(questConditionInfo.sOpHName) && string.Compare(questConditionInfo.sOpHName, "H", StringComparison.OrdinalIgnoreCase) == 0)
                        {

                        }
                    }*/
                }
                if (HUtil32.IsStringNumber(questConditionInfo.sParam1))
                {
                    questConditionInfo.nParam1 = HUtil32.StrToInt(questConditionInfo.sParam1, 0);
                }

                if (HUtil32.IsStringNumber(questConditionInfo.sParam2))
                {
                    questConditionInfo.nParam2 = HUtil32.StrToInt(questConditionInfo.sParam2, 1);
                }

                if (HUtil32.IsStringNumber(questConditionInfo.sParam3))
                {
                    questConditionInfo.nParam3 = HUtil32.StrToInt(questConditionInfo.sParam3, 1);
                }

                if (HUtil32.IsStringNumber(questConditionInfo.sParam4))
                {
                    questConditionInfo.nParam4 = HUtil32.StrToInt(questConditionInfo.sParam4, 0);
                }

                if (HUtil32.IsStringNumber(questConditionInfo.sParam5))
                {
                    questConditionInfo.nParam5 = HUtil32.StrToInt(questConditionInfo.sParam5, 0);
                }

                if (HUtil32.IsStringNumber(questConditionInfo.sParam6))
                {
                    questConditionInfo.nParam6 = HUtil32.StrToInt(questConditionInfo.sParam6, 0);
                }

                switch (questConditionInfo.CmdCode)
                {
                    case (int)ExecutionCode.CheckUserDate:
                        //  result = QuestCheckConditionCheckUserDateType(playerActor,playerActor.ChrName, m_sPath + questConditionInfo.sParam1, questConditionInfo.sParam3, questConditionInfo.sParam4, questConditionInfo.sParam5);
                        break;
                    case (int)ConditionCode.CHECKRANDOMNO:
                        LogService.Error("TODO nSC_CHECKRANDOMNO...");
                        //result = GotoLable_QuestCheckCondition_CheckRandomNo(playerActor,sMsg);
                        break;
                    case (int)ConditionCode.CHECKDIEMON:
                        result = GotoLable_QuestCheckCondition_CheckDieMon(playerActor, questConditionInfo.sParam1);
                        break;
                    case (int)ConditionCode.CHECKKILLPLAYMON:
                        result = GotoLable_QuestCheckCondition_CheckKillMon(playerActor, questConditionInfo.sParam1);
                        break;
                    case (int)ConditionCode.CHECKITEMW:
                        gotoLabParams.UserItem = CheckGotoLableItemW(playerActor, questConditionInfo.sParam1, questConditionInfo.nParam2);
                        if (gotoLabParams.UserItem == null)
                        {
                            result = false;
                        }
                        break;
                    case (int)ConditionCode.ISTAKEITEM:
                        if (gotoLabParams.ItemName != questConditionInfo.sParam1)
                        {
                            result = false;
                        }
                        break;
                    case (int)ConditionCode.CHECKDURAEVA:
                        gotoLabParams.UserItem = playerActor.QuestCheckItem(questConditionInfo.sParam1, ref n1C, ref nMaxDura, ref nDura);
                        if (n1C > 0)
                        {
                            if (HUtil32.Round(nMaxDura / n1C / 1000.0) < questConditionInfo.nParam2)
                            {
                                result = false;
                            }
                        }
                        else
                        {
                            result = false;
                        }
                        break;
                    case (int)ConditionCode.KILLBYHUM:
                        if ((playerActor.LastHiter != null) && (playerActor.LastHiter.Race != ActorRace.Play))
                        {
                            result = false;
                        }
                        break;
                    case (int)ConditionCode.KILLBYMON:
                        if ((playerActor.LastHiter != null) && (playerActor.LastHiter.Race == ActorRace.Play))
                        {
                            result = false;
                        }
                        break;
                    case (int)ConditionCode.CHECKINSAFEZONE:
                        if (!playerActor.InSafeZone())
                        {
                            result = false;
                        }
                        break;
                    case (int)ConditionCode.SCHECKDEATHPLAYMON:
                        string s01 = string.Empty;
                        //if (!GetValValue(playerActor,questConditionInfo.sParam1, ref s01))
                        //{
                        //    s01 = ConditionScript.GetLineVariableText(playerActor,questConditionInfo.sParam1);
                        //}
                        result = CheckKillMon2(playerActor, s01);
                        break;
                }
                if (!result)
                {
                    break;
                }
            }
            return result;
        }

        private static bool CheckKillMon2(IPlayerActor playerActor, string sMonName)
        {
            return true;
        }

        private bool JmpToLable(IPlayerActor playerActor, INormNpc npc, string sLabel)
        {
            playerActor.ScriptGotoCount++;
            if (playerActor.ScriptGotoCount > SystemShare.Config.ScriptGotoCountLimit)
            {
                return false;
            }
            GotoLable(playerActor, npc.ActorId, sLabel);
            return true;
        }

        private void GoToQuest(IPlayerActor playerActor, INormNpc npc, int nQuest)
        {
            IList<ScriptInfo> ScriptList = npc.ScriptList;
            for (int i = 0; i < ScriptList.Count; i++)
            {
                ScriptInfo script = ScriptList[i];
                if (script.QuestCount == nQuest)
                {
                    playerActor.Script = script;
                    playerActor.LastNpc = npc.ActorId;
                    GotoLable(playerActor, npc.ActorId, ScriptFlagConst.sMAIN);
                    break;
                }
            }
        }

        private void GotoLableTakeItem(IPlayerActor playerActor, string sItemName, int nItemCount, string sC)
        {
            UserItem userItem;
            StdItem stdItem;
            if (string.Compare(sItemName, Grobal2.StringGoldName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                playerActor.DecGold(nItemCount);
                playerActor.GoldChanged();
                if (SystemShare.GameLogGold)
                {
                    // M2Share.EventSource.AddEventLog(10, playerActor.MapName + "\t" + playerActor.CurrX + "\t" + playerActor.CurrY + "\t" + playerActor.ChrName + "\t" + Grobal2.StringGoldName + "\t" + nItemCount + "\t" + '1' + "\t" + ChrName);
                }
                return;
            }
            for (int i = playerActor.ItemList.Count - 1; i >= 0; i--)
            {
                if (nItemCount <= 0)
                {
                    break;
                }
                userItem = playerActor.ItemList[i];
                if (string.Compare(SystemShare.ItemSystem.GetStdItemName(userItem.Index), sItemName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    stdItem = SystemShare.ItemSystem.GetStdItem(userItem.Index);
                    if (stdItem.NeedIdentify == 1)
                    {
                        // M2Share.EventSource.AddEventLog(10, playerActor.MapName + "\t" + playerActor.CurrX + "\t" + playerActor.CurrY + "\t" + playerActor.ChrName + "\t" + sItemName + "\t" + userItem.MakeIndex + "\t" + '1' + "\t" + ChrName);
                    }
                    playerActor.SendDelItems(userItem);
                    sC = SystemShare.ItemSystem.GetStdItemName(userItem.Index);
                    Dispose(userItem);
                    playerActor.ItemList.RemoveAt(i);
                    nItemCount -= 1;
                }
            }
        }

        public void GotoLableGiveItem(IPlayerActor playerActor, string sItemName, int nItemCount)
        {
            UserItem userItem;
            StdItem stdItem;
            if (string.Compare(sItemName, Grobal2.StringGoldName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                playerActor.IncGold(nItemCount);
                playerActor.GoldChanged();
                if (SystemShare.GameLogGold)
                {
                    // M2Share.EventSource.AddEventLog(9, playerActor.MapName + "\t" + playerActor.CurrX + "\t" + playerActor.CurrY + "\t" + playerActor.ChrName + "\t" + Grobal2.StringGoldName + "\t" + nItemCount + "\t" + '1' + "\t" + ChrName);
                }
                return;
            }
            if (SystemShare.ItemSystem.GetStdItemIdx(sItemName) > 0)
            {
                if (!(nItemCount >= 1 && nItemCount <= 50))
                {
                    nItemCount = 1;
                }
                for (int i = 0; i < nItemCount; i++)
                {
                    if (playerActor.IsEnoughBag())
                    {
                        userItem = new UserItem();
                        if (SystemShare.ItemSystem.CopyToUserItemFromName(sItemName, ref userItem))
                        {
                            playerActor.ItemList.Add(userItem);
                            playerActor.SendAddItem(userItem);
                            stdItem = SystemShare.ItemSystem.GetStdItem(userItem.Index);
                            if (stdItem.NeedIdentify == 1)
                            {
                                //M2Share.EventSource.AddEventLog(9, playerActor.MapName + "\t" + playerActor.CurrX + "\t" + playerActor.CurrY + "\t" + playerActor.ChrName + "\t" + sItemName + "\t" + userItem.MakeIndex + "\t" + '1' + "\t" + ChrName);
                            }
                        }
                        else
                        {
                            Dispose(userItem);
                        }
                    }
                    else
                    {
                        userItem = new UserItem();
                        if (SystemShare.ItemSystem.CopyToUserItemFromName(sItemName, ref userItem))
                        {
                            stdItem = SystemShare.ItemSystem.GetStdItem(userItem.Index);
                            if (stdItem.NeedIdentify == 1)
                            {
                                // M2Share.EventSource.AddEventLog(9, playerActor.MapName + "\t" + playerActor.CurrX + "\t" + playerActor.CurrY + "\t" + playerActor.ChrName + "\t" + sItemName + "\t" + userItem.MakeIndex + "\t" + '1' + "\t" + ChrName);
                            }
                            playerActor.DropItemDown(userItem, 3, false, playerActor.ActorId, 0);
                        }
                        Dispose(userItem);
                    }
                }
            }
        }

        private static void GotoLableTakeWItem(IPlayerActor playerActor, string sItemName, int nItemCount)
        {
            string sC;
            if (HUtil32.CompareLStr(sItemName, "[NECKLACE]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Necklace].Index > 0)
                {
                    playerActor.SendDelItems(playerActor.UseItems[ItemLocation.Necklace]);
                    sC = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Necklace].Index);
                    playerActor.UseItems[ItemLocation.Necklace].Index = 0;
                    return;
                }
            }
            if (HUtil32.CompareLStr(sItemName, "[RING]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Ringl].Index > 0)
                {
                    playerActor.SendDelItems(playerActor.UseItems[ItemLocation.Ringl]);
                    sC = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Ringl].Index);
                    playerActor.UseItems[ItemLocation.Ringl].Index = 0;
                    return;
                }
                if (playerActor.UseItems[ItemLocation.Ringr].Index > 0)
                {
                    playerActor.SendDelItems(playerActor.UseItems[ItemLocation.Ringr]);
                    sC = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Ringr].Index);
                    playerActor.UseItems[ItemLocation.Ringr].Index = 0;
                    return;
                }
            }
            if (HUtil32.CompareLStr(sItemName, "[ARMRING]", 4))
            {
                if (playerActor.UseItems[ItemLocation.ArmRingl].Index > 0)
                {
                    playerActor.SendDelItems(playerActor.UseItems[ItemLocation.ArmRingl]);
                    sC = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[ItemLocation.ArmRingl].Index);
                    playerActor.UseItems[ItemLocation.ArmRingl].Index = 0;
                    return;
                }
                if (playerActor.UseItems[ItemLocation.ArmRingr].Index > 0)
                {
                    playerActor.SendDelItems(playerActor.UseItems[ItemLocation.ArmRingr]);
                    sC = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[ItemLocation.ArmRingr].Index);
                    playerActor.UseItems[ItemLocation.ArmRingr].Index = 0;
                    return;
                }
            }
            if (HUtil32.CompareLStr(sItemName, "[WEAPON]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Weapon].Index > 0)
                {
                    playerActor.SendDelItems(playerActor.UseItems[ItemLocation.Weapon]);
                    sC = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Weapon].Index);
                    playerActor.UseItems[ItemLocation.Weapon].Index = 0;
                    return;
                }
            }
            if (HUtil32.CompareLStr(sItemName, "[HELMET]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Helmet].Index > 0)
                {
                    playerActor.SendDelItems(playerActor.UseItems[ItemLocation.Helmet]);
                    sC = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Helmet].Index);
                    playerActor.UseItems[ItemLocation.Helmet].Index = 0;
                    return;
                }
            }
            if (HUtil32.CompareLStr(sItemName, "[DRESS]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Dress].Index > 0)
                {
                    playerActor.SendDelItems(playerActor.UseItems[ItemLocation.Dress]);
                    sC = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Dress].Index);
                    playerActor.UseItems[ItemLocation.Dress].Index = 0;
                    return;
                }
            }
            if (HUtil32.CompareLStr(sItemName, "[U_BUJUK]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Bujuk].Index > 0)
                {
                    playerActor.SendDelItems(playerActor.UseItems[ItemLocation.Bujuk]);
                    sC = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Bujuk].Index);
                    playerActor.UseItems[ItemLocation.Bujuk].Index = 0;
                    return;
                }
            }
            if (HUtil32.CompareLStr(sItemName, "[U_BELT]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Belt].Index > 0)
                {
                    playerActor.SendDelItems(playerActor.UseItems[ItemLocation.Belt]);
                    sC = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Belt].Index);
                    playerActor.UseItems[ItemLocation.Belt].Index = 0;
                    return;
                }
            }
            if (HUtil32.CompareLStr(sItemName, "[U_BOOTS]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Boots].Index > 0)
                {
                    playerActor.SendDelItems(playerActor.UseItems[ItemLocation.Boots]);
                    sC = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Boots].Index);
                    playerActor.UseItems[ItemLocation.Boots].Index = 0;
                    return;
                }
            }
            if (HUtil32.CompareLStr(sItemName, "[U_CHARM]", 4))
            {
                if (playerActor.UseItems[ItemLocation.Charm].Index > 0)
                {
                    playerActor.SendDelItems(playerActor.UseItems[ItemLocation.Charm]);
                    sC = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Charm].Index);
                    playerActor.UseItems[ItemLocation.Charm].Index = 0;
                    return;
                }
            }
            for (int i = 0; i < playerActor.UseItems.Length; i++)
            {
                if (nItemCount <= 0)
                {
                    return;
                }
                if (playerActor.UseItems[i].Index > 0)
                {
                    string sName = SystemShare.ItemSystem.GetStdItemName(playerActor.UseItems[i].Index);
                    if (string.Compare(sName, sItemName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        playerActor.SendDelItems(playerActor.UseItems[i]);
                        playerActor.UseItems[i].Index = 0;
                        nItemCount -= 1;
                    }
                }
            }
        }

        private bool GotoLableQuestActionProcess(INormNpc normNpc, IPlayerActor playerActor, IList<QuestActionInfo> actionList, ref GotoLabParams gotoLabParams)
        {
            bool result = true;
            for (int i = 0; i < actionList.Count; i++)
            {
                QuestActionInfo questActionInfo = actionList[i];
                if (ExecutionProcessing.IsRegister(questActionInfo.nCmdCode))
                {
                    ExecutionProcessing.Execute(normNpc, playerActor, questActionInfo, ref result);
                    return result;
                }
                ExecutionCode executionCode = (ExecutionCode)questActionInfo.nCmdCode;
                switch (executionCode)
                {
                    case ExecutionCode.Take:
                        GotoLableTakeItem(playerActor, questActionInfo.sParam1, questActionInfo.nParam2, gotoLabParams.ItemName);
                        break;
                    case ExecutionCode.Takew:
                        GotoLableTakeWItem(playerActor, questActionInfo.sParam1, questActionInfo.nParam2);
                        break;
                    case ExecutionCode.TakecheckItem:
                        if (gotoLabParams.UserItem != null)
                        {
                            playerActor.QuestTakeCheckItem(gotoLabParams.UserItem);
                        }
                        else
                        {
                            ScriptActionError(normNpc, playerActor, "", questActionInfo, ExecutionCode.TakecheckItem);
                        }
                        break;
                    case ExecutionCode.Break:
                        result = false;
                        break;
                    case ExecutionCode.Param1:
                        int n34 = questActionInfo.nParam1;
                        string s44 = questActionInfo.sParam1;
                        break;
                    case ExecutionCode.Param2:
                        int n38 = questActionInfo.nParam1;
                        string s48 = questActionInfo.sParam1;
                        break;
                    case ExecutionCode.Param3:
                        int n3C = questActionInfo.nParam1;
                        string s4C = questActionInfo.sParam1;
                        break;
                    case ExecutionCode.Param4:
                        int n40 = questActionInfo.nParam1;
                        break;
                    case ExecutionCode.Map:
                    case ExecutionCode.MapMove:
                        gotoLabParams.SendSayMsg = true;
                        break;
                    case ExecutionCode.AddBatch:
                        //if (BatchParamsList == null)
                        //{
                        //    BatchParamsList = new List<ScriptParams>();
                        //}
                        //BatchParamsList.Add(new ScriptParams()
                        //{
                        //    sParams = questActionInfo.sParam1,
                        //    nParams = n18
                        //});
                        break;
                    case ExecutionCode.BatchDelay:
                        int n18 = questActionInfo.nParam1 * 1000;
                        break;
                    case ExecutionCode.BatchMove:
                        //int n20 = 0;
                        //for (int k = 0; k < BatchParamsList.Count; k++)
                        //{
                        //    ScriptParams batchParam = BatchParamsList[k];
                        //    playerActor.SendSelfDelayMsg(Messages.RM_RANDOMSPACEMOVE, 0, 0, 0, 0, BatchParamsList[k].sParams, batchParam.nParams + n20);
                        //    n20 += batchParam.nParams;
                        //}
                        break;
                    case ExecutionCode.PlayDice:
                        gotoLabParams.SendSayMsg = true;
                        break;
                    case ExecutionCode.GoQuest:
                        GoToQuest(playerActor, normNpc, questActionInfo.nParam1);
                        break;
                    case ExecutionCode.EndQuest:
                        playerActor.Script = null;
                        break;
                    case ExecutionCode.Goto:
                        if (!JmpToLable(playerActor, normNpc, questActionInfo.sParam1))
                        {
                            LogService.Error("[脚本死循环] NPC:" + normNpc.ChrName + " 位置:" + normNpc.MapName + '(' + normNpc.CurrX + ':' + normNpc.CurrY + ')' + " 命令:" + ExecutionCode.Goto + ' ' + questActionInfo.sParam1);
                            result = false;
                            return result;
                        }
                        break;
                    case ExecutionCode.GetDlgItemValue:

                        break;
                    case ExecutionCode.TakeDlgItem:

                        break;
                }
            }
            return result;
        }

        private void GotoLableSendMerChantSayMsg(INormNpc normNpc, IPlayerActor playerActor, string sMsg, bool boFlag)
        {
            sMsg = normNpc.GetLineVariableText(playerActor, sMsg);
            playerActor.GetScriptLabel(sMsg);
            if (boFlag)
            {
                playerActor.SendPriorityMsg(Messages.RM_MERCHANTSAY, 0, 0, 0, 0, normNpc.ChrName + '/' + sMsg, MessagePriority.High);
            }
            else
            {
                playerActor.SendMsg(normNpc, Messages.RM_MERCHANTSAY, 0, 0, 0, 0, normNpc.ChrName + '/' + sMsg);
            }
        }

        private void ScriptActionError(INormNpc normNpc, IPlayerActor playerActor, string sErrMsg, QuestActionInfo QuestActionInfo, ExecutionCode sCmd)
        {
            const string sOutMessage = "[脚本错误] {0} 脚本命令:{1} NPC名称:{2} 地图:{3}({4}:{5}) 参数1:{6} 参数2:{7} 参数3:{8} 参数4:{9} 参数5:{10} 参数6:{11}";
            string sMsg = string.Format(sOutMessage, sErrMsg, sCmd, normNpc.ChrName, normNpc.MapName, normNpc.CurrX, normNpc.CurrY, QuestActionInfo.sParam1, QuestActionInfo.sParam2, QuestActionInfo.sParam3, QuestActionInfo.sParam4, QuestActionInfo.sParam5, QuestActionInfo.sParam6);
            LogService.Error(sMsg);
        }

        private void ScriptActionError(INormNpc normNpc, IPlayerActor playerActor, string sErrMsg, QuestActionInfo QuestActionInfo, string sCmd)
        {
            const string sOutMessage = "[脚本错误] {0} 脚本命令:{1} NPC名称:{2} 地图:{3}({4}:{5}) 参数1:{6} 参数2:{7} 参数3:{8} 参数4:{9} 参数5:{10} 参数6:{11}";
            string sMsg = string.Format(sOutMessage, sErrMsg, sCmd, normNpc.ChrName, normNpc.MapName, normNpc.CurrX, normNpc.CurrY, QuestActionInfo.sParam1, QuestActionInfo.sParam2, QuestActionInfo.sParam3, QuestActionInfo.sParam4, QuestActionInfo.sParam5, QuestActionInfo.sParam6);
            LogService.Error(sMsg);
        }

        private void ScriptConditionError(INormNpc normNpc, IPlayerActor playerActor, string sErrMsg, QuestConditionInfo QuestConditionInfo, ConditionCode sCmd)
        {
            const string sOutMessage = "[脚本错误] {0} 脚本命令:{1} NPC名称:{2} 地图:{3}({4}:{5}) 参数1:{6} 参数2:{7} 参数3:{8} 参数4:{9} 参数5:{10} 参数6:{11}";
            string sMsg = string.Format(sOutMessage, sErrMsg, sCmd, normNpc.ChrName, normNpc.MapName, normNpc.CurrX, normNpc.CurrY, QuestConditionInfo.sParam1, QuestConditionInfo.sParam2, QuestConditionInfo.sParam3, QuestConditionInfo.sParam4, QuestConditionInfo.sParam5, QuestConditionInfo.sParam6);
            LogService.Error(sMsg);
        }

        private void ScriptConditionError(INormNpc normNpc, IPlayerActor playerActor, QuestConditionInfo QuestConditionInfo, string sCmd)
        {
            string sMsg = "Cmd:" + sCmd + " NPC名称:" + normNpc.ChrName + " 地图:" + normNpc.MapName + " 座标:" + normNpc.CurrX + ':' + normNpc.CurrY + " 参数1:" + QuestConditionInfo.sParam1 + " 参数2:" + QuestConditionInfo.sParam2 + " 参数3:" + QuestConditionInfo.sParam3 + " 参数4:" + QuestConditionInfo.sParam4 + " 参数5:" + QuestConditionInfo.sParam5;
            LogService.Error("[脚本参数不正确] " + sMsg);
        }

        public void Dispose(object obj)
        {
            obj = null;
        }
    }
}