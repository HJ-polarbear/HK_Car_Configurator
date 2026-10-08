using System;
using UnityEngine;
using UnityEngine.UI;

namespace URPLabStudio
{
    /// <summary>
    /// Selects the already-instanced studio room prefabs under Lab_Studio_Selection.
    /// It never loads scenes or moves the persistent vehicle, UI, camera, turntable,
    /// wave controller, or post-processing controls.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Lab_StudioRoomSwitcher : MonoBehaviour
    {
        [Serializable]
        public sealed class RoomSlot
        {
            [Tooltip("Authoring label for this room.")]
            public string displayName = "Studio Room";

            [Tooltip("The room prefab instance under Lab_Studio_Selection. The selected room is active; the others are inactive.")]
            public GameObject roomPrefab;

            [Tooltip("Authoring reference for this room's baked LightingData.")]
            public UnityEngine.Object lightingData;
        }

        [Header("Studio Navigation UI")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;

        [Header("Studio Room Prefabs")]
        [SerializeField] private RoomSlot[] rooms = Array.Empty<RoomSlot>();
        [SerializeField] private int initialRoomIndex;

        private int currentRoomIndex;

        private void Awake() => BindButtons();

        private void Start()
        {
            EnsureDirectChildRoomReferences();
            SelectRoom(initialRoomIndex);
        }

        private void OnValidate() => EnsureDirectChildRoomReferences();

        private void OnDestroy()
        {
            if (previousButton != null) previousButton.onClick.RemoveListener(PreviousRoom);
            if (nextButton != null) nextButton.onClick.RemoveListener(NextRoom);
        }

        public void PreviousRoom() => SelectRelative(-1);
        public void NextRoom() => SelectRelative(1);

        public void SelectRelative(int direction)
        {
            if (rooms == null || rooms.Length < 2)
                return;

            SelectRoom((currentRoomIndex + direction + rooms.Length) % rooms.Length);
        }

        public void SelectRoom(int roomIndex)
        {
            if (rooms == null || rooms.Length == 0)
                return;

            currentRoomIndex = Mathf.Clamp(roomIndex, 0, rooms.Length - 1);
            for (int index = 0; index < rooms.Length; index++)
            {
                GameObject roomPrefab = rooms[index]?.roomPrefab;
                if (roomPrefab != null)
                    roomPrefab.SetActive(index == currentRoomIndex);
            }
        }

        private void BindButtons()
        {
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(PreviousRoom);
                previousButton.onClick.AddListener(PreviousRoom);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(NextRoom);
                nextButton.onClick.AddListener(NextRoom);
            }
        }

        private void EnsureDirectChildRoomReferences()
        {
            if (rooms == null)
                return;

            foreach (RoomSlot slot in rooms)
            {
                if (slot == null || slot.roomPrefab != null || string.IsNullOrWhiteSpace(slot.displayName))
                    continue;

                string expectedName = slot.displayName.Replace(" ", "_");
                Transform directChild = transform.Find(expectedName) ?? transform.Find("Lab_" + expectedName);
                if (directChild != null)
                    slot.roomPrefab = directChild.gameObject;
            }
        }
    }
}
