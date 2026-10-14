// Pastel pixel candy land background.
// Draws on a 384x216 pixel grid and upscales x5 (nearest neighbor) to 1920x1080.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public static class CandyBg
{
    const int W = 384;
    const int H = 216;
    const int SCALE = 5;
    const int OPAQUE = unchecked((int)0xFF000000);

    static int[] px = new int[W * H];
    static Random rng = new Random(11);

    static readonly int[,] Bayer = new int[,] {
        { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 }
    };

    // ---------- palette (brief colors + soft neighbours) ----------
    static readonly int Rose = Hex("#C8A29C");
    static readonly int Cream = Hex("#F5E6D8");
    static readonly int Pink = Hex("#E8B4B8");
    static readonly int Butter = Hex("#FFF3B0");

    // ---------- primitives ----------
    static int Hex(string s) { return OPAQUE | Convert.ToInt32(s.TrimStart('#'), 16); }

    static void Put(int x, int y, int c)
    {
        if (x < 0 || y < 0 || x >= W || y >= H) return;
        px[y * W + x] = c;
    }

    static double Thr(int x, int y) { return (Bayer[y & 3, x & 3] + 0.5) / 16.0; }

    // 0 at the horizontal center, 1 at the left/right edges.
    static double Edge(int x)
    {
        double d = (x - W / 2.0) / (W / 2.0);
        return d * d;
    }

    static string Rp(char c, int n) { return new string(c, n); }
    static string Pad(int pad, string mid) { return Rp('.', pad) + mid + Rp('.', pad); }

    // ---------- sky ----------
    static void Sky()
    {
        string[] hx = { "#EFC4C7", "#F1C9CA", "#F4D2D0", "#F6DAD6", "#F8E2DB", "#FAE9DF", "#FBEEE3" };
        int n = hx.Length;
        int[] cols = new int[n];
        for (int i = 0; i < n; i++) cols[i] = Hex(hx[i]);
        double horizon = 170.0;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                double t = Math.Min(1.0, y / horizon);
                double f = t * (n - 1) - 1.2 * Edge(x); // pinker toward the edges
                if (f < 0) f = 0;
                int i = (int)Math.Floor(f);
                if (i >= n - 1) { Put(x, y, cols[n - 1]); continue; }
                Put(x, y, (f - i) > Thr(x, y) ? cols[i + 1] : cols[i]);
            }
        }
    }

    static void Sun(int cx, int cy, int r)
    {
        int halo = 12;
        int haloCol = Hex("#FBEBCD");
        for (int y = cy - r - halo; y <= cy + r + halo; y++)
        {
            for (int x = cx - r - halo; x <= cx + r + halo; x++)
            {
                double d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d <= r)
                {
                    bool glow = Math.Sqrt((x - cx + 3) * (x - cx + 3) + (y - cy + 3) * (y - cy + 3)) < r * 0.55;
                    Put(x, y, glow ? Hex("#FFF9D2") : Butter);
                }
                else if (d <= r + halo)
                {
                    double t = 1.0 - (d - r) / halo;
                    if (t * t * 0.7 > Thr(x, y)) Put(x, y, haloCol);
                }
            }
        }
    }

    // ---------- clouds ----------
    static void Cloud(int cx, int baseY, int w, int seed)
    {
        Random r = new Random(seed);
        int n = Math.Max(3, w / 10);
        List<int[]> circles = new List<int[]>();
        for (int i = 0; i < n; i++)
        {
            double u = (double)i / (n - 1);
            int ccx = cx - w / 2 + (int)(u * w);
            int rad = 5 + (int)(Math.Sin(u * Math.PI) * 6) + r.Next(0, 3);
            circles.Add(new int[] { ccx, baseY - rad + 2, rad });
        }

        int x0 = cx - w / 2 - 14, x1 = cx + w / 2 + 14, y0 = baseY - 24;
        int mw = x1 - x0 + 1, mh = baseY - y0 + 1;
        bool[,] mask = new bool[mw, mh];
        for (int y = y0; y <= baseY; y++)
            for (int x = x0; x <= x1; x++)
                foreach (int[] c in circles)
                {
                    int dx = x - c[0], dy = y - c[1];
                    if (dx * dx + dy * dy <= c[2] * c[2]) { mask[x - x0, y - y0] = true; break; }
                }

        int main = Hex("#FFF6EE"), hi = Hex("#FFFCF7"), shade = Hex("#F2CFCC");
        for (int y = y0; y <= baseY; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (!mask[x - x0, y - y0]) continue;
                bool top = (y - y0 == 0) || !mask[x - x0, y - y0 - 1];
                int fromBottom = baseY - y;
                int col = main;
                if (top) col = hi;
                if (fromBottom < 2) col = shade;
                else if (fromBottom < 5 && Thr(x, y) < 0.5) col = shade;
                Put(x, y, col);
            }
        }
    }

    // ---------- hills ----------
    static int HillTop(int layer, int x)
    {
        double e = Edge(x);
        double y;
        switch (layer)
        {
            case 0: y = 154 + 4 * Math.Sin(x * 0.026 + 1.0) + 3 * Math.Sin(x * 0.071 + 2.0) - 42 * e; break;
            case 1: y = 171 + 4 * Math.Sin(x * 0.034 + 3.1) + 2 * Math.Sin(x * 0.089) - 45 * e; break;
            default: y = 192 + 3 * Math.Sin(x * 0.041 + 0.4) + 2 * Math.Sin(x * 0.112 + 1.5) - 46 * e; break;
        }
        return (int)Math.Round(y);
    }

    static void Hill(int layer, int body, int bodyLow, int hi, bool frosting, int cap, int capShade, int capHi)
    {
        for (int x = 0; x < W; x++)
        {
            int top = HillTop(layer, x);
            for (int y = top; y < H; y++)
            {
                double t = Math.Min(1.0, (y - top) / 40.0) * 0.8;
                Put(x, y, t > Thr(x, y) ? bodyLow : body);
            }
            Put(x, top, hi);

            if (frosting)
            {
                uint h = ((uint)(x / 6 + layer * 37) * 2654435761u) >> 12;
                int len = 3 + (int)(h % 6);
                int m = x % 6;
                if (m == 0 || m == 5) len -= 1; // rounded drip corners
                if (len > 5 && (m == 1 || m == 4)) len -= 1;
                for (int k = 0; k < len; k++) Put(x, top + k, cap);
                Put(x, top, capHi);
                Put(x, top + len - 1, capShade);
            }
        }
    }

    static void Sprinkles(int layer, int count, int[] cols)
    {
        int placed = 0, tries = 0;
        while (placed < count && tries < count * 60)
        {
            tries++;
            int x = rng.Next(1, W - 4);
            double e = Edge(x);
            if (e < 0.14) continue; // keep the center clean
            if (rng.NextDouble() > Math.Pow(e, 0.7) * 1.1 + 0.02) continue;
            int y = HillTop(layer, x) + 4 + rng.Next(0, 34);
            if (y >= H - 2) continue;
            int c = cols[rng.Next(cols.Length)];
            switch (rng.Next(3))
            {
                case 0: Put(x, y, c); Put(x + 1, y, c); Put(x + 2, y, c); break;
                case 1: Put(x, y, c); Put(x, y + 1, c); Put(x, y + 2, c); break;
                default: Put(x, y, c); Put(x + 1, y, c); break;
            }
            placed++;
        }
    }

    static void Gumdrop(int cx, int baseY, int r, int body, int sugar, int outline)
    {
        for (int dy = -r - 1; dy <= 0; dy++)
        {
            for (int dx = -r - 1; dx <= r + 1; dx++)
            {
                if (!GumInside(dx, dy, r)) continue;
                bool edge = !GumInside(dx - 1, dy, r) || !GumInside(dx + 1, dy, r) || !GumInside(dx, dy - 1, r);
                int c = body;
                if (edge) c = outline;
                else if (((cx + dx) * 3 + (baseY + dy) * 5) % 7 == 0) c = sugar;
                Put(cx + dx, baseY + dy, c);
            }
        }
    }

    static bool GumInside(int dx, int dy, int r)
    {
        if (dy > 0) return false;
        return dx * dx + dy * dy * 1.5 <= r * r;
    }

    static void Lollipop(int cx, int baseY, int stickH, int r, int c1, int c2, int outline)
    {
        for (int y = baseY - stickH; y <= baseY; y++)
        {
            Put(cx, y, Cream);
            Put(cx + 1, y, Rose);
        }
        int cy = baseY - stickH - r + 2;
        for (int dy = -r; dy <= r; dy++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d > r + 0.2) continue;
                double ang = Math.Atan2(dy, dx) / (2 * Math.PI) + 0.5;
                double s = ang + d / (r * 0.55);
                int k = (int)Math.Floor(s * 2.0);
                int c = ((k & 1) == 0) ? c1 : c2;
                if (d > r - 1.0) c = outline;
                Put(cx + dx, cy + dy, c);
            }
        }
        Put(cx - r / 2, cy - r / 2, Hex("#FFFBF4"));
    }

    // ---------- sprites ----------
    static void Sprite(string[] rows, int x, int y, Dictionary<char, int> pal)
    {
        for (int j = 0; j < rows.Length; j++)
            for (int i = 0; i < rows[j].Length; i++)
            {
                char ch = rows[j][i];
                if (ch == '.' || !pal.ContainsKey(ch)) continue;
                Put(x + i, y + j, pal[ch]);
            }
    }

    static string[] MacRows()
    {
        return new string[] {
            Pad(4, Rp('o', 8)),
            Pad(2, "oohhh" + Rp('s', 5) + "oo"),
            Pad(1, "ohhh" + Rp('s', 8) + "do"),
            "o" + Rp('s', 13) + "do",
            Pad(1, "o" + Rp('f', 12) + "o"),
            Pad(1, "o" + Rp('g', 12) + "o"),
            "o" + Rp('s', 13) + "do",
            Pad(1, "o" + Rp('d', 12) + "o"),
            Pad(2, Rp('o', 12))
        };
    }

    static string[] CupRows()
    {
        return new string[] {
            Pad(6, "oo"),
            Pad(5, "occo"),
            Pad(5, "oCco"),
            Pad(6, "oo"),
            Pad(4, "oFFFFo"),
            Pad(3, "oHFFFFSo"),
            Pad(2, "oHFFFFFFSo"),
            Pad(1, "oHHFFFFFFSSo"),
            "oHFFmFFFFFFSSo",
            "oFFFFFFmFFFSSo",
            "o" + Rp('S', 12) + "o",
            Pad(1, "owWwWwWwWwWo"),
            Pad(1, "owWwWwWwWwWo"),
            Pad(2, "owWwWwWwWo"),
            Pad(2, "owWwWwWwWo"),
            Pad(2, Rp('o', 10))
        };
    }

    static Dictionary<char, int> MacPal(string shell, string hi, string shade, string outl, string fill, string fillShade)
    {
        Dictionary<char, int> p = new Dictionary<char, int>();
        p['s'] = Hex(shell); p['h'] = Hex(hi); p['d'] = Hex(shade); p['o'] = Hex(outl);
        p['f'] = Hex(fill); p['g'] = Hex(fillShade);
        return p;
    }

    static Dictionary<char, int> CupPal(string f, string h, string s, string o, string w, string wShade, string cherry)
    {
        Dictionary<char, int> p = new Dictionary<char, int>();
        p['F'] = Hex(f); p['H'] = Hex(h); p['S'] = Hex(s); p['o'] = Hex(o);
        p['w'] = Hex(w); p['W'] = Hex(wShade);
        p['c'] = Hex(cherry); p['C'] = Hex("#FFD3D6"); p['m'] = Butter;
        return p;
    }

    static void Sparkle(int cx, int cy, int L, int c, int core)
    {
        for (int i = 1; i <= L; i++)
        {
            Put(cx - i, cy, c); Put(cx + i, cy, c);
            Put(cx, cy - i, c); Put(cx, cy + i, c);
        }
        Put(cx, cy, core);
    }

    static readonly string[] HeartRows = {
        ".XX.XX.", "XHXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..."
    };

    static void Heart(int x, int y, string main, string hi)
    {
        Dictionary<char, int> p = new Dictionary<char, int>();
        p['X'] = Hex(main); p['H'] = Hex(hi);
        Sprite(HeartRows, x, y, p);
    }

    // ---------- scene ----------
    public static void Run(string dir)
    {
        Sky();
        Sun(342, 62, 11);

        Cloud(40, 46, 60, 1);
        Cloud(118, 20, 36, 2);
        Cloud(352, 40, 54, 3);
        Cloud(300, 12, 30, 4);
        Cloud(18, 110, 44, 5);
        Cloud(372, 102, 40, 6);

        // sparkles in the sky, edges only
        int[] sparkCols = { Butter, Hex("#FFFBF4"), Hex("#FBE3E3") };
        int sp = 0, tries = 0;
        while (sp < 24 && tries < 600)
        {
            tries++;
            int x = rng.Next(4, W - 4);
            int y = rng.Next(4, 120);
            bool edgeZone = Edge(x) > 0.36 || y < 18;
            if (!edgeZone) continue;
            if (y > HillTop(0, x) - 8) continue;
            int L = rng.Next(1, 3);
            Sparkle(x, y, L, sparkCols[rng.Next(sparkCols.Length)], Hex("#FFFFFF"));
            sp++;
        }

        // hearts
        Heart(70, 74, "#E8B4B8", "#F8D6D8");
        Heart(300, 80, "#F5E6D8", "#FFFBF4");
        Heart(8, 92, "#E8B4B8", "#F8D6D8");

        // hills, back to front, each with its own decor
        int[] sprinkleCols = { Butter, Cream, Rose, Hex("#F8D6D8") };

        Hill(0, Hex("#F3D9D2"), Hex("#EBCBC6"), Hex("#FBEBE2"), false, 0, 0, 0);
        Sprinkles(0, 40, new int[] { Hex("#FBEBE2"), Butter });

        Hill(1, Pink, Hex("#DDA5AB"), Hex("#F2C9CB"), true, Hex("#FBEFE3"), Hex("#E9C9C0"), Hex("#FFF9F2"));
        Sprinkles(1, 60, sprinkleCols);
        Lollipop(14, HillTop(1, 14) + 3, 20, 8, Hex("#F8D6D8"), Cream, Rose);
        Lollipop(46, HillTop(1, 46) + 3, 14, 6, Butter, Hex("#F8D6D8"), Rose);
        Lollipop(366, HillTop(1, 366) + 3, 22, 8, Cream, Hex("#F8D6D8"), Rose);
        Lollipop(334, HillTop(1, 334) + 3, 14, 6, Hex("#F8D6D8"), Butter, Rose);

        Hill(2, Hex("#DDB2AE"), Hex("#CDA7A1"), Hex("#EAC4C0"), true, Cream, Hex("#E3C2B5"), Hex("#FFF6EC"));
        Sprinkles(2, 70, sprinkleCols);
        Gumdrop(14, HillTop(2, 14) + 3, 9, Hex("#F4C2C6"), Hex("#FFE9EA"), Hex("#B98388"));
        Gumdrop(36, HillTop(2, 36) + 3, 6, Butter, Hex("#FFFBD6"), Hex("#C2AA6F"));
        Gumdrop(58, HillTop(2, 58) + 3, 5, Cream, Hex("#FFFFFF"), Hex("#C8A29C"));
        Gumdrop(370, HillTop(2, 370) + 3, 9, Butter, Hex("#FFFBD6"), Hex("#C2AA6F"));
        Gumdrop(348, HillTop(2, 348) + 3, 6, Hex("#F4C2C6"), Hex("#FFE9EA"), Hex("#B98388"));
        Gumdrop(326, HillTop(2, 326) + 3, 5, Cream, Hex("#FFFFFF"), Hex("#C8A29C"));

        // floating macarons and cupcakes
        string[] mac = MacRows();
        string[] cup = CupRows();

        Dictionary<char, int> macPink = MacPal("#F0B2B8", "#FAD0D3", "#DB9AA2", "#B98388", "#FFF3E6", "#F3DCCB");
        Dictionary<char, int> macButter = MacPal("#FFF3B0", "#FFFADB", "#EAD88E", "#C2AA6F", "#F0B2B8", "#E19AA4");
        Dictionary<char, int> macCream = MacPal("#F5E6D8", "#FFF9F2", "#E2CBBB", "#C8A29C", "#E8B4B8", "#D99EA5");
        Dictionary<char, int> macRose = MacPal("#D9A9A6", "#E8C3C0", "#C8A29C", "#A98480", "#F5E6D8", "#E3CBBB");

        Dictionary<char, int> cupPink = CupPal("#F4BCC2", "#FFDDE0", "#DE9FA8", "#B98388", "#F5E6D8", "#E3C9B8", "#E8909A");
        Dictionary<char, int> cupCream = CupPal("#FFF3E6", "#FFFFFF", "#EBD3C3", "#B88B88", "#E8B4B8", "#D99EA5", "#E8909A");
        Dictionary<char, int> cupButter = CupPal("#FFF3B0", "#FFFADB", "#EBD88E", "#B9A56E", "#F5E6D8", "#E3C9B8", "#E8909A");

        // left cluster
        Sprite(mac, 14, 16, macPink);
        Sprite(cup, 50, 38, cupCream);
        Sprite(mac, 8, 62, macButter);
        Sprite(cup, 80, 10, cupPink);
        Sprite(mac, 30, 92, macRose);

        // right cluster
        Sprite(cup, 350, 16, cupPink);
        Sprite(mac, 296, 34, macCream);
        Sprite(mac, 358, 76, macPink);
        Sprite(cup, 322, 94, cupButter);
        Sprite(mac, 296, 118, macRose);
        Sprite(mac, 244, 8, macButter);

        Save(dir);
    }

    static void Save(string dir)
    {
        Directory.CreateDirectory(dir);
        using (Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb))
        {
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(px, 0, bd.Scan0, px.Length);
            bmp.UnlockBits(bd);
            bmp.Save(Path.Combine(dir, "candy-land-bg-384x216.png"), ImageFormat.Png);

            using (Bitmap big = new Bitmap(W * SCALE, H * SCALE, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(big))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(bmp, new Rectangle(0, 0, W * SCALE, H * SCALE), 0, 0, W, H, GraphicsUnit.Pixel);
                }
                big.Save(Path.Combine(dir, "candy-land-bg-1920x1080.png"), ImageFormat.Png);
            }
        }
    }
}
