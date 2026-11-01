using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace LunarAccountManager
{
    /// <summary>
    /// Reads and writes Lunar Client's accounts.json while preserving unknown fields.
    /// The file shape is:
    ///   { "activeAccountLocalId": "...", "accounts": { "<localId>": { ...13 fields... } } }
    /// </summary>
    public class AccountsStore
    {
        public string FilePath { get; private set; }
        public Dictionary<string, object> Root { get; private set; }
        public Dictionary<string, object> Accounts { get; private set; }

        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

        public AccountsStore(string path)
        {
            FilePath = path;
            Root = new Dictionary<string, object>();
            Accounts = new Dictionary<string, object>();
            Root["activeAccountLocalId"] = "";
            Root["accounts"] = Accounts;
        }

        public string ActiveId
        {
            get { return GetString(Root, "activeAccountLocalId"); }
            set { Root["activeAccountLocalId"] = value; }
        }

        public bool Exists()
        {
            return File.Exists(FilePath);
        }

        public void Load()
        {
            Root = new Dictionary<string, object>();
            Accounts = new Dictionary<string, object>();
            Root["activeAccountLocalId"] = "";
            Root["accounts"] = Accounts;

            if (!File.Exists(FilePath))
                return;

            string text = File.ReadAllText(FilePath, Encoding.UTF8);
            if (text.Trim().Length == 0)
                return;

            object parsed = Json.DeserializeObject(text);
            var map = parsed as Dictionary<string, object>;
            if (map == null)
                return;

            Root = map;
            if (!Root.ContainsKey("activeAccountLocalId"))
                Root["activeAccountLocalId"] = "";

            var accs = Root.ContainsKey("accounts") ? Root["accounts"] as Dictionary<string, object> : null;
            if (accs == null)
            {
                accs = new Dictionary<string, object>();
                Root["accounts"] = accs;
            }
            Accounts = accs;
        }

        public void Save()
        {
            string dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(FilePath))
                Backup(FilePath);

            string json = Json.Serialize(Root);
            json = PrettyPrint(json);

            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json, new UTF8Encoding(false));
            if (File.Exists(FilePath))
                File.Replace(tmp, FilePath, null);
            else
                File.Move(tmp, FilePath);
        }

        private static void Backup(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string bak = Path.Combine(dir, name + ".backup-" + stamp + ".json");
            try { File.Copy(path, bak, false); } catch { }
            PruneBackups(dir, name, 5);
        }

        private static void PruneBackups(string dir, string name, int keep)
        {
            try
            {
                var files = new List<FileInfo>(new DirectoryInfo(dir).GetFiles(name + ".backup-*.json"));
                files.Sort(delegate (FileInfo a, FileInfo b) { return b.LastWriteTime.CompareTo(a.LastWriteTime); });
                for (int i = keep; i < files.Count; i++)
                {
                    try { files[i].Delete(); } catch { }
                }
            }
            catch { }
        }

        /// <summary>Builds a complete account entry with the 13 fields Lunar expects.</summary>
        public static Dictionary<string, object> NewAccount(string name, string uuid, DateTime expiresUtc)
        {
            var profile = new Dictionary<string, object>();
            profile["id"] = uuid;
            profile["name"] = name;

            var acc = new Dictionary<string, object>();
            acc["accessToken"] = uuid;
            acc["accessTokenExpiresAt"] = expiresUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
            acc["eligibleForMigration"] = false;
            acc["hasMultipleProfiles"] = false;
            acc["legacy"] = false;
            acc["persistent"] = true;
            acc["localId"] = uuid;
            acc["refreshToken"] = uuid;
            acc["minecraftProfile"] = profile;
            acc["remoteId"] = uuid;
            acc["type"] = "Xbox";
            acc["username"] = name;
            return acc;
        }

        public static string GetString(Dictionary<string, object> map, string key)
        {
            if (map == null || !map.ContainsKey(key) || map[key] == null)
                return "";
            return Convert.ToString(map[key]);
        }

        public static Dictionary<string, object> GetMap(Dictionary<string, object> map, string key)
        {
            if (map == null || !map.ContainsKey(key))
                return null;
            return map[key] as Dictionary<string, object>;
        }

        /// <summary>Sets the id fields of an existing entry without dropping other keys.</summary>
        public static void ApplyIdentity(Dictionary<string, object> acc, string name, string uuid)
        {
            acc["username"] = name;
            acc["localId"] = uuid;
            acc["accessToken"] = uuid;
            acc["refreshToken"] = uuid;
            acc["remoteId"] = uuid;

            var profile = GetMap(acc, "minecraftProfile");
            if (profile == null)
            {
                profile = new Dictionary<string, object>();
                acc["minecraftProfile"] = profile;
            }
            profile["id"] = uuid;
            profile["name"] = name;
        }

        public static void RefreshExpiry(Dictionary<string, object> acc, DateTime expiresUtc)
        {
            acc["accessTokenExpiresAt"] = expiresUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        public static DateTime? GetExpiry(Dictionary<string, object> acc)
        {
            string raw = GetString(acc, "accessTokenExpiresAt");
            if (raw.Length == 0)
                return null;
            DateTime dt;
            if (DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out dt))
                return dt;
            return null;
        }

        private static string PrettyPrint(string compact)
        {
            // Small indenting serializer so the file stays human-editable.
            var sb = new StringBuilder(compact.Length + 256);
            int indent = 0;
            bool inString = false;
            bool escape = false;
            for (int i = 0; i < compact.Length; i++)
            {
                char c = compact[i];
                if (inString)
                {
                    sb.Append(c);
                    if (escape) escape = false;
                    else if (c == '\\') escape = true;
                    else if (c == '"') inString = false;
                    continue;
                }
                switch (c)
                {
                    case '"':
                        inString = true;
                        sb.Append(c);
                        break;
                    case '{':
                    case '[':
                        sb.Append(c);
                        sb.Append('\n');
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                        break;
                    case '}':
                    case ']':
                        sb.Append('\n');
                        indent--;
                        if (indent < 0) indent = 0;
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        sb.Append('\n');
                        sb.Append(new string(' ', indent * 2));
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
