using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class PopUpUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text m_popupText;
    [SerializeField]
    private CanvasGroup m_buttonVisibility;
    private float m_buttonVisibilityTimeout = -1;
    private StringBuilder m_currentText = new StringBuilder();

    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(false);
    }

    public void ShowPopup(string newText)
    {
        if (!gameObject.activeSelf)
        {
            m_currentText.Clear();
            gameObject.SetActive(true);
        }
        m_currentText.AppendLine(newText);
        m_popupText.SetText(m_currentText.ToString());
        DisableButton();
    }

    public void ClearPopup()
    {
        gameObject.SetActive(false);
    }

    private void DisableButton()
    {
        m_buttonVisibilityTimeout = 0.5f;
        m_buttonVisibility.alpha = 0.5f;
        m_buttonVisibility.interactable = false;
    }

    private void ReenableButton()
    {
        m_buttonVisibility.alpha = 1;
        m_buttonVisibility.interactable = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_buttonVisibilityTimeout >= 0)
        {
            m_buttonVisibilityTimeout -= Time.deltaTime;
            if (m_buttonVisibilityTimeout < 0)
            {
                ReenableButton();
            }
        }
    }
}
