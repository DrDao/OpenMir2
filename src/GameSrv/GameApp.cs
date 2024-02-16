﻿using CommandModule;
using GameSrv.DB;
using GameSrv.Maps;
using GameSrv.Robots;
using GameSrv.Services;
using GameSrv.Word;
using M2Server.Castle;
using M2Server.Event;
using M2Server.Guild;
using M2Server.Items;
using MediatR;
using ScriptSystem;
using System.Collections;

namespace GameSrv
{
    public class GameApp : ServerBase
    {
        public GameApp(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            M2Share.LogonCostLogList = new ArrayList();
            M2Share.CustomItemMgr = new CustomItemSystem();
            M2Share.MakeItemList = new Dictionary<string, IList<MakeItem>>(StringComparer.OrdinalIgnoreCase);
            M2Share.StartPointList = new List<StartPoint>();
            M2Share.ServerTableList = new TRouteInfo[20];
            M2Share.DenySayMsgList = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            M2Share.MiniMapList = new ConcurrentDictionary<string, short>(StringComparer.OrdinalIgnoreCase);
            //M2Share.QuestDiaryList = new List<IList<TQDDinfo>>();
            M2Share.AbuseTextList = new StringList();
            M2Share.SellOffItemList = new List<DealOffInfo>();
            M2Share.UnbindList = new Dictionary<int, string>();
            M2Share.LineNoticeList = new List<string>();
            M2Share.MonSayMsgList = new Dictionary<string, IList<MonsterSayMsg>>(StringComparer.OrdinalIgnoreCase);
            M2Share.DisableMakeItemList = new List<string>();
            M2Share.EnableMakeItemList = new List<string>();
            M2Share.DisableSellOffList = new List<string>();
            M2Share.DisableMoveMapList = new StringList();
            M2Share.DisableSendMsgList = new List<string>();
            M2Share.MonDropLimitLIst = new ConcurrentDictionary<string, MonsterLimitDrop>(StringComparer.OrdinalIgnoreCase);
            M2Share.DisableTakeOffList = new Dictionary<int, string>();
            M2Share.UnMasterList = new List<string>();
            M2Share.UnForceMasterList = new List<string>();
            M2Share.GameLogItemNameList = new List<string>();
            M2Share.DenyIPAddrList = new List<string>();
            M2Share.DenyChrNameList = new List<string>();
            M2Share.DenyAccountList = new List<string>();
            M2Share.NoClearMonLIst = new List<string>();
            M2Share.NoHptoexpMonLIst = new List<string>();
            M2Share.ItemBindIPaddr = new List<ItemBind>();
            M2Share.ItemBindAccount = new List<ItemBind>();
            M2Share.ItemBindChrName = new List<ItemBind>();
            M2Share.ProcessHumanCriticalSection = new object();
            M2Share.UserDBCriticalSection = new object();
            M2Share.DynamicVarList = new Dictionary<string, DynamicVar>(StringComparer.OrdinalIgnoreCase);
            M2Share.CommandSystem = new GameCommandSystem();
            M2Share.Authentication = new AuthenticationService();
            M2Share.ScriptEngine = new ScriptEngine();
            M2Share.AutoBot = new RobotManage();
            InitializeSystem(serviceProvider);
        }

        private static void InitializeSystem(IServiceProvider serviceProvider)
        {
            SystemShare.HumLimit = 30;
            SystemShare.MonLimit = 30;
            SystemShare.ZenLimit = 5;
            SystemShare.NpcLimit = 5;
            SystemShare.SocLimit = 10;
            SystemShare.DecLimit = 20;
            SystemShare.FilterWord = true;
            SystemShare.Config.LoadDBErrorCount = 0;
            SystemShare.Config.LoadDBCount = 0;
            SystemShare.Config.SaveDBCount = 0;
            SystemShare.Config.DBQueryID = 0;
            SystemShare.Config.ItemNumber = 0;
            SystemShare.Config.ItemNumberEx = int.MaxValue / 2;
            SystemShare.Config.WinLotteryCount = 0;
            SystemShare.Config.NoWinLotteryCount = 0;
            SystemShare.Config.WinLotteryLevel1 = 0;
            SystemShare.Config.WinLotteryLevel2 = 0;
            SystemShare.Config.WinLotteryLevel3 = 0;
            SystemShare.Config.WinLotteryLevel4 = 0;
            SystemShare.Config.WinLotteryLevel5 = 0;
            SystemShare.Config.WinLotteryLevel6 = 0;
            SystemShare.ManageNPC = new Merchant();
            SystemShare.RobotNPC = new Merchant();
            SystemShare.FunctionNPC = new Merchant();
            SystemShare.EquipmentSystem = new EquipmentSystem();
            SystemShare.GuildMgr = new GuildManager();
            SystemShare.CastleMgr = new CastleManager();
            SystemShare.MapMgr = new MapManager();
            SystemShare.EventMgr = new EventManager();
            SystemShare.WorldEngine = new WorldServer();
            SystemShare.ServiceProvider = serviceProvider;
            SystemShare.Mediator = serviceProvider.GetService<IMediator>();
        }

        /// <summary>
        /// 初始化游戏基础配置
        /// </summary>
        /// <param name="stoppingToken"></param>
        public void Initialize(CancellationToken stoppingToken)
        {
            M2Share.CommandSystem.RegisterCommand();
            M2Share.LoadGameLogItemNameList();
            M2Share.LoadDenyIPAddrList();
            M2Share.LoadDenyAccountList();
            M2Share.LoadDenyChrNameList();
            M2Share.LoadNoClearMonList();
            LogService.Info("正在加载物品数据库...");
            int nCode = GameShare.DataSource.LoadItemsDB();
            if (nCode < 0)
            {
                LogService.Info($"物品数据库加载失败!!! Code: {nCode}");
                return;
            }
            LogService.Info($"物品数据库加载成功...[{SystemShare.EquipmentSystem.ItemCount}]");
            nCode = Map.LoadMinMap();
            if (nCode < 0)
            {
                LogService.Info($"小地图数据加载失败!!! Code: {nCode}");
                return;
            }
            nCode = Map.LoadMapInfo();
            if (nCode < 0)
            {
                LogService.Info($"地图数据加载失败!!! Code: {nCode}");
                return;
            }
            LogService.Info("正在加载怪物数据库...");
            nCode = GameShare.DataSource.LoadMonsterDB();
            if (nCode < 0)
            {
                LogService.Info($"加载怪物数据库失败!!! Code: {nCode}");
                return;
            }
            LogService.Info($"加载怪物数据库成功...[{SystemShare.WorldEngine.MonsterCount}]");
            LogService.Info("正在加载技能数据库...");
            nCode = GameShare.DataSource.LoadMagicDB();
            if (nCode < 0)
            {
                LogService.Info($"加载技能数据库失败!!! Code: {nCode}");
                return;
            }
            LogService.Info($"加载技能数据库成功...[{SystemShare.WorldEngine.MagicCount}]");
            LogService.Info("正在加载怪物刷新配置信息...");
            nCode = GameShare.LocalDb.LoadMonGen(out int mongenCount);
            if (nCode < 0)
            {
                LogService.Info($"加载怪物刷新配置信息失败!!! Code: {nCode}");
                return;
            }
            LogService.Info($"加载怪物刷新配置信息成功...[{mongenCount}]");
            LogService.Info("初始化怪物处理线程...");
            SystemShare.WorldEngine.InitializeMonster();
            LogService.Info("初始化怪物处理完成...");
            LogService.Info("正加载怪物说话配置信息...");
            M2Share.LoadMonSayMsg();
            LogService.Info($"加载怪物说话配置信息成功...[{M2Share.MonSayMsgList.Count}]");
            M2Share.LoadDisableTakeOffList();
            M2Share.LoadMonDropLimitList();
            M2Share.LoadDisableMakeItem();
            M2Share.LoadEnableMakeItem();
            M2Share.LoadAllowSellOffItem();
            M2Share.LoadDisableMoveMap();
            M2Share.CustomItemMgr.LoadCustomItemName();
            M2Share.LoadDisableSendMsgList();
            M2Share.LoadItemBindIPaddr();
            M2Share.LoadItemBindAccount();
            M2Share.LoadItemBindChrName();
            M2Share.LoadUnMasterList();
            M2Share.LoadUnForceMasterList();
            LogService.Info("正在加载捆装物品信息...");
            nCode = GameShare.LocalDb.LoadUnbindList();
            if (nCode < 0)
            {
                LogService.Info($"加载捆装物品信息失败!!! Code: {nCode}");
                return;
            }
            LogService.Info("加载捆装物品信息成功...");
            LogService.Info("加载物品寄售系统...");
            GameShare.DataSource.LoadSellOffItemList();
            LogService.Info("正在加载任务地图信息...");
            nCode = GameShare.LocalDb.LoadMapQuest();
            if (nCode < 0)
            {
                LogService.Info("加载任务地图信息失败!!!");
                return;
            }
            LogService.Info("加载任务地图信息成功...");
            LogService.Info("正在加载任务说明信息...");
            nCode = GameShare.LocalDb.LoadQuestDiary();
            if (nCode < 0)
            {
                LogService.Info("加载任务说明信息失败!!!");
                return;
            }
            LogService.Info("加载任务说明信息成功...");
            if (LoadAbuseInformation(".\\!abuse.txt"))
            {
                LogService.Info("加载文字过滤信息成功...");
            }
            LogService.Info("正在加载公告提示信息...");
            if (!M2Share.LoadLineNotice(SystemShare.GetNoticeFilePath("LineNotice.txt")))
            {
                LogService.Info("加载公告提示信息失败!!!");
            }
            LogService.Info("加载公告提示信息成功...");
            LocalDb.LoadAdminList();
            LogService.Info("管理员列表加载成功...");
            M2Share.NetChannel.Initialize();
            LogService.Info("正在初始化网络服务...");
            SystemShare.CastleMgr.Initialize();
            GameShare.DataServer.Initialize();
            M2Share.Authentication.Initialize();
        }

        /// <summary>
        /// 初始化游戏世界服务
        /// </summary>
        /// <param name="stoppingToken"></param>
        public void InitializeWorld(CancellationToken stoppingToken)
        {
            try
            {
                SystemShare.MapMgr.LoadMapDoor();
                GameShare.LocalDb.LoadMerchant();
                LogService.Info("交易NPC列表加载成功...");
                GameShare.LocalDb.LoadNpcs();
                LogService.Info("管理NPC列表加载成功...");
                GameShare.LocalDb.LoadMakeItem();
                LogService.Info("炼制物品信息加载成功...");
                GameShare.LocalDb.LoadStartPoint();
                LogService.Info("回城点配置加载成功...");
                LogService.Info("正在初始安全区光圈...");
                SystemShare.MapMgr.MakeSafePkZone();
                LogService.Info("安全区光圈初始化成功...");
                SystemShare.WorldEngine.InitializationMonsterThread();
                if (!SystemShare.Config.VentureServer)
                {
                    LocalDb.LoadGuardList();
                    LogService.Info("守卫列表加载成功...");
                }
                SystemShare.WorldEngine.Initialize();
                SystemShare.GuildMgr.LoadGuildInfo();
                SystemShare.CastleMgr.LoadCastleList();
                M2Share.AutoBot.Initialize();
                M2Share.StartReady = true;
                LogService.Info("游戏处理引擎初始化成功...");
            }
            catch (Exception ex)
            {
                LogService.Error(ex);
            }
        }

        /// <summary>
        /// 读取服务器列表
        /// </summary>
        public void LoadServerTable()
        {
            int nRouteIdx = 0;
            string sIdx = string.Empty;
            string sSelGateIPaddr = string.Empty;
            string sGameGateIPaddr = string.Empty;
            string sGameGatePort = string.Empty;
            string sFileName = Path.Combine(SystemShare.BasePath, "!servertable.txt");
            if (File.Exists(sFileName))
            {
                StringList loadList = new StringList();
                loadList.LoadFromFile(sFileName);
                for (int i = 0; i < loadList.Count; i++)
                {
                    string sLineText = loadList[i];
                    if (!string.IsNullOrEmpty(sLineText) && sLineText[0] != ';')
                    {
                        sLineText = HUtil32.GetValidStr3(sLineText, ref sIdx, new[] { " ", "\t" });
                        string sGameGate = HUtil32.GetValidStr3(sLineText, ref sSelGateIPaddr, new[] { " ", "\t" });
                        if (string.IsNullOrEmpty(sIdx) || string.IsNullOrEmpty(sGameGate) || string.IsNullOrEmpty(sSelGateIPaddr))
                        {
                            continue;
                        }
                        if (M2Share.ServerTableList[nRouteIdx] == null)
                        {
                            M2Share.ServerTableList[nRouteIdx] = new TRouteInfo();
                        }
                        M2Share.ServerTableList[nRouteIdx].GateCount = 0;
                        M2Share.ServerTableList[nRouteIdx].ServerIdx = HUtil32.StrToInt(sIdx, 0);
                        M2Share.ServerTableList[nRouteIdx].SelGateIP = sSelGateIPaddr.Trim();
                        int nGateIdx = 0;
                        while (!string.IsNullOrEmpty(sGameGate))
                        {
                            sGameGate = HUtil32.GetValidStr3(sGameGate, ref sGameGateIPaddr, new[] { " ", "\t" });
                            sGameGate = HUtil32.GetValidStr3(sGameGate, ref sGameGatePort, new[] { " ", "\t" });
                            M2Share.ServerTableList[nRouteIdx].GameGateIP[nGateIdx] = sGameGateIPaddr.Trim();
                            M2Share.ServerTableList[nRouteIdx].GameGatePort[nGateIdx] = HUtil32.StrToInt(sGameGatePort, 0);
                            nGateIdx++;
                        }
                        M2Share.ServerTableList[nRouteIdx].GateCount = nGateIdx;
                        nRouteIdx++;
                        if (nRouteIdx > M2Share.ServerTableList.Length)
                        {
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 读取文字过滤配置
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool LoadAbuseInformation(string fileName)
        {
            int lineCount = 0;
            bool result = false;
            if (File.Exists(fileName))
            {
                M2Share.AbuseTextList.Clear();
                M2Share.AbuseTextList.LoadFromFile(fileName);
                while (true)
                {
                    if (M2Share.AbuseTextList.Count <= lineCount)
                    {
                        break;
                    }
                    string sText = M2Share.AbuseTextList[lineCount].Trim();
                    if (string.IsNullOrEmpty(sText))
                    {
                        M2Share.AbuseTextList.RemoveAt(lineCount);
                        continue;
                    }
                    lineCount++;
                }
                result = true;
            }
            return result;
        }
    }
}