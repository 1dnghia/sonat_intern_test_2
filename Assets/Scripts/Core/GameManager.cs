using System;
using System.Collections.Generic;
using TapAway.Core;
using TapAway.Data;
using TapAway.Infrastructure;
using UnityEngine;

namespace TapAway.Core
{
    public enum GameState
    {
        Idle,
        Playing,
        Won,
        Lost,
    }

    /// <summary>
    /// Central game manager. Owns the GridSystem and drives game flow.
    /// </summary>
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        // ── Serialized Fields ──────────────────────────────────
        [SerializeField, Tooltip("Current level data to load on Start")]
        private LevelData _currentLevelData;

        // ── Properties ────────────────────────────────────────
        public GameState State       { get; private set; } = GameState.Idle;
        public GridSystem Grid        { get; private set; }
        public int MovesRemaining     { get; private set; }
        public bool IsUnlimitedMoves  => MovesRemaining < 0;
        public LevelData CurrentLevel => _currentLevelData;

        // ── Events ────────────────────────────────────────────
        /// <summary>Fires after every valid tap. (result, tappedBlockId, blockingChain)</summary>
        public event Action<GridSystem.TapResult, int, List<Block>> OnBlockTapped;
        public event Action<int> OnMovesChanged;
        public event Action OnLevelLoaded;
        public event Action OnWin;
        public event Action OnLose;

        // ── Unity Lifecycle ───────────────────────────────────

        private void Start()
        {
            if (_currentLevelData != null)
                LoadLevel(_currentLevelData);
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>Load a level and begin play.</summary>
        public void LoadLevel(LevelData levelData)
        {
            _currentLevelData = levelData;

            var blocks = new List<Block>();
            for (int i = 0; i < levelData.blocks.Count; i++)
            {
                var bd = levelData.blocks[i];
                blocks.Add(new Block(i, bd.x, bd.y, bd.cellType, bd.direction));
            }

            Grid = new GridSystem(levelData.gridSize, blocks);
            MovesRemaining = levelData.movesLimit > 0 ? levelData.movesLimit : -1;
            State = GameState.Playing;

            OnLevelLoaded?.Invoke();
            OnMovesChanged?.Invoke(MovesRemaining);
        }

        /// <summary>
        /// Called when the player taps a grid cell.
        /// Returns false if the tap is invalid (Gear / empty) and no move is counted.
        /// </summary>
        public bool HandleTap(int x, int y)
        {
            if (State != GameState.Playing) return false;

            // Remember the block ID before it might be removed
            var tappedBlock = Grid.GetBlock(x, y);
            int tappedId = tappedBlock?.Id ?? -1;

            var result = Grid.TapBlock(x, y, out var chain);

            if (result == GridSystem.TapResult.Invalid)
                return false;

            // Count the move
            SpendMove();

            OnBlockTapped?.Invoke(result, tappedId, chain);

            if (Grid.IsWon())
            {
                State = GameState.Won;
                OnWin?.Invoke();
                return true;
            }

            if (!IsUnlimitedMoves && MovesRemaining <= 0)
            {
                State = GameState.Lost;
                OnLose?.Invoke();
            }

            return true;
        }

        /// <summary>Add extra moves (shop item: +5 Moves).</summary>
        public void AddMoves(int count)
        {
            if (IsUnlimitedMoves) return;
            MovesRemaining += count;
            OnMovesChanged?.Invoke(MovesRemaining);

            // Re-check for loss (if somehow below 0)
            if (State == GameState.Lost && MovesRemaining > 0)
                State = GameState.Playing;
        }

        /// <summary>Forcibly remove a block at (x,y) regardless of direction (shop: Remove Block).</summary>
        public bool RemoveBlock(int x, int y)
        {
            if (State != GameState.Playing) return false;
            var block = Grid.GetBlock(x, y);
            if (block == null || block.CellType != CellType.Normal) return false;

            // Directly destroy — treat as special move
            Grid.TapBlock(x, y, out _);

            OnBlockTapped?.Invoke(GridSystem.TapResult.Moved, block.Id, new List<Block>());

            if (Grid.IsWon())
            {
                State = GameState.Won;
                OnWin?.Invoke();
            }
            return true;
        }

        // ── Private ───────────────────────────────────────────

        private void SpendMove()
        {
            if (IsUnlimitedMoves) return;
            MovesRemaining--;
            OnMovesChanged?.Invoke(MovesRemaining);
        }
    }
}
