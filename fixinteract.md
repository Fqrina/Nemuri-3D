# Solusi Perbaikan Interaksi (fixinteract)

Dokumen ini menjelaskan analisis dan langkah solusi untuk dua masalah interaksi utama pada Chapter 1:

---

## 1. Bug: Pesan Peringatan "You must use [CharacterName] as player to interact" Tidak Muncul

### Masalah:
Ketika pemain mencoba berinteraksi menggunakan karakter yang salah (misal: bukan Murial pada Puzzle 1, atau bukan Feanor/Murial pada Puzzle 2), game tidak memberikan umpan balik visual atau teks peringatan ke layar player.

### Solusi:
Perubahan dilakukan pada metode interaksi utama di **[NocturneIntroController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Scenes/NocturneIntroController.cs)**:
* Memanfaatkan sistem teks override bawaan komponen `Interactable` via fungsi `SetOverridePromptText(string text, float duration)`.
* **Penerapan pada Puzzle 1 (Shattering Rock)**:
  * Di dalam `OnRock1Interacted()`, ditambahkan pengecekan indeks karakter aktif. Jika indeks bukan **2 (Murial)**, controller memanggil:
    ```csharp
    interactable.SetOverridePromptText("You must use Murial as player to interact", 3f);
    ```
* **Penerapan pada Puzzle 2 (Vines)**:
  * Di dalam `OnPuzzle2Interacted()`, ditambahkan pengecekan fase tanaman merambat. 
  * Jika fase ripping memerlukan **Murial (2)** dan karakter yang aktif salah, memanggil:
    ```csharp
    interactable.SetOverridePromptText("You must use Murial as player to interact", 3f);
    ```
  * Jika fase untangling memerlukan **Feanor (4)** dan karakter yang aktif salah, memanggil:
    ```csharp
    interactable.SetOverridePromptText("You must use Feanor as player to interact", 3f);
    ```

---

## 2. Bug: Prompt "Hold E to interact" Tersangkut/Nyangkut di Layar Setelah Puzzle 1 Selesai

### Masalah:
Setelah minigame penghancuran batu sukses dan percakapan dialog pasca-puzzle dimulai, teks perintah interaksi `"Hold E to interact"` dengan bar progressnya tetap tampil menggantung di layar.

### Solusi:
Masalah ini dipecahkan dengan memaksa penutupan (*force hide*) prompt interaksi statis saat memasuki mode layar/dialog lain:
1. **Sentralisasi pada Dialog**:
   * Menambahkan pemanggilan metode statis **`Interactable.ForceHidePrompt()`** di awal fungsi `StartConversation()` pada **[DialogueManager.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Dialogue/DialogueManager.cs)**. 
   * Dengan ini, setiap kali dialog cutscene/percakapan baru dimulai, prompt UI interaksi apa pun akan langsung ditutup paksa.
2. **Sentralisasi pada Minigame**:
   * Menambahkan **`Interactable.ForceHidePrompt()`** di awal fungsi `StartMinigame()` pada berkas minigame **[CrystalMinigame.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Interactions/CrystalMinigame.cs)** dan **[VinesMinigame.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Interactions/VinesMinigame.cs)**.
   * Ini memastikan semua prompt dibersihkan saat minigame sedang berjalan di layar.
