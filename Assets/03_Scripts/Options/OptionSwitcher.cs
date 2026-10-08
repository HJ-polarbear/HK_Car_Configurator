using UnityEngine;

/*
//  한 카테고리에서 "현재 몇 번째 옵션이 선택됐는지"를 관리하는 공통 부모.
//  인덱스 범위 검사, 초기화 전 호출 차단, 기본값 복원을 이 한 곳에서 처리한다.
//  실제 적용 방법(Prefab 켜기 / Material 칠하기)은 자식 클래스가 ApplyCurrent로 구현한다.
*/

public abstract class OptionSwitcher : MonoBehaviour
{
    //--------------- (1) ---------------
    //  <<< 변수 >>>

    [Header("카테고리")]
    [Tooltip("이 Switcher가 담당하는 카테고리")]
    [SerializeField] private OptionCategory category;

    [Tooltip("시작할 때와 초기화(Reset) 때 적용할 옵션 번호")]
    [SerializeField] protected int defaultIndex = 0;

    //  현재 선택된 번호 (-1 = 아직 없음), 초기화 여부
    protected int currentIndex = -1;
    protected bool isInitialized = false;

    //  외부에서는 읽기만 가능
    public OptionCategory Category => category;
    public int CurrentIndex => currentIndex;        // = { get { return currentIndex; } }

    //  선택지 개수 (자식 클래스가 자기 배열 길이를 돌려준다)
    public abstract int OptionCount { get; }


    //--------------- (2) ---------------
    //  <<< 함수 >>>

    //  ConfiguratorManager가 시작 시 순서대로 호출한다.
    public virtual void Init()
    {
        if(OptionCount == 0)
        {
            Debug.LogError($"[{GetType().Name}] {name}: 선택지가 하나도 연결되지 않았습니다.", this);
            return;
        }

        if(!IsValidIndex(defaultIndex))
        {
            Debug.LogWarning($"[{GetType().Name}] {name}: defaultIndex {defaultIndex}이(가) 범위(0 ~ {OptionCount - 1}) 밖이라 0으로 시작합니다.", this);
            defaultIndex = 0;
        }

        currentIndex = defaultIndex;
        isInitialized = true;
        ApplyCurrent();
    }

    //  index번째 옵션을 선택한다. 성공하면 true.
    public bool Select(int index)
    {
        if(!isInitialized)
        {
            Debug.LogWarning($"[{GetType().Name}] {name}: 아직 초기화되지 않아 선택을 무시합니다.", this);
            return false;
        }

        if(!IsValidIndex(index))
        {
            Debug.LogWarning($"[{GetType().Name}] {name}: {index}번 옵션이 없습니다. (0 ~ {OptionCount - 1})", this);
            return false;
        }

        currentIndex = index;
        ApplyCurrent();
        return true;
    }

    //  기본 옵션으로 되돌린다.
    public void ResetToDefault()
    {
        Select(defaultIndex);
    }

    //  현재 선택된 선택지 데이터 (초기화 전이면 null)
    public OptionData GetCurrentChoice()
    {
        if (!isInitialized) { return null; }
        return GetChoice(currentIndex);
    }

    //  index번재 선택지 데이터 (자식 클래스가 범위 검사 후 돌려준다)
    public abstract OptionData GetChoice(int index);

    //  현재 currentIndex를 실제로 적용한다 (자식 클래스가 구현)
    protected abstract void ApplyCurrent();

    //  자식 클래스도 쓰는 범위 검사
    protected bool IsValidIndex(int index)
    {
        return index >= 0 && index < OptionCount;
    }
}
