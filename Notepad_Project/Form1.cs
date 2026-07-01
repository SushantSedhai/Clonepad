using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Text;

namespace Notepad_Project
{
    public partial class Form1 : Form
    {
        private string _lastSearchTerm = "";
        private float _defaultFontSize;

        public Form1()
        {
            InitializeComponent();
            _defaultFontSize = textBox1.Font.Size; // Store default zoom level
        }

        // -------------------- Find Logic --------------------
        private void FindInCurrentTab(string term, bool searchForward = true)
        {
            TextBox tb = GetCurrentTextBox();
            if (tb == null || string.IsNullOrEmpty(term)) return;

            string text = tb.Text;
            int startIndex = searchForward
                ? tb.SelectionStart + tb.SelectionLength
                : tb.SelectionStart - 1;

            int index;
            if (searchForward)
            {
                index = text.IndexOf(term, Math.Max(0, startIndex), StringComparison.OrdinalIgnoreCase);
                if (index < 0) index = text.IndexOf(term, 0, StringComparison.OrdinalIgnoreCase); // Wrap
            }
            else
            {
                index = text.LastIndexOf(term, Math.Max(0, startIndex), StringComparison.OrdinalIgnoreCase);
                if (index < 0) index = text.LastIndexOf(term, StringComparison.OrdinalIgnoreCase); // Wrap
            }

            if (index >= 0)
            {
                tb.Select(index, term.Length);
                tb.ScrollToCaret();
                _lastSearchTerm = term;
            }
            else
            {
                MessageBox.Show($"Cannot find \"{term}\"", "Notepad",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // -------------------- Show Find Dialog --------------------
        private void ShowFindDialog()
        {
            Form findForm = new Form
            {
                Text = "Find",
                Size = new System.Drawing.Size(420, 130),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent
            };

            Label lbl = new Label { Text = "Find what:", Left = 10, Top = 15, Width = 80 };
            TextBox searchBox = new TextBox { Left = 95, Top = 12, Width = 210, Text = _lastSearchTerm };
            Button findBtn = new Button { Text = "Find Next", Left = 315, Top = 10, Width = 90 };
            Button closeBtn = new Button { Text = "Close", Left = 315, Top = 45, Width = 90 };

            findBtn.Click += (s, e) => { _lastSearchTerm = searchBox.Text; FindInCurrentTab(searchBox.Text); };
            closeBtn.Click += (s, e) => findForm.Close();

            findForm.Controls.AddRange(new Control[] { lbl, searchBox, findBtn, closeBtn });
            findForm.Show(this);
        }

        // -------------------- Show Replace Dialog --------------------
        private void ShowReplaceDialog()
        {
            Form replaceForm = new Form
            {
                Text = "Replace",
                Size = new System.Drawing.Size(420, 175),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent
            };

            Label findLbl = new Label { Text = "Find what:", Left = 10, Top = 15, Width = 90 };
            TextBox findBox = new TextBox { Left = 105, Top = 12, Width = 195, Text = _lastSearchTerm };
            Label replaceLbl = new Label { Text = "Replace with:", Left = 10, Top = 48, Width = 90 };
            TextBox replaceBox = new TextBox { Left = 105, Top = 45, Width = 195 };
            Button findBtn = new Button { Text = "Find Next", Left = 310, Top = 10, Width = 95 };
            Button replaceBtn = new Button { Text = "Replace", Left = 310, Top = 45, Width = 95 };
            Button replAllBtn = new Button { Text = "Replace All", Left = 310, Top = 80, Width = 95 };
            Button closeBtn = new Button { Text = "Close", Left = 10, Top = 105, Width = 80 };

            findBtn.Click += (s, e) =>
            {
                _lastSearchTerm = findBox.Text;
                FindInCurrentTab(findBox.Text);
            };

            replaceBtn.Click += (s, e) =>
            {
                TextBox tb = GetCurrentTextBox();
                if (tb == null) return;
                if (tb.SelectedText.Equals(findBox.Text, StringComparison.OrdinalIgnoreCase))
                    tb.SelectedText = replaceBox.Text;
                FindInCurrentTab(findBox.Text);
            };

            replAllBtn.Click += (s, e) =>
            {
                TextBox tb = GetCurrentTextBox();
                if (tb == null || string.IsNullOrEmpty(findBox.Text)) return;
                int count = 0;
                string newText = tb.Text;
                int idx = 0;
                while ((idx = newText.IndexOf(findBox.Text, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    newText = newText.Remove(idx, findBox.Text.Length).Insert(idx, replaceBox.Text);
                    idx += replaceBox.Text.Length;
                    count++;
                }
                tb.Text = newText;
                MessageBox.Show($"{count} replacement(s) made.", "Replace All",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            closeBtn.Click += (s, e) => replaceForm.Close();

            replaceForm.Controls.AddRange(new Control[]
                { findLbl, findBox, replaceLbl, replaceBox, findBtn, replaceBtn, replAllBtn, closeBtn });
            replaceForm.Show(this);
        }

        // -------------------- Show Goto Dialog --------------------
        private void ShowGotoDialog()
        {
            TextBox tb = GetCurrentTextBox();
            if (tb == null) return;

            Form gotoForm = new Form
            {
                Text = "Go To Line",
                Size = new System.Drawing.Size(260, 115),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent
            };

            Label lbl = new Label { Text = "Line number:", Left = 10, Top = 15, Width = 90 };
            TextBox lineBox = new TextBox { Left = 105, Top = 12, Width = 80 };
            Button goBtn = new Button { Text = "Go", Left = 60, Top = 48, Width = 60 };
            Button cancelBtn = new Button { Text = "Cancel", Left = 135, Top = 48, Width = 70 };

            goBtn.Click += (s, e) =>
            {
                if (int.TryParse(lineBox.Text, out int lineNum) && lineNum > 0)
                {
                    string[] lines = tb.Text.Split('\n');
                    if (lineNum <= lines.Length)
                    {
                        int charIndex = 0;
                        for (int i = 0; i < lineNum - 1; i++)
                            charIndex += lines[i].Length + 1;
                        tb.SelectionStart = charIndex;
                        tb.SelectionLength = 0;
                        tb.ScrollToCaret();
                        tb.Focus();
                        gotoForm.Close();
                    }
                    else
                    {
                        MessageBox.Show($"Line number out of range (1–{lines.Length}).",
                            "Go To Line", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Enter a valid line number.", "Go To Line",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            cancelBtn.Click += (s, e) => gotoForm.Close();

            gotoForm.Controls.AddRange(new Control[] { lbl, lineBox, goBtn, cancelBtn });
            gotoForm.ShowDialog(this);
        }

        // -------------------- Zoom --------------------
        private void ZoomIn()
        {
            TextBox tb = GetCurrentTextBox();
            if (tb == null) return;
            float newSize = Math.Min(tb.Font.Size + 2f, 72f);
            tb.Font = new System.Drawing.Font(tb.Font.FontFamily, newSize, tb.Font.Style);
        }

        private void ZoomOut()
        {
            TextBox tb = GetCurrentTextBox();
            if (tb == null) return;
            float newSize = Math.Max(tb.Font.Size - 2f, 6f);
            tb.Font = new System.Drawing.Font(tb.Font.FontFamily, newSize, tb.Font.Style);
        }

        private void RestoreZoom()
        {
            TextBox tb = GetCurrentTextBox();
            if (tb == null) return;
            tb.Font = new System.Drawing.Font(tb.Font.FontFamily, _defaultFontSize, tb.Font.Style);
        }

        // -------------------- Global Keyboard Shortcuts --------------------
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
           
            if (keyData == (Keys.Control | Keys.Shift | Keys.N))
            {
                Form1 newForm = new Form1();
                newForm.Show();
                newForm.Text = "Untitled";

                return true; // Mark as handled
            }
            if (keyData == (Keys.Control | Keys.N))
            {
                AddNewTabWithMenu();
                return true; // Mark as handled
            }
            if (keyData == (Keys.Control | Keys.P))
            {
                PrintFileForCurrentTab();
                return true;
            }
            if (keyData == (Keys.Control | Keys.O))
            {
                OpenFileForCurrentTab();
                return true; // Mark as handled
            }
            if (keyData == (Keys.Control | Keys.S))
            {
                SaveFileForCurrentTab();
                return true; // Mark as handled
            }

            if (keyData == (Keys.Control | Keys.Z))
            {
                TextBox tb = GetCurrentTextBox();
                if (tb != null && tb.CanUndo)
                    tb.Undo();
                return true;
            }

            if (keyData == (Keys.Control | Keys.W))
            {
                TabPage currentTab = tabControl1.SelectedTab;
                if (currentTab == null) return true;

                if (tabControl1.TabPages.Count > 1)
                {
                    tabControl1.TabPages.Remove(currentTab);
                    currentTab.Dispose();
                }
                else
                {
                    var result = MessageBox.Show(
                        "Do you want to close the last tab?",
                        "Warning",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.OK)
                    {
                        tabControl1.TabPages.Remove(currentTab);
                        currentTab.Dispose();
                        this.Close();
                    }
                }

                return true; // Mark as handled
            }
            if (keyData == (Keys.Control | Keys.F))        { ShowFindDialog();                            return true; }
if (keyData == Keys.F3)                         { FindInCurrentTab(_lastSearchTerm, true);     return true; }
if (keyData == (Keys.Shift | Keys.F3))          { FindInCurrentTab(_lastSearchTerm, false);    return true; }
if (keyData == (Keys.Control | Keys.H))         { ShowReplaceDialog();                         return true; }
if (keyData == (Keys.Control | Keys.G))         { ShowGotoDialog();                            return true; }
if (keyData == Keys.F5)                         { timeDateToolStripMenuItem_Click(null, null); return true; }
if (keyData == (Keys.Control | Keys.Oemplus))   { ZoomIn();                                    return true; }
if (keyData == (Keys.Control | Keys.OemMinus))  { ZoomOut();                                   return true; }
if (keyData == (Keys.Control | Keys.D0))        { RestoreZoom();                               return true; }
if (keyData == (Keys.Control | Keys.A))         { GetCurrentTextBox()?.SelectAll();            return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // -------------------- Print Current Tab --------------------
        private void PrintFileForCurrentTab()
        {
            TextBox tb = GetCurrentTextBox();
            if (tb == null)
            {
                MessageBox.Show("No active text box to print.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PrintDialog printDialog = new PrintDialog();
            System.Drawing.Printing.PrintDocument printDocument = new System.Drawing.Printing.PrintDocument();

            string[] lines = tb.Text.Split('\n');
            int lineIndex = 0;

            printDocument.PrintPage += (s, e) =>
            {
                float y = e.MarginBounds.Top;
                float lineHeight = tb.Font.GetHeight(e.Graphics);

                while (lineIndex < lines.Length)
                {
                    if (y + lineHeight > e.MarginBounds.Bottom)
                    {
                        e.HasMorePages = true; // More pages to print
                        return;
                    }

                    e.Graphics.DrawString(
                        lines[lineIndex],
                        tb.Font,
                        System.Drawing.Brushes.Black,
                        e.MarginBounds.Left,
                        y
                    );

                    y += lineHeight;
                    lineIndex++;
                }

                e.HasMorePages = false;
            };

            printDialog.Document = printDocument;

            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                lineIndex = 0; // Reset before printing
                printDocument.Print();
            }
        }

        // -------------------- Clone MenuStrip Items for New Tabs --------------------
        private ToolStripItem CloneToolStripItemWithAction(ToolStripItem original)
        {
            switch (original)
            {
                case ToolStripMenuItem menuItem:
                    ToolStripMenuItem clone = new ToolStripMenuItem(menuItem.Text)
                    {
                        Enabled = menuItem.Enabled,
                        Checked = menuItem.Checked,
                        Image = menuItem.Image,
                        ShortcutKeys = menuItem.ShortcutKeys,
                        ShortcutKeyDisplayString = menuItem.ShortcutKeyDisplayString, // ← ADD THIS
                        Tag = menuItem.Tag
                    };

                    // Wire actions for specific menu items
                    if (menuItem.Name == "newTabToolStripMenuItem")
                    {
                        clone.Click += (s, e) => AddNewTabWithMenu();
                    }
                    else if (menuItem.Name == "undoToolStripMenuItem")
                    {
                        clone.Click += (s, e) =>
                        {
                            TextBox tb = GetCurrentTextBox();
                            if (tb != null && tb.CanUndo)
                                tb.Undo();
                        };
                    }
                    else if (menuItem.Name == "newWindowToolStripMenuItem")
                    {
                        clone.Click += (s, e) =>
                        {
                            Form1 newForm = new Form1();
                            newForm.Show();
                            newForm.Text = "Untitled";
                        };
                    }
                    else if (menuItem.Name == "openToolStripMenuItem")
                    {
                        clone.Click += (s, e) => OpenFileForCurrentTab();
                    }
                    else if (menuItem.Name == "saveToolStripMenuItem")
                    {
                        clone.Click += (s, e) => SaveFileForCurrentTab();
                    }
                    else if (menuItem.Name == "printToolStripMenuItem")
                    {
                        clone.Click += (s, e) => PrintFileForCurrentTab();
                    }
                    else if (menuItem.Name == "closeWindowToolStripMenuItem")
                    {
                        clone.Click += (s, e) =>
                        {
                            TabPage currentTab = tabControl1.SelectedTab;
                            if (currentTab == null) return;

                            if (tabControl1.TabPages.Count > 1)
                            {
                                tabControl1.TabPages.Remove(currentTab);
                                currentTab.Dispose();
                            }
                            else
                            {
                                if (MessageBox.Show("Do you want to close the last tab?", "Warning",
                                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                                {
                                    tabControl1.TabPages.Remove(currentTab);
                                    currentTab.Dispose();
                                    this.Close();
                                }
                            }
                        };
                    }

                    else if (menuItem.Name == "closeTabToolStripMenuItem")
                    {
                        clone.Click += (s, e) =>
                        {
                            TabPage currentTab = tabControl1.SelectedTab;
                            if (currentTab == null) return;

                            if (tabControl1.TabPages.Count > 1)
                            {
                                tabControl1.TabPages.Remove(currentTab);
                                currentTab.Dispose();
                            }
                            else
                            {
                                if (MessageBox.Show("Do you sure want to close last tab!", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                                {
                                    tabControl1.TabPages.Remove(currentTab);
                                    currentTab.Dispose();
                                }
                            }
                        };
                    }
                    else if (menuItem.Name == "cutToolStripMenuItem")
                        clone.Click += (s, e) => GetCurrentTextBox()?.Cut();
                    else if (menuItem.Name == "copyToolStripMenuItem")
                        clone.Click += (s, e) => GetCurrentTextBox()?.Copy();
                    else if (menuItem.Name == "pasteToolStripMenuItem")
                        clone.Click += (s, e) => GetCurrentTextBox()?.Paste();
                    else if (menuItem.Name == "selectAllToolStripMenuItem")
                        clone.Click += (s, e) => GetCurrentTextBox()?.SelectAll();
                    else if (menuItem.Name == "deleteToolStripMenuItem")
                    {
                        clone.Click += (s, e) =>
                        {
                            TextBox tb = GetCurrentTextBox();
                            if (tb != null && tb.SelectionLength > 0)
                                tb.SelectedText = "";
                        };
                    }
                    else if (menuItem.Name == "clearFormattingToolStripMenuItem")
                        clone.Click += (s, e) => clearFormattingToolStripMenuItem_Click(s, e);
                    else if (menuItem.Name == "searchWithBingToolStripMenuItem")
                        clone.Click += (s, e) => searchWithBingToolStripMenuItem_Click(s, e);
                    else if (menuItem.Name == "findToolStripMenuItem")
                        clone.Click += (s, e) => ShowFindDialog();
                    else if (menuItem.Name == "findNextToolStripMenuItem")
                        clone.Click += (s, e) => FindInCurrentTab(_lastSearchTerm, true);
                    else if (menuItem.Name == "findPreviousToolStripMenuItem")
                        clone.Click += (s, e) => FindInCurrentTab(_lastSearchTerm, false);
                    else if (menuItem.Name == "replaceToolStripMenuItem")
                        clone.Click += (s, e) => ShowReplaceDialog();
                    else if (menuItem.Name == "gotoToolStripMenuItem")
                        clone.Click += (s, e) => ShowGotoDialog();
                    else if (menuItem.Name == "timeDateToolStripMenuItem")
                        clone.Click += (s, e) => { TextBox tb = GetCurrentTextBox(); if (tb != null) tb.SelectedText = DateTime.Now.ToString("h:mm tt M/d/yyyy"); };
                    else if (menuItem.Name == "fontToolStripMenuItem")
                        clone.Click += (s, e) => fontToolStripMenuItem_Click(s, e);
                    else if (menuItem.Name == "zoomInToolStripMenuItem")
                        clone.Click += (s, e) => ZoomIn();
                    else if (menuItem.Name == "zoomOutToolStripMenuItem")
                        clone.Click += (s, e) => ZoomOut();
                    else if (menuItem.Name == "restoreDefaultZoomToolStripMenuItem")
                        clone.Click += (s, e) => RestoreZoom();
                    else if (menuItem.Name == "statusBarToolStripMenuItem")
                        clone.Click += (s, e) => statusBarToolStripMenuItem_Click(s, e);
                    else if (menuItem.Name == "statusWrapToolStripMenuItem")
                        clone.Click += (s, e) => statusWrapToolStripMenuItem_Click(s, e);
                    else if (menuItem.Name == "markdownToolStripMenuItem")
                        clone.Click += (s, e) => markdownToolStripMenuItem_Click(s, e);
                    // Recursively clone sub-items
                    foreach (ToolStripItem subItem in menuItem.DropDownItems)
                    {
                        clone.DropDownItems.Add(CloneToolStripItemWithAction(subItem));
                    }

                    return clone;

                case ToolStripSeparator separator:
                    // Clone separator
                    return new ToolStripSeparator();

                default:
                    return new ToolStripMenuItem(original.Text);
            }
        }

        // -------------------- Create TextBox Matching textBox1 --------------------
        private TextBox CreateTextBoxLikeOriginal()
        {
            return new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = textBox1.ScrollBars,
                Font = textBox1.Font,
                BackColor = textBox1.BackColor,
                ForeColor = textBox1.ForeColor,
                WordWrap = textBox1.WordWrap
            };
        }

        // -------------------- Add New Tab --------------------
        private void AddNewTabWithMenu()
        {
            TabPage newTab = new TabPage("Untitled");

            TextBox newTextBox = CreateTextBoxLikeOriginal();

            MenuStrip newMenu = new MenuStrip { Dock = DockStyle.Top };
            foreach (ToolStripItem item in menuStrip1.Items)
                newMenu.Items.Add(CloneToolStripItemWithAction(item));

            // Must match designer order: TextBox first (index 0), MenuStrip second (index 1)
            newTab.Controls.Add(newTextBox);
            newTab.Controls.Add(newMenu);

            tabControl1.TabPages.Add(newTab);
            tabControl1.SelectedTab = newTab;
        }

        // -------------------- Get Current Tab's TextBox --------------------
        private TextBox GetCurrentTextBox()
        {
            return tabControl1.SelectedTab?.Controls.OfType<TextBox>().FirstOrDefault();
        }

        private void OpenFileForCurrentTab()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string content = File.ReadAllText(ofd.FileName);
                TextBox tb = GetCurrentTextBox();
                if (tb != null)
                {
                    tb.Text = content;
                    tabControl1.SelectedTab.Text = Path.GetFileName(ofd.FileName); // ← ADD THIS
                }
            }
        }

        private void SaveFileForCurrentTab()
        {
            TextBox tb = GetCurrentTextBox();
            if (tb != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, tb.Text);
                    tabControl1.SelectedTab.Text = Path.GetFileName(sfd.FileName); // ← ADD THIS
                    MessageBox.Show("File saved successfully!", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("No TextBox is active.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // -------------------- Designer Event Handlers --------------------
        private void newTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddNewTabWithMenu();

        }

      

        private void newWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form1 newForm = new Form1();
            newForm.Show();
            newForm.Text = "Untitled";
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileForCurrentTab();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileForCurrentTab();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void closeTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TabPage currentTab = tabControl1.SelectedTab;
            if (currentTab == null) return;

            if (tabControl1.TabPages.Count > 1)
            {
                tabControl1.TabPages.Remove(currentTab);
                currentTab.Dispose();
            }
            else
            {
                if (MessageBox.Show("Do you sure want to close last tab!", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    tabControl1.TabPages.Remove(currentTab);
                    currentTab.Dispose();
                    this.Close();
                }
            }
        }

        private void closeWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TabPage currentTab = tabControl1.SelectedTab;
            if (currentTab == null) return ;

            if (tabControl1.TabPages.Count > 1)
            {
                tabControl1.TabPages.Remove(currentTab);
                currentTab.Dispose();
            }
            else
            {
                var result = MessageBox.Show(
                    "Do you want to close the last tab?",
                    "Warning",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.OK)
                {
                    tabControl1.TabPages.Remove(currentTab);
                    currentTab.Dispose();
                    this.Close();
                }
            }

            return ; 
        }

        private void newMarkdownTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrintFileForCurrentTab();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox tb = GetCurrentTextBox();
            if (tb != null && tb.CanUndo)
                tb.Undo();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetCurrentTextBox()?.Cut();
        }


        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetCurrentTextBox()?.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetCurrentTextBox()?.Paste();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetCurrentTextBox()?.SelectAll();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox tb = GetCurrentTextBox();
            if (tb != null && tb.SelectionLength > 0)
                tb.SelectedText = "";
        }

        private void copyToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            GetCurrentTextBox()?.Copy();
        }

        private void deleteToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            TextBox tb = GetCurrentTextBox();
            if (tb != null && tb.SelectionLength > 0)
                tb.SelectedText = "";
        }

        private void pasteToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            GetCurrentTextBox()?.Paste();
        }

        private void selectAllToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            GetCurrentTextBox()?.SelectAll();
        }

        private void clearFormattingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox tb = GetCurrentTextBox();
            if (tb == null) return;
            tb.Font = new System.Drawing.Font("Consolas", _defaultFontSize, System.Drawing.FontStyle.Regular);
            tb.ForeColor = System.Drawing.SystemColors.WindowText;
            tb.BackColor = System.Drawing.SystemColors.Window;
        }

        private void searchWithBingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox tb = GetCurrentTextBox();
            if (tb == null) return;
            string query = tb.SelectedText.Trim();
            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Select some text first to search with Bing.", "Search with Bing",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            System.Diagnostics.Process.Start(
                "https://www.bing.com/search?q=" + Uri.EscapeDataString(query));
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
            => ShowFindDialog();

        private void findNextToolStripMenuItem_Click(object sender, EventArgs e)
            => FindInCurrentTab(_lastSearchTerm, searchForward: true);

        private void findPreviousToolStripMenuItem_Click(object sender, EventArgs e)
            => FindInCurrentTab(_lastSearchTerm, searchForward: false);

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
            => ShowReplaceDialog();

        private void gotoToolStripMenuItem_Click(object sender, EventArgs e)
            => ShowGotoDialog();

        private void timeDateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox tb = GetCurrentTextBox();
            if (tb != null)
                tb.SelectedText = DateTime.Now.ToString("h:mm tt M/d/yyyy");
        }

        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox tb = GetCurrentTextBox();
            if (tb == null) return;
            FontDialog fd = new FontDialog { Font = tb.Font };
            if (fd.ShowDialog() == DialogResult.OK)
                tb.Font = fd.Font;
        }

        private void zoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Parent "Zoom" menu — no action needed, sub-items handle zoom
        }

        private void statusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Toggle visibility of status strip if you add one later
            // For now just toggle the checked state as a placeholder
            statusBarToolStripMenuItem.Checked = !statusBarToolStripMenuItem.Checked;
        }

        private void statusWrapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox tb = GetCurrentTextBox();
            if (tb == null) return;
            tb.WordWrap = !tb.WordWrap;
            tb.ScrollBars = tb.WordWrap ? ScrollBars.Vertical : ScrollBars.Both;
            statusWrapToolStripMenuItem.Checked = tb.WordWrap;
        }

        private void markdownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBox tb = GetCurrentTextBox();
            if (tb == null) return;
            string html = ConvertMarkdownToHtml(tb.Text);

            // Show preview in a new borderless form with a WebBrowser control
            Form previewForm = new Form
            {
                Text = "Markdown Preview",
                Size = new System.Drawing.Size(800, 600),
                StartPosition = FormStartPosition.CenterParent
            };
            System.Windows.Forms.WebBrowser browser = new System.Windows.Forms.WebBrowser
            {
                Dock = DockStyle.Fill,
                IsWebBrowserContextMenuEnabled = false
            };
            previewForm.Controls.Add(browser);
            browser.DocumentText = html;
            previewForm.Show(this);
        }

        // -------------------- Simple Markdown to HTML Converter --------------------
        private string ConvertMarkdownToHtml(string markdown)
        {
            var lines = markdown.Split('\n');
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<html><body style='font-family:Segoe UI;padding:20px;max-width:800px'>");

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd('\r');

                if (line.StartsWith("### "))
                    sb.AppendLine($"<h3>{line.Substring(4)}</h3>");
                else if (line.StartsWith("## "))
                    sb.AppendLine($"<h2>{line.Substring(3)}</h2>");
                else if (line.StartsWith("# "))
                    sb.AppendLine($"<h1>{line.Substring(2)}</h1>");
                else if (line.StartsWith("- ") || line.StartsWith("* "))
                    sb.AppendLine($"<li>{line.Substring(2)}</li>");
                else if (string.IsNullOrWhiteSpace(line))
                    sb.AppendLine("<br/>");
                else
                {
                    // Inline: bold, italic, code
                    string processed = line;
                    processed = System.Text.RegularExpressions.Regex.Replace(processed,
                        @"\*\*(.+?)\*\*", "<strong>$1</strong>");
                    processed = System.Text.RegularExpressions.Regex.Replace(processed,
                        @"\*(.+?)\*", "<em>$1</em>");
                    processed = System.Text.RegularExpressions.Regex.Replace(processed,
                        @"`(.+?)`", "<code>$1</code>");
                    sb.AppendLine($"<p>{processed}</p>");
                }
            }

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }
        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
    => ZoomIn();

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
            => ZoomOut();

        private void restoreDefaultZoomToolStripMenuItem_Click(object sender, EventArgs e)
            => RestoreZoom();
    }
}
        