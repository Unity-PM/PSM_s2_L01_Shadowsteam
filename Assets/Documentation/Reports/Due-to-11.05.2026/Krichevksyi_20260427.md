# Sprawozdanie – Shadowsteam

**Zajęcia:** 2026-04-27  
**Autor:** Leonid Krichevksyi

---

## Co zrobiłem/am

> Zaimplementowałem rozbudowany system zdolności bojowych i w pełni zintegrowałem go z istniejącą maszyną stanów MovementBrain. System pozwala na nakładanie restrykcji na akcje w zależności od stanu ruchu (np. blokada rzucania ognistych kul podczas szybowania).
> Dodałem nowe moduły umiejętności aktywnych: system teleportacji (umożliwiający szybką zmianę pozycji) oraz moduł leczenia (Heal), oba zintegrowane z zasobami postaci.
> Zaprojektowałem i wdrożyłem wysoce elastyczny system Combo. System ten pozwala na definiowanie wielofazowych sekwencji ataków, gdzie każde kolejne naciśnięcie przycisku aktywuje inny etap kombinacji (np. Atak 1 -> Atak 2 -> Finisher).
> Zaimplementowałem pełną parametryzację systemu Combo: możliwość przypisania dowolnej ataki do konkretnej fazy, definiowanie precyzyjnych odstępów czasowych (timing windows) między atakami oraz określanie warunków stanowych (np. konkretny atak dostępny tylko w powietrzu lub tylko podczas sprintu).

---

## Problemy / blokery

> Napotkałem trudności z synchronizacją okien czasowych (input buffer) dla systemu Combo, co powodowało przerywanie sekwencji przy zbyt szybkim klikaniu. Rozwiązałem to poprzez wprowadzenie kolejkowania wejścia (Input Queuing) i precyzyjne odliczanie czasu wewnątrz logicznego modułu walki.
> Zoptymalizowałem warunki logiczne dla stanu Gliding, wprowadzając precyzyjną weryfikację flagi isGrounded, aby wyeliminować błędy przy próbie aktywacji szybowca na ziemi.

---

## Plan na kolejne zajęcia

> Implementacja pełnej interakcji zdolności bojowych z przeciwnikami – stworzenie systemu otrzymywania obrażeń, reakcji na trafienie (hit-stun) oraz odrzutu
> Integracja wszystkich nowo powstałych systemów w ramach jednej, kompleksowej sceny demonstracyjnej.
