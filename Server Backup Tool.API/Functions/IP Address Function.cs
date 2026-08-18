// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.API.Functions
{
    public static class IPAddressFunction
    {
        /// <summary>
        /// Returns the IP address for logging.
        /// </summary>
        public static string FetchIpAddress(IHttpContextAccessor contextAccessor)
        {
            string ipAddress = string.Empty;

            if (contextAccessor?.HttpContext?.Request.Headers.ContainsKey("CF-Connecting-IP") == true)
            {
                ipAddress = contextAccessor.HttpContext.Request.Headers["CF-Connecting-IP"].ToString();
            }

            else if (contextAccessor?.HttpContext?.Request.Headers.ContainsKey("X-Forwarded-For") == true)
            {
                ipAddress = contextAccessor.HttpContext.Request.Headers["X-Forwarded-For"].ToString()
                    .Split(',')[0]
                    .Trim();
            }

            else if (contextAccessor?.HttpContext?.Connection?.RemoteIpAddress != null)
            {
                ipAddress = contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            }

            return ipAddress;
        }
    }
}
