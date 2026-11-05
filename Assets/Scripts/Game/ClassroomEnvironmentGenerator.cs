using System.Collections.Generic;
using UnityEngine;

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
    public Sprite schoolBooks;

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

    [Header("Configuración de Cuadrícula")]
    public int minCol = -10;
    public int maxCol = 11;
    public float wallCenterY = 3.5f;
    public int floorRows = 7;
    public float floorStartY = 2.0f;

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
            // Asegurar que las mesas y decoraciones queden acomodadas despues de cualquier inicializacion
            DecorateTables();
        }
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
        DecorateTables();
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
        if (schoolBooks == null) schoolBooks = AssetDatabase.LoadAssetAtPath<Sprite>(baseDir + "school_books.png");

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

    // ------------------------------------------------------------------ TAREA 1: PISO Y PARED
    public void SetupFloorAndWalls()
    {
        Transform pisoT = transform.Find("Piso");
        if (pisoT == null)
        {
            GameObject pGO = new GameObject("Piso");
            pGO.transform.SetParent(transform, false);
            pisoT = pGO.transform;
        }

        // El piso sirve como contenedor padre en (0,0,0) con escala neutra (1,1,1)
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

        // Limpiar contenedor de celdas anterior
        Transform gridRoot = pisoT.Find("CuadriculaPisoPared");
        if (gridRoot != null)
        {
            DestroyObjectSafe(gridRoot.gameObject);
        }

        GameObject gridGO = new GameObject("CuadriculaPisoPared");
        gridGO.transform.SetParent(pisoT, false);
        gridRoot = gridGO.transform;

        // 1. Dos filas superiores: instancias de Classroom_wall (32x64 px -> 1x2 unidades)
        for (int col = minCol; col <= maxCol; col++)
        {
            GameObject wallGO = new GameObject($"Wall_{col}");
            wallGO.transform.SetParent(gridRoot, false);
            wallGO.transform.localPosition = new Vector3(col, wallCenterY, 0f);
            wallGO.transform.localScale = Vector3.one;

            SpriteRenderer sr = wallGO.AddComponent<SpriteRenderer>();
            sr.sprite = classroomWall;
            sr.sortingOrder = -20;
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
                sr.sortingOrder = -20;
            }
        }
    }

    // ------------------------------------------------------------------ TAREA 2: TRIM (BORDES)
    public void SetupTrimPerimeter()
    {
        Transform trimRoot = transform.Find("BordesTrim");
        if (trimRoot != null)
        {
            DestroyObjectSafe(trimRoot.gameObject);
        }

        GameObject trimGO = new GameObject("BordesTrim");
        trimGO.transform.SetParent(transform, false);
        trimRoot = trimGO.transform;

        int order = -18;

        // 4 Esquinas
        // Sup-Izq
        CreateTrimTile("Trim_C_Down_Left", trimRoot, new Vector3(minCol, 4.0f, 0f), trimCornerDownLeft, order);
        // Sup-Der
        CreateTrimTile("Trim_C_Down_Right", trimRoot, new Vector3(maxCol, 4.0f, 0f), trimCornerDownRight, order);
        // Inf-Izq
        CreateTrimTile("Trim_C_Up_Left", trimRoot, new Vector3(minCol, -4.0f, 0f), trimCornerUpLeft, order);
        // Inf-Der
        CreateTrimTile("Trim_C_Up_Right", trimRoot, new Vector3(maxCol, -4.0f, 0f), trimCornerUpRight, order);

        // Borde Superior (entre esquinas)
        for (int col = minCol + 1; col < maxCol; col++)
        {
            CreateTrimTile($"Trim_Top_{col}", trimRoot, new Vector3(col, 4.0f, 0f), trimTop, order);
        }

        // Borde Inferior (entre esquinas)
        for (int col = minCol + 1; col < maxCol; col++)
        {
            CreateTrimTile($"Trim_Bottom_{col}", trimRoot, new Vector3(col, -4.0f, 0f), trimBottom, order);
        }

        // Bordes Laterales (entre esquinas)
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
    }

    // ------------------------------------------------------------------ TAREA 3: DECORACION SUPERIOR
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

        Transform decorRoot = transform.Find("DecoracionSuperior");
        if (decorRoot != null)
        {
            DestroyObjectSafe(decorRoot.gameObject);
        }

        GameObject rootGO = new GameObject("DecoracionSuperior");
        rootGO.transform.SetParent(transform, false);
        decorRoot = rootGO.transform;

        // Distribucion aleatoria pero coherente sin superposiciones
        // Dividimos la pared superior en zonas ordenadas con jitter aleatorio
        // 1. ZONA IZQUIERDA (X de -9.5 a -2.5)
        // Puerta al ras de la base de pared (Y=3.5, bottom en 2.5)
        float doorX = Random.Range(-8.5f, -7.5f);
        CreateProp("Door", decorRoot, new Vector3(doorX, 3.5f, 0f), classroomDoor, -14);

        // Planta grande a la izquierda o junto a la puerta
        float plantLeftX = doorX < -8.0f ? doorX + 1.6f : doorX - 1.4f;
        CreateProp("BigPlant", decorRoot, new Vector3(plantLeftX, 3.5f, 0f), bigPlant, -14);

        // Ventana en la pared izquierda
        float windowX = Random.Range(-5.0f, -3.8f);
        CreateProp("SchoolWindow", decorRoot, new Vector3(windowX, 3.5f, 0f), schoolWindow, -16);

        // Tacho de reciclaje en el suelo
        float binX = windowX + Random.Range(1.6f, 2.0f);
        CreateProp("Bin", decorRoot, new Vector3(binX, 3.0f, 0f), bin, -14);

        // 2. ZONA CENTRO (X de -1.5 a 3.5)
        // Reloj colgado alto cerca de la pizarra
        float clockX = Random.Range(-1.5f, -0.8f);
        CreateProp("WallClock", decorRoot, new Vector3(clockX, 3.8f, 0f), wallClock, -16);

        // Pizarra escolar centrada / zona media
        float boardX = Random.Range(0.6f, 1.4f);
        CreateProp("SchoolBoard", decorRoot, new Vector3(boardX, 3.5f, 0f), schoolBoard, -16);

        // Pila de libros cerca de la pizarra o profesor
        float booksX = boardX + Random.Range(2.0f, 2.5f);
        CreateProp("SchoolBooks", decorRoot, new Vector3(booksX, 3.0f, 0f), schoolBooks, -14);

        // 3. ZONA DERECHA (X de 4.0 a 10.5)
        // Maceta
        float plantRightX = booksX + Random.Range(1.4f, 1.8f);
        CreateProp("ClassroomPlant", decorRoot, new Vector3(plantRightX, 3.25f, 0f), classroomPlant, -14);

        // Mapa o Corcho (uno al lado de otro de forma aleatoria)
        bool mapFirst = Random.value < 0.5f;
        float item1X = plantRightX + Random.Range(1.8f, 2.2f);
        float item2X = item1X + Random.Range(2.2f, 2.5f);

        CreateProp(mapFirst ? "ClassroomMap" : "PinBoard", decorRoot, new Vector3(item1X, 3.5f, 0f),
            mapFirst ? classroomMap : pinBoard, -16);

        CreateProp(mapFirst ? "PinBoard" : "ClassroomMap", decorRoot, new Vector3(item2X, 3.5f, 0f),
            mapFirst ? pinBoard : classroomMap, -16);

        // Estanteria o armario al extremo derecho
        float cupboardX = Mathf.Min(10.0f, item2X + 2.2f);
        CreateProp("BookCupboard", decorRoot, new Vector3(cupboardX, 3.5f, 0f), bookCupboard, -14);

        // Estante mediano si hay espacio
        if (cupboardX - item2X > 2.8f)
        {
            CreateProp("ClassroomBookshelf", decorRoot, new Vector3((item2X + cupboardX) * 0.5f, 3.25f, 0f), classroomBookshelf, -14);
        }

        // 4. Escritorio principal (main_desk) en posicion fija (4, -3)
        SetupTeacherArea();
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
    }

    void SetupTeacherArea()
    {
        Vector3 deskPos = new Vector3(4f, -3f, 0f);
        Vector3 teacherPos = new Vector3(4f, -2.4f, 0f);

        Transform deskT = transform.Find("MesaProfesor");
        if (deskT != null)
        {
            deskT.localPosition = deskPos;
            deskT.localScale = Vector3.one;
            SpriteRenderer sr = deskT.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = mainDesk;
                sr.color = Color.white;
                sr.sortingOrder = 2;
            }
        }

        Transform teacherT = transform.Find("Profesor");
        if (teacherT != null)
        {
            teacherT.localPosition = teacherPos;
            teacherT.localScale = new Vector3(0.7f, 0.7f, 1f);
            SpriteRenderer sr = teacherT.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 4;
        }
    }

    // ------------------------------------------------------------------ TAREA 4: REESTRUCTURACION DE MESAS
    public void DecorateTables()
    {
#if UNITY_EDITOR
        EnsureSpritesLoaded();
#endif
        // Buscar todas las mesas dobles
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
        srMesa.sortingOrder = 2;

        // 2. Desactivar renderers rectangulares antiguos de Borde y Cuerpo
        Transform cuerpo = mesaT.Find("Cuerpo");
        if (cuerpo != null)
        {
            var sr = cuerpo.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        Transform borde = mesaT.Find("Borde");
        if (borde != null)
        {
            var sr = borde.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        // 3. Sillas dobles (Silla1, Silla2, Silla3) en la parte frontal/inferior (local Y = -0.55)
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
            sIzq.localPosition = new Vector3(-0.74f, -0.55f, 0f);
            sIzq.localScale = Vector3.one;
            var srIzq = sIzq.GetComponent<SpriteRenderer>();
            if (srIzq == null) srIzq = sIzq.gameObject.AddComponent<SpriteRenderer>();
            srIzq.sprite = sillas[Random.Range(0, sillas.Length)];
            srIzq.sortingOrder = 3;

            // Silla Derecha
            Transform sDer = mesaT.Find("Silla_Der");
            if (sDer == null)
            {
                GameObject go = new GameObject("Silla_Der");
                go.transform.SetParent(mesaT, false);
                sDer = go.transform;
            }
            sDer.localPosition = new Vector3(0.74f, -0.55f, 0f);
            sDer.localScale = Vector3.one;
            var srDer = sDer.GetComponent<SpriteRenderer>();
            if (srDer == null) srDer = sDer.gameObject.AddComponent<SpriteRenderer>();
            srDer.sprite = sillas[Random.Range(0, sillas.Length)];
            srDer.sortingOrder = 3;
        }

        // 4. Hijos Hueco1 y Hueco2: desactivar SpriteRenderer pero mantener activo el GameObject y reubicar local Y = -0.55
        Transform h1 = mesaT.Find("Hueco1");
        if (h1 != null)
        {
            var sr = h1.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            h1.localPosition = new Vector3(-0.74f, -0.55f, 0f);
        }

        Transform h2 = mesaT.Find("Hueco2");
        if (h2 != null)
        {
            var sr = h2.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            h2.localPosition = new Vector3(0.74f, -0.55f, 0f);
        }
    }

    void DestroyObjectSafe(GameObject go)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(go);
        else
            Destroy(go);
#else
        Destroy(go);
#endif
    }
}
