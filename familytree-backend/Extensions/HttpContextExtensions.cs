using System.Net;

namespace familytree_backend.Extensions
{
    /// <summary>
    /// HttpContext 擴展方法
    /// 提供強化的IP位址提取功能，支援代理伺服器環境
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// 獲取客戶端真實IP位址
        /// 支援代理伺服器環境，檢查多個標頭以獲取真實IP
        /// </summary>
        /// <param name="httpContext">HTTP上下文</param>
        /// <returns>客戶端IP位址</returns>
        public static string GetClientIpAddress(this HttpContext httpContext)
        {
            try
            {
                // 檢查代理標頭順序：X-Forwarded-For -> X-Real-IP -> X-Client-IP -> CF-Connecting-IP
                var forwardedHeaders = new[]
                {
                    "X-Forwarded-For",
                    "X-Real-IP", 
                    "X-Client-IP",
                    "CF-Connecting-IP", // Cloudflare
                    "X-Forwarded",
                    "Forwarded-For",
                    "Forwarded"
                };

                // 優先檢查代理標頭
                foreach (var header in forwardedHeaders)
                {
                    var headerValue = httpContext.Request.Headers[header].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(headerValue))
                    {
                        // X-Forwarded-For 可能包含多個IP，取第一個
                        var ip = ExtractFirstValidIp(headerValue);
                        if (!string.IsNullOrEmpty(ip))
                        {
                            return NormalizeIpAddress(ip);
                        }
                    }
                }

                // 如果沒有代理標頭，使用連接的遠端IP
                var remoteIp = httpContext.Connection?.RemoteIpAddress;
                if (remoteIp != null)
                {
                    return NormalizeIpAddress(remoteIp.ToString());
                }

                // 如果以上都失敗，返回預設值
                return "127.0.0.1";
            }
            catch (Exception)
            {
                // 如果IP提取失敗，返回預設值，不應該影響主要功能
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// 從包含多個IP的字串中提取第一個有效的IP位址
        /// </summary>
        /// <param name="ipString">IP字串，可能包含多個IP用逗號分隔</param>
        /// <returns>第一個有效的IP位址</returns>
        private static string? ExtractFirstValidIp(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
                return null;

            // 分割可能的多個IP（用逗號或空格分隔）
            var ips = ipString.Split(',', ';', ' ')
                             .Select(ip => ip.Trim())
                             .Where(ip => !string.IsNullOrEmpty(ip));

            foreach (var ip in ips)
            {
                if (IsValidIpAddress(ip))
                {
                    return ip;
                }
            }

            return null;
        }

        /// <summary>
        /// 驗證IP位址格式是否有效
        /// </summary>
        /// <param name="ipString">IP位址字串</param>
        /// <returns>是否為有效IP位址</returns>
        private static bool IsValidIpAddress(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
                return false;

            // 移除可能的埠號
            var cleanIp = ipString.Split(':')[0];

            return IPAddress.TryParse(cleanIp, out var address) && 
                   !IsPrivateOrReservedIp(address);
        }

        /// <summary>
        /// 檢查是否為私有或保留IP位址
        /// 在某些情況下我們可能想要排除這些IP
        /// </summary>
        /// <param name="address">IP位址</param>
        /// <returns>是否為私有或保留IP</returns>
        private static bool IsPrivateOrReservedIp(IPAddress address)
        {
            // 這裡暫時不排除私有IP，因為在內網環境中可能需要記錄
            // 可以根據需求調整這個邏輯
            return false;
        }

        /// <summary>
        /// 標準化IP位址格式
        /// 處理IPv6 localhost轉換為IPv4等特殊情況
        /// </summary>
        /// <param name="ipString">原始IP位址字串</param>
        /// <returns>標準化的IP位址</returns>
        private static string NormalizeIpAddress(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
                return "127.0.0.1";

            try
            {
                // 處理IPv6 localhost
                if (ipString == "::1")
                {
                    return "127.0.0.1";
                }

                // 處理IPv6映射的IPv4位址
                if (IPAddress.TryParse(ipString, out var address))
                {
                    if (address.IsIPv4MappedToIPv6)
                    {
                        return address.MapToIPv4().ToString();
                    }

                    // 如果是IPv6 localhost
                    if (address.Equals(IPAddress.IPv6Loopback))
                    {
                        return "127.0.0.1";
                    }

                    return address.ToString();
                }

                return ipString;
            }
            catch (Exception)
            {
                // 如果標準化失敗，返回原始值或預設值
                return string.IsNullOrWhiteSpace(ipString) ? "127.0.0.1" : ipString;
            }
        }

        /// <summary>
        /// 獲取使用者代理字串
        /// </summary>
        /// <param name="httpContext">HTTP上下文</param>
        /// <returns>使用者代理字串</returns>
        public static string GetUserAgent(this HttpContext httpContext)
        {
            try
            {
                return httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
            }
            catch (Exception)
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// 獲取請求的完整URL
        /// </summary>
        /// <param name="httpContext">HTTP上下文</param>
        /// <returns>完整URL</returns>
        public static string GetFullUrl(this HttpContext httpContext)
        {
            try
            {
                var request = httpContext.Request;
                return $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
            }
            catch (Exception)
            {
                return "Unknown";
            }
        }
    }
}