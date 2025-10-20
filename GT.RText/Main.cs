using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

// Required for the non crappy folder picker 
// https://stackoverflow.com/q/11624298
// using Microsoft.WindowsAPICodePack.Dialogs;

using GT.RText.Core;
using GT.RText.Core.Exceptions;
using GT.Shared.Logging;
using GT.Shared;
using System.Linq;

namespace GT.RText
{
    public partial class Main : Form
    {
        // Windows API declarations for dark title bar
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        /// <summary>
        /// Designates whether the currently loaded content is a project folder.
        /// </summary>
        private bool _isUiFolderProject;

        /// <summary>
        /// Designates whether the currently loaded project is a GT6 locale project, 
        /// where all RT2 files are contained within a single global folder with a file for each locale.
        /// </summary>
        private bool _isGT6AndAboveProjectStyle;

        /// <summary>
        /// List of the current RText's curently openned.
        /// </summary>
        private List<RTextParser> _rTexts;

        private ListViewColumnSorter _columnSorter;
        private ContextMenuStrip _categoriesContextMenu;

        public RTextParser CurrentRText => _rTexts[tabControlLocalFiles.SelectedIndex];
        public RTextPageBase CurrentPage { get; set; }

        public Main()
        {
            InitializeComponent();

            listViewPages.Columns.Add("Category", -2, HorizontalAlignment.Left);

            _rTexts = new List<RTextParser>();
            _columnSorter = new ListViewColumnSorter();
            this.listViewEntries.ListViewItemSorter = _columnSorter;
            this.listViewEntries.Sorting = SortOrder.Ascending;
            
            InitializeExcelImportFeature();
            
            // Apply dark theme
            DarkTheme.ApplyDarkTheme(this);
            
            // Apply dark title bar
            this.Load += (s, e) => ApplyDarkTitleBar();
            
            // Load icon if exists
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
                // Ignore if unable to load the icon
            }
        }

        /// <summary>
        /// Applies dark title bar using Windows API
        /// </summary>
        private void ApplyDarkTitleBar()
        {
            try
            {
                if (this.Handle != IntPtr.Zero)
                {
                    int value = 1;
                    // Try first the newest API version
                    int result = DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
                    
                    // If it fails, try the older version
                    if (result != 0)
                    {
                        DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref value, sizeof(int));
                    }
                }
            }
            catch
            {
                // Ignore if API is not available
            }
        }

        #region Events
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) != DialogResult.OK) return;

            _rTexts.Clear();
            _isUiFolderProject = false;

            ClearListViews();
            ClearTabs();

            var rtext = ReadRTextFile(openFileDialog.FileName);
            if (rtext != null)
            {
                var tab = new TabPage(openFileDialog.FileName);
                tabControlLocalFiles.TabPages.Add(tab);
                DisplayPages();
            }
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Selecione a pasta com os arquivos RT";
            dialog.ShowNewFolderButton = false;

            if (dialog.ShowDialog() != DialogResult.OK) return;

            _rTexts.Clear();

            _isUiFolderProject = true;

            ClearListViews();
            ClearTabs();

            bool firstTab = true;
            string[] files = Directory.GetFiles(dialog.SelectedPath, "*", SearchOption.TopDirectoryOnly);

            if (files.Any(f => RTextParser.Locales.ContainsKey(Path.GetFileNameWithoutExtension(f))))
            {
                // Assume GT6+, where all RT2 files are all in one global folder compacted (i.e rtext/common/<LOCALE>.rt2)
                _isGT6AndAboveProjectStyle = true;

                foreach (var file in files)
                {
                    string locale = Path.GetFileNameWithoutExtension(file);
                    if (RTextParser.Locales.TryGetValue(locale, out string localeName))
                    {
                        var rtext = ReadRTextFile(file);
                        if (rtext != null)
                        {
                            rtext.LocaleCode = locale;
                            var tab = new TabPage(localeName);
                            tabControlLocalFiles.TabPages.Add(tab);

                            if (firstTab)
                            {
                                DisplayPages();
                                firstTab = false;
                            }
                        }
                    }
                }
            }
            else
            {
                // Locale files are located per-UI project, in their own folder (i.e arcade/US/rtext.rt2)
                string[] folders = Directory.GetDirectories(dialog.SelectedPath, "*", SearchOption.TopDirectoryOnly);
                foreach (var folder in folders)
                {
                    string actualDirName = Path.GetFileName(folder);
                    if (RTextParser.Locales.TryGetValue(actualDirName, out string localeName))
                    {
                        var rt2File = Path.Combine(folder, "rtext.rt2");
                        if (!File.Exists(rt2File))
                            continue;

                        var rtext = ReadRTextFile(rt2File);
                        if (rtext != null)
                        {
                            rtext.LocaleCode = actualDirName;
                            var tab = new TabPage(localeName);
                            tabControlLocalFiles.TabPages.Add(tab);

                            if (firstTab)
                            {
                                DisplayPages();
                                firstTab = false;
                            }
                        }
                    }
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (_isUiFolderProject)
                {
                    var dialog = new FolderBrowserDialog();
                    dialog.Description = "Selecione a pasta para salvar os arquivos";
                    dialog.ShowNewFolderButton = true;

                    if (dialog.ShowDialog() != DialogResult.OK) return;

                    foreach (var rtext in _rTexts)
                    {
                        if (_isGT6AndAboveProjectStyle)
                        {
                            string localePath = Path.Combine(dialog.SelectedPath, $"{rtext.LocaleCode}.rt2");
                            rtext.RText.Save(localePath);
                        }
                        else
                        {
                            string localePath = Path.Combine(dialog.SelectedPath, rtext.LocaleCode);
                            Directory.CreateDirectory(localePath);

                            rtext.RText.Save(Path.Combine(localePath, "rtext.rt2"));
                        }
                    }

                    toolStripStatusLabel.Text = $"{saveFileDialog.FileName} - saved successfully {_rTexts.Count} locales.";
                }
                else
                {
                    if (saveFileDialog.ShowDialog(this) != DialogResult.OK) return;

                    CurrentRText.RText.Save(saveFileDialog.FileName);
                    toolStripStatusLabel.Text = $"{saveFileDialog.FileName} - saved successfully.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                toolStripStatusLabel.Text = $"Failed to save, unknown error, please contact the developer.";
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Application.MessageLoop)
            {
                // WinForms app
                Application.Exit();
            }
            else
            {
                // Console app
                Environment.Exit(1);
            }
        }


        private void Main_SizeChanged(object sender, EventArgs e)
        {
            listViewPages.BeginUpdate();
            listViewPages.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listViewPages.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listViewPages.EndUpdate();

            listViewEntries.BeginUpdate();
            listViewEntries.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listViewEntries.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listViewEntries.EndUpdate();
        }


        private void listViewCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            listViewEntries.Items.Clear();

            if (listViewPages.SelectedItems.Count <= 0 || listViewPages.SelectedItems[0] == null) return;

            try
            {
                var lViewItem = listViewPages.SelectedItems[0];
                var page = (RTextPageBase)lViewItem.Tag;
                CurrentPage = page;

                DisplayEntries(page);

                toolStripStatusLabel.Text = $"{page.Name} - parsed with {page.PairUnits.Count} entries.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                toolStripStatusLabel.Text = ex.Message;
            }
        }

        private void listViewEntries_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listViewEntries_DoubleClick(object sender, EventArgs e)
        {
            editToolStripMenuItem_Click(null, null);
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewPages.SelectedItems.Count <= 0 || listViewPages.SelectedItems[0] == null) return;
            if (listViewEntries.SelectedItems.Count <= 0 || listViewEntries.SelectedItems[0] == null) return;

            try
            {
                var categoryLViewItem = listViewPages.SelectedItems[0];
                var page = (RTextPageBase)categoryLViewItem.Tag;

                var lViewItem = listViewEntries.SelectedItems[0];
                RTextPairUnit rowData = (RTextPairUnit)lViewItem.Tag;

                var rowEditor = new RowEditor(rowData.ID, rowData.Label, rowData.Value, _isUiFolderProject);
                if (rowEditor.ShowDialog() == DialogResult.OK)
                {
                    if (_isUiFolderProject && rowEditor.ApplyToAllLocales)
                    {
                        foreach (var rt in _rTexts)
                        {
                            var rtPage = rt.RText.GetPages()[page.Name];
                            rtPage.DeleteRow(rowData.Label);
                            rtPage.AddRow(rowEditor.Id, rowEditor.Label, rowEditor.Data);
                        }

                        toolStripStatusLabel.Text = $"{rowEditor.Label} - edited to {_rTexts.Count} locales";
                    }
                    else
                    {
                        if (rowEditor.Label != rowEditor.Label && page.PairExists(rowEditor.Label))
                        {
                            MessageBox.Show("This label already exists in this category.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Remove, Add - Incase label was changed else we can't track it in our page
                        page.DeleteRow(rowData.Label);
                        page.AddRow(rowEditor.Id, rowEditor.Label, rowEditor.Data);

                        toolStripStatusLabel.Text = $"{rowEditor.Label} - edited";
                    }

                    DisplayEntries(page);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                toolStripStatusLabel.Text = ex.Message;
            }
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewPages.SelectedItems.Count <= 0 || listViewPages.SelectedItems[0] == null) return;

            try
            {
                var pageLViewItem = listViewPages.SelectedItems[0];
                var page = (RTextPageBase)pageLViewItem.Tag;

                var rowEditor = new RowEditor(CurrentRText.RText is RT03, _isUiFolderProject);
                rowEditor.Id = page.GetLastId() + 1;

                if (rowEditor.ShowDialog() == DialogResult.OK)
                {
                    if (page.PairExists(rowEditor.Label))
                    {
                        MessageBox.Show("This label already exists in this category.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (_isUiFolderProject && rowEditor.ApplyToAllLocales)
                    {
                        foreach (var rt in _rTexts)
                        {
                            var rPage = rt.RText.GetPages()[page.Name];
                            rPage.AddRow(rowEditor.Id, rowEditor.Label, rowEditor.Data);
                        }

                        toolStripStatusLabel.Text = $"{rowEditor.Label} - added to {_rTexts.Count} locales";
                    }
                    else
                    {
                        var rowId = page.AddRow(rowEditor.Id, rowEditor.Label, rowEditor.Data);
                        toolStripStatusLabel.Text = $"{rowEditor.Label} - added";
                    }

                    DisplayEntries(page);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                toolStripStatusLabel.Text = ex.Message;
            }
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewPages.SelectedItems.Count <= 0 || listViewPages.SelectedItems[0] == null) return;
            if (listViewEntries.SelectedItems.Count <= 0 || listViewEntries.SelectedItems[0] == null) return;

            try
            {
                var pageLViewItem = listViewPages.SelectedItems[0];
                var page = (RTextPageBase)pageLViewItem.Tag;

                var lViewItem = listViewEntries.SelectedItems[0];
                RTextPairUnit rowData = (RTextPairUnit)lViewItem.Tag;

                if (MessageBox.Show($"Are you sure you want to delete {rowData.Label}?", "Delete confirmation", MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    page.DeleteRow(rowData.Label);

                    toolStripStatusLabel.Text = $"{rowData.Label} - deleted";

                    DisplayEntries(page);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                toolStripStatusLabel.Text = ex.Message;
            }
        }

        private void listViewEntries_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == _columnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                _columnSorter.Order = _columnSorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                _columnSorter.SortColumn = e.Column;
                _columnSorter.Order = SortOrder.Ascending;
            }

            // Adjust the sort icon
            this.listViewEntries.SetSortIcon(e.Column, _columnSorter.Order);

            // Perform the sort with these new sort options.
            this.listViewEntries.Sort();
        }

        private void tabControlLocalFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_isUiFolderProject || tabControlLocalFiles.TabCount <= 0)
                return;

            ClearListViews();
            DisplayPages();
        }

        private void addEditFromCSVFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_rTexts.Any() || CurrentRText is null || CurrentPage is null) return;

            if (csvOpenFileDialog.ShowDialog(this) != DialogResult.OK) return;

            Dictionary<string, string> kv = new Dictionary<string, string>();
            try
            {
                using (var file = File.OpenText(csvOpenFileDialog.FileName))
                {
                    while (!file.EndOfStream)
                    {
                        string line = file.ReadLine();
                        if (string.IsNullOrEmpty(line))
                            continue;

                        string[] spl = line.Split(',');
                        if (spl.Length != 2)
                            continue;

                        string key = spl[0];
                        string value = spl[1];

                        if (!kv.TryGetValue(key, out _))
                            kv.Add(key, value);
                    }
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel.Text = $"Unable to read CSV file: {ex.Message}";
                return;
            }

            if (!kv.Any())
            {
                toolStripStatusLabel.Text = "Error: No valid key/value pairs found in CSV file.";
                return;
            }

            if (_isUiFolderProject)
            {
                var res = MessageBox.Show($"Add to all opened locales?", "Confirmation", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                if (res == DialogResult.Yes)
                {

                    foreach (var rtext in _rTexts)
                    {
                        if (rtext.RText.GetPages().TryGetValue(CurrentPage.Name, out var localePage))
                            localePage.AddPairs(kv);
                    }
                    toolStripStatusLabel.Text = $"Added/Edited {kv.Count} entries for {_rTexts.Count} locales.";
                }
                else if (res == DialogResult.No)
                {
                    CurrentPage.AddPairs(kv);
                    toolStripStatusLabel.Text = $"Added/Edited {kv.Count} entries.";
                }
                else
                    return;
            }
            else
            {
                CurrentPage.AddPairs(kv);
                toolStripStatusLabel.Text = $"Added/Edited {kv.Count} entries.";
            }

            
            DisplayEntries(CurrentPage);
        }

        private void importExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_rTexts.Any() || CurrentRText is null || CurrentPage is null)
            {
                MessageBox.Show("No file loaded or category selected.", "Warning", 
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ImportExcelForCategory_Click(sender, e);
        }

        #endregion

        private void ClearTabs()
        {
            tabControlLocalFiles.TabPages.Clear();
        }

        private void ClearListViews()
        {
            ClearCategoriesLView();
            ClearEntriesLView();
        }

        private void ClearCategoriesLView()
        {
            listViewPages.BeginUpdate();
            listViewPages.Items.Clear();
            listViewPages.EndUpdate();
        }

        private void ClearEntriesLView()
        {
            listViewEntries.BeginUpdate();
            listViewEntries.Items.Clear();
            listViewEntries.EndUpdate();
        }

        private RTextParser ReadRTextFile(string filePath)
        {
            var rText = new RTextParser(new ConsoleWriter());
            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                rText.Read(data);
                _rTexts.Add(rText);
                return rText;
            }
            catch (XorKeyTooShortException ex)
            {
                toolStripStatusLabel.Text = $"Error reading the file: {filePath}";
                MessageBox.Show("Couldn't decrypt all strings. Please contact xfileFIN for more information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                toolStripStatusLabel.Text = $"Error reading the file: {filePath}";
            }

            return null;
        }

        private void DisplayPages()
        {
            if (CurrentRText == null)
            {
                MessageBox.Show("Read a valid RT04 file first.", "Oops...", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            listViewPages.BeginUpdate();
            listViewPages.Items.Clear();
            var pages = CurrentRText.RText.GetPages();
            var items = new ListViewItem[pages.Count];

            int i = 0;
            foreach (var page in pages)
                items[i++] = new ListViewItem(page.Key) { Tag = page.Value };

            listViewPages.Items.AddRange(items);

            listViewPages.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listViewPages.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listViewPages.EndUpdate();
        }

        private void DisplayEntries(RTextPageBase page)
        {
            listViewEntries.BeginUpdate();
            SortEntriesListView(0);
            listViewEntries.Clear();

            // Set the view to show details.
            listViewEntries.View = View.Details;
            // Allow the user to edit item text.
            listViewEntries.LabelEdit = true;
            // Show item tooltips.
            listViewEntries.ShowItemToolTips = true;
            // Allow the user to rearrange columns.
            //lView.AllowColumnReorder = true;
            // Select the item and subitems when selection is made.
            listViewEntries.FullRowSelect = true;
            // Display grid lines.
            listViewEntries.GridLines = true;

            // Add column headers
            listViewEntries.Columns.Add("RecNo", -2, HorizontalAlignment.Left);
            if ((CurrentRText.RText is RT03) == false)
                listViewEntries.Columns.Add("Id", -2, HorizontalAlignment.Left);
            listViewEntries.Columns.Add("Label", -2, HorizontalAlignment.Left);
            listViewEntries.Columns.Add("String", -2, HorizontalAlignment.Left);

            // Add entries
            var entries = page.PairUnits;
            var items = new ListViewItem[entries.Count];

            int i = 0;
            foreach (var entry in entries)
            {
                if ((CurrentRText.RText is RT03) == false)
                    items[i] = new ListViewItem(new[] { i.ToString(), entry.Value.ID.ToString(), entry.Value.Label, entry.Value.Value }) { Tag = entry.Value };
                else
                    items[i] = new ListViewItem(new[] { i.ToString(), entry.Value.Label, entry.Value.Value }) { Tag = entry.Value };
                i++;
            }

            listViewEntries.Items.AddRange(items);

            listViewEntries.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listViewEntries.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listViewEntries.EndUpdate();
        }

        private void SortEntriesListView(int columnIndex)
        {
            // Set the column number that is to be sorted; default to ascending.
            _columnSorter.SortColumn = columnIndex;
            _columnSorter.Order = SortOrder.Ascending;

            // Adjust the sort icon
            this.listViewEntries.SetSortIcon(columnIndex, _columnSorter.Order);

            // Perform the sort with these new sort options.
            this.listViewEntries.Sort();
        }

        #region Excel Import Feature

        private void InitializeExcelImportFeature()
        {
            // Criar menu de contexto para as categorias
            _categoriesContextMenu = new ContextMenuStrip();
            
            var importCsvItem = new ToolStripMenuItem("Import CSV to this category");
            importCsvItem.Click += ImportExcelForCategory_Click;
            importCsvItem.Image = null; // Pode adicionar um ícone se desejar
            
            var exportCsvItem = new ToolStripMenuItem("Export this category to CSV");
            exportCsvItem.Click += ExportCategoryToCsv_Click;
            exportCsvItem.Image = null; // Pode adicionar um ícone se desejar
            
            var createSampleItem = new ToolStripMenuItem("Create a CSV in the correct format");
            createSampleItem.Click += CreateSampleCsv_Click;
            
            _categoriesContextMenu.Items.Add(importCsvItem);
            _categoriesContextMenu.Items.Add(exportCsvItem);
            _categoriesContextMenu.Items.Add(new ToolStripSeparator());
            _categoriesContextMenu.Items.Add(createSampleItem);
            
            // Associar o menu de contexto ao ListView de páginas/categorias
            listViewPages.ContextMenuStrip = _categoriesContextMenu;
        }

        private void CreateSampleCsv_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Save sample CSV file",
                FileName = "sample_import.csv"
            };

            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    ExcelImporter.CreateSampleCsv(saveFileDialog.FileName);
                    MessageBox.Show($"Sample file created successfully!\n\nLocation: {saveFileDialog.FileName}\n\n" +
                                  "You can edit this file and use it to import data.", 
                                  "File created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    toolStripStatusLabel.Text = "Sample CSV file created successfully";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating sample file: {ex.Message}", "Error", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ImportExcelForCategory_Click(object sender, EventArgs e)
        {
            // Verificar se há uma categoria selecionada
            if (listViewPages.SelectedItems.Count <= 0 || listViewPages.SelectedItems[0] == null)
            {
                MessageBox.Show("Please select a category first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedItem = listViewPages.SelectedItems[0];
            var page = (RTextPageBase)selectedItem.Tag;
            var categoryName = page.Name;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select CSV file for import",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                ImportCsvData(openFileDialog.FileName, page, categoryName);
            }
        }

        private void ImportCsvData(string filePath, RTextPageBase page, string categoryName)
        {
            try
            {
                // Mostrar cursor de espera
                this.Cursor = Cursors.WaitCursor;
                toolStripStatusLabel.Text = "Importing CSV data...";

                var importResult = ExcelImporter.ImportFromCsv(filePath);

                if (!importResult.Success)
                {
                    MessageBox.Show($"Import error: {importResult.Message}", "Error", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Mostrar prévia e confirmar importação
                var previewMessage = $"File: {Path.GetFileName(filePath)}\n" +
                                   $"Category: {categoryName}\n" +
                                   $"Records found: {importResult.ImportedEntries.Count}\n\n" +
                                   "First records:\n";

                // Mostrar os primeiros 5 registros como prévia
                var preview = importResult.ImportedEntries.Take(5);
                foreach (var entry in preview)
                {
                    var truncatedString = entry.String.Length > 50 ? entry.String.Substring(0, 50) + "..." : entry.String;
                    previewMessage += $"• {entry.RecNo} | {entry.Label} | {truncatedString}\n";
                }

                if (importResult.ImportedEntries.Count > 5)
                {
                    previewMessage += $"... and {importResult.ImportedEntries.Count - 5} more records.\n";
                }

                previewMessage += "\nThis operation will:\n" +
                                "• Replace existing texts with same Label\n" +
                                "• Add new records if Label doesn't exist\n" +
                                "• Keep existing records not in the file\n\n" +
                                "Do you want to continue with the import?";

                var confirmResult = MessageBox.Show(previewMessage, "Confirm Import", 
                                                  MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirmResult == DialogResult.Yes)
                {
                    ApplyImportedData(importResult.ImportedEntries, page, categoryName);
                    
                    MessageBox.Show($"Import completed successfully!\n\n{importResult.Message}", 
                                  "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Atualizar a interface
                    DisplayEntries(page);
                    toolStripStatusLabel.Text = $"Import completed: {importResult.ImportedEntries.Count} records processed";
                }
                else
                {
                    toolStripStatusLabel.Text = "Import cancelled by user";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during import: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                toolStripStatusLabel.Text = "Error during import";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void ApplyImportedData(List<ImportedEntry> entries, RTextPageBase page, string categoryName)
        {
            int updatedCount = 0;
            int addedCount = 0;

            foreach (var entry in entries)
            {
                if (_isUiFolderProject)
                {
                    // Apply to all locales if it's a folder project
                    foreach (var rt in _rTexts)
                    {
                        try
                        {
                            var rtPage = rt.RText.GetPages()[categoryName];
                            if (rtPage.PairExists(entry.Label))
                            {
                                // For existing records, preserve original ID and only update the value
                                var existingPair = rtPage.PairUnits[entry.Label];
                                rtPage.EditRow(existingPair.ID, entry.Label, entry.String);
                                updatedCount++;
                            }
                            else
                            {
                                // For new records, use a unique ID based on last ID + 1
                                int newId = rtPage.PairUnits.Count > 0 ? rtPage.GetLastId() + 1 : 1;
                                rtPage.AddRow(newId, entry.Label, entry.String);
                                addedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error applying data to locale {rt.LocaleCode}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // Apply only to current file
                    if (page.PairExists(entry.Label))
                    {
                        // For existing records, preserve original ID and only update the value
                        var existingPair = page.PairUnits[entry.Label];
                        page.EditRow(existingPair.ID, entry.Label, entry.String);
                        updatedCount++;
                    }
                    else
                    {
                        // For new records, use a unique ID based on last ID + 1
                        int newId = page.PairUnits.Count > 0 ? page.GetLastId() + 1 : 1;
                        page.AddRow(newId, entry.Label, entry.String);
                        addedCount++;
                    }
                }
            }

            var summary = "";
            if (_isUiFolderProject)
            {
                summary = $"Aplicado para {_rTexts.Count} locales: ";
            }
            
            summary += $"{updatedCount} registros atualizados, {addedCount} registros adicionados";
            
            Console.WriteLine($"Importação completada: {summary}");
        }

        private void ExportCategoryToCsv_Click(object sender, EventArgs e)
        {
            // Verificar se há uma categoria selecionada
            if (listViewPages.SelectedItems.Count <= 0 || listViewPages.SelectedItems[0] == null)
            {
                MessageBox.Show("Please select a category first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedItem = listViewPages.SelectedItems[0];
            var page = (RTextPageBase)selectedItem.Tag;
            var categoryName = page.Name;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Export category to CSV",
                FileName = $"{categoryName}_export.csv",
                CheckPathExists = true
            };

            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                ExportCategoryData(saveFileDialog.FileName, page, categoryName);
            }
        }

        private void ExportCategoryData(string filePath, RTextPageBase page, string categoryName)
        {
            try
            {
                // Mostrar cursor de espera
                this.Cursor = Cursors.WaitCursor;
                toolStripStatusLabel.Text = "Exporting category data...";

                // Converter os dados da página para o formato de exportação
                var exportEntries = new List<ImportedEntry>();

                foreach (var pair in page.PairUnits.Values.OrderBy(p => p.ID))
                {
                    exportEntries.Add(new ImportedEntry
                    {
                        RecNo = pair.ID,
                        Label = pair.Label,
                        String = pair.Value
                    });
                }

                // Exportar para CSV
                bool success = ExcelImporter.ExportToCsv(filePath, exportEntries);

                if (success)
                {
                    var successMessage = $"Category '{categoryName}' exported successfully!\n\n" +
                                       $"File: {Path.GetFileName(filePath)}\n" +
                                       $"Records exported: {exportEntries.Count}\n" +
                                       $"Format: RecNo,Label,String";

                    MessageBox.Show(successMessage, "Export Successful", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);

                    toolStripStatusLabel.Text = $"Export completed: {exportEntries.Count} records exported to {Path.GetFileName(filePath)}";
                }
                else
                {
                    MessageBox.Show("Failed to export category data. Please check the file path and try again.", 
                                  "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    toolStripStatusLabel.Text = "Export failed";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during export: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                toolStripStatusLabel.Text = "Error during export";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        #endregion
    }
}
