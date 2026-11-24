using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

[assembly: AssemblyTitle("Lilt Pocket English")]
[assembly: AssemblyDescription("A quiet, English-first vocabulary companion for Windows.")]
[assembly: AssemblyProduct("Lilt Pocket English")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: AssemblyInformationalVersion("1.1.0")]

namespace PocketEnglish
{
 public class Card
 {
  public string Id { get; set; }
  public string Category { get; set; }
  public string Term { get; set; }
  public string Ipa { get; set; }
  public string BritishIpa { get; set; }
  public string Stress { get; set; }
  public string Definition { get; set; }
  public string Example { get; set; }
  public string MeaningZh { get; set; }
  public string ExampleZh { get; set; }
  public string Collocation { get; set; }
  public string PartOfSpeech { get; set; }
 }
 public class Entry
 {
  public int Meaning { get; set; }
  public int Sound { get; set; }
  public int Reviews { get; set; }
  public int GoodStreak { get; set; }
  public string LastRating { get; set; }
  public string DueUtc { get; set; }
  public string LastUtc { get; set; }
 }
 public class Review
 {
  public string CardId { get; set; }
  public string Rating { get; set; }
  public string Day { get; set; }
  public string AtUtc { get; set; }
 }
 public class AppState
 {
  public int Version { get; set; }
  public string CurrentId { get; set; }
  public bool ChineseVisible { get; set; }
  public bool WordRevealed { get; set; }
  public bool IpaMode { get; set; }
  public bool Quiet { get; set; }
  public string PronunciationMode { get; set; }
  public string UiFontName { get; set; }
  public bool KeepOnTop { get; set; }
  public int NewPosition { get; set; }
  public double Left { get; set; }
  public double Top { get; set; }
  public Dictionary<string, Entry> Entries { get; set; }
  public List<Card> CustomCards { get; set; }
  public List<Review> History { get; set; }
  public AppState()
  {
   Version=1; Quiet=true; PronunciationMode="US"; UiFontName=""; Left=-1; Top=-1;
   Entries=new Dictionary<string, Entry>(); CustomCards=new List<Card>(); History=new List<Review>();
  }
 }
 public static class Engine
 {
  // Eight work, seven car and five news slots in every twenty new cards.
  public static readonly string[] Mix={"work","cars","news","work","cars","work","news","cars","work","cars","news","work","cars","work","news","cars","work","cars","news","work"};
  public static List<Card> BuiltIns()
  {
   var cards=new List<Card>();
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("vocabulary.tsv"))
   using(var reader=new StreamReader(stream,Encoding.UTF8))
   {
    reader.ReadLine(); string line;
    while((line=reader.ReadLine())!=null)
    {
     if(String.IsNullOrWhiteSpace(line)) continue;
     var p=line.Split('\t'); if(p.Length!=11) throw new InvalidDataException("Invalid vocabulary row: "+line);
     cards.Add(new Card{Id=p[0],Category=p[1],Term=p[2],Ipa=p[3],BritishIpa=p[10],Stress=p[4],Definition=p[5],Example=p[6],MeaningZh=p[7],ExampleZh=p[8],Collocation=p[9]});
    }
   }
   return cards;
  }
  public static DateTime Date(string value)
  {
   DateTime result;
   return DateTime.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out result) ? result.ToUniversalTime() : DateTime.MinValue;
  }
  public static string Day(DateTime utc) { return utc.ToUniversalTime().AddHours(8).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture); }
  public static Card Next(AppState s,List<Card> cards,DateTime now)
  {
   var due=cards.Where(c=>s.Entries.ContainsKey(c.Id) && Date(s.Entries[c.Id].DueUtc)<=now)
    .OrderBy(c=>Date(s.Entries[c.Id].DueUtc)).FirstOrDefault();
   if(due!=null) return due;
   for(int offset=0;offset<Mix.Length;offset++)
   {
    string cat=Mix[(s.NewPosition+offset)%Mix.Length];
    var card=cards.FirstOrDefault(c=>c.Category==cat && !s.Entries.ContainsKey(c.Id));
    if(card!=null) { s.NewPosition=(s.NewPosition+offset)%Mix.Length; return card; }
   }
   return null;
  }
  public static void Rate(AppState s,Card c,int rating,DateTime now)
  {
   Entry e;
   if(!s.Entries.TryGetValue(c.Id,out e)) { e=new Entry(); s.Entries.Add(c.Id,e); s.NewPosition=(s.NewPosition+1)%Mix.Length; }
   string label;
   if(rating==0) { e.Meaning=Math.Max(0,e.Meaning-1); e.GoodStreak=0; e.DueUtc=now.AddMinutes(10).ToString("o"); label="again"; }
   else if(rating==1) { e.Meaning=Math.Max(1,e.Meaning); e.Sound=0; e.GoodStreak=0; e.DueUtc=now.AddDays(1).ToString("o"); label="sound"; }
   else if(rating==2) { e.Meaning=Math.Min(3,e.Meaning+1); e.Sound=Math.Min(3,e.Sound+1); e.GoodStreak++; int[] days={1,3,7,14,30}; e.DueUtc=now.AddDays(days[Math.Min(e.GoodStreak-1,4)]).ToString("o"); label="good"; }
   else throw new ArgumentOutOfRangeException("rating");
   e.Reviews++; e.LastRating=label; e.LastUtc=now.ToString("o");
   s.History.Add(new Review{CardId=c.Id,Rating=label,AtUtc=now.ToString("o"),Day=Day(now)});
   if(s.History.Count>10000) s.History.RemoveRange(0,s.History.Count-10000);
   s.ChineseVisible=false; s.WordRevealed=false;
  }
 }
 public class StateStore
 {
  public string DirectoryPath { get; private set; }
  public string FilePath { get { return Path.Combine(DirectoryPath,"progress.json"); } }
  public string Warning { get; private set; }
  readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=8*1024*1024};
  public StateStore(string directory) { DirectoryPath=directory; Directory.CreateDirectory(directory); }
  public AppState Parse(string text)
  {
   var root=json.DeserializeObject(text) as Dictionary<string,object>;
   if(root==null || !new[]{"Version","Entries","History","CustomCards"}.All(k=>root.ContainsKey(k))) throw new InvalidDataException("Incomplete backup.");
   var state=json.Deserialize<AppState>(text);
   if(state==null || state.Version!=1 || state.Entries==null || state.History==null || state.CustomCards==null) throw new InvalidDataException("Unsupported or incomplete backup.");
   state.PronunciationMode=Phonetics.ValidMode(state.PronunciationMode);state.UiFontName=state.UiFontName??"";
   if(state.UiFontName.Length>180 || state.UiFontName.Any(Char.IsControl))throw new InvalidDataException("Invalid UI font setting.");
   if(state.NewPosition<0 || state.NewPosition>=20 || Double.IsNaN(state.Left) || Double.IsInfinity(state.Left) || Double.IsNaN(state.Top) || Double.IsInfinity(state.Top)) throw new InvalidDataException("Invalid settings.");
   var ids=new HashSet<string>(Engine.BuiltIns().Select(c=>c.Id));
   foreach(var card in state.CustomCards)
   {
    if(card==null || String.IsNullOrWhiteSpace(card.Id) || !card.Id.StartsWith("custom-") || !ids.Add(card.Id) || String.IsNullOrWhiteSpace(card.Term) || card.Term.Length>80 || String.IsNullOrWhiteSpace(card.Definition) || String.IsNullOrWhiteSpace(card.Example) || !Engine.Mix.Contains(card.Category)) throw new InvalidDataException("Invalid custom card.");
    if(new[]{card.Definition,card.Example,card.Ipa,card.BritishIpa,card.Stress,card.MeaningZh,card.ExampleZh,card.Collocation}.Any(t=>t!=null && t.Length>2000)) throw new InvalidDataException("Card text is too long.");
   }
   foreach(var pair in state.Entries)
   {
    var e=pair.Value;
    if(!ids.Contains(pair.Key) || e==null || e.Meaning<0 || e.Meaning>3 || e.Sound<0 || e.Sound>3 || e.Reviews<1 || e.GoodStreak<0 || Engine.Date(e.DueUtc)==DateTime.MinValue || Engine.Date(e.LastUtc)==DateTime.MinValue || !new[]{"again","sound","good"}.Contains(e.LastRating)) throw new InvalidDataException("Invalid review record.");
   }
   foreach(var review in state.History)
    if(review==null || !ids.Contains(review.CardId) || Engine.Date(review.AtUtc)==DateTime.MinValue || review.Day!=Engine.Day(Engine.Date(review.AtUtc)) || !new[]{"again","sound","good"}.Contains(review.Rating)) throw new InvalidDataException("Invalid history.");
   if(state.CurrentId!=null && !ids.Contains(state.CurrentId)) state.CurrentId=null;
   return state;
  }
  public AppState Load()
  {
   Warning=null;
   if(!File.Exists(FilePath) && !File.Exists(FilePath+".bak")) return new AppState();
   foreach(string path in new[]{FilePath,FilePath+".bak"})
   {
    if(!File.Exists(path)) continue;
    try
    {
     var result=Parse(File.ReadAllText(path,Encoding.UTF8));
     if(path.EndsWith(".bak"))
     {
      Warning="Recovered progress from the previous save.";
      if(File.Exists(FilePath)) File.Copy(FilePath,Path.Combine(DirectoryPath,"damaged-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff")+".json"),true);
      File.Copy(path,FilePath,true);
     }
     return result;
    }
    catch(Exception) { }
   }
   throw new InvalidDataException("Progress could not be read. Your files have been kept in:\n"+DirectoryPath+"\nRestore a valid backup before continuing.");
  }
  public void Save(AppState state)
  {
   string text=json.Serialize(state);
   string temp=FilePath+".tmp";
   byte[] bytes=new UTF8Encoding(false).GetBytes(text);
   using(var file=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) { file.Write(bytes,0,bytes.Length); file.Flush(true); }
   if(File.Exists(FilePath)) File.Replace(temp,FilePath,FilePath+".bak",true);
   else File.Move(temp,FilePath);
  }
  public void Export(AppState state,string path) { File.WriteAllText(path,json.Serialize(state),new UTF8Encoding(false)); }
 }
 public class LookupVisit
 {
  public string Word;
  public string Context;
  public WordEntry Entry;
  public double Scroll;
 }
 public class PocketApp
 {
  Window window;
  AppState state;
  StateStore store;
  List<Card> cards;
  string page="Learn";
  Forms.NotifyIcon tray;
  SpeechSynthesizer voice;
  string englishVoice;
  bool settingControls;
  EventWaitHandle activation;
  bool saveFailed;
  string notice;
  readonly WordLookup dictionary=new WordLookup();
  readonly List<LookupVisit> lookupPath=new List<LookupVisit>();
  string lookupOrigin="Learn";
  double originScroll;
  int lookupGeneration;
  T N<T>(string name) where T:class { return window.FindName(name) as T; }
  static Brush Color(string color) { return (Brush)new BrushConverter().ConvertFromString(color); }
  public PocketApp(string directory,EventWaitHandle signal)
  {
   activation=signal; store=new StateStore(directory); state=store.Load(); notice=store.Warning;
   cards=Engine.BuiltIns().Concat(state.CustomCards).ToList();
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("MainWindow.xaml")) window=(Window)XamlReader.Load(stream);
   Phonetics.Apply(N<SelectableText>("IpaText"));
   window.Topmost=state.KeepOnTop;
   if(state.Left>=SystemParameters.VirtualScreenLeft && state.Top>=SystemParameters.VirtualScreenTop && state.Left+400<=SystemParameters.VirtualScreenLeft+SystemParameters.VirtualScreenWidth && state.Top+500<=SystemParameters.VirtualScreenTop+SystemParameters.VirtualScreenHeight) { window.WindowStartupLocation=WindowStartupLocation.Manual; window.Left=state.Left; window.Top=state.Top; }
   var availableFonts=new List<string>{"Default"};availableFonts.AddRange(Fonts.SystemFontFamilies.Select(f=>f.Source).Distinct().OrderBy(n=>n));if(!String.IsNullOrWhiteSpace(state.UiFontName)&&!availableFonts.Contains(state.UiFontName))availableFonts.Add(state.UiFontName);N<ComboBox>("UiFontCombo").ItemsSource=availableFonts;
   SetupTray(); SetupVoice(); Wire();
   if(Current()==null) SelectNext();
   RenderLearn(); ShowPage("Learn"); Save();
   var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(400)};
   timer.Tick+=delegate { if(activation.WaitOne(0)) Restore(); };
   timer.Start();
   window.Closed+=delegate { lookupGeneration++; SelectableText.WordRequested=null; timer.Stop(); tray.Visible=false; tray.Dispose(); if(voice!=null) voice.Dispose(); };
  }
  public void Run(Application app) { app.Run(window); }
  Card Current() { return cards.FirstOrDefault(c=>c.Id==state.CurrentId); }
  void SelectNext() { var c=Engine.Next(state,cards,DateTime.UtcNow); state.CurrentId=c==null?null:c.Id; state.ChineseVisible=false; state.WordRevealed=false; }
  void SetupVoice()
  {
   try
   {
    voice=new SpeechSynthesizer();
    var installed=voice.GetInstalledVoices().FirstOrDefault(v=>v.Enabled && v.VoiceInfo.Culture.Name=="en-US") ?? voice.GetInstalledVoices().FirstOrDefault(v=>v.Enabled && v.VoiceInfo.Culture.TwoLetterISOLanguageName=="en");
    if(installed!=null) { englishVoice=installed.VoiceInfo.Name; voice.SelectVoice(englishVoice); voice.Rate=-1; }
   }
   catch { englishVoice=null; }
  }
  void SetupTray()
  {
   tray=new Forms.NotifyIcon();
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("suzuha.ico"))
   using(var icon=new Drawing.Icon(stream,new Drawing.Size(32,32)))
   {
    tray.Icon=(Drawing.Icon)icon.Clone();
   }
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("suzuha.png"))
    window.Icon=System.Windows.Media.Imaging.BitmapFrame.Create(stream,System.Windows.Media.Imaging.BitmapCreateOptions.None,System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
   tray.Text="Lilt · Pocket English";
   var menu=new Forms.ContextMenuStrip();
   menu.Items.Add("Open Lilt / 打开",null,delegate { window.Dispatcher.Invoke(new Action(Restore)); });
   menu.Items.Add("Save and exit / 保存并退出",null,delegate { window.Dispatcher.Invoke(new Action(()=>window.Close())); });
   tray.ContextMenuStrip=menu;
   tray.MouseClick+=delegate(object sender,Forms.MouseEventArgs e) { if(e.Button==Forms.MouseButtons.Left) window.Dispatcher.Invoke(new Action(Restore)); };
   tray.Visible=true;
  }
  void Restore() { window.Show(); window.WindowState=WindowState.Normal; window.Activate(); }
  bool Save()
  {
   try { store.Save(state); saveFailed=false; Status(); return true; }
   catch(Exception e) { saveFailed=true; N<SelectableText>("StatusText").Text="Save failed · 请导出备份后退出"; N<SelectableText>("StatusText").ToolTip=e.Message; return false; }
  }
  void Status()
  {
   if(saveFailed) return;
   N<SelectableText>("StatusText").Text=notice ?? ((state.Quiet?"Quiet mode":"Sound available")+" · saved locally");
  }
  void Wire()
  {
   SelectableText.WordRequested=OpenWord;
   N<Button>("LookupBack").Click+=delegate { BackFromWord(); };
   N<Button>("UsesButton").Click+=delegate { var c=Current(); if(c!=null)OpenWord(c.Term,c.Example); };
   N<Button>("CreditsButton").Click+=delegate
   {
    lookupOrigin=page;lookupPath.Clear();lookupGeneration++;
    var senses=new List<WordSense>();
    foreach(string source in new[]{"Wordset-license.txt","CMUdict-license.txt","DoulosSIL-license.txt","IPA-dict-license.txt"})using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(source))using(var reader=new StreamReader(stream,Encoding.UTF8))senses.Add(new WordSense{PartOfSpeech="License",Definition=reader.ReadToEnd()});
    var visit=new LookupVisit{Word="Dictionary credits",Entry=new WordEntry{Word="Dictionary credits",Source="Wordset / WordNet: github.com/wordset/wordset-dictionary\nCMUdict: github.com/cmusphinx/cmudict",Senses=senses}};lookupPath.Add(visit);ShowPage("Lookup");RenderWord(visit,false);
   };
   N<Border>("TitleBar").MouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e) { if(!(e.OriginalSource is Button)) { try { window.DragMove(); } catch(InvalidOperationException){} } };
   N<Button>("CloseButton").Click+=delegate { window.Close(); };
   N<Button>("MinimizeButton").Click+=delegate { state.Left=window.Left; state.Top=window.Top; Save(); if(voice!=null) voice.SpeakAsyncCancelAll(); window.Hide(); };
   window.StateChanged+=delegate { if(window.WindowState==WindowState.Minimized) { Save(); if(voice!=null) voice.SpeakAsyncCancelAll(); window.Hide(); } };
   window.Closing+=delegate(object s,System.ComponentModel.CancelEventArgs e)
   {
    state.Left=window.Left; state.Top=window.Top;
    if(!Save()) { e.Cancel=true; MessageBox.Show(window,"进度未能保存。窗口已保留，请在设置中导出备份，或检查本地数据目录是否可写。","Save failed",MessageBoxButton.OK,MessageBoxImage.Warning); }
   };
   N<Button>("LearnNav").Click+=delegate { ShowPage("Learn"); };
   N<Button>("LibraryNav").Click+=delegate { ShowPage("Library"); };
   N<Button>("ProgressNav").Click+=delegate { ShowPage("Progress"); };
   N<Button>("SettingsButton").Click+=delegate { ShowPage(page=="Settings"?"Learn":"Settings"); };
   N<Button>("ChineseButton").Click+=delegate { ToggleChinese(); };
   N<Button>("ModeButton").Click+=delegate { state.IpaMode=!state.IpaMode; state.WordRevealed=false; RenderLearn(); Save(); };
   N<Button>("RevealWordButton").Click+=delegate { state.WordRevealed=true; RenderLearn(); Save(); };
   N<Button>("AgainButton").Click+=delegate { Rate(0); };
   N<Button>("SoundButton").Click+=delegate { Rate(1); };
   N<Button>("GotItButton").Click+=delegate { Rate(2); };
   N<Button>("SpeakButton").Click+=delegate
   {
    var c=Current(); if(state.Quiet || voice==null || englishVoice==null || c==null) return;
    try { voice.SpeakAsyncCancelAll(); voice.SpeakAsync(c.Term+". "+c.Example); } catch(Exception) { notice="Speech unavailable · quiet practice still works"; Status(); }
   };
   N<TextBox>("SearchBox").TextChanged+=delegate { if(page=="Library") RenderLibrary(); };
   N<ComboBox>("CategoryFilter").SelectionChanged+=delegate { if(page=="Library") RenderLibrary(); };
   N<Button>("AddButton").Click+=delegate { AddCard(); };
   N<CheckBox>("QuietCheck").Click+=delegate { if(settingControls)return; state.Quiet=N<CheckBox>("QuietCheck").IsChecked==true; if(state.Quiet && voice!=null) voice.SpeakAsyncCancelAll(); notice=null; RenderLearn(); Save(); };
   N<CheckBox>("TopmostCheck").Click+=delegate { if(settingControls)return; state.KeepOnTop=N<CheckBox>("TopmostCheck").IsChecked==true; window.Topmost=state.KeepOnTop; Save(); };
   N<ComboBox>("PronunciationCombo").SelectionChanged+=delegate
   {
    if(settingControls)return;int selected=N<ComboBox>("PronunciationCombo").SelectedIndex;if(selected<0)return;state.PronunciationMode=selected==0?"UK":selected==2?"BOTH":"US";RenderLearn();Save();
   };
   N<ComboBox>("UiFontCombo").SelectionChanged+=delegate
   {
    if(settingControls)return;string selected=N<ComboBox>("UiFontCombo").SelectedItem as string;if(selected==null)return;state.UiFontName=selected=="Default"?"":selected;UiTypography.Apply(window,state.UiFontName);Save();
   };
   N<Button>("ExportButton").Click+=delegate { Export(); };
   N<Button>("ImportButton").Click+=delegate { Import(); };
   N<Button>("FolderButton").Click+=delegate { System.Diagnostics.Process.Start("explorer.exe",store.DirectoryPath); };
   window.PreviewKeyDown+=delegate(object sender,KeyEventArgs e)
   {
    if(page=="Lookup" && e.Key==Key.Escape) { BackFromWord(); e.Handled=true; return; }
    if(page!="Learn" || Keyboard.Modifiers!=ModifierKeys.None || Keyboard.FocusedElement is TextBox) return;
    if(e.Key==Key.Space) { if(state.IpaMode && !state.WordRevealed) { state.WordRevealed=true; RenderLearn(); Save(); } else ToggleChinese(); e.Handled=true; }
    if(e.Key==Key.D1 || e.Key==Key.NumPad1) { Rate(0); e.Handled=true; }
    if(e.Key==Key.D2 || e.Key==Key.NumPad2) { Rate(1); e.Handled=true; }
    if(e.Key==Key.D3 || e.Key==Key.NumPad3) { Rate(2); e.Handled=true; }
   };
  }
  void ShowPage(string selected)
  {
   page=selected;
   foreach(string name in new[]{"Learn","Library","Progress","Settings","Lookup"}) N<Grid>(name+"Page").Visibility=name==selected?Visibility.Visible:Visibility.Collapsed;
   if(selected!="Lookup") { lookupGeneration++; lookupPath.Clear(); }
   foreach(string name in new[]{"Learn","Library","Progress"}) N<Button>(name+"Nav").Background=Color(name==selected?"#E5EBDE":"Transparent");
   if(selected=="Learn") { if(Current()==null) { SelectNext(); Save(); } RenderLearn(); }
   if(selected=="Library") RenderLibrary();
   if(selected=="Progress") RenderProgress();
   if(selected=="Settings")
   {
    settingControls=true; N<CheckBox>("QuietCheck").IsChecked=state.Quiet; N<CheckBox>("TopmostCheck").IsChecked=state.KeepOnTop;N<ComboBox>("PronunciationCombo").SelectedIndex=state.PronunciationMode=="UK"?0:state.PronunciationMode=="BOTH"?2:1;N<ComboBox>("UiFontCombo").SelectedItem=String.IsNullOrWhiteSpace(state.UiFontName)?"Default":state.UiFontName; settingControls=false;
    N<SelectableText>("VoiceInfo").Text=englishVoice==null?"No English system voice was found. All quiet practice remains available.":"Offline voice: "+englishVoice+". IPA typeface: Doulos SIL.";
   }
   UiTypography.Apply(window,state.UiFontName);
  }
  static string CategoryName(string cat) { return cat=="work"?"WORK & CODE":cat=="cars"?"CARS & THE ROAD":"AI & THE NEWS"; }
  void RenderLearn()
  {
   Card c=Current(); bool has=c!=null; bool hidden=has && state.IpaMode && !state.WordRevealed;
   int today=state.History.Count(h=>h.Day==Engine.Day(DateTime.UtcNow));
   int due=cards.Count(x=>state.Entries.ContainsKey(x.Id) && Engine.Date(state.Entries[x.Id].DueUtc)<=DateTime.UtcNow);
   N<SelectableText>("SessionCaption").Text=today+" reviewed today · "+due+" due · no daily target";
   N<Button>("ModeButton").Content=state.IpaMode?"Word first  ↗":"IPA first  ↗";
   N<SelectableText>("CategoryText").Text=has?CategoryName(c.Category)+(state.Entries.ContainsKey(c.Id)?"  /  REVISIT":"  /  DISCOVER"):"A GOOD PLACE TO PAUSE";
   N<SelectableText>("CategoryText").Foreground=Color(has && c.Category=="cars"?"#9A7942":has && c.Category=="news"?"#67829E":"#61816C");
   N<SelectableText>("WordText").Text=has?(hidden?"Read it. Recall it.":c.Term):"All caught up.";
   N<SelectableText>("WordText").FontSize=has && c.Term.Length>21?23:29;
   N<SelectableText>("IpaText").Text=has?Phonetics.Format(c.Ipa,c.BritishIpa,state.PronunciationMode):"";
   N<SelectableText>("IpaText").FontSize=hidden?25:18;
   N<SelectableText>("StressText").Text=has?(hidden?"IPA → recall the word, then reveal":c.Stress.Split('·')[0].Trim()):"Come back when a review is due.";
   N<SelectableText>("IpaText").LookupEnabled=false;
   N<SelectableText>("StressText").LookupEnabled=false;
   N<Button>("RevealWordButton").Visibility=hidden?Visibility.Visible:Visibility.Collapsed;
   N<Border>("ExampleBorder").Visibility=has && !hidden?Visibility.Visible:Visibility.Collapsed;
   var example=N<SelectableText>("ExampleText"); example.Inlines.Clear();
   if(has)
   {
    int index=c.Example.IndexOf(c.Term,StringComparison.OrdinalIgnoreCase);
    if(index>=0) { example.Inlines.Add(new Run(c.Example.Substring(0,index))); example.Inlines.Add(new Run(c.Example.Substring(index,c.Term.Length)){FontWeight=FontWeights.SemiBold}); example.Inlines.Add(new Run(c.Example.Substring(index+c.Term.Length))); }
    else example.Inlines.Add(new Run(c.Example));
   }
   N<SelectableText>("SenseLabelText").Text=has?WordLookup.PosLabel(WordLookup.CardPos(c)):"IN PLAIN ENGLISH";
   N<SelectableText>("DefinitionText").Text=has?WordLookup.FromCard(c).Senses[0].Definition:"Browse your library to revisit a word, or add a phrase you met today.";
   N<Button>("UsesButton").Visibility=has && !hidden?Visibility.Visible:Visibility.Collapsed;
   N<SelectableText>("CollocationText").Text=has && !hidden?c.Collocation:"";
   N<Button>("ChineseButton").Visibility=has && !hidden?Visibility.Visible:Visibility.Collapsed;
   N<Button>("ChineseButton").Content=state.ChineseVisible?"收起中文释义  −":"查看中文释义  ＋";
   N<Border>("ChineseBox").Visibility=has && !hidden && state.ChineseVisible?Visibility.Visible:Visibility.Collapsed;
   N<SelectableText>("ChineseText").Text=has?(String.IsNullOrWhiteSpace(c.MeaningZh)?"暂无中文释义":c.MeaningZh)+(String.IsNullOrWhiteSpace(c.ExampleZh)?"":"\n"+c.ExampleZh):"";
   N<Button>("SpeakButton").IsEnabled=has && !hidden && !state.Quiet && englishVoice!=null;
   foreach(string name in new[]{"AgainButton","SoundButton","GotItButton"}) N<Button>(name).IsEnabled=has && !hidden;
  }
  void ToggleChinese() { if(Current()==null || (state.IpaMode && !state.WordRevealed)) return; state.ChineseVisible=!state.ChineseVisible; RenderLearn(); Save(); }
  void Rate(int rating)
  {
   var c=Current(); if(c==null || (state.IpaMode && !state.WordRevealed)) return;
   notice=null; if(voice!=null) voice.SpeakAsyncCancelAll();
   Engine.Rate(state,c,rating,DateTime.UtcNow); SelectNext(); RenderLearn(); N<ScrollViewer>("CardScroll").ScrollToTop(); Save();
  }
  void Study(Card card) { state.CurrentId=card.Id; state.ChineseVisible=false; state.WordRevealed=false; notice=null; ShowPage("Learn"); N<ScrollViewer>("CardScroll").ScrollToTop(); Save(); }
  void OpenWord(string word,string context)
  {
   if(String.IsNullOrWhiteSpace(word) || WordLookup.IsStopWord(word))return;
   if(page=="Lookup" && lookupPath.Count>0 && lookupPath.Last().Word.Equals(word,StringComparison.OrdinalIgnoreCase))return;
   if(page!="Lookup") {lookupOrigin=page;originScroll=N<ScrollViewer>("CardScroll").VerticalOffset;lookupPath.Clear();}
   else if(lookupPath.Count>0)lookupPath.Last().Scroll=N<ScrollViewer>("LookupScroll").VerticalOffset;
   var visit=new LookupVisit{Word=word,Context=context}; lookupPath.Add(visit);
   ShowPage("Lookup"); RenderWord(visit,true); int generation=++lookupGeneration;
   var snapshot=cards.ToList();
   ThreadPool.QueueUserWorkItem(delegate
   {
    WordEntry entry=null; string error=null;
    try {entry=dictionary.Find(word,snapshot);}catch(Exception ex){error=ex.Message;}
    window.Dispatcher.BeginInvoke(new Action(delegate
    {
     if(generation!=lookupGeneration || page!="Lookup" || lookupPath.Last()!=visit)return;
     visit.Entry=entry;RenderWord(visit,false); if(error!=null)N<SelectableText>("StatusText").Text="Dictionary unavailable · learning progress kept";
    }));
   });
  }
  SelectableText WordText(string value,double size,string color)
  {
   return new SelectableText{Text=value??"",FontSize=size,Foreground=Color(color),Margin=new Thickness(0,0,0,8),TextWrapping=TextWrapping.Wrap};
  }
  void RenderWord(LookupVisit visit,bool loading)
  {
   var panel=N<StackPanel>("LookupContent");panel.Children.Clear();
   string title=visit.Entry==null?visit.Word:visit.Entry.Word;
   var heading=WordText(title,28,"#263D39");heading.FontFamily=new FontFamily("Georgia");panel.Children.Add(heading);
   if(!title.Equals(visit.Word,StringComparison.OrdinalIgnoreCase))panel.Children.Add(WordText("On the page: "+visit.Word+"  →  headword: "+title,10,"#829080"));
   if(loading){panel.Children.Add(WordText("Finding an offline entry…",12,"#829080"));UiTypography.Apply(window,state.UiFontName);return;}
   var entry=visit.Entry;
   if(entry!=null)
   {
    if(!String.IsNullOrWhiteSpace(entry.Ipa)||!String.IsNullOrWhiteSpace(entry.BritishIpa)){var ipa=WordText(Phonetics.Format(entry.Ipa,entry.BritishIpa,state.PronunciationMode),18,"#697A70");Phonetics.Apply(ipa);panel.Children.Add(ipa);}
    if(!String.IsNullOrWhiteSpace(entry.Forms))panel.Children.Add(WordText(entry.Forms,10,"#829080"));
    foreach(var sense in entry.Senses)
    {
     var box=new StackPanel{Margin=new Thickness(0,0,0,12)};
     var pos=WordText(WordLookup.PosLabel(sense.PartOfSpeech),11,"#47745D");pos.FontWeight=FontWeights.SemiBold;box.Children.Add(pos);
     if((!String.IsNullOrWhiteSpace(sense.Ipa)||!String.IsNullOrWhiteSpace(sense.BritishIpa)) && Phonetics.Format(sense.Ipa,sense.BritishIpa,state.PronunciationMode)!=Phonetics.Format(entry.Ipa,entry.BritishIpa,state.PronunciationMode)){var ipa=WordText(Phonetics.Format(sense.Ipa,sense.BritishIpa,state.PronunciationMode),17,"#697A70");Phonetics.Apply(ipa);box.Children.Add(ipa);}
     box.Children.Add(WordText(sense.Definition,14,"#344C46"));
     if(!String.IsNullOrWhiteSpace(sense.Example)) {box.Children.Add(new Border{BorderBrush=Color("#93AD95"),BorderThickness=new Thickness(2,0,0,0),Padding=new Thickness(10,0,0,0),Child=WordText(sense.Example,14,"#344C46"),Margin=new Thickness(0,3,0,4)});}
     if(!String.IsNullOrWhiteSpace(sense.Usage))box.Children.Add(WordText(sense.Usage,10,"#829080"));
     if(!String.IsNullOrWhiteSpace(sense.MeaningZh))
     {
      var chinese=WordText(sense.MeaningZh+(String.IsNullOrWhiteSpace(sense.ExampleZh)?"":"\n"+sense.ExampleZh),11,"#6F806E");chinese.Visibility=Visibility.Collapsed;
      var reveal=new Button{Content="查看此词性的中文释义 ＋",Background=Brushes.Transparent,Foreground=Color("#829080"),Padding=new Thickness(0,3,0,3),FontSize=10,HorizontalAlignment=HorizontalAlignment.Left};
      reveal.Click+=delegate {chinese.Visibility=chinese.Visibility==Visibility.Visible?Visibility.Collapsed:Visibility.Visible;reveal.Content=chinese.Visibility==Visibility.Visible?"收起中文释义 −":"查看此词性的中文释义 ＋";};box.Children.Add(reveal);box.Children.Add(chinese);
     }
     panel.Children.Add(new Border{Background=Color("#FFFEFA"),CornerRadius=new CornerRadius(10),Padding=new Thickness(13,12,13,2),Child=box,Margin=new Thickness(0,5,0,4)});
    }
    panel.Children.Add(WordText(entry.Source,9,"#899585"));
   }
   else
   {
    panel.Children.Add(WordText("No offline entry for this word yet.",13,"#637460"));
    var online=new Button{Content="Look up online",Background=Color("#E5EBDE"),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,4,0,12)};
    online.Click+=delegate
    {
     online.IsEnabled=false;online.Content="Looking up…";int generation=++lookupGeneration;
     ThreadPool.QueueUserWorkItem(delegate
     {
      WordEntry found=null;try{found=dictionary.Online(visit.Word);}catch{}
      window.Dispatcher.BeginInvoke(new Action(delegate {if(generation!=lookupGeneration || page!="Lookup")return;visit.Entry=found;if(found!=null)RenderWord(visit,false);else{online.IsEnabled=true;online.Content="Retry online lookup";N<SelectableText>("StatusText").Text="No result or connection unavailable · 可返回继续学习";}}));
     });
    };panel.Children.Add(online);
   }
   if(!String.IsNullOrWhiteSpace(visit.Context))
   {
    panel.Children.Add(WordText("FROM THE PAGE",9,"#899585"));panel.Children.Add(WordText(visit.Context,12,"#6F806E"));
   }
   UiTypography.Apply(window,state.UiFontName);N<ScrollViewer>("LookupScroll").ScrollToVerticalOffset(visit.Scroll);
  }
  void BackFromWord()
  {
   lookupGeneration++;
   if(lookupPath.Count>1){lookupPath.RemoveAt(lookupPath.Count-1);RenderWord(lookupPath.Last(),false);}
   else {string origin=lookupOrigin;double scroll=originScroll;ShowPage(origin);if(origin=="Learn")window.Dispatcher.BeginInvoke(new Action(()=>N<ScrollViewer>("CardScroll").ScrollToVerticalOffset(scroll)));}
  }
  void RenderLibrary()
  {
   var panel=N<StackPanel>("LibraryItems"); panel.Children.Clear(); string search=N<TextBox>("SearchBox").Text.Trim();
   int filter=N<ComboBox>("CategoryFilter").SelectedIndex; string cat=filter==1?"work":filter==2?"cars":filter==3?"news":null;
   var selected=cards.Where(c=>(cat==null || cat==c.Category) && (String.IsNullOrEmpty(search) || (c.Term+" "+c.Example+" "+c.Definition+" "+c.MeaningZh).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0)).ToList();
   N<SelectableText>("LibraryCount").Text=selected.Count+" / "+cards.Count+" cards · search above";
   foreach(var item in selected)
   {
    Card c=item; var content=new StackPanel();
    content.Children.Add(new SelectableText{Text=c.Term,FontSize=14,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap});
    content.Children.Add(new SelectableText{Text=CategoryName(c.Category)+"  ·  "+(state.Entries.ContainsKey(c.Id)?state.Entries[c.Id].LastRating=="sound"?"sound practice":"reviewed":"new"),FontSize=9,Foreground=Color("#8A9588"),Margin=new Thickness(0,4,0,0)});
    content.MaxWidth=235;
    var row=new DockPanel();var button=new Button{Content="Study ↗",Background=Color("#E5EBDE"),Padding=new Thickness(8,5,8,5),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(7,0,0,0)};
    button.Click+=delegate { Study(c); };DockPanel.SetDock(button,Dock.Right);row.Children.Add(button);row.Children.Add(content);
    panel.Children.Add(new Border{Child=row,Background=Color("#FFFEFA"),CornerRadius=new CornerRadius(8),Padding=new Thickness(10,9,10,9),Margin=new Thickness(0,0,0,5)});
   }
   if(selected.Count==0) panel.Children.Add(new SelectableText{Text="No matches. Try a shorter word.",Margin=new Thickness(5,15,0,0),FontSize=12});
   UiTypography.Apply(window,state.UiFontName);
  }
  void RenderProgress()
  {
   var values=state.Entries.Values.ToList();
   N<SelectableText>("TodayCount").Text=state.History.Count(h=>h.Day==Engine.Day(DateTime.UtcNow)).ToString();
   int due=values.Count(e=>Engine.Date(e.DueUtc)<=DateTime.UtcNow);
   N<SelectableText>("ProgressSummary").Text=values.Count+" / "+cards.Count+" words explored\n"+values.Sum(e=>e.Reviews)+" total reviews  ·  "+due+" due now\n"+values.Count(e=>e.LastRating=="sound")+" marked for sound practice";
   var panel=N<StackPanel>("WeakItems"); panel.Children.Clear();
   var weak=cards.Where(c=>state.Entries.ContainsKey(c.Id) && state.Entries[c.Id].LastRating!="good").OrderBy(c=>Engine.Date(state.Entries[c.Id].DueUtc)).Take(12).ToList();
   foreach(var item in weak) { Card c=item;var row=new DockPanel();var b=new Button{Content="Study ↗",Padding=new Thickness(8,4,8,4),Background=Color("#EBEEE4"),Margin=new Thickness(6,0,0,0)};b.Click+=delegate {Study(c);};DockPanel.SetDock(b,Dock.Right);row.Children.Add(b);row.Children.Add(WordText(c.Term,12,"#53685D"));panel.Children.Add(new Border{Child=row,Margin=new Thickness(0,0,0,5)}); }
   if(weak.Count==0) panel.Children.Add(new SelectableText{Text="Words you find tricky will appear here.",FontSize=11,Foreground=Color("#829080")});
   UiTypography.Apply(window,state.UiFontName);
  }
  void Export()
  {
   var dialog=new Microsoft.Win32.SaveFileDialog{Title="Export learning backup",Filter="Lilt backup (*.json)|*.json",FileName="Lilt-backup-"+Engine.Day(DateTime.UtcNow)+".json"};
   if(dialog.ShowDialog(window)!=true)return;
   try { store.Export(state,dialog.FileName); notice="Backup exported · 包含进度和自建词卡"; Status(); } catch(Exception e) { MessageBox.Show(window,e.Message,"Export failed"); }
  }
  void Import()
  {
   var dialog=new Microsoft.Win32.OpenFileDialog{Title="Restore learning backup",Filter="Lilt backup (*.json)|*.json"};
   if(dialog.ShowDialog(window)!=true)return;
   try
   {
    var info=new FileInfo(dialog.FileName); if(info.Length>8*1024*1024) throw new InvalidDataException("Backup is too large.");
    var restored=store.Parse(File.ReadAllText(dialog.FileName,Encoding.UTF8));
    if(MessageBox.Show(window,"恢复将替换当前进度与自建词卡。恢复前会自动保留一份当前数据备份。\n\nRestore this backup?","Restore backup",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
    store.Export(state,Path.Combine(store.DirectoryPath,"before-restore-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff")+".json"));
    store.Save(restored); state=restored; cards=Engine.BuiltIns().Concat(state.CustomCards).ToList(); window.Topmost=state.KeepOnTop;
    if(voice!=null) voice.SpeakAsyncCancelAll(); notice="Backup restored · 恢复完成"; ShowPage("Settings"); RenderLearn(); Status();
   }
   catch(Exception e) { MessageBox.Show(window,e.Message,"Restore failed · 原进度保留"); }
  }
  void AddCard()
  {
   var dialog=new Window{Title="Add a word or phrase",Width=390,Height=610,ResizeMode=ResizeMode.NoResize,Owner=window,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=Color("#F6F5EF"),FontFamily=window.FontFamily,ShowInTaskbar=false};
   var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto}; var stack=new StackPanel{Margin=new Thickness(20)}; scroll.Content=stack; dialog.Content=scroll;
   stack.Children.Add(new SelectableText{Text="Something you met today.",FontSize=21,FontFamily=new FontFamily("Georgia"),Margin=new Thickness(0,0,0,13)});
   stack.Children.Add(new SelectableText{Text="Part of speech / 词性 *",FontSize=11,Margin=new Thickness(0,5,0,4)});
   var parts=new ComboBox{ItemsSource=new[]{"noun","verb","adjective","adverb","noun phrase","phrasal verb"},SelectedIndex=0,Padding=new Thickness(7)};stack.Children.Add(parts);
   string[] labels={"Word / phrase *","American IPA (US) · optional","Stress / pronunciation note · optional","Simple English meaning *","English example *","中文释义 · 可选","例句中文 · 可选","Common phrase · optional","British IPA (UK) · optional"};
   var boxes=new List<TextBox>();
   foreach(string label in labels) { stack.Children.Add(new SelectableText{Text=label,FontSize=11,Margin=new Thickness(0,8,0,4),Foreground=Color("#6F806E")}); var box=new TextBox{Padding=new Thickness(8,6,8,6),FontSize=12,TextWrapping=TextWrapping.Wrap,MaxLength=label.StartsWith("Word")?80:2000}; boxes.Add(box); stack.Children.Add(box); }
   foreach(int index in new[]{1,8}){boxes[index].FontFamily=Phonetics.DisplayFont;boxes[index].FontSize=18;UiTypography.KeepIpaFont(boxes[index]);}
   stack.Children.Add(new SelectableText{Text="Topic",FontSize=11,Margin=new Thickness(0,9,0,4)});
   var category=new ComboBox{ItemsSource=new[]{"Work & code","Cars","AI & news"},SelectedIndex=0,Padding=new Thickness(7)}; stack.Children.Add(category);
   var error=new SelectableText{Foreground=Color("#AD5C47"),FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,0)}; stack.Children.Add(error);
   var add=new Button{Content="Save to my library  →",Margin=new Thickness(0,12,0,0),Padding=new Thickness(10),Background=Color("#315A49"),Foreground=Brushes.White}; stack.Children.Add(add);
   add.Click+=delegate
   {
    if(String.IsNullOrWhiteSpace(boxes[0].Text) || String.IsNullOrWhiteSpace(boxes[3].Text) || String.IsNullOrWhiteSpace(boxes[4].Text)) { error.Text="Please add a word, English meaning and example."; return; }
    if(cards.Any(c=>c.Term.Equals(boxes[0].Text.Trim(),StringComparison.OrdinalIgnoreCase))) { error.Text="This word is already in your library."; return; }
    var card=new Card{Id="custom-"+Guid.NewGuid().ToString("N"),Category=new[]{"work","cars","news"}[category.SelectedIndex],Term=boxes[0].Text.Trim(),Ipa=boxes[1].Text.Trim(),Stress=boxes[2].Text.Trim(),Definition=boxes[3].Text.Trim(),Example=boxes[4].Text.Trim(),MeaningZh=boxes[5].Text.Trim(),ExampleZh=boxes[6].Text.Trim(),Collocation=boxes[7].Text.Trim()};
    card.PartOfSpeech=(string)parts.SelectedItem;
    card.BritishIpa=boxes[8].Text.Trim();
    state.CustomCards.Add(card); cards.Add(card);
    if(!Save()) { state.CustomCards.Remove(card); cards.Remove(card); error.Text="Could not save. Please check the data folder."; return; }
    dialog.Close(); Study(card);
   };
   UiTypography.Apply(dialog,state.UiFontName);dialog.ShowDialog();
  }
 }
 public static class Program
 {
  static string Arg(string[] args,string key) { int i=Array.IndexOf(args,key); return i>=0 && i+1<args.Length?args[i+1]:null; }
  [STAThread] public static int Main(string[] args)
  {
   string report=Arg(args,"--self-test");
   if(report!=null) return SelfTest.Run(report);
   string directory=Arg(args,"--data-dir") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LiltPocketEnglish");
   try
   {
    string hash; using(var sha=SHA256.Create()) hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(directory).ToUpperInvariant()))).Replace("-","").Substring(0,24);
    bool first;
    using(var mutex=new Mutex(true,"Local\\LiltPocketEnglish-"+hash,out first))
    using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\LiltPocketEnglish-activate-"+hash))
    {
     if(!first) { signal.Set(); return 0; }
     var app=new Application{ShutdownMode=ShutdownMode.OnMainWindowClose};
     app.DispatcherUnhandledException+=delegate(object sender,DispatcherUnhandledExceptionEventArgs e) { MessageBox.Show("An unexpected error occurred. Saved progress remains in:\n"+directory+"\n\n"+e.Exception.Message,"Lilt"); e.Handled=true; };
     try { var pocket=new PocketApp(directory,signal); pocket.Run(app); }
     finally { mutex.ReleaseMutex(); }
    }
    return 0;
   }
   catch(Exception e) { MessageBox.Show(e.Message,"Lilt could not start",MessageBoxButton.OK,MessageBoxImage.Warning); return 1; }
  }
 }
 public static class SelfTest
 {
  static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
  static void RenderSurface(Window root,string path)
  {
   var surface=(FrameworkElement)root.Content;var resources=surface.Resources;root.Content=null;
   try
   {
    surface.Resources=root.Resources;surface.Measure(new Size(400,500));surface.Arrange(new Rect(0,0,400,500));surface.UpdateLayout();
    var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(400,500,96,96,PixelFormats.Pbgra32);bitmap.Render(surface);
    var pixels=new byte[400*500*4];bitmap.CopyPixels(pixels,400*4,0);Check(Enumerable.Range(0,400*500).Count(i=>pixels[i*4+3]>0)>5000,"Settings render was empty");
    var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));using(var output=File.Create(path))encoder.Save(output);
   }
   finally{surface.Resources=resources;root.Content=surface;}
  }
  public static int Run(string report)
  {
   var notes=new List<string>(); string directory=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)),"test-data-"+Guid.NewGuid().ToString("N"));
   try
   {
    if(Application.Current==null)new Application();Application.Current.ShutdownMode=ShutdownMode.OnExplicitShutdown;
    var cards=Engine.BuiltIns(); Check(cards.Count==100 && cards.Select(c=>c.Id).Distinct().Count()==100,"Card count / identity");
    Check(cards.Count(c=>c.Category=="work")==40 && cards.Count(c=>c.Category=="cars")==35 && cards.Count(c=>c.Category=="news")==25,"Category mix");
    var incomplete=cards.Where(c=>String.IsNullOrWhiteSpace(c.Term) || !c.Ipa.StartsWith("/",StringComparison.Ordinal) || !c.Ipa.EndsWith("/",StringComparison.Ordinal) || String.IsNullOrWhiteSpace(c.Stress) || String.IsNullOrWhiteSpace(c.Definition) || String.IsNullOrWhiteSpace(c.Example) || String.IsNullOrWhiteSpace(c.MeaningZh) || String.IsNullOrWhiteSpace(c.ExampleZh)).ToList();
    Check(incomplete.Count==0,"Incomplete cards: "+String.Join(",",incomplete.Select(c=>c.Id+" "+c.Term+" "+c.Ipa))); notes.Add("PASS: 100 complete cards, exact 40/35/25 topic mix, unique IDs.");
    var state=new AppState(); var now=new DateTime(2026,11,24,0,0,0,DateTimeKind.Utc); var sequence=new List<Card>();
    for(int i=0;i<100;i++) { var card=Engine.Next(state,cards,now); Check(card!=null,"Queue exhausted early"); sequence.Add(card); Engine.Rate(state,card,2,now); }
    Check(sequence.Select(c=>c.Id).Distinct().Count()==100,"New card repeated");
    foreach(var batch in Enumerable.Range(0,5).Select(i=>sequence.Skip(i*20).Take(20).ToList())) Check(batch.Count(c=>c.Category=="work")==8 && batch.Count(c=>c.Category=="cars")==7 && batch.Count(c=>c.Category=="news")==5,"Weighted block mismatch");
    Check(Engine.Next(state,cards,now)==null,"Future reviews served early"); notes.Add("PASS: Each 20 new cards follows 8/7/5 mix; no repeats; future reviews wait.");
    state=new AppState(); var first=cards[0]; Engine.Rate(state,first,1,now); Check(state.Entries[first.Id].Meaning==1 && state.Entries[first.Id].Sound==0 && state.Entries[first.Id].LastRating=="sound","Sound state collapsed into meaning");
    Check(Engine.Next(state,cards,now.AddDays(1)).Id==first.Id,"Due reviews should come first"); Engine.Rate(state,first,0,now.AddDays(1)); Check(Engine.Date(state.Entries[first.Id].DueUtc)==now.AddDays(1).AddMinutes(10),"Again delay");
    Check(Engine.Day(new DateTime(2026,11,23,16,1,0,DateTimeKind.Utc))=="2026-11-24","China date boundary"); notes.Add("PASS: Meaning/sound are distinct; due review priority; 10-minute retry; Shanghai day boundary.");
    var store=new StateStore(directory); state.CurrentId=cards[7].Id; state.IpaMode=true; state.WordRevealed=true; state.ChineseVisible=true; state.KeepOnTop=true; state.Left=120; state.Top=130;
    store.Save(state); var loaded=store.Load(); Check(loaded.CurrentId==cards[7].Id && loaded.IpaMode && loaded.WordRevealed && loaded.ChineseVisible && loaded.KeepOnTop && loaded.Entries[first.Id].Reviews==2,"Reopen continuity"); notes.Add("PASS: Reopening preserves exact card, reveal state, mode, settings and progress.");
    store.Save(state); File.WriteAllText(store.FilePath,"{damaged"); loaded=store.Load(); Check(store.Warning!=null && loaded.Entries[first.Id].Reviews==2,"Backup recovery"); notes.Add("PASS: Interrupted/damaged save recovers previous data and preserves damaged file.");
    string backup=Path.Combine(directory,"export.json"); store.Export(loaded,backup); Check(store.Parse(File.ReadAllText(backup)).CurrentId==cards[7].Id,"Export round trip");
    bool rejected=false; try { store.Parse("{\"Version\":99}"); } catch { rejected=true; } Check(rejected,"Unsupported version accepted");
    var custom=new Card{Id="custom-test",Category="cars",Term="test phrase",Definition="A test meaning.",Example="This is a test phrase."}; loaded.CustomCards.Add(custom); store.Save(loaded); Check(store.Load().CustomCards.Count==1,"Custom card persistence");
    loaded.CustomCards.Add(custom); rejected=false; try { string bad=Path.Combine(directory,"bad.json");store.Export(loaded,bad);store.Parse(File.ReadAllText(bad)); } catch { rejected=true; } Check(rejected,"Duplicate custom IDs accepted");
    notes.Add("PASS: Export/restore round trip; custom card persistence; invalid version and duplicate IDs rejected.");
    var dictionary=new WordLookup();
    Check(cards.All(c=>new[]{"noun","verb","adjective","adverb","noun phrase","phrasal verb"}.Contains(WordLookup.CardPos(c))),"Missing part of speech");
    Check(cards.All(c=>WordLookup.FromCard(c).Senses.All(s=>!String.IsNullOrWhiteSpace(s.Definition) && !String.IsNullOrWhiteSpace(s.Example))),"Meaning/example missing for a curated part of speech");
    var query=WordLookup.FromCard(cards.First(c=>c.Term=="query"));Check(query.Senses.Any(s=>s.PartOfSpeech=="noun") && query.Senses.Any(s=>s.PartOfSpeech=="verb"),"Query senses missing");
    var duplicate=WordLookup.FromCard(cards.First(c=>c.Term=="duplicate"));Check(duplicate.Senses.Count==3 && duplicate.Senses.Last().Ipa!=duplicate.Ipa,"Duplicate verb pronunciation missing");
    Check(WordLookup.Forms("troubleshoot","verb").Contains("troubleshot") && WordLookup.Forms("chip","verb").Contains("chipping") && WordLookup.Forms("query","verb").Contains("queried"),"Incorrect word forms");
    Check(WordLookup.Candidates("triggered").Contains("trigger") && WordLookup.Candidates("batteries").Contains("battery") && WordLookup.Candidates("running").Contains("run"),"Inflection lookup");
    Check(WordLookup.WordAt("Check the gateway, please.",11)=="gateway" && WordLookup.IsStopWord("are") && !WordLookup.IsStopWord("pressure"),"Word hit testing / stop words");
    Check(dictionary.Find("pressure",cards)!=null && dictionary.Find("settings",cards)!=null && dictionary.Find("vehicles",cards)!=null && dictionary.Find("restarting",cards)!=null,"Offline incidental-word lookup failed");
    notes.Add("PASS: Parts of speech and corresponding examples; alternate senses and pronunciations; verb forms; inflections; non-card offline words.");
    var selectable=new SelectableText{Text="Select this sentence."};selectable.SelectAll();Check(selectable.IsReadOnly && selectable.Selection.Text.Trim()=="Select this sentence.","Selectable read-only content failed");
    using(var iconStream=Assembly.GetExecutingAssembly().GetManifestResourceStream("suzuha.png"))using(var bitmap=new Drawing.Bitmap(iconStream)) {Check(bitmap.GetPixel(0,0).A==0 && bitmap.GetPixel(bitmap.Width-1,bitmap.Height-1).A==0,"Icon background is not transparent");}
    notes.Add("PASS: Read-only content remains selectable; icon corners have zero alpha.");
    Check(cards.First(c=>c.Term=="threshold").Ipa=="/ˈθreʃ.hoʊld/","Threshold transcription does not match the requested convention");
    Check(cards.All(c=>Phonetics.Normalize(c.Ipa)==c.Ipa && !c.Ipa.Contains("ɛ")),"Inconsistent learning-card IPA symbols");
    Check(Phonetics.Normalize("/ˈθrɛʃ.hoʊld/")=="/ˈθreʃ.hoʊld/" && Phonetics.Normalize("/ˈɝnɪŋz/")=="/ˈɝːnɪŋz/" && Phonetics.Normalize("/ɔɪ/")=="/ɔɪ/","IPA compatibility normalization");
    System.Windows.Media.GlyphTypeface glyph;var typeface=new Typeface(Phonetics.DisplayFont,FontStyles.Normal,FontWeights.Normal,FontStretches.Normal);
    Check(typeface.TryGetGlyphTypeface(out glyph) && glyph.FontUri.ToString().ToLowerInvariant().Contains("doulossil"),"Embedded Doulos SIL font is unavailable or fell back to a system font");
    Check(cards.All(c=>!String.IsNullOrWhiteSpace(c.BritishIpa)),"Missing curated British pronunciation");
    Check(cards.SelectMany(c=>c.Ipa+c.BritishIpa).Distinct().All(ch=>glyph.CharacterToGlyphMap.ContainsKey(ch)),"The bundled font is missing an IPA glyph");
    var dictionaryIpa=dictionary.Find("pressure",cards).Ipa;Check(dictionaryIpa=="/ˈpreʃ.ɚ/","Expanded dictionary pronunciation convention");
    var preview=new StackPanel{Width=540,Background=new SolidColorBrush(System.Windows.Media.Color.FromRgb(246,245,239)),Margin=new Thickness(0)};
    foreach(string word in new[]{"threshold","configuration","register","earnings"}) {var card=cards.First(c=>c.Term==word);preview.Children.Add(new TextBlock{Text=word,FontFamily=new FontFamily("Segoe UI"),FontSize=15,Margin=new Thickness(24,16,24,2)});var line=new SelectableText{Text=card.Ipa,FontSize=26,Margin=new Thickness(24,0,24,12)};Phonetics.Apply(line);preview.Children.Add(line);}
    preview.Measure(new Size(540,Double.PositiveInfinity));preview.Arrange(new Rect(0,0,540,preview.DesiredSize.Height));preview.UpdateLayout();
    var bitmapPreview=new System.Windows.Media.Imaging.RenderTargetBitmap(540,(int)Math.Ceiling(preview.ActualHeight),96,96,PixelFormats.Pbgra32);bitmapPreview.Render(preview);
    var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapPreview));using(var output=File.Create(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)),"ipa-v11-preview.png")))encoder.Save(output);
    notes.Add("PASS: Requested threshold IPA; consistent core/expanded symbols; embedded font identity and glyph coverage; rendered IPA preview.");
    var threshold=cards.First(c=>c.Term=="threshold");Check(Phonetics.Format(threshold.Ipa,threshold.BritishIpa,"UK")=="/ˈθreʃ.həʊld/" && Phonetics.Format(threshold.Ipa,threshold.BritishIpa,"US")=="/ˈθreʃ.hoʊld/" && Phonetics.Format(threshold.Ipa,threshold.BritishIpa,"BOTH").Contains("UK  /ˈθreʃ.həʊld/\nUS  /ˈθreʃ.hoʊld/"),"Pronunciation modes");
    Check(Phonetics.Format("/test/",null,"UK").Contains("not available"),"Unavailable British pronunciation was mislabeled");
    var input=new TextBox{FontFamily=Phonetics.DisplayFont};UiTypography.KeepIpaFont(input);var inputHost=new StackPanel();inputHost.Children.Add(input);UiTypography.Apply(inputHost,"Arial");Check(input.FontFamily.Source==Phonetics.DisplayFont.Source,"UI font changed an editable pronunciation field");
    Check(!String.IsNullOrWhiteSpace(dictionary.Find("pressure",cards).BritishIpa),"Expanded British pronunciation missing");
    var oldSettings=store.Parse("{\"Version\":1,\"CurrentId\":null,\"Entries\":{},\"History\":[],\"CustomCards\":[]}");Check(oldSettings.PronunciationMode=="US" && oldSettings.UiFontName=="","Legacy settings defaults");
    string uiDirectory=Path.Combine(directory,"ui-settings");var uiStore=new StateStore(uiDirectory);uiStore.Save(new AppState{CurrentId="w04",PronunciationMode="BOTH",UiFontName="Arial",Left=100,Top=100});
    using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset))
    {
     var pocket=new PocketApp(uiDirectory,signal);var flags=BindingFlags.Instance|BindingFlags.NonPublic;var root=(Window)typeof(PocketApp).GetField("window",flags).GetValue(pocket);root.Left=100;root.Top=100;
     try
     {
      typeof(PocketApp).GetMethod("ShowPage",flags).Invoke(pocket,new object[]{"Settings"});
      var mode=(ComboBox)root.FindName("PronunciationCombo");var font=(ComboBox)root.FindName("UiFontCombo");var ipa=(SelectableText)root.FindName("IpaText");var word=(SelectableText)root.FindName("WordText");
      Check(mode.SelectedIndex==2 && font.SelectedItem as string=="Arial","Saved settings did not populate controls");
      mode.SelectedIndex=0;Check(ipa.Text=="/ˈθreʃ.həʊld/","British selector did not update the learning card");mode.SelectedIndex=1;Check(ipa.Text=="/ˈθreʃ.hoʊld/","American selector did not update the learning card");mode.SelectedIndex=2;Check(ipa.Text.Contains("UK")&&ipa.Text.Contains("US"),"Dual selector did not update the learning card");
      font.SelectedItem="Georgia";Check(word.FontFamily.Source=="Georgia" && ipa.FontFamily.Source==Phonetics.DisplayFont.Source,"UI font selector changed the IPA font or missed learning text");
      var reopened=uiStore.Load();Check(reopened.PronunciationMode=="BOTH"&&reopened.UiFontName=="Georgia"&&reopened.CurrentId=="w04"&&reopened.History.Count==0,"Preferences did not persist independently of progress");
      RenderSurface(root,Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)),"settings-v11-preview.png"));
      typeof(PocketApp).GetMethod("ShowPage",flags).Invoke(pocket,new object[]{"Learn"});RenderSurface(root,Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)),"ipa-dual-v11-preview.png"));
      font.SelectedItem="Default";Check(ipa.FontFamily.Source==Phonetics.DisplayFont.Source && word.FontFamily.Source=="Georgia" && root.FontFamily.Source.Contains("Segoe"),"Default font restoration");
     }
     finally{root.Close();}
    }
    notes.Add("PASS: UK/US/dual selectors update the card and persist; legacy defaults; missing-data handling; UI font changes/reset preserve IPA; rendered settings preview.");
    File.WriteAllLines(report,notes.Concat(new[]{"ALL CHECKS PASSED","Isolated data: "+directory}),new UTF8Encoding(false)); return 0;
   }
   catch(Exception e) { notes.Add("FAIL: "+e); File.WriteAllLines(report,notes,new UTF8Encoding(false)); return 1; }
  }
 }
}
