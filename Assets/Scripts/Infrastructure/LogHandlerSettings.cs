using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogHandlerSettings : MonoBehaviour
{
    [SerializeField]
    [Tooltip("只有在该级别或更高级别的日志,才会在日志控制台输出.")]
    private LogMode m_editorLogVerbosity = LogMode.Critical;

    [SerializeField]
    private PopUpUI m_popUp;

    static LogHandlerSettings s_LogHandlerSettings;
    public static LogHandlerSettings Instance
    {
        get
        {
            if (s_LogHandlerSettings != null) return s_LogHandlerSettings;
            return s_LogHandlerSettings = FindObjectOfType<LogHandlerSettings>();
        }
    }

    void OnValidate()
    {
        LogHandler.Get().mode = m_editorLogVerbosity;
        Debug.Log($"Starting project with Log Level : {m_editorLogVerbosity.ToString()}");
    }

    public void SpawnErrorPopup(string errorMessage)
    {
        m_popUp.ShowPopup(errorMessage);
    }
}
