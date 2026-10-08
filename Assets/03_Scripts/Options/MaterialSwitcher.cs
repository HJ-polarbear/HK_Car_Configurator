using System.Collections.Generic;
using UnityEngine;

/*
//  Material 교체형 카테고리 (외장 색상, 내장 색상·재질, 유리).
//  같은 스크립트를 카테고리마다 하나씩 붙인다.
//  차량 아래에서 같은 카테고리의 PaintTargets를 모아 현재 Material을 칠한다.
*/

public class MaterialSwitcher : OptionSwitcher
{
    //--------------- (1) ---------------
    //  <<< 변수 >>>

    [Header("선택지")]
    [Tooltip("선택지 에셋 (순서 = 버튼 번호)")]
    [SerializeField] private MaterialChoice[] choices;

    [Header("칠할 대상")]
    [Tooltip("PaintTargets를 찾을 루트 (보통 차량 루트)")]
    [SerializeField] private Transform paintRoot;

    //  category가 일치하는 PaintTargets 목록 (Init 때 수집)
    private List<PaintTargets> targets = new List<PaintTargets>();

    //  부모가 "반드시 만들라"고 한 선택지 개수
   /// OptionCount{ get{ return choices == null ? 0 : choices.Length; } }
    public override int OptionCount => choices == null ? 0 : choices.Length;        // "=>" : 읽을 때마다 계산해서 돌려줌


    //--------------- (2) ---------------
    //  <<< 함수 >>>

    //  
    private void Awake()
    {
        if(paintRoot == null)
        {
            Debug.LogError($"[MaterialSwitcher] {name}: paintRoot이(가) 연결되지 않았습니다.", this);
        }
    }

    //  부모의 Init 앞에 "대상 모으기"를 덧붙인다.
    public override void Init()
    {
        CollectTargets();
        base.Init();        // 부모 OptionSwitcher의 Init 실행 (범위 검사 -> ApplyCurrent)
    }

    //  
    public override OptionData GetChoice(int index)
    {
        if (!IsValidIndex(index)) { return null; }
        return choices[index];
    }

    //  현재 선택된 Material을 모든 대상에 칠한다.
    protected override void ApplyCurrentOption()
    {
        MaterialChoice choice = choices[currentIndex];

        if (choice == null || choice.material == null)
        {
            Debug.LogWarning($"[MaterialSwitcher] {name}: {currentIndex}번 선택지에 Material이 없습니다.", this);
            return;
        }

        foreach(PaintTargets target in targets)
        {
            target.ApplyMaterial(choice.material);
        }
    }

    //  paintRoot 아래에서 같은 카테고리의 PaintTargets를 모은다.
    private void CollectTargets()
    {
        targets.Clear();
        if (paintRoot == null) { return; }

        /// true = 꺼져 있는 오브젝트까지 포함해서 찾는다.
        /// (나중에 꺼져 있는 시트 옵션도 미리 칠해 두기 위해)
        PaintTargets[] found = paintRoot.GetComponentsInChildren<PaintTargets>(true);

        foreach(PaintTargets target in found)
        {
            if (target.category == Category) { targets.Add(target); }
        }

        if(targets.Count == 0)
        {
            Debug.LogWarning($"[MaterialSwitcher] {name}: paintRoot 아래에 {Category} PaintTargets가 없습니다.", this);
        }
    }
}
