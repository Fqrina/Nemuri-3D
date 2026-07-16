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

---

## 3. Bug: NPC & Player Berteleportasi ke Y = 0 dan Jatuh / Melayang Saat Berpindah Mode

### Masalah:
Saat melakukan *switch* dari *playmode* ke *dialogue mode* (atau saat NPC berteleportasi ke lokasi puzzle), NPC lain seolah tidak muncul (atau melayang di Y = 0) dan player mengalami animasi jatuh terlebih dahulu sebelum mendarat di map. Ini terjadi karena posisi Y mereka secara instan tersetting ke `0`, sementara ketinggian permukaan tanah (*ground height*) sebenarnya di map berada di sekitar `-106 Y`.

### Solusi:
Analisis menunjukkan bug terjadi pada fungsi penentuan tinggi tanah **`GetGroundHeight(Vector3 position)`** di **[NocturneIntroController.cs](file:///c:/Users/2020d/UnityProject/Nemuri%203D/Assets/Scripts/Scenes/NocturneIntroController.cs)**:
* Sebelumnya, raycast didesain menembak dari `position.y + 50.0f` ke arah bawah dengan jarak jangkauan hanya `100f` unit (menjangkau rentang `[50, -50]` Y).
* Apabila input posisi awal memiliki nilai Y `0` (sebagai default teleportasi inisial sebelum *snap*), raycast ini hanya akan mencari ground hingga batas Y `-50`. Karena tanah aslinya berada di sekitar `-106 Y`, raycast gagal mengenai apa pun dan mengembalikan nilai default input (`0`).
* **Perbaikan**: Kami memodifikasi Raycast agar menembak secara absolut dari langit map (`Y = 300f`) ke bawah dengan jarak jangkauan `600f` unit (mencakup rentang `[300, -300]` Y):
  ```csharp
  Ray ray = new Ray(new Vector3(position.x, 300.0f, position.z), Vector3.down);
  RaycastHit[] hits = Physics.RaycastAll(ray, 600f, ~0);
  ```
* Melalui perbaikan ini, `GetGroundHeight` sekarang sepenuhnya responsif terhadap ketinggian tanah sejati map yang berada di kedalaman `-100 Y` ke bawah. Baik player maupun NPC akan langsung berpindah tempat tepat di atas permukaan tanah tanpa melayang atau jatuh dari atas udara.
