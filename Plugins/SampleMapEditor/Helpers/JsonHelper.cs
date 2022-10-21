using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.Helpers
{
    public class JsonHelper
    {
        public static void WriteToJSON(dynamic value, string filename, JsonConverter converter = null)
        {
            string json;

            if (converter != null)
                json = JsonConvert.SerializeObject(value, Formatting.Indented, converter);
            else
                json = JsonConvert.SerializeObject(value, Formatting.Indented);

            if (File.Exists(filename))
                File.Delete(filename);

            File.WriteAllText(filename, json);
        }

        public static dynamic CreateDictCopy(object input)
        {

            string serializedInput = JsonConvert.SerializeObject(input, Formatting.Indented);

            dynamic output = DeserializeToDictionaryOrList(serializedInput);

            return output;
        }
        public static dynamic DeserializeToDictionaryOrList(string jo, bool isArray = false) // thank you stackoverflow https://stackoverflow.com/a/6815242
        {
            if (!isArray)
            {
                isArray = jo.Substring(0, 1) == "[";
            }
            if (!isArray)
            {
                var values = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jo);
                var values2 = new Dictionary<string, dynamic>();
                foreach (KeyValuePair<string, dynamic> d in values)
                {
                    if (d.Value is Newtonsoft.Json.Linq.JObject)
                    {
                        values2.Add(d.Key, DeserializeToDictionaryOrList(d.Value.ToString()));
                    }
                    else if (d.Value is Newtonsoft.Json.Linq.JArray)
                    {
                        values2.Add(d.Key, DeserializeToDictionaryOrList(d.Value.ToString(), true));
                    }
                    else
                    {
                        if (d.Value != null)
                        {
                            if (d.Value.GetType() == typeof(double))
                            {
                                values2.Add(d.Key, Convert.ToSingle(d.Value));
                            }
                            else if (d.Value.GetType() == typeof(long))
                            {
                                values2.Add(d.Key, Convert.ToInt32(d.Value));
                            }
                            else
                            {
                                values2.Add(d.Key, d.Value);
                            }
                        }
                        else
                        {
                            values2.Add(d.Key, d.Value);
                        }
                    }
                }
                return values2;
            }
            else
            {
                var values = JsonConvert.DeserializeObject<List<dynamic>>(jo);
                var values2 = new List<dynamic>();
                foreach (var d in values)
                {
                    if (d is Newtonsoft.Json.Linq.JObject)
                    {
                        values2.Add(DeserializeToDictionaryOrList(d.ToString()));
                    }
                    else if (d is Newtonsoft.Json.Linq.JArray)
                    {
                        values2.Add(DeserializeToDictionaryOrList(d.ToString(), true));
                    }
                    else
                    {
                        if (d != null)
                        {
                            if (d.GetType() == typeof(double))
                            {
                                values2.Add(Convert.ToSingle(d));
                            }
                            else if (d.GetType() == typeof(long))
                            {
                                values2.Add(Convert.ToInt32(d));
                            }
                            else
                            {
                                values2.Add(d);
                            }
                        }
                        else
                        {
                            values2.Add(d);
                        }
                    }
                }
                return values2;
            }
        }
    }
}
