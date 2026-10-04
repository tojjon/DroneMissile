# Plány

Věci, které **se dělají nebo jsou jen naplánované** — zadané, ale nepostavené.

| Složka | Co v ní je |
|---|---|
| `plans/` | co **teprve bude nebo se právě dělá** — zadání, plán implementace, otevřené otázky |
| [`plans/archive/`](archive/README.md) | plány **dokončené a uzavřené** |
| [`features/`](../features/README.md) | co ve hře **je teď** |

## Postup pro novou featuru

1. **Zapsat do plánů.** Nový dokument `plans/<featura>.md` se stavem *plánováno*: zadání
   (`[ZADÁNÍ]`), co už je připravené, otevřené otázky. Přidat řádek do seznamu níž.
2. **Napsat plán implementace** do téhož dokumentu (sekce *Plán implementace*): kontext, co je
   dohodnuté, přístup, dotčené soubory, ověření, mimo rozsah. Stav → *rozpracováno*.
3. **Stavět** a průběžně psát do dokumentu odchylky od plánu (sekce *Odchylky při implementaci*).
4. **Po dokončení:** popis hotové featury do [`features/`](../features/README.md) (nový dokument
   nebo úprava existujícího + řádek v jejím indexu), rozhodnutí do
   [`decisions.md`](../decisions.md), a plán **přesunout do [`archive/`](archive/README.md)** se
   stavem *hotovo* a odkazem na commit. Řádek tady smazat.

## Seznam

| Plán | Stav | Blokuje to |
|---|---|---|
| [Realistická letová fyzika](flight-physics.md) | plánováno, plán hotový — čeká na schválení | Viktorovy rates (jinak výchozí) |
| [Hardcore mód](hardcore-mode.md) | postaveno, čeká na test | — |
| [Upgrady a karty](upgrades.md) | plánováno | seznam upgradů (Viktor) |
| [ESC menu](esc-menu.md) | plánováno | — |
| [Ukládání a Continue](save-continue.md) | plánováno | nejlépe až po upgradech (ukládají se taky) |
| [Konzole upgradů v sandboxu](sandbox-console.md) | plánováno | upgrady |
| [Model kamene a kamenný stun](rock-model.md) | rozpracováno | model (Viktor) |
