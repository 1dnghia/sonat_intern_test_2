using TapAway.Data;
using UnityEditor;
using UnityEngine;

namespace TapAway.Editor
{
    /// <summary>
    /// Creates the hand-crafted tutorial levels (Phase 1-4) based on level-design spec.
    /// Menu: TapAway → Create Hand-Craft Levels
    /// </summary>
    public static class HandCraftLevelCreator
    {
        private const string OUTPUT_PATH = "Assets/Data/Levels";

        [MenuItem("TapAway/Create Hand-Craft Levels")]
        public static void CreateAll()
        {
            EnsureFolder();

            CreateLevel1();
            CreateLevel2();
            CreateLevel3();
            CreateLevel4();
            CreateLevel5();
            // Gear intro
            CreateLevel6();
            // Rotator intro
            CreateLevel7();
            // Combined Gear + Rotator
            CreateLevel8();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HandCraftLevelCreator] Created hand-craft levels 1–8.");
        }

        // ── Phase 1: Only Normal Blocks (no moves limit) ─────

        /// <summary>Level 1 — 3x3, 4 blocks pointing outward, cannot block each other.</summary>
        private static void CreateLevel1()
        {
            var ld = Make(1, 3, 0);
            // Four blocks at edges of center, pointing out
            Add(ld, 0, 1, CellType.Normal, BlockDirection.Left);
            Add(ld, 2, 1, CellType.Normal, BlockDirection.Right);
            Add(ld, 1, 0, CellType.Normal, BlockDirection.Down);
            Add(ld, 1, 2, CellType.Normal, BlockDirection.Up);
            Save(ld);
        }

        /// <summary>Level 2 — 3x3, must clear top-then-side (simple chain).</summary>
        private static void CreateLevel2()
        {
            var ld = Make(2, 3, 0);
            Add(ld, 0, 2, CellType.Normal, BlockDirection.Up);   // free
            Add(ld, 1, 2, CellType.Normal, BlockDirection.Right); // blocked by (2,2)
            Add(ld, 2, 2, CellType.Normal, BlockDirection.Right); // free
            Add(ld, 1, 0, CellType.Normal, BlockDirection.Down);  // free
            Save(ld);
        }

        /// <summary>Level 3 — 3x3, chain A→B dependency.</summary>
        private static void CreateLevel3()
        {
            var ld = Make(3, 3, 0);
            // (0,1) blocked by (1,1); must remove (1,1) first
            Add(ld, 1, 1, CellType.Normal, BlockDirection.Right); // free
            Add(ld, 0, 1, CellType.Normal, BlockDirection.Right); // blocked by above
            Add(ld, 1, 2, CellType.Normal, BlockDirection.Up);    // free
            Add(ld, 2, 0, CellType.Normal, BlockDirection.Down);  // free
            Save(ld);
        }

        /// <summary>Level 4 — 3x3, longer chain A→B→C.</summary>
        private static void CreateLevel4()
        {
            var ld = Make(4, 3, 0);
            // Row: 0→1→2 all pointing Right, must clear right-to-left
            Add(ld, 2, 1, CellType.Normal, BlockDirection.Right); // free
            Add(ld, 1, 1, CellType.Normal, BlockDirection.Right); // blocked by (2,1)
            Add(ld, 0, 1, CellType.Normal, BlockDirection.Right); // blocked by (1,1)
            Add(ld, 1, 0, CellType.Normal, BlockDirection.Down);  // free
            Save(ld);
        }

        /// <summary>Level 5 — 3x3, simple puzzle needing ordering (last Normal-only level).</summary>
        private static void CreateLevel5()
        {
            var ld = Make(5, 3, 0);
            Add(ld, 0, 0, CellType.Normal, BlockDirection.Down);
            Add(ld, 0, 2, CellType.Normal, BlockDirection.Up);
            Add(ld, 1, 1, CellType.Normal, BlockDirection.Right); // free
            Add(ld, 2, 1, CellType.Normal, BlockDirection.Right); // free, must go before (1,1) is blocked
            Add(ld, 2, 2, CellType.Normal, BlockDirection.Up);
            Save(ld);
        }

        // ── Phase 2: Gear Introduction ───────────────────────

        /// <summary>Level 6 — single Gear, blocks directed into it get destroyed.</summary>
        private static void CreateLevel6()
        {
            var ld = Make(6, 3, 0);
            Add(ld, 1, 1, CellType.Gear,   BlockDirection.Up);    // Center gear
            Add(ld, 0, 1, CellType.Normal,  BlockDirection.Right); // →Gear → destroyed
            Add(ld, 1, 0, CellType.Normal,  BlockDirection.Up);    // ↑Gear → destroyed
            Add(ld, 2, 2, CellType.Normal,  BlockDirection.Up);    // free escape
            Save(ld);
        }

        // ── Phase 3: Rotator Introduction ────────────────────

        /// <summary>Level 7 — one Rotator, two connected Normal blocks.</summary>
        private static void CreateLevel7()
        {
            var ld = Make(7, 4, 0);
            // Rotator at (2,2); blocks at (1,2) and (2,1) — connected diagonally/adjacent
            Add(ld, 2, 2, CellType.Rotator, BlockDirection.Up);
            Add(ld, 1, 2, CellType.Normal,  BlockDirection.Left);  // adjacent to rotator
            Add(ld, 2, 1, CellType.Normal,  BlockDirection.Down);  // adjacent to rotator
            Add(ld, 0, 0, CellType.Normal,  BlockDirection.Left);  // standalone free block
            Save(ld);
        }

        // ── Phase 4: Gear + Rotator Combined ─────────────────

        /// <summary>Level 8 — Gear + Rotator, player must rotate block to escape via gap.</summary>
        private static void CreateLevel8()
        {
            var ld = Make(8, 4, 0);
            Add(ld, 2, 2, CellType.Gear,    BlockDirection.Up);    // Gear blocks Right escape
            Add(ld, 1, 1, CellType.Rotator, BlockDirection.Up);    // Rotator at (1,1)
            // Block at (0,1): points Right → blocked by Gear via rotate will free it
            Add(ld, 0, 1, CellType.Normal,  BlockDirection.Up);    // after rotate can escape
            Add(ld, 1, 0, CellType.Normal,  BlockDirection.Down);  // adjacant to rotator
            Add(ld, 3, 3, CellType.Normal,  BlockDirection.Right); // free
            Save(ld);
        }

        // ── Helpers ───────────────────────────────────────────

        private static LevelData Make(int index, int gridSize, int movesLimit)
        {
            var ld = ScriptableObject.CreateInstance<LevelData>();
            ld.levelIndex = index;
            ld.gridSize   = gridSize;
            ld.movesLimit = movesLimit;
            return ld;
        }

        private static void Add(LevelData ld, int x, int y, CellType ct, BlockDirection dir)
        {
            ld.blocks.Add(new BlockData { x = x, y = y, cellType = ct, direction = dir });
        }

        private static void Save(LevelData ld)
        {
            string path = $"{OUTPUT_PATH}/Level_{ld.levelIndex:D3}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(ld, path);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(OUTPUT_PATH))
                AssetDatabase.CreateFolder("Assets/Data", "Levels");
        }
    }
}
