using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera estudiantes de las seis carreras de Tecsup y gestiona la fila FIFO
/// de 4 puestos visibles. El ritmo es mucho mas rapido que en la version anterior.
/// </summary>
public class QueueManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameManager game;
    public Transform doorPoint;
    public Transform[] slots;

    [Header("Prefab")]
    public GameObject estudiantePrefab;

    [Header("Balance")]
    public float spawnInterval = 2.6f;
    public float firstSpawnDelay = 0.6f;

    [Header("Peso de cada carrera")]
    public float[] careerWeights = { 1f, 1f, 1f, 1f, 1f, 1f };

    public List<Student> Students { get { return students; } }

    readonly List<Student> students = new List<Student>();

    float timer;
    bool running;

    void Start()
    {
        timer = firstSpawnDelay;
        running = true;
    }

    void Update()
    {
        if (!running || game == null || !game.IsPlaying || game.IsPaused) return;
        if (slots == null || slots.Length == 0) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = SpawnInterval();
        if (students.Count < slots.Length) SpawnRandom();
    }

    float SpawnInterval()
    {
        float interval = spawnInterval;
        if (game != null && game.IsRush) interval /= 1.8f;
        return Mathf.Max(1.4f, interval);
    }

    public Student SpawnRandom()
    {
        Career career = PickCareer();
        return Spawn(career);
    }

    public Student Spawn(Career career)
    {
        if (slots == null || slots.Length == 0) return null;

        StudentProfile profile = game.GetProfile(career);
        if (profile == null) profile = StudentProfile.Create(career);

        Vector3 spawnPos = doorPoint != null ? doorPoint.position : new Vector3(-8f, -3.7f, 0f);
        Student s = null;

        if (estudiantePrefab != null)
        {
            GameObject go = Instantiate(estudiantePrefab, spawnPos, Quaternion.identity);
            s = go.GetComponent<Student>();
            if (s == null) s = go.AddComponent<Student>();
            s.SetupFromProfile(profile, game, this);
        }
        else
        {
            s = Student.Create(profile, spawnPos, game, this);
        }

        students.Add(s);
        int index = students.Count - 1;
        Vector3 target = SlotPosition(index);
        s.WalkTo(target, 9f);

        game.RegisterArrival(s);
        return s;
    }

    public void RemoveStudent(Student s)
    {
        if (s == null) return;
        if (!students.Remove(s)) return;

        for (int i = 0; i < students.Count; i++)
        {
            if (students[i] == null) continue;
            if (students[i].state == StudentState.InQueue)
                students[i].WalkTo(SlotPosition(i), 10f);
        }
    }

    public Vector3 SlotPosition(int index)
    {
        if (slots == null || slots.Length == 0) return Vector3.zero;
        int i = Mathf.Clamp(index, 0, slots.Length - 1);
        return slots[i] != null ? slots[i].position : Vector3.zero;
    }

    Career PickCareer()
    {
        float total = 0f;
        int count = Mathf.Min(careerWeights != null ? careerWeights.Length : 0, StudentProfile.AllCareers.Length);
        for (int i = 0; i < count; i++) total += Mathf.Max(0f, careerWeights[i]);

        if (total <= 0f)
            return StudentProfile.AllCareers[Random.Range(0, StudentProfile.AllCareers.Length)];

        float r = Random.value * total;
        for (int i = 0; i < count; i++)
        {
            float w = Mathf.Max(0f, careerWeights[i]);
            if (r < w) return StudentProfile.AllCareers[i];
            r -= w;
        }

        return StudentProfile.AllCareers[count - 1];
    }
}
