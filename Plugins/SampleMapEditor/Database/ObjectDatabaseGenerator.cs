using CafeLibrary.Rendering;
using Newtonsoft.Json;
using RedStarLibrary.GameTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary
{
    [JsonObject]
    public class ObjectDatabaseEntry
    {
        [JsonProperty]
        public string ClassName;
        [JsonProperty]
        public string PlacementCategory;
        [JsonProperty]
        public string ActorCategory;
        [JsonProperty]
        public HashSet<string> Models;
        [JsonProperty]
        public HashSet<string> Variants;
        [JsonProperty]
        public Dictionary<string, HashSet<dynamic>> ActorParams; // dictionary containing a unique list of every value found for the parameters the actor can have
    }

    public class ObjectDatabaseGenerator
    {
        private static List<ObjectDatabaseEntry> ObjDatabase = new List<ObjectDatabaseEntry>(); // game has 570 actors in 1.0, this should have most, if not all, of them

        private static HashSet<string> UnusedObjs = new HashSet<string>();

        private static readonly List<string> ParamsUseEmptyString = new List<string>()
        {
            "ChangeStageId",
            "ChangeStageName"
        };

        private static readonly List<string> FullActorList = new List<string>()
        {
            "AchievementNpc",
            "AirBubble",
            "AirBubbleGenerator",
            "AirCurrent",
            "AllDeadWatcher",
            "AllDeadWatcherWithShine",
            "AmiiboHelpNpc",
            "AmiiboNpc",
            "AnagramAlphabet",
            "Barrel2D",
            "BarrelGenerator2D",
            "BarrierField",
            "BazookaElectric",
            "BendLeafTree",
            "BgmPlayObj",
            "Bird",
            "BirdCarryMeat",
            "BirdPlayerGlideCtrl",
            "BlockBrick",
            "BlockBrick2D",
            "BlockBrickBig2D",
            "BlockEmpty",
            "BlockEmpty2D",
            "BlockHard",
            "ClashWorldBlockHard",
            "BlockQuestion",
            "CityBlockQuestion",
            "BlockQuestion2D",
            "BlockTransparent",
            "BlockTransparent2D",
            "BlowObjBeans",
            "BlowObjCan",
            "BlowObjGarbageBag",
            "BlowObjMushroom",
            "BlowObj",
            "BombTail",
            "BossForest",
            "BossForestBlock",
            "BossForestWander",
            "BossKnuckle",
            "BossKnuckleCounterGround",
            "BossKnuckleFix",
            "BossMagma",
            "BossRaid",
            "BossRaidNpc",
            "BossRaidRivet",
            "BreakablePole",
            "Breeda",
            "Bubble",
            "Bubble2D",
            "BubbleLauncher",
            "Bull",
            "Byugo",
            "Cactus",
            "CactusMini",
            "CageShine",
            "CageSaveSwitch",
            "CageStageSwitch",
            "CageBreakable",
            "CameraDemoGateMapParts",
            "CameraDemoKeyMoveMapParts",
            "CameraRailHolder",
            "CameraSub",
            "CameraWatchPoint",
            "Candlestand",
            "CandlestandFire",
            "CandlestandInitializer",
            "CandlestandBgmDirector",
            "CandlestandSaveWatcher",
            "CandlestandWatcher",
            "CapAccelerator",
            "CapAcceleratorKeyMoveMapParts",
            "CapAppearMapParts",
            "CapBeamer",
            "CapBomb",
            "CapCatapult",
            "CapFlower",
            "CapFlowerGroup",
            "CapHanger",
            "CapMessageAfterInformation",
            "CapRack",
            "CapRackTimer",
            "CapRailMover",
            "CapSlotBase",
            "CapSwitch",
            "CapSwitchSave",
            "CapSwitchTimer",
            "CapThrower",
            "CapTrampoline",
            "Car",
            "CarSandWorld",
            "CarWatcher",
            "CardboardBox",
            "CatchBomb",
            "Chair",
            "CheckpointFlag",
            "ChorobonHolder",
            "ChurchDoor",
            "CityBuilding",
            "CityStreetlight",
            "CityWorldSign",
            "CityWorldUndergroundMachine",
            "CitySign",
            "CitySignal",
            "CityWorldTable",
            "Closet",
            "CloudStep",
            "CollapseSandHill",
            "CollectAnimalWatcher",
            "CollectBgmSpeaker",
            "CollectionList",
            "Coin",
            "Coin2D",
            "Coin2DCityDirector",
            "CoinBlow",
            "CoinChameleon",
            "CoinCirclePlacement",
            "CoinCollect",
            "CoinCollectHintObj",
            "CoinCollect2D",
            "CoinLead",
            "CoinRail",
            "CoinRing",
            "CoinStackGroup",
            "CrystalBreakable",
            "DamageBallGenerator",
            "DelaySwitch",
            "DemoActorCapManHero",
            "DemoActorCapManHeroine",
            "DemoActorKoopaShip",
            "DemoActorHack",
            "DemoActorPeach",
            "DemoActorShineTower",
            "DemoPeachWorldHomeWater001",
            "DemoChangeEffectObj",
            "DemoWorldMoveHomeBackGround",
            "DemoPeachWedding",
            "DemoPlayer",
            "DemoPlayerCap",
            "DigPoint",
            "DigPointHintPhoto",
            "DigPointWater",
            "DirectionFixedBillboard",
            "Dokan",
            "DokanKoopa",
            "DokanMaze",
            "DokanMazeDirector",
            "DokanStageChange",
            "DonkeyKong2D",
            "Donsuke",
            "Doshi",
            "DoorAreaChange",
            "DoorAreaChangeCap",
            "DoorCity",
            "DoorSnow",
            "DoorWarp",
            "DoorWarpStageChange",
            "EchoBlockMapParts",
            "EffectObj",
            "EffectObjScale",
            "EffectObjAlpha",
            "EffectObjCameraEmit",
            "EffectObjFollowCamera",
            "EffectObjFollowCameraLimit",
            "EffectObjInterval",
            "EffectObjNpcManFar",
            "EffectObjQualityChange",
            "ElectricWire",
            "ElectricWireKoopa",
            "EntranceCameraStartObj",
            "EventKeyMoveCameraObjNoDemo",
            "EventKeyMoveCameraObjWithDemo",
            "FigureWalkingNpc",
            "FireBlower",
            "FireBrosPossessed",
            "FireSwitch",
            "FireHydrant",
            "FireDrum2D",
            "FishingFish",
            "FixMapParts2D",
            "FixMapPartsAppearKillAsync",
            "FixMapPartsBgmChangeAction",
            "FixMapPartsCapHanger",
            "FixMapPartsDitherAppear",
            "FixMapPartsForceSafetyPoint",
            "FixMapPartsFukankunZoomCapMessage",
            "FixMapPartsScenarioAction",
            "FlyObject",
            "ForestManSeed",
            "ForestWorldHomeBreakParts000",
            "FogRequester",
            "FrailBox",
            "Frog",
            "Fukankun",
            "FukankunZoomCapMessageSun",
            "FukuwaraiWatcher",
            "ForestWorldEnergyStand",
            "ForestWorldFlowerCtrl",
            "GabuZou",
            "GabuZouGroup",
            "Gamane",
            "GiantWanderBoss",
            "GoalMark",
            "GolemClimb",
            "Gotogoton",
            "GotogotonGoal",
            "GraphicsObjShadowMaskCube",
            "GraphicsObjShadowMaskSphere",
            "GrowerBug",
            "GrowerWorm",
            "GrowFlowerCoin",
            "GrowFlowerWatcher",
            "GrowPlantGrowPlace",
            "GrowPlantSeed",
            "GrowPlantStartStage",
            "GrowPlantWatcher",
            "Gunetter",
            "GunetterMove",
            "HackCar",
            "HackFork",
            "HammerBrosPossessed",
            "HammerBros2D",
            "HelpNpc",
            "HintNpc",
            "HintPhoto",
            "HintRouteGuidePoint",
            "HipDropSwitch",
            "HipDropSwitchSave",
            "HipDropSwitchTimer",
            "HipDropTile",
            "HipDropMoveLift",
            "HipDropRepairParts",
            "HipDropTransformPartsWatcher",
            "HomeBed",
            "HomeChair",
            "HomeInside",
            "HomeShip",
            "Hosui",
            "IcicleFall",
            "Imomu",
            "IndicatorDirector",
            "Jango",
            "Joku",
            "JugemFishing",
            "JumpingRopeNpc",
            "Kakku",
            "KaronWing",
            "KeyMoveCameraFix",
            "KickStone",
            "KillerLauncher",
            "KillerLauncherDot",
            "KinokoUfo",
            "Koopa",
            "KoopaCapPlayer",
            "KoopaChurch",
            "KoopaLv1",
            "KoopaLv2",
            "KoopaLv3",
            "KoopaShip",
            "Kuribo2D3D",
            "KuriboGenerator2D3D",
            "KuriboGirl",
            "KuriboPossessed",
            "KuriboMini",
            "KuriboTowerSwitch",
            "KuriboWing",
            "LavaFryingPan",
            "LavaStewVeget",
            "LavaPan",
            "LavaWave",
            "LifeMaxUpItem",
            "LifeMaxUpItem2D",
            "LifeUpItem",
            "LifeUpItem2D",
            "LightningController",
            "LongGenerator",
            "MarchingCubeBlock",
            "MapPartsRoulette",
            "Megane",
            "MeganeLiftExLift",
            "MeganeKeyMoveMapParts",
            "MeganeMapParts",
            "Mirror",
            "MoonBasementBreakParts",
            "MoonBasementClimaxWatcher",
            "MoonBasementFallObj",
            "MoonBasementFinalGate",
            "MoonBasementFallObjDecoration",
            "MoonBasementFloor",
            "MoonBasementGate",
            "MoonBasementMeteorAreaObj",
            "MoonBasementPillar",
            "MoonBasementRock",
            "MoonBasementSlideObj",
            "MoonRock",
            "MoonWorldBell",
            "MoonWorldCaptureParadeLift",
            "Mofumofu",
            "MofumofuLv2",
            "MofumofuScrap",
            "Motorcycle",
            "MotorcycleParkingLot",
            "MoveHomeNpc",
            "MoviePlayerMapParts",
            "MultiGateKeeperBonfire",
            "MultiGateKeeperWatcher",
            "Mummy",
            "MummyGenerator",
            "NeedleTrap",
            "Nokonoko2D",
            "NoteObjFirst",
            "NoteObjFirst2D",
            "NoteObjDirector",
            "Objex",
            "OccludedEffectRequester",
            "OceanWave",
            "CloudOcean",
            "DemoCloudOcean",
            "OneMeshFixMapParts",
            "OpeningStageStartDemo",
            "PackunFire",
            "PadRumblePoint",
            "PaintObj",
            "PaulineAtCeremony",
            "PaulineAudience",
            "PeachWorldHomeCastleCap",
            "PeachWorldGate",
            "PeachWorldMoatWater",
            "PeachWorldTree",
            "Pecho",
            "Pen",
            "PictureStageChange",
            "PillarKeyMoveParts",
            "PillarSwitchOpenMapParts",
            "PlayerMotionObserver",
            "PlayerStartObj",
            "PlayerSubjectiveWatchCheckObj",
            "PlayGuideBoard",
            "PlayRecorder",
            "PlayerStartObjNoLink",
            "PochiHintPhoto",
            "Poetter",
            "PoleClimbParts",
            "PoleClimbPartsBreak",
            "PoleGrabCeil",
            "PoleGrabCeilKeyMoveParts",
            "PopnGenerator",
            "LavaWorldPoster",
            "PosterCeremony",
            "PosterWedding",
            "ReactionObjectSkyRhythm",
            "PosterWatcher",
            "PrePassCausticsLight",
            "PrePassLineLight",
            "PrePassPointLight",
            "PrePassProjLight",
            "PrePassProjOrthoLight",
            "PrePassSpotLight",
            "ProjectRaceCheckPoint",
            "Pyramid",
            "QuestObj",
            "RabbitGraph",
            "RaceAudienceNpc",
            "RaceManGoal",
            "RaceManRace",
            "RaceManStart",
            "RaceWatcher",
            "RadiConRaceWatcher",
            "RadioCassette",
            "RadiconNpc",
            "Radish",
            "RadishGold",
            "RailDrawer",
            "RankingNpc",
            "ReactionObject",
            "CarBreakable",
            "ReactionObjectDotCharacter",
            "ReflectBombGenerator",
            "RhythmSpotlight",
            "RippleGeneratePoint",
            "RippleGenerateSquare",
            "RotateTarget",
            "RouletteSwitch",
            "RouteGuideArrow",
            "RouteGuideRail",
            "RunAwayNpc",
            "SandGeyser",
            "SandWorldHomeLift",
            "SaucePan",
            "SaveFlagCheckObj",
            "ScenarioStartCameraAnim",
            "ScenarioStartCameraSimpleZoom",
            "ScenarioStartCameraRailMove",
            "Senobi",
            "SenobiGeneratePoint",
            "SenobiMoveMapParts",
            "SenobiMoveMapPartsConnector",
            "SeBarrierObj",
            "SePlayObj",
            "SePlayObjWithSave",
            "SePlayRail",
            "SequentialSwitch",
            "SessionBgmCtrlObj",
            "SessionMayorNpc",
            "SessionMusicianNpc",
            "Shibaken",
            "ShibakenHomeShipInside",
            "Shine",
            "ShineWithAppearCamera",
            "ShineChipWatcher",
            "ShineDot",
            "ShineFukankunWatchObj",
            "ShineTowerRocket",
            "ShopBgmPlayer",
            "ShopMark",
            "ShoppingWatcher",
            "SignBoardDanger",
            "SignBoardLayoutTexture",
            "SkyFukankunZoomCapMessage",
            "SkyWorldCloud",
            "SkyWorldKoopaFire",
            "SkyWorldKoopaFrame",
            "SkyWorldMiddleViewCloud",
            "SignBoard",
            "SnowWorldBigIcicle",
            "SnowWorldSequenceFlagCheckObj",
            "Sky",
            "SmallWanderBoss",
            "SneakingMan",
            "SnowManRaceNpc",
            "SnowVolume",
            "SnowVolumeEraser",
            "Souvenir",
            "SouvenirDirector",
            "Special2KeyMoveLift",
            "Special2KeyMoveParts",
            "SphinxQuiz",
            "SphinxRide",
            "SphinxTaxiWatcher",
            "Squirrel",
            "Stacker",
            "StackerCapWorldCtrl",
            "StageEventDemo",
            "StageSwitchSelector",
            "StageTalkDemoNpcCap",
            "StageTalkDemoNpcCapMoonRock",
            "Stake",
            "Statue",
            "StatueSnapMark",
            "SubActorLodFixPartsScenarioAction",
            "SwitchAnd",
            "SwitchKeyMoveMapParts",
            "TalkMessageInfoPoint",
            "TalkMessageInfoPointSaveObj",
            "TalkNpc",
            "TalkNpcFreeze",
            "TalkNpcCapMan",
            "TalkNpcCapManHero",
            "TalkNpcCityMan",
            "TalkNpcCityManLow",
            "TalkNpcCityManSit",
            "TalkNpcCityMayor",
            "TalkNpcCollectBgm",
            "TalkNpcDesertMan",
            "TalkNpcForestMan",
            "TalkNpcForestManScrap",
            "TalkNpcKinopio",
            "TalkNpcKinopioBrigade",
            "TalkNpcKinopioMember",
            "TalkNpcLakeMan",
            "TalkNpcLavaMan",
            "TalkNpcLavaManCook",
            "TalkNpcLifeUpItemSeller",
            "TalkNpcRabbit",
            "TalkNpcSeaMan",
            "TalkNpcSnowMan",
            "TalkNpcSnowManLeader",
            "TalkNpcSnowManRacer",
            "TalkPoint",
            "Tank",
            "TankReviveCtrl",
            "TaxiStop",
            "TextureReplaceScreen",
            "ThunderRenderRequester",
            "Togezo",
            "Togezo2D",
            "TokimekiMayorNpc",
            "TrampleBush",
            "TrampleSwitch",
            "TrampleSwitchSave",
            "TrampleSwitchTimer",
            "TransparentWall",
            "TreasureBox",
            "TreasureBoxKey",
            "TreasureBoxSequentialDirector",
            "TRex",
            "TRexForceScroll",
            "TRexPatrol",
            "TRexSleep",
            "TRexScrollBreakMapParts",
            "Tsukkun",
            "TsukkunHole",
            "TwistChainList",
            "Utsubo",
            "UtsuboWatcher",
            "VocalMike",
            "VolleyballBase",
            "VolleyballNet",
            "VolleyballNpc",
            "Wanwan",
            "WanwanHole",
            "WaterAreaMoveModel",
            "WaterfallWorldBigBreakableWall",
            "WaterfallWorldFallDownBridge",
            "WaterfallWorldHomeCage",
            "WaterfallWorldWaterfall",
            "WaterRoad",
            "WeightSwitch",
            "WheelWaveSurfParts",
            "WindBlowPuzzle",
            "WorldMapEarth",
            "WorldTravelingNpc",
            "WorldTravelingPeach",
            "WorldWarpHole",
            "Fastener",
            "FastenerObj",
            "AtmosScatterRequester",
            "BackHideParts",
            "BreakMapParts",
            "CapRotateMapParts",
            "ClockMapParts",
            "ConveyerMapParts",
            "FallMapParts",
            "FixMapParts",
            "FloaterMapParts",
            "FlowMapParts",
            "GateMapParts",
            "KeyMoveMapParts",
            "KeyMoveMapPartsGenerator",
            "PossessedMapParts",
            "Pukupuku",
            "PulseSwitch",
            "RailCollision",
            "RailMoveMapParts",
            "RiseMapParts",
            "ReactionMapParts",
            "RiseMapPartsHolder",
            "RocketFlower",
            "RollingCubeMapParts",
            "RippleFixMapParts",
            "RotateMapParts",
            "SeesawMapParts",
            "SlideMapParts",
            "SubActorLodMapParts",
            "SurfMapParts",
            "SwingMapParts",
            "SwitchDitherMapParts",
            "SwitchKeepOnWatcher",
            "SwitchOpenMapParts",
            "VisibleSwitchMapParts",
            "WaveSurfMapParts",
            "WheelMapParts",
            "WobbleMapParts",
            "WindBlowMapParts",
            "Yoshi",
            "YoshiFruit",
            "YoshiFruitShineHolder",
            "Yukimaru",
            "YukimaruRacer",
            "YukimaruRacerTiago"
        };

        public static void LoadDatabase()
        {
            var dictDatabase = JsonConvert.DeserializeObject<List<ObjectDatabaseEntry>>(File.ReadAllText("ObjectDatabase.json"));

            if(dictDatabase != null)
            {
                ObjDatabase = dictDatabase;

                List<string> processedActors = ObjDatabase.Select(e => e.ClassName).ToList();

                foreach (var entry in FullActorList)
                {
                    if(!processedActors.Contains(entry))
                    {
                        Console.WriteLine("Missing Entry for: " + entry);
                    }
                }
            }
        }

        public static void RegisterActorsToDatabase(List<PlacementInfo> placementList)
        {
            foreach (var info in placementList)
            {
                var entry = ObjDatabase.Find(e => e.ClassName == info.ClassName);

                if(entry == null)
                {
                    string actorCategory = info.UnitConfig.GenerateCategory;

                    entry = new ObjectDatabaseEntry()
                    {
                        ClassName = info.ClassName,
                        PlacementCategory = info.UnitConfig.PlacementTargetFile,
                        ActorCategory = actorCategory.Remove(actorCategory.IndexOf("List"), 4),
                        Models = new HashSet<string>(),
                        Variants = new HashSet<string>(),
                        ActorParams = new Dictionary<string, HashSet<dynamic>>()
                    };

                    var copy = Helpers.Placement.CopyNode(info.ActorParams);

                    foreach (var kvp in copy)
                    {
                        if (kvp.Key == "SrcUnitLayerList")
                            continue;

                        if(kvp.Value is float)
                            entry.ActorParams.Add(kvp.Key, new HashSet<dynamic>() { 0.0f });
                        else if(ParamsUseEmptyString.Contains(kvp.Key))
                            entry.ActorParams.Add(kvp.Key, new HashSet<dynamic>() { "" });
                        else
                            entry.ActorParams.Add(kvp.Key, new HashSet<dynamic>() { kvp.Value });

                        
                    }
                        

                    Console.WriteLine($"Added {info.ClassName} to Database.");

                    ObjDatabase.Add(entry);
                }else
                {
                    foreach (var param in info.ActorParams)
                    {

                        if (param.Key == "SrcUnitLayerList") 
                            continue;

                        if (!entry.ActorParams.ContainsKey(param.Key))
                        {
                            if (param.Value is float)
                                entry.ActorParams.Add(param.Key, new HashSet<dynamic>() { 0.0f });
                            else if (ParamsUseEmptyString.Contains(param.Key))
                                entry.ActorParams.Add(param.Key, new HashSet<dynamic>() { "" });
                            else
                                entry.ActorParams.Add(param.Key, new HashSet<dynamic>() { param.Value });
                        }
                        else
                        {
                            if (param.Value is float)
                                entry.ActorParams[param.Key].Add(0.0f);
                            else if (ParamsUseEmptyString.Contains(param.Key))
                                entry.ActorParams[param.Key].Add("");
                            else
                                entry.ActorParams[param.Key].Add(param.Value);
                        }
                    }
                }

                string modelName = info.ModelName != null ? info.ModelName : info.UnitConfigName;

                if(modelName != null)
                {
                    string modelPath = ResourceManager.FindResourcePath($"ObjectData\\{modelName}.szs");

                    if (File.Exists(modelPath))
                    {
                        var modelARC = ResourceManager.FindOrLoadSARC(modelPath);

                        var modelStream = MapScene.GetModelStream(modelARC, Path.GetFileNameWithoutExtension(modelPath));

                        if (modelStream != null)
                        {
                            if (entry.Models.Add(modelName))
                                Console.WriteLine($"Added the Model {modelName} to {entry.ClassName}.");
                        }
                        else
                        {
                            if (modelName != entry.ClassName && entry.Variants.Add(modelName))
                                Console.WriteLine($"Added the Variant {modelName} to {entry.ClassName}.");
                        }
                    }
                    else
                    {
                        if (modelName != entry.ClassName && entry.Variants.Add(modelName))
                            Console.WriteLine($"Added the Variant {modelName} to {entry.ClassName}.");
                    }
                }else
                {
                    if(UnusedObjs.Add(info.ClassName))
                        Console.WriteLine($"Added {info.ClassName} to list of Unused Objects.");
                }
            }
        }

        public static void SerializeDatabase()
        {

            ObjDatabase = ObjDatabase.OrderBy(e => e.ClassName).ToList();

            Helpers.JsonHelper.WriteToJSON(ObjDatabase, "ObjectDatabase.json");

            List<string> processedActors = ObjDatabase.Select(e => e.ClassName).ToList();

            foreach (var entry in FullActorList)
            {
                if (!processedActors.Contains(entry))
                {
                    Console.WriteLine("Missing Entry for: " + entry);
                }
            }

        }

    }
}
