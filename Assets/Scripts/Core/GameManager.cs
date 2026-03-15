using TapAway.Core;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace TapAway
{
    public class GameManager : MonoBehaviour
    {
        private const int TARGET_FRAME_RATE = 60;

        // Quản lý danh sách level và level hiện tại.
        [SerializeField] private LevelManager _levelManager;
        // View hiển thị grid và block trong scene.
        [SerializeField] private GridView _gridView;
        // HUD chính: level, moves, coin, và nút chức năng.
        [SerializeField] private HUDView _hudView;
        // Panel hiển thị khi thắng level.
        [SerializeField] private WinPanel _winPanel;
        // Panel hiển thị khi thua level.
        [SerializeField] private LosePanel _losePanel;

        [Header("Economy")]
        // Số coin cần để dùng bomb.
        [SerializeField, Min(0)] private int _bombCost = 10;
        // Số coin cần để mua thêm moves từ HUD.
        [SerializeField, Min(0)] private int _addMovesCost = 10;
        // Số moves được cộng khi mua thêm từ HUD.
        [SerializeField, Min(1)] private int _addMovesAmount = 10;
        // Số moves được cộng khi bấm thêm moves ở panel thua.
        [SerializeField, Min(1)] private int _losePanelAddMovesAmount = 10;
        // Coin thưởng khi chọn nhận thưởng thường ở panel thắng.
        [SerializeField, Min(1)] private int _winGetCoinReward = 10;
        // Coin thưởng khi chọn nhận thưởng "Get More" ở panel thắng.
        [SerializeField, Min(1)] private int _winGetMoreCoinReward = 30;

        private GridSystem _gridSystem;
        private readonly List<Block> _blockedChainBuffer = new List<Block>();
        private Coroutine _showWinWhenReadyCoroutine;
        private int _movesLeft;
        private bool _isBombArmed;

        private void Start()
        {
            Application.targetFrameRate = TARGET_FRAME_RATE;

            _gridSystem = new GridSystem();

            if (_hudView != null)
            {
                _hudView.RetryPressed += OnHudRetryPressed;
                _hudView.BombPressed += OnHudBombPressed;
                _hudView.AddMovesPressed += OnHudAddMovesPressed;
            }

            if (_winPanel != null)
            {
                _winPanel.GetPressed += OnWinGetPressed;
                _winPanel.GetMorePressed += OnWinGetMorePressed;
            }

            if (_losePanel != null)
            {
                _losePanel.RetryPressed += OnLoseRetryPressed;
                _losePanel.AddMovesPressed += OnLoseAddMovesPressed;
            }

            LoadCurrentLevel();
        }

        private void OnDestroy()
        {
            if (_hudView != null)
            {
                _hudView.RetryPressed -= OnHudRetryPressed;
                _hudView.BombPressed -= OnHudBombPressed;
                _hudView.AddMovesPressed -= OnHudAddMovesPressed;
            }

            if (_winPanel != null)
            {
                _winPanel.GetPressed -= OnWinGetPressed;
                _winPanel.GetMorePressed -= OnWinGetMorePressed;
            }

            if (_losePanel != null)
            {
                _losePanel.RetryPressed -= OnLoseRetryPressed;
                _losePanel.AddMovesPressed -= OnLoseAddMovesPressed;
            }
        }

        public void LoadCurrentLevel()
        {
            if (_levelManager == null)
            {
                return;
            }

            LevelData levelData = _levelManager.GetCurrentLevel();
            if (levelData == null)
            {
                return;
            }

            _gridSystem.Build(levelData);
            if (_gridView != null)
            {
                _gridView.Build(_gridSystem, levelData.visualTheme, levelData.trailBindingIndex);
            }

            _movesLeft = levelData.moveLimit > 0 ? levelData.moveLimit : -1;
            _isBombArmed = false;

            if (_showWinWhenReadyCoroutine != null)
            {
                StopCoroutine(_showWinWhenReadyCoroutine);
                _showWinWhenReadyCoroutine = null;
            }

            RefreshHud();

            if (_winPanel != null) _winPanel.Show(false);
            if (_losePanel != null) _losePanel.Show(false);
        }

        public void TapBlock(Block block)
        {
            if (block == null)
            {
                return;
            }

            if (!HasMovesRemaining())
            {
                return;
            }

            if (_isBombArmed)
            {
                UseBombAt(block.Position);
                _isBombArmed = false;
                EvaluateState();
                return;
            }

            bool shouldConsumeMove = block.Type == CellType.Normal || block.Type == CellType.Rotator;
            if (!shouldConsumeMove)
            {
                return;
            }

            if (block.Type == CellType.Rotator)
            {
                _gridSystem.TryTapRotator(block);
            }
            else if (block.Type == CellType.Normal)
            {
                _gridSystem.TryTapNormal(block, _blockedChainBuffer);
            }

            ConsumeMove();
            RefreshHud();
            EvaluateState();
        }

        public void AddMoves(int extraMoves)
        {
            _movesLeft += Mathf.Max(0, extraMoves);
            RefreshHud();
        }

        public void RemoveOneBlock()
        {
            if (_gridSystem == null)
            {
                return;
            }

            for (int i = 0; i < _gridSystem.Blocks.Count; i++)
            {
                Block block = _gridSystem.Blocks[i];
                if (!block.IsRemoved && block.Type == CellType.Normal)
                {
                    block.TryRemove();
                    EvaluateState();
                    return;
                }
            }
        }

        public void UseBombAt(Vector2Int center)
        {
            if (_gridSystem == null)
            {
                return;
            }

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    Vector2Int pos = new Vector2Int(center.x + x, center.y + y);
                    if (_gridSystem.TryGetBlock(pos, out Block block))
                    {
                        if (!block.IsRemoved && (block.Type == CellType.Normal || block.Type == CellType.Rotator))
                        {
                            block.TryRemove();
                        }
                    }
                }
            }
        }

        public void NextLevel()
        {
            if (_levelManager == null)
            {
                return;
            }

            _levelManager.NextLevel();
            LoadCurrentLevel();
        }

        public void Retry()
        {
            LoadCurrentLevel();
        }

        private void RefreshHud()
        {
            if (_hudView == null || _levelManager == null)
            {
                return;
            }

            _hudView.SetLevel(_levelManager.CurrentLevelIndex + 1);
            _hudView.SetMoves(_movesLeft);
            _hudView.SetCoin(CoinWallet.Balance);
        }

        private bool HasMovesRemaining()
        {
            return _movesLeft < 0 || _movesLeft > 0;
        }

        private void ConsumeMove()
        {
            if (_movesLeft > 0)
            {
                _movesLeft--;
            }
        }

        private void EvaluateState()
        {
            if (_gridSystem == null)
            {
                return;
            }

            if (_gridSystem.RemainingRemovableCount() <= 0)
            {
                if (_gridView != null && _gridView.HasRemovingBlocks())
                {
                    if (_showWinWhenReadyCoroutine == null)
                    {
                        _showWinWhenReadyCoroutine = StartCoroutine(ShowWinPanelWhenBlocksSettled());
                    }
                }
                else
                {
                    ShowWinPanel();
                }

                return;
            }

            if (_movesLeft <= 0)
            {
                if (_showWinWhenReadyCoroutine != null)
                {
                    StopCoroutine(_showWinWhenReadyCoroutine);
                    _showWinWhenReadyCoroutine = null;
                }

                if (_losePanel != null) _losePanel.Show(true);
                if (_winPanel != null) _winPanel.Show(false);
            }
        }

        private IEnumerator ShowWinPanelWhenBlocksSettled()
        {
            while (_gridView != null && _gridView.HasRemovingBlocks())
            {
                yield return null;
            }

            ShowWinPanel();
            _showWinWhenReadyCoroutine = null;
        }

        private void ShowWinPanel()
        {
            if (_winPanel != null)
            {
                _winPanel.Show(true);
            }

            if (_losePanel != null)
            {
                _losePanel.Show(false);
            }
        }

        private void OnHudRetryPressed()
        {
            Retry();
        }

        private void OnHudBombPressed()
        {
            if (CoinWallet.TrySpend(_bombCost))
            {
                _isBombArmed = true;
                RefreshHud();
            }
        }

        private void OnHudAddMovesPressed()
        {
            if (CoinWallet.TrySpend(_addMovesCost))
            {
                AddMoves(_addMovesAmount);
                RefreshHud();
            }
        }

        private void OnWinGetPressed()
        {
            CoinWallet.Add(_winGetCoinReward);
            if (_winPanel != null)
            {
                _winPanel.Show(false);
            }
            NextLevel();
        }

        private void OnWinGetMorePressed()
        {
            // Placeholder for rewarded ads callback; currently grants instantly.
            CoinWallet.Add(_winGetMoreCoinReward);
            if (_winPanel != null)
            {
                _winPanel.Show(false);
            }
            NextLevel();
        }

        private void OnLoseRetryPressed()
        {
            Retry();
        }

        private void OnLoseAddMovesPressed()
        {
            // Placeholder for rewarded ads callback; currently grants instantly.
            AddMoves(_losePanelAddMovesAmount);
            if (_losePanel != null)
            {
                _losePanel.Show(false);
            }
        }
    }
}
