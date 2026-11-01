using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        private Button _btnExpiry;
        private Label _lbStatus;
        private Label _lbExpiry;
        private PictureBox _picHead;
        private PictureBox _picBody;
        private CheckBox _chkAuto;
        private Label _lbFooter;

        private bool _loadingUi;
        private bool _dirty;

        private static readonly string[] LunarProcessNames = { "Lunar Client", "LunarClient", "lunarclient", "Lunar Client Installer" };

        public MainForm(AccountsStore store)
        {
            _store = store;
            BuildUi();
            LoadStore();
        }

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
            _list.Size = new Size(560, 470);
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.MultiSelect = false;
            _list.HideSelection = false;
            _list.Columns.Add("玩家名", 150);
            _list.Columns.Add("UUID", 220);
            _list.Columns.Add("过期时间", 130);
            _list.Columns.Add("状态", 50);
            _list.SelectedIndexChanged += OnSelectionChanged;
            Controls.Add(_list);

            int x = 590;

            Controls.Add(LabelAt("玩家名", x, 12));
            _txtName = new TextBox();
            _txtName.Location = new Point(x, 32);
            _txtName.Size = new Size(180, 24);
            Controls.Add(_txtName);

            _btnLookup = ButtonAt("查询 UUID", x + 190, 30, 100);
            _btnLookup.Click += OnLookup;
            Controls.Add(_btnLookup);

            _lbStatus = LabelAt("", x, 64);
            _lbStatus.Size = new Size(398, 20);
            _lbStatus.ForeColor = Color.DimGray;
            Controls.Add(_lbStatus);

            Controls.Add(LabelAt("UUID", x, 92));
            _txtUuid = new TextBox();
            _txtUuid.Location = new Point(x, 112);
            _txtUuid.Size = new Size(398, 24);
            Controls.Add(_txtUuid);

            Controls.Add(LabelAt("过期时间", x, 146));
            _lbExpiry = LabelAt("-", x + 70, 146);
            _lbExpiry.AutoSize = true;
            _lbExpiry.Font = new Font(Font, FontStyle.Bold);
            Controls.Add(_lbExpiry);

            _btnExpiry = ButtonAt("刷新为 12 小时后", x, 170, 150);
            _btnExpiry.Click += OnRefreshExpiry;
            Controls.Add(_btnExpiry);

            _picHead = new PictureBox();
            _picHead.Location = new Point(x, 210);
            _picHead.Size = new Size(96, 96);
            _picHead.BorderStyle = BorderStyle.FixedSingle;
            _picHead.SizeMode = PictureBoxSizeMode.Zoom;
            _picHead.BackColor = Color.WhiteSmoke;
            Controls.Add(_picHead);

            _picBody = new PictureBox();
            _picBody.Location = new Point(x + 110, 210);
            _picBody.Size = new Size(160, 280);
            _picBody.BorderStyle = BorderStyle.FixedSingle;
            _picBody.SizeMode = PictureBoxSizeMode.Zoom;
            _picBody.BackColor = Color.WhiteSmoke;
            Controls.Add(_picBody);

            Controls.Add(ButtonAt("新增账户", 12, 500, 110, OnAdd));
            Controls.Add(ButtonAt("删除账户", 128, 500, 110, OnDelete));
            Controls.Add(ButtonAt("应用修改", 244, 500, 110, OnApply));
            Controls.Add(ButtonAt("设为当前", 360, 500, 110, OnActivate));
            Controls.Add(ButtonAt("写入文件", 476, 500, 110, OnSave));
            Controls.Add(ButtonAt("查询网站", 592, 500, 110, OnOpenSite));
            Controls.Add(ButtonAt("重新读取", 708, 500, 100, OnReload));

            _chkAuto = new CheckBox();
            _chkAuto.Text = "自动写入";
            _chkAuto.Location = new Point(818, 503);
            _chkAuto.AutoSize = true;
            Controls.Add(_chkAuto);

            _lbFooter = new Label();
            _lbFooter.Location = new Point(12, 545);
            _lbFooter.Size = new Size(976, 40);
            _lbFooter.ForeColor = Color.DimGray;
            Controls.Add(_lbFooter);
        }

        private static Label LabelAt(string text, int x, int y)
        {
            var l = new Label();
            l.Text = text;
            l.Location = new Point(x, y);
            l.AutoSize = true;
            return l;
        }

        private static Button ButtonAt(string text, int x, int y, int width)
        {
            return ButtonAt(text, x, y, width, null);
        }

        private static Button ButtonAt(string text, int x, int y, int width, EventHandler onClick)
        {
            var b = new Button();
            b.Text = text;
            b.Location = new Point(x, y);
            b.Size = new Size(width, 28);
            if (onClick != null) b.Click += onClick;
            return b;
        }

        private void LoadStore()
        {
            try
            {
                _store.Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "读取 accounts.json 失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshList();
            _lbFooter.Text = "文件：" + _store.FilePath;
        }

        private void RefreshList()
        {
            _loadingUi = true;
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var kv in _store.Accounts)
            {
                var acc = kv.Value as Dictionary<string, object>;
                if (acc == null) continue;
                string name = AccountsStore.GetString(acc, "username");
                var profile = AccountsStore.GetMap(acc, "minecraftProfile");
                if (profile != null)
                {
                    string pn = AccountsStore.GetString(profile, "name");
                    if (pn.Length > 0) name = pn;
                }
                string uuid = AccountsStore.GetString(acc, "localId");
                DateTime? exp = AccountsStore.GetExpiry(acc);
                string expText = exp.HasValue ? exp.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "-";
                bool active = string.Equals(kv.Key, _store.ActiveId, StringComparison.OrdinalIgnoreCase);

                var item = new ListViewItem(name);
                item.SubItems.Add(uuid);
                item.SubItems.Add(expText);
                item.SubItems.Add(active ? "当前" : "");
                item.Tag = kv.Key;
                if (active) item.Font = new Font(Font, FontStyle.Bold);
                _list.Items.Add(item);
            }
            _list.EndUpdate();
            _loadingUi = false;

            if (_list.Items.Count > 0 && _list.SelectedItems.Count == 0)
                _list.Items[0].Selected = true;
        }

        private Dictionary<string, object> SelectedAccount()
        {
            if (_list.SelectedItems.Count == 0) return null;
            string id = (string)_list.SelectedItems[0].Tag;
            if (id == null || !_store.Accounts.ContainsKey(id)) return null;
            return _store.Accounts[id] as Dictionary<string, object>;
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (_loadingUi) return;
            var acc = SelectedAccount();
            if (acc == null) return;

            _loadingUi = true;
            var profile = AccountsStore.GetMap(acc, "minecraftProfile");
            _txtName.Text = profile != null ? AccountsStore.GetString(profile, "name") : AccountsStore.GetString(acc, "username");
            _txtUuid.Text = AccountsStore.GetString(acc, "localId");
            _loadingUi = false;

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

            LoadSkin(_txtUuid.Text.Trim());
        }

        private void LoadSkin(string uuid)
        {
            _picHead.Image = null;
            _picBody.Image = null;
            if (!Web.IsUuid(uuid)) return;

            Task.Factory.StartNew(delegate
            {
                Image head = Web.DownloadImage("https://mc-heads.net/avatar/" + uuid + "/96", 8000);
                Image body = Web.DownloadImage("https://mc-heads.net/body/" + uuid + "/160", 8000);
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        if (IsDisposed) return;
                        if (_txtUuid.Text.Trim() != uuid) return;
                        _picHead.Image = head;
                        _picBody.Image = body;
                    });
                }
                catch { }
            });
        }

        private void OnLookup(object sender, EventArgs e)
        {
            string name = _txtName.Text.Trim();
            if (name.Length == 0)
            {
                SetStatus("请先输入玩家名", true);
                return;
            }
            if (name.Length > 16)
            {
                SetStatus("玩家名不能超过 16 个字符", true);
                return;
            }
            foreach (char c in name)
            {
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
                {
                    SetStatus("玩家名只能包含字母、数字和下划线", true);
                    return;
                }
            }
            Cursor = Cursors.WaitCursor;
            _btnLookup.Enabled = false;
            try
            {
                string error;
                MojangProfile p = Web.Lookup(name, out error);
                if (p == null)
                {
                    SetStatus(error ?? "玩家不存在", true);
                    return;
                }
                _txtName.Text = p.Name;
                _txtUuid.Text = p.Uuid;
                SetStatus("已找到正版档案", false);
                LoadSkin(p.Uuid);
            }
            finally
            {
                _btnLookup.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void OnAdd(object sender, EventArgs e)
        {
            string name = _txtName.Text.Trim();
            string uuid = _txtUuid.Text.Trim().ToLowerInvariant();
            if (name.Length == 0) { SetStatus("请先输入玩家名", true); return; }
            if (!Web.IsUuid(uuid)) { SetStatus("UUID 无效，请先点「查询 UUID」", true); return; }
            if (_store.Accounts.ContainsKey(uuid)) { SetStatus("该 UUID 的账户已存在", true); return; }

            var acc = AccountsStore.NewAccount(name, uuid, DateTime.UtcNow.AddHours(12));
            _store.Accounts[uuid] = acc;
            if (_store.ActiveId.Length == 0) _store.ActiveId = uuid;
            _dirty = true;
            RefreshList();
            SelectByKey(uuid);
            SetStatus("已新增账户（未写入文件）", false);
            AutoSaveIfEnabled();
        }

        private void OnDelete(object sender, EventArgs e)
        {
            if (_list.SelectedItems.Count == 0) return;
            string id = (string)_list.SelectedItems[0].Tag;
            if (MessageBox.Show(this, "确定删除这个账户吗？", "确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;

            _store.Accounts.Remove(id);
            if (string.Equals(_store.ActiveId, id, StringComparison.OrdinalIgnoreCase))
                _store.ActiveId = _store.Accounts.Count > 0 ? FirstKey() : "";
            _dirty = true;
            RefreshList();
            SetStatus("已删除（未写入文件）", false);
            AutoSaveIfEnabled();
        }

        private string FirstKey()
        {
            foreach (var k in _store.Accounts.Keys) return k;
            return "";
        }

        private void OnApply(object sender, EventArgs e)
        {
            var acc = SelectedAccount();
            if (acc == null) { SetStatus("请先选择一个账户", true); return; }

            string name = _txtName.Text.Trim();
            string uuid = _txtUuid.Text.Trim().ToLowerInvariant();
            if (name.Length == 0) { SetStatus("玩家名不能为空", true); return; }
            if (!Web.IsUuid(uuid)) { SetStatus("UUID 格式无效", true); return; }

            string oldKey = (string)_list.SelectedItems[0].Tag;
            if (!string.Equals(oldKey, uuid, StringComparison.OrdinalIgnoreCase))
            {
                if (_store.Accounts.ContainsKey(uuid)) { SetStatus("该 UUID 的账户已存在", true); return; }
                _store.Accounts.Remove(oldKey);
                AccountsStore.ApplyIdentity(acc, name, uuid);
                _store.Accounts[uuid] = acc;
                if (string.Equals(_store.ActiveId, oldKey, StringComparison.OrdinalIgnoreCase))
                    _store.ActiveId = uuid;
                _dirty = true;
                RefreshList();
                SelectByKey(uuid);
            }
            else
            {
                AccountsStore.ApplyIdentity(acc, name, uuid);
                _dirty = true;
                RefreshList();
                SelectByKey(uuid);
            }

            SetStatus("已应用修改（未写入文件）", false);
            AutoSaveIfEnabled();
        }

        private void OnActivate(object sender, EventArgs e)
        {
            if (_list.SelectedItems.Count == 0) { SetStatus("请先选择一个账户", true); return; }
            _store.ActiveId = (string)_list.SelectedItems[0].Tag;
            _dirty = true;
            RefreshList();
            SelectByKey(_store.ActiveId);
            SetStatus("已切换当前账户（未写入文件）", false);
            AutoSaveIfEnabled();
        }

        private void OnRefreshExpiry(object sender, EventArgs e)
        {
            var acc = SelectedAccount();
            if (acc == null) { SetStatus("请先选择一个账户", true); return; }
            string key = (string)_list.SelectedItems[0].Tag;
            AccountsStore.RefreshExpiry(acc, DateTime.UtcNow.AddHours(12));
            _dirty = true;
            var exp = AccountsStore.GetExpiry(acc);
            if (exp.HasValue)
            {
                _lbExpiry.Text = exp.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                _lbExpiry.ForeColor = Color.SeaGreen;
            }
            RefreshList();
            SelectByKey(key);
            SetStatus("过期时间已设为 12 小时后（未写入文件）", false);
            AutoSaveIfEnabled();
        }

        private void OnSave(object sender, EventArgs e)
        {
            if (LunarRunning())
            {
                SetStatus("Lunar Client 正在运行，请先完全退出再写入", true);
                MessageBox.Show(this, "检测到 Lunar Client 正在运行。\n请先完全退出（包括托盘图标），否则它会把文件覆盖回去。",
                    "无法写入", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                _store.Save();
                _dirty = false;
                SetStatus("已写入 " + DateTime.Now.ToString("HH:mm:ss"), false);
            }
            catch (Exception ex)
            {
                SetStatus("写入失败：" + ex.Message, true);
            }
        }

        private void AutoSaveIfEnabled()
        {
            if (!_chkAuto.Checked) return;
            if (LunarRunning()) { SetStatus("Lunar Client 正在运行，未自动写入", true); return; }
            try
            {
                _store.Save();
                _dirty = false;
                SetStatus("已自动写入", false);
            }
            catch (Exception ex)
            {
                SetStatus("自动写入失败：" + ex.Message, true);
            }
        }

        private void OnReload(object sender, EventArgs e)
        {
            if (_dirty && MessageBox.Show(this, "有未写入的修改，重新读取会丢弃它们。继续？", "确认",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;
            LoadStore();
            _dirty = false;
            SetStatus("已重新读取", false);
        }

        private void OnOpenSite(object sender, EventArgs e)
        {
            try { Process.Start("https://mcprofile.io"); }
            catch (Exception ex) { SetStatus("打开网站失败：" + ex.Message, true); }
        }

        private void SelectByKey(string key)
        {
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

        private void SetStatus(string text, bool error)
        {
            _lbStatus.Text = text;
            _lbStatus.ForeColor = error ? Color.Firebrick : Color.SeaGreen;
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
