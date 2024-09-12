using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorLobbyUI : MonoBehaviour
{
    public bool m_UseLocalLobby;
    static readonly Color s_orangeColor = new Color(0.83f, 0.36f, 0);
    static readonly Color s_greenColor = new Color(0, 0.61f, 0.45f);
    static readonly Color s_blueColor = new Color(0.0f, 0.44f, 0.69f);
    static readonly Color[] s_colorsOrdered = new Color[]
        { new Color(0.9f, 0.9f, 0.9f, 0.7f), s_orangeColor, s_greenColor, s_blueColor };
    [SerializeField]
    Graphic[] m_toRecolor;

    private void Start()
    {
        if (m_UseLocalLobby)
        {
            SetLobby(GameManager.Instance.LocalLobby);
        }
    }

    void ChangeColors(EnumLobbyColor lobbyColor)
    {
        Color color = s_colorsOrdered[(int)lobbyColor];
        foreach (Graphic graphic in m_toRecolor)
        {
            graphic.color = new Color(color.r, color.g, color.b, graphic.color.a);
        }
    }
    public void SetLobby(LocalLobby lobby)
    {
        ChangeColors(lobby.LocalLobbyColor.Value);
        lobby.LocalLobbyColor.onChanged += ChangeColors;
    }

    public void ToggleWhite(bool toggle)
    {
        if (!toggle) 
            return;

        GameManager.Instance.SetLocalLobbyColor(0);
    }
    public void ToggleOrange(bool toggle)
    {
        if (!toggle)
            return;

        GameManager.Instance.SetLocalLobbyColor(1);
    }
    public void ToggleGreen(bool toggle)
    {
        if (!toggle)
            return;

        GameManager.Instance.SetLocalLobbyColor(2);
    }
    public void ToggleBlue(bool toggle)
    {
        if (!toggle)
            return;

        GameManager.Instance.SetLocalLobbyColor(3);
    }
}
