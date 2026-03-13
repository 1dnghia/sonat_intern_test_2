---
applyTo: "**"
---

# Tap Away 2D: Remove Blocks — Unity Setup

## 1. PROJECT SETTINGS

```
Edit → Project Settings → Player:
  Default Orientation: Portrait
  Auto Rotation: OFF

Edit → Project Settings → Physics 2D:
  Gravity: (0, 0)  ← game 2D top-down, không cần gravity

Build Settings:
  Platform: Android / iOS
  Color Space: Linear (chất lượng màu tốt hơn)
```

---

## 2. GAME VIEW — PREVIEW ĐÚNG TỈ LỆ MOBILE

```
Window → Game View:
  Resolution: 1080 x 1920 (9:16 Portrait)
  Nếu chưa có → bấm + → nhập tên "Mobile Portrait", Width=1080, Height=1920
```

---

## 3. CAMERA SETUP

### Nguyên tắc
- Camera **cố định**, không zoom in/out khi đổi level
- Grid **scale theo camera** thay vì camera zoom theo grid
- Block size tự động tính theo gridSize để luôn vừa màn hình

### Inspector Settings — Main Camera
```
Projection:       Orthographic
Size:             5.5  (tạm, CameraController.cs sẽ tính lại)
Position:         X=0, Y=0, Z=-10
Background Color: màu nền game (ví dụ #1A1A2E)
```

### CameraController.cs — Gắn vào Main Camera
```
Script này tính:
  1. Block size phù hợp để grid vừa chiều rộng màn hình
  2. Căn grid vào giữa màn hình (chừa khoảng trống trên/dưới cho UI)
  3. Gọi SetupCamera(gridSize) mỗi khi load level mới

Công thức:
  aspectRatio  = Screen.width / Screen.height
  blockSize    = (orthographicSize * 2 * aspectRatio) / gridSize
  gridOffset X = -(gridSize - 1) * blockSize / 2
  gridOffset Y = -(gridSize - 1) * blockSize / 2 + verticalOffset

verticalOffset: dịch grid xuống một chút để chừa chỗ cho UI phía trên
  Ví dụ: -0.5f đến -1.0f tùy UI layout
```

### Lưu ý quan trọng
- Orthographic Size × 2 = số Unity units hiển thị theo chiều dọc. Chiều ngang = Size × 2 × aspectRatio
- Camera **KHÔNG thay đổi Size** giữa các level → không bị giật
- Chỉ **block prefab scale** thay đổi theo gridSize

---

## 4. SCENE HIERARCHY

```
Scene
├── Main Camera
│     └── CameraController.cs
│
├── GameManager (Empty GameObject)
│     └── GameManager.cs
│
├── Grid (Empty GameObject)
│     └── GridManager.cs
│     └── [Block prefabs spawn ở đây]
│
├── Canvas_Static (Screen Space - Overlay)
│     └── Canvas Scaler: Scale With Screen Size, 1080x1920
│     └── TopBar
│           ├── MovesCounter (Text)
│           └── HomeButton
│
├── Canvas_Dynamic (Screen Space - Overlay)  ← tách riêng vì update liên tục
│     └── CoinDisplay (Text)
│
├── Canvas_Popup (Screen Space - Overlay)
│     ├── WinPanel (default: inactive)
│     │     ├── CoinsEarned (Text)
│     │     ├── NextLevelButton
│     │     ├── ReplayButton
│     │     └── HomeButton
│     └── LosePanel (default: inactive)
│           ├── RetryButton
│           ├── BuyMovesButton
│           └── HomeButton
│
└── AudioManager (Empty GameObject)
      └── AudioManager.cs
```

### Lý do tách Canvas
- Khi 1 element thay đổi, toàn bộ Canvas bị rebuild. Tách static và dynamic vào Canvas riêng để tránh rebuild không cần thiết.
- Canvas_Static: TopBar, buttons → ít thay đổi
- Canvas_Dynamic: CoinDisplay, MovesCounter → update thường xuyên
- Canvas_Popup: WinPanel, LosePanel → chỉ bật khi cần

---

## 5. CANVAS SETTINGS

UI Scale Mode nên đặt là **Scale With Screen Size** thay vì Constant Pixel Size để UI tự scale đúng trên mọi thiết bị.

```
Mỗi Canvas:
  Render Mode:        Screen Space - Overlay
  UI Scale Mode:      Scale With Screen Size
  Reference Resolution: 1080 x 1920
  Screen Match Mode:  Match Width Or Height → 1 (match height, phù hợp portrait)
```

---

## 6. INPUT SYSTEM

```
Dùng Unity Input System mới (không dùng legacy):
  Package Manager → Input System → Install

Tap detection:
  Touchscreen → primaryTouch → press → position
  Dùng Physics2D.OverlapPoint() để detect block được tap
  
Không dùng OnMouseDown() vì không hỗ trợ tốt trên mobile
```

---

## 7. SORTING LAYERS

```
Project Settings → Tags and Layers → Sorting Layers:
  0: Background
  1: Grid
  2: Blocks
  3: Effects    ← particle, animation effects
  4: UI         ← nếu dùng World Space UI
```

---

## 8. PREFAB STRUCTURE

```
Prefabs/
  ├── Block.prefab
  │     ├── SpriteRenderer (Sorting Layer: Blocks)
  │     ├── BoxCollider2D
  │     └── BlockView.cs
  │
  ├── GearBlock.prefab
  │     ├── SpriteRenderer (Sorting Layer: Blocks)
  │     └── GearView.cs
  │
  └── RotatorBlock.prefab
        ├── SpriteRenderer (Sorting Layer: Blocks)
        ├── BoxCollider2D
        └── RotatorView.cs
```

---

## 9. SCRIPTS CẦN VIẾT

| Script | Gắn vào | Nhiệm vụ |
|---|---|---|
| `GameManager.cs` | GameManager | Quản lý flow game, win/lose, tiền |
| `GridManager.cs` | Grid | Khởi tạo grid, load level, xử lý logic |
| `CameraController.cs` | Main Camera | Tính block size, căn grid vào giữa màn hình |
| `BlockView.cs` | Block prefab | Visual + tap input + animation trượt ra |
| `GearView.cs` | Gear prefab | Visual bánh răng |
| `RotatorView.cs` | Rotator prefab | Visual + tap + animation xoay |
| `LevelLoader.cs` | GameManager | Load ScriptableObject level data |
| `UIManager.cs` | Canvas | Cập nhật moves counter, coin, show/hide panel |
| `AudioManager.cs` | AudioManager | Quản lý SFX và nhạc nền |

---

## 10. LƯU Ý PERFORMANCE MOBILE

```
- Dùng Object Pooling cho Block prefabs thay vì Instantiate/Destroy liên tục
- Disable Graphic Raycaster trên Canvas không cần input
- Tách static UI và dynamic UI vào Canvas riêng
- Không dùng Layout Groups nếu không cần thiết (tốn performance)
- Target Frame Rate: Application.targetFrameRate = 60
```
