using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
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
        EditorApplication.isPlaying = false;
#endif
    }
}
