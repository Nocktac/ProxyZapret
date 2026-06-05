using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProxyZapret
{
    internal static class Program
    {
        private const string AppUserModelId = "Nocktac.ProxyZapret.Client";

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

        [STAThread]
        private static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
            ShortcutManager.RefreshInstalledShortcuts();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!args.Any(argument => argument.StartsWith("--")))
            {
                try
                {
                    if (UpdateManager.TryStartUpdateWithUi())
                        return;
                }
                catch (Exception exception)
                {
                    UpdateManager.WriteUpdateLog(exception);
                }
            }

            if (args.Contains("--self-test"))
            {
                try
                {
                    ClientController.RunSelfTest();
                    Console.WriteLine("ProxyZapret self-test passed.");
                    Environment.Exit(0);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception);
                    File.WriteAllText(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime", "self-test.error.log"),
                        exception.ToString()
                    );
                    Environment.Exit(1);
                }
            }

            if (args.Contains("--subscription-test"))
            {
                try
                {
                    ClientController.RunSubscriptionTest();
                    Console.WriteLine("ProxyZapret subscription test passed.");
                    Environment.Exit(0);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception);
                    File.WriteAllText(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime", "subscription-test.error.log"),
                        exception.ToString()
                    );
                    Environment.Exit(1);
                }
            }

            if (args.Contains("--tunnel-test"))
            {
                try
                {
                    ClientController.RunTunnelTest();
                    Console.WriteLine("ProxyZapret tunnel test passed.");
                    Environment.Exit(0);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception);
                    File.WriteAllText(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime", "tunnel-test.error.log"),
                        exception.ToString()
                    );
                    Environment.Exit(1);
                }
            }

            if (args.Contains("--udp-proxy-test"))
            {
                try
                {
                    ClientController.RunUdpProxyTest();
                    Console.WriteLine("ProxyZapret UDP proxy test passed.");
                    Environment.Exit(0);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception);
                    File.WriteAllText(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime", "udp-proxy-test.error.log"),
                        exception.ToString()
                    );
                    Environment.Exit(1);
                }
            }

            if (args.Contains("--ui-preview"))
            {
                using (var preview = new MainForm(new ClientController()))
                using (var bitmap = new Bitmap(preview.Width, preview.Height))
                {
                    preview.Show();
                    preview.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                    bitmap.Save(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime", "ui-preview.png"),
                        System.Drawing.Imaging.ImageFormat.Png
                    );
                    preview.Dispose();
                }
                Environment.Exit(0);
            }

            if (args.Contains("--update-preview"))
            {
                using (var preview = new UpdateForm())
                using (var bitmap = new Bitmap(preview.Width, preview.Height))
                {
                    preview.Show();
                    preview.SetProgress("Downloading ProxyZapret " + UpdateManager.Version + "...", 58);
                    preview.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                    bitmap.Save(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime", "update-preview.png"),
                        System.Drawing.Imaging.ImageFormat.Png
                    );
                    preview.Dispose();
                }
                Environment.Exit(0);
            }

            bool created;
            using (var mutex = new System.Threading.Mutex(true, "ProxyZapret.SingleInstance", out created))
            {
                if (!created)
                {
                    MessageBox.Show("ProxyZapret is already running.", "ProxyZapret");
                    return;
                }

                var form = new MainForm(new ClientController());
                if (args.Contains("--smoke-test"))
                {
                    var timer = new Timer { Interval = 1500 };
                    timer.Tick += delegate { timer.Stop(); form.Dispose(); Application.Exit(); };
                    timer.Start();
                }
                Application.Run(form);
            }
        }
    }

    internal static class ShortcutManager
    {
        public static void RefreshInstalledShortcuts()
        {
            try
            {
                var root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
                if (File.Exists(Path.Combine(root, "config", "settings.local.json")))
                    return;

                var executable = Application.ExecutablePath;
                var iconPath = EnsureShortcutIcon(root);
                foreach (var path in GetShortcutPaths())
                    RefreshShortcut(path, executable, iconPath);
            }
            catch { }
        }

        private static string EnsureShortcutIcon(string root)
        {
            var iconPath = Path.Combine(root, "ProxyZapret-" + UpdateManager.Version + ".ico");
            if (!File.Exists(iconPath))
                BrandIconRenderer.SaveMultiSizeIcon(iconPath);

            foreach (var oldIcon in Directory.GetFiles(root, "ProxyZapret-*.ico"))
            {
                if (!String.Equals(oldIcon, iconPath, StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(oldIcon); }
                    catch { }
                }
            }

            return iconPath;
        }

        private static IEnumerable<string> GetShortcutPaths()
        {
            var names = new[] { "ProxyZapret.lnk" };
            var folders = new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Internet Explorer", "Quick Launch", "User Pinned", "TaskBar")
            };

            foreach (var folder in folders)
            {
                if (String.IsNullOrWhiteSpace(folder)) continue;
                foreach (var name in names)
                    yield return Path.Combine(folder, name);
            }
        }

        private static void RefreshShortcut(string path, string executable, string iconPath)
        {
            if (!File.Exists(path)) return;

            object shell = null;
            object shortcut = null;
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;

                shell = Activator.CreateInstance(shellType);
                shortcut = shellType.InvokeMember(
                    "CreateShortcut",
                    System.Reflection.BindingFlags.InvokeMethod,
                    null,
                    shell,
                    new object[] { path }
                );
                var shortcutType = shortcut.GetType();
                shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { executable });
                shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(executable) });
                shortcutType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { iconPath + ",0" });
                shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { "ProxyZapret" });
                shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
            }
            finally
            {
                if (shortcut != null && Marshal.IsComObject(shortcut)) Marshal.ReleaseComObject(shortcut);
                if (shell != null && Marshal.IsComObject(shell)) Marshal.ReleaseComObject(shell);
            }
        }
    }

    internal sealed class UpdateManifest
    {
        public string version { get; set; }
        public string url { get; set; }
        public string sha256 { get; set; }
        public string updaterUrl { get; set; }
        public string updaterSha256 { get; set; }
    }

    internal static class UpdateManager
    {
        private const string CurrentVersion = "0.5.0";

        public static string Version
        {
            get { return CurrentVersion; }
        }

        public static bool TryStartUpdateWithUi()
        {
            using (var form = new UpdateForm())
            {
                form.Show();
                form.Refresh();
                return TryStartUpdate(delegate(string text, int percent)
                {
                    form.SetProgress(text, percent);
                    Application.DoEvents();
                });
            }
        }

        public static bool TryStartUpdate(Action<string, int> progress)
        {
            Report(progress, "Checking for updates...", 8);
            var root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var writableRoot = GetWritableRoot();
            var settingsPath = Path.Combine(writableRoot, "config", "settings.local.json");
            var updaterPath = Path.Combine(root, "ProxyZapret.Updater.exe");
            if (!File.Exists(settingsPath) || !File.Exists(updaterPath))
                return false;

            var json = new JavaScriptSerializer();
            var settings = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(settingsPath, Encoding.UTF8));
            object rawManifestUrl;
            if (!settings.TryGetValue("updateManifestUrl", out rawManifestUrl))
                return false;
            var manifestUrl = Convert.ToString(rawManifestUrl);
            if (String.IsNullOrWhiteSpace(manifestUrl))
                return false;

            UpdateManifest manifest;
            using (var client = CreateUpdateClient())
            {
                client.Headers["User-Agent"] = "ProxyZapret-Updater/" + CurrentVersion;
                manifest = json.Deserialize<UpdateManifest>(client.DownloadString(manifestUrl));
            }

            ValidateManifest(manifest);
            if (CompareVersions(manifest.version, CurrentVersion) <= 0)
                return false;

            Report(progress, "Downloading ProxyZapret " + manifest.version + "...", 28);
            var updateDirectory = Path.Combine(writableRoot, "runtime", "update");
            Directory.CreateDirectory(updateDirectory);
            var downloaded = Path.Combine(updateDirectory, "ProxyZapret.exe.download");
            using (var client = CreateUpdateClient())
            {
                client.Headers["User-Agent"] = "ProxyZapret-Updater/" + CurrentVersion;
                client.DownloadFile(manifest.url, downloaded);
            }

            Report(progress, "Verifying update package...", 68);
            var actualHash = ComputeSha256(downloaded);
            if (!String.Equals(actualHash, manifest.sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(downloaded);
                throw new InvalidOperationException("Downloaded update SHA-256 does not match the release manifest.");
            }

            Report(progress, "Preparing updater...", 82);
            UpdateUpdater(manifest, updaterPath, updateDirectory);
            Report(progress, "Restarting to apply update...", 96);
            Process.Start(new ProcessStartInfo
            {
                FileName = updaterPath,
                Arguments = String.Join(" ", new[] {
                    Process.GetCurrentProcess().Id.ToString(),
                    Quote(downloaded),
                    Quote(Application.ExecutablePath),
                    Quote("--no-update")
                }),
                WorkingDirectory = root,
                UseShellExecute = true
            });
            System.Threading.Thread.Sleep(600);
            return true;
        }

        public static void WriteUpdateLog(Exception exception)
        {
            try
            {
                var runtime = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime");
                runtime = Path.Combine(GetWritableRoot(), "runtime");
                Directory.CreateDirectory(runtime);
                File.WriteAllText(Path.Combine(runtime, "update.error.log"), exception.ToString());
            }
            catch { }
        }

        private static void ValidateManifest(UpdateManifest manifest)
        {
            if (manifest == null || String.IsNullOrWhiteSpace(manifest.version) ||
                String.IsNullOrWhiteSpace(manifest.url) || String.IsNullOrWhiteSpace(manifest.sha256))
                throw new InvalidOperationException("Update manifest is incomplete.");
            if (manifest.sha256.Length != 64 || !manifest.sha256.All(Uri.IsHexDigit))
                throw new InvalidOperationException("Update manifest contains an invalid SHA-256 hash.");
            if (String.IsNullOrWhiteSpace(manifest.updaterUrl) != String.IsNullOrWhiteSpace(manifest.updaterSha256))
                throw new InvalidOperationException("Update manifest contains an incomplete updater package.");
            if (!String.IsNullOrWhiteSpace(manifest.updaterSha256) &&
                (manifest.updaterSha256.Length != 64 || !manifest.updaterSha256.All(Uri.IsHexDigit)))
                throw new InvalidOperationException("Update manifest contains an invalid updater SHA-256 hash.");
        }

        private static void Report(Action<string, int> progress, string text, int percent)
        {
            if (progress != null) progress(text, percent);
        }

        private static void UpdateUpdater(UpdateManifest manifest, string updaterPath, string updateDirectory)
        {
            if (String.IsNullOrWhiteSpace(manifest.updaterUrl) ||
                String.Equals(ComputeSha256(updaterPath), manifest.updaterSha256, StringComparison.OrdinalIgnoreCase))
                return;

            var downloaded = Path.Combine(updateDirectory, "ProxyZapret.Updater.exe.download");
            using (var client = CreateUpdateClient())
            {
                client.Headers["User-Agent"] = "ProxyZapret-Updater/" + CurrentVersion;
                client.DownloadFile(manifest.updaterUrl, downloaded);
            }
            if (!String.Equals(ComputeSha256(downloaded), manifest.updaterSha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(downloaded);
                throw new InvalidOperationException("Downloaded updater SHA-256 does not match the release manifest.");
            }

            var backup = updaterPath + ".previous";
            if (File.Exists(backup)) File.Delete(backup);
            File.Copy(updaterPath, backup, true);
            try
            {
                File.Copy(downloaded, updaterPath, true);
                File.Delete(downloaded);
            }
            catch
            {
                File.Copy(backup, updaterPath, true);
                throw;
            }
        }

        private static int CompareVersions(string left, string right)
        {
            System.Version leftVersion;
            System.Version rightVersion;
            if (!System.Version.TryParse(left, out leftVersion) || !System.Version.TryParse(right, out rightVersion))
                throw new InvalidOperationException("Update manifest contains an invalid version.");
            return leftVersion.CompareTo(rightVersion);
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static WebClient CreateUpdateClient()
        {
            var client = new WebClient();
            client.Proxy = null;
            return client;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string GetWritableRoot()
        {
            var root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            if (File.Exists(Path.Combine(root, "config", "settings.local.json")))
                return root;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ProxyZapret"
            );
        }
    }

    internal sealed class UpdateForm : Form
    {
        private readonly Label detail;
        private readonly CrispProgressBar progressBar;
        private readonly Color background = Color.FromArgb(14, 18, 27);
        private readonly Color card = Color.FromArgb(24, 31, 45);
        private readonly Color muted = Color.FromArgb(145, 157, 178);
        private readonly Color accent = Color.FromArgb(67, 211, 164);

        public UpdateForm()
        {
            Text = "ProxyZapret Update";
            ClientSize = new Size(420, 230);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = background;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            Padding = new Padding(1);

            var title = new Label
            {
                Text = "Updating ProxyZapret",
                Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = background,
                Location = new Point(34, 34)
            };
            Controls.Add(title);

            var version = new Label
            {
                Text = "current v" + UpdateManager.Version,
                Font = new Font("Segoe UI", 9F),
                ForeColor = accent,
                AutoSize = true,
                BackColor = background,
                Location = new Point(36, 73)
            };
            Controls.Add(version);

            var panel = new UpdatePanel
            {
                Location = new Point(34, 112),
                Size = new Size(352, 82),
                BackColor = background
            };
            Controls.Add(panel);

            detail = new Label
            {
                Text = "Checking for updates...",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(225, 231, 240),
                AutoSize = false,
                Size = new Size(308, 22),
                Location = new Point(22, 17),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(detail);

            progressBar = new CrispProgressBar
            {
                Value = 5,
                Size = new Size(308, 18),
                Location = new Point(22, 48)
            };
            panel.Controls.Add(progressBar);
        }

        public void SetProgress(string text, int percent)
        {
            detail.Text = text;
            progressBar.Value = percent;
            Refresh();
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            base.OnPaint(eventArgs);
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.FromArgb(50, 60, 78)))
                eventArgs.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly ClientController controller;
        private readonly Label status;
        private readonly Label statusDetail;
        private readonly RoundButton toggle;
        private readonly NotifyIcon tray;
        private readonly Timer refreshTimer;
        private readonly StatusPanel statusPanel;
        private readonly Color background = Color.FromArgb(14, 18, 27);
        private readonly Color card = Color.FromArgb(24, 31, 45);
        private readonly Color muted = Color.FromArgb(145, 157, 178);
        private readonly Color accent = Color.FromArgb(67, 211, 164);
        private readonly Icon brandIcon;
        private readonly Icon taskbarIcon;
        private readonly Icon trayIcon;

        public MainForm(ClientController controller)
        {
            this.controller = controller;
            brandIcon = BrandIconRenderer.CreateIcon(32);
            taskbarIcon = BrandIconRenderer.CreateIcon(48);
            trayIcon = BrandIconRenderer.CreateIcon(16);
            Text = "ProxyZapret";
            ClientSize = new Size(440, 540);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            BackColor = background;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            Icon = brandIcon;
            Padding = new Padding(1);

            var titleBar = new Panel
            {
                Location = new Point(1, 1),
                Size = new Size(438, 38),
                BackColor = Color.FromArgb(21, 26, 37)
            };
            titleBar.MouseDown += DragWindow;
            Controls.Add(titleBar);

            var windowIcon = new PictureBox
            {
                Image = BrandIconRenderer.CreateBitmap(18),
                SizeMode = PictureBoxSizeMode.Normal,
                Location = new Point(10, 9),
                Size = new Size(18, 18),
                BackColor = titleBar.BackColor
            };
            windowIcon.MouseDown += DragWindow;
            titleBar.Controls.Add(windowIcon);

            var windowTitle = new Label
            {
                Text = "ProxyZapret " + UpdateManager.Version,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(209, 217, 230),
                AutoSize = true,
                BackColor = titleBar.BackColor,
                Location = new Point(36, 10)
            };
            windowTitle.MouseDown += DragWindow;
            titleBar.Controls.Add(windowTitle);

            var minimize = CreateCaptionButton("_", 360, titleBar.BackColor, Color.FromArgb(225, 231, 240));
            minimize.Text = "_";
            minimize.Location = new Point(360, 1);
            minimize.Click += delegate { WindowState = FormWindowState.Minimized; };
            titleBar.Controls.Add(minimize);

            var close = CreateCaptionButton("X", 398, titleBar.BackColor, Color.FromArgb(225, 231, 240));
            close.Text = "X";
            close.Location = new Point(398, 1);
            close.FlatAppearance.MouseOverBackColor = Color.FromArgb(192, 64, 72);
            close.Click += delegate { Hide(); };
            titleBar.Controls.Add(close);

            var brand = new BrandPanel
            {
                Location = new Point(34, 64),
                Size = new Size(372, 66),
                BackColor = background
            };
            Controls.Add(brand);

            var title = new Label
            {
                Text = "ProxyZapret",
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = background,
                Location = new Point(104, 68)
            };
            Controls.Add(title);

            var version = new Label
            {
                Text = "v" + UpdateManager.Version,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(67, 211, 164),
                AutoSize = true,
                BackColor = background,
                Location = new Point(300, 80)
            };
            Controls.Add(version);

            var subtitle = new Label
            {
                Text = "Selective access for Windows",
                Font = new Font("Segoe UI", 9F),
                ForeColor = muted,
                AutoSize = true,
                BackColor = background,
                Location = new Point(106, 104)
            };
            Controls.Add(subtitle);

            statusPanel = new StatusPanel
            {
                Location = new Point(34, 154),
                Size = new Size(372, 190),
                BackColor = background
            };
            Controls.Add(statusPanel);

            status = new Label
            {
                Text = "Protection is off",
                Font = new Font("Segoe UI Semibold", 16, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(320, 32),
                BackColor = Color.Transparent,
                Location = new Point(26, 124)
            };
            statusPanel.Controls.Add(status);

            statusDetail = new Label
            {
                Text = "Blocked resources will open directly",
                Font = new Font("Segoe UI", 9F),
                ForeColor = muted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(320, 24),
                BackColor = Color.Transparent,
                Location = new Point(26, 154)
            };
            statusPanel.Controls.Add(statusDetail);

            toggle = new RoundButton
            {
                Text = "TURN ON",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = background,
                BackColor = accent,
                Cursor = Cursors.Hand,
                Size = new Size(252, 54),
                Location = new Point(94, 374),
                Radius = 14
            };
            toggle.FlatAppearance.BorderSize = 0;
            toggle.Click += ToggleClick;
            Controls.Add(toggle);

            var footer = new Label
            {
                Text = "Version " + UpdateManager.Version + " - only restricted services are proxied",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = muted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(390, 28),
                Location = new Point(25, 466),
                BackColor = background
            };
            footer.Text = "Version " + UpdateManager.Version + " - only restricted services are proxied";
            Controls.Add(footer);

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, delegate { ShowWindow(); });
            menu.Items.Add("Exit", null, delegate { ExitApplication(); });

            tray = new NotifyIcon
            {
                Icon = trayIcon,
                Text = "ProxyZapret",
                Visible = true,
                ContextMenuStrip = menu
            };
            tray.DoubleClick += delegate { ShowWindow(); };

            FormClosing += delegate(object sender, FormClosingEventArgs eventArgs)
            {
                if (eventArgs.CloseReason == CloseReason.UserClosing)
                {
                    eventArgs.Cancel = true;
                    Hide();
                }
            };

            refreshTimer = new Timer { Interval = 60000 };
            refreshTimer.Tick += delegate
            {
                try { controller.RefreshIfDue(); }
                catch { /* Keep the last-known-good config active. */ }
                UpdateUi();
            };
            refreshTimer.Start();
            UpdateUi();
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr windowHandle, int message, int parameter, int value);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr parameter, IntPtr value);

        protected override void OnHandleCreated(EventArgs eventArgs)
        {
            base.OnHandleCreated(eventArgs);
            ApplyWindowIcons();
        }

        private void ApplyWindowIcons()
        {
            const int wmSetIcon = 0x80;
            SendMessage(Handle, wmSetIcon, new IntPtr(0), trayIcon.Handle);
            SendMessage(Handle, wmSetIcon, new IntPtr(1), taskbarIcon.Handle);
        }

        private Button CreateCaptionButton(string text, int x, Color baseColor, Color textColor)
        {
            var button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12F),
                ForeColor = textColor,
                BackColor = baseColor,
                TabStop = false,
                Size = new Size(36, 34),
                Location = new Point(x, 1),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(48, 56, 72);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(58, 68, 86);
            return button;
        }

        private void DragWindow(object sender, MouseEventArgs eventArgs)
        {
            if (eventArgs.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, 0xA1, 0x2, 0);
        }

        private void ToggleClick(object sender, EventArgs eventArgs)
        {
            var wasRunning = controller.IsRunning;
            try
            {
                toggle.Enabled = false;
                status.Text = controller.IsRunning ? "Disconnecting..." : "Connecting...";
                Application.DoEvents();
                if (controller.IsRunning) controller.Stop();
                else controller.Start();
                if (!wasRunning && controller.IsRunning)
                {
                    UpdateUi();
                    Hide();
                    tray.ShowBalloonTip(1500, "ProxyZapret", "Connected. The app is running in the tray.", ToolTipIcon.Info);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "ProxyZapret", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                toggle.Enabled = true;
                UpdateUi();
            }
        }

        private void UpdateUi()
        {
            if (controller.IsRunning)
            {
                status.Text = "Protection is active";
                statusDetail.Text = "Restricted services use the secure route";
                status.ForeColor = Color.White;
                toggle.Text = "TURN OFF";
                toggle.BackColor = Color.FromArgb(34, 44, 62);
                toggle.ForeColor = Color.FromArgb(225, 231, 240);
                tray.Text = "ProxyZapret: connected";
                statusPanel.Active = true;
            }
            else
            {
                status.Text = "Protection is off";
                statusDetail.Text = "Turn on to unlock restricted services";
                status.ForeColor = Color.White;
                toggle.Text = "TURN ON";
                toggle.BackColor = accent;
                toggle.ForeColor = background;
                tray.Text = "ProxyZapret: off";
                statusPanel.Active = false;
            }
            statusPanel.Invalidate();
        }

        private void ShowWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            controller.Stop();
            tray.Visible = false;
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (tray != null) tray.Dispose();
                if (brandIcon != null) brandIcon.Dispose();
                if (taskbarIcon != null) taskbarIcon.Dispose();
                if (trayIcon != null) trayIcon.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            base.OnPaint(eventArgs);
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.FromArgb(50, 60, 78)))
                eventArgs.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }

    internal sealed class UpdatePanel : Panel
    {
        public UpdatePanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            base.OnPaint(eventArgs);
            var graphics = eventArgs.Graphics;
            graphics.SmoothingMode = SmoothingMode.None;
            using (var brush = new SolidBrush(Color.FromArgb(24, 31, 45)))
                graphics.FillRectangle(brush, 0, 0, Width, Height);
            using (var pen = new Pen(Color.FromArgb(36, 46, 64)))
                graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }

    internal sealed class CrispProgressBar : Control
    {
        private int value;

        public int Value
        {
            get { return value; }
            set
            {
                this.value = Math.Max(0, Math.Min(100, value));
                Invalidate();
            }
        }

        public CrispProgressBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            value = 0;
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            base.OnPaint(eventArgs);
            var graphics = eventArgs.Graphics;
            graphics.SmoothingMode = SmoothingMode.None;
            using (var background = new SolidBrush(Color.FromArgb(36, 46, 64)))
                graphics.FillRectangle(background, 0, 0, Width, Height);
            var fillWidth = Math.Max(2, (Width * value) / 100);
            using (var fill = new SolidBrush(Color.FromArgb(67, 211, 164)))
                graphics.FillRectangle(fill, 0, 0, fillWidth, Height);
            using (var pen = new Pen(Color.FromArgb(55, 68, 90)))
                graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }

    internal static class BrandIconRenderer
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static Icon CreateIcon(int size)
        {
            using (var bitmap = CreateBitmap(size))
            {
                var handle = bitmap.GetHicon();
                try
                {
                    using (var icon = Icon.FromHandle(handle))
                        return (Icon)icon.Clone();
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }

        public static Bitmap CreateBitmap(int size)
        {
            var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.Clear(Color.Transparent);
                Draw(graphics, new RectangleF(0, 0, size, size), size >= 24, true);
            }
            return bitmap;
        }

        public static void SaveMultiSizeIcon(string path)
        {
            var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
            var images = new List<byte[]>();
            foreach (var size in sizes)
            {
                using (var bitmap = CreateBitmap(size))
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    images.Add(stream.ToArray());
                }
            }

            using (var stream = File.Create(path))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)images.Count);

                var offset = 6 + (16 * images.Count);
                for (var index = 0; index < images.Count; index++)
                {
                    var size = sizes[index];
                    var data = images[index];
                    writer.Write((byte)(size == 256 ? 0 : size));
                    writer.Write((byte)(size == 256 ? 0 : size));
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)32);
                    writer.Write((uint)data.Length);
                    writer.Write((uint)offset);
                    offset += data.Length;
                }

                foreach (var data in images)
                    writer.Write(data);
            }
        }

        public static void Draw(Graphics graphics, RectangleF bounds, bool drawCheck)
        {
            Draw(graphics, bounds, drawCheck, true);
        }

        public static void Draw(Graphics graphics, RectangleF bounds, bool drawCheck, bool active)
        {
            var scale = Math.Min(bounds.Width, bounds.Height) / 256F;
            var offsetX = bounds.X + (bounds.Width - (256F * scale)) / 2F;
            var offsetY = bounds.Y + (bounds.Height - (256F * scale)) / 2F;
            var topColor = active ? Color.FromArgb(255, 77, 211, 168) : Color.FromArgb(255, 108, 122, 146);
            var bottomColor = active ? Color.FromArgb(255, 55, 126, 255) : Color.FromArgb(255, 65, 78, 104);
            var ringColor = active ? Color.FromArgb(130, 91, 166, 255) : Color.FromArgb(105, 94, 111, 140);
            var borderColor = active ? Color.FromArgb(235, 238, 246, 255) : Color.FromArgb(210, 163, 174, 196);

            using (var backgroundPath = UiDrawing.RoundedRectangleF(
                new RectangleF(offsetX + 18F * scale, offsetY + 18F * scale, 220F * scale, 220F * scale),
                48F * scale
            ))
            using (var background = new LinearGradientBrush(
                new RectangleF(offsetX + 18F * scale, offsetY + 18F * scale, 220F * scale, 220F * scale),
                Color.FromArgb(255, 18, 25, 38),
                Color.FromArgb(255, 8, 13, 24),
                LinearGradientMode.ForwardDiagonal
            ))
            using (var ring = new Pen(ringColor, Math.Max(1F, 7F * scale)))
            {
                ring.LineJoin = LineJoin.Round;
                graphics.FillPath(background, backgroundPath);
                graphics.DrawPath(ring, backgroundPath);
            }

            var shieldPoints = new[] {
                new PointF(offsetX + 128F * scale, offsetY + 55F * scale),
                new PointF(offsetX + 184F * scale, offsetY + 77F * scale),
                new PointF(offsetX + 174F * scale, offsetY + 158F * scale),
                new PointF(offsetX + 128F * scale, offsetY + 205F * scale),
                new PointF(offsetX + 82F * scale, offsetY + 158F * scale),
                new PointF(offsetX + 72F * scale, offsetY + 77F * scale)
            };

            using (var shield = new GraphicsPath())
            {
                shield.AddPolygon(shieldPoints);
                using (var fill = new LinearGradientBrush(
                    new RectangleF(offsetX + 70F * scale, offsetY + 50F * scale, 116F * scale, 160F * scale),
                    topColor,
                    bottomColor,
                    LinearGradientMode.ForwardDiagonal
                ))
                using (var border = new Pen(borderColor, Math.Max(1.4F, 9F * scale)))
                {
                    border.LineJoin = LineJoin.Round;
                    graphics.FillPath(fill, shield);
                    graphics.DrawPath(border, shield);
                }
            }

            if (!drawCheck) return;

            using (var check = new Pen(active ? Color.FromArgb(255, 245, 250, 255) : Color.FromArgb(255, 205, 214, 232), Math.Max(1.8F, 13F * scale)))
            {
                check.StartCap = LineCap.Round;
                check.EndCap = LineCap.Round;
                check.LineJoin = LineJoin.Round;
                if (active)
                {
                    graphics.DrawLines(check, new[] {
                        new PointF(offsetX + 101F * scale, offsetY + 133F * scale),
                        new PointF(offsetX + 121F * scale, offsetY + 153F * scale),
                        new PointF(offsetX + 158F * scale, offsetY + 109F * scale)
                    });
                }
                else
                {
                    graphics.DrawLine(check, offsetX + 101F * scale, offsetY + 128F * scale, offsetX + 155F * scale, offsetY + 128F * scale);
                }
            }
        }
    }

    internal sealed class BrandPanel : Panel
    {
        public BrandPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            base.OnPaint(eventArgs);
            var graphics = eventArgs.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            BrandIconRenderer.Draw(graphics, new RectangleF(0, 5, 56, 56), true);
        }
    }

    internal sealed class RoundButton : Button
    {
        public int Radius { get; set; }

        public RoundButton()
        {
            Radius = 12;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            var graphics = eventArgs.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = UiDrawing.RoundedRectangle(bounds, Radius))
            using (var brush = new SolidBrush(BackColor))
                graphics.FillPath(brush, path);
            TextRenderer.DrawText(
                graphics,
                Text,
                Font,
                bounds,
                Enabled ? ForeColor : Color.FromArgb(130, 140, 155),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            );
        }
    }

    internal sealed class StatusPanel : Panel
    {
        public bool Active { get; set; }

        public StatusPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            base.OnPaint(eventArgs);
            var graphics = eventArgs.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = UiDrawing.RoundedRectangle(bounds, 18))
            using (var brush = new LinearGradientBrush(
                bounds,
                Active ? Color.FromArgb(27, 42, 58) : Color.FromArgb(24, 31, 45),
                Active ? Color.FromArgb(20, 31, 48) : Color.FromArgb(19, 25, 37),
                LinearGradientMode.ForwardDiagonal
            ))
            using (var border = new Pen(Active ? Color.FromArgb(64, 157, 209, 182) : Color.FromArgb(42, 53, 72)))
            {
                graphics.FillPath(brush, path);
                graphics.DrawPath(border, path);
            }

            var centerX = Width / 2;
            var centerY = 58;
            BrandIconRenderer.Draw(graphics, new RectangleF(centerX - 42, centerY - 42, 84, 84), true, Active);

            var badgeText = Active ? "SECURE ROUTE" : "STANDBY";
            var badgeColor = Active ? Color.FromArgb(67, 211, 164) : Color.FromArgb(128, 142, 166);
            var badgeBounds = new Rectangle(centerX - 56, centerY + 39, 112, 24);
            using (var badgePath = UiDrawing.RoundedRectangle(badgeBounds, 12))
            using (var badgeBrush = new SolidBrush(Active ? Color.FromArgb(31, 72, 62) : Color.FromArgb(37, 47, 65)))
            using (var badgePen = new Pen(Active ? Color.FromArgb(68, 99, 213, 176) : Color.FromArgb(55, 68, 90)))
            {
                graphics.FillPath(badgeBrush, badgePath);
                graphics.DrawPath(badgePen, badgePath);
            }
            using (var badgeFont = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold))
            {
                TextRenderer.DrawText(
                    graphics,
                    badgeText,
                    badgeFont,
                    badgeBounds,
                    badgeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );
            }
        }
    }

    internal static class UiDrawing
    {
        public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static GraphicsPath RoundedRectangleF(RectangleF bounds, float radius)
        {
            var diameter = radius * 2F;
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class ClientController
    {
        private readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        private readonly string root;
        private readonly string runtime;
        private readonly string core;
        private readonly string settingsPath;
        private readonly string settingsExamplePath;
        private readonly string rulesPath;
        private readonly string sampleSubscriptionPath;
        private readonly string cachePath;
        private readonly string base64CachePath;
        private readonly string statePath;
        private readonly string generatedConfigPath;
        private readonly string rulesCachePath;
        private List<Dictionary<string, object>> preferredUriNodes = new List<Dictionary<string, object>>();
        private readonly object logLock = new object();
        private Process coreProcess;

        public ClientController()
        {
            root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var writableRoot = GetWritableRoot(root);
            runtime = Path.Combine(writableRoot, "runtime");
            core = Path.Combine(root, "core", "sing-box.exe");
            settingsPath = Path.Combine(writableRoot, "config", "settings.local.json");
            settingsExamplePath = Path.Combine(root, "config", "settings.example.json");
            rulesPath = Path.Combine(root, "config", "routing-rules.json");
            sampleSubscriptionPath = Path.Combine(root, "config", "sample-subscription.json");
            cachePath = Path.Combine(runtime, "subscription-cache.json");
            base64CachePath = Path.Combine(runtime, "subscription-cache.base64");
            statePath = Path.Combine(runtime, "state.json");
            generatedConfigPath = Path.Combine(runtime, "sing-box.generated.json");
            rulesCachePath = Path.Combine(runtime, "rules-cache.db");
            Initialize();
        }

        private static string GetWritableRoot(string root)
        {
            if (File.Exists(Path.Combine(root, "config", "settings.local.json")))
                return root;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ProxyZapret"
            );
        }

        public bool IsRunning
        {
            get
            {
                if (coreProcess == null) return false;
                try { return !coreProcess.HasExited; }
                catch { return false; }
            }
        }

        public void Start()
        {
            if (IsRunning) return;
            var settings = LoadObject(settingsPath);
            var subscription = LoadSubscription(settings, true);
            var config = BuildConfig(settings, subscription);
            SaveJson(generatedConfigPath, config);
            CheckConfig(generatedConfigPath);
            StartCore(generatedConfigPath);
        }

        public void Stop()
        {
            if (!IsRunning) return;
            try
            {
                coreProcess.Kill();
                coreProcess.WaitForExit(5000);
            }
            finally
            {
                coreProcess.Dispose();
                coreProcess = null;
            }
        }

        public void RefreshIfDue()
        {
            if (!IsRunning) return;
            var settings = LoadObject(settingsPath);
            var state = LoadObject(statePath);
            var lastRefresh = DateTime.MinValue;
            object rawRefresh;
            if (state.TryGetValue("lastSubscriptionRefreshUtc", out rawRefresh) && rawRefresh != null)
                DateTime.TryParse(Convert.ToString(rawRefresh), out lastRefresh);
            var minutes = Convert.ToInt32(settings["subscriptionRefreshMinutes"]);
            if (DateTime.UtcNow < lastRefresh.ToUniversalTime().AddMinutes(minutes)) return;

            var subscription = LoadSubscription(settings, false);
            var config = BuildConfig(settings, subscription);
            var candidate = Path.Combine(runtime, "sing-box.candidate.json");
            SaveJson(candidate, config);
            CheckConfig(candidate);
            if (File.ReadAllText(candidate) == File.ReadAllText(generatedConfigPath))
            {
                File.Delete(candidate);
                return;
            }

            File.Copy(candidate, generatedConfigPath, true);
            File.Delete(candidate);
            Stop();
            StartCore(generatedConfigPath);
        }

        public static void RunSelfTest()
        {
            var controller = new ClientController();
            var settings = controller.LoadObject(controller.settingsExamplePath);
            var subscription = controller.LoadObject(controller.sampleSubscriptionPath);
            var config = controller.BuildConfig(settings, subscription);
            controller.SaveJson(controller.generatedConfigPath, config);
            controller.CheckConfig(controller.generatedConfigPath);

            var route = AsObject(config["route"]);
            if (Convert.ToString(route["final"]) != "direct")
                throw new InvalidOperationException("Default route must remain direct.");
            var outbounds = AsList(config["outbounds"]);
            if (outbounds.Count != 4)
                throw new InvalidOperationException("Expected direct, primary, backup, and urltest outbounds.");
            if (((ICollection)route["rule_set"]).Count != 13)
                throw new InvalidOperationException("Expected complete and service-specific rule sets.");

            var uriSample = String.Join("\n", new[] {
                "ss://YWVzLTEyOC1nY206cGFzc3dvcmQ@127.0.0.1:8388#SS",
                "trojan://password@127.0.0.1:443?security=tls&sni=example.com#Trojan",
                "vless://00000000-0000-0000-0000-000000000000@127.0.0.1:443?security=reality&sni=example.com&pbk=public-key&sid=abcd&fp=chrome#Reality",
                "hysteria2://password@127.0.0.1:443?sni=example.com#Hysteria2"
            });
            var parsedNodes = controller.ParseStandardNodes(
                Convert.ToBase64String(Encoding.UTF8.GetBytes(uriSample))
            );
            if (parsedNodes.Count != 4 ||
                Convert.ToString(parsedNodes[0]["type"]) != "hysteria2" ||
                Convert.ToString(parsedNodes[1]["type"]) != "vless" ||
                Convert.ToString(parsedNodes[2]["type"]) != "trojan" ||
                Convert.ToString(parsedNodes[3]["type"]) != "shadowsocks")
                throw new InvalidOperationException("Expected extensible URI subscription import priority.");
        }

        public static void RunSubscriptionTest()
        {
            var controller = new ClientController();
            var settings = controller.LoadObject(controller.settingsPath);
            var subscription = controller.LoadSubscription(settings, true);
            var config = controller.BuildConfig(settings, subscription);
            controller.SaveJson(controller.generatedConfigPath, config);
            controller.CheckConfig(controller.generatedConfigPath);

            var managedNodes = controller.GetManagedNodes(subscription);
            if (managedNodes.Count != 2)
                throw new InvalidOperationException("Expected two managed Remnawave proxy nodes.");
        }

        public static void RunTunnelTest()
        {
            var controller = new ClientController();
            try
            {
                controller.Start();
                System.Threading.Thread.Sleep(8000);
                if (!controller.IsRunning)
                    throw new InvalidOperationException("Sing-box core exited during tunnel startup.");

                var resolved = Dns.GetHostAddresses("example.com");
                if (resolved.Length == 0)
                    throw new InvalidOperationException("DNS resolution failed while the tunnel was active.");

                using (var client = new WebClient())
                {
                    client.Headers["User-Agent"] = "ProxyZapret-Tunnel-Test";
                    client.DownloadString("https://example.com/");
                }
            }
            finally
            {
                controller.Stop();
            }
        }

        public static void RunUdpProxyTest()
        {
            var controller = new ClientController();
            var settings = controller.LoadObject(controller.settingsPath);
            var subscription = controller.LoadSubscription(settings, true);

            foreach (var outbound in new[] { "managed-primary", "managed-backup" })
            {
                var config = controller.BuildConfig(settings, subscription);
                var route = AsObject(config["route"]);
                var rules = new ArrayList((ICollection)route["rules"]);
                rules.Insert(1, Object(
                    "network", "udp",
                    "ip_cidr", UdpProbeTargets.Select(target => target + "/32").ToArray(),
                    "port", 53,
                    "action", "route",
                    "outbound", outbound
                ));
                route["rules"] = rules;

                var diagnosticPath = Path.Combine(controller.runtime, "sing-box.udp-test.json");
                controller.SaveJson(diagnosticPath, config);
                controller.CheckConfig(diagnosticPath);
                try
                {
                    controller.StartCore(diagnosticPath);
                    System.Threading.Thread.Sleep(2000);
                    if (!controller.IsRunning)
                        throw new InvalidOperationException("Sing-box core exited during UDP test for " + outbound + ".");
                    SendDnsProbe();
                }
                finally
                {
                    controller.Stop();
                }
            }
        }

        private static readonly string[] UdpProbeTargets = new[] { "1.1.1.1", "8.8.8.8", "9.9.9.9" };

        private static void SendDnsProbe()
        {
            var query = new byte[] {
                0x12, 0x34, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x07, (byte)'e', (byte)'x',
                (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e',
                0x03, (byte)'c', (byte)'o', (byte)'m', 0x00, 0x00,
                0x01, 0x00, 0x01
            };
            var errors = new List<string>();
            foreach (var target in UdpProbeTargets)
            {
                try
                {
                    using (var udp = new UdpClient())
                    {
                        udp.Client.ReceiveTimeout = 5000;
                        udp.Connect(target, 53);
                        udp.Send(query, query.Length);
                        IPEndPoint remote = null;
                        var response = udp.Receive(ref remote);
                        if (response.Length >= 12 && response[0] == 0x12 && response[1] == 0x34)
                            return;
                        errors.Add(target + ": invalid response");
                    }
                }
                catch (Exception exception)
                {
                    errors.Add(target + ": " + exception.Message);
                }
            }

            throw new InvalidOperationException("UDP DNS probe failed for all targets. " + String.Join("; ", errors));
        }

        private void Initialize()
        {
            Directory.CreateDirectory(runtime);
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            if (!File.Exists(settingsPath)) File.Copy(settingsExamplePath, settingsPath);
            if (!File.Exists(statePath))
            {
                SaveJson(statePath, new Dictionary<string, object>
                {
                    { "hwid", Guid.NewGuid().ToString() },
                    { "lastSubscriptionRefreshUtc", null }
                });
            }
        }

        private Dictionary<string, object> LoadSubscription(Dictionary<string, object> settings, bool allowCached)
        {
            var url = Convert.ToString(settings["subscriptionUrl"]);
            if (String.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("Remnawave subscription URL is not configured.");

            try
            {
                var state = LoadObject(statePath);
                using (var client = new WebClient())
                {
                    client.Headers["User-Agent"] = "ProxyZapret/0.2 Windows";
                    client.Headers["x-hwid"] = Convert.ToString(state["hwid"]);
                    client.Headers["x-device-os"] = "Windows";
                    var subscription = DeserializeObject(client.DownloadString(url));
                    SaveJson(cachePath, subscription);
                    RefreshPreferredHysteriaNodes(url, Convert.ToString(state["hwid"]));
                    state["lastSubscriptionRefreshUtc"] = DateTime.UtcNow.ToString("o");
                    SaveJson(statePath, state);
                    return subscription;
                }
            }
            catch
            {
                if (allowCached && File.Exists(cachePath))
                {
                    if (File.Exists(base64CachePath))
                        preferredUriNodes = ParseStandardNodes(File.ReadAllText(base64CachePath, Encoding.UTF8));
                    return LoadObject(cachePath);
                }
                throw;
            }
        }

        private void RefreshPreferredHysteriaNodes(string url, string hwid)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers["User-Agent"] = "ProxyZapretBase64/0.4 Windows";
                    client.Headers["x-hwid"] = hwid;
                    client.Headers["x-device-os"] = "Windows";
                    var encoded = client.DownloadString(url).Trim();
                    File.WriteAllText(base64CachePath, encoded, new UTF8Encoding(false));
                    preferredUriNodes = ParseStandardNodes(encoded);
                }
            }
            catch
            {
                if (File.Exists(base64CachePath))
                    preferredUriNodes = ParseStandardNodes(File.ReadAllText(base64CachePath, Encoding.UTF8));
            }
        }

        private List<Dictionary<string, object>> ParseStandardNodes(string encoded)
        {
            var result = new List<Dictionary<string, object>>();
            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            }
            catch
            {
                return result;
            }

            foreach (var line in decoded.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var node = ParseStandardNode(line.Trim());
                if (node != null) result.Add(node);
            }
            return result.OrderBy(GetNodePriority).ToList();
        }

        private Dictionary<string, object> ParseStandardNode(string value)
        {
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri)) return null;
            switch (uri.Scheme.ToLowerInvariant())
            {
                case "hysteria2":
                case "hy2":
                    return ParseHysteriaNode(uri);
                case "vless":
                    return ParseVlessNode(uri);
                case "trojan":
                    return ParseTrojanNode(uri);
                case "ss":
                    return ParseShadowsocksNode(value);
                default:
                    return null;
            }
        }

        private Dictionary<string, object> ParseHysteriaNode(Uri uri)
        {
            if (!HasServerAndCredentials(uri)) return null;
            var query = ParseQuery(uri.Query);
            var node = Object(
                "type", "hysteria2",
                "tag", GetUriTag(uri, "hysteria2"),
                "server", uri.Host,
                "server_port", uri.Port,
                "password", Uri.UnescapeDataString(uri.UserInfo),
                "tls", BuildTls(query, true)
            );
            string value;
            if (query.TryGetValue("obfs", out value) && !String.IsNullOrWhiteSpace(value))
            {
                var obfs = Object("type", value);
                string password;
                if (query.TryGetValue("obfs-password", out password) && !String.IsNullOrWhiteSpace(password))
                    obfs["password"] = password;
                node["obfs"] = obfs;
            }
            return node;
        }

        private Dictionary<string, object> ParseVlessNode(Uri uri)
        {
            if (!HasServerAndCredentials(uri)) return null;
            var query = ParseQuery(uri.Query);
            var node = Object(
                "type", "vless",
                "tag", GetUriTag(uri, "vless"),
                "server", uri.Host,
                "server_port", uri.Port,
                "uuid", Uri.UnescapeDataString(uri.UserInfo)
            );
            string value;
            if (query.TryGetValue("flow", out value) && !String.IsNullOrWhiteSpace(value))
                node["flow"] = value;
            if (query.TryGetValue("packetEncoding", out value) && !String.IsNullOrWhiteSpace(value))
                node["packet_encoding"] = value;
            ApplyTlsAndTransport(node, query);
            return node;
        }

        private Dictionary<string, object> ParseTrojanNode(Uri uri)
        {
            if (!HasServerAndCredentials(uri)) return null;
            var query = ParseQuery(uri.Query);
            var node = Object(
                "type", "trojan",
                "tag", GetUriTag(uri, "trojan"),
                "server", uri.Host,
                "server_port", uri.Port,
                "password", Uri.UnescapeDataString(uri.UserInfo)
            );
            ApplyTlsAndTransport(node, query);
            return node;
        }

        private Dictionary<string, object> ParseShadowsocksNode(string value)
        {
            var fragmentIndex = value.IndexOf('#');
            var tag = fragmentIndex >= 0 ? Uri.UnescapeDataString(value.Substring(fragmentIndex + 1)) : "shadowsocks";
            var content = value.Substring(5, (fragmentIndex >= 0 ? fragmentIndex : value.Length) - 5);
            var queryIndex = content.IndexOf('?');
            if (queryIndex >= 0) content = content.Substring(0, queryIndex);

            string credentials;
            string hostAndPort;
            var separator = content.LastIndexOf('@');
            if (separator >= 0)
            {
                credentials = DecodeBase64Url(content.Substring(0, separator));
                hostAndPort = content.Substring(separator + 1);
            }
            else
            {
                var decoded = DecodeBase64Url(content);
                separator = decoded.LastIndexOf('@');
                if (separator < 0) return null;
                credentials = decoded.Substring(0, separator);
                hostAndPort = decoded.Substring(separator + 1);
            }
            var colon = credentials.IndexOf(':');
            Uri server;
            if (colon <= 0 || !Uri.TryCreate("ss://" + hostAndPort, UriKind.Absolute, out server) || server.Port <= 0)
                return null;
            return Object(
                "type", "shadowsocks",
                "tag", tag,
                "server", server.Host,
                "server_port", server.Port,
                "method", credentials.Substring(0, colon),
                "password", credentials.Substring(colon + 1)
            );
        }

        private void ApplyTlsAndTransport(Dictionary<string, object> node, Dictionary<string, string> query)
        {
            string security;
            query.TryGetValue("security", out security);
            if (String.Equals(security, "tls", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(security, "reality", StringComparison.OrdinalIgnoreCase))
                node["tls"] = BuildTls(query, true);

            string transportType;
            if (!query.TryGetValue("type", out transportType) || String.IsNullOrWhiteSpace(transportType) ||
                String.Equals(transportType, "tcp", StringComparison.OrdinalIgnoreCase)) return;
            var transport = Object("type", transportType);
            string value;
            if (String.Equals(transportType, "ws", StringComparison.OrdinalIgnoreCase))
            {
                if (query.TryGetValue("path", out value) && !String.IsNullOrWhiteSpace(value))
                    transport["path"] = value;
                if (query.TryGetValue("host", out value) && !String.IsNullOrWhiteSpace(value))
                    transport["headers"] = Object("Host", value);
            }
            else if (String.Equals(transportType, "grpc", StringComparison.OrdinalIgnoreCase) &&
                     query.TryGetValue("serviceName", out value) && !String.IsNullOrWhiteSpace(value))
                transport["service_name"] = value;
            node["transport"] = transport;
        }

        private Dictionary<string, object> BuildTls(Dictionary<string, string> query, bool enabled)
        {
            var tls = Object("enabled", enabled);
            string value;
            if (query.TryGetValue("sni", out value) && !String.IsNullOrWhiteSpace(value))
                tls["server_name"] = value;
            if (query.TryGetValue("insecure", out value))
                tls["insecure"] = IsTrue(value);
            if (query.TryGetValue("fp", out value) && !String.IsNullOrWhiteSpace(value))
                tls["utls"] = Object("enabled", true, "fingerprint", value);
            string security;
            if (query.TryGetValue("security", out security) &&
                String.Equals(security, "reality", StringComparison.OrdinalIgnoreCase))
            {
                var reality = Object("enabled", true);
                if (query.TryGetValue("pbk", out value) && !String.IsNullOrWhiteSpace(value))
                    reality["public_key"] = value;
                if (query.TryGetValue("sid", out value) && !String.IsNullOrWhiteSpace(value))
                    reality["short_id"] = value;
                tls["reality"] = reality;
            }
            return tls;
        }

        private static bool HasServerAndCredentials(Uri uri)
        {
            return !String.IsNullOrWhiteSpace(uri.Host) && uri.Port > 0 && !String.IsNullOrWhiteSpace(uri.UserInfo);
        }

        private static string GetUriTag(Uri uri, string fallback)
        {
            return String.IsNullOrWhiteSpace(uri.Fragment) ? fallback : Uri.UnescapeDataString(uri.Fragment.TrimStart('#'));
        }

        private static string DecodeBase64Url(string value)
        {
            var normalized = Uri.UnescapeDataString(value).Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(normalized)); }
            catch { return Uri.UnescapeDataString(value); }
        }

        private static int GetNodePriority(Dictionary<string, object> node)
        {
            var type = Convert.ToString(node["type"]);
            if (type == "hysteria2") return 10;
            if (type == "vless") return 20;
            if (type == "trojan") return 30;
            if (type == "shadowsocks") return 40;
            return 100;
        }

        private static Dictionary<string, string> ParseQuery(string rawQuery)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in rawQuery.TrimStart('?').Split('&'))
            {
                if (String.IsNullOrWhiteSpace(part)) continue;
                var pieces = part.Split(new[] { '=' }, 2);
                var key = Uri.UnescapeDataString(pieces[0].Replace("+", " "));
                var value = pieces.Length == 2 ? Uri.UnescapeDataString(pieces[1].Replace("+", " ")) : "";
                result[key] = value;
            }
            return result;
        }

        private static bool IsTrue(string value)
        {
            return value == "1" || String.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private Dictionary<string, object> BuildConfig(Dictionary<string, object> settings, Dictionary<string, object> subscription)
        {
            var rules = LoadObject(rulesPath);
            EnsureRuleDefaults(rules);
            var nodes = GetManagedNodes(subscription);
            var tags = nodes.Select(node => node["tag"]).ToArray();
            var ruleSetTags = new object[] {
                "ru-blocked-domains-all", "ru-blocked-ip",
                "service-discord", "service-telegram", "service-telegram-ip",
                "service-meta", "service-instagram", "service-youtube",
                "service-roblox", "service-twitter", "service-twitter-ip",
                "service-tiktok", "service-whatsapp"
            };
            var outbounds = new ArrayList
            {
                Object("type", "direct", "tag", "direct")
            };
            foreach (var node in nodes) outbounds.Add(node);
            outbounds.Add(Object(
                "type", "urltest",
                "tag", "managed-auto",
                "outbounds", tags,
                "url", settings["healthCheckUrl"],
                "interval", settings["healthCheckInterval"],
                "tolerance", Convert.ToInt32(settings["healthCheckToleranceMs"]),
                "interrupt_exist_connections", true
            ));

            return Object(
                "log", Object("level", "info", "timestamp", true),
                "experimental", Object(
                    "cache_file", Object(
                        "enabled", true,
                        "path", rulesCachePath
                    )
                ),
                "dns", Object(
                    "servers", new object[] {
                        Object(
                            "type", "local",
                            "tag", "local-dns"
                        ),
                        Object(
                            "type", "https",
                            "tag", "public-dns",
                            "server", "1.1.1.1",
                            "server_port", 443,
                            "path", "/dns-query",
                            "tls", Object("enabled", true, "server_name", "cloudflare-dns.com")
                        )
                    },
                    "rules", new object[] {
                        Object("domain_suffix", rules["domainSuffixes"], "server", "public-dns"),
                        Object("rule_set", ruleSetTags, "server", "public-dns"),
                        Object("domain_suffix", rules["localDomainSuffixes"], "server", "local-dns"),
                        Object("domain_regex", new object[] { "^[^.]+$" }, "server", "local-dns")
                    },
                    "final", "local-dns",
                    "strategy", "ipv4_only",
                    "reverse_mapping", true
                ),
                "inbounds", new object[] {
                    Object(
                        "type", "tun",
                        "tag", "tun-in",
                        "interface_name", "ProxyZapret",
                        "address", new object[] { "172.19.0.1/30" },
                        "auto_route", true,
                        "strict_route", true,
                        "stack", "mixed"
                    )
                },
                "outbounds", outbounds,
                "route", Object(
                    "auto_detect_interface", true,
                    "default_domain_resolver", "public-dns",
                    "rule_set", new object[] {
                        RemoteRuleSet("ru-blocked-domains-all", "sing-box/rule-set-geosite/geosite-ru-blocked-all.srs"),
                        RemoteRuleSet("ru-blocked-ip", "sing-box/rule-set-geoip/geoip-ru-blocked.srs"),
                        RemoteRuleSet("service-discord", "sing-box/rule-set-geosite/geosite-discord.srs"),
                        RemoteRuleSet("service-telegram", "sing-box/rule-set-geosite/geosite-telegram.srs"),
                        RemoteRuleSet("service-telegram-ip", "sing-box/rule-set-geoip/geoip-telegram.srs"),
                        RemoteRuleSet("service-meta", "sing-box/rule-set-geosite/geosite-meta.srs"),
                        RemoteRuleSet("service-instagram", "sing-box/rule-set-geosite/geosite-instagram.srs"),
                        RemoteRuleSet("service-youtube", "sing-box/rule-set-geosite/geosite-youtube.srs"),
                        RemoteRuleSet("service-roblox", "sing-box/rule-set-geosite/geosite-roblox.srs"),
                        RemoteRuleSet("service-twitter", "sing-box/rule-set-geosite/geosite-twitter.srs"),
                        RemoteRuleSet("service-twitter-ip", "sing-box/rule-set-geoip/geoip-twitter.srs"),
                        RemoteRuleSet("service-tiktok", "sing-box/rule-set-geosite/geosite-tiktok.srs"),
                        RemoteRuleSet("service-whatsapp", "sing-box/rule-set-geosite/geosite-whatsapp.srs")
                    },
                    "rules", new object[] {
                        Object("action", "sniff"),
                        Object("protocol", "dns", "action", "hijack-dns"),
                        Object("ip_is_private", true, "action", "route", "outbound", "direct"),
                        Object("domain_suffix", rules["localDomainSuffixes"], "action", "route", "outbound", "direct"),
                        Object("domain_regex", new object[] { "^[^.]+$" }, "action", "route", "outbound", "direct"),
                        Object("domain_suffix", rules["domainSuffixes"], "action", "route", "outbound", "managed-auto"),
                        Object("ip_cidr", rules["ipCidrs"], "action", "route", "outbound", "managed-auto"),
                        Object(
                            "rule_set",
                            ruleSetTags,
                            "action", "route",
                            "outbound", "managed-auto"
                        ),
                        Object("process_name", rules["processNames"], "action", "route", "outbound", "managed-auto")
                    },
                    "final", "direct"
                )
            );
        }

        private static void EnsureRuleDefaults(Dictionary<string, object> rules)
        {
            if (!rules.ContainsKey("localDomainSuffixes"))
            {
                rules["localDomainSuffixes"] = new object[] {
                    "local", "lan", "home", "home.arpa", "localdomain", "localhost",
                    "intranet", "internal", "corp", "test",
                    "10.in-addr.arpa", "168.192.in-addr.arpa",
                    "16.172.in-addr.arpa", "17.172.in-addr.arpa", "18.172.in-addr.arpa",
                    "19.172.in-addr.arpa", "20.172.in-addr.arpa", "21.172.in-addr.arpa",
                    "22.172.in-addr.arpa", "23.172.in-addr.arpa", "24.172.in-addr.arpa",
                    "25.172.in-addr.arpa", "26.172.in-addr.arpa", "27.172.in-addr.arpa",
                    "28.172.in-addr.arpa", "29.172.in-addr.arpa", "30.172.in-addr.arpa",
                    "31.172.in-addr.arpa"
                };
            }

            if (!rules.ContainsKey("domainSuffixes")) rules["domainSuffixes"] = new object[0];
            if (!rules.ContainsKey("ipCidrs")) rules["ipCidrs"] = new object[0];
            if (!rules.ContainsKey("processNames")) rules["processNames"] = new object[0];
        }

        private List<Dictionary<string, object>> GetManagedNodes(Dictionary<string, object> subscription)
        {
            var excluded = new[] { "direct", "block", "dns", "selector", "urltest" };
            var nodes = new List<Dictionary<string, object>>();
            IEnumerable items = preferredUriNodes.Count >= 2
                ? (IEnumerable)preferredUriNodes
                : AsList(subscription["outbounds"]);
            foreach (var item in items)
            {
                var node = AsObject(item);
                object rawType;
                if (!node.TryGetValue("type", out rawType) || excluded.Contains(Convert.ToString(rawType))) continue;
                var clone = DeserializeObject(json.Serialize(node));
                clone["tag"] = nodes.Count == 0 ? "managed-primary" : "managed-backup";
                if (Convert.ToString(clone["type"]) == "shadowsocks")
                    clone.Remove("network");
                nodes.Add(clone);
                if (nodes.Count == 2) break;
            }
            if (nodes.Count == 0) throw new InvalidOperationException("The subscription does not contain a supported proxy outbound.");
            return nodes;
        }

        private void CheckConfig(string path)
        {
            if (!File.Exists(core)) throw new FileNotFoundException("Sing-box core is missing.", core);
            var result = RunCore(new[] { "check", "-c", Quote(path) }, false);
            if (result != 0) throw new InvalidOperationException("Sing-box rejected the generated configuration.");
        }

        private void StartCore(string path)
        {
            var info = CreateCoreStartInfo(new[] { "run", "-c", Quote(path) });
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            coreProcess = Process.Start(info);
            coreProcess.OutputDataReceived += delegate(object sender, DataReceivedEventArgs args) { AppendLog("sing-box.log", args.Data); };
            coreProcess.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs args) { AppendLog("sing-box.error.log", args.Data); };
            coreProcess.BeginOutputReadLine();
            coreProcess.BeginErrorReadLine();
        }

        private int RunCore(string[] arguments, bool hidden)
        {
            using (var process = Process.Start(CreateCoreStartInfo(arguments)))
            {
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        private ProcessStartInfo CreateCoreStartInfo(string[] arguments)
        {
            return new ProcessStartInfo
            {
                FileName = core,
                Arguments = String.Join(" ", arguments),
                WorkingDirectory = root,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
        }

        private void AppendLog(string file, string line)
        {
            if (line == null) return;
            var path = Path.Combine(runtime, file);
            lock (logLock)
            {
                if (File.Exists(path) && new FileInfo(path).Length > 5 * 1024 * 1024)
                    File.WriteAllText(path, "", new UTF8Encoding(false));
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }

        private Dictionary<string, object> LoadObject(string path)
        {
            return DeserializeObject(File.ReadAllText(path, Encoding.UTF8));
        }

        private Dictionary<string, object> DeserializeObject(string value)
        {
            return json.Deserialize<Dictionary<string, object>>(value);
        }

        private void SaveJson(string path, object value)
        {
            File.WriteAllText(path, json.Serialize(value), new UTF8Encoding(false));
        }

        private static Dictionary<string, object> Object(params object[] values)
        {
            var result = new Dictionary<string, object>();
            for (var index = 0; index < values.Length; index += 2)
                result[Convert.ToString(values[index])] = values[index + 1];
            return result;
        }

        private static Dictionary<string, object> RemoteRuleSet(string tag, string path)
        {
            return Object(
                "type", "remote",
                "tag", tag,
                "format", "binary",
                "url", "https://fastly.jsdelivr.net/gh/runetfreedom/russia-v2ray-rules-dat@release/" + path,
                "update_interval", "6h"
            );
        }

        private static Dictionary<string, object> AsObject(object value)
        {
            return (Dictionary<string, object>)value;
        }

        private static ArrayList AsList(object value)
        {
            return (ArrayList)value;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
