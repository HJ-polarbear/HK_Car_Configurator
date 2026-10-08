namespace URPLabStudio
{
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class Lab_ColorsSelection : MonoBehaviour
{
	[FormerlySerializedAs("Posts")] public GameObject[] Colors;
	[FormerlySerializedAs("selectedPost")] public int selectedColors = 0;

	public void NextColors()
	{
		if (Colors == null || Colors.Length == 0) return;
		Colors[selectedColors].SetActive(false);
		selectedColors = (selectedColors + 1) % Colors.Length;
		Colors[selectedColors].SetActive(true);
	}

	public void PreviousColors()
	{
		if (Colors == null || Colors.Length == 0) return;
		Colors[selectedColors].SetActive(false);
		selectedColors--;
		if (selectedColors < 0)
		{
			selectedColors += Colors.Length;
		}
		Colors[selectedColors].SetActive(true);
	}

	public void StartGame()
	{
		PlayerPrefs.SetInt("selectedColors", selectedColors);
		SceneManager.LoadScene(1, LoadSceneMode.Single);
	}
}
}
