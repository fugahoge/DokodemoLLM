using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using OpenAI;
using Newtonsoft.Json;

namespace DokodemoLLM
{
  public partial class MainForm : Form
  {
    private ComboBox promptCombo;
    private Label promptLabel;
    private TextBox promptEdit;
    private Button okButton;
    private Button cancelButton;
    private Button clearButton;
    private CheckBox webSearchCheck;
    private Label statusLabel;
    
    private static readonly HttpClient httpClient = new HttpClient();

    public MainForm()
    {
      InitializeComponent();
      this.FormClosed += MainForm_FormClosed;
      this.Shown += MainForm_Shown;
    }

    private void MainForm_Shown(object sender, EventArgs e)
    {
      // フォームが表示された時にフォーカスを取得
      this.BringToFront();
      this.Activate();
      this.Focus();
    }

    private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
    {
      // フォームが閉じられた時にProgramクラスの_mainFormをクリア
      Program.ClearMainForm();
    }

    private void InitializeComponent()
    {
      this.Text = "DokodemoLLM - AI アシスタント";
      this.Size = new System.Drawing.Size(1040, 600);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.FormBorderStyle = FormBorderStyle.FixedSingle;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.TopMost = true;  // 最前面に表示
      this.ShowInTaskbar = false;  // タスクバーに表示しない

      // フォント設定
      this.Font = new System.Drawing.Font("Yu Gothic UI", 14F, System.Drawing.FontStyle.Regular);

      // ウェブ検索チェックボックス
      webSearchCheck = new CheckBox();
      webSearchCheck.Text = "ウェブ検索を使用する (/w)";
      webSearchCheck.Size = new System.Drawing.Size(300, 30);
      webSearchCheck.Location = new System.Drawing.Point(20, 20);
      webSearchCheck.Font = new System.Drawing.Font("Yu Gothic UI", 14F);

      // プロンプト選択用ComboBox
      promptCombo = new ComboBox();
      promptCombo.DropDownStyle = ComboBoxStyle.DropDownList;
      promptCombo.Size = new System.Drawing.Size(1000, 30);
      promptCombo.Location = new System.Drawing.Point(20, 60);
      promptCombo.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      
      // デフォルトのプロンプトを追加
      promptCombo.Items.AddRange(new string[] {
        "要約してください：",
        "以下の文章を翻訳してください：",
        "次のトピックについて詳しく説明してください："
      });

      // プロンプト入力ラベル
      promptLabel = new Label();
      promptLabel.Text = "プロンプト入力（改行可能）:";
      promptLabel.Size = new System.Drawing.Size(1000, 30);
      promptLabel.Location = new System.Drawing.Point(20, 110);
      promptLabel.Font = new System.Drawing.Font("Yu Gothic UI", 14F);

      // プロンプト入力エリア
      promptEdit = new TextBox();
      promptEdit.Multiline = true;
      promptEdit.ScrollBars = ScrollBars.Vertical;
      promptEdit.WordWrap = true;
      promptEdit.Size = new System.Drawing.Size(1000, 300);
      promptEdit.Location = new System.Drawing.Point(20, 150);
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

      // キャンセルボタン
      cancelButton = new Button();
      cancelButton.Text = "キャンセル";
      cancelButton.Size = new System.Drawing.Size(150, 40);
      cancelButton.Location = new System.Drawing.Point(130, 510);
      cancelButton.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      cancelButton.Click += CancelButton_Click;

      // クリアボタン
      clearButton = new Button();
      clearButton.Text = "クリア";
      clearButton.Size = new System.Drawing.Size(100, 40);
      clearButton.Location = new System.Drawing.Point(290, 510);
      clearButton.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      clearButton.Click += ClearButton_Click;

      // ComboBoxの選択変更イベント
      promptCombo.SelectedIndexChanged += PromptCombo_SelectedIndexChanged;

      // コントロールをフォームに追加
      this.Controls.Add(webSearchCheck);
      this.Controls.Add(promptCombo);
      this.Controls.Add(promptLabel);
      this.Controls.Add(promptEdit);
      this.Controls.Add(statusLabel);
      this.Controls.Add(okButton);
      this.Controls.Add(cancelButton);
      this.Controls.Add(clearButton);
    }

    private void PromptCombo_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (promptCombo.SelectedIndex >= 0)
      {
        promptEdit.Text = promptCombo.SelectedItem.ToString();
      }
    }

    private void CancelButton_Click(object sender, EventArgs e)
    {
      // フォームを閉じる
      this.Close();
    }

    private void ClearButton_Click(object sender, EventArgs e)
    {
      // 入力エリアをクリア
      promptEdit.Text = "";
      promptCombo.SelectedIndex = -1;
    }

    private async void OkButton_Click(object sender, EventArgs e)
    {
      // プロンプトテキストを取得
      string promptText = promptEdit.Text.Trim();
      
      if (string.IsNullOrEmpty(promptText))
      {
        statusLabel.Text = "プロンプトを入力してください";
        statusLabel.ForeColor = Color.Red;
        return;
      }

      // 履歴に追加（重複チェック）
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
        // クリップボードからテキストを取得
        string userText = "";
        try
        {
          userText = Clipboard.GetText();
        }
        catch
        {
          statusLabel.Text = "クリップボードの読み取りに失敗しました";
          statusLabel.ForeColor = Color.Red;
          return;
        }

        if (string.IsNullOrEmpty(userText))
        {
          statusLabel.Text = "クリップボードにテキストがありません";
          statusLabel.ForeColor = Color.Red;
          return;
        }

        // プロンプトを分割
        string systemPrompt = promptText;
        if (webSearchCheck.Checked)
        {
          systemPrompt += " /w";
        }

        // AI APIを呼び出し
        string result = await CallOpenAIAPI(systemPrompt, userText);

        if (!string.IsNullOrEmpty(result))
        {
          // 結果をクリップボードにコピー
          try
          {
            Clipboard.SetText(result);
            statusLabel.Text = "結果をクリップボードにコピーしました";
            statusLabel.ForeColor = Color.Green;
          }
          catch
          {
            statusLabel.Text = "結果のクリップボードへのコピーに失敗しました";
            statusLabel.ForeColor = Color.Red;
          }
        }
        else
        {
          statusLabel.Text = "APIの呼び出しに失敗しました";
          statusLabel.ForeColor = Color.Red;
        }
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

    private async Task<string> CallOpenAIAPI(string systemPrompt, string userText)
    {
      // エンドポイントを指定
      var endpoint = new Uri("http://127.0.0.1:1234/v1/");
      var credential = new System.ClientModel.ApiKeyCredential("dummy");
      var options = new OpenAI.OpenAIClientOptions
      {
        Endpoint = endpoint
      };
      var client = new OpenAIClient(credential, options);
      
      var messages = new List<OpenAI.Chat.ChatMessage>
      {
        OpenAI.Chat.ChatMessage.CreateSystemMessage(systemPrompt),
        OpenAI.Chat.ChatMessage.CreateUserMessage(userText)
      };

      // モデル名をgoogle/gemma-3-4bに変更
      var chatClient = client.GetChatClient("google/gemma-3-4b");
      var completion = await chatClient.CompleteChatAsync(messages, new OpenAI.Chat.ChatCompletionOptions
      {
        Temperature = 0.7f,
        MaxOutputTokenCount = 4096
      });

      // レスポンスの内容を取得
      if (completion.Value?.Content != null && completion.Value.Content.Count > 0)
      {
        return completion.Value.Content[0].Text ?? "";
      }
      else
      {
        throw new Exception("APIの応答が空です");
      }
    }
    
    //private async Task<string> CallAIAPI(string systemPrompt, string userText)
    //{
    //  try
    //  {
    //    // ウェブ検索の処理
    //    bool doWebSearch = systemPrompt.Trim().EndsWith("/w");
    //    if (doWebSearch)
    //    {
    //      systemPrompt = systemPrompt.Trim().Substring(0, systemPrompt.Trim().Length - 2).Trim();
    //      var searchResults = await SearchWeb(userText.Trim());
    //      var pageExcerpts = await FetchPageExcerpts(searchResults.Item2);
    //      userText = $"{userText.Trim()}\n###以下の検索結果も必要に応じて参照してください：\n{pageExcerpts}\n";
    //    }

    //    return await CallOpenAIAPI(systemPrompt, userText);
    //  }
    //  catch (Exception ex)
    //  {
    //    throw new Exception($"API呼び出しエラー: {ex.Message}");
    //  }
    //}

    //private async Task<(string, List<string>)> SearchWeb(string query)
    //{
    //  try
    //  {
    //    var url = "https://html.duckduckgo.com/html/";
    //    var headers = new Dictionary<string, string>
    //    {
    //      { "User-Agent", "Mozilla/5.0" }
    //    };
    //    var data = new Dictionary<string, string>
    //    {
    //      { "q", query }
    //    };

    //    var content = new FormUrlEncodedContent(data);
    //    var request = new HttpRequestMessage(HttpMethod.Post, url)
    //    {
    //      Content = content
    //    };

    //    foreach (var header in headers)
    //    {
    //      request.Headers.Add(header.Key, header.Value);
    //    }

    //    var response = await httpClient.SendAsync(request);
    //    var html = await response.Content.ReadAsStringAsync();

    //    var doc = new HtmlAgilityPack.HtmlDocument();
    //    doc.LoadHtml(html);

    //    var results = new List<string>();
    //    var urlList = new List<string>();

    //    var links = doc.DocumentNode.SelectNodes("//a[@class='result__a']");
    //    if (links != null)
    //    {
    //      foreach (var link in links.Take(3))
    //      {
    //        var title = link.InnerText.Trim();
    //        var href = link.GetAttributeValue("href", "");
    //        if (!string.IsNullOrEmpty(href))
    //        {
    //          results.Add($"{title} - {href}");
    //          urlList.Add(href);
    //        }
    //      }
    //    }

    //    return (string.Join("\n", results), urlList);
    //  }
    //  catch (Exception ex)
    //  {
    //    throw new Exception($"ウェブ検索エラー: {ex.Message}");
    //  }
    //}

    //private async Task<string> FetchPageExcerpts(List<string> urls)
    //{
    //  var excerpts = new List<string>();
      
    //  foreach (var url in urls.Take(3))
    //  {
    //    try
    //    {
    //      var excerpt = await FetchPageExcerpt(url, 1000);
    //      excerpts.Add($"[{excerpts.Count + 1}] {url}\n{excerpt}\n");
    //    }
    //    catch
    //    {
    //      excerpts.Add($"[{excerpts.Count + 1}] {url}\n[ページ本文取得エラー]\n");
    //    }
    //  }

    //  return string.Join("", excerpts);
    //}

    //private async Task<string> FetchPageExcerpt(string url, int maxChars)
    //{
    //  try
    //  {
    //    var headers = new Dictionary<string, string>
    //    {
    //      { "User-Agent", "Mozilla/5.0" }
    //    };

    //    var request = new HttpRequestMessage(HttpMethod.Get, url);
    //    foreach (var header in headers)
    //    {
    //      request.Headers.Add(header.Key, header.Value);
    //    }

    //    var response = await httpClient.SendAsync(request);
    //    var html = await response.Content.ReadAsStringAsync();

    //    var doc = new HtmlAgilityPack.HtmlDocument();
    //    doc.LoadHtml(html);
    //    var text = doc.DocumentNode.InnerText;
    //    text = Regex.Replace(text, @"\s+", " ").Trim();
        
    //    return text.Length > maxChars ? text.Substring(0, maxChars) : text;
    //  }
    //  catch (Exception ex)
    //  {
    //    return $"[ページ本文取得エラー: {ex.Message}]";
    //  }
    //}
  }
}