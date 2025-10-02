using System;
using System.Windows.Forms;
using System.Drawing;

namespace DokodemoLLM
{
    public partial class MainForm : Form
    {
        private Button okButton;
        private Label titleLabel;

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
            this.Text = "DokodemoLLM";
            this.Size = new System.Drawing.Size(300, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = true;  // 最前面に表示
            this.ShowInTaskbar = false;  // タスクバーに表示しない

            // タイトルラベル
            titleLabel = new Label();
            titleLabel.Text = "DokodemoLLM";
            titleLabel.Font = new System.Drawing.Font("Arial", 18, System.Drawing.FontStyle.Regular);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.Size = new System.Drawing.Size(280, 30);
            titleLabel.Location = new System.Drawing.Point(10, 50);

            // OKボタン
            okButton = new Button();
            okButton.Text = "OK";
            okButton.Size = new System.Drawing.Size(80, 30);
            okButton.Location = new System.Drawing.Point(110, 120);
            okButton.Click += OkButton_Click;

            // コントロールをフォームに追加
            this.Controls.Add(titleLabel);
            this.Controls.Add(okButton);
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            // クリップボードに"HelloWorld"をコピー
            try
            {
                Clipboard.SetText("HelloWorld");
            }
            catch
            {
                // クリップボードへのアクセスが失敗した場合の処理
            }

            // フォームを閉じる
            this.Close();
        }
    }
}
