// 桌面键盘 —— 悬浮按键计数器（Windows）
// 逐像素透明（UpdateLayeredWindow）+ 按压动画（缩放 / 光晕 / 扩散圈 / 数字弹跳）
// 编译：build.ps1，或直接 csc /target:winexe ...
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DshKeyStats
{
    internal static class Native
    {
        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_RBUTTONDOWN = 0x0204;

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12;

        public const int ULW_ALPHA = 2;
        public const byte AC_SRC_OVER = 0;
        public const byte AC_SRC_ALPHA = 1;

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public int x;
            public int y;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public const int SWP_NOSIZE = 0x0001;
        public const int SWP_NOMOVE = 0x0002;
        public const int SWP_NOACTIVATE = 0x0010;
        public const int SWP_NOOWNERZORDER = 0x0200;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr h);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
            IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        public const int WM_INPUT = 0x00FF;
        public const uint RID_INPUT = 0x10000003;
        public const uint RIDEV_INPUTSINK = 0x00000100;
        public const uint RIM_TYPEKEYBOARD = 1;
        public const uint RIM_TYPEMOUSE = 0;

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWMOUSE
        {
            public ushort usFlags;
            public ushort usPadding;
            public ushort usButtonFlags;
            public ushort usButtonData;
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint numDevices, uint size);

        [DllImport("user32.dll")]
        public static extern uint GetRawInputData(IntPtr hRawInput, uint command, IntPtr data, ref uint size, uint headerSize);

        public static bool IsKeyDown(int vk)
        {
            return (GetAsyncKeyState(vk) & 0x8000) != 0;
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool created;
            using (Mutex mutex = new Mutex(true, "DshDesktopKeyStats_SingleInstance_v1", out created))
            {
                if (!created)
                {
                    return;
                }

                Native.SetProcessDPIAware();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new KeyStatsForm());
            }
        }
    }

    internal sealed class KeyStatsForm : Form
    {
        private sealed class KeyCell
        {
            public string Id;
            public string Label;
            public int Vk;
            public int VkAlt;
            public int MouseButton; // 0 = 键盘, 1 = 左键, 2 = 右键
            public RectangleF Rect;
            public long Count;
            public long PrevCount;
            public bool Down;
            public long PressedUntil;
            public long PressStart;
            public long RingStart;
            public long PopStart;
            public long LastPress;
        }

        private const string ConfigFileName = "桌面键盘.config.txt";

        private static readonly Color Accent = Color.FromArgb(56, 189, 248);
        private static readonly Color AccentSoft = Color.FromArgb(186, 230, 253);
        private static readonly Color Muted = Color.FromArgb(148, 163, 184);
        private static readonly Color PanelBack = Color.FromArgb(15, 19, 27);
        private static readonly Color CellBack = Color.FromArgb(255, 255, 255);
        private static readonly Color CellEdge = Color.FromArgb(255, 255, 255);

        private static readonly StringFormat CenterFmt = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.None
        };

        private static readonly StringFormat NearFmt = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.None
        };

        private readonly List<KeyCell> _cells = new List<KeyCell>();
        private readonly Dictionary<int, KeyCell> _byVk = new Dictionary<int, KeyCell>();
        private readonly Dictionary<int, KeyCell> _byMouse = new Dictionary<int, KeyCell>();
        private readonly List<long> _pressTimes = new List<long>();
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        private float _scale = 1f;
        private float _userScale = 1f;
        private double _opacity = 0.62;
        private float _u, _v, _gap, _margin, _headerH, _footerH, _cellRadius, _radius;
        private float _shadowOffset = 1.4f;

        private bool _locked = true;
        private bool _dragging;
        private Point _dragOrigin;
        private Point _dragLocation;
        private KeyCell _clickCell;

        private IntPtr _kbHook;
        private IntPtr _msHook;
        private Native.HookProc _kbProc;
        private Native.HookProc _msProc;

        private double _cps;
        private double _peakCps;
        private long _total;

        private string _toast;
        private long _toastStart;
        private long _toastEnd;

        private Font _titleFont;
        private Font _bigFont;
        private Font _smallFont;
        private Font _cpsFont;
        private Font _footerFont;

        private Bitmap _canvas;
        private Bitmap _scene;
        private bool _sceneDirty = true;
        private int _panelW;
        private int _panelH;



        private bool _needsPaint = true;
        private bool _wasAnimating;
        private bool _dirty;
        private long _lastSave;
        private long _lastTopmost;
        private string _configPath;
        private bool _saveWarned;
        private bool _stayOnTop = true;
        private bool _debugLog;
        private string _captureMode = "auto";
        private long _lastHookMs;
        private long _lastPaint;

        private ContextMenuStrip _menu;
        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();

        public KeyStatsForm()
        {
            Text = "桌面键盘";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = PanelBack;
            ClientSize = new Size(506, 234);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            CreateCells();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            using (Graphics g = CreateGraphics())
            {
                _scale = g.DpiX / 96f;
            }
            if (_scale <= 0f)
            {
                _scale = 1f;
            }

            CreateFonts();
            BuildLayout();
            LoadConfig();
            ApplyLocked(_locked, false);

            InstallHooks();
            InstallRawInput();
            _timer.Interval = 16;
            _timer.Tick += delegate { OnTick(); };
            _timer.Start();

            if (_total > 0)
            {
                ShowToast("已恢复上次计数：总计 " + _total + " · Ctrl+Alt+R 清零");
            }
            else
            {
                ShowToast("就绪 · Ctrl+Alt+L 解锁拖动 · Ctrl+Alt+T 调透明度");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timer.Stop();
            UninstallHooks();
            SaveConfig();
            if (_canvas != null)
            {
                _canvas.Dispose();
                _canvas = null;
            }
            if (_scene != null)
            {
                _scene.Dispose();
                _scene = null;
            }
            base.OnFormClosing(e);
        }

        // 内容由 Render() 推送到分层窗口，普通绘制全部关闭
        protected override void OnPaint(PaintEventArgs e)
        {
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        private void CreateCells()
        {
            _cells.Add(new KeyCell { Id = "Q", Label = "Q", Vk = 0x51 });
            _cells.Add(new KeyCell { Id = "W", Label = "W", Vk = 0x57 });
            _cells.Add(new KeyCell { Id = "E", Label = "E", Vk = 0x45 });
            _cells.Add(new KeyCell { Id = "R", Label = "R", Vk = 0x52 });
            _cells.Add(new KeyCell { Id = "A", Label = "A", Vk = 0x41 });
            _cells.Add(new KeyCell { Id = "S", Label = "S", Vk = 0x53 });
            _cells.Add(new KeyCell { Id = "D", Label = "D", Vk = 0x44 });
            _cells.Add(new KeyCell { Id = "LMB", Label = "LMB", MouseButton = 1 });
            _cells.Add(new KeyCell { Id = "RMB", Label = "RMB", MouseButton = 2 });
            _cells.Add(new KeyCell { Id = "F", Label = "F", Vk = 0x46 });
            _cells.Add(new KeyCell { Id = "G", Label = "G", Vk = 0x47 });
            _cells.Add(new KeyCell { Id = "H", Label = "H", Vk = 0x48 });
            _cells.Add(new KeyCell { Id = "J", Label = "J", Vk = 0x4A });
            _cells.Add(new KeyCell { Id = "K", Label = "K", Vk = 0x4B });
            _cells.Add(new KeyCell { Id = "L", Label = "L", Vk = 0x4C });
            _cells.Add(new KeyCell { Id = "Z", Label = "Z", Vk = 0x5A });
            _cells.Add(new KeyCell { Id = "X", Label = "X", Vk = 0x58 });
            _cells.Add(new KeyCell { Id = "C", Label = "C", Vk = 0x43 });
            _cells.Add(new KeyCell { Id = "Space", Label = "空格", Vk = 0x20 });
            _cells.Add(new KeyCell { Id = "Shift", Label = "Shift", Vk = 0xA0, VkAlt = 0xA1 });

            foreach (KeyCell c in _cells)
            {
                if (c.MouseButton != 0)
                {
                    _byMouse[c.MouseButton] = c;
                }
                if (c.Vk != 0)
                {
                    _byVk[c.Vk] = c;
                }
                if (c.VkAlt != 0)
                {
                    _byVk[c.VkAlt] = c;
                }
            }
        }

        private void CreateFonts()
        {
            // 分层窗口用 GDI+ 绘制，字体统一用「像素」单位，按 DPI+用户缩放换算
            float k = _scale * _userScale;
            string family = "Microsoft YaHei UI";
            _titleFont = MakeFont(family, 10f, FontStyle.Bold, k);
            _bigFont = MakeFont(family, 13f, FontStyle.Bold, k);      // 键名（大）
            _smallFont = MakeFont(family, 8.5f, FontStyle.Regular, k); // 次数（小）
            _cpsFont = MakeFont(family, 9f, FontStyle.Regular, k);
            _footerFont = MakeFont(family, 7.8f, FontStyle.Regular, k);
            _shadowOffset = 1.4f * k;
        }

        private static Font MakeFont(string family, float pt, FontStyle style, float k)
        {
            float px = pt * 96f / 72f * k;
            try
            {
                return new Font(family, px, style, GraphicsUnit.Pixel);
            }
            catch
            {
                try
                {
                    return new Font(family, px, FontStyle.Regular, GraphicsUnit.Pixel);
                }
                catch
                {
                    return new Font(FontFamily.GenericSansSerif, px, style, GraphicsUnit.Pixel);
                }
            }
        }

        // ---------- 布局 ----------

        private void BuildLayout()
        {
            float k = _scale * _userScale;
            _margin = 14f * k;
            _headerH = 30f * k;
            _footerH = 22f * k;
            _u = 46f * k;
            _v = 42f * k;
            _gap = 6f * k;
            _cellRadius = 8f * k;
            _radius = 12f * k;

            float clusterGap = 22f * k;
            float leftW = 3f * _u + 2f * _gap;
            float lettersW = 6f * _u + 5f * _gap;
            float contentW = leftW + clusterGap + lettersW;
            float contentH = 3f * _v + 2f * _gap;

            int width = (int)Math.Round(contentW + 2f * _margin);
            int height = (int)Math.Round(_margin + _headerH + 8f * k + contentH + 8f * k + _footerH + _margin);

            _panelW = width;
            _panelH = height;

            ClientSize = new Size(width, height);

            float x0 = _margin;
            float xL = _margin + leftW + clusterGap;
            float y0 = _margin + _headerH + 8f * k;
            float y1 = y0 + _v + _gap;
            float y2 = y1 + _v + _gap;
            float rightX = xL + lettersW - 2f * _u - _gap;

            Place("Q", x0, y0, _u, _v);
            Place("W", x0 + _u + _gap, y0, _u, _v);
            Place("E", x0 + 2f * (_u + _gap), y0, _u, _v);
            Place("R", x0 + 3f * (_u + _gap), y0, _u, _v);
            Place("LMB", rightX, y0, _u, _v);
            Place("RMB", rightX + _u + _gap, y0, _u, _v);

            Place("A", x0, y1, _u, _v);
            Place("S", x0 + _u + _gap, y1, _u, _v);
            Place("D", x0 + 2f * (_u + _gap), y1, _u, _v);

            string[] letters = new string[] { "F", "G", "H", "J", "K", "L" };
            for (int i = 0; i < letters.Length; i++)
            {
                Place(letters[i], xL + i * (_u + _gap), y1, _u, _v);
            }

            Place("Shift", x0, y2, 1.55f * _u, _v);
            float zx = x0 + 1.55f * _u + _gap;
            Place("Z", zx, y2, _u, _v);
            Place("X", zx + _u + _gap, y2, _u, _v);
            Place("C", zx + 2f * (_u + _gap), y2, _u, _v);
            float spaceX = zx + 3f * (_u + _gap);
            Place("Space", spaceX, y2, xL + lettersW - spaceX, _v);
        }

        private void Place(string id, float x, float y, float w, float h)
        {
            KeyCell cell = FindCell(id);
            if (cell == null)
            {
                return;
            }
            cell.Rect = new RectangleF((float)Math.Round(x), (float)Math.Round(y), (float)Math.Round(w), (float)Math.Round(h));
        }

        private KeyCell FindCell(string id)
        {
            foreach (KeyCell c in _cells)
            {
                if (string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return c;
                }
            }
            return null;
        }

        // ---------- 渲染 ----------

        private void EnsureBuffers()
        {
            int w = Math.Max(1, ClientSize.Width);
            int h = Math.Max(1, ClientSize.Height);
            if (_canvas != null && _canvas.Width == w && _canvas.Height == h)
            {
                return;
            }
            if (_canvas != null)
            {
                _canvas.Dispose();
            }
            if (_scene != null)
            {
                _scene.Dispose();
            }
            _canvas = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            _scene = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            _sceneDirty = true;
        }

        private void Render(bool redrawScene)
        {
            if (!IsHandleCreated || IsDisposed)
            {
                return;
            }
            EnsureBuffers();

            if (redrawScene || _sceneDirty)
            {
                using (Graphics sg = Graphics.FromImage(_scene))
                {
                    sg.Clear(Color.Transparent);
                    sg.SmoothingMode = SmoothingMode.AntiAlias;
                    sg.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    sg.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    sg.CompositingMode = CompositingMode.SourceOver;
                    DrawScene(sg);
                }
                _sceneDirty = false;
            }

            using (Graphics g = Graphics.FromImage(_canvas))
            {
                g.Clear(Color.Transparent);
                g.CompositingMode = CompositingMode.SourceOver;
                g.DrawImageUnscaled(_scene, 0, 0);
            }

            PushLayered();
            _lastPaint = _clock.ElapsedMilliseconds;
            _needsPaint = false;
        }

        private void PushLayered()
        {
            IntPtr screenDc = Native.GetDC(IntPtr.Zero);
            IntPtr memDc = Native.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = _canvas.GetHbitmap(Color.FromArgb(0));
            IntPtr oldBitmap = Native.SelectObject(memDc, hBitmap);

            Native.POINT top = new Native.POINT();
            top.x = Left;
            top.y = Top;
            Native.SIZE size = new Native.SIZE();
            size.cx = _canvas.Width;
            size.cy = _canvas.Height;
            Native.POINT src = new Native.POINT();
            Native.BLENDFUNCTION blend = new Native.BLENDFUNCTION();
            blend.BlendOp = Native.AC_SRC_OVER;
            blend.BlendFlags = 0;
            blend.SourceConstantAlpha = 255;
            blend.AlphaFormat = Native.AC_SRC_ALPHA;

            Native.UpdateLayeredWindow(Handle, screenDc, ref top, ref size, memDc, ref src, 0, ref blend, Native.ULW_ALPHA);

            Native.SelectObject(memDc, oldBitmap);
            Native.DeleteObject(hBitmap);
            Native.DeleteDC(memDc);
            Native.ReleaseDC(IntPtr.Zero, screenDc);
        }

        private void DrawScene(Graphics g)
        {
            long now = _clock.ElapsedMilliseconds;
            float k = _scale * _userScale;


            // 解锁（要拖动/点按键）时自动提高不透明度，方便看清界面；锁定后恢复设定值
            double effective = _locked ? _opacity : Math.Max(_opacity, 0.8);
            int panelAlpha = (int)Math.Round(255 * effective);
            int edgeAlpha = (int)Math.Round(70 * effective);

            using (GraphicsPath panel = RoundRect(new RectangleF(0.5f, 0.5f, _panelW - 1f, _panelH - 1f), _radius))
            {
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(panelAlpha, PanelBack)))
                {
                    g.FillPath(bg, panel);
                }
                using (Pen edge = new Pen(Color.FromArgb(edgeAlpha, 255, 255, 255), 1.2f * k))
                {
                    g.DrawPath(edge, panel);
                }
            }

            // 标题
            DrawText(g, "按键统计", _titleFont, Color.FromArgb(245, 235, 240, 248),
                new RectangleF(_margin, _margin, _panelW * 0.5f, _headerH), CenterFmt, StringAlignment.Near);

            // 右上角：CPS + 总计
            string cpsText = "CPS " + _cps.ToString("0.0", CultureInfo.InvariantCulture);
            string totalText = "总计 " + _total.ToString(CultureInfo.InvariantCulture);
            float cpsW = g.MeasureString(cpsText, _cpsFont).Width;
            float totalW = g.MeasureString(totalText, _cpsFont).Width;
            float totalX = _panelW - _margin - totalW;
            DrawText(g, totalText, _cpsFont, Color.FromArgb(220, Muted),
                new RectangleF(totalX, _margin, totalW + 4f, _headerH), CenterFmt, StringAlignment.Near);
            DrawText(g, cpsText, _cpsFont, Color.FromArgb(235, Accent),
                new RectangleF(totalX - 14f * k - cpsW, _margin, cpsW + 4f, _headerH), CenterFmt, StringAlignment.Near);

            // 按键
            foreach (KeyCell c in _cells)
            {
                DrawCell(g, c, now, k);
            }

            // 底部提示 / 临时提示
            RectangleF footer = new RectangleF(_margin, _panelH - _margin - _footerH,
                _panelW - 2f * _margin, _footerH);

            if (_toast != null)
            {
                float alpha = ToastAlpha(now);
                if (alpha > 0.02f)
                {
                    float rise = (1f - alpha) * 8f * k;
                    RectangleF tr = footer;
                    tr.Y -= rise;
                    DrawText(g, _toast, _footerFont, Color.FromArgb((int)(255 * alpha), AccentSoft), tr, CenterFmt, StringAlignment.Center);
                }
            }
            else
            {
                string hint = "峰值 " + _peakCps.ToString("0.0", CultureInfo.InvariantCulture) +
                              " · Ctrl+Alt+T 透明度 · Ctrl+Alt+L 锁定 · Ctrl+Alt+R 清零";
                DrawText(g, hint, _footerFont, Color.FromArgb(200, 110, 125, 148), footer, CenterFmt, StringAlignment.Center);
            }
        }

        private void DrawCell(Graphics g, KeyCell cell, long now, float k)
        {
            RectangleF r = cell.Rect;
            if (r.Width <= 0f || r.Height <= 0f)
            {
                return;
            }

            bool active = cell.Down || now < cell.PressedUntil;
            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;

            // 按压回弹
            float pressScale = 1f;
            if (cell.PressStart > 0 && now >= cell.PressStart && now < cell.PressStart + 160)
            {
                double q = (now - cell.PressStart) / 160.0;
                double e = 1.0 - q;
                pressScale = (float)(1.0 + 0.055 * e * e);
            }
            else if (cell.Down)
            {
                pressScale = 1.03f;
            }

            // 扩散圈
            double ring = -1.0;
            if (cell.RingStart > 0 && now >= cell.RingStart && now < cell.RingStart + 430)
            {
                ring = (now - cell.RingStart) / 430.0;
            }

            float glow = active ? 1f : 0f;
            if (ring >= 0.0)
            {
                glow = Math.Max(glow, (float)(1.0 - ring));
            }
            if (glow > 0.02f)
            {
                RectangleF gr = InflateRect(r, 6f * k);
                using (GraphicsPath gp = RoundRect(gr, _cellRadius + 4f * k))
                using (SolidBrush gb = new SolidBrush(Color.FromArgb((int)(70 * glow), Accent)))
                {
                    g.FillPath(gb, gp);
                }
            }

            if (ring >= 0.0)
            {
                float expand = (float)(4f * k + 22f * k * ring);
                RectangleF rr = InflateRect(r, expand);
                int alpha = (int)(140 * Math.Pow(1.0 - ring, 2.0));
                if (alpha > 2)
                {
                    using (GraphicsPath rp = RoundRect(rr, _cellRadius + 8f * k * (float)ring))
                    using (Pen ringPen = new Pen(Color.FromArgb(alpha, Accent), 2f * k))
                    {
                        g.DrawPath(ringPen, rp);
                    }
                }
            }

            GraphicsState cellState = g.Save();
            g.TranslateTransform(cx, cy);
            g.ScaleTransform(pressScale, pressScale);
            g.TranslateTransform(-cx, -cy);

            using (GraphicsPath path = RoundRect(r, _cellRadius))
            {
                Color fill = active
                    ? Color.FromArgb(232, 46, 158, 214)
                    : Color.FromArgb(54, CellBack);
                using (SolidBrush b = new SolidBrush(fill))
                {
                    g.FillPath(b, path);
                }

                Color edge = active
                    ? Color.FromArgb(255, 186, 230, 253)
                    : Color.FromArgb(72, CellEdge);
                using (Pen pen = new Pen(edge, (active ? 1.6f : 1f) * k))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 键名：大号，放在格子上方
            Color letterColor = active ? Color.White : Color.FromArgb(232, 238, 246);
            DrawText(g, cell.Label, _bigFont, letterColor,
                new RectangleF(r.X, r.Y + 2f * k, r.Width, 19f * k), CenterFmt, StringAlignment.Center);

            // 次数：小号放在右上角
            double qt = -1.0;
            if (cell.PopStart > 0 && now >= cell.PopStart && now < cell.PopStart + 220)
            {
                qt = (now - cell.PopStart) / 220.0;
            }
            Color numColor = active ? Color.White : Color.FromArgb(215, 150, 165, 186);
            float numScale = 1f;
            if (qt >= 0.0)
            {
                double e = 1.0 - qt;
                numScale = (float)(1.0 + 0.15 * e * e);
                numColor = Lerp(AccentSoft, numColor, (float)qt);
            }

            RectangleF numRect = new RectangleF(r.X, r.Y + 19f * k, r.Width, r.Height - 19f * k);
            string countText = cell.Count.ToString(CultureInfo.InvariantCulture);
            if (Math.Abs(numScale - 1f) > 0.001f)
            {
                GraphicsState numState = g.Save();
                float ncx = numRect.X + numRect.Width / 2f;
                float ncy = numRect.Y + numRect.Height / 2f;
                g.TranslateTransform(ncx, ncy);
                g.ScaleTransform(numScale, numScale);
                g.TranslateTransform(-ncx, -ncy);
                DrawText(g, countText, _smallFont, numColor, numRect, CenterFmt, StringAlignment.Center);
                g.Restore(numState);
            }
            else
            {
                DrawText(g, countText, _smallFont, numColor, numRect, CenterFmt, StringAlignment.Center);
            }

            g.Restore(cellState);
        }

        private float ToastAlpha(long now)
        {
            if (_toast == null)
            {
                return 0f;
            }
            long t = now - _toastStart;
            if (t < 0)
            {
                return 0f;
            }
            if (t < 140)
            {
                return t / 140f;
            }
            if (now > _toastEnd)
            {
                float fade = (now - _toastEnd) / 320f;
                return Math.Max(0f, 1f - fade);
            }
            return 1f;
        }

        private void DrawText(Graphics g, string text, Font font, Color color, RectangleF rect,
            StringFormat format, StringAlignment alignment, bool withShadow = true)
        {
            if (text == null || text.Length == 0)
            {
                return;
            }
            StringFormat fmt = format;
            if (format.Alignment != alignment)
            {
                fmt = new StringFormat(format);
                fmt.Alignment = alignment;
            }

            using (SolidBrush brush = new SolidBrush(color))
            {
                // 先画一层暗色阴影，面板很透时文字在任何背景上都还读得清
                if (withShadow)
                {
                    RectangleF shadowRect = rect;
                    shadowRect.X += _shadowOffset;
                    shadowRect.Y += _shadowOffset;
                    using (SolidBrush shadow = new SolidBrush(Color.FromArgb(Math.Min(150, (int)color.A), 0, 0, 0)))
                    {
                        g.DrawString(text, font, shadow, shadowRect, fmt);
                    }
                }
                g.DrawString(text, font, brush, rect, fmt);
            }

            if (!ReferenceEquals(fmt, format))
            {
                fmt.Dispose();
            }
        }

        private static Color Lerp(Color a, Color b, float t)
        {
            if (t <= 0f)
            {
                return a;
            }
            if (t >= 1f)
            {
                return b;
            }
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        private static RectangleF InflateRect(RectangleF r, float d)
        {
            return new RectangleF(r.X - d, r.Y - d, r.Width + 2f * d, r.Height + 2f * d);
        }

        private static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > r.Width)
            {
                d = r.Width;
            }
            if (d > r.Height)
            {
                d = r.Height;
            }
            if (d <= 0f)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180f, 90f);
            path.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
            path.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        // ---------- 钩子 ----------

        private void InstallHooks()
        {
            if (string.Equals(_captureMode, "raw", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _kbProc = new Native.HookProc(KeyboardHookProc);
            _kbHook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _kbProc, Native.GetModuleHandle(null), 0);

            _msProc = new Native.HookProc(MouseHookProc);
            _msHook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _msProc, Native.GetModuleHandle(null), 0);

            if (_kbHook == IntPtr.Zero)
            {
                ShowToast("键盘钩子安装失败（可能被安全软件拦截）");
            }
        }

        private void UninstallHooks()
        {
            if (_kbHook != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(_kbHook);
                _kbHook = IntPtr.Zero;
            }
            if (_msHook != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(_msHook);
                _msHook = IntPtr.Zero;
            }
        }

        // ---------- Raw Input（不依赖键盘钩子的第二条采集通道）----------

        private void InstallRawInput()
        {
            if (string.Equals(_captureMode, "hook", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            try
            {
                Native.RAWINPUTDEVICE[] devices = new Native.RAWINPUTDEVICE[2];

                devices[0].usUsagePage = 0x01;
                devices[0].usUsage = 0x06; // 键盘
                devices[0].dwFlags = Native.RIDEV_INPUTSINK;
                devices[0].hwndTarget = Handle;

                devices[1].usUsagePage = 0x01;
                devices[1].usUsage = 0x02; // 鼠标
                devices[1].dwFlags = Native.RIDEV_INPUTSINK;
                devices[1].hwndTarget = Handle;

                Native.RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf(typeof(Native.RAWINPUTDEVICE)));
            }
            catch
            {
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_INPUT)
            {
                try
                {
                    HandleRawInput(m.LParam);
                }
                catch
                {
                }
            }
            base.WndProc(ref m);
        }

        private void HandleRawInput(IntPtr lParam)
        {
            uint headerSize = (uint)Marshal.SizeOf(typeof(Native.RAWINPUTHEADER));
            uint size = 0;
            Native.GetRawInputData(lParam, Native.RID_INPUT, IntPtr.Zero, ref size, headerSize);
            if (size == 0)
            {
                return;
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (Native.GetRawInputData(lParam, Native.RID_INPUT, buffer, ref size, headerSize) != size)
                {
                    return;
                }

                Native.RAWINPUTHEADER header =
                    (Native.RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(Native.RAWINPUTHEADER));
                IntPtr data = new IntPtr(buffer.ToInt64() + header.dwSize);


                if (header.dwType == Native.RIM_TYPEKEYBOARD)
                {
                    HandleRawKey((Native.RAWKEYBOARD)Marshal.PtrToStructure(data, typeof(Native.RAWKEYBOARD)));
                }
                else if (header.dwType == Native.RIM_TYPEMOUSE)
                {
                    HandleRawMouse((Native.RAWMOUSE)Marshal.PtrToStructure(data, typeof(Native.RAWMOUSE)));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private void HandleRawKey(Native.RAWKEYBOARD kb)
        {
            int vk = kb.VKey;
            if (vk == 0 || vk == 0xFF)
            {
                return;
            }

            bool down = kb.Message == 0x0100 || kb.Message == 0x0104;
            if (kb.Message == 0)
            {
                down = (kb.Flags & 0x01) == 0;
            }

            if (_captureMode != "raw" && _clock.ElapsedMilliseconds - _lastHookMs < 3000)
            {
                return;
            }

            if (_debugLog)
            {
                try
                {
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"),
                        _clock.ElapsedMilliseconds + " rawkey vk=" + vk + " down=" + down + "\r\n");
                }
                catch
                {
                }
            }

            // 快捷键不计数
            if (down && (vk == 0x4C || vk == 0x52 || vk == 0x54 || vk == 0x51) &&
                Native.IsKeyDown(Native.VK_CONTROL) && Native.IsKeyDown(Native.VK_MENU))
            {
                return;
            }

            KeyCell cell;
            if (_byVk.TryGetValue(vk, out cell))
            {
                if (down && !cell.Down)
                {
                    cell.Down = true;
                    RegisterPress(cell, "raw");
                }
                else if (!down)
                {
                    cell.Down = false;
                }
            }
        }

        private void HandleRawMouse(Native.RAWMOUSE ms)
        {
            if (_captureMode != "raw" && _clock.ElapsedMilliseconds - _lastHookMs < 3000)
            {
                return;
            }

            int flags = ms.usButtonFlags;
            if (_debugLog && flags != 0)
            {
                try
                {
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"),
                        _clock.ElapsedMilliseconds + " rawmouse flags=" + flags + "\r\n");
                }
                catch
                {
                }
            }
            KeyCell cell;
            if ((flags & 0x0001) != 0 && _byMouse.TryGetValue(1, out cell))
            {
                RegisterPress(cell, "raw");
            }
            if ((flags & 0x0004) != 0 && _byMouse.TryGetValue(2, out cell))
            {
                RegisterPress(cell, "raw");
            }
        }

        private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                try
                {
                    if (HandleKeyboard(wParam.ToInt32(), lParam))
                    {
                        return new IntPtr(1);
                    }
                }
                catch
                {
                    // 钩子回调绝不能抛异常
                }
            }
            return Native.CallNextHookEx(_kbHook, nCode, wParam, lParam);
        }

        private bool HandleKeyboard(int msg, IntPtr lParam)
        {
            if (msg != Native.WM_KEYDOWN && msg != Native.WM_SYSKEYDOWN &&
                msg != Native.WM_KEYUP && msg != Native.WM_SYSKEYUP)
            {
                return false;
            }

            Native.KBDLLHOOKSTRUCT info =
                (Native.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Native.KBDLLHOOKSTRUCT));
            int vk = (int)info.vkCode;
            bool down = msg == Native.WM_KEYDOWN || msg == Native.WM_SYSKEYDOWN;

            if (down)
            {
                if (vk == Native.VK_CONTROL || vk == Native.VK_MENU)
                {
                    return false;
                }
                if (Native.IsKeyDown(Native.VK_CONTROL) && Native.IsKeyDown(Native.VK_MENU) && HandleHotkey(vk))
                {
                    return true;
                }
            }

            KeyCell cell;
            if (_byVk.TryGetValue(vk, out cell))
            {
                _lastHookMs = _clock.ElapsedMilliseconds;
                if (down)
                {
                    if (!cell.Down)
                    {
                        cell.Down = true;
                        RegisterPress(cell, "hook");
                    }
                }
                else
                {
                    cell.Down = false;
                    _needsPaint = true;
                }
            }
            return false;
        }

        private bool HandleHotkey(int vk)
        {
            switch (vk)
            {
                case 0x4C: // L
                    ApplyLocked(!_locked, true);
                    return true;
                case 0x52: // R
                    ResetCounts();
                    return true;
                case 0x54: // T
                    CycleOpacity();
                    return true;
                case 0x51: // Q
                    Close();
                    return true;
            }
            return false;
        }

        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                try
                {
                    int msg = wParam.ToInt32();
                    if (msg == Native.WM_LBUTTONDOWN || msg == Native.WM_RBUTTONDOWN)
                    {
                        Native.MSLLHOOKSTRUCT info =
                            (Native.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Native.MSLLHOOKSTRUCT));

                        // 解锁状态下点在悬浮窗上：算作操作界面，不计数
                        if (!_locked && Visible && Bounds.Contains(info.x, info.y))
                        {
                            return Native.CallNextHookEx(_msHook, nCode, wParam, lParam);
                        }

                        KeyCell cell;
                        int button = msg == Native.WM_LBUTTONDOWN ? 1 : 2;
                        if (_byMouse.TryGetValue(button, out cell))
                        {
                            _lastHookMs = _clock.ElapsedMilliseconds;
                            RegisterPress(cell, "hook");
                        }
                    }
                }
                catch
                {
                }
            }
            return Native.CallNextHookEx(_msHook, nCode, wParam, lParam);
        }

        private void RegisterPress(KeyCell cell, string source)
        {
            long now = _clock.ElapsedMilliseconds;

            // 钩子和 Raw Input 可能同时上报同一次按键，20ms 内只算一次
            if (now - cell.LastPress < 20)
            {
                return;
            }
            cell.LastPress = now;

            if (_debugLog)
            {
                try
                {
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"),
                        now + " " + source + " " + cell.Id + "\r\n");
                }
                catch
                {
                }
            }

            cell.PrevCount = cell.Count;
            cell.Count++;
            _total++;
            cell.PressedUntil = now + 130;
            cell.PressStart = now;
            cell.RingStart = now;
            cell.PopStart = now;
            _pressTimes.Add(now);
            _dirty = true;
            _needsPaint = true;
        }

        // ---------- 计时与统计 ----------

        private void OnTick()
        {
            long now = _clock.ElapsedMilliseconds;

            // 顶层窗口是会被别的置顶窗口顶下去的（游戏、播放器都会），定期重新置顶一次
            if (_stayOnTop && _locked && now - _lastTopmost > 1200 && IsHandleCreated && !IsDisposed)
            {
                _lastTopmost = now;
                Native.SetWindowPos(Handle, Native.HWND_TOPMOST, 0, 0, 0, 0,
                    Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_NOOWNERZORDER);
            }

            _pressTimes.RemoveAll(delegate (long t) { return now - t > 1500; });

            int count = 0;
            for (int i = 0; i < _pressTimes.Count; i++)
            {
                if (now - _pressTimes[i] <= 1000)
                {
                    count++;
                }
            }

            double cps = count;
            if (cps > _peakCps)
            {
                _peakCps = cps;
                _dirty = true;
            }
            if (Math.Abs(cps - _cps) > 0.0001)
            {
                _cps = cps;
                _needsPaint = true;
            }

            if (_toast != null && now > _toastEnd + 340)
            {
                _toast = null;
                _needsPaint = true;
            }


            bool animating = false;
            for (int i = 0; i < _cells.Count; i++)
            {
                KeyCell c = _cells[i];
                if (c.Down || now < c.PressedUntil || now < c.RingStart + 430 || now < c.PopStart + 220)
                {
                    animating = true;
                    break;
                }
            }
            if (_toast != null)
            {
                animating = true;
            }

            // 动画播完的那一帧要补画一次最终状态
            if (_wasAnimating && !animating)
            {
                _needsPaint = true;
            }
            _wasAnimating = animating;

            if ((_needsPaint || animating) && now - _lastPaint >= 16)
            {
                Render(true);
            }

            if (_dirty && now - _lastSave > 2000)
            {
                SaveConfig();
            }
        }

        private void ResetCounts()
        {
            foreach (KeyCell c in _cells)
            {
                c.Count = 0;
                c.PrevCount = 0;
                c.Down = false;
                c.PressStart = 0;
                c.RingStart = 0;
                c.PopStart = 0;
            }
            _total = 0;
            _peakCps = 0;
            _cps = 0;
            _pressTimes.Clear();
            _dirty = true;
            _needsPaint = true;
            SaveConfig();
            ShowToast("计数已清零");
        }

        private void CycleOpacity()
        {
            double[] steps = new double[] { 0.35, 0.5, 0.62, 0.75, 0.9 };
            int current = -1;
            for (int i = 0; i < steps.Length; i++)
            {
                if (Math.Abs(steps[i] - _opacity) < 0.02)
                {
                    current = i;
                    break;
                }
            }
            _opacity = steps[(current + 1) % steps.Length];
            _dirty = true;
            _needsPaint = true;
            ShowToast("面板不透明度 " + (int)Math.Round(_opacity * 100) + "%");
        }

        // ---------- 交互 ----------

        private void ApplyLocked(bool locked, bool notify)
        {
            _locked = locked;

            int ex = Native.GetWindowLong(Handle, Native.GWL_EXSTYLE);
            if (locked)
            {
                ex |= Native.WS_EX_TRANSPARENT;
            }
            else
            {
                ex &= ~Native.WS_EX_TRANSPARENT;
            }
            Native.SetWindowLong(Handle, Native.GWL_EXSTYLE, ex);

            Cursor = locked ? Cursors.Default : Cursors.SizeAll;
            _needsPaint = true;

            if (locked && IsHandleCreated)
            {
                Native.SetWindowPos(Handle, Native.HWND_TOPMOST, 0, 0, 0, 0,
                    Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_NOOWNERZORDER);
            }

            if (notify)
            {
                ShowToast(locked
                    ? "已锁定：点击穿透，不抢游戏鼠标"
                    : "已解锁：拖动空白处移动，单击按键清零该键");
            }
        }

        private void ShowToast(string message)
        {
            long now = _clock.ElapsedMilliseconds;
            _toast = message;
            _toastStart = now;
            _toastEnd = now + 1900;
            _needsPaint = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_locked)
            {
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                KeyCell hit = HitTest(e.Location);
                if (hit != null)
                {
                    _clickCell = hit;
                }
                else
                {
                    _dragging = true;
                    _dragOrigin = Cursor.Position;
                    _dragLocation = Location;
                    Capture = true;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                Point p = Cursor.Position;
                Location = new Point(_dragLocation.X + p.X - _dragOrigin.X, _dragLocation.Y + p.Y - _dragOrigin.Y);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_locked)
            {
                return;
            }

            if (_dragging)
            {
                _dragging = false;
                Capture = false;
                SaveConfig();
                return;
            }

            if (e.Button == MouseButtons.Left && _clickCell != null)
            {
                KeyCell cell = _clickCell;
                _clickCell = null;
                cell.Count = 0;
                cell.PrevCount = 0;
                RecomputeTotal();
                _dirty = true;
                _needsPaint = true;
                SaveConfig();
                ShowToast("已清零 " + cell.Label);
            }
            else if (e.Button == MouseButtons.Right)
            {
                ShowMenu(e.Location);
            }
        }

        private void ShowMenu(Point at)
        {
            if (_menu == null)
            {
                _menu = new ContextMenuStrip();
                _menu.Items.Add("锁定（点击穿透）", null, delegate { ApplyLocked(true, true); });
                _menu.Items.Add("解锁（可拖动）", null, delegate { ApplyLocked(false, true); });
                _menu.Items.Add(new ToolStripSeparator());
                _menu.Items.Add("切换面板不透明度", null, delegate { CycleOpacity(); });
                _menu.Items.Add("重置全部计数", null, delegate { ResetCounts(); });
                _menu.Items.Add("重新居中", null, delegate
                {
                    Rectangle wa = Screen.FromPoint(Cursor.Position).WorkingArea;
                    Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + (wa.Height - Height) / 2);
                    SaveConfig();
                });
                _menu.Items.Add(new ToolStripSeparator());
                _menu.Items.Add("退出", null, delegate { Close(); });
            }
            _menu.Show(this, at);
        }

        private KeyCell HitTest(Point p)
        {
            foreach (KeyCell c in _cells)
            {
                if (c.Rect.Contains(p))
                {
                    return c;
                }
            }
            return null;
        }

        private void RecomputeTotal()
        {
            long total = 0;
            foreach (KeyCell c in _cells)
            {
                total += c.Count;
            }
            _total = total;
        }

        // ---------- 配置 ----------

        private static string ExeDirConfigPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName); }
        }

        private static string FallbackConfigPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "桌面键盘");
                return Path.Combine(dir, ConfigFileName);
            }
        }

        private void LoadConfig()
        {
            string primary = ExeDirConfigPath;
            string fallback = FallbackConfigPath;

            // 优先读 exe 同目录；读不到再读用户目录（应对 exe 所在目录不可写的情况）
            if (File.Exists(primary))
            {
                _configPath = primary;
            }
            else if (File.Exists(fallback))
            {
                _configPath = fallback;
            }
            else
            {
                _configPath = primary;
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                Location = new Point(wa.Right - Width - 40, wa.Top + 80);
                return;
            }

            string path = _configPath;
            int x = int.MinValue;
            int y = int.MinValue;
            double peak = 0;

            try
            {
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                    {
                        continue;
                    }
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();

                    if (string.Equals(key, "scale", StringComparison.OrdinalIgnoreCase))
                    {
                        double parsed;
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) && parsed >= 0.5 && parsed <= 3.0)
                        {
                            _userScale = (float)parsed;
                        }
                    }
                    else if (string.Equals(key, "x", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out x);
                    }
                    else if (string.Equals(key, "y", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out y);
                    }
                    else if (string.Equals(key, "locked", StringComparison.OrdinalIgnoreCase))
                    {
                        _locked = value != "0";
                    }
                    else if (string.Equals(key, "capture", StringComparison.OrdinalIgnoreCase))
                    {
                        string mode = value.Trim().ToLowerInvariant();
                        if (mode == "raw" || mode == "hook" || mode == "auto")
                        {
                            _captureMode = mode;
                        }
                    }
                    else if (string.Equals(key, "debug", StringComparison.OrdinalIgnoreCase))
                    {
                        _debugLog = value != "0";
                    }
                    else if (string.Equals(key, "stayontop", StringComparison.OrdinalIgnoreCase))
                    {
                        _stayOnTop = value != "0";
                    }
                    else if (string.Equals(key, "opacity", StringComparison.OrdinalIgnoreCase))
                    {
                        double parsed;
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) && parsed >= 0.2 && parsed <= 1.0)
                        {
                            _opacity = parsed;
                        }
                    }
                    else if (string.Equals(key, "peak", StringComparison.OrdinalIgnoreCase))
                    {
                        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out peak);
                    }
                    else if (key.StartsWith("count.", StringComparison.OrdinalIgnoreCase))
                    {
                        KeyCell cell = FindCell(key.Substring(6));
                        if (cell != null)
                        {
                            long parsed;
                            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed >= 0)
                            {
                                cell.Count = parsed;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            _peakCps = peak;
            RecomputeTotal();
            foreach (KeyCell c in _cells)
            {
                c.PrevCount = c.Count;
            }

            // 若缩放来自配置，需要重新排版
            if (Math.Abs(_userScale - 1f) > 0.0001f)
            {
                CreateFonts();
                BuildLayout();
            }

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            if (x != int.MinValue && y != int.MinValue)
            {
                Location = new Point(x, y);
            }
            else
            {
                Location = new Point(screen.Right - Width - 40, screen.Top + 80);
            }

            if (!Screen.GetWorkingArea(new Rectangle(Location, Size)).IntersectsWith(new Rectangle(Location, Size)))
            {
                Location = new Point(screen.Right - Width - 40, screen.Top + 80);
            }
        }

        private void SaveConfig()
        {
            _lastSave = _clock.ElapsedMilliseconds;
            string text = BuildConfigText();

            if (TryWriteConfig(_configPath, text))
            {
                _dirty = false;
                return;
            }

            // exe 目录不可写时，退到用户本地目录，保证计数不丢
            if (TryWriteConfig(FallbackConfigPath, text))
            {
                _configPath = FallbackConfigPath;
                _dirty = false;
                return;
            }

            if (!_saveWarned && Visible && !IsDisposed)
            {
                _saveWarned = true;
                ShowToast("计数保存失败：请检查程序目录权限");
            }
        }

        private string BuildConfigText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# 桌面键盘配置（可直接编辑，重启生效）");
            sb.AppendLine("# scale: 界面缩放 0.5 ~ 3.0");
            sb.AppendLine("# opacity: 面板不透明度 0.2 ~ 1.0（越低越透，能看到下面的画面）");
            sb.AppendLine("scale=" + _userScale.ToString("0.##", CultureInfo.InvariantCulture));
            sb.AppendLine("opacity=" + _opacity.ToString("0.##", CultureInfo.InvariantCulture));
            sb.AppendLine("x=" + Location.X.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("y=" + Location.Y.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("locked=" + (_locked ? "1" : "0"));
            sb.AppendLine("stayontop=" + (_stayOnTop ? "1" : "0"));
            sb.AppendLine("debug=" + (_debugLog ? "1" : "0"));
            sb.AppendLine("capture=" + _captureMode);
            sb.AppendLine("peak=" + _peakCps.ToString("0.0", CultureInfo.InvariantCulture));
            foreach (KeyCell c in _cells)
            {
                sb.AppendLine("count." + c.Id + "=" + c.Count.ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private static bool TryWriteConfig(string path, string text)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(path, text, new UTF8Encoding(true));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
