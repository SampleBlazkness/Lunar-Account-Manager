using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LunarAccountManager
{
    public class MainForm : Form
    {
        private readonly AccountsStore _store;

        private ListView _list;
        private TextBox _txtName;
        private TextBox _txtUuid;
        private Button _btnLookup;
        private Label _lbStatus;
        private Label _lbExpiry;
        private Label _lbSkinHint;
        private PictureBox _picHead;
        private PictureBox _picBody;
        private CheckBox _chkAuto;

        private bool _loading;
        private bool _dirty;
        private bool _newMode;
        private string _selectedKey;
        private int _skinToken;

        private static readonly string[] LunarProcessNames = { "Lunar Client", "LunarClient", "lunarclient", "Lunar Client Installer" };

        public MainForm(AccountsStore store)
        {
            _store = store;
            BuildUi();
            LoadStore();
        }

        // ---------------------------------------------------------------- UI

        private void BuildUi()
        {
            Text = "Lunar 离线账户管理器";
            ClientSize = new Size(1000, 600);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9f);

            _list = new ListView();
            _list.Location = new Point(12, 12);
            _list.Size = new Size(520, 474);
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.MultiSelect = false;
            _list.HideSelection = false;
            _list.Columns.Add("玩家名", 140);
            _list.Columns.Add("UUID", 200);
            _list.Columns.Add("有效期", 120);
            _list.Columns.Add("当前", 45);
            _list.SelectedIndexChanged += OnSelectionChanged;
            Controls.Add(_list);

            var box = new GroupBox();
            box.Text = "账户信息";
            box.Location = new Point(548, 8);
            box.Size = new Size(440, 478);
            Controls.Add(box);

            box.Controls.Add(MakeLabel("玩家名", 14, 26));

            _txtName = new TextBox();
            _txtName.Location = new Point(14, 46);
            _txtName.Size = new Size(220, 24);
            _txtName.TextChanged += OnFieldChanged;
            box.Controls.Add(_txtName);

            _btnLookup = MakeButton("查询", 244, 44, 66);
            _btnLookup.Click += OnLookup;
            box.Controls.Add(_btnLookup);

            var linkSite = new LinkLabel();
            linkSite.Text = "在 mcprofile.io 查询";
            linkSite.Location = new Point(318, 48);
            linkSite.AutoSize = true;
            linkSite.LinkClicked += delegate { OpenSite(); };
            box.Controls.Add(linkSite);

            box.Controls.Add(MakeLabel("UUID", 14, 82));

            _txtUuid = new TextBox();
            _txtUuid.Location = new Point(14, 102);
            _txtUuid.Size = new Size(410, 24);
            _txtUuid.TextChanged += OnFieldChanged;
            box.Controls.Add(_txtUuid);

            box.Controls.Add(MakeLabel("有效期", 14, 146));

            _lbExpiry = new Label();
            _lbExpiry.Location = new Point(66, 146);
            _lbExpiry.AutoSize = true;
            _lbExpiry.Font = new Font(Font, FontStyle.Bold);
            box.Controls.Add(_lbExpiry);

            var btnExpiry = MakeButton("刷新 +12 小时", 244, 140, 120);
            btnExpiry.Click += OnRefreshExpiry;
            box.Controls.Add(btnExpiry);

            _picHead = new PictureBox();
            _picHead.Location = new Point(14, 184);
            _picHead.Size = new Size(96, 96);
            _picHead.SizeMode = PictureBoxSizeMode.Zoom;
            _picHead.BackColor = Color.FromArgb(245, 245, 245);
            _picHead.BorderStyle = BorderStyle.FixedSingle;
            box.Controls.Add(_picHead);

            _picBody = new PictureBox();
            _picBody.Location = new Point(122, 184);
            _picBody.Size = new Size(150, 270);
            _picBody.SizeMode = PictureBoxSizeMode.Zoom;
            _picBody.BackColor = Color.FromArgb(245, 245, 245);
            _picBody.BorderStyle = BorderStyle.FixedSingle;
            box.Controls.Add(_picBody);

            _lbSkinHint = new Label();
            _lbSkinHint.Location = new Point(14, 286);
            _lbSkinHint.Size = new Size(96, 64);
            _lbSkinHint.ForeColor = Color.DimGray;
            box.Controls.Add(_lbSkinHint);

            Controls.Add(MakeButton("新建", 12, 496, 96, OnNew));
            Controls.Add(MakeButton("删除", 114, 496, 96, OnDelete));
            Controls.Add(MakeButton("设为当前", 216, 496, 96, OnActivate));
            Controls.Add(MakeButton("保存到 Lunar", 330, 496, 120, OnSave));
            Controls.Add(MakeButton("刷新", 456, 496, 96, OnReload));

            _chkAuto = new CheckBox();
            _chkAuto.Text = "改动后自动保存";
            _chkAuto.Location = new Point(576, 500);
            _chkAuto.AutoSize = true;
            Controls.Add(_chkAuto);

            _lbStatus = new Label();
            _lbStatus.Location = new Point(12, 532);
            _lbStatus.Size = new Size(976, 22);
            Controls.Add(_lbStatus);

            var lbFile = new Label();
            lbFile.Location = new Point(12, 558);
            lbFile.Size = new Size(976, 22);
            lbFile.ForeColor = Color.DimGray;
            lbFile.Text = _store.FilePath;
            Controls.Add(lbFile);

            FormClosing += OnFormClosing;
        }

        private static Label MakeLabel(string text, int x, int y)
        {
            var l = new Label();
            l.Text = text;
            l.Location = new Point(x, y);
            l.AutoSize = true;
            return l;
        }

        private static Button MakeButton(string text, int x, int y, int width)
        {
            return MakeButton(text, x, y, width, null);
        }

        private static Button MakeButton(string text, int x, int y, int width, EventHandler onClick)
        {
            var b = new Button();
            b.Text = text;
            b.Location = new Point(x, y);
            b.Size = new Size(width, 28);
            if (onClick != null) b.Click += onClick;
            return b;
        }

        // ------------------------------------------------------------- state

        private void LoadStore()
        {
            try
            {
                _store.Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "读取 accounts.json 失败：\n" + ex.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            _dirty = false;
            _newMode = false;
            RefreshList();
        }

        private void RefreshList()
        {
            string keep = _selectedKey;
            _loading = true;
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var kv in _store.Accounts)
            {
                var acc = kv.Value as Dictionary<string, object>;
                if (acc == null) continue;

                string uuid = AccountsStore.GetString(acc, "localId");
                DateTime? exp = AccountsStore.GetExpiry(acc);
                string expText = exp.HasValue ? exp.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "-";
                bool active = string.Equals(kv.Key, _store.ActiveId, StringComparison.OrdinalIgnoreCase);

                var item = new ListViewItem(DisplayName(acc));
                item.SubItems.Add(uuid);
                item.SubItems.Add(expText);
                item.SubItems.Add(active ? "●" : "");
                item.Tag = kv.Key;
                if (active) item.Font = new Font(Font, FontStyle.Bold);
                _list.Items.Add(item);
            }
            _list.EndUpdate();
            _loading = false;

            if (keep != null) SelectByKey(keep);
            else if (_list.Items.Count > 0) _list.Items[0].Selected = true;
        }

        private static string DisplayName(Dictionary<string, object> acc)
        {
            var profile = AccountsStore.GetMap(acc, "minecraftProfile");
            string name = profile != null ? AccountsStore.GetString(profile, "name") : "";
            if (name.Length == 0) name = AccountsStore.GetString(acc, "username");
            return name;
        }

        private Dictionary<string, object> SelectedAccount()
        {
            if (_selectedKey == null || !_store.Accounts.ContainsKey(_selectedKey)) return null;
            return _store.Accounts[_selectedKey] as Dictionary<string, object>;
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            if (_list.SelectedItems.Count == 0) return;

            _newMode = false;
            _selectedKey = (string)_list.SelectedItems[0].Tag;
            var acc = SelectedAccount();
            if (acc == null) return;

            _loading = true;
            _txtName.Text = DisplayName(acc);
            _txtUuid.Text = AccountsStore.GetString(acc, "localId");
            _loading = false;

            ShowExpiry(acc);
            LoadSkin(_txtUuid.Text.Trim());
        }

        private void ShowExpiry(Dictionary<string, object> acc)
        {
            var exp = AccountsStore.GetExpiry(acc);
            if (exp.HasValue)
            {
                _lbExpiry.Text = exp.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                _lbExpiry.ForeColor = exp.Value < DateTime.UtcNow ? Color.Firebrick : Color.SeaGreen;
            }
            else
            {
                _lbExpiry.Text = "未设置";
                _lbExpiry.ForeColor = Color.Firebrick;
            }
        }

        private void OnFieldChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            MarkDirty();
        }

        private void MarkDirty()
        {
            _dirty = true;
            SetStatus("有未保存的修改", Color.DarkOrange);
            if (_chkAuto.Checked) AutoSave();
        }

        // -------------------------------------------------------------- skin

        private void LoadSkin(string uuid)
        {
            _skinToken++;
            int token = _skinToken;
            _picHead.Image = null;
            _picBody.Image = null;
            _lbSkinHint.Text = "";

            if (!Web.IsUuid(uuid)) return;

            Image cachedHead = LoadCached(uuid, "head");
            Image cachedBody = LoadCached(uuid, "body");
            if (cachedHead != null || cachedBody != null)
            {
                _picHead.Image = cachedHead ?? MakePlaceholder(true);
                _picBody.Image = cachedBody ?? MakePlaceholder(false);
                _lbSkinHint.Text = "";
            }
            else
            {
                _picHead.Image = MakePlaceholder(true);
                _picBody.Image = MakePlaceholder(false);
                _lbSkinHint.Text = "正在加载…";
            }

            string[] heads =
            {
                "https://mc-heads.net/avatar/" + uuid + "/96",
                "https://crafatar.com/avatars/" + uuid + "?size=96&overlay",
                "https://minotar.net/helm/" + uuid + "/96.png"
            };
            string[] bodies =
            {
                "https://mc-heads.net/body/" + uuid + "/140",
                "https://crafatar.com/renders/body/" + uuid + "?scale=5&overlay",
                "https://minotar.net/armor/body/" + uuid + "/140.png"
            };

            Task.Factory.StartNew(delegate
            {
                var headTask = Task.Factory.StartNew(delegate { return Web.TryDownloadImage(heads, 4000); });
                var bodyTask = Task.Factory.StartNew(delegate { return Web.TryDownloadImage(bodies, 4000); });
                Image head = headTask.Result;
                Image body = bodyTask.Result;
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        if (IsDisposed || token != _skinToken)
                        {
                            if (head != null) head.Dispose();
                            if (body != null) body.Dispose();
                            return;
                        }
                        bool missing = head == null || body == null;
                        _picHead.Image = head ?? MakePlaceholder(true);
                        _picBody.Image = body ?? MakePlaceholder(false);
                        if (head != null) SaveCached(uuid, "head", head);
                        if (body != null) SaveCached(uuid, "body", body);
                        _lbSkinHint.Text = missing ? "未找到皮肤\n显示默认形象" : "";
                    });
                }
                catch { }
            });
        }

        // ---------------------------------------------------------- commands

        private void OnLookup(object sender, EventArgs e)
        {
            string name = _txtName.Text.Trim();
            string problem = ValidateName(name);
            if (problem != null) { SetStatus(problem, Color.Firebrick); return; }

            Cursor = Cursors.WaitCursor;
            _btnLookup.Enabled = false;
            try
            {
                string error;
                MojangProfile p = Web.Lookup(name, out error);
                if (p == null) { SetStatus(error ?? "玩家不存在", Color.Firebrick); return; }
                _txtName.Text = p.Name;
                _txtUuid.Text = p.Uuid;
                SetStatus("已找到正版档案", Color.SeaGreen);
                LoadSkin(p.Uuid);
            }
            finally
            {
                _btnLookup.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void OnNew(object sender, EventArgs e)
        {
            _newMode = true;
            _selectedKey = null;
            _list.SelectedItems.Clear();
            _loading = true;
            _txtName.Text = "";
            _txtUuid.Text = "";
            _loading = false;
            _lbExpiry.Text = "12 小时后";
            _lbExpiry.ForeColor = Color.SeaGreen;
            _picHead.Image = null;
            _picBody.Image = null;
            _lbSkinHint.Text = "";
            _txtName.Focus();
            SetStatus("填写玩家名并点「查询」，然后点「保存到 Lunar」", Color.DimGray);
        }

        private void OnDelete(object sender, EventArgs e)
        {
            if (_selectedKey == null) { SetStatus("请先选择一个账户", Color.Firebrick); return; }
            if (MessageBox.Show(this, "确定删除这个账户吗？", "确认", MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question) != DialogResult.OK)
                return;

            _store.Accounts.Remove(_selectedKey);
            if (string.Equals(_store.ActiveId, _selectedKey, StringComparison.OrdinalIgnoreCase))
                _store.ActiveId = _store.Accounts.Count > 0 ? FirstKey() : "";
            _selectedKey = null;
            MarkDirty();
            RefreshList();
        }

        private void OnActivate(object sender, EventArgs e)
        {
            if (_selectedKey == null) { SetStatus("请先选择一个账户", Color.Firebrick); return; }
            _store.ActiveId = _selectedKey;
            MarkDirty();
            RefreshList();
            SelectByKey(_selectedKey);
        }

        private void OnRefreshExpiry(object sender, EventArgs e)
        {
            var acc = SelectedAccount();
            if (acc == null) { SetStatus("请先选择一个账户", Color.Firebrick); return; }
            AccountsStore.RefreshExpiry(acc, DateTime.UtcNow.AddHours(12));
            ShowExpiry(acc);
            MarkDirty();
        }

        private void OnSave(object sender, EventArgs e)
        {
            string name = _txtName.Text.Trim();
            string uuid = _txtUuid.Text.Trim().ToLowerInvariant();

            string problem = ValidateName(name);
            if (problem != null) { SetStatus(problem, Color.Firebrick); return; }
            if (!Web.IsUuid(uuid)) { SetStatus("UUID 无效，请点「查询」获取", Color.Firebrick); return; }

            if (_newMode)
            {
                if (_store.Accounts.ContainsKey(uuid)) { SetStatus("该 UUID 的账户已存在", Color.Firebrick); return; }
                var created = AccountsStore.NewAccount(name, uuid, DateTime.UtcNow.AddHours(12));
                _store.Accounts[uuid] = created;
                if (_store.ActiveId.Length == 0) _store.ActiveId = uuid;
                _selectedKey = uuid;
                _newMode = false;
            }
            else
            {
                var acc = SelectedAccount();
                if (acc == null) { SetStatus("请先在左侧选择账户，或点「新建」", Color.Firebrick); return; }

                if (!string.Equals(_selectedKey, uuid, StringComparison.OrdinalIgnoreCase))
                {
                    if (_store.Accounts.ContainsKey(uuid)) { SetStatus("该 UUID 的账户已存在", Color.Firebrick); return; }
                    _store.Accounts.Remove(_selectedKey);
                    AccountsStore.ApplyIdentity(acc, name, uuid);
                    _store.Accounts[uuid] = acc;
                    if (string.Equals(_store.ActiveId, _selectedKey, StringComparison.OrdinalIgnoreCase))
                        _store.ActiveId = uuid;
                    _selectedKey = uuid;
                }
                else
                {
                    AccountsStore.ApplyIdentity(acc, name, uuid);
                }
            }

            if (!WriteFile()) return;

            // read the file back so the list always mirrors what is on disk
            _store.Load();
            _selectedKey = uuid;
            RefreshList();
            SelectByKey(uuid);
            var saved = SelectedAccount();
            if (saved != null) ShowExpiry(saved);
            SetStatus("已保存到 accounts.json  " + DateTime.Now.ToString("HH:mm:ss"), Color.SeaGreen);
        }

        private bool WriteFile()
        {
            if (LunarRunning())
            {
                SetStatus("Lunar Client 正在运行，请先完全退出", Color.Firebrick);
                MessageBox.Show(this, "检测到 Lunar Client 正在运行。\n请先完全退出（包括托盘图标）再保存，否则它退出时会把文件覆盖回去。",
                    "无法保存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            try
            {
                _store.Save();
                _dirty = false;
                return true;
            }
            catch (Exception ex)
            {
                SetStatus("保存失败：" + ex.Message, Color.Firebrick);
                return false;
            }
        }

        private void AutoSave()
        {
            if (LunarRunning()) return;
            try
            {
                _store.Save();
                _dirty = false;
            }
            catch { }
        }

        private void OnReload(object sender, EventArgs e)
        {
            if (_dirty && MessageBox.Show(this, "有未保存的修改，刷新会丢弃它们。继续？", "确认",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;
            string keep = _selectedKey;
            LoadStore();
            _selectedKey = keep;
            RefreshList();
            if (_selectedKey != null) SelectByKey(_selectedKey);
            SetStatus("已从磁盘重新读取", Color.SeaGreen);
        }

        private void OpenSite()
        {
            try { Process.Start("https://mcprofile.io"); }
            catch (Exception ex) { SetStatus("打开网站失败：" + ex.Message, Color.Firebrick); }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_dirty) return;
            var r = MessageBox.Show(this, "还有未保存的修改，要保存吗？", "退出",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) e.Cancel = true;
            else if (r == DialogResult.Yes && !WriteFile()) e.Cancel = true;
        }

        // ----------------------------------------------------------- helpers

        /// <summary>
        /// Fallback avatar used when a UUID has no skin (offline accounts) or the skin
        /// services cannot be reached, so the panel never shows empty boxes.
        /// </summary>
        private static Image MakePlaceholder(bool headOnly)
        {
            var bg = Color.FromArgb(236, 238, 241);
            var skin = Color.FromArgb(178, 178, 178);
            var dark = Color.FromArgb(92, 92, 92);
            var hair = Color.FromArgb(72, 72, 72);

            if (headOnly)
            {
                var bmp = new Bitmap(96, 96);
                using (var g = Graphics.FromImage(bmp))
                using (var bSkin = new SolidBrush(skin))
                using (var bDark = new SolidBrush(dark))
                using (var bHair = new SolidBrush(hair))
                {
                    g.Clear(bg);
                    g.FillRectangle(bSkin, 18, 16, 60, 64);
                    g.FillRectangle(bHair, 18, 16, 60, 16);
                    g.FillRectangle(bDark, 32, 44, 8, 8);
                    g.FillRectangle(bDark, 56, 44, 8, 8);
                    g.FillRectangle(bDark, 38, 62, 20, 4);
                }
                return bmp;
            }

            var body = new Bitmap(150, 270);
            using (var g = Graphics.FromImage(body))
            using (var bSkin = new SolidBrush(skin))
            using (var bDark = new SolidBrush(dark))
            using (var bHair = new SolidBrush(hair))
            using (var bShirt = new SolidBrush(Color.FromArgb(96, 128, 176)))
            using (var bPants = new SolidBrush(Color.FromArgb(72, 82, 104)))
            {
                g.Clear(bg);
                g.FillRectangle(bSkin, 55, 18, 40, 40);
                g.FillRectangle(bHair, 55, 18, 40, 12);
                g.FillRectangle(bDark, 63, 36, 6, 6);
                g.FillRectangle(bDark, 81, 36, 6, 6);
                g.FillRectangle(bSkin, 22, 64, 20, 74);
                g.FillRectangle(bSkin, 108, 64, 20, 74);
                g.FillRectangle(bShirt, 45, 62, 60, 80);
                g.FillRectangle(bPants, 47, 146, 26, 100);
                g.FillRectangle(bPants, 77, 146, 26, 100);
            }
            return body;
        }

        /// <summary>Skin images are cached on disk so the next launch shows them instantly.</summary>
        private static string CacheDir()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                      "LunarAccountManager", "skins");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        private static string CachePath(string uuid, string kind)
        {
            return Path.Combine(CacheDir(), uuid.ToLowerInvariant() + "_" + kind + ".png");
        }

        private static Image LoadCached(string uuid, string kind)
        {
            try
            {
                string p = CachePath(uuid, kind);
                if (!File.Exists(p)) return null;
                using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var img = Image.FromStream(fs))
                    return new Bitmap(img);
            }
            catch { return null; }
        }

        private static void SaveCached(string uuid, string kind, Image img)
        {
            try
            {
                string p = CachePath(uuid, kind);
                if (File.Exists(p) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(p)).TotalHours < 12)
                    return;
                img.Save(p, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch { }
        }

        private static string ValidateName(string name)
        {
            if (name.Length == 0) return "请先输入玩家名";
            if (name.Length > 16) return "玩家名不能超过 16 个字符";
            foreach (char c in name)
            {
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
                    return "玩家名只能包含字母、数字和下划线";
            }
            return null;
        }

        private string FirstKey()
        {
            foreach (var k in _store.Accounts.Keys) return k;
            return "";
        }

        private void SelectByKey(string key)
        {
            if (key == null) return;
            foreach (ListViewItem it in _list.Items)
            {
                if (string.Equals((string)it.Tag, key, StringComparison.OrdinalIgnoreCase))
                {
                    it.Selected = true;
                    it.Focused = true;
                    it.EnsureVisible();
                    return;
                }
            }
        }

        private void SetStatus(string text, Color color)
        {
            _lbStatus.Text = text;
            _lbStatus.ForeColor = color;
        }

        private static bool LunarRunning()
        {
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (Array.IndexOf(LunarProcessNames, p.ProcessName) >= 0)
                        return true;
                }
                catch { }
            }
            return false;
        }
    }
}
