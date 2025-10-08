using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace DokodemoLLM
{
  class Program
  {
    // キーコード定数
    private const byte VK_CONTROL = 0x11;
    private const byte VK_C = 0x43;
    private const byte VK_L_WINDOWS = 0x5B;
    private const byte VK_R_WINDOWS = 0x5C;
    private const uint KEYEVENTF_KEYUP = 0x0002;
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
          _hookHandle = SetWindowsHookEx(
             WH_KEYBOARD_LL,                       // フックするイベントの種類（13：キーボード）
             _callBack,                            // フック時のコールバック関数
             GetModuleHandle(module.ModuleName),   // インスタンスハンドル
             0                                     // スレッドID（0：全てのスレッドでフック）
         );
         
          if (_hookHandle == IntPtr.Zero)
          {
            Console.WriteLine("フック登録に失敗しました");
            Console.WriteLine($"モジュール名: {module.ModuleName}");
            int error = Marshal.GetLastWin32Error();
            Console.WriteLine($"エラーコード: {error}");
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
          // Win + Ctrl + C の組み合わせを監視
          if (key == Keys.C && _winKeyPressed && _ctrlKeyPressed)
          {
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
              try
              {
                // アクティブなウィンドウの選択されたテキストを取得
                IntPtr activeWindowHandle = GetForegroundWindow();
                string selectedText = GetSelectedText(activeWindowHandle);
                
                _mainForm = new MainForm(selectedText);
                _mainForm.ShowDialog();
              }
              catch
              {
                // エラーが発生した場合は空のテキストでフォームを表示
                _mainForm = new MainForm("");
                _mainForm.ShowDialog();
              }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
          }
        }
      }

      // 他のフックにキー情報を転送
      return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    // 選択されたテキストを取得
    public static string GetSelectedText(IntPtr activeWindowHandle)
    {
      try
      {
        // クリップボードのバックアップ
        string clipboardBackup = "";
        clipboardBackup = Clipboard.GetText();
        
        // アクティブウィンドウのスレッドIDを取得
        uint activeThreadId = GetWindowThreadProcessId(activeWindowHandle, out uint _);
        uint currentThreadId = GetCurrentThreadId();

        // スレッドの入力状態を接続
        bool attachSuccess = AttachThreadInput(currentThreadId, activeThreadId, true);
        
        if (attachSuccess)
        {
          try
          {
            // アクティブウィンドウをフォアグラウンドに設定
            SetForegroundWindow(activeWindowHandle);
            SetFocus(activeWindowHandle);
              
            // Ctrl+C を送信
            keybd_event(VK_L_WINDOWS, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_R_WINDOWS, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_C, 0, 0, UIntPtr.Zero);
            keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // キー入力が反映されるまで少し待機
            System.Threading.Thread.Sleep(50);
          }
          finally
          {
            // スレッドの入力状態を切断
            AttachThreadInput(currentThreadId, activeThreadId, false);
          }
        }

        // クリップボードの内容が更新されるまで少し待機
        System.Threading.Thread.Sleep(100);

        // 選択されたテキストを取得
        string selectedText = "";
        selectedText = Clipboard.GetText();

        // クリップボードを元に戻す
        Clipboard.SetText(clipboardBackup);

        return selectedText;
      }
      catch
      {
        // エラーが発生した場合は空の文字列を返す
        return "";
      }
    }

    // デリゲートの定義
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

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
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool SetFocus(IntPtr hWnd);
  }
}