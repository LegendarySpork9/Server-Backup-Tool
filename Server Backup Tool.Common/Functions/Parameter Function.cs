// Copyright © - Unpublished - Toby Hunter
using System.Collections;
using System.Reflection;

namespace ServerBackupTool.Common.Functions
{
    public static class ParameterFunction
    {
        /// <summary>
        /// Converts the model into a log friendly format.
        /// </summary>
        public static string FormatParameters(object model)
        {
            string formattedParameters = string.Empty;

            if (model != null)
            {
                Type modelType = model.GetType();

                if (modelType.IsPrimitive || model is string || model is decimal || model is DateTime || model is Guid || modelType.IsEnum)
                {
                    formattedParameters = $"\"{model}\",";
                }

                else
                {
                    foreach (PropertyInfo property in modelType
                        .GetProperties()
                        .Where(p => p.GetIndexParameters().Length == 0))
                    {
                        object? value = property.GetValue(model);

                        if (value != null)
                        {
                            if (value is IList list)
                            {
                                foreach (object item in list)
                                {
                                    formattedParameters += $"\"{property.Name}: {item}\", ";
                                }
                            }

                            else
                            {
                                formattedParameters += $"\"{property.Name}: {value}\", ";
                            }
                        }

                        else
                        {
                            formattedParameters += $"\"{property.Name}: null\", ";
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(formattedParameters))
            {
                formattedParameters = formattedParameters.Trim()
                    .Remove(formattedParameters.LastIndexOf(","));
            }

            return formattedParameters;
        }
    }
}
