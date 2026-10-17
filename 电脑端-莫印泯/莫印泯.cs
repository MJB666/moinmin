using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using SD = System.Drawing;
using Path = System.IO.Path;

[assembly: System.Reflection.AssemblyTitle("莫印泯")]
[assembly: System.Reflection.AssemblyProduct("莫印泯")]
[assembly: System.Reflection.AssemblyDescription("一键直达官方 App / 商品页")]
[assembly: System.Reflection.AssemblyFileVersion("2.0.0.0")]
[assembly: System.Reflection.AssemblyVersion("2.0.0.0")]

namespace MoYinMin
{
    // ==================================================================
    //  数据
    // ==================================================================
    public class Shortcut
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Note { get; set; }
        public string Fallback { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? "(未命名)" : Name;
        }
    }

    public class AppSettings
    {
        public bool? Video { get; set; }
        public bool? Ball { get; set; }
        public bool? ImageAuto { get; set; }
        public string VideoPath { get; set; }
        public string ImagePath { get; set; }
    }

    public static class Cfg
    {
        public static string AppDir()
        {
            try
            {
                string l = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(l)) return Path.GetDirectoryName(l);
            }
            catch { }
            return Environment.CurrentDirectory;
        }

        public static string DataDir()
        {
            string d = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "莫印泯");
            if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            return d;
        }

        public static string LinksPath() { return Path.Combine(DataDir(), "links.json"); }
        public static string SettingsPath() { return Path.Combine(DataDir(), "settings.json"); }

        // ---------------- 卡片数据 ----------------
        public static List<Shortcut> LoadLinks()
        {
            try
            {
                if (File.Exists(LinksPath()))
                {
                    string json = File.ReadAllText(LinksPath(), Encoding.UTF8);
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<Shortcut> list = js.Deserialize<List<Shortcut>>(json);
                    if (list != null && list.Count > 0) return list;
                }
            }
            catch { }
            return Defaults();
        }

        public static void SaveLinks(List<Shortcut> items)
        {
            JavaScriptSerializer js = new JavaScriptSerializer();
            File.WriteAllText(LinksPath(), js.Serialize(items), new UTF8Encoding(true));
        }

        public static List<Shortcut> Defaults()
        {
            List<Shortcut> l = new List<Shortcut>();
            l.Add(New("淘宝", "https://www.taobao.com", "购物 · 官方网页", ""));
            l.Add(New("天猫", "https://www.tmall.com", "品牌旗舰店", ""));
            l.Add(New("京东", "https://www.jd.com", "自营 · 秒杀", ""));
            l.Add(New("拼多多", "https://mobile.yangkeduo.com", "百亿补贴", ""));
            l.Add(New("大麦网", "https://www.damai.cn", "演唱会 · 演出票", ""));
            l.Add(New("猫眼演出", "https://www.maoyan.com", "电影 · 演出票", ""));
            l.Add(New("美团", "https://www.meituan.com", "到店 · 门票", ""));
            l.Add(New("哔哩哔哩会员购", "https://show.bilibili.com", "B站演出票", ""));
            l.Add(New("苏宁易购", "https://www.suning.com", "家电 · 数码", ""));
            l.Add(New("唯品会", "https://www.vip.com", "品牌特卖", ""));
            l.Add(New("得物", "https://www.dewu.com", "潮品 · 球鞋", ""));
            l.Add(New("小红书", "https://www.xiaohongshu.com", "种草 · 笔记", ""));
            return l;
        }

        static Shortcut New(string n, string u, string note, string fb)
        {
            Shortcut s = new Shortcut();
            s.Name = n; s.Url = u; s.Note = note; s.Fallback = fb;
            return s;
        }

        // ---------------- 设置 ----------------
        public static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath()))
                {
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    AppSettings s = js.Deserialize<AppSettings>(File.ReadAllText(SettingsPath(), Encoding.UTF8));
                    if (s != null) return s;
                }
            }
            catch { }
            return new AppSettings();
        }

        public static void SaveSettings(AppSettings s)
        {
            try
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                File.WriteAllText(SettingsPath(), js.Serialize(s), new UTF8Encoding(true));
            }
            catch { }
        }

        static bool Has(string ext, string[] exts)
        {
            foreach (string e in exts) if (e == ext) return true;
            return false;
        }

        static readonly string[] VIDEO_EXT = new string[] { ".mp4", ".webm", ".wmv", ".avi", ".mkv", ".mov", ".m4v" };
        static readonly string[] IMAGE_EXT = new string[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif" };

        // 找素材的文件夹（按优先级）
        public static string[] MediaDirs()
        {
            return new string[] {
                Path.Combine(AppDir(), "莫印泯"),
                Path.Combine(AppDir(), "图片"),
                Path.Combine(AppDir(), "images"),
                Path.Combine(AppDir(), "壁纸"),
                AppDir(),
                Path.Combine(DataDir(), "图片"),
                DataDir()
            };
        }

        // ---------------- 背景视频 ----------------
        // 优先用「约定名字」；否则挑体积最小的那个视频（体积小 = 播放更流畅）
        public static string FindVideo()
        {
            string[] names = new string[] {
                "bg.mp4", "bg.webm", "bg.wmv", "bg.avi", "bg.mkv", "bg.mov",
                "background.mp4", "背景.mp4", "背景视频.mp4", "背景.webm"
            };

            foreach (string d in MediaDirs())
                foreach (string n in names)
                {
                    try
                    {
                        string p = Path.Combine(d, n);
                        if (File.Exists(p)) return p;
                    }
                    catch { }
                }

            string best = null;
            long bestLen = long.MaxValue;
            foreach (string d in MediaDirs())
            {
                try
                {
                    if (!Directory.Exists(d)) continue;
                    foreach (string f in Directory.GetFiles(d))
                    {
                        if (!Has(Path.GetExtension(f).ToLower(), VIDEO_EXT)) continue;
                        long len = new FileInfo(f).Length;
                        if (len < bestLen) { bestLen = len; best = f; }
                    }
                }
                catch { }
            }
            return best;
        }

        public static List<string> FindVideos()
        {
            List<string> res = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string d in MediaDirs())
            {
                try
                {
                    if (!Directory.Exists(d)) continue;
                    string[] fs = Directory.GetFiles(d);
                    Array.Sort(fs);
                    foreach (string f in fs)
                    {
                        if (!Has(Path.GetExtension(f).ToLower(), VIDEO_EXT)) continue;
                        if (seen.Add(f)) res.Add(f);
                    }
                }
                catch { }
            }
            return res;
        }

        // ---------------- 背景图片 ----------------
        public static bool IsImageFile(string f)
        {
            if (!Has(Path.GetExtension(f).ToLower(), IMAGE_EXT)) return false;
            if (Path.GetFileName(f).ToLower() == "app.ico") return false;
            return true;
        }

        // 太小的图拉满整屏会糊，直接跳过
        public static bool BigEnough(string path, int min)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    BitmapFrame fr = BitmapFrame.Create(fs,
                        BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    return fr.PixelWidth >= min && fr.PixelHeight >= min;
                }
            }
            catch { return false; }
        }

        public static List<string> FindImages()
        {
            List<string> res = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string d in MediaDirs())
            {
                try
                {
                    if (!Directory.Exists(d)) continue;
                    string[] fs = Directory.GetFiles(d);
                    Array.Sort(fs);
                    foreach (string f in fs)
                    {
                        if (!IsImageFile(f)) continue;
                        if (!seen.Add(f)) continue;
                        if (!BigEnough(f, 400)) continue;
                        res.Add(f);
                    }
                }
                catch { }
            }
            return res;
        }

        // ---------------- 应用图标 ----------------
        public static string FindIco()
        {
            string[] cands = new string[] {
                Path.Combine(AppDir(), "app.ico"),
                Path.Combine(AppDir(), "莫印泯", "app.ico"),
                Path.Combine(DataDir(), "app.ico"),
                Path.Combine(Environment.CurrentDirectory, "app.ico")
            };
            foreach (string c in cands)
            {
                try { if (File.Exists(c)) return c; }
                catch { }
            }
            return null;
        }
    }

    // ==================================================================
    //  打开 / 工具
    // ==================================================================
    public static class Opener
    {
        public static bool Open(string target)
        {
            if (string.IsNullOrEmpty(target)) return false;
            target = target.Trim();
            if (target.Length == 0) return false;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(target);
                psi.UseShellExecute = true;
                Process.Start(psi);
                return true;
            }
            catch { return false; }
        }

        public static string Host(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            try
            {
                Uri u = new Uri(url);
                string h = u.Host;
                if (h.StartsWith("www.")) h = h.Substring(4);
                if (h.Length > 30) h = h.Substring(0, 30) + "…";
                return h;
            }
            catch
            {
                if (url.Length > 30) return url.Substring(0, 30) + "…";
                return url;
            }
        }

        public static bool LooksLikeUrl(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            s = s.Trim();
            if (s.Length < 5) return false;
            if (s.Contains(" ") || s.Contains("\n") || s.Contains("\r")) return false;
            if (s.StartsWith("http://") || s.StartsWith("https://")) return true;
            if (s.IndexOf("://") > 0) return true;
            if (s.Length > 3 && s[1] == ':' && (s[2] == '\\' || s[2] == '/')) return true;
            return false;
        }

        public static string ReadClipboard()
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (Clipboard.ContainsText()) return Clipboard.GetText().Trim();
                    return "";
                }
                catch { System.Threading.Thread.Sleep(120); }
            }
            return "";
        }
    }

    // ==================================================================
    //  视觉
    // ==================================================================
    public static class Ui
    {
        public static readonly FontFamily YaHei = new FontFamily("Microsoft YaHei");

        public static Color Hsl(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = l - c / 2;
            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }
            return Color.FromRgb(
                (byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255),
                (byte)Math.Round((b + m) * 255));
        }

        public static Color Mix(Color a, Color b, double t)
        {
            return Color.FromRgb(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t));
        }

        public static Color Accent(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "?";
            int h = 0;
            for (int i = 0; i < key.Length; i++) h = (h * 31 + key[i]) % 360;
            return Hsl(h, 0.66, 0.58);
        }

        public static SolidColorBrush Brush(Color c)
        {
            SolidColorBrush b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        public static SolidColorBrush White(byte a) { return Brush(Color.FromArgb(a, 255, 255, 255)); }

        public static DropShadowEffect Shadow(double blur, double depth, double opacity)
        {
            DropShadowEffect d = new DropShadowEffect();
            d.BlurRadius = blur;
            d.ShadowDepth = depth;
            d.Direction = 270;
            d.Opacity = opacity;
            d.Color = Colors.Black;
            return d;
        }

        public static TextBlock Text(string s, double size, Brush color)
        {
            TextBlock t = new TextBlock();
            t.Text = s;
            t.FontSize = size;
            t.Foreground = color;
            t.FontFamily = YaHei;
            t.TextTrimming = TextTrimming.CharacterEllipsis;
            return t;
        }

        public static string Initial(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            return name.Substring(0, 1).ToUpper();
        }

        // 玻璃小球：图标底色 + 左上反光 + 细白边 + 首字
        public static Grid Badge(string name, double size)
        {
            Grid g = new Grid();
            g.Width = size;
            g.Height = size;
            Color ac = Accent(name);

            Ellipse body = new Ellipse();
            LinearGradientBrush lg = new LinearGradientBrush();
            lg.StartPoint = new Point(0.15, 0);
            lg.EndPoint = new Point(0.85, 1);
            lg.GradientStops.Add(new GradientStop(Mix(ac, Colors.White, 0.38), 0));
            lg.GradientStops.Add(new GradientStop(ac, 1));
            body.Fill = lg;
            g.Children.Add(body);

            Ellipse gloss = new Ellipse();
            RadialGradientBrush rg = new RadialGradientBrush();
            rg.GradientOrigin = new Point(0.32, 0.24);
            rg.Center = new Point(0.32, 0.24);
            rg.RadiusX = 0.74;
            rg.RadiusY = 0.74;
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(200, 255, 255, 255), 0));
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1));
            gloss.Fill = rg;
            gloss.IsHitTestVisible = false;
            g.Children.Add(gloss);

            Ellipse ring = new Ellipse();
            ring.Stroke = White(120);
            ring.StrokeThickness = Math.Max(1.0, size / 40.0);
            ring.IsHitTestVisible = false;
            g.Children.Add(ring);

            TextBlock t = Text(Initial(name), size * 0.42, Brushes.White);
            t.FontWeight = FontWeights.Bold;
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;
            t.IsHitTestVisible = false;
            g.Children.Add(t);

            return g;
        }

        // 玻璃药丸按钮
        public static Border Pill(string text, Action click, bool primary, out TextBlock label)
        {
            Border b = new Border();
            b.CornerRadius = new CornerRadius(17);
            b.Padding = new Thickness(15, 8, 15, 8);
            b.Margin = new Thickness(5, 0, 5, 0);
            b.Cursor = Cursors.Hand;
            b.BorderThickness = new Thickness(1);
            b.BorderBrush = White(90);

            if (primary)
            {
                LinearGradientBrush lg = new LinearGradientBrush();
                lg.StartPoint = new Point(0, 0);
                lg.EndPoint = new Point(1, 1);
                lg.GradientStops.Add(new GradientStop(Color.FromRgb(0x5B, 0x7C, 0xFF), 0));
                lg.GradientStops.Add(new GradientStop(Color.FromRgb(0x8B, 0x5C, 0xF6), 1));
                b.Background = lg;
            }
            else
            {
                b.Background = White(46);
            }

            TextBlock t = Text(text, 12.5, Brushes.White);
            t.TextTrimming = TextTrimming.None;
            b.Child = t;
            label = t;

            // 标记：父级对话框不要把这上面的点击当成「拖动窗口」
            b.Tag = "nograb";

            b.MouseEnter += delegate { b.Opacity = 0.84; };
            b.MouseLeave += delegate { b.Opacity = 1.0; };
            b.MouseLeftButtonUp += delegate { if (click != null) click(); };
            return b;
        }

        public static Border Pill(string text, Action click, bool primary)
        {
            TextBlock ignored;
            return Pill(text, click, primary, out ignored);
        }

        // 玻璃面板
        public static Border GlassPanel(double radius, byte bg, byte border)
        {
            Border b = new Border();
            b.CornerRadius = new CornerRadius(radius);
            b.Background = White(bg);
            b.BorderBrush = White(border);
            b.BorderThickness = new Thickness(1);
            return b;
        }

        // 顺着可视树往上找：这次点击是不是落在「可交互控件」上
        public static bool HitInteractive(DependencyObject d, DependencyObject stopAt)
        {
            while (d != null && d != stopAt)
            {
                if (d is TextBoxBase || d is ButtonBase || d is ComboBox || d is ComboBoxItem ||
                    d is ScrollBar || d is Thumb || d is MenuItem || d is ListBox ||
                    d is ListBoxItem || d is CheckBox || d is RadioButton || d is Slider ||
                    d is DatePicker || d is PasswordBox)
                {
                    return true;
                }
                FrameworkElement fe = d as FrameworkElement;
                if (fe != null && fe.Tag is string && ((string)fe.Tag) == "nograb") return true;

                d = VisualTreeHelper.GetParent(d);
            }
            return false;
        }

        public static void ScaleTo(ScaleTransform st, double to, int ms)
        {
            DoubleAnimation a = new DoubleAnimation(to, TimeSpan.FromMilliseconds(ms));
            CubicEase e = new CubicEase();
            e.EasingMode = EasingMode.EaseOut;
            a.EasingFunction = e;
            st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
        }

        public static void AnimXY(TranslateTransform tt, double x1, double x2, double y1, double y2, double sec)
        {
            SineEase e = new SineEase();
            e.EasingMode = EasingMode.EaseInOut;

            DoubleAnimation ax = new DoubleAnimation(x1, x2, TimeSpan.FromSeconds(sec));
            ax.AutoReverse = true;
            ax.RepeatBehavior = RepeatBehavior.Forever;
            ax.EasingFunction = e;
            tt.BeginAnimation(TranslateTransform.XProperty, ax);

            DoubleAnimation ay = new DoubleAnimation(y1, y2, TimeSpan.FromSeconds(sec * 1.3));
            ay.AutoReverse = true;
            ay.RepeatBehavior = RepeatBehavior.Forever;
            ay.EasingFunction = e;
            tt.BeginAnimation(TranslateTransform.YProperty, ay);
        }

        // 动态渐变背景（没有视频时的兜底）
        public static Grid Backdrop()
        {
            Grid g = new Grid();
            g.ClipToBounds = true;

            LinearGradientBrush baseBrush = new LinearGradientBrush();
            baseBrush.StartPoint = new Point(0, 0);
            baseBrush.EndPoint = new Point(1, 1);
            baseBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x0C, 0x10, 0x20), 0));
            baseBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x1A, 0x16, 0x36), 0.5));
            baseBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x08, 0x1C, 0x2C), 1));
            g.Background = baseBrush;

            AddBlob(g, Color.FromRgb(0x6C, 0x4C, 0xF1), HorizontalAlignment.Left, VerticalAlignment.Top,
                new Thickness(-180, -180, 0, 0), 620, -70, 240, -50, 200, 22);
            AddBlob(g, Color.FromRgb(0x2F, 0x7B, 0xF6), HorizontalAlignment.Right, VerticalAlignment.Bottom,
                new Thickness(0, 0, -200, -180), 700, 60, -220, 40, -180, 27);
            AddBlob(g, Color.FromRgb(0x12, 0xB5, 0xA8), HorizontalAlignment.Center, VerticalAlignment.Bottom,
                new Thickness(0, 0, 0, -260), 520, -140, 120, -110, 90, 31);

            return g;
        }

        static void AddBlob(Grid parent, Color c, HorizontalAlignment ha, VerticalAlignment va,
            Thickness margin, double size, double x1, double x2, double y1, double y2, double sec)
        {
            Ellipse e = new Ellipse();
            e.Width = size;
            e.Height = size;
            e.HorizontalAlignment = ha;
            e.VerticalAlignment = va;
            e.Margin = margin;

            RadialGradientBrush rg = new RadialGradientBrush();
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(165, c.R, c.G, c.B), 0));
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(70, c.R, c.G, c.B), 0.45));
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(0, c.R, c.G, c.B), 1));
            e.Fill = rg;

            TranslateTransform tt = new TranslateTransform();
            e.RenderTransform = tt;
            e.IsHitTestVisible = false;

            parent.Children.Add(e);
            AnimXY(tt, x1, x2, y1, y2, sec);
        }
    }

    // ==================================================================
    //  卡片
    // ==================================================================
    public static class Cards
    {
        public static Border Make(Shortcut sc, Action<Shortcut> open, Action<Shortcut> edit, Action<Shortcut> del)
        {
            Color ac = Ui.Accent(sc.Name);

            Border card = new Border();
            card.Height = 110;
            card.Margin = new Thickness(8);
            card.CornerRadius = new CornerRadius(18);
            card.Background = Ui.White(44);
            card.BorderBrush = Ui.White(72);
            card.BorderThickness = new Thickness(1);
            card.Effect = Ui.Shadow(20, 6, 0.30);
            card.Cursor = Cursors.Hand;
            card.RenderTransformOrigin = new Point(0.5, 0.5);
            ScaleTransform st = new ScaleTransform(1, 1);
            card.RenderTransform = st;

            Grid inner = new Grid();
            inner.ColumnDefinitions.Add(new ColumnDefinition());
            inner.ColumnDefinitions.Add(new ColumnDefinition());
            inner.ColumnDefinitions[0].Width = new GridLength(70);
            inner.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

            Grid badge = Ui.Badge(sc.Name, 50);
            badge.HorizontalAlignment = HorizontalAlignment.Center;
            badge.VerticalAlignment = VerticalAlignment.Center;
            badge.IsHitTestVisible = false;
            inner.Children.Add(badge);

            StackPanel sp = new StackPanel();
            sp.VerticalAlignment = VerticalAlignment.Center;
            sp.Margin = new Thickness(0, 0, 14, 0);
            sp.IsHitTestVisible = false;

            TextBlock nm = Ui.Text(sc.Name, 15.5, Brushes.White);
            nm.FontWeight = FontWeights.SemiBold;
            sp.Children.Add(nm);

            TextBlock nt = Ui.Text(sc.Note == null ? "" : sc.Note, 11,
                new SolidColorBrush(Color.FromArgb(178, 255, 255, 255)));
            nt.Margin = new Thickness(0, 4, 0, 0);
            sp.Children.Add(nt);

            TextBlock hs = Ui.Text(Opener.Host(sc.Url), 11,
                new SolidColorBrush(Ui.Mix(ac, Colors.White, 0.38)));
            hs.Margin = new Thickness(0, 4, 0, 0);
            sp.Children.Add(hs);

            Grid.SetColumn(sp, 1);
            inner.Children.Add(sp);

            // 反光罩（玻璃质感）
            Border sheen = new Border();
            sheen.CornerRadius = new CornerRadius(18);
            sheen.IsHitTestVisible = false;
            LinearGradientBrush sg = new LinearGradientBrush();
            sg.StartPoint = new Point(0, 0);
            sg.EndPoint = new Point(0.45, 1);
            sg.GradientStops.Add(new GradientStop(Color.FromArgb(56, 255, 255, 255), 0));
            sg.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.62));
            sg.GradientStops.Add(new GradientStop(Color.FromArgb(18, 255, 255, 255), 1));
            sheen.Background = sg;
            inner.Children.Add(sheen);

            card.Child = inner;

            card.MouseEnter += delegate
            {
                card.Background = Ui.White(78);
                DropShadowEffect d = card.Effect as DropShadowEffect;
                if (d != null) { d.BlurRadius = 34; d.Opacity = 0.52; }
                Ui.ScaleTo(st, 1.04, 140);
            };
            card.MouseLeave += delegate
            {
                card.Background = Ui.White(44);
                DropShadowEffect d = card.Effect as DropShadowEffect;
                if (d != null) { d.BlurRadius = 20; d.Opacity = 0.30; }
                Ui.ScaleTo(st, 1.0, 170);
            };
            card.MouseLeftButtonUp += delegate { if (open != null) open(sc); };

            ContextMenu cm = new ContextMenu();
            cm.FontFamily = Ui.YaHei;

            MenuItem mi1 = new MenuItem();
            mi1.Header = "打开";
            mi1.Click += delegate { if (open != null) open(sc); };
            cm.Items.Add(mi1);

            if (!string.IsNullOrEmpty(sc.Fallback))
            {
                MenuItem mi2 = new MenuItem();
                mi2.Header = "打开备用链接";
                mi2.Click += delegate { Opener.Open(sc.Fallback); };
                cm.Items.Add(mi2);
            }

            cm.Items.Add(new Separator());

            MenuItem mi3 = new MenuItem();
            mi3.Header = "编辑";
            mi3.Click += delegate { if (edit != null) edit(sc); };
            cm.Items.Add(mi3);

            MenuItem mi4 = new MenuItem();
            mi4.Header = "删除";
            mi4.Click += delegate { if (del != null) del(sc); };
            cm.Items.Add(mi4);

            card.ContextMenu = cm;
            return card;
        }

        public static bool OpenWithFallback(Shortcut s, Window owner)
        {
            if (Opener.Open(s.Url)) return true;
            if (!string.IsNullOrEmpty(s.Fallback) && Opener.Open(s.Fallback)) return true;
            MessageBox.Show(owner,
                "打开失败：\n" + s.Url + "\n\n可能原因：\n· 该 App 未安装，或没有注册对应的协议\n· 链接填错了",
                "打不开", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    // ==================================================================
    //  玻璃对话框外框
    // ==================================================================
    public static class Shell
    {
        public static Grid Frame(Window w, double width, double height, bool centerScreen)
        {
            w.WindowStyle = WindowStyle.None;
            w.AllowsTransparency = true;
            w.Background = Brushes.Transparent;
            w.ResizeMode = ResizeMode.NoResize;
            w.ShowInTaskbar = false;
            w.Width = width;
            w.Height = height;
            w.WindowStartupLocation = centerScreen
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner;

            Border b = new Border();
            b.CornerRadius = new CornerRadius(20);
            b.Background = new SolidColorBrush(Color.FromArgb(246, 20, 24, 38));
            b.BorderBrush = Ui.White(96);
            b.BorderThickness = new Thickness(1);
            b.Effect = Ui.Shadow(30, 10, 0.55);
            // 拖动窗口时直接用位图缓存，不再逐帧重新栅格化 —— 这是「拖动卡顿」的解药
            b.CacheMode = new BitmapCache(1.0);

            Grid inner = new Grid();
            inner.Margin = new Thickness(22);
            b.Child = inner;
            w.Content = b;

            // 只有按在「空白处 / 文字」上才拖窗口；
            // 按在按钮、输入框、下拉框上时，把事件让给控件本身（否则按钮点不动）
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ChangedButton != MouseButton.Left) return;
                if (Ui.HitInteractive(e.OriginalSource as DependencyObject, b)) return;
                try { w.DragMove(); } catch { }
            };

            return inner;
        }
    }

    // ==================================================================
    //  添加 / 编辑 对话框
    // ==================================================================
    public class EditDialog : Window
    {
        public Shortcut Result;
        TextBox txtName, txtUrl, txtNote, txtFallback;

        public EditDialog(Shortcut src, string presetUrl)
        {
            this.Title = src == null ? "添加" : "编辑";

            Grid g = Shell.Frame(this, 470, 430, false);
            g.RowDefinitions.Add(new RowDefinition());
            g.RowDefinitions.Add(new RowDefinition());

            StackPanel sp = new StackPanel();

            TextBlock title = Ui.Text(src == null ? "添加一个入口" : "编辑入口", 19, Brushes.White);
            title.FontWeight = FontWeights.Bold;
            title.Margin = new Thickness(0, 0, 0, 16);
            sp.Children.Add(title);

            txtName = Field(sp, "名称", src == null ? "" : src.Name, "例如：大麦网 / 我常买的那家店");
            txtUrl = Field(sp, "链接", src != null ? src.Url : (presetUrl == null ? "" : presetUrl),
                "支持：网址 https://… / App 协议 taobao:// / 本机程序 C:\\…\\xx.exe");
            txtNote = Field(sp, "备注", src == null ? "" : src.Note, "一句话说明（可留空）");
            txtFallback = Field(sp, "备用链接", src == null ? "" : src.Fallback,
                "主链接打不开时自动改用它（可留空）");

            Grid.SetRow(sp, 0);
            g.Children.Add(sp);

            StackPanel btns = new StackPanel();
            btns.Orientation = Orientation.Horizontal;
            btns.HorizontalAlignment = HorizontalAlignment.Right;
            btns.VerticalAlignment = VerticalAlignment.Bottom;
            Grid.SetRow(btns, 1);

            Border ok = Ui.Pill("保存", delegate { Confirm(); }, true);
            Border cancel = Ui.Pill("取消", delegate { DialogResult = false; Close(); }, false);
            btns.Children.Add(cancel);
            btns.Children.Add(ok);
            g.Children.Add(btns);
        }

        TextBox Field(StackPanel sp, string label, string value, string hint)
        {
            TextBlock l = Ui.Text(label, 12.5, new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)));
            l.FontWeight = FontWeights.SemiBold;
            l.TextTrimming = TextTrimming.None;
            l.Margin = new Thickness(2, 8, 0, 4);
            sp.Children.Add(l);

            TextBox t = new TextBox();
            t.Text = value;
            t.FontSize = 13;
            t.FontFamily = Ui.YaHei;
            t.Padding = new Thickness(10, 8, 10, 8);
            t.BorderThickness = new Thickness(1);
            t.BorderBrush = Ui.White(70);
            t.Background = Ui.White(28);
            t.Foreground = Brushes.White;
            t.CaretBrush = Brushes.White;
            t.SelectionBrush = new SolidColorBrush(Color.FromArgb(140, 91, 124, 255));
            sp.Children.Add(t);

            TextBlock h = Ui.Text(hint, 10.5, new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)));
            h.TextTrimming = TextTrimming.None;
            h.Margin = new Thickness(2, 4, 0, 0);
            sp.Children.Add(h);

            return t;
        }

        void Confirm()
        {
            if (txtName.Text.Trim().Length == 0) { MessageBox.Show(this, "请填写名称", "提示"); return; }
            if (txtUrl.Text.Trim().Length == 0) { MessageBox.Show(this, "请填写链接", "提示"); return; }
            Shortcut s = new Shortcut();
            s.Name = txtName.Text.Trim();
            s.Url = txtUrl.Text.Trim();
            s.Note = txtNote.Text.Trim();
            s.Fallback = txtFallback.Text.Trim();
            Result = s;
            DialogResult = true;
            Close();
        }
    }

    // ==================================================================
    //  到点提醒
    // ==================================================================
    public class ReminderWindow : Window
    {
        DispatcherTimer bell;

        public ReminderWindow(Shortcut target, string timeText)
        {
            this.Title = "时间到！";
            this.Topmost = true;

            Grid g = Shell.Frame(this, 460, 268, true);
            g.RowDefinitions.Add(new RowDefinition());
            g.RowDefinitions.Add(new RowDefinition());

            StackPanel sp = new StackPanel();

            TextBlock big = Ui.Text("⏰  时间到！", 30, new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)));
            big.FontWeight = FontWeights.Bold;
            big.TextTrimming = TextTrimming.None;
            sp.Children.Add(big);

            TextBlock tm = Ui.Text(timeText, 11.5, new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)));
            tm.TextTrimming = TextTrimming.None;
            tm.Margin = new Thickness(2, 6, 0, 0);
            sp.Children.Add(tm);

            TextBlock tn = Ui.Text(target == null ? "请在提醒后手动前往" : ("目标：" + target.Name),
                16, Brushes.White);
            tn.FontWeight = FontWeights.SemiBold;
            tn.TextTrimming = TextTrimming.None;
            tn.Margin = new Thickness(2, 14, 0, 0);
            sp.Children.Add(tn);

            Grid.SetRow(sp, 0);
            g.Children.Add(sp);

            StackPanel btns = new StackPanel();
            btns.Orientation = Orientation.Horizontal;
            btns.HorizontalAlignment = HorizontalAlignment.Right;
            btns.VerticalAlignment = VerticalAlignment.Bottom;
            Grid.SetRow(btns, 1);

            if (target != null)
            {
                Border go = Ui.Pill("🚀  立即前往", delegate
                {
                    if (!Cards.OpenWithFallback(target, this)) { }
                }, true);
                btns.Children.Add(go);
            }

            Border ok = Ui.Pill("知道了", delegate { Close(); }, false);
            btns.Children.Add(ok);
            g.Children.Add(btns);

            bell = new DispatcherTimer();
            bell.Interval = TimeSpan.FromMilliseconds(1150);
            bell.Tick += delegate
            {
                try { System.Media.SystemSounds.Exclamation.Play(); }
                catch { }
            };
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
            bell.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            bell.Stop();
            base.OnClosed(e);
        }
    }

    // ==================================================================
    //  悬浮球
    // ==================================================================
    public class BallPopup : Window
    {
        public event Action AddFromClipboard;
        StackPanel list;
        MainWindow owner;
        public DateTime LastHidden = DateTime.MinValue;

        public BallPopup(MainWindow mw)
        {
            owner = mw;
            this.Title = "莫印泯";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.SizeToContent = SizeToContent.WidthAndHeight;

            Border b = new Border();
            b.CornerRadius = new CornerRadius(20);
            b.Background = new SolidColorBrush(Color.FromArgb(242, 18, 22, 36));
            b.BorderBrush = Ui.White(96);
            b.BorderThickness = new Thickness(1);
            b.Effect = Ui.Shadow(30, 10, 0.55);
            b.Padding = new Thickness(14);
            this.Content = b;

            StackPanel root = new StackPanel();

            // 顶部：标题 + 存剪贴板
            Grid head = new Grid();
            head.ColumnDefinitions.Add(new ColumnDefinition());
            head.ColumnDefinitions.Add(new ColumnDefinition());
            head.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            head.ColumnDefinitions[1].Width = GridLength.Auto;

            TextBlock t = Ui.Text("一键直达", 14, Brushes.White);
            t.FontWeight = FontWeights.Bold;
            t.VerticalAlignment = VerticalAlignment.Center;
            t.TextTrimming = TextTrimming.None;
            head.Children.Add(t);

            Border addBtn = Ui.Pill("＋ 存剪贴板链接", delegate
            {
                if (AddFromClipboard != null) AddFromClipboard();
            }, true);
            addBtn.Margin = new Thickness(0);
            Grid.SetColumn(addBtn, 1);
            head.Children.Add(addBtn);

            root.Children.Add(head);

            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.MaxHeight = 420;
            sv.Margin = new Thickness(0, 12, 0, 0);

            list = new StackPanel();
            sv.Content = list;
            root.Children.Add(sv);

            b.Child = root;

            this.Deactivated += delegate { LastHidden = DateTime.Now; this.Hide(); };
        }

        public void Fill(List<Shortcut> items)
        {
            list.Children.Clear();
            foreach (Shortcut sc in items)
            {
                Shortcut s = sc;
                Border row = new Border();
                row.CornerRadius = new CornerRadius(14);
                row.Background = Ui.White(34);
                row.BorderBrush = Ui.White(56);
                row.BorderThickness = new Thickness(1);
                row.Padding = new Thickness(10, 8, 12, 8);
                row.Margin = new Thickness(0, 0, 0, 7);
                row.Cursor = Cursors.Hand;
                row.Width = 268;

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition());
                g.ColumnDefinitions.Add(new ColumnDefinition());
                g.ColumnDefinitions[0].Width = new GridLength(40);
                g.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

                Grid bd = Ui.Badge(s.Name, 30);
                bd.VerticalAlignment = VerticalAlignment.Center;
                bd.IsHitTestVisible = false;
                g.Children.Add(bd);

                StackPanel sp = new StackPanel();
                sp.VerticalAlignment = VerticalAlignment.Center;
                sp.IsHitTestVisible = false;
                TextBlock nm = Ui.Text(s.Name, 12.5, Brushes.White);
                sp.Children.Add(nm);
                TextBlock hs = Ui.Text(Opener.Host(s.Url), 10.5,
                    new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)));
                sp.Children.Add(hs);
                Grid.SetColumn(sp, 1);
                g.Children.Add(sp);

                row.Child = g;

                row.MouseEnter += delegate { row.Background = Ui.White(64); };
                row.MouseLeave += delegate { row.Background = Ui.White(34); };
                row.MouseLeftButtonUp += delegate
                {
                    this.Hide();
                    Cards.OpenWithFallback(s, owner);
                };

                list.Children.Add(row);
            }
        }
    }

    public class BallWindow : Window
    {
        MainWindow owner;
        BallPopup popup;
        bool pressed;
        bool dragged;
        Point downScreen;
        double startLeft, startTop;

        public BallWindow(MainWindow mw)
        {
            owner = mw;
            this.Title = "莫印泯";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.Width = 78;
            this.Height = 78;
            this.Cursor = Cursors.Hand;

            Grid g = new Grid();

            Ellipse glow = new Ellipse();
            RadialGradientBrush gr = new RadialGradientBrush();
            gr.GradientStops.Add(new GradientStop(Color.FromArgb(120, 120, 150, 255), 0));
            gr.GradientStops.Add(new GradientStop(Color.FromArgb(0, 120, 150, 255), 1));
            glow.Fill = gr;
            glow.Margin = new Thickness(-8);
            glow.IsHitTestVisible = false;
            g.Children.Add(glow);

            Ellipse body = new Ellipse();
            body.Margin = new Thickness(6);
            LinearGradientBrush lg = new LinearGradientBrush();
            lg.StartPoint = new Point(0.15, 0);
            lg.EndPoint = new Point(0.85, 1);
            lg.GradientStops.Add(new GradientStop(Color.FromArgb(235, 0x5B, 0x7C, 0xFF), 0));
            lg.GradientStops.Add(new GradientStop(Color.FromArgb(245, 0x7B, 0x4C, 0xE8), 1));
            body.Fill = lg;
            body.Effect = Ui.Shadow(16, 5, 0.45);
            body.IsHitTestVisible = false;
            g.Children.Add(body);

            // 中间放应用图标
            BitmapSource ic = Ui2.IconSource();
            if (ic != null)
            {
                Image img = new Image();
                img.Source = ic;
                img.Width = 46;
                img.Height = 46;
                img.Stretch = Stretch.UniformToFill;
                img.Clip = new EllipseGeometry(new Point(23, 23), 23, 23);
                img.HorizontalAlignment = HorizontalAlignment.Center;
                img.VerticalAlignment = VerticalAlignment.Center;
                img.IsHitTestVisible = false;
                g.Children.Add(img);
            }
            else
            {
                TextBlock t = Ui.Text("莫", 30, Brushes.White);
                t.FontWeight = FontWeights.Bold;
                t.TextTrimming = TextTrimming.None;
                t.HorizontalAlignment = HorizontalAlignment.Center;
                t.VerticalAlignment = VerticalAlignment.Center;
                t.IsHitTestVisible = false;
                g.Children.Add(t);
            }

            // 玻璃反光
            Ellipse gloss = new Ellipse();
            gloss.Margin = new Thickness(8);
            RadialGradientBrush rg = new RadialGradientBrush();
            rg.GradientOrigin = new Point(0.34, 0.22);
            rg.Center = new Point(0.34, 0.22);
            rg.RadiusX = 0.72;
            rg.RadiusY = 0.72;
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(150, 255, 255, 255), 0));
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1));
            gloss.Fill = rg;
            gloss.IsHitTestVisible = false;
            g.Children.Add(gloss);

            Ellipse ring = new Ellipse();
            ring.Margin = new Thickness(6);
            ring.Stroke = Ui.White(150);
            ring.StrokeThickness = 1.4;
            ring.IsHitTestVisible = false;
            g.Children.Add(ring);

            this.Content = g;

            this.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                pressed = true;
                dragged = false;
                downScreen = PointToScreen(e.GetPosition(this));
                startLeft = this.Left;
                startTop = this.Top;
                this.CaptureMouse();
            };

            this.MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (!pressed) return;
                Point p = PointToScreen(e.GetPosition(this));
                double dx = p.X - downScreen.X;
                double dy = p.Y - downScreen.Y;
                if (!dragged && (Math.Abs(dx) > 4 || Math.Abs(dy) > 4)) dragged = true;
                if (dragged)
                {
                    this.Left = startLeft + dx;
                    this.Top = startTop + dy;
                }
            };

            this.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                pressed = false;
                this.ReleaseMouseCapture();
                if (!dragged) TogglePopup();
            };

            ContextMenu cm = new ContextMenu();
            cm.FontFamily = Ui.YaHei;
            MenuItem m1 = new MenuItem();
            m1.Header = "打开主界面";
            m1.Click += delegate { owner.ShowMain(); };
            cm.Items.Add(m1);
            MenuItem m2 = new MenuItem();
            m2.Header = "隐藏悬浮球";
            m2.Click += delegate { HideBall(); };
            cm.Items.Add(m2);
            cm.Items.Add(new Separator());
            MenuItem m3 = new MenuItem();
            m3.Header = "退出";
            m3.Click += delegate { Application.Current.Shutdown(); };
            cm.Items.Add(m3);
            this.ContextMenu = cm;

            popup = new BallPopup(owner);
            popup.AddFromClipboard += delegate
            {
                if (owner.AddFromClipboard()) popup.Fill(owner.Items);
            };

            this.Loaded += delegate
            {
                Rect wa = SystemParameters.WorkArea;
                this.Left = wa.Right - this.Width - 26;
                this.Top = wa.Bottom - this.Height - 90;
            };
        }

        void TogglePopup()
        {
            if (popup.IsVisible) { popup.Hide(); popup.LastHidden = DateTime.Now; return; }
            if ((DateTime.Now - popup.LastHidden).TotalMilliseconds < 320) return;

            popup.Fill(owner.Items);
            popup.Show();
            popup.UpdateLayout();

            double pw = popup.ActualWidth > 0 ? popup.ActualWidth : popup.Width;
            double ph = popup.ActualHeight > 0 ? popup.ActualHeight : popup.Height;
            Rect wa = SystemParameters.WorkArea;

            double x = this.Left - pw - 12;
            if (x < wa.Left + 6) x = this.Left + this.Width + 12;

            double y = this.Top + this.Height - ph;
            if (y < wa.Top + 6) y = wa.Top + 6;
            if (y + ph > wa.Bottom - 6) y = wa.Bottom - ph - 6;

            popup.Left = x;
            popup.Top = y;
            popup.Activate();
        }

        public void ShowBall()
        {
            if (!this.IsVisible) this.Show();
            this.Topmost = true;
        }

        public void OpenPopup()
        {
            if (!popup.IsVisible) TogglePopup();
        }

        public void HideBall()
        {
            if (popup.IsVisible) popup.Hide();
            this.Hide();
        }
    }

    // 图标加载
    public static class Ui2
    {
        static BitmapSource cache;
        static bool tried;

        public static BitmapSource IconSource()
        {
            if (tried) return cache;
            tried = true;
            try
            {
                string ico = Cfg.FindIco();
                if (ico != null)
                {
                    SD.Icon sdi = new SD.Icon(ico, 256, 256);
                    BitmapSource bs = Imaging.CreateBitmapSourceFromHIcon(
                        sdi.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    bs.Freeze();
                    cache = bs;
                    return cache;
                }
            }
            catch { }

            try
            {
                string exe = Process.GetCurrentProcess().MainModule.FileName;
                SD.Icon sdi = SD.Icon.ExtractAssociatedIcon(exe);
                if (sdi != null)
                {
                    BitmapSource bs = Imaging.CreateBitmapSourceFromHIcon(
                        sdi.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    bs.Freeze();
                    cache = bs;
                    return cache;
                }
            }
            catch { }

            return null;
        }
    }

    // ==================================================================
    //  主窗口
    // ==================================================================
    public class MainWindow : Window
    {
        public List<Shortcut> Items;
        AppSettings set;

        MediaElement video;
        Grid videoLayer;
        Image bgA, bgB;
        DispatcherTimer imgTimer;
        List<string> imgList = new List<string>();
        int imgIndex;
        bool imgAuto = true;
        string curImgPath;
        string curVideoPath;
        ScrollViewer scroll;
        DispatcherTimer tick;
        DispatcherTimer toastTimer;

        DateTime targetTime;
        bool running;

        TextBox txtTime;
        TextBlock bigCount;
        Border btnStart; TextBlock btnStartLabel;
        Border btnTarget; TextBlock lblTarget; ContextMenu menuTarget; Shortcut selTarget;
        UniformGrid cardGrid;
        int cardCols = 0;

        Border toast;
        TextBlock toastText;

        BallWindow ball;
        bool wantVideo = true;
        TextBlock lblVideoToggle;
        TextBlock lblImgAuto;
        TextBlock lblBallToggle;
        TextBlock lblCount;

        public MainWindow(string[] args)
        {
            Items = Cfg.LoadLinks();
            set = Cfg.LoadSettings();

            this.Title = "莫印泯";
            this.Width = 1020;
            this.Height = 720;
            this.MinWidth = 800;
            this.MinHeight = 560;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowStyle = WindowStyle.None;
            this.Background = new SolidColorBrush(Color.FromRgb(0x0C, 0x10, 0x1E));
            this.FontFamily = Ui.YaHei;

            WindowChrome chrome = new WindowChrome();
            chrome.CaptionHeight = 48;
            chrome.ResizeBorderThickness = new Thickness(7);
            chrome.CornerRadius = new CornerRadius(0);
            chrome.GlassFrameThickness = new Thickness(1);
            chrome.UseAeroCaptionButtons = false;
            WindowChrome.SetWindowChrome(this, chrome);

            Grid root = new Grid();

            // 层1：动态渐变底（永远在，作为兜底）
            root.Children.Add(Ui.Backdrop());

            // 层2：图片背景（不播视频时显示）
            Grid imgLayer = new Grid();
            imgLayer.ClipToBounds = true;
            bgA = new Image();
            bgA.Stretch = Stretch.UniformToFill;
            bgA.Opacity = 1;
            bgB = new Image();
            bgB.Stretch = Stretch.UniformToFill;
            bgB.Opacity = 0;
            imgLayer.Children.Add(bgA);
            imgLayer.Children.Add(bgB);
            root.Children.Add(imgLayer);

            // 层3：视频
            videoLayer = new Grid();
            videoLayer.Visibility = Visibility.Collapsed;
            video = new MediaElement();
            video.LoadedBehavior = MediaState.Manual;
            video.UnloadedBehavior = MediaState.Manual;
            video.Stretch = Stretch.UniformToFill;
            video.IsMuted = true;
            video.Volume = 0;
            video.MediaEnded += delegate
            {
                try { video.Position = TimeSpan.Zero; video.Play(); }
                catch { }
            };
            video.MediaFailed += delegate { videoLayer.Visibility = Visibility.Collapsed; };
            videoLayer.Children.Add(video);
            root.Children.Add(videoLayer);

            // 层3：压暗 + 暗角
            root.Children.Add(Dimmer());

            // 层4：内容
            Grid content = new Grid();
            content.RowDefinitions.Add(new RowDefinition());
            content.RowDefinitions.Add(new RowDefinition());
            content.RowDefinitions.Add(new RowDefinition());
            content.RowDefinitions.Add(new RowDefinition());
            content.RowDefinitions.Add(new RowDefinition());
            content.RowDefinitions[0].Height = new GridLength(48);
            content.RowDefinitions[1].Height = GridLength.Auto;
            content.RowDefinitions[2].Height = GridLength.Auto;
            content.RowDefinitions[3].Height = new GridLength(1, GridUnitType.Star);
            content.RowDefinitions[4].Height = new GridLength(40);
            root.Children.Add(content);

            BuildTitleBar(content);
            BuildHeader(content);
            BuildCountdown(content);
            BuildCards(content);
            BuildStatus(content);
            BuildToast();

            root.Children.Add(overlayToastHost);
            this.Content = root;

            tick = new DispatcherTimer();
            tick.Interval = TimeSpan.FromMilliseconds(200);
            tick.Tick += delegate { OnTick(); };

            // 图片背景：每 1 分钟自动换一张
            imgTimer = new DispatcherTimer();
            imgTimer.Interval = TimeSpan.FromMinutes(1);
            imgTimer.Tick += delegate { NextImage(true); };

            txtTime.Text = DateTime.Now.AddMinutes(1).ToString("yyyy-MM-dd HH:mm:ss");

            RefreshCards();
            UpdateTargetCombo();

            this.Loaded += delegate { ApplySettings(); };
            this.Closing += delegate { PushSettings(); };

            ApplyArgs(args);
        }

        Grid overlayToastHost = new Grid();

        // ---------------- 压暗层 ----------------
        static Grid Dimmer()
        {
            Grid g = new Grid();
            g.IsHitTestVisible = false;

            Rectangle rect = new Rectangle();
            LinearGradientBrush lb = new LinearGradientBrush();
            lb.StartPoint = new Point(0, 0);
            lb.EndPoint = new Point(0, 1);
            lb.GradientStops.Add(new GradientStop(Color.FromArgb(150, 4, 6, 14), 0));
            lb.GradientStops.Add(new GradientStop(Color.FromArgb(178, 4, 6, 14), 1));
            rect.Fill = lb;
            g.Children.Add(rect);

            Rectangle vig = new Rectangle();
            RadialGradientBrush rg = new RadialGradientBrush();
            rg.Center = new Point(0.5, 0.42);
            rg.RadiusX = 0.9;
            rg.RadiusY = 0.9;
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.4));
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(150, 0, 0, 0), 1));
            vig.Fill = rg;
            g.Children.Add(vig);

            return g;
        }

        // ---------------- 标题栏 ----------------
        void BuildTitleBar(Grid content)
        {
            Grid bar = new Grid();
            bar.Background = Brushes.Transparent;

            StackPanel right = new StackPanel();
            right.Orientation = Orientation.Horizontal;
            right.HorizontalAlignment = HorizontalAlignment.Right;
            right.VerticalAlignment = VerticalAlignment.Center;
            right.Margin = new Thickness(0, 0, 10, 0);

            right.Children.Add(Chrome("—", delegate { this.WindowState = WindowState.Minimized; }));
            right.Children.Add(Chrome("✕", delegate { Close(); }));

            bar.Children.Add(right);
            Grid.SetRow(bar, 0);
            content.Children.Add(bar);
        }

        Border Chrome(string glyph, Action act)
        {
            Border b = new Border();
            b.Width = 36;
            b.Height = 28;
            b.CornerRadius = new CornerRadius(9);
            b.Margin = new Thickness(4, 0, 0, 0);
            b.Background = Ui.White(30);
            b.Cursor = Cursors.Hand;
            TextBlock t = Ui.Text(glyph, 12, Brushes.White);
            t.TextTrimming = TextTrimming.None;
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;
            b.Child = t;
            b.MouseEnter += delegate { b.Background = Ui.White(70); };
            b.MouseLeave += delegate { b.Background = Ui.White(30); };
            b.MouseLeftButtonUp += delegate { act(); };
            WindowChrome.SetIsHitTestVisibleInChrome(b, true);
            return b;
        }

        // ---------------- 居中的标题区 ----------------
        void BuildHeader(Grid content)
        {
            StackPanel sp = new StackPanel();
            sp.HorizontalAlignment = HorizontalAlignment.Center;
            sp.Margin = new Thickness(0, 6, 0, 14);

            // 圆形图标
            Grid iconWrap = new Grid();
            iconWrap.Width = 72;
            iconWrap.Height = 72;
            iconWrap.HorizontalAlignment = HorizontalAlignment.Center;

            Ellipse halo = new Ellipse();
            RadialGradientBrush hb = new RadialGradientBrush();
            hb.GradientStops.Add(new GradientStop(Color.FromArgb(96, 255, 255, 255), 0));
            hb.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1));
            halo.Fill = hb;
            halo.Margin = new Thickness(-14);
            iconWrap.Children.Add(halo);

            BitmapSource ic = Ui2.IconSource();
            if (ic != null)
            {
                Image img = new Image();
                img.Source = ic;
                img.Width = 64;
                img.Height = 64;
                img.Stretch = Stretch.UniformToFill;
                img.Clip = new EllipseGeometry(new Point(32, 32), 32, 32);
                iconWrap.Children.Add(img);
            }
            else
            {
                Ellipse e = new Ellipse();
                e.Width = 64;
                e.Height = 64;
                LinearGradientBrush lg = new LinearGradientBrush();
                lg.StartPoint = new Point(0, 0);
                lg.EndPoint = new Point(1, 1);
                lg.GradientStops.Add(new GradientStop(Color.FromRgb(0x5B, 0x7C, 0xFF), 0));
                lg.GradientStops.Add(new GradientStop(Color.FromRgb(0x8B, 0x5C, 0xF6), 1));
                e.Fill = lg;
                iconWrap.Children.Add(e);
            }

            Ellipse ring = new Ellipse();
            ring.Width = 64;
            ring.Height = 64;
            ring.Stroke = Ui.White(140);
            ring.StrokeThickness = 1.6;
            ring.IsHitTestVisible = false;
            iconWrap.Children.Add(ring);

            // 高光
            Ellipse gloss = new Ellipse();
            gloss.Width = 64;
            gloss.Height = 64;
            RadialGradientBrush gr = new RadialGradientBrush();
            gr.GradientOrigin = new Point(0.34, 0.2);
            gr.Center = new Point(0.34, 0.2);
            gr.RadiusX = 0.7;
            gr.RadiusY = 0.7;
            gr.GradientStops.Add(new GradientStop(Color.FromArgb(120, 255, 255, 255), 0));
            gr.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1));
            gloss.Fill = gr;
            gloss.IsHitTestVisible = false;
            iconWrap.Children.Add(gloss);

            sp.Children.Add(iconWrap);

            TextBlock name = Ui.Text("莫印泯", 34, Brushes.White);
            name.FontWeight = FontWeights.Bold;
            name.TextTrimming = TextTrimming.None;
            name.HorizontalAlignment = HorizontalAlignment.Center;
            name.Margin = new Thickness(0, 10, 0, 0);
            DropShadowEffect ds = new DropShadowEffect();
            ds.BlurRadius = 16;
            ds.ShadowDepth = 0;
            ds.Opacity = 0.7;
            ds.Color = Colors.Black;
            name.Effect = ds;
            sp.Children.Add(name);

            TextBlock sub = Ui.Text("一键直达官方 App / 商品页", 13,
                new SolidColorBrush(Color.FromArgb(190, 255, 255, 255)));
            sub.TextTrimming = TextTrimming.None;
            sub.HorizontalAlignment = HorizontalAlignment.Center;
            sub.Margin = new Thickness(0, 4, 0, 0);
            sp.Children.Add(sub);

            // 操作药丸（窗口窄了会自动换行）
            WrapPanel pills = new WrapPanel();
            pills.Orientation = Orientation.Horizontal;
            pills.HorizontalAlignment = HorizontalAlignment.Center;
            pills.MaxWidth = 900;
            pills.Margin = new Thickness(0, 14, 0, 0);

            pills.Children.Add(Ui.Pill("＋  添加", delegate { AddItem(); }, true));
            pills.Children.Add(Ui.Pill("📋  存剪贴板链接", delegate { AddFromClipboard(); }, false));
            pills.Children.Add(Ui.Pill("📂  数据目录", delegate { Opener.Open(Cfg.DataDir()); }, false));

            // 自动切换（左键开关 / 右键立刻换下一张）
            Border aPill = Ui.Pill("🔁  自动切换", delegate { ToggleImgAuto(); }, false, out lblImgAuto);
            ContextMenu acm = new ContextMenu();
            acm.FontFamily = Ui.YaHei;
            MenuItem ami = new MenuItem();
            ami.Header = "立刻换下一张";
            ami.Click += delegate { NextImage(true); };
            acm.Items.Add(ami);
            aPill.ContextMenu = acm;
            pills.Children.Add(aPill);

            // 选择图片（独立按钮）
            pills.Children.Add(Ui.Pill("🖼  选择图片", delegate { PickImage(); }, false));

            // 播放视频（左键开关 / 右键选视频）
            Border vPill = Ui.Pill("▶  播放视频", delegate { ToggleVideo(); }, false, out lblVideoToggle);
            ContextMenu vcm = new ContextMenu();
            vcm.FontFamily = Ui.YaHei;

            MenuItem v1 = new MenuItem();
            v1.Header = "选择视频文件…";
            v1.Click += delegate { PickVideo(); };
            vcm.Items.Add(v1);

            MenuItem v2 = new MenuItem();
            v2.Header = "文件夹里的视频";
            v2.FontFamily = Ui.YaHei;
            vcm.Opened += delegate
            {
                v2.Items.Clear();
                List<string> vs = Cfg.FindVideos();
                if (vs.Count == 0)
                {
                    MenuItem none = new MenuItem();
                    none.Header = "（文件夹里没有视频）";
                    none.IsEnabled = false;
                    v2.Items.Add(none);
                }
                else
                {
                    foreach (string v in vs)
                    {
                        string vv = v;
                        MenuItem it = new MenuItem();
                        long mb = 0;
                        try { mb = new FileInfo(vv).Length / 1048576; } catch { }
                        it.Header = Path.GetFileName(vv) + "   (" + mb + " MB)";
                        it.Click += delegate { UseVideo(vv); };
                        v2.Items.Add(it);
                    }
                }
            };
            vcm.Items.Add(v2);

            vcm.Items.Add(new Separator());
            MenuItem v3 = new MenuItem();
            v3.Header = "不使用视频";
            v3.Click += delegate
            {
                wantVideo = false;
                StopVideo();
                SyncVideoLabel();
                ShowToast("已关掉视频背景，现在显示图片背景");
            };
            vcm.Items.Add(v3);

            vPill.ContextMenu = vcm;
            pills.Children.Add(vPill);

            Border bPill = Ui.Pill("🔮  悬浮球", delegate { ToggleBall(); }, false, out lblBallToggle);
            pills.Children.Add(bPill);

            sp.Children.Add(pills);

            Grid.SetRow(sp, 1);
            content.Children.Add(sp);
        }

        // ---------------- 倒计时 ----------------
        void BuildCountdown(Grid content)
        {
            Border bar = Ui.GlassPanel(18, 42, 66);
            bar.Margin = new Thickness(20, 0, 20, 12);
            bar.Padding = new Thickness(18, 12, 18, 12);
            bar.Effect = Ui.Shadow(18, 5, 0.28);

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions[0].Width = GridLength.Auto;
            g.ColumnDefinitions[1].Width = GridLength.Auto;
            g.ColumnDefinitions[2].Width = GridLength.Auto;
            g.ColumnDefinitions[3].Width = GridLength.Auto;
            g.ColumnDefinitions[4].Width = new GridLength(1, GridUnitType.Star);

            // 开售时间
            StackPanel c0 = new StackPanel();
            c0.VerticalAlignment = VerticalAlignment.Center;
            TextBlock l0 = Ui.Text("开售时间", 11, new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)));
            l0.TextTrimming = TextTrimming.None;
            c0.Children.Add(l0);

            txtTime = new TextBox();
            txtTime.Width = 186;
            txtTime.FontSize = 13;
            txtTime.FontFamily = new FontFamily("Consolas");
            txtTime.Padding = new Thickness(9, 6, 9, 6);
            txtTime.Margin = new Thickness(0, 5, 0, 0);
            txtTime.BorderThickness = new Thickness(1);
            txtTime.BorderBrush = Ui.White(70);
            txtTime.Background = Ui.White(28);
            txtTime.Foreground = Brushes.White;
            txtTime.CaretBrush = Brushes.White;
            c0.Children.Add(txtTime);
            g.Children.Add(c0);

            // 快捷
            StackPanel c1 = new StackPanel();
            c1.VerticalAlignment = VerticalAlignment.Center;
            c1.Margin = new Thickness(14, 0, 0, 0);
            TextBlock l1 = Ui.Text("快捷设置", 11, new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)));
            l1.TextTrimming = TextTrimming.None;
            c1.Children.Add(l1);
            StackPanel q = new StackPanel();
            q.Orientation = Orientation.Horizontal;
            q.Margin = new Thickness(0, 5, 0, 0);
            q.Children.Add(Ui.Pill("+1分", delegate { QuickAdd(1); }, false));
            q.Children.Add(Ui.Pill("+5分", delegate { QuickAdd(5); }, false));
            q.Children.Add(Ui.Pill("+10分", delegate { QuickAdd(10); }, false));
            q.Children.Add(Ui.Pill("今晚20:00", delegate { QuickTonight(); }, false));
            c1.Children.Add(q);
            Grid.SetColumn(c1, 1);
            g.Children.Add(c1);

            // 开始
            btnStart = Ui.Pill("开始", delegate { ToggleCountdown(); }, true, out btnStartLabel);
            btnStart.Margin = new Thickness(14, 0, 0, 0);
            btnStart.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(btnStart, 2);
            g.Children.Add(btnStart);

            // 到点前往
            StackPanel c3 = new StackPanel();
            c3.VerticalAlignment = VerticalAlignment.Center;
            c3.Margin = new Thickness(16, 0, 0, 0);
            TextBlock l3 = Ui.Text("到点前往", 11, new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)));
            l3.TextTrimming = TextTrimming.None;
            c3.Children.Add(l3);

            btnTarget = Ui.GlassPanel(13, 40, 72);
            btnTarget.Width = 190;
            btnTarget.Padding = new Thickness(11, 7, 9, 7);
            btnTarget.Margin = new Thickness(0, 5, 0, 0);
            btnTarget.Cursor = Cursors.Hand;

            Grid tg = new Grid();
            tg.ColumnDefinitions.Add(new ColumnDefinition());
            tg.ColumnDefinitions.Add(new ColumnDefinition());
            tg.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            tg.ColumnDefinitions[1].Width = GridLength.Auto;

            lblTarget = Ui.Text("（不跳转，只提醒）", 12.5,
                new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)));
            lblTarget.VerticalAlignment = VerticalAlignment.Center;
            tg.Children.Add(lblTarget);

            TextBlock arrow = Ui.Text("▾", 11, new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)));
            arrow.TextTrimming = TextTrimming.None;
            arrow.VerticalAlignment = VerticalAlignment.Center;
            arrow.Margin = new Thickness(6, 0, 0, 0);
            Grid.SetColumn(arrow, 1);
            tg.Children.Add(arrow);

            btnTarget.Child = tg;

            menuTarget = new ContextMenu();
            menuTarget.FontFamily = Ui.YaHei;
            btnTarget.ContextMenu = menuTarget;

            btnTarget.MouseEnter += delegate { btnTarget.Background = Ui.White(72); };
            btnTarget.MouseLeave += delegate { btnTarget.Background = Ui.White(40); };
            btnTarget.MouseLeftButtonUp += delegate
            {
                menuTarget.PlacementTarget = btnTarget;
                menuTarget.Placement = PlacementMode.Bottom;
                menuTarget.HorizontalOffset = 0;
                menuTarget.VerticalOffset = 4;
                menuTarget.IsOpen = true;
            };

            c3.Children.Add(btnTarget);
            Grid.SetColumn(c3, 3);
            g.Children.Add(c3);

            // 大倒计时
            StackPanel c4 = new StackPanel();
            c4.VerticalAlignment = VerticalAlignment.Center;
            c4.HorizontalAlignment = HorizontalAlignment.Right;
            TextBlock l4 = Ui.Text("剩余时间", 11, new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)));
            l4.TextTrimming = TextTrimming.None;
            l4.HorizontalAlignment = HorizontalAlignment.Right;
            c4.Children.Add(l4);
            bigCount = Ui.Text("--:--:--", 30, new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)));
            bigCount.FontFamily = new FontFamily("Consolas");
            bigCount.FontWeight = FontWeights.Bold;
            bigCount.TextTrimming = TextTrimming.None;
            bigCount.HorizontalAlignment = HorizontalAlignment.Right;
            c4.Children.Add(bigCount);
            Grid.SetColumn(c4, 4);
            g.Children.Add(c4);

            bar.Child = g;
            Grid.SetRow(bar, 2);
            content.Children.Add(bar);
        }

        // ---------------- 卡片区 ----------------
        void BuildCards(Grid content)
        {
            scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scroll.Margin = new Thickness(12, 0, 12, 0);
            scroll.PanningMode = PanningMode.VerticalOnly;

            cardGrid = new UniformGrid();
            cardGrid.Margin = new Thickness(4);
            cardGrid.Rows = 0;
            cardGrid.Columns = 3;
            cardCols = 3;
            scroll.Content = cardGrid;

            scroll.SizeChanged += delegate { RelayoutCards(); };

            Grid.SetRow(scroll, 3);
            content.Children.Add(scroll);
        }

        void RelayoutCards()
        {
            double avail = scroll.ActualWidth - 14;
            if (avail <= 0) return;
            int cols = (int)Math.Floor(avail / 232.0);
            if (cols < 2) cols = 2;
            if (cols > 6) cols = 6;
            if (cols != cardCols)
            {
                cardCols = cols;
                cardGrid.Columns = cols;
            }
        }

        // ---------------- 状态栏 ----------------
        void BuildStatus(Grid content)
        {
            Grid bar = new Grid();

            StackPanel left = new StackPanel();
            left.Orientation = Orientation.Horizontal;
            left.VerticalAlignment = VerticalAlignment.Center;
            left.Margin = new Thickness(20, 0, 0, 0);

            lblCount = Ui.Text("", 11.5, new SolidColorBrush(Color.FromArgb(165, 255, 255, 255)));
            lblCount.TextTrimming = TextTrimming.None;
            lblCount.VerticalAlignment = VerticalAlignment.Center;
            left.Children.Add(lblCount);

            TextBlock hint = Ui.Text("   左键打开 · 右键菜单（编辑/删除）· 拖标题栏移动窗口",
                11.5, new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)));
            hint.TextTrimming = TextTrimming.None;
            hint.VerticalAlignment = VerticalAlignment.Center;
            left.Children.Add(hint);

            bar.Children.Add(left);

            TextBlock dataPath = Ui.Text("数据：" + Cfg.LinksPath(), 10.5,
                new SolidColorBrush(Color.FromArgb(105, 255, 255, 255)));
            dataPath.TextTrimming = TextTrimming.None;
            dataPath.HorizontalAlignment = HorizontalAlignment.Right;
            dataPath.VerticalAlignment = VerticalAlignment.Center;
            dataPath.Margin = new Thickness(0, 0, 20, 0);
            bar.Children.Add(dataPath);

            Grid.SetRow(bar, 4);
            content.Children.Add(bar);
        }

        void BuildToast()
        {
            toast = new Border();
            toast.CornerRadius = new CornerRadius(14);
            toast.Background = new SolidColorBrush(Color.FromArgb(235, 30, 36, 56));
            toast.BorderBrush = Ui.White(110);
            toast.BorderThickness = new Thickness(1);
            toast.Padding = new Thickness(18, 11, 18, 11);
            toast.HorizontalAlignment = HorizontalAlignment.Center;
            toast.VerticalAlignment = VerticalAlignment.Bottom;
            toast.Margin = new Thickness(0, 0, 0, 70);
            toast.Visibility = Visibility.Collapsed;
            toast.IsHitTestVisible = false;
            toast.Effect = Ui.Shadow(20, 6, 0.5);

            toastText = Ui.Text("", 12.5, Brushes.White);
            toastText.TextTrimming = TextTrimming.None;
            toast.Child = toastText;

            overlayToastHost.Children.Add(toast);

            toastTimer = new DispatcherTimer();
            toastTimer.Interval = TimeSpan.FromSeconds(2.4);
            toastTimer.Tick += delegate { toastTimer.Stop(); toast.Visibility = Visibility.Collapsed; };
        }

        public void ShowToast(string msg)
        {
            toastText.Text = msg;
            toast.Visibility = Visibility.Visible;
            toastTimer.Stop();
            toastTimer.Start();
        }

        // ---------------- 卡片渲染 ----------------
        void RefreshCards()
        {
            cardGrid.Children.Clear();
            foreach (Shortcut s in Items)
            {
                cardGrid.Children.Add(Cards.Make(s,
                    delegate(Shortcut x) { Cards.OpenWithFallback(x, this); },
                    delegate(Shortcut x) { EditItem(x); },
                    delegate(Shortcut x) { DeleteItem(x); }));
            }
            if (lblCount != null) lblCount.Text = "共 " + Items.Count + " 项";
            RelayoutCards();
        }

        void UpdateTargetCombo()
        {
            menuTarget.Items.Clear();

            MenuItem mi0 = new MenuItem();
            mi0.Header = "（不跳转，只提醒）";
            mi0.Click += delegate { selTarget = null; lblTarget.Text = "（不跳转，只提醒）"; };
            menuTarget.Items.Add(mi0);
            menuTarget.Items.Add(new Separator());

            foreach (Shortcut s in Items)
            {
                Shortcut cur = s;
                MenuItem mi = new MenuItem();
                mi.Header = s.Name;
                mi.Click += delegate { selTarget = cur; lblTarget.Text = cur.Name; };
                menuTarget.Items.Add(mi);
            }

            if (selTarget != null && !Items.Contains(selTarget))
            {
                selTarget = null;
            }
            lblTarget.Text = (selTarget == null) ? "（不跳转，只提醒）" : selTarget.Name;
        }

        // ---------------- 增删改 ----------------
        public void AddItem()
        {
            EditDialog d = new EditDialog(null, null);
            d.Owner = this;
            bool? r = d.ShowDialog();
            if (r == true && d.Result != null)
            {
                Items.Add(d.Result);
                Cfg.SaveLinks(Items);
                RefreshCards();
                UpdateTargetCombo();
                ShowToast("已添加：" + d.Result.Name);
            }
        }

        public void EditItem(Shortcut s)
        {
            EditDialog d = new EditDialog(s, null);
            d.Owner = this;
            bool? r = d.ShowDialog();
            if (r == true && d.Result != null)
            {
                s.Name = d.Result.Name;
                s.Url = d.Result.Url;
                s.Note = d.Result.Note;
                s.Fallback = d.Result.Fallback;
                Cfg.SaveLinks(Items);
                RefreshCards();
                UpdateTargetCombo();
            }
        }

        void DeleteItem(Shortcut s)
        {
            MessageBoxResult r = MessageBox.Show(this, "确定删除「" + s.Name + "」？", "确认",
                MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (r == MessageBoxResult.OK)
            {
                Items.Remove(s);
                Cfg.SaveLinks(Items);
                RefreshCards();
                UpdateTargetCombo();
            }
        }

        // ---------------- 存卡片（剪贴板 / 命令行共用） ----------------
        public bool AddUrlAsCard(string url, string name, string note)
        {
            if (string.IsNullOrEmpty(url)) return false;
            url = url.Trim();

            if (string.IsNullOrEmpty(name))
            {
                string host = Opener.Host(url);
                name = host;
                int n = 2;
                foreach (Shortcut ex in Items)
                {
                    if (ex.Name == name) { name = host + " (" + n + ")"; n++; }
                }
            }

            Shortcut s = new Shortcut();
            s.Name = name;
            s.Url = url;
            s.Note = note == null ? "" : note;
            s.Fallback = "";
            Items.Add(s);
            Cfg.SaveLinks(Items);
            RefreshCards();
            UpdateTargetCombo();
            ShowToast("已存为卡片：" + name + "   （右键卡片可改名）");
            return true;
        }

        public bool AddFromClipboard()
        {
            string t = Opener.ReadClipboard();
            if (string.IsNullOrEmpty(t))
            {
                ShowToast("剪贴板里没有文字。先在浏览器地址栏按 Ctrl+C 复制网址。");
                return false;
            }
            if (!Opener.LooksLikeUrl(t))
            {
                ShowToast("剪贴板内容不像网址：" + (t.Length > 32 ? t.Substring(0, 32) + "…" : t));
                return false;
            }
            return AddUrlAsCard(t, null, "来自剪贴板");
        }

        // ---------------- 设置开关 ----------------
        void ApplySettings()
        {
            bool wantBall = set.Ball.HasValue ? set.Ball.Value : false;

            // ---- 视频路径（用户上次选的） ----
            curVideoPath = set.VideoPath;

            // ---- 图片背景 ----
            imgList = Cfg.FindImages();
            imgAuto = set.ImageAuto.HasValue ? set.ImageAuto.Value : true;

            string manual = set.ImagePath;
            int at = -1;
            if (!string.IsNullOrEmpty(manual) && File.Exists(manual))
            {
                at = imgList.FindIndex(delegate(string x)
                {
                    return string.Equals(x, manual, StringComparison.OrdinalIgnoreCase);
                });
            }

            if (at >= 0)
            {
                imgIndex = at;
                ShowImage(imgList[at], false);
            }
            else if (!string.IsNullOrEmpty(manual) && File.Exists(manual))
            {
                imgIndex = 0;
                ShowImage(manual, false);
            }
            else if (imgList.Count > 0)
            {
                imgIndex = 0;
                ShowImage(imgList[0], false);
            }

            if (imgAuto && imgList.Count > 1) imgTimer.Start(); else imgTimer.Stop();
            SyncImgLabel();

            // ---- 视频 ----
            wantVideo = set.Video.HasValue ? set.Video.Value : true;
            if (wantVideo) TryLoadVideo();
            SyncVideoLabel();

            // ---- 悬浮球 ----
            if (wantBall) ShowBallNow();
            lblBallToggle.Text = (ball != null && ball.IsVisible) ? "🔮  悬浮球：开" : "🔮  悬浮球：关";
        }

        AppSettings GrabSettings()
        {
            AppSettings s = new AppSettings();
            s.Video = wantVideo;
            s.Ball = (ball != null && ball.IsVisible);
            s.ImageAuto = imgAuto;
            s.ImagePath = curImgPath;
            s.VideoPath = curVideoPath;
            return s;
        }

        void PushSettings()
        {
            set = GrabSettings();
            Cfg.SaveSettings(set);
        }

        // ==================== 图片背景 ====================
        static BitmapImage LoadBitmap(string path)
        {
            try
            {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bi.UriSource = new Uri(path);
                bi.DecodePixelWidth = 2560;
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch { return null; }
        }

        void ShowImage(string path, bool animate)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!File.Exists(path)) return;

            BitmapImage bi = LoadBitmap(path);
            if (bi == null) return;
            curImgPath = path;

            if (!animate)
            {
                bgB.BeginAnimation(UIElement.OpacityProperty, null);
                bgB.Opacity = 0;
                bgA.Opacity = 1;
                bgA.Source = bi;
                return;
            }

            bgB.Source = bi;
            DoubleAnimation fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(1100));
            fade.Completed += delegate
            {
                bgB.BeginAnimation(UIElement.OpacityProperty, null);
                bgB.Opacity = 0;
                bgA.Opacity = 1;
                bgA.Source = bi;
            };
            bgB.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        void NextImage(bool animate)
        {
            if (imgList == null || imgList.Count == 0) return;
            imgIndex = (imgIndex + 1) % imgList.Count;
            ShowImage(imgList[imgIndex], animate);
        }

        void SyncImgLabel()
        {
            if (lblImgAuto == null) return;
            if (imgList == null || imgList.Count == 0)
            {
                lblImgAuto.Text = "🔁  自动切换：无图";
                return;
            }
            lblImgAuto.Text = imgAuto
                ? "🔁  自动切换：开(" + imgList.Count + "张)"
                : "🔁  自动切换：关";
        }

        void ToggleImgAuto()
        {
            if (imgList == null || imgList.Count == 0)
            {
                ShowToast("没找到图片。把图片放进程序文件夹，或点「🖼 选择图片」自己挑一张。");
                return;
            }

            imgAuto = !imgAuto;
            SyncImgLabel();

            if (imgAuto)
            {
                imgTimer.Start();
                ShowToast("自动切换已开：每 1 分钟换一张（共 " + imgList.Count + " 张）");
            }
            else
            {
                imgTimer.Stop();
                ShowToast("自动切换已关，背景停在这一张");
            }
            Cfg.SaveSettings(GrabSettings());
        }

        void PickImage()
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Title = "选一张图片当背景";
            d.Filter = "图片|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif|所有文件|*.*";
            d.CheckFileExists = true;
            bool? r = d.ShowDialog(this);
            if (r != true) return;

            string p = d.FileName;
            if (!Cfg.BigEnough(p, 200))
            {
                MessageBoxResult mr = MessageBox.Show(this,
                    "这张图有点小，拉满整屏可能会糊。还是要用吗？", "提示",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (mr != MessageBoxResult.OK) return;
            }

            wantVideo = false;
            StopVideo();

            ShowImage(p, true);
            imgAuto = false;
            imgTimer.Stop();
            SyncImgLabel();
            SyncVideoLabel();
            Cfg.SaveSettings(GrabSettings());
            ShowToast("已换成你选的图片，自动切换已停（想恢复就点「🔁 自动切换」）");
        }

        // ==================== 视频背景 ====================
        string ResolveVideo()
        {
            if (!string.IsNullOrEmpty(curVideoPath) && File.Exists(curVideoPath)) return curVideoPath;
            return Cfg.FindVideo();
        }

        void TryLoadVideo()
        {
            string v = ResolveVideo();
            if (v == null)
            {
                videoLayer.Visibility = Visibility.Collapsed;
                return;
            }
            try
            {
                video.Source = new Uri(v);
                curVideoPath = v;
                videoLayer.Visibility = Visibility.Visible;
                video.Play();
            }
            catch
            {
                videoLayer.Visibility = Visibility.Collapsed;
            }
        }

        void StopVideo()
        {
            try { video.Stop(); } catch { }
            try { video.Source = null; } catch { }
            videoLayer.Visibility = Visibility.Collapsed;
        }

        void SyncVideoLabel()
        {
            if (lblVideoToggle == null) return;
            if (videoLayer.Visibility == Visibility.Visible)
            {
                lblVideoToggle.Text = "▶  播放视频：开";
            }
            else if (string.IsNullOrEmpty(curVideoPath) && Cfg.FindVideo() == null)
            {
                lblVideoToggle.Text = "▶  播放视频：无视频";
            }
            else
            {
                lblVideoToggle.Text = "▶  播放视频：关";
            }
        }

        void UseVideo(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                ShowToast("这个视频文件不见了");
                return;
            }

            StopVideo();
            curVideoPath = path;
            wantVideo = true;

            try
            {
                video.Source = new Uri(path);
                videoLayer.Visibility = Visibility.Visible;
                video.Play();
            }
            catch
            {
                videoLayer.Visibility = Visibility.Collapsed;
                MessageBox.Show(this, "这个视频播不了（编码不被支持）。换一个 mp4 试试。", "提示");
                SyncVideoLabel();
                return;
            }

            SyncVideoLabel();
            Cfg.SaveSettings(GrabSettings());
            ShowToast("正在播放：" + Path.GetFileName(path));
        }

        void PickVideo()
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Title = "选一个视频当背景";
            d.Filter = "视频|*.mp4;*.webm;*.wmv;*.avi;*.mkv;*.mov;*.m4v|所有文件|*.*";
            d.CheckFileExists = true;
            bool? r = d.ShowDialog(this);
            if (r != true) return;
            UseVideo(d.FileName);
        }

        void ToggleVideo()
        {
            if (videoLayer.Visibility == Visibility.Visible)
            {
                wantVideo = false;
                StopVideo();
                SyncVideoLabel();
                Cfg.SaveSettings(GrabSettings());
                ShowToast("视频已关，现在显示图片背景（每 1 分钟自动换）");
                return;
            }

            string v = ResolveVideo();
            if (v == null)
            {
                SyncVideoLabel();
                ShowToast("没找到视频，帮你打开选择框");
                PickVideo();
                return;
            }
            UseVideo(v);
        }

        void ShowBallNow()
        {
            if (ball == null) ball = new BallWindow(this);
            ball.ShowBall();
        }

        void ToggleBall()
        {
            if (ball != null && ball.IsVisible)
            {
                ball.HideBall();
                lblBallToggle.Text = "🔮  悬浮球：关";
                ShowToast("悬浮球已隐藏。要再打开，先还原窗口再点这个按钮。");
            }
            else
            {
                ShowBallNow();
                lblBallToggle.Text = "🔮  悬浮球：开";
                ShowToast("悬浮球已开启：点击球 = 快捷列表，拖动 = 移动，右键 = 菜单");
            }
        }

        public void ShowMain()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        // ---------------- 倒计时逻辑 ----------------
        void QuickAdd(int minutes)
        {
            txtTime.Text = DateTime.Now.AddMinutes(minutes).ToString("yyyy-MM-dd HH:mm:ss");
        }

        void QuickTonight()
        {
            DateTime t = DateTime.Today.AddHours(20);
            if (t <= DateTime.Now) t = t.AddDays(1);
            txtTime.Text = t.ToString("yyyy-MM-dd HH:mm:ss");
        }

        void ToggleCountdown()
        {
            if (running)
            {
                running = false;
                tick.Stop();
                btnStartLabel.Text = "开始";
                bigCount.Foreground = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255));
                return;
            }

            DateTime t;
            if (!DateTime.TryParse(txtTime.Text.Trim(), out t))
            {
                MessageBox.Show(this, "时间格式不对。请用 2026-10-17 20:00:00 这种格式。", "提示");
                return;
            }
            if (t <= DateTime.Now)
            {
                MessageBox.Show(this, "请选择一个未来的时间。", "提示");
                return;
            }

            targetTime = t;
            running = true;
            tick.Start();
            btnStartLabel.Text = "停止";
            bigCount.Foreground = new SolidColorBrush(Color.FromRgb(0x8F, 0xB4, 0xFF));
        }

        void OnTick()
        {
            TimeSpan left = targetTime - DateTime.Now;
            if (left.TotalMilliseconds <= 0)
            {
                running = false;
                tick.Stop();
                btnStartLabel.Text = "开始";
                bigCount.Text = "00:00:00";
                bigCount.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B));
                Fire();
                return;
            }
            long days = (long)left.TotalDays;
            bigCount.Text = string.Format("{0}{1:00}:{2:00}:{3:00}",
                days > 0 ? days + "天 " : "", left.Hours, left.Minutes, left.Seconds);
        }

        void Fire()
        {
            Shortcut t = selTarget;

            this.Topmost = true;
            if (this.WindowState == WindowState.Minimized) this.WindowState = WindowState.Normal;
            this.Show();
            this.Activate();

            ReminderWindow rw = new ReminderWindow(t, targetTime.ToString("yyyy-MM-dd HH:mm:ss"));
            rw.Owner = this;
            rw.ShowDialog();

            this.Topmost = false;
        }

        // ---------------- 命令行 ----------------
        void ApplyArgs(string[] args)
        {
            if (args == null || args.Length == 0) return;

            DateTime? at = null;
            string target = null;
            bool ballOnly = false;
            bool popupToo = false;
            string addUrl = null;
            string addName = null;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLower();
                if (a == "--in" && i + 1 < args.Length)
                {
                    int sec;
                    if (int.TryParse(args[++i], out sec)) at = DateTime.Now.AddSeconds(sec);
                }
                else if (a == "--at" && i + 1 < args.Length)
                {
                    DateTime tmp;
                    if (DateTime.TryParse(args[++i], out tmp)) at = tmp;
                }
                else if (a == "--target" && i + 1 < args.Length)
                {
                    target = args[++i];
                }
                else if (a == "--add" && i + 1 < args.Length)
                {
                    addUrl = args[++i];
                }
                else if (a == "--name" && i + 1 < args.Length)
                {
                    addName = args[++i];
                }
                else if (a == "--ball")
                {
                    ballOnly = true;
                }
                else if (a == "--popup")
                {
                    popupToo = true;
                }
            }

            if (target != null)
            {
                foreach (Shortcut s in Items)
                {
                    if (s.Name == target) { selTarget = s; lblTarget.Text = s.Name; break; }
                }
            }

            if (addUrl != null)
            {
                string u = addUrl;
                string nm = addName;
                string ul = u.ToLower();
                if (ul == "clip" || ul == "clipboard" || ul == "剪贴板")
                {
                    // 莫印泯.exe --add clip  →  把剪贴板里的网址存成卡片
                    this.ContentRendered += delegate { AddFromClipboard(); };
                }
                else
                {
                    this.ContentRendered += delegate { AddUrlAsCard(u, nm, "命令行添加"); };
                }
            }

            if (at.HasValue)
            {
                DateTime t = at.Value;
                if (t <= DateTime.Now) t = DateTime.Now.AddSeconds(5);
                txtTime.Text = t.ToString("yyyy-MM-dd HH:mm:ss");
                this.ContentRendered += delegate { ToggleCountdown(); };
            }

            if (ballOnly || popupToo)
            {
                set.Ball = true;
                bool wantPopup = popupToo;
                this.ContentRendered += delegate
                {
                    ShowBallNow();
                    lblBallToggle.Text = "🔮  悬浮球：开";
                    if (wantPopup)
                    {
                        DispatcherTimer d = new DispatcherTimer();
                        d.Interval = TimeSpan.FromMilliseconds(700);
                        d.Tick += delegate { d.Stop(); ball.OpenPopup(); };
                        d.Start();
                    }
                    if (ballOnly) this.Hide();
                };
            }
        }
    }

    // ==================================================================
    //  自检（隐藏功能：莫印泯.exe --check）
    //  只做一件事：证明「编辑窗口的 保存/取消 点得动、窗口照样能拖」
    // ==================================================================
    public static class SelfCheck
    {
        static List<string> log = new List<string>();
        static int okCount, badCount;

        static void Pass(string s) { log.Add("   [通过] " + s); okCount++; }
        static void Fail(string s) { log.Add("   [失败] " + s); badCount++; }

        static void Flush()
        {
            try
            {
                File.WriteAllLines(Path.Combine(Cfg.AppDir(), "自检报告.txt"),
                    log.ToArray(), new UTF8Encoding(true));
            }
            catch { }
        }

        static void CollectMarked(DependencyObject d, List<DependencyObject> res)
        {
            if (d == null) return;
            FrameworkElement fe = d as FrameworkElement;
            if (fe != null && fe.Tag is string && ((string)fe.Tag) == "nograb") res.Add(d);

            System.Collections.IEnumerable kids = null;
            try { kids = LogicalTreeHelper.GetChildren(d); } catch { }
            if (kids == null) return;
            foreach (object c in kids)
            {
                DependencyObject cd = c as DependencyObject;
                if (cd != null) CollectMarked(cd, res);
            }
        }

        static void CollectAll(DependencyObject d, List<DependencyObject> res)
        {
            if (d == null) return;
            res.Add(d);
            System.Collections.IEnumerable kids = null;
            try { kids = LogicalTreeHelper.GetChildren(d); } catch { }
            if (kids == null) return;
            foreach (object c in kids)
            {
                DependencyObject cd = c as DependencyObject;
                if (cd != null) CollectAll(cd, res);
            }
        }

        static DependencyObject FindText(DependencyObject d, string text)
        {
            TextBlock t = d as TextBlock;
            if (t != null && t.Text == text) return d;

            System.Collections.IEnumerable kids = null;
            try { kids = LogicalTreeHelper.GetChildren(d); } catch { }
            if (kids == null) return null;
            foreach (object c in kids)
            {
                DependencyObject cd = c as DependencyObject;
                if (cd == null) continue;
                DependencyObject r = FindText(cd, text);
                if (r != null) return r;
            }
            return null;
        }

        static DependencyObject FindType(DependencyObject d, Type t)
        {
            if (t.IsInstanceOfType(d)) return d;
            System.Collections.IEnumerable kids = null;
            try { kids = LogicalTreeHelper.GetChildren(d); } catch { }
            if (kids == null) return null;
            foreach (object c in kids)
            {
                DependencyObject cd = c as DependencyObject;
                if (cd == null) continue;
                DependencyObject r = FindType(cd, t);
                if (r != null) return r;
            }
            return null;
        }

        public static void Run()
        {
            log.Add("莫印泯 自检  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            log.Add("");
            Flush();

            Shortcut seed = new Shortcut();
            seed.Name = "自检";
            seed.Url = "https://selfcheck.example.com";

            EditDialog d = new EditDialog(seed, null);
            Border root = d.Content as Border;
            if (root == null)
            {
                Fail("对话框根节点不是 Border");
                log.Add("");
                log.Add("结果：" + okCount + " 通过 / " + badCount + " 失败");
                Flush();
                return;
            }

            log.Add("① 「保存/取消」按钮能不能点动");
            List<DependencyObject> marked = new List<DependencyObject>();
            CollectMarked(root, marked);
            if (marked.Count == 0) Fail("一个可点击控件都没标记到（保存/取消 会点不动）");
            else Pass("标记到 " + marked.Count + " 个可点击控件（应为 2：保存、取消）");

            List<DependencyObject> probes = new List<DependencyObject>();
            foreach (DependencyObject m in marked)
            {
                probes.Add(m);
                Border bb = m as Border;
                if (bb != null && bb.Child != null) probes.Add(bb.Child);
            }
            int blocked = 0;
            foreach (DependencyObject p in probes) if (Ui.HitInteractive(p, root)) blocked++;
            if (probes.Count > 0 && blocked == probes.Count)
                Pass("全部 " + blocked + " 个都会「挡住拖动」→ 保存/取消 点得动");
            else
                Fail("有 " + (probes.Count - blocked) + " 个没挡住拖动 → 点了会被当成拖窗口");

            log.Add("");
            log.Add("② 窗口还能不能拖");
            DependencyObject title = FindText(root, "编辑入口");
            if (title == null) Fail("找不到标题文字");
            else if (!Ui.HitInteractive(title, root)) Pass("标题/空白处没被挡住 → 窗口拖得动");
            else Fail("标题被挡住了 → 窗口拖不动");

            DependencyObject tb = FindType(root, typeof(TextBox));
            if (tb == null) Fail("找不到输入框");
            else if (Ui.HitInteractive(tb, root)) Pass("输入框被正确挡住 → 能正常打字、选词");
            else Fail("输入框没挡住 → 点上去会变成拖窗口");

            log.Add("");
            log.Add("结果：" + okCount + " 通过 / " + badCount + " 失败");
            Flush();

            // 把「保存」按钮的坐标写出来，让外部脚本注入一次真实点击，做端到端验证
            d.Loaded += delegate
            {
                DispatcherTimer t = new DispatcherTimer();
                t.Interval = TimeSpan.FromMilliseconds(700);
                t.Tick += delegate
                {
                    t.Stop();
                    try
                    {
                        List<DependencyObject> all = new List<DependencyObject>();
                        CollectAll(root, all);
                        Border okb = null;
                        foreach (DependencyObject o in all)
                        {
                            Border bb = o as Border;
                            if (bb == null) continue;
                            if (!(bb.Tag is string) || ((string)bb.Tag) != "nograb") continue;
                            TextBlock tt = bb.Child as TextBlock;
                            if (tt != null && tt.Text == "保存") { okb = bb; break; }
                        }
                        if (okb == null) { log.Add("!! 找不到「保存」按钮"); Flush(); return; }

                        Point c = okb.TransformToAncestor(d).Transform(
                            new Point(okb.ActualWidth / 2.0, okb.ActualHeight / 2.0));
                        int h = new WindowInteropHelper(d).Handle.ToInt32();
                        log.Add("");
                        log.Add(">>>CLICK " + h + " " + (int)Math.Round(c.X) + " " + (int)Math.Round(c.Y));
                        Flush();
                    }
                    catch (Exception ex)
                    {
                        log.Add("!! 取坐标失败: " + ex.Message);
                        Flush();
                    }
                };
                t.Start();
            };

            d.ShowDialog();

            log.Add("");
            log.Add(">>>CLOSED 对话框已关闭；保存下来的条目 = " +
                (d.Result == null ? "（空）" : d.Result.Name + " / " + d.Result.Url));
            Flush();
        }
    }

    // ==================================================================
    public static class Program
    {
        static bool HasArg(string[] args, string name)
        {
            if (args == null) return false;
            foreach (string a in args)
                if (a != null && a.ToLower() == name) return true;
            return false;
        }

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                if (HasArg(args, "--check"))
                {
                    SelfCheck.Run();
                    app.Shutdown();
                    return;
                }

                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                MainWindow w = new MainWindow(args);
                app.MainWindow = w;
                app.Run(w);
            }
            catch (Exception ex)
            {
                try
                {
                    string f = Path.Combine(Path.GetTempPath(), "莫印泯-错误日志.txt");
                    File.WriteAllText(f,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n\r\n" + ex.ToString(),
                        new UTF8Encoding(true));
                }
                catch { }
                MessageBox.Show("程序出错了：\r\n\r\n" + ex.Message, "莫印泯",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
