# Sprawozdanie – Shadowsteam

**Zajęcia:** 2026-06-08  
**Autor:** Leonid Krichevksyi

---

## Co zrobiłem/am

> Zintegrowałem system obrażeń z bossem (arena bossa) – dodałem dodatkowe progi reakcji oraz odporność na hit-stun po przekroczeniu określonego procentu HP, aby walka z bossem nie sprowadzała się do permanentnego przerywania jego animacji.
> Połączyłem wszystkie dotychczas stworzone systemy (MovementBrain, system zdolności, Combo, AI przeciwników, system obrażeń) w jednej spójnej scenie demonstracyjnej, eliminując rozbieżności w nazwach eventów i referencjach do ScriptableObjects, które powstały przy pracy równoległej z resztą zespołu.
> Przeprowadziłem wstępny balans wartości: obrażenia ataków w zwarciu, kuli ognia oraz finishera w systemie Combo, a także czasy trwania hit-stunu i siłę odrzutu dla zwykłych przeciwników i bossa osobno.
> Naprawiłem kilka konfliktów wynikających z merge'a brancha feature/leonid/melee-attack do develop-finale (duplikujące się subskrypcje do Event Bus po przeładowaniu sceny).

---

## Problemy / blokery

> Po połączeniu systemów w jednej scenie ujawnił się problem z kolejnością inicjalizacji – MovementBrain odwoływał się do MovementSettingsSO zanim system zapisu/wczytywania zdążył nadpisać dane gracza po wczytaniu zapisu. Rozwiązałem to, przenosząc inicjalizację ruchu do late-initialization wywoływanego po zakończeniu wczytywania stanu gracza.
> Balans bossa wymagał kilku iteracji – pierwsza wersja odporności na hit-stun była zbyt wysoka i walka sprawiała wrażenie niereagującej na działania gracza; obniżyłem próg i dodałem krótkie okno podatności po ataku specjalnym bossa.

---

## Plan na kolejne zajęcia

> Dopracowanie polish dla walki z bossem – telegrafy ataków, dodatkowa faza w drugiej połowie HP.
> Przegląd i porządkowanie martwego kodu (m.in. MovementStaminaController) przed finalnym zamrożeniem zakresu projektu.
> Testy regresji całej ścieżki rozgrywki (menu → eksploracja → walka → boss → zapis/wczytanie) pod kątem błędów wynikających z integracji systemów.
