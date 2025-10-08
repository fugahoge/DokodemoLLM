using System;
using System.Windows.Forms;
using System.Drawing;

namespace DokodemoLLM
{
  public partial class ResultDialog : Form
  {
    private TextBox resultTextBox;
    private Button okButton;
    private Button cancelButton;
    private string resultText;

    public ResultDialog(string result)
    {
      resultText = result;
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      this.Text = "結果";
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

      // OKボタン
      okButton = new Button();
      okButton.Text = "コピー";
      okButton.Size = new System.Drawing.Size(120, 40);
      okButton.Location = new System.Drawing.Point(20, 420);
      okButton.Font = new System.Drawing.Font("Yu Gothic UI", 12F);
      okButton.Click += OkButton_Click;

      // キャンセルボタン
      cancelButton = new Button();
      cancelButton.Text = "クローズ";
      cancelButton.Size = new System.Drawing.Size(120, 40);
      cancelButton.Location = new System.Drawing.Point(150, 420);
      cancelButton.Font = new System.Drawing.Font("Yu Gothic UI", 12F);
      cancelButton.Click += CancelButton_Click;

      // コントロールをフォームに追加
      this.Controls.Add(resultTextBox);
      this.Controls.Add(okButton);
      this.Controls.Add(cancelButton);

      // デフォルトボタンを設定
      this.AcceptButton = okButton;
      this.CancelButton = cancelButton;
    }

    private void OkButton_Click(object sender, EventArgs e)
    {
      try
      {
        Clipboard.SetText(resultText);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"クリップボードへのコピーに失敗しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }

      this.DialogResult = DialogResult.OK;
      this.Close();
    }

    private void CancelButton_Click(object sender, EventArgs e)
    {
      this.DialogResult = DialogResult.Cancel;
      this.Close();
    }
  }
}
