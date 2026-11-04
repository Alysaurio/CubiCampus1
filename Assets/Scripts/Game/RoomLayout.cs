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

    const float TableWidth = 2.6f;
    const float TableHeight = 1.25f;
    const float ColumnStep = 2.85f;
    const float RowStep = 1.8f;
    const float GridStartX = -5.45f;
    const float GridStartY = 3.0f;

    public static void Apply(GameManager game)
    {
        if (game == null) return;

        ResizeRoom();
        EnsureSeats(game);
        PositionSeats(game);
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

    static void EnsureSeats(GameManager game)
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

        if (complete) return;

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
        seat.scrubDistance = 2f;

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
    /// Distribuye enchufes y elige pequenos grupos exclusivos de una carrera.
    /// Se llama al aplicar la configuracion del dia; como la escena se recarga
    /// en cada reintento, la seleccion cambia en cada nivel y cada reintento.
    /// </summary>
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

        // 1 grupo en los primeros dias, hasta 3 grupos con las 24 mesas.
        int groups = Mathf.Clamp(Mathf.CeilToInt(active / 10f), 1, 3);

        List<Career> careers = new List<Career>(StudentProfile.AllCareers);
        for (int i = careers.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Career tmp = careers[i];
            careers[i] = careers[j];
            careers[j] = tmp;
        }

        List<int> used = new List<int>();

        for (int g = 0; g < groups; g++)
        {
            int size = Random.Range(2, 4); // 2 o 3 mesas
            int start = FindGroupStart(active, size, used);
            if (start < 0 && size > 2)
            {
                size = 2;
                start = FindGroupStart(active, size, used);
            }
            if (start < 0) break;

            Career career = careers[g % careers.Count];
            for (int k = 0; k < size; k++)
            {
                Seat s = game.seats[start + k];
                if (s != null) s.SetCareerGroup(true, career);
                used.Add(start + k);
            }

            // El grupo siempre tiene al menos un enchufe para su carrera.
            Seat middle = game.seats[start + size / 2];
            if (middle != null) middle.SetOutlet(true);
        }
    }

    static int FindGroupStart(int active, int size, List<int> used)
    {
        List<int> candidates = new List<int>();

        for (int i = 0; i + size - 1 < active; i++)
        {
            int row = i / TablesPerRow;
            if ((i + size - 1) / TablesPerRow != row) continue;

            bool free = true;
            for (int k = 0; k < size; k++)
            {
                if (used.Contains(i + k)) { free = false; break; }
            }
            if (free) candidates.Add(i);
        }

        if (candidates.Count == 0) return -1;
        return candidates[Random.Range(0, candidates.Count)];
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
            desk.transform.position = new Vector3(3.6f, -4.08f, 0f);
            desk.transform.localScale = new Vector3(2.6f, 1.0f, 1f);
        }

        GameObject teacher = GameObject.Find("Profesor");
        if (teacher == null)
        {
            teacher = new GameObject("Profesor");
            SpriteRenderer sr = teacher.AddComponent<SpriteRenderer>();
            sr.sprite = PixelFactory.White;
            sr.color = new Color(0.25f, 0.55f, 0.85f);
            sr.sortingOrder = 14;
            teacher.transform.position = new Vector3(3.6f, -3.55f, 0f);
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
