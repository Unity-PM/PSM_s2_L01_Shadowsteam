# Sprawozdanie – Shadowsteam

**21.06.2026** 
**Autor:** Liubetskyi Anton

---

## Co zrobiłem/am

> Naprawiłem wszystkie ostrzeżenia fizyki w konsoli — Enemy.StabilizePhysicsBody nie ustawia już prędkości na ciele kinematycznym (zerowanie tylko gdy ciało jest jeszcze dynamiczne)
> Usunąłem ostrzeżenia „DontDestroyOnLoad only works for root GameObjects" (PersistentSceneObject, BackgroundAudioManager, PauseMenuController, SceneTeleportManager) — obiekty są odpinane od rodzica przed DontDestroyOnLoad
> Naprawiłem fałszywe ostrzeżenie QuestManager „skipped unknown quest id 'main_find_wizard'" — quest jest rejestrowany dynamicznie i przywracany przez RestoreLoadedActiveQuest
> Zabezpieczyłem ComponentRegistry przed wyjątkiem InvalidCastException + dodałem fallback wyszukiwania Canvas w EnemyHealthBar
> Naprawiłem wszystkie błędy animacji — uporządkowałem przejścia stanów i identyfikatory klipów (idle, chód, bieg, atak, śmierć) dla gracza i przeciwników, bez zacinania i nakładania klipów
> Naprawiłem zapis przedmiotów — po wczytaniu nie ginie już ikona ani efekt (modyfikatory). Skanowanie nieaktywnych pickupów (FindObjectsInactive.Include) + cache przedmiotów runtime w GameSession między scenami
> Rozszerzyłem mapę — większy obszar rozgrywki, nowe sekcje terenu i punkty orientacyjne, przebudowany NavMesh, rozmieszczeni przeciwnicy i przedmioty
> Dodałem nowe przedmioty (EquipmentSO) z modyfikatorami statystyk oraz odpowiadające im ItemPickup na mapie
> Dodałem quest „Try the Fireball" (klawisz 1) jako pierwszy krok łańcucha SimpleQuestGiver — nowy typ celu CastAbilityObjective + zdarzenie AbilityCastEvent
> Przetłumaczyłem wszystkie komentarze i napisy w skryptach z rosyjskiego na angielski
> Naprawiłem błąd kompilacji buildu — narzędzie edytora AIUIGenerator przeniesione do folderu Editor

---

## Problemy / blokery

> Konsola była zalana ostrzeżeniami fizyki i DontDestroyOnLoad (mnożonymi przez liczbę przeciwników i przeładowania scen), co ukrywało prawdziwe błędy — trzeba było prześledzić każde źródło osobno
> Ikona przedmiotu nie jest zapisywana w JSON (Sprite tworzony w runtime z ItemPickup) — po zimnym restarcie gry między różnymi scenami ikona nadal nie wraca; w obrębie jednej sesji i tej samej sceny działa poprawnie
> Build gracza nie kompilował się przez skrypt edytora w folderze runtime — w samym edytorze kompilował się bez problemu, więc błąd ujawnił się dopiero przy budowaniu gry

---

## Plan na kolejne zajęcia

> PREZENTACJA PROJEKTU)