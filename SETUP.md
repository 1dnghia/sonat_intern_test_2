# Tap Away 2D — Hướng dẫn Setup Project


## 1. Tạo Asset & Prefab
- Tạo Hand-Craft Levels: Menu **TapAway → Create Hand-Craft Levels** → tạo các file `Assets/Data/Levels/Level_001.asset` đến `Level_008.asset`
- Tạo DifficultyConfig Asset: Project panel → chuột phải `Assets/Data/Config/` → Create → TapAway → DifficultyConfig → điền các range theo bảng trong spec
- Tạo Prefabs:
  - **Normal Block**: root (BlockView, BoxCollider2D) → child `Arrow` (SpriteRenderer mũi tên chỉ hướng)
  - **Gear Block**: root (GearView, SpriteRenderer) — **không có arrow**, sprite chính quay CW liên tục
    - Thêm child `Shadow` (SpriteRenderer) — bóng xoay CW liên tục, dùng GearView.cs để quản lý cả sprite chính và shadow
    - Lưu ý: pivot/tâm của cả sprite chính và shadow phải đặt đúng tâm (center) trong Sprite Editor
    - GearView.cs: đảm bảo cả sprite chính và shadow đều xoay quanh tâm riêng của từng phần, tốc độ xoay giống nhau
  - **Rotator Block**: root (RotatorView, BoxCollider2D) → child `Base` (SpriteRenderer — phần dưới cố định) + child `Cap` (SpriteRenderer — phần trên, nhô lên khi xoay thành công)
    - Kéo `Base` và `Cap` vào field tương ứng của RotatorView
    - Wiring tap input: dùng [SerializeField] để gán button hoặc input handler, không dùng OnClick Inspector (theo coding rule)
  - Drag vào `Assets/Prefabs/Environment/`

## 2. Setup Scene & Managers
- SampleScene:
  - Managers: Empty object, add GameManager + LevelManager, kéo LevelData vào
  - GridView: Empty object, add GridView, kéo 3 prefab vào
  - TapInputHandler: add vào GridView, kéo GridView vào field
  - Camera: Orthographic, position (0,0,-10), size vừa grid
  - Gán CameraController.cs vào Main Camera, kéo Grid GameObject vào trường `_gridRoot`
  - Prefab GearBlock dùng GearView.cs (quản lý cả sprite chính và shadow), RotatorBlock dùng RotatorView.cs, Block dùng BlockView.cs
  - Đảm bảo wiring tap input cho RotatorBlock qua [SerializeField], không dùng OnClick Inspector
  - AudioManager: tạo GameObject "AudioManager", gán AudioManager.cs, thêm 2 AudioSource (BGM, SFX)

## 3. Setup UI Canvas
- Canvas_Static, Canvas_Dynamic, Canvas_Popup tách riêng đúng hierarchy như sơ đồ
- Canvas (Screen Space Overlay)
- HUD: Text (level), Text (moves), Button (+5 moves), Button (remove block)
- WinPanel: Button Next, Replay, Home
- LosePanel: Button Retry, Buy Moves, Home
- Add HUDView, WinPanel, LosePanel component, wire các field

## 4. Sorting Layer & targetFrameRate
- Sorting Layer: Background, Grid, Blocks, Effects, UI (đúng thứ tự)
- Đặt Application.targetFrameRate = 60 trong GameManager hoặc bootstrap

## 5. Gen Level Tự Động (Optional)
- Menu: TapAway → Level Generator
- Kéo DifficultyConfig vào, chọn range, Generate
- Kéo level mới vào LevelManager

## 6. Build Settings
- File → Build Settings, add SampleScene
- Player Settings: Company/Product Name
- Platform: Android/iOS → Switch Platform

## 7. Kiểm tra nhanh
- Play: block hiển thị đúng, tap block trượt ra, block chặn rung, hết moves hiện LosePanel

## 8. Checklist code
- Tất cả script đã đủ, đúng chức năng:
  - GameManager.cs, GridSystem.cs, CameraController.cs, BlockView.cs, GearView.cs, RotatorView.cs, LevelManager.cs, HUDView.cs, AudioManager.cs, TapInputHandler.cs
- Các enum/data: CellType, BlockDirection, BlockData, LevelData, DifficultyConfig
- Không có lỗi compile.
