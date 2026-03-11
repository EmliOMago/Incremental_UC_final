using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDirector : MonoBehaviour
{
    public static GameDirector instancia;

    [Header("Referências de cena")]
    public LevelManenger levelManenger;
    public HUDManeger hudManeger;
    public SaveManagerPlayerPrefs saveManager;

    private void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += AoCarregarCena;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instancia == this)
            SceneManager.sceneLoaded -= AoCarregarCena;
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        AtualizarReferenciasDaCena();

        if (cena.name == "CenaJogo" && levelManenger != null)
            levelManenger.InicializarCenaJogo();
    }

    public void AtualizarReferenciasDaCena()
    {
        if (saveManager == null)
            saveManager = GetComponent<SaveManagerPlayerPrefs>();

        levelManenger = FindFirstObjectByType<LevelManenger>();
        hudManeger = FindFirstObjectByType<HUDManeger>();
    }
}
