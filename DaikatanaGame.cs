using System;
using System.Text;
using LiveSplit.ComponentUtil;

namespace LiveSplit.Daikatana
{
    using ComponentAutosplitter;

    class DaikatanaGame : Game
    {
        private static readonly Type[] eventTypes = new Type[] { typeof(LoadedMapEvent),
                                                                 typeof(FinishedMapEvent),
                                                                 typeof(MikikoDeadEvent) };
        public override Type[] EventTypes => eventTypes;

        public override string Name => "Daikatana";
        public override string[] ProcessNames => new string[] { "daikatana" };
        public override bool GameTimeExists => false;
        public override bool LoadRemovalExists => true;
    }

    abstract class DaikatanaMapEvent : MapEvent
    {
        public DaikatanaMapEvent() : base()
        {
        }

        public DaikatanaMapEvent(string map)
        {
            if (map.EndsWith(".bsp"))
            {
                this.map = map;
            }
            else
            {
                this.map = map + ".bsp";
            }

            attributeValues = new string[] { this.map };
        }
    }

    class LoadedMapEvent : DaikatanaMapEvent
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

    class FinishedMapEvent : DaikatanaMapEvent
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

    class MikikoDeadEvent : NoAttributeEvent
    {
        public override string Description => "Mikiko was killed.";

        public override bool HasOccured(GameInfo info)
        {
            return info.CurrentMap == "e4m6c.bsp" && info.CurrentMusicFile == "music/katana1.mp3";
        }

        public override string ToString()
        {
            return "Mikiko dead";
        }
    }

    public enum GameVersion
    {
        v13_old, v13_2016_9_6, v13_2016_10_6, v13_2018_3_22
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
        public string CurrentMusicFile
        {
            get
            {
                StringBuilder musicFileString = new StringBuilder(32);
                if (musicFileAddress.DerefString(gameProcess, musicFileString))
                {
                    return musicFileString.ToString();
                }
                else
                {
                    return "";
                }
            }
        }
        public bool MapChanged { get; private set; }

        private Int32 mapAddress;
        private Int32 gameStateAddress;
        private DeepPointer musicFileAddress;

        private GameVersion gameVersion;

        partial void GetVersion()
        {
            ProcessModuleWow64Safe mainModule = gameProcess.MainModuleWow64Safe();
            if (!mainModule.ModuleName.EndsWith(".exe"))
            {
                // kind of a workaround for MainModuleWow64Safe maybe not returning
                // the correct module
                throw new ArgumentException("Process not initialised yet!");
            }

            switch (mainModule.ModuleMemorySize)
            {
                case 20434944:
                    gameVersion = GameVersion.v13_2016_9_6;
                    break;
                case 20439040:
                    gameVersion = GameVersion.v13_2016_10_6;
                    break;
                case 20377600:
                    gameVersion = GameVersion.v13_2018_3_22;
                    break;
                default:
                    gameVersion = GameVersion.v13_old;
                    break;
            }

            switch (gameVersion)
            {
                case GameVersion.v13_old:
                    mapAddress = 0x104FBB1;
                    gameStateAddress = 0x7067F8;
                    musicFileAddress = new DeepPointer("audio.dll", 0x11E90);
                    break;
                case GameVersion.v13_2016_9_6:
                    mapAddress = 0x7403AD;
                    gameStateAddress = 0x305510;
                    musicFileAddress = new DeepPointer("audio_openal.dll", 0x4E695);
                    break;
                case GameVersion.v13_2016_10_6:
                    mapAddress = 0x74140D;
                    gameStateAddress = 0x306570;
                    musicFileAddress = new DeepPointer("audio_openal.dll", 0x4E6B5);
                    break;
                case GameVersion.v13_2018_3_22:
                    mapAddress = 0x6F7D99;
                    gameStateAddress = 0x2ACEC8;
                    musicFileAddress = new DeepPointer("audio_openal.dll", 0x4F4C5);
                    break;
            }
        }

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
            StringBuilder mapStringBuilder = new StringBuilder(16);
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
