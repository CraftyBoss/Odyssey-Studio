using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
using OpenTK;
using RedStarLibrary.GameTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RedStarLibrary.Helpers
{
    public class Placement
    {
        public static int GetPlacementObjectIndex(string strObjId)
        {
            if (int.TryParse(strObjId.Substring(3), out int objId))
                return objId;
            else
            {
                // edge case for placement infos with funky obj ids (note: this will be really wrong if the string has digits that are not sequential)
                if (int.TryParse(string.Join("", strObjId.Where(char.IsDigit).ToArray()), out objId))
                    return objId;
            }
            ConsoleHelper.WriteWarn($"Failed to parse object ID for index! (ID: {strObjId})");
            return -1;
        }

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

        public static Dictionary<string, dynamic> ConvertVectorToDict(Vector3 vec)
        {
            Dictionary<string, dynamic> bymlHash = new()
            {
                { "X", vec.X },
                { "Y", vec.Y },
                { "Z", vec.Z }
            };

            return bymlHash;
        }

        public static object SaveObject(object obj)
        {
            if (obj is Dictionary<string, dynamic> dict)
                return ConvertToHash(dict);
            else if (obj is List<dynamic> list)
                return ConvertToArray(list);
            else if(obj is Vector3 vec)
                return SaveVector(vec);
            else
                return obj;
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

        public static bool CompareLists(List<dynamic> origRootNode, List<dynamic> newRootNode, bool searchForMatch = false)
        {
            if (origRootNode.Count != newRootNode.Count)
            {
                ConsoleHelper.WriteError("New node does not match original Count!");
                return false;
            }

            bool isMatch = true;

            for (int i = 0; i < origRootNode.Count; i++)
            {
                Dictionary<string, dynamic> origEntry = origRootNode[i];
                Dictionary<string, dynamic> newEntry = newRootNode[i];

                if (!CompareDicts(origEntry, newEntry))
                {
                    isMatch = false;

                    if(searchForMatch)
                    {
                        int searchIdx = 0;
                        foreach (Dictionary<string, dynamic> searchEntry in newRootNode)
                        {
                            if (CompareDicts(origEntry, searchEntry))
                            {
                                ConsoleHelper.WriteWarn($"Entry at idx {i} differs from original's position ({searchIdx})");
                                isMatch = true;
                                break;
                            }
                            searchIdx++;
                        }
                    }

                    if (!isMatch)
                    {
                        ConsoleHelper.WriteError($"Entry at idx {i} does not match original!");
                        break;
                    }
                }
            }

            //if (searchForMatch && !isMatch)
            //{
            //    foreach (Dictionary<string, dynamic> origScenario in origRootNode)
            //    {
            //        foreach (Dictionary<string, dynamic> newScenario in newRootNode)
            //        {
            //            if (CompareDicts(origScenario, newScenario))
            //            {
            //                isMatch = true;
            //                break;
            //            }
            //        }

            //        if (isMatch)
            //            break;
            //    }
            //}

            if (isMatch)
                return true;
            else
            {
                ConsoleHelper.WriteError("New List does not match original!");
                return false;
            }
        }

        public static bool CompareStages(BymlIter origStage, BymlIter newStage)
        {
            return CompareLists(ConvertToList(origStage), ConvertToList(newStage));
        }

        private static bool CompareDicts(Dictionary<string,dynamic> origDict, Dictionary<string,dynamic> newDict)
        {
            int origCount = origDict.Count;
            if (origDict.ContainsKey("comment"))
                origCount--;

            int newCount = newDict.Count;
            if (newDict.ContainsKey("comment"))
                newCount--;

            if (origCount != newCount)
                return false;

            if(origDict.Select(x => x.Key)
                          .Intersect(newDict.Keys).Count() != origCount)
                return false;

            foreach (var origList in origDict)
            {
                // skip comment field for PlacementInfos
                if (origList.Key == "comment")
                    continue;

                if(!newDict.ContainsKey(origList.Key))
                    return false;
                if (newDict[origList.Key]?.GetType() != origList.Value?.GetType()) 
                    return false;

                if (newDict[origList.Key] is List<object> newList)
                    return CompareLists(origList.Value, newList, true);
                else if (newDict[origList.Key] is Dictionary<string, object> obj)
                    return CompareDicts(origList.Value, obj);
                else if (newDict[origList.Key] != origList.Value)
                    return false;
            }

            return true;
        }
    }
}
