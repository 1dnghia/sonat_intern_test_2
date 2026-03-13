---
applyTo: "**"
---

# Tap Away 2D: Remove Blocks — Game Mechanics

## 1. GRID

- Kích thước map thay đổi theo độ khó (3x3 → 7x7)
- Mỗi ô trên grid thuộc 1 trong 4 loại:
  - `Empty` — ô trống
  - `Normal` — khối thường
  - `Gear` — bánh răng
  - `Rotator` — khối xoay

---

## 2. NORMAL BLOCK

- Chiếm 1 ô
- Có 1 direction: `Up` / `Down` / `Left` / `Right`
- Tap → trượt thẳng theo hướng đó
- Đường thông đến biên → bay ra ngoài, bị xóa ✅
- Gặp `Normal` / `Rotator` / `Gear` trên đường → BLOCKED ❌
- Gặp `Gear` đúng trên đường đi → bị cắt, xóa tại chỗ (hiệu ứng vỡ, khác animation bay ra)
- Tap dù blocked hay không → **luôn tính 1 move**

---

## 3. GEAR BLOCK ⚙️

- Chiếm 1 ô, cố định, **không bao giờ bị xóa**
- Không tap được, không tính move
- Chặn đường đi của **mọi block** đi qua ô đó
- Nếu Normal block có hướng trỏ thẳng vào Gear → block bị cắt, destroy, hiệu ứng vỡ

---

## 4. ROTATOR BLOCK 🔄

- Chiếm 1 ô
- Kết nối với Normal Block ở **8 hướng xung quanh** (4 cạnh + 4 góc chéo)
- Tap → xoay **CW 90°** vị trí tất cả block kết nối quanh Rotator
- **Direction của mỗi block GIỮ NGUYÊN** sau khi xoay (chỉ vị trí thay đổi)
- Điều kiện xoay được:
  - Tất cả vị trí mới sau xoay phải là ô **TRỐNG**
  - Nếu 1 block bị chặn → **không xoay toàn bộ**
- Tap dù xoay được hay không → **luôn tính 1 move**

### Công thức xoay CW 90° quanh Rotator tại (rx, ry):
```
relX = block.x - rx
relY = block.y - ry
newX = rx + relY
newY = ry - relX
direction giữ nguyên
```

---

## 5. MOVES COUNTER

| Hành động | Tính move? |
|---|---|
| Tap Normal Block (di chuyển được) | ✅ |
| Tap Normal Block (bị blocked) | ✅ |
| Tap Rotator (xoay được) | ✅ |
| Tap Rotator (bị blocked) | ✅ |
| Tap Gear | ❌ |
| Tap ô trống | ❌ |

---

## 6. HIỆU ỨNG KHI BỊ CHẶN

Khi tap block A bị chặn bởi block B:
- Tính 1 move như bình thường
- **Block B**: đổi màu cảnh báo + rung ← CHỈ B đổi màu
- **Block C** (chặn B nếu có): rung, KHÔNG đổi màu
- **Block D** (chặn C nếu có): rung, KHÔNG đổi màu
- Lan đến hết hàng theo hướng A đang đi
- Hiệu ứng domino: mỗi block rung trễ hơn block trước ~0.05–0.1s
- Block A đứng yên hoàn toàn

---

## 7. WIN / LOSE

### Win
- Xóa hết tất cả Normal Block
- Hiện **Win Panel** + số tiền nhận được
- Nút: `Next Level` / `Replay` / `Home`

### Lose
- Hết moves limit
- Hiện **Lose Panel**
- Nút: `Retry` / `Mua thêm moves` / `Home`

> Level tutorial (giai đoạn đầu) không có moves limit → không thể thua

---

## 8. HỆ THỐNG TIỀN & TRỢ GIÚP

### Tiền
- Nhận khi thắng level
- Không có hệ thống điểm, chỉ có tiền

### Trợ giúp (mua bằng tiền)
- **+5 Moves** — thêm 5 lượt đi
- **Hint** — gợi ý nước đi tiếp theo
- **Xóa Block** — xóa 1 block bất kỳ do player chọn
