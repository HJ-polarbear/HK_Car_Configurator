namespace URPLabStudio
{
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Lab_WheelSelector : MonoBehaviour
{
    [SerializeField] private GameObject[] wheelVariants = new GameObject[4];
    [SerializeField, Range(0, 3)] private int selectedIndex;

    public int SelectedIndex => selectedIndex;

    private void Awake() => SelectWheel(selectedIndex);

    public void Configure(GameObject[] variants, int initialIndex = 0)
    {
        wheelVariants = variants;
        selectedIndex = Mathf.Clamp(initialIndex, 0, 3);
    }

    public void SelectWheel(int index)
    {
        if (wheelVariants == null || wheelVariants.Length == 0) return;
        selectedIndex = Mathf.Clamp(index, 0, Mathf.Min(3, wheelVariants.Length - 1));
        for (int i = 0; i < wheelVariants.Length; i++)
            if (wheelVariants[i] != null) wheelVariants[i].SetActive(i == selectedIndex);
    }

    public void SelectWheelA() => SelectWheel(0);
    public void SelectWheelB() => SelectWheel(1);
    public void SelectWheelC() => SelectWheel(2);
    public void SelectWheelD() => SelectWheel(3);
}
}
