using UnityEngine;

/*
//  "이 카테고리의 Material을 적용할 Renderer"를 표시하는 마커
//  (이 Renderer의 몇 번 슬롯에 어느 카테고리 Material을 칠할지?)
//  차체, 대시보드·도어 트림, 유리, 시트 Prefab 안에 붙인다.
//  칠하지 않을 Renderer는 등록하지 않는다.
*/

/// 한 Renderer의 여러 슬롯을 칠하려면 슬롯마다 PaintTargets를 하나씩 붙인다.
public class PaintTargets : MonoBehaviour
{
    //--------------- (1) ---------------
    //  <<< 변수 >>>

    [Header("대상")]
    [Tooltip("어느 카테고리의 Material을 받을지")]
    public OptionCategory category = OptionCategory.ExteriorColor;

    [Tooltip("Material을 바꿀 Renderer 목록")]
    public Renderer[] renderers;

    [Tooltip("교체할 Material 슬롯 번호 (Mesh Renderer의 Materials 목록 순서, 0부터)")]
    public int materialSlot = 0;

    //--------------- (2) ---------------
    //  <<< 함수 >>>

    /// Reset: 컴포넌트를 처음 붙일 때 에디터가 자동으로 호출한다. (실행 중에는 호출되지 않음)
    /// 같은 오브젝트의 Renderer를 미리 채워 Inspector 연결을 줄인다.
    private void Reset()
    {
        Renderer self = GetComponent<Renderer>();
        if (self != null) { renderers = new Renderer[] { self }; }
    }

    //  Awake: 재생 전 초기 실행 함수: 연결 에러 메시지 출력
    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogError($"[PaintTargets] {name}: renderers이(가) 연결되지 않았습니다.", this);
        }
    }

    //  지정한 슬롯만 material로 교체한다.
    public void ApplyMaterial(Material material)
    {
        if (material == null || renderers == null) { return; }

        foreach(Renderer target in renderers)
        {
            if (target == null) { continue; }

            /// sharedMaterials는 "복사본 배열"을 돌려준다.
            /// 배열 안의 값만 바꾸면 Renderer에는 반영되지 않으므로, 바꾼 배열을 다시 대입해야 한다.
            Material[] materials = target.sharedMaterials;

            if(materialSlot < 0 || materialSlot >= materials.Length)
            {
                Debug.LogWarning($"[PaintTargets] {name}: {target.name}에는 {materialSlot}번 슬롯이 없습니다. (슬롯 수: {materials.Length})", this);
                continue;
            }

            materials[materialSlot] = material;
            target.sharedMaterials = materials;
        }
    }
}
