using UnityEngine;

/*
//  씬에 하나만 존재하는 총괄 매니저 (싱글톤).
//  시작 시 Switcher 들의 초기화 순서를 조율하고,
//  UI에서 온 옵션 선택을 카테고리에 맞는 Switcher로 전달한다.
*/

/// DefaultExecutionOrder(-100): 이 스크립트의 Awake·Start가 다른 스크립트보다 먼저 실행되도록 순서를 앞당긴다.
[DefaultExecutionOrder(-100)]
public class ConfiguratorManager : MonoBehaviour
{
    //--------------- (1) ---------------
    //  <<< 변수 >>>

    //  싱글톤 인스턴스 (외부에서는 읽기만 가능)
    public static ConfiguratorManager Instance { get; private set; }

    [Header("차량")]
    [Tooltip("씬에 배치된 차량 루트")]
    [SerializeField] private GameObject vehicle;

    [Header("옵션 Switcher")]
    [Tooltip("외장·내장·유리·시트·휠 Switcher 목록 (순서는 상관없음)")]
    [SerializeField] private OptionSwitcher[] switchers;


    //--------------- (2) ---------------
    //  <<< Unity 이벤트 함수 >>>

    private void Awake()
    {
        //  이미 다른 매니저가 있으면 중복 생성 방지를 위해 파괴
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (!ValidateReferences()) { return; }
        InitAll();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
    }


    //--------------- (3) ---------------
    //  <<< 선택·초기화 >>>

    //  UI가 호출한다: category의 index번째 옵션을 선택
    public void SelectOption(OptionCategory category, int index)
    {
        OptionSwitcher switcher = FindSwitcher(category);
        if (switcher == null)
        {
            Debug.LogWarning($"[ConfiguratorManager] {name}: {category} Switcher가 등록되어 있지 않습니다.", this);
            return;
        }

        switcher.Select(index);
    }

    //  모든 옵션을 기본값으로 되돌린다.
    public void ResetAll()
    {
        foreach(OptionSwitcher switcher in switchers)
        {
            if (switcher != null) { switcher.ResetToDefault(); }
        }
    }


    //--------------- (4) ---------------
    //  <<< UI 버튼·상태 표시용 >>>

    //  
    public int GetOptionCount(OptionCategory category)
    {
        OptionSwitcher switcher = FindSwitcher(category);
        return switcher == null ? 0 : switcher.OptionCount;
    }

    //  
    public int GetCurrentIndex(OptionCategory category)
    {
        OptionSwitcher switcher = FindSwitcher(category);
        return switcher == null ? -1 : switcher.CurrentIndex;
    }

    //  
    public OptionData GetChoice(OptionCategory category, int index)
    {
        OptionSwitcher switcher = FindSwitcher(category);
        return switcher == null ? null : switcher.GetChoice(index);
    }

    //  
    public OptionData GetCurrentChoice(OptionCategory category)
    {
        OptionSwitcher switcher = FindSwitcher(category);
        return switcher == null ? null : switcher.GetCurrentChoice();
    }


    //--------------- (5) ---------------
    //  <<< 내부 함수 >>>

    //  Prefab형(시트·휠)을 먼저, Material형을 나중에 초기화한다.
    //  Prefab이 먼저 생성되어야 그 안의 PaintTargets까지 칠할 수 있기 때문이다.
    private void InitAll()
    {
        /// "A is B" : A가 B 타입(또는 그 자식)이면 true
        foreach(OptionSwitcher switcher in switchers)
        {
            if (switcher != null && !(switcher is MaterialSwitcher)) { switcher.Init(); }
        }

        foreach(OptionSwitcher switcher in switchers)
        {
            if (switcher is MaterialSwitcher) { switcher.Init(); }
        }
    }

    //  카테고리가 같은 Switcher를 찾는다. 없으면 null.
    private OptionSwitcher FindSwitcher(OptionCategory category)
    {
        if (switchers == null) { return null; }

        foreach(OptionSwitcher switcher in switchers)
        {
            if (switcher != null && switcher.Category == category) { return switcher; }
        }

        return null;
    }

    //  필수 참조를 검사한다. 하나라도 없으면 false.
    private bool ValidateReferences()
    {
        if(vehicle == null)
        {
            Debug.LogError($"[ConfiguratorManager] {name}: vehicle이(가) 연결되지 않았습니다.", this);
            return false;
        }

        if(switchers == null || switchers.Length == 0)
        {
            Debug.LogError($"[ConfiguratorManager] {name}: switchers이(가) 하나도 연결되지 않았습니다.", this);
            return false;
        }

        return true;
    }
}