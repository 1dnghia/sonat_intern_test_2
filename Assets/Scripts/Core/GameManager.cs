using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapAway.Core;

namespace TapAway
{
    public class GameManager : MonoBehaviour
    {
        private const int TARGET_FRAME_RATE = 60;
        private const float GET_MORE_WEIGHT_X2 = 0.40f;
        private const float GET_MORE_WEIGHT_X3 = 0.25f;
        private const float GET_MORE_WEIGHT_X4 = 0.20f;

        // Quản lý danh sách level và level hiện tại.
        [SerializeField] private LevelManager _levelManager;
        // View hiển thị grid và block trong scene.
        [SerializeField] private GridView _gridView;
        // HUD chính: level, moves, coin, và nút chức năng.
        [SerializeField] private HUDView _hudView;
        // Điều khiển camera để fit map theo kích thước grid.
        [SerializeField] private CameraController _cameraController;
        // Panel hiển thị khi thắng level.
        [SerializeField] private WinPanel _winPanel;
        // Panel hiển thị khi thua level.
        [SerializeField] private LosePanel _losePanel;

        [Header("Economy")]
        // Số coin cần để dùng bomb khi đã hết lượt miễn phí.
        [SerializeField, Min(0)] private int _bombCost = 100;
        // Số coin cần để mua thêm moves từ HUD khi đã hết lượt miễn phí.
        [SerializeField, Min(0)] private int _addMovesCost = 100;
        // Số moves được cộng khi mua thêm từ HUD.
        [SerializeField, Min(1)] private int _addMovesAmount = 10;
        // Số moves được cộng khi bấm thêm moves ở panel thua.
        [SerializeField, Min(1)] private int _losePanelAddMovesAmount = 10;
        // Coin thưởng khi chọn nhận thưởng thường ở panel thắng.
        [SerializeField, Min(1)] private int _winGetCoinReward = 10;
        // Coin gốc trước khi nhân hệ số ở nhánh Get More.
        [SerializeField, Min(1)] private int _winGetMoreBaseCoinReward = 10;

        [Header("Audio")]
        // Bật để giữ SFX tap gear riêng nếu muốn; mặc định dùng chung tap normal.
        [SerializeField] private bool _useNormalTapSfxForGear = true;

        private GridSystem _gridSystem;
        private readonly List<Block> _blockedChainBuffer = new List<Block>();
        private readonly List<Block> _softlockBlockedNormals = new List<Block>();
        private Coroutine _showWinWhenReadyCoroutine;
        private Coroutine _showLoseAfterSoftlockCoroutine;
        private Coroutine _evaluateWhenSettledCoroutine;
        private int _movesLeft;
        private bool _isBombArmed;
        private bool _hasUsedLoseAddMovesInCurrentLevel;
        private bool _isEndPanelVisible;
        private bool _isSoftlockSequencePlaying;

        private IEnumerator Start()
        {
            Application.targetFrameRate = TARGET_FRAME_RATE;

            _gridSystem = new GridSystem();

            CoinWallet.OnCoinChanged += OnCoinChanged;

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

            // Đợi level data preload xong trước khi gọi LoadCurrentLevel.
            while (_levelManager != null && !_levelManager.IsReady)
            {
                yield return null;
            }

            // Đợi prefab grid preload xong để tránh build thiếu prefab frame đầu.
            while (_gridView != null && !_gridView.IsReady)
            {
                yield return null;
            }

            LoadCurrentLevel();
        }

        private void OnDestroy()
        {
            CoinWallet.OnCoinChanged -= OnCoinChanged;

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

            StopEndStateCoroutines();
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

            if (_cameraController == null && Camera.main != null)
            {
                _cameraController = Camera.main.GetComponent<CameraController>();
            }

            if (_cameraController != null && _gridView != null)
            {
                _cameraController.SetupForGrid(levelData.width, levelData.height, _gridView.CellPitch);
            }

            _movesLeft = levelData.moveLimit > 0 ? levelData.moveLimit : -1;
            _isBombArmed = false;
            _hasUsedLoseAddMovesInCurrentLevel = false;
            _isEndPanelVisible = false;
            _isSoftlockSequencePlaying = false;

            StopEndStateCoroutines();

            RefreshHud();

            if (_winPanel != null) _winPanel.Show(false);
            if (_losePanel != null) _losePanel.Show(false);

            PlayGameplayBgm();
        }

        public void TapBlock(Block block)
        {
            if (block == null)
            {
                return;
            }

            if (_isEndPanelVisible || _isSoftlockSequencePlaying)
            {
                return;
            }

            // Khoá thao tác khi còn block đang prepare/remove để tránh cảm giác block xuyên nhau.
            if (_gridView != null && _gridView.HasRemovingBlocks())
            {
                return;
            }

            if (_isBombArmed)
            {
                if (TryConsumeBombCharge())
                {
                    UseBombAt(block.Position);
                }

                _isBombArmed = false;
                RefreshHud();
                EvaluateState();
                return;
            }

            if (block.Type == CellType.Gear)
            {
                if (AudioManager.Instance != null)
                {
                    if (_useNormalTapSfxForGear)
                    {
                        AudioManager.Instance.PlayTapNormal();
                    }
                }

                if (_gridView != null)
                {
                    _gridView.PlayGearTapFeedback(block);
                }

                return;
            }

            if (!HasMovesRemaining())
            {
                return;
            }

            bool shouldConsumeMove = block.Type == CellType.Normal || block.Type == CellType.Rotator;
            if (!shouldConsumeMove)
            {
                return;
            }

            if (block.Type == CellType.Rotator)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayTapRotator();
                }

                _gridSystem.TryTapRotator(block);
            }
            else if (block.Type == CellType.Normal)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayTapNormal();
                }

                Vector2Int tapStartPosition = block.Position;
                bool moved = _gridSystem.TryTapNormal(block, _blockedChainBuffer);
                if (_gridView != null && _blockedChainBuffer.Count > 0)
                {
                    bool colorPrimaryBlocker = IsPrimaryBlockerAdjacent(tapStartPosition, block.Direction, _blockedChainBuffer);
                    float impactDelay = 0f;
                    if (moved && tapStartPosition != block.Position)
                    {
                        if (_gridView.TryEstimateBlockTravelDuration(block, tapStartPosition, block.Position, out float travelDuration))
                        {
                            // Rung khi block gần tới điểm va chạm để tạo cảm giác impact.
                            impactDelay = Mathf.Max(0f, travelDuration * 0.85f);
                        }
                    }

                    _gridView.PlayBlockedChainFeedback(_blockedChainBuffer, colorPrimaryBlocker, impactDelay);
                }
            }

            ConsumeMove();
            RefreshHud();
            EvaluateState();
        }

        public void TapGridCell(Vector2Int cell)
        {
            if (!_isBombArmed || _isEndPanelVisible || _isSoftlockSequencePlaying)
            {
                return;
            }

            if (_gridView != null && _gridView.HasRemovingBlocks())
            {
                return;
            }

            if (!TryConsumeBombCharge())
            {
                _isBombArmed = false;
                RefreshHud();
                return;
            }

            UseBombAt(cell);
            _isBombArmed = false;
            RefreshHud();
            EvaluateState();
        }

        public void AddMoves(int extraMoves)
        {
            if (_movesLeft < 0)
            {
                return;
            }

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

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBombExplode();
            }

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    Vector2Int pos = new Vector2Int(center.x + x, center.y + y);
                    if (_gridSystem.TryGetBlock(pos, out Block block))
                    {
                        if (!block.IsRemoved && block.Type == CellType.Normal)
                        {
                            block.TryRemove(BlockRemoveReason.Bomb);
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
            RefreshBoosterHudState();
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

            if (RemainingNormalCount() <= 0)
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

            if (HasMovesRemaining() && TryStartSoftlockLoseSequence())
            {
                return;
            }

            if (_movesLeft <= 0)
            {
                ShowLosePanel();
            }
        }

        private int RemainingNormalCount()
        {
            if (_gridSystem == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _gridSystem.Blocks.Count; i++)
            {
                Block block = _gridSystem.Blocks[i];
                if (block == null || block.IsRemoved || block.Type != CellType.Normal)
                {
                    continue;
                }

                count++;
            }

            return count;
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
            _isEndPanelVisible = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWinBgm();
            }

            if (_winPanel != null)
            {
                _winPanel.Show(true);
            }

            if (_losePanel != null)
            {
                _losePanel.Show(false);
            }
        }

        private void ShowLosePanel()
        {
            _isEndPanelVisible = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLoseBgm();
            }

            if (_showWinWhenReadyCoroutine != null)
            {
                StopCoroutine(_showWinWhenReadyCoroutine);
                _showWinWhenReadyCoroutine = null;
            }

            if (_losePanel != null)
            {
                _losePanel.SetAddMovesVisible(!_hasUsedLoseAddMovesInCurrentLevel);
                _losePanel.Show(true);
            }

            if (_winPanel != null)
            {
                _winPanel.Show(false);
            }
        }

        private bool TryStartSoftlockLoseSequence()
        {
            if (_gridView != null && _gridView.HasRemovingBlocks())
            {
                EnsureEvaluateWhenSettled();
                return false;
            }

            _softlockBlockedNormals.Clear();
            _blockedChainBuffer.Clear();

            bool hasNormal = false;
            for (int i = 0; i < _gridSystem.Blocks.Count; i++)
            {
                Block block = _gridSystem.Blocks[i];
                if (block.IsRemoved || block.Type != CellType.Normal)
                {
                    continue;
                }

                hasNormal = true;
                if (_gridSystem.CanTapNormalChangeState(block))
                {
                    return false;
                }

                _softlockBlockedNormals.Add(block);
            }

            if (!hasNormal)
            {
                return false;
            }

            if (_gridSystem.CanAnyRotatorMove())
            {
                return false;
            }

            if (_showLoseAfterSoftlockCoroutine != null)
            {
                return true;
            }

            float duration = 0f;
            if (_gridView != null)
            {
                duration = _gridView.PlaySoftlockFeedback(_softlockBlockedNormals);
            }

            _showLoseAfterSoftlockCoroutine = StartCoroutine(ShowLoseAfterDelay(Mathf.Max(0f, duration)));
            _isSoftlockSequencePlaying = true;
            return true;
        }

        private void EnsureEvaluateWhenSettled()
        {
            if (_evaluateWhenSettledCoroutine != null)
            {
                return;
            }

            _evaluateWhenSettledCoroutine = StartCoroutine(EvaluateWhenBlocksSettled());
        }

        private IEnumerator EvaluateWhenBlocksSettled()
        {
            while (_gridView != null && _gridView.HasRemovingBlocks())
            {
                yield return null;
            }

            _evaluateWhenSettledCoroutine = null;

            if (_isEndPanelVisible)
            {
                yield break;
            }

            EvaluateState();
        }

        private static bool IsPrimaryBlockerAdjacent(
            Vector2Int tappedStartPosition,
            BlockDirection tappedDirection,
            IReadOnlyList<Block> blockerChain)
        {
            if (blockerChain == null || blockerChain.Count == 0)
            {
                return false;
            }

            Block primaryBlocker = blockerChain[0];
            if (primaryBlocker == null)
            {
                return false;
            }

            Vector2Int expectedAdjacentPos = tappedStartPosition + DirectionToOffset(tappedDirection);
            return primaryBlocker.Position == expectedAdjacentPos;
        }

        private static Vector2Int DirectionToOffset(BlockDirection direction)
        {
            switch (direction)
            {
                case BlockDirection.Right:
                    return Vector2Int.right;
                case BlockDirection.Down:
                    return Vector2Int.down;
                case BlockDirection.Left:
                    return Vector2Int.left;
                default:
                    return Vector2Int.up;
            }
        }

        private IEnumerator ShowLoseAfterDelay(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            ShowLosePanel();
            _isSoftlockSequencePlaying = false;
            _showLoseAfterSoftlockCoroutine = null;
        }

        private void StopEndStateCoroutines()
        {
            if (_showWinWhenReadyCoroutine != null)
            {
                StopCoroutine(_showWinWhenReadyCoroutine);
                _showWinWhenReadyCoroutine = null;
            }

            if (_showLoseAfterSoftlockCoroutine != null)
            {
                StopCoroutine(_showLoseAfterSoftlockCoroutine);
                _showLoseAfterSoftlockCoroutine = null;
            }

            if (_evaluateWhenSettledCoroutine != null)
            {
                StopCoroutine(_evaluateWhenSettledCoroutine);
                _evaluateWhenSettledCoroutine = null;
            }

            _isSoftlockSequencePlaying = false;
        }

        private void RefreshBoosterHudState()
        {
            if (_hudView == null)
            {
                return;
            }

            int bombFreeCount = Mathf.Max(0, PlayerProgressStore.BombFreeCount);
            int addMovesFreeCount = Mathf.Max(0, PlayerProgressStore.AddMovesFreeCount);
            _hudView.SetBombState(_isBombArmed, bombFreeCount, _bombCost, CoinWallet.Balance >= _bombCost);
            _hudView.SetAddMovesState(addMovesFreeCount, _addMovesCost, CoinWallet.Balance >= _addMovesCost);
        }

        private static int ClampMultiplierToAllowedRange(int multiplier)
        {
            if (multiplier < 2)
            {
                return 2;
            }

            if (multiplier > 5)
            {
                return 5;
            }

            return multiplier;
        }

        private int RollGetMoreMultiplier()
        {
            float randomValue = Random.value;
            if (randomValue < GET_MORE_WEIGHT_X2)
            {
                return 2;
            }

            if (randomValue < GET_MORE_WEIGHT_X2 + GET_MORE_WEIGHT_X3)
            {
                return 3;
            }

            if (randomValue < GET_MORE_WEIGHT_X2 + GET_MORE_WEIGHT_X3 + GET_MORE_WEIGHT_X4)
            {
                return 4;
            }

            return 5;
        }

        private bool TryConsumeBombCharge()
        {
            if (PlayerProgressStore.BombFreeCount > 0)
            {
                PlayerProgressStore.ConsumeBombFreeCount();
                return true;
            }

            return CoinWallet.TrySpend(_bombCost);
        }

        private bool CanUseBombCharge()
        {
            if (PlayerProgressStore.BombFreeCount > 0)
            {
                return true;
            }

            return CoinWallet.Balance >= _bombCost;
        }

        private bool TryConsumeAddMovesCharge()
        {
            if (PlayerProgressStore.AddMovesFreeCount > 0)
            {
                PlayerProgressStore.ConsumeAddMovesFreeCount();
                return true;
            }

            return CoinWallet.TrySpend(_addMovesCost);
        }

        private void PlayGameplayBgm()
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            AudioManager.Instance.PlayGameplayBgm();
        }

        private void OnCoinChanged(int coin)
        {
            RefreshBoosterHudState();
        }

        private void OnHudRetryPressed()
        {
            Retry();
        }

        private void OnHudBombPressed()
        {
            if (_isBombArmed)
            {
                _isBombArmed = false;
                RefreshHud();
                return;
            }

            if (CanUseBombCharge())
            {
                _isBombArmed = true;
                RefreshHud();
            }
        }

        private void OnHudAddMovesPressed()
        {
            if (_isEndPanelVisible || _movesLeft < 0)
            {
                return;
            }

            if (TryConsumeAddMovesCharge())
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

            _isEndPanelVisible = false;
            NextLevel();
        }

        private void OnWinGetMorePressed()
        {
            int multiplier = ClampMultiplierToAllowedRange(RollGetMoreMultiplier());
            int reward = _winGetMoreBaseCoinReward * multiplier;
            CoinWallet.Add(reward);

            if (_winPanel != null)
            {
                _winPanel.Show(false);
            }

            _isEndPanelVisible = false;
            NextLevel();
        }

        private void OnLoseRetryPressed()
        {
            _isEndPanelVisible = false;
            Retry();
        }

        private void OnLoseAddMovesPressed()
        {
            if (_hasUsedLoseAddMovesInCurrentLevel)
            {
                return;
            }

            _hasUsedLoseAddMovesInCurrentLevel = true;
            AddMoves(_losePanelAddMovesAmount);

            if (_losePanel != null)
            {
                _losePanel.Show(false);
                _losePanel.SetAddMovesVisible(false);
            }

            _isEndPanelVisible = false;
            PlayGameplayBgm();
            RefreshHud();
        }
    }
}
