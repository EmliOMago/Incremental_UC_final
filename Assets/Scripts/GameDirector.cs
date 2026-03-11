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
        if (instancia != null && instancia != this)
        {
            instancia.AssumirReferenciasDaCena(this);
            Destroy(this);
            return;
        }

        instancia = this;
        AtualizarReferenciasDaCena();
    }

    private void Start()
    {
        AtualizarReferenciasDaCena();

        if (levelManenger != null)
            levelManenger.InicializarCenaJogo();
    }

    private void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }

    private void AssumirReferenciasDaCena(GameDirector diretorDaCena)
    {
        if (diretorDaCena == null)
            return;

        if (diretorDaCena.saveManager != null)
            saveManager = diretorDaCena.saveManager;
        if (diretorDaCena.levelManenger != null)
            levelManenger = diretorDaCena.levelManenger;
        if (diretorDaCena.hudManeger != null)
            hudManeger = diretorDaCena.hudManeger;

        AtualizarReferenciasDaCena();
    }

    public void AtualizarReferenciasDaCena()
    {
        if (saveManager == null)
            saveManager = GetComponent<SaveManagerPlayerPrefs>();

        if (levelManenger == null)
            levelManenger = FindFirstObjectByType<LevelManenger>(FindObjectsInactive.Include);
        else if (levelManenger.gameObject.scene.name != SceneManager.GetActiveScene().name)
            levelManenger = FindFirstObjectByType<LevelManenger>(FindObjectsInactive.Include);

        if (hudManeger == null)
            hudManeger = FindFirstObjectByType<HUDManeger>(FindObjectsInactive.Include);
        else if (hudManeger.gameObject.scene.name != SceneManager.GetActiveScene().name)
            hudManeger = FindFirstObjectByType<HUDManeger>(FindObjectsInactive.Include);

        if (saveManager == null)
            saveManager = FindFirstObjectByType<SaveManagerPlayerPrefs>(FindObjectsInactive.Include);
    }
}
