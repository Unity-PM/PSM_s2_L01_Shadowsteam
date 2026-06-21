# Sprawozdanie – Shadowsteam

**Zajęcia:** 2026-05-25  
**Autor:** Leonid Krichevksyi

---

## Co zrobiłem/am

> Zaimplementowałem system otrzymywania obrażeń (Damage System) dla przeciwników, oparty na wspólnym interfejsie IDamageable, co pozwala na jednolitą obsługę trafień zarówno od ataków w zwarciu, jak i od kuli ognia.
> Dodałem mechanikę hit-stun – krótkotrwałe zablokowanie akcji przeciwnika po otrzymaniu obrażeń, zsynchronizowane z maszyną stanów AI (wymuszone przejście do stanu reakcji niezależnie od bieżącego zachowania).
> Wprowadziłem system odrzutu (knockback) wykorzystujący CharacterController – siła i kierunek odrzutu są wyliczane na podstawie pozycji atakującego względem celu.
> Zintegrowałem reakcję na trafienie z systemem animacji (DynamicAnimator), dodając osobną warstwę animacji hit-reaction, która nie przerywa dolnej warstwy ruchu.
> Podłączyłem feedback wizualny (krótkie miganie materiału przeciwnika) oraz dźwiękowy przy otrzymaniu obrażeń.

---

## Problemy / blokery

> Napotkałem konflikt między systemem knockbacku a NavMeshAgent/pathfindingiem przeciwnika – odrzucony przeciwnik próbował natychmiast wrócić na ścieżkę, co wyglądało nienaturalnie. Rozwiązałem to przez tymczasowe wyłączanie nawigacji na czas trwania odrzutu i jej ponowne włączenie po zakończeniu hit-stunu.
> Miganie materiału przy trafieniu początkowo nadpisywało docelowo wszystkie materiały przeciwnika (w tym przezroczyste elementy); naprawiłem to, filtrując tylko renderery z odpowiednim shaderem.

---

## Plan na kolejne zajęcia

> Integracja systemu obrażeń z bossem – dodatkowe fazy reakcji oraz odporność na hit-stun powyżej określonego progu obrażeń.
> Połączenie wszystkich systemów (ruch, walka, AI, obrażenia) w jednej spójnej scenie demonstracyjnej.
> Wstępny balans wartości obrażeń, odrzutu i czasu trwania hit-stunu dla różnych typów przeciwników.
