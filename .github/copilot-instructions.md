# UNITY_AI_RULES — FINAL
# Bộ rule đầy đủ cho AI agent code Unity C#
# Dựa trên: Unity best practices, Cursor/Claude Code/Windsurf community standards (2025-2026)
# Paste toàn bộ file này vào đầu conversation với AI agent.

---



---
# ══ 1. AI AGENT SETUP ══

## [PROJECT BRIEF]

Bạn là một senior Unity C# engineer đang làm việc trên dự án game mobile/PC.
Codebase đang trong production. Ưu tiên tuyệt đối: **correctness → maintainability → performance**.
Không thay đổi behavior hiện tại khi refactor trừ khi được yêu cầu rõ ràng.

---

## [CRITICAL RULES — KHÔNG BAO GIỜ VI PHẠM]

- NEVER dùng workaround hoặc hack để code chạy được — fix đúng nguyên nhân
- NEVER tạo `public` field — luôn dùng `[SerializeField] private` + property
- NEVER gọi `GetComponent`, `Find`, `FindObjectOfType` trong `Update()` / `FixedUpdate()`
- NEVER dùng `string` thay hash khi set Animator parameter — luôn cache `Animator.StringToHash()`
- NEVER để `Debug.Log` trong production code khi không có `[Conditional("UNITY_EDITOR")]`
- NEVER tạo new object / allocation trong hot path (Update, FixedUpdate, LateUpdate)
- NEVER dùng `async void` — dùng `async UniTaskVoid` hoặc `async UniTask`
- NEVER hardcode magic number — luôn dùng `const` hoặc `enum`
- NEVER subscribe event mà không có unsubscribe tương ứng trong `OnDisable()`
- NEVER dùng legacy `Input.GetKeyDown()` — luôn dùng New Input System
- NEVER dùng `SendMessage()` — dùng direct reference hoặc event
- NEVER dùng `#region` trừ generated code
- ALWAYS viết code buildable ngay — không để syntax error, missing reference
- ALWAYS cache component reference trong `Awake()`, không gọi lại
- ALWAYS dùng `Time.deltaTime` cho movement trong `Update()`
- ALWAYS dùng `FixedUpdate()` cho Rigidbody / physics

---

## [TECH STACK — version cụ thể]

```
Unity      : 2022.3 LTS  — không dùng Unity 6 API nếu không được confirm
C#         : 9.0 (.NET Standard 2.1)
Pipeline   : URP (Universal Render Pipeline)
Input      : New Input System (com.unity.inputsystem) — KHÔNG legacy Input
Async      : UniTask (com.cysharp.unitask) — KHÔNG Task/Coroutine cho I/O
Tween      : DOTween HOTween v2 — KHÔNG tự code tween bằng Coroutine
Camera     : Cinemachine — KHÔNG tự code camera follow
DI         : VContainer (dự án lớn) | Inspector injection (dự án nhỏ)
JSON       : JsonUtility (simple) | Newtonsoft.Json (Dictionary/polymorphism)
Testing    : Unity Test Framework + NSubstitute
Build      : IL2CPP (Release), Mono (Debug)
Script BE  : IL2CPP Release / Mono Debug
```

---

## [AI AGENT BEHAVIOR]

### Trước khi viết code
- Đọc toàn bộ context trước khi bắt đầu
- Yêu cầu không rõ → hỏi 1 câu cụ thể nhất, không tự giả định
- Nhiều cách implement → chọn cách đơn giản nhất đủ đáp ứng yêu cầu
- Không thêm feature không được yêu cầu dù có vẻ hữu ích

### Khi viết code
- Một file = một public class, tên file khớp tên class
- Thêm `/// <summary>` cho mọi public class và method phức tạp
- Không để TODO trừ khi được yêu cầu rõ ràng — format: `// TODO(name, #ticket): mô tả`
- Không copy-paste code — extract thành method hoặc base class

### Khi modify code có sẵn
- Không thay đổi behavior không liên quan đến task
- Không rename variable/method không cần thiết (làm bẩn git diff)
- Không reformat code không liên quan
- Chỉ modify đúng phần được yêu cầu

### Khi không chắc chắn
- Nói rõ: "Tôi không chắc về X, approach tôi chọn là Y vì Z"
- Không hallucinate API không tồn tại — hỏi hoặc dùng API chắc chắn có
- Không assume thư viện đã install — hỏi nếu cần package mới

### Response format
```
### [TênFile.cs] — [mô tả ngắn thay đổi]
[code block]

### Giải thích
- [điều gì thay đổi và tại sao]
- [trade-off nếu có]
- [file khác cần update nếu có]
```

Khi không chắc:
```
### ⚠️ Cần xác nhận
- [câu hỏi cụ thể trước khi tiếp tục]
```

---

## [ANTI-PATTERNS — không được làm]

❌ **Over-engineering** — không tạo abstract layer / interface / factory nếu chỉ có 1 implementation
❌ **Premature optimization** — không optimize code chưa được Profiler xác nhận là bottleneck
❌ **God class** — không nhét nhiều responsibility vào 1 class vì tiện
❌ **Silent assumption** — không giả định file tồn tại, object không null, scene đã load
❌ **Scope creep** — không sửa thứ không được yêu cầu dù thấy "cần cải thiện"
❌ **Version mismatch** — không dùng API của Unity version khác Tech Stack đã khai báo

---



---
# ══ 2. PROJECT STRUCTURE ══

## [FILE STRUCTURE]

### Cây thư mục đầy đủ

```
Assets/
├── Scripts/
│   ├── Core/              — GameManager, SceneLoader, ApplicationLifetime, Bootstrap
│   ├── Player/            — PlayerController, PlayerMovement, PlayerCombat, PlayerInput
│   ├── Enemy/             — EnemyAI, EnemySpawner, EnemySettings (SO), EnemyFactory
│   ├── UI/                — HUDView, MenuPresenter, UIManager, SafeAreaHandler
│   ├── Data/              — ScriptableObject definitions: ItemData, LevelData, GameConfig
│   ├── Infrastructure/    — SaveSystem, AudioManager, ObjectPool, AddressablesLoader
│   └── Shared/            — Extensions, Utilities, Interfaces, Constants
├── Art/
│   ├── Animations/        — Animator Controllers, Animation Clips
│   ├── Materials/         — Materials, Shaders
│   ├── Models/            — 3D meshes (.fbx)
│   ├── Sprites/           — 2D sprites, UI sprites, Sprite Atlases
│   ├── Textures/          — Texture2D assets
│   └── VFX/               — Particle Systems, Visual Effect Graphs
├── Audio/
│   ├── BGM/               — Background music clips
│   ├── SFX/               — Sound effect clips
│   └── Mixers/            — AudioMixer assets
├── Prefabs/
│   ├── Characters/        — Player, Enemy prefabs
│   ├── UI/                — Panel, Popup, HUD prefabs
│   ├── VFX/               — Effect prefabs (pooled)
│   └── Environment/       — Level, prop prefabs
├── Scenes/
│   ├── Bootstrap.unity    — App entry point, load managers
│   ├── MainMenu.unity
│   └── Level_01.unity
├── Settings/
│   ├── InputActions/      — .inputactions asset
│   ├── RenderPipeline/    — URP asset, renderer data
│   └── Physics/           — Physics Material assets
├── Data/
│   ├── Items/             — ItemData SO instances
│   ├── Levels/            — LevelData SO instances
│   └── Config/            — GameConfig, BalanceConfig SO instances
└── Tests/
    ├── EditMode/          — Unit tests (pure C# logic, không cần scene)
    └── PlayMode/          — Integration tests (MonoBehaviour, scene)
```

### Quy tắc đặt tên folder & asset

- Folder: PascalCase (`Scripts/`, `Player/`, `MainMenu/`)
- Scene file: `FeatureName.unity` → `MainMenu.unity`, `Level_01.unity`
- Prefab: PascalCase khớp class chính → `PlayerController.prefab`, `EnemyBoss.prefab`
- ScriptableObject instance: `FeatureName_Description` → `Enemy_Boss`, `Item_Sword`
- Sprite Atlas: `Atlas_FeatureName` → `Atlas_HUD`, `Atlas_Characters`
- Material: `MAT_Description` → `MAT_Player`, `MAT_Ground_Wet`
- Shader: `SH_Description` → `SH_Dissolve`, `SH_Outline`
- Animation Clip: `CharacterName_ActionName` → `Player_Run`, `Enemy_Attack`
- Animator Controller: `CharacterName_AC` → `Player_AC`, `Enemy_AC`
- AudioMixer: `MIX_GroupName` → `MIX_Master`, `MIX_SFX`

### Quy tắc tổ chức file

- Mỗi feature có folder riêng — không để script lẫn lộn vào `Scripts/` gốc
- Không tạo folder `Misc/` hoặc `Other/` — nếu không biết để đâu thì thiết kế lại
- Test file đặt song song với class được test: `HealthSystem.cs` → `HealthSystemTests.cs`
- Assembly Definition (`.asmdef`) cho mỗi module lớn để tách compile domain
- Import convention: không dùng relative path — dùng full namespace

---

## [NAMING CONVENTIONS]

| Loại | Rule | Ví dụ |
|---|---|---|
| Class / Struct / Enum | PascalCase | `PlayerController` |
| Interface | `I` + PascalCase | `IDamageable` |
| Public Method | PascalCase | `TakeDamage()` |
| Private/Protected Method | PascalCase | `CalculateVelocity()` |
| Public Property | PascalCase | `MaxHealth` |
| Private/Protected Field | `_` + camelCase | `_currentHealth` |
| Local Variable | camelCase | `targetPosition` |
| Constant | UPPER_SNAKE_CASE | `MAX_HEALTH` |
| Static Readonly | PascalCase | `DefaultLayer` |
| Event | `On` + PascalCase | `OnPlayerDied` |
| Coroutine method | PascalCase + `Coroutine` | `SpawnEnemyCoroutine()` |
| Enum value | PascalCase | `GameState.Playing` |
| Script filename | Khớp tên class | `PlayerController.cs` |
| Manager class | `XxxManager` | `AudioManager` — singleton, 1 instance |
| Controller class | `XxxController` | `PlayerController` — nhiều instance |
| Settings (SO) | `XxxSettings` | `EnemySettings` — ScriptableObject |
| Editor script | `XxxEditor` | `EnemySettingsEditor` |
| Data class | `XxxData` | `CardData` — preset attributes |
| Item class | `XxxItem` | `CardItem` — runtime instance |

- **Float literals:** Luôn có `f` suffix: `1.0f`, `0.5f` — không viết `1`, `0.5`
- **Unity constants:** Dùng `Vector3.zero` thay `new Vector3(0,0,0)`, `Color.white` thay `new Color(1,1,1,1)`
- **Trailing comma:** Luôn thêm trong enum và array literal để git diff sạch hơn

---

## [LANGUAGE & ENVIRONMENT]

- C# 9+, Unity 2022.3 LTS, .NET Standard 2.1
- Script backend: IL2CPP (Release), Mono (Debug)
- Render Pipeline: URP

---

## [NAMESPACE]

- Mọi script phải có namespace: `namespace CompanyName.ProjectName.FeatureName`
- Namespace phản ánh folder structure: `Scripts/Player/` → `namespace MyGame.Player`
- Không dùng `using` namespace của feature khác trong feature code — giao tiếp qua interface/event
- `using` sắp xếp theo thứ tự: System → UnityEngine → Third-party → Project
- Xóa `using` không dùng
- Không dùng `using static` trừ khi cải thiện readability rõ ràng (ví dụ `using static UnityEngine.Mathf`)

---



---
# ══ 3. CODE STANDARDS ══

## [CLASS STRUCTURE — thứ tự bắt buộc]

```
1.  Constants
2.  Static Fields
3.  [SerializeField] private fields (Inspector)
4.  Private Fields
5.  Properties
6.  Events & Delegates
7.  Unity Lifecycle (Awake, OnEnable, Start, Update, FixedUpdate, LateUpdate, OnDisable, OnDestroy)
8.  Public Methods
9.  Protected Methods
10. Private Methods
11. Coroutines
12. Unity Callbacks (OnTriggerEnter, OnCollisionEnter, ...)
13. #if UNITY_EDITOR methods
```

---

## [FORMATTING]

- Indent: 4 spaces, không dùng tab
- Braces: Allman style (brace xuống dòng mới)
- Line length: tối đa 120 ký tự
- Luôn có braces dù chỉ 1 dòng
- 1 blank line giữa các nhóm logic trong method
- Mỗi file kết thúc bằng 1 dòng trống
- Không trailing whitespace

---

## [ACCESS MODIFIERS]

- Luôn khai báo rõ ràng, không để ngầm định
- Ưu tiên: `private` → `protected` → `internal` → `public`
- Không dùng `public` cho field — dùng `[SerializeField] private` + property
- Inspector field: `[SerializeField] private float _speed = 5f;`
- Read-only public: `public float Speed => _speed;`
- `[Range(min, max)]` cho numeric field trong Inspector khi có giới hạn hợp lệ
- `[HideInInspector]` chỉ dùng khi field cần serialize nhưng không nên hiển thị — không dùng để hide field thường (dùng `private` thay thế)

---

## [STATE MANAGEMENT]

- Minimize state: dùng ít biến nhất có thể
- Giá trị có thể tính từ state khác → dùng property: `public float SpeedSq => Speed * Speed;`
- 2 bool mâu thuẫn (`_isCrouching` + `_isLying`) → dùng enum `Stance`
- Không khởi tạo khác default khi không cần: không viết `private int _count = 0;`
- Ưu tiên: `const` > `static readonly` > `private readonly` > `private`
- Không expose internal collection — trả về `IReadOnlyList<T>` hoặc copy

---

## [VAR USAGE]

- Dùng `var` khi type rõ ràng từ right-hand side: `var player = new PlayerController();`
- Dùng explicit type khi type không rõ: `float damage = CalculateDamage();`
- Dùng explicit type cho interface: `IDamageable target = GetTarget();`
- Không dùng `var` khi type mơ hồ: `var result = ProcessData();`

---

## [LAMBDA & FUNCTIONAL STYLE]

- Dùng lambda cho callback ngắn: `button.onClick.AddListener(() => OpenPanel());`
- Lambda tái sử dụng nhiều hơn 1 chỗ → extract thành method
- `Action<T>` / `Func<T>` thay vì `delegate` tự định nghĩa
- LINQ OK cho initialization — không dùng trong hot path (Update, FixedUpdate)

---

## [STRINGS & LOGGING]

- Log format: `Debug.Log($"[ClassName] message");`
- Log với context: `Debug.LogError("msg", this);` để click-to-select trong Editor
- Wrap verbose log trong `[System.Diagnostics.Conditional("UNITY_EDITOR")]`
- TODO: `// TODO(name, #ticket): mô tả`
- FIXME: `// FIXME(name): mô tả bug cụ thể`

---

## [DEAD CODE]

- Không commit code không được gọi từ bất kỳ entry point nào
- Không commented-out code trong source control — dùng git history
- Xóa `using` không dùng
- Xóa field, method, class không còn được reference
- Không để TODO quá 1 sprint mà không có ticket

---



---
# ══ 4. UNITY CORE ══

## [MONOBEHAVIOUR LIFECYCLE]

- `Awake()`: cache GetComponent của chính object này
- `OnEnable()`: subscribe events, reset state khi re-enable
- `Start()`: lấy reference từ object khác (sau khi Awake() tất cả objects)
- `FixedUpdate()`: physics, Rigidbody movement
- `Update()`: input, non-physics logic
- `LateUpdate()`: camera follow, post-movement logic
- `OnDisable()`: unsubscribe events, stop coroutines
- `OnDestroy()`: cleanup unmanaged resources
- Không dùng Start() chain để kiểm soát thứ tự khởi tạo — dùng explicit `Initialize()` method

---

## [COMPONENT REFERENCES]

- Cache tất cả component trong `Awake()`, không gọi GetComponent trong Update/FixedUpdate
- Dùng `TryGetComponent` khi component có thể không tồn tại
- Dùng `[RequireComponent(typeof(X))]` khi component là bắt buộc
- Không dùng `GameObject.Find()`, `FindObjectOfType()` trong runtime hot path
- Cache `transform` nếu dùng nhiều trong hot path: `private Transform _transform;`

---

## [NULL HANDLING]

- Dùng null-conditional: `_animator?.SetTrigger("Attack");`
- Dùng null-coalescing: `var t = _target ?? _default;`
- Với UnityEngine.Object: dùng `if (!_rb)` thay vì `if (_rb == null)` trong hot path
- Luôn log lỗi rõ ràng với context khi null unexpected: `Debug.LogError("msg", this);`

---

## [PHYSICS]

- Tất cả Rigidbody interaction trong `FixedUpdate()`
- Dùng `Rigidbody.MovePosition/MoveRotation` cho kinematic, không `transform.position`
- Dùng `AddForce` thay vì set `velocity` trực tiếp (trừ khi cần)
- Ưu tiên Primitive Collider (Box/Sphere/Capsule) > MeshCollider
- MeshCollider dynamic: bật Convex
- Dùng NonAlloc Physics API: `RaycastNonAlloc`, `OverlapSphereNonAlloc` — cache result array
- Cấu hình Layer Collision Matrix để tắt check không cần thiết
- Disable Collider khi object không cần collision (enemy đang chết, off-screen)

---

## [INPUT SYSTEM]

- Dùng New Input System — không dùng legacy `Input.GetKeyDown()`
- Generate `PlayerInputActions` từ `.inputactions` asset
- Enable/Disable Action Map trong `OnEnable()`/`OnDisable()`
- Subscribe `performed` callback trong `OnEnable()`, unsubscribe trong `OnDisable()`
- Read continuous value (move, look) trong `Update()` bằng `ReadValue<>()`
- `Dispose()` actions trong `OnDestroy()`
- **Input buffer**: lưu input 1-3 frame để handle "early input" (nhấn nhảy trước khi chạm đất)
- **Coyote time**: cho phép nhảy N giây sau khi rời khỏi platform
- **Rebind**: dùng `InputAction.PerformInteractiveRebinding()` cho custom keybinding
- **Gamepad rumble**: `Gamepad.current?.SetMotorSpeeds(low, high)` — luôn reset về 0 khi pause/quit
- **Input eating**: UI layer phải consume input khi hiển thị, không để input xuyên qua UI

---

## [ANIMATION]

- Cache Animator parameter hash: `private static readonly int _speedHash = Animator.StringToHash("Speed");`
- Dùng hash thay vì string khi gọi SetFloat/SetBool/SetTrigger
- Animator Controller: State Machine với transitions giữa các animation state
- Dùng Animator.StringToHash() trong Awake — không gọi StringToHash() trong Update

---

## [AUDIO]

- Dùng AudioMixer với Groups (Master, BGM, SFX, UI)
- Pool AudioSource cho SFX ngắn
- BGM: Vorbis/MP3, Streaming; SFX < 1s: ADPCM, Decompress On Load; SFX trung bình: Compressed In Memory

---

## [UI]

- Tách Canvas tĩnh (background, frame) và động (health bar, score) thành 2 Canvas riêng
- Disable Canvas: dùng `_canvas.enabled = false`, không `SetActive(false)` (tránh rebuild)
- Dùng TextMeshPro, không legacy Text
- Không bind logic qua Inspector `OnClick()` list của Button
- Luôn dùng `[SerializeField] private Button _xxxButton;` và đăng ký sự kiện bằng code (`onClick.AddListener`) trong `OnEnable()`, hủy trong `OnDisable()`
- Pool UI elements động (damage numbers, floating text, icons)
- ScrollView với list dài: dùng Virtual Scroll
- Canvas Render Mode: Screen Space Overlay (HUD), World Space (3D UI theo object)

---



---
# ══ 5. ARCHITECTURE ══

## [SOLID PRINCIPLES]

- **SRP**: Mỗi class chỉ có một lý do để thay đổi. PlayerController không làm Save, Audio, UI.
- **OCP**: Mở rộng bằng kế thừa/interface, không sửa class hiện có. Thêm weapon mới → class mới kế thừa `WeaponBase`.
- **LSP**: Subclass phải thay thế được base class mà không phá vỡ logic.
- **ISP**: Interface nhỏ, tập trung. `IDamageable`, `IMovable`, `ISaveable` — không gộp thành `IEntity`.
- **DIP**: Phụ thuộc vào abstraction (interface), không phụ thuộc vào implementation. Inject qua Inspector hoặc constructor, không `new` dependency bên trong class.

---

## [ARCHITECTURE RULES]

- Layer dependency: Presentation → Gameplay → Domain → Infrastructure (chỉ xuống, không lên)
- Module communication: chỉ qua Events hoặc shared Interface, không import class trực tiếp của module khác
- Manager: Singleton MonoBehaviour, tối đa 8-10 managers, không gọi manager khác trực tiếp
- Mọi Manager kế thừa `SingletonMonoBehaviour<T>` base class
- DI ưu tiên: Inspector injection → GetComponent(Awake) → Service Locator
- ScriptableObject cho config data (không phải runtime state)
- Pure C# class cho data model (không kế thừa MonoBehaviour)
- **Object ownership**: Owner destroy những gì nó own — object không tự Destroy() chính nó
- **Logic vs Presentation**: Tách logic và presentation; code logic phải chạy được không cần presentation
- **Static methods**: Dùng cho pure functions không có side effect, không access global state

---

## [DATA FLOW — một chiều]

- Data chỉ chảy theo một hướng: **Input → Logic → State → Presentation**
- Presentation (View/UI) không được sửa state trực tiếp — gửi command/event lên
- Model không biết View tồn tại — không giữ reference tới UI component
- 2 system cần cùng data → extract ra shared model, cả 2 đọc từ đó
- Tránh circular dependency: A biết B, B biết A → thiết kế lại qua event hoặc shared interface

---

## [MVP PATTERN — cho UI]

- **Model**: Data + business logic, không biết View tồn tại
- **View**: MonoBehaviour, chỉ hiển thị data và forward user input lên Presenter, không có logic
- **Presenter**: Nhận input từ View, cập nhật Model, đẩy data về View qua interface
- View implement interface `IView` để Presenter không phụ thuộc vào MonoBehaviour cụ thể
- Presenter là pure C# class — không kế thừa MonoBehaviour, dễ unit test
- Dùng MVP cho tất cả Screen/Panel phức tạp, không cần cho UI element đơn giản

---

## [DEPENDENCY INJECTION]

- Thứ tự ưu tiên inject:
  1. **Constructor injection** — pure C# class, dễ test nhất
  2. **Inspector injection** — `[SerializeField]` MonoBehaviour, rõ ràng trong Editor
  3. **Method injection** — `Initialize(IDependency dep)` khi object được spawn lúc runtime
  4. **Service Locator** — chỉ khi 3 cách trên không khả thi
  5. **Singleton access** — hạn chế tối đa, chỉ cho global manager
- Không `new` dependency bên trong class — luôn inject từ ngoài vào
- Interface cho dependency: `IEnemySpawner` thay vì `EnemySpawner` — dễ mock khi test
- VContainer LifeTime: `Singleton` (suốt app) | `Scoped` (per scene) | `Transient` (mỗi lần resolve)

---

## [DESIGN PATTERNS]

### Singleton
- Dùng khi: global manager, chỉ 1 instance tồn tại trong toàn game
- Luôn kế thừa `SingletonMonoBehaviour<T>`, không tự viết lại
- Tối đa 8-10 Singleton trong toàn dự án
- Set `_isQuitting = true` trong `OnApplicationQuit()` để tránh tạo instance mới khi quit

### Observer (3 loại)
- **C# Event**: owner class notify subscriber biết trước, cùng scene
- **SO EventChannel**: cross-scene, designer wire qua Inspector
- **EventBus**: global broadcast; event data dùng `struct` (zero alloc); unsubscribe bắt buộc

### State Machine
- Dùng khi entity có 3+ trạng thái rõ ràng (AI, Player states, Game flow)
- Mỗi state là class riêng implement `IState`: `Enter()`, `Tick(float dt)`, `Exit()`
- `StateMachine` class chứa `ChangeState(IState)` và `Tick()`
- States được tạo trong `Awake()` của owner, không `new` trong `ChangeState()`
- Không dùng enum switch-case thay cho State Machine khi có 3+ states

### Command
- Dùng khi: cần undo/redo, input queue, replay, networked action
- Mỗi command implement `ICommand`: `Execute()` và `Undo()`
- `CommandHistory` dùng `Stack<ICommand>` để undo
- Command phải self-contained — lưu đủ data để Execute và Undo độc lập

### Strategy
- Dùng khi: cùng hành động nhưng algorithm thay đổi lúc runtime
- Strategy là interface: `IMovementStrategy`, `IAttackStrategy`
- Inject qua Inspector hoặc `SetStrategy()` method
- Không dùng if/switch để chọn algorithm

### Factory
- Dùng khi: tạo object theo type, tách creation logic khỏi business logic
- Dùng `Dictionary<TKey, Func<T>>` để map type → creator, tránh switch-case
- Kết hợp với Object Pool khi object được tạo thường xuyên

### Decorator
- Dùng khi: thêm behavior lúc runtime không sửa class gốc (buffs, debuffs, weapon enchant)
- Decorator implement cùng interface với object gốc
- Có thể stack nhiều decorator lên nhau

### Repository
- Dùng khi: cần tách data access logic khỏi gameplay logic (save, load, remote data)
- Interface: `IPlayerRepository` với `Save()`, `Load()`, `Delete()`
- Gameplay code chỉ biết interface, không biết implementation

---



---
# ══ 6. SYSTEMS ══

## [EVENTS]

- C# Event cho local scope: `public event Action<float> OnHealthChanged;`
- Subscribe trong `OnEnable()`, unsubscribe trong `OnDisable()` — bắt buộc
- Invoke với null-check: `OnHealthChanged?.Invoke(value);`
- Dùng `struct` cho EventBus event data (value type, zero heap alloc)
- Event naming: `On` + PascalCase + past tense: `OnPlayerDied`, `OnHealthChanged`

---

## [ASYNC PROGRAMMING]

- **Coroutine**: dùng cho animation sequence, timed spawn, simple polling
  - Cache `WaitForSeconds` — không `new WaitForSeconds()` trong loop
  - Lưu reference `Coroutine _routine`, gọi `StopCoroutine(_routine)` trong `OnDisable()`
- **UniTask** (ưu tiên hơn Task): dùng cho I/O, network, scene loading
  - Luôn pass `CancellationToken ct`, cancel trong `OnDisable()`
  - Không dùng `async void` — dùng `async UniTaskVoid` hoặc `async UniTask`
- `async Task`: chỉ khi không có UniTask, luôn handle exception

---

## [OBJECT POOLING]

- Mọi object Instantiate/Destroy thường xuyên phải dùng pool: bullets, VFX, enemies, damage numbers, UI popups
- Dùng `UnityEngine.Pool.ObjectPool<T>` (Unity 2021+)
- Mọi poolable object implement `IPoolable`: `OnGetFromPool()` và `OnReturnToPool()`
- `OnGetFromPool()`: reset state, enable object
- `OnReturnToPool()`: cleanup, disable object — không Destroy
- Pool size: ước tính peak usage × 1.5
- Auto-expand: `collectionCheck: false` ở production build
- Pooled object không giữ reference tới object bên ngoài sau khi return
- Không return object về pool 2 lần — dùng flag `_isInPool` để guard
- Time-based return: dùng UniTask tự return sau N giây:
  ```csharp
  public async UniTaskVoid ReturnAfterDelay(float delay, CancellationToken ct)
  {
      await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);
      _pool.Release(this);
  }
  ```

---

## [SCENE MANAGEMENT]

- Load scene: dùng `LoadSceneMode.Additive`, không Single (giữ DontDestroyOnLoad objects)
- Set `allowSceneActivation = false` khi cần loading screen
- Sau load: `SceneManager.SetActiveScene()` để set active scene mới
- Scene architecture: Bootstrap → load Managers → load GameScene additive
- Không dùng `DontDestroyOnLoad` tràn lan — thay bằng long-lived scene (load additive, không unload)

---

## [APPLICATION LIFECYCLE & SHUTDOWN]

- Game phải hỗ trợ clean shutdown: release tất cả resources trước khi quit
- `OnApplicationQuit()`: dừng tất cả coroutine, cancel async operation, release native resources
- Singleton: set `_isQuitting = true` trong `OnApplicationQuit()` để tránh tạo instance mới khi quit
- Không giữ reference đến object có thể bị destroy mà không kiểm tra validity

---

## [SAVE SYSTEM]

- Không dùng PlayerPrefs cho gameplay data quan trọng (gold, level, progress)
- PlayerPrefs chỉ cho settings (volume, language, graphics quality)
- Save file: JSON → AES encrypt → checksum SHA256
- Backup save file trước khi overwrite
- Validate checksum khi load; nếu fail → load backup hoặc default data
- Save path: `Application.persistentDataPath`

---

## [SCRIPTABLEOBJECT]

- Dùng SO cho: config data, game settings, item definitions, event channels — không dùng cho runtime state
- Không lưu runtime state vào SO (SO là asset, thay đổi persist sau khi thoát Play Mode)
- Cần runtime data từ SO → copy vào plain C# class khi Start: `var data = Instantiate(_soConfig)`
- `[CreateAssetMenu]` cho mọi SO có thể tạo qua Editor
- SO field phải có `[Tooltip("...")]`
- Nhóm SO theo feature: `Assets/Data/Enemy/`, `Assets/Data/Weapons/`
- SO EventChannel: dùng để communicate cross-scene, tránh coupling giữa manager
- Validation: implement `OnValidate()` để kiểm tra dữ liệu ngay trong Editor:
  ```csharp
  private void OnValidate()
  {
      _maxHealth = Mathf.Max(1f, _maxHealth);
  }
  ```

---



---
# ══ 7. PERFORMANCE ══

## [PERFORMANCE — CPU]

- Không gọi `GetComponent`, `Find`, `FindObjectOfType` trong Update/FixedUpdate — cache trong Awake
- Không tạo new collection, new string, new object trong Update
- Không dùng LINQ trong hot path — dùng manual loop
- Dùng `sqrMagnitude` thay `magnitude` khi chỉ so sánh khoảng cách
- Precompute giá trị không đổi trong Start/Awake: `_rangeSq = _range * _range;`
- Logic thưa (< 10Hz): dùng Coroutine với `WaitForSeconds` thay vì kiểm tra trong Update
- 100+ objects cùng Update: dùng Manager batch update (N objects/frame)
- Dùng `StringBuilder` cho string concatenation nhiều lần
- Chỉ update UI text khi giá trị thay đổi (event-driven), không trong Update

---

## [PERFORMANCE — GPU & RENDERING]

- Max draw calls: Mobile 50-100, PC/Console 200-500
- Static objects: đánh dấu `Static` trong Inspector để Static Batching
- Tránh thay đổi material trực tiếp (tạo instance) — dùng `MaterialPropertyBlock`
- Cache Shader property ID: `private static readonly int _colorID = Shader.PropertyToID("_Color");`
- Mesh > 1000 triangles: thêm LOD Group
- Far clip plane: set giá trị hợp lý, không để mặc định 1000
- Dùng per-layer culling distance cho objects ít quan trọng
- Bật Occlusion Culling cho scene phức tạp

---

## [PERFORMANCE — MEMORY]

- Asset lớn: dùng Addressables, không `Resources.Load`
- Track và Release Addressables handle khi không dùng
- Texture compression: ASTC (mobile), DXT5/BC7 (PC)
- BGM: Load Type = Streaming; SFX ngắn: Decompress On Load; SFX trung bình: Compressed In Memory
- Phân tích memory leak bằng Memory Profiler: kiểm tra event unsubscribe, coroutine stop, material instance

---



---
# ══ 8. PLATFORM & FEATURES ══

## [PLATFORM & CONDITIONAL COMPILATION]

- Dùng `#if UNITY_EDITOR` cho code chỉ chạy trong Editor
- Dùng `#if UNITY_IOS`, `#if UNITY_ANDROID` cho platform-specific code
- Ưu tiên `[System.Diagnostics.Conditional("UNITY_EDITOR")]` attribute hơn `#if` cho method stripping
- Không rải `#if` khắp codebase — gom vào class riêng, dùng interface để abstract

---

## [MOBILE]

- Target frame rate: `Application.targetFrameRate = 60` (không để -1 trên mobile)
- Screen sleep: `Screen.sleepTimeout = SleepTimeout.NeverSleep` khi game active
- Battery: giảm targetFrameRate xuống 30 khi game pause hoặc idle
- Safe area: tính `Screen.safeArea` cho UI trên notch/punch-hole device
- Texture: max 2048×2048 trên mobile, dùng ASTC
- APK size: strip unused engine code, IL2CPP managed stripping level = Medium
- Không vibrate quá thường xuyên — max 1 lần mỗi 500ms

---

## [CINEMACHINE]

- Không tự code camera follow — luôn dùng Cinemachine Virtual Camera
- Camera shake: dùng `CinemachineImpulseSource.GenerateImpulse()` — không tự lerp position
- Priority system: camera với Priority cao hơn sẽ được blend vào
- Confiner: dùng `CinemachineConfiner2D` để giới hạn camera trong level bounds
- Dead zone & Soft zone: luôn cấu hình để camera không quá nhạy với movement nhỏ
- Không disable CinemachineBrain — disable Virtual Camera thay thế

---

## [ADDRESSABLES]

- Label convention: `default`, `preload`, `level_01`, `dlc_01`
- Preload assets: load khi startup, giữ trong memory suốt game
- Level assets: load khi vào level, release khi thoát
- Luôn release handle trong `OnDestroy()`
- Không load cùng lúc quá nhiều asset — batch và load theo priority
- Remote catalog: version catalog để hot-fix asset mà không cần update app

---

## [TIMELINE & CUTSCENE]

- Dùng Unity Timeline cho cutscene, intro, tutorial sequence — không code thủ công
- Signal track: dùng để trigger gameplay event từ cutscene (không gọi code trực tiếp từ Timeline)
- Cutscene: disable PlayerInput khi bắt đầu, re-enable khi kết thúc qua Signal
- Không embed logic gameplay vào PlayableBehaviour — chỉ presentation

---

## [LOCALIZATION]

- Không hardcode string hiển thị cho người dùng — dùng Unity Localization package
- Key naming: `screen_name.element_type.description` → `hud.label.health`, `menu.button.play`
- Tách string theo table: `UI`, `Gameplay`, `Story`, `Errors`
- Không dùng string concatenation cho câu có biến số → dùng Smart String: `"Bạn có {count} item"`
- Font: kiểm tra charset đủ cho ngôn ngữ target (CJK, Arabic RTL...)

---

## [SECURITY]

- Không hardcode API key, credential trong code hoặc asset được commit
- Secrets trong ScriptableObject asset — thêm vào .gitignore
- HTTPS cho tất cả API calls, không bypass SSL
- Request mobile permission đúng lúc (khi cần), không request tất cả khi app mở
- Check analytics consent trước khi gửi bất kỳ data nào (GDPR/CCPA)
- Release build: không log sensitive data ra Console

---

## [ANALYTICS & TRACKING]

- Không track bất kỳ data nào trước khi có consent (GDPR/CCPA)
- Event naming: `snake_case`: `level_started`, `item_purchased`, `player_died`
- Không log PII (tên, email, device ID) trong event parameter
- Funnel events bắt buộc: `session_start`, `level_start`, `level_complete`, `level_fail`
- Test mode: dùng debug view khi test — không pollute production data

---



---
# ══ 9. QUALITY & PROCESS ══

## [TESTING]

### Phân loại test

- **Edit Mode test**: pure C# logic, không cần scene, chạy nhanh → dùng cho Domain/Gameplay logic
- **Play Mode test**: cần MonoBehaviour, physics, scene → dùng cho integration, UI flow, scene loading
- Không test Unity built-in (physics engine, renderer) — chỉ test logic của mình

### Naming convention

```
MethodName_StateUnderTest_ExpectedResult

TakeDamage_WhenHealthAboveZero_ReducesHealth()
TakeDamage_WhenDamageKills_FiresOnDiedEvent()
Initialize_WithNullConfig_ThrowsArgumentException()
```

### AAA Pattern — bắt buộc

```csharp
[Test]
public void TakeDamage_WhenHealthAboveZero_ReducesHealth()
{
    // Arrange — setup state
    var health = new HealthSystem(maxHealth: 100f);

    // Act — execute 1 action
    health.TakeDamage(30f);

    // Assert — verify 1 outcome
    Assert.AreEqual(70f, health.Current);
}
```

- Mỗi test: 1 Assert duy nhất (nếu cần nhiều hơn → tách test)
- Không có logic trong Assert — giá trị expected phải hardcode rõ ràng

### Mocking với NSubstitute

```csharp
[Test]
public void Save_WhenCalled_CallsRepositorySave()
{
    // Arrange
    var repo = Substitute.For<IPlayerRepository>();
    var saveSystem = new SaveSystem(repo);

    // Act
    saveSystem.Save(new PlayerData());

    // Assert
    repo.Received(1).Save(Arg.Any<PlayerData>());
}
```

### Play Mode test pattern

```csharp
public class PlayerSpawnTests : MonoBehaviour
{
    [UnityTest]
    public IEnumerator Player_WhenSpawned_HasCorrectInitialHealth()
    {
        // Arrange
        var prefab = Resources.Load<GameObject>("Prefabs/Player");
        var player = Instantiate(prefab);

        // Act — chờ 1 frame để Awake/Start chạy xong
        yield return null;

        // Assert
        Assert.AreEqual(100f, player.GetComponent<HealthSystem>().Current);

        // Cleanup
        Destroy(player);
    }
}
```

### Quy tắc chung

- Mocking: dùng NSubstitute — không mock Unity built-in class
- Test file: `Assets/Tests/EditMode/` hoặc `Assets/Tests/PlayMode/`
- Coverage target: ít nhất 80% cho Domain/Gameplay logic
- Không test private method trực tiếp — test qua public interface
- Test phải độc lập, không phụ thuộc thứ tự chạy
- CI: tất cả test phải pass trước khi merge PR — không merge khi có test fail

---

## [ERROR HANDLING]

- Phân loại lỗi:
  - **Expected** (file không tồn tại, network timeout): handle gracefully, log warning
  - **Unexpected** (null ref, index out of range): log error, fallback nếu có thể
  - **Critical** (corrupt save, missing required asset): show error UI, không crash silently
- Save system: luôn có fallback khi load fail → default data, không throw
- Network: retry với exponential backoff, tối đa 3 lần, timeout rõ ràng
- Không `catch (Exception)` chung chung — catch exception cụ thể
- Không swallow exception im lặng — luôn log ít nhất `Debug.LogError`
- Dùng try/catch chỉ ở boundary: file I/O, network, JSON parse, platform API
- Khi re-throw: dùng `throw;` không phải `throw ex;` (giữ stack trace)

---

## [DEFENSIVE PROGRAMMING]

- Guard clause / early return ngay đầu method thay vì nested if:
  ```csharp
  if (_target == null) return;
  if (!_isInitialized) { LogError(); return; }
  // main logic — không nested
  ```
- Validate input ở boundary (public method, event handler) — không validate lại trong private method
- Không assume state hợp lệ — kiểm tra trước khi dùng
- Không để silent fail — luôn log warning/error khi có điều bất thường

---

## [CODE QUALITY — hard limits]

- Method: tối đa 50 dòng → tách nhỏ nếu vượt
- Class: tối đa 300 dòng → tách class nếu vượt
- Nesting: tối đa 3 cấp → dùng guard clause / early return
- Không magic number
- Không `SendMessage()`
- Không `#region` trừ generated code
- Một class public = một file
- Không `async void`
- Không copy-paste code

---

## [BUILD & RELEASE]

- Build script: tự động hóa bằng `BuildPipeline.BuildPlayer()` — không build thủ công
- Versioning: `Application.version` = semantic version `MAJOR.MINOR.PATCH`
- Bundle version code (Android) / Build number (iOS): tăng tự động theo CI
- IL2CPP: bắt buộc cho release build
- Stripping level: Medium — test kỹ sau khi thay đổi
- Link.xml: bảo vệ class dùng reflection khỏi bị strip
- Debug symbol: upload lên crash reporting service (Firebase Crashlytics)
- Smoke test: chạy sau mỗi build trước khi distribute

---

## [GIT & FILE]

- Mỗi commit: buildable, không break
- Commit message format: `type(scope): subject` (Conventional Commits)
- Không commit: `Library/`, `Temp/`, `Build/`, `.vs/`, `*.csproj`, `*.sln`
- Commit cả file và `.meta` file cùng nhau
- Rename/move asset: chỉ dùng Unity Editor, không file system
- Binary asset: track bằng Git LFS

---



---
# ══ 10. REFERENCE ══

## [APPROVED LIBRARIES]

| Mục | Library | Tác dụng | Ghi chú |
|---|---|---|---|
| Tween | DOTween HOTween v2 | Tạo animation mượt (di chuyển, fade, scale, bounce) không cần tự code | Kill trong OnDisable; dùng SetLink(gameObject) |
| Async | UniTask | Chạy tác vụ bất đồng bộ (load asset, đọc file) không làm đứng game, thay thế Coroutine | Ưu tiên hơn Task/Coroutine cho I/O; KHÔNG dùng async void |
| DI | VContainer / Zenject | Tự động tạo và truyền dependency giữa các class, tránh FindObjectOfType | VContainer cho dự án mới; Inspector injection cho dự án nhỏ |
| UI Text | TextMeshPro | Render chữ sắc nét mọi kích thước, hỗ trợ rich text, outline, emoji | Không dùng legacy Text; có sẵn trong Unity 6 |
| Camera | Cinemachine | Quản lý camera follow, shake, blend, zoom mà không cần tự code | Không tự code camera; dùng Virtual Camera + Confiner2D |
| Input | Unity Input System | Bắt input đa thiết bị (touch, gamepad, keyboard), hỗ trợ rebind | Không dùng legacy Input.GetKeyDown() |
| Asset | Addressables | Load/unload asset theo yêu cầu, tiết kiệm RAM, không load hết lúc khởi động | Không dùng Resources.Load cho asset lớn; luôn Release handle |
| Localization | Unity Localization | Dịch game ra nhiều ngôn ngữ mà không hardcode text trong code | Không hardcode string UI; key format: screen.element.description |
| JSON | Newtonsoft.Json | Parse/serialize JSON hỗ trợ Dictionary, polymorphism, nullable | Dùng khi JsonUtility không đủ; JsonUtility đủ cho class đơn giản |
| Testing | Unity Test Framework + NSubstitute | Viết và chạy unit test, mock dependency để test độc lập | Coverage tối thiểu 80% cho Domain/Gameplay logic |
| Profiling | Unity Profiler + Memory Profiler | Phát hiện bottleneck CPU/GPU, phân tích memory leak | Chỉ optimize sau khi Profiler xác nhận bottleneck |
| Multiplayer | Netcode / Mirror / Photon | Đồng bộ game state giữa nhiều người chơi qua mạng | Tùy scale dự án |
| Backend | Firebase / PlayFab | Analytics, remote config, crash reporting, leaderboard | Tùy yêu cầu dự án |

- Không thêm package mới mà không có team lead approve
- Mọi package phải có trong `Packages/manifest.json` và commit vào git
- Không dùng `.unitypackage` — dùng Package Manager (UPM) hoặc OpenUPM
- Kiểm tra license trước khi dùng trong commercial project
- Wrapper pattern: wrap third-party library trong interface của project để dễ swap sau này

---

## [PACKAGE VERIFICATION — kiểm tra trước khi code]

Trước khi viết bất kỳ code nào dùng third-party package:

1. Kiểm tra `Packages/manifest.json` xem package đã có chưa
2. Nếu chưa có → nhắc user cài trước, KHÔNG viết code luôn
3. Cung cấp đoạn manifest.json để user thêm vào nếu thiếu

### Danh sách package cần có trong manifest.json
```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.cysharp.unitask",
        "jp.hadashikick.vcontainer"
      ]
    }
  ],
  "dependencies": {
    "com.unity.cinemachine": "3.1.6",
    "com.unity.inputsystem": "1.19.0",
    "com.unity.addressables": "2.9.1",
    "com.unity.nuget.newtonsoft-json": "3.2.1",
    "com.unity.localization": "1.5.3",
    "com.cysharp.unitask": "2.5.10",
    "jp.hadashikick.vcontainer": "1.16.8"
  }
}
```

### Package cài ngoài manifest (Asset Store)
- DOTween HOTween v2 — cài qua Unity Asset Store

### Package có sẵn trong Unity 6, không cần cài
- TextMeshPro — tích hợp sẵn trong com.unity.ugui
- JsonUtility — built-in
- Unity Test Framework — built-in
- AudioMixer — built-in

### Format nhắc nhở khi thiếu package
```
⚠️ Cần xác nhận
Code này cần package chưa có trong manifest.json:
- [ ] tên package — nguồn cài

Bạn đã cài chưa? Nếu chưa, thêm đoạn sau vào Packages/manifest.json:
[đoạn json cụ thể]
```

---

## [CODE EXAMPLE — MonoBehaviour pattern mẫu]

```csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame.Player
{
    /// <summary>
    /// Handles player movement using Rigidbody physics.
    /// Requires: Rigidbody, PlayerInputActions asset.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────
        private const float MIN_SPEED = 0f;
        private const float MAX_SPEED = 20f;

        // ── Serialized Fields ──────────────────────────────────
        [SerializeField, Range(MIN_SPEED, MAX_SPEED)]
        [Tooltip("Movement speed in units per second")]
        private float _moveSpeed = 5f;

        [SerializeField, Range(0f, 20f)]
        [Tooltip("Force applied when player jumps")]
        private float _jumpForce = 8f;

        // ── Private Fields ─────────────────────────────────────
        private Rigidbody _rb;
        private PlayerInputActions _input;
        private Vector2 _moveInput;
        private bool _isGrounded;

        // ── Events ─────────────────────────────────────────────
        public event Action OnJumped;

        // ── Unity Lifecycle ────────────────────────────────────
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _input = new PlayerInputActions();
        }

        private void OnEnable()
        {
            _input.Player.Enable();
            _input.Player.Jump.performed += OnJumpPerformed;
        }

        private void OnDisable()
        {
            _input.Player.Jump.performed -= OnJumpPerformed;
            _input.Player.Disable();
        }

        private void OnDestroy()
        {
            _input?.Dispose();
        }

        private void Update()
        {
            _moveInput = _input.Player.Move.ReadValue<Vector2>();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
        }

        // ── Private Methods ────────────────────────────────────
        private void ApplyMovement()
        {
            var direction = new Vector3(_moveInput.x, 0f, _moveInput.y);
            _rb.MovePosition(_rb.position + direction * (_moveSpeed * Time.fixedDeltaTime));
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            if (!_isGrounded) return;

            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            OnJumped?.Invoke();
        }
    }
}
```

---

## [CODE REVIEW CHECKLIST]

Tự kiểm tra trước khi submit / tạo PR:
- [ ] Code buildable — không syntax error, không missing reference
- [ ] Không có `Debug.Log` trong production code (không có conditional)
- [ ] Không có magic number — đã dùng const hoặc enum
- [ ] Tất cả event đã unsubscribe trong `OnDisable()`
- [ ] Tất cả Coroutine đã stop trong `OnDisable()`
- [ ] Không gọi `GetComponent` trong `Update()`
- [ ] Không tạo new object trong hot path
- [ ] `[SerializeField]` field có `[Tooltip]`
- [ ] Public class và method phức tạp có `/// <summary>`
- [ ] Không có code unreachable hoặc unused variable
- [ ] Method không vượt 50 dòng, class không vượt 300 dòng
- [ ] File kết thúc bằng 1 dòng trống
- [ ] Commit message theo format `type(scope): subject`
- [ ] Không có merge conflict marker (`<<<<`, `>>>>`, `====`)
- [ ] .meta file đã commit cùng với file tương ứng
