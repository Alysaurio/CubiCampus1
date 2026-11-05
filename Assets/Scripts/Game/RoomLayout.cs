using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reconstruye la distribucion de la sala en tiempo de ejecucion: aula mas grande,
/// 24 mesas dobles y una fila de 4 puestos. Asi la escena antigua se actualiza
/// automaticamente sin tener que volver a generarla desde el editor.
/// </summary>
public static class RoomLayout
{
    public const int TotalTables = 24;
    public const int TableCapacity = 2;
    public const int TablesPerRow = 6;
    public const int TotalRows = 4;

    const float TableWidth = 2.75f;
    const float TableHeight = 1.45f;
    const float ColumnStep = 3.0f;
    const float RowStep = 2.3f;
    const float GridStartX = -5.5f;
    const float GridStartY = 0.65f;

    public static void Apply(GameManager game)
    {
        if (game == null) return;

        ResizeRoom();

        // Si la escena ya trae las 24 mesas con el tamano correcto, respetamos
        // las posiciones guardadas para que se puedan reacomodar desde el editor.
        if (EnsureSeats(game)) PositionSeats(game);

        EnsureQueueSlots(game);
        PositionQueue(game);
        EnsureTeacherArea();
        PositionCamera();
    }

    // ------------------------------------------------------------------ sala

    static void ResizeRoom()
    {
        GameObject piso = GameObject.Find("Piso");
        if (piso != null)
        {
            piso.transform.position = Vector3.zero;
            piso.transform.localScale = Vector3.one;
            var sr = piso.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        Hide("ParedArriba");
        Hide("ParedAbajo");
        Hide("ParedIzquierda");
        Hide("ParedDerecha");

        // La fila queda como una franja vertical a la izquierda.
        Move("ZonaFila", new Vector3(-7.95f, -0.35f, 0f), new Vector2(2.25f, 8.1f));

        // Las zonas antiguas se ocultan
        Hide("ZonaSilencio");
        Hide("ZonaDoble");
        Hide("ZonaRuido");
        Hide("MarcaSilencio");
        Hide("MarcaDoble");
        Hide("MarcaRuido");

        Hide("Estanteria0");
        Hide("Estanteria1");
        Hide("Estanteria2");
        Hide("Puerta");
        Hide("PuertaLuz");

        GameObject zf = GameObject.Find("ZonaFila");
        if (zf != null)
        {
            var srZf = zf.GetComponent<SpriteRenderer>();
            if (srZf != null) { Color c = srZf.color; c.a = 0.25f; srZf.color = c; }
        }
    }

    static void Move(string name, Vector3 pos, Vector2 size)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) return;
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    static void Hide(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) go.SetActive(false);
    }

    // ------------------------------------------------------------------ mesas

    /// <summary>
    /// Devuelve true si tuvo que reconstruir las mesas (escena antigua o tamano
    /// desactualizado). En ese caso hay que recolocarlas en la cuadricula.
    /// </summary>
    static bool EnsureSeats(GameManager game)
    {
        if (game.seats == null) game.seats = new List<Seat>();

        bool complete = game.seats.Count == TotalTables;
        if (complete)
        {
            for (int i = 0; i < game.seats.Count; i++)
            {
                Seat s = game.seats[i];
                if (s == null || s.kind != SeatKind.Doble || s.capacity != TableCapacity ||
                    !Mathf.Approximately(s.size.x, TableWidth) ||
                    !Mathf.Approximately(s.size.y, TableHeight))
                {
                    complete = false;
                    break;
                }
            }
        }

        if (complete) return false;

        // La escena antigua solo tenia 8 puestos: se retiran y se construyen
        // las 24 mesas dobles nuevas.
        for (int i = 0; i < game.seats.Count; i++)
        {
            Seat old = game.seats[i];
            if (old == null) continue;
            old.gameObject.SetActive(false);
            Object.Destroy(old.gameObject);
        }
        game.seats.Clear();

        Transform room = GameObject.Find("Sala") != null ? GameObject.Find("Sala").transform : null;

        for (int i = 0; i < TotalTables; i++)
        {
            Seat seat = CreateTable(i + 1, room);
            game.seats.Add(seat);
        }

        return true;
    }

    static Seat CreateTable(int id, Transform parent)
    {
        GameObject go = new GameObject("Mesa_" + id + "_Doble");
        if (parent != null) go.transform.SetParent(parent, false);

        Seat seat = go.AddComponent<Seat>();
        seat.seatId = id;
        seat.kind = SeatKind.Doble;
        seat.capacity = TableCapacity;
        seat.hasOutlet = false;
        seat.size = new Vector2(TableWidth, TableHeight);
        seat.scrubDistance = 2.2f;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = seat.size;

        seat.BuildVisuals();
        return seat;
    }

    static void PositionSeats(GameManager game)
    {
        int active = (game != null && game.configDia != null) ? game.configDia.activeSeats : TotalTables;
        for (int i = 0; i < game.seats.Count && i < TotalTables; i++)
        {
            Seat s = game.seats[i];
            if (s == null) continue;
            s.transform.position = ClassroomEnvironmentGenerator.GetDeskPosition(i, active);
        }
    }

    // ------------------------------------------------------------------ fila

    static void EnsureQueueSlots(GameManager game)
    {
        if (game.queue == null) return;

        Transform parent = null;
        if (game.queue.slots != null && game.queue.slots.Length > 0 && game.queue.slots[0] != null)
            parent = game.queue.slots[0].parent;

        if (parent == null)
        {
            GameObject fila = GameObject.Find("Fila");
            if (fila != null) parent = fila.transform;
        }

        if (game.queue.slots == null || game.queue.slots.Length < 4)
        {
            Transform[] old = game.queue.slots;
            Transform[] fresh = new Transform[4];

            for (int i = 0; i < fresh.Length; i++)
            {
                if (old != null && i < old.Length && old[i] != null)
                {
                    fresh[i] = old[i];
                    continue;
                }

                GameObject slot = new GameObject("SlotFila" + (i + 1));
                if (parent != null) slot.transform.SetParent(parent, false);
                PixelFactory.Quad("Marca", slot.transform, new Vector3(0f, -0.45f, 0f),
                    new Vector2(0.75f, 0.06f), new Color(0.30f, 0.38f, 0.52f, 0.8f), -12);
                fresh[i] = slot.transform;
            }

            game.queue.slots = fresh;
        }
    }

    static void PositionQueue(GameManager game)
    {
        if (game.queue == null) return;

        if (game.queue.slots != null)
        {
            for (int i = 0; i < game.queue.slots.Length; i++)
            {
                Transform slot = game.queue.slots[i];
                if (slot == null) continue;
                slot.position = new Vector3(-7.95f, 2.95f - i * 1.25f, 0f);
            }
        }

        if (game.queue.doorPoint != null)
            game.queue.doorPoint.position = new Vector3(-7.95f, -4.05f, 0f);
    }

    // ------------------------------------------------------------------ variacion por nivel

    /// <summary>
    /// Formas posibles de los grupos exclusivos. Cada Vector2Int es un
    /// desplazamiento (fila, columna): asi alternan tiradas horizontales,
    /// verticales, bloques 2x2 y formas en L.
    /// </summary>
    static readonly Vector2Int[][] GroupShapes =
    {
        new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) },
        new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) },
        new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) },
        new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1) },
        new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) },
        new[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1) },
        new[] { new Vector2Int(0, 0) },
    };

    // Tamano de grupo por nivel: empieza en 1 mesa y sube escalonado hasta 3.
    // Niveles 1-3: 1 mesa | Niveles 4-7: 2 mesas | Niveles 8-10: 3 mesas.
    static int TargetGroupSize(int dia)
    {
        if (dia <= 3) return 1;
        if (dia <= 7) return 2;
        return 3;
    }

    // Formas usadas en los ultimos niveles/reintentos. Sirve para que dos niveles
    // seguidos no generen el mismo tipo de grupo (la variedad se nota al reiniciar).
    static readonly List<int> recentShapeIndices = new List<int>();
    const int RecentShapeMemory = 8;

    public static void ConfigureLevel(GameManager game, DayConfig cfg)
    {
        if (game == null || game.seats == null || game.seats.Count == 0) return;

        int active = cfg != null ? Mathf.Clamp(cfg.activeSeats, 0, game.seats.Count) : game.seats.Count;

        // Enchufes fijos por mesa (ids 1,4,7...) y reset de grupos.
        for (int i = 0; i < game.seats.Count; i++)
        {
            Seat s = game.seats[i];
            if (s == null) continue;
            s.SetCareerGroup(false, Career.MecatronicaIndustrial);
            s.SetOutlet((s.seatId % 3) == 1);
        }

        // Limite normal: solo el 20% de las mesas activas puede formar grupos.
        // En el nivel 1 se crean entre 1 y 3 grupos pequenos de una sola mesa.
        int maxGroupsByTables = Mathf.Max(1, Mathf.FloorToInt(active * 0.2f));

        // Limite de grupos por carrera: 1 en los niveles 1-5 y 2 desde el 6.
        int groupsPerCareerMax = (game.diaActual >= 6) ? 2 : 1;
        int groups = Mathf.Min(maxGroupsByTables,
                               StudentProfile.AllCareers.Length * groupsPerCareerMax);

        if (game.diaActual <= 1)
            groups = Random.Range(1, 4); // 1 a 3 grupos en el primer nivel.

        groups = Mathf.Clamp(groups, 1,
                             Mathf.Min(active, StudentProfile.AllCareers.Length * groupsPerCareerMax));

        // Tamano de las mesas que forman cada grupo segun el nivel.
        int targetGroupSize = TargetGroupSize(game.diaActual);

        // Plan de carreras para este nivel. Se asignan una por una sin repetir:
        // primero una ronda con todas las carreras mezcladas y, solo si hiciera
        // falta (nivel 6+ con mas grupos que carreras), una segunda ronda.
        // Con 6 carreras y un maximo de 4 grupos, siempre salen distintas.
        List<Career> careerPlan = new List<Career>();
        for (int round = 0; round < groupsPerCareerMax && careerPlan.Count < groups; round++)
        {
            List<Career> roundCareers = new List<Career>(StudentProfile.AllCareers);
            Shuffle(roundCareers);

            for (int i = 0; i < roundCareers.Count && careerPlan.Count < groups; i++)
                careerPlan.Add(roundCareers[i]);
        }

        // Orden de formas para este nivel: primero las que NO se usaron en los
        // ultimos niveles, para que un reintento nunca repita el grupo anterior.
        List<int> shapeOrder = new List<int>();
        List<int> recentShapes = new List<int>();
        for (int i = 0; i < GroupShapes.Length; i++)
        {
            if (recentShapeIndices.Contains(i))
                recentShapes.Add(i);
            else
                shapeOrder.Add(i);
        }
        Shuffle(shapeOrder);
        Shuffle(recentShapes);
        shapeOrder.AddRange(recentShapes);

        // Mapa de celdas basado en la posicion real de las mesas: si el disenador
        // las reacomoda en el editor, los grupos siguen siendo contiguos.
        int rows, cols;
        Dictionary<int, int> cells = BuildCellMap(game, active, out rows, out cols);
        List<int> used = new List<int>();

        // Formas ya usadas en este nivel: ningun grupo puede repetir la misma
        // forma (disposicion de mesas) dentro del mismo nivel.
        List<int> usedShapeIndices = new List<int>();

        for (int g = 0; g < groups && g < careerPlan.Count; g++)
        {
            // Cada grupo usa una carrera distinta del plan del nivel.
            Career career = careerPlan[g];

            List<int> group = PickGroupCells(cells, rows, cols, used, shapeOrder,
                                             usedShapeIndices, targetGroupSize);
            if (group == null)
            {
                // Sin espacio para otra forma: el nivel se queda con los grupos
                // ya creados (nunca se repite una carrera para rellenar).
                break;
            }

            for (int k = 0; k < group.Count; k++)
            {
                Seat s = game.seats[group[k]];
                if (s != null) s.SetCareerGroup(true, career);
                used.Add(group[k]);
            }

            // El grupo siempre tiene al menos un enchufe para su carrera.
            Seat outlet = game.seats[group[Random.Range(0, group.Count)]];
            if (outlet != null) outlet.SetOutlet(true);
        }

        // Guarda las formas de este nivel para evitar repetirlas en el siguiente.
        for (int i = 0; i < usedShapeIndices.Count; i++)
        {
            int shape = usedShapeIndices[i];
            recentShapeIndices.Remove(shape);
            recentShapeIndices.Add(shape);
        }
        while (recentShapeIndices.Count > RecentShapeMemory)
            recentShapeIndices.RemoveAt(0);
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    /// <summary>
    /// Construye un mapa (fila, columna) -> indice de mesa usando las posiciones
    /// reales. Asi funciona igual con la cuadricula original o si se mueven las
    /// mesas manualmente en el editor.
    /// </summary>
    static Dictionary<int, int> BuildCellMap(GameManager game, int active, out int rows, out int cols)
    {
        const float TolX = 0.55f;
        const float TolY = 0.45f;

        List<float> xs = new List<float>();
        List<float> ys = new List<float>();

        for (int i = 0; i < game.seats.Count && i < active; i++)
        {
            Seat s = game.seats[i];
            if (s == null) continue;
            xs.Add(s.transform.position.x);
            ys.Add(s.transform.position.y);
        }

        List<float> uniqueX = UniqueSorted(xs, TolX, false);
        List<float> uniqueY = UniqueSorted(ys, TolY, true);

        cols = Mathf.Max(1, uniqueX.Count);
        rows = Mathf.Max(1, uniqueY.Count);

        Dictionary<int, int> cells = new Dictionary<int, int>();

        for (int i = 0; i < game.seats.Count && i < active; i++)
        {
            Seat s = game.seats[i];
            if (s == null) continue;

            int col = RankOf(uniqueX, s.transform.position.x, TolX);
            int row = RankOf(uniqueY, s.transform.position.y, TolY);
            if (col < 0 || row < 0) continue;

            int key = row * cols + col;
            if (!cells.ContainsKey(key)) cells.Add(key, i);
        }

        return cells;
    }

    static List<float> UniqueSorted(List<float> values, float tolerance, bool descending)
    {
        values.Sort();
        if (descending) values.Reverse();

        List<float> unique = new List<float>();
        for (int i = 0; i < values.Count; i++)
        {
            if (unique.Count == 0 || Mathf.Abs(unique[unique.Count - 1] - values[i]) > tolerance)
                unique.Add(values[i]);
        }
        return unique;
    }

    static int RankOf(List<float> unique, float value, float tolerance)
    {
        for (int i = 0; i < unique.Count; i++)
        {
            if (Mathf.Abs(unique[i] - value) <= tolerance) return i;
        }
        return -1;
    }

    static List<int> PickGroupCells(Dictionary<int, int> cells, int rows, int cols, List<int> used,
                                    List<int> shapeOrder, List<int> usedShapeIndices,
                                    int targetSize)
    {
        // 1) Formas del tamano del nivel que aun NO se usaron en este nivel.
        for (int i = 0; i < shapeOrder.Count; i++)
        {
            int shapeIdx = shapeOrder[i];
            if (GroupShapes[shapeIdx].Length != targetSize) continue;
            if (usedShapeIndices.Contains(shapeIdx)) continue;

            List<List<int>> candidates = ShapeCandidates(GroupShapes[shapeIdx], cells, rows, cols, used);
            if (candidates.Count == 0) continue;

            usedShapeIndices.Add(shapeIdx);
            return candidates[Random.Range(0, candidates.Count)];
        }

        // 2) Si hay mas grupos que formas disponibles (por ejemplo, grupos de
        // una sola mesa), se permite repetir la forma; la carrera sigue siendo
        // distinta para cada grupo.
        for (int i = 0; i < shapeOrder.Count; i++)
        {
            int shapeIdx = shapeOrder[i];
            if (GroupShapes[shapeIdx].Length != targetSize) continue;

            List<List<int>> candidates = ShapeCandidates(GroupShapes[shapeIdx], cells, rows, cols, used);
            if (candidates.Count == 0) continue;

            if (!usedShapeIndices.Contains(shapeIdx)) usedShapeIndices.Add(shapeIdx);
            return candidates[Random.Range(0, candidates.Count)];
        }

        return null;
    }

    static List<List<int>> ShapeCandidates(Vector2Int[] shape, Dictionary<int, int> cells,
                                           int rows, int cols, List<int> used)
    {
        int maxRow = 0;
        int maxCol = 0;
        for (int i = 0; i < shape.Length; i++)
        {
            if (shape[i].x > maxRow) maxRow = shape[i].x;
            if (shape[i].y > maxCol) maxCol = shape[i].y;
        }

        List<List<int>> result = new List<List<int>>();

        for (int r = 0; r + maxRow < rows; r++)
        {
            for (int c = 0; c + maxCol < cols; c++)
            {
                List<int> group = new List<int>(shape.Length);
                bool ok = true;

                for (int i = 0; i < shape.Length; i++)
                {
                    int key = (r + shape[i].x) * cols + (c + shape[i].y);
                    int seatIndex;

                    if (!cells.TryGetValue(key, out seatIndex) || used.Contains(seatIndex))
                    {
                        ok = false;
                        break;
                    }

                    group.Add(seatIndex);
                }

                if (ok) result.Add(group);
            }
        }

        return result;
    }

    // ------------------------------------------------------------------ profesor

    static void EnsureTeacherArea()
    {
        GameObject desk = GameObject.Find("MesaProfesor");
        if (desk == null)
        {
            desk = new GameObject("MesaProfesor");
            SpriteRenderer sr = desk.AddComponent<SpriteRenderer>();
            sr.sprite = PixelFactory.White;
            sr.color = new Color(0.36f, 0.24f, 0.14f);
            sr.sortingOrder = 2;
            desk.transform.position = new Vector3(2.4f, 2.7f, 0f);
            desk.transform.localScale = new Vector3(2.75f, 1.0f, 1f);
        }

        GameObject teacher = GameObject.Find("Profesor");
        if (teacher == null)
        {
            teacher = new GameObject("Profesor");
            SpriteRenderer sr = teacher.AddComponent<SpriteRenderer>();
            sr.sprite = PixelFactory.White;
            sr.color = new Color(0.25f, 0.55f, 0.85f);
            sr.sortingOrder = 14;
            teacher.transform.position = new Vector3(3.75f, 2.7f, 0f);
            teacher.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        }
    }

    static void PositionCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.orthographic = true;
        cam.orthographicSize = 6.55f;
        cam.transform.position = new Vector3(0.45f, -0.2f, -10f);
    }
}
