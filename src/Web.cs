using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace LunarAccountManager
{
    public class MojangProfile
    {
        public string Uuid;   // dashed, lowercase
        public string Name;
    }

    /// <summary>Mojang lookups and skin downloads. All calls are best-effort and never throw.</summary>
    public static class Web
    {
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

        static Web()
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; // TLS 1.2
            ServicePointManager.Expect100Continue = false;
        }

        /// <summary>Returns null when the name has no premium profile (or the lookup failed).</summary>
        public static MojangProfile Lookup(string name, out string error)
        {
            error = null;
            try
            {
                string url = "https://api.mojang.com/users/profiles/minecraft/" + Uri.EscapeDataString(name);
                string body = HttpGet(url, 8000);
                if (body == null)
                {
                    error = "玩家不存在";
                    return null;
                }
                var map = Json.DeserializeObject(body) as System.Collections.Generic.Dictionary<string, object>;
                if (map == null || !map.ContainsKey("id"))
                {
                    error = "玩家不存在";
                    return null;
                }
                var p = new MojangProfile();
                p.Uuid = DashUuid(Convert.ToString(map["id"]));
                p.Name = map.ContainsKey("name") ? Convert.ToString(map["name"]) : name;
                return p;
            }
            catch (WebException we)
            {
                var resp = we.Response as HttpWebResponse;
                if (resp != null && (resp.StatusCode == HttpStatusCode.NoContent ||
                                     resp.StatusCode == HttpStatusCode.NotFound ||
                                     resp.StatusCode == HttpStatusCode.BadRequest))
                {
                    error = "玩家不存在";
                    return null;
                }
                error = "查询失败：" + we.Message;
                return null;
            }
            catch (Exception ex)
            {
                error = "查询失败：" + ex.Message;
                return null;
            }
        }

        public static Image DownloadImage(string url, int timeoutMs)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = timeoutMs;
                req.ReadWriteTimeout = timeoutMs;
                req.UserAgent = BrowserUserAgent;
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var stream = resp.GetResponseStream())
                using (var ms = new MemoryStream())
                {
                    byte[] buf = new byte[8192];
                    int n;
                    while ((n = stream.Read(buf, 0, buf.Length)) > 0)
                        ms.Write(buf, 0, n);
                    if (ms.Length == 0)
                        return null;
                    ms.Position = 0;
                    return Image.FromStream(ms);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Tries each URL in order and returns the first image that downloads.</summary>
        public static Image TryDownloadImage(string[] urls, int timeoutMs)
        {
            foreach (string url in urls)
            {
                Image img = DownloadImage(url, timeoutMs);
                if (img != null)
                    return img;
            }
            return null;
        }

        public const string BrowserUserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        public static string HttpGet(string url, int timeoutMs)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            req.UserAgent = "LunarAccountManager/1.0";
            try
            {
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    return reader.ReadToEnd();
            }
            catch (WebException we)
            {
                var resp = we.Response as HttpWebResponse;
                if (resp != null && (resp.StatusCode == HttpStatusCode.NoContent || resp.StatusCode == HttpStatusCode.NotFound))
                    return null;
                throw;
            }
        }

        public static string DashUuid(string raw)
        {
            if (raw == null) return "";
            string h = raw.Replace("-", "").Trim().ToLowerInvariant();
            if (h.Length != 32) return raw.ToLowerInvariant();
            return h.Substring(0, 8) + "-" + h.Substring(8, 4) + "-" + h.Substring(12, 4) + "-" +
                   h.Substring(16, 4) + "-" + h.Substring(20, 12);
        }

        public static bool IsUuid(string s)
        {
            if (s == null) return false;
            s = s.Trim().ToLowerInvariant();
            if (s.Length != 36) return false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i == 8 || i == 13 || i == 18 || i == 23) { if (c != '-') return false; continue; }
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            }
            return true;
        }
    }
}
