using Microsoft.AspNetCore.JsonPatch.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class JObjectAdapter : IAdapter
    {
        public virtual bool TryAdd(
            object target,
            string segment,
            IContractResolver contractResolver,
            object value,
            out string errorMessage)
        {
            var obj = (JObject) target;

            obj[segment] = JToken.FromObject(value);

            errorMessage = null;
            return true;
        }

        public virtual bool TryGet(
            object target,
            string segment,
            IContractResolver contractResolver,
            out object value,
            out string errorMessage)
        {
            var obj = (JObject) target;

            if (!obj.ContainsKey(segment))
            {
                value = null;
                errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
                return false;
            }

            value = obj[segment];
            errorMessage = null;
            return true;
        }

        public virtual bool TryRemove(
            object target,
            string segment,
            IContractResolver contractResolver,
            out string errorMessage)
        {
            var obj = (JObject) target;

            if (!obj.ContainsKey(segment))
            {
                errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
                return false;
            }

            obj.Remove(segment);
            errorMessage = null;
            return true;
        }

        public virtual bool TryReplace(
            object target,
            string segment,
            IContractResolver contractResolver,
            object value,
            out string errorMessage)
        {
            var obj = (JObject) target;

            if (!obj.ContainsKey(segment))
            {
                errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
                return false;
            }

            obj[segment] = JToken.FromObject(value);

            errorMessage = null;
            return true;
        }

        public virtual bool TryTest(
            object target,
            string segment,
            IContractResolver contractResolver,
            object value,
            out string errorMessage)
        {
            var obj = (JObject) target;

            if (!obj.ContainsKey(segment))
            {
                errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
                return false;
            }

            var currentValue = obj[segment];
            
            if (currentValue == null || string.IsNullOrEmpty(currentValue.ToString()))
            {
                errorMessage = Resources.FormatValueForTargetSegmentCannotBeNullOrEmpty(segment);
                return false;
            }

            if (!JToken.DeepEquals(JsonConvert.SerializeObject(currentValue), JsonConvert.SerializeObject(value)))
            {
                errorMessage = Resources.FormatValueNotEqualToTestValue(currentValue, value, segment);
                return false;
            }

            errorMessage = null;
            return true;
        }

        public virtual bool TryTraverse(
            object target,
            string segment,
            IContractResolver contractResolver,
            out object nextTarget,
            out string errorMessage)
        {
            var obj = (JObject) target;

            if (!obj.ContainsKey(segment))
            {
                nextTarget = null;
                errorMessage = null;
                return false;
            }

            nextTarget = obj[segment];
            errorMessage = null;
            return true;
        }
    }
}