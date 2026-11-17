using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScriptLauncher
{
    public class MainForm : Form
    {
        private AppSettings _settings = AppSettings.Load();
        private readonly ToolTip _toolTip = new ToolTip();

        private readonly ListBox _listScripts = new ListBox();
        private readonly Button _btnAdd = new Button();
        private readonly Button _btnEdit = new Button();
        private readonly Button _btnRemove = new Button();

        private readonly Label _lblName = new Label();
        private readonly Label _lblPath = new Label();
        private readonly Label _lblType = new Label();
        private readonly Button _btnRun = new Button();

        public MainForm()
        {
            InitializeUI();
            RefreshList();
        }

        private void InitializeUI()
        {
            Text = "Запуск скриптов";
            ClientSize = new Size(620, 380);
            FormBorderStyle = FormBorderStyle.Sizable; // окно теперь можно менять в размере
            MinimumSize = new Size(480, 320);
            StartPosition = FormStartPosition.CenterScreen;

            // У только что созданного SplitContainer ширина по умолчанию очень маленькая,
            // а Panel1MinSize, Panel2MinSize и SplitterDistance при установке взаимно
            // проверяются друг через друга относительно ТЕКУЩЕЙ Width контрола (это видно
            // по тому, что даже Panel2MinSize изнутри дёргает set_SplitterDistance). Если
            // выставлять их до того, как контрол получил реальный размер, проверка не
            // проходит и выбрасывается исключение прямо в конструкторе окна — программа
            // падает ещё до появления на экране. Поэтому сначала явно задаём контролу
            // такой же размер, как у клиентской области формы, и только потом — сами
            // зависимые друг от друга свойства.
            var split = new SplitContainer
            {
                Size = ClientSize,
                Dock = DockStyle.Fill
            };
            split.Panel1MinSize = 160;
            split.Panel2MinSize = 220;
            split.SplitterDistance = 230;

            BuildListPanel(split.Panel1);
            BuildDetailsPanel(split.Panel2);

            Controls.Add(split);

            FormClosing += (s, e) => TrySaveSettings();
        }

        private void BuildListPanel(SplitterPanel panel)
        {
            // TableLayoutPanel с тремя РАВНЫМИ колонками вместо FlowLayoutPanel: три кнопки
            // гарантированно помещаются в один ряд при любой ширине панели (включая узкую
            // левую часть при перетаскивании разделителя). На FlowLayoutPanel с фиксированной
            // высотой третья кнопка при нехватке места переносилась на вторую строку и
            // обрезалась — становилась полностью невидимой, хотя по факту оставалась рабочей.
            var buttonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(2)
            };
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            _btnAdd.Text = "Добавить";
            _btnAdd.Dock = DockStyle.Fill;
            _btnAdd.Margin = new Padding(2);
            _btnAdd.Click += BtnAdd_Click;

            _btnEdit.Text = "Изменить";
            _btnEdit.Dock = DockStyle.Fill;
            _btnEdit.Margin = new Padding(2);
            _btnEdit.Click += BtnEdit_Click;

            _btnRemove.Text = "Удалить";
            _btnRemove.Dock = DockStyle.Fill;
            _btnRemove.Margin = new Padding(2);
            _btnRemove.Click += BtnRemove_Click;

            buttonsPanel.Controls.Add(_btnAdd, 0, 0);
            buttonsPanel.Controls.Add(_btnEdit, 1, 0);
            buttonsPanel.Controls.Add(_btnRemove, 2, 0);

            // Сначала добавляем то, что НЕ Dock.Fill, а Fill-элемент — последним,
            // иначе при изменении размера он может неправильно разделить место.
            panel.Controls.Add(buttonsPanel);

            _listScripts.Dock = DockStyle.Fill;
            _listScripts.IntegralHeight = false;
            _listScripts.SelectedIndexChanged += (s, e) => RefreshDetails();
            _listScripts.DoubleClick += (s, e) => RunSelected();
            _listScripts.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete)
                {
                    RemoveSelected();
                    e.Handled = true;
                }
            };

            panel.Controls.Add(_listScripts);
        }

        private void BuildDetailsPanel(SplitterPanel panel)
        {
            var content = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16)
            };

            var lblNameCaption = new Label { Text = "Имя:", Location = new Point(16, 12), AutoSize = true };

            _lblName.Location = new Point(16, 32);
            _lblName.Size = new Size(380, 24);
            _lblName.Font = new Font(Font, FontStyle.Bold);
            _lblName.AutoEllipsis = true;
            _lblName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblPathCaption = new Label { Text = "Путь:", Location = new Point(16, 68), AutoSize = true };

            _lblPath.Location = new Point(16, 88);
            _lblPath.Size = new Size(380, 40);
            _lblPath.ForeColor = Color.DimGray;
            _lblPath.AutoEllipsis = true;
            _lblPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblTypeCaption = new Label { Text = "Тип:", Location = new Point(16, 136), AutoSize = true };

            _lblType.Location = new Point(16, 156);
            _lblType.Size = new Size(200, 20);
            _lblType.ForeColor = Color.DimGray;

            _btnRun.Text = "Запустить";
            _btnRun.Location = new Point(16, 196);
            _btnRun.Size = new Size(200, 40);
            _btnRun.Click += (s, e) => RunSelected();

            content.Controls.Add(lblNameCaption);
            content.Controls.Add(_lblName);
            content.Controls.Add(lblPathCaption);
            content.Controls.Add(_lblPath);
            content.Controls.Add(lblTypeCaption);
            content.Controls.Add(_lblType);
            content.Controls.Add(_btnRun);

            panel.Controls.Add(content);
        }

        private void RefreshList()
        {
            int previousIndex = _listScripts.SelectedIndex;

            _listScripts.Items.Clear();
            foreach (ScriptEntry script in _settings.Scripts)
            {
                _listScripts.Items.Add(DisplayText(script));
            }

            if (_listScripts.Items.Count > 0)
            {
                _listScripts.SelectedIndex = Math.Min(Math.Max(previousIndex, 0), _listScripts.Items.Count - 1);
            }

            RefreshDetails();
        }

        private static string DisplayText(ScriptEntry entry) => $"[{TypeLabel(entry.Type)}] {entry.Name}";

        private static string TypeLabel(ScriptType type) => type switch
        {
            ScriptType.PowerShell => "PowerShell",
            ScriptType.Bat => "BAT",
            ScriptType.Cmd => "CMD",
            _ => "?"
        };

        private ScriptEntry? SelectedScript
        {
            get
            {
                int index = _listScripts.SelectedIndex;
                return index >= 0 && index < _settings.Scripts.Count ? _settings.Scripts[index] : null;
            }
        }

        private void RefreshDetails()
        {
            ScriptEntry? selected = SelectedScript;

            if (selected == null)
            {
                _lblName.Text = string.Empty;
                _lblPath.Text = _settings.Scripts.Count == 0
                    ? "Список пуст — нажмите \"Добавить\""
                    : "Выберите скрипт слева";
                _lblType.Text = string.Empty;
                _toolTip.SetToolTip(_lblPath, string.Empty);
                _btnRun.Enabled = false;
                _btnEdit.Enabled = false;
                _btnRemove.Enabled = false;
                return;
            }

            _lblName.Text = selected.Name;
            _lblPath.Text = selected.Path;
            _lblType.Text = TypeLabel(selected.Type);
            _toolTip.SetToolTip(_lblPath, selected.Path);
            _btnRun.Enabled = true;
            _btnEdit.Enabled = true;
            _btnRemove.Enabled = true;
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using (var form = new ScriptEditForm("Добавить скрипт", null))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _settings.Scripts.Add(new ScriptEntry
                    {
                        Name = form.ScriptName,
                        Path = form.ScriptPath,
                        Type = form.SelectedType
                    });

                    TrySaveSettings();
                    RefreshList();
                    _listScripts.SelectedIndex = _settings.Scripts.Count - 1;
                }
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            int index = _listScripts.SelectedIndex;
            ScriptEntry? selected = SelectedScript;
            if (selected == null)
            {
                return;
            }

            using (var form = new ScriptEditForm("Изменить скрипт", selected))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _settings.Scripts[index] = new ScriptEntry
                    {
                        Name = form.ScriptName,
                        Path = form.ScriptPath,
                        Type = form.SelectedType
                    };

                    TrySaveSettings();
                    RefreshList();
                    _listScripts.SelectedIndex = index;
                }
            }
        }

        private void BtnRemove_Click(object? sender, EventArgs e)
        {
            RemoveSelected();
        }

        private void RemoveSelected()
        {
            int index = _listScripts.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            DialogResult confirm = MessageBox.Show(this,
                $"Удалить скрипт \"{_settings.Scripts[index].Name}\" из списка?",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                _settings.Scripts.RemoveAt(index);
                TrySaveSettings();
                RefreshList();
            }
        }

        private void RunSelected()
        {
            ScriptEntry? selected = SelectedScript;
            if (selected != null)
            {
                RunScript(selected.Path, selected.Type);
            }
        }

        private void TrySaveSettings()
        {
            try
            {
                _settings.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Не удалось сохранить настройки:\n{ex.Message}",
                    "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunScript(string path, ScriptType type)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(this, "Путь к скрипту не задан.",
                    "Скрипт не найден", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(path))
            {
                MessageBox.Show(this, $"Файл не найден:\n{path}",
                    "Скрипт не найден", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    UseShellExecute = true, // скрипт откроется в отдельном окне, а не внутри программы
                    WorkingDirectory = Path.GetDirectoryName(path) ?? string.Empty
                };

                switch (type)
                {
                    case ScriptType.PowerShell:
                        psi.FileName = "powershell.exe";
                        // -NoExit — не закрывать окно после завершения скрипта
                        // -ExecutionPolicy Bypass — разрешить выполнение только для этого запуска
                        psi.Arguments = $"-NoExit -ExecutionPolicy Bypass -File \"{path}\"";
                        break;

                    case ScriptType.Bat:
                    case ScriptType.Cmd:
                        psi.FileName = "cmd.exe";
                        // /k — оставить окно открытым после выполнения.
                        // Двойные кавычки компенсируют особенность разбора параметров
                        // в cmd.exe, когда путь к файлу содержит пробелы.
                        psi.Arguments = $"/k \"\"{path}\"\"";
                        break;
                }

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Не удалось запустить скрипт:\n{ex.Message}",
                    "Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
