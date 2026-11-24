// Offline dictionary packaging. Inputs are the upstream snapshots described in THIRD_PARTY_NOTICES.md.
const fs=require('fs'),path=require('path'),zlib=require('zlib');
const root=path.resolve(__dirname,'..'),source=path.join(root,'validation','dictionary-source');
const words=JSON.parse(zlib.gunzipSync(fs.readFileSync(path.join(source,'wordset.json.gz'))));
const pronunciations=new Map();
const britishRaw=new Map();
for(const line of fs.readFileSync(path.join(source,'en_UK.txt'),'utf8').split(/\r?\n/)){const p=line.split('\t');if(p.length===2)britishRaw.set(p[0].toLowerCase(),p[1].split(',')[0].trim());}
const britishCurated=new Map(fs.readFileSync(path.join(root,'vocabulary.tsv'),'utf8').trimEnd().split(/\r?\n/).slice(1).map(line=>{const p=line.split('\t');return[p[2].toLowerCase(),p[10]];}));
function britishIpa(word){
 if(britishCurated.has(word))return britishCurated.get(word);
 let raw=britishRaw.get(word);if(!raw)return '';
 raw=raw.replace(/^\/|\/$/g,'').replace(/ɛ/g,'e').replace(/ɹ/g,'r').replace(/ɐ/g,'ə');
 const tokens=raw.match(/tʃ|dʒ|[aeɔəɪʊo][ɪʊə]|[ɑɔɜiu]ː|./gu)||[],sounds=[],vowels=[],stress=[];let mark='';
 const isVowel=p=>/^(?:aɪ|aʊ|eɪ|ɔɪ|əʊ|oʊ|ɪə|eə|ʊə|[ɑɔɜiu]ː|[ɪeæʌəʊɒiua])$/.test(p);
 for(const p of tokens){if(p==='ˈ'||p==='ˌ'){mark=p;continue;}if(p==='.')continue;sounds.push(p);if(isVowel(p)){vowels.push(sounds.length-1);stress.push(mark);mark='';}}
 const legal=new Set(['pr','br','tr','dr','kr','gr','fr','θr','ʃr','sp','st','sk','spl','spr','str','skr','skw','tw','dw','kw','gw','pl','bl','kl','gl','fl','sl','sw']);
 const boundaries=new Map();for(let n=0;n<vowels.length;n++){const i=vowels[n],prev=n?vowels[n-1]:-1;let start=i;if(prev<0)start=0;else if(i>prev+1){start=i-1;for(let len=3;len>=2;len--)if(i-len>prev&&legal.has(sounds.slice(i-len,i).join(''))){start=i-len;break;}if(stress[n-1]&&/^[ɪeæʌʊɒ]$/.test(sounds[prev]))start=Math.max(start,prev+2);}if(stress[n])boundaries.set(start,stress[n]);else if(n)boundaries.set(start,'.');}
 return '/'+sounds.map((p,i)=>(boundaries.get(i)||'')+p).join('')+'/';
}
for(const line of fs.readFileSync(path.join(source,'cmudict.dict'),'utf8').split(/\r?\n/)){
 const match=line.match(/^([a-z'-]+)(?:\(\d+\))?\s+(.+?)(?:\s+#.*)?$/);if(match&&!pronunciations.has(match[1]))pronunciations.set(match[1],match[2].trim().split(/\s+/));
}
const phonemes={AA:'ɑː',AE:'æ',AH:'ʌ',AO:'ɔː',AW:'aʊ',AY:'aɪ',B:'b',CH:'tʃ',D:'d',DH:'ð',EH:'e',ER:'ɝː',EY:'eɪ',F:'f',G:'ɡ',HH:'h',IH:'ɪ',IY:'iː',JH:'dʒ',K:'k',L:'l',M:'m',N:'n',NG:'ŋ',OW:'oʊ',OY:'ɔɪ',P:'p',R:'r',S:'s',SH:'ʃ',T:'t',TH:'θ',UH:'ʊ',UW:'uː',V:'v',W:'w',Y:'j',Z:'z',ZH:'ʒ'};
const onsets=new Set(['P R','B R','T R','D R','K R','G R','F R','TH R','SH R','S P','S T','S K','S P L','S P R','S T R','S K R','S K W','T W','D W','K W','G W','P L','B L','K L','G L','F L','S L','S W']);
function ipa(word){
 const parts=pronunciations.get(word);if(!parts){if(word.includes(' ')){const p=word.split(' ').map(ipa);if(p.every(Boolean))return '/'+p.map(x=>x.slice(1,-1)).join(' ')+'/';}return '';}
 const marks=new Map();let previousVowel=-1;
 for(let i=0;i<parts.length;i++)if(/[012]$/.test(parts[i])){const stress=parts[i].slice(-1);let start=i;if(previousVowel<0)start=0;else if(i>previousVowel+1){start=i-1;for(let n=3;n>=2;n--)if(i-n>previousVowel&&onsets.has(parts.slice(i-n,i).join(' '))){start=i-n;break;}if(/^(AE|EH|IH|AH|UH)[12]$/.test(parts[previousVowel]))start=Math.max(start,previousVowel+2);}if(stress!=='0')marks.set(start,stress==='1'?'ˈ':'ˌ');else if(previousVowel>=0)marks.set(start,'.');previousVowel=i;}
 return '/'+parts.map((p,i)=>(marks.get(i)||'')+(p==='AH0'?'ə':p==='ER0'?'ɚ':phonemes[p.replace(/[012]$/,'')]||'')).join('')+'/';
}
const entries=new Map();
for(const item of Object.values(words)){
 const word=item.word.toLowerCase();if(!/^[a-z]+(?:[ '-][a-z]+)*$/.test(word)||word.length>80||entries.has(word))continue;
 const counts={},senses=[];
 for(const meaning of item.meanings||[]){const p=meaning.speech_part==='adjective satellite'?'adjective':meaning.speech_part||'word';if((counts[p]||0)>=2)continue;counts[p]=(counts[p]||0)+1;senses.push({PartOfSpeech:p,Definition:meaning.def,Example:meaning.example||'',Usage:(meaning.synonyms||[]).slice(0,4).join(' · ')});}
 if(senses.length)entries.set(word,{Word:word,Ipa:ipa(word),BritishIpa:britishIpa(word),Senses:senses,Source:'Wordset / WordNet · CC BY-SA 4.0; US: CMUdict; UK: IPA-dict'});
}
const overrides={mqtt:['noun','A lightweight messaging protocol often used to send sensor data.','The gateway publishes readings over MQTT.'],api:['noun','An interface that lets programs request data or use a service.','The API returns the latest meter reading.'],json:['noun','A text format for storing and exchanging structured data.','The gateway configuration is stored as JSON.'],mysql:['noun','A relational database management system.','The platform stores account information in MySQL.'],gitlab:['noun','A platform for hosting code repositories and collaborating on software.','The team reviews code changes in GitLab.'],parser:['noun','A program component that reads data and identifies its structure.','The parser extracts a pressure value from the payload.'],timestamp:['noun','A stored value showing when something happened.','The timestamp shows when the reading was collected.'],settings:['noun','Values that control how a program or device behaves.','Check the settings before restarting the gateway.'],data:['noun','Information that a computer can store, process or send.','The gateway sends data to the platform.'],lilt:['noun','A light, pleasant rhythm in speech or music.','There was a gentle lilt in her voice.'],ipa:['noun','The International Phonetic Alphabet, a set of symbols for speech sounds.','IPA helps you connect a word with its pronunciation.'],dashboard:['noun','A screen that shows important information in one place.','The dashboard shows the station pressure.'],offline:['adjective','Working without an internet connection.','The offline dictionary works at your desk.']};
for(const [word,[pos,definition,example]]of Object.entries(overrides))entries.set(word,{Word:word,Ipa:ipa(word),BritishIpa:britishIpa(word),Senses:[{PartOfSpeech:pos,Definition:definition,Example:example}],Source:'Lilt · technical and interface vocabulary; UK: IPA-dict'});
const groups=new Map();for(const[word,entry]of entries){const letter=word[0];if(!groups.has(letter))groups.set(letter,[]);groups.get(letter).push(word+'\t'+JSON.stringify(entry));}
const chunks=[...groups].sort(([a],[b])=>a.localeCompare(b)).map(([letter,lines])=>({letter,bytes:zlib.gzipSync(Buffer.from(lines.sort().join('\n')+'\n'),{level:9})}));
const header=Buffer.alloc(13+chunks.length*9);header.write('LILTDICT1',0,'ascii');header.writeUInt32LE(chunks.length,9);let offset=header.length;
chunks.forEach((chunk,i)=>{let p=13+i*9;header.writeUInt8(chunk.letter.charCodeAt(0),p);header.writeUInt32LE(offset,p+1);header.writeUInt32LE(chunk.bytes.length,p+5);offset+=chunk.bytes.length;});
const bundle=Buffer.concat([header,...chunks.map(c=>c.bytes)]),split=512*1024;
for(let offset=0,index=1;offset<bundle.length;offset+=split,index++)fs.writeFileSync(path.join(root,'assets',`dictionary-chunk-${String(index).padStart(2,'0')}.bin`),bundle.subarray(offset,offset+split));
console.log(JSON.stringify({entries:entries.size,compressedBytes:offset,letters:chunks.length}));
