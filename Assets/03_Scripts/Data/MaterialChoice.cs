using UnityEngine;

/*
//  Material 교체형 선택지 하나 (외장 색상, 내장 색상·재질, 유리).
//  MaterialSwitcher가 PaintTargets에 이 Material을 칠한다.
*/

/// 유리 투명도는 투명도가 다른 Material을 미리 만들어 선택지로 등록한다.
/// 실행 중에 Material을 새로 만들지 않는다. (NFR-09)
[CreateAssetMenu(fileName = "NewMaterialChoice", menuName = "Configurator/Material Choice")]
public class MaterialChoice : OptionData
{
    //--------------- (1) ---------------
    //  <<< 변수 >>>

    [Header("Material")]
    [Tooltip("이 선택지를 고르면 칠할 Material 에셋")]
    public Material material;

    [Tooltip("켜 두면 Material의 색을 미리보기 색(previewColor)으로 자동 복사한다")]
    public bool syncPreviewColor = true;


    //--------------- (2) ---------------
    //  <<< 함수 >>>

    /// OnValidate: Inspector에서 값이 바뀔 때 에디터가 자동으로 호출하는 함수 (실행 중에는 호출되지 않음)
    private void OnValidate()
    {
        if (!syncPreviewColor || material == null) { return; }

        //  URP Lit은 _BaseColor, 예전 Standard 계열 셰이더는 _Color를 쓴다
        if(material.HasProperty("_BaseColor")) { previewColor = material.GetColor("_BaseColor"); }
        else if(material.HasProperty("_Color")) { previewColor = material.GetColor("_Color"); }
    }
}
