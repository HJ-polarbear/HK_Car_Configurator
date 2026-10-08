namespace URPLabStudio
{
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Lab_WheelSelection : MonoBehaviour
{
    [SerializeField] private Transform wheelRoot;
    [SerializeField] private Lab_WheelSelector wheelSelector;
    [SerializeField] private Button[] buttons = new Button[4];
    [SerializeField] private TMP_Text wheelName;
    [SerializeField, Range(0, 3)] private int selectedIndex;
    [SerializeField] private Color selectedColor = new Color(0.05f, 0.36f, 1f, 1f);
    [SerializeField] private Color normalColor = new Color(0.55f, 0.59f, 0.65f, 0.45f);

    private Transform[] variants;

    private void Awake()
    {
        ResolveReferences();
        Select(selectedIndex);
    }

    public void Select(int index)
    {
        ResolveReferences();
        if (variants == null || variants.Length < 4) return;
        selectedIndex = Mathf.Clamp(index, 0, 3);
        if (wheelSelector != null) wheelSelector.SelectWheel(selectedIndex);
        else
            for (int i = 0; i < variants.Length; i++)
                variants[i].gameObject.SetActive(i == selectedIndex);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            ColorBlock colors = buttons[i].colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.87f, 0.96f, 1f);
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1f;
            buttons[i].colors = colors;
        }

        if (wheelName != null)
        {
            string[] names = { "WHEEL 01", "WHEEL 02", "WHEEL 03", "WHEEL 04" };
            string[] descriptions = { "MULTI-SPOKE BLACK", "FIVE-SPOKE BLACK", "MACHINED SPORT", "MULTI-SPOKE SILVER" };
            wheelName.text = $"{names[selectedIndex]}\n<size=11><color=#68717F>{descriptions[selectedIndex]}</color></size>";
        }
    }

    private void ResolveReferences()
    {
        if (wheelRoot == null)
            wheelRoot = FindObjectsByType<Transform>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == "Lab_Tires_MD");
        if (wheelRoot != null)
        {
            if (wheelSelector == null) wheelSelector = wheelRoot.GetComponent<Lab_WheelSelector>();
            variants = new[] { "Tires_A", "Tires_B", "Tires_C", "Tires_D" }
                .Select(name => wheelRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name))
                .ToArray();
        }
    }
}
}
