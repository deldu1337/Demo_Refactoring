using UnityEngine;

/// <summary>
/// BSP 분할에 사용되는 노드를 나타내며 방과 연결 정보를 보관합니다.
/// </summary>
public class Node
{
    public Node leftNode;
    public Node rightNode;
    public Node parNode;

    public RectInt nodeRect;
    public RectInt roomRect;

    /// <summary>
    /// 방의 중심 좌표를 반환합니다.
    /// </summary>
    public Vector2Int center
    {
        get
        {
            return new Vector2Int(roomRect.x + roomRect.width / 2, roomRect.y + roomRect.height / 2);
        }
    }

    /// <summary>
    /// 분할 영역을 받아 노드를 초기화합니다.
    /// </summary>
    /// <param name="rect">할당할 사각형 영역입니다.</param>
    public Node(RectInt rect)
    {
        this.nodeRect = rect;
    }
}
