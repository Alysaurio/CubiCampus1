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
    const float ColumnStep = 2.86f;
    const float RowStep = 1.8f;
    const float GridStartX = -5.53f;
    const float GridStartY = 3.02f;

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
        Move("Piso", new Vector3(0.5f, -0.2f, 0f), new Vector2(19.8f, 9.3f));
        Move("ParedArriba", new Vector3(0.5f, 4.45f, 0f), new Vector2(20.2f, 0.25f));
        Move("ParedAbajo", new Vector3(0.5f, -4.85f, 0f), new Vector2(20.2f, 0.25f));
        Move("ParedIzquierda", new Vector3(-9.35f, -0.2f, 0f), new Vector2(0.25f, 9.6f));
        Move("ParedDerecha", new Vector3(10.35f, -0.2f, 0f), new Vector2(0.25f, 9.6f));

        // La fila queda como una franja vertical a la izquierda.
        Move("ZonaFila", new Vector3(-7.95f, -0.35f, 0f), new Vector2(2.25f, 8.1f));

        // Las zonas antiguas (silencio/dobles/ruido) ya no existen como mechanic:
        // se ocultan para que la sala se lea solo por carreras.
        Hide("ZonaSilencio");
        Hide("ZonaDoble");
        Hide("ZonaRuido");
        Hide("MarcaSilencio");
        Hide("MarcaDoble");
        Hide("MarcaRuido");

        // Estanterias al fondo superior, para que no tapen las mesas ni la mesa
        // del profesor.
        Move("Estanteria0", new Vector3(1.2f, 4.08f, 0f), new Vector2(2.4f, 0.42f));
        Move("Estanteria1", new Vector3(4.2f, 4.08f, 0f), new Vector2(2.4f, 0.42f));
        Move("Estanteria2", new Vector3(7.2f, 4.08f, 0f), new Vector2(2.4f, 0.42f));

        Move("Puerta", new Vector3(-7.95f, -4.52f, 0f), new Vector2(1.9f, 0.40f));
        Move("PuertaLuz", new Vector3(-7.95f, -4.28f, 0f), new Vector2(1.4f, 0.12f));
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
        for (int i = 0; i < game.seats.Count && i < TotalTables; i++)
        {
            Seat s = game.seats[i];
            if (s == null) continue;

            int row = i / TablesPerRow;
            int col = i % TablesPerRow;

            s.transform.position = new Vector3(
                GridStartX + col * ColumnStep,
                GridStartY - row * RowStep,
                0f);
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
    };

    // Formas usadas en los ultimos niveles/reintentos. Sirve para que dos niveles
    // seguidos no generen el mismo tipo de grupo (la variedad se nota al reiniciar).
    static readonly List<int> recentShapeIndices = new List<int>();
    const int RecentShapeMemory = 4;

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

        // Desde el primer dia hay al menos 2 grupos para que se note la variedad;
        // en los dias con mas mesas se llega a 3 grupos.
        int groups = Mathf.Clamp(Mathf.CeilToInt(active / 7f), 1, 3);

        List<Career> careers = new List<Career>(StudentProfile.AllCareers);
        for (int i = careers.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Career tmp = careers[i];
            careers[i] = careers[j];
            careers[j] = tmp;
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

        // Formas y tamanos ya usados en este nivel: ningun grupo puede repetir
        // forma ni cantidad de mesas si hay alternativas disponibles.
        List<int> usedShapeIndices = new List<int>();
        List<int> usedSizes = new List<int>();

        for (int g = 0; g < groups; g++)
        {
            List<int> group = PickGroupCells(cells, rows, cols, used, shapeOrder,
                                             usedShapeIndices, usedSizes);
            if (group == null) break;

            Career career = careers[g % careers.Count];
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

    static void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = list[i];
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
                                    List<int> usedSizes)
    {
        // 1) Forma libre en el nivel y con tamano distinto a los grupos ya creados.
        for (int i = 0; i < shapeOrder.Count; i++)
        {
            int shapeIdx = shapeOrder[i];
            if (usedShapeIndices.Contains(shapeIdx)) continue;

            int size = GroupShapes[shapeIdx].Length;
            if (usedSizes.Contains(size)) continue;

            List<List<int>> candidates = ShapeCandidates(GroupShapes[shapeIdx], cells, rows, cols, used);
            if (candidates.Count == 0) continue;

            usedShapeIndices.Add(shapeIdx);
            usedSizes.Add(size);
            return candidates[Random.Range(0, candidates.Count)];
        }

        // 2) Si no queda un tamano distinto, cualquier forma que no se haya usado.
        for (int i = 0; i < shapeOrder.Count; i++)
        {
            int shapeIdx = shapeOrder[i];
            if (usedShapeIndices.Contains(shapeIdx)) continue;

            List<List<int>> candidates = ShapeCandidates(GroupShapes[shapeIdx], cells, rows, cols, used);
            if (candidates.Count == 0) continue;

            usedShapeIndices.Add(shapeIdx);
            usedSizes.Add(GroupShapes[shapeIdx].Length);
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
            desk.transform.position = new Vector3(3.6f, -4.13f, 0f);
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
            teacher.transform.position = new Vector3(3.6f, -3.62f, 0f);
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
