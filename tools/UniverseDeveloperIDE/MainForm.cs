using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace UniverseDeveloperIDE;

public sealed class MainForm : Form
{
    private readonly TreeView tree = new();
    private readonly TabControl editorTabs = new();
    private readonly RichTextBox output = new();
    private readonly ToolStripStatusLabel status = new();
    private readonly TextBox search = new();
    private string? workspace;
    private readonly Dictionary<string, TabPage> openTabs = new(StringComparer.OrdinalIgnoreCase);

    static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ts",".tsx",".js",".jsx",".json",".css",".scss",".html",".htm",".md",".sql",".cs",
        ".xml",".yml",".yaml",".env",".bat",".cmd",".ps1",".sh",".txt",".gitignore",".npmrc"
    };

    public MainForm()
    {
        Text = "Universe Developer IDE";
        Width = 1500; Height = 950;
        StartPosition = FormStartPosition.CenterScreen;
        BuildUi();
        BuildMenus();
        Shown += (_, _) => OpenWorkspaceDialog();
    }

    void BuildUi()
    {
        var split = new SplitContainer { Dock=DockStyle.Fill, SplitterDistance=320 };
        var left = new Panel { Dock=DockStyle.Fill };
        search.PlaceholderText = "Filter files...";
        search.Dock = DockStyle.Top;
        search.Height = 32;
        search.TextChanged += (_, _) => RefreshTree();
        left.Controls.Add(tree); left.Controls.Add(search);
        tree.Dock = DockStyle.Fill;
        tree.HideSelection = false;
        tree.AfterSelect += (_, e) => OpenFile(e.Node.Tag as string);
        tree.NodeMouseDoubleClick += (_, e) => OpenFile(e.Node.Tag as string);
        split.Panel1.Controls.Add(left);

        var right = new SplitContainer { Dock=DockStyle.Fill, Orientation=Orientation.Horizontal, SplitterDistance=650 };
        editorTabs.Dock=DockStyle.Fill;
        editorTabs.DrawMode=TabDrawMode.OwnerDrawFixed;
        editorTabs.DrawItem += DrawTab;
        right.Panel1.Controls.Add(editorTabs);
        output.Dock=DockStyle.Fill;
        output.Font=new Font("Consolas",10);
        output.BackColor=Color.FromArgb(12,16,22);
        output.ForeColor=Color.Gainsboro;
        output.ReadOnly=true;
        right.Panel2.Controls.Add(output);
        split.Panel2.Controls.Add(right);
        Controls.Add(split);

        var bar = new StatusStrip();
        status.Text="Ready";
        bar.Items.Add(status);
        Controls.Add(bar);
        MainMenuStrip = BuildMenus();
    }

    MenuStrip BuildMenus()
    {
        var menu = new MenuStrip();

        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add("Open Workspace...", null, (_,_)=>OpenWorkspaceDialog());
        file.DropDownItems.Add("Open File...", null, (_,_)=>OpenFileDialog());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Save", null, (_,_)=>SaveCurrent());
        file.DropDownItems.Add("Save All", null, (_,_)=>SaveAll());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Exit", null, (_,_)=>Close());

        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.Add("Undo", null, (_,_)=>CurrentEditor()?.Undo());
        edit.DropDownItems.Add("Redo", null, (_,_)=>CurrentEditor()?.Redo());
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add("Find", null, (_,_)=>FindText());

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add("Refresh Explorer", null, (_,_)=>RefreshTree());
        view.DropDownItems.Add("Output / Terminal", null, (_,_)=>output.Focus());

        var frontend = new ToolStripMenuItem("&Frontend");
        frontend.DropDownItems.Add("Install dependencies", null, (_,_)=>RunNpm("install"));
        frontend.DropDownItems.Add("Development server", null, (_,_)=>RunNpm("run dev"));
        frontend.DropDownItems.Add("Build frontend", null, (_,_)=>RunNpm("run build"));
        frontend.DropDownItems.Add("Lint / TypeScript", null, (_,_)=>RunNpm("run lint"));
        frontend.DropDownItems.Add("Test dependency", null, (_,_)=>RunNpm("run test:dependency"));

        var backend = new ToolStripMenuItem("&Backend");
        backend.DropDownItems.Add("Build server", null, (_,_)=>RunNpm("run server:build"));
        backend.DropDownItems.Add("Start server", null, (_,_)=>RunNpm("run server:start"));
        backend.DropDownItems.Add("Development server", null, (_,_)=>RunNpm("run server:dev"));
        backend.DropDownItems.Add("Build + start", null, (_,_)=>RunNpm("run server:compile-run"));
        backend.DropDownItems.Add(new ToolStripSeparator());
        backend.DropDownItems.Add("Open server folder", null, (_,_)=>OpenFolder("server"));

        var database = new ToolStripMenuItem("&Database");
        database.DropDownItems.Add("Open database folder", null, (_,_)=>OpenFolder("database"));
        database.DropDownItems.Add("Open schema.sql", null, (_,_)=>OpenRelative("database/schema.sql"));
        database.DropDownItems.Add("Open migrations", null, (_,_)=>OpenFolder("database/migrations"));

        var tools = new ToolStripMenuItem("&Tools");
        tools.DropDownItems.Add("Generate Admin Terminal EXE", null, (_,_)=>RunCommand("dotnet", "publish tools/AdminSystemsTerminal/AdminSystemsTerminal.csproj -c Release"));
        tools.DropDownItems.Add("Generate Developer IDE EXE", null, (_,_)=>RunCommand("dotnet", "publish tools/UniverseDeveloperIDE/UniverseDeveloperIDE.csproj -c Release"));
        tools.DropDownItems.Add("Git status", null, (_,_)=>RunCommand("git","status --short"));
        tools.DropDownItems.Add("Git log", null, (_,_)=>RunCommand("git","log -10 --oneline"));
        tools.DropDownItems.Add("Open terminal here", null, (_,_)=>OpenTerminal());

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add("Workspace information", null, (_,_)=>ShowWorkspaceInfo());
        help.DropDownItems.Add("About", null, (_,_)=>MessageBox.Show("Universe Developer IDE\nBackend + frontend workspace editor for the Universe Civilization Empire at War project.\n\n.NET 8 / WinForms", "About"));

        menu.Items.AddRange(new ToolStripItem[]{file,edit,view,frontend,backend,database,tools,help});
        Controls.Add(menu);
        menu.Dock=DockStyle.Top;
        return menu;
    }

    void OpenWorkspaceDialog()
    {
        using var d=new FolderBrowserDialog { Description="Select the project/workspace root folder" };
        if (d.ShowDialog(this)==DialogResult.OK) { workspace=d.SelectedPath; Text=$"Universe Developer IDE — {workspace}"; RefreshTree(); }
    }

    void RefreshTree()
    {
        tree.BeginUpdate(); tree.Nodes.Clear();
        if (string.IsNullOrWhiteSpace(workspace) || !Directory.Exists(workspace)) { tree.EndUpdate(); return; }
        var root=new TreeNode(Path.GetFileName(workspace)) { Tag=workspace };
        tree.Nodes.Add(root);
        AddDirectoryNodes(root,workspace);
        root.Expand();
        tree.EndUpdate();
    }

    void AddDirectoryNodes(TreeNode parent,string dir)
    {
        try
        {
            foreach(var d in Directory.EnumerateDirectories(dir).OrderBy(Path.GetFileName))
            {
                if (Path.GetFileName(d) is ".git" or "node_modules" or "dist" or "dist-server" or "bin" or "obj") continue;
                var dn=new TreeNode(Path.GetFileName(d)) { Tag=d };
                parent.Nodes.Add(dn); AddDirectoryNodes(dn,d);
            }
            var filter=search.Text.Trim();
            foreach(var f in Directory.EnumerateFiles(dir).OrderBy(Path.GetFileName))
            {
                if (filter.Length>0 && !f.Contains(filter,StringComparison.OrdinalIgnoreCase)) continue;
                var ext=Path.GetExtension(f);
                if (!TextExtensions.Contains(ext) && Path.GetFileName(f)!="Dockerfile" && Path.GetFileName(f)!=".gitignore") continue;
                parent.Nodes.Add(new TreeNode(Path.GetFileName(f)) { Tag=f });
            }
        } catch { }
    }

    void OpenFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || Directory.Exists(path)) return;
        if (openTabs.TryGetValue(path,out var existing)) { editorTabs.SelectedTab=existing; return; }
        try
        {
            var text=File.ReadAllText(path);
            var box=new RichTextBox { Dock=DockStyle.Fill, Text=text, Font=new Font("Consolas",10), AcceptsTab=true, DetectUrls=false, WordWrap=false };
            box.TextChanged += (_,_)=>status.Text=$"Modified: {Path.GetFileName(path)}";
            var page=new TabPage(Path.GetFileName(path)) { Tag=path };
            page.Controls.Add(box);
            editorTabs.TabPages.Add(page); editorTabs.SelectedTab=page; openTabs[path]=page;
            status.Text=path;
        }
        catch(Exception ex){ MessageBox.Show(ex.Message,"Open file"); }
    }

    RichTextBox? CurrentEditor()=>editorTabs.SelectedTab?.Controls.OfType<RichTextBox>().FirstOrDefault();

    void SaveCurrent()
    {
        var p=editorTabs.SelectedTab?.Tag as string; var e=CurrentEditor();
        if(p is null || e is null) return;
        try { File.WriteAllText(p,e.Text,new UTF8Encoding(false)); status.Text=$"Saved: {p}"; }
        catch(Exception ex){ MessageBox.Show(ex.Message,"Save"); }
    }

    void SaveAll()
    {
        foreach(TabPage p in editorTabs.TabPages)
        {
            var path=p.Tag as string; var e=p.Controls.OfType<RichTextBox>().FirstOrDefault();
            if(path!=null && e!=null) try { File.WriteAllText(path,e.Text,new UTF8Encoding(false)); } catch { }
        }
        status.Text="All files saved";
    }

    void OpenFileDialog()
    {
        using var d=new OpenFileDialog { Filter="Source/Text files|*.ts;*.tsx;*.js;*.jsx;*.json;*.css;*.scss;*.html;*.md;*.sql;*.cs;*.xml;*.yml;*.yaml;*.env;*.bat;*.ps1;*.sh;*.txt|All files|*.*", Multiselect=false };
        if(d.ShowDialog(this)==DialogResult.OK) OpenFile(d.FileName);
    }

    void FindText()
    {
        var e=CurrentEditor(); if(e==null)return;
        var q=Microsoft.VisualBasic.Interaction.InputBox("Find text:","Find");
        if(string.IsNullOrEmpty(q))return;
        var i=e.Find(q,Math.Min(e.SelectionStart+1,e.TextLength),RichTextBoxFinds.None);
        if(i>=0)e.Select(i,q.Length);
    }

    void RunNpm(string args)=>RunCommand("npm",args);
    void RunCommand(string exe,string args)
    {
        if(workspace==null){MessageBox.Show("Open a workspace first.");return;}
        SaveAll(); output.Clear(); status.Text=$"Running {exe} {args}";
        var psi=new ProcessStartInfo(exe,args){WorkingDirectory=workspace,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
        var p=new Process{StartInfo=psi,EnableRaisingEvents=true};
        p.OutputDataReceived+=(s,e)=>{if(e.Data!=null) AppendOutput(e.Data);};
        p.ErrorDataReceived+=(s,e)=>{if(e.Data!=null) AppendOutput(e.Data);};
        p.Exited+=(s,e)=>Invoke(()=>status.Text=$"{exe} exited with code {p.ExitCode}");
        try{p.Start();p.BeginOutputReadLine();p.BeginErrorReadLine();}catch(Exception ex){AppendOutput(ex.ToString());}
    }

    void AppendOutput(string line)
    {
        if(IsDisposed)return;
        if(InvokeRequired){Invoke(()=>AppendOutput(line));return;}
        output.AppendText(line+Environment.NewLine);
    }

    void OpenFolder(string relative)
    {
        if(workspace==null)return;
        var p=Path.Combine(workspace,relative);
        if(Directory.Exists(p)) Process.Start(new ProcessStartInfo("explorer.exe",p){UseShellExecute=true});
    }

    void OpenRelative(string relative)=>OpenFile(workspace==null?null:Path.Combine(workspace,relative));

    void OpenTerminal()
    {
        if(workspace==null)return;
        Process.Start(new ProcessStartInfo("cmd.exe"){WorkingDirectory=workspace,UseShellExecute=true});
    }

    void ShowWorkspaceInfo()
    {
        if(workspace==null){MessageBox.Show("No workspace selected.");return;}
        var dirs=new[]{"src","server","shared","database","tools",".github"};
        var sb=new StringBuilder($"Workspace: {workspace}\n\n");
        foreach(var d in dirs) sb.AppendLine($"{d}: {Directory.Exists(Path.Combine(workspace,d))}");
        MessageBox.Show(sb.ToString(),"Workspace information");
    }

    void DrawTab(object? sender,DrawItemEventArgs e)
    {
        var page=editorTabs.TabPages[e.Index];
        e.Graphics.DrawString(page.Text,Font,SystemBrushes.ControlText,e.Bounds.Left+8,e.Bounds.Top+4);
    }
}
