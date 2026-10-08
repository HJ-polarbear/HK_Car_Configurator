using UnityEngine;
using UnityEngine.InputSystem;

/*
//  [임시 테스트용] ConfiguratorManager를 만들기 전까지 Switcher를 키보드로 시험한다.
//  숫자 키 1~9 =0-8번 옵션 선택, R = 기본값으로 되돌리기.
//  Manager가 생기면 스크립트 불필요, 지우기
*/

public class SwitcherTester : MonoBehaviour
{
    //--------------- 변수 ---------------

    /// 타입이 부모(OptionSwitcher)라서 MaterialSwitcher든 PartSwitcher든 끌어다 넣을 수 있다.
    [Tooltip("시험할 Switcher")]
    public OptionSwitcher target;


    //--------------- 함수 ---------------

    //  
    private void Start()
    {
        if(target == null)
        {
            Debug.LogError($"[SwitcherTester] {name}: target이(가) 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        target.Init();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) { return; }

        for(int i = 0; i < 9; i++)
        {
            ///  Key.Digit1 + i : i가 0이면 숫자 1키, 1이면 숫자 2키 ...
            if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
            {
                target.Select(i);
            }
        }

        if(keyboard.rKey.wasPressedThisFrame)
        {
            target.ResetToDefault();
        }
    }
}
