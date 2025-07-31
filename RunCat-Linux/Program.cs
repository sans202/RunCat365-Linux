using System;
using Gtk;
using System.Diagnostics;

namespace RunCat_Linux
{
    class Program
    {
        static void Main(string[] args)
        {
            Application.Init();
            new TrayIcon();
            Application.Run();
        }
    }

    class TrayIcon
    {
        private StatusIcon _icon;
        private CpuMonitor _cpuMonitor;
        private string _theme;
        private int _currentFrame;
        private Gdk.Pixbuf[] _icons;

        public TrayIcon()
        {
            _cpuMonitor = new CpuMonitor();
            _theme = GetTheme();
            _icons = LoadIcons();
            _icon = new StatusIcon(_icons[0]);
            _icon.Visible = true;
            _icon.TooltipText = "RunCat Linux";
            _icon.PopupMenu += OnPopupMenu;

            var timer = new System.Threading.Timer(e => OnTimer(), null, 0, 200);
        }

        void OnTimer()
        {
            var usage = _cpuMonitor.GetUsage();
            _icon.TooltipText = $"CPU: {usage:F1}%";

            _currentFrame = (_currentFrame + 1) % _icons.Length;
            _icon.Pixbuf = _icons[_currentFrame];
        }

        void OnPopupMenu(object sender, PopupMenuArgs args)
        {
            Menu menu = new Menu();

            CheckMenuItem startupItem = new CheckMenuItem("Start at Login");
            startupItem.Activated += OnStartupClicked;
            startupItem.Active = IsStartupEnabled();
            menu.Add(startupItem);

            MenuItem quitItem = new MenuItem("Quit");
            quitItem.Activated += (s, a) => Application.Quit();
            menu.Add(quitItem);

            menu.ShowAll();
            menu.Popup();
        }

        private void OnStartupClicked(object sender, EventArgs e)
        {
            CheckMenuItem item = (CheckMenuItem)sender;
            if (item.Active)
            {
                EnableStartup();
            }
            else
            {
                DisableStartup();
            }
        }

        private bool IsStartupEnabled()
        {
            string desktopFilePath = GetDesktopFilePath();
            return System.IO.File.Exists(desktopFilePath);
        }

        private void EnableStartup()
        {
            string desktopFilePath = GetDesktopFilePath();
            string desktopFileContent = $@"
[Desktop Entry]
Type=Application
Name=RunCat Linux
Exec={Process.GetCurrentProcess().MainModule.FileName}
Icon=runcat
Terminal=false
";
            System.IO.File.WriteAllText(desktopFilePath, desktopFileContent);
        }

        private void DisableStartup()
        {
            string desktopFilePath = GetDesktopFilePath();
            System.IO.File.Delete(desktopFilePath);
        }

        private string GetDesktopFilePath()
        {
            string autostartDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config",
                "autostart"
            );
            if (!System.IO.Directory.Exists(autostartDir))
            {
                System.IO.Directory.CreateDirectory(autostartDir);
            }
            return System.IO.Path.Combine(autostartDir, "runcat-linux.desktop");
        }

        private string GetTheme()
        {
            try
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gsettings",
                        Arguments = "get org.cinnamon.desktop.interface gtk-theme",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                string theme = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit();
                return theme.ToLower().Contains("dark") ? "dark" : "light";
            }
            catch (Exception)
            {
                return "light";
            }
        }

        private Gdk.Pixbuf[] LoadIcons()
        {
            var icons = new Gdk.Pixbuf[5];
            for (int i = 0; i < 5; i++)
            {
                icons[i] = new Gdk.Pixbuf($"resources/{_theme}_cat_{i}.ico");
            }
            return icons;
        }
    }

    class CpuMonitor
    {
        private long _prevIdle;
        private long _prevTotal;

        public double GetUsage()
        {
            var lines = System.IO.File.ReadAllLines("/proc/stat");
            var cpuLine = lines[0];
            var values = cpuLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            long user = long.Parse(values[1]);
            long nice = long.Parse(values[2]);
            long system = long.Parse(values[3]);
            long idle = long.Parse(values[4]);
            long iowait = long.Parse(values[5]);
            long irq = long.Parse(values[6]);
            long softirq = long.Parse(values[7]);
            long steal = long.Parse(values[8]);

            long currentIdle = idle + iowait;
            long nonIdle = user + nice + system + irq + softirq + steal;
            long currentTotal = currentIdle + nonIdle;

            double totald = currentTotal - _prevTotal;
            double idled = currentIdle - _prevIdle;

            _prevTotal = currentTotal;
            _prevIdle = currentIdle;

            return 100.0 * (totald - idled) / totald;
        }
    }
}
