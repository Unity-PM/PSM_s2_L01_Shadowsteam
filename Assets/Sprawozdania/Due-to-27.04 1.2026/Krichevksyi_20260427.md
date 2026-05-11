# Sprawozdanie – Shadowsteam

**Zajęcia:** 2026-04-27  
**Autor:** Leonid Krichevksyi

---

## Co zrobiłem/am

> Zaimplementowałem zaawansowany system sterowania oparty na modularnym „mózgu” (MovementBrain), który zarządza stanami postaci w sposób wysoce elastyczny i zgodny z zasadami SOLID (separacja logiki stanów od wejścia).
> Stworzyłem moduły ruchu dla stanów: Idle, Running, Sprinting, Airborne (Jump) oraz Gliding, co pozwala na łatwą zmianę zachowania gracza poprzez modyfikację warunków logicznych w jednym miejscu.
> Zrezygnowałem z komponentu Rigidbody na rzecz autorskich formuł matematycznych i CharacterController, co zapewnia precyzyjną kontrolę nad fizyką ruchu.
> Zintegrowałem system zużycia staminy oraz stworzyłem system konfiguracji parametrów przez ScriptableObjects (MovementSettingsSO).
> Zaimplementowałem system kamery trzecioosobowej (Third Person Camera) w pełni kompatybilny z nowym modelem poruszania się.

---

## Problemy / blokery

> Rozwiązałem problem resetowania prędkości sprintu do prędkości chodu podczas wykonywania skoku.
> Zoptymalizowałem warunki logiczne dla stanu Gliding, wprowadzając precyzyjną weryfikację flagi isGrounded, aby wyeliminować błędy przy próbie aktywacji szybowca na ziemi.

---

## Plan na kolejne zajęcia

> Implementacja systemu zdolności bojowych i ich integracja z maszyna stanów (np. blokada użycia ognistych kul podczas szybowania).
> Refaktoryzacja systemu walki: wprowadzenie logicznego podziału na ataki dystansowe i wręcz.
> Implementacja systemu sekwencji ataków (Combo) opartego na prostym i rozszerzalnym kodzie.
