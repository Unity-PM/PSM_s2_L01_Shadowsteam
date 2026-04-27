using UnityEngine;

public class GameSaveCommands : MonoBehaviour
{
    public void SaveGame()
    {
        if (GameSaveCoordinator.Instance != null)
            GameSaveCoordinator.Instance.SaveGame();
        else
            Debug.LogWarning("GameSaveCommands: GameSaveCoordinator not in scene.");
    }

    public void LoadGame()
    {
        if (GameSaveCoordinator.Instance != null)
            GameSaveCoordinator.Instance.LoadGame();
        else
            Debug.LogWarning("GameSaveCommands: GameSaveCoordinator not in scene.");
    }

    public void DeleteSave()
    {
        if (GameSaveCoordinator.Instance != null)
            GameSaveCoordinator.Instance.DeleteSave();
        else
            GameSaveService.DeleteSave();
    }
}
