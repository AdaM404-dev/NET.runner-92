# Phase 11 – teljesítményoptimalizálás és GPU-mérés

2026. november 12. · Unity 6000.6.2f1 · HDRP 17.6 · Windows x64

A munka a Phase 11 különálló Unity-projektjén készült, az `f9335ec` alapverzióra építve. Ez az alapverzió már tartalmazta a #55-ben átdolgozott HUD-ot, főmenüt és beállításokat. A CODEX-TEST ág a renderelési terhelés, a képkockakorlátozás és a teljesítménymérés módosításait tartalmazza. A kipróbált Windows-build a főmenüből indul, és tartalmazza a kórházi jelenetet, valamint az előre kiszámított takarási adatot. [Phase 11 integráció, #55](https://github.com/AdaM404-dev/NET.runner-92/issues/55) · [A kórház teljesítménye, #36](https://github.com/AdaM404-dev/NET.runner-92/issues/36)

A napló- és CSV-útvonalak a Unity-projekt helyi `Logs/` mappájához tartoznak. Az újrafuttatás parancsai a [Phase 11 README-ben](README.md#checks) vannak.

## Mi okozta a magas terhelést?

A főmenü korábban átvette a játék FPS-beállítását. A játékos mentett profiljában Ultra minőség, 100%-os render skála, kikapcsolt VSync és korlátlan FPS szerepelt. Ezzel a kórház bejáratánál 124 FPS-t, a betegosztályon 147,5 FPS-t mértünk a 60 Hz-es kijelzőn. A Windows szerint a játék 3D-motorja a betegosztályon 99,9%-ig jutott.

A javítás két ponton csökkentette a renderelési munkát. A takarási adat és a használaton kívüli decal-passz kikapcsolása a képkockánkénti munkát csökkenti. Az FPS-korlát a másodpercenként elkészített képkockák számát szabályozza. Ez különösen sokat számít olyan jelenetben, ahol az adott gép a kijelző frissítésénél lényegesen több képkockát tud elkészíteni.

## Kórházi geometria és HDRP

A `HospitalOcclusion` editorparancs megnyitja a `Hospital_Exploration` jelenetet. Az importált kórházi környezet alatt lévő aktív mesh renderereket az `OcclusionRules` Core szabály osztályozza. Az átlátszatlan elemek takarhatják a mögöttük lévő geometriát. Az átlátszó és alpha clip anyaggal rendelkező elemek csak kitakarható besorolást kapnak. Az importált környezeten kívüli rendererek, köztük az ajtók, a lift és a K-7-ek, dinamikusak maradnak. [Takarási editorparancs](NETRunner92_Hospital/Assets/NETRunner/World/Editor/HospitalOcclusion.cs) · [Core szabály](NETRunner92_Hospital/Assets/NETRunner/Core/Scripts/OcclusionRules.cs)

A Unity 6837 takaró és 203 csak kitakarható renderert jelölt meg, 230 renderer dinamikus maradt. A bake a 2 m-es legkisebb takaróelemet, a 0,25 m-es legkisebb nyílást és a 100%-os backface küszöböt használja. Az eredmény az `OcclusionCullingData.asset`. A `com.unity.modules.umbra` beépített Unity-függőség biztosítja, hogy a Windows-player is betöltse az adatot. Az első mérőbuild naplója még jelezte, hogy Umbra nélkül kihagyta ezt az assetet. A modul hozzáadása után a build naplója már felsorolta a csomagolt takarási adatot. [Takarási adat](NETRunner92_Hospital/Assets/NETRunner92/Scenes/Hospital_Exploration/OcclusionCullingData.asset) · [Csomaglista](NETRunner92_Hospital/Packages/manifest.json) · Windows-buildnapló (`Logs/Phase11-FramePacing-Build.log`)

A játékos kameráján a `PreferencesRuntime` kikapcsolja a `FrameSettingsField.Decals` opciót. A kórházi jelenetben nincs HDRP decal projector, ezért a kamera kihagyhatja az üres decal-passzt és a hozzá tartozó depth prepasst. A Windows-próba ellenőrizte a kamera override-ját. Ehhez a módosításhoz külön milliszekundumos mérést nem futtattam. [Kamerabeállítás](NETRunner92_Hospital/Assets/NETRunner/Settings/Scripts/PreferencesRuntime.cs)

Ha az importált épület geometriája vagy a nyílások változnak, a Unity **NET.runner-92 → Bake Occlusion Culling** parancsa újraszámolja az adatot és elmenti a jelenetet.

### A takarás hatása egy képkockára

RTX 3060 Ti, Direct3D 11, 1920×1080-as képernyőn kívüli cél, Közepes profil. A két futás ugyanazt a fejlesztői buildet és a három azonos kameranézetet használta. A játékos kameráján kapcsoltam ki, majd be a takarást.

| Nézet | Háromszög képkockánként, ki → be | P95 teljes képkockaidő, ki → be |
| --- | ---: | ---: |
| Déli ellenőrzőpont | 7,86 M → 3,07 M | 14,60 → 10,01 ms |
| Recepció | 4,26 M → 0,73 M | 10,03 → 6,26 ms |
| Betegosztály | 6,26 M → 0,86 M | 12,94 → 8,86 ms |

A háromszögszám **61–86%-kal**, a P95 képkockaidő **31–38%-kal** csökkent a három nézetben. A P95 azt az időt mutatja, amely alatt a képkockák 95%-a elkészült. A képernyőn kívüli futásban a GPU-idő számlálója nem adott mintát. Az előtte-utána képeken a vizsgált nézetekben nem láttam eltűnő geometriát. Takarás kikapcsolva (`Logs/OcclusionUmbra/Off/Views.csv`) · Takarás bekapcsolva (`Logs/OcclusionUmbra/On/Views.csv`)

## Főmenü, játékbeli FPS-korlát és mentett profilok

A `MainMenu` jelenetben a játék `Application.targetFrameRate = 30` és `QualitySettings.vSyncCount = 0` értékkel fut. A menü háttere körülbelül 20 Hz-en animálódik. A menükamerán a 3D rétegmaszk üres, ezért a `QualitySwitch` kihagyja a dinamikus felbontás engedélyezését. A Windows-próbán a menü átlagosan **29,9 FPS-t** ért el. [Kameraszűrés](NETRunner92_Hospital/Assets/NETRunner/Settings/Scripts/QualitySwitch.cs)

A kórházi jelenet betöltésekor a `PreferencesRuntime` visszaállítja a mentett játékkorlátot. A `FramePacing` tiszta C# szabálya számolja ki, használható-e hardveres VSync. Asztali Unity-playerben pozitív `vSyncCount` mellett a kijelző frissítése és az osztó szabja meg a képkockatempót. [FramePacing szabály](NETRunner92_Hospital/Assets/NETRunner/Core/Scripts/FramePacing.cs) · [Unity VSync dokumentáció](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/QualitySettings-vSyncCount.html)

| Játékbeállítás | Alkalmazott szabály |
| --- | --- |
| VSync kikapcsolva | A játék a kiválasztott szoftveres FPS-korlátot használja. |
| VSync bekapcsolva, korlátlan FPS | A motor a kijelző natív frissítéséhez szinkronizál. |
| VSync bekapcsolva, véges FPS-korlát | A motor hardveres szinkront használ, ha a kijelző natív frissítése a korlát alatt van, vagy az 1–4 közötti osztóval kapott érték legfeljebb 0,25 Hz-re van a céltól. Egyébként a szoftveres korlát érvényesül. |

Például 60 FPS-es célnál egy 120 Hz-es kijelzőn 2-es, egy 240 Hz-es kijelzőn 4-es VSync-osztó működik. 144 Hz-en a 60 FPS-es szoftveres korlát érvényesül. Játék közben bekapcsolt VSync mellett a rendszer másodpercenként ellenőrzi a kijelző frissítését, és változáskor újraszámolja a tempót.

Új profil létrehozásakor és az alapértékek visszaállításakor **Közepes profil, 67%-os render skála és 60 FPS-es játékkorlát** az alapérték. A korábbi profilok megőrzik a mentett grafikai értékeiket, így a korábban választott korlátlan FPS is megmarad. A korlát a **Beállítások → Grafika → Játék FPS-korlátja** alatt módosítható. [Profilkezelés](NETRunner92_Hospital/Assets/NETRunner/Settings/Scripts/GamePreferences.cs) · [Grafikai beállítások](NETRunner92_Hospital/Assets/NETRunner/Menu/Scripts/SettingsScreen.cs)

## F4 teljesítménypanel

Az F4-panel a főmenüben és a játékban ugyanazon a jelenetváltást túlélő objektumon fut. Az Input System F4 eseményére nyílik és záródik. Megnyitás után fél másodperces ablakban átlagolja az FPS-t, és megjeleníti a Unity által elérhető CPU-főszál- és GPU-időt. A jobb felső panelen szerepel még az aktív FPS-korlát, a VSync-osztó, a felbontás, a grafikai szint, a render skála, a CPU, a GPU, a RAM és a VRAM-kapacitás. A százalékos kihasználtságot a külön Windows- és NVIDIA-mérés rögzítette. [Panel kódja](NETRunner92_Hospital/Assets/NETRunner/Settings/Scripts/PerformanceOverlay.cs) · F4-panel a Windows-buildben (`Logs/PacingReleaseAcceptance/performance-panel.png`)

Az F4 foglalt billentyű lett. Egy régi profilban F4-re kötött játékművelet a saját alapbillentyűjére kerül, ha az szabad, egyébként egy szabad alapbillentyűt kap. A migráció megtartja a többi érvényes billentyűkötést. [F4- és profiltesztek](NETRunner92_Hospital/Assets/NETRunner/Settings/Tests/PlayMode/PerformanceOverlayTests.cs)

## GPU-terhelés a mentett Ultra profillal

A mérőbuild a kiadott Windows-build forrásából készült, kiegészítő mérőkóddal. RTX 3060 Ti-n, Direct3D 11 alatt, 1920×1080-as keret nélküli módban futott. A mentett profil másolatában Ultra minőség, 100%-os render skála, kikapcsolt VSync és korlátlan FPS szerepelt. A páros futásokban ugyanazt a kameranézetet és grafikai beállítást használtam, csak az FPS-korlátot állítottam 60-ra.

| Nézet és korlát | Átlag FPS | Játék 3D-motorja, medián / csúcs | Teljes GPU, medián / csúcs |
| --- | ---: | ---: | ---: |
| Főmenü, 30 FPS | 29,9 | 26,5% / 26,8% | 26% / 34% |
| Kórházbejárat, korlátlan | 124,0 | 98,1% / 98,6% | 88% / 96% |
| Kórházbejárat, 60 FPS | 59,5 | 44,5% / 45,9% | 42% / 44% |
| Betegosztály, korlátlan | 147,5 | 99,5% / 99,9% | 97% / 98% |
| Betegosztály, 60 FPS | 59,5 | 44,3% / 45,4% | 43% / 44% |

A 60 FPS-es korláttal a játék 3D-motorjának csúcsterhelése a két kórházi nézetben **45–46%** lett ugyanazon az Ultra beállításon. A bejáratnál a medián GPU-idő képkockánként 7,66 ms-ról 7,74 ms-ra változott, miközben az átlagos FPS 124-ről 59,5-re esett. Ebben a páros mérésben a terheléscsökkenést a kevesebb elkészített képkocka magyarázza. A külön Közepes, 60 FPS-es futásban a három vizsgált kórházi nézetben a teljes GPU mintavett csúcsa 40% volt. Mentett Ultra profil, összesítés (`Logs/GpuLoad-SavedProfile/summary.csv`) · Közepes és Magas profil, összesítés (`Logs/GpuLoad-Controlled/summary.csv`)

A játékfolyamat medián CPU-terhelése a gép összes logikai szálára vetítve a bejáratnál 53,1%-ról 23,7%-ra, a betegosztályon 67,6%-ról 15,3%-ra csökkent. A teljes kártya mért VRAM-csúcsa mind a négy kórházi futásban 2022–2031 MiB között volt. CPU- és VRAM-minták összesítése (`Logs/GpuLoad-SavedProfile/summary.csv`)

### Mintavétel és visszaellenőrzés

A mérőkód nézetenként 3 másodperc bemelegítés után 12 másodpercig gyűjtött Unity képkockaadatokat. A Windows GPU Engine számlálójából a tesztelt folyamat legterheltebb 3D-motorját vettem. Az `nvidia-smi` 500 ms-onként lekérdezte az egész videókártya kihasználtságát. Az összesítő az adott szakasz első 1,5 és utolsó 0,2 másodpercének külső mintáit kihagyja, hogy a nézetváltások kevésbé befolyásolják az értéket. A táblázat csúcsa az időszakban rögzített legmagasabb minta. [NVIDIA mintavétel](https://docs.nvidia.com/deploy/nvidia-smi/index.html#utilization)

- Unity képkockaminták (`Logs/GpuLoad-SavedProfile/unity-frames.csv`) · Windows folyamatminták (`Logs/GpuLoad-SavedProfile/process-gpu.csv`) · NVIDIA nyers minták (`Logs/GpuLoad-SavedProfile/nvidia.csv`)
- A mérőbuildhez használt átmeneti futtatókód (`Logs/GpuLoad-SavedProfile/LocalGpuProbe.cs`) és editor buildparancs (`Logs/GpuLoad-SavedProfile/LocalGpuProbeBuilder.cs`) a helyi naplókban van. A kód a mérés idejére a Diagnostics runtime és a World Editor mappájába került. A buildparancs neve `NetRunner.World.Editor.HospitalSceneBuilder.BuildLocalGpuProbe`. A futtatást és összesítést a PowerShell-mérőszkript (`Logs/Measure-Gpu.ps1`) és a CSV-összesítő (`Logs/GpuLoad-Controlled/summarize.py`) végezte. Ezek helyi fájlok a Gitből kizárt `Logs/` mappában.

## Ellenőrzési állapot

- **192/192 EditMode és 17/17 célzott PlayMode** ellenőrzés ment át Windowson. A kiadott player próbája **50/54** ellenőrzésen ment át, köztük az új játék, mentés, visszatöltés, F4 kapcsolás, menükorlát és a kórházi FPS-korlát visszaállítása. EditMode-jelentés (`Logs/Pacing-EditMode.xml`) · PlayMode-jelentés (`Logs/PacingRelease-PlayMode.xml`) · Windows-próba (`Logs/PacingReleaseAcceptance/menu-checks.txt`)
- Az elrejtett ablakos futáson a szintetikus egérkattintás és a nyilas fókuszváltás sikertelen volt. Mindkettő a korábbi buildben is sikertelen volt azonos 30 FPS-es beállítással. Két HDRP-ellenőrzés is sikertelen maradt, a belső renderfelbontás és a volume override ellenőrzése. Korábbi build bemeneti próbája (`Logs/PacingBaselineInput/menu-checks.txt`) · Végső próba (`Logs/PacingReleaseAcceptance/menu-checks.txt`)
- A korábbi build `-net92-benchmark` futása 2-es hibakóddal zárult, mert a főmenüben kereste a játékost. A javított változat aszinkron betölti a kórházat, megvárja a grafikai beállításokat, majd öt rögzített nézetben mér FPS-korlát és VSync nélkül. Csak a kórházi kamera renderelését tartalmazó képkockákat mintázza. Ha az ablak nem renderel, külön textúrába rajzol, és a CSV `Offscreen` módot jelöl. A hiányzó GPU-időzítést vagy renderelési számlálót `-1` jelzi. A fenti GPU-kihasználtsági adatok a korábbi, külön mérőbuildből származnak. [Benchmark kód](NETRunner92_Hospital/Assets/NETRunner/Diagnostics/Scripts/PerformanceCapture.cs) · Korábbi próbanapló (`Logs/ComplianceAudit/benchmark-player.log`)
- A friss Windows-build benchmarkja **0-s kilépési kóddal** írt öt adatsort 1920×1080, Medium beállításon, RTX 3060 Ti / Direct3D 11 alatt. Képernyőn kívül nézetenként 737–1443 renderelt képkocka gyűlt össze. A teljes képkockaidő P95 értéke 4,83–7,48 ms volt, és mind az öt nézetben rendelkezésre állt a háromszögszámláló. A GPU-idő és a draw call számláló ebben a futásban nem volt elérhető. CSV (`Logs/Step55/benchmark-final/performance.csv`) · Player napló (`Logs/Step55/benchmark-player-final.log`)
- A #55 negyedik lépésének helyi módosításai után Windowson **295/295 EditMode** és **100/100 PlayMode** teszt ment át. A Vulkan alatt futtatott PlayMode-csomagban mind a **95 kórházi ellenőrzés** sikerült. EditMode (`Logs/Step55/EditMode.xml`) · PlayMode (`Logs/Step55/PlayMode.xml`) · Kórházi ellenőrzések (`Logs/hospital-checks/playmode_checks.txt`)
- A friss kiadási build külön profillal futtatott próbája **55/55** ellenőrzésen ment át. A korábbi futásban sikertelen négy ellenőrzés most sikerült. A nyilas fókuszváltás és az egérkattintás működött, a 1280 pixeles ablak 80%-os render skálán 1024 belső pixelt adott, és a HDRP volume rögzítette a 18 méteres árnyéktávot és az expozíciót. Windows-próba (`Logs/Step55/menu-profile/menu-checks.txt`)
- A #55 negyedik lépéséből a Player és a World futásidejű refaktor bekerült. A World pályaépítőinek szétválasztása és a Diagnostics teljes refaktora a `main` ágon még függőben van. Ehhez a helyi verzióhoz a teljes Linux-futtatás is hátravan. [#55](https://github.com/AdaM404-dev/NET.runner-92/issues/55) · [Linux-futtatás](README.md#linux-validation)
