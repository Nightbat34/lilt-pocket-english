// Curated broad American learning transcriptions. A stress mark already marks a syllable boundary.
const fs=require('fs'),path=require('path');
const file=path.join(__dirname,'..','vocabulary.tsv');
const ipa={
w01:'/kənˌfɪɡ.jəˈreɪ.ʃən/',w02:'/ˈɡeɪt.weɪ/',w03:'/ˈmɪs.mætʃ/',w04:'/ˈθreʃ.hoʊld/',
w05:'/ˈredʒ.ɪ.stɚ/',w06:'/ˈpoʊ.lɪŋ ˈɪn.tɚ.vəl/',w07:'/baɪt ˈɔːr.dɚ/',w08:'/ˈpeɪ.loʊd/',
w09:'/ˈtʃek.sʌm/',w10:'/ˈtaɪm.aʊt/',w11:'/ˈleɪ.tən.si/',w12:'/ˈθruː.pʊt/',w13:'/ˈkwɪr.i/',
w14:'/krəˈden.ʃəl/',w15:'/pɚˈmɪʃ.ən/',w16:'/ˈskiː.mə/',w17:'/ˈɪn.deks/',w18:'/ˈtaɪm.stæmp/',
w19:'/nʌl/',w20:'/ˈduː.plə.kət/',w21:'/steɪl/',w22:'/ˈmæp.ɪŋ/',w23:'/ˈrek.ən.saɪl/',
w24:'/ˌriː.prəˈduːs/',w25:'/ˈver.ɪ.faɪ/',w26:'/kənˈstreɪnt/',w27:'/əˈsʌmp.ʃən/',
w28:'/ˈfɔːl.bæk/',w29:'/rɪˈɡreʃ.ən/',w30:'/ˈroʊl.bæk/',w31:'/dɪˈplɔɪ/',w32:'/bræntʃ/',
w33:'/mɝːdʒ ˈkɑːn.flɪkt/',w34:'/riːˈfæk.tɚ/',w35:'/ˈdep.rə.keɪ.tɪd/',w36:'/dɪˈpen.dən.si/',
w37:'/ˈbɑː.təl.nek/',w38:'/ˈtrʌb.əl.ʃuːt/',w39:'/ruːt kɔːz/',w40:'/ˌɪn.tɚˈmɪt.ənt/',
c01:'/ˈaɪ.dəl/',c02:'/stɔːl/',c03:'/ˌmɪsˈfaɪr/',c04:'/tɔːrk/',c05:'/trænzˈmɪʃ.ən/',c06:'/klʌtʃ/',
c07:'/ˈθrɑː.təl/',c08:'/əkˌsel.əˈreɪ.ʃən/',c09:'/ˈbreɪ.kɪŋ/',c10:'/ˈtræk.ʃən/',c11:'/səˈspen.ʃən/',
c12:'/əˈlaɪn.mənt/',c13:'/vaɪˈbreɪ.ʃən/',c14:'/ˈkuː.lənt/',c15:'/ˌoʊ.vɚˈhiːt/',c16:'/liːk/',
c17:'/ˈɡæs.kɪt/',c18:'/spɑːrk plʌɡ/',c19:'/ɪɡˈnɪʃ.ən/',c20:'/ˈbæt.ɚ.i/',c21:'/ˈɔːl.tɚ.neɪ.tɚ/',
c22:'/ˈfjuː.əl ɪˈfɪʃ.ən.si/',c23:'/ˈmaɪ.lɪdʒ/',c24:'/wer aʊt/',c25:'/ˈriː.kɔːl/',c26:'/ˈwɔːr.ən.ti/',
c27:'/ˈmeɪn.tən.əns/',c28:'/ˈhænd.lɪŋ/',c29:'/ˈstɪr.ɪŋ/',c30:'/leɪn/',c31:'/ˈhɔːrs.paʊ.ɚ/',
c32:'/reɪndʒ/',c33:'/rɪˈdʒen.ɚ.ə.tɪv ˈbreɪ.kɪŋ/',c34:'/blaɪnd spɑːt/',c35:'/ruːl aʊt/',
n01:'/ˈɪn.fɚ.əns/',n02:'/ˈtreɪ.nɪŋ/',n03:'/tʃɪp/',n04:'/ˌsem.i.kənˈdʌk.tɚ/',n05:'/ˈɝː.nɪŋz/',
n06:'/ˈrev.ə.nuː/',n07:'/ˈprɑː.fɪt ˈmɑːr.dʒɪn/',n08:'/ˈɡaɪ.dəns/',n09:'/ˈfɔːr.kæst/',n10:'/ˈræl.i/',
n11:'/slʌmp/',n12:'/ˌvɑː.ləˈtɪl.ə.ti/',n13:'/ˌvæl.juˈeɪ.ʃən/',n14:'/ˈbentʃ.mɑːrk/',
n15:'/ˈɪn.trəst reɪt/',n16:'/ɪnˈfleɪ.ʃən/',n17:'/ˈtær.ɪf/',n18:'/ˈek.spɔːrt rɪˈstrɪk.ʃən/',
n19:'/ˌreɡ.jəˈleɪ.ʃən/',n20:'/ɪɡˈzek.jə.tɪv/',n21:'/ɪnˈvest.mənt/',n22:'/dɪˈmænd/',
n23:'/ˈroʊl.aʊt/',n24:'/ˈtreɪd.ɔːf/',n25:'/sɝːdʒ/'
};
const lines=fs.readFileSync(file,'utf8').trimEnd().split(/\r?\n/),seen=new Set();
for(let i=1;i<lines.length;i++){const fields=lines[i].split('\t');if(!ipa[fields[0]])throw new Error('Missing IPA: '+fields[0]);seen.add(fields[0]);fields[3]=ipa[fields[0]];lines[i]=fields.join('\t');}
if(seen.size!==100||Object.keys(ipa).length!==100)throw new Error('IPA coverage mismatch');
fs.writeFileSync(file,lines.join('\n')+'\n');
console.log('Updated all 100 learning transcriptions; threshold: '+ipa.w04);
