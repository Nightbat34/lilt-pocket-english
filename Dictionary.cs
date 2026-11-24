using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace PocketEnglish
{
 public class WordSense
 {
  public string PartOfSpeech { get; set; }
  public string Definition { get; set; }
  public string Example { get; set; }
  public string MeaningZh { get; set; }
  public string ExampleZh { get; set; }
  public string Ipa { get; set; }
  public string Usage { get; set; }
 }
 public class WordEntry
 {
  public string Word { get; set; }
  public string Ipa { get; set; }
  public string Forms { get; set; }
  public string Source { get; set; }
  public List<WordSense> Senses { get; set; }
 }
 public class WordLookup
 {
  static readonly Regex tokens=new Regex(@"[A-Za-z]+(?:['’-][A-Za-z]+)*",RegexOptions.Compiled);
  static readonly HashSet<string> stop=new HashSet<string>(("a an the is are am was were be been being i me my mine we us our ours you your yours he him his she her hers it its they them their theirs this that these those of to in on at by for from with and or but as if than then not no yes do does did done have has had can could may might shall should will would must into onto upon about between out off up down over under all some any each every both either neither so such very also too only just there here who whom whose which what when where why how").Split(' '),StringComparer.OrdinalIgnoreCase);
  static readonly Dictionary<string,string> irregular=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"went","go"},{"gone","go"},{"made","make"},{"ran","run"},{"running","run"},{"sent","send"},{"found","find"},{"bought","buy"},{"brought","bring"},{"thought","think"},{"taught","teach"},{"caught","catch"},{"took","take"},{"taken","take"},{"gave","give"},{"given","give"},{"wrote","write"},{"written","write"},{"built","build"},{"held","hold"},{"kept","keep"},{"left","leave"},{"lost","lose"},{"readings","reading"},{"rose","rise"},{"risen","rise"},{"fell","fall"},{"fallen","fall"},{"wore","wear"},{"worn","wear"},{"drove","drive"},{"driven","drive"},{"better","good"},{"best","good"},{"worse","bad"},{"worst","bad"},{"children","child"},{"people","person"},{"men","man"},{"women","woman"},{"feet","foot"},{"teeth","tooth"},{"indices","index"},{"data","data"}};
  readonly Dictionary<string,WordEntry> cache=new Dictionary<string,WordEntry>(StringComparer.OrdinalIgnoreCase);
  public static bool IsStopWord(string word) { return stop.Contains(word); }
  public static string WordAt(string text,int index)
  {
   foreach(Match m in tokens.Matches(text??"")) if(index>=m.Index && index<m.Index+m.Length) return m.Value.TrimEnd('’','\'').ToLowerInvariant();
   return null;
  }
  public static List<string> Candidates(string word)
  {
   var result=new List<string>(); word=(word??"").ToLowerInvariant().Replace('’','\''); result.Add(word);
   if(word.EndsWith("'s")) result.Add(word.Substring(0,word.Length-2));
   string root; if(irregular.TryGetValue(word,out root)) result.Add(root);
   if(word.EndsWith("ies") && word.Length>4) result.Add(word.Substring(0,word.Length-3)+"y");
   if(word.EndsWith("ied") && word.Length>4) result.Add(word.Substring(0,word.Length-3)+"y");
   if(word.EndsWith("ing") && word.Length>5) { string b=word.Substring(0,word.Length-3); result.Add(b+"e"); result.Add(b); if(b.Length>2 && b[b.Length-1]==b[b.Length-2]) result.Add(b.Substring(0,b.Length-1)); }
   if(word.EndsWith("ed") && word.Length>3) { string b=word.Substring(0,word.Length-2); result.Add(b); result.Add(b+"e"); if(b.Length>2 && b[b.Length-1]==b[b.Length-2])result.Add(b.Substring(0,b.Length-1)); }
   if(word.EndsWith("es") && word.Length>3) {result.Add(word.Substring(0,word.Length-1));result.Add(word.Substring(0,word.Length-2));}
   else if(word.EndsWith("s") && !word.EndsWith("ss") && word.Length>3) result.Add(word.Substring(0,word.Length-1));
   if(word.EndsWith("er") && word.Length>4) {result.Add(word.Substring(0,word.Length-2));result.Add(word.Substring(0,word.Length-1));}
   if(word.EndsWith("est") && word.Length>5) {result.Add(word.Substring(0,word.Length-3));result.Add(word.Substring(0,word.Length-2));}
   return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
  }
  public static string PosLabel(string pos)
  {
   switch(pos) {case "noun":return "noun · n. · 名词";case "verb":return "verb · v. · 动词";case "adjective":return "adjective · adj. · 形容词";case "adverb":return "adverb · adv. · 副词";case "phrasal verb":return "phrasal verb · 短语动词";case "noun phrase":return "noun phrase · 名词短语";case "preposition":return "preposition · prep. · 介词";case "conjunction":return "conjunction · conj. · 连词";case "pronoun":return "pronoun · pron. · 代词";default:return pos??"word";}
  }
  public static string CardPos(Card c)
  {
   if(!String.IsNullOrWhiteSpace(c.PartOfSpeech))return c.PartOfSpeech;
   string p=(c.Stress??"").Split('·').Last().Trim().ToLowerInvariant();
   if(c.Id=="w20")return "adjective";
   if(c.Id=="c01")return "noun";
   if(String.IsNullOrWhiteSpace(p))return "part of speech not set";
   return p.Split('/')[0].Split(';')[0].Trim()=="plural noun"?"noun":p.Split('/')[0].Split(';')[0].Trim()=="database term"?"noun":p.Split('/')[0].Split(';')[0].Trim();
  }
  public static WordEntry FromCard(Card c)
  {
   string pos=CardPos(c);
   var entry=new WordEntry{Word=c.Term,Ipa=c.Ipa,Forms=Forms(c.Term,pos),Source="Lilt · curated for your learning context",Senses=new List<WordSense>{new WordSense{PartOfSpeech=pos,Definition=c.Definition,Example=c.Example,MeaningZh=c.MeaningZh,ExampleZh=c.ExampleZh,Ipa=c.Ipa,Usage=c.Collocation}}};
   if(c.Id=="c01") entry.Senses[0].Definition="The state of an engine running while the vehicle is not moving.";
   if(c.Id=="w20") entry.Senses[0].Definition="Existing as an extra copy of something already present.";
   Action<string,string,string,string,string,string> add=(p,d,e,z,ez,ipa)=>entry.Senses.Add(new WordSense{PartOfSpeech=p,Definition=d,Example=e,MeaningZh=z,ExampleZh=ez,Ipa=ipa??c.Ipa,Usage=Forms(c.Term,p)});
   switch(c.Id)
   {
    case "w03":add("verb","To pair things that do not fit or agree.","Do not mismatch device IDs and meter addresses.","错误匹配；使不相配","不要将设备 ID 与电表地址错误匹配。","/ˌmɪsˈmætʃ/");break;
    case "w05":add("verb","To record or enter information in an official list or system.","Register the gateway before connecting the meters.","登记；注册","连接电表之前先注册网关。",null);break;
    case "w13":add("verb","To request information from a database or system.","Query the history table for readings from this device.","查询数据库或系统","查询历史表中这台设备的读数。",null);break;
    case "w17":add("verb","To create a structure that helps locate stored information.","Index the timestamp column to speed up time-based searches.","建立索引","为时间戳列建立索引，加快按时间搜索的速度。",null);break;
    case "w20":add("noun","An extra copy of an existing item.","This record is a duplicate of the previous entry.","重复项；副本","这条记录是上一条的重复项。",null);add("verb","To make an extra copy of something.","Duplicate the template before changing its addresses.","复制；使重复","修改地址前先复制模板。","/ˈduːplɪkeɪt/");break;
    case "w32":add("verb","To divide or move into a separate path.","The parser branches according to the protocol type.","分支；分成不同路径","解析器根据协议类型走向不同分支。",null);break;
    case "c01":add("verb","To run an engine without moving the vehicle.","Let the engine idle while you listen for an unusual noise.","怠速运转","让发动机怠速运转，同时留意异常声音。",null);break;
    case "c02":add("noun","An unexpected stop in the running of an engine.","The driver reported a stall at a traffic light.","熄火；发动机意外停止运转","驾驶员报告车辆在红灯处发生了一次熄火。",null);break;
    case "c03":add("noun","An occasion when an engine cylinder does not fire correctly.","A misfire made the engine shake at idle.","发动机失火；燃烧异常","一次失火导致发动机怠速抖动。",null);break;
    case "c07":add("verb","To limit power, speed, or the rate of an activity.","The server may throttle requests when traffic is high.","限制功率、速度或处理速率","流量较高时，服务器可能限制请求速率。",null);break;
    case "c16":add("verb","To escape accidentally through a hole or gap.","Coolant can leak through a damaged gasket.","泄漏；渗漏","冷却液可能从损坏的密封垫处泄漏。",null);break;
    case "c25":add("verb","To ask for a product to be returned so a fault can be fixed.","The manufacturer may recall cars with faulty fuel pumps.","召回产品","制造商可能召回燃油泵存在故障的汽车。","/rɪˈkɔl/");break;
    case "c32":add("verb","To vary between stated limits.","Battery temperatures range from cool to warm during charging.","在一定范围内变化","充电过程中，电池温度从偏凉变化到温热。",null);break;
    case "n03":add("verb","To break a small piece off a hard surface.","Loose stones can chip the paint on a car.","使崩掉一小块；使缺损","松散的石子可能崩掉车漆。",null);break;
    case "n09":add("verb","To estimate what is likely to happen.","Analysts forecast slower growth next year.","预测；预估","分析师预测明年的增长会放缓。",null);break;
    case "n10":add("verb","For market prices to rise, often after a fall.","Technology stocks rallied after the announcement.","行情上涨；反弹","公告发布后，科技股上涨。",null);break;
    case "n11":add("verb","To fall sharply in price, sales, or activity.","Chip sales slumped when customers cut spending.","大幅下跌；骤降","客户削减支出后，芯片销售大幅下降。",null);break;
    case "n14":add("verb","To measure performance against a standard.","Benchmark the model on tasks that users actually perform.","进行基准测试","用用户实际执行的任务对模型做基准测试。",null);break;
    case "n20":add("adjective","Related to running an organization or carrying out decisions.","The board discussed changes to executive pay.","管理层的；执行的","董事会讨论了高管薪酬的调整。",null);break;
    case "n22":add("verb","To ask firmly for something or require it.","The new model demands more computing power.","强烈要求；需要","新模型需要更多算力。",null);break;
    case "n25":add("verb","To increase suddenly and strongly.","Demand surged after the product launch.","激增；急剧上升","产品推出后，需求激增。",null);break;
   }
   return entry;
  }
  public static string Forms(string word,string pos)
  {
   if(pos=="verb" || pos=="phrasal verb")
   {
    var parts=word.Split(' '); string verb=parts[0],tail=parts.Length>1?" "+String.Join(" ",parts.Skip(1)):"";
    string third=verb.EndsWith("y") && verb.Length>1 && !"aeiou".Contains(verb[verb.Length-2])?verb.Substring(0,verb.Length-1)+"ies":Regex.IsMatch(verb,@"(s|sh|ch|x|z|o)$")?verb+"es":verb+"s";
    string past=verb=="wear"?"wore":verb=="troubleshoot"?"troubleshot":verb=="chip"?"chipped":verb=="forecast"?"forecast":verb.EndsWith("e")?verb+"d":verb.EndsWith("y") && !"aeiou".Contains(verb[verb.Length-2])?verb.Substring(0,verb.Length-1)+"ied":verb+"ed";
    string ing=verb=="chip"?"chipping":verb.EndsWith("e") && !verb.EndsWith("ee")?verb.Substring(0,verb.Length-1)+"ing":verb+"ing";
    return "3rd person: "+third+tail+" · past: "+past+tail+" · -ing: "+ing+tail+(verb=="wear"?" · past participle: worn"+tail:"");
   }
   if(pos=="noun" || pos=="noun phrase")
   {
    if(new[]{"latency","throughput","null","torque","traction","coolant","ignition","fuel efficiency","mileage","maintenance","handling","steering","horsepower","regenerative braking","training","earnings","revenue","guidance","volatility","inflation","investment","demand","braking","acceleration","byte order"}.Contains(word))return word=="earnings"?"Usually plural in this sense.":"Usually uncountable in this sense.";
    string[] p=word.Split(' '); string last=p[p.Length-1]; p[p.Length-1]=last.EndsWith("y") && last.Length>1 && !"aeiou".Contains(last[last.Length-2])?last.Substring(0,last.Length-1)+"ies":Regex.IsMatch(last,@"(s|sh|ch|x|z)$")?last+"es":last+"s";
    return "plural: "+String.Join(" ",p);
   }
   return "";
  }
  public WordEntry Find(string word,List<Card> cards)
  {
   var candidates=Candidates(word); foreach(string candidate in candidates) { var c=cards.FirstOrDefault(x=>x.Term.Equals(candidate,StringComparison.OrdinalIgnoreCase)); if(c!=null)return FromCard(c); }
   lock(cache) { WordEntry found; if(cache.TryGetValue(word,out found))return found; }
   WordEntry best=null; int bestRank=Int32.MaxValue;
   using(var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("dictionary.bin"))
   using(var reader=new BinaryReader(resource,Encoding.UTF8))
   {
    if(Encoding.ASCII.GetString(reader.ReadBytes(9))!="LILTDICT1")throw new InvalidDataException("Invalid dictionary index.");
    int count=reader.ReadInt32(); var chunks=new Dictionary<char,int[]>();
    for(int i=0;i<count;i++) { char letter=(char)reader.ReadByte();chunks[letter]=new[]{reader.ReadInt32(),reader.ReadInt32()}; }
    foreach(char first in candidates.Where(w=>w.Length>0).Select(w=>w[0]).Distinct())
    {
     int[] chunk; if(!chunks.TryGetValue(first,out chunk))continue;
     resource.Seek(chunk[0],SeekOrigin.Begin); byte[] data=reader.ReadBytes(chunk[1]);
     using(var memory=new MemoryStream(data))using(var gzip=new GZipStream(memory,CompressionMode.Decompress))using(var text=new StreamReader(gzip,Encoding.UTF8))
     {
      string line; while((line=text.ReadLine())!=null)
      {
       int tab=line.IndexOf('\t'); if(tab<0)continue; string key=line.Substring(0,tab); int rank=candidates.IndexOf(key);
       if(rank<0 || rank>=bestRank)continue;
       best=new JavaScriptSerializer().Deserialize<WordEntry>(line.Substring(tab+1)); bestRank=rank; if(rank==0)break;
      }
     }
     if(bestRank==0)break;
    }
   }
   if(best!=null) lock(cache) cache[word]=best;
   return best;
  }
  public WordEntry Online(string word)
  {
   if(!Regex.IsMatch(word??"",@"^[a-zA-Z][a-zA-Z'-]{0,79}$"))return null;
   ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
   var request=(HttpWebRequest)WebRequest.Create("https://api.dictionaryapi.dev/api/v2/entries/en/"+Uri.EscapeDataString(word)); request.Timeout=7000;request.ReadWriteTimeout=7000;request.UserAgent="LiltPocketEnglish/1.0";
   using(var response=request.GetResponse())using(var stream=response.GetResponseStream())using(var reader=new StreamReader(stream,Encoding.UTF8))
   {
    var data=new JavaScriptSerializer().DeserializeObject(reader.ReadToEnd()) as object[]; if(data==null || data.Length==0)return null;
    var first=data[0] as Dictionary<string,object>; var result=new WordEntry{Word=(string)first["word"],Source="Free Dictionary API / Wiktionary · dictionaryapi.dev",Senses=new List<WordSense>()};
    object licenseValue;if(first.TryGetValue("license",out licenseValue)){var license=licenseValue as Dictionary<string,object>;if(license!=null)result.Source+=" · "+license["name"]+" · "+license["url"];}
    object value; if(first.TryGetValue("phonetic",out value))result.Ipa=value as string;
    foreach(Dictionary<string,object> meaning in (object[])first["meanings"])
    {
     string pos=(string)meaning["partOfSpeech"]; foreach(Dictionary<string,object> definition in ((object[])meaning["definitions"]).Take(2)) { result.Senses.Add(new WordSense{PartOfSpeech=pos,Definition=(string)definition["definition"],Example=definition.TryGetValue("example",out value)?value as string:null}); }
    }
    if(result.Senses.Count>0) {result.Forms=Forms(result.Word,result.Senses[0].PartOfSpeech); lock(cache)cache[word]=result;return result;} return null;
   }
  }
 }
}
