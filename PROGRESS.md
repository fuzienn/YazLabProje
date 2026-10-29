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
  Model: **`gemma3:4b`** (Apple M4, 24 GB RAM, yanıt 1,7-3,6 sn). Kurulum: `ollama pull gemma3:4b`.
  Seçim gerekçesi (rapora): `llama3.2:3b` ile karşılaştırıldı. llama Türkçede anlamsız cümle kurdu ve hakarete `OPEN_DOOR` verdi; gemma3 tutarlı Türkçe ve uygun eylem üretti.
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
- [x] Ollama modelini indir ve terminalden JSON şemalı test isteği at (gemma3:4b seçildi; `format` şeması her yanıtta geçerli JSON verdi)

### Bölüm 1: Harita (Tilemap) · Sahip: dikmenomer33
- [x] Kapı sayfası 32x32 dilimlendi (`door_grate_*`, `door_wood_*`, `chest_*`, `lever_*`); ateş sayfası sabit karelerle (`brazier_*`, `torch_*`)
- [x] Karo seçimi: `TileTheme` (zemin 4 varyant, duvar tepe/üst yüz/alt yüz). Tile Palette yerine ASCII harita + `Dungeon Escape/Build Level 01` menüsü (`LevelBuilder`)
- [x] Hücre odası + parmaklıklı kapı + farenin geçidi (`r` tüneli, `rat_hole.png`)
- [x] Koridor, muhafız salonu, spawn bölgeleri (`g`), çıkış kapısı ve gardiyan odası (`Tiles/level01.txt`)
- [x] Duvar collider'ları (TilemapCollider2D + CompositeCollider2D), sıralama: zemin -10, karakter 0, duvar 10, kapı 11
- [x] Dekor: animasyonlu mangal + Light2D (fıçı/kafes Bölüm 6 cilaya taşındı)

### Bölüm 2: Oyuncu ve Çekirdek Sistemler · Sahip: fuzienn
- [x] `PlayerController` (Input System, Rigidbody2D, 4 yön animasyon); girdi `IPlayerInput` arkasında, `F` = `Command` eylemi eklendi
- [x] Kamera takibi (`CameraFollow`, harita sınırına kenetli) + pixel-perfect kamera
- [x] `IInteractable` + `Interactor` (E tuşu, en yakın etkileşim) + istem metni (`InteractionPromptView`)
- [x] `KeyRing` (anahtar) + `IKeyHolder` + `LockedDoor` (hücre: anahtar, çıkış: `Open()`)
- [x] `GameEvents` olay yolu, `GameFlow` (InCell → Escaping → Dialogue → Won/Lost), genel `StateMachine` + `IState` (fare/muhafız kullanacak)

### Bölüm 3: Fare (Otonom Companion) · Sahip: dikmenomer33
- [x] NavMeshPlus kurulumu: `Surface_Rat` + `Surface_Guard`, ajan tipleri Rat (r=0.15) / Guard (r=0.3), `RatTunnel` alanı (3) muhafız maskesinde kapalı; çalışma anında pişirme (`NavMeshRuntimeBaker`, Start)
- [x] `RatAgent` + `NavMeshNavigator` (`INavigator`; updateRotation/upAxis kapalı, baseOffset 0)
- [x] `RaySensor2D`: 16 ışın, 3 m, Walls+Guards katmanı; hedef menzil tespiti, en yakın engel, görüş hattı
- [x] FSM: Idle → SelectTarget → Approach ⇄ Hide → Steal → Return → Deliver (`Companion/States/`)
- [x] Hedef seçimi: `NearestByPathSelector` (`ITargetSelector`), NavMesh yol uzunluğu
- [x] Gizlilik: muhafız `CanSee` ise Hide; 0.4 sn görüş dışı kalınca devam; muhafızın arkasına yaklaşır
- [x] Oyuncu komutu (`F` → `PlayerController.CommandIssued` → `RatAgent.Command`); `Status`/`StatusChanged` hazır (HUD Bölüm 6)
- [x] Gizmo çizimleri: sensör ışınları (kırmızı/yeşil), ajan yolu (camgöbeği)

### Bölüm 4: Muhafızlar · Sahip: fuzienn
- [x] `GuardSpawner`: 6 adaydan 3 muhafız, seed'li (`Spawn(seed)`, 0=rastgele), NavMesh'e oturtma, min. 2.5 m aralık
- [x] `Guard`: FSM (LookAround ⇄ Patrol → Neutralized), anahtar (`KeyRing`), `GuardVision` (4 m, 90°, duvarda kırpılan görünür koni)
- [x] Etkisiz hale getirme: arkadan (≥110°, ≤1.1 m) `E`. Kaçış aşamasında 0.6 sn görülürse `GameLost(Caught)` → `R` ile yeniden başlat
- [x] `Warden`: kapı önünde sabit, tüm muhafızlar etkisizse `TalkRequested` yayınlar (LLM bağlantısı Bölüm 5)

### Bölüm 5: LLM NPC (Gardiyan) · Sahip: dikmenomer33
- [x] `ILLMClient` + `OllamaClient` (`/api/tags` sağlık, `/api/chat`, UnityWebRequest + await, iptal); eski `Assets/Scripts/AI/LLM` kaldırıldı
- [x] JSON şeması `format` parametresinde; `GuardDecision` (reply, fear, mood, action)
- [x] `DecisionParser`: `{...}` ayıklama, kod bloğu temizleme, fear sınırlama, enum eşleme; bozuksa 1 kez model onarımı (`DialogueSession`)
- [x] Hata yönetimi: sağlık kontrolü, ServerUnreachable/ModelMissing/Timeout/EmptyResponse/Cancelled, kullanıcıya mesaj, oyun durumu değişmez. `insecureHttpOption = AlwaysAllowed` (ekip arkadaşının makinesindeki Ollama için)
- [x] Sistem promptu `WardenPrompt` (Bertan, korkak yaşlı gardiyan; her turda güncel korku)
- [x] `FearMeter` (tur başı +35/-15, eşik 70) + `DialogueRules` (OPEN_DOOR eşik altıysa ret, ATTACK korku<40 ise geçerli)
- [x] Diyalog UI (`DialogueView`/`IDialogueView`): giriş, geçmiş, "Gardiyan düşünüyor...", korku çubuğu, hata satırı, Esc
- [x] Oyun durumu: OPEN_DOOR → çıkış kapısı + gardiyan kenara çekilir → `ExitZone` → kazanma; ATTACK → `GameLost(WardenAttacked)`

### Bölüm 6: Oyun Akışı ve Cila · Sahip: dikmenomer33 (commit dengesi için fuzienn'den alındı)
- [x] Tüm kullanıcıya görünen metinler Türkçe karakterli (istemler, fare durumu, hata mesajları, kural notları)
- [x] `Dungeon Escape/Rebuild Scene` menüsü (`SceneRebuilder`): Level → Gameplay → Navigation+Rat → Guards → Dialogue → HUD
- [x] Başlık ekranı (hikâye + kontroller, Enter), Esc duraklatma (R/Q), kazanma/kaybetme ekranları (`GameHud`)
- [x] Görev metni (`ObjectiveText`, aşamaya göre), fare durumu, anahtar göstergesi
- [ ] (İsteğe bağlı) Dekor: fıçı, sandık, kafes (`Objects.png`) — harita değişirse NavMesh/test konumları etkilenir
- [ ] (İsteğe bağlı) Ses efektleri. Kapı/mangal animasyonları zaten var
- [x] Tam akış otomatik test ile baştan sona oynandı (`FullPlaythroughTest`, gerçek fare + gerçek LLM, 21.6 sn'de kazanıldı). Elle bir oynanış yine de önerilir

### Bölüm 7: Testler · Sahip: ortak (ölçüm aracı + fare ölçümü fuzienn, diyalog senaryoları dikmenomer33)
- [x] EditMode: `TargetSelectorTests` (yol uzunluğu, anahtarsız/etkisiz/ulaşılamaz muhafız) 5/5
- [x] PlayMode: `RatMissionTests` (en yakından çalma+teslim, saklanma, muhafız yok, anahtarsızları atlama, çift komut) 5/5
- [x] EditMode: `DecisionParserTests` (15), `DialogueRulesTests` (7), `DialogueSessionTests` (onarım, hata türleri, tur sınırı) → EditMode toplam 38/38
- [x] PlayMode: `OllamaIntegrationTests` (yanlış port, yönlendirilemeyen host/timeout, iptal, olmayan model, gerçek model) + `DialogueFlowTests` (kazanma, saldırı, sunucu kapalı) → PlayMode toplam 26/26
- [x] EditMode: `ObjectiveTextTests`, `ManipulationGuardTests` → EditMode toplam 52/52
- [x] Ölçüm testleri ayrı derleme biriminde (`DungeonEscape.Tests.Benchmarks`, `[Explicit]`); `run_tests` ile adıyla çalıştır
- [x] PlayMode: `GuardTests` (aynı seed aynı yerleşim, 10 seed'de ulaşılabilirlik, arkadan/önden etkisizleştirme, yakalanma, hücrede yakalanmama, gardiyan koşulu) 7/7
- [x] PlayMode: `PlayerEscapeTests` (duvar, tünel, kilitli kapı, anahtarla açma, hücreden çıkış) 6/6 geçti
- [x] EditMode: `DecisionParser` (geçerli, bozuk, eksik alan, alan dışı değer, boş)
- [x] EditMode: `FearMeter` ve kapı kuralı
- [x] Fare ölçümü (`Tests/Benchmarks/RatBenchmark`, 50 seed, rastgele başlangıç): **50/50**, ort. 5.4 sn, medyan 5.1, en uzun 12.4; 14 koşuda saklanma → `Docs/test_results/rat_runs.csv`, `rat_summary.md`
- [x] Diyalog senaryoları (`DialogueScenarioRunner`, 7 senaryo + korkutma ×5) → `Docs/test_results/dialogues.md`. Son koşu: korkutma 5/5 (4 tur), hakaret → saldırı, enjeksiyon engellendi, rüşvet/alakasız/anlamsız kapı açmadı; sınırlılık: kibar rica 3 turda açtı
- [x] Sunucu kapalıyken ekran görüntüsü (`Docs/screenshots/llm_sunucu_kapali.png`), gerçek model diyaloğu (`llm_diyalog.png`)

### Bölüm 8: Build ve Rapor · Ortak (giriş/mimari/fare/muhafız/test/sonuç: fuzienn, LLM/hata yönetimi: dikmenomer33)
- [x] macOS build (`YazLabUnity/Builds/macOS/ZindandanKacis.app`, 75 MB, başsız çalıştırma ile doğrulandı). Windows: Unity Hub'da "Windows Build Support (Mono)" modülü kurulu değil
- [x] README: giriş, senaryo, kurulum, mimari (sınıf diyagramı, SOLID tablosu)
- [x] Mermaid: fare FSM durum diyagramı + yaklaşma karar akışı + GameFlow durum diyagramı
- [x] Mermaid: LLM sequence diyagramı, sistem promptu, JSON şeması, koruma katmanları
- [x] Hata yönetimi stratejisi bölümü (tablo)
- [x] Ekran görüntüleri, test sonuçları, örnek diyaloglar
- [x] Sonuç ve tartışma; model seçim gerekçesi
- [ ] Teslim zip'i: kod + build + rapor (`YazLabProje_Teslim.zip`, proje kökünde, gitignore'da). Son sürüm teslimden hemen önce yeniden üretilmeli

### Bölüm 9 (Bonus +10): ML-Agents · Sahip: dikmenomer33
- [x] ML-Agents 4.1.0 paketi; Python 3.10 venv (`ML/.venv`, mlagents 1.1.0, torch 2.2.2 sabit, grpcio override)
- [x] Eğitim ortamı: `RatTraining` sahnesi, 12 arena, `RatStealthAgent` (RayPerceptionSensor2D + 9 vektör, sürekli 2B eylem), `TrainingGuard`
- [x] Ödül fonksiyonu (+1/-1, görülme/zaman/duvar cezası, potansiyel tabanlı yönlendirme + terminal sıfırlama)
- [x] Eğitim: 3 deneme (v1 zayıf yönlendirme durduruldu, v2 ödül hackleme, v3 başarılı), 3M adım, başarı %0 → %64
- [x] Grafikler `Docs/ml/` (`ML/plot_training.py`), model `Assets/_Project/ML/Models/RatStealth.onnx`
- [x] Değerlendirme (600 bölüm): eğitilmiş %64 başarı / %1 zaman aşımı, taban %57 / %17 → `Docs/test_results/rl_evaluation.md`
- [x] README bölüm 10
- [ ] (İsteğe bağlı) Politikayı ana oyundaki fareye entegre etmek

## Oturum Günlüğü

| Tarih | Yapılan | Sıradaki |
|---|---|---|
| 2026-10-28 | Ödev analizi, tasarım ve plan yazıldı. Unity MCP bağlı. Uzak repo incelendi: Library yanlışlıkla commit'lenmiş, `feature/ai-integration`'da ilk Ollama istemcisi var | Bölüm 0 |
| 2026-10-28 | Geçmiş temizlendi (420 MB → 77 KB), feature dalı main'e ff edildi, gitignore düzeltildi, plan push'landı | Bölüm 0: paketler + 2D renderer |
| 2026-10-28 | Paketler (NavMeshPlus 0.2.23, AI Navigation 2.0.16, 2D Sprite/Tilemap), URP 2D Renderer, Main sahnesi, `ArtImportProcessor`, karakter sprite'ları, asmdef'ler (`DungeonEscape.Runtime/Editor`) | Ollama modeli, sonra Bölüm 1 |
| 2026-10-28 | Model karşılaştırması: gemma3:4b seçildi. Bölüm 0 tamam | Bölüm 1 (kapı dilimi, harita) |
| 2026-10-28 | Bölüm 1 tamam: ASCII harita ayrıştırıcı (`LevelLayout`), `LevelBuilder` editör aracı, Main sahnesinde Level 01 | Bölüm 2 (oyuncu) |
| 2026-10-28 | Bölüm 2 tamam. Hata bulundu ve düzeltildi: aynı karede boyanan Tilemap'lerin collider geometrisi üretilmiyordu (`RebuildColliders`). Not: editör odaksızken Play modu ilerlemiyor; testler `run_tests` ile, elle deneme `EditorApplication.Step()` ile | Bölüm 3 (fare) |
| 2026-10-28 | Bölüm 3 tamam. Düzeltilen hatalar: Awake'te pişirilen NavMesh eklenmiyordu (Start'a alındı); ajan baseOffset'i z'ye kayıp CalculatePath'i bozuyordu; StateMachine iç içe geçişleri sıraya alıyor | Bölüm 4 (muhafızlar) |
| 2026-10-28 | Bölüm 4 tamam. PlayMode 18/18. Gerçek muhafızlarla seed 7: fare 4.3 sn, yol 5.5 m | Bölüm 5 (LLM gardiyan) |
| 2026-10-28 | Bölüm 5 tamam. Gerçek model denemesi: 4 turda korku 72 → OPEN_DOOR; 3. turda ATTACK korku kuralıyla reddedildi; yanıt 2-3 sn. Unity 6 düz HTTP engeli bulundu ve açıldı | Bölüm 6 (UI/cila) |
| 2026-10-29 | Bölüm 6 tamam (Ömer adına): Türkçe metinler, HUD, başlık/duraklatma/sonuç ekranları, Rebuild Scene. EditMode 41/41, PlayMode 26/26 | Bölüm 7 (ölçümler) |
| 2026-10-29 | Bölüm 7 tamam (Hüseyin). Senaryo koşucusu prompt enjeksiyonu açığını buldu → `ManipulationGuard`. Hakaret/tehdit ayrımı prompt'a eklendi, saldırı sınırı 40→30 | Bölüm 8 (build + README) |
| 2026-10-29 | Build + README tamam. Kalan: elle tam oynanış, teslim zip'i, (isteğe bağlı) Windows build ve ML-Agents bonusu | Teslim hazırlığı |
| 2026-10-29 | Gece: ML-Agents bonusu (Ömer) ve uçtan uca oynanış testi (Hüseyin). Notlar: eğitim build'i alınırken MCP build işi domain reload'da kayboluyor (build yine tamamlanıyor); çıkarım cihazı `Burst` olmalı (ComputeShader tensörleri okunamıyor); Mac uykuya geçmesin diye eğitim `caffeinate` ile | Son kontroller, teslim zip'i |
