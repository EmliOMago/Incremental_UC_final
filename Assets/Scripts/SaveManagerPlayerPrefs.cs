using UnityEngine;

public class SaveManagerPlayerPrefs : MonoBehaviour
{
    public void Salvar()
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        PlayerPrefs.SetFloat("dinheiro", GameDirector.instancia.levelManenger.dinheiro);
        PlayerPrefs.SetInt("qntMultiplicadores", GameDirector.instancia.levelManenger.qntMultiplicadores);
        PlayerPrefs.SetInt("qntGanhosPassivos", GameDirector.instancia.levelManenger.qntGanhosPassivos);
        PlayerPrefs.Save();
    }

    public void Carregar()
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        GameDirector.instancia.levelManenger.dinheiro = PlayerPrefs.GetFloat("dinheiro", 0);
        GameDirector.instancia.levelManenger.qntMultiplicadores = PlayerPrefs.GetInt("qntMultiplicadores", 0);
        GameDirector.instancia.levelManenger.qntGanhosPassivos = PlayerPrefs.GetInt("qntGanhosPassivos", 0);

        if (GameDirector.instancia.hudManeger != null)
        {
            GameDirector.instancia.hudManeger.AtualizarTudo();
        }
    }
}
