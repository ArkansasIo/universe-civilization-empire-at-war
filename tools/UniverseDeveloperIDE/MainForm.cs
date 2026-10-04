using System.Diagnostics;
using System.Text;

namespace UniverseDeveloperIDE;

public sealed class MainForm : Form
{
    private readonly TreeView tree = new();
    private readonly TabControl editorTabs = new();
    private readonly RichTextBox output = new();
    private readonly ToolStripStatusLabel status = new();
    private readonly TextBox search = new();
    private readonly Dictionary<string, TabPage> openTabs = new(StringComparer.OrdinalIgnoreCase);
    private string? workspace;

    static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ts",".tsx",".js",".jsx",".json",".css",".scss",".html",".htm",".md",".sql",".cs",
        ".xml",".yml",".yaml",".env",".bat",".cmd",".ps1",".sh",".txt",".npmrc"
    };

    public MainForm()
    {
        Text = "Universe Developer IDE";
        Width = 1500;
        Height = 950;
        StartPosition = FormStartPosition.CenterScreen;
        BuildUi();
        MainMenuStrip = BuildMenus();
        Shown += (_, _) => OpenWorkspaceDialog();
    }

    void BuildUi()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 320 };
        var left = new Panel { Dock = DockStyle.Fill };
        search.PlaceholderText = "Filter files...";
        search.Dock = DockStyle.Top;
        search.Height = 32;
        search.TextChanged += (_, _) => RefreshTree();
        tree.Dock = DockStyle.Fill;
        tree.HideSelection = false;
        tree.AfterSelect += (_, e) => OpenFile(e.Node.Tag as string);
        left.Controls.Add(tree);
        left.Controls.Add(search);
        split.Panel1.Controls.Add(left);

        var right = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 650 };
        editorTabs.Dock = DockStyle.Fill;
        editorTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        editorTabs.DrawItem += DrawTab;
        right.Panel1.Controls.Add(editorTabs);

        output.Dock = DockStyle.Fill;
        output.Font = new Font("Consolas", 10);
        output.BackColor = Color.FromArgb(12, 16, 22);
        output.ForeColor = Color.Gainsboro;
        output.ReadOnly = true;
        right.Panel2.Controls.Add(output);

        split.Panel2.Controls.Add(right);
        Controls.Add(split);

        var bar = new StatusStrip();
        status.Text = "Ready";
        bar.Items.Add(status);
        Controls.Add(bar);
    }

    MenuStrip BuildMenus()
    {
        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add("Open Workspace...", null, (_, _) => OpenWorkspaceDialog());
        file.DropDownItems.Add("Open File...", null, (_, _) => OpenFileDialog());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Save", null, (_, _) => SaveCurrent());
        file.DropDownItems.Add("Save All", null, (_, _) => SaveAll());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Exit", null, (_, _) => Close());

        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.Add("Undo", null, (_, _) => CurrentEditor()?.Undo());
        edit.DropDownItems.Add("Redo", null, (_, _) => CurrentEditor()?.Redo());
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add("Find", null, (_, _) => FindText());

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add("Refresh Explorer", null, (_, _) => RefreshTree());
        view.DropDownItems.Add("Output / Terminal", null, (_, _) => output.Focus());

        var frontend = new ToolStripMenuItem("&Frontend");
        frontend.DropDownItems.Add("Install dependencies", null, (_, _) => RunNpm("install", true));
        frontend.DropDownItems.Add("Development server", null, (_, _) => RunNpm("run dev", false));
        frontend.DropDownItems.Add("Build frontend", null, (_, _) => RunNpm("run build", true));
        frontend.DropDownItems.Add("Lint / TypeScript", null, (_, _) => RunNpm("run lint", true));
        frontend.DropDownItems.Add("Test dependency", null, (_, _) => RunNpm("run test:dependency", true));

        var backend = new ToolStripMenuItem("&Backend");
        backend.DropDownItems.Add("Build server", null, (_, _) => RunNpm("run server:build", true));
        backend.DropDownItems.Add("Start server", null, (_, _) => RunNpm("run server:start", false));
        backend.DropDownItems.Add("Development server", null, (_, _) => RunNpm("run server:dev", false));
        backend.DropDownItems.Add("Build + start", null, (_, _) => RunNpm("run server:compile-run", false));
        backend.DropDownItems.Add(new ToolStripSeparator());
        backend.DropDownItems.Add("Open server folder", null, (_, _) => OpenFolder("server"));

        var database = new ToolStripMenuItem("&Database");
        database.DropDownItems.Add("Open database folder", null, (_, _) => OpenFolder("database"));
        database.DropDownItems.Add("Open schema.sql", null, (_, _) => OpenRelative("database/schema.sql"));
        database.DropDownItems.Add("Open migrations", null, (_, _) => OpenFolder("database/migrations"));

        var tools = new ToolStripMenuItem("&Tools");
        tools.DropDownItems.Add("Generate Admin Terminal EXE", null, (_, _) =>
            RunCommand("dotnet", "publish tools/AdminSystemsTerminal/AdminSystemsTerminal.csproj -c Release", true));
        tools.DropDownItems.Add("Generate Developer IDE EXE", null, (_, _) =>
            RunCommand("dotnet", "publish tools/UniverseDeveloperIDE/UniverseDeveloperIDE.csproj -c Release", true));
        tools.DropDownItems.Add("Git status", null, (_, _) => RunCommand("git", "status --short", false));
        tools.DropDownItems.Add("Git log", null, (_, _) => RunCommand("git", "log -10 --oneline", false));
        tools.DropDownItems.Add("Open terminal here", null, (_, _) => OpenTerminal());

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add("Workspace information", null, (_, _) => ShowWorkspaceInfo());
        help.DropDownItems.Add("About", null, (_, _) => MessageBox.Show(
            "Universe Developer IDE\nBackend + frontend workspace editor for the Universe Civilization Empire at War project.\n\n.NET 8 / WinForms",
            "About"));

        menu.Items.AddRange(new ToolStripItem[] { file, edit, view, frontend, backend, database, tools, help });
        menu.Dock = DockStyle.Top;
        Controls.Add(menu);
        return menu;
    }

    void OpenWorkspaceDialog()
    {
        using var d = new FolderBrowserDialog { Description = "Select the project/workspace root folder" };
        if (d.ShowDialog(this) == DialogResult.OK)
        {
            workspace = d.SelectedPath;
            Text = $"Universe Developer IDE — {workspace}";
            RefreshTree();
        }
    }

    void RefreshTree()
    {
        tree.BeginUpdate();
        try
        {
            tree.Nodes.Clear();
            if (string.IsNullOrWhiteSpace(workspace) || !Directory.Exists(workspace))
                return;

            var root = new TreeNode(Path.GetFileName(workspace)) { Tag = workspace };
            tree.Nodes.Add(root);
            AddDirectoryNodes(root, workspace);
            root.Expand();
        }
        finally
        {
            tree.EndUpdate();
        }
    }

    void AddDirectoryNodes(TreeNode parent, string dir)
    {
        try
        {
            foreach (var d in Directory.EnumerateDirectories(dir).OrderBy(Path.GetFileName))
            {
                var name = Path.GetFileName(d);
                if (name is ".git" or "node_modules" or "dist" or "dist-server" or "bin" or "obj")
                    continue;

                var dn = new TreeNode(name) { Tag = d };
                parent.Nodes.Add(dn);
                AddDirectoryNodes(dn, d);
            }

            var filter = search.Text.Trim();
            foreach (var f in Directory.EnumerateFiles(dir).OrderBy(Path.GetFileName))
            {
                if (filter.Length > 0 && !f.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                var name = Path.GetFileName(f);
                var ext = Path.GetExtension(f);
                if (!TextExtensions.Contains(ext) && name is not "Dockerfile" and not ".gitignore")
                    continue;

                parent.Nodes.Add(new TreeNode(name) { Tag = f });
            }
        }
        catch
        {
            // Ignore inaccessible paths.
        }
    }

    void OpenFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || Directory.Exists(path))
            return;

        if (openTabs.TryGetValue(path, out var existing))
        {
            editorTabs.SelectedTab = existing;
            return;
        }

        try
        {
            var text = File.ReadAllText(path);
            var box = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Text = text,
                Font = new Font("Consolas", 10),
                AcceptsTab = true,
                DetectUrls = false,
                WordWrap = false,
                Tag = false
            };

            box.TextChanged += (_, _) =>
            {
                box.Tag = true;
                status.Text = $"Modified: {Path.GetFileName(path)}";
                UpdateTabTitle(path);
            };

            var page = new TabPage(Path.GetFileName(path)) { Tag = path };
            page.Controls.Add(box);
            editorTabs.TabPages.Add(page);
            editorTabs.SelectedTab = page;
            openTabs[path] = page;
            status.Text = path;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Open file");
        }
    }

    void UpdateTabTitle(string path)
    {
        if (!openTabs.TryGetValue(path, out var page))
            return;
        var editor = page.Controls.OfType<RichTextBox>().FirstOrDefault();
        if (editor is null)
            return;
        page.Text = editor.Tag is true ? $"* {Path.GetFileName(path)}" : Path.GetFileName(path);
    }

    RichTextBox? CurrentEditor() =>
        editorTabs.SelectedTab?.Controls.OfType<RichTextBox>().FirstOrDefault();

    void SaveCurrent()
    {
        var page = editorTabs.SelectedTab;
        var path = page?.Tag as string;
        var editor = CurrentEditor();
        if (path is null || editor is null)
            return;
        SaveEditor(path, editor);
    }

    void SaveEditor(string path, RichTextBox editor)
    {
        if (editor.Tag is not true)
        {
            status.Text = $"Unchanged: {path}";
            return;
        }

        try
        {
            File.WriteAllText(path, editor.Text, new UTF8Encoding(false));
            editor.Tag = false;
            UpdateTabTitle(path);
            status.Text = $"Saved: {path}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Save");
        }
    }

    void SaveAll()
    {
        foreach (TabPage page in editorTabs.TabPages)
        {
            var path = page.Tag as string;
            var editor = page.Controls.OfType<RichTextBox>().FirstOrDefault();
            if (path is not null && editor is not null)
                SaveEditor(path, editor);
        }
        status.Text = "All modified files saved";
    }

    void OpenFileDialog()
    {
        using var d = new OpenFileDialog
        {
            Filter = "Source/Text files|*.ts;*.tsx;*.js;*.jsx;*.json;*.css;*.scss;*.html;*.htm;*.md;*.sql;*.cs;*.xml;*.yml;*.yaml;*.env;*.bat;*.cmd;*.ps1;*.sh;*.txt;*.npmrc|All files|*.*",
            Multiselect = false
        };
        if (d.ShowDialog(this) == DialogResult.OK)
            OpenFile(d.FileName);
    }

    void FindText()
    {
        var editor = CurrentEditor();
        if (editor is null)
            return;

        using var dialog = new Form
        {
            Text = "Find",
            Width = 420,
            Height = 130,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false
        };

        var box = new TextBox { Left = 12, Top = 12, Width = 380 };
        var find = new Button { Text = "Find Next", Left = 230, Top = 48, Width = 80 };
        var cancel = new Button { Text = "Cancel", Left = 318, Top = 48, Width = 74, DialogResult = DialogResult.Cancel };

        find.Click += (_, _) =>
        {
            if (string.IsNullOrEmpty(box.Text))
                return;

            var start = Math.Min(editor.SelectionStart + editor.SelectionLength + 1, editor.TextLength);
            var index = editor.Find(box.Text, start, RichTextBoxFinds.None);
            if (index < 0 && start > 0)
                index = editor.Find(box.Text, 0, RichTextBoxFinds.None);

            if (index >= 0)
                editor.Select(index, box.Text.Length);
            else
                MessageBox.Show(dialog, "Text not found.", "Find");
        };

        dialog.Controls.Add(box);
        dialog.Controls.Add(find);
        dialog.Controls.Add(cancel);
        dialog.AcceptButton = find;
        dialog.CancelButton = cancel;
        dialog.Shown += (_, _) => box.Focus();
        dialog.ShowDialog(this);
    }

    void RunNpm(string args, bool saveModified) => RunCommand("npm", args, saveModified);

    void RunCommand(string exe, string args, bool saveModified)
    {
        if (workspace is null)
        {
            MessageBox.Show("Open a workspace first.");
            return;
        }

        if (saveModified)
            SaveAll();

        output.Clear();
        status.Text = $"Running {exe} {args}";

        var psi = new ProcessStartInfo(exe, args)
        {
            WorkingDirectory = workspace,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) AppendOutput(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) AppendOutput(e.Data); };
        process.Exited += (_, _) =>
        {
            if (!IsDisposed)
                BeginInvoke(() => status.Text = $"{exe} exited with code {process.ExitCode}");
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            AppendOutput(ex.ToString());
            status.Text = $"{exe} failed to start";
            process.Dispose();
        }
    }

    void AppendOutput(string line)
    {
        if (IsDisposed)
            return;
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendOutput(line));
            return;
        }
        output.AppendText(line + Environment.NewLine);
    }

    void OpenFolder(string relative)
    {
        if (workspace is null)
            return;
        var path = Path.Combine(workspace, relative);
        if (Directory.Exists(path))
            Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
    }

    void OpenRelative(string relative) =>
        OpenFile(workspace is null ? null : Path.Combine(workspace, relative));

    void OpenTerminal()
    {
        if (workspace is null)
            return;
        Process.Start(new ProcessStartInfo("cmd.exe")
        {
            WorkingDirectory = workspace,
            UseShellExecute = true
        });
    }

    void ShowWorkspaceInfo()
    {
        if (workspace is null)
        {
            MessageBox.Show("No workspace selected.");
            return;
        }

        var dirs = new[] { "src", "server", "shared", "database", "tools", ".github" };
        var sb = new StringBuilder($"Workspace: {workspace}\n\n");
        foreach (var d in dirs)
            sb.AppendLine($"{d}: {Directory.Exists(Path.Combine(workspace, d))}");

        MessageBox.Show(sb.ToString(), "Workspace information");
    }

    void DrawTab(object? sender, DrawItemEventArgs e)
    {
        var page = editorTabs.TabPages[e.Index];
        e.Graphics.DrawString(page.Text, Font, SystemBrushes.ControlText, e.Bounds.Left + 8, e.Bounds.Top + 4);
    }
}
