using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField] private InputField InsNome;

    private void Update()
    {
        if (!SceneManager.GetActiveScene().name.Equals("CenaMenu") || !Input.GetKeyDown(KeyCode.F12))
            return;

        SaveManagerPlayerPrefs.LimparSaveGlobal();
        Debug.Log("Save limpo no menu inicial.");
    }

    public void AtualizarNome()
    {
        if (InsNome != null)
        {
            DadosJogador.NomeJogador = InsNome.text;
            Debug.Log(DadosJogador.NomeJogador);
        }
    }

    public void IniciarJogo()
    {
        AtualizarNome();
        Invoke("Comecar", 1f);
    }

    private void Comecar()
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
