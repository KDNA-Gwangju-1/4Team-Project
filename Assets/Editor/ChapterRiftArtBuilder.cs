using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Pixel art for the chapter 2 opening: a crack tearing open in the nightmare sky.
// Drawn in code so it can be re-tuned and rebuilt; output goes to Resources so the
// cutscene loads it without any scene wiring.
//
//   Tools/Art/Build Chapter 2 Rift Sprites
//
//   rift_0..7  : the crack spreading and the tear opening (160x128)
//   shard      : a falling sky fragment (7x7)
//   dust       : a crayon dust puff for the landing (24x14)
public static class ChapterRiftArtBuilder
{
    private const string Dir = "Assets/Resources/Sprites/ChapterRift";
    private const int W = 160, H = 128, Frames = 8;
    // art pixel ~0.034 world units, the same chunkiness as the player sprite
    private const float PixelsPerUnit = 28f;

    // palette sampled from the chapter 2 backgrounds (swirling crayon vortex on deep navy,
    // lavender streaks, red crayon slivers) and the cream-topped platforms
    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    private static readonly Color32 Outline = new Color32(14, 10, 32, 255);
    private static readonly Color32 VoidDeep = new Color32(10, 8, 26, 255);
    private static readonly Color32 Indigo = new Color32(36, 26, 74, 255);
    private static readonly Color32 Purple = new Color32(84, 58, 142, 255);
    private static readonly Color32 Violet = new Color32(128, 94, 200, 255);
    private static readonly Color32 Lavender = new Color32(176, 150, 226, 255);
    private static readonly Color32 Pale = new Color32(224, 210, 244, 255);
    private static readonly Color32 Cream = new Color32(240, 228, 222, 255);
    private static readonly Color32 CreamShade = new Color32(204, 186, 214, 255);
    private static readonly Color32 Red = new Color32(166, 34, 60, 255);
    private static readonly Color32 Hatch = new Color32(128, 94, 200, 150);

    private struct Branch { public List<Vector2Int> points; public float start, end; }

    [MenuItem("Tools/Art/Build Chapter 2 Rift Sprites")]
    public static void Build()
    {
        Directory.CreateDirectory(Dir);
        var branches = MakeBranches(new System.Random(2026));
        var paths = new List<string>();
        for (int f = 0; f < Frames; f++) paths.Add(Save("rift_" + f, DrawFrame(branches, f / (float)(Frames - 1), f)));
        paths.Add(Save("shard", Shard()));
        paths.Add(Save("dust", Dust()));
        AssetDatabase.Refresh();
        foreach (string path in paths)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        Debug.Log("[ChapterRiftArtBuilder] wrote " + paths.Count + " sprites to " + Dir);
    }

    // ------------------------------------------------------------
    // crack layout: two main zig-zags out of the centre, side branches off them
    // ------------------------------------------------------------
    private static List<Branch> MakeBranches(System.Random r)
    {
        var list = new List<Branch>();
        var centre = new Vector2Int(W / 2, H / 2);
        foreach (int dir in new[] { -1, 1 })
        {
            var main = Walk(r, centre, dir, 0, W / 2 - 8, 7, 11, 5);
            list.Add(new Branch { points = main, start = 0f, end = .75f });
            // side branches from points along the main crack
            for (int b = 0; b < 3; b++)
            {
                var from = main[Mathf.Clamp(2 + b * (main.Count / 4) + r.Next(0, 2), 1, main.Count - 2)];
                int vy = r.Next(0, 2) == 0 ? -1 : 1;
                var side = Walk(r, from, dir, vy, 22 + r.Next(0, 18), 4, 7, 3);
                float s = .25f + b * .14f;
                list.Add(new Branch { points = side, start = s, end = Mathf.Min(1f, s + .35f) });
            }
        }
        // two short vertical splinters from the centre
        foreach (int vy in new[] { -1, 1 })
            list.Add(new Branch { points = Walk(r, centre, 0, vy, 20, 4, 6, 3), start = .35f, end = .7f });
        return list;
    }

    // zig-zag polyline: dx sign per step (0 = mostly vertical), dy bias, total length in px
    private static List<Vector2Int> Walk(System.Random r, Vector2Int from, int dx, int dy, int length, int minStep, int maxStep, int jitter)
    {
        var pts = new List<Vector2Int> { from };
        var p = from; int travelled = 0;
        while (travelled < length)
        {
            int step = r.Next(minStep, maxStep + 1);
            int nx = p.x + (dx != 0 ? dx * step : r.Next(-jitter, jitter + 1));
            int ny = p.y + (dx != 0 ? r.Next(-jitter, jitter + 1) + dy * step / 2 : dy * step);
            nx = Mathf.Clamp(nx, 3, W - 4); ny = Mathf.Clamp(ny, 3, H - 4);
            p = new Vector2Int(nx, ny);
            pts.Add(p);
            travelled += step;
        }
        return pts;
    }

    // ------------------------------------------------------------
    // one frame
    // ------------------------------------------------------------
    private static Color32[] DrawFrame(List<Branch> branches, float progress, int frameIndex)
    {
        var px = new Color32[W * H];
        var crack = new bool[W * H];
        var redraw = new bool[W * H];

        // 1) crack lines revealed up to this frame's progress, plus a second offset pass
        //    like a crayon line gone over twice
        foreach (var b in branches)
        {
            float t = Mathf.InverseLerp(b.start, b.end, progress);
            if (t <= 0f) continue;
            float total = 0f; for (int i = 1; i < b.points.Count; i++) total += Vector2.Distance(b.points[i - 1], b.points[i]);
            float budget = total * t;
            bool main = b.start == 0f;
            var shift = new Vector2(1, main ? 2 : 1);
            for (int i = 1; i < b.points.Count && budget > 0f; i++)
            {
                Vector2 a = b.points[i - 1], c = b.points[i];
                float seg = Vector2.Distance(a, c);
                Vector2 end = seg <= budget ? c : Vector2.Lerp(a, c, budget / seg);
                Line(crack, a, end);
                // the main cracks are two pixels thick near the centre, so they read at a distance
                if (main && Mathf.Abs(a.x - W / 2) < 40) Line(crack, a + Vector2.down, end + Vector2.down);
                Line(redraw, a + shift, end + shift);
                budget -= seg;
            }
        }

        // 2) the tear: a torn lens along the centre, opening in the second half
        float open = Mathf.Clamp01((progress - .35f) / .65f);
        var tear = new bool[W * H];
        int half = Mathf.RoundToInt(Mathf.Lerp(6, 48, open));
        float thick = Mathf.Lerp(2, 22, open * open);
        if (open > 0f)
        {
            for (int dx = -half; dx <= half; dx++)
            {
                float k = 1f - (dx / (float)half) * (dx / (float)half);
                // torn-paper edge: slow wobble plus sharp notches, fixed per column so frames stay coherent
                float wobbleTop = (Mathf.PerlinNoise((dx + 100) * .09f, 1.7f) - .5f) * 9f + (Mathf.PerlinNoise((dx + 100) * .6f, 4.2f) - .5f) * 6f;
                float wobbleBot = (Mathf.PerlinNoise((dx + 100) * .08f, 8.3f) - .5f) * 8f + (Mathf.PerlinNoise((dx + 100) * .55f, 2.9f) - .5f) * 6f;
                int hTop = Mathf.RoundToInt(thick * k + wobbleTop * Mathf.Sqrt(k) * open);
                int hBot = Mathf.RoundToInt(thick * k * .75f + wobbleBot * Mathf.Sqrt(k) * open);
                for (int dy = -hBot; dy <= hTop; dy++) Set(tear, W / 2 + dx, H / 2 + dy);
            }
        }

        // 3) halo: short diagonal crayon hatching around the crack and the tear, the same
        //    lower-left to upper-right direction as the background strokes
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                if (crack[y * W + x] || tear[y * W + x]) continue;
                bool nearTear = Near(tear, x, y, 5), nearCrack = Near(crack, x, y, 3);
                if (!nearTear && !nearCrack) continue;
                if (((x - y) & 3) != 0) continue;
                if (Hash(x, y, 7) < (nearTear ? .25f : .45f)) continue;
                px[y * W + x] = Hash(x, y, 8) < .2f ? Lavender : Hatch;
            }

        // 4) dark outline one pixel around everything bright
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                if (crack[i] || tear[i]) continue;
                if (Near(crack, x, y, 1) || Near(tear, x, y, 1)) px[i] = Outline;
            }

        // 5) the second crayon pass sits under the main line in violet, grainy
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                if (!redraw[i] || crack[i] || tear[i]) continue;
                if (Hash(x, y, 3) < .35f) continue;
                px[i] = Violet;
            }

        // 6) crack cores: pale crayon, grain breaking the line up toward the tips
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                if (!crack[y * W + x]) continue;
                float d = Mathf.Abs(x - W / 2) / (W / 2f);
                float g = Hash(x, y, 1);
                if (d > .6f && g < (d - .6f) * .9f) { px[y * W + x] = Purple; continue; }
                px[y * W + x] = g < .25f ? Lavender : d < .5f ? Cream : Pale;
            }

        // 7) tear interior: a turning crayon vortex like the level background,
        //    a torn cream paper rim and red crayon slivers caught in the swirl
        if (open > 0f)
        {
            bool flash = frameIndex >= 6 && frameIndex % 2 == 1;
            float turn = frameIndex * .55f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (!tear[i]) continue;
                    bool rim = !Get(tear, x - 1, y) || !Get(tear, x + 1, y) || !Get(tear, x, y - 1) || !Get(tear, x, y + 1);
                    if (rim)
                    {
                        px[i] = flash ? Color32.Lerp(Cream, Color.white, .6f) : Hash(x, y, 4) < .3f ? CreamShade : Cream;
                        continue;
                    }
                    // paper shadow just inside the cream edge
                    if (Near(tear, x, y, 1, invert: true)) { px[i] = Color32.Lerp(CreamShade, Purple, .55f); continue; }
                    // chunky 2px blocks, like the pixel vortex background
                    int bx = x & ~1, by = y & ~1;
                    float u = (bx - W / 2) / (float)Mathf.Max(half, 1), v = (by - H / 2) / Mathf.Max(thick, 1f);
                    float r = Mathf.Sqrt(u * u + v * v);
                    float theta = Mathf.Atan2(v, u);
                    float s = Mathf.Sin(theta * 3f + r * 11f - turn) + (Hash(bx, by, 5) - .5f) * .6f;
                    Color32 c = s > .85f ? Violet : s > .35f ? Purple : s > -.3f ? Indigo : VoidDeep;
                    if (s > .95f && Hash(bx, by, 11) < .35f) c = Lavender;
                    if (r < .3f) c = r < .18f ? VoidDeep : Color32.Lerp(c, VoidDeep, .6f);
                    else if (s > .3f && s < .45f && r > .4f && Hash(bx, by, 6) < .5f) c = Red;
                    px[i] = c;
                }
        }
        return px;
    }

    // cheap stable per-pixel noise for crayon grain
    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFF) / 65535f;
        }
    }

    // ------------------------------------------------------------
    // small sprites
    // ------------------------------------------------------------
    private static Color32[] Shard()
    {
        // a torn scrap of sky: cream paper edge, a purple crayon smear inside
        const int n = 7; var px = new Color32[n * n];
        string[] rows =
        {
            "..cc...",
            ".cppc..",
            "cpvvpc.",
            "cpvrvpc",
            ".cvvpc.",
            "..cpc..",
            "...c...",
        };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                char ch = rows[n - 1 - y][x];
                px[y * n + x] = ch == 'c' ? Cream : ch == 'p' ? Purple : ch == 'v' ? Violet : ch == 'r' ? Red : Clear;
            }
        return px;
    }

    private static Color32[] Dust()
    {
        const int w = 24, h = 14; var px = new Color32[w * h];
        var body = new Color32(196, 180, 220, 235); var shade = new Color32(128, 102, 176, 235);
        // three overlapping blobs, flat bottom
        var blobs = new[] { new Vector3(7, 5, 5), new Vector3(13, 7, 6.5f), new Vector3(18, 5, 4.5f) };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool inside = false, edge = false;
                foreach (var b in blobs)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(b.x, b.y));
                    if (d <= b.z) inside = true;
                    else if (d <= b.z + 1f) edge = true;
                }
                if (y < 2) inside = inside && x > 3 && x < w - 3;
                // crayon grain: a few pixels of the puff drop out or darken
                if (inside && Hash(x, y, 9) < .12f) inside = false;
                px[y * w + x] = inside ? (y < 4 || Hash(x, y, 10) < .2f ? shade : body) : edge ? Outline : Clear;
            }
        return px;
    }

    // ------------------------------------------------------------
    // raster helpers
    // ------------------------------------------------------------
    private static void Line(bool[] m, Vector2 a, Vector2 b)
    {
        int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y)));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, steps == 0 ? 0 : i / (float)steps);
            Set(m, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
        }
    }

    private static void Set(bool[] m, int x, int y) { if (x >= 0 && y >= 0 && x < W && y < H) m[y * W + x] = true; }
    private static bool Get(bool[] m, int x, int y) => x >= 0 && y >= 0 && x < W && y < H && m[y * W + x];

    // any set pixel within Chebyshev radius r (invert: any UNset pixel)
    private static bool Near(bool[] m, int x, int y, int r, bool invert = false)
    {
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                bool v = Get(m, x + dx, y + dy);
                if (invert ? !v : v) return true;
            }
        return false;
    }

    private static string Save(string name, Color32[] px)
    {
        int size = px.Length;
        int w = size == W * H ? W : name == "shard" ? 7 : 24;
        int h = size / w;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(px); tex.Apply();
        string path = Dir + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        return path;
    }
}
