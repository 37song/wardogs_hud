using System.Management;

﻿using System;







using System.Collections.Generic;







using System.Diagnostics;







using System.Drawing;







using System.Drawing.Drawing2D;







using System.Drawing.Text;







using System.Globalization;







using System.IO;







using System.Runtime.InteropServices;







using System.Text;







using System.Text.RegularExpressions;







using System.Threading;







using System.Windows.Forms;







using Microsoft.Win32;







namespace WardogsProfitHudApp







{







static class Program







{







[DllImport("user32.dll")]







private static extern bool SetProcessDPIAware();







[STAThread]







static void Main()







{







// 기존에 백그라운드에 켜져 있던 동일 프로세스가 있다면 자동 정리 후 항상 새 창 기동







try







{







Process current = Process.GetCurrentProcess();







foreach (Process p in Process.GetProcessesByName(current.ProcessName))







{







if (p.Id != current.Id)







{







try { p.Kill(); } catch { }







}







}







}







catch { }







AppDomain.CurrentDomain.UnhandledException += (s, e) => {







try { File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "hud_crash.txt"), e.ExceptionObject.ToString()); } catch { }







};







Application.ThreadException += (s, e) => {







try { File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "hud_crash.txt"), e.Exception.ToString()); } catch { }







};







try { SetProcessDPIAware(); } catch { }







Application.EnableVisualStyles();







Application.SetCompatibleTextRenderingDefault(false);







try







{







Application.Run(new MainHudControlForm());







}







catch (Exception ex)







{







File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "hud_crash.txt"), ex.ToString());







}







}







}







// ==================== 스팀 실시간 내 매치 순손익 타임스탬프 직통 리더 ====================













// =========================================================================

// SteamWebProfitReader: Edge 내장 브라우저 자동 연동 (최종 완결판)

// =========================================================================

public class SteamWebProfitReader : IDisposable

{

    [System.Runtime.InteropServices.DllImport("user32.dll")]

    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);



    [System.Runtime.InteropServices.DllImport("user32.dll")]

    private static extern bool SetForegroundWindow(IntPtr hWnd);



    private System.Net.HttpListener _listener;

    private System.Threading.Thread _listenThread;

    private System.Threading.Thread _watchdogThread;

    private volatile bool _running;

    private volatile bool _hasReceivedAnyData = false;

    private readonly object _lock = new object();



    private string _curVal = "";

    private string _curState = "frontend";

    private string _curFaction = "";

    private DateTime _lastReceived = DateTime.MinValue;

    private int _primaryPort = 27060;

    private int _relayPort = 0;



    private static System.Diagnostics.Process _edgeProcess = null;



    public static string ProfileDir

    {

        get

        {

            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string dir = System.IO.Path.Combine(localApp, "WardogsSteamProfile");

            if (!System.IO.Directory.Exists(dir))

            {

                System.IO.Directory.CreateDirectory(dir);

            }

            return dir;

        }

    }



    public static string ExtensionDir

    {

        get

        {

            string ext = System.IO.Path.Combine(ProfileDir, "AutoSyncExtension");

            if (!System.IO.Directory.Exists(ext))

            {

                System.IO.Directory.CreateDirectory(ext);

            }

            return ext;

        }

    }



    public static string FindEdgeExe()

    {

        string p1 = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";

        if (System.IO.File.Exists(p1)) return p1;

        string p2 = @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";

        if (System.IO.File.Exists(p2)) return p2;

        return "msedge.exe";

    }



                public static void EnsureExtensionFiles()
    {
        try
        {
            string ext = ExtensionDir;
            string manifestPath = System.IO.Path.Combine(ext, "manifest.json");
            string bgPath = System.IO.Path.Combine(ext, "background.js");
            string contentPath = System.IO.Path.Combine(ext, "content.js");

            string manifestJson = "{\n" +
                "  \"manifest_version\": 3,\n" +
                "  \"name\": \"WARDOGS Auto Sync\",\n" +
                "  \"version\": \"1.0\",\n" +
                "  \"host_permissions\": [\"http://127.0.0.1/*\", \"http://localhost/*\"],\n" +
                "  \"background\": {\n" +
                "    \"service_worker\": \"background.js\"\n" +
                "  },\n" +
                "  \"content_scripts\": [{\n" +
                "    \"matches\": [\"https://steamcommunity.com/*\", \"https://login.steampowered.com/*\", \"https://*.steampowered.com/*\"],\n" +
                "    \"js\": [\"content.js\"],\n" +
                "    \"run_at\": \"document_start\"\n" +
                "  }]\n" +
                "}";

            string bgJs = "const PORTS = [27060, 49512, 49513];\n" +
                "function sendToHud(val, st) {\n" +
                "  const v = encodeURIComponent(val || '');\n" +
                "  const s = encodeURIComponent(st || 'game');\n" +
                "  for (let i = 0; i < PORTS.length; i++) {\n" +
                "    fetch('http://127.0.0.1:' + PORTS[i] + '/profit/?val=' + v + '&st=' + s, { mode: 'no-cors' }).catch(function(){});\n" +
                "  }\n" +
                "}\n" +
                "chrome.runtime.onMessage.addListener(function(msg) {\n" +
                "  if (msg && msg.val !== undefined) {\n" +
                "    sendToHud(msg.val, msg.st);\n" +
                "  }\n" +
                "});\n" +
                "setInterval(function() { sendToHud('', 'ping'); }, 2000);";

            string contentJs = "(function() {\n" +
                "  const S = \"#friendslist-container > div > div.chat_main_flex.displayRow.Panel.Focusable > div.DropTarget.friendsListContainer.fullheight.Panel.Focusable > div > div.friendListHeaderContainer.titleBarContainer > div.currentUserContainer.ingame > div > div.labelHolder.ingame > div._9lhfreYH7b4- > div:nth-child(2)\";\n" +
                "  function findProfit() {\n" +
                "    let el = document.querySelector(S);\n" +
                "    if (el && el.innerText && el.innerText.trim()) { return el.innerText.trim(); }\n" +
                "    const userBox = document.querySelector('.currentUserContainer.ingame') || document.querySelector('.currentUserContainer');\n" +
                "    if (userBox) {\n" +
                "      const divs = userBox.querySelectorAll('div, span');\n" +
                "      for (let i = 0; i < divs.length; i++) {\n" +
                "        const t = (divs[i].innerText || '').trim();\n" +
                "        if (/^[+-]?\\s*\\$?[\\d,]+/.test(t)) { return t; }\n" +
                "      }\n" +
                "    }\n" +
                "    return '';\n" +
                "  }\n" +
                "  function sync() {\n" +
                "    let txt = findProfit();\n" +
                "    try {\n" +
                "      chrome.runtime.sendMessage({ val: txt, st: 'game' });\n" +
                "    } catch(e) {}\n" +
                "  }\n" +
                "  setInterval(sync, 1000);\n" +
                "  sync();\n" +
                "})();";

            var utf8NoBom = new System.Text.UTF8Encoding(false);
            System.IO.File.WriteAllText(manifestPath, manifestJson, utf8NoBom);
            System.IO.File.WriteAllText(bgPath, bgJs, utf8NoBom);
            System.IO.File.WriteAllText(contentPath, contentJs, utf8NoBom);
        }
        catch { }
    }



    public static bool IsEdgeAppRunning()
    {
        try
        {
            if (_edgeProcess != null && !_edgeProcess.HasExited)
            {
                return true;
            }

            using (var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'msedge.exe'"))
            {
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    object cmdObj = obj["CommandLine"];
                    if (cmdObj != null)
                    {
                        string cmd = cmdObj.ToString();
                        if (cmd.IndexOf("WardogsSteamProfile", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            cmd.IndexOf("--type=", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            int pid = Convert.ToInt32(obj["ProcessId"]);
                            try { _edgeProcess = System.Diagnostics.Process.GetProcessById(pid); } catch { }
                            return true;
                        }
                    }
                }
            }
        }
        catch { }
        return false;
    }



    public static void FocusExistingEdgeApp()

    {

        try

        {

            var procs = System.Diagnostics.Process.GetProcessesByName("msedge");

            foreach (var p in procs)

            {

                try

                {

                    IntPtr hwnd = p.MainWindowHandle;

                    if (hwnd != IntPtr.Zero)

                    {

                        string title = p.MainWindowTitle;

                        if (!string.IsNullOrEmpty(title) && (title.IndexOf("Steam", StringComparison.OrdinalIgnoreCase) >= 0 || title.IndexOf("친구", StringComparison.OrdinalIgnoreCase) >= 0))

                        {

                            ShowWindow(hwnd, 9); // SW_RESTORE

                            SetForegroundWindow(hwnd);

                            return;

                        }

                    }

                }

                catch { }

            }

        }

        catch { }

    }



    public static void KillExistingEdgeApp()

    {

        try

        {

            var pidsToKill = new System.Collections.Generic.List<int>();

            using (var searcher = new System.Management.ManagementObjectSearcher(

                "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'msedge.exe'"))

            {

                foreach (System.Management.ManagementObject obj in searcher.Get())

                {

                    object cmdObj = obj["CommandLine"];

                    if (cmdObj != null && cmdObj.ToString().IndexOf("WardogsSteamProfile", StringComparison.OrdinalIgnoreCase) >= 0)

                    {

                        pidsToKill.Add(Convert.ToInt32(obj["ProcessId"]));

                    }

                }

            }



            foreach (int pid in pidsToKill)

            {

                try

                {

                    var proc = System.Diagnostics.Process.GetProcessById(pid);

                    proc.Kill();

                    proc.WaitForExit(1000);

                }

                catch { }

            }

        }

        catch { }

    }



    public static void LaunchEdgeApp(bool showNormalWindow)
    {
        try
        {
            if (IsEdgeAppRunning())
            {
                if (showNormalWindow)
                {
                    FocusExistingEdgeApp();
                }
                return;
            }

            KillExistingEdgeApp();
            EnsureExtensionFiles();
            string edge = FindEdgeExe();
            string profile = ProfileDir;
            string ext = ExtensionDir;

            string args = string.Format(
                "--user-data-dir=\"{0}\" --load-extension=\"{1}\" --no-first-run --no-default-browser-check --disable-background-timer-throttling --disable-backgrounding-occluded-windows --disable-renderer-backgrounding --app=https://steamcommunity.com/chat",
                profile, ext
            );

            var psi = new System.Diagnostics.ProcessStartInfo(edge, args);
            psi.WindowStyle = showNormalWindow ? System.Diagnostics.ProcessWindowStyle.Normal : System.Diagnostics.ProcessWindowStyle.Minimized;

            _edgeProcess = System.Diagnostics.Process.Start(psi);
        }
        catch { }
    }



    public static void ResetSteamLogin()
    {
        try
        {
            KillExistingEdgeApp();
            System.Threading.Thread.Sleep(500);

            string profile = ProfileDir;
            string defaultDir = System.IO.Path.Combine(profile, "Default");
            for (int r = 0; r < 5; r++)
            {
                if (!System.IO.Directory.Exists(defaultDir)) break;
                try
                {
                    System.IO.Directory.Delete(defaultDir, true);
                    break;
                }
                catch
                {
                    KillExistingEdgeApp();
                    System.Threading.Thread.Sleep(300);
                }
            }

            System.Windows.Forms.MessageBox.Show(
                "스팀 로그인 정보와 브라우저 권한 설정이 완전히 초기화되었습니다.\n열리는 창에서 로그인 후 나타나는 팝업에서 [허용]을 눌러주세요.",
                "초기화 완료",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information
            );

            LaunchEdgeApp(true);
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show("초기화 실패: " + ex.Message, "오류");
        }
    }

    public bool IsWardogsRunning

    {

        get

        {

            try

            {

                var procs = System.Diagnostics.Process.GetProcessesByName("WardogsClient-Win64-Shipping");

                return procs != null && procs.Length > 0;

            }

            catch { return true; }

        }

    }



    public SteamWebProfitReader(int primaryPort, int relayPort = 0)

    {

        _primaryPort = primaryPort;

        _relayPort = relayPort;

        StartListener();



        System.Threading.ThreadPool.QueueUserWorkItem((_) =>

        {

            System.Threading.Thread.Sleep(1500);

            if (!IsEdgeAppRunning())

            {

                LaunchEdgeApp(false);

            }

        });



        _watchdogThread = new System.Threading.Thread(WatchdogLoop);

        _watchdogThread.IsBackground = true;

        _watchdogThread.Name = "EdgeKeepAliveWatchdog";

        _watchdogThread.Start();

    }



    private void WatchdogLoop()
    {
        while (_running)
        {
            System.Threading.Thread.Sleep(5000);
            if (!_running) break;

            if (!IsEdgeAppRunning())
            {
                if (_hasReceivedAnyData && (DateTime.Now - _lastReceived).TotalSeconds > 15)
                {
                    LaunchEdgeApp(false);
                }
            }
        }
    }



    private void StartListener()
    {
        int[] portsToTry = new int[] { 27060, 49512, 49513 };
        for (int retry = 0; retry < 3; retry++)
        {
            foreach (int p in portsToTry)
            {
                try
                {
                    var l = new System.Net.HttpListener();
                    l.Prefixes.Add(string.Format("http://127.0.0.1:{0}/", p));
                    l.Prefixes.Add(string.Format("http://127.0.0.1:{0}/profit/", p));
                    l.Start();
                    _listener = l;
                    _running = true;
                    _listenThread = new System.Threading.Thread(ListenLoop);
                    _listenThread.IsBackground = true;
                    _listenThread.Name = "SteamWebProfitReader_" + p;
                    _listenThread.Start();
                    return;
                }
                catch { }
            }
            System.Threading.Thread.Sleep(300);
        }
    }



    private void ListenLoop()

    {

        while (_running && _listener != null && _listener.IsListening)

        {

            try

            {

                var context = _listener.GetContext();

                System.Threading.ThreadPool.QueueUserWorkItem(ProcessRequest, context);

            }

            catch

            {

                if (!_running) break;

            }

        }

    }



    private void ProcessRequest(object state)

    {

        try

        {

            var context = (System.Net.HttpListenerContext)state;

            var req = context.Request;

            var resp = context.Response;



            resp.Headers.Add("Access-Control-Allow-Origin", "*");

            resp.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");

            resp.Headers.Add("Access-Control-Allow-Headers", "*");

            resp.Headers.Add("Access-Control-Allow-Private-Network", "true");



            if (req.HttpMethod == "OPTIONS")

            {

                resp.StatusCode = 204;

                resp.Close();

                return;

            }



            string rawVal = req.QueryString["val"] ?? "";

            string st = req.QueryString["st"] ?? "frontend";

            string fac = req.QueryString["fac"] ?? "";



            string cleanVal = rawVal;

            if (!string.IsNullOrEmpty(rawVal))

            {

                var match = System.Text.RegularExpressions.Regex.Match(rawVal, @"([+-]?\s*\$?[\d,]+)");

                if (match.Success)

                {

                    cleanVal = match.Groups[1].Value.Replace(" ", "");

                }

            }



            lock (_lock)
            {
                if (!string.IsNullOrEmpty(cleanVal))
                {
                    _curVal = cleanVal;
                    _curState = st;
                    _curFaction = fac;
                }
                _lastReceived = DateTime.Now;
                _hasReceivedAnyData = true;
            }



            byte[] buf = System.Text.Encoding.UTF8.GetBytes("OK");

            resp.ContentType = "text/plain";

            resp.ContentLength64 = buf.Length;

            resp.OutputStream.Write(buf, 0, buf.Length);

            resp.OutputStream.Close();

        }

        catch { }

    }



    public bool Poll(out string profitLossStr, out string stateStr, out string factionStr, out string gameStateStr, out ulong timestamp)

    {

        lock (_lock)

        {

            timestamp = (ulong)DateTime.UtcNow.Ticks;

            factionStr = _curFaction;

            gameStateStr = "playing";



            if ((DateTime.Now - _lastReceived).TotalSeconds <= 30)

            {

                if (!string.IsNullOrEmpty(_curVal))

                {

                    profitLossStr = _curVal;

                    stateStr = _curState;

                    return true;

                }

                else

                {

                    profitLossStr = "$0";

                    stateStr = "frontend";

                    return true;

                }

            }

            else

            {

                profitLossStr = "스팀 로그인 필요";

                stateStr = "frontend";

                return false;

            }

        }

    }



    public void Dispose()

    {

        _running = false;

        try { if (_listener != null && _listener.IsListening) _listener.Stop(); } catch { }

        try { if (_listener != null) _listener.Close(); } catch { }

        try { if (_listenThread != null) _listenThread.Abort(); } catch { }

        try { if (_watchdogThread != null) _watchdogThread.Abort(); } catch { }

        try { KillExistingEdgeApp(); } catch { }

    }

}





public class MainHudControlForm : Form







{







[DllImport("user32.dll")]







private static extern short GetAsyncKeyState(int vKey);







[DllImport("dwmapi.dll")]







private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);







private const int VK_F7 = 0x76;







private const int VK_F8 = 0x77;







private const string RegistryKeyPath = @"Software\WardogsProfitHud";







private SteamWebProfitReader steamReader;







private ProfitHudOverlayForm hudOverlay;







private System.Windows.Forms.Timer pollTimer;







private Thread hotkeyThread;







private volatile bool isRunning = true;







private bool isHudVisible = true;







private bool isClickThrough = false;







private bool showHudBox = true;







private int hudFontSize = 16;







private int hudOpacityPercent = 90;







private Point hudPosition = new Point(30, 30);







private bool hasLastAmount = false;







private long lastAmountValue = 0;







private long lastDeltaValue = 0;







private DateTime lastDeltaTime = DateTime.MinValue;







private Button btnToggleHud;







private Button btnToggleLock;







private NumericUpDown numFontSize, numOpacity;







private CheckBox chkShowBox;







private CheckBox chkControlTopMost;







private Label lblLiveValuePreview;







private bool suppressEvents = false;







public MainHudControlForm()







{







this.Text = "WARDOGS 수익 HUD";







try







{







Icon exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);







if (exeIcon != null) this.Icon = exeIcon;







}







catch { }







this.FormBorderStyle = FormBorderStyle.FixedSingle;







this.MaximizeBox = false;







this.ClientSize = new Size(320, 276);







this.StartPosition = FormStartPosition.CenterScreen;







this.BackColor = Color.FromArgb(12, 12, 14);







this.ForeColor = Color.FromArgb(168, 174, 184);







this.Font = new Font("맑은 고딕", 9f, FontStyle.Regular);







hudOverlay = new ProfitHudOverlayForm();







hudOverlay.PositionChangedByDrag += (newPos) =>







{







hudPosition = newPos;







SaveSettings();







};







hudOverlay.FontSizeChangedByWheel += (delta) =>







{







if (numFontSize != null)







{







int next = (int)numFontSize.Value + delta;







numFontSize.Value = Math.Max(numFontSize.Minimum, Math.Min(numFontSize.Maximum, next));







}







};







BuildUI();







LoadSettings();







ApplySettingsToHud();







steamReader = new SteamWebProfitReader(49513, 0);







pollTimer = new System.Windows.Forms.Timer();







pollTimer.Interval = 200;







pollTimer.Tick += PollTimer_Tick;







pollTimer.Start();







hotkeyThread = new Thread(HotkeyLoop) { IsBackground = true };







hotkeyThread.Start();







PollTimer_Tick(null, EventArgs.Empty);







}







protected override void OnShown(EventArgs e)







{







base.OnShown(e);







this.BringToFront();







this.Activate();







}







protected override void OnHandleCreated(EventArgs e)







{







base.OnHandleCreated(e);







try







{







int useDark = 1;







if (DwmSetWindowAttribute(this.Handle, 20, ref useDark, sizeof(int)) != 0)







{







DwmSetWindowAttribute(this.Handle, 19, ref useDark, sizeof(int));







}







}







catch { }







}







private void BuildUI()







{







// 상단: 수익 숫자 나오던 네모 박스 복원







Panel pnlPreview = new Panel()







{







Location = new Point(14, 12),







Size = new Size(292, 52),







BackColor = Color.FromArgb(18, 18, 22),







BorderStyle = BorderStyle.FixedSingle







};







lblLiveValuePreview = new Label()







{







Dock = DockStyle.Fill,







TextAlign = ContentAlignment.MiddleCenter,







Font = new Font("맑은 고딕", 15f, FontStyle.Bold),







Text = "실시간 순손익 수집 중...",







ForeColor = Color.FromArgb(200, 206, 216)







};







pnlPreview.Controls.Add(lblLiveValuePreview);







// 하단: HUD 오버레이 설정







GroupBox grpControls = new GroupBox()







{







Text = "HUD 오버레이 설정 (F7: 표시 / F8: 위치잠금)",







Font = new Font("맑은 고딕", 9f, FontStyle.Bold),







Location = new Point(14, 72),







Size = new Size(292, 138),







ForeColor = Color.FromArgb(158, 164, 174)







};







btnToggleHud = new Button()







{







Text = "HUD 켜짐",







Location = new Point(12, 24),







Size = new Size(130, 36),







Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold),







FlatStyle = FlatStyle.Flat,







TextAlign = ContentAlignment.MiddleCenter,







BackColor = Color.FromArgb(24, 64, 38),







ForeColor = Color.FromArgb(168, 225, 184)







};







btnToggleHud.FlatAppearance.BorderColor = Color.FromArgb(48, 120, 72);







btnToggleHud.Click += (s, e) => ToggleHudVisible();







btnToggleLock = new Button()







{







Text = "마우스 이동 가능",







Location = new Point(150, 24),







Size = new Size(130, 36),







Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold),







FlatStyle = FlatStyle.Flat,







TextAlign = ContentAlignment.MiddleCenter,







BackColor = Color.FromArgb(22, 22, 26),







ForeColor = Color.FromArgb(172, 178, 188)







};







btnToggleLock.FlatAppearance.BorderColor = Color.FromArgb(46, 48, 54);







btnToggleLock.Click += (s, e) => ToggleClickThrough();







Label lblFont = new Label() { Text = "글자크기:", Location = new Point(14, 72), AutoSize = true, Font = new Font("맑은 고딕", 9f), ForeColor = Color.FromArgb(155, 160, 170) };







numFontSize = CreateNum(76, 69, 8, 60, 16, 1);







numFontSize.ValueChanged += (s, e) => { if (!suppressEvents) { hudFontSize = (int)numFontSize.Value; ApplySettingsToHud(); SaveSettings(); } };







numFontSize.MouseWheel += (s, e) => {







((HandledMouseEventArgs)e).Handled = true;







int delta = e.Delta > 0 ? 1 : -1;







int next = (int)numFontSize.Value + delta;







numFontSize.Value = Math.Max(numFontSize.Minimum, Math.Min(numFontSize.Maximum, next));







};







Label lblOpacity = new Label() { Text = "불투명도(%):", Location = new Point(146, 72), AutoSize = true, Font = new Font("맑은 고딕", 9f), ForeColor = Color.FromArgb(155, 160, 170) };







numOpacity = CreateNum(226, 69, 15, 100, 90, 5);







numOpacity.ValueChanged += (s, e) => { if (!suppressEvents) { hudOpacityPercent = (int)numOpacity.Value; ApplySettingsToHud(); SaveSettings(); } };







numOpacity.MouseWheel += (s, e) => {







((HandledMouseEventArgs)e).Handled = true;







int delta = e.Delta > 0 ? 5 : -5;







int next = (int)numOpacity.Value + delta;







numOpacity.Value = Math.Max(numOpacity.Minimum, Math.Min(numOpacity.Maximum, next));







};







chkShowBox = new CheckBox()







{







Text = "어두운 배경 박스",







Location = new Point(14, 104),







AutoSize = true,







Font = new Font("맑은 고딕", 9f),







ForeColor = Color.FromArgb(162, 168, 178),







Checked = true







};







chkShowBox.CheckedChanged += (s, e) => { if (!suppressEvents) { showHudBox = chkShowBox.Checked; ApplySettingsToHud(); SaveSettings(); } };







chkControlTopMost = new CheckBox()







{







Text = "창 항상 위",







Location = new Point(164, 104),







AutoSize = true,







Font = new Font("맑은 고딕", 9f),







ForeColor = Color.FromArgb(162, 168, 178),







Checked = true







};







chkControlTopMost.CheckedChanged += (s, e) => { this.TopMost = chkControlTopMost.Checked; if (!suppressEvents) SaveSettings(); };







grpControls.Controls.AddRange(new Control[]







{







btnToggleHud, btnToggleLock,







lblFont, numFontSize, lblOpacity, numOpacity,







chkShowBox, chkControlTopMost







});







this.Controls.Add(pnlPreview);







this.Controls.Add(grpControls);



        Button btnSteamLogin = new Button();

        btnSteamLogin.Text = "스팀 로그인 (우클릭: 계정변경)";

        btnSteamLogin.Font = new Font("맑은 고딕", 9f, FontStyle.Bold);

        btnSteamLogin.BackColor = Color.FromArgb(31, 110, 165);

        btnSteamLogin.ForeColor = Color.White;

        btnSteamLogin.FlatStyle = FlatStyle.Flat;

        btnSteamLogin.FlatAppearance.BorderSize = 0;

        btnSteamLogin.Size = new Size(292, 32);

        btnSteamLogin.Location = new Point(14, 216);

        btnSteamLogin.Cursor = Cursors.Hand;

        btnSteamLogin.MouseDown += (s, me) => {

            if (me.Button == MouseButtons.Right) {

                if (MessageBox.Show("스팀 계정을 다른 아이디로 변경하시겠습니까?\n(기존 로그인 정보가 초기화됩니다)", "계정 변경", MessageBoxButtons.YesNo) == DialogResult.Yes) {

                    SteamWebProfitReader.ResetSteamLogin();

                }

            } else {

                SteamWebProfitReader.LaunchEdgeApp(true);

            }

        };

        this.Controls.Add(btnSteamLogin);

        btnSteamLogin.BringToFront();

        







Label lblCredit = new Label()







{







Text = "by HIST",







Font = new Font("맑은 고딕", 8.5f, FontStyle.Bold),







ForeColor = Color.FromArgb(255, 238, 140),







Location = new Point(14, 252),







Size = new Size(292, 18),







TextAlign = ContentAlignment.MiddleRight







};







this.Controls.Add(lblCredit);







this.TopMost = chkControlTopMost.Checked;







}







private NumericUpDown CreateNum(int x, int y, int min, int max, int val, int increment)







{







NumericUpDown num = new NumericUpDown()







{







Location = new Point(x, y),







Size = new Size(54, 23),







Font = new Font("맑은 고딕", 9f),







Minimum = min,







Maximum = max,







Value = val,







Increment = increment,







DecimalPlaces = 0,







BackColor = Color.FromArgb(20, 20, 24),







ForeColor = Color.FromArgb(185, 192, 202),







BorderStyle = BorderStyle.FixedSingle,







TextAlign = HorizontalAlignment.Center







};







return num;







}







private void ToggleHudVisible()







{







isHudVisible = !isHudVisible;







ApplySettingsToHud();







SaveSettings();







}







private void ToggleClickThrough()







{







isClickThrough = !isClickThrough;







ApplySettingsToHud();







SaveSettings();







}







private void ApplySettingsToHud()







{







if (isHudVisible)







{







btnToggleHud.Text = "HUD 켜짐";







btnToggleHud.BackColor = Color.FromArgb(24, 64, 38);







btnToggleHud.ForeColor = Color.FromArgb(168, 225, 184);







btnToggleHud.FlatAppearance.BorderColor = Color.FromArgb(48, 120, 72);







}







else







{







btnToggleHud.Text = "HUD 꺼짐";







btnToggleHud.BackColor = Color.FromArgb(22, 22, 26);







btnToggleHud.ForeColor = Color.FromArgb(148, 154, 164);







btnToggleHud.FlatAppearance.BorderColor = Color.FromArgb(46, 48, 54);







}







if (isClickThrough)







{







btnToggleLock.Text = "위치 잠금 (투과)";







btnToggleLock.BackColor = Color.FromArgb(62, 42, 18);







btnToggleLock.ForeColor = Color.FromArgb(245, 200, 125);







btnToggleLock.FlatAppearance.BorderColor = Color.FromArgb(130, 90, 36);







}







else







{







btnToggleLock.Text = "마우스 이동 가능";







btnToggleLock.BackColor = Color.FromArgb(22, 22, 26);







btnToggleLock.ForeColor = Color.FromArgb(172, 178, 188);







btnToggleLock.FlatAppearance.BorderColor = Color.FromArgb(46, 48, 54);







}







hudOverlay.ConfigureHud(hudPosition, hudFontSize, hudOpacityPercent, showHudBox, isClickThrough, isHudVisible);







}







private void HotkeyLoop()







{







bool f7Prev = false;







bool f8Prev = false;







while (isRunning)







{







bool f7Down = (GetAsyncKeyState(VK_F7) & 0x8000) != 0;







bool f8Down = (GetAsyncKeyState(VK_F8) & 0x8000) != 0;







if (f7Down && !f7Prev)







{







if (!IsDisposed && IsHandleCreated)







this.BeginInvoke((Action)ToggleHudVisible);







}







if (f8Down && !f8Prev)







{







if (!IsDisposed && IsHandleCreated)







this.BeginInvoke((Action)ToggleClickThrough);







}







f7Prev = f7Down;







f8Prev = f8Down;







Thread.Sleep(25);







}







}







private void PollTimer_Tick(object sender, EventArgs e)







{







string profitLoss;







string state;







string faction;







string gameState;







ulong ts;







bool found = steamReader.Poll(out profitLoss, out state, out faction, out gameState, out ts);







string displayText;







int sign = 0;







if (found && !string.IsNullOrEmpty(profitLoss))







{







if (state == "loss" || profitLoss.Contains("-"))







{







sign = -1;







displayText = profitLoss;







}







else if (state == "profit" || profitLoss.Contains("+") || (!profitLoss.Contains("-") && profitLoss != "$0"))







{







sign = 1;







string formatted = profitLoss.StartsWith("+") ? profitLoss : ("+" + profitLoss);







displayText = formatted;







}







else







{







sign = 0;







displayText = profitLoss;







}







}







else







{







if (steamReader.IsWardogsRunning)







{







displayText = "대기 중";







}







else







{







displayText = "WARDOGS 대기 중";







}







sign = 0;







}







string deltaText = "";







long parsedAmount;







if (TryExtractSignedAmount(profitLoss, sign, out parsedAmount))







{







if (hasLastAmount && parsedAmount != lastAmountValue)







{







lastDeltaValue = parsedAmount - lastAmountValue;







lastDeltaTime = DateTime.UtcNow;







}







lastAmountValue = parsedAmount;







hasLastAmount = true;







}







if ((DateTime.UtcNow - lastDeltaTime).TotalSeconds <= 8.0 && lastDeltaValue != 0)







{







deltaText = lastDeltaValue > 0







? string.Format("(+${0:N0})", lastDeltaValue)







: string.Format("(-${0:N0})", Math.Abs(lastDeltaValue));







}







UpdateHudDisplay(displayText, "", sign);







}







private static bool TryExtractSignedAmount(string text, int sign, out long amount)







{







amount = 0;







if (string.IsNullOrEmpty(text)) return false;







Match m = Regex.Match(text, @"([\+\-])?\s*\$?\s*([\d,]+)");







if (!m.Success) return false;







string numStr = m.Groups[2].Value.Replace(",", "");







long parsed;







if (!long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))







return false;







if (m.Groups[1].Value == "-" || sign < 0)







amount = -parsed;







else







amount = parsed;







return true;







}







private void UpdateHudDisplay(string mainText, string deltaText, int sign)







{







if (lblLiveValuePreview != null)







{







lblLiveValuePreview.Text = mainText;







if (sign > 0) lblLiveValuePreview.ForeColor = Color.FromArgb(46, 230, 118);







else if (sign < 0) lblLiveValuePreview.ForeColor = Color.FromArgb(255, 85, 85);







else lblLiveValuePreview.ForeColor = Color.FromArgb(205, 212, 222);







}







hudOverlay.UpdateContent(mainText, deltaText, sign);







}







private void LoadSettings()







{







suppressEvents = true;







try







{







using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))







{







if (key != null)







{







hudPosition.X = unchecked((int)Convert.ToInt64(key.GetValue("X", 30)));







hudPosition.Y = unchecked((int)Convert.ToInt64(key.GetValue("Y", 30)));







hudFontSize = Math.Max(8, Math.Min(60, Convert.ToInt32(key.GetValue("FontSize", 16))));







hudOpacityPercent = Math.Max(15, Math.Min(100, Convert.ToInt32(key.GetValue("Opacity", 90))));







showHudBox = Convert.ToInt32(key.GetValue("ShowBox", 1)) != 0;







isClickThrough = Convert.ToInt32(key.GetValue("ClickThrough", 0)) != 0;







isHudVisible = Convert.ToInt32(key.GetValue("HudVisible", 1)) != 0;







bool topMost = Convert.ToInt32(key.GetValue("ControlTopMost", 1)) != 0;







numFontSize.Value = hudFontSize;







numOpacity.Value = hudOpacityPercent;







chkShowBox.Checked = showHudBox;







chkControlTopMost.Checked = topMost;







this.TopMost = topMost;







}







}







}







catch { }







suppressEvents = false;







}







private void SaveSettings()







{







if (suppressEvents) return;







try







{







using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))







{







if (key != null)







{







key.SetValue("X", hudPosition.X, RegistryValueKind.DWord);







key.SetValue("Y", hudPosition.Y, RegistryValueKind.DWord);







key.SetValue("FontSize", hudFontSize, RegistryValueKind.DWord);







key.SetValue("Opacity", hudOpacityPercent, RegistryValueKind.DWord);







key.SetValue("ShowBox", showHudBox ? 1 : 0, RegistryValueKind.DWord);







key.SetValue("ClickThrough", isClickThrough ? 1 : 0, RegistryValueKind.DWord);







key.SetValue("HudVisible", isHudVisible ? 1 : 0, RegistryValueKind.DWord);







key.SetValue("ControlTopMost", chkControlTopMost.Checked ? 1 : 0, RegistryValueKind.DWord);







}







}







}







catch { }







}







protected override void OnFormClosing(FormClosingEventArgs e)







{







isRunning = false;







if (pollTimer != null) pollTimer.Stop();







if (steamReader != null) steamReader.Dispose();







if (hudOverlay != null && !hudOverlay.IsDisposed) hudOverlay.Close();







base.OnFormClosing(e);







}







}







// ==================== 인게임 미니 HUD 오버레이 폼 ====================







public class ProfitHudOverlayForm : Form







{







private const int WS_EX_TRANSPARENT = 0x00000020;







private const int WS_EX_LAYERED = 0x00080000;







private const int WS_EX_TOOLWINDOW = 0x00000080;







private const int WS_EX_NOACTIVATE = 0x08000000;







private const int GWL_EXSTYLE = -20;







private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);







private const uint SWP_NOMOVE = 0x0002;







private const uint SWP_NOSIZE = 0x0001;







private const uint SWP_NOACTIVATE = 0x08000000 | 0x0010;







private const uint LWA_COLORKEY = 0x00000001;







private const uint LWA_ALPHA = 0x00000002;







[DllImport("user32.dll")]







private static extern int GetWindowLong(IntPtr hWnd, int nIndex);







[DllImport("user32.dll")]







private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);







[DllImport("user32.dll")]







private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);







[DllImport("user32.dll")]







private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);







public event Action<Point> PositionChangedByDrag;







public event Action<int> FontSizeChangedByWheel;







private string mainText = "WARDOGS 대기 중";







private string deltaText = "";







private int sign = 0;







private int fontSize = 16;







private int opacityPercent = 90;







private bool showBox = true;







private bool clickThrough = false;







private bool isDragging = false;







private Point dragStartMouse;







private Point dragStartForm;







private static readonly Color KeyColor = Color.FromArgb(1, 2, 3);







private const uint KeyColorCr = 0x00030201;







public ProfitHudOverlayForm()







{







this.Text = "WARDOGS 수익 HUD";







try







{







Icon exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);







if (exeIcon != null) this.Icon = exeIcon;







}







catch { }







this.FormBorderStyle = FormBorderStyle.None;







this.ShowInTaskbar = false;







this.StartPosition = FormStartPosition.Manual;







this.BackColor = Color.FromArgb(14, 16, 20);







this.DoubleBuffered = true;







this.TopMost = true;







this.Size = new Size(280, 48);







this.MouseDown += OnHudMouseDown;







this.MouseMove += OnHudMouseMove;







this.MouseUp += OnHudMouseUp;







this.MouseWheel += OnHudMouseWheel;







}







protected override CreateParams CreateParams







{







get







{







CreateParams cp = base.CreateParams;







cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;







if (clickThrough) cp.ExStyle |= WS_EX_TRANSPARENT;







return cp;







}







}







protected override bool ShowWithoutActivation







{







get { return true; }







}







public void ConfigureHud(Point pos, int fSize, int opacity, bool box, bool isClickThrough, bool visible)







{







this.Location = pos;







this.fontSize = fSize;







this.opacityPercent = opacity;







this.showBox = box;







this.clickThrough = isClickThrough;







this.Cursor = isClickThrough ? Cursors.Default : Cursors.SizeAll;







if (visible && !this.Visible)







{







this.Show();







}







else if (!visible && this.Visible)







{







this.Hide();







}







ApplyWindowStyles();







RecalculateSizeAndRepaint();







}







public void UpdateContent(string text, string delta, int s)







{







if (mainText == text && deltaText == delta && sign == s) return;







mainText = text;







deltaText = delta;







sign = s;







RecalculateSizeAndRepaint();







}







private void ApplyWindowStyles()







{







if (!this.IsHandleCreated) return;







int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);







exStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;







if (clickThrough)







exStyle |= WS_EX_TRANSPARENT;







else







exStyle &= ~WS_EX_TRANSPARENT;







SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);







byte alpha = (byte)Math.Max(30, Math.Min(255, (int)Math.Round(255.0 * opacityPercent / 100.0)));







if (showBox)







{







this.BackColor = Color.FromArgb(14, 16, 20);







SetLayeredWindowAttributes(this.Handle, 0, alpha, LWA_ALPHA);







}







else







{







this.BackColor = KeyColor;







SetLayeredWindowAttributes(this.Handle, KeyColorCr, alpha, LWA_COLORKEY | LWA_ALPHA);







}







SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);







}







private void RecalculateSizeAndRepaint()







{







if (!this.IsHandleCreated) return;







using (Graphics g = this.CreateGraphics())







using (Font mainFont = new Font("맑은 고딕", fontSize, FontStyle.Bold))







using (Font subFont = new Font("맑은 고딕", Math.Max(9f, fontSize * 0.68f), FontStyle.Bold))







{







string prefix = "";







SizeF mainSz = g.MeasureString(prefix + mainText, mainFont);







SizeF subSz = string.IsNullOrEmpty(deltaText) ? SizeF.Empty : g.MeasureString(deltaText, subFont);







int w = (int)Math.Ceiling(mainSz.Width + (string.IsNullOrEmpty(deltaText) ? 24 : (subSz.Width + 30)));







int h = (int)Math.Ceiling(Math.Max(mainSz.Height, subSz.Height) + 10);







w = Math.Max(48, w);







h = Math.Max(28, h);







if (this.Width != w || this.Height != h)







{







this.Size = new Size(w, h);







}







}







this.Invalidate();







}







protected override void OnPaint(PaintEventArgs e)







{







base.OnPaint(e);







Graphics g = e.Graphics;







Color mainColor;







Color accentBorder;







string prefix;







if (sign > 0)







{







mainColor = Color.FromArgb(46, 235, 122);







accentBorder = Color.FromArgb(40, 160, 88);







prefix = "";







}







else if (sign < 0)







{







mainColor = Color.FromArgb(255, 88, 88);







accentBorder = Color.FromArgb(180, 56, 56);







prefix = "";







}







else







{







mainColor = Color.FromArgb(220, 226, 236);







accentBorder = Color.FromArgb(65, 72, 84);







prefix = "";







}







if (showBox)







{







g.SmoothingMode = SmoothingMode.AntiAlias;







g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;







using (SolidBrush barBrush = new SolidBrush(mainColor))







{







g.FillRectangle(barBrush, 0, 0, 4, this.ClientSize.Height);







}







using (Pen borderPen = new Pen(clickThrough ? accentBorder : Color.FromArgb(110, 150, 220), 1f))







{







g.DrawRectangle(borderPen, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);







}







}







else







{







g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;







}







using (Font mainFont = new Font("맑은 고딕", fontSize, FontStyle.Bold))







using (Font subFont = new Font("맑은 고딕", Math.Max(9f, fontSize * 0.68f), FontStyle.Bold))







{







string fullMain = prefix + mainText;







SizeF mainSz = g.MeasureString(fullMain, mainFont);







float x = 12f;







float y = (this.ClientSize.Height - mainSz.Height) / 2f;







using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(10, 10, 12)))







{







g.DrawString(fullMain, mainFont, shadowBrush, x + 1f, y + 1f);







}







using (SolidBrush textBrush = new SolidBrush(mainColor))







{







g.DrawString(fullMain, mainFont, textBrush, x, y);







}







if (!string.IsNullOrEmpty(deltaText))







{







float subX = x + mainSz.Width + 4f;







SizeF subSz = g.MeasureString(deltaText, subFont);







float subY = (this.ClientSize.Height - subSz.Height) / 2f;







Color subColor = deltaText.StartsWith("(+")







? Color.FromArgb(130, 250, 175)







: Color.FromArgb(255, 145, 145);







using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(10, 10, 12)))







{







g.DrawString(deltaText, subFont, shadowBrush, subX + 1f, subY + 1f);







}







using (SolidBrush subBrush = new SolidBrush(subColor))







{







g.DrawString(deltaText, subFont, subBrush, subX, subY);







}







}







}







}







private void OnHudMouseDown(object sender, MouseEventArgs e)







{







if (clickThrough) return;







if (e.Button == MouseButtons.Left)







{







isDragging = true;







dragStartMouse = Control.MousePosition;







dragStartForm = this.Location;







}







}







private void OnHudMouseMove(object sender, MouseEventArgs e)







{







if (!isDragging || clickThrough) return;







Point cur = Control.MousePosition;







Point next = new Point(







dragStartForm.X + (cur.X - dragStartMouse.X),







dragStartForm.Y + (cur.Y - dragStartMouse.Y)







);







this.Location = next;







}







private void OnHudMouseUp(object sender, MouseEventArgs e)







{







if (isDragging)







{







isDragging = false;







if (PositionChangedByDrag != null)







{







PositionChangedByDrag(this.Location);







}







}







}







private void OnHudMouseWheel(object sender, MouseEventArgs e)







{







if (clickThrough) return;







int delta = e.Delta > 0 ? 1 : -1;







if (FontSizeChangedByWheel != null)







{







FontSizeChangedByWheel(delta);







}







}







}







}







