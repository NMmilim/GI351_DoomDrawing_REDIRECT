# Team Handoff: New Systems & Script Changes

All scripts compile against Unity **6000.3.10f1** (0 errors). **No UI, scenes, prefabs, or Canvas objects were created or modified.** Every new UI script only *connects* to UI your team builds, and every reference is optional.

> [!IMPORTANT]
> Your existing **main menu (GameMenu Canvas) and `MainMenu.cs` were not touched.** The shop is a separate script (`BaseUpgradeMenu`) you hook up when you add the Upgrade button.

---

## 1. New scripts

| Script | Attach to | What it does |
|---|---|---|
| [MetaProgression.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/Upgrades/MetaProgression.cs) | **Nothing** (static) | Saves coins and base-upgrade levels (PlayerPrefs). **Costs and bonuses are set here.** |
| [UpgradeManager.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/Upgrades/UpgradeManager.cs) | Any active object in the gameplay scene (**not** the panel) | Wave-end "Select Upgrade": rolls 3 cards by rarity, pauses the game, applies the pick. Pool is editable in the Inspector. |
| [UpgradeCardUI.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/Upgrades/UpgradeCardUI.cs) | Each of the 3 cards | Fills in title, description, "current → next" line, rarity, and tint. Click is wired by code. |
| [GameOverScreen.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/UI/GameOverScreen.cs) | Any active object (e.g. Canvas) | Shows the death screen with stats and coins. Wires **Restart / Menu / Quit** by code. |
| [RailgunCooldownUI.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/UI/RailgunCooldownUI.cs) | Railgun icon | Dark shadow that slides down at the exact cooldown speed (see §4). |
| [AmmoUI.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/UI/AmmoUI.cs) | HUD (optional) | Ammo text, "RELOADING" label, reload fill, bullet icons. |
| [BaseUpgradeMenu.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/MainMenu/BaseUpgradeMenu.cs) | Active object in GameMenu Canvas | Shop panel: `Open()` / `Close()`, coin display. Right-click gives debug +1000 coins / reset. |
| [BaseUpgradeRowUI.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/MainMenu/BaseUpgradeRowUI.cs) | Each shop row | One stat: level, bonus, cost, 5 level pips, Buy button. |
| [AnimatorUtil.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/PlayerScript/AnimatorUtil.cs) | **Nothing** (static) | Sets Animator parameters only if they exist, so nothing breaks before the animators are done. |

## 2. Modified scripts

| Script | Change |
|---|---|
| [Shooting.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/PlayerScript/Shooting.cs) | Limited magazine, infinite reloads (**R** or automatic when empty), fire-rate cooldown, hold-to-fire, animation hooks. Can't fire while reloading or charging the railgun. |
| [Skill_rail.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/PlayerScript/Skill_rail.cs) | **Charge before firing**: muzzle glow, aim laser, optional FX/sound, slows the player. Cooldown starts after the shot. Animation hooks. Class renamed `Skill_` → `Skill_rail` (now matches the filename). |
| [Bullet.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/PlayerScript/Bullet.cs) | Ricochets = 1 + upgrades (3 upgrades = 4 bounces). **Homing ricochet** chains into the next visible enemy. Null-safe blood, 6s lifetime safety. |
| [PlayerManagement.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/PlayerScript/PlayerManagement.cs) | Final stat = **Base + permanent + in-run** upgrades. New *Base Stats* and *upgrade strength* fields. Death flag. |
| [Player.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/PlayerScript/Player.cs) | HP reaching 0 → Game Over flow. HP is clamped. |
| [PlayerMovement.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/PlayerScript/PlayerMovement.cs) | Stops when dead. Applies the railgun-charge slow. |
| [GameManager.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/GameManager.cs) | Singleton. Game Over (banks coins, short slow-mo, shows screen). `ReturnToMenu()` (CrossFade if LevelManager exists). Tracks run coins. Esc is blocked during upgrade / death screens. `goMenu` now shows/hides a pause panel. |
| [EnemySpawner.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/Enemscript/EnemySpawner.cs) | After a cleared wave: awards wave coins → opens upgrade screen → waits for the pick → next wave. Toggle with `offerUpgradesBetweenWaves`. |
| [Enemy_Range.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/Enemscript/Enemy_Range.cs) | `coinReward` field. Active-enemy list (used by homing). Can't be "killed twice" in one frame. |
| [Enemy_Close.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/Enemscript/Enemy_Close.cs) | **Bug fix**: it was passing `-damage`, which *healed* the player. It also crashed when touching non-player objects. |
| [Upg_ricochet.cs](file:///c:/Github/GI351_DoomDrawing_PlaceHolder/Assets/Script/Bullet_upgrade/Upg_ricochet.cs) | Class name fixed. Now superseded by the new upgrade system, so it can be deleted. |

> [!WARNING]
> After Unity recompiles, check the **Railgun component in TestPlace**. It was renamed to match its file and should stay attached. If it shows "Missing Script", re-add `Skill_rail` and re-drag `bulletPrefab`, `firepos`, and `muzzleLight`.

---

## 3. Animation parameters (all optional; names editable in the Inspector)

Assign the Animator to `Shooting → gunAnimator` and `Skill_rail → railAnimator`.

| Animator | Parameter | Type | When |
|---|---|---|---|
| Pistol | `Fire` | Trigger | Every shot |
| Pistol | `Reload` | Trigger | Reload starts |
| Pistol | `IsReloading` | Bool | True for the whole reload |
| Pistol | `ReloadSpeed` | Float | `reloadClipLength / reloadTime` |
| Railgun | `IsCharging` | Bool | True while charging |
| Railgun | `ChargeSpeed` | Float | `chargeClipLength / chargeTime` |
| Railgun | `RailFire` | Trigger | Shot fires (recoil) |
| Railgun | `RailCancel` | Trigger | Charge cancelled |

> [!TIP]
> To keep animations synced to gameplay timing, set the state's **Speed → Multiplier → Parameter** to `ReloadSpeed` / `ChargeSpeed`. Then enter the clip length in `reloadClipLength` / `chargeClipLength`. Changing `reloadTime` or `chargeTime` will then rescale the animation automatically.

---

## 4. Railgun cooldown UI: use a Filled Image instead of a Slider

A Slider works (it's supported), but a **Filled Image** is simpler and looks cleaner. There's no handle or fill-area setup, and it can darken only the gun's silhouette.

```
RailgunIcon        (Image – railgun picture)        ← RailgunCooldownUI here
 └─ CooldownShadow (Image – same railgun sprite, black ~70% alpha)
      Image Type = Filled | Fill Method = Vertical | Fill Origin = Bottom
```
- Drag `CooldownShadow` into **Shadow Image**. It goes fully dark when fired, and its top edge slides down until clear. The speed always matches `cooldown × 3`.
- Optional extras: seconds-left text, charge fill while charging, grey tint, and a small "pop" when ready.
- Prefer a Slider? Drag it into **Shadow Slider** instead (Direction: Bottom To Top, delete the Handle).

---

## 5. Setup checklist

**Gameplay scene**
1. `UpgradeManager` on an active object → assign `panel`, 3 `cards` (each with `UpgradeCardUI`), optional `stageText`.
2. `GameOverScreen` on an active object → assign `root`, 3 buttons, optional texts → drag it into **GameManager → Game Over Screen**.
3. `RailgunCooldownUI` and (optional) `AmmoUI` on the HUD.
4. Optional: `Skill_rail → aimLineMask` = Wall layer (so the laser stops at walls). Untick `showAimLine` if you don't want the laser.

**GameMenu scene** (when you add the Upgrade button)
1. `BaseUpgradeMenu` on the Canvas → assign `panel`, `coinsText`, 3 rows, `closeButton`.
2. Each row: `BaseUpgradeRowUI` and pick its **Stat** (MaxHp / MaxSpeed / MaxMagazine).
3. New Upgrade button → OnClick → `BaseUpgradeMenu.Open`.

**SoundLibrary IDs to add** (missing ones are just silent): `Pistol_Reload`, `Pistol_Empty`, `Railgun_Charge`, `Upgrade_Pick`, `Upgrade_Buy`.

---

## 6. Default balance (all editable)

| What | Value | Where |
|---|---|---|
| Magazine / fire interval / reload | 8 bullets / 0.25s / 1.2s | PlayerManagement, Shooting |
| Railgun charge | 0.8s, 50% move speed | Skill_rail |
| In-run upgrade per pick | Fire rate ×0.85 interval, +0.5 speed, +2 mag, +1 bounce | PlayerManagement |
| Card rarity | Ricochet *Rare*, Fire Rate / Speed / Mag *Common*, Seeker Rounds (homing) *Legendary*, weight 3, once per run | UpgradeManager pool |
| Coins | 5 per kill, 20 × wave number per cleared wave | Enemy_Range, GameManager |
| Base upgrade costs (Lv 1→5) | 150 / 350 / 700 / 1200 / 2000 | MetaProgression |
| Base upgrade per level | +20 HP, +0.4 speed, +2 bullets | MetaProgression |
