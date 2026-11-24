const fs=require('fs'),path=require('path');const file=path.join(__dirname,'..','vocabulary.tsv');
const different={
w01:'/kənˌfɪɡ.əˈreɪ.ʃən/',w04:'/ˈθreʃ.həʊld/',w05:'/ˈredʒ.ɪ.stə/',w06:'/ˈpəʊ.lɪŋ ˈɪn.tə.vəl/',
w07:'/baɪt ˈɔː.də/',w08:'/ˈpeɪ.ləʊd/',w13:'/ˈkwɪə.ri/',w14:'/krɪˈden.ʃəl/',w15:'/pəˈmɪʃ.ən/',
w20:'/ˈdjuː.plɪ.kət/',w24:'/ˌriː.prəˈdjuːs/',w30:'/ˈrəʊl.bæk/',w32:'/brɑːntʃ/',
w33:'/mɜːdʒ ˈkɒn.flɪkt/',w34:'/riːˈfæk.tə/',w37:'/ˈbɒt.əl.nek/',w40:'/ˌɪn.təˈmɪt.ənt/',
c03:'/ˌmɪsˈfaɪə/',c04:'/tɔːk/',c07:'/ˈθrɒt.əl/',c15:'/ˌəʊ.vəˈhiːt/',c18:'/spɑːk plʌɡ/',
c20:'/ˈbæt.ər.i/',c21:'/ˈɔːl.tə.neɪ.tə/',c24:'/weər aʊt/',c26:'/ˈwɒr.ən.ti/',c29:'/ˈstɪə.rɪŋ/',
c31:'/ˈhɔːs.paʊə/',c33:'/rɪˈdʒen.ər.ə.tɪv ˈbreɪ.kɪŋ/',c34:'/blaɪnd spɒt/',
n01:'/ˈɪn.fər.əns/',n04:'/ˌsem.i.kənˈdʌk.tə/',n05:'/ˈɜː.nɪŋz/',n06:'/ˈrev.ən.juː/',
n07:'/ˈprɒf.ɪt ˈmɑː.dʒɪn/',n09:'/ˈfɔː.kɑːst/',n12:'/ˌvɒl.əˈtɪl.ə.ti/',n14:'/ˈbentʃ.mɑːk/',
n18:'/ˈek.spɔːt rɪˈstrɪk.ʃən/',n22:'/dɪˈmɑːnd/',n23:'/ˈrəʊl.aʊt/',n24:'/ˈtreɪd.ɒf/',n25:'/sɜːdʒ/'
};
const same=new Set(('w02 w03 w09 w10 w11 w12 w16 w17 w18 w19 w21 w22 w23 w25 w26 w27 w28 w29 w31 w35 w36 w38 w39 c01 c02 c05 c06 c08 c09 c10 c11 c12 c13 c14 c16 c17 c19 c22 c23 c25 c27 c28 c30 c32 c35 n02 n03 n08 n10 n11 n13 n15 n16 n17 n19 n20 n21').split(' '));
const lines=fs.readFileSync(file,'utf8').trimEnd().split(/\r?\n/);let count=0;
if(!lines[0].includes('britishIpa'))lines[0]+='\tbritishIpa';
for(let i=1;i<lines.length;i++){const p=lines[i].split('\t');if(!different[p[0]]&&!same.has(p[0]))throw new Error('British IPA not reviewed: '+p[0]);p[10]=different[p[0]]||p[3];lines[i]=p.join('\t');count++;}
if(count!==100)throw new Error('Expected all 100 British transcriptions');fs.writeFileSync(file,lines.join('\n')+'\n');console.log('Added reviewed British IPA for '+count+' learning cards.');
