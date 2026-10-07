using UnityEngine;

/*
//  Prefab 교체형 선택지 하나 (시트, 휠).
//  PartSwitcher가 partPrefab를 시작 시 한 번만 생성해 두고, 이후에는 SetActive로 전환한다.
*/

[CreateAssetMenu(fileName = "NewOptionChoice", menuName = "Configurator/Option Choice")]
public class OptionChoice : OptionData
{
    //--------------- (1) ---------------
    //  <<< 변수 >>>

    [Header("Prefab")]
    [Tooltip("이 선택지를 고르면 보여 줄 Prefab 목록")]
    public GameObject[] partPrefabs;
}