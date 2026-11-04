using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstruye GameScene: sala cenital 2D (cuadrados), 24 mesas dobles, fila de
/// espera, HUD y managers del bucle de juego.
/// Menu: Tools > CubiCampus > Rebuild Game Scene
/// </summary>
public static class CubiGameBuilder
{
    const string ScenePath = "Assets/Scenes/GameScene.unity";

    static readonly Color ColFondo      = new Color(0.055f, 0.06f, 0.09f, 1f);
    static readonly Color ColPiso       = new Color(0.13f, 0.13f, 0.18f, 1f);
    static readonly Color ColPared      = new Color(0.09f, 0.10f, 0.14f, 1f);
    static readonly Color ColZonaSil    = new Color(0.11f, 0.20f, 0.32f, 0.55f);
    static readonly Color ColZonaDob    = new Color(0.10f, 0.22f, 0.18f, 0.55f);
    static readonly Color ColZonaRui    = new Color(0.26f, 0.16f, 0.09f, 0.55f);
    static readonly Color ColFila       = new Color(0.10f, 0.12f, 0.18f, 0.85f);
    static readonly Color ColTexto      = new Color(0.95f, 0.95f, 0.98f, 1f);
    static readonly Color ColTextoTenue = new Color(0.68f, 0.72f, 0.82f, 1f);
    static readonly Color ColBotonAzul  = new Color(0.20f, 0.55f, 0.95f, 1f);
    static readonly Color ColBotonRojo  = new Color(0.80f, 0.26f, 0.26f, 1f);
    static readonly Color ColPanel      = new Color(0.10f, 0.11f, 0.16f, 0.98f);

    [MenuItem("Tools/CubiCampus/Rebuild Game Scene")]
    public static void Rebuild()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);

        foreach (GameObject root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);

        // ---------------------------------------------------------------- camara
        GameObject camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.55f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColFondo;
        cam.transform.position = new Vector3(0.45f, -0.2f, -10f);
        camGO.AddComponent<AudioListener>();

        // ---------------------------------------------------------------- event system
        GameObject esGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        esGO.transform.SetParent(null);

        // ---------------------------------------------------------------- sala
        GameObject room = new GameObject("Sala");

        // Piso
        NewSprite("Piso", room.transform, new Vector3(0.5f, -0.2f, 0f), new Vector2(19.8f, 9.3f), ColPiso, -20);

        // Paredes
        NewSprite("ParedArriba", room.transform, new Vector3(0.5f, 4.45f, 0f), new Vector2(20.2f, 0.25f), ColPared, -18);
        NewSprite("ParedAbajo", room.transform, new Vector3(0.5f, -4.85f, 0f), new Vector2(20.2f, 0.25f), ColPared, -18);
        NewSprite("ParedIzquierda", room.transform, new Vector3(-9.35f, -0.2f, 0f), new Vector2(0.25f, 9.6f), ColPared, -18);
        NewSprite("ParedDerecha", room.transform, new Vector3(10.35f, -0.2f, 0f), new Vector2(0.25f, 9.6f), ColPared, -18);

        // Franja de la fila
        NewSprite("ZonaFila", room.transform, new Vector3(-7.95f, -0.35f, 0f), new Vector2(2.25f, 8.1f), ColFila, -15);

        // Estanterias decorativas al fondo superior
        for (int i = 0; i < 3; i++)
        {
            NewSprite("Estanteria" + i, room.transform,
                new Vector3(1.2f + i * 3.0f, 4.08f, 0f), new Vector2(2.4f, 0.42f),
                new Color(0.24f, 0.20f, 0.16f), -16);
        }

        // Puerta
        NewSprite("Puerta", room.transform, new Vector3(-7.95f, -4.52f, 0f), new Vector2(1.9f, 0.40f),
            new Color(0.30f, 0.42f, 0.55f), -14);
        NewSprite("PuertaLuz", room.transform, new Vector3(-7.95f, -4.28f, 0f), new Vector2(1.4f, 0.12f),
            new Color(0.55f, 0.85f, 1f, 0.75f), -13);

        // Mesa del profesor + placeholder cuadrado (reemplazable por arte final).
        CreateTeacherArea(room.transform);

        // ---------------------------------------------------------------- puestos
        // 24 mesas dobles (2 alumnos por mesa): 6 columnas x 4 filas.
        List<Seat> seats = new List<Seat>();
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 6; col++)
            {
                int id = row * 6 + col + 1;
                Vector3 pos = new Vector3(-5.53f + col * 2.86f, 3.02f - row * 1.8f, 0f);
                seats.Add(MakeSeat(room.transform, id, SeatKind.Doble, pos, new Vector2(2.75f, 1.45f), false));
            }
        }

        // ---------------------------------------------------------------- fila
        GameObject queueRoot = new GameObject("Fila");
        Transform[] slots = new Transform[4];
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slot = new GameObject("SlotFila" + (i + 1));
            slot.transform.SetParent(queueRoot.transform, false);
            slot.transform.position = new Vector3(-7.95f, 2.95f - i * 1.25f, 0f);
            NewSprite("Marca", slot.transform, slot.transform.position + new Vector3(0f, -0.45f, 0f),
                new Vector2(0.75f, 0.06f), new Color(0.30f, 0.38f, 0.52f, 0.8f), -12);
            slots[i] = slot.transform;
        }

        GameObject doorPoint = new GameObject("PuntoPuerta");
        doorPoint.transform.SetParent(queueRoot.transform, false);
        doorPoint.transform.position = new Vector3(-7.95f, -4.05f, 0f);

        // ---------------------------------------------------------------- managers
        GameObject gmGO = new GameObject("GameManager");
        GameManager gm = gmGO.AddComponent<GameManager>();
        gm.seats = seats;

        GameObject qGO = new GameObject("QueueManager");
        QueueManager queue = qGO.AddComponent<QueueManager>();
        queue.game = gm;
        queue.doorPoint = doorPoint.transform;
        queue.slots = slots;
        queue.spawnInterval = 2.6f;

        GameObject dragGO = new GameObject("DragAssigner");
        DragAssigner drag = dragGO.AddComponent<DragAssigner>();
        drag.game = gm;
        drag.cam = cam;

        gm.queue = queue;
        gm.dragAssigner = drag;

        // ---------------------------------------------------------------- canvas
        GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10f;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Transform canvasT = canvasGO.transform;
        HUDGame hud = canvasGO.AddComponent<HUDGame>();
        hud.game = gm;
        gm.hud = hud;

        // Barra superior
        GameObject topBar = NewUI("BarraSuperior", canvasT);
        RectTransform topRT = topBar.GetComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0f, 1f);
        topRT.anchorMax = new Vector2(1f, 1f);
        topRT.pivot = new Vector2(0.5f, 1f);
        topRT.anchoredPosition = Vector2.zero;
        topRT.sizeDelta = new Vector2(0f, 130f);
        AddImage(topBar, new Color(0.06f, 0.07f, 0.11f, 0.95f)).raycastTarget = false;

        GameObject puntosGO = NewUI("TxtPuntos", canvasT);
        Place(puntosGO.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -22f), new Vector2(560f, 56f));
        Text txtPuntos = AddText(puntosGO, "PUNTOS  0", 38, TextAnchor.MiddleLeft, ColTexto);
        txtPuntos.fontStyle = FontStyle.Bold;
        hud.txtPuntos = txtPuntos;

        GameObject tiempoGO = NewUI("TxtTiempo", canvasT);
        Place(tiempoGO.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(360f, 62f));
        Text txtTiempo = AddText(tiempoGO, "03:00", 54, TextAnchor.MiddleCenter, ColTexto);
        txtTiempo.fontStyle = FontStyle.Bold;
        hud.txtTiempo = txtTiempo;

        hud.barraTiempo = MakeBar(canvasT, "BarraTiempo", new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(540f, 16f), new Color(0.35f, 0.70f, 1f));

        GameObject efGO = NewUI("TxtEficiencia", canvasT);
        Place(efGO.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -20f), new Vector2(460f, 52f));
        Text txtEf = AddText(efGO, "EFICIENCIA  100%", 36, TextAnchor.MiddleRight, ColTexto);
        txtEf.fontStyle = FontStyle.Bold;
        hud.txtEficiencia = txtEf;

        hud.barraEficiencia = MakeBar(canvasT, "BarraEficiencia", new Vector2(1f, 1f), new Vector2(-40f, -74f), new Vector2(340f, 16f), new Color(0.35f, 0.90f, 0.45f));

        GameObject diaGO = NewUI("TxtDia", canvasT);
        Place(diaGO.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 150f), new Vector2(620f, 36f));
        Text txtDia = AddText(diaGO, "DIA 1 / 10   -   META 60%", 22, TextAnchor.MiddleRight, ColTextoTenue);
        txtDia.fontStyle = FontStyle.Bold;
        hud.txtDia = txtDia;

        GameObject comboGO = NewUI("TxtCombo", canvasT);
        Place(comboGO.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -80f), new Vector2(420f, 38f));
        Text txtCombo = AddText(comboGO, "RACHA 0 / 3", 24, TextAnchor.MiddleLeft, ColTextoTenue);
        hud.txtCombo = txtCombo;

        GameObject rushGO = NewUI("TxtRush", canvasT);
        Place(rushGO.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(560f, 42f));
        Text txtRush = AddText(rushGO, "MODO RUSH", 30, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.25f));
        txtRush.fontStyle = FontStyle.Bold;
        txtRush.gameObject.SetActive(false);
        hud.txtRush = txtRush;

        GameObject aforoGO = NewUI("TxtAforo", canvasT);
        Place(aforoGO.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 68f), new Vector2(620f, 40f));
        Text txtAforo = AddText(aforoGO, "AFORO  0 / 48", 26, TextAnchor.MiddleLeft, ColTexto);
        txtAforo.fontStyle = FontStyle.Bold;
        hud.txtAforo = txtAforo;

        GameObject abGO = NewUI("TxtAbandonos", canvasT);
        Place(abGO.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 28f), new Vector2(1200f, 36f));
        hud.txtAbandonos = AddText(abGO, "ATENDIDOS 0   ABANDONOS 0   PELEAS 0", 21, TextAnchor.MiddleLeft, ColTextoTenue);

        GameObject fpsGO = NewUI("TxtFPS", canvasT);
        Place(fpsGO.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -140f), new Vector2(120f, 36f));
        Text txtFPS = AddText(fpsGO, "FPS 60", 24, TextAnchor.MiddleRight, Color.white);
        txtFPS.fontStyle = FontStyle.Bold;
        hud.txtFPS = txtFPS;

        

        // Botones
        Button btnBono = MakeButton(canvasT, "BtnBono", "BONO SILENCIO", ColBotonAzul, new Vector2(280f, 92f), 24);
        Place(btnBono.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-320f, 40f), new Vector2(280f, 92f));
        hud.btnBono = btnBono;
        hud.txtBono = btnBono.GetComponentInChildren<Text>();

        Button btnPausa = MakeButton(canvasT, "BtnPausa", "PAUSA", new Color(0.25f, 0.28f, 0.38f), new Vector2(160f, 92f), 24);
        Place(btnPausa.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(160f, 92f));
        hud.btnPausa = btnPausa;

        // Overlay de alerta
        GameObject alertaGO = NewUI("OverlayAlerta", canvasT);
        Stretch(alertaGO.GetComponent<RectTransform>());
        Image alertaImg = AddImage(alertaGO, new Color(1f, 0.15f, 0.15f, 0.12f));
        alertaImg.raycastTarget = false;
        alertaGO.SetActive(false);
        hud.overlayAlerta = alertaImg;

        // Texto de fin de turno
        GameObject estadoGO = NewUI("TxtEstado", canvasT);
        Place(estadoGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 240f));
        Text txtEstado = AddText(estadoGO, "TURNO FINALIZADO", 56, TextAnchor.MiddleCenter, ColTexto);
        txtEstado.fontStyle = FontStyle.Bold;
        Outline outline = estadoGO.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);
        estadoGO.SetActive(false);
        hud.txtEstado = txtEstado;

        // Panel de pausa
        GameObject panelPausa = NewUI("PanelPausa", canvasT);
        Stretch(panelPausa.GetComponent<RectTransform>());
        AddImage(panelPausa, new Color(0f, 0f, 0f, 0.78f));

        GameObject contPausa = NewUI("ContenedorPausa", panelPausa.transform);
        Place(contPausa.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 560f));
        AddImage(contPausa, ColPanel);

        GameObject pauseTitleGO = NewUI("TituloPausa", contPausa.transform);
        Place(pauseTitleGO.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(620f, 80f));
        Text pauseTitle = AddText(pauseTitleGO, "PAUSA", 54, TextAnchor.MiddleCenter, ColTexto);
        pauseTitle.fontStyle = FontStyle.Bold;

        Button btnReanudar = MakeButton(contPausa.transform, "BtnReanudar", "REANUDAR", ColBotonAzul, new Vector2(460f, 88f), 30);
        Place(btnReanudar.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(460f, 88f));
        hud.btnReanudar = btnReanudar;

        Button btnMenu = MakeButton(contPausa.transform, "BtnMenu", "SALIR AL MENU", ColBotonRojo, new Vector2(460f, 88f), 30);
        Place(btnMenu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(460f, 88f));
        hud.btnMenu = btnMenu;

        GameObject pauseInfoGO = NewUI("InfoPausa", contPausa.transform);
        Place(pauseInfoGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(680f, 40f));
        AddText(pauseInfoGO, "ESC para reanudar", 22, TextAnchor.MiddleCenter, ColTextoTenue);

        panelPausa.SetActive(false);
        hud.panelPausa = panelPausa;

        // ---------------------------------------------------------------- guardar
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CubiCampus] GameScene reconstruida con 24 mesas dobles, fila de 4, HUD y bucle de juego.");
    }

    [MenuItem("Tools/CubiCampus/Agregar Mesa del Profesor")]
    public static void AddTeacherArea()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath);

        Transform room = null;
        GameObject sala = GameObject.Find("Sala");
        if (sala != null) room = sala.transform;

        CreateTeacherArea(room);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[CubiCampus] Mesa del profesor y placeholder del profesor listos en GameScene.");
    }

    static void CreateTeacherArea(Transform parent)
    {
        if (GameObject.Find("MesaProfesor") == null)
        {
            NewSprite("MesaProfesor", parent, new Vector3(3.6f, -4.13f, 0f), new Vector2(2.75f, 1.0f),
                new Color(0.36f, 0.24f, 0.14f), 2);
        }

        if (GameObject.Find("Profesor") == null)
        {
            NewSprite("Profesor", parent, new Vector3(3.6f, -3.62f, 0f), new Vector2(0.7f, 0.7f),
                new Color(0.25f, 0.55f, 0.85f), 14);
        }
    }

    // ---------------------------------------------------------------- helpers

    static Seat MakeSeat(Transform parent, int id, SeatKind kind, Vector3 pos, Vector2 size, bool outlet)
    {
        GameObject go = new GameObject("Mesa_" + id + "_" + kind);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        Seat seat = go.AddComponent<Seat>();
        seat.seatId = id;
        seat.kind = kind;
        seat.capacity = kind == SeatKind.Doble ? 2 : 1;
        seat.hasOutlet = outlet;
        seat.size = size;
        seat.scrubDistance = 2.2f;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;

        seat.BuildVisuals();
        return seat;
    }

    static GameObject NewSprite(string name, Transform parent, Vector3 pos, Vector2 size, Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelFactory.White;
        sr.color = color;
        sr.sortingOrder = order;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        return go;
    }

    static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = 5;
        return go;
    }

    static Image AddImage(GameObject go, Color color)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static Text AddText(GameObject go, string content, int size, TextAnchor anchor, Color color)
    {
        Text t = go.AddComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = size;
        t.alignment = anchor;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Image MakeBar(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color fillColor)
    {
        GameObject fondo = NewUI(name, parent);
        Place(fondo.GetComponent<RectTransform>(), anchor, anchor, pos, size);
        AddImage(fondo, new Color(0.06f, 0.07f, 0.10f, 0.95f)).raycastTarget = false;

        GameObject relleno = NewUI("Relleno", fondo.transform);
        RectTransform rt = relleno.GetComponent<RectTransform>();
        Stretch(rt);
        rt.offsetMin = new Vector2(2f, 2f);
        rt.offsetMax = new Vector2(-2f, -2f);

        Image img = AddImage(relleno, fillColor);
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
        img.raycastTarget = false;
        return img;
    }

    static Button MakeButton(Transform parent, string name, string label, Color bg, Vector2 size, int fontSize)
    {
        GameObject go = NewUI(name, parent);
        go.GetComponent<RectTransform>().sizeDelta = size;

        Image img = AddImage(go, bg);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        cb.pressedColor = new Color(0.70f, 0.70f, 0.70f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        GameObject labelGO = NewUI("Texto", go.transform);
        Stretch(labelGO.GetComponent<RectTransform>());
        Text t = AddText(labelGO, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        t.fontStyle = FontStyle.Bold;

        return btn;
    }
}
