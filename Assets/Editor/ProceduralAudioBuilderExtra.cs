using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Second batch of offline-synthesised audio, alongside ProceduralAudioBuilder (which is left as is).
// Placeholder quality on purpose: every file here is meant to be replaced by authored audio later,
// keeping the same file name so nothing needs re-wiring.
//
//   Tools/Audio/Build Extra Cues And Placeholder Music
//
// Cues   -> Assets/Resources/Audio/Generated/   (GameSfx loads everything in that folder)
// Music  -> Assets/Resources/Audio/Music/        (GameSfx.PlayMusic("name"))
public static class ProceduralAudioBuilderExtra
{
    private const int Rate = 44100;
    private const string CueDir = "Assets/Resources/Audio/Generated";
    private const string MusicDir = "Assets/Resources/Audio/Music";

    [MenuItem("Tools/Audio/Build Extra Cues And Placeholder Music")]
    public static void Build()
    {
        Directory.CreateDirectory(CueDir);
        Directory.CreateDirectory(MusicDir);
        var written = new List<string>();

        void Cue(string name, float[] s, float peak = .8f) { written.Add(Write(CueDir, name, s, peak)); }
        void Song(string name, float[] s) { written.Add(Write(MusicDir, name, s, .78f)); }

        Cue("DialogueNext", DialogueNext());
        Cue("WallJump", WallJump());
        Cue("DashEmpty", DashEmpty(), .55f);
        Cue("DashReady", DashReady(), .6f);
        Cue("Respawn", Respawn());
        Cue("Heartbeat", Heartbeat());
        Cue("Retry", Retry(), .65f);
        Cue("Swipe", Swipe());
        Cue("Charge", Charge());
        Cue("RangedShot", RangedShot(), .6f);
        Cue("PhaseShift", PhaseShift());
        Cue("NightmareVanish", NightmareVanish());
        Cue("PlushNotice", PlushNotice(), .6f);
        Cue("LampNotice", LampNotice(), .55f);
        Cue("AmbNightmare", AmbNightmare(), .55f);

        Song("BGM_Garden", Garden());
        Song("BGM_GardenBoss", GardenBoss());
        Song("BGM_Ending", Ending());

        AssetDatabase.Refresh();
        foreach (string path in written)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            if (importer == null) continue;
            bool music = path.StartsWith(MusicDir) || path.EndsWith("AmbNightmare.wav");
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = .7f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }
        Debug.Log("[ProceduralAudioBuilderExtra] wrote " + written.Count + " files.");
    }

    // ============================================================
    // helpers
    // ============================================================

    private static float[] Buf(float seconds) => new float[Mathf.RoundToInt(seconds * Rate)];
    private static float Midi(int m) => 440f * Mathf.Pow(2f, (m - 69) / 12f);
    private static float Env(float t, float attack, float decay) => Mathf.Min(1f, t / Mathf.Max(attack, 1e-4f)) * Mathf.Exp(-t * decay);

    // one-pole low-pass noise; cutoff 0..1
    private sealed class Noise
    {
        private readonly System.Random r; private float lp;
        public Noise(int seed) { r = new System.Random(seed); }
        public float White() => (float)(r.NextDouble() * 2 - 1);
        public float Low(float k) { lp += (White() - lp) * k; return lp; }
        public double Next() => r.NextDouble();
    }

    private static string Write(string dir, string name, float[] s, float peak)
    {
        float max = 0f; foreach (float v in s) max = Mathf.Max(max, Mathf.Abs(v));
        if (max > 0f) for (int i = 0; i < s.Length; i++) s[i] *= peak / max;
        string path = dir + "/" + name + ".wav";
        using (var w = new BinaryWriter(File.Create(path)))
        {
            int n = s.Length;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n * 2);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(n * 2);
            foreach (float v in s) w.Write((short)(Mathf.Clamp(v, -.98f, .98f) * 32767));
        }
        return path;
    }

    // ============================================================
    // cues
    // ============================================================

    // soft paper-bell tick when a dialogue line is advanced
    private static float[] DialogueNext()
    {
        var s = Buf(.14f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            s[i] = (Mathf.Sin(2 * Mathf.PI * 1175 * t) + .35f * Mathf.Sin(2 * Mathf.PI * 2350 * t) * Mathf.Exp(-t * 60)) * Env(t, .002f, 32);
        }
        return s;
    }

    // short crayon swish rising in pitch
    private static float[] WallJump()
    {
        var s = Buf(.18f); var n = new Noise(11);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate, p = t / .18f;
            float band = n.Low(Mathf.Lerp(.08f, .45f, p)) - n.Low(.02f) * .6f;
            s[i] = band * Mathf.Sin(Mathf.PI * p) + .25f * Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(300, 700, p) * t) * Env(t, .005f, 18);
        }
        return s;
    }

    // tiny blocked "tuk" - dash pressed with no stamina
    private static float[] DashEmpty()
    {
        var s = Buf(.14f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            float f = Mathf.Lerp(240, 150, t / .14f);
            float sq = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * t)) * .35f + Mathf.Sin(2 * Mathf.PI * f * t) * .65f;
            s[i] = sq * Env(t, .003f, 28);
        }
        return s;
    }

    // two small rising chimes - dash charges full again
    private static float[] DashReady()
    {
        var s = Buf(.3f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            float a = Mathf.Sin(2 * Mathf.PI * 988 * t) * Env(t, .002f, 20);
            float t2 = t - .08f;
            float b = t2 > 0 ? Mathf.Sin(2 * Mathf.PI * 1319 * t2) * Env(t2, .002f, 16) : 0;
            s[i] = a * .7f + b;
        }
        return s;
    }

    // reverse swell - pulled back onto the last safe ground
    private static float[] Respawn()
    {
        var s = Buf(.7f); var n = new Noise(21);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate, p = t / .7f;
            float swell = Mathf.Pow(p, 2.2f) * (p < .96f ? 1f : (1f - p) / .04f);
            s[i] = (n.Low(.12f) * .7f + .5f * Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(180, 880, p * p) * t)) * swell;
        }
        return s;
    }

    // lub-dub, low and muffled
    private static float[] Heartbeat()
    {
        var s = Buf(.75f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate, v = 0f;
            foreach (var (start, gain) in new[] { (0f, 1f), (.24f, .65f) })
            {
                float u = t - start; if (u < 0) continue;
                float f = Mathf.Lerp(72, 44, Mathf.Min(1, u / .12f));
                v += Mathf.Sin(2 * Mathf.PI * f * u) * Env(u, .006f, 17) * gain;
            }
            s[i] = v;
        }
        return s;
    }

    // neutral up-blip for retry / restart
    private static float[] Retry()
    {
        var s = Buf(.4f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            float a = Mathf.Sin(2 * Mathf.PI * 523 * t) * Env(t, .003f, 14);
            float u = t - .1f;
            float b = u > 0 ? Mathf.Sin(2 * Mathf.PI * 784 * u) * Env(u, .003f, 9) : 0;
            s[i] = a * .6f + b * .8f;
        }
        return s;
    }

    // claw swing: bright noise sweeping down with a crayon scrape edge
    private static float[] Swipe()
    {
        var s = Buf(.42f); var n = new Noise(31);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate, p = t / .42f;
            float k = Mathf.Lerp(.55f, .05f, p);
            float scrape = (float)Math.Sin(2 * Math.PI * 90 * t) > .6 ? .35f : 0f;
            s[i] = (n.Low(k) + scrape * n.White() * .4f) * Mathf.Sin(Mathf.PI * Mathf.Pow(p, .6f)) * (1 - p * .3f);
        }
        return s;
    }

    // heavy rising whoosh before the unicorn lunges
    private static float[] Charge()
    {
        var s = Buf(.6f); var n = new Noise(41);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate, p = t / .6f;
            float body = n.Low(Mathf.Lerp(.03f, .25f, p));
            float tone = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(110, 260, p) * t) * .4f;
            s[i] = (body + tone) * Mathf.Sin(Mathf.PI * p) ;
        }
        return s;
    }

    // dark little shot from the ranged night-lights
    private static float[] RangedShot()
    {
        var s = Buf(.24f); var n = new Noise(51);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate, p = t / .24f;
            s[i] = (Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(620, 210, p) * t) * .8f + n.Low(.3f) * .25f) * Env(t, .002f, 14);
        }
        return s;
    }

    // phase 1 -> 2: paper tearing, a reversed inhale, then a low hit
    private static float[] PhaseShift()
    {
        var s = Buf(2.4f); var n = new Noise(61);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate, v = 0f;
            if (t < 1f)   // tearing: noise gated in irregular bursts
            {
                float gate = (Mathf.PerlinNoise(t * 38f, .3f) > .45f ? 1f : .15f) * Mathf.Min(1, t / .05f) * (1 - t * .3f);
                v += n.Low(.5f) * gate;
            }
            if (t > .8f && t < 1.6f)   // inhale
            {
                float p = (t - .8f) / .8f;
                v += n.Low(.08f) * Mathf.Pow(p, 2.5f) * 1.2f;
            }
            if (t >= 1.6f)   // hit
            {
                float u = t - 1.6f;
                v += Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(62, 32, Mathf.Min(1, u / .5f)) * u) * Env(u, .004f, 4.5f) * 1.6f
                   + n.Low(.2f) * Env(u, .002f, 12) * .6f;
            }
            s[i] = v;
        }
        return s;
    }

    // the nightmare wiping away: crackling paper grains thinning out, a fading shimmer
    private static float[] NightmareVanish()
    {
        var s = Buf(2.8f); var n = new Noise(71);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate, p = t / 2.8f;
            float density = Mathf.Lerp(.02f, .0008f, p);
            float grain = n.Next() < density ? 1f : 0f;
            float shimmer = (Mathf.Sin(2 * Mathf.PI * 1568 * t) + Mathf.Sin(2 * Mathf.PI * 2093 * t)) * .12f * (1 - p);
            s[i] = n.Low(.35f) * .35f * (1 - p) + grain * (float)(n.Next() - .5) * 1.4f + shimmer * Mathf.Min(1, t * 2);
        }
        return s;
    }

    // soft plush squeak - a stuffed rabbit notices you
    private static float[] PlushNotice()
    {
        var s = Buf(.28f); var n = new Noise(81);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate, p = t / .28f;
            float f = Mathf.Lerp(520, 880, Mathf.Sin(Mathf.PI * p * .8f)) + 18 * Mathf.Sin(2 * Mathf.PI * 22 * t);
            s[i] = (Mathf.Sin(2 * Mathf.PI * f * t) * .7f + n.Low(.15f) * .3f) * Mathf.Sin(Mathf.PI * p);
        }
        return s;
    }

    // little electric double-chirp - a night-light switches on
    private static float[] LampNotice()
    {
        var s = Buf(.3f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate, v = 0f;
            foreach (float start in new[] { 0f, .11f })
            {
                float u = t - start; if (u < 0) continue;
                float f = 1400 + 300 * Mathf.Min(1, u / .05f);
                v += Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * u)) * Env(u, .002f, 40) * .5f;
            }
            s[i] = v;
        }
        return s;
    }

    // 12 s seamless nightmare room tone: two low drones breathing, air, rare paper stirs
    private static float[] AmbNightmare()
    {
        const float len = 12f;
        var s = Buf(len); var n = new Noise(91);
        int count = s.Length;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)Rate;
            // whole-number cycles over the loop so the seam lines up
            float breathe = .6f + .4f * Mathf.Sin(2 * Mathf.PI * t * 2f / len);
            float drone = Mathf.Sin(2 * Mathf.PI * 55f * t) * .5f + Mathf.Sin(2 * Mathf.PI * 82.5f * t) * .3f * breathe
                        + Mathf.Sin(2 * Mathf.PI * 110f * t + Mathf.Sin(2 * Mathf.PI * t * 3f / len)) * .12f;
            float air = n.Low(.02f) * 1.4f;
            float stir = Mathf.PerlinNoise(t * .9f, 7.1f) > .78f ? n.Low(.4f) * .25f : 0f;
            s[i] = drone * .55f + air * .5f + stir;
        }
        // cross-fade the last 0.5 s into the first 0.5 s
        int fade = Rate / 2;
        for (int i = 0; i < fade; i++)
        {
            float k = i / (float)fade;
            s[i] = s[i] * k + s[count - fade + i] * (1 - k);
        }
        Array.Resize(ref s, count - fade);
        return s;
    }

    // ============================================================
    // placeholder music
    // ============================================================

    private static void AddNote(float[] s, int startSample, float freq, float gain, Func<float, float, float> voice, float seconds)
    {
        int len = Mathf.RoundToInt(seconds * Rate);
        for (int i = 0; i < len; i++)
        {
            int idx = (startSample + i) % s.Length;   // wrap tails so the loop is seamless
            s[idx] += voice(freq, i / (float)Rate) * gain;
        }
    }

    private static float MusicBox(float f, float t) =>
        (Mathf.Sin(2 * Mathf.PI * f * t) + .5f * Mathf.Sin(2 * Mathf.PI * 2f * f * t) * Mathf.Exp(-t * 6)
         + .22f * Mathf.Sin(2 * Mathf.PI * 3.01f * f * t) * Mathf.Exp(-t * 10)) * Env(t, .003f, 2.4f);

    private static float Staccato(float f, float t) => MusicBox(f, t) * Mathf.Exp(-t * 6);

    private static float Pluck(float f, float t) =>
        (Mathf.Sin(2 * Mathf.PI * f * t) + .3f * Mathf.Sin(2 * Mathf.PI * 2 * f * t) * Mathf.Exp(-t * 5)) * Env(t, .004f, 3f);

    private static float Piano(float f, float t) =>
        (Mathf.Sin(2 * Mathf.PI * f * t) + .45f * Mathf.Sin(2 * Mathf.PI * 2 * f * t) * Mathf.Exp(-t * 2.5f)
         + .18f * Mathf.Sin(2 * Mathf.PI * 3 * f * t) * Mathf.Exp(-t * 4) + .06f * Mathf.Sin(2 * Mathf.PI * 4.02f * f * t) * Mathf.Exp(-t * 6))
        * Env(t, .004f, 1.1f);

    private static float Thump(float f, float t) =>
        Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(f * 1.6f, f, Mathf.Min(1, t / .12f)) * t) * Env(t, .003f, 13);

    // soft pad chord over a bar
    private static void AddPad(float[] s, int start, float seconds, int[] chord, float gain)
    {
        int len = Mathf.RoundToInt(seconds * Rate);
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)Rate, p = t / seconds;
            float env = Mathf.Min(1, p / .25f) * Mathf.Min(1, (1 - p) / .25f);
            float v = 0f;
            foreach (int m in chord) { float f = Midi(m); v += Mathf.Sin(2 * Mathf.PI * f * t) + .4f * Mathf.Sin(2 * Mathf.PI * f * 1.003f * t); }
            s[(start + i) % s.Length] += v * env * gain;
        }
    }

    // triads by root (midi) and quality
    private static int[] Triad(int root, bool minor) => new[] { root, root + (minor ? 3 : 4), root + 7 };

    // chord names for the garden theme in C, and the melody (quarter notes, -1 = rest)
    private static readonly (int root, bool minor)[] GardenChords =
    {
        (48,false),(45,true),(41,false),(43,false),(48,false),(45,true),(50,true),(43,false),
        (41,false),(43,false),(52,true),(45,true),(41,false),(43,false),(48,false),(48,false),
    };
    private static readonly int[][] GardenMelody =
    {
        new[]{76,79,81,79}, new[]{76,-1,72,74}, new[]{72,74,76,-1}, new[]{74,-1,-1,-1},
        new[]{76,79,84,83}, new[]{81,-1,79,76}, new[]{77,76,74,72}, new[]{74,-1,-1,-1},
        new[]{81,-1,84,81}, new[]{79,-1,83,79}, new[]{79,76,71,76}, new[]{84,-1,81,-1},
        new[]{81,79,77,76}, new[]{74,76,77,74}, new[]{76,-1,74,-1}, new[]{72,-1,-1,-1},
    };

    private static float[] RenderTheme(float bpm, int transpose, Func<float, float, float> lead, float leadGain,
        bool arpeggio, Func<float, float, float> arpVoice, float padGain)
    {
        float beat = 60f / bpm;
        var s = Buf(beat * 4 * GardenChords.Length);
        for (int bar = 0; bar < GardenChords.Length; bar++)
        {
            int barStart = Mathf.RoundToInt(bar * 4 * beat * Rate);
            var (root, minor) = GardenChords[bar];
            int[] chord = Triad(root + transpose, minor);

            AddPad(s, barStart, beat * 4, chord, padGain);
            // bass on beats 1 and 3
            AddNote(s, barStart, Midi(root + transpose - 12), .45f, Pluck, beat * 2.2f);
            AddNote(s, barStart + Mathf.RoundToInt(2 * beat * Rate), Midi(root + transpose - 12 + 7), .3f, Pluck, beat * 2f);

            if (arpeggio)
            {
                int[] order = { 0, 2, 1, 2, 3, 2, 1, 2 };
                for (int e = 0; e < 8; e++)
                {
                    int idx = order[e];
                    int m = idx == 3 ? chord[0] + 24 : chord[idx] + 12;
                    AddNote(s, barStart + Mathf.RoundToInt(e * beat * .5f * Rate), Midi(m), .16f, arpVoice, beat * 1.5f);
                }
            }

            int[] mel = GardenMelody[bar];
            for (int q = 0; q < 4; q++)
            {
                if (mel[q] < 0) continue;
                AddNote(s, barStart + Mathf.RoundToInt(q * beat * Rate), Midi(mel[q] + transpose), leadGain, lead, beat * 3f);
            }
        }
        return s;
    }

    // exploration theme: music box over soft pad, 84 bpm, ~46 s loop
    private static float[] Garden() => RenderTheme(84f, 0, MusicBox, .5f, true, MusicBox, .035f);

    // ending: the same melody moved to F, slower, on a piano-like voice, ~58 s
    private static float[] Ending() => RenderTheme(66f, 5 - 12, Piano, .55f, true, Piano, .03f);

    // unicorn boss: toy march in A minor, staccato ostinato + thumps, 112 bpm, ~34 s loop
    private static float[] GardenBoss()
    {
        const float bpm = 112f; float beat = 60f / bpm;
        var chords = new[] { (57, true), (53, false), (50, true), (52, false) };   // Am F Dm E
        var lead = new[]
        {
            new[]{81,-1,76,81}, new[]{84,-1,81,77}, new[]{86,84,81,77}, new[]{76,80,83,88},
            new[]{81,83,84,81}, new[]{77,81,84,-1}, new[]{86,-1,89,86}, new[]{83,80,76,-1},
        };
        int bars = 16;
        var s = Buf(beat * 4 * bars); var n = new Noise(101);
        for (int bar = 0; bar < bars; bar++)
        {
            int barStart = Mathf.RoundToInt(bar * 4 * beat * Rate);
            var (root, minor) = chords[bar % 4];
            int[] chord = Triad(root, minor);
            AddPad(s, barStart, beat * 4, new[] { chord[0] - 12, chord[1] - 12, chord[2] - 12 }, .025f);
            for (int b = 0; b < 4; b++)
            {
                int beatStart = barStart + Mathf.RoundToInt(b * beat * Rate);
                AddNote(s, beatStart, Midi(root - 24), b == 0 ? .7f : .45f, Thump, .35f);
                if (b % 2 == 1)   // snare-ish click on 2 and 4
                {
                    int len = Mathf.RoundToInt(.06f * Rate);
                    for (int i = 0; i < len; i++) s[(beatStart + i) % s.Length] += n.White() * Mathf.Exp(-i / (float)Rate * 60f) * .12f;
                }
            }
            int[] ost = { chord[0], chord[1], chord[2], chord[1] };
            for (int e = 0; e < 8; e++)
                AddNote(s, barStart + Mathf.RoundToInt(e * beat * .5f * Rate), Midi(ost[e % 4] + 12), .2f, Staccato, beat);
            int[] mel = lead[bar % 8];
            for (int q = 0; q < 4; q++)
                if (mel[q] >= 0) AddNote(s, barStart + Mathf.RoundToInt(q * beat * Rate), Midi(mel[q] - (bar >= 8 ? 0 : 12)), .42f, Staccato, beat * 1.6f);
        }
        return s;
    }
}
