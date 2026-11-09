# NET.runner / 92 — önálló interfészcsomag

Futurisztikus, letisztult felület a lopakodós, hackelős NET.runner-92 játékhoz. Sötét grafit alapok, törtfehér szöveg, visszafogott türkiz kiemelések és borostyán figyelmeztetések. Magyar feliratok; 1920 × 1080 referenciafelbontás.

Ez különálló grafikai és interakciós csomag. Az eredeti játék Unity-projektjében nem változtat fájlokat. A térkép, a távolságok és az állapotok tervezési mintaadatok; a folyosó saját, egyszerű vektoros háttér a HUD elhelyezésének bemutatásához.

## Megnyitás

1. Nyisd meg a **NET-runner-92-preview.html** fájlt egy böngészőben. Internet, telepítés és helyi szerver sem szükséges. A teljes előnézet ebben az egy fájlban van.
2. A **blender/NET-runner-92-interface.blend** a natív, szerkeszthető 2D Blender-forrás. A felső Scene választóban hét jelenet közül választhatsz. Az Outliner gyűjteményei külön tartják a felület részeit és a szemléltető hátteret.
3. A **screens/** hat teljes, szerkeszthető SVG-képernyőt tartalmaz. A **components/** harminc külön SVG-elemet ad az egyenkénti átmásoláshoz.
4. A **png/screens/** teljes képernyőképeket, a **png/components/** pedig átlátszó RGBA-elemeket tartalmaz. Az `@2x` utótag kétszeres felbontást jelöl.

## Képernyők

| Fájl kezdete | Tartalom |
| --- | --- |
| `01-main-menu` | Főmenü, műveletindítás, térkép és beállítások |
| `02-gameplay-hud` | Aktív cél, iránytű, rejtettség, állapot, energia, interakció és képességek |
| `03-pause` | Szünetmenü és visszatérés a művelethez |
| `04-settings` | Hangerő, HUD-méret, erős kontraszt és mozgáscsökkentés |
| `05-hacking` | Karbantartási terminál és kapcsolódási folyamat |
| `06-mission-map` | Útvonal, kijelölhető csomópontok és célpontadatok |

A Blender-fájl hetedik jelenete, a **hud-overlay**, csak a felületet tartalmazza, átlátszó háttérrel. Minden felirat natív szövegobjektum, a panelek és ikonok külön síkbeli geometriai elemek. A 30 kivágott komponens teljes vektoros forrása az SVG-kben található.

## Az előnézet kezelése

| Billentyű | Funkció |
| --- | --- |
| `E` | Terminál megnyitása a HUD-on |
| `Q` | Impulzus: 20 energia, 6 másodperc újratöltés |
| `Tab` vagy `M` | Térkép a HUD-on |
| `Esc` | Szünet, illetve visszalépés |
| `V` | Rejtett / keresés alatt állapotminta váltása |
| `Tab`, `Shift+Tab`, `Enter`, `Space` | A menük fókuszálása és kezelése |
| `←`, `→`, `Home`, `End` | A fókuszált hangerőcsúszka kezelése |

A térképen az archívum, a belépési pont és a K7 járőr kijelölhető. A csatlakozás rövid, helyi bemutató; befejezése frissíti a célfeliratot. A szünetmenü és a többi megnyitott panel alatt az energia és az impulzus újratöltése megáll. A hackelés megszakításakor a folyamat időzítője leáll.

A beállításokat az előnézet a böngésző helyi tárolójába menti. Ha az adott böngésző ezt fájlmegnyitásnál tiltja, a beállítások az aktuális munkamenetben működnek, és a felület ezt kiírja. A művelet állapotához nem készül játékmentés.

## Átmásolás más programba

- **Szerkeszthető grafika:** a kívánt SVG-t importáld a célprogramba. A feliratok szövegek maradnak az SVG-forrásban, a formák vektorosak. Nincs beágyazott raszteres háttér vagy külső képfüggőség. Az importáló program a saját SVG-támogatása szerint kezeli a csoportokat és a szöveget.
- **Képként:** használd a megfelelő átlátszó PNG-t. A teljes `hud-overlay.png` 1920 × 1080-as koordináták szerint illeszthető játékfelvételre vagy kameraképre. Az `@2x` HUD 3840 × 2160-as.
- **Egyedi elemek:** a kivágott panelek körül 4 referencia-pixel átlátszó hely marad, hogy a körvonalak ne vágódjanak le. Eredeti képernyőpozíciójuk a `source/layouts.json` `sourcePosition` mezőjében található.
- **Natív Blender-szerkesztés:** válassz jelenetet, majd szövegobjektumot vagy a megfelelő panelgyűjteményt. A kamerák ortografikusak; a képernyők az XY-síkban vannak, 100 pixel = 1 Blender-egység méretarányban.
- **Későbbi játékbeli kötés:** a változó tartalmak SVG-azonosítói és az elvárt adatok a `binding-contract.json` fájlban vannak. A csomag nem tartalmaz Unity-, Unreal- vagy más motorhoz kötött integrációt.

Színek, méretek és térközök: **design-tokens.json**. A referenciakép 16:9 arányú, 64 pixeles külső biztonsági sávval. Az előnézet más képarányon aránytartóan, üres oldalsávval jelenik meg; külön mobilos elrendezés nincs benne.

A betűkészletek a Windows rendszerben megtalálható **Segoe UI** és **Consolas**. A csomag nem másolja a betűkészletfájlokat. Más rendszeren ezeket a célprogramban elérhető betűre cserélheted; a PNG-k már tartalmazzák a megjelenített szöveget. A Blender-forrás a helyi Windows-betűket hivatkozza, és a szöveg méretét a mért SVG-megjelenéshez igazítja.

## Újragenerálás

A `source/` könyvtár a teljes generálható forrást tartalmazza.

```powershell
python .\source\build_interface.py
python .\source\export_and_verify.py
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --factory-startup --python .\source\create_blender.py
```

Az exportáló szkript helyi Chrome-ot vagy Edge-et és Pillow-t használ. Külön, ideiglenes böngészőprofilt indít. A Blender-generálást az export után futtasd, hogy az SVG-kből mért szövegméretek is rendelkezésre álljanak. A HTML-előnézethez és a kész SVG/PNG-elemek használatához ezekre nincs szükség.

## Ellenőrzött állapot — 2026. november 3.

- 6 teljes SVG-képernyő; 30 külön SVG-elem; 66 PNG-export.
- A 36 SVG XML-szerkezete érvényes, az azonosítók fájlon belül egyediek.
- A 60 komponens-PNG mindegyike RGBA, átlátszó pixelekkel; 1× és 2× méretek ellenőrizve.
- 22 működési ellenőrzés sikeres a helyi Chrome-ban: indítás, impulzus, újratöltés, szünet, térkép, beállítások, billentyűzetes csúszka, hackelés, megszakítás és főmenü-visszatérés.
- A mentett Blender-forrás hét ortografikus, szerkeszthető jelenetet tartalmaz; a főmenü és a HUD a visszanyitott forrásból is renderelve és vizuálisan ellenőrizve.
- Az SVG-alapú hat képernyő áttekintő képe: **review/interface-overview.jpg**.

A részletes jelentések a `review/` mappában vannak. A grafikai terv és a helyi előnézet készült el; valódi játékmenet, játékmentés és motorintegráció nincs mögötte.
