# Pruh HP nad turretem

**Stav:** hotovo · otestováno hraním: ano · `[OVĚŘENO 04.10.2026]`

## Co to je

Červený pruh **ve světě nad každým turretem**, který se zkracuje se ztrátou HP. Patří konkrétnímu
turretu: zakrývá ho terén a zmenšuje se se vzdáleností. Boss ho nemá — jeho pruh je nahoře na
obrazovce ([boss.md](boss.md)).

## Ladění

| Parametr | Hodnota |
|---|---|
| výška nad turretem | 4,8 m |
| velikost | 2,7 × 0,33 m |
| viditelný do | 90 m |
| barva | červená po celé délce |

## Kde v projektu

`Assets/scripts/TurretHealthBar.cs` na kořeni turretu; canvas si staví sám za běhu.

## Souvisí

[turret.md](turret.md), rozhodnutí [#13](../decisions.md).
