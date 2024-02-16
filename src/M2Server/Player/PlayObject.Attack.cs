using M2Server.Magic;
using OpenMir2;
using OpenMir2.Consts;
using OpenMir2.Data;
using OpenMir2.Enums;
using OpenMir2.Packets.ClientPackets;
using SystemModule;
using SystemModule.Actors;
using SystemModule.Const;
using SystemModule.Enums;

namespace M2Server.Player
{
    public partial class PlayObject
    {
        /// <summary>
        /// 计算施法魔法值
        /// </summary>
        internal static ushort GetMagicSpell(UserMagic userMagic)
        {
            return (ushort)HUtil32.Round(userMagic.Magic.Spell / 4.0 * (userMagic.Level + 1));
        }

        protected void AttackDir(IActor targetObject, short wHitMode, byte nDir)
        {
            IActor attackTarget = targetObject ?? GetPoseCreate();
            if (UseItems[ItemLocation.Weapon] != null && (UseItems[ItemLocation.Weapon].Index > 0) && UseItems[ItemLocation.Weapon].Desc[ItemAttr.WeaponUpgrade] > 0)
            {
                if (attackTarget != null)
                {
                    CheckWeaponUpgrade();
                }
            }

            switch (wHitMode)
            {
                case 5 when (MagicArr[MagicConst.SKILL_BANWOL] != null):
                    if (WAbil.MP > 0)
                    {
                        DamageSpell((ushort)(MagicArr[MagicConst.SKILL_BANWOL].Magic.DefSpell + GetMagicSpell(MagicArr[MagicConst.SKILL_BANWOL])));
                        HealthSpellChanged();
                    }
                    else
                    {
                        wHitMode = Messages.RM_HIT;
                    }
                    break;
                case 8 when (MagicArr[MagicConst.SKILL_CROSSMOON] != null):
                    if (WAbil.MP > 0)
                    {
                        DamageSpell((ushort)(MagicArr[MagicConst.SKILL_CROSSMOON].Magic.DefSpell + GetMagicSpell(MagicArr[MagicConst.SKILL_CROSSMOON])));
                        HealthSpellChanged();
                    }
                    else
                    {
                        wHitMode = Messages.RM_HIT;
                    }
                    break;
                case 12 when (MagicArr[MagicConst.SKILL_REDBANWOL] != null):
                    if (WAbil.MP > 0)
                    {
                        DamageSpell((ushort)(MagicArr[MagicConst.SKILL_REDBANWOL].Magic.DefSpell + GetMagicSpell(MagicArr[MagicConst.SKILL_REDBANWOL])));
                        HealthSpellChanged();
                    }
                    else
                    {
                        wHitMode = Messages.RM_HIT;
                    }
                    break;
            }

            bool canHit = false;
            int nPower = GetAttackPowerHit(wHitMode, GetBaseAttackPoewr(), attackTarget, ref canHit);
            SkillAttackDamage(wHitMode, nPower);
            AttackDir(attackTarget, nPower, nDir);
            SendAttackMsg(GetHitMode(wHitMode), Dir, CurrX, CurrY);
            AttackSuccess(wHitMode, nPower, canHit, attackTarget);
        }

        private int GetHitMode(short wHitMode)
        {
            int wIdent = Messages.RM_HIT;
            switch (wHitMode)
            {
                case 0:
                    wIdent = Messages.RM_HIT;
                    break;
                case 1:
                    wIdent = Messages.RM_HEAVYHIT;
                    break;
                case 2:
                    wIdent = Messages.RM_BIGHIT;
                    break;
                case 3:
                    if (PowerHit)
                    {
                        wIdent = Messages.RM_SPELL2;
                    }
                    break;
                case 4:
                    if (MagicArr[MagicConst.SKILL_ERGUM] != null)
                    {
                        wIdent = Messages.RM_LONGHIT;
                    }
                    break;
                case 5:
                    if (MagicArr[MagicConst.SKILL_BANWOL] != null)
                    {
                        wIdent = Messages.RM_WIDEHIT;
                    }
                    break;
                case 7:
                    if (FireHitSkill)
                    {
                        wIdent = Messages.RM_FIREHIT;
                    }
                    break;
                case 8:
                    if (MagicArr[MagicConst.SKILL_CROSSMOON] != null)
                    {
                        wIdent = Messages.RM_CRSHIT;
                    }
                    break;
                case 9:
                    if (TwinHitSkill)
                    {
                        wIdent = Messages.RM_TWINHIT;
                    }
                    break;
                case 12:
                    if (MagicArr[MagicConst.SKILL_REDBANWOL] != null)
                    {
                        wIdent = Messages.RM_WIDEHIT;
                    }
                    break;
            }
            return wIdent;
        }

        private void SkillAttackDamage(short wHitMode, int nPower)
        {
            int nSecPwr = 0;
            if (wHitMode > 0)
            {
                switch (wHitMode)
                {
                    case 4:// 刺杀
                        if (MagicArr[MagicConst.SKILL_ERGUM] != null)
                        {
                            nSecPwr = HUtil32.Round(nPower / (MagicArr[MagicConst.SKILL_ERGUM].Magic.TrainLv + 2.0) * (MagicArr[MagicConst.SKILL_ERGUM].Level + 2.0));
                        }
                        if (nSecPwr > 0)
                        {
                            if (!SwordLongAttack(ref nSecPwr) && SystemShare.Config.LimitSwordLong)
                            {
                                wHitMode = 0;
                            }
                        }
                        break;
                    case 5:
                        {
                            if (MagicArr[MagicConst.SKILL_BANWOL] != null)
                            {
                                nSecPwr = HUtil32.Round(nPower / (MagicArr[MagicConst.SKILL_BANWOL].Magic.TrainLv + 10.0) * (MagicArr[MagicConst.SKILL_BANWOL].Level + 2.0));
                            }
                            if (nSecPwr > 0)
                            {
                                SwordWideAttack(ref nSecPwr);
                            }
                            break;
                        }
                    case 12:
                        {
                            if (MagicArr[MagicConst.SKILL_REDBANWOL] != null)
                            {
                                nSecPwr = HUtil32.Round(nPower / (MagicArr[MagicConst.SKILL_REDBANWOL].Magic.TrainLv + 10.0) * (MagicArr[MagicConst.SKILL_REDBANWOL].Level + 2.0));
                            }
                            if (nSecPwr > 0)
                            {
                                SwordWideAttack(ref nSecPwr);
                            }
                            break;
                        }
                    case 8:
                        {
                            if (MagicArr[MagicConst.SKILL_CROSSMOON] != null)
                            {
                                nSecPwr = HUtil32.Round(nPower / (MagicArr[MagicConst.SKILL_CROSSMOON].Magic.TrainLv + 10.0) * (MagicArr[MagicConst.SKILL_CROSSMOON].Level + 2.0));
                            }
                            if (nSecPwr > 0)
                            {
                                CrsWideAttack(nSecPwr);
                            }
                            break;
                        }
                }
            }
        }

        private void CheckSkillProficiency(int nTranPoint, UserMagic userMagic)
        {
            if (userMagic != null)
            {
                if ((userMagic.Level < 3) && (userMagic.Magic.TrainLevel[userMagic.Level] <= Abil.Level))
                {
                    TrainSkill(userMagic, nTranPoint);
                    if (!CheckMagicLevelUp(userMagic))
                    {
                        SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, userMagic.Magic.MagicId, userMagic.Level, userMagic.TranPoint, "", 3000);
                    }
                }
            }
        }

        private void AttackSuccess(short wHitMode, int nPower, bool canHit, IActor targetObject)
        {
            if (targetObject == null)
            {
                return;
            }
            if (!targetObject.UnParalysis && Paralysis && (M2Share.RandomNumber.Random(targetObject.AntiPoison + SystemShare.Config.AttackPosionRate) == 0))
            {
                targetObject.MakePosion(PoisonState.STONE, SystemShare.Config.AttackPosionTime, 0);
            }
            ushort nWeaponDamage = (ushort)(M2Share.RandomNumber.Random(5) + 2 - AddAbil.WeaponStrong);
            if ((nWeaponDamage > 0) && (UseItems[ItemLocation.Weapon] != null) && (UseItems[ItemLocation.Weapon].Index > 0))
            {
                DoDamageWeapon(nWeaponDamage);
            }
            if (SuckupEnemyHealthRate > 0)// 虹魔，吸血
            {
                SuckupEnemyHealth = nPower / 100.0 * SuckupEnemyHealthRate;
                if (SuckupEnemyHealth >= 2.0)
                {
                    ushort n20 = Convert.ToUInt16(SuckupEnemyHealth);
                    SuckupEnemyHealth = n20;
                    DamageHealth((ushort)-n20);
                }
            }
            UserMagic attackMagic;
            if (MagicArr[MagicConst.SKILL_ILKWANG] != null)
            {
                attackMagic = GetAttackMagic(MagicConst.SKILL_ILKWANG);
                if (attackMagic.Level < 3)
                {
                    CheckSkillProficiency(M2Share.RandomNumber.Random(3) + 1, attackMagic);
                }
            }
            if (canHit && (MagicArr[MagicConst.SKILL_YEDO] != null))
            {
                attackMagic = GetAttackMagic(MagicConst.SKILL_YEDO);
                if (attackMagic.Level < 3)
                {
                    CheckSkillProficiency(M2Share.RandomNumber.Random(3) + 1, attackMagic);
                }
            }
            switch (wHitMode)
            {
                case 4:
                    attackMagic = GetAttackMagic(MagicConst.SKILL_ERGUM);
                    CheckSkillProficiency(1, attackMagic);
                    break;
                case 5:
                    attackMagic = GetAttackMagic(MagicConst.SKILL_BANWOL);
                    CheckSkillProficiency(1, attackMagic);
                    break;
                case 12:
                    attackMagic = GetAttackMagic(MagicConst.SKILL_REDBANWOL);
                    CheckSkillProficiency(1, attackMagic);
                    break;
                case 7:
                    attackMagic = GetAttackMagic(MagicConst.SKILL_FIRESWORD);
                    CheckSkillProficiency(1, attackMagic);
                    break;
                case 8:
                    attackMagic = GetAttackMagic(MagicConst.SKILL_CROSSMOON);
                    CheckSkillProficiency(1, attackMagic);
                    break;
                case 9:
                    attackMagic = GetAttackMagic(MagicConst.SKILL_TWINBLADE);
                    CheckSkillProficiency(1, attackMagic);
                    break;
            }
            if (SystemShare.Config.MonDelHptoExp)
            {
                switch (Race)
                {
                    case ActorRace.Play:
                        if (IsRobot)
                        {
                            //if (((IRobotPlayer)this).Abil.Level <= SystemShare.Config.MonHptoExpLevel)
                            //{
                            //    if (!M2Share.GetNoHptoexpMonList(targetObject.ChrName))
                            //    {
                            //        ((IRobotPlayer)this).GainExp(nPower * SystemShare.Config.MonHptoExpmax);
                            //    }
                            //}
                        }
                        else
                        {
                            if (Abil.Level <= SystemShare.Config.MonHptoExpLevel)
                            {
                                if (!M2Share.GetNoHptoexpMonList(targetObject.ChrName))
                                {
                                    GainExp(nPower * SystemShare.Config.MonHptoExpmax);
                                }
                            }
                        }
                        break;
                    case ActorRace.PlayClone:
                        if (Master != null)
                        {
                            if (Master.IsRobot)
                            {
                                //if (((IRobotPlayer)Master).Abil.Level <= SystemShare.Config.MonHptoExpLevel)
                                //{
                                //    if (!M2Share.GetNoHptoexpMonList(targetObject.ChrName))
                                //    {
                                //        ((IRobotPlayer)Master).GainExp(nPower * SystemShare.Config.MonHptoExpmax);
                                //    }
                                //}
                            }
                            else
                            {
                                if (((IPlayerActor)Master).Abil.Level <= SystemShare.Config.MonHptoExpLevel)
                                {
                                    if (!M2Share.GetNoHptoexpMonList(targetObject.ChrName))
                                    {
                                        ((IPlayerActor)Master).GainExp(nPower * SystemShare.Config.MonHptoExpmax);
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            TrainCurrentSkill(wHitMode);
        }

        public bool IsTrainingSkill(int nIndex)
        {
            for (int i = 0; i < MagicList.Count; i++)
            {
                UserMagic userMagic = MagicList[i];
                if ((userMagic != null) && (userMagic.MagIdx == nIndex))
                {
                    return true;
                }
            }
            return false;
        }

        private void TrainCurrentSkill(int wHitMode)
        {
            if (Race != ActorRace.Play)
            {
                return;
            }
            int nCLevel = Abil.Level;
            if ((MagicArr[MagicConst.SKILL_ONESWORD] != null))
            {
                if ((MagicArr[MagicConst.SKILL_ONESWORD].Level < MagicArr[MagicConst.SKILL_ONESWORD].Magic.TrainLv) && (MagicArr[MagicConst.SKILL_ONESWORD].Magic.TrainLevel[MagicArr[MagicConst.SKILL_ONESWORD].Level] <= nCLevel))
                {
                    TrainSkill(MagicArr[MagicConst.SKILL_ONESWORD], M2Share.RandomNumber.Random(3) + 1);
                    if (!CheckMagicLevelUp(MagicArr[MagicConst.SKILL_ONESWORD]))
                    {
                        SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[MagicConst.SKILL_ONESWORD].Magic.MagicId, MagicArr[MagicConst.SKILL_ONESWORD].Level, MagicArr[MagicConst.SKILL_ONESWORD].TranPoint, "", 3000);
                    }
                }
            }
            if ((MagicArr[MagicConst.SKILL_ILKWANG] != null))
            {
                if ((MagicArr[MagicConst.SKILL_ILKWANG].Level < MagicArr[MagicConst.SKILL_ILKWANG].Magic.TrainLv) && (MagicArr[MagicConst.SKILL_ILKWANG].Magic.TrainLevel[MagicArr[MagicConst.SKILL_ILKWANG].Level] <= nCLevel))
                {
                    TrainSkill(MagicArr[MagicConst.SKILL_ILKWANG], M2Share.RandomNumber.Random(3) + 1);
                    if (!CheckMagicLevelUp(MagicArr[MagicConst.SKILL_ILKWANG]))
                    {
                        SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[MagicConst.SKILL_ILKWANG].Magic.MagicId, MagicArr[MagicConst.SKILL_ILKWANG].Level, MagicArr[MagicConst.SKILL_ILKWANG].TranPoint, "", 3000);
                    }
                }
            }
            switch (wHitMode)
            {
                case 3 when (MagicArr[MagicConst.SKILL_YEDO] != null):
                    {
                        if ((MagicArr[MagicConst.SKILL_YEDO].Level < MagicArr[MagicConst.SKILL_YEDO].Magic.TrainLv) && (MagicArr[MagicConst.SKILL_YEDO].Magic.TrainLevel[MagicArr[MagicConst.SKILL_YEDO].Level] <= nCLevel))
                        {
                            TrainSkill(MagicArr[MagicConst.SKILL_YEDO], M2Share.RandomNumber.Random(3) + 1);
                            if (!CheckMagicLevelUp(MagicArr[MagicConst.SKILL_YEDO]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[MagicConst.SKILL_YEDO].Magic.MagicId, MagicArr[MagicConst.SKILL_YEDO].Level, MagicArr[MagicConst.SKILL_YEDO].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 4 when (MagicArr[MagicConst.SKILL_ERGUM] != null):
                    {
                        if ((MagicArr[MagicConst.SKILL_ERGUM].Level < MagicArr[MagicConst.SKILL_ERGUM].Magic.TrainLv) && (MagicArr[MagicConst.SKILL_ERGUM].Magic.TrainLevel[MagicArr[MagicConst.SKILL_ERGUM].Level] <= nCLevel))
                        {
                            TrainSkill(MagicArr[MagicConst.SKILL_ERGUM], 1);
                            if (!CheckMagicLevelUp(MagicArr[MagicConst.SKILL_ERGUM]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[MagicConst.SKILL_ERGUM].Magic.MagicId, MagicArr[MagicConst.SKILL_ERGUM].Level, MagicArr[MagicConst.SKILL_ERGUM].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 5 when (MagicArr[MagicConst.SKILL_BANWOL] != null):
                    {
                        if ((MagicArr[MagicConst.SKILL_BANWOL].Level < MagicArr[MagicConst.SKILL_BANWOL].Magic.TrainLv) && (MagicArr[MagicConst.SKILL_BANWOL].Magic.TrainLevel[MagicArr[MagicConst.SKILL_BANWOL].Level] <= nCLevel))
                        {
                            TrainSkill(MagicArr[MagicConst.SKILL_BANWOL], 1);
                            if (!CheckMagicLevelUp(MagicArr[MagicConst.SKILL_BANWOL]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[MagicConst.SKILL_BANWOL].Magic.MagicId, MagicArr[MagicConst.SKILL_BANWOL].Level, MagicArr[MagicConst.SKILL_BANWOL].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 7 when (MagicArr[MagicConst.SKILL_FIRESWORD] != null):
                    {
                        if ((MagicArr[MagicConst.SKILL_FIRESWORD].Level < MagicArr[MagicConst.SKILL_FIRESWORD].Magic.TrainLv) && (MagicArr[MagicConst.SKILL_FIRESWORD].Magic.TrainLevel[MagicArr[MagicConst.SKILL_FIRESWORD].Level] <= nCLevel))
                        {
                            TrainSkill(MagicArr[MagicConst.SKILL_FIRESWORD], 1);
                            if (!CheckMagicLevelUp(MagicArr[MagicConst.SKILL_FIRESWORD]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[MagicConst.SKILL_FIRESWORD].Magic.MagicId, MagicArr[MagicConst.SKILL_FIRESWORD].Level, MagicArr[MagicConst.SKILL_FIRESWORD].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 9 when (MagicArr[43] != null):
                    {
                        if ((MagicArr[43].Level < MagicArr[43].Magic.TrainLv) && (MagicArr[43].Magic.TrainLevel[MagicArr[43].Level] <= nCLevel))
                        {
                            TrainSkill(MagicArr[43], 1);
                            if (!CheckMagicLevelUp(MagicArr[43]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[43].Magic.MagicId, MagicArr[43].Level, MagicArr[43].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 13 when (MagicArr[56] != null):
                    {
                        if ((MagicArr[56].Level < MagicArr[56].Magic.TrainLv) && (MagicArr[56].Magic.TrainLevel[MagicArr[56].Level] <= nCLevel))
                        {
                            TrainSkill(MagicArr[56], 1);
                            if (!CheckMagicLevelUp(MagicArr[56]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[56].Magic.MagicId, MagicArr[56].Level, MagicArr[56].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 8 when (MagicArr[40] != null):
                    {
                        if ((MagicArr[40].Level < MagicArr[40].Magic.TrainLv) && (MagicArr[40].Magic.TrainLevel[MagicArr[40].Level] <= nCLevel))
                        {
                            TrainSkill(MagicArr[40], 1);
                            if (!CheckMagicLevelUp(MagicArr[40]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[40].Magic.MagicId, MagicArr[40].Level, MagicArr[40].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 10 when (MagicArr[42] != null):
                    {
                        if ((MagicArr[42].Level < MagicArr[42].Magic.TrainLv) && (MagicArr[42].Magic.TrainLevel[MagicArr[42].Level] <= nCLevel))
                        {
                            TrainSkill(MagicArr[42], 1);
                            if (!CheckMagicLevelUp(MagicArr[42]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[42].Magic.MagicId, MagicArr[42].Level, MagicArr[42].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 12 when (MagicArr[66] != null):
                    {
                        if ((MagicArr[66].Level < MagicArr[66].Magic.TrainLv) && (MagicArr[66].Magic.TrainLevel[MagicArr[66].Level] <= nCLevel))
                        {
                            TrainSkill(MagicArr[66], 1);
                            if (!CheckMagicLevelUp(MagicArr[66]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[66].Magic.MagicId, MagicArr[66].Level, MagicArr[66].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 61 when (MagicArr[61] != null):
                    {
                        if ((MagicArr[61].Level < MagicArr[61].Magic.TrainLv) && (MagicArr[61].Magic.TrainLevel[MagicArr[61].Level] <= nCLevel))
                        {
                            TrainSkill(MagicArr[61], 1);
                            if (!CheckMagicLevelUp(MagicArr[61]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[61].Magic.MagicId, MagicArr[61].Level, MagicArr[61].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 20 when (MagicArr[101] != null):
                    {
                        if (MagicArr[101].Magic.TrainLevel[MagicArr[101].Level] <= nCLevel)
                        {
                            TrainSkill(MagicArr[101], 1);
                            if (!CheckMagicLevelUp(MagicArr[101]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[101].Magic.MagicId, MagicArr[101].Level, MagicArr[101].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 21 when (MagicArr[102] != null):
                    {
                        if (MagicArr[102].Magic.TrainLevel[MagicArr[102].Level] <= nCLevel)
                        {
                            TrainSkill(MagicArr[102], 1);
                            if (!CheckMagicLevelUp(MagicArr[102]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[102].Magic.MagicId, MagicArr[102].Level, MagicArr[102].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 22 when (MagicArr[103] != null):
                    {
                        if (MagicArr[103].Magic.TrainLevel[MagicArr[103].Level] <= nCLevel)
                        {
                            TrainSkill(MagicArr[103], 1);
                            if (!CheckMagicLevelUp(MagicArr[103]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[103].Magic.MagicId, MagicArr[103].Level, MagicArr[103].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 23 when (MagicArr[114] != null):
                    {
                        if (MagicArr[114].Magic.TrainLevel[MagicArr[114].Level] <= nCLevel)
                        {
                            TrainSkill(MagicArr[114], 1);
                            if (!CheckMagicLevelUp(MagicArr[114]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[114].Magic.MagicId, MagicArr[114].Level, MagicArr[114].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 24 when (MagicArr[113] != null):
                    {
                        if (MagicArr[113].Magic.TrainLevel[MagicArr[113].Level] <= nCLevel)
                        {
                            TrainSkill(MagicArr[113], 1);
                            if (!CheckMagicLevelUp(MagicArr[113]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[113].Magic.MagicId, MagicArr[113].Level, MagicArr[113].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
                case 25 when (MagicArr[115] != null):
                    {
                        if (MagicArr[115].Magic.TrainLevel[MagicArr[115].Level] <= nCLevel)
                        {
                            TrainSkill(MagicArr[115], 1);
                            if (!CheckMagicLevelUp(MagicArr[115]))
                            {
                                SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, MagicArr[115].Magic.MagicId, MagicArr[115].Level, MagicArr[115].TranPoint, "", 3000);
                            }
                        }
                        break;
                    }
            }
        }

        protected int GetAttackPowerHit(short wHitMode, int nPower, IActor targetObject, ref bool canHit)
        {
            canHit = false;
            if (targetObject != null)
            {
                switch (wHitMode)
                {
                    case 3 when PowerHit:
                        PowerHit = false;
                        nPower += HitPlus;
                        canHit = true;
                        break;
                    case 7 when FireHitSkill:// 烈火剑法
                        FireHitSkill = false;
                        LatestFireHitTick = HUtil32.GetTickCount();// 禁止双烈火
                        nPower = (ushort)(nPower + HUtil32.Round(nPower / 100 * HitDouble * 10.0));
                        canHit = true;
                        break;
                    case 9 when TwinHitSkill:// 烈火剑法
                        TwinHitSkill = false;
                        LatestTwinHitTick = HUtil32.GetTickCount();// 禁止双烈火
                        nPower = (ushort)(nPower + HUtil32.Round(nPower / 100 * HitDouble * 10.0));
                        canHit = true;
                        break;
                }
            }
            else
            {
                switch (wHitMode)
                {
                    case 3 when PowerHit:
                        PowerHit = false;
                        nPower += HitPlus;
                        canHit = true;
                        break;
                    case 7 when FireHitSkill:
                        FireHitSkill = false;
                        LatestFireHitTick = HUtil32.GetTickCount();// 禁止双烈火
                        break;
                    case 9 when TwinHitSkill:
                        TwinHitSkill = false;
                        LatestTwinHitTick = HUtil32.GetTickCount();// 禁止双烈火
                        break;
                }
            }
            return nPower;
        }

        protected override bool IsAttackTarget(IActor targetObject)
        {
            bool result = false;
            switch (AttatckMode)
            {
                case AttackMode.HAM_ALL:
                    if ((targetObject.Race < ActorRace.NPC) || (targetObject.Race > ActorRace.PeaceNpc))
                    {
                        result = true;
                    }
                    if (SystemShare.Config.PveServer)
                    {
                        return true;
                    }
                    break;
                case AttackMode.HAM_PEACE:
                    if (targetObject.Race >= ActorRace.Animal)
                    {
                        return true;
                    }
                    break;
                case AttackMode.HAM_DEAR:
                    if (targetObject.ActorId != DearHuman)
                    {
                        return true;
                    }
                    break;
                case AttackMode.HAM_MASTER:
                    if (targetObject.Race == ActorRace.Play)
                    {
                        result = true;
                        if (IsMaster)
                        {
                            for (int i = 0; i < MasterList.Count; i++)
                            {
                                if (MasterList[i] == targetObject)
                                {
                                    result = false;
                                    break;
                                }
                            }
                        }
                        if (((IPlayerActor)targetObject).IsMaster)
                        {
                            for (int i = 0; i < ((IPlayerActor)targetObject).MasterList.Count; i++)
                            {
                                if (((IPlayerActor)targetObject).MasterList[i] == this)
                                {
                                    result = false;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        result = true;
                    }
                    break;
                case AttackMode.HAM_GROUP:
                    if ((targetObject.Race < ActorRace.NPC) || (targetObject.Race > ActorRace.PeaceNpc))
                    {
                        result = true;
                    }
                    if (targetObject.Race == ActorRace.Play)
                    {
                        if (IsGroupMember(targetObject))
                        {
                            result = false;
                        }
                    }
                    if (SystemShare.Config.PveServer)
                    {
                        return true;
                    }
                    break;
                case AttackMode.HAM_GUILD:
                    if ((targetObject.Race < ActorRace.NPC) || (targetObject.Race > ActorRace.PeaceNpc))
                    {
                        result = true;
                    }
                    if (targetObject.Race == ActorRace.Play)
                    {
                        if (MyGuild != null)
                        {
                            if (MyGuild.IsMember(targetObject.ChrName))
                            {
                                result = false;
                            }
                            if (GuildWarArea && (((IPlayerActor)targetObject).MyGuild != null))
                            {
                                if (MyGuild.IsAllyGuild(((IPlayerActor)targetObject).MyGuild))
                                {
                                    result = false;
                                }
                            }
                        }
                    }
                    if (SystemShare.Config.PveServer)
                    {
                        result = true;
                    }
                    break;
                case AttackMode.HAM_PKATTACK:
                    if ((targetObject.Race < ActorRace.NPC) || (targetObject.Race > ActorRace.PeaceNpc))
                    {
                        result = true;
                    }
                    if (targetObject.Race == ActorRace.Play)
                    {
                        if (PvpLevel() >= 2)
                        {
                            result = ((IPlayerActor)targetObject).PvpLevel() < 2;
                        }
                        else
                        {
                            result = ((IPlayerActor)targetObject).PvpLevel() >= 2;
                        }
                    }
                    if (SystemShare.Config.PveServer)
                    {
                        result = true;
                    }
                    break;
            }
            return result;
        }

        public override bool IsProperFriend(IActor targetObject)
        {
            if (targetObject.Race == ActorRace.Play)
            {
                bool result = IsProperIsFriend(targetObject);
                if (targetObject.Race < ActorRace.Animal)
                {
                    return result;
                }
                if (targetObject.Master == this)
                {
                    return true;
                }
                if (targetObject.Master != null)
                {
                    return IsProperIsFriend(targetObject.Master);
                }
                if (targetObject.Race > ActorRace.Play)
                {
                    return result;
                }

                IPlayerActor playObject = (IPlayerActor)targetObject;
                if (!playObject.InGuildWarArea)
                {
                    if (SystemShare.Config.boPKLevelProtect)// 新人保护
                    {
                        if (Abil.Level > SystemShare.Config.nPKProtectLevel)// 如果大于指定等级
                        {
                            if (!playObject.PvpFlag && targetObject.WAbil.Level <= SystemShare.Config.nPKProtectLevel && playObject.PvpLevel() < 2)// 被攻击的人物小指定等级没有红名，则不可以攻击。
                            {
                                return false;
                            }
                        }
                        if (Abil.Level <= SystemShare.Config.nPKProtectLevel)// 如果小于指定等级
                        {
                            if (!playObject.PvpFlag && targetObject.WAbil.Level > SystemShare.Config.nPKProtectLevel && playObject.PvpLevel() < 2)
                            {
                                return false;
                            }
                        }
                    }
                    // 大于指定级别的红名人物不可以杀指定级别未红名的人物。
                    if (PvpLevel() >= 2 && Abil.Level > SystemShare.Config.nRedPKProtectLevel)
                    {
                        if (targetObject.Abil.Level <= SystemShare.Config.nRedPKProtectLevel && playObject.PvpLevel() < 2)
                        {
                            return false;
                        }
                    }
                    // 小于指定级别的非红名人物不可以杀指定级别红名人物。
                    if (Abil.Level <= SystemShare.Config.nRedPKProtectLevel && PvpLevel() < 2)
                    {
                        if (playObject.PvpLevel() >= 2 && targetObject.Abil.Level > SystemShare.Config.nRedPKProtectLevel)
                        {
                            return false;
                        }
                    }
                    if (((HUtil32.GetTickCount() - MapMoveTick) < 3000) || ((HUtil32.GetTickCount() - playObject.MapMoveTick) < 3000))
                    {
                        result = false;
                    }
                }
                return result;
            }
            return base.IsProperFriend(targetObject);
        }

        private bool ClientHitXY(int wIdent, int nX, int nY, byte nDir, bool boLateDelivery, ref int delayTime)
        {
            bool result = false;
            short n14 = 0;
            short n18 = 0;
            const string sExceptionMsg = "[Exception] PlayObject::ClientHitXY";
            delayTime = 0;
            try
            {
                if (!IsCanHit)
                {
                    return false;
                }
                if (Death || StatusTimeArr[PoisonState.STONE] != 0)// 防麻
                {
                    return false;
                }
                if (!SystemShare.Config.CloseSpeedHackCheck)
                {
                    if (!boLateDelivery)
                    {
                        if (!CheckActionStatus(wIdent, ref delayTime))
                        {
                            IsFilterAction = false;
                            return false;
                        }
                        IsFilterAction = true;
                        int dwAttackTime = HUtil32._MAX(0, SystemShare.Config.HitIntervalTime - HitSpeed * SystemShare.Config.ItemSpeed);
                        int dwCheckTime = HUtil32.GetTickCount() - AttackTick;
                        if (dwCheckTime < dwAttackTime)
                        {
                            AttackCount++;
                            delayTime = dwAttackTime - dwCheckTime;
                            if (delayTime > SystemShare.Config.DropOverSpeed)
                            {
                                if (AttackCount >= 4)
                                {
                                    AttackTick = HUtil32.GetTickCount();
                                    AttackCount = 0;
                                    delayTime = SystemShare.Config.DropOverSpeed;
                                    if (TestSpeedMode)
                                    {
                                        SysMsg($"攻击忙!!!{delayTime}", MsgColor.Red, MsgType.Hint);
                                    }
                                }
                                else
                                {
                                    AttackCount = 0;
                                }
                                return false;
                            }
                            if (TestSpeedMode)
                            {
                                SysMsg($"攻击步忙!!!{delayTime}", MsgColor.Red, MsgType.Hint);
                            }
                            return false;
                        }

                    }
                }
                if (nX == CurrX && nY == CurrY)
                {
                    result = true;
                    AttackTick = HUtil32.GetTickCount();
                    if (wIdent == Messages.CM_HEAVYHIT && UseItems[ItemLocation.Weapon] != null && UseItems[ItemLocation.Weapon].Dura > 0)// 挖矿
                    {
                        if (GetFrontPosition(ref n14, ref n18) && !Envir.CanWalk(n14, n18, false))
                        {
                            StdItem StdItem = SystemShare.EquipmentSystem.GetStdItem(UseItems[ItemLocation.Weapon].Index);
                            if (StdItem != null && StdItem.Shape == 19)
                            {
                                if (PileStones(n14, n18))
                                {
                                    SendSocket("=DIG");
                                }
                                HealthTick -= 30;
                                SpellTick -= 50;
                                SpellTick = HUtil32._MAX(0, SpellTick);
                                PerHealth -= 2;
                                PerSpell -= 2;
                                return true;
                            }
                        }
                    }
                    switch (wIdent)
                    {
                        case Messages.CM_HIT:
                            AttackDir(null, 0, nDir);
                            break;
                        case Messages.CM_HEAVYHIT:
                            AttackDir(null, 1, nDir);
                            break;
                        case Messages.CM_BIGHIT:
                            AttackDir(null, 2, nDir);
                            break;
                        case Messages.CM_POWERHIT:
                            AttackDir(null, 3, nDir);
                            break;
                        case Messages.CM_LONGHIT:
                            AttackDir(null, 4, nDir);
                            break;
                        case Messages.CM_WIDEHIT:
                            AttackDir(null, 5, nDir);
                            break;
                        case Messages.CM_FIREHIT:
                            AttackDir(null, 7, nDir);
                            break;
                        case Messages.CM_CRSHIT:
                            AttackDir(null, 8, nDir);
                            break;
                        case Messages.CM_TWINHIT:
                            AttackDir(null, 9, nDir);
                            break;
                        case Messages.CM_42HIT:
                            AttackDir(null, 10, nDir);
                            AttackDir(null, 11, nDir);
                            break;
                    }
                    if (MagicArr[MagicConst.SKILL_YEDO] != null && UseItems[ItemLocation.Weapon].Dura > 0)
                    {
                        AttackSkillCount -= 1;
                        if (AttackSkillPointCount == AttackSkillCount)
                        {
                            PowerHit = true;
                            SendSocket("+PWR");
                        }
                        if (AttackSkillCount <= 0)
                        {
                            AttackSkillCount = (byte)(7 - MagicArr[MagicConst.SKILL_YEDO].Level);
                            AttackSkillPointCount = M2Share.RandomNumber.RandomByte(AttackSkillCount);
                        }
                    }
                    HealthTick -= 30;
                    SpellTick -= 100;
                    SpellTick = HUtil32._MAX(0, SpellTick);
                    PerHealth -= 2;
                    PerSpell -= 2;
                }
            }
            catch (Exception e)
            {
                LogService.Error(sExceptionMsg);
                LogService.Error(e.StackTrace);
            }
            return result;
        }

        private bool ClientHorseRunXY(int wIdent, short nX, short nY, bool boLateDelivery, ref int delayTime)
        {
            bool result = false;
            byte n14;
            int dwCheckTime;
            delayTime = 0;
            if (!IsCanRun)
            {
                return result;
            }
            if (Death || StatusTimeArr[PoisonState.STONE] != 0)// 防麻
            {
                return result;
            }
            if (!SystemShare.Config.CloseSpeedHackCheck)
            {
                if (!boLateDelivery)
                {
                    if (!CheckActionStatus(wIdent, ref delayTime))
                    {
                        IsFilterAction = false;
                        return result;
                    }
                    IsFilterAction = true;
                    dwCheckTime = HUtil32.GetTickCount() - MoveTick;
                    if (dwCheckTime < SystemShare.Config.RunIntervalTime)
                    {
                        MoveCount++;
                        delayTime = SystemShare.Config.RunIntervalTime - dwCheckTime;
                        if (delayTime > SystemShare.Config.DropOverSpeed)
                        {
                            if (MoveCount >= 4)
                            {
                                MoveTick = HUtil32.GetTickCount();
                                MoveCount = 0;
                                delayTime = SystemShare.Config.DropOverSpeed;
                                if (TestSpeedMode)
                                {
                                    SysMsg("马跑步忙复位!!!" + delayTime, MsgColor.Red, MsgType.Hint);
                                }
                            }
                            else
                            {
                                MoveCount = 0;
                            }
                            return result;
                        }
                        else
                        {
                            if (TestSpeedMode)
                            {
                                SysMsg("马跑步忙!!!" + delayTime, MsgColor.Red, MsgType.Hint);
                            }
                            return result;
                        }
                    }
                }
            }
            MoveTick = HUtil32.GetTickCount();
            SpaceMoved = false;
            n14 = M2Share.GetNextDirection(CurrX, CurrY, nX, nY);
            if (HorseRunTo(n14, false))
            {
                if (Transparent && HideMode)
                {
                    StatusTimeArr[PoisonState.STATETRANSPARENT] = 1;
                }
                if (SpaceMoved || CurrX == nX && CurrY == nY)
                {
                    result = true;
                }
                HealthTick -= 60;
                SpellTick -= 10;
                SpellTick = HUtil32._MAX(0, SpellTick);
                PerHealth -= 1;
                PerSpell -= 1;
            }
            else
            {
                MoveCount = 0;
            }
            return result;
        }

        private bool ClientSpellXY(int wIdent, int nKey, short nTargetX, short nTargetY, IActor targetObject, bool boLateDelivery, ref int delayTime)
        {
            delayTime = 0;
            if (!IsCanSpell)
            {
                return false;
            }
            if (Death || StatusTimeArr[PoisonState.STONE] != 0)// 防麻
            {
                return false;
            }
            UserMagic UserMagic = GetMagicInfo(nKey);
            if (UserMagic == null)
            {
                return false;
            }
            bool boIsWarrSkill = MagicManager.IsWarrSkill(UserMagic.MagIdx);
            if (!boLateDelivery && !boIsWarrSkill && (!SystemShare.Config.CloseSpeedHackCheck))
            {
                if (!CheckActionStatus(wIdent, ref delayTime))
                {
                    IsFilterAction = false;
                    return false;
                }
                IsFilterAction = true;
                int dwCheckTime = HUtil32.GetTickCount() - MagicAttackTick;
                if (dwCheckTime < MagicAttackInterval)
                {
                    MagicAttackCount++;
                    delayTime = MagicAttackInterval - dwCheckTime;
                    if (delayTime > SystemShare.Config.MagicHitIntervalTime / 3)
                    {
                        if (MagicAttackCount >= 4)
                        {
                            MagicAttackTick = HUtil32.GetTickCount();
                            MagicAttackCount = 0;
                            delayTime = SystemShare.Config.MagicHitIntervalTime / 3;
                            if (TestSpeedMode)
                            {
                                SysMsg("魔法忙复位!!!" + delayTime, MsgColor.Red, MsgType.Hint);
                            }
                        }
                        else
                        {
                            MagicAttackCount = 0;
                        }
                        return false;
                    }
                    if (TestSpeedMode)
                    {
                        SysMsg("魔法忙!!!" + delayTime, MsgColor.Red, MsgType.Hint);
                    }
                    return false;
                }
            }
            SpellTick -= 450;
            SpellTick = HUtil32._MAX(0, SpellTick);
            if (!boIsWarrSkill)
            {
                MagicAttackInterval = UserMagic.Magic.DelayTime + SystemShare.Config.MagicHitIntervalTime;
            }
            MagicAttackTick = HUtil32.GetTickCount();
            ushort nSpellPoint;
            bool result;
            switch (UserMagic.MagIdx)
            {
                case MagicConst.SKILL_ERGUM:
                    if (MagicArr[MagicConst.SKILL_ERGUM] != null)
                    {
                        if (!UseThrusting)
                        {
                            ThrustingOnOff(true);
                            SendSocket("+LNG");
                        }
                        else
                        {
                            ThrustingOnOff(false);
                            SendSocket("+ULNG");
                        }
                    }
                    result = true;
                    break;
                case MagicConst.SKILL_BANWOL:
                    if (MagicArr[MagicConst.SKILL_BANWOL] != null)
                    {
                        if (!UseHalfMoon)
                        {
                            HalfMoonOnOff(true);
                            SendSocket("+WID");
                        }
                        else
                        {
                            HalfMoonOnOff(false);
                            SendSocket("+UWID");
                        }
                    }
                    result = true;
                    break;
                case MagicConst.SKILL_REDBANWOL:
                    if (MagicArr[MagicConst.SKILL_REDBANWOL] != null)
                    {
                        if (!RedUseHalfMoon)
                        {
                            RedHalfMoonOnOff(true);
                            SendSocket("+WID");
                        }
                        else
                        {
                            RedHalfMoonOnOff(false);
                            SendSocket("+UWID");
                        }
                    }
                    result = true;
                    break;
                case MagicConst.SKILL_FIRESWORD:
                    if (MagicArr[MagicConst.SKILL_FIRESWORD] != null)
                    {
                        if (AllowFireHitSkill())
                        {
                            nSpellPoint = GetSpellPoint(UserMagic);
                            if (WAbil.MP >= nSpellPoint)
                            {
                                if (nSpellPoint > 0)
                                {
                                    DamageSpell(nSpellPoint);
                                    HealthSpellChanged();
                                }
                                SendSocket("+FIR");
                            }
                        }
                    }
                    result = true;
                    break;
                case MagicConst.SKILL_MOOTEBO:
                    result = true;
                    if ((HUtil32.GetTickCount() - DoMotaeboTick) > 3 * 1000)
                    {
                        DoMotaeboTick = HUtil32.GetTickCount();
                        Dir = (byte)nTargetX;
                        nSpellPoint = GetSpellPoint(UserMagic);
                        if (WAbil.MP >= nSpellPoint)
                        {
                            if (nSpellPoint > 0)
                            {
                                DamageSpell(nSpellPoint);
                                HealthSpellChanged();
                            }
                            if (DoMotaebo(Dir, UserMagic.Level))
                            {
                                if (UserMagic.Level < 3)
                                {
                                    if (UserMagic.Magic.TrainLevel[UserMagic.Level] < Abil.Level)
                                    {
                                        TrainSkill(UserMagic, M2Share.RandomNumber.Random(3) + 1);
                                        if (!CheckMagicLevelUp(UserMagic))
                                        {
                                            SendSelfDelayMsg(Messages.RM_MAGIC_LVEXP, 0, UserMagic.Magic.MagicId, UserMagic.Level, UserMagic.TranPoint, "", 1000);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                case MagicConst.SKILL_CROSSMOON:
                    if (MagicArr[MagicConst.SKILL_CROSSMOON] != null)
                    {
                        if (!CrsHitkill)
                        {
                            SkillCrsOnOff(true);
                            SendSocket("+CRS");
                        }
                        else
                        {
                            SkillCrsOnOff(false);
                            SendSocket("+UCRS");
                        }
                    }
                    result = true;
                    break;
                case MagicConst.SKILL_TWINBLADE:// 狂风斩
                    if (MagicArr[MagicConst.SKILL_TWINBLADE] != null)
                    {
                        if (AllowTwinHitSkill())
                        {
                            nSpellPoint = GetSpellPoint(UserMagic);
                            if (WAbil.MP >= nSpellPoint)
                            {
                                if (nSpellPoint > 0)
                                {
                                    DamageSpell(nSpellPoint);
                                    HealthSpellChanged();
                                }
                                SendSocket("+TWN");
                            }
                        }
                    }
                    result = true;
                    break;
                case MagicConst.SKILL_43:// 破空剑
                    if (MagicArr[MagicConst.SKILL_43] != null)
                    {
                        if (!MBo43Kill)
                        {
                            Skill43OnOff(true);
                            SendSocket("+CID");
                        }
                        else
                        {
                            Skill43OnOff(false);
                            SendSocket("+UCID");
                        }
                    }
                    result = true;
                    break;
                default:
                    Dir = M2Share.GetNextDirection(CurrX, CurrY, nTargetX, nTargetY); ;
                    IActor spellObject = null;
                    if (CretInNearXy(targetObject, nTargetX, nTargetY)) // 检查目标角色，与目标座标误差范围，如果在误差范围内则修正目标座标
                    {
                        spellObject = targetObject;
                        nTargetX = spellObject.CurrX;
                        nTargetY = spellObject.CurrY;
                    }
                    if (!DoSpell(UserMagic, nTargetX, nTargetY, spellObject))
                    {
                        SendRefMsg(Messages.RM_MAGICFIREFAIL, 0, 0, 0, 0, "");
                    }
                    result = true;
                    break;
            }
            return result;
        }

        private bool ClientRunXY(int wIdent, short nX, short nY, int nFlag, ref int delayTime)
        {
            bool result = false;
            byte nDir;
            delayTime = 0;
            if (!IsCanRun)
            {
                return false;
            }
            if (Death || StatusTimeArr[PoisonState.STONE] != 0)
            {
                return false;
            }
            if (nFlag != wIdent && (!SystemShare.Config.CloseSpeedHackCheck))
            {
                if (!CheckActionStatus(wIdent, ref delayTime))
                {
                    IsFilterAction = false;
                    return false;
                }
                IsFilterAction = true;
                int dwCheckTime = HUtil32.GetTickCount() - MoveTick;
                if (dwCheckTime < SystemShare.Config.RunIntervalTime)
                {
                    MoveCount++;
                    delayTime = SystemShare.Config.RunIntervalTime - dwCheckTime;
                    if (delayTime > SystemShare.Config.RunIntervalTime / 3)
                    {
                        if (MoveCount >= 4)
                        {
                            MoveTick = HUtil32.GetTickCount();
                            MoveCount = 0;
                            delayTime = SystemShare.Config.RunIntervalTime / 3;
                            if (TestSpeedMode)
                            {
                                SysMsg("跑步忙复位!!!" + delayTime, MsgColor.Red, MsgType.Hint);
                            }
                        }
                        else
                        {
                            MoveCount = 0;
                        }
                        return result;
                    }
                    if (TestSpeedMode)
                    {
                        SysMsg("跑步忙!!!" + delayTime, MsgColor.Red, MsgType.Hint);
                    }
                    return result;
                }
            }
            MoveTick = HUtil32.GetTickCount();
            SpaceMoved = false;
            nDir = M2Share.GetNextDirection(CurrX, CurrY, nX, nY);
            if (RunTo(nDir, false, nX, nY))
            {
                if (Transparent && HideMode)
                {
                    StatusTimeArr[PoisonState.STATETRANSPARENT] = 1;
                }
                if (SpaceMoved || CurrX == nX && CurrY == nY)
                {
                    result = true;
                }
                HealthTick -= 60;
                SpellTick -= 10;
                SpellTick = HUtil32._MAX(0, SpellTick);
                PerHealth -= 1;
                PerSpell -= 1;
            }
            else
            {
                MoveCount = 0;
            }
            return result;
        }

        private bool ClientWalkXY(int wIdent, short nX, short nY, bool boLateDelivery, ref int delayTime)
        {
            bool result = false;
            delayTime = 0;
            if (!IsCanWalk)
            {
                return false;
            }
            if (Death || StatusTimeArr[PoisonState.STONE] != 0)
            {
                return false; // 防麻
            }
            if (!boLateDelivery && (!SystemShare.Config.CloseSpeedHackCheck))
            {
                if (!CheckActionStatus(wIdent, ref delayTime))
                {
                    IsFilterAction = false;
                    return false;
                }
                IsFilterAction = true;
                int dwCheckTime = HUtil32.GetTickCount() - MoveTick;
                if (dwCheckTime < SystemShare.Config.WalkIntervalTime)
                {
                    MoveCount++;
                    delayTime = SystemShare.Config.WalkIntervalTime - dwCheckTime;
                    if (delayTime > SystemShare.Config.WalkIntervalTime / 3)
                    {
                        if (MoveCount >= 4)
                        {
                            MoveTick = HUtil32.GetTickCount();
                            MoveCount = 0;
                            delayTime = SystemShare.Config.WalkIntervalTime / 3;
                            if (TestSpeedMode)
                            {
                                SysMsg("走路忙复位!!!" + delayTime, MsgColor.Red, MsgType.Hint);
                            }
                        }
                        else
                        {
                            MoveCount = 0;
                        }
                        return false;
                    }
                    if (TestSpeedMode)
                    {
                        SysMsg("走路忙!!!" + delayTime, MsgColor.Red, MsgType.Hint);
                    }
                    return false;
                }
            }
            MoveTick = HUtil32.GetTickCount();
            SpaceMoved = false;
            byte nextDir = M2Share.GetNextDirection(CurrX, CurrY, nX, nY);
            if (WalkTo(nextDir, false))
            {
                if (SpaceMoved || CurrX == nX && CurrY == nY)
                {
                    result = true;
                }
                HealthTick -= 10;
            }
            else
            {
                MoveCount = 0;
            }
            return result;
        }

        /// <summary>
        /// 减少武器持久值
        /// </summary>
        protected void DoDamageWeapon(ushort nWeaponDamage)
        {
            if (UseItems[ItemLocation.Weapon] == null || UseItems[ItemLocation.Weapon].Index <= 0)
            {
                return;
            }
            ushort nDura = UseItems[ItemLocation.Weapon].Dura;
            int nDuraPoint = HUtil32.Round(nDura / 1.03);
            nDura -= nWeaponDamage;
            if (nDura <= 0)
            {
                nDura = 0;
                UseItems[ItemLocation.Weapon].Dura = nDura;
                if (Race == ActorRace.Play)
                {
                    this.SendDelItems(UseItems[ItemLocation.Weapon]);
                    StdItem stdItem = SystemShare.EquipmentSystem.GetStdItem(UseItems[ItemLocation.Weapon].Index);
                    if (stdItem.NeedIdentify == 1)
                    {
                        //M2Share.EventSource.AddEventLog(3, MapName + "\t" + CurrX + "\t" + CurrY + "\t" + ChrName + "\t" + stdItem.Name + "\t" +
                        //                                   UseItems[ItemLocation.Weapon].MakeIndex + "\t" + HUtil32.BoolToIntStr(Race == ActorRace.Play) + "\t" + '0');
                    }
                }
                UseItems[ItemLocation.Weapon].Index = 0;
                SendMsg(Messages.RM_DURACHANGE, ItemLocation.Weapon, nDura, UseItems[ItemLocation.Weapon].DuraMax, 0);
            }
            else
            {
                UseItems[ItemLocation.Weapon].Dura = nDura;
            }
            if (Math.Abs((nDura / 1.03)) != nDuraPoint)
            {
                SendMsg(Messages.RM_DURACHANGE, ItemLocation.Weapon, UseItems[ItemLocation.Weapon].Dura, UseItems[ItemLocation.Weapon].DuraMax, 0);
            }
        }
    }
}