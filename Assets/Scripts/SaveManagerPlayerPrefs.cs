using UnityEngine;

public class SaveManagerPlayerPrefs : MonoBehaviour
{
    private const string ChaveDinheiro = "dinheiro";
    private const string ChaveHudMelhorias = "hud_melhorias_v2";

    public void Salvar()
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        PlayerPrefs.SetFloat(ChaveDinheiro, GameDirector.instancia.levelManenger.dinheiro);

        if (GameDirector.instancia.hudManeger != null)
            PlayerPrefs.SetString(ChaveHudMelhorias, GameDirector.instancia.hudManeger.ExportarSaveHUD());

        PlayerPrefs.Save();
    }

    public void Carregar()
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        GameDirector.instancia.levelManenger.dinheiro = PlayerPrefs.GetFloat(ChaveDinheiro, 0f);

        if (GameDirector.instancia.hudManeger != null)
        {
            string jsonHud = PlayerPrefs.GetString(ChaveHudMelhorias, string.Empty);
            GameDirector.instancia.hudManeger.AplicarSaveHUD(jsonHud);
            GameDirector.instancia.hudManeger.AtualizarTudo();
        }
    }

    public void CarregarOuCriar()
    {
        bool existeSave = PlayerPrefs.HasKey(ChaveDinheiro) || PlayerPrefs.HasKey(ChaveHudMelhorias);
        if (!existeSave)
        {
            if (GameDirector.instancia != null && GameDirector.instancia.levelManenger != null)
                GameDirector.instancia.levelManenger.dinheiro = 0f;

            GameDirector.instancia?.hudManeger?.AplicarSaveHUD(string.Empty);
            Salvar();
            return;
        }

        Carregar();
    }
}
