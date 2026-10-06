# Zvuk motorů

**Stav:** postaveno, **čeká na test hraním** (poslechem).

## Co to má být `[ZADÁNÍ]`

Viktor, 06.10.2026: „could you add the classic fpv sim propeller sounds?" — typický zvuk motorů
z FPV simulátorů (Liftoff, Velocidrone): kvílení, jehož výška jde s otáčkami, mění se s plynem
i při flipech a rollech.

## Co už je připravené

- Ve hře **není žádný zvuk** — žádný `AudioSource`, žádný klip. `AudioListener` je na `Main Camera`
  (potomek dronu) ve všech scénách.
- `DroneControls.FixedUpdate` počítá plyn, pitch, roll a yaw, ale nikam je nevystavuje.
- Efekty se v projektu stavějí v kódu, bez assetů ([rozhodnutí #16](../decisions.md)).

## Plán implementace

### Přístup: syntéza v kódu, žádný zvukový soubor

Stejná filozofie jako u efektů: `DroneMotorSound` generuje zvuk sám v `OnAudioFilterRead`
(audio vlákno) — nic se nenahrává, nic se nepřidává do repa, a výška tónu jde plynule s otáčkami
bez artefaktů z přeladění klipu.

1. **Čtyři motory.** `DroneControls` vystaví vstupy (`ThrottleInput`, `PitchInput`, `RollInput`,
   `YawInput`; při stunu a po pádu nula). Malý **mixer quad X** z nich spočítá příkaz pro každý
   motor (plyn ± pitch ± roll ± yaw), idle 8 %. Při flipu tak dva motory vytočí a dva spadnou —
   to je ten známý „řev" při triku.
2. **Otáčky** jdou za příkazem se zpožděním (nahoru ~40 ms, dolů ~90 ms), počítané po vzorcích
   na audio vlákně — žádné skoky v tónu. Při stunu motory **doběhnou do ticha**.
3. **Tón motoru:** frekvence listu vrtule 120 → 780 Hz podle otáček, 4 harmonické (bzučivé kvílení);
   čtyři motory mírně **rozladěné**, takže se tóny „tlučou" jako skutečné motory.
4. **Šum vrtulí a vzduchu:** filtrovaný šum, hlasitější s otáčkami a s rychlostí letu (vítr).
5. **Pauza** (`timeScale 0` — karty, konzole, konec runu): zvuk se ztlumí.
6. 2D zvuk (`spatialBlend 0`) — je to pohled z dronu, zvuk je „náš".

Všechno laditelné v Inspectoru: hlasitost, rozsah frekvencí, rozladění, poměr šumu, odezva motorů.

### Soubory

- **Nové:** `Assets/scripts/DroneMotorSound.cs`, `Assets/Editor/DroneMotorSoundBuilder.cs`
  (`Tools > DroneMissile > Build Drone Motor Sound` — přidá komponentu dronu v SampleScene, Sandbox
  a Arena; headless `-executeMethod DroneMotorSoundBuilder.BuildAll`).
- **Úpravy:** `DroneControls.cs` (vstupy ke čtení), `MainMenuBuilder.cs` (v menu scéně komponentu
  odstranit — vyžaduje `DroneControls`), dokumentace.

### Ověření

1. Headless kompilace + builder.
2. Viktor poslechem: idle na zemi, kvílení roste s plynem, flip/roll změní tón, stun = doběh do
   ticha, pád = ticho, pauza mezi vlnami = ticho, menu bez zvuku motorů.

### Mimo rozsah

Zvuky raket, výbuchů, turretů; hudba; prop wash jako zvuk (patří k [flight-physics](flight-physics.md)).

## Odchylky při implementaci

- Builder spuštěný headless 06.10.2026 — komponenta je na dronu v SampleScene, Sandbox i Arena.
  Unity při uložení dopsalo do scén i nová pole (upgrady, odpor vzduchu) s výchozími hodnotami.
- Rozhodnutí zapsané jako [#31](../decisions.md).
- **Přepracováno po prvním poslechu** (Viktor, 06.10.2026: „při zatáčení to zní, jako by mě unášeli
  mimozemšťani"; předloha video *The UNCUT RAW Sound of Motors | DRONE FREESTYLE*). Příčina: čisté
  tóny (4 harmonické sinusů), které se při plném sticku rozjely o ±35 % (`mixAmount` 0,35), a
  rozladění 1,8 % = pravidelné „vlnění" ~9× za s. Teď: výška hlavně z plynu, `mixAmount` 0,12;
  stick přidává **zátěž** (`stickLoad` — chrapot a hlasitost); tón je **pilovitý** (PolyBLEP, bez
  aliasingu), k tomu **chop** — šum spínaný každým průchodem listu (`rasp`) — a saturace (`drive`);
  rozladění 0,4 % + náhodný drift (`jitter`) místo pravidelného vlnění.
- Scény si drží staré hodnoty komponenty → nový příkaz `Tools > DroneMissile > Reset Drone Motor
  Sound` (`ResetAll`) komponentu vymění za výchozí.

## Otevřené otázky `[OTEVŘENÉ]`

1. **Barva zvuku** — ladit poslechem (výška, bzučivost, hlasitost šumu).
2. Až přijde [realistická fyzika](flight-physics.md), otáčky půjdou přímo z jejího modelu motorů
   místo z mixeru tady.

## Souvisí

[features/flight.md](../features/flight.md), [decisions.md](../decisions.md) #16.
