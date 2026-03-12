using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    private void Update()
    {
        if (!SceneManager.GetActiveScene().name.Equals("CenaMenu") || !Input.GetKeyDown(KeyCode.F12))
            return;

        SaveManagerPlayerPrefs.LimparSaveGlobal();
        Debug.Log("Save limpo no menu inicial.");
    }

    public void IniciarJogo()
    {
        SceneManager.LoadScene("CenaJogo");
    }

    public void VoltarMenu()
    {
        if (GameDirector.instancia != null && GameDirector.instancia.saveManager != null)
        {
            GameDirector.instancia.saveManager.Salvar();
        }

        SceneManager.LoadScene("CenaMenu");
    }

    public void SairJogo()
    {
        if (GameDirector.instancia != null && GameDirector.instancia.saveManager != null)
        {
            GameDirector.instancia.saveManager.Salvar();
        }

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
