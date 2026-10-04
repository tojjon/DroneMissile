# Konzole upgradů v sandboxu

**Stav:** hotovo — commit `3d9ff72`, otestováno hraním 04.10.2026. Hotová featura:
[features/sandbox.md](../../features/sandbox.md).

## Co to má být `[ZADÁNÍ]`

V sandboxu se po stisku **Enter** otevře textové pole. Hráč napíše **jméno upgradu** a dron ho
dostane. Smysl: vyzkoušet si upgrady a jejich kombinace, aniž by je musel vyhrát v runu.

## Návrh implementace

- Pole se staví v kódu stejně jako ostatní UI (`UiKit`), hra se při psaní zastaví nebo aspoň
  přestane číst ovládání z klávesnice (jinak by WASD pohnulo dronem).
- Jména se hledají ve stejném seznamu upgradů jako karty a katalog; při psaní by šlo napovídat.
- Neznámé jméno → krátká hláška, nic se nestane.

## Rozhodnuté otázky `[ROZHODNUTO 04.10.2026]`

1. **Odebírání:** příkaz `remove <jméno>` (sundá jeden stack) a `clear` (všechno pryč).
2. **Jména:** obojí — pod polem se vypisují upgrady odpovídající tomu, co je napsané (prázdné
   pole = všechny).
3. **Pád:** upgrady pád přežijí. Žijí v `GameSession` (statické), které reload scény nemaže;
   vynuluje je až návrat do menu (`MainMenu.Start` → `ResetRun`).

## Plán implementace

- **`SandboxConsole`** (nový `MonoBehaviour`, `Assets/scripts/`), UI stavěné v kódu přes `UiKit`
  jako `RunUI`: vlastní canvas, panel dole vlevo s legacy `InputField`, řádek se zprávou a seznam
  návrhů. Mimo konzoli malá nápověda „ENTER — upgrade console" a seznam vlastněných upgradů.
- **Enter** otevře konzoli: `Time.timeScale = 0` (stojí fyzika i `Shoting`, WASD nic nedělá),
  pole dostane fokus. Enter v poli provede příkaz; úspěch konzoli zavře, chyba ji nechá otevřenou
  s hláškou. Prázdný Enter nebo **Esc** zavře beze změny.
- **Hledání jména:** podle `id` i `displayName`, bez ohledu na velikost písmen, mezery a `_`;
  stačí jednoznačný začátek (`dou` → Double Strike).
- **`GameSession`:** `RemoveUpgrade(id)`, `ClearUpgrades()`; `AddUpgrade` dostane přepínač pro
  sandbox, protože obtížnost z posledního runu (třeba Hardcore) v sandboxu nemá co blokovat.
- **Esc:** `ReturnToMenu` nesmí na Esc, kterým se zavírá konzole, odejít do menu.
- **Do scény:** `MainMenuBuilder.AddSandboxConsole` (idempotentní, `Tools > DroneMissile > Add
  Sandbox Console`), volá ho i `Build Sandbox Scene`.

**Ověření:** kompilace přes Unity CLI; v sandboxu Enter → `double` → Enter → dávka 2 raket;
`remove`, `clear`, neznámé jméno, Esc nezavře hru, upgrady po pádu zůstanou.

**Mimo rozsah:** ovládání konzole vysílačkou, historie příkazů.

## Odchylky při implementaci

- Příkaz se neprovádí podle toho, jestli byl stisknutý Enter, ale podle toho, jestli pole při
  ukončení editace obsahuje text: Esc vrací pole na prázdný text, takže prázdné = zavřít. Klik
  mimo pole s napsaným jménem ho tedy taky provede — neškodné a nezávislé na pořadí, v jakém Unity
  zpracuje klávesu a UI.
- Navíc: když je konzole zavřená, vlevo dole je nápověda „ENTER upgrade console" a seznam
  vlastněných upgradů (sandbox jinak nemá kde je ukázat).
- Do scény ji dal `Tools > DroneMissile > Add Sandbox Console` spuštěný z CLI — scéna se kvůli
  tomu nepřestavovala.

## Souvisí

[upgrades.md](../upgrades.md), [design/game-structure.md](../../design/game-structure.md).
