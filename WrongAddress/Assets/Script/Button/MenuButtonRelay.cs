using UnityEngine;

public enum MenuAction { StartGame, Quit, Resume, Replay, ReturnMenu }

public class MenuButtonRelay : MonoBehaviour
{
    [SerializeField] private MenuAction _action;

    public void InvokeAction()
    {
        var gm = GameManager.Instance;
        switch (_action)
        {
            case MenuAction.StartGame: gm.StartGame(); break;
            case MenuAction.Quit: gm.QuitGame(); break;
            case MenuAction.Resume: gm.ResumeGame(); break;
            case MenuAction.Replay: gm.ReplayGame(); break;
            case MenuAction.ReturnMenu: gm.ReturnToMainMenu(); break;
        }
    }
}
