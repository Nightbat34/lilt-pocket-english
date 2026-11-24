using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace PocketEnglish
{
 public class SelectableText : RichTextBox
 {
  public static Action<string,string> WordRequested;
  public static readonly DependencyProperty TextProperty=DependencyProperty.Register("Text",typeof(string),typeof(SelectableText),new FrameworkPropertyMetadata("",OnText));
  public string Text { get { return (string)GetValue(TextProperty); } set { SetValue(TextProperty,value??""); } }
  public TextWrapping TextWrapping { get; set; }
  public TextTrimming TextTrimming { get; set; }
  public bool LookupEnabled { get; set; }
  public TextAlignment TextAlignment { get { return paragraph.TextAlignment; } set { paragraph.TextAlignment=value; } }
  public double LineHeight { get { return paragraph.LineHeight; } set { paragraph.LineHeight=value; } }
  public InlineCollection Inlines { get { return paragraph.Inlines; } }
  readonly Paragraph paragraph;
  public SelectableText()
  {
   IsReadOnly=true; IsReadOnlyCaretVisible=true; IsDocumentEnabled=true; IsUndoEnabled=false;
   BorderThickness=new Thickness(0); Padding=new Thickness(0); Background=Brushes.Transparent;
   VerticalScrollBarVisibility=ScrollBarVisibility.Disabled; HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled;
   FocusVisualStyle=null; IsTabStop=false; Cursor=Cursors.IBeam; LookupEnabled=true;
   paragraph=new Paragraph{Margin=new Thickness(0)};
   Document=new FlowDocument(paragraph){PagePadding=new Thickness(0)};
   ToolTip="Double-click: word notes · Drag and Ctrl+C: copy · Ctrl+double-click: select only";
   ToolTipService.SetInitialShowDelay(this,1800);
   var menu=new ContextMenu();
   menu.Items.Add(new MenuItem{Header="Copy / 复制",Command=ApplicationCommands.Copy,CommandTarget=this});
   menu.Items.Add(new MenuItem{Header="Select all / 全选",Command=ApplicationCommands.SelectAll,CommandTarget=this});
   ContextMenu=menu;
  }
  static void OnText(DependencyObject obj,DependencyPropertyChangedEventArgs args)
  {
   var text=(SelectableText)obj; text.paragraph.Inlines.Clear(); text.paragraph.Inlines.Add(new Run((string)args.NewValue??""));
  }
  protected override void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
  {
   base.OnPreviewMouseDoubleClick(e);
   if(!LookupEnabled || e.ChangedButton!=MouseButton.Left || (Keyboard.Modifiers&ModifierKeys.Control)!=0 || WordRequested==null) return;
   var position=GetPositionFromPoint(e.GetPosition(this),false); if(position==null)return;
   string actual=new TextRange(Document.ContentStart,Document.ContentEnd).Text.TrimEnd('\r','\n');
   int index=new TextRange(Document.ContentStart,position).Text.Length;
   string word=WordLookup.WordAt(actual,index);
   if(word==null || WordLookup.IsStopWord(word))return;
   var callback=WordRequested; string context=actual;
   e.Handled=true;
   Dispatcher.BeginInvoke(new Action(()=>callback(word,context)),System.Windows.Threading.DispatcherPriority.Background);
  }
 }
}
