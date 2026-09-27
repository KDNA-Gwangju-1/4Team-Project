$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
public static class BossPixelPacking {
    static void CleanCell(Bitmap b, Rectangle cell) {
        int w=cell.Width,h=cell.Height;
        bool[] seen=new bool[w*h];
        var largest=new System.Collections.Generic.List<int>();
        for(int sy=0;sy<h;sy++) for(int sx=0;sx<w;sx++) {
            int start=sy*w+sx;
            if(seen[start] || b.GetPixel(cell.X+sx,cell.Y+sy).A<128) continue;
            var part=new System.Collections.Generic.List<int>();part.Add(start);seen[start]=true;
            for(int i=0;i<part.Count;i++) {
                int x=part[i]%w,y=part[i]/w;
                for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++) {
                    int nx=x+dx,ny=y+dy;
                    if(nx<0||ny<0||nx>=w||ny>=h) continue;
                    int k=ny*w+nx;
                    if(seen[k] || b.GetPixel(cell.X+nx,cell.Y+ny).A<128) continue;
                    seen[k]=true;part.Add(k);
                }
            }
            if(part.Count>largest.Count) largest=part;
        }
        Array.Clear(seen,0,seen.Length);
        foreach(int k in largest) seen[k]=true;
        for(int y=0;y<h;y++) for(int x=0;x<w;x++)
            if(!seen[y*w+x]) b.SetPixel(cell.X+x,cell.Y+y,Color.Transparent);
    }
    static Rectangle Bounds(Bitmap b, Rectangle cell) {
        int l=cell.Right,t=cell.Bottom,r=cell.Left,d=cell.Top;
        for(int y=cell.Top;y<cell.Bottom;y++) for(int x=cell.Left;x<cell.Right;x++) {
            if(b.GetPixel(x,y).A<128) continue;
            l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);d=Math.Max(d,y);
        }
        return Rectangle.FromLTRB(l,t,r+1,d+1);
    }
    public static void Run(string source,string reference,string output,string preview) {
        using(var art=new Bitmap(source)) using(var old=new Bitmap(reference))
        using(var atlas=new Bitmap(8192,1024,PixelFormat.Format32bppArgb))
        using(var contact=new Bitmap(1024,512,PixelFormat.Format32bppArgb)) {
            for(int frame=0;frame<8;frame++) {
                int col=frame%4,row=frame/4;
                var cell=Rectangle.FromLTRB(col*art.Width/4,row*art.Height/2,(col+1)*art.Width/4,(row+1)*art.Height/2);
                CleanCell(art,cell);
                var src=Bounds(art,cell);
                var dst=Bounds(old,new Rectangle(frame*512,0,512,512));
                int left=(dst.Left-frame*512)/2,top=dst.Top/2;
                int w=(dst.Width+1)/2,h=(dst.Height+1)/2;
                for(int y=0;y<h;y++) for(int x=0;x<w;x++) {
                    var c=art.GetPixel(src.Left+Math.Min(src.Width-1,(int)((x+0.5)*src.Width/w)),src.Top+Math.Min(src.Height-1,(int)((y+0.5)*src.Height/h)));
                    if(c.A<128) continue;
                    // Snap the generated art to a 256px grid and an exact colour
                    // lattice; expand each logical pixel into a uniform 4x4 block.
                    c=Color.FromArgb(255,Math.Min(255,((c.R+8)/17)*17),Math.Min(255,((c.G+8)/17)*17),Math.Min(255,((c.B+8)/17)*17));
                    int px=left+x,py=top+y;
                    contact.SetPixel(col*256+px,row*256+py,c);
                    for(int dy=0;dy<4;dy++) for(int dx=0;dx<4;dx++) atlas.SetPixel(frame*1024+px*4+dx,py*4+dy,c);
                }
            }
            atlas.Save(output,ImageFormat.Png); contact.Save(preview,ImageFormat.Png);
        }
    }
}
'@
$project = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
[BossPixelPacking]::Run("$project/ArtSource/Chapter2BossPolish/boss-phase2-imagegen.png", "$project/ArtSource/Chapter2BossPolish/boss-phase2-original.png", "$project/Assets/Sprites/2d_sprites/Boss/boss-phase2-claw-8f.png", "$project/ArtSource/Chapter2BossPolish/pixel-contact-sheet.png")

