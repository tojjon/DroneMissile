# HUD

**Stav:** hotovo · otestováno hraním: ano · `[OVĚŘENO 04.10.2026]`

## Co to je

Herní překryv obrazovky, postavený v kódu (`DroneHUD`), jen zobrazuje — nebere input.

## Co ukazuje

- **Zaměřovací křížek** uprostřed — přesný bod dopadu rakety; při smrti zhasne.
- **Červený pulzující rám** po dobu stunu (u všech druhů stunu). Zásah odražený i-frames nic
  neukáže.
- **„YOU DIED"** po pádu. Na Easy v aréně zmizí, jakmile se vlna restartuje.

V aréně je nad ním ještě `RunUI` (číslo vlny, bonus z kruhů, pruh bosse, obrazovky) — viz
[waves.md](waves.md), [between-wave-screen.md](between-wave-screen.md),
[end-screen.md](end-screen.md).

## Kde v projektu

`Assets/scripts/DroneHUD.cs`, objekt `HUD` ve scénách (přidává
`Tools > DroneMissile > Build HUD`).

## Souvisí

Rozhodnutí [#12](../decisions.md), [#23](../decisions.md).
