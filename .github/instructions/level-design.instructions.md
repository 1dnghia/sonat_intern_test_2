---
applyTo: "**"
---

# Tap Away 2D: Remove Blocks — Level Design

## 1. KÍCH THƯỚC MAP THEO ĐỘ KHÓ

| Level Range | Grid Size |
|---|---|
| Level 1–10 | 3×3 |
| Level 11–25 | 4×4 |
| Level 26–50 | 5×5 |
| Level 51–80 | 6×6 |
| Level 81+ | 7×7 |

---

## 2. HAND-CRAFT LEVELS — CÁC GIAI ĐOẠN DẠY CƠ CHẾ

### Giai đoạn 1 — Chỉ Normal Block
- Vài level đầu tiên
- Không có Gear, không có Rotator
- **Moves limit: không có** (không thể thua)
- Mục đích: dạy cơ chế tap, thứ tự tháo block, chain cơ bản
- Ví dụ level 1: 4 block, 4 hướng khác nhau, không chặn nhau

### Giai đoạn 2 — Xuất hiện Gear ⚙️
- Vài level tiếp theo, có Gear, **chưa có Rotator**
- Level mẫu đầu tiên: chỉ **1 Gear duy nhất**
- Mục đích: dạy block bị cắt khi đi vào Gear
- Gear đặt đơn giản, dễ nhận biết

### Giai đoạn 3 — Xuất hiện Rotator 🔄
- Vài level, có Rotator + Normal Block, **chưa có Gear**
- Level mẫu đầu tiên: chỉ **1 Rotator đơn giản**
- Mục đích: dạy xoay vị trí, direction giữ nguyên
- Rotator kết nối ít block (2–3 block) để dễ hiểu

### Giai đoạn 4 — Kết hợp Gear + Rotator
- Vài level kết hợp cả hai cơ chế
- Level mẫu rõ ràng, không quá phức tạp
- Mục đích: dạy phối hợp Gear và Rotator cùng lúc

### Giai đoạn 5 — Gen tự động
- Từ đây trở đi toàn bộ gen bằng Unity Editor Tool
- Dựa trên DifficultyParams và Difficulty Score

---

## 3. THAM SỐ ĐỘ KHÓ THEO LEVEL RANGE

| Level | Grid | blockDensity | gearRatio | rotatorRatio | movesBuffer |
|---|---|---|---|---|---|
| 1–10 | 3×3 | 0.4–0.5 | 0 | 0 | unlimited |
| 11–25 | 4×4 | 0.4–0.5 | 0 | 0 | loose (+5) |
| 26–35 | 4×4 | 0.5–0.6 | 0.1 | 0 | medium (+3) |
| 36–50 | 5×5 | 0.4–0.5 | 0.15 | 0 | medium (+3) |
| 51–65 | 5×5 | 0.5–0.6 | 0.15 | 0.08 | tight (+2) |
| 66–80 | 6×6 | 0.4–0.5 | 0.15 | 0.08 | tight (+2) |
| 81+ | 7×7 | 0.4–0.6 | 0.2 | 0.1 | very tight (+1) |

> `movesBuffer`: movesLimit = optimalMoves + buffer

---

## 4. PHÂN LOẠI ĐỘ KHÓ THEO DIFFICULTY SCORE

> **Quan trọng**: Ngưỡng score dưới đây là ước tính ban đầu.
> Sau khi implement xong, cần:
> 1. Chạy solver trên hand-craft levels
> 2. Lấy score thực tế làm ground truth
> 3. Calibrate lại ngưỡng theo phân phối thực tế

| Độ khó | Score ước tính | Đặc điểm |
|---|---|---|
| Tutorial | 0–3 | Không moves limit, không Gear/Rotator |
| Easy | 3–7 | Có moves limit nhẹ, chưa có Gear/Rotator |
| Medium | 7–12 | Có Gear, moves limit vừa |
| Hard | 12–17 | Có Gear + Rotator, moves limit chặt |
| Very Hard | 17+ | Gear + Rotator đầy đủ, moves limit rất chặt |

---

## 5. NGUYÊN TẮC DESIGN LEVEL TỐT

```
- Ít nhất 50% block phải bị chặn bởi block khác
- Phải có ít nhất 1 dependency chain (A chặn B)
- Không quá nhiều block free ngay từ đầu (tối đa 2–3)
- Gear đặt ở trung tâm, không sát biên
- Rotator cần ít nhất 2 block liền kề để có tác dụng
- Mỗi level phải có ít nhất 1 lời giải hợp lệ (validate bằng BFS Solver)
```
