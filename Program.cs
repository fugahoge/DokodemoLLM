using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace DokodemoLLM
{
    class Program
    {
        private const int WH_KEYBOARD_LL = 0x0D;

        private static IntPtr _hookHandle = IntPtr.Zero;
        private static readonly LowLevelKeyboardProc _callBack = CallbackProc;
        private static Form _mainForm = null;
        
        // キーの状態を追跡する変数
        private static bool _winKeyPressed = false;
        private static bool _ctrlKeyPressed = false;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // タスクトレイアイコンを作成
            NotifyIcon trayIcon = new NotifyIcon();
            trayIcon.Icon = System.Drawing.SystemIcons.Application;
            trayIcon.Text = "DokodemoLLM";
            trayIcon.Visible = true;

            // タスクトレイメニューを作成
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("終了", null, (s, e) => {
                UnhookKeyboard();
                trayIcon.Visible = false;
                Application.Exit();
            });
            trayIcon.ContextMenuStrip = contextMenu;

            // キーボードフックを開始
            HookKeyboard();

            // アプリケーションを実行（ウィンドウは表示しない）
            Application.Run();
        }

        private static void HookKeyboard()
        {
            // キーボードフックの登録
            using (Process process = Process.GetCurrentProcess())
            {
                using (ProcessModule module = process.MainModule)
                {
                    Console.WriteLine($"フック登録を試行中... モジュール名: {module.ModuleName}");
                    
                    _hookHandle = SetWindowsHookEx(
                       WH_KEYBOARD_LL,                       // フックするイベントの種類（13：キーボード）
                       _callBack,                            // フック時のコールバック関数
                       GetModuleHandle(module.ModuleName),   // インスタンスハンドル
                       0                                     // スレッドID（0：全てのスレッドでフック）
                   );
                   
                    if (_hookHandle == IntPtr.Zero)
                    {
                        Console.WriteLine("フック登録に失敗しました！");
                        int error = Marshal.GetLastWin32Error();
                        Console.WriteLine($"エラーコード: {error}");
                    }
                    else
                    {
                        Console.WriteLine("フック登録が成功しました！");
                    }
                }
            }
        }

        private static void UnhookKeyboard()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }

        public static void ClearMainForm()
        {
            _mainForm = null;
        }

        private static IntPtr CallbackProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Keys key = (Keys)(short)Marshal.ReadInt32(lParam);
                bool isKeyDown = (int)wParam == 0x0100; // WM_KEYDOWN
                bool isKeyUp = (int)wParam == 0x0101;   // WM_KEYUP

                // キーの状態を更新
                if (isKeyDown)
                {
                    if (key == Keys.LWin || key == Keys.RWin)
                        _winKeyPressed = true;
                    else if (key == Keys.LControlKey || key == Keys.RControlKey)
                        _ctrlKeyPressed = true;
                }
                else if (isKeyUp)
                {
                    if (key == Keys.LWin || key == Keys.RWin)
                        _winKeyPressed = false;
                    else if (key == Keys.LControlKey || key == Keys.RControlKey)
                        _ctrlKeyPressed = false;
                }

                // キーボードが押された時のみ処理
                if (isKeyDown)
                {
                    Console.WriteLine($"キーが押されました: {key}");
                    Console.WriteLine($"Win: {_winKeyPressed}, Ctrl: {_ctrlKeyPressed}, Key: {key}");
                    
                    // Win + Ctrl + C の組み合わせを監視
                    if (key == Keys.C && _winKeyPressed && _ctrlKeyPressed)
                    {
                        Console.WriteLine("Win + Ctrl + C の組み合わせが押されました");
                        
                        // 既存のフォームがある場合は閉じる
                        if (_mainForm != null && !_mainForm.IsDisposed)
                        {
                            try
                            {
                                _mainForm.Invoke(new Action(() => {
                                    _mainForm.Close();
                                }));
                            }
                            catch
                            {
                                // フォームが既に閉じられている場合
                                _mainForm = null;
                            }
                        }
                        
                        // 新しいフォームを作成して表示
                        Thread thread = new Thread(() => {
                            _mainForm = new MainForm();
                            _mainForm.ShowDialog();
                        });
                        thread.SetApartmentState(ApartmentState.STA);
                        thread.Start();
                    }
                }
            }

            // 他のフックにキー情報を転送
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        // Win32 API のインポート
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // デリゲートの定義
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    }
}
