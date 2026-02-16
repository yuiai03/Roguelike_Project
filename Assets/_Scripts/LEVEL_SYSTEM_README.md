# Hệ thống Level & Exp với Buff Cards

Hệ thống level, exp và buff cards được hệ thống hóa cho player trong Unity.

## Các Components Đã Tạo

### 1. Core Systems (Assets/Scripts/Systems/)
- **PlayerLevelSystem.cs**: Quản lý level và exp của player
- **BuffCard.cs**: ScriptableObject định nghĩa các buff cards
- **BuffCardManager.cs**: Quản lý pool và random selection của cards

### 2. UI Components (Assets/Scripts/UI/)
- **PlayerStatsUI.cs**: Hiển thị Level, Exp Bar và Wave hiện tại
- **CardSelectionUI.cs**: UI cho việc chọn card khi level up
- **BuffCardUI.cs**: Component hiển thị từng card riêng lẻ

### 3. Integration (Assets/Scripts/GamePlay/Enemy/)
- **ExpDropper.cs**: Component tự động drop exp khi enemy chết

---

## Hướng Dẫn Setup

### Bước 1: Tạo Buff Cards (ScriptableObjects)

1. Trong Unity, chuột phải vào Project window
2. Chọn `Create > Game > Buff Card`
3. Đặt tên cho card (ví dụ: "IncreaseDamage_Common")
4. Cấu hình các thông số:
   - **Card Name**: Tên hiển thị (ví dụ: "+25% Damage")
   - **Description**: Mô tả (ví dụ: "Increase damage by {value}%")
   - **Buff Type**: Chọn loại buff (IncreaseDamage, IncreaseMaxHealth, v.v.)
   - **Value**: Giá trị buff (25 = 25%)
   - **Rarity**: 1=Common, 2=Rare, 3=Epic, 4=Legendary
   - **Icon**: Sprite icon cho card (optional)
   - **Card Color**: Màu background của card

#### Ví dụ các Buff Cards nên tạo:

**Common Cards (Rarity 1):**
- Increase Damage +15% (value: 15)
- Increase Max Health +100 (value: 100)
- Increase Move Speed +10% (value: 10)
- Heal +50 HP (value: 50)

**Rare Cards (Rarity 2):**
- Increase Damage +25% (value: 25)
- Increase Attack Speed +20% (value: 20)
- Increase Max Health +200 (value: 200)
- Increase Move Speed +20% (value: 20)

**Epic Cards (Rarity 3):**
- Increase Damage +40% (value: 40)
- Increase Attack Speed +35% (value: 35)
- Reduce Dash Cooldown -30% (value: 30)
- Increase Projectile Speed +10 (value: 10)

**Legendary Cards (Rarity 4):**
- Increase Damage +60% (value: 60)
- Heal +200 HP (value: 200)
- Increase Attack Speed +50% (value: 50)

### Bước 2: Setup Scene Objects

#### A. Tạo PlayerLevelSystem GameObject
1. Tạo Empty GameObject tên "PlayerLevelSystem"
2. Add component `PlayerLevelSystem`
3. Cấu hình:
   - Current Level: 1
   - Current Exp: 0
   - Exp To Next Level: 100
   - Exp Scaling Factor: 1.5 (mỗi level cần exp * 1.5)

#### B. Tạo BuffCardManager GameObject
1. Tạo Empty GameObject tên "BuffCardManager"
2. Add component `BuffCardManager`
3. Cấu hình:
   - **All Cards**: Kéo tất cả BuffCard ScriptableObjects vào list này
   - **Cards Per Selection**: 3 (số card hiện khi level up)
   - **Player Data**: Kéo Player GameObject (component PlayerData)
   - **Player Health**: Kéo Player GameObject (component PlayerHealth)

#### C. Setup Player Stats UI (Canvas)
1. Trong Canvas, tạo Empty GameObject tên "PlayerStatsUI"
2. Add component `PlayerStatsUI`
3. Tạo các UI elements con:
   - **Level Text**: TextMeshProUGUI hiển thị "Level X"
   - **Exp Bar**: Image với Fill Amount (sử dụng Filled type)
   - **Exp Text**: TextMeshProUGUI hiển thị "X/Y"
   - **Wave Text**: TextMeshProUGUI hiển thị "Wave: X/Y"
4. Kéo các references vào component:
   - Level Text, Exp Bar Fill, Exp Text, Wave Text
   - Player Level System, Wave Spawner

#### D. Setup Card Selection UI (Canvas)
1. Tạo Panel tên "CardSelectionPanel" (ẩn ban đầu)
2. Add component `CardSelectionUI`
3. Tạo cấu trúc UI:
   ```
   CardSelectionPanel (Panel, background tối)
   ├── Title (TextMeshProUGUI: "LEVEL UP!")
   ├── CardsContainer (Horizontal Layout Group)
   └── (Card prefabs sẽ spawn vào đây)
   ```
4. Tạo Card Prefab:
   - Tạo GameObject tên "BuffCardPrefab"
   - Add component `BuffCardUI`
   - Tạo cấu trúc:
     ```
     BuffCardPrefab (Button với Image background)
     ├── Icon (Image)
     ├── Name (TextMeshProUGUI)
     ├── Description (TextMeshProUGUI)
     └── Rarity (TextMeshProUGUI)
     ```
   - Kéo các references vào BuffCardUI component
   - Lưu thành Prefab trong Assets/Prefabs/

5. Cấu hình CardSelectionUI:
   - **Selection Panel**: CardSelectionPanel GameObject
   - **Cards Container**: CardsContainer GameObject
   - **Card UI Prefab**: BuffCardPrefab
   - **Pause Game On Show**: true

#### E. Setup Enemies (Prefabs)
1. Mở tất cả Enemy Prefabs
2. Add component `ExpDropper` vào mỗi enemy prefab
3. Component sẽ tự động tìm Enemy và EnemyData
4. Trong EnemyData, cấu hình **Exp Value**:
   - Weak enemies: 10 exp
   - Normal enemies: 25 exp
   - Strong enemies: 50 exp
   - Boss enemies: 100+ exp

---

## Cách Hoạt Động

1. **Giết Enemy → Nhận Exp**
   - Enemy chết → ExpDropper.DropExp() được gọi
   - PlayerLevelSystem.AddExp() cộng exp
   - Exp bar UI tự động update

2. **Đủ Exp → Level Up**
   - PlayerLevelSystem kiểm tra currentExp >= expToNextLevel
   - Trigger OnLevelUp event
   - Time.timeScale = 0 (pause game)
   - CardSelectionUI hiển thị

3. **Chọn Card**
   - BuffCardManager random 3 cards dựa trên rarity
   - Hiển thị 3 cards trong UI
   - Player click chọn 1 card
   - Card buff được apply vào PlayerData
   - UI ẩn, game resume (Time.timeScale = 1)

4. **Buff được Apply**
   - BuffCard.ApplyBuff() modify PlayerData stats
   - Các multipliers (damageMultiplier, moveSpeedMultiplier, v.v.) được tăng
   - Player mạnh hơn!

---

## Các Loại Buff Hiện Tại

### Damage Buffs
- **IncreaseDamage**: Tăng % damage
- **IncreaseProjectileSpeed**: Tăng tốc độ đạn
- **IncreaseAttackSpeed**: Tăng % attack speed

### Health Buffs
- **IncreaseMaxHealth**: Tăng max HP (và heal lượng tăng)
- **HealthRegen**: Heal ngay lập tức

### Movement Buffs
- **IncreaseMoveSpeed**: Tăng % tốc độ di chuyển
- **ReduceDashCooldown**: Giảm % cooldown dash

### Special Buffs (TODO)
- **ExpBoost**: Tăng % exp nhận được (cần implement)
- **MultiShot**: Bắn nhiều đạn cùng lúc (cần implement)
- **CriticalChance**: Cơ hội chí mạng (cần implement)

---

## Testing

### Test trong Unity Editor:

1. **Test Level System:**
   - Chọn PlayerLevelSystem GameObject
   - Trong Inspector, chuột phải component
   - Chọn "Add 50 Exp" hoặc "Level Up"

2. **Test Card Manager:**
   - Chọn BuffCardManager GameObject
   - Chuột phải component
   - Chọn "Test Random 3 Cards"

3. **Test Card Selection UI:**
   - Chọn CardSelectionUI GameObject
   - Chuột phải component
   - Chọn "Test Show Cards"

---

## Mở Rộng Hệ Thống

### Thêm Buff Type Mới:

1. Mở `BuffCard.cs`
2. Thêm enum trong `BuffType`:
   ```csharp
   public enum BuffType
   {
       // ...existing...
       YourNewBuff
   }
   ```

3. Thêm case trong `ApplyBuff()`:
   ```csharp
   case BuffType.YourNewBuff:
       // Your implementation
       break;
   ```

### Tùy Chỉnh Rarity Chances:

Mở `BuffCardManager.cs`, method `GetWeightedRandomCard()`:
```csharp
// Hiện tại: Common=4, Rare=3, Epic=2, Legendary=1
float weight = 5f - card.rarity;

// Có thể đổi thành:
float weight = Mathf.Pow(2f, 5 - card.rarity); // Exponential scaling
```

---

## Troubleshooting

**Vấn đề: Card UI không hiện khi level up**
- Kiểm tra CardSelectionUI có đăng ký OnLevelUp event không
- Kiểm tra BuffCardManager có cards trong list không
- Kiểm tra Card Prefab và references

**Vấn đề: Không nhận exp khi giết enemy**
- Kiểm tra Enemy có component ExpDropper không
- Kiểm tra EnemyData.expValue > 0
- Kiểm tra PlayerLevelSystem instance tồn tại

**Vấn đề: Buff không apply**
- Kiểm tra BuffCardManager có reference PlayerData không
- Kiểm tra BuffCard.ApplyBuff() có chạy không (debug log)

---

## Notes

- Hệ thống sử dụng UnityEvents để decouple components
- Time.timeScale = 0 khi chọn card (pause game)
- Cards được weight theo rarity (higher rarity = lower chance)
- Unlimited levels (không có level cap)
- Multipliers stack additively (e.g., +15% + +25% = +40%)
