using System;
using System.IO;
using UnityEngine;
using UnityEditor;

// Offline Unity synthesis: deterministic PCM assets, editable/replacable without runtime DSP.
public static class ProceduralAudioBuilder
{
    private struct Cue
    {
        public string name; public float duration, start, end, noise, gain; public int shape;
        public Cue(string n, float d, float f, float e, float h, float g, int s=0) { name=n;duration=d;start=f;end=e;noise=h;gain=g;shape=s; }
    }
    [MenuItem("Tools/Audio/Build Authored Game Cues")]
    public static void Build()
    {
        var cues = new[] {
            new Cue("UiHover",.065f,900,1100,.02f,.35f), new Cue("UiClick",.12f,650,980,.08f,.5f),
            new Cue("UiBack",.13f,650,370,.04f,.42f), new Cue("Door",.55f,150,65,.65f,.6f),
            new Cue("Pickup",.7f,520,1040,.02f,.52f,1), new Cue("Clue",1.1f,392,784,.015f,.45f,1),
            new Cue("Paper",.32f,220,160,.95f,.38f), new Cue("Shot",.24f,1050,180,.27f,.65f),
            new Cue("Purify",.65f,440,1320,.13f,.5f,1), new Cue("WrongHit",.32f,140,75,.42f,.55f),
            new Cue("Hurt",.28f,110,52,.5f,.65f), new Cue("Weakpoint",.9f,660,1320,.025f,.62f,1),
            new Cue("Victory",1.8f,392,784,.01f,.5f,1), new Cue("Defeat",1.5f,230,58,.18f,.55f),
            new Cue("Warning",.16f,740,620,.01f,.42f), new Cue("RiftOpen",2.3f,58,120,.62f,.6f,2),
            new Cue("RiftGrab",.6f,130,35,.55f,.7f), new Cue("RiftClose",1.5f,180,40,.65f,.5f,2),
            new Cue("Portal",1.3f,150,1000,.42f,.5f,2), new Cue("Land",.17f,90,45,.72f,.45f),
            new Cue("StepTile",.14f,210,75,.82f,.44f), new Cue("StepGrass",.18f,100,45,.95f,.36f),
            new Cue("Jump",.2f,200,390,.65f,.33f), new Cue("Slam",.65f,95,30,.72f,.7f),
            new Cue("AmbHospital",8,60,60,.85f,.18f,3), new Cue("AmbGarden",8,160,160,.94f,.16f,3),
            new Cue("Brand",1.8f,330,660,.01f,.5f,1)
        };
        const int rate=48000;
        const string directory="Assets/Resources/Audio/Generated";
        Directory.CreateDirectory(directory);
        foreach (var cue in cues)
        {
            int count=Mathf.RoundToInt(cue.duration*rate);
            var samples=new float[count];
            var random=new System.Random(193+Array.IndexOf(cues,cue));
            double phase=0; float filtered=0;
            for(int i=0;i<count;i++)
            {
                float t=i/(float)rate, p=i/(float)(count-1);
                float frequency=Mathf.Lerp(cue.start,cue.end,p);
                if(cue.shape==1) frequency=cue.start*new[]{1f,1.25f,1.5f,2f}[Mathf.Min(3,(int)(p*4))];
                phase+=2*Math.PI*frequency/rate;
                float raw=(float)(random.NextDouble()*2-1);
                filtered=Mathf.Lerp(filtered,raw,cue.shape==3?.025f:.22f);
                float tone=(float)(Math.Sin(phase)+.25*Math.Sin(phase*2.001))*.7f;
                float envelope=Mathf.Min(1,t/.012f)*Mathf.Pow(1-p,cue.shape==2?.7f:1.7f);
                if(cue.shape==3) envelope=.7f+.3f*Mathf.Sin(2*Mathf.PI*p);
                samples[i]=(tone*(1-cue.noise)+filtered*cue.noise)*envelope*cue.gain;
            }
            // Ambience loops meet at zero with a short equal-shaped seam fade.
            if(cue.shape==3) for(int i=0;i<2400;i++) {float k=i/2399f;samples[i]*=k;samples[count-1-i]*=k;}
            float peak=0;foreach(float sample in samples)peak=Mathf.Max(peak,Mathf.Abs(sample));
            if(peak>0)for(int i=0;i<count;i++)samples[i]*=cue.gain/peak;
            string path=directory+"/"+cue.name+".wav";
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
                writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                foreach(float sample in samples)writer.Write((short)(Mathf.Clamp(sample,-.95f,.95f)*32767));
            }
        }
        AssetDatabase.Refresh();
        foreach(var cue in cues)
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath(directory+"/"+cue.name+".wav");
            var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
            importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
        }
        Debug.Log("Authored 27 procedural audio cues.");
    }
}
