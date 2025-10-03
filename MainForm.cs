using System;
using System.Windows.Forms;
using System.Drawing;

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

    public MainForm()
    {
      InitializeComponent();
      this.FormClosed += MainForm_FormClosed;
      this.Shown += MainForm_Shown;
    }

    private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
    {
      // フォームが閉じられた時にProgramクラスの_mainFormをクリア
      Program.ClearMainForm();
    }

    private void MainForm_Shown(object sender, EventArgs e)
    {
      // フォームが表示された時にフォーカスを取得
      this.BringToFront();
      this.Activate();
      this.Focus();
    }

    private void InitializeComponent()
    {
      this.Text = "プロンプトの入力";
      this.Size = new System.Drawing.Size(1040, 500);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.FormBorderStyle = FormBorderStyle.FixedSingle;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.TopMost = true;  // 最前面に表示
      this.ShowInTaskbar = false;  // タスクバーに表示しない

      // フォント設定
      this.Font = new System.Drawing.Font("Yu Gothic UI", 14F, System.Drawing.FontStyle.Regular);

      // 履歴選択用ComboBox
      promptCombo = new ComboBox();
      promptCombo.DropDownStyle = ComboBoxStyle.DropDownList;
      promptCombo.Size = new System.Drawing.Size(1000, 30);
      promptCombo.Location = new System.Drawing.Point(20, 20);
      promptCombo.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      
      // デフォルトの履歴項目を追加
      promptCombo.Items.AddRange(new string[] {
        "要約してください：",
        "以下の文章を翻訳してください：",
        "次のトピックについて詳しく説明してください："
      });

      // プロンプト入力ラベル
      promptLabel = new Label();
      promptLabel.Text = "プロンプト入力（改行可能）:";
      promptLabel.Size = new System.Drawing.Size(1000, 30);
      promptLabel.Location = new System.Drawing.Point(20, 70);
      promptLabel.Font = new System.Drawing.Font("Yu Gothic UI", 14F);

      // プロンプト入力エリア
      promptEdit = new TextBox();
      promptEdit.Multiline = true;
      promptEdit.ScrollBars = ScrollBars.Vertical;
      promptEdit.WordWrap = true;
      promptEdit.Size = new System.Drawing.Size(1000, 300);
      promptEdit.Location = new System.Drawing.Point(20, 110);
      promptEdit.Font = new System.Drawing.Font("Yu Gothic UI", 14F);

      // OKボタン
      okButton = new Button();
      okButton.Text = "OK";
      okButton.Size = new System.Drawing.Size(80, 30);
      okButton.Location = new System.Drawing.Point(20, 430);
      okButton.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      okButton.Click += OkButton_Click;

      // キャンセルボタン
      cancelButton = new Button();
      cancelButton.Text = "キャンセル";
      cancelButton.Size = new System.Drawing.Size(150, 30);
      cancelButton.Location = new System.Drawing.Point(110, 430);
      cancelButton.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      cancelButton.Click += CancelButton_Click;

      // クリアボタン
      clearButton = new Button();
      clearButton.Text = "クリア";
      clearButton.Size = new System.Drawing.Size(80, 30);
      clearButton.Location = new System.Drawing.Point(270, 430);
      clearButton.Font = new System.Drawing.Font("Yu Gothic UI", 14F);
      clearButton.Click += ClearButton_Click;

      // ComboBoxの選択変更イベント
      promptCombo.SelectedIndexChanged += PromptCombo_SelectedIndexChanged;

      // コントロールをフォームに追加
      this.Controls.Add(promptCombo);
      this.Controls.Add(promptLabel);
      this.Controls.Add(promptEdit);
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

    private void OkButton_Click(object sender, EventArgs e)
    {
      // プロンプトテキストを取得
      string promptText = promptEdit.Text.Trim();
      
      if (!string.IsNullOrEmpty(promptText))
      {
        // 履歴に追加（重複チェック）
        if (!promptCombo.Items.Contains(promptText))
        {
          promptCombo.Items.Insert(0, promptText);
          promptCombo.SelectedIndex = 0;
        }
        
        // クリップボードにプロンプトテキストをコピー
        try
        {
          Clipboard.SetText(promptText);
        }
        catch
        {
          // クリップボードへのアクセスが失敗した場合の処理
        }
      }

      // フォームを閉じる
      this.Close();
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
  }
}