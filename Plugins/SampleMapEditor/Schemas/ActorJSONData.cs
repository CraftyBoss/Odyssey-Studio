using Newtonsoft.Json;
using OpenTK;
using RedStarLibrary.JsonConverters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.Schemas
{
    internal class ActorJSONData(string path)
    {
        public static readonly Dictionary<string, string> TypeTranslation = new()
        {
            { "bush", "SeaWorldBush" },
            { "spiny", "Togezo" },
            { "frog", "Frog" },
            { "coin", "Coin" },
            { "coin-circle", "CoinCirclePlacement" },
            { "coin-ring", "CoinRing" },
            { "rolling-rock", "KickStone" },
            { "odyssey", "ShineTowerRocket" },
            { "goomba", "KuriboPossessed" },
            { "mini-goomba", "KuriboMini" },
            { "lost_kingdom_shop", "KinokoUfo" },
            { "skull-sign", "SignBoardDanger" },
            { "binocular", "Fukankun" },
            { "hat-trampoline", "CapTrampoline" },
            { "torch", "SandWorldCandlestand000" },
            { "sherm", "Tank" },
            { "piranha-plant", "PackunFire" },
            { "toad", "KinopioMember" },
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
            { "swinging-pole", "PoleGrabCeil" },
            // vine and nut will be added manually
        };

        public static readonly Dictionary<string, Vector3> TypeOffsets = new()
        {
            {"bush", new Vector3(0, -45.0f, 0.0f) },
            {"spiny", new Vector3(0, -45.0f, 0.0f) },
            {"coin", new Vector3(0, 25.0f, 0.0f) },
            {"rolling-rock", new Vector3(0, -25.0f, 0.0f) },
            {"torch", new Vector3(0, -45.0f, 0.0f) },
            {"hat-trampoline", new Vector3(0, -45.0f, 0.0f) },
            {"mini-goomba", new Vector3(0, -40.0f, 0.0f) },
            {"binocular", new Vector3(0, -40.0f, 0.0f) },
            {"piranha-plant", new Vector3(0, -45.0f, 0.0f) },
             {"sherm", new Vector3(0, -45.0f, 0.0f) },

             {"cardboard-box", new Vector3(0, -90.0f, 0.0f) },
             {"crate", new Vector3(0, -90.0f, 0.0f) },
             {"rocket-flower", new Vector3(0, -40.0f, 0.0f) },
             {"bullet-bill-launcher", new Vector3(0, -50.0f, 0.0f) },
             {"heart-life-up", new Vector3(0, -30.0f, 0.0f) },
             {"Qblock", new Vector3(0, -45.0f, 0.0f) },
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
        };

        [JsonObject]
        public class ActorEntry
        {
            [JsonProperty("pos"), JsonConverter(typeof(Vector3Converter))]
            public Vector3 Position;
            [JsonProperty("actorType")]
            public string Name;
        }

        public List<ActorEntry> ActorEntries = JsonConvert.DeserializeObject<List<ActorEntry>>(File.ReadAllText(path), new Vector3Converter());
    }
}
