using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

static class App
{
    static string ffmpeg;

    static string ExtractFfmpeg()
    {
        var asm = Assembly.GetExecutingAssembly();
        var resName = "ffmpeg.exe";
        string dir = Path.Combine(Path.GetTempPath(), "avif-to-mp4");
        Directory.CreateDirectory(dir);
        string target = Path.Combine(dir, "ffmpeg.exe");
        using (var stream = asm.GetManifestResourceStream(resName))
        {
            if (stream == null)
                throw new Exception("Embedded ffmpeg.exe resource not found.");
            if (File.Exists(target) && new FileInfo(target).Length == stream.Length)
                return target;
            using (var fs = File.Create(target)) stream.CopyTo(fs);
        }
        return target;
    }

    static ListBox list;
    static NumericUpDown crf;
    static CheckBox lossless;
    static TextBox log;
    static Button convertBtn;
    static TextBox outFolderBox;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try { ffmpeg = ExtractFfmpeg(); }
        catch (Exception ex) {
            MessageBox.Show("Failed to prepare bundled ffmpeg:\n" + ex.Message,
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var form = new Form {
            Text = "AVIF to MP4 Converter",
            Size = new Size(640, 600),
            StartPosition = FormStartPosition.CenterScreen,
            AllowDrop = true
        };

        var banner = new Label {
            Text = "Naber Sedoo! ~(^_^)~",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(220, 30),
            Location = new Point(10, 10)
        };
        form.Controls.Add(banner);

        int bx = 10, bdir = 2;
        int maxX = 625 - banner.Width;
        var rainbow = new Color[] {
            Color.FromArgb(230, 60, 60),  Color.FromArgb(230, 140, 40),
            Color.FromArgb(230, 200, 40), Color.FromArgb(60, 180, 80),
            Color.FromArgb(60, 140, 220), Color.FromArgb(140, 80, 200)
        };
        int colorIdx = 0;
        var animTimer = new Timer { Interval = 40 };
        animTimer.Tick += (s, e) => {
            bx += bdir;
            if (bx < 10) { bx = 10; bdir = -bdir; }
            if (bx > maxX) { bx = maxX; bdir = -bdir; }
            banner.Left = bx;
            colorIdx = (colorIdx + 1) % (rainbow.Length * 8);
            banner.ForeColor = rainbow[(colorIdx / 8) % rainbow.Length];
        };
        animTimer.Start();

        list = new ListBox {
            Location = new Point(10, 50),
            Size = new Size(605, 200),
            SelectionMode = SelectionMode.MultiExtended,
            AllowDrop = true
        };
        form.Controls.Add(list);

        var btnAdd = new Button { Text = "Add files...", Location = new Point(10, 260), Size = new Size(100, 28) };
        btnAdd.Click += (s, e) => {
            using (var dlg = new OpenFileDialog { Filter = "AVIF files (*.avif)|*.avif|All files (*.*)|*.*", Multiselect = true }) {
                if (dlg.ShowDialog() == DialogResult.OK) AddFiles(dlg.FileNames);
            }
        };
        form.Controls.Add(btnAdd);

        var btnRemove = new Button { Text = "Remove selected", Location = new Point(115, 260), Size = new Size(120, 28) };
        btnRemove.Click += (s, e) => {
            var sel = new List<object>();
            foreach (var it in list.SelectedItems) sel.Add(it);
            foreach (var it in sel) list.Items.Remove(it);
        };
        form.Controls.Add(btnRemove);

        var btnClear = new Button { Text = "Clear", Location = new Point(240, 260), Size = new Size(70, 28) };
        btnClear.Click += (s, e) => list.Items.Clear();
        form.Controls.Add(btnClear);

        var lblQ = new Label { Text = "Quality (CRF, lower = better):", Location = new Point(330, 265), AutoSize = true };
        form.Controls.Add(lblQ);

        crf = new NumericUpDown { Location = new Point(500, 262), Size = new Size(50, 24), Minimum = 0, Maximum = 51, Value = 15 };
        form.Controls.Add(crf);

        var lblOut = new Label { Text = "Output:", Location = new Point(10, 298), AutoSize = true };
        form.Controls.Add(lblOut);

        outFolderBox = new TextBox {
            Text = "(same folder as source)",
            Location = new Point(65, 295), Size = new Size(430, 24),
            ReadOnly = true, ForeColor = Color.Gray
        };
        form.Controls.Add(outFolderBox);

        var btnBrowse = new Button { Text = "Browse...", Location = new Point(500, 293), Size = new Size(75, 28) };
        btnBrowse.Click += (s, e) => {
            using (var dlg = new FolderBrowserDialog { Description = "Pick output folder for MP4s" }) {
                if (dlg.ShowDialog() == DialogResult.OK) {
                    outFolderBox.Text = dlg.SelectedPath;
                    outFolderBox.ForeColor = Color.Black;
                }
            }
        };
        form.Controls.Add(btnBrowse);

        var btnResetOut = new Button { Text = "X", Location = new Point(580, 293), Size = new Size(35, 28) };
        btnResetOut.Click += (s, e) => {
            outFolderBox.Text = "(same folder as source)";
            outFolderBox.ForeColor = Color.Gray;
        };
        form.Controls.Add(btnResetOut);

        lossless = new CheckBox {
            Text = "Max quality (near-lossless, big files, plays everywhere)",
            Location = new Point(10, 330), AutoSize = true
        };
        form.Controls.Add(lossless);

        convertBtn = new Button {
            Text = "Convert all to MP4",
            Location = new Point(10, 358), Size = new Size(605, 34),
            BackColor = Color.FromArgb(60, 140, 220),
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat
        };
        convertBtn.Click += (s, e) => RunConversion();
        form.Controls.Add(convertBtn);

        log = new TextBox {
            Location = new Point(10, 400), Size = new Size(605, 150),
            Multiline = true, ScrollBars = ScrollBars.Vertical,
            ReadOnly = true, Font = new Font("Consolas", 9)
        };
        form.Controls.Add(log);

        DragEventHandler dragEnter = (s, e) => {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        };
        DragEventHandler dragDrop = (s, e) => {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddFiles(files);
        };
        form.DragEnter += dragEnter; form.DragDrop += dragDrop;
        list.DragEnter += dragEnter; list.DragDrop += dragDrop;

        Application.Run(form);
    }

    static void AddFiles(IEnumerable<string> paths)
    {
        foreach (var p in paths)
            if (File.Exists(p) && p.EndsWith(".avif", StringComparison.OrdinalIgnoreCase) && !list.Items.Contains(p))
                list.Items.Add(p);
    }

    static void WriteLog(string msg)
    {
        log.AppendText(msg + "\r\n");
        Application.DoEvents();
    }

    static string Quote(string s) { return "\"" + s.Replace("\"", "\\\"") + "\""; }

    static void RunConversion()
    {
        if (list.Items.Count == 0) {
            MessageBox.Show("Add some .avif files first.", "Nothing to do");
            return;
        }
        convertBtn.Enabled = false;
        int ok = 0, fail = 0;
        string customOut = (outFolderBox.ForeColor == Color.Gray) ? null : outFolderBox.Text;
        if (customOut != null && !Directory.Exists(customOut)) {
            try { Directory.CreateDirectory(customOut); }
            catch (Exception ex) {
                MessageBox.Show("Can't create output folder:\n" + ex.Message, "Error");
                convertBtn.Enabled = true; return;
            }
        }
        foreach (var item in list.Items)
        {
            string input = item.ToString();
            string output = customOut != null
                ? Path.Combine(customOut, Path.GetFileNameWithoutExtension(input) + ".mp4")
                : Path.ChangeExtension(input, ".mp4");
            WriteLog("Converting: " + Path.GetFileName(input));

            var probe = RunProcess(ffmpeg, "-hide_banner -i " + Quote(input));
            int videoStreams = Regex.Matches(probe, @"Stream #0:\d+.*?Video:").Count;
            string mapArg = videoStreams > 1 ? "-map 0:v:1" : "-map 0:v:0";

            string qualityArgs = lossless.Checked
                ? "-c:v libx264 -preset veryslow -crf 12 -profile:v high -pix_fmt yuv420p"
                : "-c:v libx264 -preset slow -crf " + (int)crf.Value + " -pix_fmt yuv420p";

            string args = "-y -i " + Quote(input) + " " + mapArg +
                          " -movflags +faststart -vf scale=trunc(iw/2)*2:trunc(ih/2)*2 " +
                          qualityArgs + " " + Quote(output);

            int exit;
            string err = RunProcessExit(ffmpeg, args, out exit);
            if (exit == 0) {
                WriteLog("  OK -> " + Path.GetFileName(output));
                ok++;
            } else {
                var lines = err.Split('\n');
                var tail = new List<string>();
                for (int i = lines.Length - 1; i >= 0 && tail.Count < 5; i--) {
                    var l = lines[i].Trim();
                    if (l.Length > 0) tail.Insert(0, l);
                }
                WriteLog("  FAILED (exit " + exit + "): " + string.Join(" | ", tail));
                fail++;
            }
        }
        WriteLog(string.Format("Done. {0} succeeded, {1} failed.", ok, fail));
        convertBtn.Enabled = true;
    }

    static string RunProcess(string exe, string args)
    {
        var psi = new ProcessStartInfo(exe, args) {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardError = true, RedirectStandardOutput = true,
            StandardErrorEncoding = Encoding.UTF8, StandardOutputEncoding = Encoding.UTF8
        };
        using (var p = Process.Start(psi)) {
            string err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return err;
        }
    }

    static string RunProcessExit(string exe, string args, out int exitCode)
    {
        var psi = new ProcessStartInfo(exe, args) {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardError = true, RedirectStandardOutput = true,
            StandardErrorEncoding = Encoding.UTF8, StandardOutputEncoding = Encoding.UTF8
        };
        using (var p = Process.Start(psi)) {
            string err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            exitCode = p.ExitCode;
            return err;
        }
    }
}
