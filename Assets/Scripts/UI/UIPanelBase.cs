using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(CanvasGroup))]//基础UI元素可以被显示或隐藏
public class UIPanelBase : MonoBehaviour
{
    [SerializeField]
    private UnityEvent<bool> m_onVisiblityChange;
    bool showing;

    CanvasGroup m_canvasGroup;
    //当该UIPanel被显示隐藏时,子UIPanel不知道要更新它们的可见性，故这里存储用于调用.
    List<UIPanelBase> m_uiPanelsInChildren = new List<UIPanelBase>();

    public virtual void Start()
    {
        var children = GetComponentsInChildren<UIPanelBase>(true);//注意，如果有运行时添加的子UIPanelBase,这里不会检测到。
        foreach (var child in children)
        {
            if (child != this)
                m_uiPanelsInChildren.Add(child);
        }
    }

    protected CanvasGroup MyCanvasGroup
    {
        get
        {
            if (m_canvasGroup == null)
                m_canvasGroup = GetComponent<CanvasGroup>();
            return m_canvasGroup;
        }
    }

    public void Toggle()
    {
        if (showing)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        Show(true);
    }

    public void Show(bool propagateToChildren)
    {
        MyCanvasGroup.alpha = 1;
        MyCanvasGroup.interactable = true;
        MyCanvasGroup.blocksRaycasts = true;
        showing = true;
        m_onVisiblityChange?.Invoke(true);
        if (!propagateToChildren)
            return;
        foreach (UIPanelBase child in m_uiPanelsInChildren)
            child.m_onVisiblityChange?.Invoke(true);
    }

    public void Hide()
    {
        Hide(0);
    }

    public void Hide(float targetAlpha)
    {
        MyCanvasGroup.alpha = targetAlpha;
        MyCanvasGroup.interactable = false;
        MyCanvasGroup.blocksRaycasts = false;
        showing = false;
        m_onVisiblityChange?.Invoke(false);
        foreach (UIPanelBase child in m_uiPanelsInChildren)
            child.m_onVisiblityChange?.Invoke(false);
    }
}
