# EventSourcing

Einfache Begriffe für schwierige Konzepte:

Command 
: Ein Befehl, der eine Aktion anfordert.

Validator
: Ein Validator, der sicherstellt, dass der Befehl gültig ist. Die Validierung bezieht sich auf die Eingabewerte.

CommandHandler
: Ein Handler, der den Befehl verarbeitet und die Geschäftslogik anwendet.

Event
: Ein Ereignis, das in der Vergangenheit stattgefunden hat. Das Ereignis ist das Ergebnis des Befehls.

EventStore
: Ein Speicher, der alle Ereignisse speichert. Der EventStore ist die Quelle der Wahrheit.

Aggregate
: Ein Aggregat ist die Summe aller Ereignisse. Es ist der aktuelle Zustand des Systems.
 
Projection
: Eine Projektion ist eine Abbildung der Ereignisse auf ein bestimmtes Modell. Es ist eine Sicht auf die Daten, die für den Benutzer relevant sind. Die Projektion **ist ungleich** dem Aggregat, da sie nur eine Darstellungsform der Daten ist. Die Projektion wird aus den Ereignissen erstellt (entweder live oder asynchron) und meist in einer (anderen) Datenbank gespeichert.

Query
: Eine Abfrage, die fast immer auf das Ergebnis einer Projektion zugreift. Die Projektion hat in der Regel eine andere Struktur als das Aggregat, weil sie die Daten in einer Form aufbereitet, die für den Benutzer relevant ist. So können auch komplizierte Ansichten beim Eintreffen eines Events über die Projektion erstellt/aktualisiert werden (write) und dann immer wieder sehr schnell aus den Projektionsergebnissen (z.B. Datenbank) ausgelesen werden (read).

## Konzepte für Event Sourcing

Event Sourcing kam in der Vergangenheit (und auch heute noch) fast untrennbar mit Domain Driven Design (DDD) und strikter Objektorientierung vor. 
Neuere Ansätze kommen über CQRS (Command Query Responsibility Segregation) und Clean Architecture daher. Kombinationen sind ebenfalls möglich.
Es gibt viele Ansätze, Konzepte und auch Werkzeuge (nicht zuletzt je nach Programmiersprache) um Event Sourcing zu implementieren.
Ich versuche in den folgenden Abschnitten die Konzepte mit denen ich mich tiefer beschäftigt habe noch einmal "schriftlich zu verarbeiten" und meine Gedanken dazu zu sortieren.

### Konzept 1: OOP und DDD

Mit diesem Konzept wird die gesamte Businesslogik in den Aggregaten abgebildet. Die Aggregate sind die Wurzel der Geschäftslogik. Sie sind die Quelle der Wahrheit und die einzige Stelle, an der Änderungen am Zustand des Systems vorgenommen werden können.
Das ist objektorientierte Programmierung in Reinkultur. Relativ schnell sind aber bei mir Fragen aufgekommen ...

- Wenn ich ein Aggregat habe, welches die Validierung der Eingangswerte übernimmt, wie verhält es sich in Web-APIs.
  - Ich sollte dort die Validierung der Eingabewerte machen, bevor ich den Command an den CommandHandler übergebe. Das wäre also eine doppelte Validierung.
  - Ich lasse die Validierung im Aggregate und gebe FluentResults zurück. Das überfrachtet das Aggregat und macht es unübersichtlicher, dafür habe ich die Validierung nur an einer Stelle.
  - Ich lasse die Validierung im Aggregate und werfe Exceptions. Das ist einfach, aber erfordert eine Exception-Handling-Logik (z.B. Middleware) welche die Fehlermeldungen dann adaptiert und an den Client zurückgibt.  
  Dies wird häufiger gemacht, aber gerade bei Validierungsfehlern (die dann doch häufiger vorkommen) ist das aufgrund der Performance nicht optimal.
  - Um die Validierung sowohl in der API als auch im Aggregate konsistent zu halten, sollte man die Validierung in eine eigene Klasse auslagern (z.B. `XyzValidator:AbstractValidator<Xyz>`).  
    Dies müsste dann mittels DependencyInjection in das Aggregat injiziert werden.  
    Dafür muss es dann für das Aggregat eine Factory geben, die das Aggregat mit dem Validator erstellt.  
    Soweit so gut - es bleibt allerdings dabei, dass dann die Validierung doppelt ausgeführt wird ...
- Wenn ich ein Aggregat habe, welches die Änderung seines Status gegen andere Entitäten abgleichen (validieren) muss, wie mache ich das?  
Beispiel: Ich möchte, dass ein Name nur einmal vergeben werden kann. Das bedeutet, dass ich den Namen gegen alle anderen Namen abgleichen muss. Das ist nicht trivial, da ich ja nicht alle anderen Aggregate laden kann (und auch nicht will).
  - Hier wird oft von einem "DomainService" gesprochen. Dieser Service stellt nun die Businesslogik bereit, die nicht in einem Aggregat abgebildet werden kann. Auch diese DomainServices müssen dann in die Aggregate injiziert werden, was wiederrum in einer Factory passieren müsste.
  - Ansonsten müsse der Abgleich ausgeführt werden, bevor die Daten an die entsprechende Method des Aggregates übergeben werden. Das widerspricht aber der Objektorientierung, da ich dann die Businesslogik nicht mehr im Aggregate habe.
- Wenn ein Aggregat eine Methode hat, welche mehrere Parameter hat, dann sollte man (wie immer) für bessere Lesbarkeit ein Übergabeobject schaffen
  - Dieses Objekt ist dann quasi ein "Command" und die Method der "CommandHandler"
  - Wenn ich aber rein technisch eine API habe welche bereits "CQRS"-Architektur hat, dann habe ich ja bereits ein Command-Objekt. Das bedeutet, dass ich dann eine API habe, die ein Command-Objekt an den CommandHandler übergibt, der dann ein weiteres (nicht dasselbe, vom Gefühl her) Command-Objekt an das Aggregat übergibt ... das ist nicht optimal.

### Konzept 2: CQRS und API

Beim Schreiben überkommt mich das Gefühl, dass ich mittlerweile ein tieferes Verständnis für das Konzept habe und dass ich auch die Fragen mittlerweile ziemlich sicher beantworten kann.
Was bleibt ist dieses Gefühl, dass die reine Objektorientierung mit der Kapselung im Aggregat - quasi das "Rich-Domain-Model" - nicht wirklich zu den modernen Web-APIs passt. 
- Mittels CQRS wird das ganze entzerrt und das Aggregat bildet dann nur noch den aktuellen Zustand ab.
- Die Aktionen werden dann in den Commands abgebildet.
- Die Validierung wird in den CommandValidatoren abgebildet und mittels Middleware (PipelineBehavior) durchgeführt.
- Die Businesslogik wird dann in den CommandHandlern abgebildet. 
- Die CommandHandler sind Objekte und können über DependencyInjection Services/Repositories/DbContexts injiziert bekommen mit welchen die Businesslogik ergänzt werden kann.
- Die CommandHandler verarbeiten die Commands und erzeugen Events, die dann auf das Aggregat angewendet werden und in den EventStore gespeichert werden. Die dadurch ausgelösten Projection-Updates sind dann die Sicht auf die Daten.
- Das Aggregat ist dann beschränkt auf die Anwendung der Events (Apply-Methoden) und die Darstellung des aktuellen Zustands.
- Das ist dann eher ein "Anemic Domain Model" zumindest was das Aggregat angeht.

### Erkenntnisse

- Vom Gefühl her entspricht das Konzept 2 mehr meinen Vorlieben. Ich finde "CleanArchitecture" und CQRS sehr übersichtlich und die Trennung der Verantwortlichkeiten ist sehr klar. (Ich weiß, dass es auch Nachteile gibt)
- Das Konzept 1 ist sehr objektorientiert und die Kapselung ist sehr stark.  
  Ich habe gerade überlegt, ob dieses Konzept einfacher zu testen ist, weil ich keine API-Technologien etc. brauche. Aber ich glaube tatsächlich, dass es in der Tat schwerer zu testen ist:
    - Egal welche Funktion ich teste, ich muss immer alle Abhängigkeiten für das gesamte Aggregat mitbringen. Das ist nicht trivial. Für den Test eines einzelnen CommandHandlers muss ich nur die Abhängigkeiten dieser einzelnen Aktion mitbringen. Das müsste deutlich einfacher sein.
    - Natürlich können Abhängigkeiten in beiden Konzepten gemockt werden, aber ich habe das Gefühl, dass es in Konzept 2 einfacher ist.
    - Die gerade getätigte Aussage ist nur dann richtig, wenn die übergebene Abhängigkeit sauber abgegrenzt ist - z.B. ein Repository (idealerweise Interface), welches nur die Datenbank Abfrage für z.B. die Namensüberprüfung beinhaltet (z.B. `INameRepository` mit nur der Methode `bool NameExists(string name)`).  
      Wenn ich aber ein Repository habe, welches alle Abfragen für alle Aggregate beinhaltet (z.B. `IRepository` mit der Methode `bool Exists(string name)`), dann ist das nicht mehr so einfach. Auch bei der Übergabe eines kompletten DatabaseContexts (z.B. `MyDomainDbContext`) ist das nicht mehr so einfach.  
      **Und gerade das mache ich aktuell mit EF Core gerne ... das sollte ich überdenken!**
- Ich habe mittlerweile auch Anwendungen gesehen, welche für das Aggregat gezielt `records` eingesetzt haben - daher Immutables. Jede `Apply`-Methode gibt ein neues `record` zurück, welches den neuen Zustand des Aggregates abbildet. Das ist eine interessante Idee, weil so die Daten unveränderlich sind.  
  Es ist außerdem interessant, weil so in UnitTests sehr einfach die einzelnen Apply-Methoden getestet werden können, ohne die gesamte Logik des Aggregates zu testen.
- Mir ist beim Schreiben und immer wieder Lesen aufgefallen, dass ein Punkt, der irgendwie immer für die Objektorientierung gesprochen hat, irgendwie gar nicht so wichtig ist für die API und das EventSourcing Konzept:  
  der "innere" Zustand des Aggregates und dessen Kapselung (private Felder etc.) ist generell nicht wichtig. 
  - Die API kommuniziert nach außen nur eine Projektion welche die für den Benutzer wichtigen Daten enthält.
  - Wenn ich wie gerade beschrieben ein `record` habe, welcher über sehr viele Eigenschaften verfügt, welche für die BusinessLogic gebraucht werden (z.B. Zeitstempel, oder Zustände für Rollback-Aktionen etc.),  
    dann wird dieser `record` ohnehin nicht aus der API herausgegeben.
  - Intern muss ich diesen Arbeitszustand nicht kapseln, da er nur über die Apply-Methoden (über die Events) verändert werden kann.

## Zukunft

Ich habe das Gefühl, dass ich die Bibliothek in eine neue Richtung weiterentwickeln möchte.

**Ich möchte den Einsatz von `records` forcieren und von dem "Rich Domain Model" wegkommen.**

Dies stellt eine größere Aufgabe dar, da im Prinzip, das `EventRepository` komplett umgebaut werden muss um mit `records` zu arbeiten. 
Wobei das noch nicht klar ist, vielleicht muss es auch nur mit Events umgehen. 
In irgendeiner Form müssen dann aber die Events an die Apply-Methoden übergeben werden und das in möglichst einfacher und performanter Art und Weise.

"MartenDB" ist ein Beispiel dafür wie das gelöst werden kann. Ich weiß aber nicht, ob ich die Komplexität wirklich durchdringen kann.
Ich würde mir wünschen, dass nicht zu viel Reflection nötig ist und man vielleicht was mit SourceGeneratoren machen kann, um performanter zu sein.  
*(Brain-dump: Vielleicht kann ich für jedes Aggregat ein eigenes Repository generieren, welches dann die Apply-Methoden kennt und direkt aufruft ?!)*

## Braindump

- EventStore muss überarbeitet werden, damit die Versionierung (optimistic concurrency) funktioniert:
  - Aktuelle Version des Aggregates muss schnell abgerufen werden können.
- EventRepository muss noch mehr umgebaut werden
  - Aktuelle ist bereits ein SourceGenerator im Einsatz, der die Serialisierung und Deserialisierung, sowie die Create und Apply-Methoden Aufrufe in einem Repository generiert.
  - Vielleicht sollte ich daber das Repository so umbauen, dass es soetwas wie ein ChangeTracker hat, dann kann ich mir nämlich die Veröffentlichung der "Versionsnummern" sparen.  
  Wenn diese intern geführt werden (zur Id in einem Lookup) dann ist das schöner für den Anwender.
  - Das EventRepository wollte dann `IDisposable` implementieren, damit ich die Einträge im ChangeTracker verwerfen kann.
  - Transactions im EventRepository wären auch interessant, dann funktioniert auch die In-Process-Projection sicherer!
  - Brauche ich im EventStore oder Repository eine "StartStream" Methode, die den Stream anlegt, wenn er nicht existiert? Oder reicht es, wenn ich einfach Events hinzufüge?
  - Ich kann für den EventStore einen eigenen Datenbank Lookup-Table anlegen, der die StreamId und die aktuelle Version enthält - das sollte performant sein.