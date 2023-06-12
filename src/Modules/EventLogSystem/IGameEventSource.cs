﻿namespace EventLogSystem
{
    public interface IGameEventSource
    {
        void AddEventLog(int eventType, string meesage);

        void AddEventLog(GameEventLogType eventType, string meesage);
    }
}