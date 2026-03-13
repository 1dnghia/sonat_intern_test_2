---
applyTo: "**"
---

# Tap Away 2D: Remove Blocks — Level Generator

## 1. TỔNG QUAN

- Tool gen level chạy trong **Unity Editor** (MenuItem)
- Xuất ra **ScriptableObject** lưu sẵn trong project
- Player chỉ load file có sẵn, không gen runtime
- Xử lý trùng lặp bằng **HashSet**

---

## 2. PIPELINE GEN LEVEL

```
1. Đọc DifficultyParams từ ScriptableObject
2. Tính blockCount, gearCount, rotatorCount
3. Đặt Gear → Rotator → Normal Block lên grid
4. Chạy BFS Solver → lấy các metric
5. Tính Difficulty Score
6. Kiểm tra Score có trong target range không
7. Kiểm tra trùng lặp bằng HashSet
8. Nếu pass → set movesLimit → xuất ScriptableObject
9. Nếu fail → gen lại (tối đa 1000 lần)
10. Thất bại sau 1000 lần → dùng fallback level
```

---

## 3. CÔNG THỨC PHÂN BỔ SỐ LƯỢNG

```
blockCount   = round(blockDensity × gridSize²)
gearCount    = round(gearRatio × blockCount)
rotatorCount = round(rotatorRatio × blockCount)
movesLimit   = optimalMoves + movesBuffer
```

---

## 4. PHÂN BỔ VỊ TRÍ TRÊN GRID

### Gear ⚙️
- Ưu tiên đặt ở **trung tâm grid** (khó né hơn)
- Không đặt sát biên (block dễ né)

### Rotator 🔄
- Ưu tiên vị trí có **nhiều ô trống xung quanh**
- Cần ít nhất **2 block liền kề** mới có tác dụng
- Không đặt sát góc (ít block kết nối)

### Normal Block
- Phân bổ đều, không tập trung 1 góc
- Ít nhất **50% block phải bị chặn** bởi block khác
- Direction có **trọng số theo vị trí gần biên**:

| Vị trí block | Tăng xác suất hướng |
|---|---|
| Gần biên trái | Left |
| Gần biên phải | Right |
| Gần biên trên | Up |
| Gần biên dưới | Down |
| Trung tâm | Random đều 4 hướng |

---

## 5. BFS SOLVER

Solver chạy để:
- Kiểm tra level **có lời giải không**
- Lấy `optimalMoves` (số bước tối ưu)
- Lấy `maxChainDepth` (độ sâu chain dài nhất)
- Lấy `branchingFactor` (trung bình lựa chọn mỗi bước)
- Lấy `freeBlockAtStart` (số block free ngay từ đầu)
- Lấy `lockedBlocks` (số block bị chặn)

### Encode state để tránh thăm lại:
```csharp
string Encode() {
    // Sắp xếp theo vị trí để đảm bảo unique
    var blocks = blocks
        .OrderBy(b => b.x * 100 + b.y)
        .Select(b => $"{b.x},{b.y},{b.dir}");
    return string.Join("|", blocks);
}
```

---

## 6. CÔNG THỨC TÍNH DIFFICULTY SCORE

```
Difficulty Score =
    (normalizedMoves      × 4.0)
  + (normalizedGridSize   × 3.0)
  + (blockDensity         × 3.0)
  + (lockedBlockRatio     × 2.0)
  + (normalizedChainDepth × 3.0)
  + (normalizedBranching  × 3.0)
  + (log(gearCount + 1)   × 2.0)
  + (log(rotatorCount + 1)× 3.0)
  - (normalizedFreeBlock  × 3.0)
```

### Giải thích từng thông số:

```
normalizedMoves (×4.0) — SỐ BƯỚC TỐI ƯU
  Tỉ lệ số bước tối ưu so với tổng có thể tap tối đa
  Càng nhiều bước → càng khó
  clamp(optimalMoves / (blockCount × 2), 0, 1)
  Chia blockCount×2 vì thực tế player có thể tap sai

normalizedGridSize (×3.0) — KÍCH THƯỚC MAP
  Map lớn hơn = phức tạp hơn, dùng sqrt để tăng chậm dần
  sqrt((gridSize - 3) / (7 - 3))
  3x3→0.00, 4x4→0.50, 5x5→0.71, 6x6→0.87, 7x7→1.00

blockDensity (×3.0) — MẬT ĐỘ BLOCK TRÊN MAP
  Tỉ lệ ô có block so với tổng ô, map đặc → khó hơn
  blockCount / gridSize²  → tự nhiên trong [0,1]

lockedBlockRatio (×2.0) — TỈ LỆ BLOCK BỊ KHÓA
  Tỉ lệ block bị chặn, không thể thoát ngay
  Càng nhiều block bị khóa → càng phải suy nghĩ thứ tự
  lockedBlocks / blockCount  → [0,1]

normalizedChainDepth (×3.0) — ĐỘ SÂU CHUỖI PHỤ THUỘC
  Độ dài chain dài nhất (A chặn B chặn C...)
  Chain sâu → phải lên kế hoạch trước nhiều hơn
  clamp(maxChainDepth / blockCount, 0, 1)

normalizedBranching (×3.0) — ĐỘ PHỨC TẠP QUYẾT ĐỊNH
  Trung bình số lựa chọn hợp lệ mỗi bước
  Nhiều lựa chọn → khó biết bước nào đúng
  Dùng log để tránh giá trị quá lớn khi branching cao
  clamp(log(branchingFactor + 1) / log(7), 0, 1)
  branchingFactor = totalPlayableMoves / optimalMoves

log(gearCount + 1) × 2.0 — SỐ BÁNH RĂNG
  Dùng log vì Gear thứ 5-6 không khó hơn Gear 1-2 nhiều
  gear=1→1.39, gear=2→2.20, gear=4→3.22

log(rotatorCount + 1) × 3.0 — SỐ KHỐI XOAY
  Rotator phức tạp hơn Gear, trọng số cao hơn (×3 vs ×2)
  Dùng log tương tự Gear
  rotator=1→2.08, rotator=2→3.30, rotator=3→4.16

normalizedFreeBlock (×3.0) — TỈ LỆ BLOCK TỰ DO (trừ điểm)
  Block thoát được ngay không cần giải phóng → level dễ hơn
  clamp(freeBlockAtStart / blockCount, 0, 1)
  Clamp để tránh edge case khi generator bug
```

---

## 7. XỬ LÝ TRÙNG LẶP

```csharp
// Hash level thành string duy nhất
string Hash(LevelData level) {
    var blocks = level.blocks
        .OrderBy(b => b.position.x * 100 + b.position.y)
        .Select(b => $"{b.position.x},{b.position.y},{b.direction}");
    var gears = level.gears
        .OrderBy(g => g.x * 100 + g.y)
        .Select(g => $"G{g.x},{g.y}");
    var rotators = level.rotators
        .OrderBy(r => r.x * 100 + r.y)
        .Select(r => $"R{r.x},{r.y}");

    return $"{level.gridSize}|"
         + string.Join("|", blocks) + "|"
         + string.Join("|", gears) + "|"
         + string.Join("|", rotators);
}

// Trùng → gen lại, KHÔNG tính vào retry limit
// Không trùng → lưu vào HashSet + xuất file
```

---

## 8. DIFFICULTY PARAMS SCRIPTABLEOBJECT

```csharp
[CreateAssetMenu(menuName = "TapAway/DifficultyConfig")]
public class DifficultyConfig : ScriptableObject {
    public List<DifficultyRange> ranges;
}

[Serializable]
public class DifficultyRange {
    public int levelFrom;
    public int levelTo;
    public int gridSize;
    public float blockDensityMin;
    public float blockDensityMax;
    public float gearRatio;
    public float rotatorRatio;
    public int movesBuffer;       // 0 = unlimited
    public float targetScoreMin;  // Difficulty Score tối thiểu
    public float targetScoreMax;  // Difficulty Score tối đa
    public int minChainDepth;
    public int maxChainDepth;
    public int maxFreeBlocksAtStart;
}
```

> Tất cả tham số chỉnh trong Inspector, không cần đụng code.

---

## 9. LƯU Ý QUAN TRỌNG

```
- Ngưỡng targetScoreMin/Max ban đầu là ước tính
- Sau khi implement xong:
  1. Chạy solver trên hand-craft levels
  2. Thu thập score thực tế làm ground truth
  3. Calibrate lại ngưỡng theo phân phối thực tế
  4. Sau khi ship: tinh chỉnh theo completion rate của player

- Gen level nên chạy trước (offline), không gen runtime
- Kết quả xuất ra ScriptableObject, ship cùng game
- Có thể gen thêm level mới bất cứ lúc nào bằng cách chạy lại tool
```
