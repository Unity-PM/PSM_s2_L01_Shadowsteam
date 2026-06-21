# Sprawozdanie – Shadowsteam

**Zajęcia:** 2026-06-21  
**Autor:** Leonid Krichevksyi

---

## Co zrobiłem/am

> Dodałem telegrafy ataków bossa (krótkie okno ostrzegawcze przed atakiem specjalnym, sygnalizowane animacją i dźwiękiem), co znacząco poprawiło czytelność walki.
> Wprowadziłem drugą fazę walki z bossem aktywującą się poniżej 50% HP – zwiększone tempo ataków oraz dodatkowy wzorzec ataku obszarowego.
> Uporządkowałem martwy kod związany z MovementStaminaController – usunąłem zakomentowaną logikę i scaliłem odpowiedzialność za zużycie staminy w jednym miejscu (MovementBrain), zgodnie z wcześniejszymi ustaleniami zespołu.
> Przeprowadziłem pełne testy regresji ścieżki rozgrywki: menu główne → eksploracja → walka w zwarciu i dystansowa → system Combo → walka z bossem → śmierć/odrodzenie → zapis/wczytanie gry.
> Naprawiłem zgłoszone podczas testów błędy: nieprawidłowe resetowanie liczników Combo po odrodzeniu gracza oraz przypadek, w którym zapis gry podczas trwania hit-stunu prowadził do zablokowania ruchu po wczytaniu.

---

## Problemy / blokery

> Testy regresji ujawniły rzadki błąd (niedeterministyczny, występujący przy bardzo szybkim przełączaniu między atakiem dystansowym a leczeniem) powodujący przejściowe zablokowanie wejścia gracza. Udało się zawęzić przyczynę do wyścigu między kolejkowaniem wejścia (Input Queuing) a resetem stanu Combo, ale pełne rozwiązanie wymaga jeszcze dodatkowych testów przed uznaniem go za zamknięte.
> Druga faza walki z bossem przy pierwszych testach okazała się zbyt wymagająca w połączeniu z mechaniką hit-stunu gracza – zmniejszyłem częstotliwość ataku obszarowego, by zachować balans.

---

## Plan na kolejne zajęcia

> Domknięcie błędu związanego z wyścigiem Input Queuing / reset Combo przy szybkim przełączaniu zdolności.
> Dalsze testy balansu drugiej fazy bossa z udziałem reszty zespołu.
> Przegląd i ewentualne uporządkowanie nieużywanych assetów/pakietów (Scene Teleportation Kit, Visual Scripting) przed finalnym etapem projektu.
