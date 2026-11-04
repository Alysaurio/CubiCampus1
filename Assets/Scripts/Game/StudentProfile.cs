using System;
using UnityEngine;

/// <summary>
/// Carreras de Tecsup. Cada alumno pertenece a una carrera y esa es ahora su
/// identidad principal en la sala (antes eran "tipos" identificados por color).
/// </summary>
public enum Career
{
    MecatronicaIndustrial,
    MecatronicaGestionAutomotriz,
    MarketingDigitalAnalitico,
    OperacionPlantasMinerales,
    GestionSeguridadSaludTrabajo,
    DisenoSimuladoresVideojuegos
}

/// <summary>
/// Datos de balance de cada carrera. Todas las carreras pueden sentarse en
/// cualquier mesa, pero compartir mesa con otra carrera provoca una pelea.
/// </summary>
[Serializable]
public class StudentProfile
{
    public Career career;
    public string displayName;
    public string shortName;
    public int basePoints;
    public float studyTime;
    public float patience;
    public Color color;

    public static readonly Career[] AllCareers =
    {
        Career.MecatronicaIndustrial,
        Career.MecatronicaGestionAutomotriz,
        Career.MarketingDigitalAnalitico,
        Career.OperacionPlantasMinerales,
        Career.GestionSeguridadSaludTrabajo,
        Career.DisenoSimuladoresVideojuegos
    };

    /// <summary>Codigo corto de la carrera (MEC, AUT, MKT, MIN, SST, SIM).</summary>
    public static string ShortCode(Career career)
    {
        switch (career)
        {
            case Career.MecatronicaIndustrial: return "MEC";
            case Career.MecatronicaGestionAutomotriz: return "AUT";
            case Career.MarketingDigitalAnalitico: return "MKT";
            case Career.OperacionPlantasMinerales: return "MIN";
            case Career.GestionSeguridadSaludTrabajo: return "SST";
            case Career.DisenoSimuladoresVideojuegos: return "SIM";
        }
        return "?";
    }

    /// <summary>Color identificador de la carrera (tambien lo usan las mesas grupales).</summary>
    public static Color ColorOf(Career career)
    {
        switch (career)
        {
            case Career.MecatronicaIndustrial: return new Color(1.00f, 0.56f, 0.20f);
            case Career.MecatronicaGestionAutomotriz: return new Color(0.30f, 0.70f, 1.00f);
            case Career.MarketingDigitalAnalitico: return new Color(1.00f, 0.38f, 0.72f);
            case Career.OperacionPlantasMinerales: return new Color(0.92f, 0.76f, 0.26f);
            case Career.GestionSeguridadSaludTrabajo: return new Color(0.35f, 0.90f, 0.50f);
            case Career.DisenoSimuladoresVideojuegos: return new Color(0.66f, 0.50f, 1.00f);
        }
        return Color.white;
    }

    public static StudentProfile Create(Career career)
    {
        StudentProfile p = new StudentProfile();
        p.career = career;

        switch (career)
        {
            case Career.MecatronicaIndustrial:
                p.displayName = "Mecatronica Industrial";
                p.shortName = "MEC";
                p.basePoints = 120;
                p.studyTime = 12f;
                p.patience = 14f;
                p.color = new Color(1.00f, 0.56f, 0.20f);
                break;

            case Career.MecatronicaGestionAutomotriz:
                p.displayName = "Mecatronica y Gestion Automotriz";
                p.shortName = "AUT";
                p.basePoints = 130;
                p.studyTime = 14f;
                p.patience = 15f;
                p.color = new Color(0.30f, 0.70f, 1.00f);
                break;

            case Career.MarketingDigitalAnalitico:
                p.displayName = "Marketing Digital Analitico";
                p.shortName = "MKT";
                p.basePoints = 105;
                p.studyTime = 10f;
                p.patience = 13f;
                p.color = new Color(1.00f, 0.38f, 0.72f);
                break;

            case Career.OperacionPlantasMinerales:
                p.displayName = "Operacion de Plantas de Procesamiento de Minerales";
                p.shortName = "MIN";
                p.basePoints = 145;
                p.studyTime = 16f;
                p.patience = 17f;
                p.color = new Color(0.92f, 0.76f, 0.26f);
                break;

            case Career.GestionSeguridadSaludTrabajo:
                p.displayName = "Gestion de Seguridad y Salud en el Trabajo";
                p.shortName = "SST";
                p.basePoints = 100;
                p.studyTime = 11f;
                p.patience = 14f;
                p.color = new Color(0.35f, 0.90f, 0.50f);
                break;

            case Career.DisenoSimuladoresVideojuegos:
                p.displayName = "Diseno y Desarrollo de Simuladores y Videojuegos";
                p.shortName = "SIM";
                p.basePoints = 150;
                p.studyTime = 15f;
                p.patience = 16f;
                p.color = new Color(0.66f, 0.50f, 1.00f);
                break;
        }

        return p;
    }

    public StudentProfile Clone()
    {
        return (StudentProfile)MemberwiseClone();
    }
}
