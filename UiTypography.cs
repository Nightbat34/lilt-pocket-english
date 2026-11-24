using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PocketEnglish
{
 public static class UiTypography
 {
  sealed class Saved { public object Value; public Saved(object value){Value=value;} }
  static readonly DependencyProperty OriginalFontProperty=DependencyProperty.RegisterAttached("OriginalFont",typeof(object),typeof(UiTypography),new PropertyMetadata(null));
  static readonly DependencyProperty KeepIpaFontProperty=DependencyProperty.RegisterAttached("KeepIpaFont",typeof(bool),typeof(UiTypography),new PropertyMetadata(false));
  public static void KeepIpaFont(DependencyObject target){target.SetValue(KeepIpaFontProperty,true);}
  public static void Apply(DependencyObject root,string family)
  {
   if(root==null)return;
   if((bool)root.GetValue(KeepIpaFontProperty))return;
   var ipa=root as SelectableText;if(ipa!=null && ipa.IsPhonetic)return;
   if(root is Control || root is TextBlock)
   {
    var old=root.GetValue(OriginalFontProperty) as Saved;
    if(!String.IsNullOrWhiteSpace(family))
    {
     if(old==null){old=new Saved(root.ReadLocalValue(TextElement.FontFamilyProperty));root.SetValue(OriginalFontProperty,old);}
     root.SetValue(TextElement.FontFamilyProperty,new FontFamily(family));
    }
    else if(old!=null)
    {
     if(old.Value==DependencyProperty.UnsetValue)root.ClearValue(TextElement.FontFamilyProperty);
     else root.SetValue(TextElement.FontFamilyProperty,old.Value);
    }
   }
   foreach(object child in LogicalTreeHelper.GetChildren(root)){var item=child as DependencyObject;if(item!=null)Apply(item,family);}
  }
 }
}
