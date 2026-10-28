# Dungeon Escape: Geliştirme Planı ve İlerleme

> **Yeni oturumda ilk iş:** Bu dosyayı oku, ilk `[ ]` maddeyi bul ve oradan devam et.
> Her madde bitince `[x]` yap, bölüm bitince commit at ve "Oturum Günlüğü"ne bir satır ekle.
> Son teslim: **6 Kasım 2026 Cuma 17:00**. Sunum: 9-13 Kasım 2026.

## Oyun Tasarımı

**Tür:** 2D, kuş bakışı (top-down) zindandan kaçış. Pixel art, 16px tile.

**Akış:**
1. Oyuncu kilitli bir **hücrede** başlar. Hücre duvarında yalnızca farenin sığdığı **dar bir geçit** var.
2. Oyuncu yoldaşı **fareye** komut verir (`F`). Fare NavMesh ile yol bulur ve dar geçitten çıkar.
3. Fare, **rastgele yerleşen** muhafızlardan **NavMesh yol uzunluğuna göre en yakınını** seçer.
   Raycast sensörleriyle muhafızın görüş konisini ve engelleri algılar, sezdirmeden yaklaşıp anahtarı çalar.
4. Fare aynı geçitten **geri döner** ve anahtarı oyuncuya getirir. Oyuncu hücre kapısını açar.
5. Oyuncu devriye gezen muhafızları arkadan yaklaşarak **etkisiz hale getirir** (gizlilik + `E`).
   Görülürse yakalanır ve bölüm yeniden başlar.
6. Çıkış kapısındaki **son muhafız (Gardiyan)** dövüşmez, konuşur. Oyuncu ona serbest metin yazar.
   Yerel LLM (Ollama) yapılandırılmış JSON ile cevap verir. Oyuncu onu kapıyı açacak kadar korkutmalıdır.
7. Korku eşiği aşılıp model `OPEN_DOOR` dediğinde kapı açılır ve oyuncu **özgürlüğüne kavuşur** (oyun sonu).
   Model `ATTACK` derse oyun kaybedilir.

**Ödev eşlemesi** (`26_27Guz_YazLab_I_Proje_I_Dokuman (1).pdf`):

| Ödev isteri | Oyundaki karşılığı |
|---|---|
| Oyuncunun aşamadığı fiziksel engel | Hücre + yalnızca farenin sığdığı geçit |
| Komutla çalışan otonom companion, NavMesh yol bulma | Fare: NavMeshPlus ajanı, `F` komutu |
| Raycast sensörü (mesafe/açı) | Farenin engel ve muhafız görüş algısı için ışın demeti |
| Karar mantığı açıkça tasarlanmış (FSM) | `RatStateMachine`: Idle → SelectTarget → Approach → Steal → Return → Deliver |
| Kapıyı koruyan, serbest metinle konuşulan LLM NPC | Çıkıştaki Gardiyan |
| Yapılandırılmış JSON çıktı + sınıfla ayrıştırma | Ollama `format` JSON şeması → `GuardDecision` DTO → `DecisionParser` |
| Bozuk JSON, sunucu kapalı, model yok, zaman aşımı | Sağlık kontrolü, timeout, JSON onarma, güvenli fallback |
| Bloklamayan async + "Düşünüyor..." | `async/await` + UnityWebRequest, UI göstergesi |
| Farklı başlangıçlarda tekrarlanabilir ajan testi | Seed'li spawner + PlayMode test, başarı oranı ve süre CSV |
| En az 5 diyalog senaryosu | Senaryo koşucusu: ikna, korkutma, kızdırma, alakasız, rüşvet, boş girdi |
| OOP/SOLID | Arayüzler: `ILLMClient`, `IDecisionParser`, `ICommandable`, `IInteractable`, `IKeyHolder`, `IState` |
| README + Mermaid rapor | Durum, akış ve sequence diyagramları |

## Teknik Kararlar

- **Unity:** 6000.6.6f1, URP. Proje klasörü: `YazLabUnity/`.
- **2D NavMesh:** `com.unity.ai.navigation` + **NavMeshPlus** (h8man). Farenin ayrı bir agent type'ı olur ("Rat", küçük yarıçap). Dar geçit yalnız onun NavMesh'inde açıktır.
- **Oyuncu:** Rigidbody2D + Input System. NavMesh kullanmaz, çarpıştırıcısı geçide sığmaz.
- **LLM:** Ollama `http://localhost:11434/api/chat`, `stream:false`, `format` = JSON şeması.
  Model: `llama3.2:3b` (Apple M4, 24 GB RAM). Yedek: `qwen2.5:3b`. Kurulum: `ollama pull llama3.2:3b`.
- **Çıktı şeması:** `{ "reply": string, "fear": 0-100, "mood": "calm|nervous|scared|angry", "action": "NONE|OPEN_DOOR|ATTACK" }`
  Güvenlik kuralı: kapı ancak `action == OPEN_DOOR` **ve** biriken korku ≥ eşik ise açılır.
- **Asset'ler:** `free-2d-top-down-pixel-dungeon-asset-pack` (craftpix, 16px) ana tileset. Duvar, zemin, parmaklıklı/ahşap kapı animasyonu, anahtar, şalter, sandık.
  `2ddungeonassetpackwithoutoutline` (CC0, 2048px) yalnızca UI ikonları için (anahtar, iksir).
  **Eksik:** karakter sprite'ları (oyuncu, fare, muhafız). Bunlar Bölüm 0'da çözülecek.
- **Git:** `git@github.com:fuzienn/YazLabProje.git`. İki SSH hesabı: `github.com` → fuzienn (Hüseyin Melih Güzel), `github-ikinci` → dikmenomer33 (Ömer Alp Dikmen, `349348461+dikmenomer33@users.noreply.github.com`).
  Library geçmişten silindi (filter-branch, force push 2026-10-28). Eski 420 MB'lık klonlar kullanılmamalı.
  Commit'ler bölüm sahibine göre yazar kimliğiyle atılır (aşağıdaki "Sahip" sütunu).

## Bölümler

### Bölüm 0: Altyapı · Sahip: fuzienn
- [x] Yerel klasörü uzak repoyla birleştir (`main` + `feature/ai-integration` geçmişi korunarak)
- [x] `.gitignore`'u `YazLabUnity/Library`, `Temp`, `Logs`, `UserSettings`, `Builds` için düzelt. Library'yi index'ten çıkar
- [x] Ham asset klasörlerini repoya alma (sadece kullanılanlar `Assets/Art` altına)
- [x] Paketler: `com.unity.ai.navigation`, NavMeshPlus, `com.unity.2d.sprite`, `com.unity.2d.tilemap`, Test Framework
- [x] URP 2D Renderer'a geç, `Main` sahnesini oluştur, SampleScene'i kaldır
- [x] Klasör yapısı: `Assets/_Project/{Scripts,Art,Prefabs,Scenes,Tiles,Tests}`
- [x] Tileset'leri içe aktar ve dilimle (16px, Point filter, PPU 16)
- [x] Karakter sprite'larını bul (CC0) veya üret: oyuncu, fare, muhafız, gardiyan → `Tools/generate_characters.py` ile üretildi (satır: down/up/side, sütun: durus/yurume1/yurume2; sola bakış flipX)
- [ ] Ollama modelini indir (`ollama pull llama3.2:3b`) ve terminalden JSON şemalı test isteği at

### Bölüm 1: Harita (Tilemap) · Sahip: dikmenomer33
- [ ] Kapı sayfası (`doors_lever_chest_animation.png`) otomatik dilimde birleşiyor: kapılar için 32x32 ızgara dilimi ayarla
- [ ] Tile Palette ve kural tile'ları (duvar/zemin)
- [ ] Hücre odası + parmaklıklı kapı + farenin geçidi (duvarda çatlak/delik)
- [ ] Koridor, muhafız salonu, spawn bölgeleri, çıkış kapısı ve gardiyan odası
- [ ] Duvar collider'ları (TilemapCollider2D + CompositeCollider2D), sorting layer'lar
- [ ] Dekor: meşale (ışık), fıçılar, kafes

### Bölüm 2: Oyuncu ve Çekirdek Sistemler · Sahip: fuzienn
- [ ] `PlayerController` (Input System, Rigidbody2D, 4 yön animasyon)
- [ ] Kamera takibi (Cinemachine veya basit follow), pixel-perfect kamera
- [ ] `IInteractable` + `Interactor` (E tuşu, en yakın etkileşim)
- [ ] `Inventory` (anahtar) + `IKeyHolder`
- [ ] `GameEvents` olay yolu ve `GameStateMachine` (Cell → Escape → Dialogue → Won/Lost)

### Bölüm 3: Fare (Otonom Companion) · Sahip: dikmenomer33
- [ ] NavMeshPlus kurulumu: `NavMeshSurface` (2D), "Rat" agent type, geçidin sadece fareye açık olması
- [ ] `RatAgent` + `NavMeshAgent` 2D ayarları (updateRotation/upAxis kapalı)
- [ ] `RaySensor2D`: N ışınlı demet, engel mesafe/açı + muhafız görüş algısı (Physics2D.Raycast)
- [ ] FSM: `IState` arayüzü, Idle/Follow → SelectTarget → Approach → Steal → Return → Deliver
- [ ] Hedef seçimi: tüm muhafızlar için `NavMesh.CalculatePath` uzunluğu, en kısa olan
- [ ] Gizlilik: muhafız fareye bakıyorsa bekle/etrafından dolaş
- [ ] Oyuncu komutu (`F`) + UI'da farenin durumu
- [ ] Gizmo/debug çizimleri (yol, ışınlar). Rapor görselleri için gerekli

### Bölüm 4: Muhafızlar · Sahip: fuzienn
- [ ] `GuardSpawner`: seed'li rastgele konumlar, geçerli NavMesh noktası, birbirine min. mesafe
- [ ] `Guard`: devriye (NavMesh), anahtar taşıma (`IKeyHolder`), görüş konisi (raycast)
- [ ] Etkisiz hale getirme: arkadan yaklaşıp `E`. Görülürse yakalanma → yeniden başlat
- [ ] Gardiyan (son muhafız) sabit konumda, savaş yok, diyaloğu tetikler

### Bölüm 5: LLM NPC (Gardiyan) · Sahip: dikmenomer33
- [ ] Mevcut `OllamaClient`'ı yeniden düzenle: `ILLMClient` arayüzü, `/api/chat`, mesaj geçmişi
- [ ] JSON şeması ile `format` parametresi; `GuardDecision` DTO
- [ ] `IDecisionParser`: doğrulama, `{...}` ayıklama ile onarım, enum/aralık kontrolü
- [ ] Hata yönetimi: başlangıçta `/api/tags` sağlık kontrolü, model yok, sunucu kapalı, timeout, iptal
- [ ] Sistem promptu (kişilik + amaç + kurallar), Türkçe; rapora koy
- [ ] `FearMeter`: korku birikimi ve eşik; kapı açma kuralı
- [ ] Diyalog UI: metin girişi, sohbet geçmişi, "Düşünüyor..." animasyonu, korku barı, hata mesajları
- [ ] Oyun durumuna bağla: OPEN_DOOR → kapı animasyonu → kazanma; ATTACK → kaybetme

### Bölüm 6: Oyun Akışı ve Cila · Sahip: fuzienn
- [ ] Ana menü, durdurma menüsü, kazanma/kaybetme ekranları
- [ ] Görev ipuçları (HUD: "Fareye F ile emir ver" vb.)
- [ ] Ses efektleri (opsiyonel), kapı/meşale animasyonları
- [ ] Tam akış uçtan uca oynanabilir

### Bölüm 7: Testler · Sahip: dikmenomer33
- [ ] EditMode: `DecisionParser` (geçerli, bozuk, eksik alan, alan dışı değer, boş)
- [ ] EditMode: `FearMeter` ve kapı kuralı
- [ ] PlayMode: fare görevi × 50 farklı seed → başarı oranı, ort. süre → `Docs/test_results/rat_runs.csv`
- [ ] Diyalog senaryo koşucusu: ≥5 senaryo gerçek Ollama ile → `Docs/test_results/dialogues.md`
- [ ] Ollama kapalıyken ve model yokken oyunun çökmediğini doğrula (ekran görüntüsü)

### Bölüm 8: Build ve Rapor · Ortak (bölümler sahiplerine göre)
- [ ] macOS build (ve mümkünse Windows)
- [ ] README: giriş, senaryo, mimari (sınıf diyagramı)
- [ ] Mermaid: fare FSM durum diyagramı + karar akışı
- [ ] Mermaid: LLM sequence diyagramı, sistem promptu, JSON şeması
- [ ] Hata yönetimi stratejisi bölümü
- [ ] Ekran görüntüleri, test sonuçları, örnek diyaloglar
- [ ] Sonuç ve tartışma; model seçim gerekçesi
- [ ] Teslim zip'i: kod + build + rapor

### Bölüm 9 (Bonus +10): ML-Agents · İsteğe bağlı
- [ ] Fare için RL ortamı, ödül fonksiyonu, eğitim, TensorBoard ödül grafiği

## Oturum Günlüğü

| Tarih | Yapılan | Sıradaki |
|---|---|---|
| 2026-10-28 | Ödev analizi, tasarım ve plan yazıldı. Unity MCP bağlı. Uzak repo incelendi: Library yanlışlıkla commit'lenmiş, `feature/ai-integration`'da ilk Ollama istemcisi var | Bölüm 0 |
| 2026-10-28 | Geçmiş temizlendi (420 MB → 77 KB), feature dalı main'e ff edildi, gitignore düzeltildi, plan push'landı | Bölüm 0: paketler + 2D renderer |
| 2026-10-28 | Paketler (NavMeshPlus 0.2.23, AI Navigation 2.0.16, 2D Sprite/Tilemap), URP 2D Renderer, Main sahnesi, `ArtImportProcessor`, karakter sprite'ları, asmdef'ler (`DungeonEscape.Runtime/Editor`) | Ollama modeli, sonra Bölüm 1 |
