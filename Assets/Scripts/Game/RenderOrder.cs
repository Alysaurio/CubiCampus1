/// <summary>
/// Orden de dibujado unico para el aula. La decoracion (piso, paredes, props,
/// mesas y sillas) queda siempre por debajo del gameplay (alumnos, etiquetas y
/// arrastre) para que ningun elemento de fondo tape a los actores.
/// </summary>
public static class RenderOrder
{
    public const int Floor = -20;
    public const int Trim = -18;
    public const int WallDecor = -16;
    public const int Prop = -14;

    public const int Table = 0;
    public const int SeatBorder = 2;
    public const int Chair = 2;
    public const int SeatFill = 3;
    public const int SeatMarker = 7;

    public const int SeatedStudent = 8;
    public const int StandingStudent = 20;
    public const int DraggedStudent = 60;
}
