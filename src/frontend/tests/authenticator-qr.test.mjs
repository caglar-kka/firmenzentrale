import{test}from'node:test';import assert from'node:assert/strict';
import{qrMatrix,authenticatorUri}from'../node_modules/.cache/betriebsplattform-tests/AuthenticatorQr.js';
test('Lokaler QR: Byte-Dekodierung, TOTP-URI, Format und Reed-Solomon-Prüfsymbole',()=>{
 const secret='GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ',m=qrMatrix(secret);assert.equal(m.length,33);assert.equal(m[25][8],true);
 const uri=new URL(authenticatorUri(secret));assert.equal(uri.protocol,'otpauth:');assert.equal(uri.hostname,'totp');assert.equal(uri.searchParams.get('secret'),secret);assert.equal(uri.searchParams.get('issuer'),'MBOP');
 const formatPositions=[[8,0],[8,1],[8,2],[8,3],[8,4],[8,5],[8,7],[8,8],[7,8],[5,8],[4,8],[3,8],[2,8],[1,8],[0,8]];
 assert.equal(formatPositions.reduce((v,[x,y],i)=>v+(m[y][x]?2**i:0),0),0x77c4);
 const functional=(x,y)=>(x<=8&&y<=8)||(x>=25&&y<=8)||(x<=8&&y>=25)||x===6||y===6||(x>=24&&x<=28&&y>=24&&y<=28);
 const bits=[];let upwards=true;for(let col=32;col>0;col-=2){if(col===6)col--;for(let i=0;i<33;i++){const y=upwards?32-i:i;for(let x=col;x>=col-1;x--)if(!functional(x,y))bits.push(Number(m[y][x]!==((x+y)%2===0)));}upwards=!upwards;}
 assert.equal(bits.length,807);const read=(start,n)=>bits.slice(start,start+n).reduce((a,b)=>a*2+b,0);assert.equal(read(0,4),4);
 const length=read(4,8);assert.equal(String.fromCharCode(...Array.from({length},(_,i)=>read(12+8*i,8))),authenticatorUri(secret));
 const bytes=Array.from({length:100},(_,i)=>read(i*8,8));
 const multiply=(x,y)=>{let p=0;while(y){if(y%2)p^=x;y>>=1;x<<=1;if(x>=256)x^=285;}return p;};
 let alpha=1;for(let exponent=0;exponent<20;exponent++){assert.equal(bytes.reduce((v,b)=>multiply(v,alpha)^b,0),0);alpha=multiply(alpha,2);}
 assert.throws(()=>qrMatrix('invalid'));assert.throws(()=>authenticatorUri(secret+'?external=1'));
});
