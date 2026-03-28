using UnityEngine;

/// <summary>
/// Attached to each cell marker so raycasts can identify which board cell was hit.
/// </summary>
public class CellData : MonoBehaviour
{
    public int Row;
    public int Col;
}
