using Newtonsoft.Json;
using OpenTK;
using RedStarLibrary.JsonConverters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RedStarLibrary.Schemas
{
    internal class ActorJSONData
    {
        public static readonly Dictionary<string, string> TypeTranslation = new()
        {
            { "bush", "SeaWorldBush" },
            { "spiny", "Togezo" },
            { "frog", "Frog" },
            { "coin", "Coin" },
            { "coin-circle", "CoinCirclePlacement" },
            { "coin-ring", "CoinRing" },
            { "coinstacks", "CoinStackGroup" },
            { "rolling-rock", "KickStone" },
            { "odyssey", "ShineTowerRocket" },
            { "goomba", "KuriboPossessed" },
            { "mini-goomba", "KuriboMini" },
            { "lost_kingdom_shop", "KinokoUfo" },
            { "skull-sign", "SignBoardDanger" },
            { "binocular", "Fukankun" },
            { "hat-trampoline", "CapTrampoline" },
            { "hat_trampoline", "CapTrampoline" },
            { "torch", "SandWorldCandlestand000" },
            { "sherm", "Tank" },
            { "piranha-plant", "PackunFire" },
            { "toad", "KinopioMember" },
            { "Toad", "KinopioMember" },
            //{ "checkpoint", "CapTrampoline" },
            //{ "breakable-rock", "WaterfallWorldBreakParts006" },

            { "cardboard-box", "CardboardBox" },
            { "crate", "FrailBox" },
            { "pokio", "Tsukkun" },
            { "bench", "CityWorldHomeBench000" },
            { "lakitu", "JugemFishing" },
            { "wind", "AirCurrent" },
            { "hat-cloud", "CapAppearTargetStepA" },
            //{ "wire", "ElectricWire" },
            { "pipe", "Dokan" },
            { "uproot-spawner", "SenobiGeneratePoint" },
            { "glydon", "Kakku" },

            { "hatapult", "CapCatapult" },
            { "princess_peach", "Peach" },
            { "rocket-flower", "RocketFlower" },
            { "hammer-bro", "HammerBrosPossessed" },
            { "pan-bro", "HammerBrosPossessed" },
            { "bullet-bill-launcher", "KillerLauncher" },
            { "bowser-bomb-turret", "ReflectBombGenerator" },
            { "heart-life-up", "LifeMaxUpItem" },

            //{ "water", "WaterArea" },
            { "Qblock", "BlockQuestion" },

            { "climbing-pole", "CapWorldHomePole000" },
            { "cheep-cheep", "Pukupuku" },
            { "parabones", "KaronWing" },
            { "gushen", "Hosui" },
            { "swinging-pole", "PoleGrabCeil"},
            // vine and nut will be added manually

            {"burbo", "Popn" }, // this will not generate anything by default
            {"jizo", "StatueJizo" },
            {"breakable_fossil_rock", "WaterfallWorldBreakParts003" },
            {"tostarenan", "TalkNpcDesertMan" },
            {"moeye", "Megane" },
            {"coin-coffer", "Gamane" },
            {"cap_kingdom_resident", "TalkNpcCapMan" },
            {"Lake_Kingdom_Mermaid", "LakeMan" },
            {"seaside_goon", "SeaMan" },
            {"fork_npc", "LavaMan" },
            {"shiverian", "SnowMan" },
            {"Wooded_robot", "TalkNpcForestMan" },
            {"New_Donker", "TalkNpcCityMan" },
            {"on_fire_campfire", "FireSwitch" },
            {"tropical-wiggler", "Imomu" },
        };

        public static readonly Dictionary<string, Vector3> TypeOffsets = new()
        {
            {"SeaWorldBush", new Vector3(0, -45.0f, 0.0f) },
            {"Togezo", new Vector3(0, -45.0f, 0.0f) },
            {"Coin", new Vector3(0, 25.0f, 0.0f) },
            {"KickStone", new Vector3(0, -25.0f, 0.0f) },
            {"SandWorldCandlestand000", new Vector3(0, -45.0f, 0.0f) },
            {"CapTrampoline", new Vector3(0, -45.0f, 0.0f) },
            {"KuriboMini", new Vector3(0, -40.0f, 0.0f) },
            {"Fukankun", new Vector3(0, -40.0f, 0.0f) },
            {"PackunFire", new Vector3(0, -45.0f, 0.0f) },
            {"Tank", new Vector3(0, -45.0f, 0.0f) },

            {"CardboardBox", new Vector3(0, -90.0f, 0.0f) },
            {"FrailBox", new Vector3(0, -90.0f, 0.0f) },
            {"RocketFlower", new Vector3(0, -40.0f, 0.0f) },
            {"KillerLauncher", new Vector3(0, -50.0f, 0.0f) },
            {"LifeMaxUpItem", new Vector3(0, -30.0f, 0.0f) },
            {"BlockQuestion", new Vector3(0, -45.0f, 0.0f) },
            {"LakeMan", new Vector3(0, -100.0f, 0.0f) },
            {"TalkNpcCityMan", new Vector3(0, -60.0f, 0.0f) },
            {"SnowMan", new Vector3(0, 40.0f, 0.0f) },
            // {"binocular", new Vector3(0, -40.0f, 0.0f) },
        };

        public static readonly Dictionary<string, Dictionary<string, dynamic>> TypeParams = new()
        {
            {"Coin", new Dictionary<string, dynamic>
            {
                {"ShadowLength", 10.0f }
            } },
            {"CoinCirclePlacement", new Dictionary<string, dynamic>
            {
                {"ShadowLength", 10.0f }
            } },
            {"CoinRing", new Dictionary<string, dynamic>
            {
                {"ShadowLength", 10.0f }
            } },
            {"CoinStackGroup", new Dictionary<string, dynamic>
            {
                {"MustSave", false },
                {"StacksAmount", 0 },
            } },
            {"BlockQuestion", new Dictionary<string, dynamic>
            {
                {"ItemType", "Coin" },
                {"ShadowLength", 10.0f }
            } },
            {"PoleGrabCeil", new Dictionary<string, dynamic>
            {
                {"IsConnectPose", false },
                {"IsConnectToCollision", false },
            } },
            {"KaronWing", new Dictionary<string, dynamic>
            {
                {"IsWearingCap", false },
            } },
            {"PukuPuku", new Dictionary<string, dynamic>
            {
                {"IsRevive", true },
                {"LightType", 1 },
                {"MoveType", 1 },
            } },
            {"PopnGenerator", new Dictionary<string, dynamic>
            {
                {"FindDistance", 0.0f },
                {"IsLean", false },
                {"IsNoAddGenerate", false },
                {"IsReviveOutOfArea", true },
                {"PopnNum", 0 },
                {"WaitFrame", 1 }
            } }
        };

        public static readonly Dictionary<string, string> TypeModels = new()
        {
            {"TalkNpcForestMan", "ForestMan" },
            {"TalkNpcCityMan", "CityMan" },
            {"TalkNpcCapMan", "CapMan" },
            {"TalkNpcDesertMan", "DesertMan" },
        };

        [JsonObject]
        private class ActorJsonEntry
        {
            [JsonConverter(typeof(Vector3Converter))]
            public Vector3 pos;
            [JsonProperty]
            public string actorType;
        }

        public class ActorEntry
        {
            public Vector3 Position = Vector3.Zero;
            public string ClassName = string.Empty;
            public bool HasParams = false;
            public Dictionary<string, dynamic> ActorParams = new();
        }

        public List<ActorEntry> ActorEntries = new();

        private float scale;
        private float groundScale;
        private Vector3 blockCoordOffset;
        private Vector3 mapCoordOffset;

        public ActorJSONData(string path, float s, float gs, Vector3 bco, Vector3 mco)
        {
            var unparsedData = JsonConvert.DeserializeObject<List<ActorJsonEntry>>(File.ReadAllText(path), new Vector3Converter());

            scale = s;
            groundScale = gs;
            blockCoordOffset = bco;
            mapCoordOffset = mco;

            HashSet<string> unknownTypes = new HashSet<string>();
            foreach (var entry in unparsedData)
            {
                if (entry.actorType == "checkpoint")
                    continue; // checkpoints aren't able to be done yet

                if (!TypeTranslation.TryGetValue(entry.actorType, out string className))
                {
                    unknownTypes.Add(entry.actorType);
                    continue;
                }

                Vector3 actorOffset = Vector3.Zero;
                TypeOffsets.TryGetValue(className, out actorOffset);

                var newPos = ((entry.pos + blockCoordOffset) * (scale * groundScale)) + actorOffset + mapCoordOffset;

                bool hasParams = TypeParams.ContainsKey(className);

                if (className == "Popn")
                {
                    var existing = ActorEntries.FirstOrDefault(e =>
                    {
                        return e.ClassName == "PopnGenerator" && (e.Position - newPos).Length <= 500;
                    });

                    if (existing != null)
                    {
                        existing.ActorParams["PopnNum"] += 1;
                    }else
                    {
                        var popnGenEntry = new ActorEntry()
                        {
                            Position = newPos,
                            ClassName = "PopnGenerator",
                            HasParams = true,
                            ActorParams = Helpers.Placement.CopyNode(TypeParams["PopnGenerator"])
                        };
                        ActorEntries.Add(popnGenEntry);
                    }

                    continue;
                }

                var actorEntry = new ActorEntry()
                {
                    Position = newPos,
                    ClassName = className,
                    HasParams = hasParams
                };

                if(hasParams)
                    actorEntry.ActorParams = Helpers.Placement.CopyNode(TypeParams[className]);

                if (className == "CoinStackGroup")
                {
                    var rng = new Random();
                    actorEntry.ActorParams["StacksAmount"] = rng.Next(1, 6);
                }

                ActorEntries.Add(actorEntry);
            }

            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var type in unknownTypes)
                Console.WriteLine("Unknown Actor Type: " + type);
            Console.ForegroundColor = prevColor;
        }

    }
}
