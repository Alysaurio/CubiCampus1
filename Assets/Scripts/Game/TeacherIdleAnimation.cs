using UnityEngine;

/// <summary>
/// Animacion idle suave para el profesor, simulando respiracion al oscilar
/// transform.localScale.y entre minScaleY y maxScaleY manteniendo la escala base configurada.
/// </summary>
public class TeacherIdleAnimation : MonoBehaviour
{
    public float velocidad = 2.5f;
    public float minScaleY = 1.0f;
    public float maxScaleY = 1.05f;

    float baseScaleX = 1.8f;
    float baseScaleY = 2.0f;
    float baseScaleZ = 1f;

    void Awake()
    {
        baseScaleX = transform.localScale.x;
        baseScaleY = transform.localScale.y;
        baseScaleZ = transform.localScale.z;
        if (Mathf.Approximately(baseScaleX, 0f)) baseScaleX = 1.8f;
        if (Mathf.Approximately(baseScaleY, 0f)) baseScaleY = 2.0f;
        if (Mathf.Approximately(baseScaleZ, 0f)) baseScaleZ = 1f;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * velocidad) + 1f) * 0.5f;
        float factorY = Mathf.Lerp(minScaleY, maxScaleY, t);
        transform.localScale = new Vector3(baseScaleX, baseScaleY * factorY, baseScaleZ);
    }
}
