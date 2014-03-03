using System;
using System.Globalization;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    public static class ModelBindingHelper
    {
        internal static TModel CastOrDefault<TModel>(object model)
        {
            return (model is TModel) ? (TModel)model : default(TModel);
        }

        internal static string CreateIndexModelName(string parentName, int index)
        {
            return CreateIndexModelName(parentName, index.ToString(CultureInfo.InvariantCulture));
        }

        internal static string CreateIndexModelName(string parentName, string index)
        {
            return (parentName.Length == 0) ? "[" + index + "]" : parentName + "[" + index + "]";
        }

        internal static string CreatePropertyModelName(string prefix, string propertyName)
        {
            if (String.IsNullOrEmpty(prefix))
            {
                return propertyName ?? String.Empty;
            }
            else if (String.IsNullOrEmpty(propertyName))
            {
                return prefix ?? String.Empty;
            }
            else
            {
                return prefix + "." + propertyName;
            }
        }

        internal static Type GetPossibleBinderInstanceType(Type closedModelType, Type openModelType, Type openBinderType)
        {
            Type[] typeArguments = TypeExtensions.GetTypeArgumentsIfMatch(closedModelType, openModelType);
            return (typeArguments != null) ? openBinderType.MakeGenericType(typeArguments) : null;
        }

        internal static void ReplaceEmptyStringWithNull(ModelMetadata modelMetadata, ref object model)
        {
            if (model is string &&
                modelMetadata.ConvertEmptyStringToNull &&
                String.IsNullOrWhiteSpace(model as string))
            {
                model = null;
            }
        }

        internal static void ValidateBindingContext(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw Error.ArgumentNull("bindingContext");
            }

            if (bindingContext.ModelMetadata == null)
            {
                throw Error.Argument("bindingContext", Resources.ModelBinderUtil_ModelMetadataCannotBeNull);
            }
        }

        internal static void ValidateBindingContext(ModelBindingContext bindingContext, Type requiredType, bool allowNullModel)
        {
            ValidateBindingContext(bindingContext);

            if (bindingContext.ModelType != requiredType)
            {
                var message = Resources.FormatModelBinderUtil_ModelTypeIsWrong(bindingContext.ModelType, requiredType);
                throw Error.Argument("bindingContext", message);
            }

            if (!allowNullModel && bindingContext.Model == null)
            {
                var message = Resources.FormatModelBinderUtil_ModelCannotBeNull(requiredType);
                throw Error.Argument("bindingContext", message);
            }

            if (bindingContext.Model != null && !bindingContext.ModelType.GetTypeInfo().IsAssignableFrom(requiredType.GetTypeInfo()))
            {
                var message = Resources.FormatModelBinderUtil_ModelInstanceIsWrong(
                    bindingContext.Model.GetType(), 
                    requiredType);
                throw Error.Argument("bindingContext", message);
            }
        }
    }
}
