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
        public static dynamic CopyNode(Dictionary<string, object> node)
        {

            Dictionary<string, object> copy = new Dictionary<string, object>();

            foreach (var kvp in node)
            {
                if (kvp.Value is Dictionary<string, object> dict)
                {
                    copy.Add(kvp.Key, CopyNode(dict));
                }
                else if (kvp.Value is List<object> list)
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
        public static dynamic CopyNode(List<object> node)
        {

            List<object> copy = new List<object>();

            foreach (var val in node)
            {
                if (val is Dictionary<string, object> dict)
                {
                    copy.Add(CopyNode(dict));
                }
                else if (val is List<object> list)
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
        public static bool CompareStages(List<dynamic> origRootNode, List<dynamic> newRootNode)
        {

            if (origRootNode.Count != newRootNode.Count)
                throw new Exception("New node does not match original Count!");

            bool isFound = false;

            foreach (Dictionary<string, dynamic> origScenario in origRootNode)
            {
                foreach (Dictionary<string, dynamic> newScenario in newRootNode)
                {
                    if (CompareScenarios(origScenario, newScenario))
                    {
                        isFound = true;
                        break;
                    }
                }

                if(isFound)
                {
                    break;
                }
                
            }

            if(isFound)
            {
                return true;
            }else
            {
                throw new Exception("New dictionary entry does not match original!");
            }
        }

        private static bool CompareScenarios(Dictionary<string,dynamic> origScenario, Dictionary<string,dynamic> newScenario)
        {

            if (origScenario.Count != newScenario.Count)
                return false;

            foreach (var origList in origScenario)
            {
                if(newScenario.ContainsKey(origList.Key))
                {
                    if(newScenario[origList.Key] is List<object> newList)
                    {
                        if (!CompareStages(origList.Value, newList))
                        {
                            return false;
                        }
                    }else if(newScenario[origList.Key] is Dictionary<string,object> obj)
                    {
                        if (!CompareScenarios(origList.Value, obj))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if(newScenario[origList.Key] != origList.Value)
                        {
                            return false;
                        }
                    }

                }else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
