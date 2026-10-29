// Fixed QR version 4, byte mode, error correction L, mask 0. No network or storage.
export function authenticatorUri(secret:string){
 if(!/^[A-Z2-7]{32}$/.test(secret))throw new Error('Ungültiger Einrichtungsschlüssel.');
 return `otpauth://totp/MBOP?secret=${secret}&issuer=MBOP`;
}
export function qrMatrix(secret:string):boolean[][]{
 const value=authenticatorUri(secret),bits:number[]=[];
 const append=(v:number,n:number)=>{for(let i=n-1;i>=0;i--)bits.push((v>>>i)&1);};
 append(4,4);append(value.length,8);for(const ch of value)append(ch.charCodeAt(0),8);
 append(0,Math.min(4,640-bits.length));while(bits.length%8)bits.push(0);
 const data:number[]=[];for(let i=0;i<bits.length;i+=8)data.push(bits.slice(i,i+8).reduce((a,b)=>(a<<1)|b,0));
 for(let pad=0;data.length<80;pad++)data.push(pad%2?0x11:0xec);
 const mul=(a:number,b:number)=>{let r=0;for(let i=0;i<8;i++){if(b&1)r^=a;b>>>=1;a=(a<<1)^((a&128)?0x11d:0);}return r;};
 let generator=[1],power=1;
 for(let i=0;i<20;i++){const next=Array<number>(generator.length+1).fill(0);for(let j=0;j<generator.length;j++){next[j]=next[j]!^generator[j]!;next[j+1]=next[j+1]!^mul(generator[j]!,power);}generator=next;power=mul(power,2);}
 const remainder=[...data,...Array<number>(20).fill(0)];
 for(let i=0;i<80;i++){const factor=remainder[i]!;for(let j=0;j<generator.length;j++)remainder[i+j]=remainder[i+j]!^mul(generator[j]!,factor);}
 const words=[...data,...remainder.slice(80)],size=33;
 const matrix=Array.from({length:size},()=>Array<boolean>(size).fill(false)),reserved=Array.from({length:size},()=>Array<boolean>(size).fill(false));
 const set=(x:number,y:number,v:boolean)=>{if(x>=0&&x<size&&y>=0&&y<size){matrix[y]![x]=v;reserved[y]![x]=true;}};
 for(let i=0;i<size;i++){set(6,i,i%2===0);set(i,6,i%2===0);}
 for(const[cx,cy]of[[3,3],[29,3],[3,29]])for(let y=-4;y<=4;y++)for(let x=-4;x<=4;x++){const d=Math.max(Math.abs(x),Math.abs(y));set(cx!+x,cy!+y,d!==2&&d!==4);}
 for(let y=-2;y<=2;y++)for(let x=-2;x<=2;x++)set(26+x,26+y,Math.max(Math.abs(x),Math.abs(y))!==1);
 const format=0x77c4,bit=(i:number)=>((format>>>i)&1)!==0;
 for(let i=0;i<6;i++)set(8,i,bit(i));set(8,7,bit(6));set(8,8,bit(7));set(7,8,bit(8));
 for(let i=9;i<15;i++)set(14-i,8,bit(i));for(let i=0;i<8;i++)set(size-1-i,8,bit(i));for(let i=8;i<15;i++)set(8,size-15+i,bit(i));set(8,size-8,true);
 let pos=0,up=true;
 for(let right=size-1;right>=1;right-=2){if(right===6)right=5;for(let row=0;row<size;row++){const y=up?size-1-row:row;for(let dx=0;dx<2;dx++){const x=right-dx;if(reserved[y]![x])continue;const v=pos<800?((words[pos>>>3]!>>>(7-(pos&7)))&1)!==0:false;matrix[y]![x]=v!==((x+y)%2===0);pos++;}}up=!up;}
 return matrix;
}
export default function AuthenticatorQr({secret}:{secret:string}){
 const matrix=qrMatrix(secret),path=matrix.flatMap((row,y)=>row.flatMap((v,x)=>v?[`M${x+4},${y+4}h1v1h-1z`]:[])).join('');
 return <svg role="img" aria-label="QR-Code für die Authenticator-Einrichtung" viewBox="0 0 41 41" width="246" height="246" className="p-authenticator-qr" shapeRendering="crispEdges"><rect width="41" height="41" fill="white"/><path d={path} fill="black"/></svg>;
}
