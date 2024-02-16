﻿using OpenMir2;
using OpenMir2.Common;
using OpenMir2.Data;
using OpenMir2.Enums;
using ScriptSystem.Processings;
using System.Text;
using SystemModule;
using SystemModule.Actors;

namespace ScriptSystem
{
    public class ProcessingBase
    {
        internal bool GetMovDataHumanInfoValue(INormNpc normNpc, IPlayerActor playerActor, string sVariable, ref string sValue, ref int nValue, ref int nDataType)
        {
            string s10 = string.Empty;
            string sVarValue2 = string.Empty;
            DynamicVar DynamicVar;
            sValue = "";
            nValue = -1;
            nDataType = -1;
            bool result = false;
            if (string.IsNullOrEmpty(sVariable))
            {
                return false;
            }
            string sMsg = sVariable;
            HUtil32.ArrestStringEx(sMsg, "<", ">", ref s10);
            if (string.IsNullOrEmpty(s10))
            {
                return false;
            }
            sVariable = s10;
            //全局信息
            switch (sVariable)
            {
                case "$SERVERNAME":
                    sValue = SystemShare.Config.ServerName;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$SERVERIP":
                    sValue = SystemShare.Config.ServerIPaddr;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$WEBSITE":
                    sValue = SystemShare.Config.sWebSite;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BBSSITE":
                    sValue = SystemShare.Config.sBbsSite;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$CLIENTDOWNLOAD":
                    sValue = SystemShare.Config.sClientDownload;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$QQ":
                    sValue = SystemShare.Config.sQQ;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$PHONE":
                    sValue = SystemShare.Config.sPhone;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BANKACCOUNT0":
                    sValue = SystemShare.Config.sBankAccount0;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BANKACCOUNT1":
                    sValue = SystemShare.Config.sBankAccount1;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BANKACCOUNT2":
                    sValue = SystemShare.Config.sBankAccount2;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BANKACCOUNT3":
                    sValue = SystemShare.Config.sBankAccount3;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BANKACCOUNT4":
                    sValue = SystemShare.Config.sBankAccount4;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BANKACCOUNT5":
                    sValue = SystemShare.Config.sBankAccount5;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BANKACCOUNT6":
                    sValue = SystemShare.Config.sBankAccount6;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BANKACCOUNT7":
                    sValue = SystemShare.Config.sBankAccount7;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BANKACCOUNT8":
                    sValue = SystemShare.Config.sBankAccount8;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BANKACCOUNT9":
                    sValue = SystemShare.Config.sBankAccount9;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$GAMEGOLDNAME":
                    sValue = SystemShare.Config.GameGoldName;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$GAMEPOINTNAME":
                    sValue = SystemShare.Config.GamePointName;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$USERCOUNT":
                    sValue = SystemShare.WorldEngine.PlayObjectCount.ToString();
                    nDataType = 0;
                    result = true;
                    return result;
                case "$MACRUNTIME":
                    sValue = (HUtil32.GetTickCount() / 86400000).ToString();// (24 * 60 * 60 * 1000)
                    nDataType = 0;
                    result = true;
                    return result;
                case "$SERVERRUNTIME":
                    sValue = DateTimeOffset.FromUnixTimeMilliseconds(SystemShare.StartTime).ToString("YYYY-MM-DD HH:mm:ss");
                    nDataType = 0;
                    result = true;
                    return result;
                case "$DATETIME":
                    sValue = DateTime.Now.ToString("YYYY-MM-DD HH:mm:ss");
                    nDataType = 0;
                    result = true;
                    return result;
                case "$DATE":
                    sValue = DateTime.Now.ToString("yyyy-MM-dd");
                    nDataType = 0;
                    result = true;
                    return result;
                case "$CASTLEWARDATE":// 申请攻城的日期
                    {
                        if (normNpc.Castle == null)
                        {
                            // normNpc.Castle = SystemShare.CastleMgr.GetCastle(0);
                        }
                        if (normNpc.Castle != null)
                        {
                            if (!normNpc.Castle.UnderWar)
                            {
                                sValue = normNpc.Castle.GetWarDate();
                                if (!string.IsNullOrEmpty(sValue))
                                {
                                    sMsg = ReplaceVariableText(sMsg, "<$CASTLEWARDATE>", sValue);
                                }
                            }
                        }
                        else
                        {
                            sValue = "????";
                        }
                        nDataType = 0;
                        result = true;
                        return result;
                    }
                case "$USERNAME":// 个人信息
                    sValue = playerActor.ChrName;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$KILLER":// 杀人者变量 
                    {
                        if (playerActor.Death && (playerActor.LastHiter != null))
                        {
                            if (playerActor.LastHiter.Race == ActorRace.Play)
                            {
                                sValue = playerActor.LastHiter.ChrName;
                            }
                        }
                        else
                        {
                            sValue = "未知";
                        }
                        nDataType = 0;
                        result = true;
                        return result;
                    }
                case "$MONKILLER":// 杀人的怪物变量 
                    {
                        if (playerActor.Death && (playerActor.LastHiter != null))
                        {
                            if (playerActor.LastHiter.Race != ActorRace.Play)
                            {
                                sValue = playerActor.LastHiter.ChrName;
                            }
                        }
                        else
                        {
                            sValue = "未知";
                        }
                        nDataType = 0;
                        result = true;
                        return result;
                    }
                case "$USERALLNAME":// 全名 
                    sValue = playerActor.GetShowName();
                    nDataType = 0;
                    result = true;
                    return result;
                case "$SFNAME":// 师傅名 
                    sValue = playerActor.MasterName;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$STATSERVERTIME":// 显示M2启动时间
                    DateTimeOffset.FromUnixTimeMilliseconds(SystemShare.StartTime).ToString("YYYY-MM-DD HH:mm:ss");
                    nDataType = 0;
                    result = true;
                    return result;
                case "$RUNDATETIME":// 开区间隔时间 显示为XX小时。
                    TimeSpan ts = DateTimeOffset.Now - DateTimeOffset.FromUnixTimeMilliseconds(SystemShare.StartTime);
                    sValue = $"服务器运行:[{ts.Days}天{ts.Hours}小时{ts.Minutes}分{ts.Seconds}秒]";
                    nDataType = 0;
                    result = true;
                    return result;
                case "$RANDOMNO":// 随机值变量
                    nValue = SystemShare.RandomNumber.Random(int.MaxValue);
                    nDataType = 1;
                    result = true;
                    return result;
                case "$USERID":// 登录账号
                    sValue = playerActor.UserAccount;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$IPADDR":// 登录IP
                    sValue = playerActor.LoginIpAddr;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$X": // 人物X坐标
                    nValue = playerActor.CurrX;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$Y": // 人物Y坐标
                    nValue = playerActor.CurrY;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAP":
                    sValue = playerActor.Envir.MapName;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$GUILDNAME":
                    {
                        if (playerActor.MyGuild != null)
                        {
                            sValue = playerActor.MyGuild.GuildName;
                        }
                        else
                        {
                            sValue = "无";
                        }
                        nDataType = 0;
                        result = true;
                        return result;
                    }
                case "$RANKNAME":
                    sValue = playerActor.GuildRankName;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$RELEVEL":
                    nValue = playerActor.ReLevel;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$LEVEL":
                    nValue = playerActor.Abil.Level;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$HP":
                    nValue = playerActor.WAbil.HP;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAXHP":
                    nValue = playerActor.WAbil.MaxHP;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MP":
                    nValue = playerActor.WAbil.MP;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAXMP":
                    nValue = playerActor.WAbil.MaxMP;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$AC":
                    nValue = HUtil32.LoWord(playerActor.WAbil.AC);
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAXAC":
                    nValue = HUtil32.HiWord(playerActor.WAbil.AC);
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAC":
                    nValue = HUtil32.LoWord(playerActor.WAbil.MAC);
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAXMAC":
                    nValue = HUtil32.HiWord(playerActor.WAbil.MAC);
                    nDataType = 1;
                    result = true;
                    return result;
                case "$DC":
                    nValue = HUtil32.LoWord(playerActor.WAbil.DC);
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAXDC":
                    nValue = HUtil32.HiWord(playerActor.WAbil.DC);
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MC":
                    nValue = HUtil32.LoWord(playerActor.WAbil.MC);
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAXMC":
                    nValue = HUtil32.HiWord(playerActor.WAbil.MC);
                    nDataType = 1;
                    result = true;
                    return result;
                case "$SC":
                    nValue = HUtil32.LoWord(playerActor.WAbil.SC);
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAXSC":
                    nValue = HUtil32.HiWord(playerActor.WAbil.SC);
                    nDataType = 1;
                    result = true;
                    return result;
                case "$EXP":
                    nValue = playerActor.Abil.Exp;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAXEXP":
                    nValue = playerActor.Abil.MaxExp;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$PKPOINT":
                    nValue = playerActor.PkPoint;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$CREDITPOINT":
                    nValue = playerActor.CreditPoint;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$HW":
                    nValue = playerActor.WAbil.HandWeight;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAXHW":
                    nValue = playerActor.WAbil.MaxHandWeight;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$BW":
                    nValue = playerActor.WAbil.Weight;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAXBW":
                    nValue = playerActor.WAbil.MaxWeight;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$WW":
                    nValue = playerActor.WAbil.WearWeight;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$MAXWW":
                    nValue = playerActor.WAbil.MaxWearWeight;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$GOLDCOUNT":
                    nValue = playerActor.Gold;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$GOLDCOUNTX":
                    nValue = playerActor.GoldMax;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$GAMEGOLD":
                    nValue = playerActor.GameGold;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$GAMEPOINT":
                    nValue = playerActor.GamePoint;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$HUNGER":
                    nValue = playerActor.GetMyStatus();
                    nDataType = 1;
                    result = true;
                    return result;
                case "$LOGINTIME":
                    sValue = DateTimeOffset.FromUnixTimeMilliseconds(playerActor.LogonTime).ToString("yyyy-MM-dd HH:mm:ss");
                    nDataType = 0;
                    result = true;
                    return result;
                case "$LOGINLONG":
                    nValue = (HUtil32.GetTickCount() - playerActor.LogonTick) / 60000;
                    nDataType = 1;
                    result = true;
                    return result;
                case "$DRESS":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Dress].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$WEAPON":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Weapon].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$RIGHTHAND":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.RighThand].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$HELMET":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Helmet].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$NECKLACE":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Necklace].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$RING_R":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Ringr].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$RING_L":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Ringl].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$ARMRING_R":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.ArmRingr].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$ARMRING_L":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.ArmRingl].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BUJUK":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Bujuk].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BELT":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Belt].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$BOOTS":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Boots].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$CHARM":
                    sValue = SystemShare.EquipmentSystem.GetStdItemName(playerActor.UseItems[ItemLocation.Charm].Index);
                    nDataType = 0;
                    result = true;
                    return result;
                case "$IPLOCAL":
                    sValue = playerActor.LoginIpLocal;
                    nDataType = 0;
                    result = true;
                    return result;
                case "$GUILDBUILDPOINT":
                    {
                        if (playerActor.MyGuild != null)
                        {
                            //nValue = playerActor.MyGuild.nBuildPoint;
                        }
                        nDataType = 0;
                        result = true;
                        return result;
                    }
                case "$GUILDAURAEPOINT":
                    {
                        if (playerActor.MyGuild != null)
                        {
                            nValue = playerActor.MyGuild.Aurae;
                        }
                        nDataType = 0;
                        result = true;
                        return result;
                    }
                case "$GUILDSTABILITYPOINT":
                    {
                        if (playerActor.MyGuild != null)
                        {
                            nValue = playerActor.MyGuild.Stability;
                        }
                        nDataType = 0;
                        result = true;
                        return result;
                    }
                case "$GUILDFLOURISHPOINT":
                    {
                        if (playerActor.MyGuild != null)
                        {
                            nValue = playerActor.MyGuild.Flourishing;
                        }
                        nDataType = 0;
                        result = true;
                        return result;
                    }
            }
            if (HUtil32.CompareLStr(sVariable, "$GLOBAL", 6))//  全局变量
            {
                HUtil32.ArrestStringEx(sVariable, "(", ")", ref sVarValue2);
                if (SystemShare.DynamicVarList.TryGetValue(sVarValue2, out DynamicVar))
                {
                    switch (DynamicVar.VarType)
                    {
                        case VarType.Integer:
                            nValue = DynamicVar.Internet;
                            nDataType = 1;
                            result = true;
                            return result;
                        case VarType.String:
                            sValue = DynamicVar.String;
                            nDataType = 0;
                            result = true;
                            return result;
                    }
                }
            }
            if (HUtil32.CompareLStr(sVariable, "$HUMAN", 6))//  人物变量
            {
                HUtil32.ArrestStringEx(sVariable, "(", ")", ref sVarValue2);
                if (playerActor.DynamicVarMap.TryGetValue(sVarValue2, out DynamicVar))
                {
                    switch (DynamicVar.VarType)
                    {
                        case VarType.Integer:
                            nValue = DynamicVar.Internet;
                            nDataType = 1;
                            result = true;
                            return result;
                        case VarType.String:
                            sValue = DynamicVar.String;
                            nDataType = 0;
                            result = true;
                            return result;
                    }
                }
            }
            if (HUtil32.CompareLStr(sVariable, "$ACCOUNT", 8)) //  人物变量
            {
                HUtil32.ArrestStringEx(sVariable, "(", ")", ref sVarValue2);
                if (playerActor.DynamicVarMap.TryGetValue(sVarValue2, out DynamicVar))
                {
                    switch (DynamicVar.VarType)
                    {
                        case VarType.Integer:
                            nValue = DynamicVar.Internet;
                            nDataType = 1;
                            result = true;
                            return result;
                        case VarType.String:
                            sValue = DynamicVar.String;
                            nDataType = 0;
                            result = true;
                            return result;
                    }
                }
            }
            return result;
        }

        internal static bool SetMovDataValNameValue(IPlayerActor playerActor, string sVarName, string sValue, int nValue, int nDataType)
        {
            bool result = false;
            int n100 = SystemShare.GetValNameNo(sVarName);
            if (n100 >= 0)
            {
                switch (nDataType)
                {
                    case 1:
                        if (HUtil32.RangeInDefined(n100, 0, 99))
                        {
                            playerActor.MNVal[n100] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 100, 199))
                        {
                            SystemShare.Config.GlobalVal[n100 - 100] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 200, 299))
                        {
                            playerActor.MDyVal[n100 - 200] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 300, 399))
                        {
                            playerActor.MNMval[n100 - 300] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 400, 499))
                        {
                            SystemShare.Config.GlobaDyMval[n100 - 400] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 500, 599))
                        {
                            playerActor.MNInteger[n100 - 500] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 600, 699))
                        {
                            playerActor.MSString[n100 - 600] = nValue.ToString();
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 700, 799))
                        {
                            SystemShare.Config.GlobalAVal[n100 - 700] = nValue.ToString();
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 800, 1199)) // G变量
                        {
                            SystemShare.Config.GlobalVal[n100 - 700] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 1200, 1599)) // A变量
                        {
                            SystemShare.Config.GlobalAVal[n100 - 1100] = nValue.ToString();
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }
                        break;
                    case 0:
                        if (HUtil32.RangeInDefined(n100, 0, 99))
                        {
                            playerActor.MNVal[n100] = HUtil32.StrToInt(sValue, 0);
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 100, 199))
                        {
                            SystemShare.Config.GlobalVal[n100 - 100] = HUtil32.StrToInt(sValue, 0);
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 200, 299))
                        {
                            playerActor.MDyVal[n100 - 200] = HUtil32.StrToInt(sValue, 0);
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 300, 399))
                        {
                            playerActor.MNMval[n100 - 300] = HUtil32.StrToInt(sValue, 0);
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 400, 499))
                        {
                            SystemShare.Config.GlobaDyMval[n100 - 400] = HUtil32.StrToInt(sValue, 0);
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 500, 599))
                        {
                            playerActor.MNInteger[n100 - 500] = HUtil32.StrToInt(sValue, 0);
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 600, 699))
                        {
                            playerActor.MSString[n100 - 600] = sValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 700, 799))
                        {
                            SystemShare.Config.GlobalAVal[n100 - 700] = sValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 800, 1199)) // G变量
                        {
                            SystemShare.Config.GlobalVal[n100 - 700] = HUtil32.StrToInt(sValue, 0);
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 1200, 1599)) // A变量
                        {
                            SystemShare.Config.GlobalAVal[n100 - 1100] = sValue;
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }
                        break;
                    case 3:
                        if (HUtil32.RangeInDefined(n100, 0, 99))
                        {
                            playerActor.MNVal[n100] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 100, 199))
                        {
                            SystemShare.Config.GlobalVal[n100 - 100] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 200, 299))
                        {
                            playerActor.MDyVal[n100 - 200] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 300, 399))
                        {
                            playerActor.MNMval[n100 - 300] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 400, 499))
                        {
                            SystemShare.Config.GlobaDyMval[n100 - 400] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 500, 599))
                        {
                            playerActor.MNInteger[n100 - 500] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 600, 699))
                        {
                            playerActor.MSString[n100 - 600] = sValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 700, 799))
                        {
                            SystemShare.Config.GlobalAVal[n100 - 700] = sValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 800, 1199)) // G变量
                        {
                            SystemShare.Config.GlobalVal[n100 - 700] = nValue;
                            result = true;
                        }
                        else if (HUtil32.RangeInDefined(n100, 1200, 1599)) // A变量
                        {
                            SystemShare.Config.GlobalAVal[n100 - 1100] = sValue;
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }
                        break;
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        internal static bool GetMovDataValNameValue(IPlayerActor playerActor, string sVarName, ref string sValue, ref int nValue, ref int nDataType)
        {
            bool result = false;
            nValue = -1;
            sValue = "";
            nDataType = -1;
            int n100 = SystemShare.GetValNameNo(sVarName);
            if (n100 >= 0)
            {
                if (HUtil32.RangeInDefined(n100, 0, 99))
                {
                    nValue = playerActor.MNVal[n100];
                    nDataType = 1;
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n100, 100, 199))
                {
                    nValue = SystemShare.Config.GlobalVal[n100 - 100];
                    nDataType = 1;
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n100, 200, 299))
                {
                    nValue = playerActor.MDyVal[n100 - 200];
                    nDataType = 1;
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n100, 300, 399))
                {
                    nValue = playerActor.MNMval[n100 - 300];
                    nDataType = 1;
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n100, 400, 499))
                {
                    nValue = SystemShare.Config.GlobaDyMval[n100 - 400];
                    nDataType = 1;
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n100, 500, 599))
                {
                    nValue = playerActor.MNInteger[n100 - 500];
                    nDataType = 1;
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n100, 600, 699))
                {
                    sValue = playerActor.MSString[n100 - 600];
                    nDataType = 0;
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n100, 700, 799))
                {
                    sValue = SystemShare.Config.GlobalAVal[n100 - 700];
                    nDataType = 0;
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n100, 800, 1199))//G变量
                {
                    nValue = SystemShare.Config.GlobalVal[n100 - 700];
                    nDataType = 1;
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n100, 1200, 1599))//A变量
                {
                    sValue = SystemShare.Config.GlobalAVal[n100 - 1100];
                    nDataType = 0;
                    result = true;
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        internal static bool GetMovDataDynamicVarValue(IPlayerActor playerActor, string sVarType, string sVarName, ref string sValue, ref int nValue, ref int nDataType)
        {
            string sName = string.Empty;
            sValue = "";
            nValue = -1;
            nDataType = -1;
            Dictionary<string, DynamicVar> DynamicVarList = GetDynamicVarMap(playerActor, sVarType, ref sName);
            if (DynamicVarList == null)
            {
                return false;
            }
            if (DynamicVarList.TryGetValue(sVarName, out DynamicVar DynamicVar))
            {
                switch (DynamicVar.VarType)
                {
                    case VarType.Integer:
                        nValue = DynamicVar.Internet;
                        nDataType = 1;
                        break;
                    case VarType.String:
                        sValue = DynamicVar.String;
                        nDataType = 0;
                        break;
                }
                return true;
            }
            return false;
        }

        internal static bool SetMovDataDynamicVarValue(IPlayerActor playerActor, string sVarType, string sVarName, string sValue, int nValue, int nDataType)
        {
            string sName = string.Empty;
            bool boVarFound = false;
            Dictionary<string, DynamicVar> DynamicVarList = GetDynamicVarMap(playerActor, sVarType, ref sName);
            if (DynamicVarList == null)
            {
                return false;
            }
            if (DynamicVarList.TryGetValue(sVarName, out DynamicVar DynamicVar))
            {
                if (nDataType == 1 && DynamicVar.VarType == VarType.Integer)
                {
                    DynamicVar.Internet = nValue;
                    boVarFound = true;
                }
                else if (DynamicVar.VarType == VarType.String)
                {
                    DynamicVar.String = sValue;
                    boVarFound = true;
                }
            }
            return boVarFound;
        }

        internal static int GetMovDataType(QuestActionInfo questActionInfo)
        {
            string sParam1 = string.Empty;
            string sParam2 = string.Empty;
            string sParam3 = string.Empty;
            int result = -1;
            if (HUtil32.CompareLStr(questActionInfo.sParam1, "<$STR(", 6))
            {
                HUtil32.ArrestStringEx(questActionInfo.sParam1, "(", ")", ref sParam1);
            }
            else
            {
                sParam1 = questActionInfo.sParam1;
            }
            if (HUtil32.CompareLStr(questActionInfo.sParam2, "<$STR(", 6))
            {
                HUtil32.ArrestStringEx(questActionInfo.sParam2, "(", ")", ref sParam2);
            }
            else
            {
                sParam2 = questActionInfo.sParam2;
            }
            if (HUtil32.CompareLStr(questActionInfo.sParam3, "<$STR(", 6))
            {
                HUtil32.ArrestStringEx(questActionInfo.sParam3, "(", ")", ref sParam3);
            }
            else
            {
                sParam3 = questActionInfo.sParam3;
            }
            if (HUtil32.IsVarNumber(sParam1))
            {
                if ((!string.IsNullOrEmpty(sParam3)) && (sParam3[0] == '<') && (sParam3[^1] == '>'))
                {
                    result = 0;
                }
                else if ((!string.IsNullOrEmpty(sParam3)) && (SystemShare.GetValNameNo(sParam3) >= 0))
                {
                    result = 1;
                }
                else if ((!string.IsNullOrEmpty(sParam3)) && HUtil32.IsStringNumber(sParam3))
                {
                    result = 2;
                }
                else
                {
                    result = 3;
                }
                return result;
            }
            int n01 = SystemShare.GetValNameNo(sParam1);
            if (n01 >= 0)
            {
                if (((!string.IsNullOrEmpty(sParam2))) && (sParam2[0] == '<') && (sParam2[^1] == '>'))
                {
                    result = 4;
                }
                else if (((!string.IsNullOrEmpty(sParam2))) && (SystemShare.GetValNameNo(sParam2) >= 0))
                {
                    result = 5;
                }
                else if (((!string.IsNullOrEmpty(sParam2))) && HUtil32.IsVarNumber(sParam2))
                {
                    result = 6;
                }
                else
                {
                    result = 7;
                }
                return result;
            }
            return result;
        }

        protected static Dictionary<string, DynamicVar> GetDynamicVarMap(IPlayerActor playerActor, string sType, ref string sName)
        {
            Dictionary<string, DynamicVar> result = null;
            if (HUtil32.CompareLStr(sType, "HUMAN", 5))
            {
                result = playerActor.DynamicVarMap;
                sName = playerActor.ChrName;
            }
            else if (HUtil32.CompareLStr(sType, "GUILD", 5))
            {
                if (playerActor.MyGuild == null)
                {
                    return null;
                }
                result = playerActor.MyGuild.DynamicVarList;
                sName = playerActor.MyGuild.GuildName;
            }
            else if (HUtil32.CompareLStr(sType, "GLOBAL", 6))
            {
                result = SystemShare.DynamicVarList;
                sName = "GLOBAL";
            }
            else if (HUtil32.CompareLStr(sType, "Account", 7))
            {
                result = playerActor.DynamicVarMap;
                sName = playerActor.UserAccount;
            }
            return result;
        }

        /// <summary>
        /// 取文本变量
        /// </summary>
        /// <returns></returns>
        protected VarInfo GetVarValue(IPlayerActor playerActor, string sData, ref int nValue)
        {
            string sVar = string.Empty;
            string sValue = string.Empty;
            return GetVarValue(playerActor, sData, ref sVar, ref sValue, ref nValue);
        }

        /// <summary>
        /// 取文本变量
        /// </summary>
        /// <returns></returns>
        protected VarInfo GetVarValue(IPlayerActor playerActor, string sData, ref string sValue)
        {
            string sVar = string.Empty;
            int nValue = 0;
            return GetVarValue(playerActor, sData, ref sVar, ref sValue, ref nValue);
        }

        protected bool GetValValue(IPlayerActor playerActor, string sMsg, ref string sValue)
        {
            if (string.IsNullOrEmpty(sMsg))
            {
                return false;
            }
            bool result = false;
            int n01 = SystemShare.GetValNameNo(sMsg);
            if (n01 >= 0)
            {
                if (HUtil32.RangeInDefined(n01, 600, 699))
                {
                    sValue = playerActor.MSString[n01 - 600];
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 700, 799))
                {
                    sValue = SystemShare.Config.GlobalAVal[n01 - 700];
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 1200, 1599))
                {
                    sValue = SystemShare.Config.GlobalAVal[n01 - 1100];// A变量(100-499)
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 1600, 1699))
                {
                    sValue = playerActor.ServerStrVal[n01 - 1600];
                    result = true;
                }
            }
            return result;
        }

        protected bool GetValValue(IPlayerActor playerActor, string sMsg, ref int nValue)
        {
            bool result = false;
            if (string.IsNullOrEmpty(sMsg))
            {
                return false;
            }
            int n01 = SystemShare.GetValNameNo(sMsg);
            if (n01 >= 0)
            {
                if (HUtil32.RangeInDefined(n01, 0, 99))
                {
                    nValue = playerActor.MNVal[n01];
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 100, 199))
                {
                    nValue = SystemShare.Config.GlobalVal[n01 - 100];
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 200, 299))
                {
                    nValue = playerActor.MDyVal[n01 - 200];
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 300, 399))
                {
                    nValue = playerActor.MNMval[n01 - 300];
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 400, 499))
                {
                    nValue = SystemShare.Config.GlobaDyMval[n01 - 400];
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 500, 599))
                {
                    nValue = playerActor.MNInteger[n01 - 500];
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 600, 699))
                {
                    nValue = HUtil32.StrToInt(playerActor.MSString[n01 - 600], 0);
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 700, 799))
                {
                    nValue = HUtil32.StrToInt(SystemShare.Config.GlobalAVal[n01 - 700], 0);
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 800, 1199))
                {
                    nValue = SystemShare.Config.GlobalVal[n01 - 700];
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 1200, 1599))
                {
                    nValue = HUtil32.StrToInt(SystemShare.Config.GlobalAVal[n01 - 1100], 0);
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 1600, 1699))
                {
                    nValue = HUtil32.StrToInt(playerActor.ServerStrVal[n01 - 1600], 0);
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 1700, 1799))
                {
                    nValue = playerActor.ServerIntVal[n01 - 1700];
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// 取文本变量
        /// </summary>
        /// <returns></returns>
        protected VarInfo GetVarValue(IPlayerActor playerActor, string sData, ref string sVar, ref string sValue, ref int nValue)
        {
            long n10;
            sVar = sData;
            sValue = sData;
            VarInfo result = new VarInfo { VarType = VarType.None, VarAttr = VarAttr.aNone };
            if (sData == "")
            {
                return result;
            }
            string sVarName = sData;
            string sName = sData;
            if (sData[0] == '<' && sData[^1] == '>')// <$STR(S0)>
            {
                sData = HUtil32.ArrestStringEx(sData, "<", ">", ref sName);
            }
            if (HUtil32.CompareLStr(sName, "$STR("))// $STR(S0)
            {
                sVar = '<' + sName + '>';
                result.VarType = GetValNameValue(playerActor, sVar, ref sValue, ref nValue);
                result.VarAttr = VarAttr.aFixStr;
            }
            else if (HUtil32.CompareLStr(sName, "$HUMAN("))
            {
                sVar = '<' + sName + '>';
                result.VarType = GetDynamicValue(playerActor, sVar, ref sValue, ref nValue);
                result.VarAttr = VarAttr.aDynamic;
            }
            else if (HUtil32.CompareLStr(sName, "$GUILD("))
            {
                sVar = '<' + sName + '>';
                result.VarType = GetDynamicValue(playerActor, sVar, ref sValue, ref nValue);
                result.VarAttr = VarAttr.aDynamic;
            }
            else if (HUtil32.CompareLStr(sName, "$GLOBAL("))
            {
                sVar = '<' + sName + '>';
                result.VarType = GetDynamicValue(playerActor, sVar, ref sValue, ref nValue);
                result.VarAttr = VarAttr.aDynamic;
            }
            else if (sName[0] == '$')
            {
                sName = sName[1..^0];
                n10 = SystemShare.GetValNameNo(sName);
                if (n10 >= 0)
                {
                    sVar = "<$STR(" + sName + ")>";
                    result.VarType = GetValNameValue(playerActor, sVar, ref sValue, ref nValue);
                    result.VarAttr = VarAttr.aFixStr;
                }
                else
                {
                    sVar = "<$" + sName + '>';
                    sValue = GetLineVariableText(playerActor, sVar);
                    if (string.Compare(sValue, sVar, StringComparison.Ordinal) == 0)
                    {
                        sValue = sVarName;
                        nValue = HUtil32.StrToInt(sVarName, 0);
                    }
                    else
                    {
                        result.VarType = VarType.String;
                        nValue = HUtil32.StrToInt(sValue, 0);
                        if (HUtil32.IsStringNumber(sValue))
                        {
                            result.VarType = VarType.Integer;
                        }
                    }
                    result.VarAttr = VarAttr.aConst;
                }
            }
            else
            {
                n10 = SystemShare.GetValNameNo(sName);
                if (n10 >= 0)
                {
                    sVar = "<$STR(" + sName + ")>";
                    result.VarType = GetValNameValue(playerActor, sVar, ref sValue, ref nValue);
                    result.VarAttr = VarAttr.aFixStr;
                }
                else
                {
                    result.VarType = VarType.String;
                    nValue = HUtil32.StrToInt(sValue, 0);
                    if (HUtil32.IsStringNumber(sValue))
                    {
                        result.VarType = VarType.Integer;
                    }
                    result.VarAttr = VarAttr.aConst;
                }
            }
            return result;
        }

        private VarType GetDynamicValue(IPlayerActor playerActor, string sVar, ref string sValue, ref int nValue)
        {
            string sVarName = "";
            string sVarType = "";
            string sData = sVar;
            string sName = "";
            VarType result = VarType.None;
            if (sData[0] == '<' && sData[^1] == '>')
            {
                sData = HUtil32.ArrestStringEx(sData, "<", ">", ref sName);
            }
            if (HUtil32.CompareLStr(sName, "$HUMAN("))
            {
                sData = HUtil32.ArrestStringEx(sName, "(", ")", ref sVarName);
                sVarType = "HUMAN";
            }
            else if (HUtil32.CompareLStr(sName, "$GUILD("))
            {
                sData = HUtil32.ArrestStringEx(sName, "(", ")", ref sVarName);
                sVarType = "GUILD";
            }
            else if (HUtil32.CompareLStr(sName, "$GLOBAL("))
            {
                sData = HUtil32.ArrestStringEx(sName, "(", ")", ref sVarName);
                sVarType = "GLOBAL";
            }
            if (sVarName == "" || sVarType == "")
            {
                return result;
            }
            Dictionary<string, DynamicVar> DynamicVarList = GeDynamicVarList(playerActor, sVarType, ref sName);
            if (DynamicVarList == null)
            {
                return result;
            }
            if (DynamicVarList.TryGetValue(sVarName, out DynamicVar DynamicVar))
            {
                if (string.Compare(DynamicVar.Name, sVarName, StringComparison.Ordinal) == 0)
                {
                    switch (DynamicVar.VarType)
                    {
                        case VarType.Integer:
                            nValue = DynamicVar.Internet;
                            sValue = nValue.ToString();
                            result = VarType.Integer;
                            break;

                        case VarType.String:
                            sValue = DynamicVar.String;
                            nValue = HUtil32.StrToInt(sValue, 0);
                            result = VarType.String;
                            break;
                    }
                }
            }
            return result;
        }

        private VarType GetValNameValue(IPlayerActor playerActor, string sVar, ref string sValue, ref int nValue)
        {
            VarType result = VarType.None;
            string sName = string.Empty;
            if (sVar == "")
            {
                return result;
            }
            string sData = sVar;
            if (sData[0] == '<' && sData[^1] == '>')
            {
                sData = HUtil32.ArrestStringEx(sData, "<", ">", ref sName); // <$STR(S0)>
            }
            if (HUtil32.CompareLStr(sName, "$STR("))
            {
                sData = HUtil32.ArrestStringEx(sName, "(", ")", ref sName); // $STR(S0)
            }
            if (sName[0] == '$')
            {
                sName = sName[1..];// $S0
            }
            int n01 = SystemShare.GetValNameNo(sName);
            if (n01 >= 0)
            {
                if (HUtil32.RangeInDefined(n01, 0, 99))
                {
                    nValue = SystemShare.Config.GlobalVal[n01];// G
                    sValue = nValue.ToString();
                    result = VarType.Integer;
                }
                else if (HUtil32.RangeInDefined(n01, 1000, 1099))
                {
                    nValue = SystemShare.Config.GlobaDyMval[n01 - 1000];// I
                    sValue = nValue.ToString();
                    result = VarType.Integer;
                }
                else if (HUtil32.RangeInDefined(n01, 1100, 1109))
                {
                    nValue = playerActor.MNVal[n01 - 1100];// P
                    sValue = nValue.ToString();
                    result = VarType.Integer;
                }
                else if (HUtil32.RangeInDefined(n01, 1110, 1119))
                {
                    nValue = playerActor.MDyVal[n01 - 1110];// D
                    sValue = nValue.ToString();
                    result = VarType.Integer;
                }
                else if (HUtil32.RangeInDefined(n01, 1200, 1299))
                {
                    nValue = playerActor.MNMval[n01 - 1200];// M
                    sValue = nValue.ToString();
                    result = VarType.Integer;
                }
                else if (HUtil32.RangeInDefined(n01, 1300, 1399))
                {
                    nValue = playerActor.MNInteger[n01 - 1300];// N
                    sValue = nValue.ToString();
                    result = VarType.Integer;
                }
                else if (HUtil32.RangeInDefined(n01, 2000, 2499))
                {
                    sValue = SystemShare.Config.GlobalAVal[n01 - 2000];// A
                    nValue = HUtil32.StrToInt(sValue, 0);
                    result = VarType.String;
                }
                else if (HUtil32.RangeInDefined(n01, 1400, 1499))
                {
                    sValue = playerActor.MSString[n01 - 1400];// S
                    nValue = HUtil32.StrToInt(sValue, 0);
                    result = VarType.String;
                }
            }
            else if (sName != "" && char.ToUpper(sName[0]) == 'S')
            {
                if (playerActor.m_StringList.ContainsKey(sName))
                {
                    sValue = playerActor.m_StringList[sName];
                }
                else
                {
                    playerActor.m_StringList.Add(sName, "");
                    sValue = "";
                }
                result = VarType.String;
            }
            else if (sName != "" && char.ToUpper(sName[0]) == 'N')
            {
                if (playerActor.IntegerList.ContainsKey(sName))
                {
                    nValue = playerActor.IntegerList[sName];
                }
                else
                {
                    nValue = 0;
                    playerActor.IntegerList.Add(sName, nValue);
                }
                result = VarType.Integer;
            }
            return result;
        }

        public string GetLineVariableText(IPlayerActor playerActor, string sMsg)
        {
            int nC = 0;
            string sText = string.Empty;
            string tempstr = sMsg;
            while (true)
            {
                if (tempstr.IndexOf('>') <= 0)
                {
                    break;
                }
                tempstr = HUtil32.ArrestStringEx(tempstr, "<", ">", ref sText);
                if (!string.IsNullOrEmpty(sText) && sText[0] == '$')
                {
                    GetVariableText(playerActor, ref sMsg, sText);
                }
                nC++;
                if (nC >= 101)
                {
                    break;
                }
            }
            return sMsg;
        }

        public virtual void GetVariableText(IPlayerActor playerActor, ref string sMsg, string sVariable)
        {
            string dynamicName = string.Empty;
            DynamicVar DynamicVar;
            bool boFoundVar;
            if (HUtil32.IsStringNumber(sVariable))//检查发送字符串是否有数字
            {
                string sIdx = sVariable[1..];
                int nIdx = HUtil32.StrToInt(sIdx, -1);
                if (nIdx == -1)
                {
                    return;
                }
                GrobalVarProcessingSys.Handler(playerActor, nIdx, sVariable, ref sMsg);
                return;
            }

            // 个人信息
            if (sVariable == "$CMD_ATTACKMODE")
            {
                //  sMsg = CombineStr(sMsg, "<$CMD_ATTACKMODE>", CommandMgr.GameCommands.AttackMode.CmdName);
                return;
            }
            if (sVariable == "$CMD_REST")
            {
                // sMsg = CombineStr(sMsg, "<$CMD_REST>", CommandMgr.GameCommands.Rest.CmdName);
                return;
            }
            if (sVariable == "$CMD_UNLOCK")
            {
                // sMsg = CombineStr(sMsg, "<$CMD_UNLOCK>", CommandMgr.GameCommands.Unlock.CmdName);
                return;
            }

            if (HUtil32.CompareLStr(sVariable, "$HUMAN("))
            {
                HUtil32.ArrestStringEx(sVariable, "(", ")", ref dynamicName);
                boFoundVar = false;
                if (playerActor.DynamicVarMap.TryGetValue(dynamicName, out DynamicVar))
                {
                    if (string.Compare(DynamicVar.Name, dynamicName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        switch (DynamicVar.VarType)
                        {
                            case VarType.Integer:
                                sMsg = CombineStr(sMsg, '<' + sVariable + '>', DynamicVar.Internet.ToString());
                                boFoundVar = true;
                                break;

                            case VarType.String:
                                sMsg = CombineStr(sMsg, '<' + sVariable + '>', DynamicVar.String);
                                boFoundVar = true;
                                break;
                        }
                    }
                }
                if (!boFoundVar)
                {
                    sMsg = "??";
                }
                return;
            }
            if (HUtil32.CompareLStr(sVariable, "$GUILD("))
            {
                if (playerActor.MyGuild == null)
                {
                    return;
                }
                HUtil32.ArrestStringEx(sVariable, "(", ")", ref dynamicName);
                boFoundVar = false;
                if (playerActor.MyGuild.DynamicVarList.TryGetValue(dynamicName, out DynamicVar))
                {
                    if (string.Compare(DynamicVar.Name, dynamicName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        switch (DynamicVar.VarType)
                        {
                            case VarType.Integer:
                                sMsg = CombineStr(sMsg, '<' + sVariable + '>', DynamicVar.Internet.ToString());
                                boFoundVar = true;
                                break;

                            case VarType.String:
                                sMsg = CombineStr(sMsg, '<' + sVariable + '>', DynamicVar.String);
                                boFoundVar = true;
                                break;
                        }
                    }
                }
                if (!boFoundVar)
                {
                    sMsg = "??";
                }
                return;
            }
            if (HUtil32.CompareLStr(sVariable, "$GLOBAL("))
            {
                HUtil32.ArrestStringEx(sVariable, "(", ")", ref dynamicName);
                boFoundVar = false;
                if (SystemShare.DynamicVarList.TryGetValue(dynamicName, out DynamicVar))
                {
                    if (string.Compare(DynamicVar.Name, dynamicName, StringComparison.Ordinal) == 0)
                    {
                        switch (DynamicVar.VarType)
                        {
                            case VarType.Integer:
                                sMsg = CombineStr(sMsg, '<' + sVariable + '>', DynamicVar.Internet.ToString());
                                boFoundVar = true;
                                break;
                            case VarType.String:
                                sMsg = CombineStr(sMsg, '<' + sVariable + '>', DynamicVar.String);
                                boFoundVar = true;
                                break;
                        }
                    }
                }
                if (!boFoundVar)
                {
                    sMsg = "??";
                }
                return;
            }
            if (HUtil32.CompareLStr(sVariable, "$STR("))
            {
                HUtil32.ArrestStringEx(sVariable, "(", ")", ref dynamicName);
                int n18 = SystemShare.GetValNameNo(dynamicName);
                if (n18 >= 0)
                {
                    if (HUtil32.RangeInDefined(n18, 0, 499))
                    {
                        sMsg = CombineStr(sMsg, '<' + sVariable + '>', SystemShare.Config.GlobalVal[n18].ToString());
                    }
                    else if (HUtil32.RangeInDefined(n18, 1100, 1109))
                    {
                        sMsg = CombineStr(sMsg, '<' + sVariable + '>', playerActor.MNVal[n18 - 1100].ToString());
                    }
                    else if (HUtil32.RangeInDefined(n18, 1110, 1119))
                    {
                        sMsg = CombineStr(sMsg, '<' + sVariable + '>', playerActor.MDyVal[n18 - 1110].ToString());
                    }
                    else if (HUtil32.RangeInDefined(n18, 1200, 1299))
                    {
                        sMsg = CombineStr(sMsg, '<' + sVariable + '>', playerActor.MNMval[n18 - 1200].ToString());
                    }
                    else if (HUtil32.RangeInDefined(n18, 1000, 1099))
                    {
                        sMsg = CombineStr(sMsg, '<' + sVariable + '>', SystemShare.Config.GlobaDyMval[n18 - 1000].ToString());
                    }
                    else if (HUtil32.RangeInDefined(n18, 1300, 1399))
                    {
                        sMsg = CombineStr(sMsg, '<' + sVariable + '>', playerActor.MNInteger[n18 - 1300].ToString());
                    }
                    else if (HUtil32.RangeInDefined(n18, 1400, 1499))
                    {
                        sMsg = CombineStr(sMsg, '<' + sVariable + '>', playerActor.MSString[n18 - 1400]);
                    }
                    else if (HUtil32.RangeInDefined(n18, 2000, 2499))
                    {
                        sMsg = CombineStr(sMsg, '<' + sVariable + '>', SystemShare.Config.GlobalAVal[n18 - 2000]);
                    }
                }
                else if (dynamicName != "" && char.ToUpper(dynamicName[1]) == 'S')
                {
                    sMsg = CombineStr(sMsg, '<' + sVariable + '>', playerActor.m_StringList.ContainsKey(dynamicName) ? playerActor.m_StringList[dynamicName] : "");
                }
                else if (dynamicName != "" && char.ToUpper(dynamicName[1]) == 'N')
                {
                    sMsg = CombineStr(sMsg, '<' + sVariable + '>', playerActor.IntegerList.ContainsKey(dynamicName) ? Convert.ToString(playerActor.IntegerList[dynamicName]) : "-1");
                }
            }
        }

        protected bool SetDynamicValue(IPlayerActor playerActor, string sVar, string sValue, int nValue)
        {
            bool result = false;
            string sVarName = "";
            string sVarType = "";
            string sName = "";
            string sData = sVar;
            if (sData[0] == '<' && sData[^1] == '>')
            {
                sData = HUtil32.ArrestStringEx(sData, "<", ">", ref sName);
            }
            if (HUtil32.CompareLStr(sName, "$HUMAN("))
            {
                sData = HUtil32.ArrestStringEx(sName, "(", ")", ref sVarName);
                sVarType = "HUMAN";
            }
            else if (HUtil32.CompareLStr(sName, "$GUILD("))
            {
                sData = HUtil32.ArrestStringEx(sName, "(", ")", ref sVarName);
                sVarType = "GUILD";
            }
            else if (HUtil32.CompareLStr(sName, "$GLOBAL("))
            {
                sData = HUtil32.ArrestStringEx(sName, "(", ")", ref sVarName);
                sVarType = "GLOBAL";
            }
            if (sVarName == "" || sVarType == "")
            {
                return false;
            }
            Dictionary<string, DynamicVar> dynamicVarList = GeDynamicVarList(playerActor, sVarType, ref sName);
            if (dynamicVarList == null)
            {
                return false;
            }
            if (dynamicVarList.TryGetValue(sVarName, out DynamicVar dynamicVar))
            {
                switch (dynamicVar.VarType)
                {
                    case VarType.Integer:
                        dynamicVar.Internet = nValue;
                        break;

                    case VarType.String:
                        dynamicVar.String = sValue;
                        break;
                }
                result = true;
            }
            return result;
        }

        protected bool SetValNameValue(IPlayerActor playerActor, string sVar, string sValue, int nValue)
        {
            string sName = string.Empty;
            bool result = false;
            if (sVar == "")
            {
                return false;
            }
            string sData = sVar;
            if (sData[0] == '<' && sData[^1] == '>') // <$STR(S0)>
            {
                sData = HUtil32.ArrestStringEx(sData, "<", ">", ref sName);
            }
            if (HUtil32.CompareLStr(sName, "$STR("))// $STR(S0)
            {
                sData = HUtil32.ArrestStringEx(sName, "(", ")", ref sName);
            }
            if (sName[0] == '$')// $S0
            {
                sName = sName[1..];
            }
            int n01 = SystemShare.GetValNameNo(sName);
            if (n01 >= 0)
            {
                if (HUtil32.RangeInDefined(n01, 0, 499))
                {
                    SystemShare.Config.GlobalVal[n01] = nValue;// G
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 1000, 1099))
                {
                    SystemShare.Config.GlobaDyMval[n01 - 1000] = nValue;// I
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 1100, 1109))
                {
                    playerActor.MNVal[n01 - 1100] = nValue;// P
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 1110, 1119))
                {
                    playerActor.MDyVal[n01 - 1110] = nValue;// D
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 1200, 1299))
                {
                    playerActor.MNMval[n01 - 1200] = nValue;// M
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 1300, 1399))
                {
                    playerActor.MNInteger[n01 - 1300] = nValue;// N
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 2000, 2499))
                {
                    SystemShare.Config.GlobalAVal[n01 - 2000] = sValue;// A
                    result = true;
                }
                else if (HUtil32.RangeInDefined(n01, 1400, 1499))
                {
                    playerActor.MSString[n01 - 1400] = sValue;// S
                    result = true;
                }
            }
            else if (sName != "" && char.ToUpper(sName[0]) == 'S')
            {
                if (playerActor.m_StringList.ContainsKey(sName))
                {
                    playerActor.m_StringList[sName] = sValue;
                    result = true;
                }
                else
                {
                    playerActor.m_StringList.Add(sName, sValue);
                    result = true;
                }
            }
            else if (sName != "" && char.ToUpper(sName[0]) == 'N')
            {
                if (playerActor.IntegerList.ContainsKey(sName))
                {
                    playerActor.IntegerList[sName] = nValue;
                    result = true;
                }
                else
                {
                    playerActor.IntegerList.Add(sName, nValue);
                    result = true;
                }
            }
            return result;
        }

        protected Dictionary<string, DynamicVar> GeDynamicVarList(IPlayerActor playerActor, string sType, ref string sName)
        {
            Dictionary<string, DynamicVar> result = null;
            if (HUtil32.CompareLStr(sType, "HUMAN"))
            {
                result = playerActor.DynamicVarMap;
                sName = playerActor.ChrName;
            }
            else if (HUtil32.CompareLStr(sType, "GUILD"))
            {
                if (playerActor.MyGuild == null)
                {
                    return null;
                }
                result = playerActor.MyGuild.DynamicVarList;
                sName = playerActor.MyGuild.GuildName;
            }
            else if (HUtil32.CompareLStr(sType, "GLOBAL"))
            {
                result = SystemShare.DynamicVarList;
                sName = "GLOBAL";
            }
            return result;
        }

        /// <summary>
        /// 合并字符串
        /// </summary>
        /// <returns></returns>
        private string CombineStr(string sMsg, string variable, string variableVal)
        {
            string result;
            int varIndex = sMsg.IndexOf(variable, StringComparison.Ordinal);
            if (varIndex > -1)
            {
                string s14 = sMsg[..varIndex];
                string s18 = sMsg[(variable.Length + varIndex)..];
                result = s14 + variableVal + s18;
            }
            else
            {
                result = sMsg;
            }
            return result;
        }

        protected bool GotoLableCheckStringList(string sHumName, string sListFileName)
        {
            bool result = false;
            sListFileName = SystemShare.GetEnvirFilePath(sListFileName);
            if (File.Exists(sListFileName))
            {
                StringList LoadList = new StringList();
                try
                {
                    LoadList.LoadFromFile(sListFileName);
                }
                catch
                {
                    LogService.Error("loading fail.... => " + sListFileName);
                }
                for (int i = 0; i < LoadList.Count; i++)
                {
                    if (string.Compare(LoadList[i].Trim(), sHumName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        result = true;
                        break;
                    }
                }
                LoadList = null;
            }
            else
            {
                LogService.Error("file not found => " + sListFileName);
            }
            return result;
        }

        private static string ReplaceVariableText(string sMsg, string sStr, string sText)
        {
            int n10 = sMsg.IndexOf(sStr, StringComparison.OrdinalIgnoreCase);
            if (n10 > -1)
            {
                ReadOnlySpan<char> s18 = sMsg.AsSpan()[(sStr.Length + n10)..sMsg.Length];
                StringBuilder builder = new StringBuilder();
                builder.Append(sMsg[..n10]);
                builder.Append(sText);
                builder.Append(s18);
                return builder.ToString();
            }
            return sMsg;
        }
    }
}