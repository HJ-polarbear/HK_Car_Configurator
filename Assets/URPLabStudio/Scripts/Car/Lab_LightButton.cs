namespace URPLabStudio
{
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lab_LightButton : MonoBehaviour
{
    public Light[] m_lights;
    private bool[] m_lightEnables;
    private bool m_isLightOn = false;

    private void Start()
    {
        m_lightEnables = new bool[4];
        for(int i = 0; i < m_lights.Length; i++)
        {
            m_lightEnables[i] = m_lights[i].enabled;
        }
        m_isLightOn = m_lightEnables[0];
    }

    public void OnLightButtonClicked()
    {
        m_isLightOn = !m_isLightOn;
        for(int i = 0; i < m_lights.Length; i++)
        {
            m_lights[i].enabled = m_isLightOn;
        }
    }
}
}
