using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace PocketEnglish
{
 public static class Phonetics
 {
  static FontFamily displayFont;
  public static FontFamily DisplayFont
  {
   get { return displayFont ?? (displayFont=new FontFamily(new Uri("pack://application:,,,/"),"./assets/fonts/#Doulos SIL")); }
  }
  public static string Normalize(string ipa)
  {
   if(String.IsNullOrWhiteSpace(ipa))return "";
   string text=ipa.Trim().Replace('ɛ','e').Replace('ɹ','r').Replace('g','ɡ').Replace('ɐ','ə');
   text=Regex.Replace(text,@"ɝ(?!ː)","ɝː");
   text=Regex.Replace(text,@"ɑ(?!ː)","ɑː");
   text=Regex.Replace(text,@"ɔ(?![ːɪ])","ɔː");
   text=Regex.Replace(text,@"ɜ(?!ː)","ɜː");
   return text;
  }
  public static void Apply(SelectableText control)
  {
   control.IsPhonetic=true;
   UiTypography.KeepIpaFont(control);
   control.FontFamily=DisplayFont;control.FontWeight=FontWeights.Normal;control.FontStyle=FontStyles.Normal;
   control.LookupEnabled=false;control.ToolTip="IPA · Doulos SIL · pronunciation mode in Settings";
  }
  public static string ValidMode(string mode){return mode=="UK" || mode=="BOTH"?mode:"US";}
  public static string Format(string us,string uk,string mode)
  {
   us=Normalize(us);uk=Normalize(uk);mode=ValidMode(mode);
   if(mode=="UK")return String.IsNullOrWhiteSpace(uk)?"UK · pronunciation not available":uk;
   if(mode=="BOTH")return "UK  "+(String.IsNullOrWhiteSpace(uk)?"pronunciation not available":uk)+"\nUS  "+(String.IsNullOrWhiteSpace(us)?"pronunciation not available":us);
   return String.IsNullOrWhiteSpace(us)?"US · pronunciation not available":us;
  }
 }
}
