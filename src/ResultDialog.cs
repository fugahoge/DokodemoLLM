using System;
using System.Windows.Forms;
using System.Drawing;

namespace DokodemoLLM
{
  public partial class ResultDialog : Form
  {
    private TextBox resultTextBox = null!;
    private Button addButton = null!;
    private Button repButton = null!;
    private Button closeButton = null!;
    private string resultText;

    public ResultDialog(string result)
    {
      resultText = result;
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      this.Text = "これでよいですか？";
      this.Size = new System.Drawing.Size(800, 520);
      this.StartPosition = FormStartPosition.CenterParent;
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.ShowInTaskbar = false;

      // フォント設定
      this.Font = new System.Drawing.Font("Yu Gothic UI", 12F, System.Drawing.FontStyle.Regular);

      // 結果表示用テキストボックス
      resultTextBox = new TextBox();
      resultTextBox.Multiline = true;
      resultTextBox.ScrollBars = ScrollBars.Vertical;
      resultTextBox.WordWrap = true;
      resultTextBox.ReadOnly = true;
      resultTextBox.Size = new System.Drawing.Size(740, 380);
      resultTextBox.Location = new System.Drawing.Point(20, 20);
      resultTextBox.Font = new System.Drawing.Font("Yu Gothic UI", 12F);
      resultTextBox.Text = resultText;
      
      // テキストを全選択しないようにカーソルを先頭に設定
      resultTextBox.SelectionStart = 0;
      resultTextBox.SelectionLength = 0;

      // 追加ボタン
      addButton = new Button();
      addButton.Text = "追加";
      addButton.Size = new System.Drawing.Size(120, 40);
      addButton.Location = new System.Drawing.Point(20, 420);
      addButton.Font = new System.Drawing.Font("Yu Gothic UI", 12F);
      addButton.Click += AddButton_Click;

      // 置換ボタン
      repButton = new Button();
      repButton.Text = "置換";
      repButton.Size = new System.Drawing.Size(120, 40);
      repButton.Location = new System.Drawing.Point(150, 420);
      repButton.Font = new System.Drawing.Font("Yu Gothic UI", 12F);
      repButton.Click += ReplaceButton_Click;

      // 閉じるボタン
      closeButton = new Button();
      closeButton.Text = "閉じる";
      closeButton.Size = new System.Drawing.Size(120, 40);
      closeButton.Location = new System.Drawing.Point(280, 420);
      closeButton.Font = new System.Drawing.Font("Yu Gothic UI", 12F);
      closeButton.Click += CloseButton_Click;

      // コントロールをフォームに追加
      this.Controls.Add(resultTextBox);
      this.Controls.Add(addButton);
      this.Controls.Add(repButton);
      this.Controls.Add(closeButton);

      // デフォルトボタンを設定
      this.AcceptButton = addButton;
      this.CancelButton = closeButton;
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
      this.DialogResult = DialogResult.Yes;
      this.Close();
    }

    private void ReplaceButton_Click(object? sender, EventArgs e)
    {
      this.DialogResult = DialogResult.No;
      this.Close();
    }

    private void CloseButton_Click(object? sender, EventArgs e)
    {
      this.DialogResult = DialogResult.Abort;
      this.Close();
    }
  }
}
