using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowController : MonoBehaviour
{
    [Header("실제 움직일 창문 3D 메쉬 오브젝트 목록")]
    public List<Transform> targetMeshes = new List<Transform>();

    [Header("창문 이동 설정")]
    [Tooltip("내려갈 거리 (음수: 아래로 이동)")]
    public float openDistance = -0.5f;

    [Tooltip("이동 시간(초)")]
    public float duration = 0.8f;

    // 각 창문들의 닫힌 좌표와 열린 좌표를 보관할 리스트
    private List<Vector3> closedPositions = new List<Vector3>();
    private List<Vector3> openPositions = new List<Vector3>();

    private bool isOpen = false;
    private Coroutine routine;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        closedPositions.Clear();
        openPositions.Clear();

        // 1. 반복문으로 등록된 모든 창문의 기본 위치(닫힘/열림) 계산
        for (int i = 0; i < targetMeshes.Count; i++)
        {
            if (targetMeshes[i] != null)
            {
                Vector3 closed = targetMeshes[i].localPosition;
                Vector3 open = closed + Vector3.up * openDistance;

                closedPositions.Add(closed);
                openPositions.Add(open);
            }
            else
            {
                closedPositions.Add(Vector3.zero);
                openPositions.Add(Vector3.zero);
            }
        }
    }

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);
    public void Toggle() => SetOpen(!isOpen);

    public void SetOpen(bool open)
    {
        if (targetMeshes == null || targetMeshes.Count == 0) return;
        if (isOpen == open && routine == null) return;

        isOpen = open;

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(MoveSmoothly(open));
    }

    // 창문들을 매 프레임 부드럽게 이동시키는 코루틴
    private IEnumerator MoveSmoothly(bool open)
    {
        // 이동 시작 시점의 각 창문 좌표를 기록
        List<Vector3> startPositions = new List<Vector3>();
        for (int i = 0; i < targetMeshes.Count; i++)
        {
            if (targetMeshes[i] != null)
            {
                startPositions.Add(targetMeshes[i].localPosition);
            }
            else
            {
                startPositions.Add(Vector3.zero);
            }
        }

        float elapsed = 0f;

        // 경과 시간 동안 보간 이동
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 2. 반복문으로 4개 창문의 위치를 동시에 Lerp
            for (int i = 0; i < targetMeshes.Count; i++)
            {
                if (targetMeshes[i] != null)
                {
                    Vector3 target = open ? openPositions[i] : closedPositions[i];
                    targetMeshes[i].localPosition = Vector3.Lerp(startPositions[i], target, t);
                }
            }

            yield return null;
        }

        // 3. 반복문으로 오차 보정 (최종 목표 좌표로 고정)
        for (int i = 0; i < targetMeshes.Count; i++)
        {
            if (targetMeshes[i] != null)
            {
                targetMeshes[i].localPosition = open ? openPositions[i] : closedPositions[i];
            }
        }

        routine = null;
    }

    [ContextMenu("Test Toggle")]
    private void TestToggle() => Toggle();
}