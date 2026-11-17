using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScriptLauncher
{
    /// <summary>
    /// Диалог добавления или изменения одного скрипта в списке.
    /// </summary>
    public class ScriptEditForm : Form
    {
        public string ScriptName { get; private set; } = string.Empty;
        public string ScriptPath { get; private set; } = string.Empty;
        public ScriptType SelectedType { get; private set; } = ScriptType.PowerShell;

        private readonly TextBox _txtName = new TextBox();
        private readonly TextBox _txtPath = new TextBox();
        private readonly RadioButton _radioPowerShell = new RadioButton();
        private readonly RadioButton _radioBat = new RadioButton();
        private readonly RadioButton _radioCmd = new RadioButton();

        public ScriptEditForm(string title, ScriptEntry? existing)
        {
            InitializeUI(title);

            if (existing != null)
            {
                _txtName.Text = existing.Name;
                _txtPath.Text = existing.Path;
                SetRadioByType(existing.Type);
            }
            else
            {
                _radioPowerShell.Checked = true;
            }
        }

        private void InitializeUI(string title)
        {
            Text = title;
            ClientSize = new Size(460, 250);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var lblName = new Label { Text = "Имя скрипта:", Location = new Point(20, 16), Size = new Size(200, 20) };
            _txtName.Location = new Point(20, 36);
            _txtName.Size = new Size(420, 24);

            var lblPath = new Label { Text = "Путь к файлу:", Location = new Point(20, 70), Size = new Size(200, 20) };
            _txtPath.Location = new Point(20, 90);
            _txtPath.Size = new Size(330, 24);

            var btnBrowse = new Button { Text = "Обзор...", Location = new Point(360, 89), Size = new Size(80, 26) };
            btnBrowse.Click += BtnBrowse_Click;

            var lblType = new Label { Text = "Тип скрипта:", Location = new Point(20, 128), Size = new Size(200, 20) };

            _radioPowerShell.Text = "PowerShell (.ps1)";
            _radioPowerShell.Location = new Point(20, 150);
            _radioPowerShell.AutoSize = true;

            _radioBat.Text = "BAT (.bat)";
            _radioBat.Location = new Point(190, 150);
            _radioBat.AutoSize = true;

            _radioCmd.Text = "CMD (.cmd)";
            _radioCmd.Location = new Point(310, 150);
            _radioCmd.AutoSize = true;

            var btnOk = new Button { Text = "ОК", Location = new Point(270, 200), Size = new Size(80, 32) };
            btnOk.Click += BtnOk_Click;

            var btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(360, 200),
                Size = new Size(80, 32),
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(lblName);
            Controls.Add(_txtName);
            Controls.Add(lblPath);
            Controls.Add(_txtPath);
            Controls.Add(btnBrowse);
            Controls.Add(lblType);
            Controls.Add(_radioPowerShell);
            Controls.Add(_radioBat);
            Controls.Add(_radioCmd);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void SetRadioByType(ScriptType type)
        {
            _radioPowerShell.Checked = type == ScriptType.PowerShell;
            _radioBat.Checked = type == ScriptType.Bat;
            _radioCmd.Checked = type == ScriptType.Cmd;
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "Скрипты (*.ps1;*.bat;*.cmd)|*.ps1;*.bat;*.cmd|Все файлы (*.*)|*.*",
                CheckFileExists = true
            })
            {
                if (!string.IsNullOrWhiteSpace(_txtPath.Text) && File.Exists(_txtPath.Text))
                {
                    dlg.InitialDirectory = Path.GetDirectoryName(_txtPath.Text) ?? string.Empty;
                }

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _txtPath.Text = dlg.FileName;

                    if (string.IsNullOrWhiteSpace(_txtName.Text))
                    {
                        _txtName.Text = Path.GetFileNameWithoutExtension(dlg.FileName);
                    }

                    string ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
                    if (ext == ".ps1") _radioPowerShell.Checked = true;
                    else if (ext == ".bat") _radioBat.Checked = true;
                    else if (ext == ".cmd") _radioCmd.Checked = true;
                }
            }
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtPath.Text))
            {
                MessageBox.Show(this, "Укажите путь к файлу скрипта.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ScriptPath = _txtPath.Text.Trim();
            ScriptName = string.IsNullOrWhiteSpace(_txtName.Text)
                ? Path.GetFileNameWithoutExtension(ScriptPath)
                : _txtName.Text.Trim();
            SelectedType = _radioPowerShell.Checked ? ScriptType.PowerShell
                : _radioBat.Checked ? ScriptType.Bat
                : ScriptType.Cmd;

            DialogResult = DialogResult.OK; // закрывает диалог только после успешной проверки
        }
    }
}
