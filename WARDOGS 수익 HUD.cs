using System;



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



public class SteamDirectProfitReader : IDisposable



{



[DllImport("kernel32.dll", SetLastError = true)]



private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);



[DllImport("kernel32.dll", SetLastError = true)]



private static extern bool CloseHandle(IntPtr hObject);



[DllImport("kernel32.dll", SetLastError = true)]



private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);



[DllImport("kernel32.dll", SetLastError = true)]



private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);



[StructLayout(LayoutKind.Sequential)]



private struct MEMORY_BASIC_INFORMATION



{



public IntPtr BaseAddress;



public IntPtr AllocationBase;



public uint AllocationProtect;



public ushort PartitionId;



public IntPtr RegionSize;



public uint State;



public uint Protect;



public uint Type;



}



private const uint PROCESS_VM_READ = 0x0010;



private const uint PROCESS_QUERY_INFORMATION = 0x0400;



private readonly object syncLock = new object();



private Thread bgWorker;



private volatile bool isRunning = true;



private string curVal = "";



private string curState = "";



private string curFaction = "";



private string curGame = "";



private ulong curTs = 0;



private bool hasData = false;



public bool IsSteamRunning { get; private set; }



public bool IsWardogsRunning { get; private set; }



public SteamDirectProfitReader()



{



bgWorker = new Thread(WorkerLoop)



{



IsBackground = true,



Name = "SteamProfitReaderWorker"



};



bgWorker.Start();



}



public bool Poll(out string profitLossStr, out string stateStr, out string factionStr, out string gameStateStr, out ulong timestamp)



{



lock (syncLock)



{



profitLossStr = curVal;



stateStr = curState;



factionStr = curFaction;



gameStateStr = curGame;



timestamp = curTs;



return hasData;



}



}



private static Process FindTargetRenderer()



{



Process[] helpers = Process.GetProcessesByName("steamwebhelper");



if (helpers == null || helpers.Length == 0) return null;



Process best = null;



long maxWs = 0;



foreach (Process p in helpers)



{



try



{



if (p.WorkingSet64 > maxWs)



{



maxWs = p.WorkingSet64;



best = p;



}



}



catch { }



}



return best;



}



private void CheckProcesses(out int steamPid, out uint activeUser, ref ulong steamId64)



{



steamPid = 0;



activeUser = 0;



try



{



using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam\ActiveProcess"))



{



if (key != null)



{



steamPid = Convert.ToInt32(key.GetValue("pid", 0));



activeUser = Convert.ToUInt32(key.GetValue("ActiveUser", 0));



if (activeUser != 0) steamId64 = 76561197960265728UL + activeUser;



}



}



}



catch { }



IsSteamRunning = steamPid != 0 && (Process.GetProcessesByName("steam").Length > 0 || Process.GetProcessesByName("steamwebhelper").Length > 0);



IsWardogsRunning = Process.GetProcessesByName("WardogsClient-Win64-Shipping").Length > 0;



}



                private static ulong ExtractOwnerSteamId(byte[] buf, int needlePos)
        {
            int start = Math.Max(0, needlePos - 300);
            for (int j = needlePos - 12; j >= start; j--)
            {
                if (buf[j] == 0x09 && buf[j + 9] == 0x10 && buf[j + 10] == 0x01 && buf[j + 11] == 0x18)
                {
                    return BitConverter.ToUInt64(buf, j + 1);
                }
            }
            for (int j = needlePos - 8; j >= start; j--)
            {
                if (buf[j + 4] == 0x01 && buf[j + 5] == 0x00 && buf[j + 6] == 0x10 && buf[j + 7] == 0x01)
                {
                    return BitConverter.ToUInt64(buf, j);
                }
            }
            return 0UL;
        }

        private void WorkerLoop()
        {
            int steamPid = 0;
            ulong steamId64 = 0UL;
            IntPtr hProc = IntPtr.Zero;
            int currentPid = 0;
            long cachedAddr = 0;
            int pollTick = 0;
            int missingMatchTicks = 0;
            int lobbyConfirmTicks = 0;
            byte[] pageBuf = new byte[65536];

            while (isRunning)
            {
                try
                {
                    uint activeUser;
                    CheckProcesses(out steamPid, out activeUser, ref steamId64);
                    if (!IsSteamRunning || !IsWardogsRunning)
                    {
                        if (!IsSteamRunning && hProc != IntPtr.Zero) { CloseHandle(hProc); hProc = IntPtr.Zero; currentPid = 0; }
                        cachedAddr = 0;
                        lock (syncLock) { hasData = false; curVal = ""; curState = "frontend"; }
                        Thread.Sleep(1000);
                        continue;
                    }

                    // 1. Prefer steamwebhelper (renders Steam Friends list UI with real-time in-place status)
                    int targetPid = 0;
                    bool isRenderer = false;
                    Process renderer = FindTargetRenderer();
                    if (renderer != null && !renderer.HasExited)
                    {
                        targetPid = renderer.Id;
                        isRenderer = true;
                    }

                    // 2. Fallback to native steam.exe if renderer is not found
                    if (targetPid == 0 && steamPid != 0)
                    {
                        try
                        {
                            Process sp = Process.GetProcessById(steamPid);
                            if (!sp.HasExited) { targetPid = steamPid; isRenderer = false; }
                        }
                        catch { }
                    }
                    if (targetPid == 0)
                    {
                        Process[] sps = Process.GetProcessesByName("steam");
                        if (sps != null && sps.Length > 0 && !sps[0].HasExited) { targetPid = sps[0].Id; isRenderer = false; }
                    }
                    if (targetPid == 0)
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    if (targetPid != currentPid || hProc == IntPtr.Zero)
                    {
                        if (hProc != IntPtr.Zero) CloseHandle(hProc);
                        currentPid = targetPid;
                        hProc = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, currentPid);
                        cachedAddr = 0;
                    }
                    if (hProc == IntPtr.Zero)
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    string val = "", state = "";
                    long foundAddr = 0;
                    bool inGame = false;
                    bool ok = false;

                    // Periodic full re-scan every 10 seconds (40 ticks) to keep sync without disrupting cache
                    if (++pollTick % 40 == 0)
                    {
                        cachedAddr = 0;
                    }

                    // 1. Cached fast read (0.01ms) with owner verification
                    if (cachedAddr > 0)
                    {
                        IntPtr nRead;
                        long readStart = Math.Max(0, cachedAddr - 250);
                        int readOffset = (int)(cachedAddr - readStart);
                        if (ReadProcessMemory(hProc, (IntPtr)readStart, pageBuf, 512, out nRead) && (int)nRead > readOffset + 64)
                        {
                            ulong cacheOwner = ExtractOwnerSteamId(pageBuf, readOffset);
                            if (steamId64 != 0 && cacheOwner != steamId64)
                            {
                                // Invalidate cache immediately if not strictly matching our SteamID64!
                                cachedAddr = 0;
                            }
                            else
                            {
                                string body = Encoding.ASCII.GetString(pageBuf, readOffset, (int)nRead - readOffset);
                                Match mp = Regex.Match(body, @"profit_loss[^\$\+\-\d]*([\+\-]?\s*\$[\d,]+)");
                                if (mp.Success && !body.Contains("match_lost"))
                                {
                                    val = mp.Groups[1].Value.Replace(" ", "");
                                    Match mst = Regex.Match(body, @"in_profit_or_loss[^\w]*(loss|profit)", RegexOptions.IgnoreCase);
                                    if (mst.Success) state = mst.Groups[1].Value.ToLowerInvariant();
                                    else state = val.Contains("-") ? "loss" : "profit";
                                    inGame = true;
                                    ok = true;
                                }
                                else if (body.Contains("#Status_Frontend") || body.Contains("#Status_ShootingRange"))
                                {
                                    state = body.Contains("#Status_ShootingRange") ? "shootingrange" : "frontend";
                                    inGame = false;
                                    val = "";
                                    ok = true;
                                    cachedAddr = 0;
                                }
                                else
                                {
                                    cachedAddr = 0;
                                }
                            }
                        }
                        else
                        {
                            cachedAddr = 0;
                        }
                    }

                    // 2. Full scan for steam_display with strict owner verification
                    if (!ok)
                    {
                        long scanStart = 0x10000000000L;
                        long scanEnd = isRenderer ? 0x25000000000L : 0x40000000000L;
                        ok = ScanNativeStreamRange(hProc, scanStart, scanEnd, activeUser, steamId64, isRenderer, pageBuf, out foundAddr, out val, out state, out inGame);
                        if (ok && foundAddr > 0 && inGame)
                        {
                            cachedAddr = foundAddr;
                        }
                        else
                        {
                            cachedAddr = 0;
                        }
                    }

                    lock (syncLock)
                    {
                        if (inGame && !string.IsNullOrEmpty(val))
                        {
                            // In-game match data actively confirmed for current user
                            curVal = val;
                            curState = state;
                            hasData = true;
                            missingMatchTicks = 0;
                            lobbyConfirmTicks = 0;
                        }
                        else if (ok && (state == "frontend" || state == "shootingrange"))
                        {
                            // Lobby packet observed for current user
                            if (hasData && IsWardogsRunning)
                            {
                                if (++lobbyConfirmTicks <= 10)
                                {
                                    // Hold previous profit value firmly!
                                }
                                else
                                {
                                    curVal = "";
                                    curState = state;
                                    hasData = false;
                                    missingMatchTicks = 0;
                                    lobbyConfirmTicks = 0;
                                }
                            }
                            else
                            {
                                curVal = "";
                                curState = state;
                                hasData = false;
                                missingMatchTicks = 0;
                                lobbyConfirmTicks = 0;
                            }
                        }
                        else
                        {
                            lobbyConfirmTicks = 0;
                            if (hasData && IsWardogsRunning && ++missingMatchTicks <= 20)
                            {
                                // Keep curVal and hasData completely untouched!
                            }
                            else
                            {
                                curVal = "";
                                curState = "frontend";
                                hasData = false;
                                missingMatchTicks = 0;
                            }
                        }
                    }
                }
                catch { }

                Thread.Sleep(250);
            }

            if (hProc != IntPtr.Zero) CloseHandle(hProc);
        }

        private bool ScanNativeStreamRange(IntPtr hProc, long startAddr, long endAddr, uint activeUser, ulong steamId64, bool isRendererTarget, byte[] pageBuf, out long foundAddr, out string val, out string state, out bool inGame)
        {
            foundAddr = 0; val = ""; state = "none"; inGame = false;
            byte[] needle = Encoding.ASCII.GetBytes("steam_display");

            long latestMatchAddr = 0;
            string latestMatchVal = "";
            string latestMatchState = "profit";
            ulong maxMatchTs = 0;

            long latestIdleAddr = 0;
            ulong maxIdleTs = 0;

            long addr = startAddr;
            while (addr < endAddr && isRunning)
            {
                MEMORY_BASIC_INFORMATION mbi;
                if (VirtualQueryEx(hProc, (IntPtr)addr, out mbi, Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) == 0)
                    break;

                long baseAddr = mbi.BaseAddress.ToInt64();
                long size = mbi.RegionSize.ToInt64();
                if (size <= 0) break;
                if (baseAddr == 0) baseAddr = addr;

                if (mbi.State == 0x1000 && (mbi.Protect == 0x04 || mbi.Protect == 0x02 || mbi.Protect == 0x08))
                {
                    long offset = 0;
                    while (offset < size && isRunning)
                    {
                        int toRead = (int)Math.Min((long)pageBuf.Length, size - offset);
                        IntPtr nRead;
                        if (ReadProcessMemory(hProc, (IntPtr)(baseAddr + offset), pageBuf, toRead, out nRead))
                        {
                            int bytesRead = (int)nRead;
                            for (int i = 0; i <= bytesRead - needle.Length; i++)
                            {
                                if (pageBuf[i] == needle[0] && pageBuf[i + 1] == needle[1] && pageBuf[i + 6] == needle[6] && pageBuf[i + 12] == needle[12])
                                {
                                    bool match = true;
                                    for (int k = 2; k < needle.Length; k++)
                                    {
                                        if (pageBuf[i + k] != needle[k]) { match = false; break; }
                                    }
                                    if (match)
                                    {
                                        // Strict owner verification: Extract owner SteamID64 preceding the packet
                                        ulong ownerSteamId = ExtractOwnerSteamId(pageBuf, i);
                                        if (steamId64 != 0 && ownerSteamId != steamId64)
                                        {
                                            // Packet belongs to a friend (e.g. Steam chat / friends list) or unknown. Skip!
                                            continue;
                                        }

                                        int bodyLen = Math.Min(bytesRead - i, 250);
                                        string body = Encoding.ASCII.GetString(pageBuf, i, bodyLen);
                                        Match ms = Regex.Match(body, @"#Status_(\w+)");
                                        if (ms.Success)
                                        {
                                            string st = ms.Groups[1].Value;

                                            // Extract 8-byte Steam Rich Presence sequence ID from lobby header
                                            ulong ts = 0;
                                            int lIdx = -1;
                                            int lStart = Math.Max(0, i - 80);
                                            for (int j = i - 6; j >= lStart; j--)
                                            {
                                                if (pageBuf[j] == 'l' && pageBuf[j + 1] == 'o' && pageBuf[j + 2] == 'b' && pageBuf[j + 3] == 'b' && pageBuf[j + 4] == 'y' && pageBuf[j + 5] == 0)
                                                {
                                                    lIdx = j;
                                                    break;
                                                }
                                            }
                                            if (lIdx != -1 && lIdx + 14 <= bytesRead)
                                            {
                                                ts = BitConverter.ToUInt64(pageBuf, lIdx + 6);
                                            }
                                            else
                                            {
                                                int tIdx = FindBytesSub(pageBuf, Math.Max(0, i - 120), Math.Min(120, i), new byte[] { 0xf8, 0x02 });
                                                if (tIdx != -1) ts = DecodeVarint(pageBuf, tIdx + 2);
                                                else
                                                {
                                                    tIdx = FindBytesSub(pageBuf, Math.Max(0, i - 120), Math.Min(120, i), new byte[] { 0xe8, 0x02 });
                                                    if (tIdx != -1) ts = DecodeVarint(pageBuf, tIdx + 2);
                                                }
                                            }

                                            // If packet contains profit_loss, it is unconditionally an in-match packet!
                                            Match mp = Regex.Match(body, @"profit_loss[^\$\+\-\d]*([\+\-]?\s*\$[\d,]+)");
                                            if (mp.Success && !body.Contains("match_lost"))
                                            {
                                                string pVal = mp.Groups[1].Value.Replace(" ", "");
                                                string pState;
                                                Match mst = Regex.Match(body, @"in_profit_or_loss[^\w]*(loss|profit)", RegexOptions.IgnoreCase);
                                                if (mst.Success) pState = mst.Groups[1].Value.ToLowerInvariant();
                                                else pState = pVal.Contains("-") ? "loss" : "profit";

                                                bool isNewerMatch = (ts > maxMatchTs) || (ts == maxMatchTs && (baseAddr + offset + i) > latestMatchAddr);
                                                if (isNewerMatch)
                                                {
                                                    maxMatchTs = ts;
                                                    latestMatchAddr = baseAddr + offset + i;
                                                    latestMatchVal = pVal;
                                                    latestMatchState = pState;
                                                }
                                            }
                                            else if (st == "Frontend" || st == "ShootingRange")
                                            {
                                                bool isNewerIdle = (ts > maxIdleTs) || (ts == maxIdleTs && (baseAddr + offset + i) > latestIdleAddr);
                                                if (isNewerIdle)
                                                {
                                                    maxIdleTs = ts;
                                                    latestIdleAddr = baseAddr + offset + i;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        offset += toRead;
                    }
                }

                long nextAddr = baseAddr + size;
                if (nextAddr <= addr) break;
                addr = nextAddr;
            }

            bool matchIsNewer = false;
            if (latestMatchAddr > 0 && !string.IsNullOrEmpty(latestMatchVal))
            {
                if (maxMatchTs > 0 || maxIdleTs > 0)
                {
                    matchIsNewer = (maxMatchTs > maxIdleTs) || (maxMatchTs == maxIdleTs && latestMatchAddr >= latestIdleAddr);
                }
                else
                {
                    matchIsNewer = latestIdleAddr == 0 || latestMatchAddr >= latestIdleAddr;
                }
            }

            if (matchIsNewer && latestMatchAddr > 0 && !string.IsNullOrEmpty(latestMatchVal))
            {
                foundAddr = latestMatchAddr;
                val = latestMatchVal;
                state = latestMatchState;
                inGame = true;
                return true;
            }

            if (latestIdleAddr > 0)
            {
                foundAddr = latestIdleAddr;
                val = "";
                state = "frontend";
                inGame = false;
                return true;
            }

            return false;
        }


private static ulong DecodeVarint(byte[] data, int pos)



{



ulong val = 0;



int shift = 0;



while (pos < data.Length)



{



byte b = data[pos++];



val |= ((ulong)(b & 0x7f)) << shift;



if ((b & 0x80) == 0) break;



shift += 7;



if (shift >= 64) break;



}



return val;



}



private static int FindBytesSub(byte[] src, int startIdx, int count, byte[] pattern)



{



if (pattern == null || pattern.Length == 0) return -1;



int max = startIdx + count - pattern.Length;



for (int i = startIdx; i <= max; i++)



{



bool match = true;



for (int j = 0; j < pattern.Length; j++)



{



if (src[i + j] != pattern[j]) { match = false; break; }



}



if (match) return i;



}



return -1;



}



public void Dispose()



{



isRunning = false;



try { bgWorker.Abort(); } catch { }



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



private SteamDirectProfitReader steamReader;



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



this.ClientSize = new Size(320, 236);



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



steamReader = new SteamDirectProfitReader();



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



Label lblCredit = new Label()



{



Text = "by HIST",



Font = new Font("맑은 고딕", 8.5f, FontStyle.Bold),



ForeColor = Color.FromArgb(255, 238, 140),



Location = new Point(14, 214),



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



