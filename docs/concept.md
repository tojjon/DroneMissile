# Koncept hry

## Vize

**FPV dron jako raketomet.** Hráč létá FPV dronem v acro módu s reálným citem letu a ničí
nepřátelské pozemní věže (turrety), které po něm střílí zpět.

Vzniklo to jako primitivní FPV simulátor a přerostlo to do shooteru. To pořadí je důležité:
**letová fyzika je základ, střílení je nadstavba.** Kdyby si někdy protiřečily, ustupuje střílení.

## Herní pilíře

1. **Věrný acro let.** Cílová skupina jsou lidi, co reálně létají FPV. Cit letu se neobětuje
   přístupnosti. Viz [design/flight-model.md](design/flight-model.md).
2. **Vše je uhýbatelné.** Nepřátelé střílí fyzické projektily, ne hitscan. Zásah je vždy důsledek
   letu hráče, ne hodu kostkou. Viz [design/weapons.md](design/weapons.md).
3. **Nepřátelé vydrží.** Turrety mají HP, ne one-shot kill — z boje se stává manévr, ne reflex.

## Rozsah — co existuje `[OVĚŘENO 03.09.2026]`

Jedna scéna (`SampleScene`) s terénem, jedním dronem a **jedním** turretem. Hráč umí létat a
střílet rakety, turret umí mířit a střílet zpět, turret má HP a dá se zničit.

## Co ještě neexistuje `[OTEVŘENÉ]`

Tohle nejsou bugy, tohle jsou nerozhodnuté věci. Žádná z nich není zadaná:

- **Žádná výhra.** Prohra a restart už existují (viz níž), cíl ne — turret se dá zničit, ale nic
  se tím neukončí.
- **Žádné UI kolem restartu nad rámec hlášky.** Pád ukáže `YOU DIED` a po sekundě načte scénu —
  žádné počítadlo pokusů, žádné „retry" tlačítko (restart input v projektu pořád neexistuje).
  `[AKTUALIZOVÁNO 04.09.2026 — viz rozhodnutí #12]`
- **HUD skoro žádné.** Existuje zaměřovací křížek uprostřed, červený rám při stunu a hláška při
  smrti ([rozhodnutí #12](decisions.md)). Není vidět throttle, počet raket, nic dalšího. (HP dron
  nemá — viz [rozhodnutí #10](decisions.md).)
- **Žádný zvuk.**
- **Žádná munice.** Rakety jsou nekonečné, jen s cooldownem `fireRate`.
- **Jeden typ nepřítele.** Viz [design/enemies.md](design/enemies.md).
- **Žádná úroveň/mise.** Terén je prázdný, turret stojí na jednom místě.

## Otevřené otázky ke konceptu `[OTEVŘENÉ]`

Tohle je potřeba rozhodnout dřív než cokoliv z toho výše, protože to určuje, co má vůbec smysl dělat:

1. **Kam to má směřovat?** Sandbox na létání s cíli k odstřelení / mise s definovaným cílem /
   arénová vlna nepřátel / časovka? Každá varianta chce jinou infrastrukturu.
2. **Je to FPV, nebo third-person?** Ve scéně je `Main Camera` — chová se to jako FPV z pohledu
   dronu (což by koncept vyžadoval), nebo se dron pozoruje z boku? Na tom stojí celý „FPV" v názvu.
3. ~~**Umírá hráč?**~~ **ROZHODNUTO 04.09.2026** — [rozhodnutí #10](decisions.md). Dron nemá HP;
   `DroneHealth` je smazaná. Jakýkoli dotek pevného objektu (terén, plošina, turret) **znovu načte
   scénu**, nepřátelská raketa místo damage na sekundu **bere ovládání**. Navazující otázka
   (němý restart vs. „crashed / retry" obrazovka) je taky **ROZHODNUTA 04.09.2026** —
   [rozhodnutí #12](decisions.md): němý není, pád ukáže hlášku a scéna se načte až po sekundě.
   Plnou „retry" obrazovku s tlačítkem to ale nedělá; restart input pořád neexistuje.
4. **Má být cílem přesnost, nebo přežití?** Určuje to, jestli přidat munici a jak agresivní mají
   být turrety.
