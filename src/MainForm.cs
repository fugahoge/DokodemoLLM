using OpenAI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using HtmlAgilityPack;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DokodemoLLM
{
  public partial class MainForm : Form
  {
    private static readonly HttpClient httpClient = new HttpClient();

    private ComboBox promptCombo = null!;
    private Label promptLabel = null!;
    private TextBox promptEdit = null!;
    private Button okButton = null!;
    private Button closeButton = null!;
    private Button clearButton = null!;
    private CheckBox webSearchCheck = null!;
    private Label statusLabel = null!;

    public string selectText = "";
    public string resultText = "";
    public IntPtr activeWindowHandle = IntPtr.Zero;

    public MainForm(string selectText)
    {
      this.selectText = selectText;

      // フォームを初期化
      InitializeComponent();
      this.FormClosed += MainForm_FormClosed;
      this.Shown += MainForm_Shown;
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
      // ウインドウをアクティブにする
      this.BringToFront();
      this.Activate();
      this.Focus();
    }

    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
      // MainFormが閉じられた後に実行する処理
      if (this.DialogResult == DialogResult.Yes && !string.IsNullOrEmpty(this.resultText) && this.activeWindowHandle != IntPtr.Zero)
      {
        // 結果を選択されたテキストとして設定
        Program.SetSelectedText(this.activeWindowHandle, this.resultText);
      }
      
      // フォームが閉じられた時にProgramクラスの_mainFormをクリア
      Program.ClearMainForm();
    }

    private void InitializeComponent()
    {
      this.Text = "DokodemoLLM";
      this.Size = new System.Drawing.Size(1024, 600);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.FormBorderStyle = FormBorderStyle.FixedSingle;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.ShowInTaskbar = false;

      // フォント設定
      this.Font = new System.Drawing.Font("Yu Gothic UI", 14F, System.Drawing.FontStyle.Regular);

      // ウェブ検索チェックボックス
      webSearchCheck = new CheckBox();
      webSearchCheck.Text = "ウェブ検索を使用する";
      webSearchCheck.Size = new System.Drawing.Size(300, 30);
      webSearchCheck.Location = new System.Drawing.Point(20, 420);
      webSearchCheck.Font = new System.Drawing.Font("Yu Gothic UI", 14F);

      // プロンプト選択用コンボボックス
      promptCombo = new ComboBox();
      promptCombo.DropDownStyle = ComboBoxStyle.DropDownList;
      promptCombo.Size = new System.Drawing.Size(964, 30);
      promptCombo.Location = new System.Drawing.Point(20, 60);
      promptCombo.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      
      // 設定ファイルからプロンプトを読み込んでコンボボックスに追加
      var config = ConfigManager.Config;
      promptCombo.Items.AddRange(config.Prompts.ToArray());

      // プロンプト入力ラベル
      promptLabel = new Label();
      promptLabel.Text = "プロンプト：";
      promptLabel.Size = new System.Drawing.Size(964, 30);
      promptLabel.Location = new System.Drawing.Point(20, 20);
      promptLabel.Font = new System.Drawing.Font("Yu Gothic UI", 14F);

      // プロンプト入力エリア
      promptEdit = new TextBox();
      promptEdit.Multiline = true;
      promptEdit.ScrollBars = ScrollBars.Vertical;
      promptEdit.WordWrap = true;
      promptEdit.Size = new System.Drawing.Size(964, 300);
      promptEdit.Location = new System.Drawing.Point(20, 110);
      promptEdit.Font = new System.Drawing.Font("Yu Gothic UI", 14F);

      // ステータスラベル
      statusLabel = new Label();
      statusLabel.Text = "準備完了";
      statusLabel.Size = new System.Drawing.Size(1000, 30);
      statusLabel.Location = new System.Drawing.Point(20, 470);
      statusLabel.Font = new System.Drawing.Font("Yu Gothic UI", 12F);
      statusLabel.ForeColor = Color.Blue;

      // OKボタン
      okButton = new Button();
      okButton.Text = "実行";
      okButton.Size = new System.Drawing.Size(100, 40);
      okButton.Location = new System.Drawing.Point(20, 510);
      okButton.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      okButton.Click += OkButton_Click;

      // クリアボタン
      clearButton = new Button();
      clearButton.Text = "クリア";
      clearButton.Size = new System.Drawing.Size(100, 40);
      clearButton.Location = new System.Drawing.Point(130, 510);
      clearButton.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      clearButton.Click += ClearButton_Click;

      // 閉じるボタン
      closeButton = new Button();
      closeButton.Text = "閉じる";
      closeButton.Size = new System.Drawing.Size(100, 40);
      closeButton.Location = new System.Drawing.Point(240, 510);
      closeButton.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      closeButton.Click += CloseButton_Click;

      // コンボボックスの選択変更イベント
      promptCombo.SelectedIndexChanged += PromptCombo_SelectedIndexChanged;

      // コンボボックスの先頭のアイテムを選択
      if (promptCombo.Items.Count > 0)
      {
        promptCombo.SelectedIndex = 0;
      }

      // コントロールをフォームに追加
      this.Controls.Add(webSearchCheck);
      this.Controls.Add(promptCombo);
      this.Controls.Add(promptLabel);
      this.Controls.Add(promptEdit);
      this.Controls.Add(statusLabel);
      this.Controls.Add(okButton);
      this.Controls.Add(closeButton);
      this.Controls.Add(clearButton);
    }

    private void PromptCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
      if (promptCombo.SelectedIndex >= 0)
      {
        promptEdit.Text = promptCombo.SelectedItem?.ToString() ?? "";
      }
    }

    private void CloseButton_Click(object? sender, EventArgs e)
    {
      // フォームを閉じる
      this.DialogResult = DialogResult.Abort;
      this.Close();
    }

    private void ClearButton_Click(object? sender, EventArgs e)
    {
      // 入力エリアをクリア
      promptEdit.Text = "";
      promptCombo.SelectedIndex = -1;
    }

    private async void OkButton_Click(object? sender, EventArgs e)
    {
      // プロンプトテキストを取得
      string promptText = promptEdit.Text.Trim();
      
      if (string.IsNullOrEmpty(promptText))
      {
        statusLabel.Text = "プロンプトを入力してください";
        statusLabel.ForeColor = Color.Red;
        return;
      }

      // プロンプトを履歴に追加
      if (!promptCombo.Items.Contains(promptText))
      {
        promptCombo.Items.Insert(0, promptText);
        promptCombo.SelectedIndex = 0;
      }
      
      // ボタンを無効化
      okButton.Enabled = false;
      statusLabel.Text = "処理中...";
      statusLabel.ForeColor = Color.Orange;

      try
      {
        // ウェブ検索を使用する場合
        if (webSearchCheck.Checked)
        {
          // ウェブ検索の処理
          var searchResults = await SearchWeb(selectText.Trim());
          var pageExcerpts = await FetchPageExcerpts(searchResults.Item2);
          promptText = $"{promptText.Trim()}\n###以下の検索結果も必要に応じて参照してください：\n{pageExcerpts}\n";
        }

        // APIを呼び出し
        this.resultText = await CallAPI(promptText, selectText);
        if (string.IsNullOrEmpty(this.resultText)) return;

        // 結果をダイアログで表示
        using (var dialog = new ResultDialog(this.resultText))
        {
          var dialogResult = dialog.ShowDialog(this);

          // ダイアログの結果に応じて処理
          if (dialogResult == DialogResult.Cancel)
          {
            statusLabel.Text = "キャンセルしました";
            statusLabel.ForeColor = Color.Blue;
            return;
          }

          // 新しい選択テキストを作成
          if (dialogResult == DialogResult.Yes)
          {
            this.resultText = selectText + "\n" + this.resultText + "\n";
          }
          else
          {
            this.resultText = selectText;
          }

          // クリップボードにコピー
          if (CopyToClipboard(this.resultText))
          {
            statusLabel.Text = "結果をクリップボードにコピーしました";
            statusLabel.ForeColor = Color.Green;
          }
          else
          {
            statusLabel.Text = "クリップボードへのコピーに失敗しました";
            statusLabel.ForeColor = Color.Red;
          }
        }

        // フォームを閉じる
        this.DialogResult = DialogResult.Yes;
        this.Close();
      }
      catch (Exception ex)
      {
        statusLabel.Text = $"エラー: {ex.Message}";
        statusLabel.ForeColor = Color.Red;
      }
      finally
      {
        okButton.Enabled = true;
      }
    }

    private async Task<string> CallAPI(string systemPrompt, string selectText)
    {
      // 設定ファイルから設定を読み込み
      var config = ConfigManager.Config;
      
      // エンドポイントを指定
      var endpoint = new Uri(config.Endpoint);
      var credential = new System.ClientModel.ApiKeyCredential(config.ApiKey);
      var options = new OpenAI.OpenAIClientOptions
      {
        Endpoint = endpoint
      };
      var client = new OpenAIClient(credential, options);
      
      var messages = new List<OpenAI.Chat.ChatMessage>
      {
        OpenAI.Chat.ChatMessage.CreateSystemMessage(systemPrompt),
        OpenAI.Chat.ChatMessage.CreateUserMessage(selectText)
      };

      // モデルを設定
      var chatClient = client.GetChatClient(config.Model);
      var completion = await chatClient.CompleteChatAsync(messages, new OpenAI.Chat.ChatCompletionOptions
      {
        Temperature = config.Temperature,
        MaxOutputTokenCount = config.MaxOutputTokenCount
      });

      // レスポンスの内容を取得
      if (completion.Value?.Content == null || completion.Value.Content.Count == 0)
      {
        throw new Exception("APIの応答が空です");
      }

      return completion.Value.Content[0].Text ?? "";
    }

    // ウェブ検索
    private async Task<(string, List<string>)> SearchWeb(string query)
    {
      try
      {
        var url = "https://html.duckduckgo.com/html/";
        var headers = new Dictionary<string, string>
        {
          { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36" }
        };
        var data = new Dictionary<string, string>
        {
          { "q", query }
        };

        var content = new FormUrlEncodedContent(data);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
          Content = content
        };

        foreach (var header in headers)
        {
          request.Headers.Add(header.Key, header.Value);
        }

        var response = await httpClient.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        var doc = new HtmlAgilityPack.HtmlDocument();
        HtmlNode.ElementsFlags.Remove("form");
        doc.LoadHtml(html);

        var results = new List<string>();
        var urlList = new List<string>();

        var links = doc.DocumentNode.SelectNodes("//a[@class='result__a']");
        if (links != null)
        {
          foreach (var link in links.Take(3))
          {
            var title = link.InnerText.Trim();
            var href = link.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(href))
            {
              results.Add($"{title} - {href}");
              urlList.Add(href);
            }
          }
        }

        return (string.Join("\n", results), urlList);
      }
      catch (Exception ex)
      {
        throw new Exception($"ウェブ検索エラー: {ex.Message}");
      }
    }

    // 
    private async Task<string> FetchPageExcerpts(List<string> urls)
    {
      var excerpts = new List<string>();
      
      foreach (var url in urls.Take(3))
      {
        try
        {
          var excerpt = await FetchPageExcerpt(url, 1000);
          excerpts.Add($"[{excerpts.Count + 1}] {url}\n{excerpt}\n");
        }
        catch
        {
          excerpts.Add($"[{excerpts.Count + 1}] {url}\n[ページ本文取得エラー]\n");
        }
      }

      return string.Join("", excerpts);
    }

    private async Task<string> FetchPageExcerpt(string url, int maxChars)
    {
      try
      {
        var headers = new Dictionary<string, string>
        {
          { "User-Agent", "Mozilla/5.0" }
        };

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var header in headers)
        {
          request.Headers.Add(header.Key, header.Value);
        }

        var response = await httpClient.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);
        var text = doc.DocumentNode.InnerText;
        text = Regex.Replace(text, @"\s+", " ").Trim();
        
        return text.Length > maxChars ? text.Substring(0, maxChars) : text;
      }
      catch (Exception ex)
      {
        return $"[ページ本文取得エラー: {ex.Message}]";
      }
    }

    // クリップボードにテキストをコピーするメソッド
    public bool CopyToClipboard(string text)
    {
      try
      {
        Clipboard.SetText(text);
        return true;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"クリップボードへのコピーに失敗しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
      }
    }

  }
}