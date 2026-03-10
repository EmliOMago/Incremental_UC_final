using UnityEngine;

public class GameDirector : MonoBehaviour
{
    public static GameDirector instancia;
    public LevelManenger levelManenger;
    public HUDManeger hudManeger;

    private void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
