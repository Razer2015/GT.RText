using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GT.RText
{
    public partial class RowEditor : Form
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public string Data { get; set; }
        public bool ApplyToAllLocales { get; set; }

        private bool _isUiProject;

        public RowEditor()
        {
            InitializeComponent();
            
            // Aplicar tema escuro
            DarkTheme.ApplyDarkTheme(this);
            
            // Carregar ícone se existir
            LoadIcon();
        }

        public RowEditor(bool isWithoutId, bool isUiProject)
        {
            InitializeComponent();

            if (isWithoutId) HandleId(-1);

            _isUiProject = isUiProject;
            if (isUiProject)
                applyAllLocalesCheckBox.Visible = true;
                
            // Aplicar tema escuro
            DarkTheme.ApplyDarkTheme(this);
            
            // Carregar ícone se existir
            LoadIcon();
        }

        public RowEditor(int id, string label, string data, bool isUiProject)
        {
            InitializeComponent();

            Id = id;
            Label = label;
            Data = data;

            _isUiProject = isUiProject;
            if (_isUiProject)
                applyAllLocalesCheckBox.Visible = true;

            textBox_label.Text = label;
            richTextBox_data.Text = data;

            HandleId(id);
            
            // Aplicar tema escuro
            DarkTheme.ApplyDarkTheme(this);
            
            // Carregar ícone se existir
            LoadIcon();
        }
        
        private void LoadIcon()
        {
            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "app.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch
            {
                // Ignorar se não conseguir carregar o ícone
            }
        }

        private void HandleId(int id)
        {
            if (id == -1)
            {
                // RT03 doesn't have ID
                label_id.Visible = false;
                numericUpDown_id.Visible = false;
            }
            else
            {
                label_id.Visible = true;
                numericUpDown_id.Visible = true;
                numericUpDown_id.Value = id;
            }
        }

        private void numericUpDown_id_ValueChanged(object sender, EventArgs e)
        {
            Id = (int)numericUpDown_id.Value;
        }

        private void textBox_label_TextChanged(object sender, EventArgs e)
        {
            Label = textBox_label.Text;
        }

        private void richTextBox_data_TextChanged(object sender, EventArgs e)
        {
            Data = richTextBox_data.Text;
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void applyAllLocalesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyToAllLocales = applyAllLocalesCheckBox.Checked;
        }
    }
}
