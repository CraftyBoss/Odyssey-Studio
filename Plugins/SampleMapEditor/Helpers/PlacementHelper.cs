using OpenTK;
using RedStarLibrary.GameTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.Helpers
{
    public class Placement
    {
        public static Dictionary<string, dynamic> CopyNode(Dictionary<string, dynamic> node)
        {

            Dictionary<string, dynamic> copy = new Dictionary<string, dynamic>();

            foreach (var kvp in node)
            {
                if (kvp.Value is Dictionary<string, dynamic> dict)
                {
                    copy.Add(kvp.Key, CopyNode(dict));
                }
                else if (kvp.Value is List<dynamic> list)
                {
                    copy.Add(kvp.Key, CopyNode(list));
                }
                else
                {
                    copy.Add(kvp.Key, kvp.Value);
                }
            }

            return copy;
        }
        public static List<dynamic> CopyNode(List<dynamic> node)
        {

            List<dynamic> copy = new List<dynamic>();

            foreach (var val in node)
            {
                if (val is Dictionary<string, dynamic> dict)
                {
                    copy.Add(CopyNode(dict));
                }
                else if (val is List<dynamic> list)
                {
                    copy.Add(CopyNode(list));
                }
                else
                {
                    copy.Add(val);
                }
            }

            return copy;
        }

        public static Vector3 LoadVector(Dictionary<string, dynamic> node, string key)
        {
            return new Vector3(node[key]["X"], node[key]["Y"], node[key]["Z"]);
        }

        public static Vector3 LoadVector(Dictionary<string, dynamic> node)
        {
            return new Vector3(node["X"], node["Y"], node["Z"]);
        }

        public static List<dynamic> ConvertPlacementInfoList(List<PlacementInfo> list)
        {
            List<dynamic> placementList = new List<dynamic>();

            foreach (var placement in list)
            {

                if(placement.UnitConfigName == "PlayerActorHakoniwa")
                {
                    Console.WriteLine($"PlayerActorHakoniwa Transform X: {placement.Translate.X} Y: {placement.Translate.Y} Z: {placement.Translate.Z}");
                }

                placementList.Add(placement.SerializePlacement());
            }

            return placementList;
        }
    }
}
