/*
//  옵션 카테고리 구분값.
//  Switcher, PaintTargets, OptionButton, SelectionStatusDisplay가 공통으로 사용한다.
*/

/// 주의: Inspector에는 enum이 숫자로 저장된다.
/// 새 값은 항상 맨 뒤에 추가하고, 기존 값의 순서는 바꾸지 않는다.
public enum OptionCategory
{
    ExteriorColor,  // 외장 색상
    InteriorColor,  // 내장 색상·재질
    Seat,           // 시트
    Wheel,          // 휠
    Glass           // 유리 (투명도)
}
