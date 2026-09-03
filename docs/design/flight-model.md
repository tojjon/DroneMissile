# Letová fyzika

## Požadavek `[ZADÁNÍ]`

Hráč reálně létá FPV drony (RC pilot), takže **fyzika musí odpovídat skutečnému acro módu**, ne hře
pro casual hráče. Konkrétně:

- **Žádný umělý strop rychlosti.**
- **Žádné tlumení pohybu jako u DJI dronu** — nic, co by dron samo stabilizovalo nebo srovnávalo.
- **Žádný auto-level.** Dron zůstane naklopený, dokud ho hráč nesrovná.

## Jak vzniká pohyb `[ZADÁNÍ]`

Tohle je jádro celého modelu a nejdůležitější věc v tomhle dokumentu:

**Tah (throttle) působí čistě podél `transform.up` dronu** — tedy lokálního nahoru, ne world-space
nahoru. Pitch a roll tím naklápí vektor tahu, a to je **jediný zdroj pohybu vpřed/vzad/do stran**.

Z toho plyne pravidlo, které se nesmí porušit:

> **Žádný samostatný „move force".** Horizontální pohyb je vedlejší efekt náklonu a throttlu.
> Kdokoli přidá do `DroneControls` sílu ve směru `transform.forward`, rozbije tím celý letový model.

Přesně takhle se chová reálný acro FPV dron: chceš letět dopředu, tak se nakloníš dopředu a přidáš
gas. Viz [rozhodnutí #2](../decisions.md).

## Tuning `[OVĚŘENO 03.09.2026]`

Tlumení na Rigidbody dronu je **čistě subjektivní tuning**, aby to nedriftovalo do nekonečna — žádný
fyzikální požadavek za tím není. Stejně tak konkrétní čísla níž; jsou to hodnoty, které se osvědčily,
ne posvátné konstanty.

Hodnoty jsou rozdělené mezi kód a scénu, což je při ladění potřeba vědět:

| Kde | Parametr | Hodnota |
|---|---|---|
| `DroneControls` (Inspector) | `throttleForce` | 15 |
| | `pitchSpeed` / `rollSpeed` / `yawSpeed` | 100 / 100 / 100 |
| | `deadzone` | 0.02 |
| `Drone` Rigidbody (scéna) | `mass` | **0.0075** |
| | `linearDamping` | 0.3 |
| | `angularDamping` | 0.5 |
| | `useGravity` | ano |
| | `collisionDetection` | Discrete |

Pozor na tu **masu 0,0075 kg** (7,5 g) proti `throttleForce` 15. Poměr síly k mase je extrémní, takže
`throttleForce` je vůči hmotnosti velmi citlivý — kdo bude ladit odezvu, musí měnit obě čísla společně,
jinak dron buď stojí, nebo odletí. Reálný 5" FPV dron váží ~600 g; tady jde o čistě herní tuning,
ne o simulaci konkrétního stroje.

## Rotace `[OVĚŘENO 03.09.2026]`

Rotace se aplikuje přes `rb.MoveRotation`, ne přes torque — takže **rotace nemá inerci**. Stick pustíš
a otáčení okamžitě přestane. Reálný dron by ještě chvíli dorotovával.

## Otevřené otázky `[OTEVŘENÉ]`

1. **Má mít rotace inerci?** Přechod na `AddTorque` by byl fyzikálně věrnější, ale hůř se ovládá a
   znamená to přeladit všechny tři `*Speed` hodnoty. Pilot to pozná — chceme to?
2. **Kolize dronu.** `collisionDetection` je Discrete a masa 7,5 g — co se má stát při nárazu do
   terénu? Teď nic (dron nemá `DroneHealth`, viz [backlog.md](../backlog.md)). Crash damage? Odraz?
3. **Chybí modely propeleru / motorů** — má tah reagovat na poškození jednotlivých motorů, nebo je
   dron „jeden kus"?
4. **Rate profil.** Reálné acro vysílačky mají expo/rate křivky (nelineární odezva sticku). Teď je
   odezva lineární. Přidat?
