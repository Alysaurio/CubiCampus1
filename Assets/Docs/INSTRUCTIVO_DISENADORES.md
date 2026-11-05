# Instructivo para Diseñadores - CubiCampus

## 1) Editar Estudiantes con Prefab

Los estudiantes ahora se pueden editar con un Prefab sin tocar código.

### Crear el prefab
1. Abre la escena `GameScene`.
2. En Hierarchy, crea o copia un objeto `Estudiante_*` (o crea uno vacío).
3. Asegúrate de que tenga el componente `Student`.
4. Arrástralo a `Assets/Prefabs/` para convertirlo en Prefab.
5. Borra la instancia de la escena.

### Asignarlo en el juego
1. Selecciona el objeto `QueueManager` en `GameScene`.
2. En el componente `QueueManager`, arrastra el prefab `Estudiante` al campo `Estudiante Prefab`.
3. Listo. Ahora todos los alumnos usarán ese prefab.

### Qué se puede editar
- **Visual**: Colores, formas, iconos, barra de estado, etc. (Todo lo que esté en `Visual`)
- **Tamaños/posiciones**: Ajusta los hijos del prefab.
- **Collider**: Modifica `BoxCollider2D` si lo necesitas.

> Si el prefab NO está asignado, el juego seguirá usando el sistema geométrico actual (fallback).

---

## 2) Editar Sonidos sin tocar código

Los sonidos ahora se gestionan con un `AudioConfig` (`ScriptableObject`).

### Crear el AudioConfig
1. Ve a `Assets/Resources/Audio/`
2. Click derecho > `Create > CubiCampus > Audio > Config`
3. Guárdalo como `AudioConfig.asset`

### Asignar sonidos
1. Selecciona `AudioConfig.asset`
2. En `Entradas`, pulsa `+` para añadir un nuevo elemento
3. Configura:
   - `Tipo`: Selecciona el efecto (UiClick, Pickup, Pack, Assign, Happy, Attention, Distraction, Abandon, EarlyLeave, Error, Combo, Bono, Apartado, Rush, Tick, Whistle, Clean, Victory, Defeat, DayStart)
   - `Clip`: Arrastra tu archivo `.wav`/`.mp3`/`.ogg`
   - `Volumen`: Ajusta el volumen relativo
   - `Pitch`: Tono base
   - `Usar Pitch Aleatorio`: Marca para dar variación natural
   - `Variacion Pitch`: Cuánta variación aplicar

### Música (opcional)
También puedes asignar:
- `Musica Menu`
- `Musica Juego`
- `Musica Resultados`

> **Prioridad:** Si hay un `AudioClip` asignado para ese `Sfx` en el `AudioConfig`, se usará ese. Si no hay ninguno, se seguirá usando el sonido procedural generado por código.

---

---

## 3) Enchufes, grupos exclusivos y mesa del profesor

### Enchufes
- Algunas mesas tienen un **rayo amarillo**: son sitios con cargador.
- Algunos alumnos llegan con un icono de **bateria** (bateria baja). Si los sientas en una mesa sin enchufe pierden eficiencia y estudian mas lento, asi que conviene llevarlos a una mesa con rayo.
- Puedes cambiar cuantas mesas tienen enchufe en `RoomLayout.ConfigureLevel` (patron `seatId % 3 == 1`).

### Grupos exclusivos de carrera
- En cada nivel aparecen pequenos grupos de 2-3 mesas marcadas con una **franja del color de una carrera** y su codigo (MEC, AUT, MKT, MIN, SST o SIM).
- Esas mesas son exclusivas de esa carrera: si sientas a otra carrera habra pelea y bajara la eficiencia.
- La seleccion de grupos y carreras se sortea al cargar la escena, asi que **cambia en cada nivel y en cada reintento**.

### Mesa del profesor y sprites reemplazables
- La sala incluye `MesaProfesor` (rectangulo marron) y `Profesor` (cuadrado azul) como **placeholders**.
- Para que existan de forma persistente en la escena, ejecuta `Tools > CubiCampus > Agregar Mesa del Profesor` (o reconstruye la escena con `Tools > CubiCampus > Rebuild Game Scene`).
- Luego selecciona `MesaProfesor` o `Profesor` en la Hierarchy y arrastra tu sprite al campo `Sprite` del `SpriteRenderer`. El juego respeta esos objetos y no los reemplaza al iniciar.
- Si tu sprite tiene otra escala, ajusta `Transform > Scale` del objeto (`MesaProfesor` usa 2.6 x 1.0 y `Profesor` 0.7 x 0.7).

---

## 4) Capas visuales, resaltado y colores de carrera

### Orden de dibujado
Los ordenes de renderizado estan centralizados en `Assets/Scripts/Game/RenderOrder.cs` para que el gameplay quede siempre por encima de la decoracion:

| Elemento | Orden |
| --- | --- |
| Suelo | -20 |
| Zocalos / molduras | -18 |
| Decoracion de pared | -16 |
| Props / plantas | -14 |
| Mesa (superficie) | 0 |
| Borde y silla | 2 |
| Cuerpo de la mesa | 3 |
| Marcador de carrera | 7 |
| Alumno sentado | 8 |
| Alumno de pie | 20 |
| Alumno mientras se arrastra | 60 |

Si agregas un objeto nuevo, usa `RenderOrder` en vez de escribir el numero a mano. Asi el alumno sentado se ve sobre la mesa y la silla, y el que arrastras queda visible por encima de todo.

### Resaltado al arrastrar
Al pasar un alumno por encima de una mesa compatible, el sprite de la mesa (`VisualMesa`) hace una respiracion suave (escala 1.00x a 1.05x) y se tine de verde si la carrera coincide o de amarillo si la mesa es valida pero no ideal. Ya no se dibuja el rectangulo grande `HighlightDrag`; el efecto usa el propio sprite de la mesa. Se controla desde `Seat.SetDragHighlight(valid, ideal)` y se limpia con `Seat.ClearDragHighlight()`.

### Color por carrera
Cada carrera tiene un color fijo en `StudentProfile.ColorOf` y un codigo corto (`MEC`, `AUT`, `MKT`, `MIN`, `SST`, `SIM`). Ese color se usa en:
- La franja y el texto de los grupos exclusivos de mesa.
- La etiqueta flotante del alumno.
- La insignia `CarreraFondo` que aparece encima del alumno.

Para cambiar un color, edita `StudentProfile.ColorOf` y mantenlo coherente con la franja de los grupos.

### Suelo y luces 2D
El suelo generado usa `ClassroomEnvironmentGenerator.floorTint` (gris violaceo semitransparente) para dar contraste con las mesas. Los sprites generados reciben `Sprite-Lit-Default` de URP en `ClassroomEnvironmentGenerator.spriteLitMaterial`; si necesitas que una luz 2D afecte un objeto nuevo, asignale ese material.

---

## Consejos
- Usa `Resources/Audio/AudioConfig.asset` con ese nombre exacto para que se cargue automáticamente.
- Guarda los prefabs en `Assets/Prefabs/`
- Los `ScriptableObject` de audio van en `Assets/Resources/Audio/`
- Cualquier cambio en el prefab o en el AudioConfig se aplica al instante sin recompilar.
