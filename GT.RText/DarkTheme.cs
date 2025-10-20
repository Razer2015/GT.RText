using System;
using System.Drawing;
using System.Windows.Forms;

namespace GT.RText
{
    /// <summary>
    /// Class to apply dark theme to Windows Forms controls
    /// </summary>
    public static class DarkTheme
    {
        // Dark theme colors
        public static readonly Color DarkBackground = Color.Black;
        public static readonly Color DarkSecondaryBackground = Color.FromArgb(20, 20, 20);
        public static readonly Color DarkControlBackground = Color.FromArgb(15, 15, 15);
        public static readonly Color DarkForeground = Color.White;
        public static readonly Color DarkBorder = Color.FromArgb(40, 40, 40);
        public static readonly Color DarkSelection = Color.FromArgb(0, 122, 204);
        public static readonly Color DarkMenuBackground = Color.FromArgb(20, 20, 20);
        public static readonly Color DarkMenuForeground = Color.White;
        public static readonly Color DarkStatusBackground = Color.FromArgb(0, 122, 204);

        /// <summary>
        /// Applies dark theme to the main form and all its controls
        /// </summary>
        /// <param name="form">Form to be modified</param>
        public static void ApplyDarkTheme(Form form)
        {
            // Configure main form
            form.BackColor = DarkBackground;
            form.ForeColor = DarkForeground;

            // Apply theme to all controls recursively
            ApplyDarkThemeToControls(form.Controls);
        }

        /// <summary>
        /// Applies dark theme to a collection of controls recursively
        /// </summary>
        /// <param name="controls">Control collection</param>
        private static void ApplyDarkThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                ApplyDarkThemeToControl(control);

                // Apply recursively to child controls
                if (control.HasChildren)
                {
                    ApplyDarkThemeToControls(control.Controls);
                }
            }
        }

        /// <summary>
        /// Applies dark theme to a specific control
        /// </summary>
        /// <param name="control">Control to be modified</param>
        private static void ApplyDarkThemeToControl(Control control)
        {
            try
            {
                switch (control)
                {
                    case MenuStrip menuStrip:
                        ApplyDarkThemeToMenuStrip(menuStrip);
                        break;

                    case ContextMenuStrip contextMenu:
                        ApplyDarkThemeToContextMenu(contextMenu);
                        break;

                    case StatusStrip statusStrip:
                        ApplyDarkThemeToStatusStrip(statusStrip);
                        break;

                    case TabControl tabControl:
                        ApplyDarkThemeToTabControl(tabControl);
                        break;

                    case ListView listView:
                        ApplyDarkThemeToListView(listView);
                        break;

                    case TextBox textBox:
                        ApplyDarkThemeToTextBox(textBox);
                        break;

                    case Button button:
                        ApplyDarkThemeToButton(button);
                        break;

                    case Panel panel:
                        ApplyDarkThemeToPanel(panel);
                        break;

                    case GroupBox groupBox:
                        ApplyDarkThemeToGroupBox(groupBox);
                        break;

                    case Label label:
                        ApplyDarkThemeToLabel(label);
                        break;

                    default:
                        // Apply basic theme for non-specific controls
                        control.BackColor = DarkBackground;
                        control.ForeColor = DarkForeground;
                        break;
                }
            }
            catch (Exception)
            {
                // Ignore errors from controls that don't support color changes
            }
        }

        private static void ApplyDarkThemeToMenuStrip(MenuStrip menuStrip)
        {
            menuStrip.BackColor = DarkMenuBackground;
            menuStrip.ForeColor = DarkMenuForeground;
            menuStrip.Renderer = new DarkMenuRenderer();

            foreach (ToolStripItem item in menuStrip.Items)
            {
                ApplyDarkThemeToMenuItem(item);
            }
        }

        private static void ApplyDarkThemeToContextMenu(ContextMenuStrip contextMenu)
        {
            contextMenu.BackColor = DarkMenuBackground;
            contextMenu.ForeColor = DarkMenuForeground;
            contextMenu.Renderer = new DarkMenuRenderer();

            foreach (ToolStripItem item in contextMenu.Items)
            {
                ApplyDarkThemeToMenuItem(item);
            }
        }

        private static void ApplyDarkThemeToMenuItem(ToolStripItem item)
        {
            item.BackColor = DarkMenuBackground;
            item.ForeColor = DarkMenuForeground;

            if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
            {
                foreach (ToolStripItem subItem in menuItem.DropDownItems)
                {
                    ApplyDarkThemeToMenuItem(subItem);
                }
            }
        }

        private static void ApplyDarkThemeToStatusStrip(StatusStrip statusStrip)
        {
            statusStrip.BackColor = DarkSecondaryBackground;
            statusStrip.ForeColor = DarkForeground;
            statusStrip.Renderer = new DarkStatusStripRenderer();

            foreach (ToolStripItem item in statusStrip.Items)
            {
                item.BackColor = DarkSecondaryBackground;
                item.ForeColor = DarkForeground;
            }
        }

        private static void ApplyDarkThemeToTabControl(TabControl tabControl)
        {
            tabControl.BackColor = DarkBackground;
            tabControl.ForeColor = DarkForeground;

            foreach (TabPage tabPage in tabControl.TabPages)
            {
                tabPage.BackColor = DarkBackground;
                tabPage.ForeColor = DarkForeground;
            }
        }

        private static void ApplyDarkThemeToListView(ListView listView)
        {
            listView.BackColor = DarkControlBackground;
            listView.ForeColor = DarkForeground;
            listView.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void ApplyDarkThemeToTextBox(TextBox textBox)
        {
            textBox.BackColor = DarkControlBackground;
            textBox.ForeColor = DarkForeground;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void ApplyDarkThemeToButton(Button button)
        {
            button.BackColor = DarkSecondaryBackground;
            button.ForeColor = DarkForeground;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = DarkBorder;
            button.FlatAppearance.MouseOverBackColor = DarkSelection;
        }

        private static void ApplyDarkThemeToPanel(Panel panel)
        {
            panel.BackColor = DarkBackground;
            panel.ForeColor = DarkForeground;
        }

        private static void ApplyDarkThemeToGroupBox(GroupBox groupBox)
        {
            groupBox.BackColor = DarkBackground;
            groupBox.ForeColor = DarkForeground;
        }

        private static void ApplyDarkThemeToLabel(Label label)
        {
            label.BackColor = Color.Transparent;
            label.ForeColor = DarkForeground;
        }
    }

    /// <summary>
    /// Renderer personalizado para menus em tema escuro
    /// </summary>
    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkMenuColorTable()) { }
    }

    /// <summary>
    /// Renderer personalizado para status strip em tema escuro
    /// </summary>
    public class DarkStatusStripRenderer : ToolStripProfessionalRenderer
    {
        public DarkStatusStripRenderer() : base(new DarkMenuColorTable()) { }
    }

    /// <summary>
    /// Tabela de cores personalizada para menus em tema escuro
    /// </summary>
    public class DarkMenuColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => DarkTheme.DarkSelection;
        public override Color MenuItemSelectedGradientBegin => DarkTheme.DarkSelection;
        public override Color MenuItemSelectedGradientEnd => DarkTheme.DarkSelection;
        public override Color MenuItemPressedGradientBegin => DarkTheme.DarkSelection;
        public override Color MenuItemPressedGradientEnd => DarkTheme.DarkSelection;
        public override Color MenuItemBorder => DarkTheme.DarkBorder;
        public override Color MenuBorder => DarkTheme.DarkBorder;
        public override Color MenuStripGradientBegin => DarkTheme.DarkMenuBackground;
        public override Color MenuStripGradientEnd => DarkTheme.DarkMenuBackground;
        public override Color ToolStripDropDownBackground => DarkTheme.DarkMenuBackground;
        public override Color ImageMarginGradientBegin => DarkTheme.DarkMenuBackground;
        public override Color ImageMarginGradientMiddle => DarkTheme.DarkMenuBackground;
        public override Color ImageMarginGradientEnd => DarkTheme.DarkMenuBackground;
        public override Color SeparatorDark => DarkTheme.DarkBorder;
        public override Color SeparatorLight => DarkTheme.DarkBorder;
    }
}
