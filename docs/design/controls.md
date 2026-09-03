# Ovládání

## Filozofie `[ZADÁNÍ]`

Vysílačka je **primární** ovládání, klávesnice je fallback pro testování. Ne naopak. Když se návrh
ovládání rozhodne mezi „dobře na klávesnici" a „dobře na vysílačce", vyhrává vysílačka.

## Mapování os — Mode 2 `[ZADÁNÍ]`

| Osa | Význam | Klávesnice | Vysílačka |
|---|---|---|---|
| **Throttle** | tah podél lokálního `up` | `LeftShift` / `LeftCtrl` | levý stick vertikálně (`z`) |
| **Yaw** | rotace kolem svislé osy | `Q` / `E` | levý stick horizontálně (`rx`) |
| **Pitch** | naklopení nosem dolů/nahoru | `W` / `S` | pravý stick vertikálně (`stick/y`) |
| **Roll** | naklopení do strany (bank) | `A` / `D` | pravý stick horizontálně (`stick/x`) |
| **Fire** | výstřel rakety | `Space` | `trigger` |

Zásadní upozornění k roll: **`A`/`D` je skutečná rotace (naklápění), NE strafe do stran.** Je to
pořád jen náklon vektoru tahu — pohyb do strany z toho vzniká až důsledkem, viz
[flight-model.md](flight-model.md).

Pitch i roll jsou **invertované** proti výchozímu chování zařízení — čistá uživatelská preference
(Viktor chce opačný směr než default). V kódu jsou proto obě čtení negovaná.

Hardwarové cesty ke kontrolům (`"z"`, `"rx"`, `"stick/y"`, ...) nejsou libovolné a nedají se
odhadnout — viz [reference/radiomaster-pocket.md](../reference/radiomaster-pocket.md).

## Past při úpravách `[OVĚŘENO 03.09.2026]`

Logika hledání vysílačky je **duplikovaná** ve `DroneControls.Start()` a `Shoting.Start()` —
každý má svou kopii. Kdo mění mapování nebo detekci zařízení, musí to změnit **v obou**.

Zároveň: chybějící control vrací `0f` **mlčky**. Špatná cesta k ose se tedy neprojeví chybou, ale
tím, že osa „nic nedělá". Při debugování ovládání je to první věc, kterou zkontrolovat.

## Otevřené otázky `[OTEVŘENÉ]`

1. **Sjednotit detekci zařízení?** Duplikace ve dvou skriptech je zjevný kandidát na jedno místo
   (jeden `TransmitterInput` zdroj). Chceme to teď, nebo až bude víc skriptů, co vysílačku potřebují?
2. **Použít `InputSystem_Actions.inputactions`?** Asset ve projektu existuje, ale kód ho vůbec
   nepoužívá — čte zařízení přímo. Přechod na actions by dal rebinding a víc zařízení zdarma,
   ale u exotických HID cest vysílačky je přímé čtení spolehlivější. Zatím záměrně přímo.
3. **Rebinding za běhu?** Každá vysílačka má jiné cesty k osám. Teď to znamená přepsat kód.
4. **Arm/disarm?** Reálný dron se musí armovat. Přidat, nebo je to zbytečná ceremonie?
5. **Chybí ovládání kamery** — pokud bude FPV, sedí kamera nastálo na dronu? Pokud third-person,
   čím se hýbe?
