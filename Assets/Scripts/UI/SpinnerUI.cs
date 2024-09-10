using TMPro;
using UnityEngine;

public class SpinnerUI : UIPanelBase
{
    [SerializeField] TMP_Text m_errorText;
    [SerializeField] UIPanelBase m_spinnerImage;
    [SerializeField] UIPanelBase m_noServerText;
    [SerializeField] UIPanelBase m_errorTextVisibility;
    [Tooltip("��spinner�ɼ�ʱ,��ֹѡ������һ������.")]
    [SerializeField] UIPanelBase m_raycastBlocker;

    public override void Start()
    {
        base.Start();
        Manager.LobbyList.QueryState.onChanged += QueryStateChanged;
    }

    private void OnDestroy()
    {
        if (Manager == null)
            return;
        Manager.LobbyList.QueryState.onChanged -= QueryStateChanged;
    }

    void QueryStateChanged(EnumLobbyQueryState state)
    {
        if (state == EnumLobbyQueryState.Fetching)
        {
            Show();
            m_spinnerImage.Show();
            m_raycastBlocker.Show();
            m_noServerText.Hide();
            m_errorTextVisibility.Hide();
        }
        else if (state == EnumLobbyQueryState.Error)
        {
            m_spinnerImage.Hide();
            m_raycastBlocker.Hide();
            m_errorTextVisibility.Show();
            m_errorText.SetText("����,��unity�����־��ȡ��ϸ��Ϣ��");
        }
        else if (state == EnumLobbyQueryState.Fetched)
        {
            if (Manager.LobbyList.CurrentLobbies.Count < 1)
            {
                m_noServerText.Show();
            }
            else
            {
                m_noServerText.Hide();
            }

            m_spinnerImage.Hide();
            m_raycastBlocker.Hide();
        }
    }
}