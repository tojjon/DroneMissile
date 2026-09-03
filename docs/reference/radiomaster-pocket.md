# RadioMaster Pocket — Unity Input System

> **Needitovat z hlavy.** Všechno níž bylo zjištěno empiricky ruční sondou skutečného zařízení.
> Input Debugger v Unity tyhle informace nezobrazuje. Když to někdo „opraví" podle intuice,
> ovládání přestane fungovat *mlčky* (chybějící control vrací `0f` bez chyby).

Vysílačka je připojená přes **USB** a v Unity Input Systemu se hlásí jako generický `Joystick`.

## Zařízení se registruje dvakrát

Jedna fyzická vysílačka se v Unity objeví jako **dvě zařízení** s prakticky stejným display name:

```
Radiomaster Pocket Joystick
Radiomaster Pocket Joystick1
```

Proto kód prochází `Joystick.all` a hledá zařízení, jehož jméno obsahuje `Joystick1`; když ho
nenajde, vezme **poslední** položku v `Joystick.all` jako fallback.

## Cesty ke kontrolům

Tohle je ta část, kterou nejde uhádnout. **Osy nejsou všechny na rootu zařízení:**

| Funkce | Cesta pro `TryGetChildControl` | Kde leží |
|---|---|---|
| Throttle | `"z"` | root zařízení |
| Yaw | `"rx"` | root zařízení |
| Pitch | `"stick/y"` | **zanořeno pod sub-kontrolou `stick`** |
| Roll | `"stick/x"` | **zanořeno pod sub-kontrolou `stick`** |
| Fire | `"trigger"` | root, typ `ButtonControl` |

Pitch a roll **nejsou** dostupné jako `"y"` a `"x"` — i když Input Debugger zobrazuje jen ten krátký
název. Plná cesta vede přes `stick/`.

## Rozsahy os

| Osa | Rozsah | Klid | Centruje se? |
|---|---|---|---|
| `z` (throttle) | −1 = motory off, +1 = plný tah | **0 = 50 % tahu** | ne |
| `rx` (yaw) | −1 .. +1 | 0 | ano |
| `stick/x` (roll) | −1 .. +1 | 0 | ano |
| `stick/y` (pitch) | −1 .. +1 | 0 | ano |

Throttle je ta zrádná: **`0` neznamená „vypnuto", ale poloviční tah** — na rozdíl od gamepad
throttlu, kde by 0 typicky byla nula. Proto se v kódu remapuje na `0..1` vzorcem `(raw + 1) / 2`.
A na rozdíl od ostatních os se **sám nevystřeďuje** — zůstane, kde ho pilot nechá.

Pitch a roll se navíc **invertují** (negují) — to je ale uživatelská preference, ne vlastnost
hardwaru, viz [design/controls.md](../design/controls.md).

## Jak se to zjistilo (a jak to zjistit pro jinou vysílačku)

Postup, který zabral, když Input Debugger nestačil:

1. Projít `transmitter.allControls` a vypsat u každého `name`, `path` a `GetType().Name`. Teprve
   `path` odhalí zanoření pod `stick/`.
2. Hýbat postupně každou osou a sledovat, které cesty reagují.
3. Mačkat tlačítka a logovat, co se změní — takhle se našel `"trigger"`.

Krok 1 je **stále v kódu**: `DroneControls.Start()` loguje všechna zařízení i všechny jejich controly
při každém spuštění. Je to záměr, ne zapomenutý debug — je to nástroj na rekalibraci pro jinou
vysílačku. Než to někdo smaže, ať ví, že tím zahodí jediný způsob, jak tyhle cesty zjistit.
