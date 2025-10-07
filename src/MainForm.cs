using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using OpenAI;

namespace DokodemoLLM
{
  public partial class MainForm : Form
  {
    private ComboBox promptCombo;
    private Label promptLabel;
    private TextBox promptEdit;
    private Button okButton;
    private Button closeButton;
    private Button clearButton;
    private CheckBox webSearchCheck;
    private Label statusLabel;

    private string userText = "";

    public MainForm(string selectedText)
    {
      userText = selectedText;

      // フォームを初期化
      InitializeComponent();
      this.FormClosed += MainForm_FormClosed;
      this.Shown += MainForm_Shown;
    }

    private void MainForm_Shown(object sender, EventArgs e)
    {
      // ウインドウをアクティブにする
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

      // クローズボタン
      closeButton = new Button();
      closeButton.Text = "クローズ";
      closeButton.Size = new System.Drawing.Size(100, 40);
      closeButton.Location = new System.Drawing.Point(240, 510);
      closeButton.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      closeButton.Click += CloseButton_Click;

      // クリアボタン
      clearButton = new Button();
      clearButton.Text = "クリア";
      clearButton.Size = new System.Drawing.Size(100, 40);
      clearButton.Location = new System.Drawing.Point(130, 510);
      clearButton.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      clearButton.Click += ClearButton_Click;

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

    private void PromptCombo_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (promptCombo.SelectedIndex >= 0)
      {
        promptEdit.Text = promptCombo.SelectedItem.ToString();
      }
    }

    private void CloseButton_Click(object sender, EventArgs e)
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
          // システムプロンプトを作成
          string systemPrompt = promptText;
          if (webSearchCheck.Checked)
          {
            systemPrompt += " /w";
          }

          // APIを呼び出し
          string result = await CallAPI(systemPrompt, userText);

        if (!string.IsNullOrEmpty(result))
        {
          // 結果をダイアログで表示
          using (var dialog = new ResultDialog(result))
          {
            var dialogResult = dialog.ShowDialog(this);
            if (dialogResult == DialogResult.OK)
            {
              statusLabel.Text = "結果をクリップボードにコピーしました";
              statusLabel.ForeColor = Color.Green;
            }
            else
            {
              statusLabel.Text = "キャンセルしました";
              statusLabel.ForeColor = Color.Blue;
            }
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

    private async Task<string> CallAPI(string systemPrompt, string userText)
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
        OpenAI.Chat.ChatMessage.CreateUserMessage(userText)
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

  }
}