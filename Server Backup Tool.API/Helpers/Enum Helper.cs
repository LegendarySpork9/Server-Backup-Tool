// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.API.Helpers
{
    public static class EnumHelper
    {
        /// <summary>
        /// converts the string to the specified entity.
        /// </summary>
        public static T ParseEnum<T>(string value) => (T)Enum.Parse(typeof(T), value, true);
    }
}
