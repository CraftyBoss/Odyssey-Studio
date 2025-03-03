using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.JsonConverters
{
    internal class ObjectDatabaseEntryConverter : JsonConverter<ObjectDatabaseEntry.ParamEntry>
    {
        public override bool CanWrite => false;

        public override ObjectDatabaseEntry.ParamEntry ReadJson(JsonReader reader, Type objectType, ObjectDatabaseEntry.ParamEntry existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var entry = new ObjectDatabaseEntry.ParamEntry();

            var obj = serializer.Deserialize<JObject>(reader);

            var paramTypeStr = obj.Value<string>("ParamType");

            if(!string.IsNullOrEmpty(paramTypeStr))
                entry.ParamType = Type.GetType(obj.Value<string>("ParamType"));

            entry.Required = obj.Value<bool>("Required");
            var FoundValues = obj.GetValue("FoundValues").Children().ToList();

            if(entry.ParamType != null)
            {
                foreach (var val in FoundValues)
                    entry.AddValue(val.ToObject(entry.ParamType));
            }else
                entry.AddValue(null);

            return entry;
        }

        public override void WriteJson(JsonWriter writer, ObjectDatabaseEntry.ParamEntry value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
