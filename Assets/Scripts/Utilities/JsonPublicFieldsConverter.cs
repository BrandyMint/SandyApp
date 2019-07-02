using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Utilities {
    public class JsonPublicFieldsConverter : JsonConverter {
        private readonly JsonSerializerSettings _serializerSettings;

        private class PublicFieldsContractResolver : DefaultContractResolver {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType) {
                return GetMembers(objectType);
            }

            public static List<MemberInfo> GetMembers(Type objectType) {
                var members = objectType.GetMembers()
                    .Where(member => member.MemberType == MemberTypes.Field && ((FieldInfo) member).IsPublic && !((FieldInfo) member).IsLiteral)
                    .ToList();
                return members;
            }
        }

        public JsonPublicFieldsConverter() {
            _serializerSettings = new JsonSerializerSettings {
                ContractResolver = new PublicFieldsContractResolver()    
            };
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var jObj = JsonConvert.SerializeObject(value, serializer.Formatting, _serializerSettings);
            writer.WriteValue(jObj);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.Value != null)
                return JsonConvert.DeserializeObject((string)reader.Value, objectType, _serializerSettings);
            
            if (reader.TokenType == JsonToken.Null) {
                return null;
            }

            /*var value = Activator.CreateInstance(objectType);
            if (value == null) {
                throw new JsonSerializationException("No object created.");
            }*/

            serializer.Populate(reader, existingValue);
            return existingValue;
        }

        public override bool CanConvert(Type objectType) {
            return PublicFieldsContractResolver.GetMembers(objectType).Any();
        }
    }
}