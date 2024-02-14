﻿using OpenMir2;
using SystemModule;

namespace PlanesSystem
{
    public class PlanesService : IPlanesService
    {
        public Task Start()
        {
            if (SystemShare.ServerIndex == 0)
            {
                PlanesServer.Instance.StartPlanesServer();
            }
            else
            {
                PlanesClient.Instance.Initialize();
                _ = PlanesClient.Instance.Start();
                LogService.Info($"节点运行模式...主机端口:[{SystemShare.Config.MasterSrvAddr}:{SystemShare.Config.MasterSrvPort}]");
            }
            return Task.CompletedTask;
        }
    }
}