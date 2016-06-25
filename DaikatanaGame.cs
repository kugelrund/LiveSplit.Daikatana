﻿using System;
using System.Text;
using LiveSplit.ComponentUtil;

namespace LiveSplit.Daikatana
{
    using ComponentAutosplitter;

    class DaikatanaGame : Game
    {
        private static readonly Type[] eventTypes = new Type[] { typeof(LoadedMapEvent),
                                                                 typeof(FinishedMapEvent) };
        public override Type[] EventTypes => eventTypes;

        public override string Name => "Daikatana";
        public override string ProcessName => "daikatana";
    }

    class LoadedMapEvent : MapEvent
    {
        public override string Description => "A certain map was loaded.";

        public LoadedMapEvent() : base()
        {
        }

        public LoadedMapEvent(string map) : base(map)
        {
        }

        public override bool HasOccured(GameInfo info)
        {
            return (info.PreviousGameState != DaikatanaState.InGame) &&
                   info.InGame && (info.CurrentMap == map);
        }

        public override string ToString()
        {
            return "Map '" + map + "' was loaded";
        }
    }

    class FinishedMapEvent : MapEvent
    {
        public override string Description => "A certain map was finished.";

        public FinishedMapEvent() : base()
        {
        }

        public FinishedMapEvent(string map) : base(map)
        {
        }

        public override bool HasOccured(GameInfo info)
        {
            return info.MapChanged && (info.CurrentMap != map) && (info.PreviousMap == map);
        }

        public override string ToString()
        {
            return "Map '" + map + "' was finished";
        }
    }

    public enum DaikatanaState
    {
        Menu = 1, Loading = 3, InGame = 4
    }
}

namespace LiveSplit.ComponentAutosplitter
{
    using Daikatana;

    partial class GameInfo
    {
        public DaikatanaState PreviousGameState { get; private set; }
        public DaikatanaState CurrentGameState { get; private set; }
        public string PreviousMap { get; private set; }
        public string CurrentMap { get; private set; }
        public bool MapChanged { get; private set; }

        private Int32 mapAddress = 0x104FBB1;
        private Int32 gameStateAddress = 0x7067F8;

        partial void UpdateInfo()
        {
            int gameState;
            if (gameProcess.ReadValue(baseAddress + gameStateAddress, out gameState))
            {
                PreviousGameState = CurrentGameState;
                CurrentGameState = (DaikatanaState)gameState;
            }

            if (PreviousGameState != CurrentGameState)
            {
                UpdateMap();
                InGame = (CurrentGameState == DaikatanaState.InGame);
            }
            else
            {
                MapChanged = false;
            }
        }

        public void UpdateMap()
        {
            StringBuilder mapStringBuilder = new StringBuilder();
            if (gameProcess.ReadString(baseAddress + mapAddress, mapStringBuilder) &&
                mapStringBuilder.ToString() != CurrentMap)
            {
                PreviousMap = CurrentMap;
                CurrentMap = mapStringBuilder.ToString();
                MapChanged = true;
            }
        }
    }
}
