// 椰子味的魈宝键盘精灵 —— 键盘锁定键 / 输入法状态悬浮指示器
// 显示 大写 / 数字 / 输入法（中·英）三项状态，样式由同目录 panel_bg.png 提供。
// 只读取键盘与输入法状态，不修改系统设置；仅把面板位置与尺寸存在 exe 同目录的 ini。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

// 程序集信息：在“文件属性 → 详细信息”里显示的名称
[assembly: System.Reflection.AssemblyTitle("椰子味的魈宝键盘精灵")]
[assembly: System.Reflection.AssemblyProduct("椰子味的魈宝键盘精灵")]
[assembly: System.Reflection.AssemblyDescription("键盘锁定键与输入法状态悬浮指示器")]
[assembly: System.Reflection.AssemblyCompany("")]
[assembly: System.Reflection.AssemblyCopyright("")]
[assembly: System.Reflection.AssemblyTrademark("")]
[assembly: System.Reflection.AssemblyCulture("")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]

namespace KeyStateTray
{
    // ======================= 原生调用 =======================
    internal static class Native
    {
        [DllImport("user32.dll")] public static extern short GetKeyState(int vk);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vk);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] public static extern IntPtr GetKeyboardLayout(uint tid);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h, IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
        [DllImport("user32.dll")] static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr dst,
            ref POINT dstPt, ref SIZE size, IntPtr src, ref POINT srcPt, int key, ref BLENDFUNCTION blend, int flags);

        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] public struct SIZE { public int W, H; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct BLENDFUNCTION { public byte Op, Flags, Alpha, Format; }

        public const int VK_CAPITAL = 0x14, VK_NUMLOCK = 0x90;
        const int VK_LSHIFT = 0xA0, VK_RSHIFT = 0xA1, VK_SHIFT = 0x10;

        // ---- 锁定键状态 ----
        // GetKeyboardState 依赖线程输入队列，在跨会话时不可靠；
        // 这里一律用 GetKeyState 的 toggle 位（系统级锁存，与焦点无关）。
        public static bool CapsOn { get { return (GetKeyState(VK_CAPITAL) & 1) != 0; } }
        public static bool NumOn { get { return (GetKeyState(VK_NUMLOCK) & 1) != 0; } }

        // ---- 输入法中/英状态 ----
        // 微软拼音是纯 TSF 输入法：ImmGetContext 恒为 0、HKL 在 Shift 切换时不变，
        // 因此改为监听 Shift 抬起沿来翻转本地状态（与系统行为一致）。
        static volatile bool _imeChinese = true;
        static bool _shiftDown, _comboDuringShift;

        public static bool ImeChinese { get { return _imeChinese; } }

        public static void InitIme()
        {
            _imeChinese = IsChineseLayout();
            _shiftDown = ShiftDown();
        }

        static bool ShiftDown()
        {
            return (GetAsyncKeyState(VK_LSHIFT) & 0x8000) != 0
                || (GetAsyncKeyState(VK_RSHIFT) & 0x8000) != 0
                || (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0
                || (GetKeyState(VK_LSHIFT) & 0x8000) != 0
                || (GetKeyState(VK_SHIFT) & 0x8000) != 0;
        }

        // 每拍调用一次：检测 Shift 抬起沿并翻转中/英状态
        public static void PollIme()
        {
            try
            {
                bool down = ShiftDown();

                if (down && !_comboDuringShift && AnyOtherKey()) _comboDuringShift = true;

                if (_shiftDown && !down)
                {
                    if (!_comboDuringShift) _imeChinese = !_imeChinese;
                    _comboDuringShift = false;
                }
                else if (!down) _comboDuringShift = false;

                _shiftDown = down;
            }
            catch { }
        }

        // Shift 按住期间是否夹杂了其它键（排除 Shift+字母 这类组合）
        static bool AnyOtherKey()
        {
            for (int vk = 0x08; vk < 0xFF; vk++)
            {
                if (vk == VK_SHIFT || vk == VK_LSHIFT || vk == VK_RSHIFT) continue;
                if (vk == 0x10 || vk == 0x11 || vk == 0x12) continue;     // 修饰键
                if (vk == 0xA0 || vk == 0xA1 || vk == 0xA2 || vk == 0xA3) continue;
                if (vk == 0x5B || vk == 0x5C) continue;                   // Win
                if (vk == VK_CAPITAL || vk == VK_NUMLOCK || vk == 0x91) continue;
                if ((GetAsyncKeyState(vk) & 0x8000) != 0) return true;
            }
            return false;
        }

        static bool IsChineseLayout()
        {
            try
            {
                IntPtr h = GetForegroundWindow();
                if (h == IntPtr.Zero) return true;
                uint pid;
                uint t = GetWindowThreadProcessId(h, out pid);
                if (t == 0) return true;
                int lang = (int)(GetKeyboardLayout(t).ToInt64() & 0xFFFF);
                return lang == 0x0804 || lang == 0x0404 || lang == 0x1004 || lang == 0x0C04;
            }
            catch { return true; }
        }

        // ---- 全屏（游戏）检测 ----
        // 判断当前前台窗口是否以「独占全屏」方式铺满屏幕。满足以下条件视为全屏应用：
        //   1) 前台窗口存在且可见
        //   2) 不是桌面 / 任务栏 / 本程序自身
        //   3) 窗口矩形覆盖整块屏幕（允许几像素误差）
        // 用于在游戏运行时自动隐藏悬浮面板，避免干扰。
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        // 自身窗口句柄（由主窗体在创建后写入，避免把自己判成全屏程序）
        public static IntPtr SelfHandle = IntPtr.Zero;

        public static bool IsFullscreenAppRunning()
        {
            try
            {
                IntPtr h = GetForegroundWindow();
                if (h == IntPtr.Zero || !IsWindowVisible(h)) return false;
                if (h == SelfHandle) return false;

                // 排除桌面与任务栏
                StringBuilder cls = new StringBuilder(64);
                GetClassNameW(h, cls, 64);
                string c = cls.ToString();
                if (c == "Progman" || c == "WorkerW" || c == "Shell_TrayWnd"
                    || c == "Shell_SecondaryTrayWnd" || c == "Windows.UI.Core.CoreWindow")
                    return false;

                RECT r;
                if (!GetWindowRect(h, out r)) return false;

                // 与主屏尺寸比较（容差 2px）
                Rectangle s = Screen.PrimaryScreen.Bounds;
                int w = r.Right - r.Left;
                int ht = r.Bottom - r.Top;
                return Math.Abs(w - s.Width) <= 2 && Math.Abs(ht - s.Height) <= 2
                       && r.Left <= s.Left + 2 && r.Top <= s.Top + 2;
            }
            catch { return false; }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        // ---- 分层窗口：逐像素 alpha，实现素材的透明背景 ----
        // opacity: 0..255 的整体不透明度，由 BLENDFUNCTION.SourceConstantAlpha 控制
        public static void ApplyLayered(IntPtr hwnd, Bitmap bmp, int x, int y, byte opacity)
        {
            IntPtr screen = GetDC(IntPtr.Zero);
            IntPtr mem = CreateCompatibleDC(screen);
            IntPtr hbmp = bmp.GetHbitmap(Color.FromArgb(0));
            IntPtr old = SelectObject(mem, hbmp);
            try
            {
                POINT dst = new POINT(); dst.X = x; dst.Y = y;
                SIZE sz = new SIZE(); sz.W = bmp.Width; sz.H = bmp.Height;
                POINT src = new POINT();
                BLENDFUNCTION bf = new BLENDFUNCTION();
                bf.Op = 0;                    // AC_SRC_OVER
                bf.Flags = 0;
                bf.Alpha = opacity;           // 整体不透明度（255 = 完全不透明）
                bf.Format = 1;                // AC_SRC_ALPHA
                UpdateLayeredWindow(hwnd, screen, ref dst, ref sz, mem, ref src, 0, ref bf, 2);
            }
            finally
            {
                SelectObject(mem, old);
                DeleteObject(hbmp);
                DeleteDC(mem);
                ReleaseDC(IntPtr.Zero, screen);
            }
        }
    }

    // ======================= 配置 =======================
    internal sealed class AppConfig
    {
        public bool ShowCaps = true;
        public bool ShowNum = true;
        public bool ShowIme = true;
        public int Width = 180;                  // 面板宽度（高度按素材比例推导）
        public int Opacity = 100;                // 不透明度百分比 20..100
        public bool NoFade = false;              // 永不透明（忽略闲置淡出）
        public int HoldSeconds = 5;              // 最后一次输入后保持完全不透明的秒数
        public bool HideInGame = true;           // 检测到全屏游戏时自动隐藏面板
        public string SeenVersion = "";          // 已查看过更新日志的版本号（空 = 从未运行过）
        public Point Location = new Point(-1, -1);

        static string Path_
        {
            get
            {
                return System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Application.ExecutablePath), "XiaoBaoKeySprite.ini");
            }
        }

        public static AppConfig Load()
        {
            AppConfig c = new AppConfig();
            try
            {
                if (!File.Exists(Path_)) return c;
                foreach (string raw in File.ReadAllLines(Path_, Encoding.UTF8))
                {
                    string s = raw.Trim();
                    if (s.Length == 0 || s[0] == ';' || s[0] == '#') continue;
                    int eq = s.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = s.Substring(0, eq).Trim().ToLowerInvariant();
                    string v = s.Substring(eq + 1).Trim();
                    switch (k)
                    {
                        case "caps": c.ShowCaps = v != "0"; break;
                        case "num": c.ShowNum = v != "0"; break;
                        case "ime": c.ShowIme = v != "0"; break;
                        case "width": int w; if (int.TryParse(v, out w)) c.Width = w; break;
                        case "opacity":
                            int o;
                            if (int.TryParse(v, out o)) c.Opacity = Math.Max(20, Math.Min(100, o));
                            break;
                        case "nofade": c.NoFade = v != "0"; break;
                        case "hold":
                            int hs;
                            if (int.TryParse(v, out hs)) c.HoldSeconds = Math.Max(1, Math.Min(60, hs));
                            break;
                        case "hideingame": c.HideInGame = v != "0"; break;
                        case "seenversion": c.SeenVersion = v; break;
                        case "x": int x; if (int.TryParse(v, out x)) c.Location.X = x; break;
                        case "y": int y; if (int.TryParse(v, out y)) c.Location.Y = y; break;
                    }
                }
            }
            catch { }
            return c;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("; 椰子味的魈宝键盘精灵 配置（删除本文件恢复默认）");
                sb.AppendLine("caps=" + (ShowCaps ? 1 : 0));
                sb.AppendLine("num=" + (ShowNum ? 1 : 0));
                sb.AppendLine("ime=" + (ShowIme ? 1 : 0));
                sb.AppendLine("width=" + Width);
                sb.AppendLine("opacity=" + Opacity);
                sb.AppendLine("nofade=" + (NoFade ? 1 : 0));
                sb.AppendLine("hold=" + HoldSeconds);
                sb.AppendLine("hideingame=" + (HideInGame ? 1 : 0));
                sb.AppendLine("seenversion=" + SeenVersion);
                sb.AppendLine("x=" + Location.X);
                sb.AppendLine("y=" + Location.Y);
                File.WriteAllText(Path_, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }
    }

    // ======================= 托盘应用 =======================
    internal sealed class KeyStateTrayApp : ApplicationContext
    {
        // 对外显示的应用名称
        internal const string AppName = "椰子味的魈宝键盘精灵";

        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string AppId = "XiaoBaoKeySprite";

        readonly AppConfig _cfg;
        readonly NotifyIcon _tray;
        readonly KeyStateForm _panel;
        readonly Timer _timer;
        ContextMenuStrip _menu;
        ToolStripMenuItem _capsItem, _numItem, _imeItem, _startupItem;
        bool _caps, _num, _ime;

        public AppConfig Config { get { return _cfg; } }
        public bool CapsState { get { return _caps; } }
        public bool NumState { get { return _num; } }
        public bool ImeState { get { return _ime; } }

        public KeyStateTrayApp()
        {
            try { Native.SetProcessDPIAware(); } catch { }
            _cfg = AppConfig.Load();

            Native.InitIme();

            BuildMenu();
            _tray = new NotifyIcon();
            _tray.ContextMenuStrip = _menu;
            _tray.Visible = true;
            _tray.DoubleClick += delegate { TogglePanel(); };
            _tray.MouseClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) TogglePanel();
            };

            _panel = new KeyStateForm(this);
            _panel.Show();

            _timer = new Timer();
            _timer.Interval = 150;
            _timer.Tick += delegate
            {
                RefreshState();
                _panel.TickOpacity();   // 活跃期结束后自动淡回设定透明度
            };
            _timer.Start();

            RefreshState();

            // 首次运行（或版本更新后首次运行）弹出更新日志
            // 放在最后执行，确保面板与托盘已就绪
            ChangelogForm.ShowIfFirstRun(_cfg);
        }

        void BuildMenu()
        {
            _menu = new ContextMenuStrip();
            // 不指定字体，使用系统默认菜单字体与尺寸

            _capsItem = new ToolStripMenuItem("显示大写锁定", null, delegate { _cfg.ShowCaps = !_cfg.ShowCaps; SaveRefresh(); });
            _numItem = new ToolStripMenuItem("显示数字锁定", null, delegate { _cfg.ShowNum = !_cfg.ShowNum; SaveRefresh(); });
            _imeItem = new ToolStripMenuItem("显示输入法（中/英）", null, delegate { _cfg.ShowIme = !_cfg.ShowIme; SaveRefresh(); });

            ToolStripMenuItem size = new ToolStripMenuItem("面板大小");
            AddSize(size, "特小", 80);
            AddSize(size, "小", 120);
            AddSize(size, "中", 180);
            AddSize(size, "大", 260);
            AddSize(size, "特大", 360);
            size.DropDownItems.Add(new ToolStripSeparator());
            size.DropDownItems.Add(new ToolStripMenuItem("放大一级", null, delegate { SetWidth(_panel.Width + 30); }));
            size.DropDownItems.Add(new ToolStripMenuItem("缩小一级", null, delegate { SetWidth(_panel.Width - 30); }));

            ToolStripMenuItem backBR = new ToolStripMenuItem("面板回到屏幕右下角", null, delegate
            {
                _panel.Show();
                _panel.MoveToBottomRight();
            });

            // 透明度子菜单（与面板右键菜单一致的档位）
            ToolStripMenuItem opa = new ToolStripMenuItem("闲置透明度");
            int[] pcts = new int[] { 100, 90, 80, 70, 60, 50, 40, 30, 20 };
            foreach (int pct in pcts)
            {
                int p = pct;   // 闭包捕获
                string label = p == 100 ? "100%  不透明" : (p == 20 ? "20%  最淡" : p + "%");
                ToolStripMenuItem mi = new ToolStripMenuItem(label, null, delegate { SetOpacity(p); _panel.NotifyActivity(); });
                mi.Tag = p;
                opa.DropDownItems.Add(mi);
            }
            opa.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem noFadeTray = new ToolStripMenuItem("永不透明", null, delegate { SetNoFade(!_cfg.NoFade); });
            opa.DropDownItems.Add(noFadeTray);

            // 保持时长子菜单（与面板右键菜单一致）
            ToolStripMenuItem holdTray = new ToolStripMenuItem("保持完全不透明的时间");
            int[] secsTray = new int[] { 1, 2, 3, 5, 8, 10, 15, 20, 30 };
            foreach (int sc in secsTray)
            {
                int s3 = sc;
                ToolStripMenuItem hi = new ToolStripMenuItem(s3 + " 秒", null, delegate { SetHoldSeconds(s3); });
                hi.Tag = s3;
                holdTray.DropDownItems.Add(hi);
            }

            ToolStripMenuItem reset = new ToolStripMenuItem("恢复默认位置与大小", null, delegate
            {
                _cfg.Width = 180; _cfg.Location = new Point(-1, -1); SaveRefresh();
            });

            ToolStripMenuItem hideGame = new ToolStripMenuItem("全屏游戏时自动隐藏", null, delegate { SetHideInGame(!_cfg.HideInGame); });

            _startupItem = new ToolStripMenuItem("开机自动启动", null, delegate { ToggleStartup(); });

            // 顶部署名项：系统默认菜单样式（单行、默认字体与颜色）
            // 内嵌二维码资源时（脱敏版）可点击弹出二维码，并附带联系提示。
            ToolStripMenuItem credit = new ToolStripMenuItem("by 椰子味");   // && 显示为单个 &
            if (QrForm.Available)
            {
                credit.Enabled = true;
                credit.Text = "by 椰子味  有问题？单击联系我";
                credit.Click += delegate { QrForm.ShowQr(); };
            }
            else
            {
                credit.Enabled = false;
            }

            _menu.Items.Add(credit);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_capsItem);
            _menu.Items.Add(_numItem);
            _menu.Items.Add(_imeItem);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(size);
            _menu.Items.Add(opa);
            _menu.Items.Add(holdTray);
            _menu.Items.Add(hideGame);
            _menu.Items.Add(backBR);
            _menu.Items.Add(reset);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_startupItem);
            _menu.Items.Add(new ToolStripMenuItem("显示/隐藏面板", null, delegate { TogglePanel(); }));
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(new ToolStripMenuItem("退出", null, delegate { ExitApp(); }));

            _menu.Opening += delegate
            {
                _capsItem.Checked = _cfg.ShowCaps;
                _numItem.Checked = _cfg.ShowNum;
                _imeItem.Checked = _cfg.ShowIme;
                _startupItem.Checked = StartupEnabled;
                hideGame.Checked = _cfg.HideInGame;
                // 同步透明度与永不透明勾选
                foreach (ToolStripItem it in opa.DropDownItems)
                {
                    ToolStripMenuItem mi = it as ToolStripMenuItem;
                    if (mi == null) continue;
                    if (mi == noFadeTray) mi.Checked = _cfg.NoFade;
                    else if (mi.Tag is int) mi.Checked = ((int)mi.Tag == _cfg.Opacity);
                }
                // 同步保持时长勾选
                foreach (ToolStripItem it in holdTray.DropDownItems)
                {
                    ToolStripMenuItem mi = it as ToolStripMenuItem;
                    if (mi != null && mi.Tag is int)
                        mi.Checked = ((int)mi.Tag == _cfg.HoldSeconds);
                }
            };
        }

        void AddSize(ToolStripMenuItem parent, string text, int width)
        {
            parent.DropDownItems.Add(new ToolStripMenuItem(text, null, delegate { SetWidth(width); }));
        }

        // 用户是否希望面板可见（区别于「因游戏被自动隐藏」）
        bool _panelWantedVisible = true;

        void TogglePanel()
        {
            _panelWantedVisible = !_panel.Visible;
            if (_panelWantedVisible) { _panel.Show(); _panel.ApplyLayout(); }
            else _panel.Hide();
            RefreshState();
        }

        void SaveRefresh()
        {
            _cfg.Save();
            _panel.ApplyLayout();
            RefreshState();
        }

        public void SetWidth(int w)
        {
            _cfg.Width = Math.Max(80, Math.Min(600, w));
            _cfg.Save();
            _panel.ApplyLayout();
            _panel.Redraw();
        }

        // 设置整体不透明度（百分比 20..100）
        public void SetOpacity(int pct)
        {
            _cfg.Opacity = Math.Max(20, Math.Min(100, pct));
            _cfg.Save();
            _panel.Redraw();
        }

        // 永不透明开关
        public void SetNoFade(bool on)
        {
            _cfg.NoFade = on;
            _cfg.Save();
            _panel.Redraw();
        }

        // 保持完全不透明的时长（秒）
        public void SetHoldSeconds(int s)
        {
            _cfg.HoldSeconds = Math.Max(1, Math.Min(60, s));
            _cfg.Save();
            _panel.Redraw();
        }

        // 游戏时自动隐藏开关
        public void SetHideInGame(bool on)
        {
            _cfg.HideInGame = on;
            _cfg.Save();
            if (!on && _hiddenByGame)
            {
                _hiddenByGame = false;
                if (_panelWantedVisible) { _panel.Show(); _panel.ApplyLayout(); _panel.Redraw(); }
            }
        }

        public void SaveLocation(Point p)
        {
            _cfg.Location = p;
            _cfg.Save();
        }

        // ---- 开机自启（当前用户 Run 项）----
        public bool StartupEnabled
        {
            get
            {
                try
                {
                    using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, false))
                    {
                        if (k == null) return false;
                        string v = k.GetValue(AppId) as string;
                        return !string.IsNullOrEmpty(v) &&
                               v.IndexOf(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
                catch { return false; }
            }
        }

        void ToggleStartup()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (StartupEnabled) k.DeleteValue(AppId, false);
                    else k.SetValue(AppId, "\"" + Application.ExecutablePath + "\"", RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置开机启动失败：" + ex.Message, AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        void ExitApp()
        {
            _cfg.Save();
            _timer.Stop();
            _tray.Visible = false;
            _tray.Dispose();
            _panel.Close();
            ExitThread();
        }

        public void RefreshState()
        {
            bool c = Native.CapsOn;
            bool n = Native.NumOn;

            Native.PollIme();
            bool ime = Native.ImeChinese;

            bool changed = c != _caps || n != _num || ime != _ime;
            _caps = c; _num = n; _ime = ime;

            // 托盘图标是固定的头像，只在首次设置一次，避免反复重建
            if (_tray.Icon == null)
                _tray.Icon = IndicatorIcons.BuildTrayIcon(_cfg, c, n, ime);

            if (changed || string.IsNullOrEmpty(_tray.Text) || _tray.Text.StartsWith(AppName))
            {
                StringBuilder tip = new StringBuilder();
                if (_cfg.ShowCaps) tip.Append("Caps:").Append(c ? "开" : "关").Append("  ");
                if (_cfg.ShowNum) tip.Append("Num:").Append(n ? "开" : "关").Append("  ");
                if (_cfg.ShowIme) tip.Append("输入法:").Append(ime ? "中文" : "英文");
                string t = tip.ToString().Trim();
                _tray.Text = t.Length > 0 ? AppName + "  " + t : AppName;
            }

            // 状态有变化 → 认为有交互，面板临时恢复完全不透明
            if (changed) _panel.NotifyActivity();

            // 全屏游戏检测：游戏中隐藏面板，退出后自动恢复
            UpdateGameAutoHide();

            _panel.Redraw();
        }

        // 上一次的隐藏状态，避免每拍都调用 Show/Hide
        bool _hiddenByGame;

        void UpdateGameAutoHide()
        {
            bool shouldHide = _cfg.HideInGame && Native.IsFullscreenAppRunning();

            if (shouldHide == _hiddenByGame) return;
            _hiddenByGame = shouldHide;

            try
            {
                if (shouldHide)
                {
                    _panel.Hide();
                }
                else if (_panelWantedVisible)
                {
                    // 只有在用户本来就想显示面板时才恢复
                    _panel.Show();
                    _panel.ApplyLayout();
                    _panel.Redraw();
                }
            }
            catch { }
        }
    }

    // ======================= 联系方式窗口 =======================
    // 并排展示微信 / 抖音二维码，资源来自内嵌图片（构建时打包）
    internal sealed class QrForm : Form
    {
        static Image _wechat, _douyin;
        static bool _loaded;

        static Image LoadRes(string name)
        {
            try
            {
                using (Stream s = System.Reflection.Assembly.GetExecutingAssembly()
                           .GetManifestResourceStream(name))
                {
                    if (s == null) return null;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        return Image.FromStream(ms);
                    }
                }
            }
            catch { return null; }
        }

        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _wechat = LoadRes("qr_wechat.jpg");
            _douyin = LoadRes("qr_douyin.jpg");
        }

        /// <summary>是否具备可展示的联系方式（至少一张二维码）。</summary>
        public static bool Available
        {
            get
            {
                EnsureLoaded();
                return _wechat != null || _douyin != null;
            }
        }

        // 弹出联系方式窗口（内置，不依赖外部图片查看器）
        public static void ShowQr()
        {
            try
            {
                using (QrForm f = new QrForm())
                {
                    f.ShowDialog();
                }
            }
            catch { }
        }

        // 单个图块的排版信息
        struct Tile
        {
            public Image Img;
            public string Label;
            public Rectangle Rect;
        }

        Tile[] _tiles = new Tile[0];
        const int CaptionH = 26;      // 每张图下方文字高度
        const int GapX = 18;          // 两图间距
        const int PadOuter = 16;      // 外边距

        public QrForm()
        {
            EnsureLoaded();

            Image[] imgs = new Image[] { _wechat, _douyin };
            string[] labels = new string[] { "微信 · 扫码加好友", "抖音 · 扫码关注" };

            int n = 0;
            foreach (Image im in imgs) if (im != null) n++;
            if (n == 0) return;

            Text = "联系我";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.White;

            // 单张最大边长：按屏幕可用空间计算，两张并排也不越界
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int maxTile = Math.Min(320, Math.Min((wa.Width - PadOuter * 3 - GapX * (n - 1)) / n,
                                                 wa.Height - 160));
            if (maxTile < 140) maxTile = 140;

            // 计算画布
            int totalW = PadOuter * 2 + maxTile * n + GapX * (n - 1);
            int totalH = PadOuter * 2 + maxTile + CaptionH;
            ClientSize = new Size(totalW, totalH);

            // 排布图块
            _tiles = new Tile[n];
            int idx = 0, x = PadOuter;
            for (int i = 0; i < imgs.Length; i++)
            {
                if (imgs[i] == null) continue;
                Tile t = new Tile();
                t.Img = imgs[i];
                t.Label = labels[i];
                t.Rect = new Rectangle(x, PadOuter, maxTile, maxTile);
                _tiles[idx++] = t;
                x += maxTile + GapX;
            }

            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Click += delegate { Close(); };
            KeyPreview = true;
            KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) Close();
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.White);

            using (Pen border = new Pen(Color.FromArgb(225, 225, 225)))
            using (Font cf = new Font("Microsoft YaHei UI", 9f))
            using (Brush cb = new SolidBrush(Color.FromArgb(70, 70, 70)))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                foreach (Tile t in _tiles)
                {
                    if (t.Img == null) continue;
                    g.DrawImage(t.Img, t.Rect);
                    g.DrawRectangle(border, t.Rect.X - 1, t.Rect.Y - 1, t.Rect.Width + 1, t.Rect.Height + 1);

                    Rectangle capRect = new Rectangle(t.Rect.X, t.Rect.Bottom + 2, t.Rect.Width, CaptionH - 4);
                    g.DrawString(t.Label, cf, cb, capRect, sf);
                }
            }
        }
    }

    // ======================= 更新日志窗口 =======================
    // 内容来自内嵌资源 changelog.txt（构建时打包）
    internal sealed class ChangelogForm : Form
    {
        static string _text;
        static bool _loaded;

        public static string LoadText()
        {
            if (_loaded) return _text;
            _loaded = true;
            try
            {
                using (Stream s = System.Reflection.Assembly.GetExecutingAssembly()
                           .GetManifestResourceStream("changelog.txt"))
                {
                    if (s != null)
                        using (StreamReader r = new StreamReader(s, Encoding.UTF8))
                        {
                            string raw = r.ReadToEnd();
                            // 统一换行为 CRLF，Windows 文本框才能正确分行
                            _text = raw.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
                        }
                }
            }
            catch { _text = null; }
            return _text;
        }

        public static bool Available { get { return !string.IsNullOrEmpty(LoadText()); } }

        public ChangelogForm()
        {
            Text = "更新日志";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            AutoScaleMode = AutoScaleMode.None;

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int w = Math.Min(620, Math.Max(420, wa.Width - 400));
            int h = Math.Min(660, Math.Max(380, wa.Height - 180));
            ClientSize = new Size(w, h);

            TextBox tb = new TextBox();
            tb.Multiline = true;
            tb.ReadOnly = true;
            tb.ScrollBars = ScrollBars.Vertical;
            tb.WordWrap = true;                 // 自动换行，避免长行撑出横向滚动条
            tb.BorderStyle = BorderStyle.None;
            tb.BackColor = Color.White;
            tb.ForeColor = Color.FromArgb(30, 30, 30);
            tb.Font = new Font("Microsoft YaHei UI", 9f);
            tb.Dock = DockStyle.Fill;
            tb.Text = Compact(LoadText() ?? "");
            tb.SelectionStart = 0;
            tb.SelectionLength = 0;
            // 去掉文本框默认左右内边距，让内容真正顶格
            tb.HandleCreated += delegate
            {
                const int EM_SETMARGINS = 0xD3;
                const int EC_LEFTMARGIN = 0x1;
                const int EC_RIGHTMARGIN = 0x2;
                Native.SendMessage(tb.Handle, EM_SETMARGINS,
                    (IntPtr)(EC_LEFTMARGIN | EC_RIGHTMARGIN), IntPtr.Zero);
            };

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 46;
            bottom.BackColor = Color.FromArgb(248, 248, 248);

            Button ok = new Button();
            ok.Text = "我知道了";
            ok.Size = new Size(100, 30);
            ok.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            ok.Location = new Point(ClientSize.Width - 116, 8);
            ok.DialogResult = DialogResult.OK;
            bottom.Controls.Add(ok);

            Label tip = new Label();
            tip.Text = "Esc 关闭";
            tip.AutoSize = true;
            tip.ForeColor = Color.Gray;
            tip.Location = new Point(14, 14);
            bottom.Controls.Add(tip);

            Controls.Add(tb);
            Controls.Add(bottom);
            AcceptButton = ok;
            CancelButton = ok;
            KeyPreview = true;
            KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) Close();
            };
        }

        // 把带框线的日志文本精简为紧凑的纯文本
        static string Compact(string src)
        {
            if (string.IsNullOrEmpty(src)) return src;

            const string NL = "\r\n";   // Windows 文本框只认 CRLF 换行
            StringBuilder sb = new StringBuilder(src.Length);
            bool lastWasBlank = false;

            foreach (string raw in src.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n'))
            {
                string line = raw.TrimEnd();

                // 丢弃纯框线/分隔行（整行只由制表符字符组成）
                bool onlyBox = line.Length > 0;
                foreach (char ch in line)
                {
                    if ("─━═╔╗╚╝║┌┐└┘├┤┬┴┼╭╮╰╯▬ ".IndexOf(ch) < 0) { onlyBox = false; break; }
                }
                if (onlyBox) continue;

                // 去掉行首的框线字符与多余空白
                line = line.TrimStart('║', '│', '├', '┤', ' ', '\t');
                // 去掉行尾的框线字符
                line = line.TrimEnd('║', '│', ' ', '\t');

                // 连续空行压缩为一个
                if (line.Length == 0)
                {
                    if (sb.Length == 0 || lastWasBlank) continue;
                    sb.Append(NL);
                    lastWasBlank = true;
                    continue;
                }
                sb.Append(line).Append(NL);
                lastWasBlank = false;
            }
            return sb.ToString().TrimEnd('\r', '\n');
        }

        // 首次运行时弹出；已看过该版本则跳过
        public static void ShowIfFirstRun(AppConfig cfg)
        {
            try
            {
                if (!Available) return;

                string ver = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetName().Version.ToString();
                if (cfg.SeenVersion == ver) return;

                using (ChangelogForm f = new ChangelogForm())
                    f.ShowDialog();

                cfg.SeenVersion = ver;
                cfg.Save();
            }
            catch { }
        }
    }

    // ======================= 绘制 =======================
    internal static class IndicatorIcons
    {
        static Icon _faceIcon;
        static bool _faceTried;

        // 从 exe 内嵌 app.ico 中取出指定尺寸的图标（纯数据切片，不经过重绘，
        // 避免 GetHicon/DrawIcon 破坏抗锯齿边缘的 alpha 通道导致噪点）
        static Icon FaceIcon(int size)
        {
            if (!_faceTried)
            {
                _faceTried = true;
                try
                {
                    using (Stream s = System.Reflection.Assembly.GetExecutingAssembly()
                               .GetManifestResourceStream("app.ico"))
                    {
                        if (s != null)
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                s.CopyTo(ms);
                                ms.Position = 0;
                                _faceIcon = new Icon(ms);   // 保留完整多尺寸
                            }
                        }
                    }
                }
                catch { _faceIcon = null; }
            }
            if (_faceIcon == null) return null;
            try { return new Icon(_faceIcon, size, size); }
            catch { return _faceIcon; }
        }

        // 托盘图标：内嵌头像直接使用（状态信息已在托盘提示与面板中呈现）
        public static Icon BuildTrayIcon(AppConfig cfg, bool caps, bool num, bool ime)
        {
            // 托盘通常按 16/20/24/32 显示，取 32 让系统自行缩放，保证各 DPI 清晰
            return FaceIcon(32);
        }

        // 单个状态块：主值在上、小标题贴底，字号按可用空间自动收缩
        public static void Cell(Graphics g, string title, string value, bool on, Rectangle r,
            Color? bgColor, Color? fgColor)
        {
            Color bg = bgColor ?? (on ? Color.FromArgb(255, 40, 130, 255) : Color.FromArgb(255, 64, 68, 76));
            int rad = Math.Max(4, (int)Math.Round(r.Height * 0.22f));
            using (GraphicsPath p = Round(r, rad))
            using (Brush b = new SolidBrush(bg))
                g.FillPath(b, p);

            Color fc = fgColor ?? Color.White;
            Color tc = fgColor ?? Color.FromArgb(225, 240, 246, 255);

            float ts = Math.Max(6f, r.Height * 0.24f);
            float vs = Math.Max(9f, r.Height * 0.40f);

            // 按宽度约束收缩，避免文字溢出或挤成一团
            using (Font probe = new Font("Microsoft YaHei UI", ts, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                SizeF need = g.MeasureString(title, probe);
                if (need.Width > r.Width * 0.92f) ts = Math.Max(6f, ts * (r.Width * 0.92f) / need.Width);
            }
            using (Font probe = new Font("Microsoft YaHei UI", vs, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                SizeF need = g.MeasureString(value, probe);
                if (need.Width > r.Width * 0.88f) vs = Math.Max(9f, vs * (r.Width * 0.88f) / need.Width);
            }

            using (Font tf = new Font("Microsoft YaHei UI", ts, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Font vf = new Font("Microsoft YaHei UI", vs, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                float th = ts + 4f;
                using (Brush vb = new SolidBrush(fc))
                    g.DrawString(value, vf, vb, new RectangleF(r.X, r.Y + 1, r.Width, r.Height - th - 1), sf);
                using (Brush tb = new SolidBrush(tc))
                    g.DrawString(title, tf, tb, new RectangleF(r.X, r.Bottom - th, r.Width, th), sf);
            }
        }

        static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // ======================= 悬浮面板 =======================
    internal sealed class KeyStateForm : Form
    {
        // 素材 panel_bg.png 原始尺寸，及其中可绘制的灰绿方框（逐像素实测）
        const int SrcW = 938, SrcH = 958;
        const int BoxX = 48, BoxY = 611, BoxW = 843, BoxH = 301;

        static Image _bg;
        static bool _bgLoaded;

        readonly KeyStateTrayApp _app;
        bool _dragging;
        Point _dragStart;

        public KeyStateForm(KeyStateTrayApp app)
        {
            // 必须在设置尺寸前关闭自动缩放，否则 WinForms 会按系统 DPI 再缩放一次，
            // 导致高 DPI 下窗口被缩小、内容错位。
            AutoScaleMode = AutoScaleMode.None;
            AutoScaleDimensions = new SizeF(1f, 1f);

            _app = app;
            Text = KeyStateTrayApp.AppName;   // 窗口标题（任务管理器/Alt+Tab 显示用）
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;   // 不在任务栏显示图标，只保留托盘图标
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            ApplyLayout();

            MouseDown += delegate(object s, MouseEventArgs e)
            {
                // 任何点击（含右键菜单前）都视为交互，先恢复完全不透明
                NotifyActivity();
                if (e.Button == MouseButtons.Left) { _dragging = true; _dragStart = e.Location; }
                else if (e.Button == MouseButtons.Right) ShowPanelMenu();
            };
            MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (!_dragging) return;
                Point p = PointToScreen(e.Location);
                // 拖动时实时约束在工作区内，避免盖住任务栏或拖出屏幕
                Location = ClampToWorkArea(new Point(p.X - _dragStart.X, p.Y - _dragStart.Y), Width, Height);
            };
            MouseUp += delegate
            {
                if (!_dragging) return;
                _dragging = false;
                _app.SaveLocation(Location);
            };

            BuildPanelMenu();
        }

        void BuildPanelMenu()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            // 不指定字体，使用系统默认菜单字体与尺寸

            // 顶部署名项：系统默认菜单样式（单行、默认字体与颜色）
            // 内嵌二维码资源时（脱敏版）可点击弹出二维码，并附带联系提示。
            ToolStripMenuItem credit = new ToolStripMenuItem("by 椰子味");   // && 显示为单个 &
            if (QrForm.Available)
            {
                credit.Enabled = true;
                credit.Text = "by 椰子味  有问题？单击联系我";
                credit.Click += delegate { QrForm.ShowQr(); };
            }
            else
            {
                credit.Enabled = false;
            }
            m.Items.Add(credit);
            m.Items.Add(new ToolStripSeparator());

            m.Items.Add(new ToolStripMenuItem("↘  回到屏幕右下角", null, delegate { MoveToBottomRight(); }));
            m.Items.Add(new ToolStripSeparator());

            AddSize(m, "特小", 80);
            AddSize(m, "小", 120);
            AddSize(m, "中", 180);
            AddSize(m, "大", 260);
            AddSize(m, "特大", 360);
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(new ToolStripMenuItem("放大一级", null, delegate { _app.SetWidth(Width + 30); }));
            m.Items.Add(new ToolStripMenuItem("缩小一级", null, delegate { _app.SetWidth(Width - 30); }));

            // 透明度子菜单
            ToolStripMenuItem op = new ToolStripMenuItem("闲置透明度");
            AddOpacity(op, "100%  不透明", 100);
            AddOpacity(op, "90%", 90);
            AddOpacity(op, "80%", 80);
            AddOpacity(op, "70%", 70);
            AddOpacity(op, "60%", 60);
            AddOpacity(op, "50%", 50);
            AddOpacity(op, "40%", 40);
            AddOpacity(op, "30%", 30);
            AddOpacity(op, "20%  最淡", 20);
            op.DropDownItems.Add(new ToolStripSeparator());

            // 永不透明开关
            ToolStripMenuItem noFade = new ToolStripMenuItem("永不透明", null, delegate
            {
                _app.SetNoFade(!_app.Config.NoFade);
            });
            op.DropDownItems.Add(noFade);

            // 保持时长子菜单
            ToolStripMenuItem hold = new ToolStripMenuItem("保持完全不透明的时间");
            int[] secs = new int[] { 1, 2, 3, 5, 8, 10, 15, 20, 30 };
            foreach (int sc in secs)
            {
                int s2 = sc;
                ToolStripMenuItem hi = new ToolStripMenuItem(s2 + " 秒", null, delegate { _app.SetHoldSeconds(s2); });
                hi.Tag = s2;
                hold.DropDownItems.Add(hi);
            }

            // 全屏游戏时自动隐藏
            ToolStripMenuItem hideGamePanel = new ToolStripMenuItem("全屏游戏时自动隐藏", null,
                delegate { _app.SetHideInGame(!_app.Config.HideInGame); });

            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(op);
            m.Items.Add(hold);
            m.Items.Add(hideGamePanel);

            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(new ToolStripMenuItem("隐藏面板", null, delegate { Hide(); }));

            // 每次弹出菜单时刷新勾选状态
            m.Opening += delegate
            {
                foreach (ToolStripItem it in op.DropDownItems)
                {
                    ToolStripMenuItem mi = it as ToolStripMenuItem;
                    if (mi == null) continue;
                    if (mi == noFade) mi.Checked = _app.Config.NoFade;
                    else if (mi.Tag is int) mi.Checked = ((int)mi.Tag == _app.Config.Opacity);
                }
                foreach (ToolStripItem it in hold.DropDownItems)
                {
                    ToolStripMenuItem mi = it as ToolStripMenuItem;
                    if (mi != null && mi.Tag is int)
                        mi.Checked = ((int)mi.Tag == _app.Config.HoldSeconds);
                }
                hideGamePanel.Checked = _app.Config.HideInGame;
            };

            // 关键：不设置 ContextMenuStrip 属性。
            // 面板窗口带 WS_EX_NOACTIVATE（不抢焦点），WinForms 无法感知“点击外部”
            // 因而菜单不会自动关闭。这里改用全局低级鼠标/键盘钩子主动监测外部点击。
            _menu = m;
            m.ShowImageMargin = false;
            m.AutoClose = true;
            // 外部点击由全局钩子负责关闭；任何方式关闭后都卸载钩子
            m.Closed += delegate { RemoveMenuHooks(); };

            // 兜底：选中任意菜单项后主动关闭。
            // 面板窗口不抢焦点，AutoClose 在部分情况下不生效，这里显式处理。
            m.ItemClicked += delegate { CloseMenu(); };
        }

        ContextMenuStrip _menu;

        // ---- 菜单外部点击检测（全局钩子）----
        const int WH_MOUSE_LL = 14;
        const int WH_KEYBOARD_LL = 13;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_RBUTTONDOWN = 0x0204;
        const int WM_MBUTTONDOWN = 0x0207;
        const int WM_KEYDOWN = 0x0100;
        const int WM_SYSKEYDOWN = 0x0104;

        delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        IntPtr _mouseHook = IntPtr.Zero;
        IntPtr _keyHook = IntPtr.Zero;
        HookProc _mouseProc, _keyProc;

        public void ShowPanelMenu()
        {
            if (_menu == null || _menu.Visible) return;
            try
            {
                _menu.Show(Cursor.Position);
                InstallMenuHooks();
            }
            catch { }
        }

        void InstallMenuHooks()
        {
            try
            {
                if (_mouseHook == IntPtr.Zero)
                {
                    _mouseProc = MenuMouseHook;
                    IntPtr mod = GetModuleHandle(null);
                    _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, mod, 0);
                }
                if (_keyHook == IntPtr.Zero)
                {
                    _keyProc = MenuKeyHook;
                    IntPtr mod = GetModuleHandle(null);
                    _keyHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyProc, mod, 0);
                }
            }
            catch { }
        }

        void RemoveMenuHooks()
        {
            try
            {
                if (_mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(_mouseHook); _mouseHook = IntPtr.Zero; }
                if (_keyHook != IntPtr.Zero) { UnhookWindowsHookEx(_keyHook); _keyHook = IntPtr.Zero; }
            }
            catch { }
        }

        // 鼠标按下：如果在菜单矩形之外，则关闭菜单
        IntPtr MenuMouseHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && _menu != null && _menu.Visible)
                {
                    int msg = wParam.ToInt32();
                    if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN)
                    {
                        // MSLLHOOKSTRUCT: pt 在偏移 0（两个 int）
                        int x = Marshal.ReadInt32(lParam, 0);
                        int y = Marshal.ReadInt32(lParam, 4);

                        Rectangle r = _menu.Bounds;
                        if (!r.Contains(x, y))
                        {
                            CloseMenu();
                        }
                    }
                }
            }
            catch { }
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        // 按键：Esc 也关闭菜单
        IntPtr MenuKeyHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && _menu != null && _menu.Visible)
                {
                    int msg = wParam.ToInt32();
                    if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                    {
                        int vk = Marshal.ReadInt32(lParam, 0);
                        if (vk == 0x1B) CloseMenu();   // VK_ESCAPE
                    }
                }
            }
            catch { }
            return CallNextHookEx(_keyHook, nCode, wParam, lParam);
        }

        void CloseMenu()
        {
            try { if (_menu != null && _menu.Visible) _menu.Close(); } catch { }
            RemoveMenuHooks();
        }

        void AddOpacity(ToolStripMenuItem parent, string text, int pct)
        {
            ToolStripMenuItem mi = new ToolStripMenuItem(text, null, delegate
            {
                _app.SetOpacity(pct);
                NotifyActivity();   // 设完立即以完全不透明展示一次，便于确认
            });
            mi.Tag = pct;
            parent.DropDownItems.Add(mi);
        }

        void AddSize(ContextMenuStrip m, string text, int w)
        {
            m.Items.Add(new ToolStripMenuItem(text, null, delegate { _app.SetWidth(w); }));
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW：不出现在任务栏与 Alt+Tab
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE：点击面板不抢前台焦点
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED  ：启用逐像素透明
                return cp;
            }
        }

        static Image Background()
        {
            if (_bgLoaded) return _bg;
            _bgLoaded = true;

            // 1) 优先读取 exe 内嵌资源（单文件分发）
            try
            {
                using (Stream s = System.Reflection.Assembly.GetExecutingAssembly()
                           .GetManifestResourceStream("panel_bg.png"))
                {
                    if (s != null)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            s.CopyTo(ms);
                            _bg = Image.FromStream(ms);
                        }
                    }
                }
                if (_bg != null) return _bg;
            }
            catch { }

            // 2) 回退：exe 同目录的 panel_bg.png（便于自行替换皮肤）
            try
            {
                string dir = Path.GetDirectoryName(Application.ExecutablePath);
                string p = Path.Combine(dir, "panel_bg.png");
                if (!File.Exists(p)) p = Path.Combine(Path.Combine(dir, "assets"), "panel_bg.png");
                if (File.Exists(p)) _bg = Image.FromStream(new MemoryStream(File.ReadAllBytes(p)));
            }
            catch { _bg = null; }

            return _bg;
        }

        // 取面板所在屏幕的有效工作区（已排除任务栏），用于约束面板位置
        Rectangle WorkBounds()
        {
            try
            {
                Screen s = Screen.FromControl(this);
                if (s != null) return s.WorkingArea;
            }
            catch { }
            return Screen.PrimaryScreen.WorkingArea;
        }

        // 把矩形限制在屏幕工作区内（不遮挡任务栏、不越出屏幕）
        Point ClampToWorkArea(Point p, int w, int h)
        {
            Rectangle wa = WorkBounds();
            int maxX = Math.Max(wa.Left, wa.Right - w);
            int maxY = Math.Max(wa.Top, wa.Bottom - h);
            return new Point(
                Math.Min(Math.Max(p.X, wa.Left), maxX),
                Math.Min(Math.Max(p.Y, wa.Top), maxY));
        }

        public void ApplyLayout()
        {
            // 严格保持素材 938:958 的宽高比，绝不拉伸变形
            int w = Math.Max(80, Math.Min(600, _app.Config.Width));
            int h = (int)Math.Round(w * (double)SrcH / SrcW);
            Size = new Size(w, h);

            Rectangle wa = WorkBounds();
            Point loc = _app.Config.Location;

            // 首次启动（配置为 -1,-1）或保存的位置完全在工作区外时，
            // 默认放到屏幕右下角（贴边留 16px 间距）
            bool noSaved = loc.X < 0 || loc.Y < 0;
            bool outOfScreen = loc.X >= wa.Right || loc.Y >= wa.Bottom
                               || loc.X + w <= wa.Left || loc.Y + h <= wa.Top;
            if (noSaved || outOfScreen)
                loc = new Point(wa.Right - w - 16, wa.Bottom - h - 16);
            else
                loc = ClampToWorkArea(loc, w, h);

            Location = loc;
        }

        // 一键回到屏幕右下角
        public void MoveToBottomRight()
        {
            Rectangle wa = WorkBounds();
            Location = ClampToWorkArea(
                new Point(wa.Right - Width - 16, wa.Bottom - Height - 16),
                Width, Height);
            _app.SaveLocation(Location);
            Redraw();
        }

        // ---- 智能透明度 ----
        // 仅当「状态发生变化」（大写/数字/输入法）时才临时完全不透明；
        // 鼠标移动、普通按键都不会触发，避免一直亮着。
        int _activeUntil;          // 保持完全不透明的截止时刻（TickCount）

        int HoldMs()
        {
            int s = Math.Max(1, Math.Min(60, _app.Config.HoldSeconds));
            return s * 1000;
        }

        // 通知面板：状态刚发生变化 → 从此刻起保持完全不透明 HoldMs 毫秒
        public void NotifyActivity()
        {
            _activeUntil = unchecked(Environment.TickCount + HoldMs());
            Redraw();
        }

        // 是否处于「刚变化」的活跃期
        bool IsActive()
        {
            return unchecked(Environment.TickCount - _activeUntil) < 0;
        }

        // 当前应使用的 alpha（0..255）
        byte CurrentAlpha()
        {
            // 「永不透明」开关打开时始终 255
            if (_app.Config.NoFade) return 255;

            int pct = Math.Max(20, Math.Min(100, _app.Config.Opacity));

            // 设定为 100% 本来就不透明，无需计时
            if (pct >= 100) return 255;

            // 状态刚变化不久 → 完全不透明
            if (IsActive()) return 255;

            return (byte)Math.Round(pct * 255.0 / 100.0);
        }

        public void Redraw()
        {
            if (!IsHandleCreated || Width <= 0 || Height <= 0) return;
            try
            {
                using (Bitmap canvas = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(canvas))
                        RenderTo(g);
                    Native.ApplyLayered(Handle, canvas, Left, Top, CurrentAlpha());
                }
            }
            catch { }
        }

        // 由定时器驱动：输入停止超过 ActiveHoldMs 后自动淡回设定透明度
        bool _wasActive;

        public void TickOpacity()
        {
            // 「永不透明」或设定 100% 时无需处理
            if (_app.Config.NoFade) return;
            int pct = Math.Max(20, Math.Min(100, _app.Config.Opacity));
            if (pct >= 100) return;

            // 只在「活跃 → 非活跃」切换的那一刻重绘一次（淡出）
            bool active = IsActive();
            if (active != _wasActive)
            {
                _wasActive = active;
                Redraw();
            }
        }

        void RenderTo(Graphics g)
        {
            g.Clear(Color.Transparent);          // 透明底，素材外的区域保持透明
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            Image bg = Background();
            if (bg == null) return;

            // 等比铺底
            float s = Math.Min(Width / (float)SrcW, Height / (float)SrcH);
            int dw = (int)Math.Round(SrcW * s), dh = (int)Math.Round(SrcH * s);
            int dx = (Width - dw) / 2, dy = (Height - dh) / 2;
            g.DrawImage(bg, new Rectangle(dx, dy, dw, dh));

            // 素材里的灰绿方框位置，换算到当前缩放
            Rectangle box = new Rectangle(
                dx + (int)Math.Round(BoxX * s), dy + (int)Math.Round(BoxY * s),
                (int)Math.Round(BoxW * s), (int)Math.Round(BoxH * s));

            DrawCells(g, box);
        }

        void DrawCells(Graphics g, Rectangle box)
        {
            List<string[]> items = new List<string[]>();
            List<bool> states = new List<bool>();
            List<Color?[]> colors = new List<Color?[]>();

            if (_app.Config.ShowCaps)
            {
                items.Add(new string[] { "大写", _app.CapsState ? "开" : "关" });
                states.Add(_app.CapsState); colors.Add(new Color?[] { null, null });
            }
            if (_app.Config.ShowNum)
            {
                items.Add(new string[] { "数字", _app.NumState ? "开" : "关" });
                states.Add(_app.NumState); colors.Add(new Color?[] { null, null });
            }
            if (_app.Config.ShowIme)
            {
                bool zh = _app.ImeState;
                items.Add(new string[] { "输入法", zh ? "中" : "英" });
                states.Add(zh);
                // 中文绿底黑字 / 英文红底黑字，均为高对比
                colors.Add(zh
                    ? new Color?[] { Color.FromArgb(255, 40, 180, 70), Color.Black }
                    : new Color?[] { Color.FromArgb(255, 220, 50, 50), Color.Black });
            }
            if (items.Count == 0) return;

            int n = items.Count;
            int px = (int)Math.Round(box.Width * 0.05f);
            int py = (int)Math.Round(box.Height * 0.14f);
            int aw = box.Width - px * 2, ah = box.Height - py * 2;
            if (aw <= 0 || ah <= 0) return;

            int gap = (int)Math.Round(aw * 0.035f);
            int cw = (aw - gap * (n - 1)) / n;
            int ch = ah;
            if (cw > ch * 2.0) cw = (int)Math.Round(ch * 2.0);
            if (ch > cw * 1.6) ch = (int)Math.Round(cw * 1.6);

            int total = cw * n + gap * (n - 1);
            int x = box.X + (box.Width - total) / 2;
            int y = box.Y + (box.Height - ch) / 2;

            for (int i = 0; i < n; i++)
            {
                IndicatorIcons.Cell(g, items[i][0], items[i][1], states[i],
                    new Rectangle(x, y, cw, ch), colors[i][0], colors[i][1]);
                x += cw + gap;
            }
        }

        protected override void OnPaint(PaintEventArgs e) { Redraw(); }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Native.SelfHandle = Handle;   // 记录自身句柄，避免被全屏检测误判
            Redraw();
        }
        protected override void OnMove(EventArgs e) { base.OnMove(e); Redraw(); }
        protected override bool ShowWithoutActivation { get { return true; } }
    }

    // ======================= 入口 =======================
    internal static class Program
    {
        [DllImport("user32.dll")]
        static extern IntPtr SetProcessDpiAwarenessContext(IntPtr v);
        [DllImport("shcore.dll")]
        static extern int SetProcessDpiAwareness(int v);

        [STAThread]
        static void Main()
        {
            // 高 DPI 感知：Per-Monitor V2 → Per-Monitor → 系统级，逐级降级
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); }
            catch
            {
                try { SetProcessDpiAwareness(2); }
                catch { try { Native.SetProcessDPIAware(); } catch { } }
            }

            bool createdNew;
            using (System.Threading.Mutex m = new System.Threading.Mutex(
                true, "XiaoBaoKeySprite_SingleInstance_Mutex", out createdNew))
            {
                if (!createdNew) return;

                string crash = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath), "crash.log");

                AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
                {
                    try { File.AppendAllText(crash, DateTime.Now + " UNHANDLED:\r\n" + e.ExceptionObject + "\r\n\r\n", Encoding.UTF8); }
                    catch { }
                };

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e)
                {
                    try { File.AppendAllText(crash, DateTime.Now + " THREAD:\r\n" + e.Exception + "\r\n\r\n", Encoding.UTF8); }
                    catch { }
                };

                try { Application.Run(new KeyStateTrayApp()); }
                catch (Exception ex)
                {
                    try { File.AppendAllText(crash, DateTime.Now + " RUN:\r\n" + ex + "\r\n\r\n", Encoding.UTF8); }
                    catch { }
                }
            }
        }
    }
}
