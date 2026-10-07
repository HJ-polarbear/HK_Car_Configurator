using UnityEngine;

/*
//  모든 선택지 데이터(ScriptableObject)의 공통 부모.
//  UI는 이 타입만 보고 이름과 미리보기를 표시하므로,
//  선택지가 Prefab형(OptionChoice)인지 Material형(MaterialChoice)인지 몰라도 된다.
*/

public abstract class OptionData : ScriptableObject
{
    //--------------- (1) ---------------
    //  <<< 변수 >>>

    [Header("표시 정보")]
    [Tooltip("UI에 표시할 이름 (예: Silver Exterior, Standard Seat)")]
    public string choiceName;

    [Tooltip("색상 미리보기 버튼에 칠할 색")]
    public Color previewColor = Color.white;

    [Tooltip("(직접 고르거나 icon 사용 선택) 휠·시트 버튼에 쓸 썸네일 이미지")]
    public Sprite icon;
}
