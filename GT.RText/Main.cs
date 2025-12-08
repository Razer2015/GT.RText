using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

// Required for the non crappy folder picker 
// https://stackoverflow.com/q/11624298
using Microsoft.WindowsAPICodePack.Dialogs;

using GT.RText.Core;
using GT.RText.Core.Exceptions;
using GT.Shared.Logging;
using System.Linq;
using System.Text;

namespace GT.RText
{
    public partial class Main : Form
    {
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

        public RTextParser CurrentRText
        {
            get
            {
                int index = tabControlLocalFiles.SelectedIndex;

                if (index < 0 || index >= _rTexts.Count)
                    return null; // or throw or handle gracefully

                return _rTexts[index];
            }
        }
        public RTextPageBase CurrentPage { get; set; }

        public Main()
        {
            InitializeComponent();

            listViewPages.Columns.Add("Category", -2, HorizontalAlignment.Left);

            _rTexts = new List<RTextParser>();
            _columnSorter = new ListViewColumnSorter();
            this.listViewEntries.ListViewItemSorter = _columnSorter;
            this.listViewEntries.Sorting = SortOrder.Ascending;
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
            var dialog = new CommonOpenFileDialog();
            dialog.EnsureFileExists = true;
            dialog.EnsurePathExists = true;

            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

            _rTexts.Clear();

            _isUiFolderProject = true;

            ClearListViews();
            ClearTabs();

            bool firstTab = true;
            string[] files = Directory.GetFiles(dialog.FileName, "*", SearchOption.TopDirectoryOnly);

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
                string[] folders = Directory.GetDirectories(dialog.FileName, "*", SearchOption.TopDirectoryOnly);
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
                    var dialog = new CommonOpenFileDialog();
                    dialog.EnsureFileExists = true;
                    dialog.EnsurePathExists = true;

                    dialog.IsFolderPicker = true;

                    if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

                    foreach (var rtext in _rTexts)
                    {
                        if (_isGT6AndAboveProjectStyle)
                        {
                            string localePath = Path.Combine(dialog.FileName, $"{rtext.LocaleCode}.rt2");
                            rtext.RText.Save(localePath);
                        }
                        else
                        {
                            string localePath = Path.Combine(dialog.FileName, rtext.LocaleCode);
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

		private void exportCSVToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (CurrentRText is null || CurrentPage is null)
			{
				MessageBox.Show("No category selected to export.", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "CSV File (*.csv)|*.csv";
				sfd.Title = "Export Category to CSV";
				sfd.FileName = $"{CurrentPage.Name}.csv";

				if (sfd.ShowDialog() != DialogResult.OK)
					return;

				try
				{
					using (var writer = new StreamWriter(sfd.FileName, false, new System.Text.UTF8Encoding(true)))
					{
						if ((CurrentRText.RText is RT03) == false)
							writer.WriteLine("RecNo,Id,Label,String");
						else
							writer.WriteLine("RecNo,Label,String");

						int index = 0;
						foreach (var entry in CurrentPage.PairUnits)
						{
							var unit = entry.Value;

							string safeLabel = unit.Label.Replace("\"", "\"\"");
							string safeString = unit.Value.Replace("\"", "\"\"");

							if ((CurrentRText.RText is RT03) == false)
							{
								writer.WriteLine($"{index},{unit.ID},\"{safeLabel}\",\"{safeString}\"");
							}
							else
							{
								writer.WriteLine($"{index},\"{safeLabel}\",\"{safeString}\"");
							}

							index++;
						}
					}

					toolStripStatusLabel.Text = $"CSV exported: {sfd.FileName}";
				}
				catch (Exception ex)
				{
					toolStripStatusLabel.Text = $"Error exporting CSV: {ex.Message}";
					MessageBox.Show($"Failed to export CSV:\n{ex.Message}", "Error",
						MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

        private void addEditFromCSVFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_rTexts.Any() || CurrentRText is null || CurrentPage is null)
                return;

            if (csvOpenFileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                bool isRT03 = CurrentRText.RText is RT03;

                Encoding encoding;
                using (var fs = new FileStream(csvOpenFileDialog.FileName, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new StreamReader(fs, true)) // detect BOM
                    {
                        reader.Peek(); // trigger BOM detection
                        encoding = reader.CurrentEncoding;
                    }
                }

                if (encoding == Encoding.UTF8)
                {
                    byte[] bytes = File.ReadAllBytes(csvOpenFileDialog.FileName);
                    string text = Encoding.UTF8.GetString(bytes);
                    if (text.Contains(' ')) // replacement character indicates misread encoding
                        encoding = Encoding.GetEncoding(1252); // Windows-1252
                }

                List<string> lines = new List<string>();
                using (var reader = new StreamReader(csvOpenFileDialog.FileName, encoding))
                {
                    while (!reader.EndOfStream)
                        lines.Add(reader.ReadLine());
                }

                if (lines.Count <= 1)
                {
                    MessageBox.Show("CSV contains no entries.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                List<(int? Id, string Label, string Value)> parsed = new List<(int?, string, string)>();
                string currentLine = null;

                for (int i = 1; i < lines.Count; i++)
                {
                    string line = lines[i];

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (currentLine == null)
                        currentLine = line;
                    else
                        currentLine += "\n" + line;

                    int quoteCount = currentLine.Count(c => c == '"');
                    if (quoteCount % 2 == 0)
                    {
                        string[] fields = ParseCsvLine(currentLine);
                        currentLine = null;

                        if (fields == null || (!isRT03 && fields.Length < 4) || (isRT03 && fields.Length < 3))
                            continue;

                        if (!int.TryParse(fields[0], out int recNo))
                            throw new Exception($"Invalid RecNo at line {i + 1}: \"{fields[0]}\"");

                        int? id = null;
                        string label;
                        string value;

                        if (!isRT03)
                        {
                            if (!int.TryParse(fields[1], out int parsedId))
                                throw new Exception($"Invalid ID at line {i + 1}: \"{fields[1]}\"");
                            id = parsedId;
                            label = fields[2];
                            value = fields[3];
                        }
                        else
                        {
                            label = fields[1];
                            value = fields[2];
                        }

                        parsed.Add((id, label, value));
                    }
                }

                if (!parsed.Any())
                {
                    MessageBox.Show("No valid rows found in CSV.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool applyToAll = false;
                if (_isUiFolderProject)
                {
                    var res = MessageBox.Show("Apply changes to all opened locales?", "Confirmation",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                    if (res == DialogResult.Cancel)
                        return;

                    applyToAll = (res == DialogResult.Yes);
                }

                void ApplyToPage(RTextPageBase page)
                {
                    foreach (var (Id, Label, Value) in parsed)
                    {
                        if (page.PairExists(Label))
                            page.DeleteRow(Label);

                        if (!isRT03 && Id != null)
                            page.AddRow(Id.Value, Label, Value);
                        else
                            page.AddRow(page.GetLastId() + 1, Label, Value);
                    }
                }

                if (applyToAll)
                {
                    foreach (var rtext in _rTexts)
                    {
                        if (rtext.RText.GetPages().TryGetValue(CurrentPage.Name, out var page))
                            ApplyToPage(page);
                    }

                    toolStripStatusLabel.Text =
                        $"Added/Edited {parsed.Count} entries for {_rTexts.Count} locales.";
                }
                else
                {
                    ApplyToPage(CurrentPage);
                    toolStripStatusLabel.Text = $"Added/Edited {parsed.Count} entries.";
                }

                DisplayEntries(CurrentPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to import CSV:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        private string[] ParseCsvLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return Array.Empty<string>();

            var fields = new List<string>();
            bool inQuotes = false;
            var field = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            fields.Add(field.ToString());
            return fields.ToArray();
        }

        private void saveFileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
