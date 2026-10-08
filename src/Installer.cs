using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

internal static class Installer {
 static byte[] Payload() { using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("NivalisPayload.zip")) { if(s==null)throw new Exception("Brak osadzonej paczki.");using(var m=new MemoryStream()){s.CopyTo(m);return m.ToArray();} } }
 static string Hash(byte[] bytes) {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}
 static byte[] ReadEntry(ZipArchiveEntry e) {using(var s=e.Open())using(var m=new MemoryStream()){s.CopyTo(m);return m.ToArray();}}
 static string Name(ZipArchiveEntry e) {return e.FullName.Replace('\\','/');}
 public static Dictionary<string,object> Verify(byte[] bytes) {
  using(var m=new MemoryStream(bytes))using(var z=new ZipArchive(m,ZipArchiveMode.Read)) {
   string prefix="Nivalis_Nights_PL/";
   if(z.Entries.Select(Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=z.Entries.Count)throw new Exception("Powtórzone nazwy plików w paczce.");
   foreach(string file in new[]{"spolszczenie.ps1","engine.ps1","manifest.json","tools/AssetsTools.NET.dll","tools/classdata.tpk"})if(!z.Entries.Any(e=>Name(e)==prefix+file))throw new Exception("Niepełna paczka: "+file);
   var entry=z.Entries.Single(e=>Name(e)==prefix+"manifest.json");
   var j=new JavaScriptSerializer();var manifest=(Dictionary<string,object>)j.DeserializeObject(Encoding.UTF8.GetString(ReadEntry(entry)).TrimStart('\ufeff'));
   foreach(var pair in (Dictionary<string,object>)manifest["translation_hashes"]){var data=ReadEntry(z.Entries.Single(e=>Name(e)==prefix+"localization/"+pair.Key));if(Hash(data)!=(string)pair.Value)throw new Exception("Uszkodzone tłumaczenie: "+pair.Key);}
   return new Dictionary<string,object>{{"status","OK"},{"files",z.Entries.Count},{"payload_bytes",bytes.Length},{"payload_sha256",Hash(bytes)},{"release",manifest["release"]}};
  }
 }
 public static string Extract(byte[] bytes) {
  Verify(bytes);
  string cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NivalisPL","cache",Hash(bytes).Substring(0,16));
  Directory.CreateDirectory(cache);
  using(var m=new MemoryStream(bytes))using(var z=new ZipArchive(m,ZipArchiveMode.Read))foreach(var entry in z.Entries) {
   string relative=Name(entry);if(relative.EndsWith("/"))continue;
   string path=Path.GetFullPath(Path.Combine(cache,relative.Replace('/',Path.DirectorySeparatorChar)));
   if(!path.StartsWith(cache+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Nieprawidłowa ścieżka w paczce.");
   Directory.CreateDirectory(Path.GetDirectoryName(path));using(var source=entry.Open())using(var output=File.Create(path))source.CopyTo(output);
  }
  return Path.Combine(cache,"Nivalis_Nights_PL","spolszczenie.ps1");
 }
 [STAThread] public static int Main(string[] args) {
  try {
   byte[] payload=Payload();
   if(args.Length>0&&args[0]=="--verify"){var report=Verify(payload);if(args.Length>1)File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(report),new UTF8Encoding(false));return 0;}
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   using(var form=new InstallerWindow(payload)) {
    if(args.Length>1&&args[0]=="--preview") {form.Show();Application.DoEvents();form.PerformLayout();using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);}form.Hide();return 0;}
    using(var mutex=new Mutex(false,"Local\\NivalisPLInstaller")) {
     if(!mutex.WaitOne(0)){MessageBox.Show("Instalator jest już uruchomiony.","Nivalis Nights");return 1;}
     try{Application.Run(form);}finally{mutex.ReleaseMutex();}
    }
   }
   return 0;
  } catch(Exception e) {if(args.Length==0)MessageBox.Show(e.Message,"Nivalis Nights",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
 }
}

internal sealed class InstallerWindow : Form {
 readonly byte[] payload;
 readonly TextBox path=new TextBox(),log=new TextBox();
 readonly Button install=new Button(),uninstall=new Button(),browse=new Button();
 readonly Label status=new Label();
 bool busy;
 public InstallerWindow(byte[] data) {
  Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
  payload=data;Text="Nivalis Nights - spolszczenie";ClientSize=new Size(680,430);MinimumSize=new Size(660,440);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",9);AutoScaleMode=AutoScaleMode.Font;
  var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=1,RowCount=6};
  layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  foreach(int height in new[]{46,24,40,54,24})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,height));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  var title=new Label {Text="Nivalis Nights",Font=new Font("Segoe UI",20,FontStyle.Bold),Dock=DockStyle.Fill,AutoSize=false};layout.Controls.Add(title,0,0);
  layout.Controls.Add(new Label {Text="Folder gry",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,1);
  var folder=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=new Padding(0)};folder.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));folder.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,40));
  path.Dock=DockStyle.Fill;path.Margin=new Padding(0,6,8,0);browse.Text="...";browse.Dock=DockStyle.Fill;browse.Margin=new Padding(0,2,0,4);new ToolTip().SetToolTip(browse,"Wybierz folder gry");folder.Controls.Add(path,0,0);folder.Controls.Add(browse,1,0);layout.Controls.Add(folder,0,2);
  foreach(string candidate in new[]{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"I:\\Gry\\Nivalis Nights","C:\\GOG Games\\Nivalis Nights","C:\\Program Files (x86)\\Steam\\steamapps\\common\\Nivalis Nights"})if(File.Exists(Path.Combine(candidate,"Nivalis Nights.exe"))){path.Text=candidate;break;}
  var commands=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false,Margin=new Padding(0),Padding=new Padding(0,8,0,0)};
  install.Text="Zainstaluj spolszczenie";uninstall.Text="Odinstaluj spolszczenie";foreach(var button in new[]{install,uninstall}){button.Size=new Size(208,34);button.Margin=new Padding(0,0,12,0);commands.Controls.Add(button);}layout.Controls.Add(commands,0,3);
  status.Text="Gotowe";status.Dock=DockStyle.Fill;status.TextAlign=ContentAlignment.MiddleLeft;layout.Controls.Add(status,0,4);
  log.Multiline=true;log.ReadOnly=true;log.ScrollBars=ScrollBars.Both;log.WordWrap=false;log.Dock=DockStyle.Fill;log.BackColor=SystemColors.Window;log.Font=new Font("Consolas",9);layout.Controls.Add(log,0,5);Controls.Add(layout);
  browse.Click+=delegate{using(var dialog=new FolderBrowserDialog {Description="Folder zawierający Nivalis Nights.exe",ShowNewFolderButton=false,SelectedPath=path.Text})if(dialog.ShowDialog(this)==DialogResult.OK)path.Text=dialog.SelectedPath;};
  install.Click+=delegate{Run("Install");};uninstall.Click+=delegate{Run("Uninstall");};
  FormClosing+=delegate(object sender,FormClosingEventArgs e){if(busy){e.Cancel=true;status.Text="Poczekaj na zakończenie operacji.";}};
 }
 void Append(string text) {if(text==null||IsDisposed)return;BeginInvoke((Action)delegate{log.AppendText(text+Environment.NewLine);});}
 static string Quote(string value){if(value.IndexOf('"')>=0)throw new Exception("Nieprawidłowa ścieżka.");return "\""+value+(value.EndsWith("\\")?"\\":"")+"\"";}
 void Run(string action) {
  string game=path.Text.Trim();if(!File.Exists(Path.Combine(game,"Nivalis Nights.exe"))){MessageBox.Show(this,"Wybierz folder zawierający Nivalis Nights.exe.",Text,MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
  busy=true;install.Enabled=uninstall.Enabled=browse.Enabled=path.Enabled=false;status.Text=action=="Install"?"Instalowanie...":"Odinstalowywanie...";log.Clear();
  ThreadPool.QueueUserWorkItem(delegate{
   int code=1;
   try {
    string script=Installer.Extract(payload);
    var info=new ProcessStartInfo {FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe"),Arguments="-NoProfile -ExecutionPolicy Bypass -File "+Quote(script)+" -Action "+action+" -GameDir "+Quote(game),UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
    using(var process=new Process {StartInfo=info}){process.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs e){Append(e.Data);};process.ErrorDataReceived+=delegate(object sender,DataReceivedEventArgs e){Append(e.Data);};process.Start();process.BeginOutputReadLine();process.BeginErrorReadLine();process.WaitForExit();code=process.ExitCode;}
   } catch(Exception e){Append(e.Message);}
   BeginInvoke((Action)delegate{busy=false;install.Enabled=uninstall.Enabled=browse.Enabled=path.Enabled=true;status.Text=code==0?(action=="Install"?"Spolszczenie zainstalowane":"Spolszczenie odinstalowane"):"Operacja nie powiodła się. Szczegóły poniżej.";});
  });
 }
}
