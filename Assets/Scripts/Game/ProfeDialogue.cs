using System.Collections;
using System.Text;
using UnityEngine;

/// <summary>
/// Dialogos del profesor. Muestra frases aleatorias cada 12 segundos y una
/// secuencia especial al empezar el modo rush. El bocadillo se dibuja sobre
/// el NPC y se cancela/reinicia para que nunca se solapen dos mensajes.
/// </summary>
public class ProfeDialogue : MonoBehaviour
{
    [Header("Referencias")]
    public GameManager game;
    public Transform profe;

    [Header("Tiempos")]
    public float intervaloNormal = 12f;
    public float duracionBocadillo = 4f;
    public float esperaRush = 3f;

    [Header("Bocadillo")]
    public float anchoBocadillo = 5.6f;
    public float alturaMinima = 0.8f;
    public float alturaPorLinea = 0.36f;
    public int caracteresPorLinea = 30;

    // Frases normales que se eligen al azar cada intervalo.
    static readonly string[] frasesNormales =
    {
        "¿Cómo estan muchanchos?",
        "¡Nooo mis tokens!",
        "¿Quien rompió la pantalla?",
        "Wazaaaa",
        "¡No halen, que los regreso a primer ciclo!",
        "Si usan HTML los jalo"
    };

    // Secuencia exacta del modo rush.
    static readonly string[] frasesRush =
    {
        "¡Muchanchos ya va a terminar la clase pusheen sus cambios!",
        "Ah, esperen acá no hay computadoras jaja"
    };

    const string SortingLayerBocadillo = "UI";
    const int OrdenBorde = 112;
    const int OrdenFondo = 113;
    const int OrdenTexto = 114;

    Transform bubbleRoot;
    SpriteRenderer bubbleBorder;
    SpriteRenderer bubbleFill;
    SpriteRenderer bubbleTailBorder;
    SpriteRenderer bubbleTailFill;
    TextMesh bubbleText;
    MeshRenderer bubbleTextRenderer;

    float normalTimer;
    float bubbleTimer;
    bool rushAnunciado;
    bool rushSecuenciaActiva;
    int ultimoIndice = -1;

    void Awake()
    {
        if (game == null) game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (profe == null)
        {
            GameObject go = GameObject.Find("Profesor");
            if (go != null) profe = go.transform;
        }

        CrearBocadillo();
        OcultarBocadillo();
    }

    void Update()
    {
        if (game == null) return;

        bool rushAhora = game.IsRush && game.IsPlaying;

        // Al entrar en rush se interrumpe el timer normal y suena la secuencia.
        if (rushAhora && !rushAnunciado && !rushSecuenciaActiva)
        {
            rushAnunciado = true;
            StartCoroutine(SecuenciaRush());
        }
        if (!rushAhora && !rushSecuenciaActiva) rushAnunciado = false;

        // El timer normal solo corre con la partida activa y fuera de rush.
        if (game.IsPlaying && !game.IsPaused && !rushAhora && !rushSecuenciaActiva)
        {
            normalTimer += Time.deltaTime;
            if (normalTimer >= intervaloNormal)
            {
                normalTimer = 0f;
                MostrarFraseAleatoria();
            }
        }

        // El bocadillo se oculta solo; el timer usa tiempo de juego para que
        // la pausa congele tambien el mensaje.
        if (bubbleTimer > 0f)
        {
            bubbleTimer -= Time.deltaTime;
            if (bubbleTimer <= 0f) OcultarBocadillo();
        }
    }

    void LateUpdate()
    {
        if (bubbleRoot != null && profe != null)
        {
            bubbleRoot.position = profe.position + Vector3.up * 1.55f;
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        rushSecuenciaActiva = false;
    }

    IEnumerator SecuenciaRush()
    {
        rushSecuenciaActiva = true;
        normalTimer = 0f;

        MostrarBocadillo(frasesRush[0]);
        yield return new WaitForSeconds(esperaRush);
        MostrarBocadillo(frasesRush[1]);

        yield return new WaitForSeconds(duracionBocadillo);
        OcultarBocadillo();

        rushSecuenciaActiva = false;
        normalTimer = 0f;
    }

    void MostrarFraseAleatoria()
    {
        if (frasesNormales.Length == 0) return;

        int indice = Random.Range(0, frasesNormales.Length);
        if (frasesNormales.Length > 1 && indice == ultimoIndice)
        {
            indice = (indice + 1) % frasesNormales.Length;
        }

        ultimoIndice = indice;
        MostrarBocadillo(frasesNormales[indice]);
    }

    void MostrarBocadillo(string frase)
    {
        if (bubbleRoot == null || bubbleText == null) return;

        string texto = AjustarLineas(frase, caracteresPorLinea);
        int lineas = 1;
        for (int i = 0; i < texto.Length; i++)
        {
            if (texto[i] == '\n') lineas++;
        }

        float alto = Mathf.Max(alturaMinima, 0.36f + lineas * alturaPorLinea);
        PixelFactory.SetSize(bubbleBorder, new Vector2(anchoBocadillo + 0.14f, alto + 0.12f));
        PixelFactory.SetSize(bubbleFill, new Vector2(anchoBocadillo, alto));

        // La cola queda debajo del globo, apuntando al profesor.
        Vector3 colaPos = new Vector3(0f, -alto * 0.5f - 0.10f, 0f);
        if (bubbleTailBorder != null) bubbleTailBorder.transform.localPosition = colaPos;
        if (bubbleTailFill != null) bubbleTailFill.transform.localPosition = colaPos;

        bubbleText.text = texto;
        bubbleRoot.gameObject.SetActive(true);
        bubbleTimer = duracionBocadillo;
    }

    void OcultarBocadillo()
    {
        bubbleTimer = 0f;
        if (bubbleRoot != null) bubbleRoot.gameObject.SetActive(false);
    }

    void CrearBocadillo()
    {
        GameObject rootGO = new GameObject("BocadilloProfe");
        bubbleRoot = rootGO.transform;
        bubbleRoot.SetParent(profe != null ? profe.parent : transform, false);
        bubbleRoot.position = (profe != null ? profe.position : transform.position) + Vector3.up * 1.55f;

        Color borde = new Color(0.08f, 0.09f, 0.14f, 0.96f);
        Color fondo = new Color(0.99f, 0.98f, 0.92f, 0.98f);

        bubbleBorder = PixelFactory.Quad("Borde", bubbleRoot, Vector3.zero,
            new Vector2(anchoBocadillo + 0.14f, alturaMinima + 0.12f), borde, OrdenBorde);
        bubbleFill = PixelFactory.Quad("Fondo", bubbleRoot, Vector3.zero,
            new Vector2(anchoBocadillo, alturaMinima), fondo, OrdenFondo);

        // Cola del bocadillo: dos cuadrados girados 45 grados, uno de borde y
        // otro de relleno, para integrarse con el globo.
        bubbleTailBorder = PixelFactory.Quad("ColaBorde", bubbleRoot, Vector3.zero,
            new Vector2(0.34f, 0.34f), borde, OrdenBorde);
        bubbleTailBorder.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        bubbleTailFill = PixelFactory.Quad("ColaFondo", bubbleRoot, Vector3.zero,
            new Vector2(0.26f, 0.26f), fondo, OrdenFondo);
        bubbleTailFill.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        GameObject textoGO = new GameObject("Texto");
        textoGO.transform.SetParent(bubbleRoot, false);
        textoGO.transform.localPosition = new Vector3(0f, 0.02f, 0f);

        bubbleText = textoGO.AddComponent<TextMesh>();
        bubbleText.text = "";
        bubbleText.anchor = TextAnchor.MiddleCenter;
        bubbleText.alignment = TextAlignment.Center;
        bubbleText.fontSize = 64;
        bubbleText.characterSize = 0.045f;
        bubbleText.fontStyle = FontStyle.Bold;
        bubbleText.color = new Color(0.08f, 0.09f, 0.14f, 1f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null) bubbleText.font = font;

        bubbleTextRenderer = textoGO.GetComponent<MeshRenderer>();
        if (bubbleTextRenderer != null)
        {
            if (font != null) bubbleTextRenderer.sharedMaterial = font.material;
            bubbleTextRenderer.sortingLayerName = SortingLayerBocadillo;
            bubbleTextRenderer.sortingOrder = OrdenTexto;
        }

        ConfigurarRenderer(bubbleBorder, OrdenBorde);
        ConfigurarRenderer(bubbleFill, OrdenFondo);
        ConfigurarRenderer(bubbleTailBorder, OrdenBorde);
        ConfigurarRenderer(bubbleTailFill, OrdenFondo);
    }

    static void ConfigurarRenderer(SpriteRenderer sr, int orden)
    {
        if (sr == null) return;
        sr.sortingLayerName = SortingLayerBocadillo;
        sr.sortingOrder = orden;

        // La capa UI no aisla la iluminacion 2D: usamos el material unlit para
        // que el bocadillo conserve su color crema aunque el aula este oscura.
        Material unlit = Student.GetUnlitMaterial();
        if (unlit != null) sr.sharedMaterial = unlit;
    }

    // Ajusta el texto a varias lineas sin cortar palabras.
    static string AjustarLineas(string frase, int maximo)
    {
        if (string.IsNullOrEmpty(frase)) return "";

        string[] palabras = frase.Split(' ');
        StringBuilder sb = new StringBuilder(frase.Length + 4);
        int largoLinea = 0;

        for (int i = 0; i < palabras.Length; i++)
        {
            string palabra = palabras[i];
            if (palabra.Length == 0) continue;

            if (largoLinea == 0)
            {
                sb.Append(palabra);
                largoLinea = palabra.Length;
            }
            else if (largoLinea + 1 + palabra.Length <= maximo)
            {
                sb.Append(' ').Append(palabra);
                largoLinea += 1 + palabra.Length;
            }
            else
            {
                sb.Append('\n').Append(palabra);
                largoLinea = palabra.Length;
            }
        }

        return sb.ToString();
    }
}
