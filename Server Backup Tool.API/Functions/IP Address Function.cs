// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.API.Functions
{
    public static class IPAddressFunction
    {
        /// <summary>
        /// Returns the IP address for logging.
        /// </summary>
        public static string FetchIpAddress(HttpContext context)
        {
            string ipAddress = string.Empty;

            if (context.Request.Headers.ContainsKey("CF-Connecting-IP"))
            {
                ipAddress = context.Request.Headers["CF-Connecting-IP"].ToString();
            }

            else if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = context.Request.Headers["X-Forwarded-For"].ToString()
                    .Split(',')[0]
                    .Trim();
            }

            else if (context.Connection.RemoteIpAddress != null)
            {
                ipAddress = context.Connection.RemoteIpAddress.ToString();
            }

            return ipAddress;
        }
    }
}
