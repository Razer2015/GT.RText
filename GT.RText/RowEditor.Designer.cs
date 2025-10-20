namespace GT.RText
{
    partial class RowEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label_id = new System.Windows.Forms.Label();
            this.numericUpDown_id = new System.Windows.Forms.NumericUpDown();
            this.label_label = new System.Windows.Forms.Label();
            this.textBox_label = new System.Windows.Forms.TextBox();
            this.label_data = new System.Windows.Forms.Label();
            this.richTextBox_data = new System.Windows.Forms.RichTextBox();
            this.button_save = new System.Windows.Forms.Button();
            this.applyAllLocalesCheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_id)).BeginInit();
            this.SuspendLayout();
            // 
            // label_id
            // 
            this.label_id.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_id.Location = new System.Drawing.Point(0, 0);
            this.label_id.Name = "label_id";
            this.label_id.Size = new System.Drawing.Size(537, 23);
            this.label_id.TabIndex = 0;
            this.label_id.Text = "String ID";
            this.label_id.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // numericUpDown_id
            // 
            this.numericUpDown_id.Dock = System.Windows.Forms.DockStyle.Top;
            this.numericUpDown_id.Location = new System.Drawing.Point(0, 23);
            this.numericUpDown_id.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.numericUpDown_id.Name = "numericUpDown_id";
            this.numericUpDown_id.Size = new System.Drawing.Size(537, 20);
            this.numericUpDown_id.TabIndex = 1;
            this.numericUpDown_id.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numericUpDown_id.ValueChanged += new System.EventHandler(this.numericUpDown_id_ValueChanged);
            // 
            // label_label
            // 
            this.label_label.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_label.Location = new System.Drawing.Point(0, 43);
            this.label_label.Name = "label_label";
            this.label_label.Size = new System.Drawing.Size(537, 23);
            this.label_label.TabIndex = 2;
            this.label_label.Text = "Label (Must be Unique)";
            this.label_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBox_label
            // 
            this.textBox_label.Dock = System.Windows.Forms.DockStyle.Top;
            this.textBox_label.Location = new System.Drawing.Point(0, 66);
            this.textBox_label.Name = "textBox_label";
            this.textBox_label.Size = new System.Drawing.Size(537, 20);
            this.textBox_label.TabIndex = 3;
            this.textBox_label.TextChanged += new System.EventHandler(this.textBox_label_TextChanged);
            // 
            // label_data
            // 
            this.label_data.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_data.Location = new System.Drawing.Point(0, 86);
            this.label_data.Name = "label_data";
            this.label_data.Size = new System.Drawing.Size(537, 23);
            this.label_data.TabIndex = 4;
            this.label_data.Text = "Data";
            this.label_data.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // richTextBox_data
            // 
            this.richTextBox_data.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox_data.Location = new System.Drawing.Point(0, 109);
            this.richTextBox_data.Name = "richTextBox_data";
            this.richTextBox_data.Size = new System.Drawing.Size(537, 177);
            this.richTextBox_data.TabIndex = 5;
            this.richTextBox_data.Text = "";
            this.richTextBox_data.TextChanged += new System.EventHandler(this.richTextBox_data_TextChanged);
            // 
            // button_save
            // 
            this.button_save.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.button_save.Location = new System.Drawing.Point(0, 286);
            this.button_save.Name = "button_save";
            this.button_save.Size = new System.Drawing.Size(537, 23);
            this.button_save.TabIndex = 6;
            this.button_save.Text = "Save";
            this.button_save.UseVisualStyleBackColor = true;
            this.button_save.Click += new System.EventHandler(this.button_save_Click);
            // 
            // applyAllLocalesCheckBox
            // 
            this.applyAllLocalesCheckBox.AutoSize = true;
            this.applyAllLocalesCheckBox.Location = new System.Drawing.Point(424, 4);
            this.applyAllLocalesCheckBox.Name = "applyAllLocalesCheckBox";
            this.applyAllLocalesCheckBox.Size = new System.Drawing.Size(113, 17);
            this.applyAllLocalesCheckBox.TabIndex = 7;
            this.applyAllLocalesCheckBox.Text = "Apply to all locales";
            this.applyAllLocalesCheckBox.UseVisualStyleBackColor = true;
            this.applyAllLocalesCheckBox.Visible = false;
            this.applyAllLocalesCheckBox.CheckedChanged += new System.EventHandler(this.applyAllLocalesCheckBox_CheckedChanged);
            // 
            // RowEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(537, 309);
            this.Controls.Add(this.applyAllLocalesCheckBox);
            this.Controls.Add(this.richTextBox_data);
            this.Controls.Add(this.button_save);
            this.Controls.Add(this.label_data);
            this.Controls.Add(this.textBox_label);
            this.Controls.Add(this.label_label);
            this.Controls.Add(this.numericUpDown_id);
            this.Controls.Add(this.label_id);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "RowEditor";
            this.Text = "GT.RText - Row Editor";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_id)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_id;
        private System.Windows.Forms.NumericUpDown numericUpDown_id;
        private System.Windows.Forms.Label label_label;
        private System.Windows.Forms.TextBox textBox_label;
        private System.Windows.Forms.Label label_data;
        private System.Windows.Forms.RichTextBox richTextBox_data;
        private System.Windows.Forms.Button button_save;
        private System.Windows.Forms.CheckBox applyAllLocalesCheckBox;
    }
}
