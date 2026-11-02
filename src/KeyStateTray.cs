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
[assembly: System.Reflection.AssemblyDescription("键盘状态悬浮指示器 + 桌面宠物：显示锁定键与输入法，可喂食互动")]
[assembly: System.Reflection.AssemblyCompany("")]
[assembly: System.Reflection.AssemblyCopyright("")]
[assembly: System.Reflection.AssemblyTrademark("")]
[assembly: System.Reflection.AssemblyCulture("")]
[assembly: System.Reflection.AssemblyVersion("2.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("2.0.0.0")]

namespace KeyStateTray
{
    // ======================= 原生调用 =======================
    internal static class Native
    {
        // ---- 系统深色模式检测 ----
        public static bool IsDarkMode()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser
                           .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("AppsUseLightTheme");
                        if (v is int) return (int)v == 0;
                    }
                }
            }
            catch { }
            return false;
        }

        // 按系统主题给菜单上色
        public static void ApplyMenuTheme(ContextMenuStrip menu)
        {
            if (menu == null) return;

            bool dark = IsDarkMode();
            if (dark)
            {
                menu.BackColor = Color.FromArgb(43, 43, 45);
                menu.ForeColor = Color.FromArgb(235, 235, 238);
                menu.Renderer = new DarkMenuRenderer();
            }
            else
            {
                Color defBack = SystemColors.Control;
                Color defFore = SystemColors.ControlText;
                try
                {
                    ToolStripProfessionalRenderer pr = new ToolStripProfessionalRenderer();
                    menu.Renderer = pr;
                    defBack = pr.ColorTable.ToolStripDropDownBackground;
                }
                catch { menu.Renderer = new ToolStripProfessionalRenderer(); }
                menu.BackColor = defBack;
                menu.ForeColor = defFore;
            }
        }

        // 深色菜单渲染器
        sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
        {
            public DarkMenuRenderer() : base(new DarkColorTable()) { }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                if (e.Item.Enabled)
                {
                    // 保留开关项设置的自定义颜色（绿/灰）
                    if (e.Item.ForeColor == Color.Empty)
                        e.TextColor = Color.FromArgb(235, 235, 238);
                }
                else
                {
                    e.TextColor = Color.FromArgb(120, 120, 125);
                }
                base.OnRenderItemText(e);
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(43, 43, 45)))
                    e.Graphics.FillRectangle(b, e.AffectedBounds);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using (Pen p = new Pen(Color.FromArgb(70, 70, 74)))
                    e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.Selected || !e.Item.Enabled) return;
                Rectangle r = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(72, 72, 78)))
                    e.Graphics.FillRectangle(b, r);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                Rectangle r = new Rectangle(28, e.Item.Height / 2, e.Item.Width - 36, 1);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(75, 75, 80)))
                    e.Graphics.FillRectangle(b, r);
            }
        }

        // 深色配色表
        sealed class DarkColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected { get { return Color.FromArgb(72, 72, 78); } }
            public override Color MenuItemBorder { get { return Color.FromArgb(72, 72, 78); } }
            public override Color MenuBorder { get { return Color.FromArgb(70, 70, 74); } }
            public override Color ToolStripDropDownBackground { get { return Color.FromArgb(43, 43, 45); } }
            public override Color ImageMarginGradientBegin { get { return Color.FromArgb(43, 43, 45); } }
            public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(43, 43, 45); } }
            public override Color ImageMarginGradientEnd { get { return Color.FromArgb(43, 43, 45); } }
            public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(72, 72, 78); } }
            public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(72, 72, 78); } }
            public override Color MenuItemPressedGradientBegin { get { return Color.FromArgb(60, 60, 65); } }
            public override Color MenuItemPressedGradientEnd { get { return Color.FromArgb(60, 60, 65); } }
            public override Color SeparatorDark { get { return Color.FromArgb(75, 75, 80); } }
            public override Color SeparatorLight { get { return Color.FromArgb(55, 55, 58); } }
            public override Color CheckBackground { get { return Color.FromArgb(72, 72, 78); } }
            public override Color CheckSelectedBackground { get { return Color.FromArgb(85, 85, 92); } }
            public override Color CheckPressedBackground { get { return Color.FromArgb(85, 85, 92); } }
        }

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

        // ---- 输入法识别 ----
        // 通过前台窗口线程的 HKL 读取当前键盘布局，映射为输入法显示名。
        // 英语类布局显示「英语」，中文类布局显示「微软拼音」等。
        static volatile string _imeName = "英语";
        static volatile bool _imeChinese = false;

        public static bool ImeChinese { get { return _imeChinese; } }
        public static string ImeName { get { return _imeName; } }

        public static void InitIme()
        {
            PollIme();
        }

        // 每拍调用：读取当前输入法名称
        public static void PollIme()
        {
            try
            {
                IntPtr h = GetForegroundWindow();
                if (h == IntPtr.Zero) return;
                uint pid;
                uint t = GetWindowThreadProcessId(h, out pid);
                if (t == 0) return;

                long hkl = GetKeyboardLayout(t).ToInt64();
                int lang = (int)(hkl & 0xFFFF);

                string name;
                bool zh;
                switch (lang)
                {
                    case 0x0804:   // 简体中文
                    case 0x1004:   // 新加坡中文
                        name = "拼音"; zh = true; break;
                    case 0x0404:   // 繁体中文（台湾）
                    case 0x0C04:   // 繁体中文（香港）
                        name = "注音"; zh = true; break;
                    case 0x0409:   // 美国英语
                    case 0x0809:   // 英国英语
                    case 0x0C09:   // 澳洲英语
                    case 0x1009:   // 加拿大英语
                        name = "英语"; zh = false; break;
                    case 0x0411:   // 日语
                        name = "日语"; zh = false; break;
                    case 0x0412:   // 韩语
                        name = "韩语"; zh = false; break;
                    case 0x0407:   // 德语
                        name = "德语"; zh = false; break;
                    case 0x040C:   // 法语
                        name = "法语"; zh = false; break;
                    case 0x0410:   // 意大利语
                        name = "意语"; zh = false; break;
                    case 0x040A:   // 西班牙语
                        name = "西语"; zh = false; break;
                    case 0x0419:   // 俄语
                        name = "俄语"; zh = false; break;
                    default:
                        // 中文系语言 ID 的低 10 位为 0x004
                        zh = (lang & 0x3FF) == 0x004;
                        name = zh ? "中文" : "其他";
                        break;
                }

                // 通用简化：去掉常见厂商前缀与冗余后缀
                name = SimplifyImeName(name);

                _imeName = name;
                _imeChinese = zh;
            }
            catch { }
        }

        // 输入法名简化：微软拼音→拼音、搜狗五笔→五笔、美式英语→英语
        static string SimplifyImeName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "其他";

            // 去掉厂商前缀
            string[] prefixes = new string[] {
                "微软", "搜狗", "百度", "QQ", "腾讯", "讯飞", "华宇", "小狼毫",
                "极点", "紫光", "谷歌", "手心", "万能", "美式", "英式", "澳式", "加式"
            };
            foreach (string p in prefixes)
                if (s.StartsWith(p, StringComparison.Ordinal))
                {
                    s = s.Substring(p.Length).TrimStart(' ', '-', '_', '·');
                    break;
                }

            // 去掉冗余后缀
            string[] suffixes = new string[] {
                "输入法", "输入方法", "键位", "键盘", "版", "风格"
            };
            foreach (string suf in suffixes)
                if (s.EndsWith(suf, StringComparison.Ordinal) && s.Length > suf.Length)
                {
                    s = s.Substring(0, s.Length - suf.Length).TrimEnd(' ', '-', '_', '·');
                    break;
                }

            return string.IsNullOrEmpty(s) ? "其他" : s;
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
        public int Width = 340;                  // 面板宽度（高度按素材比例推导）
        public int Opacity = 100;                // 不透明度百分比 20..100
        public bool NoFade = false;              // 永不透明（忽略闲置淡出）
        public int HoldSeconds = 5;              // 最后一次输入后保持完全不透明的秒数
        public bool HideInGame = true;           // 检测到全屏游戏时自动隐藏面板
        public string SeenVersion = "";          // 已查看过更新日志的版本号（空 = 从未运行过）
        public Point Location = new Point(-1, -1);

        // ---- 桌面宠物 ----
        public bool ShowPet = false;             // 是否显示桌宠
        public int PetSize = 180;                // 桌宠宽度（像素）
        public bool PetWalk = true;              // 是否自动走动
        public Point PetLocation = new Point(-1, -1);

        // ---- 喂食系统 ----
        public int Hunger = 0;                   // 饥饿值 0(饱)~100(饿)，随时间增长
        public long LastFedTicks = 0;            // 上次喂食时间（UTC ticks）
        public int FeedCount = 0;                // 累计喂食次数
        public int HungerRateMinutes = 45;       // 每多少秒饥饿 +1（秒数），越小越容易饿

        // ---- 整点提醒 ----
        public bool HourlyRemind = false;        // 整点是否提醒
        public string LastHourlyKey = "";        // 上次提醒的整点标识（yyyyMMddHH）

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
                        case "showpet": c.ShowPet = v != "0"; break;
                        case "petsize":
                            int ps;
                            if (int.TryParse(v, out ps)) c.PetSize = Math.Max(80, Math.Min(400, ps));
                            break;
                        case "petwalk": c.PetWalk = v != "0"; break;
                        case "petx": int pxx; if (int.TryParse(v, out pxx)) c.PetLocation.X = pxx; break;
                        case "pety": int pyy; if (int.TryParse(v, out pyy)) c.PetLocation.Y = pyy; break;
                        case "hunger":
                            int hg;
                            if (int.TryParse(v, out hg)) c.Hunger = Math.Max(0, Math.Min(100, hg));
                            break;
                        case "lastfed":
                            long lf;
                            if (long.TryParse(v, out lf)) c.LastFedTicks = lf;
                            break;
                        case "feedcount":
                            int fc;
                            if (int.TryParse(v, out fc)) c.FeedCount = Math.Max(0, fc);
                            break;
                        case "hungerrate":
                            int hr;
                            if (int.TryParse(v, out hr)) c.HungerRateMinutes = Math.Max(2, Math.Min(3600, hr));
                            break;
                        case "hourlyremind": c.HourlyRemind = v != "0"; break;
                        case "lasthourly": c.LastHourlyKey = v ?? ""; break;
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
                sb.AppendLine("showpet=" + (ShowPet ? 1 : 0));
                sb.AppendLine("petsize=" + PetSize);
                sb.AppendLine("petwalk=" + (PetWalk ? 1 : 0));
                sb.AppendLine("petx=" + PetLocation.X);
                sb.AppendLine("pety=" + PetLocation.Y);
                sb.AppendLine("hunger=" + Hunger);
                sb.AppendLine("lastfed=" + LastFedTicks);
                sb.AppendLine("feedcount=" + FeedCount);
                sb.AppendLine("hungerrate=" + HungerRateMinutes);
                sb.AppendLine("hourlyremind=" + (HourlyRemind ? 1 : 0));
                sb.AppendLine("lasthourly=" + LastHourlyKey);
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

        // 当前输入法显示名（英语 / 微软拼音 / 日语 …）
        public string ImeName { get { return Native.ImeName; } }

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

            // 桌宠：按配置决定是否创建
            if (_cfg.ShowPet && PetForm.Available) EnsurePet();

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
            Native.ApplyMenuTheme(_menu);
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
            KeyStateForm.EnableRightClickSubmenu(opa);
            int[] pcts = new int[] { 100, 90, 80, 70, 60, 50, 40, 30, 20 };
            foreach (int pct in pcts)
            {
                int p = pct;   // 闭包捕获
                string label = p == 100 ? "100%  不透明" : (p == 20 ? "20%  最淡" : p + "%");
                ToolStripMenuItem mi = new ToolStripMenuItem(label, null, delegate { SetOpacity(p); _panel.ApplyOpacityNow(); });
                mi.Tag = p;
                opa.DropDownItems.Add(mi);
            }
            opa.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem noFadeTray = new ToolStripMenuItem("永不透明", null, delegate { SetNoFade(!_cfg.NoFade); });
            opa.DropDownItems.Add(noFadeTray);

            // 保持时长子菜单（与面板右键菜单一致）
            ToolStripMenuItem holdTray = new ToolStripMenuItem("保持完全不透明的时间");
            KeyStateForm.EnableRightClickSubmenu(holdTray);
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
                _cfg.Width = 340; _cfg.Location = new Point(-1, -1); SaveRefresh();
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
                credit.Click += delegate { QrForm.ShowQrWithTests(this); };
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

            // 桌宠开关
            ToolStripMenuItem petItem = new ToolStripMenuItem("显示桌面宠物", null, delegate
            {
                if (!PetForm.Available)
                {
                    MessageBox.Show("未找到桌宠素材，无法启用。", KeyStateTrayApp.AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                TogglePet();
            });
            _menu.Items.Add(petItem);

            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(new ToolStripMenuItem("退出", null, delegate { ExitApp(); }));

            _menu.Opening += delegate
            {
                // 重新检测系统主题
                Native.ApplyMenuTheme(_menu);

                _capsItem.Checked = _cfg.ShowCaps;
                _numItem.Checked = _cfg.ShowNum;
                _imeItem.Checked = _cfg.ShowIme;
                _startupItem.Checked = StartupEnabled;
                hideGame.Checked = _cfg.HideInGame;
                petItem.Checked = PetVisible;
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
            _panel.ApplyOpacityNow();
        }

        // 永不透明开关
        public void SetNoFade(bool on)
        {
            _cfg.NoFade = on;
            _cfg.Save();
            _panel.ApplyOpacityNow();
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

        // ---- 桌面宠物 ----
        PetForm _pet;

        public bool PetVisible
        {
            get { return _pet != null && _pet.Visible; }
        }

        // 供合并面板绘制时使用
        public PetForm Pet
        {
            get { return _pet; }
        }

        // 面板窗体（桌宠状态变化时请求重绘）
        public KeyStateForm PanelForm
        {
            get { return _panel; }
        }

        // 键状态面板是否可见与其屏幕范围（供桌宠避让食物落点）
        public bool PanelVisible
        {
            get { return _panel != null && _panel.Visible; }
        }

        // 当前是否因游戏模式而隐藏（供气泡判断）
        public bool GameHidden
        {
            get { return _hiddenByGame; }
        }

        // 手动设置游戏隐藏状态（供测试按钮使用）
        public void SetGameHidden(bool hidden)
        {
            _hiddenByGame = hidden;
        }

        public Rectangle PanelBounds
        {
            get
            {
                if (_panel == null || !_panel.Visible) return Rectangle.Empty;
                return _panel.Bounds;
            }
        }

        public void EnsurePet()
        {
            if (!PetForm.Available) return;
            if (_pet == null || _pet.IsDisposed)
            {
                _pet = new PetForm(this);
                _pet.FormClosed += delegate { _pet = null; };
            }
            // 合并模式：桌宠不再独立显示，由面板统一绘制
            _pet.EnsureRunning();
            if (_panel != null) _panel.Redraw();
        }

        public void TogglePet()
        {
            _cfg.ShowPet = !PetVisible;
            _cfg.Save();
            if (_cfg.ShowPet) EnsurePet();
            else if (_pet != null) _pet.Hide();
        }

        public void SetPetSize(int size)
        {
            _cfg.PetSize = Math.Max(80, Math.Min(400, size));
            _cfg.Save();
        }

        public void SetPetWalk(bool on)
        {
            _cfg.PetWalk = on;
            _cfg.Save();
        }

        public void SavePetLocation(Point p)
        {
            _cfg.PetLocation = p;
            _cfg.Save();
        }

        // 保存桌宠状态（饥饿值、喂食次数等）
        public void SavePetState()
        {
            _cfg.Save();
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

        // 测试模式下锁定隐藏状态，跳过真实全屏检测
        bool _gameTestLocked;

        public void SetGameTestLock(bool locked)
        {
            _gameTestLocked = locked;
        }

        void UpdateGameAutoHide()
        {
            // 测试期间不覆盖手动设置的隐藏状态
            if (_gameTestLocked) return;

            bool shouldHide = _cfg.HideInGame && Native.IsFullscreenAppRunning();
            if (shouldHide == _hiddenByGame) return;
            _hiddenByGame = shouldHide;

            try
            {
                if (shouldHide)
                {
                    // 隐藏面板
                    _panel.Hide();
                    // 同时隐藏桌宠食物与气泡
                    if (_pet != null) _pet.HideFoodForGame(false);
                    if (_pet != null) _pet.HideBubbleForGame(false);
                }
                else
                {
                    if (_panelWantedVisible)
                    {
                        _panel.Show();
                        _panel.ApplyLayout();
                        _panel.Redraw();
                    }
                    // 游戏结束后恢复食物与气泡
                    if (_pet != null) _pet.HideFoodForGame(true);
                    if (_pet != null) _pet.HideBubbleForGame(true);

                    // 退出游戏模式后自动回到右下角
                    if (_panelWantedVisible) _panel.MoveToBottomRight();
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

        // 弹出联系方式窗口，并附带工程测试按钮
        public static void ShowQrWithTests(KeyStateTrayApp app)
        {
            try
            {
                using (QrForm f = new QrForm())
                {
                    f.AttachTests(app);
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

        // ===== 工程测试按钮（仅用于开发验证）=====
        KeyStateTrayApp _app;
        const int TestRowH = 46;

        public void AttachTests(KeyStateTrayApp app)
        {
            _app = app;

            // 窗口加高，底部腾出两行按钮区
            int rowH = 38;
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + rowH * 2 + 22);

            int pad = 8;
            int btnW = (ClientSize.Width - pad * 3) / 2;
            int btnH = 30;
            int y1 = ClientSize.Height - (rowH * 2 + 22) + 18;
            int y2 = y1 + rowH;

            // 第一行：整点报时 / 饥饿拉满
            AddTestButton("整点报时测试", pad, y1, btnW, btnH,
                delegate { if (_app.Pet != null) _app.Pet.TestHourlyNow(); });
            AddTestButton("饥饿拉满测试", pad * 2 + btnW, y1, btnW, btnH,
                delegate { if (_app.Pet != null) _app.Pet.ForceHungry(); });

            // 第二行：游戏隐藏 / 游走测试
            AddTestButton("游戏隐藏测试", pad, y2, btnW, btnH,
                delegate { if (_app.Pet != null) _app.Pet.TestGameHide(); });
            AddTestButton("游走测试", pad * 2 + btnW, y2, btnW, btnH,
                delegate { if (_app.Pet != null) _app.Pet.TestWalkNow(); });

            // 提示文字
            Label tip = new Label();
            tip.Text = "以下仅为工程测试按钮";
            tip.ForeColor = Color.Gray;
            tip.Font = new Font("Microsoft YaHei UI", 7.5f);
            tip.AutoSize = false;
            tip.TextAlign = ContentAlignment.MiddleCenter;
            tip.SetBounds(0, y1 - 16, ClientSize.Width, 14);
            Controls.Add(tip);
            tip.BringToFront();
        }

        void AddTestButton(string text, int x, int y, int w, int h, EventHandler onClick)
        {
            Button b = new Button();
            b.Text = text;
            b.SetBounds(x, y, w, h);
            b.FlatStyle = FlatStyle.System;
            b.Font = new Font("Microsoft YaHei UI", 8.5f);
            b.Click += onClick;
            Controls.Add(b);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);            Graphics g = e.Graphics;
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
        // 新版素材为纯框体 1226x532，可绘制区 X53-1172, Y48-483
        const int SrcW = 1226, SrcH = 532;
        const int BoxX = 53, BoxY = 48, BoxW = 1119, BoxH = 435;

        static Image _bg;
        static bool _bgLoaded;

        readonly KeyStateTrayApp _app;
        bool _dragging;
        Point _dragStart;

        // 桌宠区域的拖动/点击状态（合并模式下）
        bool _petDrag;
        bool _petDragMoved;
        Point _petDragStart;
        Point _petDragAnchor;                      // 按下时的屏幕坐标，用于拖动阈值判断
        const int DragThreshold = 5;               // 移动超过 5px 才算拖动

        // 判断点击位置是否落在魈的绘制范围内（而非整个桌宠区）
        bool IsOnPetBody(Point p)
        {
            if (_app.Pet == null) return false;

            // 魈绘制在框体上方，取该区域并留出合理余量
            int boxH = (int)Math.Round(Width * (double)SrcH / SrcW);
            int petBottom = Height - boxH - 4;
            if (p.Y >= petBottom) return false;        // 点在框体或更低位置

            // 魈水平居中，宽度约为窗口的 70%
            int halfW = (int)(Width * 0.35f);
            int cx = Width / 2;
            return p.X >= cx - halfW && p.X <= cx + halfW;
        }

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


                // 点中桌宠本体 → 交给桌宠（换表情/拖动），面板不拖动
                if (_app.Pet != null && IsOnPetBody(e.Location))
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        _petDrag = true;
                        _petDragStart = e.Location;
                        _petDragMoved = false;
                        _petDragAnchor = PointToScreen(e.Location);   // 记录按下时的屏幕坐标
                    }
                    else if (e.Button == MouseButtons.Right) ShowPanelMenu();
                    return;
                }

                if (e.Button == MouseButtons.Left) { _dragging = true; _dragStart = e.Location; }
                else if (e.Button == MouseButtons.Right) ShowPanelMenu();
            };
            MouseMove += delegate(object s, MouseEventArgs e)
            {
                // 拖动桌宠区域：整体移动面板（需超过阈值才认定为拖动）
                if (_petDrag)
                {
                    Point cur = PointToScreen(e.Location);
                    if (!_petDragMoved)
                    {
                        int dx0 = Math.Abs(cur.X - _petDragAnchor.X);
                        int dy0 = Math.Abs(cur.Y - _petDragAnchor.Y);
                        if (dx0 < DragThreshold && dy0 < DragThreshold) return;   // 轻微抖动不算拖动
                        _petDragMoved = true;
                    }

                    Point pp = PointToScreen(e.Location);
                    Location = ClampToWorkArea(new Point(pp.X - _petDragStart.X, pp.Y - _petDragStart.Y), Width, Height);
                    return;
                }

                if (!_dragging) return;
                Point p = PointToScreen(e.Location);
                // 拖动时实时约束在工作区内，避免盖住任务栏或拖出屏幕
                Location = ClampToWorkArea(new Point(p.X - _dragStart.X, p.Y - _dragStart.Y), Width, Height);
            };
            MouseUp += delegate
            {
                // 桌宠区域点击：未移动视为单击 → 换表情
                if (_petDrag)
                {
                    _petDrag = false;
                    if (!_petDragMoved && _app.Pet != null)
                        _app.Pet.TriggerClick();
                    else
                        _app.SaveLocation(Location);
                    return;
                }

                if (!_dragging) return;
                _dragging = false;
                _app.SaveLocation(Location);
            };

            BuildPanelMenu();
        }

        // 构建测试功能横向按钮行（署名正下方）
        Panel BuildTestRow()
        {
            Panel row = new Panel();
            row.Height = 26;
            row.Width = 320;
            row.BackColor = Color.Transparent;

            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = false;
            flow.AutoSize = false;
            flow.BackColor = Color.Transparent;

            flow.Controls.Add(MakeTestButton("整点报时", delegate { TestHourly(); }));
            flow.Controls.Add(MakeTestButton("饥饿值", delegate { ShowHungerDialog(); }));
            flow.Controls.Add(MakeTestButton("游戏隐藏", delegate { TestGameHide(); }));

            row.Controls.Add(flow);
            // 菜单弹出时刷新按钮文字（体现当前状态）
            row.Tag = flow;
            return row;
        }

        Button MakeTestButton(string text, EventHandler onClick)
        {
            Button b = new Button();
            b.Text = text;
            b.AutoSize = true;
            b.Height = 22;
            b.Margin = new Padding(1, 1, 1, 1);
            b.Padding = new Padding(6, 0, 6, 0);
            b.FlatStyle = FlatStyle.System;
            b.Font = new Font("Microsoft YaHei UI", 8.5f);
            b.Cursor = Cursors.Hand;
            b.Click += onClick;
            return b;
        }

        // 测试：立即触发一次整点提醒
        void TestHourly()
        {
            if (_app.Pet != null) _app.Pet.TestHourlyNow();
        }

        // 测试：设置饥饿值
        void ShowHungerDialog()
        {
            if (_app.Pet == null) return;
            _app.Pet.ShowHungerDialog(this);
        }

        // 测试：切换游戏隐藏
        void TestGameHide()
        {
            _app.SetHideInGame(!_app.Config.HideInGame);
        }

        void BuildPanelMenu()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            Native.ApplyMenuTheme(m);
            // 不指定字体，使用系统默认菜单字体与尺寸

            // 顶部署名项：系统默认菜单样式（单行、默认字体与颜色）
            // 内嵌二维码资源时（脱敏版）可点击弹出二维码，并附带联系提示。
            ToolStripMenuItem credit = new ToolStripMenuItem("by 椰子味");   // && 显示为单个 &
            if (QrForm.Available)
            {
                credit.Enabled = true;
                credit.Text = "by 椰子味  有问题？单击联系我";
                credit.Click += delegate { QrForm.ShowQrWithTests(_app); };
            }
            else
            {
                credit.Enabled = false;
            }
            m.Items.Add(credit);

            m.Items.Add(new ToolStripSeparator());

            m.Items.Add(new ToolStripMenuItem("↘  回到屏幕右下角", null, delegate { MoveToBottomRight(); }));
            m.Items.Add(new ToolStripSeparator());

            AddSize(m, "小", 120);
            AddSize(m, "中", 180);
            AddSize(m, "大", 260);
            AddSize(m, "大肥鸟", 360);
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(new ToolStripMenuItem("放大一级", null, delegate { _app.SetWidth(Width + 30); }));
            m.Items.Add(new ToolStripMenuItem("缩小一级", null, delegate { _app.SetWidth(Width - 30); }));

            // 透明度子菜单
            ToolStripMenuItem op = new ToolStripMenuItem("闲置透明度");
            EnableRightClickSubmenu(op);
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
            EnableRightClickSubmenu(hold);
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

            // ---- 桌宠设置（合并自桌宠右键菜单）----
            m.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem greet = new ToolStripMenuItem("让桌宠打个招呼", null,
                delegate { if (_app.Pet != null) _app.Pet.TriggerClick(); });
            m.Items.Add(greet);

            ToolStripMenuItem petWalk = new ToolStripMenuItem("自动游走", null,
                delegate { _app.SetPetWalk(!_app.Config.PetWalk); });
            m.Items.Add(petWalk);

            // 饥饿速度
            ToolStripMenuItem rate = new ToolStripMenuItem("饥饿速度");
            EnableRightClickSubmenu(rate);
            int[] rates = new int[] { 5, 15, 30, 45, 90, 180, 600 };
            string[] rateNames = new string[] {
                "5 秒/点 · 极快", "15 秒/点 · 快", "30 秒/点",
                "45 秒/点 · 默认", "90 秒/点", "3 分钟/点", "10 分钟/点 · 慢" };
            for (int i = 0; i < rates.Length; i++)
            {
                int rv = rates[i];
                string rn = rateNames[i];
                ToolStripMenuItem ri = new ToolStripMenuItem(rn, null, delegate
                {
                    _app.Config.HungerRateMinutes = rv;
                    _app.Config.Save();
                });
                ri.Tag = rv;
                rate.DropDownItems.Add(ri);
            }
            m.Items.Add(rate);

            // 整点提醒开关
            ToolStripMenuItem hourly = new ToolStripMenuItem("整点提醒", null,
                delegate
                {
                    _app.Config.HourlyRemind = !_app.Config.HourlyRemind;
                    _app.Config.Save();
                });
            m.Items.Add(hourly);

            string hungerLabel = "喂食 · 杏仁豆腐";
            if (_app.Config.Hunger > 0) hungerLabel += "  (饥饿 " + _app.Config.Hunger + "%)";
            m.Items.Add(new ToolStripMenuItem(hungerLabel, null,
                delegate { if (_app.Pet != null) _app.Pet.Feed(); }));

            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(new ToolStripMenuItem("隐藏面板", null, delegate { Hide(); }));

            // 每次弹出菜单时刷新勾选状态
            m.Opening += delegate
            {
                // 重新检测系统主题（用户可能切换了深色/浅色）
                Native.ApplyMenuTheme(m);

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

                // 桌宠状态同步
                petWalk.Checked = _app.Config.PetWalk;
                hourly.Checked = _app.Config.HourlyRemind;
                greet.Enabled = _app.Pet != null;

                // 开关项：勾选 + 文字标记 + 颜色，状态一目了然
                StyleToggle(hideGamePanel, _app.Config.HideInGame, "全屏游戏时自动隐藏");
                StyleToggle(petWalk, _app.Config.PetWalk, "自动游走");
                StyleToggle(hourly, _app.Config.HourlyRemind, "整点提醒");
                StyleToggle(noFade, _app.Config.NoFade, "永不透明");

                foreach (ToolStripItem it in rate.DropDownItems)
                {
                    ToolStripMenuItem mi = it as ToolStripMenuItem;
                    if (mi != null && mi.Tag is int)
                        mi.Checked = ((int)mi.Tag == _app.Config.HungerRateMinutes);
                }
            };

            // 关键：不设置 ContextMenuStrip 属性。
            // 面板窗口带 WS_EX_NOACTIVATE（不抢焦点），WinForms 无法感知“点击外部”
            // 因而菜单不会自动关闭。这里改用全局低级鼠标/键盘钩子主动监测外部点击。
            _menu = m;
            // 显示勾选标记区域（✓），让开关状态一目了然
            m.ShowImageMargin = true;
            m.ShowCheckMargin = true;
            m.AutoClose = true;
            // 外部点击由全局钩子负责关闭；任何方式关闭后都卸载钩子
            m.Closed += delegate { RemoveMenuHooks(); };

            // 兜底：选中顶层菜单项后主动关闭。
            // 子菜单项（透明度、保持时长等）不应在此关闭，否则会取消其 Click 动作。
            m.ItemClicked += delegate(object s, ToolStripItemClickedEventArgs e)
            {
                // 只有顶层菜单项才关闭；子菜单项让其 Click 正常执行
                if (e.ClickedItem == null) return;
                ToolStripItem owner = e.ClickedItem.OwnerItem;
                if (owner == null) CloseMenu();   // 顶层项（没有 OwnerItem）
            };
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

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X; public int Y; public POINT(int x, int y) { X = x; Y = y; } }

        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(POINT p);

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

        // 鼠标按下：如果点在菜单（含所有子菜单）之外，则关闭菜单
        IntPtr MenuMouseHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && _menu != null && _menu.Visible)
                {
                    int msg = wParam.ToInt32();
                    bool isDown = (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN);

                    if (isDown)
                    {
                        int x = Marshal.ReadInt32(lParam, 0);
                        int y = Marshal.ReadInt32(lParam, 4);

                        // 点在菜单内部：什么都不做，让菜单自己处理
                        // （右键可展开子菜单，左键可选中，均不关闭主菜单）
                        if (IsInAnyMenuRect(x, y))
                            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);

                        // 点在菜单外部：关闭
                        CloseMenu();
                    }
                }
            }
            catch { }
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        // 点是否落在菜单（主菜单或任意子菜单）的矩形范围内
        bool IsInAnyMenuRect(int x, int y)
        {
            if (_menu == null) return false;

            try
            {
                if (_menu.Visible)
                {
                    Rectangle r = _menu.Bounds;
                    // 放宽 2px 容差
                    if (x >= r.Left - 2 && x <= r.Right + 2 && y >= r.Top - 2 && y <= r.Bottom + 2)
                        return true;
                }
            }
            catch { }

            // 递归检查所有已展开的子菜单
            foreach (ToolStripItem it in _menu.Items)
                if (IsInSubMenuRect(it, x, y)) return true;

            return false;
        }

        bool IsInSubMenuRect(ToolStripItem item, int x, int y)
        {
            ToolStripMenuItem mi = item as ToolStripMenuItem;
            if (mi == null) return false;

            try
            {
                if (mi.DropDown != null && mi.DropDown.Visible)
                {
                    Rectangle r = mi.DropDown.Bounds;
                    if (x >= r.Left - 2 && x <= r.Right + 2 && y >= r.Top - 2 && y <= r.Bottom + 2)
                        return true;
                }
            }
            catch { }

            foreach (ToolStripItem sub in mi.DropDownItems)
                if (IsInSubMenuRect(sub, x, y)) return true;

            return false;
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

        // 开关项样式：勾选标记 + 前缀符号 + 颜色区分
        static void StyleToggle(ToolStripMenuItem item, bool on, string baseText)
        {
            if (item == null) return;
            item.Checked = on;
            item.Text = (on ? "✔  " : "✘  ") + baseText;
            item.ForeColor = on ? Color.FromArgb(30, 140, 70) : Color.FromArgb(130, 130, 135);
            // 用加粗强化"已开启"
            item.Font = new Font("Microsoft YaHei UI", 9f,
                on ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point, 134);
        }

        // 让带子菜单的父项支持：右键立即展开子菜单（无需悬停等待）
        internal static void EnableRightClickSubmenu(ToolStripMenuItem item)
        {
            if (item == null) return;
            item.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Right) return;
                if (item.DropDownItems.Count == 0) return;

                try
                {
                    // 立即展开子菜单
                    item.DropDown.Show(item.Owner == null ? item.GetCurrentParent() : item.Owner,
                        new Point(item.Bounds.Right, item.Bounds.Top));
                }
                catch
                {
                    try { item.ShowDropDown(); } catch { }
                }
            };
        }

        void AddOpacity(ToolStripMenuItem parent, string text, int pct)
        {            ToolStripMenuItem mi = new ToolStripMenuItem(text, null, delegate
            {
                _app.SetOpacity(pct);
                // 设置透明度后立即生效（清除活跃期），避免被"保持完全不透明"盖住
                ApplyOpacityNow();
            });
            mi.Tag = pct;
            parent.DropDownItems.Add(mi);
        }

        // 立即应用当前透明度设置：结束活跃期，让新透明度马上可见
        public void ApplyOpacityNow()
        {
            _activeUntil = 0;      // 清除活跃期
            _wasActive = false;
            Redraw();
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
            // 宽度由配置决定；框体按素材比例，上方额外留出桌宠区域
            int w = Math.Max(120, Math.Min(900, _app.Config.Width));
            int boxH = (int)Math.Round(w * (double)SrcH / SrcW);
            // 桌宠区：框高的 1.35 倍，保证魈足够大
            int petExtra = (int)Math.Round(boxH * 1.35);
            // 气泡区：在窗口顶部额外预留，仅供气泡显示，不占用桌宠区域
            int bubbleExtra = (int)Math.Round(boxH * 0.9);
            int h = boxH + petExtra + bubbleExtra;
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
            // 通知桌宠：重置游走状态，避免立刻又走开
            if (_app.Pet != null) _app.Pet.ResetWalk();
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

        // 保持完全不透明指定秒数（用于点击出文本时）
        public void KeepOpaqueFor(int seconds)
        {
            _activeUntil = unchecked(Environment.TickCount + seconds * 1000);
            _wasActive = true;
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

            // ===== 四段式布局：气泡(顶) → 魈(中) → 框体 → 测试按钮(底) =====
            int btnBarH = 0;                      // 面板上不再显示测试按钮

            // 框体尺寸固定：宽度占窗口 78%，高度按素材比例
            int boxMaxW = (int)(Width * 0.78f);
            int gap = Math.Max(4, (int)Math.Round(Height * 0.012f));

            // 先算出框体实际需要的高度
            float scBox = boxMaxW / (float)SrcW;
            int dw = (int)Math.Round(SrcW * scBox);
            int dh = (int)Math.Round(SrcH * scBox);

            // 框体放在按钮条上方
            int dx = (Width - dw) / 2;
            int dy = Height - btnBarH - dh;

            // 魈占据框体上方的全部空间
            int petTop = 0;
            int petBottom = dy - gap;
            int petAreaH = Math.Max(40, petBottom - petTop);

            // 素材里的灰绿方框位置，换算到当前缩放
            Rectangle box = new Rectangle(
                dx + (int)Math.Round(BoxX * scBox), dy + (int)Math.Round(BoxY * scBox),
                (int)Math.Round(BoxW * scBox), (int)Math.Round(BoxH * scBox));

            // 魈绘制在框体上方（高度 = 框体以上全部空间）
            _app.Pet.DrawOnto(g, new Rectangle(0, petTop, Width, petAreaH));

            // 框体绘制在最上层，保证状态文字绝对清晰
            g.DrawImage(bg, new Rectangle(dx, dy, dw, dh));
            DrawCells(g, box);

        }

        // 底部测试按钮条高度
        const int TestBarPadding = 4;
        public int TestBarHeight { get { return 26; } }

        // 三个测试按钮的矩形区域（供绘制与点击判定共用）
        public Rectangle[] TestButtonRects()
        {
            int barTop = Height - TestBarHeight;
            int btnW = (Width - TestBarPadding * 4) / 3;
            int btnH = TestBarHeight - TestBarPadding * 2;
            Rectangle[] r = new Rectangle[3];
            for (int i = 0; i < 3; i++)
                r[i] = new Rectangle(TestBarPadding + i * (btnW + TestBarPadding),
                                     barTop + TestBarPadding, btnW, btnH);
            return r;
        }

        static readonly string[] TestButtonTexts = new string[] { "整点报时", "饥饿值", "游戏隐藏" };

        void DrawTestBar(Graphics g, Rectangle bar)
        {
            Rectangle[] rects = TestButtonRects();
            using (Font bf = new Font("Microsoft YaHei UI", 8f))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                for (int i = 0; i < rects.Length; i++)
                {
                    Rectangle r = rects[i];

                    // 游戏隐藏按钮：开启时高亮
                    bool active = (i == 2) && _app.Config.HideInGame;

                    using (GraphicsPath gp = RoundedRect(r, 5))
                    using (Brush bg = new SolidBrush(active
                        ? Color.FromArgb(235, 90, 160, 100)
                        : Color.FromArgb(225, 250, 250, 252)))
                    using (Pen bp = new Pen(Color.FromArgb(200, 180, 185, 195)))
                    {
                        g.FillPath(bg, gp);
                        g.DrawPath(bp, gp);
                    }

                    using (Brush tb = new SolidBrush(active ? Color.White : Color.FromArgb(60, 62, 70)))
                        g.DrawString(TestButtonTexts[i], bf, tb, r, sf);
                }
            }
        }

        static GraphicsPath RoundedRect(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = Math.Max(2, rad * 2);
            if (d >= r.Width || d >= r.Height) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        // 判断点击是否落在测试按钮上，返回索引（-1 表示未命中）
        public int HitTestBar(Point p)
        {
            if (p.Y < Height - TestBarHeight) return -1;
            Rectangle[] rects = TestButtonRects();
            for (int i = 0; i < rects.Length; i++)
                if (rects[i].Contains(p)) return i;
            return -1;
        }

        // 气泡区高度（仅供参考，实际由 DrawOnto 按内容动态计算）
        public int BubbleAreaHeight
        {
            get
            {
                int boxH = (int)Math.Round(Width * (double)SrcH / SrcW);
                return (int)Math.Round(boxH * 1.2);
            }
        }

        // 桌宠区高度 = 窗口高度 - 框体高度（即框体以上的全部空间）
        public int PetAreaHeight
        {
            get
            {
                int boxH = (int)Math.Round(Width * (double)SrcH / SrcW);
                int gap = Math.Max(4, (int)Math.Round(Height * 0.012f));
                return Math.Max(40, Height - boxH - gap);
            }
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
                string imeName = _app.ImeName;

                // 标题显示输入法类别，值显示具体名称
                items.Add(new string[] { "输入法", imeName });
                states.Add(zh);

                // 中文系绿底 / 外语红底，均黑字高对比
                colors.Add(zh
                    ? new Color?[] { Color.FromArgb(255, 40, 180, 70), Color.Black }
                    : new Color?[] { Color.FromArgb(255, 220, 50, 50), Color.Black });
            }
            if (items.Count == 0) return;

            int n = items.Count;
            // 与旧版一致的内边距比例（5% / 14%），保持熟悉的观感
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
        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            Redraw();
            // 面板移动时同步气泡窗口位置
            if (_app.Pet != null) _app.Pet.RepositionBubble();
        }
        protected override bool ShowWithoutActivation { get { return true; } }
    }

    // ======================= 桌面宠物 =======================
    // 透明浮窗角色：可拖动、闲置自动走动、点击有表情与气泡反应
    internal sealed class PetForm : Form
    {
        // 表情资源名（构建时内嵌）
        static readonly string[] SpriteNames = new string[]
        {
            "pet_01_eat.png",   // 吃东西
            "pet_02_idle.png",  // 待机
            "pet_03_wink.png",  // 眨眼
            "pet_04_cold.png",  // 冷漠
            "pet_05_sleep.png", // 睡觉
            "pet_06_tsk.png",   // 啧
            "pet_07_think.png", // 思考
            "pet_08_fight.png"  // 来吧
        };
        // ================= 魈的官方台词库 =================
        // 全部为游戏内原文，未做任何改写。点击互动时随机抽取。
        static readonly string[] XiaoPhrases = new string[]
        {
            // 初次见面
            "如遇世道旷野之难、路遭贼人之难、水火刀兵之难、鬼神药毒之难、饿瘦毒虫之难、冤家恶人之难，便呼我名，三眼五显仙人「魈」听召，前来守护。",

            // 问候
            "时候不早了，出发吧。",
            "午餐，回望舒客栈吧。",
            "夜晚，不祥之物最宜骚动，你最好别出门。",
            "你去吧，我会在这里等你。",

            // 闲聊
            "我居然沦落到如此闲散的地步，可笑。",
            "荒野上的孤魂，休想伤我分毫。",
            "哼，想起了一些不愉快的事。",

            // 天气
            "魔物不会因为下雨就休息，我们走。",
            "天晴了。哼，我不关心天气。",
            "不少凡人害怕打雷，无法理解，这分明很寻常。",
            "雪积起来之后，就可以挖着吃了。",

            // 关于魈自己
            "我并非人类，不太能够理解人类的感情。",
            "近来璃月变化得过于迅速了。哼，罢了，我也没打算适应。",

            // 关于我们
            "如果有一天，连你也陷入了黑暗，就由我来。",
            "别被污染，我不会留情的。我是说，既然是你，你应该能保持坚廷。",
            "谁？你觉得我是站着睡着了？呃，不敬仙师。",
            "海灯节？原来又到了凡人往海中倾倒发光垃圾的时候了，为何如此幼稚。",

            // 关于「神之眼」
            "「神之眼」·欲望，哼，不要用凡人的标准来揣测仙人，我没有欲望。",
            "并不是有意隐瞒，我的欲望不应对外人讲。人间不是有着愿望说出口就会失效的规矩？额，不是一回事？",

            // 想要分享 / 见闻
            "杀戮是我的强项，如果你下不去手，就叫我来。",
            "荻花洲，曾经一度荒芜。虽然，是过去好多年，不，好几百年的事了。",

            // 关于他人
            "胡桃，那孩子活泼到令人头疼，索性她还算风趣，不必担心会成长为无趣的人类。",
            "嗯，打听一下，请问钟离大人平日里都在？额，遛鸟、赏花、品鉴古文？钟离大人做这些是有什么深意呢？",
            "北斗吗？我与她没有什么交情。不过冥海巨兽确有其事，能够将「海山」击败，作为人类，她的实力值得肯定。",
            "你想，托付白术研制缓解我痛苦的药物？呵，你的好意我心领了，凡人的药物，对仙人是无法起效的",
            "温迪？哼，原来如此，名字叫做温迪啊。他演奏的曲调……不，没什么。",

            // 想要了解魈
            "我会为你护法，但是，别靠近我，别妨碍我。否则，你会后悔的。",
            "在这千年以来，我猎杀过数以万计的冤魂。不想惹上麻烦，就离我远点，把我当作工具就好。",
            "他们的声音，常常在我的耳边出现，咆哮，嚎哭，嘶喊着杀戮。这是我自己的选择和自己的报应，与你无关。",
            "「摩拉克斯」？关于这个名字，你究竟知道多少？他是赐予我「魈」之名之人，将我从漫长的痛苦中解放，胆敢对他不敬的话，即使是你，我也不会原谅。",
            "已经迟了，我们之间的关联，已经太多了。即使你想要放弃，也来不及了吧。嗯？没有这样想过。唉，「靖妖傩舞」，我的战斗，是不会停下的。但是，关于你的事情，我想了解更多。",

            // 爱好 / 烦恼
            "你还真是胆量不小，有勇气询问我的爱好，那便来战吧。以你的身躯，扛得住我几招呢？",
            "烦恼？哼，这个问题对仙人毫无意义。没有什么烦恼能够存在千年。",

            // 喜欢的食物
            "我对人间的食物并无兴致。烹饪繁琐，等待漫长，唯独杏仁豆腐，勉强可以入口。",
            "杏仁豆腐的味道，和「美梦」，非常相似呢。",
            "要我亲手在薄饼中包入肉片，额，然后，一起吃下去？哼，麻烦，如今的人间，真是越来越缺乏常识了。",

            // 突破
            "只是这种程度而已，没什么值得大惊小怪的。",
            "新的力量？这力量，也只会被用于斩杀。",
            "力量的尽头，是自我毁灭。回答我，为何如此执着呢？",
            "我的枪缨，早就涂入了无尽的冤魂，连我本人不免也被黑暗吞噬。你却敢于驱使这样的我，将我当作，同伴。你是想要救赎我吗？哼，你，真是难以理解的生物。"
        };

        // 上次说的话，避免连续重复
        static int _lastPhraseIdx = -1;

        static string RandomPhrase()
        {
            if (XiaoPhrases.Length == 0) return "";
            Random rnd = new Random();
            int idx = rnd.Next(0, XiaoPhrases.Length);
            if (XiaoPhrases.Length > 1 && idx == _lastPhraseIdx)
                idx = (idx + 1 + rnd.Next(0, XiaoPhrases.Length - 1)) % XiaoPhrases.Length;
            _lastPhraseIdx = idx;
            return XiaoPhrases[idx];
        }

        // 吃东西时（官方原文）
        static readonly string[] EatPhrases = new string[]
        {
            "杏仁豆腐的味道，和「美梦」，非常相似呢。",
            "我对人间的食物并无兴致。烹饪繁琐，等待漫长，唯独杏仁豆腐，勉强可以入口。"
        };

        static string RandomEatPhrase()
        {
            return EatPhrases[new Random().Next(0, EatPhrases.Length)];
        }

        // 饥饿时（官方原文中与进食相关的句子）
        static readonly string[] HungryPhrases = new string[]
        {
            "我对人间的食物并无兴致。烹饪繁琐，等待漫长，唯独杏仁豆腐，勉强可以入口。",
            "杏仁豆腐的味道，和「美梦」，非常相似呢。"
        };

        static string RandomHungryPhrase()
        {
            return HungryPhrases[new Random().Next(0, HungryPhrases.Length)];
        }

        // ---------- 静态资源 ----------
        static Image[] _sprites;
        static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _sprites = new Image[SpriteNames.Length];
            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                for (int i = 0; i < SpriteNames.Length; i++)
                {
                    using (Stream s = asm.GetManifestResourceStream(SpriteNames[i]))
                    {
                        if (s == null) continue;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            s.CopyTo(ms);
                            _sprites[i] = Image.FromStream(ms);
                        }
                    }
                }
            }
            catch { }
        }

        public static bool Available
        {
            get
            {
                EnsureLoaded();
                if (_sprites == null) return false;
                foreach (Image im in _sprites) if (im != null) return true;
                return false;
            }
        }

        // ---------- 常量 ----------
        const int TickMs = 60;
        const int ClickAnimFrames = 50;       // 点击互动：50 帧 × 60ms ≈ 3 秒
        const int IdleSwitchMinFrames = 1000; // 待机表情切换 ≈ 1 分钟
        const int IdleSwitchMaxFrames = 3000; // ≈ 3 分钟
        const int IdleBeforeWalkMs = 12000;
        const int WalkDurationMs = 3000;
        const int BubbleShowMs = 3000;
        const int HungryThreshold = 55;
        const int StarvingThreshold = 85;
        const int TskFrame = 5;               // 啧，仅饥饿时使用

        static readonly int[] IdlePool = new int[] { 0, 1, 2, 3, 4, 6, 7 };

        // ---------- 实例字段 ----------
        readonly KeyStateTrayApp _app;
        readonly Timer _timer;
        readonly Timer _bubbleTimer;

        AppConfig _cfg { get { return _app.Config; } }

        int _frame = 1;
        int _frameHold;
        int _idleLeftMs2;
        bool _running;

        bool _dragging;
        Point _dragOffset;
        bool _draggingMoved;

        bool _walking;
        int _walkDir;
        long _walkLeftMs;
        long _idleLeftMs;
        double _bobPhase;
        int _baseY;

        string _bubbleText;
        bool _stickyBubble;      // 当前气泡是否为常驻（饥饿台词）

        long _hungerAccum;
        int _lastNagLevel;

        bool _eating;
        int _eatFrame;

        FoodForm _food;
        bool _foodSpawnedForThisHunger;
        bool _highlight;

        long _lastClickTicks;

        // 供食物窗口使用
        public KeyStateForm PanelRef { get { return _app != null ? _app.PanelForm : null; } }

        // ---------- 构造 ----------
        public PetForm(KeyStateTrayApp app)
        {
            EnsureLoaded();
            _app = app;

            AutoScaleMode = AutoScaleMode.None;
            AutoScaleDimensions = new SizeF(1f, 1f);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;

            Size = new Size(200, 220);
            _frame = 1;
            _idleLeftMs2 = IdleSwitchMinFrames;

            _timer = new Timer();
            _timer.Interval = TickMs;
            _timer.Tick += delegate { OnTick(); };

            _bubbleTimer = new Timer();
            _bubbleTimer.Interval = BubbleShowMs;
            _bubbleTimer.Tick += delegate
            {
                _bubbleTimer.Stop();
                _bubbleText = null;
                _stickyBubble = false;   // 普通对话过期后，允许饥饿台词重新接管
                SyncBubbleWindow();      // 同步隐藏气泡窗口
                if (_app.PanelForm != null) _app.PanelForm.Redraw();
            };
        }

        // 合并模式：作为逻辑对象启动（窗口不显示，由面板绘制）
        public void EnsureRunning()
        {
            if (_running) return;
            _running = true;
            if (!_timer.Enabled) _timer.Start();
        }

        public void ApplySizePublic() { }

        // ---------- 交互 ----------
        public void TriggerClick()
        {
            long now = DateTime.UtcNow.Ticks;
            _lastClickTicks = now;
            SetFrame(RandomFrame(), ClickAnimFrames, true);
        }

        void SetFrame(int idx, int hold, bool showPhrase)
        {
            if (_sprites == null || idx < 0 || idx >= _sprites.Length || _sprites[idx] == null) return;
            _frame = idx;
            _frameHold = hold;
            if (showPhrase) Say(RandomPhrase());
            if (_app.PanelForm != null) _app.PanelForm.Redraw();
        }

        void Say(string text)
        {
            Say(text, false);
        }

        // sticky=true 时气泡常驻，直到被其他对话替换
        void Say(string text, bool sticky)
        {
            _bubbleText = text;
            _stickyBubble = sticky;

            _bubbleTimer.Stop();
            if (!sticky) _bubbleTimer.Start();

            // 同步到独立气泡窗口
            SyncBubbleWindow();

            // 出文本时强制保持完全不透明 5 秒
            if (_app.PanelForm != null)
            {
                _app.PanelForm.KeepOpaqueFor(5);
                _app.PanelForm.Redraw();
            }
        }

        // 独立气泡窗口（不占桌宠空间）
        BubbleForm _bubble;

        // 把当前气泡文字同步到独立窗口，并按魈的位置摆放
        void SyncBubbleWindow()
        {
            // 游戏隐藏期间一律不显示气泡
            if (_app.GameHidden)
            {
                if (_bubble != null && !_bubble.IsDisposed && _bubble.Visible) _bubble.Hide();
                return;
            }

            KeyStateForm panel = _app.PanelForm;
            if (panel == null || !panel.Visible)
            {
                if (_bubble != null && !_bubble.IsDisposed && _bubble.Visible) _bubble.Hide();
                return;
            }

            if (string.IsNullOrEmpty(_bubbleText))
            {
                if (_bubble != null && !_bubble.IsDisposed) _bubble.ClearText();
                return;
            }

            if (_bubble == null || _bubble.IsDisposed)
            {
                _bubble = new BubbleForm();
                _bubble.FormClosed += delegate { _bubble = null; };
            }

            // 魈的实际屏幕区域（框体上方）
            Rectangle pb = panel.Bounds;
            int petH = panel.PetAreaHeight;
            Rectangle petArea = new Rectangle(pb.X, pb.Y, pb.Width, petH);

            _bubble.ShowText(_bubbleText, _stickyBubble, petArea);
        }

        // 面板移动时调用：仅重算气泡位置，不改变内容
        public void RepositionBubble()
        {
            if (_bubble == null || _bubble.IsDisposed || !_bubble.Visible) return;
            if (string.IsNullOrEmpty(_bubbleText)) return;

            KeyStateForm panel = _app.PanelForm;
            if (panel == null || !panel.Visible) return;

            Rectangle pb = panel.Bounds;
            int petH = panel.PetAreaHeight;
            Rectangle petArea = new Rectangle(pb.X, pb.Y, pb.Width, petH);

            _bubble.ShowText(_bubbleText, _stickyBubble, petArea);
        }

        // 饥饿时的常驻台词：雪积起来之后，就可以挖着吃了
        const string HungryStickyLine = "雪积起来之后，就可以挖着吃了。";

        // 检查是否需要显示常驻饥饿台词（无其他对话占用时）
        void UpdateStickyHungry()
        {
            if (_eating) return;

            bool starving = _cfg.Hunger >= StarvingThreshold;

            if (starving)
            {
                // 饿了：如果当前没有内容（或被普通对话占用后过期），显示常驻台词
                // 游戏隐藏期间不设置文字（退出游戏后由 UpdateStickyHungry 再次触发）
                if (string.IsNullOrEmpty(_bubbleText) && !_stickyBubble && !_app.GameHidden)
                {
                    Say(HungryStickyLine, true);
                }
            }
            else if (_stickyBubble)
            {
                // 喂食后饥饿归零：清除常驻台词
                _stickyBubble = false;
                _bubbleText = null;
                SyncBubbleWindow();
                if (_app.PanelForm != null) _app.PanelForm.Redraw();
            }
        }

        int RandomFrame()
        {
            int start = new Random().Next(0, IdlePool.Length);
            for (int i = 0; i < IdlePool.Length; i++)
            {
                int idx = IdlePool[(start + i) % IdlePool.Length];
                if (idx < _sprites.Length && _sprites[idx] != null) return idx;
            }
            return 1;
        }

        // ---------- 每帧 ----------
        void OnTick()
        {
            if (!_running) return;

            // 饥饿 / 整点 / 进食动画
            UpdateHunger();
            UpdateHourly();
            UpdateFoodAnim();
            UpdateStickyHungry();

            // 走动推进（在桌宠区内左右平移）
            UpdateWalk();

            if (_frameHold > 0) _frameHold--;

            if (!_eating && !_dragging)
            {
                int want;
                if (_frameHold > 0)
                {
                    want = _frame;
                }
                else if (_cfg.Hunger >= StarvingThreshold)
                {
                    want = TskFrame;
                }
                else if (_cfg.Hunger >= HungryThreshold)
                {
                    want = 6;
                }
                else
                {
                    _idleLeftMs2--;
                    if (_idleLeftMs2 <= 0)
                    {
                        want = RandomFrame();
                        _idleLeftMs2 = IdleSwitchMinFrames
                                     + new Random().Next(IdleSwitchMaxFrames - IdleSwitchMinFrames);
                    }
                    else
                    {
                        want = _frame;
                        if (want == TskFrame) { want = RandomFrame(); _idleLeftMs2 = IdleSwitchMinFrames; }
                    }
                }

                if (_frame != want)
                {
                    _frame = want;
                    if (_app.PanelForm != null) _app.PanelForm.Redraw();
                }
            }
        }

        // ---------- 自动游走（整个悬浮窗在工作区内随机移动）----------
        double _walkTargetX;      // 目标位置
        double _walkTargetY;
        long _walkPauseLeft;      // 到达后的停留时间

        // 移动时长（毫秒）：每次走 3 秒的曲线
        const int WalkMoveMs = 2000;
        long _walkMoveLeft;       // 本次移动剩余毫秒

        // 贝塞尔曲线路径：起点(记录)、控制点、终点
        double _curveX0, _curveY0;
        double _curveCX, _curveCY;
        double _curveX1, _curveY1;
        double _curveT;           // 0..1 进度
        int _lastDirX;            // 上次水平方向，避免左右往返
        int _lastDirY;            // 上次垂直方向，避免上下往返
        int _lastArcSign;         // 上次弧线方向

        void UpdateWalk()
        {
            KeyStateForm panel = _app.PanelForm;
            if (panel == null) return;

            if (!_cfg.PetWalk)
            {
                _walking = false;
                return;
            }

            if (_dragging || _eating) return;

            // 停留阶段
            if (_walkPauseLeft > 0)
            {
                _walkPauseLeft -= TickMs;
                return;
            }

            Rectangle wa = Screen.FromControl(panel).WorkingArea;
            int maxX = Math.Max(wa.Left, wa.Right - panel.Width);
            int maxY = Math.Max(wa.Top, wa.Bottom - panel.Height);

            // 开始一段曲线移动
            if (!_walking)
            {
                Random rnd = new Random();

                _curveX0 = panel.Left;
                _curveY0 = panel.Top;

                // 终点：小步移动
                int spanX = Math.Max(40, (maxX - wa.Left) / 8);
                int spanY = Math.Max(30, (maxY - wa.Top) / 10);

                // 水平方向：70% 延续上次方向，减少左右往返
                int dirX = _lastDirX != 0 ? _lastDirX : (rnd.Next(2) == 0 ? -1 : 1);
                if (rnd.NextDouble() < 0.3) dirX = -dirX;

                // 垂直方向：同样加惯性，避免上下反复摆动
                int dirY;
                if (_lastDirY == 0)
                    dirY = rnd.Next(2) == 0 ? -1 : 1;
                else
                    dirY = rnd.NextDouble() < 0.25 ? -_lastDirY : _lastDirY;   // 仅 25% 概率反向

                double tx = _curveX0 + dirX * (spanX * (0.6 + rnd.NextDouble() * 0.4));
                double ty = _curveY0 + dirY * (spanY * (0.4 + rnd.NextDouble() * 0.6));

                _curveX1 = Math.Max(wa.Left, Math.Min(tx, maxX));
                _curveY1 = Math.Max(wa.Top, Math.Min(ty, maxY));

                // 控制点：轻微弧线（弧度减小，避免大幅上下摆动）
                double midX = (_curveX0 + _curveX1) / 2;
                double midY = (_curveY0 + _curveY1) / 2;
                double arcSize = (12 + rnd.NextDouble() * 25) * (_lastArcSign == 0 ? 1 : _lastArcSign);
                if (rnd.NextDouble() < 0.2) arcSize = -arcSize;   // 偶尔换弧线方向
                _curveCX = midX;
                _curveCY = midY + arcSize;

                _curveCX = Math.Max(wa.Left, Math.Min(_curveCX, maxX));
                _curveCY = Math.Max(wa.Top, Math.Min(_curveCY, maxY));

                _lastDirX = dirX;
                _lastDirY = dirY;
                _lastArcSign = arcSize >= 0 ? 1 : -1;
                _curveT = 0;
                _walking = true;
                _walkMoveLeft = WalkMoveMs;
                return;
            }

            // 曲线推进
            _walkMoveLeft -= TickMs;
            _curveT += TickMs / (double)WalkMoveMs;

            if (_curveT >= 1.0 || _walkMoveLeft <= 0)
            {
                // 到达终点：停留 1~3 分钟
                panel.Location = new Point(
                    Math.Max(wa.Left, Math.Min((int)Math.Round(_curveX1), maxX)),
                    Math.Max(wa.Top, Math.Min((int)Math.Round(_curveY1), maxY)));
                _walking = false;
                Random rnd = new Random();
                _walkPauseLeft = 60000 + rnd.Next(120000);
                _app.SaveLocation(panel.Location);
                return;
            }

            // 二次贝塞尔插值
            double t = _curveT;
            double it = 1.0 - t;
            double bx = it * it * _curveX0 + 2 * it * t * _curveCX + t * t * _curveX1;
            double by = it * it * _curveY0 + 2 * it * t * _curveCY + t * t * _curveY1;

            int nx = (int)Math.Round(bx);
            int ny = (int)Math.Round(by);
            nx = Math.Max(wa.Left, Math.Min(nx, maxX));
            ny = Math.Max(wa.Top, Math.Min(ny, maxY));

            panel.Location = new Point(nx, ny);
        }

        // 兼容保留：面板不再使用水平偏移
        public double WalkOffset { get { return 0; } }

        // 归位后重置游走状态（停留一段时间再走）
        public void ResetWalk()
        {
            _walking = false;
            Random rnd = new Random();
            _walkPauseLeft = 60000 + rnd.Next(120000);   // 1~3 分钟后再开始游走
        }

        // 测试：立即触发一次曲线游走（忽略停留与开关）
        public void TestWalkNow()
        {
            _walkPauseLeft = 0;
            _walking = false;

            if (!_cfg.PetWalk)
            {
                _cfg.PetWalk = true;
                _cfg.Save();
            }
        }

        // ---------- 饥饿 ----------
        int HungerFullMs
        {
            get
            {
                int sec = _cfg.HungerRateMinutes;
                if (sec < 2) sec = 2;
                if (sec > 3600) sec = 3600;
                return sec * 1000;
            }
        }

        void UpdateHunger()
        {
            if (_cfg.Hunger >= 100) return;

            _hungerAccum += TickMs;
            while (_hungerAccum >= HungerFullMs)
            {
                _hungerAccum -= HungerFullMs;
                _cfg.Hunger = Math.Min(100, _cfg.Hunger + 1);
            }

            // 饥饿时只掉落食物，不说话（对话只在喂食时触发）
            if (_cfg.Hunger >= HungryThreshold && !_foodSpawnedForThisHunger && !_eating)
            {
                _foodSpawnedForThisHunger = true;
                DropFood();
            }
        }

        // ---------- 整点提醒 ----------
        void UpdateHourly()
        {
            if (!_cfg.HourlyRemind) return;

            DateTime now = DateTime.Now;
            if (now.Minute != 0) return;

            string key = now.ToString("yyyyMMddHH");
            if (_cfg.LastHourlyKey == key) return;

            _cfg.LastHourlyKey = key;
            _app.Config.Save();

            int h = now.Hour;
            string msg;
            if (h == 0)            msg = "零点啦，早点休息";
            else if (h < 6)        msg = h + " 点了，还醒着？";
            else if (h < 9)        msg = h + " 点了，早安";
            else if (h < 12)       msg = h + " 点了，记得喝水";
            else if (h == 12)      msg = "中午 12 点，该吃饭了";
            else if (h < 14)       msg = h + " 点了，午休一下";
            else if (h < 18)       msg = h + " 点了，起来活动活动";
            else if (h < 22)       msg = h + " 点了，注意休息眼睛";
            else                   msg = h + " 点了，准备收工吧";

            SetFrame(RandomFrame(), ClickAnimFrames, false);
            Say(msg);
        }

        // ---------- 进食 ----------
        public void Feed()
        {
            if (_eating) return;

            _eating = true;
            _eatFrame = 0;
            _cfg.Hunger = 0;
            _cfg.LastFedTicks = DateTime.UtcNow.Ticks;
            _cfg.FeedCount++;
            _lastNagLevel = 0;
            _hungerAccum = 0;
            _foodSpawnedForThisHunger = false;
            _highlight = false;

            try { if (_food != null && !_food.IsDisposed) _food.Close(); }
            catch { }
            _food = null;

            _app.SavePetState();
            SetFrame(0, 0, false);
            Say(RandomEatPhrase());
        }

        public void FeedFromFood()
        {
            _foodSpawnedForThisHunger = true;
            Feed();
        }

        void UpdateFoodAnim()
        {
            if (!_eating) return;

            _eatFrame++;
            if (_eatFrame > 24)
            {
                _eating = false;
                _frame = 1;
                if (_app.PanelForm != null) _app.PanelForm.Redraw();
                return;
            }
            if (_app.PanelForm != null) _app.PanelForm.Redraw();
        }

        public void ForceHungry()
        {
            _cfg.Hunger = 90;
            _foodSpawnedForThisHunger = false;
            _lastNagLevel = 0;
            _hungerAccum = 0;
            // 只掉落食物，不弹对话（对话只在喂食时出现）
            DropFood();
            if (_app.PanelForm != null) _app.PanelForm.Redraw();
        }

        // 测试：立即触发一次整点提醒
        // 测试：模拟进入游戏 → 隐藏 5 秒 → 恢复
        public void TestGameHide()
        {
            KeyStateForm panel = _app.PanelForm;
            if (panel == null) return;

            // 锁定：期间不让真实全屏检测覆盖状态
            _app.SetGameTestLock(true);

            // 模拟进入游戏：1.5 秒后隐藏（面板 + 食物 + 气泡），5 秒后恢复
            Timer t = new Timer();
            t.Interval = 1500;
            t.Tick += delegate
            {
                t.Stop();
                t.Dispose();

                // 与真实游戏模式一致：先置标记，再隐藏所有元素
                _app.SetGameHidden(true);

                panel.Hide();
                HideFoodForGame(false);
                HideBubbleForGame(false);

                Timer t2 = new Timer();
                t2.Interval = 5000;
                t2.Tick += delegate
                {
                    t2.Stop();
                    t2.Dispose();

                    // 恢复：先显示面板，再清标记，最后恢复气泡/食物
                    panel.Show();
                    panel.ApplyLayout();
                    panel.Redraw();

                    _app.SetGameHidden(false);
                    _app.SetGameTestLock(false);   // 解除锁定，恢复正常检测

                    HideFoodForGame(true);
                    HideBubbleForGame(true);
                };
                t2.Start();
            };
            t.Start();
        }

        // 测试：立即触发一次整点提醒
        public void TestHourlyNow()
        {            DateTime now = DateTime.Now;
            int h = now.Hour;
            string msg;
            if (h == 0)            msg = "零点啦，早点休息";
            else if (h < 6)        msg = h + " 点了，还醒着？";
            else if (h < 9)        msg = h + " 点了，早安";
            else if (h < 12)       msg = h + " 点了，记得喝水";
            else if (h == 12)      msg = "中午 12 点，该吃饭了";
            else if (h < 14)       msg = h + " 点了，午休一下";
            else if (h < 18)       msg = h + " 点了，起来活动活动";
            else if (h < 22)       msg = h + " 点了，注意休息眼睛";
            else                   msg = h + " 点了，准备收工吧";

            if (_app.PanelForm != null) _app.PanelForm.KeepOpaqueFor(5);
            SetFrame(RandomFrame(), ClickAnimFrames, false);
            Say(msg);
        }

        // 设置饥饿值（弹窗）
        public void ShowHungerDialog(IWin32Window owner)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "设置饥饿值";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ClientSize = new Size(260, 110);
                dlg.Font = new Font("Microsoft YaHei UI", 9f);

                Label lb = new Label();
                lb.Text = "饥饿值（0 = 饱，100 = 极饿）";
                lb.SetBounds(14, 12, 230, 18);
                dlg.Controls.Add(lb);

                NumericUpDown num = new NumericUpDown();
                num.Minimum = 0;
                num.Maximum = 100;
                num.Value = Math.Max(0, Math.Min(100, _cfg.Hunger));
                num.SetBounds(14, 34, 100, 24);
                dlg.Controls.Add(num);

                Button ok = new Button();
                ok.Text = "确定";
                ok.SetBounds(130, 34, 55, 24);
                ok.DialogResult = DialogResult.OK;
                dlg.Controls.Add(ok);

                Button cancel = new Button();
                cancel.Text = "取消";
                cancel.SetBounds(190, 34, 55, 24);
                cancel.DialogResult = DialogResult.Cancel;
                dlg.Controls.Add(cancel);

                Label tip = new Label();
                tip.Text = "≥55 进入饥饿状态，≥85 极度饥饿";
                tip.ForeColor = Color.Gray;
                tip.SetBounds(14, 66, 230, 18);
                dlg.Controls.Add(tip);

                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;

                if (dlg.ShowDialog(owner) == DialogResult.OK)
                {
                    int v = (int)num.Value;
                    _cfg.Hunger = v;
                    _hungerAccum = 0;
                    _lastNagLevel = 0;
                    _foodSpawnedForThisHunger = (v < HungryThreshold);
                    _app.Config.Save();

                    if (v >= HungryThreshold)
                    {
                        Say(RandomHungryPhrase());
                        DropFood();
                    }
                    if (_app.PanelForm != null) _app.PanelForm.Redraw();
                }
            }
        }

        // ---------- 掉落食物 ----------
        // 游戏模式下隐藏/恢复食物
        public void HideFoodForGame(bool show)
        {
            try
            {
                if (_food == null || _food.IsDisposed) return;
                if (show) _food.Show();
                else _food.Hide();
            }
            catch { }
        }

        // 游戏模式下隐藏/恢复气泡窗口
        public void HideBubbleForGame(bool show)
        {
            try
            {
                if (_bubble == null || _bubble.IsDisposed) return;

                if (show)
                {
                    // 恢复：若还有文字则重新显示
                    if (!string.IsNullOrEmpty(_bubbleText)) SyncBubbleWindow();
                }
                else
                {
                    _bubble.Hide();
                }
            }
            catch { }
        }

        public void DropFood()
        {
            if (_food != null && !_food.IsDisposed) return;
            if (!FoodForm.Available) return;

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Rectangle panel = _app.PanelBounds;
            Random rnd = new Random();

            int size = 72;
            int minX = wa.Left + (int)(wa.Width * 0.55f);
            int maxX = wa.Right - size - 20;
            int minY = wa.Top + (int)(wa.Height * 0.55f);
            int maxY = wa.Bottom - size - 20;

            Point pos = new Point(maxX - 40, maxY - 40);
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int x = rnd.Next(minX, Math.Max(minX + 1, maxX));
                int y = rnd.Next(minY, Math.Max(minY + 1, maxY));
                Rectangle r = new Rectangle(x, y, size, size);
                if (panel.Width > 0 && panel.IntersectsWith(r)) continue;
                pos = new Point(x, y);
                break;
            }

            try
            {
                _food = new FoodForm(this, pos);
                _food.FormClosed += delegate { _food = null; };
                _food.Show();
            }
            catch { _food = null; }
        }

        public void HighlightForFood(bool on)
        {
            if (_highlight == on) return;
            _highlight = on;
            if (_app.PanelForm != null) _app.PanelForm.Redraw();
        }

        // ---------- 绘制 ----------
        public void DrawOnto(Graphics g, Rectangle dest)
        {
            EnsureLoaded();
            Image img = (_sprites != null && _frame < _sprites.Length) ? _sprites[_frame] : null;
            if (img == null) return;

            // 魈始终占满整个区域，尺寸不受气泡影响（气泡由独立窗口绘制）
            int maxPetH = (int)(dest.Height * 0.72f);
            double sc = Math.Min(dest.Width / (double)img.Width, maxPetH / (double)img.Height);
            int dw = (int)Math.Round(img.Width * sc);
            int dh = (int)Math.Round(img.Height * sc);

            // 水平位置：居中 + 走动偏移
            int baseDx = dest.X + (dest.Width - dw) / 2;
            int maxShift = Math.Max(0, (dest.Width - dw) / 2);
            int walkShift = (int)Math.Round(WalkOffset * maxShift);
            int dx = baseDx + walkShift;

            int dy = dest.Bottom - dh;
            g.DrawImage(img, new Rectangle(dx, dy, dw, dh));
        }

        static GraphicsPath RoundPath(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = Math.Max(2, rad * 2);
            if (d >= r.Width || d >= r.Height) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public void Redraw()
        {
            if (_running)
            {
                if (_app != null && _app.PanelForm != null) _app.PanelForm.Redraw();
                return;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080;
                cp.ExStyle |= 0x08000000;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }
    }

    // ======================= 气泡对话窗口 =======================
    // 独立透明窗口显示台词，不占用桌宠绘制空间，魈的尺寸因此保持恒定。
    internal sealed class BubbleForm : Form
    {
        string _text = "";
        bool _sticky;

        public BubbleForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            AutoScaleDimensions = new SizeF(1f, 1f);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        // 设置文字并重新计算尺寸与位置
        public void ShowText(string text, bool sticky, Rectangle petArea)
        {
            _text = text ?? "";
            _sticky = sticky;

            if (string.IsNullOrEmpty(_text) || petArea.Width <= 0)
            {
                Hide();
                return;
            }

            using (Font f = new Font("Microsoft YaHei UI", 9f))
            {
                int padX = 10, padY = 6;
                int maxTextW = Math.Max(60, (int)(petArea.Width * 0.92f) - padX * 2);

                SizeF sz;
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    sz = g.MeasureString(_text, f, maxTextW, sf);
                }

                int bw = (int)Math.Ceiling(sz.Width) + padX * 2;
                int bh = (int)Math.Ceiling(sz.Height) + padY * 2 + 6;
                if (bw < 60) bw = 60;

                // 位置：魈的上方，水平居中
                int x = petArea.X + (petArea.Width - bw) / 2;
                int y = petArea.Y - bh - 4;

                // 避免超出屏幕顶部
                Rectangle wa = Screen.FromRectangle(petArea).WorkingArea;
                if (y < wa.Top) y = petArea.Y + 2;
                if (x < wa.Left) x = wa.Left;
                if (x + bw > wa.Right) x = wa.Right - bw;

                Size = new Size(bw, bh);
                Location = new Point(x, y);

                if (!Visible) Show();
                Redraw();
            }
        }

        public void ClearText()
        {
            _text = "";
            Hide();
        }

        void Redraw()
        {
            if (!IsHandleCreated || Width <= 0 || Height <= 0) return;
            try
            {
                using (Bitmap canvas = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(canvas))
                    {
                        g.Clear(Color.Transparent);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                        using (Font f = new Font("Microsoft YaHei UI", 9f))
                        {
                            int padX = 10, padY = 6;
                            int bh = Height - 7;   // 留出尖角空间

                            Rectangle box = new Rectangle(0, 0, Width, bh);
                            using (GraphicsPath gp = RoundRect(box, 8))
                            using (Brush bg = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
                            using (Pen bp = new Pen(Color.FromArgb(210, 210, 215)))
                            {
                                g.FillPath(bg, gp);
                                g.DrawPath(bp, gp);
                            }

                            // 底部尖角
                            Point[] tri = new Point[]
                            {
                                new Point(Width / 2 - 5, box.Bottom - 1),
                                new Point(Width / 2 + 5, box.Bottom - 1),
                                new Point(Width / 2, box.Bottom + 6)
                            };
                            using (Brush bg2 = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
                                g.FillPolygon(bg2, tri);

                            Rectangle textRect = new Rectangle(
                                padX, padY, Width - padX * 2, bh - padY * 2);

                            using (StringFormat sf = new StringFormat())
                            {
                                sf.Alignment = StringAlignment.Center;
                                sf.LineAlignment = StringAlignment.Center;
                                using (Brush tb = new SolidBrush(Color.FromArgb(40, 40, 45)))
                                    g.DrawString(_text, f, tb, textRect, sf);
                            }
                        }
                    }
                    Native.ApplyLayered(Handle, canvas, Left, Top, 255);
                }
            }
            catch { }
        }

        static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = Math.Max(2, rad * 2);
            if (d >= r.Width || d >= r.Height) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }
    }

    // ======================= 掉落食物窗口 =======================
    // 一个可拖动的小窗口，显示杏仁豆腐。拖到桌宠身上即完成喂食。
    internal sealed class FoodForm : Form
    {
        static Image _img;
        static bool _loaded;

        public static void EnsureImage()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                using (Stream s = System.Reflection.Assembly.GetExecutingAssembly()
                           .GetManifestResourceStream("pet_food.png"))
                {
                    if (s != null)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            s.CopyTo(ms);
                            _img = Image.FromStream(ms);
                        }
                    }
                }
            }
            catch { _img = null; }
        }

        public static bool Available { get { EnsureImage(); return _img != null; } }

        readonly PetForm _pet;
        readonly Timer _anim;
        bool _dragging;
        Point _dragOffset;
        bool _draggedEnough;
        int _bobPhase;
        int _spawnY;          // 起始高度（用于下落动画）
        bool _dropping;

        const int FoodSize = 64;

        public FoodForm(PetForm pet, Point pos)
        {
            _pet = pet;
            EnsureImage();

            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            Size = new Size(FoodSize + 8, FoodSize + 8);
            _spawnY = pos.Y - 60;          // 从上方掉落
            Location = new Point(pos.X, _spawnY);
            _dropping = true;

            _anim = new Timer();
            _anim.Interval = 25;
            _anim.Tick += delegate { Tick(); };
            _anim.Start();

            MouseDown += OnDown;
            MouseMove += OnMove;
            MouseUp += OnUp;
            Cursor = Cursors.Hand;
        }

        int _groundY;

        void Tick()
        {
            // 下落动画
            if (_dropping)
            {
                _groundY = Screen.FromPoint(Location).WorkingArea.Bottom - Height - 8;

                int ny = Location.Y + 9;
                if (ny >= _groundY)
                {
                    ny = _groundY;
                    _dropping = false;
                }
                Location = new Point(Location.X, ny);
                return;
            }

            if (_dragging) return;

            // 静止时轻微上下浮动
            _bobPhase++;
            Redraw();
        }

        void OnDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _dragging = true;
            _draggedEnough = false;
            _dragOffset = e.Location;
        }

        // 桌宠在屏幕上的实际范围（合并模式下由面板窗口的桌宠区决定）
        Rectangle PetScreenBounds()
        {
            if (_pet == null) return Rectangle.Empty;
            KeyStateForm panel = _pet.PanelRef;
            if (panel == null || !panel.Visible) return Rectangle.Empty;
            Rectangle pb = panel.Bounds;
            int petH = panel.PetAreaHeight;
            return new Rectangle(pb.X, pb.Y, pb.Width, petH);
        }

        void OnMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            _draggedEnough = true;
            _dropping = false;
            Point sp = PointToScreen(e.Location);
            Location = new Point(sp.X - _dragOffset.X, sp.Y - _dragOffset.Y);
            Redraw();

            // 实时检测是否进入桌宠范围
            Rectangle petRect = PetScreenBounds();
            bool hit = petRect.Width > 0 && petRect.IntersectsWith(Bounds);
            _pet.HighlightForFood(hit);
        }

        void OnUp(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            _dragging = false;

            if (!_draggedEnough)
            {
                // 单击不做事，保持掉落物存在
                return;
            }

            // 判断是否投喂成功
            Rectangle petRect = PetScreenBounds();
            if (petRect.Width > 0 && petRect.IntersectsWith(Bounds))
            {
                _pet.HighlightForFood(false);
                _pet.FeedFromFood();
                Close();
                return;
            }

            _pet.HighlightForFood(false);
            // 未投中：落到最近的地面
            _dropping = true;
        }

        void Redraw()
        {
            if (!IsHandleCreated) return;
            try
            {
                using (Bitmap canvas = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(canvas))
                    {
                        g.Clear(Color.Transparent);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        if (_img != null)
                        {
                            int bob = _dragging ? 0 : (int)(Math.Sin(_bobPhase * 0.12) * 3);
                            int sc = FoodSize;
                            double s = Math.Min(sc / (double)_img.Width, sc / (double)_img.Height);
                            int dw = (int)Math.Round(_img.Width * s);
                            int dh = (int)Math.Round(_img.Height * s);
                            int dx = (Width - dw) / 2;
                            int dy = (Height - dh) / 2 + bob;

                            // 柔和阴影
                            using (Brush sh = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                                g.FillEllipse(sh, dx + dw / 6, dy + dh - dh / 7, dw - dw / 3, dh / 7);

                            g.DrawImage(_img, new Rectangle(dx, dy, dw, dh));
                        }
                    }
                    Native.ApplyLayered(Handle, canvas, Left, Top, 255);
                }
            }
            catch { }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
                return cp;
            }
        }

        protected override void OnShown(EventArgs e) { base.OnShown(e); Redraw(); }
        protected override bool ShowWithoutActivation { get { return true; } }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _anim != null) { _anim.Stop(); _anim.Dispose(); }
            base.Dispose(disposing);
        }
    }
    // ======================= 入口 =======================
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
