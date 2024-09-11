using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RateLimitVisibility : MonoBehaviour
{
    [SerializeField]
    UIPanelBase m_target;
    [SerializeField]
    float m_alphaWhenHidden = 0.5f;
    [SerializeField]
    LobbyManager.EnumRequestType m_requestType;
    private void Start()
    {
        GameManager.Instance.LobbyManager.GetRateLimiter(m_requestType).onCooldownChange += UpdateVisibility;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null || GameManager.Instance.LobbyManager == null)
            return;
        GameManager.Instance.LobbyManager.GetRateLimiter(m_requestType).onCooldownChange -= UpdateVisibility;
    }

    void UpdateVisibility(bool isCollingDown)
    {
        if (isCollingDown)
            m_target.Hide(m_alphaWhenHidden);
        else
            m_target.Show();
    }
}
