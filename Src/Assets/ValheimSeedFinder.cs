using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using Valheim;

namespace ValheimSeedFinder
{
    public class ValheimSeedFinder : MonoBehaviour
    {
        private static float time = 0;
        private static int lastCount = 0;
        public static int GenerationCountPerSecond = 0;

        private void OnEnable()
        {
            Application.targetFrameRate = 60;
            Generator.Start();
        }

        private void Update()
        {
            PopulateSeedQueue();
            UpdateGenerationCountPerSecond();

            static void PopulateSeedQueue()
            {
                lock (Generator.QUEUE_LOCK)
                {
                    // Seeds needs to be generated on main thread.
                    while (Generator.WorkQueue.Count < 1000)
                    {
                        var seed = Generator.Options.SeedPrefix + Generator.SeedId++;
                        var world = new World(seed);
                        var gen = new WorldGenerator(world);
                        Generator.WorkQueue.Enqueue(new Result()
                        {
                            Gen = gen,
                            Seed = seed,
                        });
                    }
                }
            }

            static void UpdateGenerationCountPerSecond()
            {
                time += Time.deltaTime;
                var updateInterval = 0.2f;
                if (time > updateInterval)
                {
                    var newCount = Generator.GenerationCount;
                    GenerationCountPerSecond = (int)(Math.Max(0, newCount - lastCount) / updateInterval);
                    lastCount = newCount;
                    time = 0;
                }
            }
        }

        private void OnGUI()
        {
            lock (Generator.Options.OPTIONS_LOCK)
            {
                var rect = new Rect(0, 0, Screen.width, Screen.height);
                GUITools.DrawRect(rect, new Color(0.05f, 0.05f, 0.05f));
                GUITools.DrawOptions(rect.TakeFromRight(400));
                GUITools.DrawResults(rect);
            }
        }
    }

    public static class GUITools
    {
        public static void DrawResults(Rect rect)
        {
            rect = rect.Padding(10);

            for (int i = 0; i < Generator.Results.Count; i++)
            {
                var result = Generator.Results[i];
                var r = rect.SplitGrid(rect.width / 5 - 0.01f, rect.width / 5, i).Padding(5);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    Application.OpenURL($"https://valheim-map.world/?seed={result.Seed}&offset={(int)result.WorldPos.x}%2C{(int)result.WorldPos.y}&zoom=0.025&view=0&ver=0.212.6");
                    GUI.changed = false;
                }

                var mouseOverAdd = r.Contains(Event.current.mousePosition) ? 0.2f : 0;
                DrawRect(r, new Color(0.1f + mouseOverAdd, 0.1f + mouseOverAdd, 0.1f + mouseOverAdd));
                DrawBorders(r, new Color(0.2f + mouseOverAdd, 0.2f + mouseOverAdd, 0.2f + mouseOverAdd));

                var row = r.TakeFromTop(24);
                GUI.Label(row.TakeFromRight(80), "score: " + result.Score.ToString("0.000"));
                GUI.Label(row, "seed: " + result.Seed);

                DrawProfile(r.TakeFromBottom(20), result.Profile);

                if (result.Thumbnail == null)
                {
                    result.MakeThumbnail();
                }

                r.TakeFromBottom(4);

                if (result.Thumbnail)
                {
                    GUI.DrawTexture(r, result.Thumbnail, ScaleMode.ScaleToFit);
                }
            }
        }

        public static void DrawOptions(Rect rect)
        {
            var lineHeight = 24;
            var labelWidth = 120;
            GUI.changed = false;

            DrawRect(rect.TakeFromLeft(1), new Color(0.2f, 0.2f, 0.2f));
            DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));
            rect = rect.Padding(10, 10, 0, 10);

            DrawTitle(ref rect, "Seed");
            {
                {
                    var r = rect.TakeFromTop(lineHeight);
                    GUI.Label(r.TakeFromLeft(labelWidth), "Seed Prefix");
                    Generator.Options.SeedPrefix = GUI.TextField(r.AlignTop(19), Generator.Options.SeedPrefix);
                }
                {
                    var r = rect.TakeFromTop(lineHeight);

                    var l1 = r.Split(0, 2);
                    var l2 = r.Split(1, 2);
                    GUI.Label(l1.TakeFromLeft(labelWidth), "Seeds scanned:");
                    GUI.Label(l1, Generator.GenerationCount.ToString());
                    GUI.Label(l2.TakeFromLeft(80), "per second:");
                    GUI.Label(l2, ValheimSeedFinder.GenerationCountPerSecond.ToString());
                }
            }
            rect.TakeFromTop(10);

            DrawTitle(ref rect, "Performance");
            {
                {
                    var r = rect.TakeFromTop(lineHeight);
                    GUI.Label(r.TakeFromLeft(labelWidth), "Search Resolution");
                    GUI.Label(r.TakeFromRight(70), Generator.Options.WorldMapResolution + " x " + Generator.Options.WorldMapResolution);
                    Generator.Options.WorldMapResolution = (int)GUI.HorizontalSlider(r, Generator.Options.WorldMapResolution, 0, 500);
                }
                {
                    var donTriggerChange = GUI.changed;
                    var r = rect.TakeFromTop(lineHeight);
                    GUI.Label(r.TakeFromLeft(labelWidth), "CPU Usage %");
                    GUI.Label(r.TakeFromRight(70), (int)(Generator.Options.CPU_Usage * Environment.ProcessorCount) + " threads");
                    Generator.Options.CPU_Usage = GUI.HorizontalSlider(r, Generator.Options.CPU_Usage, 0, 1);
                    GUI.changed = donTriggerChange;
                }
            }
            rect.TakeFromTop(10);

            DrawTitle(ref rect, "Profile");
            {
                {
                    var r = rect.TakeFromTop(lineHeight);
                    GUI.Label(r.TakeFromLeft(labelWidth), "Size / inv zoom");
                    GUI.Label(r.TakeFromRight(70), Generator.Options.Size.ToString());
                    Generator.Options.Size = (int)GUI.HorizontalSlider(r, Generator.Options.Size, 0, 10_000);
                }

                {
                    var r = rect.TakeFromTop(lineHeight);
                    GUI.Label(r.TakeFromLeft(labelWidth), "Center Biom");
                    GUI.Label(r.TakeFromRight(70), Generator.Options.CenterBiom.ToString());
                    Generator.Options.CenterBiom = (Biome)GUI.HorizontalSlider(r, (int)Generator.Options.CenterBiom, 1, (int)Biome.Max - 1);

                    r = rect.TakeFromTop(20);
                    r.y += 1;
                    DrawRect(r, GetColor(Generator.Options.CenterBiom));
                    DrawBorders(r);
                }
                DrawProfile(rect.TakeFromTop(40), Generator.TargetProfile);

                for (int i = 1; i < Generator.TargetProfile.Length - 1; i++)
                {
                    ref Profile item = ref Generator.TargetProfile[i];
                    var row = rect.TakeFromTop(lineHeight);

                    item.MustBeNextToTile = GUI.Toggle(row.TakeFromRight(row.height), item.MustBeNextToTile, "", GUI.skin.toggle);

                    DrawRect(row.TakeFromLeft(row.height), GetColor(item.Biom));
                    row.TakeFromLeft(10);
                    item.Enabled = GUI.Toggle(row.TakeFromLeft(90), item.Enabled, item.Biom.ToString(), GUI.skin.toggle);
                    if (item.Enabled)
                        item.Weight = GUI.HorizontalSlider(row, item.Weight, 0, 1);
                }
            }

            rect.TakeFromTop(10);

            if (GUI.Button(rect.TakeFromTop(lineHeight), "Normalize"))
            {
                var sum = Generator.TargetProfile.Sum(x => x.Weight * (x.Enabled ? 1 : 0));
                for (int j = 0; j < Generator.TargetProfile.Length; j++)
                {
                    ref var p = ref Generator.TargetProfile[j];
                    if (p.Weight > 0 && p.Enabled)
                        p.Weight /= sum;
                }
            }

            if (GUI.changed)
            {
                lock (Generator.QUEUE_LOCK)
                {
                    Generator.WorkQueue.Clear();
                    Generator.Options.OptionsVersion++;
                    Generator.Results.Clear();
                    Generator.SeedId = 0;
                    Generator.GenerationCount = 0;
                }
            }

            void DrawTitle(ref Rect rect, string title)
            {
                GUI.Label(rect.TakeFromTop(lineHeight).HorizontalPadding(-10).AlignTop(24), title, GUI.skin.box);
                rect.TakeFromTop(5);
            }
        }

        public static void DrawProfile(Rect rect, Profile[] profile)
        {
            if (profile == null)
                return;

            var orig = rect;
            foreach (var s in profile)
            {
                if (s.Enabled)
                {
                    var r = rect.TakeFromLeft(s.Weight * orig.width);
                    DrawRect(r, GetColor(s.Biom));
                }
            }

            DrawBorders(orig);
        }

        public static void DrawBorders(Rect orig)
        {
            DrawBorders(orig, new Color(0, 0, 0, 1));
        }

        public static void DrawBorders(Rect orig, Color col)
        {
            DrawRect(orig.TakeFromLeft(1), col);
            DrawRect(orig.TakeFromRight(1), col);
            DrawRect(orig.TakeFromTop(1), col);
            DrawRect(orig.TakeFromBottom(1), col);
        }

        public static void DrawRect(Rect rect, Color color)
        {
            var p = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = p;
        }

        public static Color GetColor(Biome b)
        {
            return b switch
            {
                Biome.None => Color.black,
                Biome.Meadows => new Color(0.57f, 0.65f, 0.36f, 1f),
                Biome.Swamp => new Color(0.64f, 0.44f, 0.34f, 1f),
                Biome.Mountain => new Color(0.8f, 0.8f, 0.8f, 1),
                Biome.BlackForest => new Color(0.2f, 0.37f, 0.23f, 1f),
                Biome.Plains => new Color(0.78f, 0.78f, 0.19f, 1f),
                Biome.AshLands => Color.red,
                Biome.DeepNorth => Color.white,
                Biome.Ocean => new Color(0f, 0f, 0.6f, 1f),
                Biome.Mistlands => new Color(0.32f, 0.32f, 0.32f, 1f),
                Biome.Shallows => new Color(0.4f, 0.4f, 1f, 1f),
                _ => Color.black,
            };
        }

        public static Rect TakeFromLeft(this ref Rect rect, float width)
        {
            var toTake = Math.Min(rect.width, width);
            var result = rect;
            result.width = toTake;
            rect.x += toTake;
            rect.width -= toTake;
            return result;
        }

        public static Rect TakeFromRight(this ref Rect rect, float width)
        {
            var toTake = Math.Min(rect.width, width);
            var result = rect.AlignRight(toTake);
            rect.width -= toTake;
            return result;
        }

        public static Rect TakeFromTop(this ref Rect rect, float height)
        {
            var toTake = Math.Min(rect.height, height);
            var result = rect;
            result.height = toTake;
            rect.y += toTake;
            rect.height -= toTake;
            return result;
        }

        public static Rect TakeFromBottom(this ref Rect rect, float height)
        {
            var toTake = Math.Min(rect.height, height);
            var result = rect.AlignBottom(toTake);
            rect.height -= height;
            return result;
        }

        public static Rect AlignRight(this Rect rect, float width)
        {
            rect.x = rect.x + rect.width - width;
            rect.width = width;
            return rect;
        }

        public static Rect AlignBottom(this Rect rect, float height)
        {
            rect.y = rect.y + rect.height - height;
            rect.height = height;
            return rect;
        }

        public static Rect HorizontalPadding(this Rect rect, float padding)
        {
            rect.x += padding;
            rect.width -= padding * 2f;
            return rect;
        }

        public static Rect VerticalPadding(this Rect rect, float padding)
        {
            rect.y += padding;
            rect.height -= padding * 2f;
            return rect;
        }

        public static Rect Padding(this Rect rect, float padding)
        {
            rect.position += new Vector2(padding, padding);
            rect.size -= new Vector2(padding, padding) * 2f;
            return rect;
        }

        public static Rect Padding(this Rect rect, float left, float right, float top, float bottom)
        {
            rect.position += new Vector2(left, top);
            rect.size -= new Vector2(left + right, top + bottom);
            return rect;
        }

        public static Rect AlignTop(this Rect rect, float height)
        {
            rect.height = height;
            return rect;
        }

        public static Rect Split(this Rect rect, int index, int count)
        {
            float cellSize = rect.width / count;
            rect.width = cellSize;
            rect.x += cellSize * index;
            return rect;
        }

        public static Rect SplitGrid(this Rect rect, float width, float height, int index)
        {
            int rowCount = (int)(rect.width / width);
            rowCount = rowCount > 0 ? rowCount : 1;
            int x = index % rowCount;
            int y = index / rowCount;

            rect.x += x * width;
            rect.y += y * height;
            rect.width = width;
            rect.height = height;
            return rect;
        }
    }

    public class Result
    {
        public string Seed;
        public float Score;
        public Profile[] Profile;
        public Texture2D Thumbnail;
        public WorldGenerator Gen;
        public int OptionsVersion;
        public Vector2 WorldPos;
        public float Size;
        public int WorldMapSize;
        public Biome CenterBiom;

        public void MakeThumbnail()
        {
            if (Thumbnail == null)
            {
                var sizeF = Size / 20_000;
                var thumbSize = 100;
                var colors = new Color[thumbSize * thumbSize];

                var i = 0;
                for (int y = 0; y < thumbSize; y++)
                {
                    for (int x = 0; x < thumbSize; x++)
                    {
                        var p = this.WorldPos;
                        var fx = x / (float)thumbSize;
                        var fy = y / (float)thumbSize;
                        p.x += (fx - 0.5f) * 20_000 * sizeF;
                        p.y += (fy - 0.5f) * 20_000 * sizeF;
                        var b1 = Gen.GetBiome(p.x, p.y);

                        if (Gen.GetBiomeHeight(b1, p.x, p.y, out _) < 29 && b1 != Biome.Ocean)
                            b1 = Biome.Shallows;

                        colors[i++] = GUITools.GetColor(b1);
                    }
                }

                var tex = new Texture2D(thumbSize, thumbSize);
                tex.SetPixels(colors);
                tex.Apply();
                this.Thumbnail = tex;
            }
        }
    }

    public struct Profile
    {
        public Biome Biom;
        public bool Enabled;
        public bool MustBeNextToTile;
        public float Weight;

        public Profile(Biome biom, bool enabled, float weight)
        {
            this.Biom = biom;
            this.Enabled = enabled;
            this.Weight = weight;
            this.MustBeNextToTile = false;
        }
    }

    public class Contour
    {
        public Vector2Int Center;
        public List<Vector2Int> points = new List<Vector2Int>();
    }

    public static class Generator
    {
        public static object QUEUE_LOCK = new object();
        public static List<Result> Results = new List<Result>();
        public static Queue<Result> WorkQueue = new Queue<Result>();
        public static bool IsRunning;
        public static float MaxScore;
        public static int SeedId = 0;
        public static int GenerationCount = 0;

        public static class Options
        {
            public static object OPTIONS_LOCK = new object();

            public static int WorldMapResolution = 120;
            public static string SeedPrefix = "vsf";
            public static int OptionsVersion;
            public static int Size = 1000;
            public static Biome CenterBiom = Biome.Meadows;
            public static float CPU_Usage = 0.5f;
        }


        public static Profile[] TargetProfile = new Profile[]
        {
            new Profile(Biome.None, false, 0.1f),
            new Profile(Biome.Meadows, false, 0.1f),
            new Profile(Biome.Swamp, false, 0.1f),
            new Profile(Biome.Mountain, false, 0.1f),
            new Profile(Biome.BlackForest, false, 0.1f),
            new Profile(Biome.Plains, false, 0.1f),
            new Profile(Biome.AshLands, false, 0.1f),
            new Profile(Biome.DeepNorth, false, 0.1f),
            new Profile(Biome.Ocean, false, 0.1f),
            new Profile(Biome.Mistlands, false, 0.1f),
            new Profile(Biome.Shallows, false, 0.1f),
            new Profile(Biome.Max, false, 0.1f)
        };

        public static void Start()
        {
            if (!IsRunning)
            {
                IsRunning = true;
                Application.quitting += () => { IsRunning = false; };

                new System.Threading.Thread(() =>
                {
                    while (IsRunning)
                    {
                        var todo = new List<Result>();
                        var profile = (Profile[])null;

                        lock (QUEUE_LOCK)
                        {
                            var CPU_Usage = Options.CPU_Usage;
                            var maxNumOfParralelWork = (int)(System.Environment.ProcessorCount * CPU_Usage);
                            var numOfParralelWork = Math.Min(WorkQueue.Count, maxNumOfParralelWork);
                            for (int i = 0; i < numOfParralelWork; i++)
                                todo.Add(WorkQueue.Dequeue());
                        }

                        lock (Options.OPTIONS_LOCK)
                        {
                            profile = TargetProfile.ToArray();
                            foreach (var item in todo)
                            {
                                item.Size = Options.Size;
                                item.CenterBiom = Options.CenterBiom;
                                item.WorldMapSize = Options.WorldMapResolution;
                                item.OptionsVersion = Options.OptionsVersion;
                            }
                        }

                        if (!IsValidProfile(profile))
                        {
                            Thread.Sleep(10);
                            continue;
                        }

                        System.Threading.Tasks.Parallel.ForEach(todo, (x) =>
                        {
                            Generate(x, profile);
                        });

                        lock (Options.OPTIONS_LOCK)
                        {
                            foreach (var best in todo.OrderByDescending(x => x.Score).Take(3))
                                if (best.OptionsVersion == Options.OptionsVersion)
                                    Results.Add(best);
                            Results = Results.OrderByDescending(x => x.Score).Take(20).ToList();
                        }

                    }
                }).Start();
            }
        }

        private static bool IsValidProfile(Profile[] profile)
        {
            return profile.Any(x => x.Enabled || x.MustBeNextToTile);
        }

        public static void Generate(Result r, Profile[] target)
        {
            var sizeFactor = r.Size / 20_000f;
            var worldMapSize = r.WorldMapSize;
            var bioms = new Biome[worldMapSize, worldMapSize];
            var centerBiomTiles = new HashSet<Vector2Int>();
            var contours = new List<Contour>();
            var halfSize = (int)((sizeFactor * worldMapSize) / 2);

            // Generate World Biom Map
            {
                for (int z = 0; z < worldMapSize; z++)
                {
                    var wz = ((z / (worldMapSize - 1f)) * 2 - 1) * 10_000;
                    for (int x = 0; x < worldMapSize; x++)
                    {
                        var wx = ((x / (worldMapSize - 1f)) * 2 - 1) * 10_000;
                        var outOfBounds = new Vector3(wx, 0, wz).magnitude > 10_000;
                        if (outOfBounds)
                        {
                            bioms[x, z] = Biome.None;
                        }
                        else
                        {
                            var biom = r.Gen.GetBiome(wx, wz);

                            if (r.Gen.GetBiomeHeight(biom, wx, wz, out _) < 29 && biom != Biome.Ocean)
                                biom = Biome.Shallows;

                            if (biom == r.CenterBiom)
                                centerBiomTiles.Add(new Vector2Int(x, z));

                            bioms[x, z] = biom;
                        }
                    }
                }
            }

            // Isolate Center Biom Contours
            {
                var flagsFilter = 0;
                for (int i = 0; i < target.Length; i++)
                    if (target[i].MustBeNextToTile)
                        flagsFilter |= 1 << i;

                while (centerBiomTiles.Count > 0)
                {
                    var contour = new Contour();
                    var openList = new Queue<Vector2Int>();
                    var closedList = new HashSet<Vector2Int>();
                    var first = centerBiomTiles.First();
                    openList.Enqueue(first);
                    closedList.Add(first);
                    while (openList.Count != 0)
                    {
                        var p = openList.Dequeue();
                        if (centerBiomTiles.Contains(p))
                        {
                            contour.points.Add(p);
                            contour.Center += p;
                            centerBiomTiles.Remove(p);
                            var a1 = p + new Vector2Int(-1, 0);
                            var a2 = p + new Vector2Int(+1, 0);
                            var a3 = p + new Vector2Int(0, +1);
                            var a4 = p + new Vector2Int(0, -1);
                            if (closedList.Add(a1)) openList.Enqueue(a1);
                            if (closedList.Add(a2)) openList.Enqueue(a2);
                            if (closedList.Add(a3)) openList.Enqueue(a3);
                            if (closedList.Add(a4)) openList.Enqueue(a4);
                        }
                    }

                    contour.Center /= contour.points.Count;

                    if (contour.points.Count > 5)
                    {
                        if (flagsFilter != 0)
                        {
                            var flagsContour = 0;
                            foreach (var item in contour.points)
                            {
                                var a1 = item + new Vector2Int(-1, 0);
                                var a2 = item + new Vector2Int(+1, 0);
                                var a3 = item + new Vector2Int(0, +1);
                                var a4 = item + new Vector2Int(0, -1);
                                flagsContour |= (1 << (int)bioms[a1.x, a1.y]);
                                flagsContour |= (1 << (int)bioms[a2.x, a2.y]);
                                flagsContour |= (1 << (int)bioms[a3.x, a3.y]);
                                flagsContour |= (1 << (int)bioms[a4.x, a4.y]);
                            }
                            if ((flagsContour & flagsFilter) == flagsFilter)
                            {
                                contours.Add(contour);
                            }
                        }
                        else
                        {
                            contours.Add(contour);
                        }
                    }
                }
            }

            // Find best contour
            {
                r.Score = 0; // It's already zero.. But...

                foreach (var contour in contours)
                {
                    var profile = new Profile[target.Length];

                    for (int x = -halfSize; x < halfSize; x++)
                        for (int z = -halfSize; z < halfSize; z++)
                        {
                            var p = contour.Center;
                            p.x += x;
                            p.y += z;

                            if (p.x >= 0 && p.x < worldMapSize && p.y >= 0 && p.y < worldMapSize)
                            {
                                var bo = bioms[p.x, p.y];
                                profile[(int)bo].Weight++;
                            }
                        }

                    var sum = profile.Sum(x => x.Weight);
                    for (int j = 0; j < profile.Length; j++)
                    {
                        ref var p = ref profile[j];
                        p.Biom = (Biome)j;
                        if (p.Weight > 0)
                        {
                            p.Enabled = true;
                            p.Weight /= sum;
                        }
                    }

                    var score = 0f;
                    var count = 0;
                    for (int i = 0; i < target.Length; i++)
                    {
                        if (target[i].Enabled)
                        {
                            score++;
                            count++;
                            var k = (int)target[i].Biom;
                            score -= Mathf.Abs(profile[k].Weight - target[i].Weight);
                        }
                    }

                    if (count > 0)
                        score /= count;

                    if (score > r.Score)
                    {
                        r.Score = score;
                        r.Profile = profile;
                        r.WorldPos = ((contour.Center - new Vector2(worldMapSize / 2, worldMapSize / 2)) / worldMapSize) * 20_000;
                    }
                }
            }

            Interlocked.Add(ref GenerationCount, 1);
        }
    }
}

namespace Valheim
{
    // Changes made to the Valheim source code:


    // 
    // * Generator is no longer static
    // * Biome enum is no longer a mask enum.
    // * In order to cut down on performance, lots of stuff have been removed. And a few variables has been hacked in.
    // * 
    // 

    public enum Biome
    {
        None,
        Meadows,
        Swamp,
        Mountain,
        BlackForest,
        Plains,
        AshLands,
        DeepNorth,
        Ocean,
        Mistlands,
        Shallows,
        Max,
    }

    public class World
    {
        internal int m_seed;
        internal int m_worldGenVersion = 2;
        public World(string seed)
        {
            this.m_seed = ((seed == "") ? 0 : GetStableHashCode(seed));
        }
        static int GetStableHashCode(string str)
        {
            int num = 5381;
            int num2 = num;
            int num3 = 0;
            while (num3 < str.Length && str[num3] != '\0')
            {
                num = ((num << 5) + num ^ (int)str[num3]);
                if (num3 == str.Length - 1 || str[num3 + 1] == '\0')
                {
                    break;
                }
                num2 = ((num2 << 5) + num2 ^ (int)str[num3 + 1]);
                num3 += 2;
            }
            return num + num2 * 1566083941;
        }
    }

    public class WorldGenerator
    {
        private World m_world;
        private int m_version;
        private float m_offset0;
        private float m_offset1;
        private float m_offset2;
        private float m_offset3;
        private int m_riverSeed;
        private int m_streamSeed;
        private float m_offset4;
        private float maxMarshDistance = 6000f;
        private float minDarklandNoise = 0.4f;
        private float m_minMountainDistance = 1000f;
        public const float worldSize = 10000f;
        public const float waterEdge = 10500f;
        public WorldGenerator(World world)
        {
            m_world = world;
            m_version = m_world.m_worldGenVersion;
            VersionSetup(m_version);
            UnityEngine.Random.State state = UnityEngine.Random.state;
            UnityEngine.Random.InitState(m_world.m_seed);
            m_offset0 = UnityEngine.Random.Range(-10000, 10000);
            m_offset1 = UnityEngine.Random.Range(-10000, 10000);
            m_offset2 = UnityEngine.Random.Range(-10000, 10000);
            m_offset3 = UnityEngine.Random.Range(-10000, 10000);
            m_riverSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            m_streamSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            m_offset4 = UnityEngine.Random.Range(-10000, 10000);
            UnityEngine.Random.state = state;
        }

        private void VersionSetup(int version)
        {
            if (version <= 0)
            {
                m_minMountainDistance = 1500f;
            }

            if (version <= 1)
            {
                minDarklandNoise = 0.5f;
                maxMarshDistance = 8000f;
            }
        }

        public BiomeArea GetBiomeArea(Vector3 point)
        {
            Biome biome = GetBiome(point);
            Biome biome2 = GetBiome(point - new Vector3(-64f, 0f, -64f));
            Biome biome3 = GetBiome(point - new Vector3(64f, 0f, -64f));
            Biome biome4 = GetBiome(point - new Vector3(64f, 0f, 64f));
            Biome biome5 = GetBiome(point - new Vector3(-64f, 0f, 64f));
            Biome biome6 = GetBiome(point - new Vector3(-64f, 0f, 0f));
            Biome biome7 = GetBiome(point - new Vector3(64f, 0f, 0f));
            Biome biome8 = GetBiome(point - new Vector3(0f, 0f, -64f));
            Biome biome9 = GetBiome(point - new Vector3(0f, 0f, 64f));
            if (biome == biome2 && biome == biome3 && biome == biome4 && biome == biome5 && biome == biome6 && biome == biome7 && biome == biome8 && biome == biome9)
            {
                return BiomeArea.Median;
            }

            return BiomeArea.Edge;
        }
        public Biome GetBiome(Vector3 point)
        {
            return GetBiome(point.x, point.z);
        }
        public float GetHeight(float wx, float wy)
        {
            Biome biome = GetBiome(wx, wy);
            Color mask;
            return GetBiomeHeight(biome, wx, wy, out mask);
        }
        public float GetBiomeHeight(Biome biome, float wx, float wy, out Color mask, bool preGeneration = false)
        {
            mask = Color.black;

            if (biome == Biome.Mountain)
            {
                return 60;
            }

            return GetMenuHeight(wx, wy) * 200f;
        }
        public Biome GetBiome(float wx, float wy)
        {
            //if (m_world.m_menu)
            //{
            //    if (GetBaseHeight(wx, wy, menuTerrain: true) >= 0.4f)
            //    {
            //        return Biome.Mountain;
            //    }

            //    return Biome.BlackForest;
            //}
            float magnitude = new Vector2(wx, wy).magnitude;
            float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
            float num = WorldAngle(wx, wy) * 100f;
            if (new Vector2(wx, wy + -4000f).magnitude > 12000f + num)
            {
                return Biome.AshLands;
            }

            if ((double)baseHeight <= 0.02)
            {
                return Biome.Ocean;
            }

            if (new Vector2(wx, wy + 4000f).magnitude > 12000f + num)
            {
                if (baseHeight > 0.4f)
                {
                    return Biome.Mountain;
                }

                return Biome.DeepNorth;
            }

            if (baseHeight > 0.4f)
            {
                return Biome.Mountain;
            }

            if (Mathf.PerlinNoise((m_offset0 + wx) * 0.001f, (m_offset0 + wy) * 0.001f) > 0.6f && magnitude > 2000f && magnitude < maxMarshDistance && baseHeight > 0.05f && baseHeight < 0.25f)
            {
                return Biome.Swamp;
            }

            if (Mathf.PerlinNoise((m_offset4 + wx) * 0.001f, (m_offset4 + wy) * 0.001f) > minDarklandNoise && magnitude > 6000f + num && magnitude < 10000f)
            {
                return Biome.Mistlands;
            }

            if (Mathf.PerlinNoise((m_offset1 + wx) * 0.001f, (m_offset1 + wy) * 0.001f) > 0.4f && magnitude > 3000f + num && magnitude < 8000f)
            {
                return Biome.Plains;
            }

            if (Mathf.PerlinNoise((m_offset2 + wx) * 0.001f, (m_offset2 + wy) * 0.001f) > 0.4f && magnitude > 600f + num && magnitude < 6000f)
            {
                return Biome.BlackForest;
            }

            if (magnitude > 5000f + num)
            {
                return Biome.BlackForest;
            }

            return Biome.Meadows;
        }
        private float WorldAngle(float wx, float wy)
        {
            return Mathf.Sin(Mathf.Atan2(wx, wy) * 20f);
        }
        private float GetBaseHeight(float wx, float wy, bool menuTerrain)
        {
            if (menuTerrain)
            {
                wx += 100000f + m_offset0;
                wy += 100000f + m_offset1;
                float num = 0f;
                num += Mathf.PerlinNoise(wx * 0.002f * 0.5f, wy * 0.002f * 0.5f) * Mathf.PerlinNoise(wx * 0.003f * 0.5f, wy * 0.003f * 0.5f) * 1f;
                num += Mathf.PerlinNoise(wx * 0.002f * 1f, wy * 0.002f * 1f) * Mathf.PerlinNoise(wx * 0.003f * 1f, wy * 0.003f * 1f) * num * 0.9f;
                num += Mathf.PerlinNoise(wx * 0.005f * 1f, wy * 0.005f * 1f) * Mathf.PerlinNoise(wx * 0.01f * 1f, wy * 0.01f * 1f) * 0.5f * num;
                return num - 0.07f;
            }

            float num2 = Length(wx, wy);
            wx += 100000f + m_offset0;
            wy += 100000f + m_offset1;
            float num3 = 0f;
            num3 += Mathf.PerlinNoise(wx * 0.002f * 0.5f, wy * 0.002f * 0.5f) * Mathf.PerlinNoise(wx * 0.003f * 0.5f, wy * 0.003f * 0.5f) * 1f;
            num3 += Mathf.PerlinNoise(wx * 0.002f * 1f, wy * 0.002f * 1f) * Mathf.PerlinNoise(wx * 0.003f * 1f, wy * 0.003f * 1f) * num3 * 0.9f;
            num3 += Mathf.PerlinNoise(wx * 0.005f * 1f, wy * 0.005f * 1f) * Mathf.PerlinNoise(wx * 0.01f * 1f, wy * 0.01f * 1f) * 0.5f * num3;
            num3 -= 0.07f;
            float num4 = Mathf.PerlinNoise(wx * 0.002f * 0.25f + 0.123f, wy * 0.002f * 0.25f + 0.15123f);
            float num5 = Mathf.PerlinNoise(wx * 0.002f * 0.25f + 0.321f, wy * 0.002f * 0.25f + 0.231f);
            float v = Mathf.Abs(num4 - num5);
            float num6 = 1f - LerpStep(0.02f, 0.12f, v);
            num6 *= SmoothStep(744f, 1000f, num2);
            num3 *= 1f - num6;
            if (num2 > 10000f)
            {
                float t = LerpStep(10000f, 10500f, num2);
                num3 = Mathf.Lerp(num3, -0.2f, t);
                float num7 = 10490f;
                if (num2 > num7)
                {
                    float t2 = LerpStep(num7, 10500f, num2);
                    num3 = Mathf.Lerp(num3, -2f, t2);
                }
            }

            if (num2 < m_minMountainDistance && num3 > 0.28f)
            {
                float t3 = Mathf.Clamp01((num3 - 0.28f) / 0.099999994f);
                num3 = Mathf.Lerp(Mathf.Lerp(0.28f, 0.38f, t3), num3, LerpStep(m_minMountainDistance - 400f, m_minMountainDistance, num2));
            }

            return num3;
        }
        private float GetMenuHeight(float wx, float wy)
        {
            float baseHeight = GetBaseHeight(wx, wy, menuTerrain: true);
            wx += 100000f + m_offset3;
            wy += 100000f + m_offset3;
            float num = Mathf.PerlinNoise(wx * 0.01f, wy * 0.01f) * Mathf.PerlinNoise(wx * 0.02f, wy * 0.02f);
            num += Mathf.PerlinNoise(wx * 0.05f, wy * 0.05f) * Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * num * 0.5f;
            return baseHeight + num * 0.1f + Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * 0.01f + Mathf.PerlinNoise(wx * 0.4f, wy * 0.4f) * 0.003f;
        }
        public static bool InForest(Vector3 pos)
        {
            return GetForestFactor(pos) < 1.15f;
        }
        public static float Length(float x, float y)
        {
            return Mathf.Sqrt(x * x + y * y);
        }
        public static float SmoothStep(float p_Min, float p_Max, float p_X)
        {
            float num = Mathf.Clamp01((p_X - p_Min) / (p_Max - p_Min));
            return num * num * (3f - 2f * num);
        }
        public static float GetForestFactor(Vector3 pos)
        {
            float num = 0.4f;
            return Fbm(pos * 0.01f * num, 3, 1.6f, 0.7f);
        }
        public int GetSeed()
        {
            return m_world.m_seed;
        }
        public static float Fbm(Vector3 p, int octaves, float lacunarity, float gain)
        {
            return Fbm(new Vector2(p.x, p.z), octaves, lacunarity, gain);
        }
        public static float Fbm(Vector2 p, int octaves, float lacunarity, float gain)
        {
            float num = 0f;
            float num2 = 1f;
            Vector2 vector = p;
            for (int i = 0; i < octaves; i++)
            {
                num += num2 * Mathf.PerlinNoise(vector.x, vector.y);
                num2 *= gain;
                vector *= lacunarity;
            }
            return num;
        }
        public static double LerpStep(double l, double h, double v)
        {
            return Clamp01((v - l) / (h - l));
        }
        public static float LerpStep(float l, float h, float v)
        {
            return Mathf.Clamp01((v - l) / (h - l));
        }
        public static double Clamp01(double v)
        {
            if (v > 1.0)
            {
                return 1.0;
            }
            if (v < 0.0)
            {
                return 0.0;
            }
            return v;
        }
    }

    public enum BiomeArea
    {
        Edge = 1,
        Median,
        Everything
    }
}
