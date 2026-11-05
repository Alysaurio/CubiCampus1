using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public enum StudentState
{
    InQueue,
    Dragging,
    Walking,
    Seated,
    Ready,
    Leaving,
    Gone,
    Packing    // listo: recogiendo sus cosas (los clics aceleran la salida)
}

/// <summary>
/// Estudiante de la sala. Cada alumno pertenece a una carrera de Tecsup.
/// Paciencia en fila, tiempo de estudio, distraccion y salida. Visual pixel art
/// con transicion frontal/trasera, animacion procedural y datos flotantes en capa UI.
/// </summary>
public class Student : MonoBehaviour
{
    [Header("Datos")]
    public StudentProfile profile;
    public StudentState state = StudentState.InQueue;
    public float patienceMax = 20f;
    public float patience = 20f;
    public float studyMax = 30f;
    public float studyLeft = 30f;
    public bool sharedDouble;
    public bool needsAttention;
    public float attentionLeft;
    public bool distracted;          // este alumno se distrae a mitad del estudio
    public bool careerConflict;      // comparte mesa con otra carrera: pelea
    public bool needsCharge;         // bateria baja: necesita una mesa con enchufe
    public bool missingCharge;       // esta sentado sin enchufe
    public bool chargePenalized;     // ya se le desconto eficiencia por no tener cargador
    public bool readyPenalized;      // ya se le desconto eficiencia por esperar listo
    public Seat seat;
    public int npcIndex = -1;

    public SpriteRenderer Body => body;

    GameManager game;
    QueueManager queue;

    Transform visual;
    Transform floatingUI;
    SpriteRenderer body;
    SpriteRenderer barBg;
    SpriteRenderer barFill;
    SpriteRenderer alertBox;
    SpriteRenderer readyBox;
    SpriteRenderer conflictBox;
    GameObject chargeRoot;
    SpriteRenderer chargeBody;
    SpriteRenderer careerBadge;
    TextMesh careerLabel;
    Collider2D col;

    Coroutine mover;
    bool attentionDone;
    float readyTime;
    float bobPhase;
    float packLeft;
    float packMax = 1.4f;

    Sprite frontSprite;
    Sprite backSprite;

    static Sprite[] frontSprites;
    static Sprite[] backSprites;
    static Material litMaterial;
    static Material unlitMaterial;

    public float PatienceFraction
    {
        get { return patienceMax > 0f ? Mathf.Clamp01(patience / patienceMax) : 0f; }
    }

    public float ReadySeconds { get { return readyTime; } }

    public float PackFraction
    {
        get { return packMax > 0f ? Mathf.Clamp01(1f - packLeft / packMax) : 1f; }
    }

    public static void LoadNPCSprites()
    {
        if (frontSprites != null && frontSprites.Length == 6 && frontSprites[0] != null) return;
        frontSprites = new Sprite[6];
        backSprites = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            frontSprites[i] = Resources.Load<Sprite>($"NPCs/npc{i + 1}f");
            backSprites[i] = Resources.Load<Sprite>($"NPCs/npc{i + 1}b");
#if UNITY_EDITOR
            if (frontSprites[i] == null)
                frontSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/NPCs/npc{i + 1}f.png");
            if (backSprites[i] == null)
                backSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/NPCs/npc{i + 1}b.png");
#endif
        }
    }

    public static Material GetLitMaterial()
    {
        if (litMaterial == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (s != null) litMaterial = new Material(s);
        }
        return litMaterial;
    }

    public static Material GetUnlitMaterial()
    {
        if (unlitMaterial == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s != null) unlitMaterial = new Material(s);
        }
        return unlitMaterial;
    }

    public static Student Create(StudentProfile profile, Vector3 position, GameManager game, QueueManager queue)
    {
        GameObject go = new GameObject("Estudiante_" + profile.shortName);
        go.transform.position = position;
        go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        Student s = go.AddComponent<Student>();
        s.SetupFromProfile(profile, game, queue);
        return s;
    }

    public void SetupFromProfile(StudentProfile p, GameManager g, QueueManager q)
    {
        profile = p;
        game = g;
        queue = q;

        transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        patienceMax = p.patience;
        patience = p.patience;
        studyMax = p.studyTime;
        studyLeft = p.studyTime;
        bobPhase = Random.Range(0f, 6.28f);
        attentionDone = false;
        needsAttention = false;
        careerConflict = false;
        readyPenalized = false;
        sharedDouble = false;
        needsCharge = g != null && Random.value < g.ChargeChance;
        missingCharge = false;
        chargePenalized = false;
        distracted = g != null && Random.value < g.DistractionChance;

        LoadNPCSprites();
        if (npcIndex < 0 || npcIndex >= 6)
        {
            npcIndex = Random.Range(0, 6);
        }
        if (frontSprites != null && frontSprites[npcIndex] != null) frontSprite = frontSprites[npcIndex];
        if (backSprites != null && backSprites[npcIndex] != null) backSprite = backSprites[npcIndex];

        if (visual == null) BuildVisual();
        if (chargeRoot != null) chargeRoot.SetActive(needsCharge);
    }

    void BuildVisual()
    {
        LoadNPCSprites();
        if (npcIndex < 0 || npcIndex >= 6)
        {
            npcIndex = Random.Range(0, 6);
        }
        if (frontSprite == null && frontSprites != null && frontSprites[npcIndex] != null)
            frontSprite = frontSprites[npcIndex];
        if (backSprite == null && backSprites != null && backSprites[npcIndex] != null)
            backSprite = backSprites[npcIndex];

        // Objeto Visual con UNICO SpriteRenderer en capa Default y material Sprite-Lit-Default
        GameObject v = new GameObject("Visual");
        v.transform.SetParent(transform, false);
        visual = v.transform;

        body = visual.gameObject.AddComponent<SpriteRenderer>();
        body.sprite = frontSprite;
        body.sortingLayerName = "Default";
        body.sortingOrder = RenderOrder.StandingStudent;

        Material litMat = GetLitMaterial();
        if (litMat != null) body.sharedMaterial = litMat;

        // Objeto FloatingUI para datos flotantes en Sorting Layer "UI" (aislado de iluminacion 2D)
        GameObject fui = new GameObject("FloatingUI");
        fui.transform.SetParent(transform, false);
        floatingUI = fui.transform;

        Color dark = new Color(0.05f, 0.06f, 0.09f, 1f);
        BuildCareerLabel(dark);

        // Barra de paciencia / estudio
        SpriteRenderer bFondo = PixelFactory.Quad("BarraFondo", floatingUI, new Vector3(0f, 0.78f, 0f), new Vector2(0.82f, 0.13f), new Color(0.05f, 0.06f, 0.09f, 0.95f), 101);
        barBg = PixelFactory.Quad("BarraInterior", floatingUI, new Vector3(0f, 0.78f, 0f), new Vector2(0.76f, 0.08f), new Color(0.18f, 0.20f, 0.26f), 102);
        barFill = PixelFactory.Quad("BarraFill", floatingUI, new Vector3(0f, 0.78f, 0f), new Vector2(0.76f, 0.08f), new Color(0.35f, 0.9f, 0.45f), 103);

        SetupFloatingRenderer(bFondo, 101);
        SetupFloatingRenderer(barBg, 102);
        SetupFloatingRenderer(barFill, 103);

        // Marcas de estado
        alertBox = PixelFactory.Quad("Alerta", floatingUI, new Vector3(0f, 1.04f, 0f), new Vector2(0.22f, 0.22f), new Color(1f, 0.85f, 0.25f), 105);
        SetupFloatingRenderer(alertBox, 105);
        alertBox.gameObject.SetActive(false);

        readyBox = PixelFactory.Quad("Listo", floatingUI, new Vector3(0f, 1.04f, 0f), new Vector2(0.24f, 0.24f), new Color(0.35f, 1f, 0.55f), 105);
        SetupFloatingRenderer(readyBox, 105);
        readyBox.gameObject.SetActive(false);

        // Aviso de pelea: la mesa tiene alumnos de otra carrera.
        conflictBox = PixelFactory.Quad("Pelea", floatingUI, new Vector3(0.30f, 0.24f, 0f), new Vector2(0.17f, 0.17f),
            new Color(1f, 0.25f, 0.25f), 105);
        SetupFloatingRenderer(conflictBox, 105);
        conflictBox.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        conflictBox.gameObject.SetActive(false);

        // Bateria baja: icono amarillo que avisa que necesita un enchufe.
        chargeRoot = new GameObject("Bateria");
        chargeRoot.transform.SetParent(floatingUI, false);
        chargeRoot.transform.localPosition = new Vector3(-0.30f, 0.36f, 0f);

        chargeBody = PixelFactory.Quad("BateriaCuerpo", chargeRoot.transform, Vector3.zero,
            new Vector2(0.13f, 0.19f), new Color(1f, 0.86f, 0.30f), 104);
        SpriteRenderer bTip = PixelFactory.Quad("BateriaTip", chargeRoot.transform, new Vector3(0f, 0.12f, 0f),
            new Vector2(0.06f, 0.05f), new Color(1f, 0.86f, 0.30f), 104);
        SpriteRenderer bRay = PixelFactory.Quad("BateriaRayo", chargeRoot.transform, new Vector3(0f, 0f, 0f),
            new Vector2(0.035f, 0.10f), new Color(0.25f, 0.22f, 0.10f), 105);

        SetupFloatingRenderer(chargeBody, 104);
        SetupFloatingRenderer(bTip, 104);
        SetupFloatingRenderer(bRay, 105);
        chargeRoot.SetActive(false);

        BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 1.2f);
        box.offset = new Vector2(0f, 0.3f);
        col = box;
    }

    void SetupFloatingRenderer(SpriteRenderer sr, int order)
    {
        if (sr == null) return;
        sr.sortingLayerName = "UI";
        sr.sortingOrder = order;
        Material unlit = GetUnlitMaterial();
        if (unlit != null) sr.sharedMaterial = unlit;
    }

    void BuildCareerLabel(Color dark)
    {
        // Insignia con el color de la carrera: el texto va encima, oscuro,
        // para que se lea igual sobre cualquier fondo del aula.
        careerBadge = PixelFactory.Quad("CarreraFondo",
            floatingUI != null ? floatingUI : transform,
            new Vector3(0.02f, 0.55f, 0f),
            new Vector2(0.46f, 0.20f),
            Color.Lerp(StudentProfile.ColorOf(profile.career), dark, 0.18f),
            105);
        SetupFloatingRenderer(careerBadge, 105);

        GameObject labelGO = new GameObject("Carrera");
        labelGO.transform.SetParent(floatingUI != null ? floatingUI : transform, false);
        labelGO.transform.localPosition = new Vector3(0.02f, 0.55f, 0f);

        careerLabel = labelGO.AddComponent<TextMesh>();
        careerLabel.text = profile.shortName;
        careerLabel.anchor = TextAnchor.MiddleCenter;
        careerLabel.alignment = TextAlignment.Center;
        careerLabel.fontSize = 64;
        careerLabel.characterSize = 0.04f;
        careerLabel.fontStyle = FontStyle.Bold;
        careerLabel.color = new Color(dark.r, dark.g, dark.b, 0.95f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            careerLabel.font = font;
            MeshRenderer mr = labelGO.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = font.material;
                mr.sortingLayerName = "UI";
                mr.sortingOrder = 106;
            }
        }
    }

    public void ApplySeatedVisuals()
    {
        if (body != null)
        {
            body.sprite = backSprite;
            body.sortingLayerName = "Default";
            body.sortingOrder = RenderOrder.SeatedStudent;
        }
        if (visual != null)
        {
            visual.localPosition = new Vector3(0f, 0.18f, 0f);
            visual.localScale = Vector3.one;
        }
        if (floatingUI != null)
        {
            floatingUI.localPosition = new Vector3(0f, 0.18f, 0f);
        }
    }

    public void ApplyStandingVisuals(int sortingOrder = RenderOrder.StandingStudent)
    {
        if (body != null)
        {
            body.sprite = frontSprite;
            body.sortingLayerName = "Default";
            body.sortingOrder = sortingOrder;
        }
        if (visual != null)
        {
            visual.localPosition = Vector3.zero;
            visual.localScale = Vector3.one;
        }
        if (floatingUI != null)
        {
            floatingUI.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// Marca si el alumno comparte mesa con otra carrera. Lo usa la mesa para
    /// mantener el aviso de pelea en todos sus ocupantes, no solo en el ultimo.
    /// </summary>
    public void SetCareerConflict(bool conflict)
    {
        careerConflict = conflict;
        if (conflictBox != null) conflictBox.gameObject.SetActive(conflict);
    }

    // ------------------------------------------------------------------ ciclo

    void Update()
    {
        if (game == null || GameManager.Instance == null) return;
        if (game.IsPaused) return;

        switch (state)
        {
            case StudentState.InQueue:
            case StudentState.Dragging:
                TickPatience(Time.deltaTime);
                break;

            case StudentState.Seated:
                TickStudy(Time.deltaTime);
                break;

            case StudentState.Ready:
                readyTime += Time.deltaTime;
                if (readyTime > 14f) Release(false);
                break;

            case StudentState.Packing:
                TickPacking(Time.deltaTime);
                break;
        }

        // Animacion de caminado procedural: mientras se este moviendo, oscila localScale.y entre 1.0 y 1.1
        if (mover != null)
        {
            float step = Mathf.Lerp(1.0f, 1.1f, (Mathf.Sin(Time.time * 16f) + 1f) * 0.5f);
            if (visual != null) visual.localScale = new Vector3(1f, step, 1f);
        }
        else if (state != StudentState.Leaving)
        {
            if (visual != null) visual.localScale = Vector3.one;
        }

        UpdateBar();
        UpdateIdle();
        CheckHoverFallback();
    }

    bool isHovered;

    void OnMouseEnter()
    {
        SetHover(true);
    }

    void OnMouseExit()
    {
        SetHover(false);
    }

    public void SetHover(bool hover)
    {
        isHovered = hover;
        UpdateCareerAlpha();
    }

    void UpdateCareerAlpha()
    {
        if (careerLabel != null)
        {
            Color c = careerLabel.color;
            c.a = isHovered ? 1.0f : 0.85f;
            careerLabel.color = c;
        }

        if (careerBadge != null)
        {
            Color c = careerBadge.color;
            c.a = isHovered ? 1.0f : 0.70f;
            careerBadge.color = c;
        }
    }

    void CheckHoverFallback()
    {
        if (state == StudentState.Gone || state == StudentState.Leaving || state == StudentState.Dragging) return;
        if (col == null || Camera.main == null) return;
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mousePos);
        mouseWorld.z = 0f;
        bool contains = col.OverlapPoint(mouseWorld);
        if (contains != isHovered)
        {
            SetHover(contains);
        }
    }

    void TickPatience(float dt)
    {
        if (game.PatienceFrozen) return;

        // En rush llegan mas alumnos: la paciencia baja mas despacio para no castigar sin jugar.
        float rate = game.IsRush ? Mathf.Clamp(game.rushPatienceFactor, 0.1f, 1f) : 1f;

        patience -= dt * rate;
        if (patience <= 0f)
        {
            patience = 0f;
            Abandon();
        }
    }

    void TickStudy(float dt)
    {
        float speed = missingCharge && game != null ? Mathf.Clamp(game.noChargeStudyFactor, 0.1f, 1f) : 1f;
        studyLeft -= dt * speed;

        if (distracted && !attentionDone && studyLeft <= studyMax * 0.5f)
            StartDistraction();

        if (needsAttention)
        {
            attentionLeft -= dt;
            if (attentionLeft <= 0f)
            {
                LeaveEarly();
                return;
            }
        }

        if (studyLeft <= 0f)
        {
            studyLeft = 0f;
            BecomeReady();
        }
    }

    void BecomeReady()
    {
        state = StudentState.Ready;
        readyTime = 0f;
        readyPenalized = false;
        needsAttention = false;
        if (alertBox != null) alertBox.gameObject.SetActive(false);
        if (readyBox != null) readyBox.gameObject.SetActive(true);
    }

    // ------------------------------------------------------------------ salida por clics

    /// <summary>Primer clic: empieza a recoger sus cosas. Los clics siguientes aceleran.</summary>
    public void BeginPacking()
    {
        if (state != StudentState.Ready) return;

        state = StudentState.Packing;
        packMax = game != null ? Mathf.Max(0.4f, game.packDuration) : 1.4f;
        packLeft = packMax;

        AudioManager.Play(Sfx.Pack, 0.9f, 1f);
        if (readyBox != null) readyBox.gameObject.SetActive(false);
        game.SetMessage("Clic repetido para que se vaya!", 1.4f);
    }

    /// <summary>Cada clic quita tiempo de salida. Devuelve true si el clic se aprovecho.</summary>
    public bool PackClick()
    {
        if (state != StudentState.Packing) return false;

        float power = game != null ? Mathf.Max(0.05f, game.packClickPower) : 0.55f;
        packLeft -= power;

        AudioManager.Play(Sfx.Pack, 0.7f, UnityEngine.Random.Range(1.05f, 1.22f));

        if (packLeft <= 0f)
        {
            packLeft = 0f;
            Release(true);
        }

        return true;
    }

    void TickPacking(float dt)
    {
        packLeft -= dt;
        if (packLeft <= 0f) Release(true);
    }

    void StartDistraction()
    {
        attentionDone = true;
        needsAttention = true;
        attentionLeft = 4.5f;
        AudioManager.Play(Sfx.Distraction, 0.85f, UnityEngine.Random.Range(0.95f, 1.05f));
        if (alertBox != null) alertBox.gameObject.SetActive(true);
    }

    public void ResolveDistraction()
    {
        if (!needsAttention) return;
        needsAttention = false;
        if (alertBox != null) alertBox.gameObject.SetActive(false);
        game.RegisterAttention();
    }

    void UpdateBar()
    {
        if (barFill == null) return;

        float frac;
        Color c;

        if (state == StudentState.Packing)
        {
            // Barra azul: progreso de "recoger cosas". Los clics la llenan rapido.
            frac = PackFraction;
            c = new Color(0.55f, 0.90f, 1f);
        }
        else if (state == StudentState.Seated || state == StudentState.Ready)
        {
            frac = 1f - Mathf.Clamp01(studyLeft / Mathf.Max(0.01f, studyMax));
            c = Color.Lerp(new Color(0.35f, 0.60f, 1f), new Color(0.40f, 1f, 0.55f), frac);
        }
        else
        {
            frac = PatienceFraction;
            c = Color.Lerp(new Color(1f, 0.28f, 0.28f), new Color(0.40f, 0.95f, 0.45f), frac);
        }

        frac = Mathf.Clamp01(frac);
        barFill.color = c;
        barFill.transform.localScale = new Vector3(0.76f * frac, 0.08f, 1f);
        barFill.transform.localPosition = new Vector3(-0.38f * (1f - frac), 0.78f, 0f);

        if (readyBox != null && state == StudentState.Ready)
        {
            float a = 0.55f + Mathf.PingPong(Time.time * 2.5f, 0.45f);
            readyBox.color = new Color(0.35f, 1f, 0.55f, a);
        }
    }

    void UpdateIdle()
    {
        if (visual == null) return;

        if (state == StudentState.InQueue)
        {
            float bob = Mathf.Sin(Time.time * 4f + bobPhase) * 0.035f;
            visual.localPosition = new Vector3(0f, bob, 0f);
        }

        bool urgent = (state == StudentState.InQueue || state == StudentState.Dragging) && PatienceFraction < 0.22f;
        if (urgent)
            visual.localPosition = new Vector3(Mathf.Sin(Time.time * 45f) * 0.035f, visual.localPosition.y, 0f);
        else if (state != StudentState.InQueue)
        {
            float targetY = (state == StudentState.Seated || state == StudentState.Ready || state == StudentState.Packing) ? 0.18f : 0f;
            visual.localPosition = new Vector3(0f, targetY, 0f);
        }

        if (needsAttention && alertBox != null)
        {
            float a = 0.55f + Mathf.PingPong(Time.time * 5f, 0.45f);
            alertBox.color = new Color(1f, 0.85f, 0.25f, a);
        }

        if (conflictBox != null && conflictBox.gameObject.activeSelf)
        {
            float a = 0.55f + Mathf.PingPong(Time.time * 6f, 0.45f);
            conflictBox.color = new Color(1f, 0.25f, 0.25f, a);
        }

        if (chargeRoot != null && chargeRoot.activeSelf && chargeBody != null)
        {
            if (missingCharge)
            {
                float a = 0.55f + Mathf.PingPong(Time.time * 6f, 0.45f);
                chargeBody.color = new Color(1f, 0.22f, 0.22f, a);
            }
            else if (seat != null && seat.hasOutlet)
            {
                chargeBody.color = new Color(0.35f, 1f, 0.50f);
            }
            else
            {
                chargeBody.color = new Color(1f, 0.86f, 0.30f);
            }
        }
    }

    // ------------------------------------------------------------------ acciones

    public void BeginDrag()
    {
        if (mover != null)
        {
            StopCoroutine(mover);
            mover = null;
        }
        state = StudentState.Dragging;
        transform.SetParent(null);
        if (col != null) col.enabled = false;
        ApplyStandingVisuals(RenderOrder.DraggedStudent);
        game.SetDragOrder(this, true);
        AudioManager.Play(Sfx.Pickup, 0.8f, UnityEngine.Random.Range(0.96f, 1.05f));
    }

    public void CancelDrag(Vector3 queuePosition)
    {
        state = StudentState.InQueue;
        if (col != null) col.enabled = true;
        ApplyStandingVisuals(RenderOrder.StandingStudent);
        game.SetDragOrder(this, false);
        SetHover(false);
        WalkTo(queuePosition, 12f);
    }

    public void ReturnToSeat()
    {
        state = StudentState.Seated;
        if (col != null) col.enabled = true;
        ApplyStandingVisuals(RenderOrder.StandingStudent);
        game.SetDragOrder(this, false);
        SetHover(false);
        if (seat != null) WalkTo(seat.SpotPosition(Mathf.Max(0, seat.occupants.IndexOf(this))), 12f);
    }

    public bool AssignTo(Seat s)
    {
        if (s == null || !s.CanAccept(profile)) return false;

        if (seat != null) seat.RemoveOccupant(this);
        if (queue != null) queue.RemoveStudent(this);

        seat = s;
        int idx = s.AddOccupant(this);
        state = StudentState.Seated;
        studyLeft = studyMax;
        attentionDone = false;
        needsAttention = false;
        careerConflict = !s.MatchesProfile(profile);
        missingCharge = needsCharge && !s.hasOutlet;
        bool penalizeCharge = missingCharge && !chargePenalized;
        if (penalizeCharge) chargePenalized = true;
        readyPenalized = false;
        if (alertBox != null) alertBox.gameObject.SetActive(false);
        if (conflictBox != null) conflictBox.gameObject.SetActive(careerConflict);
        if (readyBox != null) readyBox.gameObject.SetActive(false);
        if (col != null) col.enabled = true;
        ApplyStandingVisuals(RenderOrder.StandingStudent);
        game.SetDragOrder(this, false);
        SetHover(false);

        // Camina rapido a su sitio: el turno ahora es mas agil.
        WalkTo(s.SpotPosition(idx), 11f);
        game.RegisterAssigned(this);
        if (penalizeCharge) game.RegisterNoCharger(this);
        return true;
    }

    public void Release(bool withBonus = true)
    {
        if (state != StudentState.Seated && state != StudentState.Ready && state != StudentState.Packing) return;

        bool pair = seat != null && seat.pairBonus;
        studyLeft = Mathf.Max(0f, studyLeft);

        if (seat != null)
        {
            seat.RemoveOccupant(this);
            seat.MarkDirty();
            seat = null;
        }

        careerConflict = false;
        game.RegisterHappy(this, PatienceFraction, pair, withBonus);

        state = StudentState.Leaving;
        if (col != null) col.enabled = false;
        if (readyBox != null) readyBox.gameObject.SetActive(false);
        if (conflictBox != null) conflictBox.gameObject.SetActive(false);
        StartCoroutine(ExitRoutine(new Color(0.55f, 1f, 0.60f)));
    }

    public void Abandon()
    {
        if (state == StudentState.Leaving || state == StudentState.Gone) return;

        if (seat != null)
        {
            seat.RemoveOccupant(this);
            seat = null;
        }
        if (queue != null) queue.RemoveStudent(this);

        game.RegisterAbandon(this);

        state = StudentState.Leaving;
        if (col != null) col.enabled = false;
        if (readyBox != null) readyBox.gameObject.SetActive(false);
        if (conflictBox != null) conflictBox.gameObject.SetActive(false);
        StartCoroutine(ExitRoutine(new Color(1f, 0.35f, 0.35f)));
    }

    void LeaveEarly()
    {
        if (state != StudentState.Seated) return;

        if (seat != null)
        {
            seat.RemoveOccupant(this);
            seat.MarkDirty();
            seat = null;
        }

        game.RegisterEarlyLeave(this);

        state = StudentState.Leaving;
        if (col != null) col.enabled = false;
        if (alertBox != null) alertBox.gameObject.SetActive(false);
        if (conflictBox != null) conflictBox.gameObject.SetActive(false);
        StartCoroutine(ExitRoutine(new Color(1f, 0.65f, 0.30f)));
    }

    IEnumerator ExitRoutine(Color flash)
    {
        ApplyStandingVisuals(RenderOrder.StandingStudent);
        if (body != null) body.color = flash;
        Vector3 door = game != null ? game.DoorPosition : transform.position + Vector3.down * 3f;

        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            float step = Mathf.Lerp(1.0f, 1.1f, (Mathf.Sin(Time.time * 16f) + 1f) * 0.5f);
            if (visual != null) visual.localScale = new Vector3(1f, step, 1f);
            transform.position = Vector3.MoveTowards(transform.position, door, 8f * Time.deltaTime);
            if (body != null)
            {
                Color c = body.color;
                c.a = Mathf.Lerp(1f, 0.15f, t / 1.2f);
                body.color = c;
            }
            yield return null;
        }

        state = StudentState.Gone;
        Destroy(gameObject);
    }

    public void WalkTo(Vector3 target, float speed)
    {
        if (!gameObject.activeInHierarchy) return;
        if (mover != null) StopCoroutine(mover);
        mover = StartCoroutine(WalkRoutine(target, speed));
    }

    IEnumerator WalkRoutine(Vector3 target, float speed)
    {
        while ((transform.position - target).sqrMagnitude > 0.003f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
        mover = null;
        if (visual != null) visual.localScale = Vector3.one;

        if (state == StudentState.Seated)
        {
            ApplySeatedVisuals();
        }
        else
        {
            ApplyStandingVisuals(state == StudentState.Dragging ? RenderOrder.DraggedStudent : RenderOrder.StandingStudent);
        }
    }
}
