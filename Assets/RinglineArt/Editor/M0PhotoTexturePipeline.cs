using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ringline.Art.Editor
{
    public static class M0PhotoTexturePipeline
    {
        const string Root = "ArtSource/Ringline/M0";
        const int Width = 32, WallHeight = 77, ReviewScale = 12;
        static readonly string[] Names = {"Wall_Basic", "Wall_Weathered", "Floor"};

        [MenuItem("Ringline/Art/Photo Pipeline/1 Prepare Downsamples")]
        public static void Prepare()
        {
            Directory.CreateDirectory(Root+"/Downsampled");
            Directory.CreateDirectory(Root+"/Review");
            foreach(var name in Names)
            {
                var source=Read(Root+"/PhotoSources/"+name+"_Source.png");
                int h=name=="Floor" ? Width : WallHeight;
                var reduced=BoxReduce(source,Width,h);
                Write(Root+"/Downsampled/"+name+".png",reduced);
                var preview=new Texture2D(Width*ReviewScale,h*ReviewScale,TextureFormat.RGB24,false);
                var pixels=reduced.GetPixels32();var zoom=new Color32[preview.width*preview.height];
                for(int y=0;y<preview.height;y++)for(int x=0;x<preview.width;x++)zoom[y*preview.width+x]=pixels[(y/ReviewScale)*Width+x/ReviewScale];
                preview.SetPixels32(zoom);preview.Apply();
                Write(Root+"/Review/"+name+"_Nearest12x.png",preview);
                Debug.Log($"PHOTO_DOWNSAMPLE {name}: {source.width}x{source.height} -> {Width}x{h}, area box filter.");
                Object.DestroyImmediate(source);Object.DestroyImmediate(reduced);Object.DestroyImmediate(preview);
            }
        }

        [MenuItem("Ringline/Art/Photo Pipeline/2 Apply Local Retouch And Pack")]
        public static void Complete()
        {
            Directory.CreateDirectory(Root+"/Retouched");
            Directory.CreateDirectory(Root+"/RetouchMasks");
            Directory.CreateDirectory(Root+"/FinalAtlases");
            var report=new System.Text.StringBuilder("Photorealistic generated source -> area-box downsample -> local retouch -> UV atlas.\n");
            foreach(var name in Names)
            {
                var reduced=Read(Root+"/Downsampled/"+name+".png");
                int h=name=="Floor" ? Width : WallHeight;
                if(reduced.width!=Width||reduced.height!=h)throw new InvalidOperationException("Run Prepare at the current resolution first.");
                var original=reduced.GetPixels32();var result=(Color32[])original.Clone();
                var mask=new Color32[Width*h];for(int i=0;i<mask.Length;i++)mask[i]=new Color32(0,0,0,255);
                var regions=name=="Floor"
                    ? new[]{new RectInt(10,10,1,1),new RectInt(21,10,1,1),new RectInt(10,21,1,1),new RectInt(21,21,1,1)}
                    : name=="Wall_Weathered"
                        ? new[]{new RectInt(9,54,2,2),new RectInt(20,54,2,2),new RectInt(10,65,1,1),new RectInt(21,65,1,1)}
                        : Array.Empty<RectInt>();
                if(regions.Length>0)
                {
                    var candidate=Read(Root+"/RetouchCandidates/"+name+".png");
                    var small=BoxReduce(candidate,Width,h);var edited=small.GetPixels32();
                    foreach(var region in regions)for(int y=region.y;y<region.yMax;y++)for(int x=region.x;x<region.xMax;x++)
                    {
                        // Regions are specified from the visible image's top-left.
                        int i=(h-1-y)*Width+x;
                        // Match existing soft texels. Never redraw or darken a grid.
                        byte Mix(byte before,byte after)=>(byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(before,after,.5f)),Math.Max(0,before-6),Math.Min(255,before+6));
                        result[i]=new Color32(Mix(original[i].r,edited[i].r),Mix(original[i].g,edited[i].g),Mix(original[i].b,edited[i].b),255);
                        mask[i]=new Color32(255,255,255,255);
                    }
                    Object.DestroyImmediate(candidate);Object.DestroyImmediate(small);
                }
                int changed=0,outside=0,maxDelta=0;
                for(int i=0;i<result.Length;i++)
                {
                    int delta=Math.Max(Math.Abs(result[i].r-original[i].r),Math.Max(Math.Abs(result[i].g-original[i].g),Math.Abs(result[i].b-original[i].b)));
                    if(delta>0){changed++;if(mask[i].r==0)outside++;}maxDelta=Math.Max(maxDelta,delta);
                }
                if(outside!=0 || changed>result.Length*.02 || maxDelta>6)throw new InvalidOperationException("Retouch exceeded its local scope.");
                reduced.SetPixels32(result);reduced.Apply();Write(Root+"/Retouched/"+name+".png",reduced);
                var maskTexture=new Texture2D(Width,h,TextureFormat.RGB24,false);maskTexture.SetPixels32(mask);maskTexture.Apply();
                Write(Root+"/RetouchMasks/"+name+".png",maskTexture);Object.DestroyImmediate(maskTexture);
                var atlas=Pack(reduced,name=="Floor");
                Write(Root+"/FinalAtlases/"+(name=="Floor" ? "T_Floor" : "T_"+name)+".png",atlas);
                if(name=="Wall_Weathered")Write(Root+"/FinalAtlases/T_Wall_Weathered_Bilinear.png",atlas);
                report.AppendLine($"{name}: {Width}x{h}; changed={changed}/{result.Length} ({100f*changed/result.Length:F3}%); changedOutsideMask={outside}; maximumChannelDelta={maxDelta}/255.");
                Object.DestroyImmediate(reduced);Object.DestroyImmediate(atlas);
            }
            Directory.CreateDirectory("Docs/Art/Evidence");
            File.WriteAllText("Docs/Art/Evidence/M0_PhotoTextureValidation.txt",report.ToString().Replace("\r\n","\n"));
            Debug.Log(report.ToString());
        }

        static Texture2D Pack(Texture2D surface,bool floor)
        {
            int aw=64,ah=floor ? 64 : 128;
            var pixels=new Color32[aw*ah];for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(128,128,116,255);
            var src=surface.GetPixels32();
            void Island(int x0,int y0,int w,int h,int sourceX,int sourceY)
            {
                // All stored detail is copied from the reduced photograph.
                for(int y=-4;y<h+4;y++)for(int x=-4;x<w+4;x++)
                {
                    int dx=x0+x,dy=y0+y;if(dx<0||dx>=aw||dy<0||dy>=ah)continue;
                    pixels[dy*aw+dx]=src[(sourceY+Mathf.Clamp(y,0,h-1))*surface.width+sourceX+Mathf.Clamp(x,0,w-1)];
                }
            }
            Island(4,4,Width,surface.height,0,0);
            if(!floor){Island(44,4,5,WallHeight,14,0);Island(4,96,Width,5,0,WallHeight-5);}
            var atlas=new Texture2D(aw,ah,TextureFormat.RGB24,false);atlas.SetPixels32(pixels);atlas.Apply();return atlas;
        }

        static Texture2D Read(string path)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGB24,false);
            if(!texture.LoadImage(File.ReadAllBytes(path)))throw new InvalidOperationException("Cannot decode "+path);
            return texture;
        }

        static void Write(string path,Texture2D texture)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path,texture.EncodeToPNG());
        }

        // Area-weighted box filtering deliberately merges photographic microdetail.
        // No synthetic pixel-art pattern, grain or sharpening is added.
        static Texture2D BoxReduce(Texture2D source,int width,int height)
        {
            var pixels=source.GetPixels32();var result=new Color32[width*height];
            double sx=(double)source.width/width,sy=(double)source.height/height;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                double x0=x*sx,x1=(x+1)*sx,y0=y*sy,y1=(y+1)*sy;
                double r=0,g=0,b=0,total=0;
                for(int yy=(int)y0;yy<Math.Ceiling(y1)&&yy<source.height;yy++)
                    for(int xx=(int)x0;xx<Math.Ceiling(x1)&&xx<source.width;xx++)
                    {
                        double weight=(Math.Min(x1,xx+1)-Math.Max(x0,xx))*(Math.Min(y1,yy+1)-Math.Max(y0,yy));
                        var p=pixels[yy*source.width+xx];r+=p.r*weight;g+=p.g*weight;b+=p.b*weight;total+=weight;
                    }
                result[y*width+x]=new Color32((byte)Math.Round(r/total),(byte)Math.Round(g/total),(byte)Math.Round(b/total),255);
            }
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.SetPixels32(result);texture.Apply();return texture;
        }
    }
}
