using UnityEngine;
using System.Linq;

public class RoomBoundsTrigger : MonoBehaviour
{
    public Room currentRoom;
    public Door entranceDoor;
    private bool hasPlayerEntered = false;

    public bool HasPlayerEntered => hasPlayerEntered;
    private Bounds roomBounds;
    private Transform player;
    private float checkInterval = 0.2f;
    private float lastCheckTime = 0f;

    private void Start()
    {
        if (currentRoom == null)
        {
            currentRoom = GetComponentInParent<Room>();
            if (currentRoom == null)
            {
                return;
            }
        }

        roomBounds = currentRoom.GetRoomBounds();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            return;
        }

        CheckPlayerPosition();
    }

    private void Update()
    {
        if (player == null || Time.time - lastCheckTime < checkInterval)
            return;

        lastCheckTime = Time.time;
        CheckPlayerPosition();
    }

    private void CheckPlayerPosition()
    {
        bool isPlayerInRoom = roomBounds.Contains(player.position);

        if (isPlayerInRoom && !hasPlayerEntered)
        {
            OnRoomEnter();
        }
        else if (!isPlayerInRoom && hasPlayerEntered)
        {
            OnRoomExit();
        }
    }

    private void OnRoomEnter()
    {
        hasPlayerEntered = true;

        if (entranceDoor != null)
        {
            
            
            entranceDoor.StopDoorSounds();
        }
        
        if (currentRoom != null && !currentRoom.IsInitialized)
        {
            currentRoom.SetupRoom(currentRoom.roomNumber);
        }
        
        if (currentRoom != null && currentRoom.CurrentAnomaly != "MirrorAnomaly")
        {
            if (MirrorAnomaly.Instance != null)
            {
                MirrorAnomaly.Instance.ForceRestoreMirror();
            }
        }

        
        if (currentRoom != null && currentRoom.CurrentAnomaly == "MirrorAnomaly" && !currentRoom.mirrorAnomalyActivated)
        {
            currentRoom.mirrorAnomalyActivated = true;
            if (MirrorAnomaly.Instance != null)
            {
                MirrorAnomaly.Instance.ActivateMirrorAnomaly(currentRoom.roomNumber);
            }
        }

        ReinitializeAllDoorsAudio();
        SetupUVMarksIfNeeded();
    }
    
    public Door GetEntranceDoor()
    {
        return entranceDoor;
    }

    private void OnRoomExit()
    {
        hasPlayerEntered = false;

        
        if (currentRoom != null)
        {
            currentRoom.DeactivateLocalAnomalies();
            
            currentRoom.mirrorAnomalyActivated = false;
        }

        Room[] allRooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
        bool isGoingToRoom3 = allRooms.Any(room => room.roomNumber == 3 && room != currentRoom);
    }

    private void SetupUVMarksIfNeeded()
    {
        if (UVFlashlightPuzzleManager.PlayerHasUVFlashlight && currentRoom != null)
        {
            currentRoom.SetupUVMarksIfNeeded();
        }
    }

    private void ReinitializeAllDoorsAudio()
    {
        Door[] allDoorsInRoom = currentRoom.GetComponentsInChildren<Door>();
        foreach (Door door in allDoorsInRoom)
        {
            if (door != null)
            {
                door.ReinitializeAudio();
                door.RecheckSoundTrigger();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (currentRoom != null)
        {
            Gizmos.color = hasPlayerEntered ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(roomBounds.center, roomBounds.size);
        }
    }
}
