using System;
using UnityEngine;

/// <summary>
/// Configuracion de un dia de la campana: umbral, duracion del turno, ritmo de
/// llegada de alumnos, frecuencia de apartados, paciencia y mesas activas.
/// El ritmo es mas corto e intenso para que el turno tenga mas energia.
/// </summary>
[Serializable]
public class DayConfig
{
    public int dia;
    public string titulo;

    [Header("Objetivo")]
    public float umbralEficiencia = 60f;

    [Header("Turno")]
    public float turnDuration = 95f;
    public float rushLastSeconds = 20f;

    [Header("Llegada de alumnos")]
    public float spawnInterval = 3.2f;
    public float firstSpawnDelay = 0.6f;

    [Header("Apartados")]
    public float apartadoMin = 32f;
    public float apartadoMax = 44f;

    [Header("Paciencia")]
    [Tooltip("Multiplicador sobre la paciencia base de cada carrera. Los primeros dias son mas tolerantes.")]
    public float patienceMultiplier = 1.25f;

    [Header("Sala")]
    [Tooltip("Mesas activas: 8 en el tutorial y hasta 24 en el dia 10.")]
    public int activeSeats = 8;

    [Header("Distracciones")]
    [Tooltip("Probabilidad de que un alumno se distraiga a mitad del estudio.")]
    [Range(0f, 1f)] public float distractionChance = 0.12f;

    [Header("Cargadores")]
    [Tooltip("Probabilidad de que un alumno llegue con bateria baja y necesite enchufe.")]
    [Range(0f, 1f)] public float chargeChance = 0.18f;
}

/// <summary>
/// Campana de 10 dias en la misma sala. La dificultad sube de forma escalada:
/// mas alumnos, apartados mas frecuentes, rush mas largo y mas mesas abiertas.
/// El dia actual se guarda en PlayerPrefs con la clave "DiaActual".
/// </summary>
public static class DayProgression
{
    public const string KeyDiaActual = "DiaActual";

    static readonly DayConfig[] dias =
    {
        // dia 1 - tutorial: 8 mesas, ritmo tranquilo pero ya con energia
        new DayConfig
        {
            dia = 1, titulo = "Primer turno",
            umbralEficiencia = 60f,
            turnDuration = 95f, rushLastSeconds = 20f,
            spawnInterval = 3.2f, firstSpawnDelay = 0.6f,
            apartadoMin = 32f, apartadoMax = 44f,
            patienceMultiplier = 1.25f, activeSeats = 8,
            distractionChance = 0.12f,
            chargeChance = 0.18f
        },
        // dia 2 - mas gente y primeras peleas de carreras
        new DayConfig
        {
            dia = 2, titulo = "Ritmo de biblioteca",
            umbralEficiencia = 60f,
            turnDuration = 98f, rushLastSeconds = 22f,
            spawnInterval = 3.0f, firstSpawnDelay = 0.6f,
            apartadoMin = 30f, apartadoMax = 42f,
            patienceMultiplier = 1.18f, activeSeats = 10,
            distractionChance = 0.14f,
            chargeChance = 0.20f
        },
        // dia 3 - se abren 12 mesas
        new DayConfig
        {
            dia = 3, titulo = "Sala completa",
            umbralEficiencia = 72f,
            turnDuration = 100f, rushLastSeconds = 22f,
            spawnInterval = 2.8f, firstSpawnDelay = 0.55f,
            apartadoMin = 28f, apartadoMax = 40f,
            patienceMultiplier = 1.10f, activeSeats = 12,
            distractionChance = 0.16f,
            chargeChance = 0.22f
        },
        // dia 4
        new DayConfig
        {
            dia = 4, titulo = "Semana de examenes",
            umbralEficiencia = 72f,
            turnDuration = 104f, rushLastSeconds = 24f,
            spawnInterval = 2.6f, firstSpawnDelay = 0.55f,
            apartadoMin = 26f, apartadoMax = 37f,
            patienceMultiplier = 1.05f, activeSeats = 14,
            distractionChance = 0.18f,
            chargeChance = 0.22f
        },
        // dia 5
        new DayConfig
        {
            dia = 5, titulo = "Punto medio",
            umbralEficiencia = 72f,
            turnDuration = 106f, rushLastSeconds = 24f,
            spawnInterval = 2.45f, firstSpawnDelay = 0.5f,
            apartadoMin = 24f, apartadoMax = 34f,
            patienceMultiplier = 1f, activeSeats = 16,
            distractionChance = 0.20f,
            chargeChance = 0.24f
        },
        // dia 6
        new DayConfig
        {
            dia = 6, titulo = "Aforo tensionado",
            umbralEficiencia = 74f,
            turnDuration = 110f, rushLastSeconds = 26f,
            spawnInterval = 2.3f, firstSpawnDelay = 0.5f,
            apartadoMin = 22f, apartadoMax = 32f,
            patienceMultiplier = 0.96f, activeSeats = 18,
            distractionChance = 0.22f,
            chargeChance = 0.26f
        },
        // dia 7
        new DayConfig
        {
            dia = 7, titulo = "Vigilia de biblioteca",
            umbralEficiencia = 74f,
            turnDuration = 112f, rushLastSeconds = 26f,
            spawnInterval = 2.15f, firstSpawnDelay = 0.5f,
            apartadoMin = 20f, apartadoMax = 30f,
            patienceMultiplier = 0.93f, activeSeats = 20,
            distractionChance = 0.24f,
            chargeChance = 0.27f
        },
        // dia 8 - 22 mesas
        new DayConfig
        {
            dia = 8, titulo = "Choque de carreras",
            umbralEficiencia = 74f,
            turnDuration = 115f, rushLastSeconds = 28f,
            spawnInterval = 2.0f, firstSpawnDelay = 0.45f,
            apartadoMin = 18f, apartadoMax = 28f,
            patienceMultiplier = 0.90f, activeSeats = 22,
            distractionChance = 0.26f,
            chargeChance = 0.28f
        },
        // dia 9
        new DayConfig
        {
            dia = 9, titulo = "Casi al limite",
            umbralEficiencia = 76f,
            turnDuration = 118f, rushLastSeconds = 28f,
            spawnInterval = 1.9f, firstSpawnDelay = 0.45f,
            apartadoMin = 17f, apartadoMax = 26f,
            patienceMultiplier = 0.88f, activeSeats = 23,
            distractionChance = 0.28f,
            chargeChance = 0.30f
        },
        // dia 10 - final: las 24 mesas abiertas
        new DayConfig
        {
            dia = 10, titulo = "Aforo al limite",
            umbralEficiencia = 76f,
            turnDuration = 122f, rushLastSeconds = 30f,
            spawnInterval = 1.8f, firstSpawnDelay = 0.4f,
            apartadoMin = 16f, apartadoMax = 24f,
            patienceMultiplier = 0.85f, activeSeats = 24,
            distractionChance = 0.30f,
            chargeChance = 0.32f
        }
    };

    public static int TotalDias { get { return dias.Length; } }

    /// <summary>Configuracion del dia indicado (1..10), con recorte automatico.</summary>
    public static DayConfig Get(int dia)
    {
        int i = Mathf.Clamp(dia, 1, dias.Length) - 1;
        return dias[i];
    }

    public static DayConfig PrimerDia { get { return dias[0]; } }

    public static int SiguienteDia(int dia)
    {
        return Mathf.Clamp(dia + 1, 1, dias.Length);
    }

    public static int DiaGuardado()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(KeyDiaActual, 1), 1, dias.Length);
    }

    public static void GuardarDia(int dia)
    {
        PlayerPrefs.SetInt(KeyDiaActual, Mathf.Clamp(dia, 1, dias.Length));
        PlayerPrefs.Save();
    }

    public static void Reiniciar()
    {
        GuardarDia(1);
    }

    /// <summary>Texto corto para HUD y menu: "DIA 3 / 10 - META 72%".</summary>
    public static string Etiqueta(int dia)
    {
        DayConfig c = Get(dia);
        return "DIA " + c.dia + " / " + dias.Length + "  -  META " + Mathf.RoundToInt(c.umbralEficiencia) + "%";
    }
}
