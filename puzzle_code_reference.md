# Nocturne Intro Scene Puzzle Code Reference

This document maps each puzzle's visual context and interactive logic in Chapter 1 to its respective file paths and line ranges in the codebase.

---

## 🧩 Puzzle 1: Shattering Rock (Somnia Seed)

* **Target Object**: `dobj.001` (Crystal) / `rockpuzzle1` (Rock obstruction)
* **Active Character Required**: **Murial** (Index 2) to break the rock.

### Code Mappings:
1. **Minigame Behavior**:
   * File: [CrystalMinigame.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Interactions/CrystalMinigame.cs#L320-L345)
   * Description: Manages the minigame state, progress, and updates the minigame title dynamically to `"SHATTERING ROCK"`.
2. **Interaction Trigger**:
   * File: [NocturneIntroController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Scenes/NocturneIntroController.cs#L2526-L2554)
   * Description: `OnRock1Interacted()` validates if the active character is Murial. If not, displays warning override text: `"You must use Murial as player to interact"`.
3. **Approaching/Walking State**:
   * File: [NocturneIntroController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Scenes/NocturneIntroController.cs#L1160-L1320)
   * Description: `Update()` handles the state `WaitingForSomniaDialogue` when characters arrive near `rockpuzzle1`.
4. **Dialogue & Collection Flow**:
   * File: [NocturneIntroController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Scenes/NocturneIntroController.cs#L2555-L2665)
   * Description: Triggers Somnia Seed Part 1 dialogue and walks, resolves minigame completion, lowers `rockpuzzle1`, enables collider on `dobj.001`, and triggers D57/D58/T8 after physical pickup in [CrystalPickup.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Interactions/CrystalPickup.cs#L37-L43).

---

## 🧩 Puzzle 2: Rip & Untangle Vines (Crescent Tear)

* **Target Object**: `dobj` (Crescent Tear) / `Puzzle2InteractionPoint` (Vines)
* **Active Characters Required**:
  1. **Murial** (Index 2) to rip the vines.
  2. **Feanor** (Index 4) to untangle the vines.

### Code Mappings:
1. **Vines Interactions**:
   * File: [NocturneIntroController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Scenes/NocturneIntroController.cs#L2717-L2769)
   * Description: `OnPuzzle2Interacted()` handles character checks for both phases (Murial for ripping, Feanor for untangling), using `SetOverridePromptText` for wrong character warnings.
2. **Walk Arrival**:
   * File: [NocturneIntroController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Scenes/NocturneIntroController.cs#L1360-L1510)
   * Description: `Update()` state `WaitingForCrescentDialogue` triggers when party characters arrive near the vines.
3. **Dialogue & Tele-alignment**:
   * File: [NocturneIntroController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Scenes/NocturneIntroController.cs#L2810-L2955)
   * Description: `AlignCharactersAtCrescentTearDialoguePositions()` resets active character to Kael and positions Kael player alongside RonaNPC, MurialNPC, KeikoNPC, and FeanorNPC around the vines to prevent missing characters during cutscenes.
4. **Vines Dissolving Animation**:
   * File: [NocturneIntroController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Scenes/NocturneIntroController.cs#L2927-L2975)
   * Description: `SmoothMovePuzzle2Routine()` lowers the vines/crystal `dobj` Y position smoothly by 1 unit on Feanor success.

---

## 🧩 Puzzle 3: Dreampearl Jembatan (Bridge 1 & 2)

* **Target Object**: `dobj.002` (Dreampearl) / `PINEALGRAND` / `Puzzle3InteractionPoint`
* **Active Character Required**: **Rona** (Index 1) to build both bridges.

### Code Mappings:
1. **Bridge 1 Proximity & Fixing**:
   * File: [BridgeController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/BridgeController.cs) (Entire File)
   * Description: Detects player distance to `"pivot bridge"` (bridge center). Triggers Dialogue automatically on proximity; requires Rona to hold E to manifest Bridge 1.
2. **Bridge 1 Success Dialogue**:
   * File: [NocturneIntroController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Scenes/NocturneIntroController.cs#L2976-L3079)
   * Description: Handled by `OnBridgeInteracted()`, `TriggerBridge1Success()`, and success dialogue sequence.
3. **Bridge 2 Interaction**:
   * File: [NocturneIntroController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Scenes/NocturneIntroController.cs#L3132-L3170)
   * Description: `OnPuzzle3Interacted()` triggers Dialogue 3, and then requires Rona to manifest the second bridge.
4. **Bridge 2 Manifestation Animation**:
   * File: [NocturneIntroController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Scenes/NocturneIntroController.cs#L3171-L3220)
   * Description: `SmoothMovePuzzle3BridgeRoutine()` makes all child rigidbodies kinematic (ignores gravity) and smoothly interpolates `PuzzleBridge` from water level up to Y = `2.985f` while fading in materials.
