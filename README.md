# Zindandan Kaçış

**Kocaeli Üniversitesi · Bilişim Sistemleri Mühendisliği · Yazılım Geliştirme Laboratuvarı-I · Proje 1**

| | |
|---|---|
| Ekip | Hüseyin Melih Güzel · Ömer Alp Dikmen |
| Tarih | Kasım 2026 |
| Motor | Unity 6000.6.6f1 (URP 2D), C# |
| Yapay zekâ | NavMesh (NavMeshPlus) otonom yoldaş · Ollama üzerinde yerel LLM (`gemma3:4b`) |

Kuşbakışı 2D bir zindandan kaçış oyunu. Oyuncu, hücresinden yalnızca küçük bir farenin geçebileceği bir delikten yoldaşı fareyi gönderir. Fare NavMesh ile yol bulup en yakın muhafızın anahtarını çalar ve geri getirir. Oyuncu hücreden çıkar, muhafızları sessizce etkisiz hâle getirir ve çıkış kapısındaki son gardiyanı, yerel bir dil modeliyle serbest metinle konuşarak, kapıyı açacak kadar korkutur.

![Başlık ekranı](Docs/screenshots/baslik_ekrani.png)

## İçindekiler

1. [Giriş](#1-giriş)
2. [Kurulum ve Çalıştırma](#2-kurulum-ve-çalıştırma)
3. [Mimari](#3-mimari)
4. [Otonom AI Kahraman: Fare](#4-otonom-ai-kahraman-fare)
5. [Muhafızlar ve Oyun Akışı](#5-muhafızlar-ve-oyun-akışı)
6. [LLM Entegrasyonu: Gardiyan Bertan](#6-llm-entegrasyonu-gardiyan-bertan)
7. [Hata Yönetimi](#7-hata-yönetimi)
8. [Testler ve Sonuçlar](#8-testler-ve-sonuçlar)
9. [Sonuç ve Tartışma](#9-sonuç-ve-tartışma)

## 1. Giriş

### 1.1. Problem ve amaç

Projenin amacı, tek bir Unity oyununda birbirinden bağımsız iki yapay zekâ bileşenini bütünleştirmektir:

- **Otonom yoldaş:** Oyuncunun kendi başına aşamadığı fiziksel bir engeli, NavMesh yol bulma ve ışın (raycast) tabanlı algılama ile aşan bir karakter.
- **Yerel LLM destekli NPC:** Oyuncunun serbest metinle konuştuğu, yanıtlarını yerelde çalışan bir dil modeliyle üreten ve kararını **yapılandırılmış JSON** olarak bildiren bir karakter.

Her iki bileşen de sahte/hardcoded davranış içermez: farenin her adımı NavMesh'ten, gardiyanın her cümlesi Ollama'dan gelir. Oyun, sunucu kapalı olsa ya da model bozuk çıktı verse bile çökmez.

### 1.2. Oyun senaryosu

```mermaid
flowchart LR
    A([Hücrede başla]) --> B[F: fareye emir ver]
    B --> C{{Fare: en yakın muhafızı seç,<br/>tünelden geç, anahtarı çal}}
    C --> D[Anahtar oyuncuda]
    D --> E[E: hücre kapısını aç]
    E --> F[Muhafızları arkadan<br/>etkisiz hâle getir]
    F -->|görülürse| X([Yakalandın])
    F --> G[Gardiyanla serbest metinle konuş]
    G -->|korku ≥ 70 ve OPEN_DOOR| H[Kapı açılır]
    G -->|öfkeli ve ATTACK| Y([Gardiyan saldırdı])
    H --> I([Özgürsün!])
```

| Ödev isteri | Oyundaki karşılığı |
|---|---|
| Oyuncunun aşamadığı fiziksel engel | Hücre; duvardaki tünel yalnızca farenin NavMesh alanına açık |
| Komutla çalışan otonom companion | `F` tuşu → `RatAgent.Command()` |
| NavMesh yol bulma | NavMeshPlus 2D yüzeyleri, iki ajan tipi (Rat, Guard) |
| Sensör tabanlı gözlem (Raycast) | `RaySensor2D`: 16 ışınlık demet, engel/muhafız mesafe ve açısı |
| Açıkça tasarlanmış karar mantığı | Farenin 7 durumlu sonlu durum makinesi |
| Kilitli kapıyı koruyan, serbest metinle konuşulan NPC | Gardiyan Bertan |
| Yerel model (Ollama) | `gemma3:4b`, `http://localhost:11434` |
| Yapılandırılmış çıktı ve sınıfla ayrıştırma | JSON şeması → `DecisionParser` → `GuardDecision` |
| Bozuk çıktı, sunucu kapalı, zaman aşımı | Hata türleri, bir kez onarım, kullanıcıya mesaj; oyun durumu değişmez |
| Bloklamayan async, "Düşünüyor..." | `async/await` + `UnityWebRequest`, animasyonlu gösterge |

## 2. Kurulum ve Çalıştırma

### 2.1. Gereksinimler

- Unity **6000.6.6f1** (projeyi açınca paketler otomatik kurulur: NavMeshPlus, AI Navigation, Input System, URP, Newtonsoft JSON)
- [Ollama](https://ollama.com/) ve model:

```bash
ollama pull gemma3:4b
ollama serve        # masaüstü uygulaması açıksa zaten çalışıyordur
```

Ollama başka bir makinede çalışıyorsa `Assets/_Project/Settings/OllamaSettings.asset` içindeki `baseUrl` alanı `http://<ip>:11434` yapılır (projede düz HTTP'ye izin verildi).

### 2.2. Çalıştırma

- **Build:** `Builds/macOS/ZindandanKacis.app` (teslim arşivinde).
- **Editör:** `Assets/_Project/Scenes/Main.unity` açılıp Play'e basılır.
- Sahne tamamen koddan üretilir: `Dungeon Escape → Rebuild Scene` menüsü haritayı, oyuncuyu, NavMesh'i, muhafızları, diyalog arayüzünü ve HUD'u sırayla yeniden kurar.

### 2.3. Kontroller

| Tuş | Eylem |
|---|---|
| WASD / Oklar | Yürü |
| F | Fareye emir ver (anahtarı getir) |
| E | Etkileşim: kapı, muhafızı etkisiz hâle getirme, gardiyanla konuşma |
| Enter | Diyalogda mesaj gönder |
| Esc | Duraklat / konuşmayı bitir |
| R | Oyun bitince yeniden başla |

## 3. Mimari

### 3.1. Klasör ve derleme birimleri

```
YazLabUnity/Assets/_Project/
├── Scripts/                     DungeonEscape.Runtime (asmdef)
│   ├── Core/                    StateMachine, IState, GameEvents, GameFlow
│   ├── Level/                   LevelLayout (ASCII harita), SpawnMarker, ExitZone
│   ├── Characters/              CharacterAnimator, CharacterSpriteSet
│   ├── Player/                  PlayerController, IPlayerInput, CameraFollow
│   ├── Interaction/             IInteractable, Interactor, IKeyHolder, KeyRing, LockedDoor
│   ├── Navigation/              INavigator, NavMeshNavigator, NavMeshRuntimeBaker
│   ├── Companion/               RatAgent, RaySensor2D, ITargetSelector, States/
│   ├── Guards/                  IGuard, Guard, GuardVision, GuardSpawner, Warden, States/
│   ├── Dialogue/                ILLMClient, OllamaClient, DecisionParser, DialogueSession, ...
│   ├── UI/                      GameHud, ObjectiveText
│   └── Editor/                  DungeonEscape.Editor: sahne kurulum araçları
├── Tests/
│   ├── EditMode/                saf mantık testleri (52)
│   ├── PlayMode/                sahne içi testler (26)
│   └── Benchmarks/              uzun ölçümler: fare ×50, diyalog senaryoları
├── Tiles/level01.txt            ASCII harita (tek doğru kaynak)
└── Art/                         tileset, karakterler (Tools/generate_characters.py ile üretildi)
```

Çalışma zamanı kodu 64 dosya ve yaklaşık 3.700 satırdır. Ayrıca yaklaşık 1.100 satır editör aracı ve 1.570 satır test kodu vardır.

### 3.2. Sınıf diyagramı

```mermaid
classDiagram
    direction LR
    class IState { <<interface>> +Enter() +Tick(dt) +Exit() }
    class StateMachine { +Current +ChangeState(IState) +Tick(dt) }
    StateMachine o-- IState

    class INavigator { <<interface>> +SetDestination() +TryGetPathLength() +TrySnap() }
    class NavMeshNavigator
    INavigator <|.. NavMeshNavigator

    class ICommandable { <<interface>> +Command() }
    class RatAgent
    class RaySensor2D { +Sense() +Detects() +HasLineOfSight() }
    class ITargetSelector { <<interface>> +Select(guards, keyId) }
    class NearestByPathSelector
    ICommandable <|.. RatAgent
    ITargetSelector <|.. NearestByPathSelector
    RatAgent --> INavigator
    RatAgent --> RaySensor2D
    RatAgent --> ITargetSelector
    RatAgent --> StateMachine
    NearestByPathSelector --> INavigator

    class IGuard { <<interface>> +Keys +CanSee(point) +IsActive }
    class IInteractable { <<interface>> +CanInteract() +Interact() }
    class Guard
    class Warden
    IGuard <|.. Guard
    IInteractable <|.. Guard
    IInteractable <|.. Warden
    IInteractable <|.. LockedDoor
    RatAgent ..> IGuard : hedef

    class ILLMClient { <<interface>> +CheckHealthAsync() +ChatAsync() }
    class OllamaClient
    class IDecisionParser { <<interface>> +Parse(raw) }
    class DecisionParser
    class DialogueSession { +SendAsync(text) }
    class FearMeter
    class DialogueController
    class IDialogueView { <<interface>> }
    ILLMClient <|.. OllamaClient
    IDecisionParser <|.. DecisionParser
    DialogueSession --> ILLMClient
    DialogueSession --> IDecisionParser
    DialogueSession --> FearMeter
    DialogueController --> DialogueSession
    DialogueController --> IDialogueView
    DialogueController --> Warden
```

### 3.3. OOP ve SOLID uygulaması

| İlke | Projedeki karşılığı |
|---|---|
| Tek sorumluluk | `RaySensor2D` yalnızca algılar, `NearestByPathSelector` yalnızca seçer, `DecisionParser` yalnızca ayrıştırır, `DialogueView` yalnızca gösterir |
| Açık/kapalı | Yeni bir fare veya muhafız davranışı, mevcut durumlara dokunmadan yeni bir `IState` sınıfıyla eklenir; hedef seçme stratejisi `ITargetSelector` ile değişir |
| Liskov | Testlerde `TestGuard`, `FakePlayerInput`, `ScriptedClient` gerçek sınıfların yerine geçer ve oyun aynı çalışır |
| Arayüz ayrımı | `IKeyHolder`, `IInteractable`, `IGuard`, `ICommandable` küçük ve amaca özel arayüzlerdir |
| Bağımlılığın tersine çevrilmesi | `DialogueSession` somut Ollama'yı değil `ILLMClient`'ı, fare somut NavMeshAgent'ı değil `INavigator`'ı bilir |
| Gevşek bağlılık | Bileşenler birbirini değil `GameEvents` olay yolunu dinler (kapı açıldı, anahtar teslim edildi, oyun bitti) |
