using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
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
        public static dynamic CopyNode<K,V>(Dictionary<K, V> node)
        {
            Dictionary<K, V> copy = new();

            foreach (var kvp in node)
            {
                if (kvp.Value is Dictionary<string, object> dict)
                    copy.Add(kvp.Key, CopyNode(dict));
                else if (kvp.Value is List<object> list)
                    copy.Add(kvp.Key, CopyNode(list));
                else
                    copy.Add(kvp.Key, kvp.Value);
            }

            return copy;
        }
        public static dynamic CopyNode<V>(List<V> node)
        {
            List<V> copy = new List<V>();

            foreach (var val in node)
            {
                if (val is Dictionary<string, object> dict)
                    copy.Add(CopyNode(dict));
                else if (val is List<object> list)
                    copy.Add(CopyNode(list));
                else
                    copy.Add(val);
            }

            return copy;
        }

        public static Dictionary<string, dynamic> ConvertToDict(BymlIter iter)
        {
            var dict = new Dictionary<string, dynamic>();

            if(iter.TryGetSize(out var size))
            {
                if(size <= 0)
                {
                    return dict;
                }
            }

            foreach ((string key, object? value) in iter)
            {
                if (value is BymlIter subIter)
                {
                    if (subIter.Type == BymlDataType.Array)
                        dict.Add(key, ConvertToList(subIter));
                    else
                        dict.Add(key, ConvertToDict(subIter));
                }
                else
                {
                    dict.Add(key, value);
                }
            }

            return dict;
        }
        public static List<dynamic> ConvertToList(BymlIter iter)
        {
            var list = new List<dynamic>();

            if (iter.TryGetSize(out var size))
            {
                if (size <= 0)
                {
                    return list;
                }
            }

            foreach ((string key, object? value) in iter)
            {
                if (value is BymlIter subIter)
                {
                    if (subIter.Type == BymlDataType.Array)
                        list.Add(ConvertToList(subIter));
                    else
                        list.Add(ConvertToDict(subIter));
                }
                else
                {
                    list.Add(value);
                }
            }

            return list;
        }
        public static BymlHash ConvertToHash(Dictionary<string, dynamic> node)
        {
            BymlHash hash = new BymlHash();

            foreach (var kvp in node)
            {
                if (kvp.Value is Dictionary<string, dynamic> dict)
                    hash.Add(kvp.Key, ConvertToHash(dict));
                else if (kvp.Value is List<dynamic> list)
                    hash.Add(kvp.Key, ConvertToArray(list));
                else if (kvp.Value == null)
                    hash.AddNull(kvp.Key);
                else
                    hash.Add(kvp.Key, kvp.Value);
            }

            return hash;
        }
        public static BymlArray ConvertToArray(List<dynamic> node)
        {
            BymlArray arr = new BymlArray();

            foreach (var val in node)
            {
                if (val is Dictionary<string, dynamic> dict)
                    arr.Add(ConvertToHash(dict));
                else if (val is List<dynamic> list)
                    arr.Add(ConvertToArray(list));
                else
                    arr.Add(val);
            }

            return arr;
        }

        public static Vector3 LoadVector(BymlIter iter, string key)
        {

            if (!iter.TryGetValue(key, out BymlIter vecIter))
                throw new Exception("Unable to find Iter! Key: " + key);

            return LoadVector(vecIter);
        }
        public static Vector3 LoadVector(BymlIter iter)
        {
            Vector3 vec = new Vector3();

            if(
                iter.TryGetValue("X", out vec.X) &&
                iter.TryGetValue("Y", out vec.Y) &&
                iter.TryGetValue("Z", out vec.Z))
            {
                return vec;
            }else
            {
                throw new Exception("Unable to find vector values!");
            }
        }
        public static Vector3 LoadVector(Dictionary<string, dynamic> node, string key)
        {
            return new Vector3(node[key]["X"], node[key]["Y"], node[key]["Z"]);
        }
        public static Vector3 LoadVector(Dictionary<string, dynamic> node)
        {
            return new Vector3(node["X"], node["Y"], node["Z"]);
        }

        public static BymlContainer SaveVector(Vector3 vec)
        {
            BymlHash bymlHash = new BymlHash();

            bymlHash.Add("X", vec.X);
            bymlHash.Add("Y", vec.Y);
            bymlHash.Add("Z", vec.Z);

            return bymlHash;
        }

        public static Dictionary<string, dynamic> GetActorParamsFromDatabaseEntry(ObjectDatabaseEntry objEntry)
        {
            Dictionary<string, dynamic> actorParams = new Dictionary<string, dynamic>();

            foreach (var param in objEntry.ActorParams)
            {
                var entry = param.Value;
                actorParams.Add(param.Key, entry.FoundValues.FirstOrDefault());
            }

            return actorParams;
        }

        public static bool CompareLists(List<dynamic> origRootNode, List<dynamic> newRootNode, bool isRoot = false)
        {
            if (origRootNode.Count != newRootNode.Count)
                throw new Exception("New node does not match original Count!");

            bool isMatch = true;

            if(isRoot)
            {
                for (int i = 0; i < origRootNode.Count; i++)
                {
                    Console.WriteLine($"Comparing Scenario {i}.");

                    Dictionary<string, dynamic> origScenario = origRootNode[i];
                    Dictionary<string, dynamic> newScenario = newRootNode[i];

                    if (!CompareDicts(origScenario, newScenario))
                    {
                        Console.WriteLine("New Scenario does not match original!");
                        isMatch = false;
                        break;
                    }
                }
            }else
            {
                isMatch = false;
                foreach (Dictionary<string, dynamic> origScenario in origRootNode)
                {
                    foreach (Dictionary<string, dynamic> newScenario in newRootNode)
                    {
                        if (CompareDicts(origScenario, newScenario))
                        {
                            isMatch = true;
                            break;
                        }
                    }

                    if (isMatch)
                        break;
                }
            }

            if(isMatch)
                return true;
            else
                throw new Exception("New dictionary entry does not match original!");
        }

        public static bool CompareStages(BymlIter origStage, BymlIter newStage)
        {
            return CompareLists(ConvertToList(origStage), ConvertToList(newStage), true);
        }

        private static bool CompareDicts(Dictionary<string,dynamic> origScenario, Dictionary<string,dynamic> newScenario)
        {
            if (origScenario.Count != newScenario.Count)
                return false;

            foreach (var origList in origScenario)
            {
                if(!newScenario.ContainsKey(origList.Key))
                    return false;

                if (newScenario[origList.Key] is List<object> newList)
                    return CompareLists(origList.Value, newList);
                else if (newScenario[origList.Key] is Dictionary<string, object> obj)
                    return CompareDicts(origList.Value, obj);
                else if (newScenario[origList.Key] != origList.Value)
                    return false;
            }

            return true;
        }
    }
}
