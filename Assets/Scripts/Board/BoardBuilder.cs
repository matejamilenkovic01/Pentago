using UnityEngine;

/// <summary>
/// Procedurally builds the Pentago board at runtime:
/// - A flat board base
/// - 4 QuadrantPivot GameObjects (used as rotation roots in M4)
/// - 36 cell marker cylinders parented to their quadrant pivot
///
/// Quadrant layout (top-down view, Z increases downward on screen):
///   Q0 (rows 0-2, cols 0-2) | Q1 (rows 0-2, cols 3-5)
///   Q2 (rows 3-5, cols 0-2) | Q3 (rows 3-5, cols 3-5)
/// </summary>
public class BoardBuilder : MonoBehaviour
{
    [Header("Layout")]
    public float cellSpacing  = 1.1f;
    public float quadrantGap  = 1.2f;
    public float cellScale    = 0.82f;

    /// <summary>World-space transforms of all 36 cells, indexed [row, col].</summary>
    public Transform[,] CellTransforms { get; private set; }

    /// <summary>
    /// The 4 quadrant pivot transforms (rotation roots).
    /// Q0=top-left, Q1=top-right, Q2=bottom-left, Q3=bottom-right.
    /// </summary>
    public Transform[] QuadrantPivots { get; private set; }

    private void Awake()
    {
        CellTransforms = new Transform[6, 6];
        QuadrantPivots = new Transform[4];
        Build();
    }

    private void Build()
    {
        Material boardMat     = CreateMaterial(new Color(0.38f, 0.22f, 0.08f));  // dark wood
        Material cellMat      = CreateMaterial(new Color(0.87f, 0.80f, 0.66f));  // light beige
        Material separatorMat = CreateMaterial(new Color(0.20f, 0.10f, 0.03f));  // darker wood for dividers

        BuildBoardBase(boardMat);

        // qOffset = distance from board center to quadrant pivot center
        float qOffset = cellSpacing + quadrantGap / 2f;

        BuildSeparators(separatorMat, qOffset);

        for (int q = 0; q < 4; q++)
        {
            int qRow = q / 2;
            int qCol = q % 2;

            float px = qCol == 0 ? -qOffset : qOffset;
            float pz = qRow == 0 ? -qOffset : qOffset;

            var pivot = new GameObject($"QuadrantPivot_{q}");
            pivot.transform.SetParent(transform, false);
            pivot.transform.localPosition = new Vector3(px, 0f, pz);
            QuadrantPivots[q] = pivot.transform;

            for (int lr = 0; lr < 3; lr++)
            for (int lc = 0; lc < 3; lc++)
            {
                int gr = qRow * 3 + lr;
                int gc = qCol * 3 + lc;

                var cell = BuildCell(gr, gc, pivot.transform, cellMat,
                    new Vector3((lc - 1) * cellSpacing, 0f, (lr - 1) * cellSpacing));

                CellTransforms[gr, gc] = cell;
            }
        }
    }

    private void BuildSeparators(Material mat, float qOffset)
    {
        float cellRadius = cellScale * 0.5f;
        float boardHalf  = qOffset + cellSpacing + cellRadius + 0.1f;
        // Max safe thickness: inner cells are quadrantGap/2 from divider center,
        // cell radius = cellScale/2, so safe max = quadrantGap/2 - cellScale/2 = (quadrantGap-cellScale)/2
        // Use 60% of that to leave a clear visible gap
        float thickness  = (quadrantGap - cellScale) * 0.6f * 0.5f;
        float height     = 0.04f;

        // Horizontal divider (along X axis, at Z=0)
        var hDiv = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hDiv.name = "Separator_H";
        hDiv.transform.SetParent(transform, false);
        hDiv.transform.localScale    = new Vector3(boardHalf * 2f, height, thickness);
        hDiv.transform.localPosition = new Vector3(0f, 0.001f, 0f);
        hDiv.GetComponent<Renderer>().material = mat;
        Destroy(hDiv.GetComponent<Collider>());

        // Vertical divider (along Z axis, at X=0)
        var vDiv = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vDiv.name = "Separator_V";
        vDiv.transform.SetParent(transform, false);
        vDiv.transform.localScale    = new Vector3(thickness, height, boardHalf * 2f);
        vDiv.transform.localPosition = new Vector3(0f, 0.001f, 0f);
        vDiv.GetComponent<Renderer>().material = mat;
        Destroy(vDiv.GetComponent<Collider>());
    }

    private void BuildBoardBase(Material mat)
    {
        float qOffset    = cellSpacing + quadrantGap / 2f;
        float cellRadius = cellScale * 0.5f;
        // Extend to fully contain outermost cell circles + small margin
        float totalWidth = 2f * (qOffset + cellSpacing + cellRadius) + 0.3f;

        var boardBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boardBase.name = "BoardBase";
        boardBase.transform.SetParent(transform, false);
        boardBase.transform.localScale    = new Vector3(totalWidth, 0.08f, totalWidth);
        boardBase.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        boardBase.GetComponent<Renderer>().material = mat;
        Destroy(boardBase.GetComponent<Collider>());
    }

    private Transform BuildCell(int row, int col, Transform parent, Material mat, Vector3 localPos)
    {
        var cell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cell.name = $"Cell_{row}_{col}";
        cell.transform.SetParent(parent, false);
        cell.transform.localPosition = localPos;
        cell.transform.localScale    = new Vector3(cellScale, 0.02f, cellScale);
        cell.GetComponent<Renderer>().material = mat;

        // Replace the default CapsuleCollider with a flat BoxCollider
        // (easier to raycast from above in M3)
        Destroy(cell.GetComponent<Collider>());
        var bc = cell.AddComponent<BoxCollider>();
        bc.size = new Vector3(1f, 8f, 1f);  // tall in local Y so click from any height hits

        var data = cell.AddComponent<CellData>();
        data.Row = row;
        data.Col = col;

        return cell.transform;
    }

    private static Material CreateMaterial(Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        return mat;
    }
}
