using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MatrixComparator : MonoBehaviour
{
    [Header("Settings")]
    public GameObject pointPrefab; // Префаб для визуализации точек
    public Color modelColor = Color.blue; // Цвет точек модели
    public Color spaceColor = Color.red; // Цвет точек пространства

    private List<Matrix4x4Data> modelMatrices;
    private List<Matrix4x4Data> spaceMatrices;

    void Start()
    {
        // Загрузка данных
        LoadMatrices("model.json", out modelMatrices);
        LoadMatrices("space.json", out spaceMatrices);

        // Визуализация
        VisualizeMatrices(modelMatrices, modelColor);
        VisualizeMatrices(spaceMatrices, spaceColor);

        // Поиск смещений
        FindCommonOffsets();
    }

    private void LoadMatrices(string filename, out List<Matrix4x4Data> matrices)
    {
        string path = Path.Combine(Application.streamingAssetsPath, filename);
        if (!File.Exists(path))
        {
            Debug.LogError($"Файл {filename} не найден в StreamingAssets!");
            matrices = new List<Matrix4x4Data>();
            return;
        }

        string json = File.ReadAllText(path);
        matrices = JsonConvert.DeserializeObject<List<Matrix4x4Data>>(json);
        Debug.Log($"Загружено {matrices.Count} матриц из {filename}");
    }

    private void FindCommonOffsets()
    {
        Dictionary<Vector3, int> offsetCandidates = new Dictionary<Vector3, int>();
        bool isFirstModel = true;

        foreach (var model in modelMatrices)
        {
            HashSet<Vector3> currentOffsets = new HashSet<Vector3>();

            foreach (var space in spaceMatrices)
            {
                if (MatricesMatchExceptTranslation(model, space))
                {
                    Vector3 offset = new Vector3(
                        space.m03 - model.m03,
                        space.m13 - model.m13,
                        space.m23 - model.m23
                    );
                    currentOffsets.Add(offset);
                }
            }

            // Обновление кандидатов
            if (isFirstModel)
            {
                foreach (var offset in currentOffsets)
                    offsetCandidates[offset] = 1;
                isFirstModel = false;
            }
            else
            {
                List<Vector3> invalidOffsets = new List<Vector3>();
                foreach (var key in offsetCandidates.Keys)
                {
                    if (!currentOffsets.Contains(key))
                        invalidOffsets.Add(key);
                }
                foreach (var key in invalidOffsets)
                    offsetCandidates.Remove(key);
            }

            if (offsetCandidates.Count == 0) break;
        }

        // Экспорт результатов
        ExportOffsets(offsetCandidates.Keys);
    }

    private bool MatricesMatchExceptTranslation(Matrix4x4Data a, Matrix4x4Data b, float epsilon = 1e-4f)
    {
        return Mathf.Abs(a.m00 - b.m00) < epsilon &&
               Mathf.Abs(a.m01 - b.m01) < epsilon &&
               Mathf.Abs(a.m02 - b.m02) < epsilon &&
               Mathf.Abs(a.m10 - b.m10) < epsilon &&
               Mathf.Abs(a.m11 - b.m11) < epsilon &&
               Mathf.Abs(a.m12 - b.m12) < epsilon &&
               Mathf.Abs(a.m20 - b.m20) < epsilon &&
               Mathf.Abs(a.m21 - b.m21) < epsilon &&
               Mathf.Abs(a.m22 - b.m22) < epsilon;
    }

    private void ExportOffsets(IEnumerable<Vector3> offsets)
    {
        List<Vector3> result = new List<Vector3>(offsets);
        string json = JsonConvert.SerializeObject(result, Formatting.Indented);
        string path = Path.Combine(Application.dataPath, "Results", "offsets.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
        Debug.Log($"Экспортировано {result.Count} смещений: {path}");
    }

    private void VisualizeMatrices(List<Matrix4x4Data> matrices, Color color)
    {
        if (pointPrefab == null)
        {
            Debug.LogError("Не назначен префаб для визуализации!");
            return;
        }

        foreach (var matrix in matrices)
        {
            Vector3 position = new Vector3(matrix.m03, matrix.m13, matrix.m23);
            GameObject point = Instantiate(pointPrefab, position, Quaternion.identity, transform);
            point.GetComponent<Renderer>().material.color = color;
        }
    }
}