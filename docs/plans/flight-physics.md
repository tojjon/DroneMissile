# Realistická letová fyzika

**Stav:** plánováno — plán implementace hotový, **čeká na schválení.** Zadání 04.10.2026.

## Zadání `[ZADÁNÍ]`

„Opravit fyziku": najít, jakou fyziku používají ostatní FPV simulátory, a použít ji. Dohodnuto
s Viktorem 04.10.2026:

- **Předloha: 5" freestyle dron na 6S** — ~650 g s baterkou, poměr tahu k váze ~8 : 1, max. ~150 km/h.
- **Jádro:** skutečná hmotnost a tah, rotace poháněná motory **se setrvačností**, rates přesně jako
  Betaflight, realistický odpor vzduchu.
- **Navíc:** roztáčení motorů + pokles tahu s rychlostí, **prop wash**, **ground effect**.
  (Vybíjení baterky ne.)
- **Rates:** Betaflight i Actual rates podle zdrojáku Betaflightu. Viktor dodá hodnoty ze své
  skutečné vysílačky; do té doby výchozí freestyle Actual 180 / 950 / 0,70.
- **Stránka Rates v menu**, aby šly měnit bez Unity.

## Dnešní stav `[OVĚŘENO 04.10.2026]`

Z pohledu skutečného dronu je dnešní model hodně zjednodušený (`DroneControls.FixedUpdate`):

| Co | Dnes | Problém |
|---|---|---|
| hmotnost | 0,0075 kg | reálný 5" má ~0,65 kg |
| tah | `throttleForce` 0,4 N, lineárně s plynem, okamžitě | žádné motory, žádné roztáčení, tah nezávisí na rychlosti |
| rotace | `MoveRotation` — stick = přímo úhel za krok | **žádná setrvačnost**, žádný regulátor; stick pustíš a rotace okamžitě stojí |
| rates | 100 °/s lineárně na všech osách | reálné rates jsou 600–1000 °/s s křivkou |
| odpor | `linearDamping` 0,3 (lineární, všude stejný) | skutečný odpor roste s druhou mocninou rychlosti a závisí na natočení; teoretická max. rychlost ~140 m/s |
| prop wash, ground effect | nic | — |

## Co dělají ostatní simulátory (rešerše)

- **Liftoff** simuluje každou část zvlášť: baterii, motory, vrtule, rám a letový kontrolér. Tah
  z laboratorních dat: max. otáčky motoru → **statický tah při otáčkách → dynamický tah** (klesá
  s rychlostí), upravený **ground effectem**. Odpor podle **plochy dronu promítnuté do směru
  rychlosti** (předpočítáno pro 42 směrů). Letový kontrolér s PID a rates Betaflight / Actual /
  RaceFlight / KISS. Vítr, vztlak, asymetrické zatížení vrtulí.
  ([Liftoff simulations](https://www.liftoff-game.com/liftoff-simulations))
- **Velocidrone** je chválený za **prop wash**: po stažení plynu do pádu a prudkém přidání se dron
  rozkmitá jako skutečný 5" na výchozích PID. Liftoff má prop wash mírnější.
  ([srovnání simulátorů](https://blog.uavmodel.com/fpv-simulator-training-guide-velocidrone-vs-liftoff-vs-tryp-for-race-and-freestyle-skills-2026/))
- **Výzkumné simulátory** (Agilicious, Flightmare) berou tah jako **kvadratickou funkci otáček**
  a motor jako **systém prvního řádu s časovou konstantou** (nedosáhne otáček okamžitě).
  ([Agilicious](https://arxiv.org/pdf/2307.06100))
- **Pokles tahu s rychlostí:** dynamický tah klesá s rychlostí proudu do vrtule — v jednom měření
  dával plný plyn při 15 m/s jen 44 % statického tahu.
  ([QuadPlane wind tunnel](https://arxiv.org/pdf/2301.12316))
- **Rotor drag** je hlavní aerodynamický jev při rychlém letu a je **lineární v rychlosti**.
  ([Faessler, Franchi, Scaramuzza](https://arxiv.org/abs/1712.02402v2))
- **Vortex ring state / prop wash:** při klesání do vlastního proudu vzduchu tvoří vrtule víry,
  tah klesá a dron vibruje.
- **Rates:** přesné vzorce jsou v Betaflightu `src/main/fc/rc.c`
  ([zdroj](https://raw.githubusercontent.com/betaflight/betaflight/master/src/main/fc/rc.c)),
  výchozí a typické hodnoty u [Oscara Lianga](https://oscarliang.com/rates/).
- **Typický 5":** 250–300 g bez baterky, tah 1,5–2 kg na motor, poměr 6–8 : 1, ~150 km/h na 6S.
  ([specifikace](https://unanswered.io/guide/5-inch-fpv-drone-specs),
  [rychlosti](https://www.unmannedtechshop.co.uk/fr/blogs/knowledge-base/fpv-drone-top-speed-how-fast-real-world-by-build-class))
- **Open source k nahlédnutí:** [Rotorcross](https://rotorcross.itch.io/rotorcross) (fyzika na
  285 Hz, PID z Cleanflightu), [RC-Flight-Sim](https://ithub.global.ssl.fastly.net/DaScient/RC-Flight-Sim)
  (5" model s mixerem a PID).

## Plán implementace

### Model (co se bude počítat)

Stejná stavba jako Liftoff, jen bez baterie. **Pilot neřídí rotaci přímo — řídí letový kontrolér,
který roztáčí motory.** Rotace tak vzniká stejně jako u skutečného dronu.

1. **Stick → požadovaná úhlová rychlost** přes rates (vzorce 1 : 1 z Betaflightu,
   `applyBetaflightRates` a `applyActualRates`), zvlášť pro roll, pitch, yaw.
2. **Rate PID** (jako Betaflight v acro): rozdíl mezi požadovanou a skutečnou úhlovou rychlostí →
   požadovaný moment na každé ose.
3. **Mixer (quad X) s airmode:** moment + plyn → příkaz pro 4 motory. Airmode drží kontrolu
   i na nulovém plynu, idle 5 %. Při plném plynu nezbývá rezerva — dron je méně obratný (emerguje
   samo, jako ve skutečnosti).
4. **Motory:** otáčky jdou za příkazem se zpožděním (časová konstanta ~30 ms nahoru, ~40 ms dolů).
5. **Tah vrtule** = max. tah × (otáčky)² × **pokles s rychlostí** (podle složky rychlosti proti
   ose vrtule a „pitch speed" vrtule) × **ground effect** (víc tahu nízko nad zemí,
   Cheeseman–Bennett). Každý motor tlačí **podél lokálního nahoru** v místě svého ramene —
   [rozhodnutí #2](../decisions.md) platí dál.
6. **Momenty z rozdílu tahů** (roll, pitch) a z odporu vrtulí (yaw) → úhlové zrychlení přes
   **skutečný moment setrvačnosti** → rotace má setrvačnost.
7. **Odpor vzduchu:** kvadratický, s plochou promítnutou do směru letu (krabicová aproximace:
   přední / boční / horní plocha) + malý lineární **rotor drag**.
8. **Prop wash:** když dron klesá do vlastního proudu (rychlost proti ose vrtule v pásmu kolem
   indukované rychlosti), dostanou motory šum v tahu a pokles účinnosti — PID s tím bojuje a dron
   se rozkmitá, nejvíc při „punch-outu" z pádu.
9. **Stun:** motory na nulu, PID vypnutý — dron **padá a dál se točí se setrvačností** (dnes drží
   náklon). Mění to detail [rozhodnutí #10](../decisions.md) → nové rozhodnutí.

**Krok simulace:** Unity fyzika běží na 50 Hz — na PID a 30 ms motory moc hrubé. Letový model si
proto uvnitř každého kroku udělá **8 vlastních podkroků (400 Hz)**: motory, PID a úhlovou rychlost
integruje sám, výsledek předá Rigidbody (úhlová rychlost + průměrná síla). **Globální krok fyziky
se nemění**, takže ladění nepřátelských projektilů ([rozhodnutí #14](../decisions.md), šedý
prolétává) zůstane, jak je.

### Výchozí hodnoty (5" freestyle 6S)

Ty se zdrojem jsou z rešerše; ty označené `[HYPOTÉZA]` jsou odhady k doladění hraním.

| Parametr | Hodnota | Původ |
|---|---|---|
| hmotnost | 0,65 kg | 250–300 g rám + 6S baterie |
| max. tah na motor | 12,75 N (≈ 1,3 kg) → poměr 8 : 1 | rešerše: 6–8 : 1 |
| rameno (střed → motor) | 0,113 m | `[HYPOTÉZA]` 5" rám ~225 mm diagonála |
| moment setrvačnosti Ixx / Iyy / Izz | 0,0025 / 0,0025 / 0,0045 kg·m² | `[HYPOTÉZA]` |
| časová konstanta motoru | 30 ms nahoru / 40 ms dolů | `[HYPOTÉZA]`, model prvního řádu |
| idle | 5 % | Betaflight |
| poloměr vrtule | 0,0635 m (5") | — |
| pitch speed vrtule při max. otáčkách | ~55 m/s | `[HYPOTÉZA]` 4,3" stoupání × ~30 000 ot/min |
| odporové plochy (Cd·A) přední / boční / horní | ladit tak, aby **max. rychlost v rovině ≈ 42 m/s (150 km/h)** | rešerše |
| rates | Actual 180 / 950 / 0,70 (všechny osy, yaw možná nižší) | Oscar Liang freestyle |
| PID | ladit na odezvu ~40–60 ms bez překmitu na výchozích rates | `[HYPOTÉZA]` |

Všechno žije v jednom **profilu dronu** (asset), takže jde mít později víc dronů (racer, whoop).

### Soubory

- **Nové:**
  - `DronePhysics.cs` — celý letový model výš (rates → PID → mixer → motory → síly), podkroky.
  - `DroneProfile.cs` — `ScriptableObject` s parametry; asset `Assets/Settings/DroneProfile_5inch.asset`.
  - `RateCurves.cs` — statické vzorce Betaflight / Actual (bez Unity závislostí, testovatelné).
  - `RateSettings.cs` — uživatelské rates uložené v `PlayerPrefs`, přebíjí profil.
  - `Assets/Editor/DronePhysicsBuilder.cs` — vytvoří profil, nasadí `DronePhysics` na dron
    v `SampleScene`, `Sandbox` a `Arena`, nastaví Rigidbody (hmotnost, setrvačnost, damping 0).
- **Úpravy:**
  - `DroneControls.cs` — zůstává vstup, pád, stun a arming; let předá `DronePhysics`. Klávesnice
    dostane **plynulý náběh** sticku (s 950 °/s by digitální klávesa byla neovladatelná).
  - `MainMenu.cs` + `UiKit.cs` — stránka **Rates**: výběr systému (Betaflight / Actual), tři hodnoty
    na osu (tlačítka − / +), náhled „max °/s"; uloží se okamžitě.
  - `MainMenuBuilder.cs` — v menu scéně odstranit i `DronePhysics`.
  - Na ESC menu ([esc-menu.md](esc-menu.md)) se později přidá odkaz na tutéž stránku.

### Pořadí

1. **Jádro:** profil, rates, PID, mixer, motory, tah, odpor, setrvačnost, stun → první let.
2. **Doladění** na 5" pocit: hover kolem 25–35 % plynu, max. rychlost ~42 m/s, odezva.
3. **Prop wash + ground effect.**
4. **Stránka Rates** v menu.

### Ověření

1. Headless kompilace.
2. **Testy vzorců:** `RateCurves` proti číslům z Betaflight konfigurátoru (např. Actual
   180 / 950 / 0,70 → 950 °/s na plném sticku; Betaflight výchozí 1,0 / 0,7 / 0 → 667 °/s).
   EditMode test v `Assets/Editor/Tests` — pokud ho runner nenajde (projekt nemá asmdef), náhradou
   headless `-executeMethod`, který výsledky vypíše do logu.
3. **Kontrola rovnováhy** stejnými vzorci: plyn pro hover, ustálená max. rychlost.
4. **Viktor hraním** (je pilot — tohle je hlavní test): hover a přesnost, flipy a rolly na plných
   rates, setrvačnost po puštění sticku, pokles tahu ve vysoké rychlosti, prop wash při punch-outu
   z pádu, ground effect nízko nad podlahou arény, stun (pád s rotací), klávesnice ještě ovladatelná.
5. Zbytek hry beze změny: rakety, turrety, vlny, crash, menu.

### Mimo rozsah

Baterie a její vybíjení, vítr, víc profilů dronů, ESC menu (má vlastní plán), dědění rychlosti
dronu raketou.

## Dopady, na které myslet

- **Aréna 300 m** se při 42 m/s přeletí za ~7 s — možná bude potřeba větší (konstanta
  v `ArenaBuilder`).
- **Raketa hráče** letí 120 m/s bez ohledu na rychlost dronu — při 42 m/s dopředu je to znát.
- [Rozhodnutí #1](../decisions.md) a [#2](../decisions.md) platí a model je naplňuje víc než dnes.
- [design/flight-model.md](../design/flight-model.md): otázka 1 (setrvačnost rotace) a 4 (rate
  profil) se tímhle zodpoví; tabulka hodnot v něm je zastaralá (`throttleForce` 15 vs. 0,4 ve scéně).

## Otevřené otázky `[OTEVŘENÉ]`

1. **Viktorovy rates** — Betaflight nebo Actual a jaké hodnoty?
2. ~~**Úhel kamery**~~ **ROZHODNUTO 04.10.2026** — **35°** nahoru ([rozhodnutí #24](../decisions.md)).
3. **Zvětšit arénu**, pokud bude 150 km/h na 300 m málo?
4. **Má raketa dědit rychlost dronu?**
5. **Výdrž baterky / vítr** později jako samostatné plány?

## Souvisí

[features/flight.md](../features/flight.md), [features/controls.md](../features/controls.md),
[design/flight-model.md](../design/flight-model.md), [design/controls.md](../design/controls.md),
rozhodnutí [#1](../decisions.md), [#2](../decisions.md), [#10](../decisions.md),
[#11](../decisions.md), [#14](../decisions.md).
