using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// 보스 기믹 전용 효과음. ProceduralAudioBuilder와 같은 방식(오프라인 합성 -> PCM wav)이라
// 기존 효과음과 음색이 어울리고, 값만 바꿔 다시 만들 수 있다.
// 결과는 Resources/Audio/Generated에 들어가 GameSfx가 이름으로 바로 불러 쓴다.
public static class BossGimmickAudioBuilder
{
    private const int Rate = 48000;
    private const string Directory_ = "Assets/Resources/Audio/Generated";

    [MenuItem("Tools/Audio/Build Boss Gimmick Cues")]
    public static void Build()
    {
        Directory.CreateDirectory(Directory_);
        Write("HornCharge", HornCharge(), .8f);
        Write("StarImpact", StarImpact(), .85f);
        Write("LaserHum", LaserHum(), .7f);
        AssetDatabase.Refresh();
        foreach (var name in new[] { "HornCharge", "StarImpact", "LaserHum" })
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(Directory_ + "/" + name + ".wav");
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }
        Debug.Log("Authored boss gimmick audio cues: HornCharge, StarImpact, LaserHum.");
    }

    /// <summary>
    /// 회전 레이저가 도는 동안 반복 재생하는 빔 소리 (2초 루프). HornCharge가 끝나는 음역(220~440Hz)을
    /// 이어받아 충전 → 발사 → 유지가 한 소리처럼 들린다. 15초 내내 깔리므로 귀가 피곤하지 않게
    /// 충전음보다 낮고 고르게 둔다.
    /// 주기 성분은 전부 2초에 딱 맞는 주파수(0.5Hz 배수)로 골라 이음매가 없고,
    /// 잡음처럼 주기가 없는 성분은 끝부분을 시작부분에 겹쳐(크로스페이드) 이어 붙인다.
    /// </summary>
    private static float[] LaserHum()
    {
        const float loop = 2f;
        const float crossfade = 0.25f;
        int count = Mathf.RoundToInt(loop * Rate);
        int fade = Mathf.RoundToInt(crossfade * Rate);
        var raw = new float[count + fade];
        var random = new System.Random(7303);
        float filtered = 0f;
        for (int i = 0; i < raw.Length; i++)
        {
            double t = i / (double)Rate;
            // 몸통 - 220Hz와 221Hz를 겹쳐 1초에 한 번 느리게 맥동한다
            double a = 2 * Math.PI * 220 * t + 0.4 * Math.Sin(2 * Math.PI * 6 * t); // 6Hz 미세 떨림
            double b = 2 * Math.PI * 221 * t;
            float body = (float)(Math.Sin(a) + 0.4 * Math.Sin(2 * a) + 0.18 * Math.Sin(3 * a)
                               + 0.6 * Math.Sin(b) + 0.2 * Math.Sin(2 * b));
            // 반짝이는 윗소리 - HornCharge의 윗소리(기본음의 4배)와 같은 결
            float shimmer = (float)(Math.Sin(2 * Math.PI * 880 * t) * 0.18 + Math.Sin(2 * Math.PI * 1762 * t) * 0.06)
                          * (0.6f + 0.4f * (float)Math.Sin(2 * Math.PI * 3 * t));
            // 전기 잡음 - 아주 약하게
            float n = (float)(random.NextDouble() * 2 - 1);
            filtered = Mathf.Lerp(filtered, n, 0.25f);
            float crackle = filtered * 0.07f;
            // 느린 맥동 (8Hz 떨림은 빔이 살아 있는 느낌만 살짝)
            float tremolo = 0.9f + 0.1f * (float)Math.Sin(2 * Math.PI * 8 * t);
            raw[i] = (body * 0.5f + shimmer + crackle) * tremolo;
        }

        var s = new float[count];
        for (int i = 0; i < count; i++) s[i] = raw[i];
        // 끝(loop 지점 이후)을 시작부분에 겹쳐 이어 붙인다 - 루프 이음매에서 끊김/딸깍 소리가 없다.
        for (int i = 0; i < fade; i++)
        {
            float w = i / (float)fade;
            s[i] = raw[i] * w + raw[count + i] * (1f - w);
        }
        return s;
    }

    /// <summary>
    /// 레이저 충전 (1.4초 = BossHornLaser.chargeDuration). 낮은 음이 점점 높아지며 떨리고,
    /// 끝으로 갈수록 떨림이 빨라지고 반짝이는 윗소리가 붙어 "곧 터진다"는 느낌을 준다.
    /// 끝은 짧게 끊어서 바로 이어지는 발사음(Charge)과 겹치지 않게 한다.
    /// </summary>
    private static float[] HornCharge()
    {
        const float duration = 1.4f;
        int count = Mathf.RoundToInt(duration * Rate);
        var s = new float[count];
        var random = new System.Random(7101);
        double phase = 0, shimmerPhase = 0, vibPhase = 0;
        float filtered = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)Rate, p = i / (float)(count - 1);
            // 110Hz -> 440Hz, 지수로 올라가서 끝에서 더 급하게 치솟는다
            float baseFreq = 110f * Mathf.Pow(4f, p * p * 0.6f + p * 0.4f);
            float vibRate = Mathf.Lerp(5f, 19f, p);
            float vibDepth = Mathf.Lerp(0.004f, 0.03f, p);
            vibPhase += 2 * Math.PI * vibRate / Rate;
            float freq = baseFreq * (1f + vibDepth * (float)Math.Sin(vibPhase));
            phase += 2 * Math.PI * freq / Rate;
            // 톱니 느낌의 배음 - 뿔에 에너지가 모이는 웅웅 소리
            float body = (float)(Math.Sin(phase) + 0.45 * Math.Sin(phase * 2) + 0.22 * Math.Sin(phase * 3) + 0.1 * Math.Sin(phase * 4));
            // 반짝이는 윗소리 - 뒤로 갈수록 커진다
            shimmerPhase += 2 * Math.PI * (freq * 4.01f) / Rate;
            float shimmer = (float)Math.Sin(shimmerPhase) * Mathf.SmoothStep(0f, 1f, p) * 0.35f;
            // 전기 느낌 잡음
            float raw = (float)(random.NextDouble() * 2 - 1);
            filtered = Mathf.Lerp(filtered, raw, 0.3f);
            float noise = filtered * Mathf.Lerp(0.03f, 0.12f, p);
            // 떨림(음량) - 끝으로 갈수록 불안정하게 빨라진다
            float tremolo = 1f - Mathf.Lerp(0.05f, 0.25f, p) * (0.5f + 0.5f * (float)Math.Sin(vibPhase * 1.5));
            float envelope = Mathf.Min(1f, t / 0.15f) * Mathf.Lerp(0.35f, 1f, p) * Mathf.Min(1f, (duration - t) / 0.03f);
            s[i] = (body * 0.55f + shimmer + noise) * tremolo * envelope;
        }
        return s;
    }

    /// <summary>
    /// 별 착지 (0.7초). "쿵" 하는 짧은 저음 위에 "짠" 하는 맑은 종소리가 얹힌다.
    /// 기존 Slam보다 가볍고 반짝여서, 점프 찍기 착지와 소리로 구분된다.
    /// </summary>
    private static float[] StarImpact()
    {
        const float duration = 0.7f;
        int count = Mathf.RoundToInt(duration * Rate);
        var s = new float[count];
        var random = new System.Random(7202);
        double thudPhase = 0;
        float filtered = 0f;
        // 반짝임 - G6, D7, G7 (살짝 어긋나게 해서 맑게 반짝이도록)
        float[] bellFreq = { 1568f, 2349f, 3136f, 1571.5f };
        float[] bellGain = { 0.5f, 0.35f, 0.22f, 0.3f };
        float[] bellDecay = { 0.28f, 0.2f, 0.14f, 0.3f };
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)Rate;
            // 쿵 - 120Hz에서 40Hz로 급히 떨어지는 저음
            float thudFreq = Mathf.Lerp(40f, 120f, Mathf.Exp(-t / 0.05f));
            thudPhase += 2 * Math.PI * thudFreq / Rate;
            float thud = (float)Math.Sin(thudPhase) * Mathf.Exp(-t / 0.11f);
            // 부딪히는 순간의 짧은 파열음
            float raw = (float)(random.NextDouble() * 2 - 1);
            filtered = Mathf.Lerp(filtered, raw, 0.35f);
            float burst = filtered * Mathf.Exp(-t / 0.02f) * 0.6f;
            // 짠 - 10ms 뒤에 올라오는 종소리
            float bt = Mathf.Max(0f, t - 0.01f);
            float bell = 0f;
            for (int k = 0; k < bellFreq.Length; k++)
                bell += (float)Math.Sin(2 * Math.PI * bellFreq[k] * bt) * bellGain[k] * Mathf.Exp(-bt / bellDecay[k]);
            float twinkle = 0.85f + 0.15f * (float)Math.Sin(2 * Math.PI * 22f * bt);
            float attack = Mathf.Min(1f, t / 0.002f);
            float tail = Mathf.Min(1f, (duration - t) / 0.02f);
            s[i] = (thud * 0.75f + burst + bell * twinkle * 0.45f) * attack * tail;
        }
        return s;
    }

    private static void Write(string name, float[] samples, float targetPeak)
    {
        float peak = 0f;
        foreach (float v in samples) peak = Mathf.Max(peak, Mathf.Abs(v));
        if (peak > 0f) for (int i = 0; i < samples.Length; i++) samples[i] *= targetPeak / peak;
        using (var writer = new BinaryWriter(File.Create(Directory_ + "/" + name + ".wav")))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
            writer.Write(Rate); writer.Write(Rate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (float v in samples) writer.Write((short)(Mathf.Clamp(v, -.95f, .95f) * 32767));
        }
    }
}
