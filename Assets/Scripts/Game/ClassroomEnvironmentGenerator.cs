using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Generador del entorno visual del aula (piso, paredes, molduras y decoraciones)
/// y reestructurador visual de las mesas dobles con arte pixel art de Cozy Classroom.
/// </summary>
[ExecuteAlways]
public class ClassroomEnvironmentGenerator : MonoBehaviour
{
    [Header("Sprites de Pared (Colgados en la pared superior)")]
    public Sprite schoolBoard;
    public Sprite schoolWindow;
    public Sprite classroomMap;
    public Sprite pinBoard;
    public Sprite wallClock;

    [Header("Sprites Apoyados (Base de pared / primer fila de piso)")]
    public Sprite classroomDoor;
    public Sprite bookCupboard;
    public Sprite classroomBookshelf;
    public Sprite bigPlant;
    public Sprite classroomPlant;
    public Sprite bin;

    [Header("Sprites de Mesa y Sillas")]
    public Sprite mainDesk;
    public Sprite mesa1;
    public Sprite mesa2;
    public Sprite[] sillas;

    [Header("Sprites de Piso, Pared y Trim")]
    public Sprite classroomWall;
    public Sprite classroomFloor;
    public Sprite trimTop;
    public Sprite trimBottom;
    public Sprite trimDownLeft;
    public Sprite trimDownRight;
    public Sprite trimCornerDownLeft;
    public Sprite trimCornerDownRight;
    public Sprite trimCornerUpLeft;
    public Sprite trimCornerUpRight;

    [Header("Configuración de Cuadrícula (Ampliación 1 unidad a ambos lados)")]
    public int minCol = -11;
    public int maxCol = 12;
    public float wallCenterY = 3.5f;
    public int floorRows = 7;
    public float floorStartY = 2.0f;

    [Header("Material y tinte de piso")]
    [Tooltip("Material Sprite-Lit-Default. Se asigna a los sprites creados en runtime para que la luz 2D funcione igual en editor y build.")]
    public Material spriteLitMaterial;
    [Tooltip("Tinte del suelo: mas oscuro y algo transparente para dar contraste a mesas y alumnos.")]
    public Color floorTint = new Color(0.72f, 0.70f, 0.74f, 0.90f);

    [Header("Iluminación 2D URP")]
    public Color globalLightColor = new Color(1.0f, 0.957f, 0.902f, 1.0f); // #FFF4E6
    [Range(0.1f, 2.0f)] public float globalLightIntensity = 0.68f;
    public Color windowLightColor = new Color(1.0f, 0.875f, 0.557f, 1.0f); // #FFDF8E
    [Range(0.5f, 3.0f)] public float windowLightIntensity = 1.15f;
    [Range(60f, 180f)] public float windowLightOuterAngle = 120f;
    [Range(10f, 90f)] public float windowLightInnerAngle = 50f;
    [Range(5f, 30f)] public float windowLightOuterRadius = 19f;
    [Range(0f, 1f)] public float windowLightFalloff = 0.65f;
    [Range(0f, 1f)] public float windowShadowIntensity = 0.45f;
    public float windowLightRotationZ = -140f;

    void Awake()
    {
        if (Application.isPlaying)
        {
            GenerateEnvironment();
        }
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            int active = GetCurrentActiveSeats();
            AutoDistributeTables(active);
            DecorateTables();
        }
    }

    int GetCurrentActiveSeats()
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null && gm.configDia != null)
        {
            return gm.configDia.activeSeats;
        }
        return 8; // Predeterminado nivel 1
    }

    [ContextMenu("Generate Environment")]
    public void GenerateEnvironment()
    {
#if UNITY_EDITOR
        EnsureSpritesLoaded();
#endif
        SetupFloorAndWalls();
        SetupTrimPerimeter();
        SetupDecorativeFurniture();
        int active = Application.isPlaying ? GetCurrentActiveSeats() : 8;
        AutoDistributeTables(active);
        DecorateTables();
        SetupLighting2D();
        SetupShadowCasters();
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Load Sprites")]
    public void EnsureSpritesLoaded()
    {
        string baseDir = "Assets/Sprites/Cozy Classroom — FREE Pixel Art Pack/";
        if (schoolBoard == null) schoolBoard = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "school_bored.png");
        if (schoolWindow == null) schoolWindow = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "school_window.png");
        if (classroomMap == null) classroomMap = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "classroom_map.png");
        if (pinBoard == null) pinBoard = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "pin_bored.png");
        if (wallClock == null) wallClock = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "wall_clock.png");

        if (classroomDoor == null) classroomDoor = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "classroom_door.png");
        if (bookCupboard == null) bookCupboard = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "book_cupboard.png");
        if (classroomBookshelf == null) classroomBookshelf = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "classroom_book_unit.png");
        if (bigPlant == null) bigPlant = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "big_plant.png");
        if (classroomPlant == null) classroomPlant = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "classroom_plant.png");
        if (bin == null) bin = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "bin.png");

        if (mainDesk == null) mainDesk = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "main_desk.png");
        if (mesa1 == null) mesa1 = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "Mesa1.png");
        if (mesa2 == null) mesa2 = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "Mesa2.png");

        if (sillas == null || sillas.Length == 0 || sillas[0] == null)
        {
            sillas = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "Silla1.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "Silla2.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "Silla3.png")
            };
        }

        if (classroomWall == null) classroomWall = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "Classroom_wall.png");
        if (classroomFloor == null) classroomFloor = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "Classroom_floor.png");

        if (spriteLitMaterial == null)
            spriteLitMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");

        string trimDir = baseDir + "Trim/";
        if (trimTop == null) trimTop = AssetDatabase.LoadAssetAtPath<Sprite>(trimDir + "spr_wood_trim_top.png");
        if (trimBottom == null) trimBottom = AssetDatabase.LoadAssetAtPath<Sprite>(trimDir + "spr_wood_trim_bottom.png");
        if (trimDownLeft == null) trimDownLeft = AssetDatabase.LoadAssetAtPath<Sprite>(trimDir + "spr_wood_trim_down_left.png");
        if (trimDownRight == null) trimDownRight = AssetDatabase.LoadAssetAtPath<Sprite>(trimDir + "spr_wood_trim_down_right.png");
        if (trimCornerDownLeft == null) trimCornerDownLeft = AssetDatabase.LoadAssetAtPath<Sprite>(trimDir + "spr_wood_trim_c_down_left.png");
        if (trimCornerDownRight == null) trimCornerDownRight = AssetDatabase.LoadAssetAtPath<Sprite>(trimDir + "spr_wood_trim_c_down_right.png");
        if (trimCornerUpLeft == null) trimCornerUpLeft = AssetDatabase.LoadAssetAtPath<Sprite>(trimDir + "spr_wood_trim_c_up_left.png");
        if (trimCornerUpRight == null) trimCornerUpRight = AssetDatabase.LoadAssetAtPath<Sprite>(trimDir + "spr_wood_trim_c_up_right.png");
    }
#endif

    // ------------------------------------------------------------------ TAREA 1: PISO Y PARED (AMPLIADO)
    public void SetupFloorAndWalls()
    {
        Transform pisoT = transform.Find("Piso");
        if (pisoT == null)
        {
            GameObject pGO = new GameObject("Piso");
            pGO.transform.SetParent(transform, false);
            pisoT = pGO.transform;
        }

        pisoT.localPosition = Vector3.zero;
        pisoT.localScale = Vector3.one;

        SpriteRenderer originalSr = pisoT.GetComponent<SpriteRenderer>();
        if (originalSr != null) originalSr.enabled = false;

        // Desactivar paredes antiguas
        string[] oldWalls = { "ParedArriba", "ParedAbajo", "ParedIzquierda", "ParedDerecha" };
        foreach (string w in oldWalls)
        {
            Transform t = transform.Find(w);
            if (t != null)
            {
                var sr = t.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
                t.gameObject.SetActive(false);
            }
        }

        Transform gridRoot = pisoT.Find("CuadriculaPisoPared");
        if (gridRoot == null)
        {
            GameObject gridGO = new GameObject("CuadriculaPisoPared");
            gridGO.transform.SetParent(pisoT, false);
            gridRoot = gridGO.transform;
        }
        else
        {
            ClearChildren(gridRoot);
        }

        // 1. Dos filas superiores: Classroom_wall (32x64 px -> 1x2 unidades)
        for (int col = minCol; col <= maxCol; col++)
        {
            GameObject wallGO = new GameObject($"Wall_{col}");
            wallGO.transform.SetParent(gridRoot, false);
            wallGO.transform.localPosition = new Vector3(col, wallCenterY, 0f);
            wallGO.transform.localScale = Vector3.one;

            SpriteRenderer sr = wallGO.AddComponent<SpriteRenderer>();
            sr.sprite = classroomWall;
            sr.sortingOrder = RenderOrder.Floor;
            ApplyLitMaterial(sr);
        }

        // 2. Filas inferiores: Classroom_floor (32x32 px -> 1x1 unidad)
        for (int r = 0; r < floorRows; r++)
        {
            float y = floorStartY - r * 1.0f;
            for (int col = minCol; col <= maxCol; col++)
            {
                GameObject floorGO = new GameObject($"Floor_{col}_{r}");
                floorGO.transform.SetParent(gridRoot, false);
                floorGO.transform.localPosition = new Vector3(col, y, 0f);
                floorGO.transform.localScale = Vector3.one;

                SpriteRenderer sr = floorGO.AddComponent<SpriteRenderer>();
                sr.sprite = classroomFloor;
                sr.color = floorTint;
                sr.sortingOrder = RenderOrder.Floor;
                ApplyLitMaterial(sr);
            }
        }
    }

    // ------------------------------------------------------------------ TAREA 2: TRIM PERIMETRAL (AMPLIADO)
    public void SetupTrimPerimeter()
    {
        Transform trimRoot = transform.Find("BordesTrim");
        if (trimRoot == null)
        {
            GameObject trimGO = new GameObject("BordesTrim");
            trimGO.transform.SetParent(transform, false);
            trimRoot = trimGO.transform;
        }
        else
        {
            ClearChildren(trimRoot);
        }

        int order = RenderOrder.Trim;

        // 4 Esquinas
        CreateTrimTile("Trim_C_Down_Left", trimRoot, new Vector3(minCol, 4.0f, 0f), trimCornerDownLeft, order);
        CreateTrimTile("Trim_C_Down_Right", trimRoot, new Vector3(maxCol, 4.0f, 0f), trimCornerDownRight, order);
        CreateTrimTile("Trim_C_Up_Left", trimRoot, new Vector3(minCol, -4.0f, 0f), trimCornerUpLeft, order);
        CreateTrimTile("Trim_C_Up_Right", trimRoot, new Vector3(maxCol, -4.0f, 0f), trimCornerUpRight, order);

        // Borde Superior
        for (int col = minCol + 1; col < maxCol; col++)
        {
            CreateTrimTile($"Trim_Top_{col}", trimRoot, new Vector3(col, 4.0f, 0f), trimTop, order);
        }

        // Borde Inferior
        for (int col = minCol + 1; col < maxCol; col++)
        {
            CreateTrimTile($"Trim_Bottom_{col}", trimRoot, new Vector3(col, -4.0f, 0f), trimBottom, order);
        }

        // Bordes Laterales
        for (int y = -3; y <= 3; y++)
        {
            CreateTrimTile($"Trim_Left_{y}", trimRoot, new Vector3(minCol, y, 0f), trimDownLeft, order);
            CreateTrimTile($"Trim_Right_{y}", trimRoot, new Vector3(maxCol, y, 0f), trimDownRight, order);
        }
    }

    void CreateTrimTile(string name, Transform parent, Vector3 localPos, Sprite sprite, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        ApplyLitMaterial(sr);
    }

    Material GetSpriteLitMaterial()
    {
        if (spriteLitMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader != null)
            {
                spriteLitMaterial = new Material(shader);
                spriteLitMaterial.hideFlags = HideFlags.DontSave;
            }
        }
        return spriteLitMaterial;
    }

    void ApplyLitMaterial(SpriteRenderer sr)
    {
        if (sr == null) return;
        Material mat = GetSpriteLitMaterial();
        if (mat != null) sr.sharedMaterial = mat;
    }

    // ------------------------------------------------------------------ TAREA 3: PUERTA, PROFESOR, PIZARRA Y DECORACIONES
    public void SetupDecorativeFurniture()
    {
        // Ocultar placeholders antiguos
        string[] oldDecor = { "Estanteria0", "Estanteria1", "Estanteria2", "Puerta", "PuertaLuz" };
        foreach (string d in oldDecor)
        {
            Transform t = transform.Find(d);
            if (t != null)
            {
                var sr = t.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
                t.gameObject.SetActive(false);
            }
        }

        // Configurar ZonaFila: posicion x = -8, y = -1, escala y = 6.9, transparencia Alpha = 0.25
        GameObject zf = GameObject.Find("ZonaFila");
        if (zf != null)
        {
            Vector3 lp = zf.transform.localPosition;
            lp.x = -8.0f;
            lp.y = -1.0f;
            zf.transform.localPosition = lp;

            Vector3 ls = zf.transform.localScale;
            ls.y = 6.9f;
            zf.transform.localScale = ls;

            SpriteRenderer srZf = zf.GetComponent<SpriteRenderer>();
            if (srZf != null)
            {
                Color c = srZf.color;
                c.a = 0.25f;
                srZf.color = c;
            }
        }

        Transform decorRoot = transform.Find("DecoracionSuperior");
        if (decorRoot == null)
        {
            GameObject rootGO = new GameObject("DecoracionSuperior");
            rootGO.transform.SetParent(transform, false);
            decorRoot = rootGO.transform;
        }
        else
        {
            ClearChildren(decorRoot);
        }

        // 1. PUERTA EN POSICIÓN EXACTA X = -8 (sobre la base de la pared en Y = 3.5)
        CreateProp("Door", decorRoot, new Vector3(-8.0f, 3.5f, 0f), classroomDoor, -14);

        // ZONA DE EXCLUSION ALREDEDOR DE LA PUERTA: entre X = -9.2f y X = -6.8f.
        // NINGUN otro objeto se coloca en este rango.

        // Objeto en esquina extrema izquierda (X <= -9.5):
        CreateProp("BigPlant", decorRoot, new Vector3(-10.0f, 3.5f, 0f), bigPlant, -14);

        // 2. PARED IZQUIERDA (entre zona exclusion y pizarra, X entre -6.5 y -2.2):
        CreateProp("SchoolWindow", decorRoot, new Vector3(-4.8f, 3.5f, 0f), schoolWindow, -16);
        CreateProp("Bin", decorRoot, new Vector3(-3.0f, 3.0f, 0f), bin, -14);

        // 3. PIZARRA Y PROFESOR (ZONA SUPERIOR)
        // Pizarra escolar centrada en X = 0.0, Y = 3.5
        float boardX = 0.0f;
        CreateProp("SchoolBoard", decorRoot, new Vector3(boardX, 3.5f, 0f), schoolBoard, -16);

        // Reloj colgado alto cerca de la pizarra (X = -1.6f, Y = 3.8f)
        CreateProp("WallClock", decorRoot, new Vector3(-1.6f, 3.8f, 0f), wallClock, -16);

        // MesaProfesor (main_desk) en Y = 2.7, horizontalmente cerca de la pizarra en X = 2.4
        float deskX = 2.4f;
        float deskY = 2.7f;
        SetupTeacherArea(deskX, deskY);

        // REGLA DE DESPEJE: Entre X = -1.5 y X = 4.5 NO se genera ningun sprite decorativo extra.
        // Los unicos objetos aqui son SchoolBoard, WallClock, MesaProfesor y Profesor.
        // Nota: Se elimino por completo el uso de "school_books".

        // 4. PARED DERECHA (X >= 4.8 hasta 11.5) con segunda ventana
        CreateProp("ClassroomPlant", decorRoot, new Vector3(5.0f, 3.25f, 0f), classroomPlant, -14);
        CreateProp("ClassroomMap", decorRoot, new Vector3(6.5f, 3.5f, 0f), classroomMap, -16);
        CreateProp("SchoolWindow2", decorRoot, new Vector3(8.0f, 3.5f, 0f), schoolWindow, -16);
        CreateProp("PinBoard", decorRoot, new Vector3(9.5f, 3.5f, 0f), pinBoard, -16);
        CreateProp("BookCupboard", decorRoot, new Vector3(11.0f, 3.5f, 0f), bookCupboard, -14);
    }

    void CreateProp(string name, Transform parent, Vector3 localPos, Sprite sprite, int order)
    {
        if (sprite == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        ApplyLitMaterial(sr);
    }

    void SetupTeacherArea(float deskX, float deskY)
    {
        Transform deskT = transform.Find("MesaProfesor");
        if (deskT != null)
        {
            deskT.localPosition = new Vector3(deskX, deskY, 0f);
            deskT.localScale = Vector3.one;
            SpriteRenderer sr = deskT.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = mainDesk;
                sr.color = Color.white;
                sr.sortingOrder = RenderOrder.Table;
                ApplyLitMaterial(sr);
            }
        }

        Transform teacherT = transform.Find("Profesor");
        if (teacherT != null)
        {
            // Profesor a un costado (derecho) de MesaProfesor
            teacherT.localPosition = new Vector3(deskX + 1.35f, deskY, 0f);
            teacherT.localScale = new Vector3(1.8f, 2.0f, 1.0f);
            SpriteRenderer sr = teacherT.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Sprite profeSprite = Resources.Load<Sprite>("NPCs/npcprofe");
#if UNITY_EDITOR
                if (profeSprite == null) profeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/NPCs/npcprofe.png");
#endif
                if (profeSprite != null) sr.sprite = profeSprite;
                sr.color = Color.white;
                sr.sortingOrder = 4;
                ApplyLitMaterial(sr);
            }
            if (teacherT.GetComponent<TeacherIdleAnimation>() == null)
            {
                teacherT.gameObject.AddComponent<TeacherIdleAnimation>();
            }
        }
    }

    // ------------------------------------------------------------------ TAREA 4: DISTRIBUCIÓN AUTOMÁTICA DE MESAS
    public static void CalculateGridDimensions(int activeCount, out int cols, out int rows)
    {
        if (activeCount <= 8)
        {
            cols = 4;
            rows = 2;
        }
        else if (activeCount <= 10)
        {
            cols = 5;
            rows = 2;
        }
        else if (activeCount <= 12)
        {
            cols = 4;
            rows = 3;
        }
        else if (activeCount <= 15)
        {
            cols = 5;
            rows = 3;
        }
        else if (activeCount <= 18)
        {
            cols = 6;
            rows = 3;
        }
        else if (activeCount <= 20)
        {
            cols = 5;
            rows = 4;
        }
        else
        {
            cols = 6;
            rows = 4;
        }
    }

    public static Vector3 GetDeskPosition(int index, int activeCount)
    {
        int cols, rows;
        CalculateGridDimensions(activeCount, out cols, out rows);

        int r, c;
        if (index < activeCount)
        {
            r = index / cols;
            c = index % cols;
        }
        else
        {
            // Mesas inactivas de reserva: se ubican en slots posteriores
            r = index / 6;
            c = index % 6;
        }

        // Coordenadas calculadas para que esten mas arriba en Y y mas juntas entre si
        float colStep = 2.7f;
        float startX = -4.5f;

        // Centrado horizontal dinamico segun cantidad de columnas
        if (cols <= 4)
        {
            colStep = 2.8f;
            startX = -4.2f;
        }
        else if (cols == 5)
        {
            colStep = 2.7f;
            startX = -5.0f;
        }
        else
        {
            colStep = 2.6f;
            startX = -5.2f;
        }

        // Distribucion vertical: Fila 0 mas arriba (Y = 1.0 a 1.15)
        float startY = 1.0f;
        float rowStep = 1.95f;

        if (rows == 2)
        {
            startY = 0.95f;
            rowStep = 2.1f;
        }
        else if (rows == 3)
        {
            startY = 1.05f;
            rowStep = 1.95f;
        }
        else
        {
            startY = 1.15f;
            rowStep = 1.50f;
        }

        float posX = startX + c * colStep;
        float posY = startY - r * rowStep;

        return new Vector3(posX, posY, 0f);
    }

    [ContextMenu("Auto Distribute Tables (8)")]
    public void AutoDistributeTables8()
    {
        AutoDistributeTables(8);
    }

    public void AutoDistributeTables(int activeCount)
    {
        if (activeCount <= 0) activeCount = 8;

        for (int i = 1; i <= 24; i++)
        {
            Transform mesaT = transform.Find($"Mesa_{i}_Doble");
            if (mesaT == null) continue;

            mesaT.position = GetDeskPosition(i - 1, activeCount);
        }
    }

    // ------------------------------------------------------------------ TAREA 4 Y 5: DECORACIÓN DE MESAS, SILLAS Y UI
    public void DecorateTables()
    {
#if UNITY_EDITOR
        EnsureSpritesLoaded();
#endif
        for (int i = 1; i <= 24; i++)
        {
            Transform mesaT = transform.Find($"Mesa_{i}_Doble");
            if (mesaT == null) continue;

            DecorateSingleTable(mesaT, i);
        }
    }

    public void DecorateSingleTable(Transform mesaT, int seatId)
    {
        // 1. Sprite aleatorio Mesa1 o Mesa2
        Sprite mesaSprite = (Random.value < 0.5f) ? mesa1 : mesa2;

        Transform visualT = mesaT.Find("VisualMesa");
        if (visualT == null)
        {
            GameObject vGO = new GameObject("VisualMesa");
            vGO.transform.SetParent(mesaT, false);
            visualT = vGO.transform;
        }
        visualT.localPosition = Vector3.zero;
        visualT.localScale = Vector3.one;
        SpriteRenderer srMesa = visualT.GetComponent<SpriteRenderer>();
        if (srMesa == null) srMesa = visualT.gameObject.AddComponent<SpriteRenderer>();
        srMesa.sprite = mesaSprite;
        srMesa.color = Color.white;
        srMesa.sortingOrder = RenderOrder.Table;
        ApplyLitMaterial(srMesa);

        Seat seat = mesaT.GetComponent<Seat>();
        if (seat != null)
        {
            seat.highlightVisual = visualT;
            Transform oldHighlight = mesaT.Find("HighlightDrag");
            if (oldHighlight != null) DestroyObjectSafe(oldHighlight.gameObject);
        }

        // 2. Desactivar renderers rectangulares antiguos de Borde y Cuerpo
        Transform cuerpo = mesaT.Find("Cuerpo");
        if (cuerpo != null)
        {
            var sr = cuerpo.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            cuerpo.gameObject.SetActive(false);
        }

        Transform borde = mesaT.Find("Borde");
        if (borde != null)
        {
            var sr = borde.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            borde.gameObject.SetActive(false);
        }

        // 3. Sillas dobles ajustadas a X = -0.5 y X = 0.5
        if (sillas != null && sillas.Length > 0)
        {
            // Silla Izquierda
            Transform sIzq = mesaT.Find("Silla_Izq");
            if (sIzq == null)
            {
                GameObject go = new GameObject("Silla_Izq");
                go.transform.SetParent(mesaT, false);
                sIzq = go.transform;
            }
            sIzq.localPosition = new Vector3(-0.5f, -0.55f, 0f);
            sIzq.localScale = Vector3.one;
            var srIzq = sIzq.GetComponent<SpriteRenderer>();
            if (srIzq == null) srIzq = sIzq.gameObject.AddComponent<SpriteRenderer>();
            srIzq.sprite = sillas[Random.Range(0, sillas.Length)];
            srIzq.sortingOrder = RenderOrder.Chair;
            ApplyLitMaterial(srIzq);

            // Silla Derecha
            Transform sDer = mesaT.Find("Silla_Der");
            if (sDer == null)
            {
                GameObject go = new GameObject("Silla_Der");
                go.transform.SetParent(mesaT, false);
                sDer = go.transform;
            }
            sDer.localPosition = new Vector3(0.5f, -0.55f, 0f);
            sDer.localScale = Vector3.one;
            var srDer = sDer.GetComponent<SpriteRenderer>();
            if (srDer == null) srDer = sDer.gameObject.AddComponent<SpriteRenderer>();
            srDer.sprite = sillas[Random.Range(0, sillas.Length)];
            srDer.sortingOrder = RenderOrder.Chair;
            ApplyLitMaterial(srDer);
        }

        // 4. Hijos Hueco1 y Hueco2 alineados a X = -0.5 y X = 0.5 (desactivados para evitar rectángulos iluminados)
        Transform h1 = mesaT.Find("Hueco1");
        if (h1 != null)
        {
            var sr = h1.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            h1.gameObject.SetActive(false);
            h1.localPosition = new Vector3(-0.5f, -0.55f, 0f);
        }

        Transform h2 = mesaT.Find("Hueco2");
        if (h2 != null)
        {
            var sr = h2.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            h2.gameObject.SetActive(false);
            h2.localPosition = new Vector3(0.5f, -0.55f, 0f);
        }

        Vector3[] deskPath = new Vector3[]
        {
            new Vector3(-0.95f, -0.55f, 0f),
            new Vector3(-0.95f,  0.65f, 0f),
            new Vector3( 0.95f,  0.65f, 0f),
            new Vector3( 0.95f, -0.55f, 0f)
        };
        ApplyShadowCaster(mesaT.gameObject, deskPath);

        // 5. Interfaz de GrupoCarrera (GrupoFranja escala (1.8, 0.3), Alpha 0.75; GrupoTexto Negrita)
        Transform grupoCarrera = mesaT.Find("GrupoCarrera");
        if (grupoCarrera != null)
        {
            grupoCarrera.localPosition = new Vector3(grupoCarrera.localPosition.x, 0.1f, grupoCarrera.localPosition.z);
            Transform franja = grupoCarrera.Find("GrupoFranja");
            if (franja != null)
            {
                franja.localScale = new Vector3(1.8f, 0.3f, 1f);
                SpriteRenderer srFranja = franja.GetComponent<SpriteRenderer>();
                if (srFranja != null)
                {
                    Color c = srFranja.color;
                    c.a = 0.75f;
                    srFranja.color = c;
                }
            }

            Transform texto = grupoCarrera.Find("GrupoTexto");
            if (texto != null)
            {
                TextMesh tm = texto.GetComponent<TextMesh>();
                if (tm != null)
                {
                    tm.fontStyle = FontStyle.Bold;
                    tm.color = Color.black;
                }
            }
        }
    }

    // ------------------------------------------------------------------ TAREA 6: ILUMINACIÓN 2D (URP)
    public void SetupLighting2D()
    {
        Transform lightsParent = transform.Find("Iluminacion2D");
        if (lightsParent == null)
        {
            GameObject go = new GameObject("Iluminacion2D");
            go.transform.SetParent(transform, false);
            lightsParent = go.transform;
        }

        // 1. Global Light 2D
        Transform globalLightT = lightsParent.Find("Global Light 2D");
        if (globalLightT == null)
        {
            GameObject go = new GameObject("Global Light 2D");
            go.transform.SetParent(lightsParent, false);
            globalLightT = go.transform;
        }

        Light2D globalLight = globalLightT.GetComponent<Light2D>();
        if (globalLight == null) globalLight = globalLightT.gameObject.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = globalLightColor;
        globalLight.intensity = globalLightIntensity;

        // Sorting layers: Excluir estrictamente la capa UI para no iluminar Canvas ni menus
        System.Collections.Generic.List<int> validLayers = new System.Collections.Generic.List<int>();
        foreach (var sl in SortingLayer.layers)
        {
            if (sl.name != "UI")
            {
                validLayers.Add(sl.id);
            }
        }
        int[] lightLayers = validLayers.ToArray();
        globalLight.targetSortingLayers = lightLayers;
        Canvas mainCanvas = Object.FindFirstObjectByType<Canvas>();
        if (mainCanvas != null) mainCanvas.sortingLayerName = "UI";

        Transform decorRoot = transform.Find("DecoracionSuperior");
        if (decorRoot != null)
        {
            // 2. Luz Solar de Ventana 1 (SchoolWindow -> LuzVentana)
            Transform windowT = decorRoot.Find("SchoolWindow");
            if (windowT != null)
            {
                Transform windowLightT = windowT.Find("LuzVentana");
                if (windowLightT == null)
                {
                    GameObject go = new GameObject("LuzVentana");
                    go.transform.SetParent(windowT, false);
                    windowLightT = go.transform;
                }

                windowLightT.localPosition = new Vector3(0f, 0.6f, 0f);
                windowLightT.localRotation = Quaternion.Euler(0f, 0f, 179.347f);

                Light2D windowLight = windowLightT.GetComponent<Light2D>();
                if (windowLight == null) windowLight = windowLightT.gameObject.AddComponent<Light2D>();

                windowLight.lightType = Light2D.LightType.Point;
                windowLight.color = windowLightColor;
                windowLight.intensity = 1.15f;
                windowLight.pointLightInnerRadius = 5.712087f;
                windowLight.pointLightOuterRadius = 13.85255f;
                windowLight.pointLightInnerAngle = 118.05f;
                windowLight.pointLightOuterAngle = 195.26f;
                windowLight.falloffIntensity = 0.65f;
                windowLight.volumetricEnabled = true;
                windowLight.volumeIntensity = 0.06f;
                windowLight.shadowsEnabled = true;
                windowLight.shadowIntensity = 0.45f;
                windowLight.targetSortingLayers = lightLayers;

#if UNITY_EDITOR
                SerializedObject so1 = new SerializedObject(windowLight);
                var vProp1 = so1.FindProperty("m_LightVolumeEnabled");
                if (vProp1 != null) vProp1.boolValue = true;
                var viProp1 = so1.FindProperty("m_LightVolumeIntensity");
                if (viProp1 != null) viProp1.floatValue = 0.06f;
                so1.ApplyModifiedProperties();
#endif
            }

            // 3. Luz Solar de Ventana 2 (SchoolWindow2 -> LuzVentana2)
            Transform window2T = decorRoot.Find("SchoolWindow2");
            if (window2T != null)
            {
                Transform windowLight2T = window2T.Find("LuzVentana2");
                if (windowLight2T == null)
                {
                    GameObject go = new GameObject("LuzVentana2");
                    go.transform.SetParent(window2T, false);
                    windowLight2T = go.transform;
                }

                windowLight2T.localPosition = new Vector3(5f, -2.5f, 0f);
                windowLight2T.localRotation = Quaternion.Euler(0f, 0f, 125.527f);

                Light2D windowLight2 = windowLight2T.GetComponent<Light2D>();
                if (windowLight2 == null) windowLight2 = windowLight2T.gameObject.AddComponent<Light2D>();

                windowLight2.lightType = Light2D.LightType.Point;
                windowLight2.color = windowLightColor;
                windowLight2.intensity = 1.15f;
                windowLight2.pointLightInnerRadius = 8.459514f;
                windowLight2.pointLightOuterRadius = 18.44963f;
                windowLight2.pointLightInnerAngle = 67.84f;
                windowLight2.pointLightOuterAngle = 171.39f;
                windowLight2.falloffIntensity = 0.65f;
                windowLight2.volumetricEnabled = true;
                windowLight2.volumeIntensity = 0.06f;
                windowLight2.shadowsEnabled = true;
                windowLight2.shadowIntensity = 0.45f;
                windowLight2.targetSortingLayers = lightLayers;

#if UNITY_EDITOR
                SerializedObject so2 = new SerializedObject(windowLight2);
                var vProp2 = so2.FindProperty("m_LightVolumeEnabled");
                if (vProp2 != null) vProp2.boolValue = true;
                var viProp2 = so2.FindProperty("m_LightVolumeIntensity");
                if (viProp2 != null) viProp2.floatValue = 0.06f;
                so2.ApplyModifiedProperties();
#endif
            }
        }
    }

    // ------------------------------------------------------------------ TAREA 7: SOMBRAS (SHADOW CASTER 2D)
    public void SetupShadowCasters()
    {
        // Polígono para mesas: envuelve el contorno del cuerpo de la mesa excluyendo las sillas
        Vector3[] deskPath = new Vector3[]
        {
            new Vector3(-0.95f, -0.55f, 0f),
            new Vector3(-0.95f,  0.65f, 0f),
            new Vector3( 0.95f,  0.65f, 0f),
            new Vector3( 0.95f, -0.55f, 0f)
        };

        // Mesas 1 a 24
        for (int i = 1; i <= 24; i++)
        {
            Transform mesaT = transform.Find($"Mesa_{i}_Doble");
            if (mesaT != null)
            {
                ApplyShadowCaster(mesaT.gameObject, deskPath);
            }
        }

        // Mesa Profesor
        Transform mesaProfesor = transform.Find("MesaProfesor");
        if (mesaProfesor != null)
        {
            ApplyShadowCaster(mesaProfesor.gameObject, deskPath);
        }

        // Objetos altos en la pared y base
        Transform decorRoot = transform.Find("DecoracionSuperior");
        if (decorRoot != null)
        {
            Transform bookCupboardT = decorRoot.Find("BookCupboard");
            if (bookCupboardT != null)
            {
                Vector3[] path = new Vector3[]
                {
                    new Vector3(-0.95f, -0.95f, 0f),
                    new Vector3(-0.95f,  0.95f, 0f),
                    new Vector3( 0.95f,  0.95f, 0f),
                    new Vector3( 0.95f, -0.95f, 0f)
                };
                ApplyShadowCaster(bookCupboardT.gameObject, path);
            }

            Transform bookshelfT = decorRoot.Find("ClassroomBookshelf");
            if (bookshelfT != null)
            {
                Vector3[] path = new Vector3[]
                {
                    new Vector3(-0.70f, -0.70f, 0f),
                    new Vector3(-0.70f,  0.70f, 0f),
                    new Vector3( 0.70f,  0.70f, 0f),
                    new Vector3( 0.70f, -0.70f, 0f)
                };
                ApplyShadowCaster(bookshelfT.gameObject, path);
            }

            Transform bigPlantT = decorRoot.Find("BigPlant");
            if (bigPlantT != null)
            {
                Vector3[] path = new Vector3[]
                {
                    new Vector3(-0.45f, -0.95f, 0f),
                    new Vector3(-0.45f,  0.95f, 0f),
                    new Vector3( 0.45f,  0.95f, 0f),
                    new Vector3( 0.45f, -0.95f, 0f)
                };
                ApplyShadowCaster(bigPlantT.gameObject, path);
            }

            Transform binT = decorRoot.Find("Bin");
            if (binT != null)
            {
                Vector3[] path = new Vector3[]
                {
                    new Vector3(-0.40f, -0.45f, 0f),
                    new Vector3(-0.40f,  0.45f, 0f),
                    new Vector3( 0.40f,  0.45f, 0f),
                    new Vector3( 0.40f, -0.45f, 0f)
                };
                ApplyShadowCaster(binT.gameObject, path);
            }
        }

        // Profesor
        Transform profT = transform.Find("Profesor");
        if (profT != null)
        {
            Vector3[] path = new Vector3[]
            {
                new Vector3(-0.40f, -0.45f, 0f),
                new Vector3(-0.40f,  0.45f, 0f),
                new Vector3( 0.40f,  0.45f, 0f),
                new Vector3( 0.40f, -0.45f, 0f)
            };
            ApplyShadowCaster(profT.gameObject, path);
        }
    }

    public static void ApplyShadowCaster(GameObject go, Vector3[] path)
    {
        if (go == null) return;
        ShadowCaster2D sc = go.GetComponent<ShadowCaster2D>();
        if (sc == null) sc = go.AddComponent<ShadowCaster2D>();
        sc.selfShadows = false;
        sc.castsShadows = true;

#if UNITY_EDITOR
        SerializedObject so = new SerializedObject(sc);
        SerializedProperty pathProp = so.FindProperty("m_ShapePath");
        if (pathProp != null)
        {
            pathProp.arraySize = path.Length;
            for (int i = 0; i < path.Length; i++)
            {
                pathProp.GetArrayElementAtIndex(i).vector3Value = path[i];
            }
        }
        SerializedProperty sourceProp = so.FindProperty("m_ShadowCastingSource");
        if (sourceProp != null) sourceProp.intValue = 1; // Shape Editor
        SerializedProperty silProp = so.FindProperty("m_UseRendererSilhouette");
        if (silProp != null) silProp.boolValue = false;
        so.ApplyModifiedProperties();
#endif

        var pathField = typeof(ShadowCaster2D).GetField("m_ShapePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        pathField?.SetValue(sc, path);

        var sourceField = typeof(ShadowCaster2D).GetField("m_ShadowCastingSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (sourceField != null)
        {
            var enumVal = System.Enum.ToObject(sourceField.FieldType, 1);
            sourceField.SetValue(sc, enumVal);
        }

        var silField = typeof(ShadowCaster2D).GetField("m_UseRendererSilhouette", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        silField?.SetValue(sc, false);
    }

    void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            DestroyObjectSafe(child);
        }
    }

    void DestroyObjectSafe(GameObject go)
    {
        if (go == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(go);
            return;
        }
#endif
        go.name = "_Destroying_" + go.name;
        go.SetActive(false);
        Destroy(go);
    }
}
